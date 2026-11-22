using System; // 실제 독립 검사와 임시 유물 정리
using System.Collections; // 실제 Unity 전투 상태와 카드 연출 대기
using NUnit.Framework; // 실제 명중과 두 상태 수치 비교
using UnityEngine; // 유물 원본과 테스트용 카드 자료
using UnityEngine.TestTools; // 실제 Unity 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 손패와 적 클릭 경로
using Object = UnityEngine.Object; // 검사에서 생성한 실제 자산 정리
using Random = UnityEngine.Random; // 기존 전투의 실제 백분율 난수

public sealed class WinterfallSwampStatusOffenseTests // 늪지대 출혈과 마법 저항 및 중독 확률 유물의 실제 검증
{ // 새 세 가지 유물의 전투 확인 시작
    [UnityTest] // 원래 검 카드 한 장의 같은 대상과 기계 치환 확인
    public IEnumerator ObsidianRollsOncePerTargetAndUsesNativeMechanicalBleedConversion() // 같은 검을 두 번 맞은 적과 기계형 출혈의 실제 결과
    { // 실제 검 카드 후속 효과 검사 시작
        yield return new EnterPlayMode(); // 기존 전투와 원래 화면에서 실제 검 카드 사용 준비
        using (var shard = new OwnedRelic("ObsidianBladeShard")) // 실제 흑요석 칼편 장착
        { // 발동 실패와 성공 및 기계형 치환의 독립 검증 시작
            using (var fight = new Fight("DOUBLE_SLASH", "DOUBLE_SLASH")) // 같은 적을 두 번 맞히는 원래 검 카드
            { // 하나의 사용에서 첫 실패와 다음 타격을 비교
                var target = fight.Enemies[0]; // 실제 두 번째 타격도 맞을 생존 적
                int seed = FindSeed(15, second => second < 15); // 첫 난수 실패 및 두 번째만 성공하는 실제 시드
                fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 실제 카드 사용 직전의 원래 정수 난수 복원
                yield return fight.Use("DOUBLE_SLASH", 0); // 실제 원래 두 타격을 모두 적용
                Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 같은 카드의 두 번째 타격에서 확률을 다시 굴리지 않음
                int bleedSignals = 0; // 처음 실제 부여와 같은 상태 갱신의 횟수
                target.StatusEffectResolved += (_, _, type, _) => // 실제 원래 상태 결과의 이벤트 감시
                { // 현재 대상의 실제 출혈 시도 확인
                    if (type == BattleStatusEffectType.Bleed) // 새 검 유물이 원래 상태를 부여한 실제 결과
                    { // 원래 결과를 대상으로 한 횟수 집계
                        bleedSignals++; // 실제 적용 또는 중첩 신호의 개수
                    } // 원래 결과를 대상으로 한 횟수 집계 종료
                }; // 실제 원래 상태 결과 감시 종료
                seed = FindSeed(14, second => second < 15); // 새 카드의 두 타격이 성공 범위에 드는 대조
                yield return fight.Use("DOUBLE_SLASH", 0); // 실제 같은 적을 친 다음 원본 검 카드
                var bleed = target.GetStatusEffect(BattleStatusEffectType.Bleed); // 실제 남은 출혈 효과 원본
                Assert.AreEqual(1, bleedSignals); // 같은 카드의 동일 생존 적에는 성공이어도 한 번
                Assert.AreEqual(1, bleed?.EffectiveValue); // 흑요석 칼편의 원래 출혈 값 한 점
                Assert.AreEqual(2, bleed?.RemainingTurns); // 새 원본에 정한 두 적 차례
            } // 하나의 사용에서 첫 실패와 다음 타격 비교 종료
            var mechanical = ScriptableObject.CreateInstance<EnemyData>(); // 실제 원본 분류를 바꿀 기계형 적 자료
            try // 별도 기계 적의 임시 Unity 원본 정리 보장
            { // 명시 기계형의 실제 검 상태 처리 시작
                JsonUtility.FromJsonOverwrite("{\"enemyId\":\"TEST_SWAMP_MECHANICAL\",\"maxHealth\":200,\"isMechanical\":true}", mechanical); // 명시된 기계 판정의 원본 시트 필드 구성
                using (var conveyor = new OwnedRelic("ConveyorTooth")) // 기존 출혈을 치환하고 확률을 높이는 실제 컨베이어 톱니 장착
                using (var fight = Fight.WithEnemySources(new[] { mechanical }, "DOUBLE_SLASH")) // 실제 같은 명중을 맞는 기계형 적 한 명
                { // 명확히 기본 실패와 변환 성공의 경계 검증
                    var target = fight.Enemies[0]; // 실제 기계형으로 분류된 생존 적
                    int seed = FindSeed(34); // 원래 15에 컨베이어 20을 더해 마지막 성공하는 백분율
                    fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 실제 명중 전에 기존 원본 백분율 재현
                    yield return fight.Use("DOUBLE_SLASH", 0); // 기계에게 직접 검의 동일 타격 두 번
                    Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Bleed)); // 실제 출혈 변환에서 원래 출혈과 함께 남기지 않음
                    Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)?.EffectiveValue); // 컨베이어의 실제 물리 방어 약화 수치
                    Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)?.RemainingTurns); // 기계형 변환의 원래 두 적 차례
                } // 명확히 기본 실패와 변환 성공의 경계 검증 종료
            } // 명시 기계형의 실제 검 상태 처리 종료
            finally // 외부 다른 검 사용에 임시 대상이 남지 않도록 실제 자료 정리
            { // 이 실제 검사에서 만든 Unity 에셋 제거
                Object.Destroy(mechanical); // 임시 기계 원본을 게임 본래 데이터와 분리해 폐기
            } // 이 실제 검사에서 만든 Unity 에셋 제거 종료
        } // 발동 실패와 성공 및 기계형 치환의 독립 검증 종료
        yield return new ExitPlayMode(); // 독립 유니티 검사 환경 복구
    } // 실제 검 카드 후속 효과 검사 종료

    [UnityTest] // 실제 카드의 화상과 별도 발생원의 상태를 비교
    public IEnumerator FlameVeinAddsSeparateMagicSusceptibilityOnlyAfterSuccessfulCardBurn() // 실제 검 및 지팡이 화상의 별개 마법 저항 상태
    { // 원래 두 종류 카드 화상 검증 시작
        yield return new EnterPlayMode(); // 실제 상태 적용 및 피해 회로 입구 준비
        using (var vein = new OwnedRelic("FlameVeinDebris")) // 실제 화염 광맥 부스러기 획득
        using (var pulse = new OwnedRelic("VioletPulsingCore")) // 가짜 침식 분류에 반응할 기존 보랏빛 맥동핵 장착
        using (var fight = new Fight("FLAME_SLASH", "FLAME_SLASH", "SMALL_FLAME")) // 원래 명시 화상 카드 두 종류의 실제 게임 사용
        { // 세 가지 실제 상태와 별도 주문 피해 확인
            var ally = fight.Allies[0]; // 실제 아군 카드의 시전자
            var target = fight.Enemies[0]; // 실제 검 카드의 화상 대상
            var immune = fight.Enemies[1]; // 원래 약화 면역의 대조 적
            var standalone = fight.Enemies[2]; // 아군에 귀속됐지만 카드 적용이 아닌 화상 대상
            var wandTarget = fight.Enemies[3]; // 원래 선택지 화상 지팡이의 적
            standalone.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, ally); // 실제 카드가 아닌 직접 상태 부여
            Assert.IsNull(standalone.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)); // 카드 외 별도 화상에서 새 상태 추가 없음
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 원래 적의 약화 면역 준비
            target.ApplyStatusEffect(BattleStatusEffectType.CorrosionResistanceDown, 5, 3, 1, ally); // 방어 전 마법 저항 약화의 별도 기존 효과
            int impacts = 0; // 실제 원래 검의 카드 피해와 가짜 침식의 추가 피해 비교
            target.DamageTaken += (_, _) => impacts++; // 같은 적에게 해당 카드가 실제 피해를 주는 횟수
            yield return fight.Use("FLAME_SLASH", 0); // 원본 검의 성공한 정상 화상 부여
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.Burn)); // 먼저 살아 있는 실제 카드 화상 확인
            Assert.AreEqual(5, target.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)?.EffectiveValue); // 다른 침식과 섞이지 않는 원래 새 수치
            Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)?.RemainingTurns); // 정상 적 차례 한 번만 유지
            Assert.AreEqual(1, impacts); // 별도 저항 약화가 침식 핵의 피해를 잘못 발동하지 않음
            Assert.AreEqual(110, target.PreviewDamage(100, BattleDamageType.Magical, ally).FinalDamage); // 저항이 영이어도 같은 두 5퍼센트포인트가 원본 피해에 합산
            Assert.AreEqual(100, target.PreviewDamage(100, BattleDamageType.Physical, ally).FinalDamage); // 물리 방어 및 원본 피해는 그대로 유지
            yield return fight.Use("FLAME_SLASH", 1); // 원래 약화 면역에서 막히는 직접 화상
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)); // 화상이 적용되지 않으면 가산 상태도 없음
            int seed = FindSeed(24); // 작은 불꽃의 실제 25퍼센트 확률 경계 성공
            fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 마지막 실제 지팡이 카드에 원본 난수 적용
            yield return fight.Use("SMALL_FLAME", 3); // 원본 확률형 지팡이 화상도 명중 뒤 적용
            Assert.AreEqual(5, wandTarget.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)?.EffectiveValue); // 같은 실제 지팡이 상태 적용 경로의 유물 성공
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 적 차례 한 번 종료
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)); // 기존 침식과 별개로 한 적 차례 뒤 만료
            Assert.AreEqual(105, target.PreviewDamage(100, BattleDamageType.Magical, ally).FinalDamage); // 함께 남은 원래 침식 약화만 계속 적용
        } // 세 가지 실제 상태와 별도 주문 피해 확인 종료
        yield return new ExitPlayMode(); // 원래 Unity 독립 검사 정리
    } // 원래 두 종류 카드 화상 검증 종료

    [UnityTest] // 정해진 카드 메타데이터에만 중독 확률 가산
    public IEnumerator BottleBoostsOnlyAuthoredChanceAndLeavesAbsentOrCertainPoisonAlone() // 카드 실제 입력의 30퍼센트 경계와 확정 및 미설정
    { // 실제 카드 원본 필드의 독립 확률 확인 시작
        yield return new EnterPlayMode(); // 실제 출전과 카드 입력 경로 시작
        using (var bottle = new OwnedRelic("BlackSwampWaterBottle")) // 검은 늪물병의 원래 보유 자료
        { // 카드를 쓰면 엔진이 읽는 기존 메타 속성의 테스트 시작
            var chance = MakePoisonCard("POISON_FIXTURE_CHANCE", 20, true); // 실제 20퍼센트 중독 상태를 지닌 합성 카드
            var absent = MakePoisonCard("POISON_FIXTURE_ABSENT", 0, false); // 중독 부여가 정의되지 않은 일반 타격
            var guaranteed = MakePoisonCard("POISON_FIXTURE_CERTAIN", 100, true); // 원래부터 확정인 실제 상태
            try // 세 임시 에셋만 게임 원본과 분리해 정리
            { //같은 원래 적용 코드의 여러 확률 카드 사용 시작
                using var fight = new Fight(new[] { chance, chance, absent, guaranteed }); // 원래 카드 검사의 실제 확률 시도
                int seed = FindSeed(29); // 20에 10퍼센트포인트를 더할 때의 마지막 성공
                fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 실제 손패 입력 뒤 원래 부여 확률만 제어
                yield return fight.Use("POISON_FIXTURE_CHANCE", 0); // 실제 새 성공 범위의 중독 적용
                Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 20퍼센트에서 실패할 수치가 성공
                seed = FindSeed(30); // 같은 늘어난 확률의 첫 실패
                yield return fight.Use("POISON_FIXTURE_CHANCE", 1); // 30은 30퍼센트의 실제 굴림에서 실패
                Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 새 30퍼센트의 상한 경계만 유지
                seed = FindSeed(0); // 잘못 10퍼센트가 생기면 성공할 첫 정수
                yield return fight.Use("POISON_FIXTURE_ABSENT", 2); // 중독 메타 옵션을 끈 원래 순수 타격
                Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Poison)); // 납득할 원본 중독 시도가 없는 일반 공격 유지
                seed = FindSeed(99); // 원래 확정 효과의 최대 정수
                yield return fight.Use("POISON_FIXTURE_CERTAIN", 3); // 기존 상태 확정 카드의 실제 부여
                Assert.IsNotNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Poison)); // 기존 확정 카드에 별도 상태 변경 없음
            } // 같은 원래 적용 코드의 여러 확률 카드 사용 종료
            finally // 실제 원본 카드에서 만든 새 세 검사 자료를 해제
            { // 임시 상태 메타데이터 에셋 정리
                Object.Destroy(chance); // 실제 확률 경계 검사용 카드의 정리
                Object.Destroy(absent); // 미설정 원래 타격 카드의 정리
                Object.Destroy(guaranteed); // 원래 100퍼센트 합성 카드의 정리
            } // 임시 상태 메타데이터 에셋 정리 종료
        } // 카드를 쓰면 엔진이 읽는 기존 메타 속성의 테스트 종료
        yield return new ExitPlayMode(); // 이전 Unity 검사 실행 모드 복귀
    } // 실제 카드 원본 필드의 독립 확률 확인 종료

    private static CardData MakePoisonCard(string id, int chance, bool isAuthored) // 시트에 없는 조합의 실제 엔진용 중독 메타 구성
    { // 기존 카드 자료와 같은 합성 검증 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 동일한 원래 CardData 직렬 클래스
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_{id}\",\"displayName\":\"중독 확률 검사\",\"cardType\":{(int)CardType.Wand},\"targetType\":{(int)CardTargetType.SingleEnemy},\"apCost\":1,\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.Magical},\"effectValue\":5,\"secondaryEffectValue\":1,\"statusEffectType\":{(int)BattleStatusEffectType.Poison},\"statusDuration\":2,\"statusMaximumStacks\":1,\"applyWandHitStatus\":{(isAuthored ? "true" : "false")},\"wandHitStatusChancePercent\":{chance}}}", card); // 원래 직렬화된 상태 적용 필드와 확률 값
        return card; // 독립 실제 손패에서 쓸 검증 원본 반환
    } // 기존 카드 자료와 같은 합성 검증 종료

    private static int FindSeed(int first, Func<int, bool> secondMatches = null) // Unity 원래 백분율의 지정된 첫째와 둘째 비교
    { // 실제 난수 경계의 독립 시드 후보 확인
        for (int seed = 0; seed < 20000; seed++) // 원래 Unity 난수의 일반 초기 구간
        { // 현재 시드의 첫째 및 둘째 정수 확인
            Random.InitState(seed); // 실제 기존 정상 난수 초기화
            if (Random.Range(0, 100) == first && (secondMatches == null || secondMatches(Random.Range(0, 100)))) // 필요한 성공 또는 실패의 원래 두 값
            { // 실제 검증에 쓸 원래 시드 반환
                return seed; // 실제 카드 입력 직전에 복원할 동일 초기값
            } // 실제 검증에 쓸 원래 시드 반환 종료
        } // 현재 시드의 첫째 및 둘째 정수 확인 종료
        throw new InvalidOperationException("원본 전투 난수의 실제 경계 시드 누락"); // 지정 정수의 검증 환경 확인
    } // 실제 난수 경계의 독립 시드 후보 확인 종료

    private sealed class OwnedRelic : IDisposable // 원래 회차 보유에 정확히 한 유물 설치
    { // 현재 실물 원본 부여와 테스트 뒤 상태 복구
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 기존 회차에 들어 있던 실제 객체
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 정상 유물 획득을 다루는 기존 관리자
        private readonly RelicData relic; // 이 실제 검증에서 장착한 실물 원본

        public OwnedRelic(string key) // 실제 원화와 규칙이 담긴 리소스 원본의 보유
        { // 해당 실제 유물 획득부터 검증 시작
            relic = Resources.Load<RelicData>("Relics/" + key); // 도감과 게임에 쓰는 같은 실제 에셋
            Assert.IsNotNull(relic, key); // 검사 전에 원본 실물 자산 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 회차 보유에 성공한 유물만 검사
        } // 해당 실제 유물 획득부터 검증 종료

        public void Dispose() // 이번 검사에서 얻은 원래 유물만 제거
        { // 원래 회차 상태를 유지하는 실제 정리 시작
            manager.TryRemove(relic.RelicId); // 해당 실제 검사 중 추가한 유물의 정확한 아이디 삭제
            if (previous == null) // 원래 실제 회차 보유 객체가 없던 경우 확인
            { // 이번 검사에서 처음 만든 실제 객체 제거
                Object.Destroy(manager.gameObject); // 기존 원본 사용자 소유 회차에 미치는 변경 제외
            } // 이번 검사에서 처음 만든 실제 객체 제거 종료
        } // 원래 회차 상태를 유지하는 실제 정리 종료
    } // 현재 실물 원본 부여와 테스트 뒤 상태 복구 종료
} // 새 세 가지 유물의 전투 확인 종료
