using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 자동 검증 사용
using TMPro; // 행동력 텍스트 사용
using UnityEngine; // 게임 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 구체 이미지 검사 사용
public sealed class BattleActionPointOrbTests // 행동력 구체 검사
{ // 검사 시작
    [UnityTest] // 실제 화면 검사 지정
    public IEnumerator OrbTracksActionPointsAndReservesCardSpace() // 행동력 변화와 화면 배치 확인
    { // 구체 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        GameObject canvas = new GameObject("OrbCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 시 캔버스 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 부모 연결
        handObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 350f); // 손패 화면 크기 설정
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Transform orb = handObject.transform.Find("ActionPointOrb"); // 손패 패널 왼쪽 위 행동력 구체 조회
        Assert.IsNotNull(orb); // 행동력 구체 존재 확인
        Assert.AreEqual(new Vector2(0f, 1f), (orb as RectTransform).anchorMin); // 구체의 손패 왼쪽 위 앵커 확인
        Assert.AreEqual(new Vector2(0f, 1f), (orb as RectTransform).anchorMax); // 구체의 손패 왼쪽 위 기준 확인
        Image fill = orb.Find("APFill").GetComponent<Image>(); // 구체 충전 이미지 조회
        Assert.IsNotNull(orb.Find("APFrame").GetComponent<Image>().sprite); // 구체 테두리 이미지 확인
        Assert.IsFalse(fill.raycastTarget); // 구체의 입력 차단 방지 확인
        Assert.AreEqual(Image.Type.Filled, fill.type); // 행동력 충전 방식 확인
        TMP_Text label = orb.Find("ActionPointText").GetComponent<TMP_Text>(); // 구체 중앙 텍스트 조회
        Assert.AreEqual(TextAlignmentOptions.Center, label.alignment); // 구체 글자 중앙 정렬 확인
        RectTransform layout = handObject.transform.Find("HandLayoutRoot").GetComponent<RectTransform>(); // 손패 배치 영역 조회
        Assert.GreaterOrEqual(layout.offsetMin.x, 150f); // 카드와 구체 분리 확인
        Assert.Greater(handObject.transform.Find("EscapeButton").GetComponent<RectTransform>().anchorMin.x, 0.7f); // 도주 버튼 오른쪽 배치 확인
        Assert.Greater(handObject.transform.Find("EndTurnButton").GetComponent<RectTransform>().anchorMin.x, 0.8f); // 턴 종료 오른쪽 배치 확인
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 아군 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 정보 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 정보 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new IDeckCardEntry[0], new[] { ally }, 20, 1); // 빈 전투 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 실제 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { ally }, new[] { BattleUnitRuntime.CreateEnemy(enemyData) }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 행동력 연결 확인
        Assert.AreEqual("AP 6/6", label.text); // 최대 행동력 글자 확인
        Assert.AreEqual(1f, fill.fillAmount, 0.001f); // 최대 행동력 충전 확인
        Assert.IsTrue(points.Spend(3)); // 행동력 절반 사용 확인
        Assert.AreEqual("AP 3/6", label.text); // 행동력 감소 글자 확인
        Assert.AreEqual(0.5f, fill.fillAmount, 0.001f); // 행동력 감소 구체 확인
        Assert.IsTrue(points.Restore()); // 행동력 회복 확인
        Assert.AreEqual(1f, fill.fillAmount, 0.001f); // 행동력 회복 구체 확인
        Assert.IsTrue(points.Spend(6)); // 행동력 전체 사용 확인
        Assert.AreEqual(0f, fill.fillAmount, 0.001f); // 행동력 소진 구체 확인
        hand.Unbind(); // 손패 연결 해제
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 임시 적 정보 정리
        Object.DestroyImmediate(deckData); // 임시 덱 정보 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 구체 검사 종료
} // 검사 종료
