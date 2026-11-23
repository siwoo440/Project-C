using System.Collections; // 실제 카드 플레이 모드 검사 사용
using System.Linq; // 등록된 유물 원본의 고유 검색 사용
using NUnit.Framework; // 실제 피해와 방어도 비교 사용
using UnityEngine; // 독립 검사 카드와 적 원본 생성 사용
using UnityEngine.TestTools; // 테스트의 실제 플레이 모드 전환 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 대상 선택 전투 환경

public sealed class WinterfallFearAndMechanicalHitRelicTests // 피리구멍과 회로핀의 실제 직접 타격 경계 확인
{ // 적 상태와 분류 유물 검사 시작
    [UnityTest] // 실제 적의 활성 공포와 첫 기계형 명중 검사 지정
    public IEnumerator FearDamageAppliesPerLandedHitAndCircuitOnlyToFirstMechanicalHit() // 공포마다 피해 가산과 기계형 첫 명중의 전투당 한 번 적용 확인
    { // 전투 유물 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 손패와 타격 환경 진입
        yield return CheckDirectHits(); // 도메인 전환이 끝난 뒤 실제 명중 검사 실행
        yield return new ExitPlayMode(); // 직접 타격 테스트 전투 종료
    } // 전투 유물 플레이 모드 종료

    private static IEnumerator CheckDirectHits() // 두 타격 카드의 공포와 기계형 대상별 차이 확인
    { // 적 직접 명중 검사 시작
        RelicData whistle = Hold(RelicEffectType.FearTargetDirectDamage); // 실제 등록한 어두운 피리구멍 보유
        RelicData pin = Hold(RelicEffectType.FirstMechanicalDirectHitBonus); // 실제 등록한 녹슨 회로핀 보유
        Assert.AreEqual(2, whistle.EffectValue); // 활성 공포 대상의 실제 유물 가산량 확인
        Assert.AreEqual(RelicTriggerType.CardAttackHit, whistle.TriggerType); // 피리구멍의 직접 카드 명중용 설정 확인
        Assert.AreEqual(4, pin.EffectValue); // 첫 기계형 명중의 실제 유물 가산량 확인
        Assert.AreEqual(RelicTriggerType.CardAttackHit, pin.TriggerType); // 회로핀의 직접 카드 명중용 설정 확인
        Assert.AreEqual(1, pin.MaximumTriggersPerBattle); // 기계형 전투당 한 번 설정 확인
        EnemyData ordinary = CreateEnemy("TEST_FEAR", false); // 공포만 검사할 일반 적 원본
        EnemyData mechanical = CreateEnemy("TEST_MACHINE", true); // 두 기계형 대상에 사용할 명시 분류 원본
        CardData strike = ScriptableObject.CreateInstance<CardData>(); // 정량 마법 피해와 두 타격을 가진 독립 검사 카드 생성
        try // 원본 정리를 보장한 직접 카드 검증
        { // 실제 전투 검사 시작
            JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"TEST_RELIC_STRIKE\",\"displayName\":\"연속 시험\",\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.Magical},\"effectValue\":5,\"damageHitCount\":2}}", strike); // 실제 두 번 마법 피해 오를 주는 원본 설정
            using var fight = Fight.WithSources(new[] { ordinary, mechanical, mechanical }, new[] { strike, strike, strike, strike }); // 여러 직접 타격을 실행할 별도 적 명단과 손패 구성
            var scared = fight.Enemies[0]; // 활성 공포를 가진 일반 적
            var evasive = fight.Enemies[1]; // 처음 두 타격을 피할 기계형 적
            var armored = fight.Enemies[2]; // 간접 피해 후 첫 실제 명중을 방어할 기계형 적
            Assert.IsFalse(scared.EnemySource.IsMechanical); // 거짓 또는 기본 분류와 실제 일반 적 확인
            Assert.IsTrue(armored.EnemySource.IsMechanical); // 실제 원본에 표시한 기계형 분류 확인
            scared.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1); // 일반 적에 활성 공포 효과 부여
            evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 일반 직접 피해 전 회피 기계형 적 준비
            armored.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1); // 회로핀과 피리구멍을 함께 받을 다른 기계형 대상 준비
            armored.TakeDamage(2, BattleDamageType.None, fight.Allies[0]); // 별도 유물 처리 없는 아군 출처 간접 피해 확인
            Assert.AreEqual(198, armored.CurrentHealth); // 간접 피해에 회로핀이나 공포 피해가 붙지 않는지 확인
            yield return PlayStrike(fight, strike, 0); // 활성 공포 일반 적에게 실제 두 번 타격
            Assert.AreEqual(186, scared.CurrentHealth); // 기본 오와 유물 이가 두 타격에 각각 적용되는지 확인
            yield return PlayStrike(fight, strike, 1); // 기계형에는 명중하지 못하는 두 번의 회피 타격 실시
            Assert.AreEqual(200, evasive.CurrentHealth); // 회피한 기계형 적이 피해를 받지 않는지 확인
            armored.GainBlock(100); // 회로핀의 첫 명중 전체를 흡수할 기존 방어도 준비
            yield return PlayStrike(fight, strike, 2); // 실제 첫 기계형 두 타격의 공포와 회로핀 피해 처리
            Assert.AreEqual(198, armored.CurrentHealth); // 일반 방어도가 두 타격의 체력 손실을 흡수 확인
            Assert.AreEqual(82, armored.CurrentBlock); // 첫 열한 점과 다음 일곱 점 각각의 방어도 차감 확인
            scared.GetStatusEffect(BattleStatusEffectType.Fear).AdvanceDuration(); // 제거 처리가 되기 전에 기간이 만료된 공포 경계 확인
            yield return PlayStrike(fight, strike, 0); // 이미 만료된 공포에는 추가 피해를 주지 않는 두 타격 확인
            Assert.AreEqual(176, scared.CurrentHealth); // 만료 상태에서는 기본 오씩만 적용 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 회로핀을 사용한 아군 턴 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴의 손패와 방어도 재설정
            Assert.AreEqual(0, armored.CurrentBlock); // 다음 라운드에서 남은 이전 방어도 제거 확인
            Assert.IsNotNull(armored.GetStatusEffect(BattleStatusEffectType.Fear)); // 두 번째 라운드에도 두 타격을 강화할 공포 확인
            yield return PlayStrike(fight, strike, 2); // 다음 아군 턴의 기계 대상에 다시 정상 두 번 명중
            Assert.AreEqual(184, armored.CurrentHealth); // 공포 두 타격만 적용하고 회로핀을 전투 내 재사용하지 않는지 확인
        } // 실제 전투 검사 종료
        finally // 실제 전투 종료 뒤 임시 원본 정리
        { // 검사 자료 해제 시작
            Object.Destroy(strike); // 임시 두 타격 카드 원본 제거
            Object.Destroy(ordinary); // 일반 적 검사 원본 제거
            Object.Destroy(mechanical); // 기계형 적 검사 원본 제거
        } // 검사 자료 해제 종료
    } // 적 직접 명중 검사 종료

    private static EnemyData CreateEnemy(string id, bool mechanical) // 정확한 게임 원본 분류를 가진 검사 적 생성
    { // 검사 적 원본 준비 시작
        var data = ScriptableObject.CreateInstance<EnemyData>(); // 독립 적 원본 에셋 생성
        JsonUtility.FromJsonOverwrite($"{{\"enemyId\":\"{id}\",\"maxHealth\":200,\"isMechanical\":{(mechanical ? "true" : "false")}}}", data); // 최대 체력과 기계 분류 기본값 설정
        return data; // 실제 전투 명단에서 선택할 적 원본 반환
    } // 검사 적 원본 준비 종료

    private static IEnumerator PlayStrike(Fight fight, CardData source, int index) // 기존 손패의 실제 클릭으로 두 타격 카드 사용
    { // 실제 공격 카드 입력 시작
        return WinterfallCommonCardExpansionTests.Play(source, fight.Hand, fight.EnemyViews[index], fight.Runner, fight.Deck, fight.Points); // 실제 공용 행동력과 버린 카드 및 타격 경로 검증 사용
    } // 실제 공격 카드 입력 종료

    private static RelicData Hold(RelicEffectType effect) // 실제 새 전투 유물의 회차 보유 처리
    { // 유물 조회와 획득 시작
        var relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == effect); // 실제 등록한 유물 효과로 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 현재 회차에 보유된 실제 유물 확인
        return relic; // 새 원본의 효과 값과 발동 시점 확인 자료 반환
    } // 유물 조회와 획득 종료
} // 적 상태와 분류 유물 검사 종료
