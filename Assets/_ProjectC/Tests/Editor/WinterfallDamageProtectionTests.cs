using System.Linq; // 실제 광역 피격 결과 확인
using NUnit.Framework; // 방어 적용 결과 검증
using UnityEngine; // 독립 전투 원본 생성

public sealed class WinterfallDamageProtectionTests // 피해 무효와 광역 피해 감소 검증
{ // 방어 효과 검사 시작
    private CharacterData character; // 기본 방어력 없는 검사 아군 원본
    private EnemyData enemyData; // 검사 공격 의도 원본

    [SetUp] // 매 검사마다 원본 준비
    public void SetUp() // 테스트 전투 원본 생성
    { // 원본 초기화 시작
        character = ScriptableObject.CreateInstance<CharacterData>(); // 방어력 없는 아군 생성 자료
        enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 공격 규칙을 지정할 적 생성 자료
    } // 원본 초기화 종료

    [TearDown] // 매 검사 원본 정리
    public void TearDown() // 임시 전투 원본 삭제
    { // 원본 정리 시작
        Object.DestroyImmediate(character); // 임시 아군 원본 제거
        Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
    } // 원본 정리 종료

    [Test] // 현재 방어도와 첫 양수 피해 검증
    public void GuardProtectsHealthAndBlockOnceWithoutPreviewConsumption() // 방어도 손실 방지와 한 번 소모의 실제 결과
    { // 일회 보호 검사 시작
        var ally = BattleUnitRuntime.CreateAlly(character); // 피해를 받는 실제 아군 생성
        var enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 피해 발생자 생성
        ally.GainBlock(12); // 피해로 잃을 기존 방어도 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 일회 피해 무효 상태 부여
        new BattleStatusEffectProcessor().ProcessPhaseEnd(ally, 1); // 피해 이전의 아군 턴 종료
        int health = ally.CurrentHealth; // 피해 전 실제 체력 보관
        Assert.IsFalse(ally.TakeDamage(0, BattleDamageType.Physical, enemy, enemyDirectAttack: true).WasDamageNullified); // 영점 피해 미소모 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.ShockAbsorption, 8, 1, 1); // 방어도에 닿기 전 피해 전량 감소 준비
        Assert.IsFalse(ally.TakeDamage(8, BattleDamageType.Physical, enemy, enemyDirectAttack: true).WasDamageNullified); // 기존 경감만으로 영점인 피해의 무효 상태 미소모
        ally.RemoveStatusEffect(BattleStatusEffectType.ShockAbsorption); // 다음 실제 피해 비교에 쓸 추가 경감 정리
        Assert.IsTrue(ally.PreviewDamage(8, BattleDamageType.Physical, enemy, enemyDirectAttack: true).WasDamageNullified); // 실제 적용 전 일회 피해 무효 예고
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 예고와 턴 종료 뒤 상태 보존 확인
        var firstHit = ally.TakeDamage(8, BattleDamageType.Physical, enemy, enemyDirectAttack: true); // 기존 방어도에 흡수될 공격 입력
        Assert.IsTrue(firstHit.WasDamageNullified); // 첫 양수 공격의 피해 무효 기록
        Assert.AreEqual(0, firstHit.AbsorbedDamage); // 방어도의 피해 흡수 없음 확인
        Assert.AreEqual(health, ally.CurrentHealth); // 실제 체력 손실 없음 확인
        Assert.AreEqual(12, ally.CurrentBlock); // 기존 방어도 보존 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 실제 무효화 한 번의 상태 소모 확인
        Assert.AreEqual(8, ally.TakeDamage(8, BattleDamageType.Physical, enemy, enemyDirectAttack: true).AbsorbedDamage); // 다음 유효 피해의 정상 방어도 소모
    } // 일회 보호 검사 종료

    [Test] // 실제 출혈과 체력 비용 검증
    public void GuardCanAnnulUnblockedDamageAndKeepsEachAllysUseIndependent() // 무방어 피해와 한 명씩 다른 소모 상태 검증
    { // 개별 보호 검사 시작
        var first = BattleUnitRuntime.CreateAlly(character); // 첫 번째 보호 대상 생성
        var second = BattleUnitRuntime.CreateAlly(character); // 두 번째 보호 대상 생성
        first.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 첫 아군의 독립 보호 등록
        second.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 둘째 아군의 독립 보호 등록
        Assert.IsTrue(first.TrySpendHealth(2)); // 피해와 별개인 체력 비용 지급
        first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 1, 1); // 방어도를 우회하는 실제 출혈 부여
        int health = first.CurrentHealth; // 출혈 처리 전 체력 보관
        new BattleStatusEffectProcessor().ProcessPhaseEnd(first, 1); // 정상 출혈 지속 피해 처리
        Assert.AreEqual(health, first.CurrentHealth); // 출혈의 무방어 체력 피해 차단 확인
        Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 첫 아군의 실제 한 번 소모 확인
        Assert.IsNotNull(second.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 둘째 아군의 독립 보호 보존
    } // 개별 보호 검사 종료

