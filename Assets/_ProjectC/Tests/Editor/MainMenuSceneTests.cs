using System.Collections; // 코루틴 테스트 기능 사용
using NUnit.Framework; // 테스트 기능 사용
using TMPro; // 메뉴 글자 조회 기능 사용
using UnityEditor.SceneManagement; // 편집기 씬 조회 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.SceneManagement; // 씬 자료형 사용
using UnityEngine.TestTools; // 플레이 모드 전환 기능 사용
using UnityEngine.UI; // 메뉴 이미지와 버튼 조회 기능 사용
public sealed class MainMenuSceneTests // 메인 메뉴 씬 검증
{ // 테스트 클래스 시작
    [UnityTest] // 실행 흐름 테스트 지정
    public IEnumerator MenuButtons_OpenSettingsReturnAndStartLobby() // 실제 메뉴 이동 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메뉴 진입 대기
        { // 대기 시작
            yield return null; // 다음 프레임 대기
        } // 대기 종료
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메뉴 진입 확인
        yield return null; // 메뉴 이벤트 연결 대기
        MainMenuView view = Object.FindFirstObjectByType<MainMenuView>(); // 메뉴 화면 조회
        Assert.IsNotNull(view); // 새 메뉴 화면 존재 확인
        Assert.IsFalse(view.transform.Find("ContinueButton").GetComponent<Button>().interactable); // 이어하기 비활성 확인
        view.transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke(); // 설정 버튼 실행
        yield return null; // 설정 씬 전환 대기
        Assert.AreEqual("90_Settings", SceneManager.GetActiveScene().name); // 설정 진입 확인
        Object.FindFirstObjectByType<SettingsController>().Back(); // 메뉴 복귀 요청
        yield return null; // 메뉴 씬 복귀 대기
        yield return null; // 메뉴 이벤트 재연결 대기
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메뉴 복귀 확인
        view = Object.FindFirstObjectByType<MainMenuView>(); // 복귀 메뉴 화면 조회
        view.NewGameButton.onClick.Invoke(); // 새로하기 버튼 실행
        yield return null; // 거점 씬 전환 대기
        yield return null; // 거점 초기화 대기
        Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 거점 진입 확인
        Assert.IsNotNull(Object.FindFirstObjectByType<PartyRosterView>()); // 편성 화면 연결 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MenuBindings_InvokeEachActionOnceAfterRebinding() // 메뉴 중복 실행 방지 확인
    { // 테스트 시작
        GameObject canvasObject = new GameObject("TestCanvas", typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 객체 정리 보장
        { // 검사 구역 시작
            MainMenuView view = MainMenuView.Build(canvasObject.GetComponent<Canvas>()); // 메뉴 화면 생성
            int[] counts = new int[4]; // 실행 횟수 목록
            view.Bind(() => counts[0]++, () => counts[1]++, () => counts[2]++, () => counts[3]++, true); // 첫 메뉴 동작 연결
            view.Bind(() => counts[0]++, () => counts[1]++, () => counts[2]++, () => counts[3]++, true); // 메뉴 동작 재연결
            foreach (Button button in view.GetComponentsInChildren<Button>()) // 메뉴 버튼 순회
            { // 버튼 실행 시작
                button.onClick.Invoke(); // 연결 동작 실행
            } // 버튼 실행 종료
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, counts); // 모든 동작 단일 실행 확인
        } // 검사 구역 종료
        finally // 객체 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(canvasObject); // 테스트 캔버스 제거
        } // 정리 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MainMenuScene_HasSeparateArtworkAndFourButtons() // 독립 이미지와 네 버튼 확인
    { // 테스트 시작
        Scene scene = SceneManager.GetSceneByPath("Assets/_ProjectC/Scenes/10_MainMenu.unity"); // 기존 메뉴 씬 조회
        bool openedForTest = !scene.IsValid() || !scene.isLoaded; // 테스트용 씬 열기 필요 확인
        if (openedForTest) // 미개방 씬 확인
        { // 씬 열기 시작
            scene = EditorSceneManager.OpenScene("Assets/_ProjectC/Scenes/10_MainMenu.unity", OpenSceneMode.Additive); // 메인 메뉴 씬 열기
        } // 씬 열기 종료
        try // 씬 검사 시작
        { // 검사 구역 시작
            Transform menu = Find(scene, "MainMenuUI"); // 새 메인 메뉴 화면 조회
            Assert.IsNotNull(menu); // 메뉴 화면 존재 확인
            Image background = menu.Find("MenuBackground").GetComponent<Image>(); // 배경 이미지 조회
            Image logo = menu.Find("GameLogo").GetComponent<Image>(); // 로고 이미지 조회
            Assert.IsNotNull(background.sprite); // 배경 이미지 연결 확인
            Assert.IsNotNull(logo.sprite); // 로고 이미지 연결 확인
            Assert.AreNotSame(background.sprite, logo.sprite); // 별도 이미지 사용 확인
            string[] names = { "NewGameButton", "ContinueButton", "SettingsButton", "QuitButton" }; // 버튼 이름 목록
            string[] labels = { "새로하기", "이어하기", "설정", "나가기" }; // 버튼 문구 목록
            Assert.AreEqual(4, menu.GetComponentsInChildren<Button>(true).Length); // 네 버튼 구성 확인
            for (int index = 0; index < names.Length; index++) // 버튼 목록 순회
            { // 개별 버튼 확인 시작
                Button button = menu.Find(names[index]).GetComponent<Button>(); // 개별 버튼 조회
                Assert.IsNotNull(button.image.sprite, names[index]); // 버튼 장식 이미지 연결 확인
                Assert.AreEqual(labels[index], button.GetComponentInChildren<TMP_Text>().text); // 실제 텍스트 문구 확인
                Assert.AreEqual(index != 1, button.interactable); // 저장 미구현 이어하기 비활성 확인
            } // 개별 버튼 확인 종료
        } // 검사 구역 종료
        finally // 씬 정리 처리
        { // 정리 시작
            if (openedForTest) // 테스트용으로 연 씬 확인
            { // 검사 씬 정리 시작
                EditorSceneManager.CloseScene(scene, true); // 검사 씬 닫기
            } // 검사 씬 정리 종료
        } // 정리 종료
    } // 테스트 종료
    private static Transform Find(Scene scene, string name) // 씬 내부 오브젝트 조회
    { // 조회 시작
        foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
        { // 루트 확인 시작
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) // 전체 자식 순회
            { // 자식 확인 시작
                if (child.name == name) // 이름 일치 확인
                { // 일치 처리 시작
                    return child; // 일치 오브젝트 반환
                } // 일치 처리 종료
            } // 자식 확인 종료
        } // 루트 확인 종료
        return null; // 조회 실패 반환
    } // 조회 종료
} // 테스트 클래스 종료
