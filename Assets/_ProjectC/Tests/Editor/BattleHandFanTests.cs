using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 손패 카드 목록 사용
using NUnit.Framework; // 손패 배치 검증 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 손패 이미지 사용
public sealed class BattleHandFanTests // 겹친 손패와 선택 연출 검사
{ // 손패 연출 검사 시작
    [UnityTest] // 실제 손패 배치 검사 지정
    public IEnumerator CardsFanTogetherAndSpreadAroundHoveredCard() // 부채꼴과 이웃 카드 간격 확인
    { // 손패 연출 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData ownerData = roster.Characters[0]; // 카드 소유자 원본 조회
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 카드 소유자 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/Attack"); // 실제 공격 카드 조회
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 손패 카드 항목 생성
        for (int index = 0; index < 8; index++) // 여덟 장 카드 순회
        { // 손패 카드 추가 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 소유자 포함 카드 항목 추가
        } // 손패 카드 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, entries, new[] { owner }, 20, 1); // 실제 손패 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { owner }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 실제 전투 턴 생성
        Assert.AreEqual(8, deck.DrawCards(8)); // 여덟 장 카드 뽑기 확인
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("FanCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 캔버스 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 전투 캔버스에 손패 연결
        handObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 350f); // 실제와 비슷한 손패 크기 설정
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 덱과 턴 연결 확인
        Canvas.ForceUpdateCanvases(); // 손패 배치 갱신
        yield return null; // 실제 카드 배치 대기
        Transform layout = handObject.transform.Find("HandLayoutRoot"); // 카드 배치 영역 조회
        Transform hits = handObject.transform.Find("HandHitRoot"); // 독립 카드 입력 영역 조회
        RectTransform first = layout.GetChild(0) as RectTransform; // 왼쪽 카드 조회
        RectTransform left = layout.GetChild(2) as RectTransform; // 선택 카드 왼쪽 이웃 조회
        RectTransform chosen = layout.GetChild(3) as RectTransform; // 가운데 선택 카드 조회
        RectTransform right = layout.GetChild(4) as RectTransform; // 선택 카드 오른쪽 이웃 조회
        RectTransform last = layout.GetChild(7) as RectTransform; // 오른쪽 카드 조회
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, first.localEulerAngles.z)), 0.01f); // 왼쪽 카드 부채꼴 회전 확인
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, last.localEulerAngles.z)), 0.01f); // 오른쪽 카드 부채꼴 회전 확인
        Assert.Less(last.anchoredPosition.x - first.anchoredPosition.x, 7f * BattleCardView.CardWidth); // 카드가 서로 겹쳐 모인 배치 확인
        Vector2 leftBefore = left.anchoredPosition; // 왼쪽 이웃 기본 위치 저장
        Vector2 rightBefore = right.anchoredPosition; // 오른쪽 이웃 기본 위치 저장
        Vector2 chosenBefore = chosen.anchoredPosition; // 선택 카드 기본 위치 저장
        Vector2 hitBefore = (hits.GetChild(3) as RectTransform).anchoredPosition; // 선택 카드 입력 위치 저장
        BattleCardHitRegion hit = hits.GetChild(3).GetComponent<BattleCardHitRegion>(); // 선택 카드 입력 영역 조회
        hit.OnPointerEnter(null); // 선택 카드 마우스 진입 처리
        Assert.Less(left.anchoredPosition.x, leftBefore.x); // 왼쪽 카드가 바깥쪽으로 이동 확인
        Assert.Greater(right.anchoredPosition.x, rightBefore.x); // 오른쪽 카드가 바깥쪽으로 이동 확인
        Assert.Greater(chosen.anchoredPosition.y, chosenBefore.y); // 선택 카드 위쪽 이동 확인
        Assert.AreEqual(layout.childCount - 1, chosen.GetSiblingIndex()); // 선택 카드 최상단 표시 확인
        Assert.AreEqual(hitBefore, (hits.GetChild(3) as RectTransform).anchoredPosition); // 마우스 진입 중 입력 영역 안정성 확인
        hand.SetCardDragging(hit.CardView, true); // 실제 드래그 중인 카드 상태 설정
        hit.OnPointerExit(null); // 드래그 시작 시 카드 마우스 이탈 처리
        Assert.Less(left.anchoredPosition.x, leftBefore.x); // 드래그 중 왼쪽 간격 유지 확인
        Assert.Greater(right.anchoredPosition.x, rightBefore.x); // 드래그 중 오른쪽 간격 유지 확인
        hand.SetCardDragging(hit.CardView, false); // 카드 드래그 취소 처리
        Assert.AreEqual(leftBefore, left.anchoredPosition); // 왼쪽 카드 원위치 복원 확인
        Assert.AreEqual(rightBefore, right.anchoredPosition); // 오른쪽 카드 원위치 복원 확인
        Assert.AreEqual(chosenBefore, chosen.anchoredPosition); // 선택 카드 원위치 복원 확인
        hand.Unbind(); // 손패 연결 해제
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
        Object.DestroyImmediate(deckData); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 손패 연출 검사 종료
} // 검사 종료
