using System.Collections; // 실제 탐사 화면 대기 사용
using NUnit.Framework; // 탐사 화면 구성 검증 사용
using UnityEngine; // 실제 탐사 화면 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 전환 사용
using UnityEngine.TestTools; // 실제 플레이 모드 검사 사용
public sealed class ExplorationHudCleanupTests // 탐사 화면 우측 하단 정리 검사
{ // 탐사 화면 정리 검사 시작
    [UnityTest] // 실제 탐사 화면 검사 지정
    public IEnumerator ExplorationHidesDebugMapAndSaveEntryButKeepsMap() // 시험 정보와 저장 버튼 제거 및 탐사 유지 확인
    { // 실제 탐사 화면 검사 시작
        yield return new EnterPlayMode(); // 실제 게임 플레이 시작
        SceneManager.LoadScene("00_Boot"); // 실제 게임 시작 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 실제 시작 메뉴 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 실제 거점 대기
        { // 거점 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 실제 탐사 화면 대기
        { // 탐사 화면 대기 시작
            yield return null; // 다음 화면 갱신 대기
        } // 탐사 화면 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<ExplorationPlayerController>()); // 실제 탐사 플레이어 확인
        yield return new WaitForSecondsRealtime(0.1f); // 탐사 HUD 생성 완료 대기
        Assert.IsNotNull(Object.FindFirstObjectByType<ExplorationMapRuntime>()); // 실제 탐사 맵 런타임 유지 확인
        Assert.IsNotNull(Object.FindFirstObjectByType<ExplorationTilemapView>()); // 실제 탐사 타일맵 유지 확인
        Assert.IsNull(Object.FindFirstObjectByType<ExplorationMapDebugView>()); // 우측 하단 시험 정보와 미니맵 제거 확인
        Assert.IsNull(Object.FindFirstObjectByType<SaveMenuEntry>()); // 탐사 화면 우측 하단 저장 버튼 제거 확인
        Assert.IsNotNull(Object.FindFirstObjectByType<ExplorationRunLoopDebugView>()); // 탐사 결과와 거점 복귀 처리 유지 확인
        yield return new ExitPlayMode(); // 실제 게임 플레이 종료
    } // 실제 탐사 화면 검사 종료
} // 탐사 화면 정리 검사 종료
