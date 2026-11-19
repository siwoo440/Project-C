using System.Linq; // 논리적 항목 순서와 활성 셀 비교
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
        var grid = (RectTransform)list.Find("Viewport/Grid"); // 실제 위치를 가진 셀의 부모
        var scroll = list.GetComponent<ScrollRect>(); // 분류 목록의 실제 스크롤
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 원본의 전체 목록
        Assert.AreEqual(((RectTransform)grid.Find("Entry_0")).anchoredPosition.y, ((RectTransform)grid.Find("Entry_5")).anchoredPosition.y); // 첫 행에 여섯 항목 배치
        Assert.Less(((RectTransform)grid.Find("Entry_6")).anchoredPosition.y, ((RectTransform)grid.Find("Entry_5")).anchoredPosition.y); // 일곱 번째부터 다음 줄 표시
        Assert.AreEqual(40f + Mathf.Ceil(catalog.Relics.Count / 6f) * 253f - 18f, grid.rect.height); // 셀 수와 별개로 전체 유물 행의 스크롤 길이 유지
        int initialCells = grid.childCount; // 상단에서 생성된 실제 셀 수
        int visibleCapacity = 6 * (Mathf.CeilToInt(scroll.viewport.rect.height / 253f) + 2); // 보이는 행과 여유 행의 최대 셀 수
        Assert.LessOrEqual(initialCells, Mathf.Min(visibleCapacity, catalog.Relics.Count)); // 전체 자료가 늘어도 화면을 넘는 셀 생성 방지
        var initialIds = grid.Cast<Transform>().Select(cell => cell.GetInstanceID()).ToArray(); // 같은 객체 재사용을 확인할 식별자
        scroll.verticalNormalizedPosition = 0f; // 유물 목록 맨 아래까지 이동
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 실제 스크롤 위치 변경 전달
        var tail = grid.Find("Entry_" + (catalog.Relics.Count - 1)); // 마지막 논리적 유물의 재사용 셀
        Assert.IsTrue(tail != null && tail.gameObject.activeInHierarchy); // 마지막 행까지 스크롤 가능 확인
        var lastRelic = CodexData.Load(3).OrderBy(entry => entry.Name, System.StringComparer.Ordinal).Last(); // 데이터의 실제 마지막 항목
        Assert.AreEqual(lastRelic.Name, tail.Find("Name").GetComponent<TMP_Text>().text); // 재연결한 원본 이름 일치
        Assert.AreSame(lastRelic.Image, tail.Find("Image").GetComponent<Image>().sprite); // 이전 셀 그림과 현재 원본 구별
        tail.GetComponent<Button>().onClick.Invoke(); // 스크롤 뒤 마지막 실제 항목 상세 열기
        Assert.AreEqual(lastRelic.Name, view.transform.Find("Detail/Name").GetComponent<TMP_Text>().text); // 재사용한 버튼의 최신 이름 확인
        Assert.AreEqual(lastRelic.Description, view.transform.Find("Detail/Description").GetComponent<TMP_Text>().text); // 재사용한 버튼의 최신 설명 확인
        view.transform.Find("Detail/CloseButton").GetComponent<Button>().onClick.Invoke(); // 실제 상세 닫기
        yield return null; // 닫은 상세 화면 제거 대기
        scroll.verticalNormalizedPosition = 1f; // 상단으로 복귀
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 재사용 셀 재연결
        Assert.AreEqual(initialCells, grid.childCount); // 맨 아래와 상단 스크롤 동안 신규 셀 없음
        list.Find("SortButton").GetComponent<Button>().onClick.Invoke(); // 같은 유물 목록의 이름순 전환
        CollectionAssert.AreEquivalent(initialIds, grid.Cast<Transform>().Select(cell => cell.GetInstanceID())); // 정렬 중 파괴와 대체 없이 기존 셀을 그대로 유지
        Assert.IsNotNull(list.Find("SortButton").GetComponent<Button>()); // 이름 정렬 버튼 확인
        if (catalog.Relics.Count > 0) // 표시할 유물 존재 확인
        { // 유물 상세 확인 시작
            grid.Find("Entry_0").GetComponent<Button>().onClick.Invoke(); // 첫 유물 상세 열기
            Assert.IsTrue(view.transform.Find("Detail").gameObject.activeInHierarchy); // 실제 항목 상세 표시 확인
            view.transform.Find("Detail/CloseButton").GetComponent<Button>().onClick.Invoke(); // 상세 닫기
        } // 유물 상세 확인 종료
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 분류 화면 복귀
        Assert.IsTrue(view.transform.Find("Categories").gameObject.activeInHierarchy); // 도감 분류 화면 복귀 확인
        view.transform.Find("Categories/Category_4").GetComponent<Button>().onClick.Invoke(); // 상태 효과 목록 열기
        var states = CodexData.Load(4).OrderBy(entry => entry.Name, System.StringComparer.Ordinal).ToArray(); // 상태 도감의 전체 정방향 원본
        list.Find("SortButton").GetComponent<Button>().onClick.Invoke(); // 이름 역순 전환
        Assert.AreEqual(states.Last().Name, grid.Find("Entry_0/Name").GetComponent<TMP_Text>().text); // 역순 첫 항목과 상단 복귀 확인
        scroll.verticalNormalizedPosition = 0f; // 역순 정렬의 마지막 위치로 이동
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 재연결된 마지막 행 표시
        Assert.AreEqual(states.First().Name, grid.Find("Entry_" + (states.Length - 1) + "/Name").GetComponent<TMP_Text>().text); // 전체 원본의 역순 마지막 확인
        list.Find("SortButton").GetComponent<Button>().onClick.Invoke(); // 아래쪽에서 다시 이름순 정렬
        Assert.AreEqual(0f, grid.anchoredPosition.y, 0.1f); // 정렬 변경 시 기존 맨 위 복귀 유지
        Assert.AreEqual(states.First().Name, grid.Find("Entry_0/Name").GetComponent<TMP_Text>().text); // 정방향 첫 항목 복원
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 도감 분류 복귀
        view.transform.Find("Categories/Category_5").GetComponent<Button>().onClick.Invoke(); // 기록 분류 열기
        Assert.IsFalse(grid.Cast<Transform>().Any(child => child.gameObject.activeSelf)); // 빈 분류에 이전 항목 버튼 미노출
        Assert.IsTrue(view.transform.Find("ListPanel/Empty").gameObject.activeInHierarchy); // 데이터 없는 분류 안내 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 도감 분류 복귀
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 로비 복귀
        yield return null; // 도감 화면 제거 대기
        Assert.IsNull(Object.FindFirstObjectByType<CodexView>()); // 도감 닫힘 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 도감 화면 검증 종료
} // 도감 테스트 종료
