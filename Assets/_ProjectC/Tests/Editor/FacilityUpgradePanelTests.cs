using System.Collections; // 플레이 모드 검사 사용
using NUnit.Framework; // 자동 검증 기능 사용
using TMPro; // 설비 정보 문구 검증 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 설비 버튼과 이미지 검사 사용
public sealed class FacilityUpgradePanelTests // 설비 강화 화면 검사
{ // 설비 화면 검사 시작
    [UnityTest] // 실제 로비 화면 검사 지정
    public IEnumerator FacilityPanel_ShowsArtAndUpgradesSelectedFacility() // 설비 화면과 강화 흐름 확인
    { // 설비 화면 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "10_MainMenu";) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 로비 이동 요청
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 화면 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 실제 로비 화면 조회
        Assert.IsNotNull(lobby); // 로비 화면 존재 확인
        PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
        FacilityUpgradeManager facilities = FacilityUpgradeManager.EnsureInstance(); // 실제 설비 관리자 조회
        resources.ResetResources(); // 테스트 자원 초기화
        facilities.ResetFacilities(); // 테스트 설비 초기화
        resources.AddResources(100, 20, 20, 20); // 테스트 강화 자원 지급
        lobby.transform.Find("FacilityButton").GetComponent<Button>().onClick.Invoke(); // 실제 설비 강화 버튼 클릭
        yield return null; // 설비 화면 생성 대기
        GameObject overlay = GameObject.Find("FacilityUpgradeOverlay"); // 설비 화면 조회
        Assert.IsNotNull(overlay); // 설비 화면 생성 확인
        FacilityUpgradeManager.EnsureInstance().GetComponent<FacilityUpgradeDebugView>().OpenPanel(); // 열린 설비 화면 재요청
        Assert.AreEqual(1, Object.FindObjectsByType<FacilityUpgradePanel>(FindObjectsSortMode.None).Length); // 중복 설비 화면 방지 확인
        RectTransform panel = overlay.transform.Find("FacilityPanel").GetComponent<RectTransform>(); // 실제 설비 패널 조회
        Assert.GreaterOrEqual(panel.anchorMax.x - panel.anchorMin.x, 0.85f); // 화면에 비해 넓은 패널 확인
        Assert.GreaterOrEqual(panel.anchorMax.y - panel.anchorMin.y, 0.8f); // 화면에 비해 높은 패널 확인
        Assert.IsNotNull(panel.GetComponent<Image>().sprite); // 제작 패널 이미지 적용 확인
        Transform resourceRow = panel.Find("Resources"); // 보유 자원 영역 조회
        string[] resourceNames = { "Gold", "Screw", "Iron", "Wire" }; // 기존 자원 아이콘 이름 목록
        int[] amounts = { resources.Gold, resources.Screw, resources.IronPlate, resources.Wire }; // 실제 보유 자원 수량 목록
        for (int index = 0; index < resourceNames.Length; index++) // 네 자원 표시 순회
        { // 개별 자원 표시 검사 시작
            Transform item = resourceRow.Find("Resource_" + resourceNames[index]); // 개별 자원 표시 조회
            Assert.IsNotNull(item.Find("Icon").GetComponent<Image>().sprite); // 기존 자원 아이콘 이미지 확인
            Assert.AreEqual(amounts[index].ToString(), item.Find("Value").GetComponent<TMP_Text>().text); // 실제 자원 수량 확인
        } // 개별 자원 표시 검사 종료
        Transform list = panel.Find("FacilityList/Viewport/Content"); // 설비 목록 조회
        Assert.IsNotNull(list); // 설비 목록 존재 확인
        Assert.AreEqual(System.Enum.GetValues(typeof(FacilityType)).Length, list.childCount); // 전체 설비 버튼 확인
        list.Find("Facility_DefenseBarrier").GetComponent<Button>().onClick.Invoke(); // 방어 차폐 장치 선택
        Assert.AreEqual("방어 차폐 장치", panel.Find("Detail/FacilityName").GetComponent<TMP_Text>().text); // 선택 설비 이름 확인
        StringAssert.Contains("아군 받는 피해 3% 감소", panel.Find("Detail/NextEffect").GetComponent<TMP_Text>().text); // 다음 설비 효과 확인
        Button upgrade = panel.Find("Detail/UpgradeButton").GetComponent<Button>(); // 강화 버튼 조회
        Assert.IsTrue(upgrade.interactable); // 자원 보유 시 강화 가능 확인
        int previousScrew = resources.Screw; // 강화 전 나사 보관
        int previousIron = resources.IronPlate; // 강화 전 철판 보관
        upgrade.onClick.Invoke(); // 실제 강화 실행
        Assert.AreEqual(1, facilities.GetLevel(FacilityType.DefenseBarrier)); // 선택 설비 단계 증가 확인
        Assert.AreEqual(previousScrew - 5, resources.Screw); // 강화 나사 비용 차감 확인
        Assert.AreEqual(previousIron - 10, resources.IronPlate); // 강화 철판 비용 차감 확인
        Assert.AreEqual(resources.IronPlate.ToString(), resourceRow.Find("Resource_Iron/Value").GetComponent<TMP_Text>().text); // 비용 차감 후 수량 화면 갱신 확인
        StringAssert.Contains("1 / 5", panel.Find("Detail/Level").GetComponent<TMP_Text>().text); // 강화 후 단계 화면 갱신 확인
        Assert.IsFalse(upgrade.interactable); // 다음 단계 자원 부족 시 강화 차단 확인
        resources.AddResources(0, 0, 5, 0); // 부족한 철판 지급
        Assert.IsTrue(upgrade.interactable); // 자원 지급 시 강화 버튼 갱신 확인
        panel.Find("CloseButton").GetComponent<Button>().onClick.Invoke(); // 실제 화면 닫기 클릭
        yield return null; // 닫힌 화면 정리 대기
        Assert.IsNull(GameObject.Find("FacilityUpgradeOverlay")); // 설비 화면 닫힘 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 설비 화면 검사 종료
} // 설비 화면 검사 종료
