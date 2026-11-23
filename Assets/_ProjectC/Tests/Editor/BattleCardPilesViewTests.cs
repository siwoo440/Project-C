using System.Collections; // 실제 전투 화면 대기 사용
using System.Collections.Generic; // 실제 손패 카드 목록 사용
using NUnit.Framework; // 카드 더미와 배치 검증 사용
using TMPro; // 실제 카드 더미 숫자 확인 사용
using UnityEngine; // 전투 화면 좌표와 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 카드 더미 이미지 검사 사용
public sealed class BattleCardPilesViewTests // 뽑기와 버리기 더미 화면 검사
{ // 카드 더미 화면 검사 시작
    [UnityTest] // 실제 손패 화면 검사 지정
    public IEnumerator PilesAndHandRemainSeparateAtDifferentWidths() // 양쪽 카드 더미와 중앙 손패 배치 확인
    { // 실제 카드 더미 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData ownerData = roster.Characters[0]; // 실제 카드 소유자 조회
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 실제 아군 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 테스트 적 데이터 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 데이터 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/Attack"); // 실제 공격 카드 원본 조회
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 실제 손패 카드 목록 생성
        for (int index = 0; index < 8; index++) // 여덟 장 카드 순회
        { // 실제 카드 추가 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 실제 소유자와 카드 연결
        } // 실제 카드 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, entries, new[] { owner }, 20, 1); // 실제 뽑기와 손패 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 실제 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { owner }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 실제 전투 턴 생성
        Assert.AreEqual(4, deck.DrawCards(4)); // 실제 카드 네 장 뽑기 확인
        Assert.IsTrue(turns.StartBattle(0)); // 실제 전투 턴 시작 확인
        GameObject canvasObject = new GameObject("PileCanvas", typeof(RectTransform), typeof(Canvas)); // 실제 손패 테스트 캔버스 생성
        Object.DontDestroyOnLoad(canvasObject); // 씬 전환 중 테스트 캔버스 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 전투 손패 화면 생성
        handObject.transform.SetParent(canvasObject.transform, false); // 테스트 캔버스에 손패 연결
        RectTransform handRect = handObject.GetComponent<RectTransform>(); // 실제 손패 영역 조회
        handRect.sizeDelta = new Vector2(1400f, 350f); // 실제와 비슷한 손패 화면 크기
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 실제 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 전투 덱과 손패 연결 확인
        PlayerLevelConfig levelConfig = ScriptableObject.CreateInstance<PlayerLevelConfig>(); // 레벨 표시 설정 생성
        PlayerLevelRunManager levelManager = PlayerLevelRunManager.EnsureInstance(levelConfig); // 실제 전투 레벨 관리자 조회
        PlayerLevelHudView level = PlayerLevelHudView.Create(canvasObject.GetComponent<Canvas>(), levelManager); // 실제 손패 레벨 표시 생성
        Assert.AreEqual(-32f, (level.transform as RectTransform).anchoredPosition.y); // 지정한 손패 상단 중앙의 레벨 위치 확인
        Canvas.ForceUpdateCanvases(); // 실제 손패 화면 배치 갱신
        RectTransform draw = hand.transform.Find("DrawPile") as RectTransform; // 왼쪽 뽑기 더미 영역 조회
        RectTransform discard = hand.transform.Find("DiscardPile") as RectTransform; // 오른쪽 버리기 더미 영역 조회
        RectTransform orb = hand.transform.Find("ActionPointOrb") as RectTransform; // 손패 왼쪽 위 행동력 구체 영역 조회
        RectTransform layout = hand.transform.Find("HandLayoutRoot") as RectTransform; // 실제 가운데 손패 영역 조회
        Assert.IsNotNull(draw); // 왼쪽 뽑기 더미 존재 확인
        Assert.IsNotNull(discard); // 오른쪽 버리기 더미 존재 확인
        Assert.IsNull(hand.transform.Find("DeckStatusText")); // 손패 상단 카드 수량 문구 제거 확인
        TMP_Text drawCount = draw.Find("Count/CountText").GetComponent<TMP_Text>(); // 뽑기 더미의 실제 수량 조회
        TMP_Text discardCount = discard.Find("Count/CountText").GetComponent<TMP_Text>(); // 버린 더미의 실제 수량 조회
        Assert.AreEqual("4", drawCount.text); // 남은 뽑기 카드 수 확인
        Assert.AreEqual("0", discardCount.text); // 빈 버린 카드 수 확인
        Assert.IsFalse(drawCount.raycastTarget || discardCount.raycastTarget); // 수량 글자의 카드 입력 차단 방지 확인
        Assert.AreEqual("DrawPile", draw.GetComponent<Image>().sprite.texture.name); // 제작한 뽑기 카드 이미지 연결 확인
        Assert.AreEqual("DiscardPile", discard.GetComponent<Image>().sprite.texture.name); // 제작한 묘지 카드 이미지 연결 확인
        Assert.IsFalse(draw.GetComponent<Image>().raycastTarget); // 뽑기 더미의 카드 입력 차단 방지 확인
        Assert.IsFalse(discard.GetComponent<Image>().raycastTarget); // 버리기 더미의 카드 입력 차단 방지 확인
        Assert.IsTrue(hand.DrawPileAnchor == draw && hand.DiscardPileAnchor == discard); // 카드 이동 연출용 실제 더미 위치 연결 확인
        Assert.IsNotNull(orb); // 손패 패널 왼쪽 위 행동력 구체 확인
        Assert.GreaterOrEqual(draw.rect.width, 170f); // 확대된 뽑기 더미 너비 확인
        Assert.GreaterOrEqual(discard.rect.width, 170f); // 확대된 묘지 더미 너비 확인
        AssertPileLayout(handRect, draw, discard, orb, layout); // 실제 크기에서 더미와 손패 구분 확인
        handRect.sizeDelta = new Vector2(600f, 350f); // 좁은 화면 손패 크기 설정
        Canvas.ForceUpdateCanvases(); // 좁은 화면 손패 배치 갱신
        yield return new WaitForSecondsRealtime(0.1f); // 실제 좁은 화면 배치 갱신 대기
        AssertPileLayout(handRect, draw, discard, orb, layout); // 좁은 화면의 더미와 손패 구분 확인
        Color emptyDiscard = discard.GetComponent<Image>().color; // 빈 버리기 더미 표시 색상 저장
        CardInstance keptCard = deck.Hand[1]; // 제거 뒤에도 실제 손패에 남을 복제 카드
        BattleCardView keptView = layout.GetChild(1).GetComponent<BattleCardView>(); // 현재 카드에 연결된 화면 참조
        Transform hitRoot = hand.transform.Find("HandHitRoot"); // 대응 카드의 실제 입력 영역
        BattleCardHitRegion keptHit = hitRoot.GetChild(1).GetComponent<BattleCardHitRegion>(); // 현재 카드에 연결된 입력 참조
        keptHit.OnPointerEnter(null); // 실제 버튼 위치를 가리킨 상태에서 손패 갱신 재현
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, keptView.transform.localEulerAngles.z)), 0.01f); // 진입 직후 카드 강조 확인
        deck.ShuffleDrawPile(); // 포인터 대상과 슬롯이 그대로인 실제 덱 변경
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, keptView.transform.localEulerAngles.z)), 0.01f); // 자리 이동이 없는 갱신의 강조 유지 확인
        Assert.IsTrue(deck.DiscardCard(deck.Hand[0])); // 실제 손패 카드 버리기 확인
        Assert.Greater(discard.GetComponent<Image>().color.a, emptyDiscard.a); // 버린 카드 존재 시 더미 표시 강조 확인
        Assert.AreEqual("1", discardCount.text); // 실제 버린 카드 수 갱신 확인
        Assert.AreEqual(1, deck.DrawCards(1)); // 실제 카드 한 장 추가 뽑기
        Assert.AreEqual("3", drawCount.text); // 실제 뽑기 더미 수 갱신 확인
        yield return null; // 제거 카드의 장면 정리 대기
        Assert.AreSame(keptCard, deck.Hand[0]); // 삭제 뒤 남은 카드의 원본 손패 순서 확인
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, keptView.transform.localEulerAngles.z)), 0.01f); // 포인터를 다시 움직이기 전에도 기존 카드 강조 유지 확인
        keptHit.OnPointerExit(null); // 실제 입력 영역에서 포인터 이탈
        Assert.AreSame(keptView, layout.GetChild(0).GetComponent<BattleCardView>()); // 남은 카드 그림과 상태 재사용 확인
        Assert.AreSame(keptHit, hitRoot.GetChild(0).GetComponent<BattleCardHitRegion>()); // 남은 카드의 실제 입력 영역 재사용 확인
        Assert.AreSame(keptView, keptHit.CardView); // 옮겨진 슬롯의 클릭 대상 일치 확인
        Assert.AreEqual(deck.HandCount, layout.GetComponentsInChildren<BattleCardView>(false).Length); // 프레임 끝 제거 전에도 실제 손패만 표시되는지 확인
        hand.Unbind(); // 실제 손패와 덱 연결 해제
        level.Dispose(); // 레벨 표시 이벤트 연결 해제
        turns.Dispose(); // 실제 전투 턴 관리자 정리
        Object.DestroyImmediate(canvasObject); // 테스트 캔버스 정리
        Object.DestroyImmediate(enemyData); // 테스트 적 데이터 정리
        Object.DestroyImmediate(deckData); // 테스트 덱 데이터 정리
        Object.DestroyImmediate(levelConfig); // 테스트 레벨 설정 정리
        yield return new ExitPlayMode(); // 실제 전투 화면 플레이 종료
    } // 실제 카드 더미 검사 종료
    private static void AssertPileLayout(RectTransform hand, RectTransform draw, RectTransform discard, RectTransform orb, RectTransform layout) // 카드 더미와 손패 경계 검사
    { // 카드 더미 경계 검사 시작
        Vector3[] corners = new Vector3[4]; // 실제 화면 꼭짓점 계산 배열
        draw.GetWorldCorners(corners); // 왼쪽 뽑기 더미 꼭짓점 조회
        float drawRight = hand.InverseTransformPoint(corners[2]).x; // 뽑기 더미 오른쪽 경계
        float drawCenter = hand.InverseTransformPoint(draw.TransformPoint(draw.rect.center)).y; // 뽑기 더미 세로 중앙 위치
        float drawTop = hand.InverseTransformPoint(corners[1]).y; // 뽑기 더미 위쪽 경계
        discard.GetWorldCorners(corners); // 오른쪽 버리기 더미 꼭짓점 조회
        float discardLeft = hand.InverseTransformPoint(corners[0]).x; // 버리기 더미 왼쪽 경계
        float discardCenter = hand.InverseTransformPoint(discard.TransformPoint(discard.rect.center)).y; // 묘지 더미 세로 중앙 위치
        orb.GetWorldCorners(corners); // 손패 왼쪽 위 행동력 구체 꼭짓점 조회
        float orbBottom = hand.InverseTransformPoint(corners[0]).y; // 행동력 구체 아래쪽 경계
        layout.GetWorldCorners(corners); // 실제 가운데 손패 영역 꼭짓점 조회
        float layoutLeft = hand.InverseTransformPoint(corners[0]).x; // 손패 영역 왼쪽 경계
        float layoutRight = hand.InverseTransformPoint(corners[2]).x; // 손패 영역 오른쪽 경계
        Assert.LessOrEqual(drawRight, layoutLeft); // 뽑기 더미와 손패 가로 겹침 방지 확인
        Assert.GreaterOrEqual(discardLeft, layoutRight); // 버리기 더미와 손패 가로 겹침 방지 확인
        Assert.AreEqual(drawCenter, discardCenter, 0.01f); // 양쪽 카드 더미의 같은 높이 확인
        Assert.GreaterOrEqual(orbBottom, drawTop); // 행동력 구체와 뽑기 더미 세로 겹침 방지 확인
    } // 카드 더미 경계 검사 종료
} // 카드 더미 화면 검사 종료
