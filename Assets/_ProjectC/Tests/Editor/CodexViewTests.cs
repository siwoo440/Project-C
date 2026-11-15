using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 도감 화면 검증 사용
using TMPro; // 도감 항목 이름 조회 사용
using UnityEngine; // 화면 객체와 실제 데이터 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 도감 버튼과 목록 사용

public sealed class CodexViewTests // 도감 분류와 목록 검증
{ // 도감 테스트 시작
    [Test] // 편집기 데이터 검사 지정
    public void CodexData_UsesGameAssetsAndLeavesMissingCategoriesEmpty() // 실제 데이터와 빈 분류 확인
    { // 도감 원본 검사 시작
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 아이템 원본 조회
        Assert.IsNotNull(catalog); // 아이템 원본 존재 확인
        int potions = 0; // 실제 포션 수 초기화
        foreach (ConsumableItemData item in catalog.Consumables) // 실제 소모품 원본 순회
        { // 실제 포션 집계 시작
            if (item is PotionData) // 포션 자료형 확인
            { // 포션 집계 시작
                potions++; // 실제 포션 수 증가
            } // 포션 집계 종료
        } // 실제 포션 집계 종료
        Assert.AreEqual(potions, CodexData.Load(1).Count); // 실제 포션 목록 일치 확인
        Assert.AreEqual(catalog.Relics.Count, CodexData.Load(3).Count); // 실제 유물 목록 일치 확인
        Assert.AreEqual(ExplorationEventCatalog.LoadEvents().Count, CodexData.Load(0).Count); // 실제 이벤트 목록 일치 확인
        Assert.Greater(CodexData.Load(2).Count, 0); // 실제 조우의 적 포함 확인
        Assert.Greater(CodexData.Load(4).Count, 0); // 실제 전투 상태 효과 포함 확인
        Assert.AreEqual(0, CodexData.Load(5).Count); // 원본 없는 기록 분류 확인
        Assert.AreEqual(0, CodexData.Load(6).Count); // 원본 없는 오브젝트 분류 확인
    } // 도감 원본 검사 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator LobbyCodex_OpensCategoriesAndActualEntries() // 도감 분류와 실제 데이터 확인
    { // 도감 화면 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 메뉴 준비 대기
        { // 메뉴 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 준비 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 로비 진입
        for (float sceneDeadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < sceneDeadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 준비 대기
        { // 로비 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 준비 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 로비 화면 조회
        Assert.IsNotNull(lobby); // 로비 화면 존재 확인
        Button open = lobby.transform.Find("CodexButton").GetComponent<Button>(); // 도감 버튼 조회
        Assert.IsTrue(open.interactable); // 도감 버튼 활성 확인
        open.onClick.Invoke(); // 도감 분류 화면 열기
        CodexView view = Object.FindFirstObjectByType<CodexView>(); // 도감 화면 조회
        Assert.IsNotNull(view); // 도감 화면 존재 확인
        for (int index = 0; index < 7; index++) // 일곱 분류 순회
        { // 분류 버튼 확인 시작
            Assert.IsNotNull(view.transform.Find("Categories/Category_" + index)); // 개별 분류 버튼 존재 확인
        } // 분류 버튼 확인 종료
        view.transform.Find("Categories/Category_3").GetComponent<Button>().onClick.Invoke(); // 유물 목록 열기
        Transform list = view.transform.Find("ListPanel"); // 유물 목록 화면 조회
        Assert.IsTrue(list.gameObject.activeInHierarchy); // 유물 목록 표시 확인
        GridLayoutGroup grid = list.Find("Viewport/Grid").GetComponent<GridLayoutGroup>(); // 도감 항목 격자 조회
        Assert.AreEqual(6, grid.constraintCount); // 도감 여섯 열 배치 확인
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장 원본 목록 조회
        Assert.AreEqual(catalog.Relics.Count, grid.transform.childCount); // 실제 유물 항목 수 확인
        Assert.IsNotNull(list.Find("SortButton").GetComponent<Button>()); // 이름 정렬 버튼 확인
        if (catalog.Relics.Count > 0) // 표시할 유물 존재 확인
        { // 유물 상세 확인 시작
            grid.transform.GetChild(0).GetComponent<Button>().onClick.Invoke(); // 첫 유물 상세 열기
            Assert.IsTrue(view.transform.Find("Detail").gameObject.activeInHierarchy); // 실제 항목 상세 표시 확인
            view.transform.Find("Detail/CloseButton").GetComponent<Button>().onClick.Invoke(); // 상세 닫기
        } // 유물 상세 확인 종료
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 분류 화면 복귀
        Assert.IsTrue(view.transform.Find("Categories").gameObject.activeInHierarchy); // 도감 분류 화면 복귀 확인
        view.transform.Find("Categories/Category_4").GetComponent<Button>().onClick.Invoke(); // 상태 효과 목록 열기
        string firstName = grid.transform.GetChild(0).Find("Name").GetComponent<TMP_Text>().text; // 이름순 첫 항목 보관
        string lastName = grid.transform.GetChild(grid.transform.childCount - 1).Find("Name").GetComponent<TMP_Text>().text; // 이름순 마지막 항목 보관
        list.Find("SortButton").GetComponent<Button>().onClick.Invoke(); // 이름 역순 전환
        Assert.AreEqual(lastName, grid.transform.GetChild(0).Find("Name").GetComponent<TMP_Text>().text); // 역순 첫 항목 확인
        Assert.AreEqual(firstName, grid.transform.GetChild(grid.transform.childCount - 1).Find("Name").GetComponent<TMP_Text>().text); // 역순 마지막 항목 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 도감 분류 복귀
        view.transform.Find("Categories/Category_5").GetComponent<Button>().onClick.Invoke(); // 기록 분류 열기
        Assert.IsTrue(view.transform.Find("ListPanel/Empty").gameObject.activeInHierarchy); // 데이터 없는 분류 안내 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 도감 분류 복귀
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 로비 복귀
        yield return null; // 도감 화면 제거 대기
        Assert.IsNull(Object.FindFirstObjectByType<CodexView>()); // 도감 닫힘 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 도감 화면 검증 종료
} // 도감 테스트 종료