    [TestCase(false)] // 생존 아군 두 명의 실제 광역 공격
    [TestCase(true)] // 생존 아군 한 명의 실제 광역 공격
    public void AreaIntentUsesDistributedDefenseWithAnySurvivorCount(bool loneSurvivor) // 공격 의도와 실제 피해 및 예고 일치 확인
    { // 광역 의도 검사 시작
        JsonUtility.FromJsonOverwrite("{\"targetRule\":3,\"basicAttackPower\":20}", enemyData); // 전체 공격으로 설정한 실제 적 원본
        var enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 선언된 전체 공격 사용 적 생성
        var first = BattleUnitRuntime.CreateAlly(character); // 피해를 회피하지 않는 처음 대상
        var second = BattleUnitRuntime.CreateAlly(character); // 둘째 공격 대상 생성
        if (loneSurvivor) // 생존자 한 명인 비교 조건 확인
        { // 둘째 대상 사망 처리 시작
            second.ApplyPersistentHealth(0); // 남은 생존자 한 명 설정
        } // 둘째 대상 사망 처리 종료
        first.ApplyStatusEffect(BattleStatusEffectType.DistributedDefense, 35, 2, 1); // 처음 대상에만 광역 피해 감소 적용
        using (var runtime = new BattleEnemyActionRuntime(new[] { enemy }, new[] { first, second })) // 실제 적 공격 관리자 생성
        { // 실제 예고와 실행 검사 시작
            Assert.AreEqual(1, runtime.PrepareActions()); // 적의 광역 행동이 한 순서로 예약되는지 확인
            var area = runtime.PlannedActions.Single(); // 실제 광역 예정 행동 조회
            Assert.IsTrue(area.IsAreaAttack); // 생존 인원과 별개인 광역 표시 확인
            int firstPreview = area.PreviewDamage(first).AppliedDamage; // 처음 대상의 실제 피해 예고
            Assert.AreEqual(area.AttackAmount * 65 / 100, firstPreview); // 광역 삼십오 퍼센트 감소와 내림 확인
            int secondPreview = area.PreviewDamage(second).AppliedDamage; // 둘째 대상의 생존 여부 반영 예고
            Assert.AreEqual(loneSurvivor ? 0 : area.AttackAmount, secondPreview); // 보호 없는 생존자와 사망자 구분 확인
            runtime.ExecuteAction(area); // 한 번의 적 행동으로 모든 살아 있는 대상 공격
            Assert.AreEqual(loneSurvivor ? 1 : 2, area.AreaHitResults.Count); // 죽은 대상 제외와 생존 대상의 실제 공격 수 확인
            Assert.AreEqual(firstPreview, area.AreaHitResults[0].Result.DamageResult.AppliedDamage); // 첫 대상의 예고와 실제 일치 확인
            if (!loneSurvivor) // 두 대상 조건의 둘째 결과 확인
            { // 두 번째 피격 대조 시작
                Assert.AreEqual(secondPreview, area.AreaHitResults[1].Result.DamageResult.AppliedDamage); // 보호 없는 둘째 대상의 실제 피해 검증
            } // 두 번째 피격 대조 종료
        } // 실제 예고와 실행 검사 종료
    } // 광역 의도 검사 종료

