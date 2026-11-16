using System.Collections; // 실제 씬 대기 사용
using NUnit.Framework; // 실제 화면 연결 검사 사용
using TMPro; // 안내 문구 확인 사용
using UnityEngine; // 실제 게임 객체 조회 사용
using UnityEngine.SceneManagement; // 실제 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 화면 버튼 사용
public sealed class WorldTutorialSceneTests // 실제 거점과 탐사 안내 검사
{ // 실제 안내 검사 시작
    [UnityTest] // 거점과 탐사 화면 연결 검사 지정
    public IEnumerator LobbyHelpAndExplorationMovement_ShowRealGuides() // 거점 다시 보기와 탐사 이동 확인
    { // 실제 화면 연결 검사 시작
        yield return new EnterPlayMode(); // 실제 게임 플레이 시작
        SceneManager.LoadScene("00_Boot"); // 게임 시작 화면 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 준비 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 메뉴 준비 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 화면 준비 대기
        { // 거점 화면 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 거점 화면 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 실제 거점 화면 조회
        Assert.IsNotNull(lobby); // 실제 거점 화면 존재 확인
        yield return null; // 거점 안내 연결 프레임 대기
        Button help = lobby.transform.Find("HelpButton").GetComponent<Button>(); // 실제 거점 도움말 버튼 조회
        Assert.IsTrue(help.interactable); // 거점 도움말 사용 가능 확인
        help.onClick.Invoke(); // 실제 거점 안내 다시 보기 실행
        WorldTutorialView lobbyTutorial = Object.FindFirstObjectByType<WorldTutorialView>(); // 실제 거점 안내 화면 조회
        Assert.IsNotNull(lobbyTutorial); // 거점 안내 화면 존재 확인
        Transform lobbyGuide = lobbyTutorial.transform.Find("Guide"); // 거점 안내 영역 조회
        Assert.IsTrue(lobbyGuide.gameObject.activeSelf); // 거점 다시 보기 표시 확인
        Button lobbyNext = lobbyGuide.Find("Panel/NextButton").GetComponent<Button>(); // 거점 다음 안내 버튼 조회
        lobbyNext.onClick.Invoke(); // 파티 편성 안내 이동
        Assert.IsTrue(lobbyGuide.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("구역")); // 구역 선택 후 편성 경로 안내 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 파티로 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 실제 탐사 플레이어 대기
        { // 탐사 플레이어 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 탐사 플레이어 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 실제 탐사 플레이어 확인
        yield return null; // 탐사 안내 생성 프레임 대기
        WorldTutorialView exploration = Object.FindFirstObjectByType<WorldTutorialView>(); // 실제 탐사 안내 조회
        Assert.IsNotNull(exploration); // 실제 탐사 안내 존재 확인
        Transform guide = exploration.transform.Find("Guide"); // 탐사 안내 영역 조회
        if (!guide.gameObject.activeSelf) // 자동 안내 미표시 확인
        { // 탐사 안내 다시 보기 시작
            exploration.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 실제 탐사 도움말 다시 열기
        } // 탐사 안내 다시 보기 종료
        Button next = guide.Find("Panel/NextButton").GetComponent<Button>(); // 탐사 다음 안내 버튼 조회
        Assert.IsFalse(next.interactable); // 이동 전 탐사 안내 대기 확인
        player.transform.position += Vector3.right * 0.2f; // 실제 탐사 플레이어 위치 변경
        yield return null; // 실제 이동 감시 프레임 대기
        Assert.IsTrue(next.interactable); // 실제 위치 변경 후 다음 안내 허용 확인
        next.onClick.Invoke(); // 실제 접촉 상호작용 안내 이동
        Assert.IsTrue(guide.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("접촉")); // 현재 구현된 접촉 방식 안내 확인
        yield return new ExitPlayMode(); // 실제 게임 플레이 종료
    } // 실제 화면 연결 검사 종료
} // 실제 안내 검사 종료
