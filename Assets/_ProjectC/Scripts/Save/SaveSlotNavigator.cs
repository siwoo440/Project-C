using UnityEngine; // 씬 전환 가능 여부 사용

public static class SaveSlotNavigator // 슬롯 복원과 씬 전환 연결
{ // 슬롯 이동 기능 시작
    public static bool Load(SaveSlotService service, int index) // 선택 슬롯 복원과 이동
    { // 슬롯 복원 이동 시작
        if (service == null || !service.CanLoad(index)) // 유효한 슬롯 저장 확인
        { // 저장 없음 처리 시작
            return false; // 기존 게임 상태 유지
        } // 저장 없음 처리 종료
        bool exploration = service.IsExploration(index); // 탐사 저장 여부 확인
        string targetScene = exploration ? "30_Exploration" : "20_Lobby"; // 복원 대상 씬 선택
        SceneFlowManager flow = SceneFlowManager.Instance; // 씬 전환 관리자 조회
        if (flow == null || flow.IsLoadingScene || !Application.CanStreamedLevelBeLoaded(targetScene)) // 씬 전환 가능 여부 확인
        { // 씬 이동 불가 처리 시작
            return false; // 기존 게임 상태 유지
        } // 씬 이동 불가 처리 종료
        if (!service.Load(index)) // 선택 슬롯 복원 확인
        { // 슬롯 복원 실패 처리 시작
            return false; // 씬 이동 중단
        } // 슬롯 복원 실패 처리 종료
        if (!exploration) // 거점 저장 복원 확인
        { // 이전 탐사 정리 시작
            MainMenuController.ResetRunState(); // 이전 탐사 회차 상태 제거
        } // 이전 탐사 정리 종료
        flow.LoadScene(targetScene); // 복원된 게임 씬으로 이동
        return true; // 복원과 이동 성공 반환
    } // 슬롯 복원 이동 종료
} // 슬롯 이동 기능 종료
