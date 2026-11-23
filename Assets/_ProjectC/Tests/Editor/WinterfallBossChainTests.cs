using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 카드 목록 사용
using System.Reflection; // 실제 카드 충돌 처리 호출 사용
using NUnit.Framework; // 연속 공격 취소 검증 사용
using UnityEngine; // 전투 화면과 원본 생성 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 생성 사용
public sealed class WinterfallBossChainTests // 보스 전환 시 남은 타격 검사
{ // 남은 타격 검사 시작
    [UnityTest] // 실제 태양 카드와 전환 검사 지정
    public IEnumerator SunChain_StopsSecondHitWhenBossChangesPhase() // 첫 타격에 보스 전환 시 두 번째 타격 취소 확인
    { // 태양 연속 타격 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterData sunSource = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[19]; // 실제 태양 캐릭터 조회
        CardData chain = Resources.Load<CardData>("StarterCards/SunChain"); // 실제 태양 연속 공격 카드 조회
        EnemyData bossSource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 보스 원본 생성
        EnemyData otherSource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 동료 적 원본 생성
        DeckData deckSource = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime sun = BattleUnitRuntime.CreateAlly(sunSource); // 태양 전투 유닛 생성
        BattleUnitRuntime boss = BattleUnitRuntime.CreateEnemy(bossSource); // 보스 전투 유닛 생성
        BattleUnitRuntime other = BattleUnitRuntime.CreateEnemy(otherSource); // 다른 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { sun }; // 아군 목록 준비
        BattleUnitRuntime[] enemies = { boss, other }; // 적 목록 준비
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckSource, new List<IDeckCardEntry> { new RunDeckCardEntry(chain, sunSource) }, allies, 5, 1); // 태양 카드 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Boss); // 보스 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        WinterfallBossPhaseRuntime phase = new WinterfallBossPhaseRuntime(boss, turns, actions, 0); // 보스 페이즈 관리자 생성
        Assert.IsTrue(turns.StartBattle(1)); // 실제 카드 한 장으로 전투 시작
        CardInstance card = deck.Hand[0]; // 손패의 실제 태양 카드 조회
        boss.TakeDamage(boss.MaxHealth / 2 - 1, BattleDamageType.None, sun); // 전환 경계 바로 위까지 체력 감소
        Assert.IsFalse(phase.HasPendingTransition); // 경계 이전 전환 방지 확인
        GameObject canvas = new GameObject("BossChainCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 화면 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 화면을 캔버스에 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 턴 관리자 연결
        BattleUnitView sunView = CreateView(canvas.transform, sun); // 태양 화면 생성
        BattleUnitView bossView = CreateView(canvas.transform, boss); // 보스 화면 생성
        BattleUnitView otherView = CreateView(canvas.transform, other); // 다른 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { sunView }, new[] { bossView, otherView }, actions, phase); // 실제 보스 전환 연결 카드 처리기 생성
        typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { card, new[] { boss } }); // 태양 카드의 실제 첫 타격 처리
        Assert.IsTrue(phase.HasPendingTransition); // 보스 페이즈 전환 예약 확인
        Assert.AreEqual(other.MaxHealth, other.CurrentHealth); // 다른 적의 두 번째 타격 취소 확인
        typeof(BattleCardActionController).GetMethod("HandleActionSequenceCompleted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null); // 실제 카드 행동 완료 처리
        Assert.AreEqual(2, turns.CurrentRound); // 보스 전환 후 다음 라운드 확인
        Assert.IsFalse(phase.HasPendingTransition); // 페이즈 전환 예약 해제 확인
        controller.Dispose(); // 카드 처리기 정리
        phase.Dispose(); // 보스 페이즈 관리자 정리
        hand.Unbind(); // 손패 화면 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 임시 전투 화면 정리
        Object.DestroyImmediate(bossSource); // 임시 보스 원본 정리
        Object.DestroyImmediate(otherSource); // 임시 다른 적 원본 정리
        Object.DestroyImmediate(deckSource); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 태양 연속 타격 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject item = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        item.transform.SetParent(parent, false); // 캔버스 자식으로 연결
        BattleUnitView view = item.GetComponent<BattleUnitView>(); // 유닛 화면 컴포넌트 조회
        view.Bind(unit); // 전투 유닛 연결
        return view; // 생성한 유닛 화면 반환
    } // 유닛 화면 생성 종료
} // 남은 타격 검사 종료
