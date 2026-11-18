using System.Collections; // 실제 전투 화면 대기 사용
using System.Collections.Generic; // 테스트 카드 목록 사용
using NUnit.Framework; // 카드 이동 결과 검증 사용
using UnityEngine; // 전투 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 이미지 사용
public sealed class BattleCardMovementTests // 카드 더미 이동 연출 검사
{ // 카드 이동 검사 시작
    [UnityTest] // 실제 손패 이동 검사 지정
    public IEnumerator DrawUseAndEndTurnAnimateWithoutChangingDeckRules() // 뽑기와 버리기 실제 이동 확인
    { // 카드 이동 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 플레이 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData ownerData = roster.Characters[0]; // 실제 소유자 데이터 조회
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 실제 아군 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 테스트 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/Attack"); // 실제 카드 원본 조회
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 테스트 덱 카드 목록 생성
        for (int index = 0; index < 8; index++) // 카드 여덟 장 생성 반복
        { // 카드 목록 추가 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 실제 소유자의 카드 추가
        } // 카드 목록 추가 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, entries, new[] { owner }, 20, 1); // 실제 전투 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 실제 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { owner }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 실제 턴 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        GameObject canvas = new GameObject("MovementCanvas", typeof(RectTransform), typeof(Canvas)); // 손패 테스트 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 캔버스 유지
        GameObject root = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        root.transform.SetParent(canvas.transform, false); // 캔버스에 손패 연결
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 350f); // 실제 손패 크기 설정
        BattleHandView hand = root.GetComponent<BattleHandView>(); // 실제 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 전투 덱 연결 확인
        Canvas.ForceUpdateCanvases(); // 카드 위치 갱신
        Assert.AreEqual(2, deck.DrawCards(2)); // 실제 카드 두 장 뽑기 확인
        BattleCardFlightView[] drawFlights = root.GetComponentsInChildren<BattleCardFlightView>(); // 실제 뽑기 연출 조회
        Assert.AreEqual(2, drawFlights.Length); // 뽑은 카드별 연출 수 확인
        foreach (BattleCardFlightView flight in drawFlights) // 뽑기 연출 순회
        { // 뽑기 방향 검사 시작
            Assert.IsTrue(flight.IsDrawing); // 뽑기 방향 구분 확인
            Assert.Less(flight.StartPosition.x, flight.EndPosition.x); // 왼쪽 더미에서 손패 방향 확인
        } // 뽑기 방향 검사 종료
        yield return new WaitForSecondsRealtime(0.6f); // 뽑기 연출 종료 대기
        Assert.AreEqual(0, root.GetComponentsInChildren<BattleCardFlightView>().Length); // 완료한 뽑기 연출 정리 확인
        Assert.IsTrue(deck.DiscardCard(deck.Hand[0])); // 실제 사용 카드 이동 확인
        BattleCardFlightView[] useFlights = root.GetComponentsInChildren<BattleCardFlightView>(); // 사용 카드 연출 조회
        Assert.AreEqual(1, useFlights.Length); // 사용 카드 연출 수 확인
        Assert.IsFalse(useFlights[0].IsDrawing); // 버리기 방향 구분 확인
        Assert.Less(useFlights[0].StartPosition.x, useFlights[0].EndPosition.x); // 손패에서 오른쪽 묘지 방향 확인
        yield return new WaitForSecondsRealtime(0.6f); // 사용 카드 연출 종료 대기
        Assert.IsTrue(turns.EndPlayerTurn()); // 실제 플레이어 턴 종료 확인
        BattleCardFlightView[] endFlights = root.GetComponentsInChildren<BattleCardFlightView>(); // 턴 종료 버리기 연출 조회
        Assert.AreEqual(1, endFlights.Length); // 남은 카드의 버리기 연출 확인
        Assert.IsFalse(endFlights[0].IsDrawing); // 턴 종료 버리기 방향 확인
        Assert.AreEqual(0, deck.HandCount); // 턴 종료 후 실제 손패 비우기 확인
        Assert.AreEqual(2, deck.DiscardPileCount); // 실제 버린 카드 수 유지 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 수와 중복 상태 검증
        hand.Unbind(); // 실제 덱 연결 해제
        turns.Dispose(); // 실제 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 캔버스 제거
        Object.DestroyImmediate(enemyData); // 임시 적 데이터 제거
        Object.DestroyImmediate(deckData); // 임시 덱 데이터 제거
        yield return new ExitPlayMode(); // 실제 전투 플레이 종료
    } // 카드 이동 검사 종료
} // 카드 이동 검사 종료
