using System.Collections; // 실제 플레이 모드와 공격 연출 대기
using System.Linq; // 살아 있는 대상과 실제 적용한 표식 확인
using System.Reflection; // 별도 편집기 검사에서 내부 계산의 실제 경계 관찰
using NUnit.Framework; // 실제 전투 상태와 피해 비교
using UnityEngine; // 실제 테스트 카드와 유물 원본 조회
using UnityEngine.TestTools; // 아군과 적의 정상 전투 모드 실행
using Fight = WinterfallSwordCombatTests.Fight; // 원래 손패와 턴을 이용하는 실제 게임 검사 환경

public sealed class WinterfallSensorAndAreaRelicTests // 개막과 명중 및 광역 피해의 회귀 검사
{ // 사막 기계 유물의 실제 전투 검사 시작
    [UnityTest] // 실제 시작 시 임의의 기존 적 한 명에게 두 적 턴 표식
    public IEnumerator WatchPodLensMarksExactlyOneEnemyAtOpeningForTwoEnemyTurns() // 실제 적 턴 감소와 아군 턴 제외 검증
    { // 원래 전투 개막 진행 시작
        yield return new EnterPlayMode(); // 실제 전투 시작 동작을 실행할 플레이 모드
        Acquire("WatchPodLens"); // 출시 원본 유물의 실제 회차 보유
        using (var fight = new Fight("RUNE_BURST")) // 정상 시작 절차와 생존 적 네 명을 맡을 실제 전투
        { // 개막과 다음 두 적 차례의 표식 확인 시작
            var targets = fight.Enemies.Where(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark) != null).ToArray(); // 기존 실제 공명 표식을 받은 적 명단
            Assert.AreEqual(1, targets.Length); // 실제 독립 적 한 명에게만 표식 지급 확인
            var marked = targets[0]; // 이제부터 원래 두 적 차례의 표식을 확인할 기존 적
            var status = marked.GetStatusEffect(BattleStatusEffectType.PublicMark); // 일반 교체나 별도 가짜 상태 없는 원래 표식
            Assert.AreEqual(2, status.RemainingTurns); // 시작한 전투에서 실제 적 턴 두 번 기간 확인
            Assert.IsTrue(status.PublicMarkCountsEnemyTurns); // 원래 공명 표식의 적 턴 전용 카운터 사용 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 아군 턴 경계로 다음 적 차례 진입
            Assert.AreEqual(2, status.RemainingTurns); // 아군 턴 종료에서 표식 시간 감소 없음 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 적 차례와 원래 상태 정산
            Assert.AreEqual(1, status.RemainingTurns); // 첫 실제 적 턴 종료 후 남은 턴 계산 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 둘째 실제 적 턴 종료 상태 확인
            Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 두 적 턴까지 적용하고 기존 상태 정상 해제 확인
        } // 개막과 다음 두 적 차례의 표식 확인 종료
        yield return new ExitPlayMode(); // 실제 표식 검사 환경 종료
    } // 원래 전투 개막 진행 종료

    [UnityTest] // 지팡이 직접 명중에만 활성 감전의 고정 기본 피해 가산
    public IEnumerator LeakageCoilAddsFourPerDirectWandHitWithoutAffectingPeriodicOrSwordDamage() // 실제 피해와 순수 예고의 동일한 감전 보정 확인
    { // 감전 상태와 직접 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 유물과 나머지 피해 기능 실행
        yield return VerifyLeakageCoil(); // 모드 전환이 끝난 뒤 생성한 카드로 감전 피해 확인
        yield return new ExitPlayMode(); // 실제 감전 피해 검사 환경 종료
    } // 감전 상태와 직접 피해 검사 종료

    private static IEnumerator VerifyLeakageCoil() // 직접 지팡이와 일반 피해의 독립 비교
    { // 플레이 모드에서 자료와 전투 준비 시작
        Assert.AreEqual(4, Acquire("LeakageCoil").EffectValue); // 현재 출시 누전 코일의 원본 수치 확인
        var wand = DamageCard("DAY280_COIL_WAND", CardType.Wand); // 고정 5 피해와 직접 지팡이 계열의 물리 카드 원본
        var sword = DamageCard("DAY280_COIL_SWORD", CardType.Sword); // 같은 기본 피해의 다른 실제 계열 카드
        try // 테스트용 원본의 항상 해제 보장
        { // 실제 보유 유물의 피해 조건 비교 시작
            using var fight = new Fight(new[] { wand, sword }); // 정상 전투 및 손패로 진입
            var first = fight.Enemies[0]; // 지금 활성 감전이 적용될 생존 적
            var unshocked = fight.Enemies[1]; // 같은 출처라도 감전이 없는 비교 대상
            first.ApplyStatusEffect(BattleStatusEffectType.Shock, 1, 3, 1); // 지금 실제 감전 상태 부여
            Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.Shock)); // 실제 부여된 유효 감전 확인
            var source = fight.View("DAY280_COIL_WAND").RuntimeCard.OwnerUnit; // 실제 지팡이 원본의 아군 공격자
            Assert.AreEqual(9, first.PreviewDamage(5, BattleDamageType.None, source, alliedDirectAttack: true, directCardType: CardType.Wand).AppliedDamage); // 실제 예고에 4점 방어 전 가산 확인
            int enemyHealth = first.CurrentHealth; // 활성 감전의 기존 적 체력을 보관
            int otherHealth = unshocked.CurrentHealth; // 감전이 없는 실제 적의 초기 체력
            yield return fight.Use("DAY280_COIL_WAND"); // 실제 지팡이 공격 카드 한 장을 정상 해결
            Assert.AreEqual(enemyHealth - 9, first.CurrentHealth); // 감전 적의 실제 직접 피해가 예고와 같음 확인
            Assert.AreEqual(otherHealth - 5, unshocked.CurrentHealth); // 감전이 없는 대상의 실제 지팡이 기본 피해 확인
            Assert.AreEqual(5, first.TakeDamage(5, BattleDamageType.None, source).AppliedDamage); // 아군 출처여도 직접 타격이 없는 일반 피해 가산 제외
            enemyHealth = first.CurrentHealth; // 같은 감전 중 독립 비교의 현재 체력
            yield return fight.Use("DAY280_COIL_SWORD"); // 실제 다른 계열 일반 공격 카드 해결
            Assert.AreEqual(enemyHealth - 5, first.CurrentHealth); // 동일 대상의 감전이 있어도 검 피해에 가산 없음 확인
            var sameWand = fight.Deck.Cards.Single(card => card.SourceData == wand); // 이미 사용한 원래 물리 지팡이 참조
            BattleCardDamageContext.Begin(sameWand, new[] { first }); // 같은 실제 지팡이의 둘째 타격용 기존 직접 문맥
            try // 원래 카드 문맥의 항상 해제 보장
            { // 반복 타격의 개별 실제 피해 확인 시작
                Assert.AreEqual(9, first.TakeDamage(5, BattleDamageType.None, source, alliedDirectAttack: true).AppliedDamage); // 감전 중 다음 실제 직접 지팡이에도 가산 확인
            } // 반복 타격의 개별 실제 피해 확인 종료
            finally // 기존 타격의 원래 문맥 정리
            { // 이후 테스트와 다른 직접 피해의 원래 범위 복구
                BattleCardDamageContext.Clear(); // 다음 실제 피해를 독립적으로 계산하는 기본 상태
            } // 이후 테스트와 다른 직접 피해의 원래 범위 복구 완료
        } // 실제 보유 유물의 피해 조건 비교 종료
        finally // 생성한 검사 카드 정리
        { // 현재 테스트 자료만 해제 시작
            Object.Destroy(wand); // 현재 검사 지팡이 원본 해제
            Object.Destroy(sword); // 현재 검사 검 원본 해제
        } // 현재 테스트 자료만 해제 종료
    } // 플레이 모드에서 자료와 전투 준비 종료

    [UnityTest] // 첫 실명 공격 범위 전체와 이어지는 실제 일반 공격 비교
    public IEnumerator HeatSeekerIgnoresBlindDuringOneActualAttackAndPreservesOtherAccuracyModifiers() // 순수 미리 보기와 실제 다중 공격 및 두 번째 빗나감 검증
    { // 첫 실제 실명 공격의 전투 검사 시작
        yield return new EnterPlayMode(); // 정상 명중과 다중 대상의 실제 전투 진입
        Acquire("HeatSeeker"); // 출시 열원 탐색기의 원래 효과 보유
        var earlier = DamageCard("DAY280_HEAT_EARLY", CardType.Sword); // 실명 없이 먼저 실행할 원래 공격 카드
        var first = DamageCard("DAY280_HEAT_FIRST", CardType.Sword); // 실명 상태에서 처음 실제 타격할 카드
        var second = DamageCard("DAY280_HEAT_SECOND", CardType.Sword); // 동일 상태의 두 번째 실제 원래 카드 공격
        try // 테스트에서 만든 카드 원본의 해제 보장
        { // 아군 실제 다중 공격의 범위 비교 시작
            using var fight = new Fight(new[] { earlier, first, second }); // 세 차례 아군의 카드 정산을 실행할 실제 손패
            yield return fight.Use("DAY280_HEAT_EARLY"); // 실명 없는 정상 아군 공격부터 실제 해결
            var firstCard = fight.View("DAY280_HEAT_FIRST").RuntimeCard; // 실명 범위를 확인할 첫 실제 사용할 원본
            var source = firstCard.OwnerUnit; // 초기 공격과 동일한 살아 있는 아군 시전자
            source.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 3, 1); // 보정이 없으면 확정 빗나갈 실제 실명
            source.ApplyStatusEffect(BattleStatusEffectType.Disorientation, 25, 3, 1); // 실명과 별도인 실제 방향 감각 약화
            var dodger = fight.Enemies[2]; // 첫 실제 범위에서도 회피할 생존 적
            dodger.ApplyStatusEffect(BattleStatusEffectType.Evasion, 60, 3, 1); // 미리 보기의 회피와 방향 감각 합산 재현
            Assert.AreEqual(0, BattleHitChance.GetPercent(source, dodger)); // 기존 일반 명중에 두 약화와 회피 유지 확인
            var preview = PreviewHeatSeeker(fight.Turns, firstCard, dodger); // 현재 첫 실명 공격 자격을 순수 조회
            Assert.IsTrue(preview); // 정상 비실명 앞 공격으로 유물 기회를 소비하지 않았는지 확인
            var calculate = typeof(BattleHitChance).GetMethod("GetPercentWithBlindOverride", BindingFlags.Static | BindingFlags.NonPublic); // 일반 회피와 방향 감각을 사용하는 실제 내부 명중률 조회
            Assert.IsNotNull(calculate); // 공개 범위를 바꾸지 않은 원래 명중률 메서드 확인
            Assert.AreEqual(15, calculate.Invoke(null, new object[] { source, dodger, preview })); // 열원 탐색기 보정에서도 다른 두 실제 수치 유지 확인
            Assert.IsTrue(PreviewHeatSeeker(fight.Turns, firstCard, dodger)); // 같은 순수 조회가 실제 전투 단회를 소모하지 않는 확인
            source.RemoveStatusEffect(BattleStatusEffectType.Disorientation); // 실제 사용을 고정 결과로 확인할 방향 약화 해제
            dodger.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 같은 적에게 실제 확정 회피를 새로 등록할 준비
            dodger.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 3, 1); // 실제 첫 범위에서도 성공해야 하는 회피 상태
            int[] initial = fight.Enemies.Select(enemy => enemy.CurrentHealth).ToArray(); // 공격 전 대상별 실제 생명 값 보관
            yield return fight.Use("DAY280_HEAT_FIRST"); // 실제 네 적 공격에 한 번의 실명 보정 적용
            for (int index = 0; index < initial.Length; index++) // 실제 모든 타격과 회피 대조
            { // 같은 공격 범위의 각 대상 결과 검사 시작
                Assert.AreEqual(initial[index] - (index == 2 ? 0 : 5), fight.Enemies[index].CurrentHealth); // 실명만 무시하고 회피한 적의 기존 체력 유지 확인
            } // 같은 공격 범위의 각 대상 결과 검사 종료
            Assert.IsNotNull(source.GetStatusEffect(BattleStatusEffectType.Blind)); // 명중 보정이 원래 실명 상태를 제거하지 않음을 확인
            int[] afterFirst = fight.Enemies.Select(enemy => enemy.CurrentHealth).ToArray(); // 첫 실제 범위 완료 뒤 생명 값 보관
            yield return fight.Use("DAY280_HEAT_SECOND"); // 다음 실제 공격은 일반 실명 명중률로 정산
            CollectionAssert.AreEqual(afterFirst, fight.Enemies.Select(enemy => enemy.CurrentHealth).ToArray()); // 두 번째 공격의 정상 확정 빗나감 검사
        } // 아군 실제 다중 공격의 범위 비교 종료
        finally // 검사 카드 원본 해제 보장
        { // 실제 임시 원본 제거 시작
            Object.Destroy(earlier); // 빗나감 없는 비교용 테스트 원본 해제
            Object.Destroy(first); // 첫 실명 공격의 테스트 원본 해제
            Object.Destroy(second); // 두 번째 실제 공격의 테스트 원본 해제
        } // 실제 임시 원본 제거 종료
        yield return new ExitPlayMode(); // 열원 탐색기 실제 전투 종료
    } // 첫 실제 실명 공격의 전투 검사 종료

    [UnityTest] // 적 전체 대상 공격의 순수 예고와 실제 체력 피해
    public IEnumerator MechanicalBugShellUsesTheSameAreaDamageForPreviewAndActualEnemyAttack() // 명시적인 광역 종류와 실제 원래 피해 감소 순서 비교
    { // 실제 적과 아군의 피해 검사 시작
        yield return new EnterPlayMode(); // 원래 적 예고와 실제 타격 실행 환경 진입
        Assert.AreEqual(10, Acquire("MechanicalBugShell").EffectValue); // 화면에 반영해야 할 실제 원본 경감률 확인
        using (var fight = new Fight("RUNE_BURST")) // 실제 파티와 적 전투 런타임
        { // 원래 상태에 광역을 별도로 계산하는 정상 전투 확인 시작
            var ally = fight.Allies[0]; // 단일과 실제 광역의 체력 피해를 볼 첫 아군
            var enemy = fight.Enemies[0]; // 실제 공격 흐름을 사용하는 적 원본
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 차례에서 실행하는 양수 직접 광역 공격 준비
            var area = Attack(enemy, ally, EnemyTargetRule.AllLiving); // 생존 아군 전체 명시 실제 광역 공격
            var setTargets = typeof(BattleEnemyAction).GetMethod("SetAreaTargets", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 적 행동 계획에서 쓰는 내부 광역 참가 목록 조회
            Assert.IsNotNull(setTargets); // 실제 광역 참가 목록을 연결하는 원래 메서드 확인
            setTargets.Invoke(area, new object[] { fight.Allies }); // 실제 적 행동의 원래 참가 대상 전체 연결
            var single = Attack(enemy, ally, EnemyTargetRule.FirstLiving); // 같은 실제 기본 피해의 일반 단일 공격 예고
            Assert.AreEqual(18, area.PreviewDamage(ally).AppliedDamage); // 원래 20점에서 광역 경감 10%의 예고 확인
            Assert.AreEqual(20, single.PreviewDamage(ally).AppliedDamage); // 같은 원본 단일 공격의 원래 수치 보존 확인
            ally.ApplyStatusEffect(BattleStatusEffectType.DistributedDefense, 20, 3, 1); // 원래 공용 광역 방패의 별도 20% 경감 추가
            Assert.AreEqual(14, area.PreviewDamage(ally).AppliedDamage); // 기존 분산 방어와 껍질의 단일 내림 조합 확인
            int before = ally.CurrentHealth; // 예고로는 변화하지 않은 실제 체력 상태 확인
            area.Execute(); // 기존 적 광역 명중과 실제 피해 처리
            Assert.AreEqual(before - 14, ally.CurrentHealth); // 원래 예고와 동일한 실제 피해 확인
            ally.ApplyPersistentHealth(ally.MaxHealth); // 일반 피해 대조를 위한 원래 현 전투 체력 복구
            Assert.AreEqual(20, ally.TakeDamage(20, BattleDamageType.None, enemy).AppliedDamage); // 적 출처의 비직접 피해에는 광역 유물 미적용 확인
        } // 원래 상태에 광역을 별도로 계산하는 정상 전투 확인 종료
        yield return new ExitPlayMode(); // 실제 광역 적 행동 검증 환경 종료
    } // 실제 적과 아군의 피해 검사 종료

    private static RelicData Acquire(string resourceKey) // 실제 현 회차에 출시 유물을 정상 획득
    { // 해당 원본과 인벤토리 수치 확인 시작
        var data = Resources.Load<RelicData>("Relics/" + resourceKey); // 출시 가져오기 이름으로 현재 유물 로드
        Assert.IsNotNull(data, resourceKey); // 실제 유물 에셋 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(data)); // 원래 인벤토리 획득 동작으로 등록
        return data; // 원본의 실제 효과 표시와 적용값 검사에 쓰는 자료
    } // 해당 원본과 인벤토리 수치 확인 종료

    private static bool PreviewHeatSeeker(BattleTurnRuntime turns, CardInstance card, BattleUnitRuntime target) // 원래 실명 공격의 순수 조회를 별도 검사에서 호출
    { // 내부 메서드의 공개 범위를 유지한 조회 시작
        var method = typeof(BattleTurnRuntime).GetMethod("ResolveHeatSeekerBlindOverride", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 미리 보기와 공격 소모 경계를 가진 원래 메서드
        Assert.IsNotNull(method); // 실제 전투 흐름의 독립 내부 메서드 확인
        return (bool)method.Invoke(turns, new object[] { card, target, true }); // 실제 상태를 바꾸지 않는 미리 보기 요청을 반영
    } // 내부 메서드의 공개 범위를 유지한 조회 종료

    private static CardData DamageCard(string id, CardType type) // 정해진 피해와 모든 실제 적의 명중 비교를 위한 검사 카드
    { // 원래 전투 카드 데이터 구성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 정상 실제 손패로 연결되는 독립 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"CRD_SHEET_{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)type},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.None},\"targetType\":{(int)CardTargetType.AllEnemies},\"effectValue\":5,\"apCost\":1}}", card); // 같은 기본 피해와 원래 전체 적 카드 처리의 원본 설정
        return card; // 검과 지팡이의 차이만 있는 실제 공격 자료
    } // 원래 전투 카드 데이터 구성 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime ally, EnemyTargetRule rule) // 실제 20 기본 피해의 원래 적 행동 생성
    { // 실제 예고 및 공격 해결에서 쓸 동일 입력 구성 시작
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.None, 20, BattleStatusEffectType.None, 1, 1, rule, "광역 피해 검사", 1, 1, 5, 0, percentRoll: () => 50, battleType: BattleType.Normal); // 명중이 확정인 동일 적 행동에서 대상 종류만 설정
    } // 실제 예고 및 공격 해결에서 쓸 동일 입력 구성 종료
} // 사막 기계 유물의 실제 전투 검사 종료
