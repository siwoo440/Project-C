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
    private static readonly string[] HiddenCardIds = { "CRD_TEST_ALLY_HEAL", "CRD_TEST_ATTACK", "CRD_TEST_MAGIC_ATTACK", "CRD_TEST_SELF_HEAL", "CRD_DEBUG_ALLY_MENTAL_BREAK", "CRD_DEBUG_ALLY_INSTANT_KILL", "CRD_DEBUG_INSTANT_KILL" }; // 도감에서만 제외할 검증 카드 식별자
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
        CollectionAssert.AreEqual(catalog.Consumables.OfType<PotionData>().Select(item => (item.DisplayName, item.Description)), CodexData.Load(1).Select(item => (item.Name, item.Description))); // 포션 설명과 도감의 원본 일치 확인
        Assert.AreEqual(catalog.Relics.Count, CodexData.Load(3).Count); // 실제 유물 목록 일치 확인
        Assert.AreEqual(ExplorationEventCatalog.LoadEvents().Count, CodexData.Load(0).Count); // 실제 이벤트 목록 일치 확인
        CollectionAssert.AreEqual(catalog.Relics.Select(item => (item.DisplayName, item.Description)), CodexData.Load(3).Select(item => (item.Name, item.Description))); // 생성된 색인의 원본 순서와 내용 확인
        CollectionAssert.AreEqual(ExplorationEventCatalog.LoadEvents().Select(item => (item.DisplayName, item.Description)), CodexData.Load(0).Select(item => (item.Name, item.Description))); // 코드형 사건의 원문과 원본 순서 확인
        Assert.Greater(CodexData.Load(2).Count, 0); // 실제 조우의 적 포함 확인
        Assert.Greater(CodexData.Load(4).Count, 0); // 실제 전투 상태 효과 포함 확인
        Assert.AreEqual(0, CodexData.Load(5).Count); // 원본 없는 기록 분류 확인
        Assert.AreEqual(0, CodexData.Load(6).Count); // 원본 없는 오브젝트 분류 확인
        var publishedCards = catalog.Cards.Where(item => !HiddenCardIds.Contains(item.CardId)).ToArray(); // 공개 도감에 남을 원본 카드 목록
        Assert.AreEqual(7, catalog.Cards.Count(item => HiddenCardIds.Contains(item.CardId))); // 이전 저장과 내부 검증에서 쓰는 원본 확인
        var entries = CodexData.Load(7); // 도감에 등록된 실제 카드 목록
        CollectionAssert.AreEqual(publishedCards.Select(item => (item.DisplayName, item.Description)), entries.Select(item => (item.Name, item.Description))); // 지정된 일곱 카드만 제외한 실제 내용과 순서
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 카드 전투 인스턴스용 실제 아군 원본
        var owner = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 원본 카드 기본 비용을 확인할 소유자
        CollectionAssert.AreEqual(publishedCards.Select(item => item.CardType), entries.Select(item => item.CardKind.Value)); // 모든 도감 카드의 실제 종류 확인
        CollectionAssert.AreEqual(publishedCards.Select((item, index) => CardInstance.Create(item, owner, index, 0).CostLabel), entries.Select(item => item.CostLabel)); // 고정·전액·최소 비용과 전투 표시 일치 확인
    } // 도감 원본 검사 종료
    [Test] // 등록 항목의 목록 이미지와 상태 상세 표시 검사
    public void CodexData_LoadsArtworkForEveryRegisteredEntry() // 이미지가 비어 있는 기존 도감 항목 검증
    { // 도감 전체 썸네일 검사 시작
        foreach (int category in new[] { 0, 1, 2, 3, 4, 7 }) // 실제 자료가 있는 도감 분류 순회
        { // 분류별 이미지 검사 시작
            foreach (CodexEntry entry in CodexData.Load(category)) // 실제 등록 항목 순회
            { // 개별 항목의 미리보기 확인 시작
                Assert.IsTrue(entry.HasArtwork, entry.Name); // 항목의 원화 연결 확인
                Assert.IsNotNull(entry.LoadThumbnail(), entry.Name); // 목록에서 사용하는 미리보기 확인
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Description), entry.Name); // 등록 항목의 상세 설명 확인
            } // 개별 항목의 미리보기 확인 종료
        } // 분류별 이미지 검사 종료
        foreach (CodexEntry status in CodexData.Load(4)) // 전투 상태 상세 이미지 순회
        { // 상태 상세 원화 확인 시작
            Assert.IsNotNull(status.Image, status.Name); // 목록과 상세 양쪽의 상태 이미지 확인
        } // 상태 상세 원화 확인 종료
    } // 도감 전체 썸네일 검사 종료
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
        Scrollbar scrollbar = scroll.verticalScrollbar; // 공통 도감의 세로 이동 막대
        Assert.IsNotNull(scrollbar); // 마우스로 조작할 수 있는 세로 막대 표시 확인
        Assert.AreEqual(Scrollbar.Direction.BottomToTop, scrollbar.direction); // 상단 값과 목록 이동 방향 일치 확인
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 원본의 전체 목록
        Assert.AreEqual(((RectTransform)grid.Find("Entry_0")).anchoredPosition.y, ((RectTransform)grid.Find("Entry_5")).anchoredPosition.y); // 첫 행에 여섯 항목 배치
        Assert.Less(((RectTransform)grid.Find("Entry_6")).anchoredPosition.y, ((RectTransform)grid.Find("Entry_5")).anchoredPosition.y); // 일곱 번째부터 다음 줄 표시
        Assert.AreEqual(40f + Mathf.Ceil(catalog.Relics.Count / 6f) * 253f - 18f, grid.rect.height); // 셀 수와 별개로 전체 유물 행의 스크롤 길이 유지
        int initialCells = grid.childCount; // 상단에서 생성된 실제 셀 수
        int visibleCapacity = 6 * (Mathf.CeilToInt(scroll.viewport.rect.height / 253f) + 2); // 보이는 행과 여유 행의 최대 셀 수
        Assert.LessOrEqual(initialCells, Mathf.Min(visibleCapacity, catalog.Relics.Count)); // 전체 자료가 늘어도 화면을 넘는 셀 생성 방지
        var initialIds = grid.Cast<Transform>().Select(cell => cell.GetInstanceID()).ToArray(); // 같은 객체 재사용을 확인할 식별자
        scrollbar.value = 0f; // 실제 스크롤바 조작으로 유물 목록 끝까지 이동
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 실제 스크롤 위치 변경 전달
        var tail = grid.Find("Entry_" + (catalog.Relics.Count - 1)); // 마지막 논리적 유물의 재사용 셀
        Assert.IsTrue(tail != null && tail.gameObject.activeInHierarchy); // 마지막 행까지 스크롤 가능 확인
        var lastRelic = CodexData.Load(3).OrderBy(entry => entry.Name, System.StringComparer.Ordinal).Last(); // 데이터의 실제 마지막 항목
        Assert.AreEqual(lastRelic.Name, tail.Find("Name").GetComponent<TMP_Text>().text); // 재연결한 원본 이름 일치
        Sprite thumb = tail.Find("Image").GetComponent<Image>().sprite; // 현재 셀과 연결된 실제 썸네일 조회
        Assert.AreSame(lastRelic.LoadThumbnail(), thumb); // 이전 셀 그림과 독립 썸네일 구별
        Assert.IsNotNull(thumb); // 원화가 있는 유물의 독립 썸네일 확인
        Assert.LessOrEqual(Mathf.Max(thumb.texture.width, thumb.texture.height), 128); // 작은 별도 텍스처의 실제 임포트 크기 확인
        tail.GetComponent<Button>().onClick.Invoke(); // 스크롤 뒤 마지막 실제 항목 상세 열기
        foreach (string part in new[] { "Nameplate", "ArtworkFrame", "DescriptionPanel" }) // 상세 배경 세 영역 순회
        { // 상세 배경 검사 시작
            Transform backdrop = view.transform.Find("Detail/" + part); // 상세 화면의 배경 영역 조회
            Assert.IsNotNull(backdrop, part); // 지정한 위치의 독립 배경 확인
            Image backdropImage = backdrop.GetComponent<Image>(); // 배경 스프라이트 표시 요소 조회
            Assert.IsNotNull(backdropImage.sprite, part); // 실제 배경 이미지 연결 확인
            Assert.AreEqual("CodexDetail" + part, backdropImage.sprite.name, part); // 영역별 고유 배경 확인
            Assert.AreEqual(Image.Type.Sliced, backdropImage.type, part); // 해상도 변경 시 테두리 형상 보존 확인
            Assert.IsFalse(backdropImage.raycastTarget, part); // 꾸밈 이미지의 입력 가로채기 방지 확인
        } // 상세 배경 검사 종료
        Assert.AreEqual(lastRelic.Name, view.transform.Find("Detail/Name").GetComponent<TMP_Text>().text); // 재사용한 버튼의 최신 이름 확인
        Assert.AreEqual(lastRelic.Description, view.transform.Find("Detail/Description").GetComponent<TMP_Text>().text); // 재사용한 버튼의 최신 설명 확인
        Image detailArtwork = view.transform.Find("Detail/Artwork").GetComponent<Image>(); // 선택 상세의 별도 원화 표시 조회
        for (float artDeadline = Time.realtimeSinceStartup + 15f; detailArtwork.sprite == null && Time.realtimeSinceStartup < artDeadline;) // 원화 비동기 로드 완료 확인
        { // 원화 렌더 대기 시작
            yield return null; // 실제 플레이어 프레임에서 요청 결과 대기
        } // 원화 렌더 대기 종료
        Assert.IsNotNull(detailArtwork.sprite); // 선택한 유물 원화 실제 표시 확인
        Assert.AreNotSame(thumb.texture, detailArtwork.sprite.texture); // 원화와 목록 썸네일의 독립 네이티브 텍스처 확인
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
        Assert.IsTrue(grid.Cast<Transform>().All(cell => cell.Find("Image").GetComponent<Image>().sprite == null)); // 분류를 닫은 뒤 재사용 셀의 썸네일 참조 해제 확인
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
        Assert.IsFalse(scrollbar.interactable); // 내용이 없는 목록의 이동 비활성 확인
        list.Find("CategoryButton").GetComponent<Button>().onClick.Invoke(); // 목록에서 분류 메뉴 열기
        Transform categoryMenu = list.Find("CategoryMenu"); // 바로 전환할 도감 분류 메뉴 조회
        Assert.IsTrue(categoryMenu.gameObject.activeSelf); // 이름 정렬과 다른 분류 메뉴 표시 확인
        categoryMenu.Find("Category_7").GetComponent<Button>().onClick.Invoke(); // 분류 메뉴에서 카드 목록으로 이동
        Assert.IsFalse(categoryMenu.gameObject.activeSelf); // 분류 전환 후 메뉴 닫기 확인
        Assert.IsTrue(scrollbar.interactable); // 긴 카드 목록에서 스크롤바 활성 확인
        var cards = CodexData.Load(7).OrderBy(item => item.Name, System.StringComparer.Ordinal).ToArray(); // 전투 원본을 별도 대조한 실제 카드 도감의 이름순 자료
        Button categoryButton = list.Find("CategoryButton").GetComponent<Button>(); // 카드 분류 패널의 실제 열기 버튼
        categoryButton.onClick.Invoke(); // 카드 분류 패널 열기
        Transform typeSort = categoryMenu.Find("SortByType"); // 카드 종류 정렬 선택지 조회
        Assert.IsNotNull(typeSort); // 카드 목록에서 종류순 선택지 확인
        Transform costSort = categoryMenu.Find("SortByCost"); // AP 비용 정렬 선택지 조회
        Assert.IsNotNull(costSort); // 카드 목록에서 AP 비용순 선택지 확인
        var typeOrder = cards.OrderBy(card => card.CardKind).ToArray(); // 종류와 이름 기준의 전체 카드 순서
        AssertSortDirection(typeSort, grid, typeOrder); // 종류순과 반대 방향 및 화살표 이미지 검사
        var costOrder = cards.OrderBy(card => CostOrder(card.CostLabel)).ToArray(); // 비용 표기에 따른 전체 카드 순서
        AssertSortDirection(costSort, grid, costOrder); // AP 비용순과 반대 방향 및 화살표 이미지 검사
        Assert.IsFalse(typeSort.Find("Direction").GetComponent<Image>().enabled); // 선택이 바뀐 종류순의 화살표 숨김 확인
        list.Find("SortButton").GetComponent<Button>().onClick.Invoke(); // 비용순에서 이름 정방향으로 복귀
        categoryButton.onClick.Invoke(); // 카드 분류 패널 닫기
        AssertCardBadges(grid, cards); // 첫 행의 카드 종류와 AP 비용 확인
        scrollbar.value = 0f; // 카드 목록 끝으로 이동하는 실제 스크롤바 입력
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 표시 중인 재사용 셀 갱신
        Assert.IsNotNull(grid.Find("Entry_" + (cards.Length - 1))); // 마지막 카드에 실제로 도달했는지 확인
        AssertCardBadges(grid, cards); // 재사용한 카드 칸의 새 종류와 비용 확인
        list.Find("CategoryButton").GetComponent<Button>().onClick.Invoke(); // 같은 목록에서 다시 분류 열기
        categoryMenu.Find("Category_3").GetComponent<Button>().onClick.Invoke(); // 카드에서 유물 목록으로 전환
        Assert.IsTrue(grid.Cast<Transform>().All(cell => cell.Find("CardMeta") == null || !cell.Find("CardMeta").gameObject.activeSelf)); // 재사용된 유물 칸에 카드 정보가 남지 않는지 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 도감 분류 복귀
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 로비 복귀
        yield return null; // 도감 화면 제거 대기
        Assert.IsNull(Object.FindFirstObjectByType<CodexView>()); // 도감 닫힘 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 도감 화면 검증 종료
    private static void AssertSortDirection(Transform button, RectTransform grid, CodexEntry[] ordered) // 정렬 버튼과 카드 목록의 양방향 검사
    { // 정렬 방향 검사 시작
        button.GetComponent<Button>().onClick.Invoke(); // 새 기준의 정방향 정렬 적용
        Image arrow = button.Find("Direction").GetComponent<Image>(); // 정렬 방향의 스프라이트 이미지 조회
        Assert.IsTrue(arrow.enabled); // 현재 정렬 기준의 화살표 표시 확인
        Assert.AreEqual("SortAscending", arrow.sprite.name); // 오름차순 이미지 확인
        CollectionAssert.AreEqual(ordered.Take(6).Select(card => card.Name), Enumerable.Range(0, 6).Select(index => grid.Find("Entry_" + index + "/Name").GetComponent<TMP_Text>().text)); // 정방향 첫 행의 카드 순서 확인
        button.GetComponent<Button>().onClick.Invoke(); // 같은 기준을 다시 눌러 뒤집기
        Assert.AreEqual("SortDescending", arrow.sprite.name); // 내림차순 이미지 확인
        CollectionAssert.AreEqual(ordered.Reverse().Take(6).Select(card => card.Name), Enumerable.Range(0, 6).Select(index => grid.Find("Entry_" + index + "/Name").GetComponent<TMP_Text>().text)); // 역방향 첫 행의 카드 순서 확인
    } // 정렬 방향 검사 종료
    private static int CostOrder(string label) // 표시 비용별 기대 정렬 순서 계산
    { // 비용 순서 계산 시작
        return label == "X" ? int.MaxValue : int.Parse(label.TrimEnd('+')) * 2 + (label.EndsWith("+") ? 1 : 0); // 고정·최소·전액 비용의 비교값 반환
    } // 비용 순서 계산 종료
    private static void AssertCardBadges(RectTransform grid, CodexEntry[] cards) // 화면에 연결된 카드 칸의 표시값 검사
    { // 카드 배지 검사 시작
        foreach (Transform cell in grid.Cast<Transform>().Where(item => item.gameObject.activeSelf)) // 재사용 중인 활성 카드 칸 확인
        { // 카드별 범위와 표시 검증 시작
            int index = int.Parse(cell.name.Substring("Entry_".Length)); // 현재 실제 카드 목록의 논리 번호
            Transform badge = cell.Find("CardMeta"); // 카드 왼쪽 위 종류와 비용 표시 조회
            Assert.IsNotNull(badge); // 카드 배지 생성 확인
            Assert.IsTrue(badge.gameObject.activeSelf); // 해당 칸의 배지 표시 확인
            Assert.AreEqual("AP " + cards[index].CostLabel, badge.Find("Cost").GetComponent<TMP_Text>().text); // 전투 카드와 같은 기본 비용과 가변 표기 확인
            Assert.IsNull(badge.Find("Type")); // 카드 종류 글자 제거 확인
            Image typeIcon = badge.Find("Icon").GetComponent<Image>(); // 기존 종류 아이콘 조회
            Assert.AreEqual(cards[index].CardKind != CardType.Curse, typeIcon.enabled, cards[index].Name); // 기존 종류 원화의 표시 범위 확인
            if (typeIcon.enabled) // 기존 원화가 연결된 종류 확인
            { // 아이콘 원화 확인 시작
                Assert.IsNotNull(typeIcon.sprite, cards[index].Name); // 표시된 아이콘의 원본 확인
            } // 아이콘 원화 확인 종료
        } // 카드별 범위와 표시 검증 종료
    } // 카드 배지 검사 종료
} // 도감 테스트 종료
