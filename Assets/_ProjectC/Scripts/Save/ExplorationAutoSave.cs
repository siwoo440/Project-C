using UnityEngine; // 탐사 위치 사용

public static class ExplorationAutoSave // 전투 전후 자동 저장 지점
{ // 탐사 자동 저장 시작
    public static bool SaveBeforeBattle(Vector3 position, string runPath = null) // 전투 시작 전 탐사 상태 저장
    { // 전투 전 저장 시작
        return ExplorationSaveService.Save(runPath ?? ExplorationSaveService.DefaultPath, position); // 전투 시작 전 탐사 저장 결과 반환
    } // 전투 전 저장 종료
    public static bool SaveCurrentExploration(string runPath = null) // 보상이나 거래 후 현재 탐사 상태 저장
    { // 현재 탐사 상태 저장 시작
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회
        if (player == null) // 탐사 플레이어 존재 확인
        { // 위치 누락 처리 시작
            return false; // 잘못된 위치 저장 방지
        } // 위치 누락 처리 종료
        return ExplorationSaveService.Save(runPath ?? ExplorationSaveService.DefaultPath, player.transform.position); // 현재 위치와 결과가 반영된 탐사 저장
    } // 현재 탐사 상태 저장 종료
    public static bool SaveAfterBattle(Vector3 position, string runPath = null, string lobbyPath = null) // 전투 결과 반영 후 상태 저장
    { // 전투 후 저장 시작
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 현재 탐사 상태 조회
        if (session == null) // 탐사 세션 존재 확인
        { // 세션 누락 처리 시작
            return false; // 자동 저장 실패 반환
        } // 세션 누락 처리 종료
        string explorationFile = runPath ?? ExplorationSaveService.DefaultPath; // 탐사 자동 저장 경로 결정
        if (!session.IsExplorationCompleted) // 탐사 진행 상태 확인
        { // 진행 중 탐사 저장 시작
            return ExplorationSaveService.Save(explorationFile, position); // 보상과 클리어 상태가 반영된 탐사 저장
        } // 진행 중 탐사 저장 종료
        if (!LobbySaveService.Save(lobbyPath ?? LobbySaveService.DefaultPath)) // 탐사 종료 후 거점 상태 저장 확인
        { // 거점 저장 실패 처리 시작
            return false; // 기존 탐사 저장 보존
        } // 거점 저장 실패 처리 종료
        return ExplorationSaveService.Remove(explorationFile); // 거점 저장 성공 후 이전 탐사 저장 정리
    } // 전투 후 저장 종료
} // 탐사 자동 저장 종료
