using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 설정 화면 검증 사용
using TMPro; // 설정 문구 조회 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 설정 버튼과 목록 사용

public sealed class SettingsViewTests // 설정 탭과 옵션 화면 검증
{ // 설정 화면 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Settings_ShowsFiveTabsAndReturnsToPreviousScene() // 설정 분류와 복귀 확인
    { // 설정 화면 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && Object.FindFirstObjectByType<MainMenuView>() == null;) // 메뉴 화면 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        yield return null; // 메뉴 버튼 연결 대기
        Object.FindFirstObjectByType<MainMenuView>().transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke(); // 메인 메뉴 설정 열기
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && Object.FindFirstObjectByType<SettingsView>() == null;) // 설정 화면 준비 대기
        { // 설정 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 설정 화면 대기 종료
        SettingsView view = Object.FindFirstObjectByType<SettingsView>(); // 설정 화면 조회
        Assert.IsNotNull(view); // 설정 화면 존재 확인
        Transform tabs = view.transform.Find("Tabs"); // 설정 탭 영역 조회
        Assert.AreEqual(5, tabs.childCount); // 다섯 설정 탭 확인
        Transform list = view.transform.Find("Options/Viewport/Rows"); // 설정 항목 목록 조회
        Assert.AreEqual(13, list.childCount); // 그래픽 설정 항목 수 확인
        Assert.AreEqual("화면 모드", list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 기획서 그래픽 항목 이름 확인
        Assert.AreEqual("무테창", list.GetChild(0).Find("Value").GetComponent<TMP_Text>().text); // 기획서 그래픽 기본값 확인
        Assert.IsFalse(list.GetChild(0).Find("Previous").GetComponent<Button>().interactable); // 연결 전 값 변경 차단 확인
        tabs.Find("Tab_1").GetComponent<Button>().onClick.Invoke(); // 사운드 탭 선택
        Assert.AreEqual(9, list.childCount); // 기획서 사운드 항목 수 확인
        Assert.AreEqual("마스터 볼륨", list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 사운드 첫 항목 확인
        Assert.AreEqual("80", list.GetChild(0).Find("Value").GetComponent<TMP_Text>().text); // 사운드 기본값 확인
        tabs.Find("Tab_2").GetComponent<Button>().onClick.Invoke(); // 게임 플레이 탭 선택
        Assert.AreEqual(16, list.childCount); // 기획서 게임 플레이 항목 수 확인
        Assert.AreEqual("난이도", list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 게임 플레이 첫 항목 확인
        Canvas.ForceUpdateCanvases(); // 설정 목록 배치 갱신
        ScrollRect scroll = view.transform.Find("Options").GetComponent<ScrollRect>(); // 설정 목록 스크롤 조회
        float initialPosition = scroll.content.anchoredPosition.y; // 스크롤 시작 위치 보관
        scroll.verticalNormalizedPosition = 0f; // 설정 목록 맨 아래로 이동
        Assert.Greater(scroll.content.anchoredPosition.y, initialPosition); // 긴 설정 목록 스크롤 확인
        tabs.Find("Tab_3").GetComponent<Button>().onClick.Invoke(); // 조작 탭 선택
        Assert.AreEqual(9, list.childCount); // 기획서 조작 항목 수 확인
        Assert.AreEqual("키 설정(리바인드)", list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 조작 첫 항목 확인
        tabs.Find("Tab_4").GetComponent<Button>().onClick.Invoke(); // 언어 탭 선택
        Assert.AreEqual(6, list.childCount); // 기획서 언어 항목 수 확인
        Assert.AreEqual("언어 선택", list.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 언어 첫 항목 확인
        Assert.IsTrue(view.transform.Find("Options").GetComponent<ScrollRect>().vertical); // 긴 설정 목록 스크롤 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 이전 씬 복귀 요청
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && SceneManager.GetActiveScene().name != "10_MainMenu";) // 메뉴 복귀 대기
        { // 메뉴 복귀 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 복귀 대기 종료
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 이전 메뉴 복귀 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 설정 화면 검증 종료
} // 설정 화면 테스트 종료
