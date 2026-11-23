using System.Collections; // 코루틴 테스트 사용
using NUnit.Framework; // 테스트 검증 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.SceneManagement; // 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 진행 막대 조회 사용

public sealed class LoadingScreenTests // 비동기 로딩 화면 검증
{ // 로딩 화면 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator LoadingScreen_ShowsUntilSceneTransitionCompletes() // 로딩 화면과 중복 이동 확인
    { // 전환 흐름 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 메뉴 준비 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        SceneFlowManager flow = SceneFlowManager.Instance; // 씬 전환 관리자 조회
        Assert.IsNotNull(flow); // 씬 관리자 존재 확인
        flow.LoadScene("20_Lobby"); // 비동기 로비 이동 시작
        Assert.IsTrue(flow.IsLoadingScene); // 로딩 중 상태 확인
        LoadingScreenView loading = Object.FindFirstObjectByType<LoadingScreenView>(); // 표시된 로딩 화면 조회
        Assert.IsNotNull(loading); // 로딩 화면 즉시 표시 확인
        Assert.IsNotNull(loading.transform.Find("Background").GetComponent<Image>().sprite); // 제작된 배경 적용 확인
        Assert.IsNotNull(loading.transform.Find("ProgressTrack/ProgressFill")); // 진행 막대 존재 확인
        flow.LoadScene("90_Settings"); // 중복 씬 이동 요청
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && flow.IsLoadingScene;) // 씬 전환 완료 대기
        { // 씬 전환 대기 시작
            yield return null; // 다음 프레임 대기
        } // 씬 전환 대기 종료
        Assert.IsFalse(flow.IsLoadingScene); // 로딩 상태 종료 확인
        Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 최초 요청 씬 도착 확인
        yield return null; // 화면 제거 대기
        Assert.IsNull(Object.FindFirstObjectByType<LoadingScreenView>()); // 로딩 화면 제거 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전환 흐름 검증 종료
} // 로딩 화면 테스트 종료
