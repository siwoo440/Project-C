using System; // 테스트 자원 정리 사용
using System.Collections.Generic; // 빈 전투 덱 목록 사용
using NUnit.Framework; // 상태 이상 동작 검증 사용
using UnityEngine; // 전투 데이터 생성 사용
public sealed class StatusDurationRulesTests // 상태 강도와 지속 턴 규칙 검사
{ // 상태 규칙 검사 시작
    [Test] // 중첩 중독의 전체 수치 감소 검사 지정
    public void Poison_DecreasesTotalByOneAtEachOwnTurnStart() // 중첩 중독 피해와 전체 수치 감소 확인
    { // 중독 감소 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            battle.Turns.EndPlayerTurn(); // 적 턴 시작
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 2, 2); // 중독 수치 셋 적용
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 2, 2); // 중독 두 번째 중첩 적용
            int health = battle.Ally.CurrentHealth; // 중독 적용 전 체력 기록
            battle.Turns.CompleteEnemyTurn(); // 첫 중독 대상 턴 시작
            Assert.AreEqual(health - 6, battle.Ally.CurrentHealth); // 첫 턴 중첩 합계 피해 확인
            Assert.AreEqual(5, battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 전체 중독 수치 한 점 감소 확인
            Assert.AreEqual(2, battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 턴 시작에 지속 턴 유지 확인
            battle.Turns.EndPlayerTurn(); // 첫 대상 턴 종료
            Assert.AreEqual(1, battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison).RemainingTurns); // 첫 턴 종료 지속 감소 확인
            battle.Turns.CompleteEnemyTurn(); // 두 번째 중독 대상 턴 시작
            Assert.AreEqual(health - 11, battle.Ally.CurrentHealth); // 감소한 수치 다섯의 피해 확인
            Assert.AreEqual(4, battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 중독 전체 수치 재감소 확인
            battle.Turns.EndPlayerTurn(); // 두 번째 대상 턴 종료
            Assert.IsNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 지속 턴 만료 제거 확인
        } // 독립 전투 검사 종료
    } // 중독 감소 검사 종료
    [Test] // 사용자 확인 화상 지속 검사 지정
    public void Burn_OwnTurnApplicationSkipsOnlyFirstDurationDecrease() // 자기 턴 화상 피해와 지속 턴 분리 확인
    { // 화상 지속 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            int health = battle.Ally.CurrentHealth; // 화상 적용 전 체력 기록
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 자기 턴에 화상 적용
            battle.Turns.EndPlayerTurn(); // 화상 대상의 첫 턴 종료
            BattleStatusEffectInstance burn = battle.Ally.GetStatusEffect(BattleStatusEffectType.Burn); // 남은 화상 조회
            Assert.AreEqual(health - 2, battle.Ally.CurrentHealth); // 첫 종료 화상 피해 둘 확인
            Assert.AreEqual(1, burn.EffectiveValue); // 화상 수치 한 점 감소 확인
            Assert.AreEqual(2, burn.RemainingTurns); // 첫 종료 지속 턴 감소 생략 확인
            battle.Turns.CompleteEnemyTurn(); // 다음 아군 턴 시작
            battle.Turns.EndPlayerTurn(); // 두 번째 아군 턴 종료
            Assert.AreEqual(health - 3, battle.Ally.CurrentHealth); // 두 번째 화상 피해 하나 확인
            Assert.IsNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.Burn)); // 화상 수치 소진 제거 확인
        } // 독립 전투 검사 종료
    } // 화상 지속 검사 종료
    [Test] // 자기 턴 강화 지속 검사 지정
    public void OwnTurnBuff_RemainsUsableThroughNextTurn() // 자기 턴에 받은 강화의 첫 감소 생략 확인
    { // 자기 턴 강화 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 1, 1); // 자기 턴에 한 턴 공격 강화 적용
            battle.Turns.EndPlayerTurn(); // 적용한 아군 턴 종료
            Assert.AreEqual(1, battle.Ally.GetStatusEffect(BattleStatusEffectType.AttackPowerUp).RemainingTurns); // 적용 턴 지속 감소 생략 확인
            battle.Turns.CompleteEnemyTurn(); // 다음 아군 턴 시작
            Assert.AreEqual(2, battle.Ally.AttackPowerBonus); // 다음 턴에도 공격 강화 사용 가능 확인
            battle.Turns.EndPlayerTurn(); // 다음 아군 턴 종료
            Assert.IsNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 다음 턴 종료 후 만료 확인
        } // 독립 전투 검사 종료
    } // 자기 턴 강화 검사 종료
    [Test] // 상대 턴 상태 지속 검사 지정
    public void OpponentTurnDebuff_RemainsUntilTargetTurnEnds() // 상대 턴에 받은 약화의 대상 턴 지속 확인
    { // 상대 턴 약화 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            battle.Turns.EndPlayerTurn(); // 적 턴 시작
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 1, 1, 1); // 상대 턴에 한 턴 방어 약화 적용
            battle.Turns.CompleteEnemyTurn(); // 대상 아군 턴 시작
            Assert.IsNotNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 대상 턴 시작 후 약화 유지 확인
            battle.Turns.EndPlayerTurn(); // 대상 아군 턴 종료
            Assert.IsNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 대상 턴 종료 후 약화 만료 확인
        } // 독립 전투 검사 종료
    } // 상대 턴 약화 검사 종료
    [Test] // 감소한 중독의 이전 검사 지정
    public void TransferredPoison_PreservesAccumulatedDecay() // 약해진 중독을 다른 아군에게 이전할 때 수치 유지 확인
    { // 중독 이전 검사 시작
        CharacterData sourceData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime source = BattleUnitRuntime.CreateAlly(sourceData); // 중독 원래 아군 생성
        BattleUnitRuntime recipient = BattleUnitRuntime.CreateAlly(sourceData); // 중독을 받을 아군 생성
        source.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 첫 중독 중첩 적용
        source.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 두 번째 중독 중첩 적용
        new BattleStatusEffectProcessor().ProcessPhaseStart(source, 1); // 중독 발동과 전체 수치 감소
        Assert.AreEqual(5, source.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 감소한 중독 수치 확인
        Assert.IsTrue(BattleDebuffTransfer.TryTransfer(source, recipient, source.GetStatusEffect(BattleStatusEffectType.Poison))); // 감소한 중독 이전 확인
        Assert.IsNull(source.GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 아군의 중독 제거 확인
        Assert.AreEqual(5, recipient.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 이전 후 감소한 수치 유지 확인
        UnityEngine.Object.DestroyImmediate(sourceData); // 임시 아군 원본 제거
    } // 중독 이전 검사 종료
    [Test] // 기존 중독과의 이전 병합 검사 지정
    public void TransferredPoison_MergesWithoutRestoringDecayedStrength() // 중독 병합 시 이전한 감소량 보존 확인
    { // 중독 병합 검사 시작
        CharacterData sourceData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime source = BattleUnitRuntime.CreateAlly(sourceData); // 중독 원래 아군 생성
        BattleUnitRuntime recipient = BattleUnitRuntime.CreateAlly(sourceData); // 중독을 받을 아군 생성
        source.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 첫 중독 중첩 적용
        source.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 두 번째 중독 중첩 적용
        recipient.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 받는 아군의 기존 중독 적용
        new BattleStatusEffectProcessor().ProcessPhaseStart(source, 1); // 이전할 중독 총량 감소
        Assert.IsTrue(BattleDebuffTransfer.TryTransfer(source, recipient, source.GetStatusEffect(BattleStatusEffectType.Poison))); // 기존 중독에 추가 이전 확인
        Assert.AreEqual(8, recipient.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 기존 셋과 감소한 다섯의 합계 확인
        UnityEngine.Object.DestroyImmediate(sourceData); // 임시 아군 원본 제거
    } // 중독 병합 검사 종료
    [Test] // 소환 적의 자기 턴 상태 지속 검사 지정
    public void SummonedEnemy_SkipsFirstDurationDecreaseDuringOwnTurn() // 실제 등록한 소환 적의 화상 지속 처리 확인
    { // 소환 적 상태 지속 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData sourceDeck = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime original = BattleUnitRuntime.CreateEnemy(enemyData); // 기존 적 유닛 생성
        BattleUnitRuntime summoned = BattleUnitRuntime.CreateEnemy(enemyData); // 소환할 적 유닛 생성
        BattleUnitRuntime[] allies = { ally }; // 아군 목록 준비
        List<BattleUnitRuntime> enemies = new List<BattleUnitRuntime> { original }; // 변경 가능한 적 목록 준비
        BattleDeckRuntime deck = BattleDeckRuntime.Create(sourceDeck, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 실제 턴 관리자 생성
        BattleStatusEffectController controller = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 관리자 생성
        turns.StartBattle(0); // 첫 아군 턴 시작
        turns.EndPlayerTurn(); // 적 턴 시작
        enemies.Add(summoned); // 소환 적을 실제 전투 목록에 추가
        Assert.IsTrue(turns.RegisterSummonedEnemy(summoned)); // 소환 적 승패 판정 등록 확인
        Assert.IsTrue(controller.RegisterSummonedEnemy(summoned)); // 소환 적 상태 감시 등록 확인
        summoned.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 적 자신의 턴에 화상 적용
        int health = summoned.CurrentHealth; // 화상 적용 전 체력 기록
        turns.CompleteEnemyTurn(); // 소환 적의 첫 턴 종료
        Assert.AreEqual(health - 2, summoned.CurrentHealth); // 첫 턴 종료 화상 피해 확인
        Assert.AreEqual(2, summoned.GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 소환 적의 첫 지속 감소 생략 확인
        controller.Dispose(); // 상태 관리자 연결 해제
        turns.Dispose(); // 턴 관리자 연결 해제
        UnityEngine.Object.DestroyImmediate(allyData); // 임시 아군 원본 제거
        UnityEngine.Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
        UnityEngine.Object.DestroyImmediate(sourceDeck); // 임시 덱 원본 제거
    } // 소환 적 상태 지속 검사 종료
    [Test] // 중독의 수치 소진 검사 지정
    public void Poison_RemovesAfterItsTotalReachesZero() // 중독 수치 소진 시 추가 피해 방지 확인
    { // 중독 소진 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            battle.Turns.EndPlayerTurn(); // 적 턴 시작
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 5, 1); // 오래 지속되는 중독 수치 하나 적용
            int health = battle.Ally.CurrentHealth; // 중독 전 체력 기록
            battle.Turns.CompleteEnemyTurn(); // 중독 대상 턴 시작
            Assert.AreEqual(health - 1, battle.Ally.CurrentHealth); // 중독 피해 하나 확인
            Assert.IsNull(battle.Ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 수치 소진 후 상태 제거 확인
        } // 독립 전투 검사 종료
    } // 중독 소진 검사 종료
    [Test] // 중첩 화상의 전체 수치 감소 검사 지정
    public void Burn_DecreasesTotalByOneEvenWhenStacked() // 중첩된 화상 총 피해가 한 점만 감소하는지 확인
    { // 화상 전체 수치 검사 시작
        using (TestBattle battle = new TestBattle()) // 독립 전투 생성과 정리 보장
        { // 독립 전투 검사 시작
            battle.Turns.EndPlayerTurn(); // 적 턴 시작
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 2); // 화상 첫 중첩 적용
            battle.Ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 2); // 화상 두 번째 중첩 적용
            int health = battle.Ally.CurrentHealth; // 화상 전 체력 기록
            battle.Turns.CompleteEnemyTurn(); // 화상 대상 턴 시작
            battle.Turns.EndPlayerTurn(); // 화상 대상 턴 종료
            Assert.AreEqual(health - 4, battle.Ally.CurrentHealth); // 첫 화상 피해 합계 확인
            Assert.AreEqual(3, battle.Ally.GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 전체 화상 수치 한 점 감소 확인
        } // 독립 전투 검사 종료
    } // 화상 전체 수치 검사 종료
    [Test] // 큰 중독 수치의 감소 경계 검사 지정
    public void Poison_VisibleStrengthStillDecreasesAtIntegerLimit() // 큰 중첩 수치도 표시 한 점씩 감소하는지 확인
    { // 중독 정수 경계 검사 시작
        BattleStatusEffectInstance poison = new BattleStatusEffectInstance(BattleStatusEffectType.Poison, int.MaxValue, 2, 2); // 최대 수치의 중독 생성
        poison.Refresh(int.MaxValue, 2, 2); // 최대 중독 수치 중첩
        Assert.AreEqual(int.MaxValue, poison.EffectiveValue); // 표시 가능한 최대 중독 수치 확인
        poison.DecayPoison(); // 전체 중독 수치 한 점 감소
        Assert.AreEqual(int.MaxValue - 1, poison.EffectiveValue); // 표시되는 전체 중독 수치 감소 확인
    } // 중독 정수 경계 검사 종료
    [Test] // 턴 종료 연쇄 효과의 지속 검사 지정
    public void EffectAppliedDuringEndProcessing_SkipsFirstDurationForUnprocessedAlly() // 앞선 아군의 종료 효과가 뒤 아군에게 적용된 경우 확인
    { // 턴 종료 연쇄 적용 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        CharacterData secondData = ScriptableObject.CreateInstance<CharacterData>(); // 두 번째 아군 원본 생성
        typeof(CharacterData).GetField("characterId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(secondData, "second"); // 두 번째 아군 고유 번호 설정
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(allyData); // 먼저 처리할 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(secondData); // 나중에 처리할 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleUnitRuntime[] allies = { first, second }; // 순서가 있는 아군 목록 생성
        BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController controller = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 관리자 생성
        turns.StartBattle(0); // 첫 아군 턴 시작
        turns.EndPlayerTurn(); // 적 턴 시작
        first.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 첫 아군의 종료 화상 적용
        turns.CompleteEnemyTurn(); // 아군 턴 시작
        controller.StatusEffectsProcessed += (unit, results) => // 상태 처리 완료 후 연쇄 효과 연결
        { // 연쇄 상태 효과 시작
            if (unit == first) // 먼저 처리된 아군의 종료 효과 확인
            { // 다음 아군 강화 적용 시작
                second.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 1, 1); // 아직 처리 전인 아군에게 강화 적용
            } // 다음 아군 강화 적용 종료
        }; // 연쇄 상태 효과 연결 종료
        turns.EndPlayerTurn(); // 첫 아군의 화상과 두 번째 아군의 지속 처리
        Assert.IsNotNull(second.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 새로 받은 강화의 첫 지속 감소 생략 확인
        Assert.AreEqual(1, second.GetStatusEffect(BattleStatusEffectType.AttackPowerUp).RemainingTurns); // 적용 턴의 지속 한 턴 유지 확인
        controller.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        UnityEngine.Object.DestroyImmediate(allyData); // 임시 아군 원본 제거
        UnityEngine.Object.DestroyImmediate(secondData); // 두 번째 아군 원본 제거
        UnityEngine.Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
        UnityEngine.Object.DestroyImmediate(deckData); // 임시 덱 원본 제거
    } // 턴 종료 연쇄 적용 검사 종료
    [Test] // 이미 처리한 아군의 연쇄 효과 검사 지정
    public void EffectAppliedAfterTargetEndProcessing_ExpiresAtNextOwnTurnEnd() // 종료 처리가 끝난 아군의 다음 턴 지속 확인
    { // 처리 완료 아군 연쇄 적용 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        CharacterData secondData = ScriptableObject.CreateInstance<CharacterData>(); // 두 번째 아군 원본 생성
        typeof(CharacterData).GetField("characterId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(secondData, "second"); // 두 번째 아군 고유 번호 설정
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(allyData); // 먼저 처리할 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(secondData); // 나중에 처리할 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleUnitRuntime[] allies = { first, second }; // 순서가 있는 아군 목록 생성
        BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController controller = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 관리자 생성
        turns.StartBattle(0); // 첫 아군 턴 시작
        turns.EndPlayerTurn(); // 적 턴 시작
        second.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1); // 두 번째 아군의 종료 화상 적용
        turns.CompleteEnemyTurn(); // 아군 턴 시작
        controller.StatusEffectsProcessed += (unit, results) => // 상태 처리 완료 후 연쇄 효과 연결
        { // 연쇄 상태 효과 시작
            if (unit == second && turns.CurrentRound == 2) // 두 번째 아군의 첫 종료 효과 확인
            { // 이미 처리된 아군 강화 적용 시작
                first.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 1, 1); // 종료 처리가 끝난 아군에게 강화 적용
            } // 이미 처리된 아군 강화 적용 종료
        }; // 연쇄 상태 효과 연결 종료
        turns.EndPlayerTurn(); // 먼저 첫 아군을 처리하고 두 번째 아군의 화상 처리
        Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 처리 이후 받은 강화 유지 확인
        turns.CompleteEnemyTurn(); // 다음 아군 턴 시작
        Assert.AreEqual(2, first.AttackPowerBonus); // 다음 아군 턴의 강화 효과 확인
        turns.EndPlayerTurn(); // 다음 아군 턴 종료
        Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 다음 턴 종료 후 정상 만료 확인
        controller.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        UnityEngine.Object.DestroyImmediate(allyData); // 임시 아군 원본 제거
        UnityEngine.Object.DestroyImmediate(secondData); // 두 번째 아군 원본 제거
        UnityEngine.Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
        UnityEngine.Object.DestroyImmediate(deckData); // 임시 덱 원본 제거
    } // 처리 완료 아군 연쇄 적용 검사 종료
    private sealed class TestBattle : IDisposable // 상태 규칙 검사용 독립 전투
    { // 독립 전투 시작
        private readonly CharacterData allyData; // 임시 아군 원본
        private readonly EnemyData enemyData; // 임시 적 원본
        private readonly DeckData deckData; // 임시 덱 원본
        private readonly BattleStatusEffectController status; // 상태 이상 관리자
        public BattleUnitRuntime Ally { get; } // 검사 아군 조회
        public BattleTurnRuntime Turns { get; } // 전투 턴 관리자 조회
        public TestBattle() // 독립 전투 생성과 시작
        { // 독립 전투 생성 시작
            allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
            deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
            Ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 전투 유닛 생성
            BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
            BattleUnitRuntime[] allies = { Ally }; // 아군 목록 생성
            BattleUnitRuntime[] enemies = { enemy }; // 적 목록 생성
            BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 독립 턴 관리자 생성
            status = new BattleStatusEffectController(Turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 관리자 생성
            Assert.IsTrue(Turns.StartBattle(0)); // 첫 아군 턴 시작 확인
        } // 독립 전투 생성 종료
        public void Dispose() // 전투 이벤트와 원본 정리
        { // 독립 전투 정리 시작
            status.Dispose(); // 상태 관리자 연결 해제
            Turns.Dispose(); // 턴 관리자 연결 해제
            UnityEngine.Object.DestroyImmediate(allyData); // 임시 아군 원본 제거
            UnityEngine.Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
            UnityEngine.Object.DestroyImmediate(deckData); // 임시 덱 원본 제거
        } // 독립 전투 정리 종료
    } // 독립 전투 종료
} // 상태 규칙 검사 종료
