using System.Linq; // 실제 사용 가능한 카드 보상 수량 조회

internal static class ExplorationRegionalRewards // 묻힌 도시의 문장의 실제 탐사 사건 보상
{ // 지역 보상과 한 번 선택 정산 시작
    internal enum Option // 기존 보물에서 지급할 수 있는 실제 자원 선택
    { // 분리된 네 가지 보상 표시 시작
        Gold, // 기존 보물 골드 보상
        Screw, // 기존 보물 제작 재료 나사
        IronPlate, // 기존 보물 제작 재료 철판
        Wire // 기존 보물 제작 재료 전선
    } // 분리된 네 가지 보상 표시 종료

    internal readonly struct RewardState // 정상 사건 선택 전 실제 보관 자원과 품목 수량
    { // 사건의 선택 전후 지급 수량 비교 시작
        private readonly int gold, screw, ironPlate, wire, cards, relics, consumables; // 원래 보상 지급을 확인하는 실제 수량

        internal RewardState(PlayerResourceManager resources) // 보유 유물이 있는 사막 사건에서만 실제 수량 수집
        { // 실제 현재 회차의 지급 출처 조회 시작
            gold = resources?.Gold ?? 0; // 골드 표시 및 지갑의 현재 양
            screw = resources?.Screw ?? 0; // 회차 보관 중인 나사 수량
            ironPlate = resources?.IronPlate ?? 0; // 회차 보관 중인 철판 수량
            wire = resources?.Wire ?? 0; // 회차 보관 중인 전선 수량
            cards = RunDeckManager.Instance?.Cards.Count(entry => entry.Card.EffectType != CardEffectType.Unplayable && entry.Card.CardId != WinterfallRiskCardReward.CurseCardId) ?? 0; // 사용할 수 있는 실제 회차 카드 수량
            relics = RelicRunManager.Instance?.Inventory.OwnedRelics.Count ?? 0; // 새로 실제 지급받은 유물을 확인할 수량
            consumables = ConsumableRunManager.Instance?.Inventory.Count ?? 0; // 실제 슬롯에 보관한 소모품 수량
        } // 실제 현재 회차의 지급 출처 조회 종료

        internal bool GainsRewardComparedWith(RewardState before) // 실제 선택 결과에 긍정적인 보상 변화가 생겼는지 확인
        { // 순수 비용이나 손실만 있는 선택 검사 시작
            return gold > before.gold || screw > before.screw || ironPlate > before.ironPlate || wire > before.wire || // 실제 제작 자원이나 골드 증가 확인
                cards > before.cards || relics > before.relics || consumables > before.consumables; // 실제 덱 또는 인벤토리 보상 증가 확인
        } // 순수 비용이나 손실만 있는 선택 검사 종료
    } // 사건의 선택 전후 지급 수량 비교 종료

    internal static RewardState? BeginEvent(ExplorationEventData data, string runtimeId) // 확정 전에 보유한 문장과 실제 사막 지역 확인
    { // 일반 사건의 추가 보상 후보 판정 시작
        var session = ExplorationSessionManager.Instance; // 이미 진행 중인 탐사만 조회
        if (data == null || data.Region != ExplorationRegion.Desert || !data.IsValidData() || string.IsNullOrWhiteSpace(runtimeId) || // 실제 지역이 지정된 유효 원본과 방 확인
            session == null || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(runtimeId) || session.BuriedCityCrestEventId != null || // 완료한 실제 사건과 현재 회차의 최초 기회 확인
            RelicRunManager.Instance?.Inventory.FindEffect(RelicEffectType.BuriedCityCrestDesertExtraReward) == null) // 현재 소지한 실제 유물 효과 확인
        { // 보상 관찰 조건 실패 시작
            return null; // 원래 이벤트만 평소대로 처리
        } // 보상 관찰 조건 실패 종료
        return new RewardState(PlayerResourceManager.Instance); // 이 선택 결과에서 비교할 실제 지급 전 상태
    } // 일반 사건의 추가 보상 후보 판정 종료

    internal static void FinishEvent(string runtimeId, RewardState? before) // 원래 선택 지급과 방 완료 뒤에만 보상 후보 등록
    { // 실제 사건 결과와 새 보상 자격 확인 시작
        if (before.HasValue && new RewardState(PlayerResourceManager.Instance).GainsRewardComparedWith(before.Value)) // 기존 결과가 실제 보상 한 가지 이상을 지급했는지 확인
        { // 실제 보상 선택의 회차 대기 기록 시작
            ExplorationSessionManager.Instance?.TryQueueBuriedCityCrestReward(runtimeId); // 같은 완료 원본으로 회차 중 한 번의 대기 기록 등록
        } // 실제 보상 선택의 회차 대기 기록 종료
    } // 실제 사건 결과와 새 보상 자격 확인 종료

    internal static string GetOptionText(Option option) // 현재 지갑에서 실제로 받을 보상 표시
    { // 기존 지급 상수와 같은 표시 내용 시작
        return option switch // 네 가지 실제 자원 보상의 현지화 표시
        { // 지급량 일치 표시 분기 시작
            Option.Gold => $"골드 +{ExplorationTreasureRewardService.GoldRewardAmount}", // 기존 보물 골드의 실제 양 표시
            Option.Screw => $"나사 +{ExplorationTreasureRewardService.ResourceRewardAmount}", // 기존 제작 보상과 같은 나사 양 표시
            Option.IronPlate => $"철판 +{ExplorationTreasureRewardService.ResourceRewardAmount}", // 기존 제작 보상과 같은 철판 양 표시
            Option.Wire => $"전선 +{ExplorationTreasureRewardService.ResourceRewardAmount}", // 기존 제작 보상과 같은 전선 양 표시
            _ => null // 정의하지 않은 보상 입력 차단
        }; // 지급량 일치 표시 분기 종료
    } // 기존 지급 상수와 같은 표시 내용 종료

    internal static bool TryClaim(string runtimeId, Option option) // 완료한 원래 방의 추가 보상 한 번 선택
    { // 선택과 실제 지급이 한 경로에서 실행되는 정산 시작
        var session = ExplorationSessionManager.Instance; // 원래 대기 기록을 가진 탐사 회차 조회
        var resources = PlayerResourceManager.Instance; // 실제 탐사에서 이미 준비한 회차 자원 지갑 확인
        if (GetOptionText(option) == null || resources == null || session == null || !session.TryClaimBuriedCityCrestReward(runtimeId)) // 유효한 보상과 원래 대기 방의 단 한 번 정산 확인
        { // 부정확한 선택 또는 중복 클릭 차단 시작
            return false; // 실제 지급이나 추가 소비 없음
        } // 부정확한 선택 또는 중복 클릭 차단 종료
        int amount = ExplorationTreasureRewardService.ResourceRewardAmount; // 기존 제작 보물과 같은 표기 및 지급 수량
        resources.AddResources( // 기존 회차 구분을 같이 기록하는 실제 자원 지갑으로 지급
            option == Option.Gold ? ExplorationTreasureRewardService.GoldRewardAmount : 0, // 선택한 경우 기존 보물 골드 지급
            option == Option.Screw ? amount : 0, // 선택한 경우 기존 나사 보물 지급
            option == Option.IronPlate ? amount : 0, // 선택한 경우 기존 철판 보물 지급
            option == Option.Wire ? amount : 0); // 선택한 경우 기존 전선 보물 지급
        return true; // 회차의 보상 선택과 지갑 지급 완료
    } // 선택과 실제 지급이 한 경로에서 실행되는 정산 종료
} // 지역 보상과 한 번 선택 정산 종료
