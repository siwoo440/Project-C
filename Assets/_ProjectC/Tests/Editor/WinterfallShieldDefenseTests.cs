using System.Linq; // 공격 종류별 예정 행동 조회
using NUnit.Framework; // 런타임 전투 결과 확인
using UnityEngine; // 임시 전투 원본 생성

public sealed class WinterfallShieldDefenseTests // 도발과 직접 공격 경감 검사
{ // 전투 검사 시작
    private CharacterData[] characters; // 세 아군의 서로 다른 원본
    private DeckData deckSource; // 빈 전투 덱 원본
    private EnemyData attackSource; // 직접 공격 적 원본
    private EnemyData statusSource; // 상태 적용 적 원본
    private BattleUnitRuntime[] allies; // 원래 대상과 보호자와 도발자 목록
    private BattleUnitRuntime[] enemies; // 직접 공격 둘과 상태 행동 적 목록
    private BattleTurnRuntime turns; // 실제 적 턴 정리 관리자
    private BattleEnemyActionRuntime actions; // 공격 예고와 실행 관리자

    [SetUp] // 각 검사의 독립 전투 준비
    public void SetUp() // 아군 세 명과 적 세 명 구성
    { // 전투 구성 시작
        characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 서로 다른 카드 소유자 원본 생성
        allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 원래 대상부터 순서대로 아군 구성
        attackSource = ScriptableObject.CreateInstance<EnemyData>(); // 공격 적 원본 생성
        statusSource = ScriptableObject.CreateInstance<EnemyData>(); // 상태 적 원본 생성
        JsonUtility.FromJsonOverwrite("{\"basicAttackPower\":20}", attackSource); // 직접 공격의 고정 원본 피해 설정
        JsonUtility.FromJsonOverwrite($"{{\"actionType\":{(int)EnemyActionType.ApplyStatusEffect},\"statusEffectType\":{(int)BattleStatusEffectType.Poison}}}", statusSource); // 같은 대상을 향하는 독립 상태 행동 설정
        enemies = new[] { BattleUnitRuntime.CreateEnemy(attackSource), BattleUnitRuntime.CreateEnemy(attackSource), BattleUnitRuntime.CreateEnemy(statusSource) }; // 직접 공격자와 상태 행동자 배치
        deckSource = DeckData.CreateRuntime(new DeckCardEntry[0]); // 카드 없이 턴 경계에 집중한 전투 덱
        var deck = BattleDeckRuntime.Create(deckSource, allies); // 실제 아군 소유자와 빈 덱 연결
        turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, BattleType.Normal); // 공용 행동력과 진영 턴 구성
        actions = new BattleEnemyActionRuntime(enemies, allies, turns); // 적 행동과 턴 종료 연결
        Assert.IsTrue(turns.StartBattle(0)); // 정상 아군 턴에서 전투 시작
    } // 전투 구성 종료

    [TearDown] // 각 검사의 자원 해제
    public void TearDown() // 전투 회차와 임시 원본 정리
    { // 전투 정리 시작
        actions?.Dispose(); // 적 행동 이벤트 연결 해제
        turns?.Dispose(); // 진영 턴 이벤트 연결 해제
        Object.DestroyImmediate(deckSource); // 임시 덱 원본 제거
        Object.DestroyImmediate(attackSource); // 임시 공격자 원본 제거
        Object.DestroyImmediate(statusSource); // 임시 상태 유닛 원본 제거
        foreach (var character in characters) // 아군 원본 순회
        { // 개별 아군 원본 정리 시작
            Object.DestroyImmediate(character); // 임시 아군 원본 제거
        } // 개별 아군 원본 정리 종료
    } // 전투 정리 종료

    [Test] // 도발의 현재와 이후 적 공격 및 수명 검사
    public void TauntAffectsOnlyAttacksAndExpiresAfterEnemyTurn() // 상태 행동 보존과 새 턴 정리 확인
    { // 도발 수명 검사 시작
        Assert.IsTrue(actions.TryShieldTaunt(allies[1])); // 예고 전 첫 도발자 지정
        actions.PrepareActions(); // 현재 도발을 포함한 예고 생성
        Assert.IsTrue(actions.TryShieldTaunt(allies[2])); // 두 번째 도발자로 교체
        Assert.IsTrue(actions.PlannedActions.Where(action => action.ActionType == EnemyActionType.Attack).All(action => action.Target == allies[2])); // 기존 공격의 도발 대상 교체 확인
        Assert.AreSame(allies[0], actions.FindAction(enemies[2]).Target); // 상태 행동의 원래 대상 유지 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 현재 도발을 첫 적 턴까지 유지
        actions.PrepareActions(); // 같은 적 턴에 다시 준비한 예고 생성
        Assert.IsTrue(actions.PlannedActions.Where(action => action.ActionType == EnemyActionType.Attack).All(action => action.Target == allies[2])); // 새 공격에도 활성 도발 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 실제 적 턴 종료 이벤트 발생
        Assert.IsTrue(actions.PlannedActions.All(action => action.Target == allies[0])); // 도발 해제 뒤 기존 대상 복원 확인
        actions.PrepareActions(); // 다음 라운드 적 공격 예고 생성
        Assert.IsTrue(actions.PlannedActions.All(action => action.Target == allies[0])); // 다음 적 턴 도발 미전파 확인
    } // 도발 수명 검사 종료

    [Test] // 몸으로 막기와 도발의 동시 사용 검사
    public void TauntedHitCanBeGuardedAndTaunterDeathRestoresTarget() // 첫 명중 보호와 도발자 사망 확인
    { // 겹친 효과 검사 시작
        Assert.IsTrue(actions.TryShieldTaunt(allies[1])); // 아군에게 적 공격 집중
        Assert.IsTrue(actions.TryBodyGuard(allies[2], allies[1])); // 도발자의 다음 공격만 대신 받을 예약
        actions.PrepareActions(); // 집중된 공격과 첫 보호 예고 생성
        var attacks = actions.PlannedActions.Where(action => action.ActionType == EnemyActionType.Attack).ToArray(); // 행동 순서대로 두 공격 조회
        Assert.AreSame(allies[2], attacks[0].Target); // 첫 공격의 보호자 전환 확인
        Assert.AreSame(allies[1], attacks[1].Target); // 다음 공격은 도발 대상 유지 확인
        actions.ExecuteAction(attacks[0]); // 보호자의 실제 첫 피격
        Assert.AreEqual(allies[2].MaxHealth - 20, allies[2].CurrentHealth); // 보호자의 실제 체력 피해 확인
        Assert.AreEqual(allies[1].MaxHealth, allies[1].CurrentHealth); // 도발자의 첫 피해 보존 확인
        allies[1].TakeDamage(1000, BattleDamageType.None); // 남은 공격 전 도발자 사망 처리
        Assert.AreSame(allies[0], attacks[1].Target); // 생존한 원래 대상으로 예고 복원 확인
        actions.ExecuteAction(attacks[1]); // 복원된 대상의 피격 실행
        Assert.AreEqual(allies[0].MaxHealth - 20, allies[0].CurrentHealth); // 원래 대상에 남은 공격 피해 확인
    } // 겹친 효과 검사 종료

    [Test] // 위험 차단의 단일 소모와 방패 반격 검사
    public void DefenseStatusesUseTheFirstActualBlockedHit() // 비율 경감과 두 번째 예고 및 반격 확인
    { // 직접 피해 방어 검사 시작
        var defender = allies[0]; // 공통 공격의 원래 대상 조회
        defender.GainBlock(3); // 첫 공격에서 쓸 방어도 부여
        defender.ApplyStatusEffect(BattleStatusEffectType.GuardianCircle, 10, 2, 1); // 직접 피해 두 턴 경감 준비
        defender.ApplyStatusEffect(BattleStatusEffectType.DangerInterception, 70, 1, 1); // 다음 직접 피해 한 번 경감 준비
        defender.ApplyStatusEffect(BattleStatusEffectType.ShieldCounter, 8, 1, 1); // 막은 공격에 고정 반격 준비
        actions.PrepareActions(); // 두 직접 공격 예고 생성
        var attacks = actions.PlannedActions.Where(action => action.ActionType == EnemyActionType.Attack).ToArray(); // 두 직접 공격의 순서 조회
        attacks[0].Actor.GainBlock(20); // 반격이 무시할 공격자 방어도 부여
        Assert.AreEqual(2, attacks[0].PreviewDamage().AppliedDamage); // 두 경감 비율과 방어도 뒤의 첫 체력 피해 확인
        Assert.AreEqual(18, attacks[1].PreviewDamage().AppliedDamage); // 둘째 예고에 수호의 원만 남는지 확인
        Assert.IsNotNull(defender.GetStatusEffect(BattleStatusEffectType.DangerInterception)); // 예고는 위험 차단을 소모하지 않음 확인
        Assert.AreEqual(2, actions.ExecuteAction(attacks[0]).DamageResult.AppliedDamage); // 실제 피격에 위험 차단과 방어도 적용 확인
        Assert.IsNull(defender.GetStatusEffect(BattleStatusEffectType.DangerInterception)); // 실제 첫 양수 명중에만 소모 확인
        Assert.AreEqual(attacks[0].Actor.MaxHealth - 8, attacks[0].Actor.CurrentHealth); // 방어도를 우회하는 고정 반격 확인
        Assert.AreEqual(20, attacks[0].Actor.CurrentBlock); // 공격자의 방어도 보존 확인
        Assert.AreEqual(18, actions.ExecuteAction(attacks[1]).DamageResult.AppliedDamage); // 다음 직접 공격의 수호 경감만 적용 확인
        Assert.AreEqual(attacks[1].Actor.MaxHealth, attacks[1].Actor.CurrentHealth); // 방어도가 흡수하지 않은 타격의 반격 없음 확인
    } // 직접 피해 방어 검사 종료
} // 전투 검사 종료
