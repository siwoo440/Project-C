using System.Collections.Generic; // 선택지와 희귀 보상 목록 사용
using System.Linq; // 복원 가능한 공용 카드 조회
using UnityEngine; // 이벤트와 그림 원본 사용

public static class AshstormEyeEvent // 윈터폴 재폭풍의 눈 이벤트
{ // 이벤트 시작
    public const string Id = "winterfall_ashstorm_eye"; // 재폭풍의 눈 원본 식별자
    public const int Gaze = 0; // 응시 선택 순서
    public const int Shatter = 1; // 파괴 선택 순서
    public const int LookAway = 2; // 시선 회피 선택 순서
    private static bool resolving; // 결과 반영 중 같은 입력 재진입 방지

    internal static ExplorationEventData Create() // 이벤트 원본과 선택지 구성
    { // 원본 구성 시작
        var choices = new List<ExplorationEventChoiceData> // 세 선택지 구성
        { // 선택지 목록 시작
            Choice("응시한다", "눈을 들여다보는 순간, 세상이 한 박자 늦게 움직인다.\n당신이 숨을 쉬기 전에, 당신의 숨이 먼저 보인다.\n머릿속에 낯선 도식이 박히고, 손끝이 저절로 어떤 “형태”를 기억한다."), // 응시 결과
            Choice("돌로 깨뜨린다", "돌이 닿는 순간, 눈은 유리처럼 깨지지 않고 고기처럼 찢어진다.\n튄 파편이 살을 긁어 피가 나지만, 그 안에서 반짝이는 금속 조각이 떨어져 나온다."), // 파괴 결과
            Choice("눈을 피한다", "시선을 거두자 고요가 깨지고 바람이 다시 분다.\n눈은 여전히 그 자리에 있지만, 당신은 “보지 않았다”는 사실로 스스로를 붙잡는다.") // 시선 회피 결과
        }; // 선택지 목록 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 탐사 이벤트 원본 생성
        data.Initialize(Id, "재폭풍의 눈", "재폭풍이 몰아치다, 갑자기 주변이 고요해진다.\n원형으로 재가 말려 올라간 공간 한가운데, 검은 유리 같은 “눈”이 떠 있다.\n눈은 움직이지 않는데도… 당신이 들여다보고 있다는 사실을 알고 있다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/AshstormEye"), choices); // 원문과 전용 삽화 연결
        return data; // 재폭풍의 눈 원본 반환
    } // 원본 구성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 행동 문구와 결과 연결
    { // 선택지 생성 시작
        var choice = new ExplorationEventChoiceData(); // 선택지 생성
        choice.Initialize(label, story, null); // 선택 문구와 결과 원문 지정
        return choice; // 선택지 반환
    } // 선택지 생성 종료

    public static ShopOfferData GetRareReward(string runtimeId) // 현재 이벤트의 실제 희귀 공용 카드 조회
    { // 보상 조회 시작
        ShopCatalogData offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 획득 가능한 카드 상품 조회
        SaveAssetCatalog saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 복원 가능한 카드 원본 조회
        CardSheetCatalog sheet = CardSheetCatalog.Load(); // 카드 기획 시트 조회
        if (offers == null || saved == null || sheet == null || string.IsNullOrWhiteSpace(runtimeId)) // 보상 원본과 이벤트 식별자 확인
        { // 누락 처리 시작
            return null; // 보상 없음 반환
        } // 누락 처리 종료
        var candidates = new List<ShopOfferData>(); // 지급 가능한 희귀 공용 카드 목록
        var seen = new HashSet<CardData>(); // 중복 상품의 카드 확률 중복 방지
        foreach (ShopOfferData offer in offers.Offers) // 실제 카드 상품 순회
        { // 후보 확인 시작
            if (offer != null && offer.IsValidData() && offer.RequiresOwnerChoice && saved.Cards.Contains(offer.Card) && sheet.FindByName(offer.DisplayName)?.Rarity == "희귀" && seen.Add(offer.Card)) // 소유자 선택과 저장 등록 및 시트 희귀도 확인
            { // 희귀 카드 등록 시작
                candidates.Add(offer); // 실제 지급 후보 추가
            } // 희귀 카드 등록 종료
        } // 후보 확인 종료
        candidates.Sort((left, right) => string.CompareOrdinal(left.Card.CardId, right.Card.CardId)); // 원본 식별자 순서 고정
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(runtimeId)); // 재시도와 불러오기용 동일 난수
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : null; // 등록된 희귀 카드 한 장 선택
    } // 보상 조회 종료

    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, CharacterData owner, out string summary) // 행동자와 소유자 확정 후 결과 한 번 적용
    { // 이벤트 결과 처리 시작
        summary = string.Empty; // 미확정 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 조회
        var party = BattleResultManager.EnsureInstance(); // 전투와 탐사가 공유하는 파티 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(runtimeId) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(runtimeId) || !party.IsLivingPartyMember(actor) || choice < Gaze || choice > LookAway) // 현재 선택이 유효한 미처리 행동인지 확인
        { // 잘못된 선택 처리 시작
            return false; // 비용과 보상 변경 방지
        } // 잘못된 선택 처리 종료
        resolving = true; // 덱과 상태 갱신 알림 중 중복 적용 차단
        try // 중복 잠금 해제 보장
        { // 선택 결과 확정 시작
            party.TryGetSavedAllyState(actor, out int health, out int mental, out _); // 효과 전 행동자 수치 확인
            switch (choice) // 선택한 행동 구분
            { // 행동별 적용 시작
                case Gaze: // 희귀 공용 카드 획득
                    ShopOfferData reward = GetRareReward(runtimeId); // 현재 방 고정 보상 조회
                    if (reward == null || !party.ActiveParty.ContainsCharacter(owner) || !RunDeckManager.EnsureInstance().TryAddCard(reward.Card, owner)) // 실제 소유자 덱에 카드 한 장 추가 확인
                    { // 카드 지급 실패 시작
                        return false; // 정신력과 이벤트 보존
                    } // 카드 지급 실패 종료
                    party.ApplyEventChange(actor, 0, -8); // 응시한 생존자의 정신력 여덟 감소
                    summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Max(BattleMentalRuntime.MinimumMental, mental - 8)} · {owner.DisplayName} 카드 [{reward.DisplayName}] 획득"; // 행동자와 카드 소유자 구분 표시
                    break; // 응시 처리 종료
                case Shatter: // 체력을 치르고 재료 획득
                    party.ApplyEventChange(actor, 7, 0); // 돌을 던진 생존자의 체력 일곱 감소
                    PlayerResourceManager.EnsureInstance().AddResources(0, 1, 1, 0); // 나사와 철판 한 개씩 지급
                    summary = $"{actor.DisplayName} 체력 {health} → {Mathf.Max(0, health - 7)} · 철판 +1 · 나사 +1"; // 행동자 피해와 획득 재료 표시
                    break; // 파괴 처리 종료
                default: // 시선을 피하고 정신력 회복
                    party.ApplyEventChange(actor, 0, 1); // 시선을 피한 생존자의 정신력 하나 회복
                    summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Min(BattleMentalRuntime.MaximumMental, mental + 1)}"; // 상한을 반영한 실제 회복 표시
                    break; // 회피 처리 종료
            } // 행동별 적용 종료
            session.MarkEventResolved(runtimeId); // 보상과 비용 확정 후 현재 방 완료 기록
            if (choice == Shatter && party.IsActivePartyWiped()) // 체력 피해 후 전멸 여부 확인
            { // 전멸 정산 시작
                session.CompleteExplorationFailure("재폭풍의 눈 파편에 마지막 생존자가 쓰러졌습니다."); // 탐사 획득 재화 회수와 실패 기록
            } // 전멸 정산 종료
            return true; // 선택 반영 성공 반환
        } // 선택 결과 확정 종료
        finally // 재시도 가능 상태 복구
        { // 중복 잠금 해제 시작
            resolving = false; // 이번 결과 처리 종료
        } // 중복 잠금 해제 종료
    } // 이벤트 결과 처리 종료
} // 이벤트 종료
