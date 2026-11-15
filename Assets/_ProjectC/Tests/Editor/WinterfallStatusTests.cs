using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 유닛과 카드 목록 사용
using System.Reflection; // 실제 카드 충돌 처리 사용
using NUnit.Framework; // 적 상태 이상 규칙 검증 사용
using UnityEngine; // 임시 전투 데이터 생성 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 전투 화면 사용
public sealed class WinterfallStatusTests // 윈터폴 상태 이상 초기 규칙 검사
{ // 상태 이상 검사 시작
    [UnityTest] // 태양 연속 공격의 독립 명중 검사 지정
    public IEnumerator SunChain_SecondAttackStillRollsAfterFirstMisses() // 첫 빗나감 이후 다른 적의 독립 공격 확인
    { // 태양 연속 명중 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterData sunData = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[19]; // 실제 태양 캐릭터 조회
        CardData sunCard = Resources.Load<CardData>("StarterCards/SunChain"); // 실제 태양 전용 카드 조회
        EnemyData firstData = ScriptableObject.CreateInstance<EnemyData>(); // 첫 적 원본 생성
        EnemyData secondData = ScriptableObject.CreateInstance<EnemyData>(); // 두 번째 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime sun = BattleUnitRuntime.CreateAlly(sunData); // 태양 전투 유닛 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(firstData); // 첫 적 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(secondData); // 두 번째 적 생성
        BattleUnitRuntime[] allies = { sun }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { first, second }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry> { new RunDeckCardEntry(sunCard, sunData) }, allies, 5, 1); // 태양 카드 전투 덱 생성
        deck.DrawCards(1); // 태양 카드 드로우
        CardInstance card = deck.Hand[0]; // 실제 전투 카드 조회
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 아군 턴 시작 확인
        sun.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1); // 태양의 실명 적용
        GameObject canvas = new GameObject("SunBlindCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 덱 연결 확인
        BattleUnitView sunView = CreateView(canvas.transform, sun); // 태양 화면 생성
        BattleUnitView firstView = CreateView(canvas.transform, first); // 첫 적 화면 생성
        BattleUnitView secondView = CreateView(canvas.transform, second); // 두 번째 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { sunView }, new[] { firstView, secondView }, actions); // 실제 카드 처리기 생성
        Random.State previousRandom = Random.state; // 기존 전역 난수 상태 저장
        int chosenSeed = -1; // 첫 공격 실패와 두 번째 성공 난수 초기화
        for (int seed = 1; seed < 10000; seed++) // 독립 명중 시나리오 난수 탐색
        { // 시나리오 난수 검사 시작
            Random.InitState(seed); // 후보 난수 상태 설정
            int firstRoll = Random.Range(0, 100); // 첫 공격 명중 난수 조회
            Random.Range(0, 1); // 다른 적 하나를 고르는 난수 소비
            int secondRoll = Random.Range(0, 100); // 두 번째 공격 명중 난수 조회
            if (firstRoll >= 75 && secondRoll < 75) // 첫 실패와 두 번째 성공 확인
            { // 시나리오 선택 시작
                chosenSeed = seed; // 유효한 난수 상태 저장
                break; // 후보 검색 종료
            } // 시나리오 선택 종료
        } // 시나리오 난수 검사 종료
        Assert.Greater(chosenSeed, 0); // 독립 명중 시나리오 존재 확인
        Random.InitState(chosenSeed); // 실제 공격에 동일 난수 적용
        typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { card, new[] { first } }); // 실제 태양 카드 충돌 처리
        Random.state = previousRandom; // 전역 난수 상태 복원
        Assert.AreEqual(first.MaxHealth, first.CurrentHealth); // 첫 공격 빗나감 확인
        Assert.AreEqual(second.MaxHealth - 6, second.CurrentHealth); // 두 번째 공격의 독립 명중 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 전투 화면 정리
        Object.DestroyImmediate(firstData); // 첫 적 원본 정리
        Object.DestroyImmediate(secondData); // 두 번째 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 태양 연속 명중 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 실명 공격과 공격 후 상태 적용 검사 지정
    public void EnemyAttack_MissDoesNotApplyDamageOrBurn() // 빗나감과 적중 시 화상 적용 확인
    { // 공격과 화상 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 생성
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1); // 적에게 실명 적용
        int health = ally.CurrentHealth; // 이전 아군 체력 기록
        BattleEnemyAction miss = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.Burn, 2, 1, EnemyTargetRule.FirstLiving, "잿발톱", 1, 1, 5, 0, 100, 2, () => 99); // 반드시 빗나가는 화상 공격 생성
        BattleEnemyActionResult missed = miss.Execute(); // 실명 적의 공격 실행
        Assert.IsTrue(missed.WasMissed); // 공격 빗나감 확인
        Assert.AreEqual(health, ally.CurrentHealth); // 빗나간 공격의 피해 없음 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 빗나간 공격의 화상 없음 확인
        enemy.RemoveStatusEffect(BattleStatusEffectType.Blind); // 적 실명 제거
        BattleEnemyAction hit = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.Burn, 2, 1, EnemyTargetRule.FirstLiving, "잿발톱", 1, 1, 5, 0, 100, 2, () => 0); // 반드시 적중하는 화상 공격 생성
        BattleEnemyActionResult applied = hit.Execute(); // 적의 화상 공격 실행
        Assert.IsFalse(applied.WasMissed); // 공격 적중 확인
        Assert.Less(ally.CurrentHealth, health); // 적중한 공격 피해 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, applied.StatusApplyResult); // 공격 후 화상 적용 결과 확인
        Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 초기 화상 수치 둘 확인
        Object.DestroyImmediate(allyData); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 공격과 화상 검사 종료
    [Test] // 화상 턴 종료 피해와 감소 검사 지정
    public void Burn_TicksTwoThenOneAtOwnTurnEnd() // 사용자 확인 화상 피해와 소멸 확인
    { // 화상 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(data); // 아군 전투 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 처리 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 첫 아군 턴 시작 확인
        int health = ally.CurrentHealth; // 화상 전 체력 기록
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1)); // 화상 수치 둘과 지속 두 턴 적용
        Assert.IsTrue(turns.EndPlayerTurn()); // 첫 아군 턴 종료
        Assert.AreEqual(health - 2, ally.CurrentHealth); // 첫 종료 시 화상 피해 둘 확인
        Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 다음 화상 수치 하나 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 종료와 다음 아군 턴 시작
        Assert.AreEqual(health - 2, ally.CurrentHealth); // 턴 시작 시 화상 피해 없음 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 두 번째 아군 턴 종료
        Assert.AreEqual(health - 3, ally.CurrentHealth); // 두 번째 종료 시 화상 피해 하나 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 화상 소멸 확인
        statuses.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(data); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 화상 검사 종료
    [Test] // 실명 명중 경계와 턴 지속 검사 지정
    public void Blind_ReducesAccuracyAndExpiresAfterOwnTurn() // 명중 경계와 턴 시작 유지 확인
    { // 실명 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(data); // 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 첫 아군 턴 시작 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴으로 이동
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1)); // 실명 명중률 감소 적용
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 실명 대상의 다음 턴 시작
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 턴 시작 후 실명 유지 확인
        Assert.IsTrue(BattleHitChance.Hits(ally, 74)); // 실명 상태의 명중 경계 확인
        Assert.IsFalse(BattleHitChance.Hits(ally, 75)); // 실명 상태의 빗나감 경계 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 실명 대상 턴 종료
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 턴 종료 후 실명 제거 확인
        Assert.IsTrue(BattleHitChance.Hits(ally, 99)); // 실명 해제 후 기본 명중 확인
        statuses.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(data); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 실명 검사 종료
    [Test] // 화상 턴 종료 전멸 처리 검사 지정
    public void Burn_DefeatAtTurnEndDoesNotStartEnemyTurn() // 화상으로 전멸한 전투의 단계 보존 확인
    { // 턴 종료 전멸 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(data); // 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 상태 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 아군 턴 시작 확인
        ally.ApplyPersistentHealth(1); // 전멸 직전 체력 설정
        ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 치명적인 화상 적용
        Assert.IsTrue(turns.EndPlayerTurn()); // 턴 종료 처리 확인
        Assert.AreEqual(BattleResult.Defeat, turns.Result); // 화상 전멸 패배 확인
        Assert.AreEqual(BattleTurnPhase.Defeat, turns.CurrentPhase); // 패배 뒤 적 턴으로 넘어가지 않음 확인
        statuses.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(data); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 턴 종료 전멸 검사 종료
} // 상태 이상 검사 종료
