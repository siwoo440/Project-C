using System.Collections; // 플레이 모드 코루틴 사용
using NUnit.Framework; // 화면 검증 사용
using TMPro; // 자원 문구 조회 사용
using UnityEngine; // 화면 객체 조회 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 로비 버튼 조회 사용

public sealed class LobbyLayoutTests // 새 로비 배치 검증
{ // 로비 배치 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Lobby_ShowsRequestedLayoutWithoutOldPartyWindow() // 새 로비 화면과 기존 창 제거 확인
    { // 로비 화면 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<MainMenuController>() == null; frame++) // 메인 메뉴 준비 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 새 거점 진입
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyController>() == null; frame++) // 새 로비 화면 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        yield return null; // 로비 화면 초기화 대기
        System.Type viewType = typeof(MainMenuController).Assembly.GetType("LobbyLayoutView"); // 새 로비 화면 형식 조회
        Assert.IsNotNull(viewType); // 새 로비 화면 형식 존재 확인
        Component view = (Component)Object.FindFirstObjectByType(viewType); // 새 로비 화면 조회
        Assert.IsNotNull(view); // 새 로비 화면 존재 확인
        Assert.IsNull(GameObject.Find("PartyRosterView")); // 기존 파티 편성 창 제거 확인
        Assert.IsNotNull(view.transform.Find("Gold")); // 상단 골드 표시 확인
        Assert.IsNotNull(view.transform.Find("GoldIcon").GetComponent<Image>().sprite); // 골드 아이콘 이미지 확인
        Assert.IsNotNull(view.transform.Find("Resources")); // 하단 자원 표시 확인
        Assert.IsNotNull(view.transform.Find("CharacterButton").GetComponent<Button>()); // 캐릭터 버튼 확인
        Assert.IsNotNull(view.transform.Find("FormationButton").GetComponent<Button>()); // 편성 버튼 확인
        Assert.IsNotNull(view.transform.Find("ExpeditionButton").GetComponent<Button>()); // 탐사 버튼 확인
        Assert.IsNotNull(view.transform.Find("CodexButton").GetComponent<Button>()); // 도감 버튼 확인
        Assert.IsNotNull(view.transform.Find("FacilityButton").GetComponent<Button>()); // 설비 강화 버튼 확인
        Assert.IsNotNull(view.transform.Find("HelpButton").GetComponent<Button>()); // 도움말 버튼 확인
        Assert.IsNotNull(view.transform.Find("SettingsButton").GetComponent<Button>()); // 설정 버튼 확인
        RectTransform settingsRect = view.transform.Find("SettingsButton").GetComponent<RectTransform>(); // 설정 버튼 영역 조회
        RectTransform helpRect = view.transform.Find("HelpButton").GetComponent<RectTransform>(); // 도움말 버튼 영역 조회
        Assert.AreEqual(settingsRect.sizeDelta.x, settingsRect.sizeDelta.y); // 정사각형 설정 버튼 확인
        Assert.AreEqual(helpRect.sizeDelta.x, helpRect.sizeDelta.y); // 정사각형 도움말 버튼 확인
        Assert.AreSame(Resources.Load<Texture2D>("UI/Lobby/Settings"), settingsRect.GetComponent<Image>().sprite.texture); // 제작된 톱니바퀴 이미지 사용 확인
        Assert.AreSame(Resources.Load<Texture2D>("UI/Lobby/Help"), helpRect.GetComponent<Image>().sprite.texture); // 제작된 물음표 이미지 사용 확인
        Assert.AreEqual(string.Empty, settingsRect.Find("Label").GetComponent<TMP_Text>().text); // 설정 버튼 글자 제거 확인
        Assert.AreEqual(string.Empty, helpRect.Find("Label").GetComponent<TMP_Text>().text); // 도움말 버튼 별도 글자 제거 확인
        Assert.IsNotNull(view.transform.Find("CharacterButton").GetComponent<Image>().sprite); // 제작한 로비 버튼 이미지 확인
        Assert.IsNotNull(GameObject.Find("SaveMenuEntry").GetComponent<Image>().sprite); // 저장 메뉴 버튼 이미지 확인
        RectTransform characterButton = view.transform.Find("CharacterButton").GetComponent<RectTransform>(); // 캐릭터 버튼 위치 조회
        RectTransform formationButton = view.transform.Find("FormationButton").GetComponent<RectTransform>(); // 편성 버튼 위치 조회
        RectTransform expeditionButton = view.transform.Find("ExpeditionButton").GetComponent<RectTransform>(); // 탐사 버튼 위치 조회
        Assert.Greater(characterButton.anchorMin.x, 0.7f); // 캐릭터 버튼 오른쪽 배치 확인
        Assert.Greater(characterButton.anchorMin.y, formationButton.anchorMin.y); // 캐릭터 버튼의 편성 위 배치 확인
        Assert.Greater(formationButton.anchorMin.y, expeditionButton.anchorMin.y); // 편성 버튼의 탐사 위 배치 확인
        Assert.Less(view.transform.Find("CodexButton").GetComponent<RectTransform>().anchorMin.x, 0.2f); // 도감 버튼 왼쪽 배치 확인
        PlayerResourceManager resources = PlayerResourceManager.Instance; // 현재 자원 관리자 조회
        resources.AddResources(37, 2, 3, 4); // 자원 표시 갱신용 값 변경
        Assert.AreEqual(resources.Gold.ToString(), view.transform.Find("Gold").GetComponent<TMP_Text>().text); // 골드 숫자만 표시 확인
        Transform group = view.transform.Find("Resources"); // 물자 영역 조회
        Assert.LessOrEqual(group.GetComponent<RectTransform>().sizeDelta.x, 330f); // 물자 전체 영역 폭 축소 확인
        string[] names = { "Screw", "IronPlate", "Wire" }; // 개별 물자 영역 이름
        int[] counts = { resources.Screw, resources.IronPlate, resources.Wire }; // 개별 물자 실제 수량
        for (int index = 0; index < names.Length; index++) // 전체 물자 영역 순회
        { // 개별 물자 표시 확인 시작
            Transform item = group.Find(names[index]); // 독립 물자 영역 조회
            Assert.IsNotNull(item); // 개별 물자 영역 존재 확인
            Assert.IsNotNull(item.GetComponent<Image>().sprite); // 개별 물자 배경 이미지 확인
            Image icon = item.Find("Icon").GetComponent<Image>(); // 물자 아이콘 조회
            Assert.IsNotNull(icon.sprite); // 물자 아이콘 이미지 확인
            Assert.AreEqual(icon.rectTransform.sizeDelta.x, icon.rectTransform.sizeDelta.y); // 정사각형 아이콘 크기 확인
            Assert.AreEqual(counts[index].ToString(), item.Find("Value").GetComponent<TMP_Text>().text); // 물자 이름 없이 수량만 표시 확인
        } // 개별 물자 표시 확인 종료
        view.transform.Find("FacilityButton").GetComponent<Button>().onClick.Invoke(); // 설비 강화 화면 열기
        FacilityUpgradeDebugView facility = Object.FindFirstObjectByType<FacilityUpgradeDebugView>(); // 설비 강화 화면 조회
        System.Reflection.FieldInfo visible = typeof(FacilityUpgradeDebugView).GetField("panelVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 설비 화면 표시 상태 조회
        Assert.IsTrue((bool)visible.GetValue(facility)); // 설비 강화 화면 표시 확인
        view.transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke(); // 로비 설정 화면 열기
        yield return null; // 설정 씬 이동 대기
        Assert.AreEqual("90_Settings", SceneManager.GetActiveScene().name); // 로비 설정 씬 진입 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 로비 화면 검증 종료
} // 로비 배치 테스트 종료
