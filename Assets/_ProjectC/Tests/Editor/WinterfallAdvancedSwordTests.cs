using System.Collections; // 플레이 모드 검사 대기
using System.Linq; // 실제 카드와 대상 목록 조회
using NUnit.Framework; // 결과 대조
using UnityEngine; // 실제 게임 난수와 화면 사용
using UnityEngine.TestTools; // 전투 프레임 대기
using Fight = WinterfallSwordCombatTests.Fight; // 실제 보물 카드로 구성하는 전투 검사 환경

public sealed class WinterfallAdvancedSwordTests // 추가된 검 카드 여덟 종의 전투 검사
{ // 검 카드 검사 시작
    [UnityTest] // 복수 타격과 소유자의 준비 상태 검사
    public IEnumerator PreparedSwordAddsThreeToEveryHitOfOneCard() // 준비 갱신과 전체 타격 및 미사용 만료 확인
    { // 정비 검사 시작
        yield return new EnterPlayMode(); // 게임 손패 입력 시작
        using (var fight = new Fight("BLADE_MAINTENANCE", "BLADE_MAINTENANCE", "BLADE_MAINTENANCE", "FRONTLINE_ORDER", "TWIN_BLADE_FLURRY", "DOUBLE_SLASH")) // 실제 보물 원본과 사용 순서 준비
        { // 정비 전투 시작
            var actor = fight.Allies[0]; // 카드 소유자 조회
            var ally = fight.Allies[1]; // 전열 정돈으로 보호할 동료 조회
            yield return fight.Use("BLADE_MAINTENANCE"); // 이번 턴 검 피해 준비
            yield return fight.Use("BLADE_MAINTENANCE"); // 재사용으로 세 점의 보너스 갱신
            Assert.AreEqual(3, actor.GetStatusEffect(BattleStatusEffectType.BladeMaintenance).EffectiveValue); // 중복 사용의 피해 증가 누적 방지
            var allyView = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == ally); // 실제 대상 동료 화면 조회
            yield return WinterfallCommonCardExpansionTests.Play(fight.View("FRONTLINE_ORDER").RuntimeCard.SourceData, fight.Hand, allyView, fight.Runner, fight.Deck, fight.Points); // 전열 정돈을 동료에게 사용
            Assert.AreEqual(5, ally.CurrentBlock); // 선택한 동료의 방어도 다섯 획득 확인
            Assert.AreEqual(1, ally.AttackPowerBonus); // 선택한 동료의 공격력 한 점 확인
            Assert.IsNotNull(actor.GetStatusEffect(BattleStatusEffectType.BladeMaintenance)); // 피해 없는 검 사용 뒤 준비 상태 확인
            fight.Enemies[1].TakeDamage(999, BattleDamageType.None); // 무작위 후보에서 사망한 적 제외
            fight.Enemies[2].TakeDamage(999, BattleDamageType.None); // 생존 후보 추가 정리
            fight.Enemies[0].TakeDamage(999, BattleDamageType.None); // 앞 세 칸을 제외한 마지막 생존 적 재선택 준비
            yield return fight.Use("TWIN_BLADE_FLURRY"); // 남은 적에게 네 번 무작위 타격
            Assert.AreEqual(172, fight.Enemies[3].CurrentHealth); // 네 번째 적에게 네 타격 각각 사에 삼을 더한 피해 스물여덟 확인
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.BladeMaintenance)); // 카드 한 장을 사용한 뒤 준비 소진 확인
            yield return fight.Use("DOUBLE_SLASH", 3); // 준비가 없는 다음 연속 공격
            Assert.AreEqual(164, fight.Enemies[3].CurrentHealth); // 다음 카드의 두 타격은 원래 피해 적용 확인
            yield return fight.Use("BLADE_MAINTENANCE"); // 미사용 상태의 턴 종료 만료 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 종료
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.BladeMaintenance)); // 다음 턴으로 넘어가지 않는 준비 효과 확인
        } // 정비 전투 종료
        yield return new ExitPlayMode(); // 게임 손패 입력 종료
    } // 정비 검사 종료

    [UnityTest] // 공격별 체력 경계와 방어도 관통 검사
    public IEnumerator ExecutionAndPiercingRespectExactHealthAndShield() // 절반 경계와 관통하는 방어도만 보존
    { // 조건부 공격 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 시작
        using (var fight = new Fight("EXECUTION_SLASH", "PIERCING_LUNGE", "PIERCING_LUNGE")) // 조건부 공격과 두 차례의 관통 공격 준비
        { // 조건부 공격 전투 시작
            var foe = fight.Enemies[0]; // 최대 체력 이백의 무방어 적 조회
            var execution = fight.View("EXECUTION_SLASH").RuntimeCard; // 처형 베기 실제 손패 조회
            var notReady = fight.Points.CurrentActionPoints; // 잘못된 대상 시 비용 보존 확인
            fight.View("EXECUTION_SLASH").OnPointerClick(null); // 전원 체력이 절반보다 많은 상태에서 선택
            fight.EnemyViews[0].OnPointerClick(null); // 조건을 만족하지 않는 적에게 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(execution)); // 무효 입력 후 카드 미소모 확인
            Assert.AreEqual(notReady, fight.Points.CurrentActionPoints); // 무효 입력 후 행동력 미소모 확인
            foe.GainBlock(10); // 일시 방어도 열 점 준비
            yield return fight.Use("PIERCING_LUNGE", 0); // 방어도 셋을 이번 타격에서 건너뜀
            Assert.AreEqual(195, foe.CurrentHealth); // 물리 피해 십이 중 방어도 일곱만 흡수
            Assert.AreEqual(3, foe.CurrentBlock); // 건너뛴 방어도 셋 보존 확인
            yield return fight.Use("PIERCING_LUNGE", 0); // 남은 방어도가 삼일 때 관통 수량 내림
            Assert.AreEqual(186, foe.CurrentHealth); // 관통 영과 방어도 세 점 흡수 확인
            foe.TakeDamage(85, BattleDamageType.None); // 절반보다 한 점 많은 체력 준비
            fight.View("EXECUTION_SLASH").OnPointerClick(null); // 여전히 부족한 처형 조건 확인
            fight.EnemyViews[0].OnPointerClick(null); // 체력 백일에 무효 공격 입력
            Assert.IsTrue(fight.Deck.Hand.Contains(execution)); // 절반 초과 적에게 카드 미소모 확인
            foe.TakeDamage(1, BattleDamageType.None); // 정확한 절반 체력으로 변경
            yield return fight.Use("EXECUTION_SLASH", 0); // 절반인 적에 실제 처형 베기
            Assert.AreEqual(82, foe.CurrentHealth); // 처형 피해 십팔 확인
        } // 조건부 공격 전투 종료
        yield return new ExitPlayMode(); // 실제 카드 입력 종료
    } // 조건부 공격 검사 종료

    [UnityTest] // 반사의 공격 종류와 실제 체력 손실 검사
    public IEnumerator ReflectionOnlyReturnsOneDirectSurvivingPhysicalLoss() // 방어도 흡수와 무효 및 분담 피해 제외
    { // 검격 반사 검사 시작
        yield return new EnterPlayMode(); // 실제 상태 사용 시작
        using (var fight = new Fight("PHYSICAL_RIPOSTE", "PHYSICAL_RIPOSTE", "PHYSICAL_RIPOSTE")) // 반사와 미발동 상태 만료 준비
        { // 반사 전투 시작
            var actor = fight.Allies[0]; // 반격 준비 소유자 조회
            var enemy = fight.Enemies[0]; // 실제 공격자 조회
            yield return fight.Use("PHYSICAL_RIPOSTE"); // 첫 반사 준비
            enemy.GainBlock(99); // 고정 반격의 방어도 무시 검증 준비
            actor.GainBlock(10); // 완전히 막을 첫 공격 준비
            actor.TakeDamage(5, BattleDamageType.Physical, enemy); // 전부 방어도로 흡수한 공격
            actor.RemoveBlock(actor.CurrentBlock); // 다음 공격의 직접 피해 준비
            actor.TakeDamage(8, BattleDamageType.Magical, enemy); // 마법 공격으로 반격 제외
            actor.TakeDamage(10, BattleDamageType.Physical, enemy, preventHealthDamage: () => true); // 군번줄이 직접 체력 손실을 막은 경로
            fight.Allies[1].TakeDamage(10, BattleDamageType.Physical, enemy, actor); // 동료가 받은 공격에서 분담된 체력 손실
            Assert.IsNotNull(actor.GetStatusEffect(BattleStatusEffectType.PhysicalRiposte)); // 모두 반격 기회 미소모 확인
            var received = actor.TakeDamage(13, BattleDamageType.Physical, enemy); // 생존하는 직접 물리 공격 수신
            Assert.Greater(received.AppliedDamage, 0); // 실제 손실 발생 확인
            Assert.AreEqual(200 - received.AppliedDamage / 2, enemy.CurrentHealth); // 내림한 반격 고정 피해 확인
            Assert.AreEqual(99, enemy.CurrentBlock); // 공격자 방어도 보존 확인
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.PhysicalRiposte)); // 최초 유효 피격에서 한 번 소모 확인
            yield return fight.Use("PHYSICAL_RIPOSTE"); // 이번에는 피해 없는 턴 준비
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 턴으로 전환
            Assert.IsNotNull(actor.GetStatusEffect(BattleStatusEffectType.PhysicalRiposte)); // 적 턴에 반사 상태 유지 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴으로 전환
            Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.PhysicalRiposte)); // 다음 아군 턴 시작에서 미사용 반사 만료 확인
            yield return fight.Use("PHYSICAL_RIPOSTE"); // 치명적 피해의 반사 제외 준비
            int before = enemy.CurrentHealth; // 공격자 체력 기록
            actor.TakeDamage(999, BattleDamageType.Physical, enemy); // 준비한 소유자가 생존하지 못하는 피격
            Assert.AreEqual(before, enemy.CurrentHealth); // 사망한 반격자의 고정 피해 없음 확인
        } // 반사 전투 종료
        yield return new ExitPlayMode(); // 실제 상태 사용 종료
    } // 검격 반사 검사 종료

    [UnityTest] // 실제 난수의 확률 경계와 대상별 출혈 검사
    public IEnumerator VitalAimAndBloodGaleUseIndependentHitChances() // 급소 치명과 여러 대상의 출혈 부여 확인
    { // 확률 검사 시작
        yield return new EnterPlayMode(); // 실제 타격 난수 시작
        using (var fight = new Fight("VITAL_AIM", "VITAL_AIM", "BLOOD_GALE")) // 치명 경계 두 번과 전체 적 출혈 준비
        { // 확률 전투 시작
            int seed = FindSeed(9); // 십 퍼센트 치명의 마지막 성공 값
            fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 타격 직전 실제 게임 난수 초기화
            yield return fight.Use("VITAL_AIM", 0); // 이번 카드 치명 추가 확률 발동
            Assert.AreEqual(191, fight.Enemies[0].CurrentHealth); // 여섯 피해의 일점오 배 확인
            seed = FindSeed(10); // 십 퍼센트 치명의 첫 실패 값
            yield return fight.Use("VITAL_AIM", 0); // 치명 밖의 일반 타격
            Assert.AreEqual(185, fight.Enemies[0].CurrentHealth); // 여섯 기본 피해 확인
            seed = Enumerable.Range(0, 1000).First(probe => MixedBleedRolls(probe)); // 네 대상 중 성공과 실패가 모두 있는 시드
            Random.InitState(seed); // 대상별 예상 발동 순서 준비
            var expected = fight.Enemies.Select(enemy => Random.Range(0, 100) < 30).ToArray(); // 독립 네 번의 출혈 판정 기록
            var previous = fight.Enemies.Select(enemy => enemy.CurrentHealth).ToArray(); // 피해 전 적별 체력 기록
            yield return fight.Use("BLOOD_GALE"); // 실제 전체 적 타격 및 출혈
            for (int index = 0; index < fight.Enemies.Length; index++) // 피해 대상별 결과 비교
            { // 대상별 검사 시작
                var enemy = fight.Enemies[index]; // 해당 위치의 실제 적 조회
                Assert.AreEqual(previous[index] - 6, enemy.CurrentHealth); // 적마다 직접 피해 여섯 확인
                var bleed = enemy.GetStatusEffect(BattleStatusEffectType.Bleed); // 해당 적의 실제 출혈 조회
                Assert.AreEqual(expected[index], bleed != null); // 적마다 별도로 판정한 삼십 퍼센트 확인
                if (bleed != null) // 성공 대상의 출혈 수치 확인
                { // 출혈 검사 시작
                    Assert.AreEqual(2, bleed.EffectiveValue); // 기존 고정 출혈 피해 두 점 확인
                    Assert.AreEqual(2, bleed.RemainingTurns); // 기존 두 턴 지속 확인
                } // 출혈 검사 종료
            } // 대상별 검사 종료
        } // 확률 전투 종료
        yield return new ExitPlayMode(); // 실제 타격 난수 종료
    } // 확률 검사 종료
    [UnityTest] // 칼날 정비와 첫 검 명중 유물의 합산 검사
    public IEnumerator FirstSwordRelicAddsTwoOnceWhilePreparationAddsThreePerHit() // 검집과 준비 효과의 서로 다른 적용 범위 확인
    { // 실제 유물 전투 시작
        yield return new EnterPlayMode(); // 유물을 지급할 실제 회차 시작
        var relics = RelicRunManager.EnsureInstance(); // 회차 유물 관리자 조회
        var scabbard = Resources.Load<RelicData>("Relics/RustyScabbard"); // 원본 녹슨 검집 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(scabbard)); // 이번 회차의 첫 검 피해 보너스 지급
        try // 검사 뒤 지급한 유물 제거 보장
        { // 중복 효과 검사 시작
            using (var fight = new Fight("BLADE_MAINTENANCE", "TWIN_BLADE_FLURRY", "BLADE_MAINTENANCE", "DOUBLE_SLASH")) // 누적 타격과 완전 빗나감 비교
            { // 실제 카드 입력 시작
                yield return fight.Use("BLADE_MAINTENANCE"); // 네 타격 전체에 쓸 준비 효과
                int previousHealth = fight.Enemies.Sum(unit => unit.CurrentHealth); // 전투 전 적 전체 체력 합계
                yield return fight.Use("TWIN_BLADE_FLURRY"); // 검집과 칼날 정비가 겹친 한 장 사용
                Assert.AreEqual(30, previousHealth - fight.Enemies.Sum(unit => unit.CurrentHealth)); // 기본 사와 정비 삼을 네 번 적용한 뒤 검집 이를 한 번 가산
                yield return fight.Use("BLADE_MAINTENANCE"); // 빗나갈 검 피해 카드 준비
                fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 확정 빗나감 준비
                previousHealth = fight.Enemies[0].CurrentHealth; // 피격 전 적 체력 보관
                yield return fight.Use("DOUBLE_SLASH", 0); // 두 타격이 모두 빗나가는 검 사용
                Assert.AreEqual(previousHealth, fight.Enemies[0].CurrentHealth); // 빗나간 카드의 체력 피해 없음 확인
                Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.BladeMaintenance)); // 미명중이어도 사용한 준비 한 번 소모
            } // 실제 카드 입력 종료
        } // 중복 효과 검사 종료
        finally // 임시 지급 유물 정리
        { // 회차 정리 시작
            relics.Inventory.TryRemove(scabbard.RelicId); // 검사에서 지급한 녹슨 검집 제거
        } // 회차 정리 종료
        yield return new ExitPlayMode(); // 전투 화면 종료
    } // 실제 유물 전투 종료

    [Test] // 상태 도감에서 두 실물 원화와 수치 확인
    public void StatusCodexUsesBothNewIconsAndTheirSheetValues() // 도감 사진과 실제 전투의 수치 대조
    { // 도감 검사 시작
        var entries = CodexData.Load(4); // 게임의 상태 분류 열기
        foreach (var effect in new[] { BattleStatusEffectType.BladeMaintenance, BattleStatusEffectType.PhysicalRiposte }) // 준비와 반격 상태 순회
        { // 개별 도감 확인 시작
            var entry = entries.Single(item => item.Name == BattleStatusEffectInstance.GetDisplayName(effect)); // 실제 표시 이름과 도감 연결
            Assert.AreSame(BattleStatusIconArt.Get(effect), entry.Image); // 전투 상태와 도감의 같은 실물 원화 확인
            Assert.AreEqual(effect + "StatusIcon", entry.Image.texture.name); // 상태별 독립 이미지 확인
            StringAssert.Contains(effect == BattleStatusEffectType.BladeMaintenance ? "3" : "50", entry.Description); // 도감의 실제 정비 피해와 반격 비율 확인
        } // 개별 도감 확인 종료
    } // 도감 검사 종료

    private static int FindSeed(int value) // 원하는 첫 백분율 난수의 시드 검색
    { // 시드 검색 시작
        return Enumerable.Range(0, 5000).First(seed => FirstRoll(seed) == value); // 실제 난수로 경계 시드 반환
    } // 시드 검색 종료
    private static int FirstRoll(int seed) // 시드별 첫 난수 조회
    { // 첫 난수 시작
        Random.InitState(seed); // 게임 난수 복원
        return Random.Range(0, 100); // 첫 확률 값 반환
    } // 첫 난수 종료
    private static bool MixedBleedRolls(int seed) // 네 대상의 성공과 실패가 섞이는지 확인
    { // 혼합 판정 시작
        Random.InitState(seed); // 출혈 시드 복원
        var rolls = Enumerable.Range(0, 4).Select(index => Random.Range(0, 100) < 30).ToArray(); // 네 번 독립 추첨
        return rolls.Any(value => value) && rolls.Any(value => !value); // 성공과 실패 동시 발생 확인
    } // 혼합 판정 종료
} // 검 카드 검사 종료
