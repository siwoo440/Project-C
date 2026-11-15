using System.Collections; // 플레이 모드 검사 사용
using System.Collections.Generic; // 카드 목록 사용
using NUnit.Framework; // 자동 검증 기능 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.EventSystems; // 실제 포인터 적중 검사 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 손패 화면 이미지 사용
public sealed class BattleHandLayoutTests // 손패 배치와 마우스 강조 검사
{ // 검사 시작
    private static int clickedCount; // 검사용 카드 클릭 횟수
    [UnityTest] // 실제 손패 화면 검사 지정
    public IEnumerator Hand_FitsCardsAndRaisesHoveredCardWithoutReordering() // 다수 카드 배치와 강조 확인
    { // 손패 화면 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData ownerData = roster.Characters[0]; // 카드 소유자 원본 조회
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 카드 소유자 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/Attack"); // 실제 공격 카드 조회
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 손패 카드 항목 생성
        for (int index = 0; index < 20; index++) // 최대 손패 장수 순회
        { // 손패 항목 추가 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 소유자 포함 카드 항목 추가
        } // 손패 항목 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, entries, new[] { owner }, 20, 1); // 최대 손패 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { owner }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 턴 관리자 생성
        Assert.AreEqual(20, deck.DrawCards(20)); // 최대 손패 드로우 확인
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("HandLayoutCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // 독립 테스트 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 테스트 캔버스 유지
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // 실제 화면 좌표 캔버스 설정
        GameObject events = new GameObject("HandLayoutEvents", typeof(EventSystem)); // 독립 포인터 이벤트 시스템 생성
        Object.DontDestroyOnLoad(events); // 씬 전환 중 테스트 입력 시스템 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 캔버스 자식 배치
        RectTransform handRect = handObject.GetComponent<RectTransform>(); // 손패 사각형 조회
        handRect.sizeDelta = new Vector2(600f, 320f); // 좁은 손패 크기 설정
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, new BattleActionPointRuntime(6), turns)); // 실제 손패 연결 확인
        Canvas.ForceUpdateCanvases(); // 캔버스 배치 갱신
        yield return null; // 손패 배치 프레임 대기
        RectTransform layout = handObject.transform.Find("HandLayoutRoot").GetComponent<RectTransform>(); // 실제 손패 배치 영역 조회
        BattleCardView[] cards = layout.GetComponentsInChildren<BattleCardView>(); // 배치된 카드 화면 조회
        Assert.AreEqual(20, cards.Length); // 최대 손패 화면 수 확인
        AssertCardsWithinWidth(layout, cards); // 모든 카드의 화면 내부 배치 확인
        BattleCardView hovered = cards[4]; // 겹친 카드 선택
        RectTransform cardRect = hovered.GetComponent<RectTransform>(); // 카드 사각형 조회
        Vector2 originalPosition = cardRect.anchoredPosition; // 원래 카드 위치 저장
        int originalOrder = hovered.transform.GetSiblingIndex(); // 원래 카드 순서 저장
        BattleCardHitRegion hit = handObject.transform.Find("HandHitRoot").GetChild(4).GetComponent<BattleCardHitRegion>(); // 강조 카드의 실제 입력 영역 조회
        hit.OnPointerEnter(null); // 실제 입력 영역의 마우스 진입 처리
        Assert.Greater(cardRect.anchoredPosition.y, originalPosition.y); // 마우스 진입 시 위쪽 이동 확인
        Assert.AreEqual(layout.childCount - 1, hovered.transform.GetSiblingIndex()); // 강조 카드 최상단 표시 확인
        AssertCardsWithinWidth(layout, cards); // 강조 카드의 화면 내부 유지 확인
        Transform hitRoot = handObject.transform.Find("HandHitRoot"); // 카드별 독립 입력 영역 조회
        Assert.IsNotNull(hitRoot); // 카드 입력 영역 존재 확인
        Assert.AreEqual(20, hitRoot.childCount); // 모든 카드의 입력 영역 확인
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>(); // 실제 캔버스 적중 검사기 조회
        EventSystem eventSystem = events.GetComponent<EventSystem>(); // 포인터 이벤트 시스템 조회
        AssertRaycastHits(raycaster, eventSystem, hitRoot.GetChild(4) as RectTransform); // 강조 카드의 독립 적중 확인
        AssertRaycastHits(raycaster, eventSystem, hitRoot.GetChild(5) as RectTransform); // 겹친 이웃 카드의 독립 적중 확인
        hit.OnPointerExit(null); // 실제 입력 영역의 마우스 이탈 처리
        Assert.AreEqual(originalPosition, cardRect.anchoredPosition); // 마우스 이탈 시 원래 위치 복원 확인
        Assert.AreEqual(originalOrder, hovered.transform.GetSiblingIndex()); // 마우스 이탈 시 카드 순서 복원 확인
        handRect.sizeDelta = new Vector2(380f, 320f); // 더 좁은 화면 크기 설정
        Canvas.ForceUpdateCanvases(); // 캔버스 크기 갱신
        yield return null; // 크기 변경 반영 대기
        yield return null; // 손패 재배치 반영 대기
        AssertCardsWithinWidth(layout, cards); // 화면 크기 변경 후 내부 배치 확인
        Assert.IsNotNull(hand, "크기 변경 후 손패 화면"); // 손패 화면 유지 확인
        Assert.IsNotNull(hit, "크기 변경 후 입력 영역"); // 카드 입력 영역 유지 확인
        Assert.IsNotNull(hovered, "크기 변경 후 카드 화면"); // 카드 화면 유지 확인
        clickedCount = 0; // 카드 클릭 횟수 초기화
        hand.CardClicked += RecordClicked; // 실제 손패 카드 클릭 기록
        hit.OnPointerEnter(null); // 카드 입력 영역 재진입 처리
        hit.OnPointerClick(null); // 강조 카드 입력 영역 클릭 처리
        Assert.AreEqual(1, clickedCount); // 강조 카드 클릭 유지 확인
        hovered.SetInteractable(false); // 카드 사용 잠금 적용
        Assert.AreEqual(originalPosition.y, cardRect.anchoredPosition.y); // 사용 잠금 시 강조 위치 해제 확인
        Assert.AreEqual(originalOrder, hovered.transform.GetSiblingIndex()); // 사용 잠금 시 원래 순서 복원 확인
        hand.Unbind(); // 손패 이벤트 연결 해제
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 캔버스 정리
        Object.DestroyImmediate(events); // 이벤트 시스템 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
        Object.DestroyImmediate(deckData); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 손패 화면 검사 종료
    private static void RecordClicked(CardInstance card) // 실제 카드 클릭 기록
    { // 클릭 기록 시작
        clickedCount++; // 카드 클릭 횟수 증가
    } // 클릭 기록 종료
    private static void AssertCardsWithinWidth(RectTransform layout, BattleCardView[] cards) // 손패 가로 범위 검사
    { // 가로 범위 검사 시작
        foreach (BattleCardView card in cards) // 손패 카드 순회
        { // 카드 범위 검사 시작
            RectTransform rect = card.GetComponent<RectTransform>(); // 카드 사각형 조회
            float halfWidth = rect.rect.width * rect.localScale.x * 0.5f; // 현재 카드 절반 너비 계산
            Assert.LessOrEqual(Mathf.Abs(rect.anchoredPosition.x) + halfWidth, layout.rect.width * 0.5f + 0.1f); // 손패 내부 가로 범위 확인
        } // 카드 범위 검사 종료
    } // 가로 범위 검사 종료
    private static void AssertRaycastHits(GraphicRaycaster raycaster, EventSystem eventSystem, RectTransform target) // 실제 포인터 적중 검사
    { // 포인터 적중 검사 시작
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center)); // 입력 영역 중심의 화면 좌표 계산
        PointerEventData pointer = new PointerEventData(eventSystem) { position = point }; // 실제 화면 좌표 포인터 생성
        List<RaycastResult> results = new List<RaycastResult>(); // 포인터 적중 결과 목록 생성
        raycaster.Raycast(pointer, results); // 실제 캔버스 포인터 적중 검사
        Image image = target.GetComponent<Image>(); // 입력 이미지 상태 조회
        if (image.depth < 0) // 렌더링 없는 테스트 환경 확인
        { // 입력 영역 좌표 검사 시작
            Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(target, point, null)); // 포인터의 입력 영역 포함 확인
            return; // 렌더링 기반 적중 검사 생략
        } // 입력 영역 좌표 검사 종료
        Assert.IsNotEmpty(results, $"좌표 {point} / 화면 {Screen.width}x{Screen.height} / 깊이 {image.depth} / 컬링 {image.canvasRenderer.cull} / 캔버스 {raycaster.GetComponent<Canvas>().renderMode} / 입력 {image.raycastTarget}"); // 실제 적중 결과와 환경 확인
        Assert.AreSame(target.gameObject, results[0].gameObject); // 해당 카드 입력 영역 최우선 적중 확인
    } // 포인터 적중 검사 종료
} // 검사 종료
