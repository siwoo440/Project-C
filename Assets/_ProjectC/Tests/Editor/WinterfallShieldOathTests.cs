using System.Linq; // 예정 공격의 피격 대상 조회
using NUnit.Framework; // 방패 카드 전투 결과 검증
using UnityEngine; // 검사 카드와 적 원본 생성

public sealed class WinterfallShieldOathTests // 수호 맹세의 예고와 실제 피해 검사
{ // 수호 맹세 검사 시작
    private CharacterData protectedCharacter; // 보호할 아군 원본
    private CharacterData guardCharacter; // 처음 맹세를 적용할 아군 원본
    private CharacterData backupCharacter; // 교체 보호자 아군 원본
    private EnemyData enemy; // 검사 적 원본
    private DeckData source; // 검사 전투의 공용 덱 원본
    private BattleUnitRuntime protectedAlly; // 첫 공격 대상인 보호 아군
    private BattleUnitRuntime guard; // 처음 맹세를 적용하는 보호자
    private BattleUnitRuntime backup; // 교체 및 다른 보호 검사용 동료
    private BattleTurnRuntime turns; // 실제 진영 턴 경계
    private BattleEnemyActionRuntime actions; // 예정 및 실제 적 공격 처리

    [SetUp] // 실제 전투 변수 준비
    public void SetUp() // 아군 세 명과 적 두 명의 전투 생성
    { // 검사 전투 구성 시작
        protectedCharacter = ScriptableObject.CreateInstance<CharacterData>(); // 기본 체력의 선두 아군 원본
        guardCharacter = ScriptableObject.CreateInstance<CharacterData>(); // 기본 체력의 첫 보호자 원본
        backupCharacter = ScriptableObject.CreateInstance<CharacterData>(); // 기본 체력의 교체 보호자 원본
        enemy = ScriptableObject.CreateInstance<EnemyData>(); // 첫 생존 아군을 공격하는 적 원본
        source = ScriptableObject.CreateInstance<DeckData>(); // 드로우 없는 검사 덱 원본
        protectedAlly = BattleUnitRuntime.CreateAlly(protectedCharacter); // 첫 적 공격을 받는 선두 아군
        guard = BattleUnitRuntime.CreateAlly(guardCharacter); // 첫 수호 맹세의 보호자
        backup = BattleUnitRuntime.CreateAlly(backupCharacter); // 다시 맹세할 수 있는 두 번째 보호자
        var allies = new[] { protectedAlly, guard, backup }; // 선두 대상을 고정한 아군 배치
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy), BattleUnitRuntime.CreateEnemy(enemy) }; // 한 턴에 두 공격을 할 적 배치
        var deck = BattleDeckRuntime.Create(source, allies); // 실제 전투용 빈 덱 생성
        turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, BattleType.Boss); // 보스 전환과 일반 적 턴을 지원하는 전투
        actions = new BattleEnemyActionRuntime(enemies, allies, turns); // 실제 적 행동과 턴 경계 연결
        Assert.IsTrue(turns.StartBattle(0)); // 손패 없이 첫 아군 턴 시작 확인
    } // 검사 전투 구성 종료

    [TearDown] // 각 검사의 전투 자원 정리
    public void TearDown() // 유닛 연결과 검사 원본 해제
    { // 검사 자원 해제 시작
        actions?.Dispose(); // 적 행동과 유닛 이벤트 연결 해제
        turns?.Dispose(); // 아군과 적 턴 연결 해제
        Object.DestroyImmediate(source); // 공용 덱 원본 제거
        Object.DestroyImmediate(enemy); // 적 원본 제거
        Object.DestroyImmediate(protectedCharacter); // 선두 아군 원본 제거
        Object.DestroyImmediate(guardCharacter); // 첫 보호자 원본 제거
        Object.DestroyImmediate(backupCharacter); // 교체 보호자 원본 제거
    } // 검사 자원 해제 종료

    [Test] // 두 적 턴의 모든 단일 적 공격 확인
    public void OathCoversAllAttacksUntilTwoEnemyTurnsFinish() // 아군 턴과 보스 전환을 제외한 적 턴 지속 확인
    { // 수호 지속 확인 시작
        Assert.IsTrue(actions.TryShieldOath(guard, protectedAlly, 2)); // 두 적 턴 동안 선두 아군 보호 설정
        turns.QueueBossPhaseTransition(); // 적 행동을 건너뛰는 강제 라운드 예약
        Assert.IsTrue(turns.CompleteBossPhaseTransition()); // 적 턴 없이 다음 아군 턴 전환 확인
        for (int round = 0; round < 2; round++) // 실제 적 턴 두 번 진행
        { // 적 진영 행동 시작
            Assert.AreEqual(2, actions.PrepareActions()); // 적 턴마다 두 공격 예고 생성 확인
            CollectionAssert.AreEqual(new[] { guard, guard }, actions.PlannedActions.Select(action => action.Target)); // 모든 예정 공격의 보호자 예고 확인
            Assert.IsTrue(turns.EndPlayerTurn()); // 실제 적 턴 진입 확인
            actions.ExecuteAction(actions.PlannedActions[0]); // 첫 공격의 실제 보호 확인
            Assert.AreSame(guard, actions.PlannedActions[0].Target); // 첫 명중 뒤에도 두 번째 공격 보호 확인
            actions.ExecuteAction(actions.PlannedActions[0]); // 두 번째 공격의 실제 보호 확인
            Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 종료와 맹세 기간 감소 확인
            Assert.AreEqual(guard.MaxHealth - (round + 1) * 16, guard.CurrentHealth); // 두 공격의 체력 피해를 보호자가 받았는지 확인
            Assert.AreEqual(protectedAlly.MaxHealth, protectedAlly.CurrentHealth); // 보호 대상의 피해 없음 확인
        } // 적 진영 행동 종료
        actions.PrepareActions(); // 두 번째 적 턴 이후 새 적 예고 생성
        CollectionAssert.AreEqual(new[] { protectedAlly, protectedAlly }, actions.PlannedActions.Select(action => action.Target)); // 만료 후 모든 예정 공격의 원래 대상 확인
    } // 수호 지속 확인 종료

    [Test] // 같은 아군의 재적용과 상호 보호 검사
    public void ReapplyingOathReplacesGuardAndDoesNotRedirectRecursively() // 최근 보호자와 한 차례 전환 확인
    { // 교체 맹세 확인 시작
        Assert.IsTrue(actions.TryShieldOath(guard, protectedAlly, 1)); // 기존 한 턴의 맹세 설정
        Assert.IsTrue(actions.TryShieldOath(backup, protectedAlly, 2)); // 같은 대상의 보호자와 기간 교체
        Assert.IsTrue(actions.TryShieldOath(protectedAlly, backup, 2)); // 서로 대신 받는 맞은편 맹세 설정
        actions.PrepareActions(); // 실제 적 공격 두 차례 예고
        Assert.AreSame(backup, actions.PlannedActions[0].Target); // 연쇄 전환 없이 가장 최근 보호자 선택 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 실제 적 진영 행동 시작
        actions.ExecuteAction(actions.PlannedActions[0]); // 서로의 맹세에도 처음 보호자에게 한 번만 전환
        Assert.AreEqual(backup.MaxHealth - 8, backup.CurrentHealth); // 새 보호자의 실제 피해 확인
        Assert.AreEqual(guard.MaxHealth, guard.CurrentHealth); // 교체된 전 보호자의 피해 없음 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 교체한 기간의 첫 적 턴 완료
        actions.PrepareActions(); // 교체 후 두 번째 턴 예고 생성
        Assert.AreSame(backup, actions.PlannedActions[0].Target); // 새 두 턴 기간이 계속되는지 확인
    } // 교체 맹세 확인 종료

    [TestCase(true)] // 보호자가 사망하는 경우
    [TestCase(false)] // 보호 대상 아군이 사망하는 경우
    public void DeathOfEitherParticipantClearsOath(bool guardDies) // 생존 쌍의 유효성 및 사망 후 예고 확인
    { // 사망 정리 확인 시작
        Assert.IsFalse(actions.CanShieldOath(guard, guard)); // 자신을 보호 대상으로 고를 수 없는지 확인
        Assert.IsFalse(actions.CanShieldOath(actions.EnemyUnits[0], protectedAlly)); // 적을 보호자로 고를 수 없는지 확인
        Assert.IsFalse(actions.TryShieldOath(guard, protectedAlly, 0)); // 양수가 아닌 지속 기간 거부 확인
        Assert.IsTrue(actions.TryShieldOath(guard, protectedAlly, 2)); // 사망 전 유효한 두 아군의 맹세 등록
        actions.PrepareActions(); // 사망 전 보호 예고 준비
        Assert.AreSame(guard, actions.PlannedActions[0].Target); // 사망 전 보호자 예고 확인
        var casualty = guardDies ? guard : protectedAlly; // 사망할 맹세 참여자 선택
        casualty.TakeDamage(casualty.MaxHealth, BattleDamageType.Physical); // 참여자 사망 이벤트 발생
        Assert.IsFalse(actions.CanShieldOath(guard, protectedAlly)); // 사망자가 포함되는 새 맹세 거부 확인
        var survivor = guardDies ? protectedAlly : guard; // 사망 후 첫 생존 공격 대상
        CollectionAssert.AreEqual(new[] { survivor, survivor }, actions.PlannedActions.Select(action => action.Target)); // 사망자가 없는 예정 대상 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 남은 대상의 적 진영 행동 시작
        actions.ExecuteAction(actions.PlannedActions[0]); // 사망한 맹세 연결 없이 실제 공격 실행
        Assert.AreEqual(survivor.MaxHealth - 8, survivor.CurrentHealth); // 재선택된 살아 있는 대상에게 피해 확인
    } // 사망 정리 확인 종료

    [Test] // 도발 우선 대상과 한 차례 보호 공존 검사
    public void TauntChangesTheOathTargetAndBodyGuardRemainsArmedUntilOathExpires() // 도발 순서와 기존 몸으로 막기 보존 확인
    { // 보호 우선순위 확인 시작
        Assert.IsTrue(actions.TryBodyGuard(backup, protectedAlly)); // 기존 몸으로 막기 한 건 설정
        Assert.IsTrue(actions.TryShieldOath(guard, protectedAlly, 1)); // 같은 대상에 한 적 턴의 맹세 설정
        actions.PrepareActions(); // 맹세와 몸으로 막기의 예정 대상 준비
        Assert.IsTrue(actions.TryShieldTaunt(backup)); // 보호 대상이 아닌 동료의 전체 도발 적용
        CollectionAssert.AreEqual(new[] { backup, backup }, actions.PlannedActions.Select(action => action.Target)); // 도발 우선 대상의 피격 예고 확인
        Assert.IsTrue(actions.TryShieldTaunt(protectedAlly)); // 보호 대상이 직접 도발하도록 변경
        CollectionAssert.AreEqual(new[] { guard, guard }, actions.PlannedActions.Select(action => action.Target)); // 도발 이후 맹세의 보호자 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 실제 적 공격 실행 시점 진입
        actions.ExecuteAction(actions.PlannedActions[0]); // 맹세로 첫 공격 대신 받기
        actions.ExecuteAction(actions.PlannedActions[0]); // 같은 맹세로 두 번째 공격 대신 받기
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 맹세와 도발이 끝나는 적 턴 완료
        actions.PrepareActions(); // 남아 있는 한 차례 보호의 적 예고 생성
        CollectionAssert.AreEqual(new[] { backup, protectedAlly }, actions.PlannedActions.Select(action => action.Target)); // 기존 몸으로 막기가 한 공격만 남아 있는지 확인
    } // 보호 우선순위 확인 종료

    [Test] // 적이 직접 상태 이상만 적용하는 경우 검사
    public void OathDoesNotRedirectStatusOnlyActions() // 피해 없는 적 상태 행동의 원래 대상 유지 확인
    { // 상태 공격 구분 시작
        JsonUtility.FromJsonOverwrite($"{{\"actionType\":{(int)EnemyActionType.ApplyStatusEffect},\"statusEffectType\":{(int)BattleStatusEffectType.Bleed}}}", enemy); // 직접 피해 없는 출혈 적용 적 설정
        Assert.IsTrue(actions.TryShieldOath(guard, protectedAlly, 2)); // 상태 적용 전 검사용 맹세 등록
        actions.PrepareActions(); // 지정 아군을 향한 상태 효과 예고 생성
        Assert.AreSame(protectedAlly, actions.PlannedActions[0].Target); // 상태 이상 행동의 기존 대상 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 상태 적용이 진행되는 적 턴 진입
        Assert.IsTrue(actions.ExecuteAction(actions.PlannedActions[0]).IsStatusAction); // 실제 출혈 적용 행동 실행 확인
        Assert.IsNotNull(protectedAlly.GetStatusEffect(BattleStatusEffectType.Bleed)); // 기존 지정 아군의 출혈 적용 확인
        Assert.IsNull(guard.GetStatusEffect(BattleStatusEffectType.Bleed)); // 맹세 보호자의 출혈 없음 확인
    } // 상태 공격 구분 종료
} // 수호 맹세 검사 종료
