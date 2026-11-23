using System.Collections.Generic; // 원문 선택지 목록 사용
using UnityEngine; // 이벤트 원본과 일러스트 사용

public enum MapFragmentContact // 지도 조각과 전투 접촉 처리 결과
{ // 접촉 결과 시작
    NoEffect, // 회피 판정 대상 아님
    Avoided, // 회피 성공과 탐사 저장 완료
    Battle, // 회피 실패와 전투 직전 저장 완료
    SaveFailed // 저장 실패와 조각 사용 취소
} // 접촉 결과 종료

public static class AshMapFragmentEvent // 윈터폴 재 속의 지도 조각
{ // 지도 조각 이벤트 시작
    public const string Id = "winterfall_ash_map_fragment"; // 지도 조각 원본 식별자
    public const int PickUp = 0; // 조각 줍기 선택 순서
    public const int Burn = 1; // 조각 태우기 선택 순서
    public const int Tear = 2; // 조각 찢기 선택 순서
    private static bool resolving; // 상태 변경 알림 중 중복 선택 방지

    internal static ExplorationEventData Create() // 원문 세 행동과 전용 삽화 연결
    { // 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData> // 지도 조각 행동 목록
        { // 원문 선택지 시작
            Choice("줍는다", "종이를 집는 순간, 손끝이 뜨거워진다.\n지도는 길을 알려주는 게 아니라, 길을 강요하는 것 같다.\n그래도 한 줄의 방향이 생기자, 발걸음이 가벼워진다."), // 조각 획득 결말
            Choice("태운다", "종이는 재가 아니라, 검은 연기로 타오른다.\n그 연기가 하늘로 오르며 잠깐 형태를 만든다.\n당신은 그 형태를 보지 않으려 하며, 남은 금속 클립만 챙긴다."), // 정신력과 나사 획득 결말
            Choice("찢어버린다", "찢는 순간, 종이 안쪽에서 바스러진 글씨가 떨어진다.\n“돌아오지 마.”\n당신은 그 문장을 들은 적 없는 것처럼 주머니를 여민다.") // 변화 없는 결말
        }; // 원문 선택지 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 바로 배치할 이벤트 생성
        data.Initialize(Id, "재 속의 지도 조각", "찢어진 종이 조각이 재에 반쯤 묻혀 있다.\n잿빛으로 얼룩졌지만, 선명한 펜 선이 남아 있다.\n그 선은 현재 위치가 아니라… 당신이 가야 할 위치를 가리키는 듯하다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/AshMapFragment"), choices); // 원문 본문과 전용 그림 설정
        return data; // 실제 탐사 후보 원본 반환
    } // 원본 생성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 행동 이름과 결과 연결
    { // 선택지 생성 시작
        var choice = new ExplorationEventChoiceData(); // 원본 행동 생성
        choice.Initialize(label, story, null); // 기획서 행동과 결말 지정
        return choice; // 행동 원본 반환
    } // 선택지 생성 종료

    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, out string summary) // 무효 선택을 제외하고 한 번만 결과 적용
    { // 행동 처리 시작
        summary = string.Empty; // 미확정 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 진행 상태 조회
        var allies = BattleResultManager.EnsureInstance(); // 생존 파티 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(runtimeId) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(runtimeId) || choice < PickUp || choice > Tear || allies.GetLivingAllyCount() < 1) // 유효한 미처리 탐사 행동 확인
        { // 미처리 조건 실패 시작
            return false; // 재지급과 종료된 탐사 차단
        } // 미처리 조건 실패 종료
        resolving = true; // 파티원과 재화 변경 중 재진입 차단
        try // 결과 처리 후 선택 잠금 정리
        { // 실제 행동 적용 시작
            switch (choice) // 지도 조각 행동 구분
            { // 행동별 적용 시작
                case PickUp: // 다음 일반 전투에 지도 조각 예약
                    if (!session.TryKeepMapFragment(runtimeId)) // 보관 중인 이전 조각 확인
                    { // 중복 보관 처리 시작
                        summary = "이미 지도 조각을 가지고 있습니다. 다른 행동을 선택하세요."; // 현재 방을 다시 선택할 수 있도록 안내
                        return false; // 이전 대기 효과와 현재 방 보존
                    } // 중복 보관 처리 종료
                    summary = "다음 일반 전투에서 40% 확률로 전투 회피 · 1회"; // 실제 발동 조건 안내
                    break; // 줍기 처리 종료
                case Burn: // 나사 획득과 행동자 정신력 회복
                    if (!allies.IsLivingPartyMember(actor)) // 선택한 생존 파티원 확인
                    { // 행동자 선택 실패 시작
                        return false; // 미확정 대상의 비용과 보상 보존
                    } // 행동자 선택 실패 종료
                    allies.TryGetSavedAllyState(actor, out _, out int mental, out _); // 행동자 정신력 조회
                    allies.ApplyEventChange(actor, 0, 2); // 행동자 정신력 두 점 회복
                    PlayerResourceManager.EnsureInstance().AddResources(0, 1, 0, 0); // 파티 나사 한 개 지급
                    summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Min(BattleMentalRuntime.MaximumMental, mental + 2)} · 나사 +1"; // 실제 정신력과 보상 안내
                    break; // 태우기 처리 종료
                default: // 지도 조각 찢기
                    break; // 추가 상태 변화 없이 완료
            } // 행동별 적용 종료
            session.MarkEventResolved(runtimeId, true); // 정상 완료한 일반 사건의 구역 수치 기록
            return true; // 선택 결과 적용 성공
        } // 실제 행동 적용 종료
        finally // 알림 처리 후 잠금 정리
        { // 선택 잠금 해제 시작
            resolving = false; // 다음 방의 선택 허용
        } // 선택 잠금 해제 종료
    } // 행동 처리 종료
} // 지도 조각 이벤트 종료
