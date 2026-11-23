using System.Collections.Generic; // 우물 행동 목록 구성
using System.Linq; // 저장 가능한 체력 회복 포션 조회
using UnityEngine; // 사건과 전용 삽화 원본 생성

public static class CharredWellEvent // 윈터폴 검게 탄 우물
{ // 우물 사건 시작
    public const string Id = "winterfall_charred_well"; // 검게 탄 우물 원본 식별자
    public const int Dig = 0; // 재를 파내는 행동
    public const int DrawWater = 1; // 물을 뜨는 행동
    public const int Seal = 2; // 우물을 봉인하는 행동
    public const string PotionId = "POTION_TEST_HEAL"; // 기존 체력 회복 포션 원본 식별자
    private static bool resolving; // 상태 변경 알림 중 중복 입력 방지

    internal static ExplorationEventData Create() // 기획서 원문으로 사건 구성
    { // 원본 구성 시작
        var choices = new List<ExplorationEventChoiceData> // 원문 순서의 세 행동
        { // 선택 목록 시작
            Choice("재를 파낸다", "재를 파는 동안, 손등에 검은 가루가 달라붙는다.\n마치 상처를 찾아 기어오르는 것처럼.\n끝내 바닥에서 축축한 병 하나를 건져 올린다.\n병은 차갑지 않은데도 손이 떨린다."), // 회복 아이템 획득 결말
            Choice("물을 뜨려 한다", "물은 떠지지 않는다.\n대신 손바닥 위로 검은 액체가 얇게 퍼진다.\n머릿속이 잠깐 비어버리고, 그 빈자리에 “살아남는 법” 하나가 들어온다."), // 즉시 회복 예약 결말
            Choice("우물을 봉인한다", "돌을 밀어 넣고 재를 덮자, 목소리는 멀어진다.\n이상하게도 주변 바람이 조금 잦아든 것 같다.\n당신은 “지금은 충분하다”라고 중얼거린다.") // 다음 층 봉인 결말
        }; // 선택 목록 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 탐사 방 사건 생성
        data.Initialize(Id, "검게 탄 우물", "우물은 타버린 마을의 중심에 있다.\n재로 메워졌는데도, 안쪽에서 물소리가 들린다.\n돌을 떨어뜨리면… 닿기 전에 소리가 멈춘다.\n그리고 누군가 아주 작은 목소리로 말한다.\n“더…”", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/CharredWell"), choices); // 원문 본문과 전용 삽화 연결
        return data; // 탐사 후보 반환
    } // 원본 구성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 선택 행동과 원문 결말 연결
    { // 행동 원본 생성 시작
        var choice = new ExplorationEventChoiceData(); // 행동 선택지 생성
        choice.Initialize(label, story, null); // 원문 이름과 내용 지정
        return choice; // 실제 선택지 반환
    } // 행동 원본 생성 종료

    public static bool TryResolve(string room, int choice, CharacterData actor, out string summary) // 생존 행동자의 비용과 우물 보상 한 번 적용
    { // 사건 처리 시작
        summary = string.Empty; // 미확정 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 조회
        var party = BattleResultManager.EnsureInstance(); // 행동자 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(room) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(room) || choice < Dig || choice > Seal || !party.IsLivingPartyMember(actor)) // 현재 탐사와 미완료 방 및 생존자 확인
        { // 유효하지 않은 선택 시작
            return false; // 상태 변경과 재지급 차단
        } // 유효하지 않은 선택 종료
        resolving = true; // 보관함 및 파티 알림 중 재입력 차단
        try // 입력 잠금 해제 보장
        { // 선택별 결과 시작
            party.TryGetSavedAllyState(actor, out int health, out int mental, out _); // 선택 전 실제 수치 조회
            if (choice == Dig) // 재 속 병 발견 처리
            { // 포션 지급 시작
                var potion = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog")?.Consumables.FirstOrDefault(item => item != null && item.ItemId == PotionId); // 저장 가능한 기존 체력 포션 조회
                if (potion == null || !ConsumableRunManager.EnsureInstance().TryAcquire(potion, out _)) // 빈 슬롯과 실제 지급 확인
                { // 지급 불가 시작
                    summary = "체력 회복 포션을 받을 빈 슬롯이 필요합니다."; // 비용 없는 재선택 안내
                    return false; // 포션 없이 체력 소모 방지
                } // 지급 불가 종료
                party.ApplyEventChange(actor, 5, 0); // 행동자 체력 다섯 소모
                party.TryGetSavedAllyState(actor, out int afterHealth, out _, out _); // 실제 남은 체력 조회
                summary = $"{actor.DisplayName} 체력 {health} → {afterHealth} · {potion.DisplayName} 1개 획득"; // 체력과 아이템 결과 안내
            } // 포션 지급 종료
            else if (choice == DrawWater) // 행동자 한 번 회복 예약
            { // 물 보상 시작
                if (!session.Well.TryReserveWater(actor.CharacterId)) // 동일 생존자의 미사용 물 확인
                { // 중복 예약 시작
                    summary = "이 생존자에게 아직 사용하지 않은 우물 회복이 있습니다."; // 비용 없는 다른 행동자 선택 안내
                    return false; // 기존 효과와 정신력 보존
                } // 중복 예약 종료
                party.ApplyEventChange(actor, 0, -6); // 행동자 정신력 여섯 소모
                party.TryGetSavedAllyState(actor, out _, out int afterMental, out _); // 실제 남은 정신력 조회
                summary = $"{actor.DisplayName} 정신력 {mental} → {afterMental} · 생존 피격 시 자신의 체력 8 회복 1회"; // 행동자에게 연결된 회복 안내
            } // 물 보상 종료
            else // 다음 층 재폭풍 봉인 선택
            { // 봉인 예약 시작
                if (!session.Well.TrySealNextFloor(room)) // 다음 층에 대기 중인 봉인 확인
                { // 중복 봉인 시작
                    summary = "다음 층에 적용할 우물 봉인이 이미 있습니다."; // 다른 행동 선택 안내
                    return false; // 정신력 중복 회복 방지
                } // 중복 봉인 종료
                party.ApplyEventChange(actor, 0, 4); // 행동자 정신력 네 점 회복
                party.TryGetSavedAllyState(actor, out _, out int afterMental, out _); // 실제 회복 후 정신력 조회
                summary = $"{actor.DisplayName} 정신력 {mental} → {afterMental} · 다음 층의 첫 재폭풍의 눈 교체 확률 15%"; // 행동자 회복과 다음 층 효과 안내
            } // 봉인 예약 종료
            session.MarkEventResolved(room, true); // 보상과 비용 및 정상 해결의 구역 수치 기록
            if (choice == Dig && party.IsActivePartyWiped()) // 재를 파내다 마지막 생존자 사망 확인
            { // 전멸 정산 시작
                session.CompleteExplorationFailure("검게 탄 우물의 재를 파낸 마지막 생존자가 쓰러졌습니다."); // 탐사 전멸과 복귀 정산
            } // 전멸 정산 종료
            return true; // 우물 선택 확정
        } // 선택별 결과 종료
        finally // 다음 선택을 위한 잠금 정리
        { // 잠금 정리 시작
            resolving = false; // 후속 입력 허용
        } // 잠금 정리 종료
    } // 사건 처리 종료
} // 우물 사건 종료
