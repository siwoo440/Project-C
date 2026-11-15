using System.Collections.Generic; // 전투 카드 목록 사용
using NUnit.Framework; // 보스 전환 결과 검증 사용
using UnityEngine; // 전투 원본 데이터 생성 사용
public sealed class WinterfallBossPhaseTests // 보스 페이즈 전환 검사
{ // 보스 전환 검사 시작
    [Test] // 실제 보스 조우와 페이즈 패턴 검사 지정
    public void BossEncounter_UsesVersionedDataAndSeparatePhaseCycles() // 보스 수치와 각 페이즈 행동 순환 확인
    { // 실제 보스 조우 검사 시작
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 새 보스 조우 조회
        Assert.IsNotNull(encounter); // 새 보스 조우 존재 확인
        Assert.IsFalse(encounter.IsAvailableForPoolVersion(1)); // 기존 탐사에서 새 보스 제외 확인
        Assert.IsTrue(encounter.IsAvailableForPoolVersion(2)); // 새 탐사에서 보스 사용 확인
        Assert.AreEqual(BattleType.Boss, encounter.BattleType); // 보스 조우 유형 확인
        EnemyData source = encounter.Enemies[0]; // 실제 보스 원본 조회
        Assert.AreEqual("ENM_WF_ASHSTORM_CORE", source.EnemyId); // 보스 식별자 확인
        Assert.AreEqual(350, source.MaxHealth); // 임시 시트 기본 체력 확인
        Assert.AreEqual(12, source.PhysicalDefense); // 임시 시트 방어력 확인
        Assert.AreEqual(12, source.MagicalResistance); // 임시 시트 저항력 확인
        Assert.AreEqual(4, source.ActionPatternCount); // 두 페이즈의 네 행동 확인
        Assert.AreEqual(2, source.SecondPhasePatternStartIndex); // 두 번째 페이즈 시작 순번 확인
        Assert.IsNotNull(source.Portrait); // 실제 보스 이미지 연결 확인
        CharacterData allySource = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allySource); // 아군 유닛 생성
        BattleUnitRuntime boss = BattleUnitRuntime.CreateEnemy(source); // 보스 유닛 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { boss }, new[] { ally }); // 적 행동 관리자 생성
        actions.PrepareActions(); // 첫 번째 예정 행동 생성
        Assert.AreEqual(1, actions.FindAction(boss).PatternIndex); // 첫 페이즈 첫 행동 확인
        actions.PrepareActions(); // 두 번째 예정 행동 생성
        Assert.AreEqual(2, actions.FindAction(boss).PatternIndex); // 첫 페이즈 두 번째 행동 확인
        actions.PrepareActions(); // 첫 페이즈 순환 행동 생성
        Assert.AreEqual(1, actions.FindAction(boss).PatternIndex); // 첫 페이즈에서만 반복 확인
        actions.SetBossPhaseStart(boss, source.SecondPhasePatternStartIndex); // 두 번째 페이즈 시작
        actions.PrepareActions(); // 두 번째 페이즈 첫 행동 생성
        Assert.AreEqual(3, actions.FindAction(boss).PatternIndex); // 두 번째 페이즈 첫 행동 확인
        actions.PrepareActions(); // 두 번째 페이즈 다음 행동 생성
        Assert.AreEqual(4, actions.FindAction(boss).PatternIndex); // 두 번째 페이즈 두 번째 행동 확인
        actions.PrepareActions(); // 두 번째 페이즈 순환 행동 생성
        Assert.AreEqual(3, actions.FindAction(boss).PatternIndex); // 두 번째 페이즈에서만 반복 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(allySource); // 임시 아군 원본 정리
    } // 실제 보스 조우 검사 종료
    [Test] // 보스 적중 정신력 효과 검사 지정
    public void BossAttack_MentalPressureAppliesOnHitButNotOnMiss() // 명중과 빗나감에 따른 정신력 변화 확인
    { // 정신력 압박 검사 시작
        CharacterData sourceAlly = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData sourceBoss = ScriptableObject.CreateInstance<EnemyData>(); // 임시 보스 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(sourceAlly); // 아군 전투 유닛 생성
        BattleUnitRuntime boss = BattleUnitRuntime.CreateEnemy(sourceBoss); // 보스 전투 유닛 생성
        ally.GainBlock(100); // 직접 피해에 따른 정신력 변화를 방어도로 차단
        int before = ally.CurrentMental; // 원래 정신력 기록
        boss.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1); // 보스 공격에 실명 적용
        BattleEnemyAction miss = new BattleEnemyAction(boss, ally, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "재 균열", 1, 1, 7, 0, 0, 1, () => 99, -4); // 반드시 빗나가는 정신력 공격 생성
        Assert.IsTrue(miss.Execute().WasMissed); // 보스 공격 빗나감 확인
        Assert.AreEqual(before, ally.CurrentMental); // 빗나간 공격의 정신력 유지 확인
        boss.RemoveStatusEffect(BattleStatusEffectType.Blind); // 보스 실명 제거
        BattleEnemyAction hit = new BattleEnemyAction(boss, ally, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "재 균열", 1, 1, 7, 0, 0, 1, () => 0, -4); // 반드시 적중하는 정신력 공격 생성
        Assert.AreEqual(-4, hit.AdditionalMentalChange); // 행동 예고의 정신력 변화 확인
        Assert.IsFalse(hit.Execute().WasMissed); // 보스 공격 적중 확인
        Assert.AreEqual(before - 4, ally.CurrentMental); // 방어도 흡수 후 정신력 압박 확인
        Object.DestroyImmediate(sourceAlly); // 임시 아군 원본 정리
        Object.DestroyImmediate(sourceBoss); // 임시 보스 원본 정리
    } // 정신력 압박 검사 종료
    [Test] // 적 턴의 전환 예약 검사 지정
    public void Transition_DuringEnemyTurnDoesNotEndNextPlayerTurn() // 적 턴 피해 이후 중복 턴 종료 방지 확인
    { // 적 턴 전환 검사 시작
        CharacterData allySource = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData bossSource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 보스 원본 생성
        DeckData deckSource = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allySource); // 아군 유닛 생성
        BattleUnitRuntime boss = BattleUnitRuntime.CreateEnemy(bossSource); // 보스 유닛 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 준비
        BattleUnitRuntime[] enemies = { boss }; // 보스 목록 준비
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckSource, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Boss); // 보스 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        WinterfallBossPhaseRuntime phase = new WinterfallBossPhaseRuntime(boss, turns, actions, 0); // 보스 전환 관리자 생성
        turns.StartBattle(0); // 첫 아군 턴 시작
        turns.EndPlayerTurn(); // 적 턴 진입
        boss.TakeDamage(boss.MaxHealth / 2, BattleDamageType.None, ally); // 적 턴 중 절반 체력 도달
        Assert.IsTrue(phase.HasPendingTransition); // 적 턴 전환 예약 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 한 번 종료
        Assert.IsTrue(phase.CompleteAfterEnemyTurn()); // 다음 아군 턴에서 페이즈 전환 마무리
        Assert.AreEqual(2, turns.CurrentRound); // 추가 턴 종료 없이 다음 라운드 유지 확인
        Assert.IsTrue(turns.IsPlayerTurn); // 아군 턴 유지 확인
        Assert.IsFalse(phase.CompleteAfterEnemyTurn()); // 중복 전환 방지 확인
        phase.Dispose(); // 보스 전환 관리자 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(allySource); // 임시 아군 원본 정리
        Object.DestroyImmediate(bossSource); // 임시 보스 원본 정리
        Object.DestroyImmediate(deckSource); // 임시 덱 원본 정리
    } // 적 턴 전환 검사 종료
    [Test] // 보스 전환의 턴 정리 검사 지정
    public void Transition_CleansPlayerTurnOnceAndSkipsEnemyAction() // 남은 손패 정리와 다음 아군 턴 확인
    { // 보스 전환 검사 시작
        CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 아군 원본 조회
        CardData card = Resources.Load<CardData>("StarterCards/Attack"); // 실제 공격 카드 조회
        EnemyData sourceEnemy = ScriptableObject.CreateInstance<EnemyData>(); // 임시 보스 원본 생성
        DeckData sourceDeck = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 아군 전투 유닛 생성
        BattleUnitRuntime boss = BattleUnitRuntime.CreateEnemy(sourceEnemy); // 보스 전투 유닛 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 준비
        BattleUnitRuntime[] enemies = { boss }; // 적 목록 준비
        BattleDeckRuntime deck = BattleDeckRuntime.Create(sourceDeck, new List<IDeckCardEntry> { new RunDeckCardEntry(card, character), new RunDeckCardEntry(card, character) }, allies, 5, 1); // 두 장의 실제 카드 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Boss); // 보스 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 예정 행동 관리자 생성
        WinterfallBossPhaseRuntime phase = new WinterfallBossPhaseRuntime(boss, turns, actions, 0); // 절반 체력 전환 관리자 생성
        boss.ModifyMaxHealth(50); // 기존 보스 난이도의 최대 체력 증가
        Assert.IsFalse(phase.HasPendingTransition); // 최대 체력 증가만으로 전환하지 않음 확인
        boss.RestoreHealth(50); // 난이도 증가분 체력 회복
        int ended = 0; // 아군 턴 종료 알림 횟수 초기화
        int enemyStarted = 0; // 적 턴 시작 알림 횟수 초기화
        turns.PhaseEnded += (kind, round) => // 아군 턴 종료 횟수 기록 연결
        { // 턴 종료 확인 시작
            if (kind == BattleTurnPhase.PlayerTurn) // 아군 턴 여부 확인
            { // 아군 턴 종료 기록 시작
                ended++; // 아군 턴 종료 횟수 증가
            } // 아군 턴 종료 기록 종료
        }; // 턴 종료 확인 연결 종료
        turns.PhaseStarted += (kind, round) => // 적 턴 시작 횟수 기록 연결
        { // 턴 시작 확인 시작
            if (kind == BattleTurnPhase.EnemyTurn) // 적 턴 여부 확인
            { // 적 턴 시작 기록 시작
                enemyStarted++; // 적 턴 시작 횟수 증가
            } // 적 턴 시작 기록 종료
        }; // 턴 시작 확인 연결 종료
        Assert.IsTrue(turns.StartBattle(2)); // 시작 손패 두 장 확인
        Assert.AreEqual(1, actions.PrepareActions()); // 기존 보스 행동 예고 확인
        points.Spend(2); // 기존 행동력 일부 소모
        boss.TakeDamage(boss.MaxHealth / 2, BattleDamageType.None, ally); // 보스 체력을 절반으로 감소
        Assert.IsTrue(phase.HasPendingTransition); // 즉시 전환 예약 확인
        Assert.IsTrue(phase.CompletePendingTransition()); // 미처리 행동 취소와 턴 전환 확인
        Assert.AreEqual(1, ended); // 아군 턴 종료 처리 한 번 확인
        Assert.AreEqual(0, enemyStarted); // 기존 적 행동 생략 확인
        Assert.AreEqual(2, turns.CurrentRound); // 다음 라운드 이동 확인
        Assert.IsTrue(turns.IsPlayerTurn); // 다음 아군 턴 확인
        Assert.AreEqual(0, deck.HandCount); // 기존 일반 손패 정리 확인
        Assert.AreEqual(2, deck.DiscardPileCount); // 기존 손패의 버림 더미 이동 확인
        Assert.AreEqual(6, points.CurrentActionPoints); // 새 턴 행동력 회복 확인
        Assert.IsFalse(phase.CompletePendingTransition()); // 중복 전환 방지 확인
        phase.Dispose(); // 보스 체력 감시 해제
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(sourceEnemy); // 임시 보스 원본 정리
        Object.DestroyImmediate(sourceDeck); // 임시 덱 원본 정리
    } // 보스 전환 검사 종료
} // 보스 전환 검사 종료
