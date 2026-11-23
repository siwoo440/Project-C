using System.Collections; // 실제 카드 애니메이션과 전투 환경 진행
using NUnit.Framework; // 피해와 회복 및 원본 투영 결과 검사
using UnityEngine; // 실제 출시 유물 원본 조회
using UnityEngine.TestTools; // 실제 플레이 모드에서 정상 경로 실행
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 유닛 및 공용 전투 구성

public sealed class WinterfallRuinedSunAndHeartTests // 276일차 태양문양과 깨진 하트 회귀 검사
{ // 직접 카드와 반사 및 치유 비용 검사 시작
    [UnityTest] // 성배의 실제 정상 타격과 기존 화상 조건 확인
    public IEnumerator RuinedSunGlyphAppliesOnlyToOriginalChaliceHitsOnActiveBurn() // 타격 조건과 순수 피해 예고 대조
    { // 원본 카드와 영향을 받는 적 확인 시작
        yield return new EnterPlayMode(); // 등록 원본으로 실제 전투 시작
        yield return VerifyGlyph(); // 같은 화상에 성배 및 검과 일반 피해를 전달
        yield return new ExitPlayMode(); // 원래 자원 정리와 편집 모드 복귀
    } // 원본 카드와 영향을 받는 적 확인 종료

    private static IEnumerator VerifyGlyph() // 실제 성배 카드를 사용한 화상 피해 비교
    { // 유물과 전투의 원본 확인 시작
        var run = RelicRunManager.EnsureInstance(); // 실제 현재 보유 유물 관리자
        var relic = Acquire(run, "RuinedSunGlyph"); // 해당 원본의 일반 등록으로 보유
        try // 되돌릴 유물 보유 상태의 정리 보장
        { // 성배와 일반 피해의 경계 검사 시작
            using var fight = new Fight("HOLY_WATER", "THRUST"); // 실제 보물 원본 성수와 단일 검
            var caster = fight.Allies[0]; // 두 실제 카드의 소유 아군
            var target = fight.Enemies[0]; // 같은 적으로 순수 예고를 비교
            var cup = fight.View("HOLY_WATER").RuntimeCard; // 등록된 원래 성배 손패 카드
            Assert.AreEqual(CardType.Cup, cup.CardType); // 성배 계열의 실제 원본
            Assert.AreEqual(20, target.PreviewDamage(20, BattleDamageType.Magical, caster, alliedDirectAttack: true, directCardType: CardType.Cup).RawDamage); // 화상이 없을 때 같은 성배 예고
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, target.ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 2, 1, caster)); // 실제 활성 화상을 가진 대상
            Assert.AreEqual(25, target.PreviewDamage(20, BattleDamageType.Magical, caster, alliedDirectAttack: true, directCardType: CardType.Cup).RawDamage); // 화상 적을 향한 순수 성배 예고의 다섯 점
            Assert.AreEqual(20, target.PreviewDamage(20, BattleDamageType.Physical, caster, alliedDirectAttack: true, directCardType: CardType.Sword).RawDamage); // 같은 화상과 원본 검 예고는 유지
            Assert.AreEqual(20, target.TakeDamage(20, BattleDamageType.None, caster).RawDamage); // 단순 출처 피해에는 실제 성배 직접 가산 없음
            BattleCardDamageContext.Begin(cup, new[] { target }); // 성배가 보류된 공격 문맥에서도 반향을 따로 대조
            try // 반향 경로 뒤 기존 문맥 정리 보장
            { // 실제 이차 마법 피해 시작
                Assert.AreEqual(20, target.TakeWandEchoDamage(20, caster).RawDamage); // 일반적인 성배 공격의 문맥이 있어도 반향 가산 제외
            } // 실제 이차 마법 피해 종료
            finally // 원래 카드 사용 전 독립 대상 문맥 정리
            { // 다음 실제 공격의 문맥 초기화 시작
                BattleCardDamageContext.Clear(); // 기존 대상 인덱스를 다음 검증에 남기지 않고 정리
            } // 다음 실제 공격의 문맥 초기화 종료
            int raw = 0; // 정상 실제 카드 타격의 피해를 보관할 수치
            target.DamageResolved += (source, unit, result) => raw = result.RawDamage; // 원래 실제 피해 사건에서 방어 전 값 구독
            int expected = target.PreviewDamage(cup.EffectValue, cup.DamageType, caster, alliedDirectAttack: true, directCardType: CardType.Cup).RawDamage; // 원본 카드 자체 수치의 순수 대상 예고
            yield return fight.Use("HOLY_WATER", 0); // 실제 손패와 대상 표면 입력으로 원래 성배 타격
            Assert.AreEqual(cup.EffectValue + 5, raw); // 양수 실제 명중에서 원본 피해에 다섯 점 추가
            Assert.AreEqual(expected, raw); // 예고의 기준과 실제 카드의 공유 가산 일치
            yield return fight.Use("THRUST", 0); // 같은 활성 화상을 유지한 적에게 실제 검 타격
            Assert.AreEqual(7, raw); // 기본 찌르기의 원래 피해를 그대로 적용
            target.RemoveStatusEffect(BattleStatusEffectType.Burn); // 기존 활성 상태를 확실히 없앤 뒤
            Assert.AreEqual(20, target.PreviewDamage(20, BattleDamageType.Magical, caster, alliedDirectAttack: true, directCardType: CardType.Cup).RawDamage); // 화상 해제 뒤 순수 예고 복원
        } // 성배와 일반 피해의 경계 검사 종료
        finally // 회차 유물 보유 원상 복원
        { // 이번 검사에서만 얻은 실제 유물 제거
            run.TryRemove(relic.RelicId); // 보유함의 정상 처리로 원래 상태 복원
        } // 이번 검사에서만 얻은 실제 유물 제거 종료
    } // 유물과 전투의 원본 확인 종료

    [UnityTest] // 반사량과 원래 반사 조합 및 광역의 실제 공격자 생존 검증
    public IEnumerator BrokenHeartReflectsOnlyOwnDirectHealthLossAndStopsALethalAreaRetaliation() // 실제 체력 손실과 기존 명중 결과 대조
    { // 전투 반사 검사 시작
        yield return new EnterPlayMode(); // 실제 보유와 방어 및 적 공격 환경 준비
        VerifyReflection(); // 체력 손실과 분담을 포함한 원래 적 공격의 효과 검사
        yield return new ExitPlayMode(); // 검사 유물과 임시 전투 정리
    } // 전투 반사 검사 종료

    private static void VerifyReflection() // 실제 두 반사와 광역 공격의 연속 경계 확인
    { // 깨진 하트의 실제 반격 검사 시작
        var run = RelicRunManager.EnsureInstance(); // 실제 현재 보유 경로
        var relic = Acquire(run, "GlassDesertBrokenHeart"); // 체력 반사의 출시 사용자 원본
        try // 실제 보유 유물의 확실한 정리
        { // 연속 실피격과 기존 반격의 전투 시작
            using var fight = new Fight("THRUST"); // 기존 실전 피해 함수의 독립 전투
            var defender = fight.Allies[0]; // 실제 방어도와 최종 체력의 주 대상
            var split = fight.Allies[1]; // 독립 공격을 새로 받은 것이 아닌 피해 분담 대상
            var attacker = fight.Enemies[0]; // 첫 직접 피해의 원래 생존 적
            int enemyStart = attacker.CurrentHealth; // 새 반사 이전 실제 값
            attacker.GainBlock(20); // 반사가 직접 감소시킬 적의 방어도 보존 확인
            defender.GainBlock(10); // 원래 피해 전부 흡수한 적 타격의 방어도
            Assert.Zero(defender.TakeDamage(10, BattleDamageType.None, attacker, enemyDirectAttack: true).AppliedDamage); // 실제 방어도만 사라진 원래 적피격
            Assert.AreEqual(enemyStart, attacker.CurrentHealth); // 체력에 닿지 않으면 유물 역피해가 없음
            defender.TakeDamage(8, BattleDamageType.None, attacker, enemyDirectAttack: true); // 실제 체력 손실이 있어도 내림 시 영인 피격
            defender.TakeDamage(14, BattleDamageType.None, attacker); // 적 출처의 일반 피해로 직접 피격 조건 확인
            Assert.AreEqual(enemyStart, attacker.CurrentHealth); // 두 원래 시나리오 모두 반사 미발동
            defender.ApplyStatusEffect(BattleStatusEffectType.PhysicalRiposte, 50, 1, 1); // 원래 검의 별도 실제 반사 효과
            var hit = defender.TakeDamage(30, BattleDamageType.Physical, attacker, enemyDirectAttack: true); // 충분히 양수인 실제 물리 피격
            Assert.GreaterOrEqual(hit.AppliedDamage, 10); // 기존 피해 방어 뒤 실제 반사가 한 점 이상인 조건
            Assert.AreEqual(enemyStart - hit.AppliedDamage * 50 / 100 - hit.AppliedDamage * 10 / 100, attacker.CurrentHealth); // 기존 검 반사와 새 유물의 개별 내림 및 실제 합계
            Assert.AreEqual(20, attacker.CurrentBlock); // 두 고정 반사가 방어도를 넘는 원래 공통 경로
            Assert.IsNull(defender.GetStatusEffect(BattleStatusEffectType.PhysicalRiposte)); // 기존 일회 반사의 정상 소비
            Assert.IsTrue(defender.ApplyPersistentHealth(defender.MaxHealth)); // 분담 검사를 위한 동일 원래 체력 복원
            int beforeSplit = attacker.CurrentHealth; // 원래 피격 대상만 반사하는 검사 시작 체력
            var linked = defender.TakeDamage(20, BattleDamageType.None, attacker, sharingAlly: split, enemyDirectAttack: true); // 기존 원래 대상과 동료 체력 분담
            Assert.AreEqual(10, linked.AppliedDamage); // 원래 대상의 절반 체력 손실
            Assert.AreEqual(beforeSplit - 1, attacker.CurrentHealth); // 분담 대상의 체력 손실을 중복 반사하지 않음
            Assert.IsTrue(defender.ApplyPersistentHealth(17)); // 사망하는 일반 피격 대상의 실제 남은 체력
            int beforeFatal = attacker.CurrentHealth; // 치명타 시 반사 수치를 계산할 적 체력
            var fatal = defender.TakeDamage(80, BattleDamageType.None, attacker, enemyDirectAttack: true); // 실제 아군이 쓰러지고 다른 동료가 살아 있는 피격
            Assert.AreEqual(17, fatal.AppliedDamage); // 실제 잃은 체력까지로 제한한 양수 피해
            Assert.AreEqual(beforeFatal - 1, attacker.CurrentHealth); // 치명 피해에서도 모두 실제로 잃은 열일곱의 내림
            var areaAttacker = fight.Enemies[1]; // 남은 광역 대상이 있는 실제 다른 적
            areaAttacker.TakeDamage(areaAttacker.CurrentHealth - 1, BattleDamageType.None); // 첫 반사에 쓰러질 적의 실제 체력 준비
            Assert.AreEqual(1, areaAttacker.CurrentHealth); // 실제 적의 남은 체력 한 점 확인
            var untouched = fight.Allies[2]; // 첫 유효 피격 이후 맞지 않을 광역의 남은 아군
            int untouchedHealth = untouched.CurrentHealth; // 실제 두 번째 대상의 피격 전 체력
            var area = new BattleEnemyAction(areaAttacker, split, EnemyActionType.Attack, BattleDamageType.None, 20, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.AllLiving, "반사 광역 검사", 0, 1, 4, 0, percentRoll: () => 0, battleType: BattleType.Normal); // 두 실제 광역 대상과 원래 공격자
            area.SetAreaTargets(new[] { split, untouched }); // 먼저 명중할 생존 아군과 실제 남은 아군을 명단에 등록
            area.Execute(); // 가짜 피해 신호가 아닌 실제 적 공격 실행
            Assert.IsTrue(areaAttacker.IsDead); // 최초 생존 피격의 실제 체력 반사가 공격자를 처치
            Assert.AreEqual(untouchedHealth, untouched.CurrentHealth); // 쓰러진 공격자가 다음 대상에게 피해를 주지 않음
        } // 연속 실피격과 기존 반격의 전투 종료
        finally // 격리 전투를 종료한 뒤 보유 유물 제거 보장
        { // 유물 보유의 실제 원상 복구 시작
            run.TryRemove(relic.RelicId); // 검증 중 실제 장착한 유물만 회수
        } // 유물 보유의 실제 원상 복구 종료
    } // 깨진 하트의 실제 반격 검사 종료

    [UnityTest] // 실제 치유에만 같은 단일 정수 내림으로 두 페널티 적용
    public IEnumerator BrokenHeartCombinesHealingRatesBeforeOneFloorAndLeavesRevivalUntouched() // 원래 치유와 탐사 전/부활의 경계
    { // 실제 아군 회복 복합 검사 시작
        yield return new EnterPlayMode(); // 현재 유물과 실제 치유의 게임 환경 시작
        VerifyHealing(); // 원래 선인장과 새 하트의 전체 정수 곱 검증
        yield return new ExitPlayMode(); // 사용한 현재 Unity 원본 정리
    } // 실제 아군 회복 복합 검사 종료

    private static void VerifyHealing() // 회복 감소의 실제 생존 범위와 영의 처리
    { // 현재 회복 유물의 실제 정산 준비
        var run = RelicRunManager.EnsureInstance(); // 원래 실제 탐사 보유 이용
        var heart = Acquire(run, "GlassDesertBrokenHeart"); // 독립 곱셈을 적용하는 원래 하트
        var cactus = Acquire(run, "DriedCactusSap"); // 앞선 독립 95퍼센트 계수의 선인장즙
        try // 두 보유 원본의 확실한 원상복구
        { // 실제 아군과 적의 회복을 개별 비교
            using var fight = new Fight("THRUST"); // 실제 양쪽 참가자와 진행 중인 전투
            var ally = fight.Allies[0]; // 원래 유물 효과가 적용될 실제 살아 있는 아군
            var enemy = fight.Enemies[0]; // 치유 감소가 적용되지 않는 실제 적 대조
            Assert.IsTrue(ally.ApplyPersistentHealth(20)); // 여유가 충분한 실제 체력 필요량
            Assert.AreEqual(35, ally.RestoreHealth(41)); // 41×0.95×0.90의 단일 내림과 체력 증가량
            Assert.IsTrue(ally.ApplyPersistentHealth(20)); // 기존 수신 회복 증가·감소를 대조할 시작점 복원
            ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 20, 2, 1); // 기존 잔잔한 축복의 양수 수신 계수
            ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 20, 2, 1); // 기존 부정 수신 계수 동시 적용
            Assert.AreEqual(41, ally.RestoreHealth(50)); // 50×1.2×0.8×0.95×0.90의 단일 내림
            enemy.TakeDamage(enemy.CurrentHealth - 60, BattleDamageType.None); // 같은 회복을 받을 적의 실제 시작 체력 준비
            Assert.AreEqual(60, enemy.CurrentHealth); // 실제 적의 정해진 체력 부족 확인
            Assert.AreEqual(41, enemy.RestoreHealth(41)); // 적의 원래 회복 변화 없음
            var fallen = fight.Allies[1]; // 회복과 별개의 실제 부활 검증 대상
            fallen.TakeDamage(fallen.MaxHealth + 10, BattleDamageType.None); // 일반 체력 피해로 실제 전투불능 도달
            Assert.IsTrue(fallen.IsDead); // 정상 부활 전에 쓰러진 상태 확인
            int expected = (fallen.MaxHealth * 25 + 99) / 100; // 기존 25퍼센트 체력 부활 시 올림 기준
            Assert.AreEqual(expected, fallen.Revive(25)); // 원래 부활 체력 복원을 두 비율에서 제외
            Assert.AreEqual(expected, fallen.CurrentHealth); // 진짜 부활 이후 같은 실제 체력
        } // 실제 아군과 적의 회복 개별 비교 종료
        finally // 원래 유물 인벤토리의 복원 보장
        { // 실제 검사에서 얻은 두 원본의 제거 시작
            run.TryRemove(cactus.RelicId); // 실제 선인장즙의 이번 보유 내용 제거
            run.TryRemove(heart.RelicId); // 실제 하트의 이번 보유 내용 제거
        } // 실제 검사에서 얻은 두 원본의 제거 종료
    } // 현재 회복 유물의 실제 정산 종료

    private static RelicData Acquire(RelicRunManager run, string key) // 실제 출시 유물 데이터와 보유 경로
    { // 원본 유물 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 정식 등록과 같은 실제 유니티 리소스
        Assert.IsNotNull(relic, key); // 실제 출시 유물 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, run.TryAcquire(relic)); // 실제 회차 보유 설정
        return relic; // 이 검사에만 추가한 원본의 조회 값
    } // 원본 유물 준비 종료
} // 직접 카드와 반사 및 치유 비용 검사 종료