    [Test] // 실제 적 턴 두 회의 분산 방어 검증
    public void DistributedDefenseIgnoresSingleTargetAndExpiresAfterTwoEnemyTurns() // 세 번째 광역 공격부터 원래 피해 복원 확인
    { // 광역 방어 기간 검사 시작
        JsonUtility.FromJsonOverwrite("{\"targetRule\":3,\"basicAttackPower\":20}", enemyData); // 명시적 광역 공격 원본 준비
        var ally = BattleUnitRuntime.CreateAlly(character); // 실제 한 명의 생존 아군 생성
        var enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 광역 적 생성
        var allies = new[] { ally }; // 턴 제어기에 등록할 아군 목록
        var enemies = new[] { enemy }; // 턴 제어기에 등록할 적 목록
        var source = DeckData.CreateRuntime(System.Array.Empty<DeckCardEntry>()); // 드로우가 필요 없는 독립 전투 덱 원본
        try // 임시 덱 정리 보장
        { // 실제 적 턴 진행 시작
            var deck = BattleDeckRuntime.Create(source, allies); // 피해 시점 검사에 사용할 실제 전투 덱
            using (var turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, BattleType.Normal)) // 실제 전투 턴 관리자 생성
            using (var controller = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor())) // 실제 아군 상태와 적 턴 연결
            using (var actions = new BattleEnemyActionRuntime(enemies, allies, turns)) // 실제 예정 행동 관리자 연결
            { // 방어 상태의 기간별 실제 전투 시작
                Assert.IsTrue(turns.StartBattle(0)); // 첫 아군 턴 시작 확인
                ally.ApplyStatusEffect(BattleStatusEffectType.DistributedDefense, 35, 2, 1); // 아군 턴에 광역 방어 부여
                Assert.AreEqual(20, ally.PreviewDamage(20, BattleDamageType.Physical, enemy, enemyDirectAttack: true).AppliedDamage); // 기본 단일 공격 피해 미경감 확인
                for (int index = 0; index < 3; index++) // 보호된 두 턴과 이후 한 턴 확인
                { // 다음 적 턴의 보호 효과 확인 시작
                    Assert.IsTrue(turns.EndPlayerTurn()); // 실제 적 턴 진입 확인
                    Assert.AreEqual(1, actions.PrepareActions()); // 실제 광역 공격 하나 예약 확인
                    var action = actions.PlannedActions.Single(); // 현재 턴의 광역 공격 조회
                    int expected = index < 2 ? action.AttackAmount * 65 / 100 : action.AttackAmount; // 두 턴 동안 적용될 실제 예상 피해량
                    Assert.AreEqual(expected, action.PreviewDamage(ally).AppliedDamage); // 적 턴에 맞는 예상 경감 확인
                    Assert.AreEqual(expected, actions.ExecuteAction(action).DamageResult.AppliedDamage); // 실제 적 턴별 피해 검증
                    Assert.IsTrue(turns.CompleteEnemyTurn()); // 실제 적 턴 종료와 상태 지속 감소
                } // 다음 적 턴의 보호 효과 확인 종료
                Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.DistributedDefense)); // 두 적 턴 종료에서 분산 방어 제거 확인
            } // 방어 상태의 기간별 실제 전투 종료
        } // 실제 적 턴 진행 종료
        finally // 임시 덱 정리
        { // 임시 덱 정리 시작
            Object.DestroyImmediate(source); // 독립 덱 원본 삭제
        } // 임시 덱 정리 종료
    } // 광역 방어 기간 검사 종료

    [Test] // 복수 공격이 시뮬레이션할 일회 보호 검증
    public void PlannedImpactsUseNullificationOnlyOnFirstPositiveFinalDamage() // 실제 공격 순서와 예고 무효 상태 일치 확인
    { // 피해 예고 순서 확인 시작
        var ally = BattleUnitRuntime.CreateAlly(character); // 한 명의 공격 대상 생성
        var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 같은 대상을 공격하는 적 둘 생성
        ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 첫 타격을 무효화할 보호 등록
        using (var actions = new BattleEnemyActionRuntime(enemies, new[] { ally })) // 실제 적의 두 예정 행동 관리자 준비
        { // 앞선 피격의 보호 소비 예측 검사 시작
            Assert.AreEqual(2, actions.PrepareActions()); // 두 적의 유효 공격 준비
            var first = actions.PlannedActions[0]; // 현재 순서의 첫 타격 조회
            var next = actions.PlannedActions[1]; // 현재 순서의 둘째 타격 조회
            Assert.IsTrue(first.PreviewDamage().WasDamageNullified); // 처음 예정된 타격만 무효화 가능
            Assert.Greater(next.PreviewDamage().AppliedDamage, 0); // 두 번째 예정 피해에 정상 체력 손실 표시
            Assert.IsTrue(actions.ExecuteAction(first).DamageResult.WasDamageNullified); // 첫 타격의 실제 피해 무효 확인
            Assert.Greater(actions.ExecuteAction(next).DamageResult.AppliedDamage, 0); // 다음 타격의 실제 체력 손실 확인
        } // 앞선 피격의 보호 소비 예측 검사 종료
    } // 피해 예고 순서 확인 종료
} // 방어 효과 검사 종료
