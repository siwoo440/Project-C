using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 손패 카드 목록 사용
using NUnit.Framework; // 카드 미리보기 검증 사용
using TMPro; // 카드 설명 표시 검사 사용
using UnityEngine; // 전투 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 카드 이미지와 입력 검사 사용
public sealed class BattleCardPreviewTests // 확대 카드와 설명 표시 검사
{ // 카드 미리보기 검사 시작
    private static int clickCount; // 사용 불가 카드 클릭 기록
    private static void RecordClick(CardInstance card) // 카드 클릭 기록 처리
    { // 클릭 기록 시작
        clickCount++; // 실제 카드 클릭 횟수 증가
    } // 클릭 기록 종료
    [UnityTest] // 실제 손패 화면 검사 지정
    public IEnumerator PreviewShowsReadableDescriptionAndPreservesCardInput() // 확대 카드와 설명 및 입력 확인
    { // 카드 미리보기 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData ownerData = roster.Characters[3]; // 긴 설명 카드 소유자 조회
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 실제 카드 소유자 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/EmpressFeast"); // 실제 긴 설명 카드 조회
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 손패 카드 항목 생성
        for (int index = 0; index < 8; index++) // 여덟 장 손패 순회
        { // 손패 카드 추가 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 실제 카드 항목 추가
        } // 손패 카드 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, entries, new[] { owner }, 20, 1); // 실제 손패 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { owner }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 실제 전투 턴 생성
        Assert.AreEqual(8, deck.DrawCards(8)); // 손패 카드 뽑기 확인
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 테스트 캔버스 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        handObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 350f); // 실제 손패 화면 크기 설정
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 실제 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 덱과 턴 연결 확인
        Canvas.ForceUpdateCanvases(); // 손패 배치 갱신
        yield return null; // 실제 카드 배치 대기
        BattleCardHitRegion hit = handObject.transform.Find("HandHitRoot").GetChild(3).GetComponent<BattleCardHitRegion>(); // 실제 카드 입력 영역 조회
        hit.OnPointerEnter(null); // 카드에 마우스 진입 처리
        Transform preview = handObject.transform.Find("CardPreview"); // 확대 카드 미리보기 조회
        Assert.IsNotNull(preview); // 확대 카드 화면 존재 확인
        Assert.IsTrue(preview.gameObject.activeSelf); // 마우스 진입 시 확대 카드 표시 확인
        Canvas previewCanvas = preview.GetComponent<Canvas>(); // 확대 카드 표시 순서 조회
        Assert.IsTrue(previewCanvas.overrideSorting); // 확대 카드 독립 표시 순서 확인
        Assert.Greater(previewCanvas.sortingOrder, 430); // 레벨 표시보다 앞에 카드 제목 배치 확인
        Assert.Less(previewCanvas.sortingOrder, 440); // 전투 선택 창보다 뒤에 카드 배치 확인
        RectTransform previewRect = preview as RectTransform; // 확대 카드 영역 조회
        Assert.Greater(previewRect.rect.width, BattleCardView.CardWidth * 1.5f); // 기본 카드보다 충분히 큰 너비 확인
        Assert.Greater(previewRect.rect.height, BattleCardView.CardHeight * 1.5f); // 기본 카드보다 충분히 큰 높이 확인
        TMP_Text description = preview.Find("PreviewDescription").GetComponent<TMP_Text>(); // 확대 카드 설명 조회
        Assert.AreEqual(cardData.Description, description.text); // 실제 카드의 전체 설명 표시 확인
        Assert.AreNotEqual(TextOverflowModes.Ellipsis, description.overflowMode); // 긴 설명 생략 표시 방지 확인
        foreach (Graphic graphic in preview.GetComponentsInChildren<Graphic>()) // 확대 카드의 전체 그래픽 순회
        { // 확대 카드 입력 검사 시작
            Assert.IsFalse(graphic.raycastTarget); // 카드와 버튼 입력 통과 확인
        } // 확대 카드 입력 검사 종료
        hit.OnPointerExit(null); // 카드 마우스 이탈 처리
        Assert.IsFalse(preview.gameObject.activeSelf); // 선택하지 않은 카드의 미리보기 종료 확인
        hand.SetSelectedCard(hit.CardView.RuntimeCard); // 실제 카드 선택 표시 설정
        Assert.IsTrue(preview.gameObject.activeSelf); // 선택 후 마우스 이탈 상태의 확대 유지 확인
        hand.SetCardDragging(hit.CardView, true); // 실제 카드 드래그 시작 상태 설정
        Assert.IsFalse(preview.gameObject.activeSelf); // 드래그 중 확대 화면 숨김 확인
        hand.SetCardDragging(hit.CardView, false); // 카드 드래그 종료 상태 설정
        Assert.IsTrue(preview.gameObject.activeSelf); // 드래그 종료 후 선택 카드 확대 복원 확인
        hand.SetSelectedCard(null); // 카드 선택 취소
        Assert.IsFalse(preview.gameObject.activeSelf); // 선택 취소 후 확대 화면 숨김 확인
        Assert.IsNotNull(hand); // 손패 화면 유지 확인
        Assert.IsNotNull(hit); // 카드 입력 영역 유지 확인
        Assert.IsNotNull(hit.CardView); // 카드 화면 연결 유지 확인
        clickCount = 0; // 사용 불가 카드 클릭 기록 초기화
        hand.CardClicked += RecordClick; // 실제 카드 클릭 횟수 기록
        hit.CardView.SetInteractable(false); // 카드 사용 불가 상태 설정
        hit.OnPointerEnter(null); // 사용 불가 카드 마우스 진입
        Assert.IsTrue(preview.gameObject.activeSelf); // 사용 불가 카드의 설명 확인 허용
        hit.OnPointerClick(null); // 사용 불가 카드 클릭 시도
        Assert.AreEqual(0, clickCount); // 사용 불가 카드 선택 차단 확인
        hit.OnPointerExit(null); // 사용 불가 카드 마우스 이탈
        string[] cardPaths = { "Attack", "FoolChoice", "HangedTransfer", "FateReroll", "EmpressFeast" }; // 다섯 종류의 실제 카드 경로
        string[] iconNames = { "CardTypeSword", "CardTypeWand", "CardTypeCup", "CardTypePentacle", "CardTypeShield" }; // 카드 종류별 투명 아이콘 이름
        Assert.IsNull(hit.CardView.transform.Find("TypeText")); // 손패의 카드 종류 영문 표시 제거 확인
        for (int index = 0; index < cardPaths.Length; index++) // 다섯 종류의 실제 카드 순회
        { // 카드별 코스트 아이콘 검사 시작
            CardData typeData = Resources.Load<CardData>("StarterCards/" + cardPaths[index]); // 카드 종류별 실제 원본 조회
            Assert.IsNotNull(typeData); // 실제 카드 원본 존재 확인
            CardInstance typeCard = CardInstance.Create(typeData, owner, index); // 카드 종류별 실제 인스턴스 생성
            hit.CardView.Bind(typeCard); // 실제 손패 카드에 종류별 인스턴스 연결
            hit.OnPointerEnter(null); // 사용 불가 카드를 포함한 확대 화면 표시
            Image badge = hit.CardView.transform.Find("CostBadge").GetComponent<Image>(); // 손패 카드 코스트 아이콘 조회
            Image previewBadge = preview.Find("PreviewCostBadge").GetComponent<Image>(); // 확대 카드 코스트 아이콘 조회
            Assert.AreEqual(iconNames[index], badge.sprite.texture.name); // 종류별 손패 코스트 아이콘 확인
            Assert.AreEqual(iconNames[index], previewBadge.sprite.texture.name); // 종류별 확대 코스트 아이콘 확인
            Assert.AreEqual(typeCard.ApCost.ToString(), badge.transform.Find("CostText").GetComponent<TMP_Text>().text); // 손패 코스트 숫자 유지 확인
            Assert.AreEqual(typeCard.ApCost.ToString(), previewBadge.transform.Find("PreviewCost").GetComponent<TMP_Text>().text); // 확대 코스트 숫자 유지 확인
            Assert.AreEqual(owner.DisplayName, preview.Find("PreviewInfo").GetComponent<TMP_Text>().text); // 확대 카드의 영문 속성 제거 확인
            hit.OnPointerExit(null); // 종류별 카드 마우스 이탈
        } // 카드별 코스트 아이콘 검사 종료
        hand.Unbind(); // 손패 연결 해제
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
        Object.DestroyImmediate(deckData); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 카드 미리보기 검사 종료
} // 카드 미리보기 검사 종료
