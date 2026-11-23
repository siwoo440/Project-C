using System.Collections.Generic; // 원문 선택지 목록 사용
using UnityEngine; // 사건 원본과 전용 그림 사용

public static class BurntScarecrowEvent // 윈터폴 타버린 허수아비
{ // 사건 시작
    public const string Id = "winterfall_burnt_scarecrow"; // 이벤트 원본 식별자
    public const int Investigate = 0; // 허수아비 조사 선택
    public const int Burn = 1; // 허수아비 소각 선택
    public const int Pass = 2; // 변화 없이 지나치는 선택
    private static bool resolving; // 변경 알림 중 같은 선택 재진입 차단

    internal static ExplorationEventData Create() // 기획서의 허수아비 사건 구성
    { // 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData> // 기획서 순서의 세 행동
        { // 선택 목록 시작
            Choice("조사한다", "허수아비의 가슴팍을 손으로 밀치자, 타버린 짚이 바스러지며 검은 가루가 손가락 틈으로 스민다.\n그 순간, 당신의 머릿속에 누군가의 웃음이 끼어든다.\n너무 낯설어서, 오히려 당신 것처럼 느껴진다."), // 조사 결과 원문
            Choice("불태운다", "불꽃이 붙는 순간, 허수아비는 스스로 타오르는 것처럼 크게 번진다.\n타는 소리 사이로, 누군가 아주 작은 목소리로 “고맙다”라고 말한 것 같다.\n재가 되어 흩어지며, 가슴의 답답함이 조금 풀린다."), // 소각 결과 원문
            Choice("지나친다", "등을 돌리는 순간, 바람이 한 번 더 불고 허수아비의 팔이 살짝 움직인다.\n당신은 보지 못했지만, 허수아비는 끝까지 당신을 바라본다.") // 지나침 결과 원문
        }; // 선택 목록 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 탐사 사건 원본 생성
        data.Initialize(Id, "타버린 허수아비", "검게 탄 허수아비가 길 한가운데 서 있다.\n바람이 불어도 움직이지 않는다.\n그런데 가까이 갈수록, 허수아비의 머리가 아주 조금씩 당신을 따라 도는 것 같다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/BurntScarecrow"), choices); // 원문 본문과 전용 삽화 연결
        return data; // 탐사에 배치할 사건 반환
    } // 원본 생성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 행동과 결말 연결
    { // 선택지 생성 시작
        var item = new ExplorationEventChoiceData(); // 한 선택지 생성
        item.Initialize(label, story, null); // 행동 이름과 원문 결말 지정
        return item; // 사건 선택지 반환
    } // 선택지 생성 종료

    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, out string summary) // 해당 방의 선택 재검증과 결과 적용
    { // 사건 처리 시작
        summary = string.Empty; // 미확정 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 탐사 조회
        var party = BattleResultManager.EnsureInstance(); // 출전 파티 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(runtimeId) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(runtimeId) || choice < Investigate || choice > Pass || party.GetLivingAllyCount() < 1) // 처리 가능한 미완료 탐사 선택 확인
        { // 잘못된 입력 처리 시작
            return false; // 중복 지급과 종료된 탐사 방지
        } // 잘못된 입력 처리 종료
        if (choice != Pass && !party.IsLivingPartyMember(actor)) // 비용을 지불할 출전 생존자 확인
        { // 행동자 누락 처리 시작
            return false; // 행동자 비용과 완료 상태 보존
        } // 행동자 누락 처리 종료
        resolving = true; // 덱과 파티 변경 알림 중 재진입 차단
        try // 최종 입력 잠금 해제 보장
        { // 선택별 결과 시작
            if (choice == Investigate) // 조사 보상과 정신력 비용 확인
            { // 조사 처리 시작
                if (!WinterfallRiskCardReward.TryGrant(runtimeId, actor, out CardData card)) // 행동자 본인의 덱에 카드 한 장 지급
                { // 지급 실패 처리 시작
                    return false; // 보상 없이 정신력 차감 방지
                } // 지급 실패 처리 종료
                party.TryGetSavedAllyState(actor, out _, out int mental, out _); // 조사 전 정신력 조회
                party.ApplyEventChange(actor, 0, -4); // 조사자 정신력 네 점 소모
                party.TryGetSavedAllyState(actor, out _, out int afterMental, out _); // 실제 조사 후 정신력 조회
                summary = $"{actor.DisplayName} 정신력 {mental} → {afterMental} · [{card.DisplayName}] 1장 획득"; // 행동자 비용과 카드 결과 표시
            } // 조사 처리 종료
            else if (choice == Burn) // 소각 비용과 본인의 저주 확인
            { // 소각 처리 시작
                party.TryGetSavedAllyState(actor, out int health, out _, out _); // 소각 전 행동자 체력 조회
                bool removed = RunDeckManager.EnsureInstance().TryRemoveAshCurse(actor); // 행동자 자신의 저주 한 장 제거
                party.ApplyEventChange(actor, 4, 0); // 소각한 생존자 체력 네 점 소모
                party.TryGetSavedAllyState(actor, out int afterHealth, out _, out _); // 실제 소각 후 체력 조회
                summary = $"{actor.DisplayName} 체력 {health} → {afterHealth} · " + (removed ? "잿빛 저주 1장 제거" : "제거할 잿빛 저주 없음"); // 실제 소각 결과 표시
            } // 소각 처리 종료
            else // 지나침 선택 확인
            { // 지나침 처리 시작
                summary = "변화 없음"; // 비용과 보상 없는 결과 표시
            } // 지나침 처리 종료
            session.MarkEventResolved(runtimeId); // 카드와 비용 확정 후 방문 완료 기록
            if (choice == Burn && party.IsActivePartyWiped()) // 소각으로 마지막 생존자 사망 확인
            { // 전멸 정산 시작
                session.CompleteExplorationFailure("타버린 허수아비를 불태운 마지막 생존자가 쓰러졌습니다."); // 탐사 실패와 거점 복귀 정산
            } // 전멸 정산 종료
            return true; // 사건 처리 완료
        } // 선택별 결과 종료
        finally // 사건 재사용 잠금 정리
        { // 잠금 정리 시작
            resolving = false; // 다음 방의 선택 허용
        } // 잠금 정리 종료
    } // 사건 처리 종료
} // 사건 종료
