using System; // 실제 전투와 임시 자원 정리
using System.Collections; // 플레이 모드 및 카드 연출 대기
using System.Collections.Generic; // 건강 손실과 부활 신호의 실제 기록
using System.Linq; // 배치 적 행동과 손패 원본 조회
using System.Reflection; // 기존 방어 우회 경로의 내부 실행 연결
using NUnit.Framework; // 실제 유물 결과 검증
using UnityEngine; // 전투 원본과 실제 화면 구성
using UnityEngine.TestTools; // 플레이 모드 진입과 종료
using UnityEngine.UI; // 실제 손패 화면용 구성
using Object = UnityEngine.Object; // 임시 화면과 원본 제거
using Random = UnityEngine.Random; // 순환장치 확률의 결정적 경계 검증
using static WinterfallCommonCardExpansionTests; // 실제 카드 사용 및 기존 전투 화면 생성 공통 검사

public sealed class WinterfallAbyssReactiveRelicTests // 260일차 전투 사건 반응 유물 검사
{ // 어비스 전투 유물 검사 시작
    [UnityTest] // 실제 양수 체력 손실에만 확률과 현재 행동력 증가 적용
    public IEnumerator VoidCirculatorRollsOnceForHealthLossAndKeepsTheExistingCap() // 방어와 무효 및 전투 외 상태의 실제 행동력 검사
    { // 순환장치 검사 시작
        yield return new EnterPlayMode(); // 실제 보관함 및 전투 이벤트 시작
        CheckVoidCirculator(); // 실제 체력 손실 경계와 행동력 보상 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 순환장치 검사 종료

    private static void CheckVoidCirculator() // 실제 피해 결과 신호와 행동력 확률 대조
    { // 공허 순환장치 경계 검사 시작
        using var relics = new OwnedRelics("VoidCirculator"); // 실제 공허 순환장치 획득
        using var fight = new Combat(BattleType.Normal, 2); // 보관함과 같은 회차의 실제 전투 구성
        var ally = fight.Allies[0]; // 일반 및 치명 피해의 대조 생존 아군
        var enemy = fight.Enemies[0]; // 일반 체력 손실의 실제 적 출처
        int success = FindSeed(roll => roll < 15); // 실제 유물 확률 성공 값
        int failure = FindSeed(roll => roll >= 15); // 실제 유물 확률 실패 값
        Assert.IsTrue(fight.Points.Spend(2)); // 전투 시작 밖에서 행동력 가산의 빈자리 확보
        var noRoll = StateAfterRolls(success, 0); // 추가 난수를 쓰지 않은 기준 상태
        ally.TakeDamage(2, BattleDamageType.None, enemy); // 시작 전 가능한 원본 유닛 상태 변경
        Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 실제 체력 변화라도 전투 전에 행동력 증가 없음 확인
        Assert.AreEqual(noRoll, Random.state); // 시작 전 체력 손실의 난수 소비 없음 확인
        fight.Start(); // 실제 아군 턴 시작과 유물 연결
        Assert.IsTrue(fight.Points.Spend(2)); // 발동 성공을 받을 현재 행동력 공간
        ally.GainBlock(8); // 일반 적 피해의 전부 흡수 비교용 방어도
        noRoll = StateAfterRolls(success, 0); // 실제 피해가 없는 경우의 난수 상태
        Assert.AreEqual(0, ally.TakeDamage(4, BattleDamageType.None, enemy).AppliedDamage); // 방어도에 모두 흡수된 양수 공격
        Assert.AreEqual(noRoll, Random.state); // 실제 체력 손실 없는 방어도 공격 난수 없음 확인
        ally.ClearBlock(); // 기존 방어도 제거
        ally.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 한 번 완전히 무효화할 기존 보호
        Assert.AreEqual(0, ally.TakeDamage(4, BattleDamageType.None, enemy).AppliedDamage); // 실제 무효 상태에 막힌 양수 피해
        ally.ChangeMental(-3, BattleMentalChangeReason.DisruptReceived, enemy); // 체력과 독립인 실제 정신력 손실
        Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 방어 및 무효와 정신 손실의 행동력 불변 확인
        Assert.AreEqual(noRoll, Random.state); // 위 경로 모두 체력 유물의 확률 미소모 확인
        var nextRoll = StateAfterRolls(failure, 1); // 실제 유효 손실 한 건의 실패 난수 소모 상태
        Random.InitState(failure); // 첫 유효 체력 손실에 확률 실패 적용
        Assert.AreEqual(4, ally.TakeDamage(4, BattleDamageType.None, enemy).AppliedDamage); // 첫 실제 양수 체력 손실 확인
        Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 정상 확률 실패 시 현재 행동력 유지
        Assert.AreEqual(nextRoll, Random.state); // 한 유효 사건의 단일 확률 소비 확인
        nextRoll = StateAfterRolls(success, 1); // 양수 체력 손실의 확률 성공 난수 소비 상태
        Random.InitState(success); // 일반 방어도를 거치지 않는 실제 피해에 성공 값 설정
        Assert.AreEqual(2, ApplyUnblockedDamage(ally, 2, BattleDamageType.None, enemy)); // 분담이나 지속 피해의 실제 체력 손실 경로
        Assert.AreEqual(7, fight.Points.CurrentActionPoints); // 실제 분담 계열 손실 한 번의 현재 행동력 한 점 증가
        Assert.AreEqual(nextRoll, Random.state); // 무방어 경로도 실제 한 사건당 한 번만 굴림
        Random.InitState(success); // 원래 살아 있던 대상의 치명 피해 성공 값
        Assert.IsTrue(ally.TakeDamage(ally.CurrentHealth, BattleDamageType.None, enemy).WasTargetDefeated); // 살아 있을 때 시작한 실제 치명 피해
        Assert.AreEqual(8, fight.Points.CurrentActionPoints); // 치명 손실도 실제 전투 중 한 점 회복 가능 확인
        Assert.AreEqual(8, fight.Points.MaxActionPoints); // 순환장치가 최대 행동력을 바꾸지 않는지 확인
        var survivor = fight.Allies[1]; // 이전 대상이 죽어도 실제 전투가 계속된 아군
        noRoll = StateAfterRolls(success, 0); // 상한에서 추가 난수 없는 기준 상태
        survivor.TakeDamage(2, BattleDamageType.None, enemy); // 행동력 상한에 도달한 뒤 양수 체력 손실
        Assert.AreEqual(8, fight.Points.CurrentActionPoints); // 상한을 넘어 행동력이 증가하지 않는지 확인
        Assert.AreEqual(noRoll, Random.state); // 회복할 공간 없는 경우 확률 미소모 확인
        Assert.IsTrue(fight.Turns.TryEscape()); // 원래 전투 종료 경로로 실제 도주
        Assert.IsTrue(fight.Points.Spend(2)); // 결과 확정 뒤의 행동력 수치를 대조할 빈자리
        survivor.TakeDamage(2, BattleDamageType.None, enemy); // 전투 결과 확정 뒤의 실제 런타임 체력 감소 대조
        Assert.AreEqual(6, fight.Points.CurrentActionPoints); // 전투 밖에서는 행동력 유물 미적용
    } // 공허 순환장치 경계 검사 종료

    [UnityTest] // 실제 카드와 적 예고에서의 조우 등급별 왕관못 검사
    public IEnumerator CrownNailDistinguishesTheBossFromAddsAndBoostsOnlyNormalEnemyAttacks() // 고정 피해와 실제 직접 타격 및 예정 수치 비교
    { // 왕관못 검사 시작
        yield return new EnterPlayMode(); // 적 계획과 플레이 카드 입력용 플레이 모드
        yield return CheckCrownNail(); // 세 조우 등급의 실제 피해와 예고 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 왕관못 검사 종료

    private static IEnumerator CheckCrownNail() // 실제 보스와 소환 구분 및 일반 전투 피해 확인
    { // 왕관못 직접 피해 대조 시작
        using var relics = new OwnedRelics("NecrateCrownNail"); // 실제 네크레이트 왕관못 획득
        foreach (BattleType type in new[] { BattleType.Normal, BattleType.Elite, BattleType.Boss }) // 세 종류의 실제 전투 등급 대조
        { // 개별 조우의 카드와 공격 준비 시작
            using var fight = new Combat(type, 2); // 원본 보스 자리와 추가 적 자리가 있는 실제 전투
            fight.Start(); // 실제 턴의 원본 유물과 적 배치 활성화
            var primary = fight.Enemies[0]; // 보스 조우의 처음부터 배치된 적
            var add = fight.Enemies[1]; // 같은 조우라도 직접 보스가 아닌 두 번째 적
            yield return Play(fight.Card, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 실제 명중 카드로 주 적 타격
            Assert.AreEqual(type == BattleType.Boss ? 188 : 190, primary.CurrentHealth); // 원본 보스에게만 양수 카드 명중 피해 이십 퍼센트 가산
            yield return Play(fight.Card, fight.Hand, fight.EnemyViews[1], fight.Runner, fight.Deck, fight.Points); // 같은 카드로 조우의 다른 적 직접 공격
            Assert.AreEqual(190, add.CurrentHealth); // 보스 조우에 포함된 다른 적의 기본 피해 보존
            int prior = primary.CurrentHealth; // 별도 고정 피해의 실제 체력을 비교할 수치
            ApplyUnblockedDamage(primary, 10, BattleDamageType.Physical, fight.Allies[0]); // 반사와 지속 피해가 쓰는 기존 고정 무방어 경로
            Assert.AreEqual(prior - 10, primary.CurrentHealth); // 직접 카드가 아닌 고정 피해에 왕관못 증가가 없는지 확인
            Assert.AreEqual(2, fight.Actions.PrepareActions()); // 실제 적 행동 계획과 조우 유형 연결
            var attack = fight.Actions.PlannedActions.Single(action => action.Actor == primary); // 주 적이 생성한 실제 공격
            int expected = type == BattleType.Normal ? Mathf.RoundToInt(attack.Amount * 1.1f) : attack.Amount; // 기존 층 보정을 유지한 일반 전투의 십 퍼센트 가산
            Assert.AreEqual(expected, attack.AttackAmount); // 예상과 실행에서 공통인 직접 원본 공격량 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 계획한 실제 적 행동을 실행할 적 턴 전환
            var preview = attack.PreviewDamage(); // 피해 저항과 방어도를 포함한 실제 예고
            var hit = fight.Actions.ExecuteAction(attack).DamageResult; // 원래 적 행동 실행기의 실제 타격
            Assert.AreEqual(expected, hit.RawDamage); // 일반 조우 조건이 실행에도 정확히 한 번 반영
            Assert.AreEqual(preview.AppliedDamage, hit.AppliedDamage); // 전투 예고와 피해 적용의 동일한 수치 확인
        } // 개별 조우의 카드와 공격 준비 종료
    } // 왕관못 직접 피해 대조 종료

    [UnityTest] // 실제 전투불능 알림 뒤 부활의 우선순위와 한 전투 한 번 검사
    public IEnumerator AbyssGrailRevivesTheFirstFallenAllyBeforeDefeatAndChargesTheLivingParty() // 기존 씨앗과 정신력 보호의 실제 조합 대조
    { // 성배 조각 검사 시작
        yield return new EnterPlayMode(); // 단독 플레이 전투의 실제 유물 보관함 시작
        CheckAbyssGrail(); // 실제 부활 순서와 살아 있는 아군의 비용 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 성배 조각 검사 종료

    private static void CheckAbyssGrail() // 실제 아군 사망과 부활 및 전멸 결과 검사
    { // 성배 부활 경계 검사 시작
        using var relics = new OwnedRelics("AbyssGrailShard", "BlackAbyssPlate"); // 실제 성배와 첫 정신 피해 무효 유물 동시 보유
        using (var fight = new Combat(BattleType.Normal, 2)) // 함께 있는 생존 아군의 명시 비용 비교
        { // 첫 아군 사망 및 공동 대가 시작
            fight.Start(); // 실제 아군 턴 진입
            var fallen = fight.Allies[0]; // 실제로 쓰러지고 돌아올 첫 아군
            var survivor = fight.Allies[1]; // 명시적 비용을 같이 내는 남은 아군
            var seen = new List<string>(); // 기존 사망과 부활 알림의 발생 순서 기록
            var costs = new List<int>(); // 직접 정신 피해와 다른 실제 유물 비용만 기록
            fallen.Died += target => seen.Add(target.IsDead ? "dead" : "alive"); // 사망 수신자가 먼저 실제 전투불능을 확인
            fallen.HealthChanged += target => // 기존 체력 변경 신호의 실제 부활 확인
            { // 사망 뒤 체력 복구 알림 시작
                if (seen.Count > 0 && !target.IsDead) // 이미 사망을 알린 아군이 생존한 상태 확인
                { // 실제 부활 시점 기록 시작
                    seen.Add("revived"); // 사망 뒤 양수 체력으로 돌아온 실제 이벤트
                } // 실제 부활 시점 기록 종료
            }; // 사망 뒤 체력 복구 알림 종료
            foreach (var ally in fight.Allies) // 실제 두 아군의 정신력 신호 연결
            { // 부활 비용 관찰 시작
                ally.MentalChanged += (target, change) => // 실제 정신 변화 결과 관찰
                { // 원인별 정신력 확인 시작
                    if (change.Reason == BattleMentalChangeReason.RelicEffect) // 사망과 일반 피해의 정신력 변경 제외
                    { // 명시 비용 기록 시작
                        costs.Add(change.AppliedDelta); // 기존 보호와 구분한 실제 감소량 기록
                    } // 명시 비용 기록 종료
                }; // 원인별 정신력 확인 종료
            } // 부활 비용 관찰 종료
            fallen.ApplyStatusEffect(BattleStatusEffectType.ResurrectionSeed, 80, 2, 1); // 같은 사망에 기존 부활 씨앗의 다른 수치 준비
            int expectedHealth = (fallen.MaxHealth * 30 + 99) / 100; // 올림 정산의 성배 체력 기준값
            int healthBefore = fallen.CurrentHealth; // 치명 피해 전에 사라질 실제 체력
            Assert.AreEqual(healthBefore, ApplyUnblockedDamage(fallen, healthBefore, BattleDamageType.None, fight.Enemies[0])); // 직접 무방어 치명 피해의 실제 처리
            CollectionAssert.AreEqual(new[] { "dead", "revived" }, seen); // 기존 사망 알림 뒤 첫 부활이 정확히 한 번인지 확인
            CollectionAssert.AreEqual(new[] { -10, -10 }, costs); // 되살아난 아군과 남은 아군의 명시 정신력 대가 확인
            Assert.AreEqual(expectedHealth, fallen.CurrentHealth); // 더 높은 부활 씨앗에 덮이지 않는 실제 성배 체력
            Assert.IsNull(fallen.GetStatusEffect(BattleStatusEffectType.ResurrectionSeed)); // 같은 사망에서 이미 소모한 씨앗 미보존 확인
            Assert.AreEqual(BattleResult.None, fight.Turns.Result); // 정상 전투가 부활 뒤 계속되는지 확인
            Assert.AreEqual(0, survivor.ChangeMental(-4, BattleMentalChangeReason.DisruptReceived, fight.Enemies[0]).AppliedDelta); // 명시 비용 뒤에도 첫 직접 정신 피해 보호 보존
            fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None, fight.Enemies[0]); // 같은 전투에서 두 번째 실제 전투불능
            Assert.IsTrue(fallen.IsDead); // 한 전투당 조각이 한 번만 되살리는지 확인
            CollectionAssert.AreEqual(new[] { -10, -10 }, costs); // 두 번째 사망은 정신 대가도 반복하지 않는지 확인
        } // 첫 아군 사망 및 공동 대가 종료
        using var nextBattle = new Combat(BattleType.Normal, 1); // 같은 회차의 다음 전투에서 단독 아군 재구성
        nextBattle.Start(); // 새 전투의 조각 사용 횟수 새로 시작
        var lastAlly = nextBattle.Allies[0]; // 쓰러지면 패배가 확정될 마지막 실제 아군
        lastAlly.TakeDamage(lastAlly.CurrentHealth, BattleDamageType.None, nextBattle.Enemies[0]); // 새 전투의 첫 치명 피해 적용
        Assert.IsFalse(lastAlly.IsDead); // 새 전투의 조각 재사용과 마지막 생존자 부활 확인
        Assert.AreEqual(BattleResult.None, nextBattle.Turns.Result); // 실제 부활 전에 패배를 저장하지 않는지 확인
        lastAlly.TakeDamage(lastAlly.CurrentHealth, BattleDamageType.None, nextBattle.Enemies[0]); // 조각을 사용한 뒤 마지막 아군의 다음 사망
        Assert.AreEqual(BattleResult.Defeat, nextBattle.Turns.Result); // 정상 패배 결과가 이번에는 확정되는지 확인
    } // 성배 부활 경계 검사 종료

    private static int ApplyUnblockedDamage(BattleUnitRuntime unit, int amount, BattleDamageType type, BattleUnitRuntime source) // 방어도가 없는 실제 피해의 내부 전투 경로
    { // 방어 우회 실행 시작
        var apply = typeof(BattleUnitRuntime).GetMethod("TakeUnblockedDamage", BindingFlags.Instance | BindingFlags.NonPublic); // 반사와 지속 피해의 실제 공통 메서드
        Assert.IsNotNull(apply); // 기존 피해 경로 메서드 연결 확인
        return (int)apply.Invoke(unit, new object[] { amount, type, source, false }); // 테스트가 전달한 피해의 실제 처리 결과
    } // 방어 우회 실행 종료

    private static int FindSeed(Predicate<int> accepts) // 정확한 유물 확률 범위의 난수 시드 검색
    { // 실제 난수 검색 시작
        for (int seed = 0; seed < 1000; seed++) // 제한 범위 안에서 확률 경계 후보 비교
        { // 각 고정 난수 시드 준비 시작
            Random.InitState(seed); // 현재 시드의 실제 게임 난수 준비
            if (accepts(Random.Range(0, 100))) // 유물의 정수 확률을 만족하는 값 확인
            { // 재현 가능한 난수 후보 반환 시작
                return seed; // 실제 피해 시점에 설정할 게임 난수 시드
            } // 재현 가능한 난수 후보 반환 종료
        } // 각 고정 난수 시드 준비 종료
        throw new InvalidOperationException("No matching relic roll seed."); // 지정한 확률 범위의 재현 실패
    } // 실제 난수 검색 종료

    private static Random.State StateAfterRolls(int seed, int count) // 원래 난수 소비를 정확히 확인할 상태 준비
    { // 난수 내용 기록 시작
        Random.InitState(seed); // 테스트 피해 시점의 게임 난수 재현
        for (int index = 0; index < count; index++) // 필요한 원래 판정 수만큼 순회
        { // 실제 정수 난수 호출 시작
            Random.Range(0, 100); // 본래 유물과 같은 한 번의 확률 소비
        } // 실제 정수 난수 호출 종료
        return Random.state; // 동일한 후속 상태를 비교할 실제 난수 값
    } // 난수 내용 기록 종료

    private sealed class Combat : IDisposable // 실제 카드와 적 행동 및 전멸 정산의 공유 검사 전투
    { // 임시 실제 전투 시작
        internal readonly BattleUnitRuntime[] Allies; // 한 명 또는 두 명의 실제 아군
        internal readonly BattleUnitRuntime[] Enemies; // 주 적과 두 번째 적의 실제 배치 순서
        internal readonly BattleUnitView[] EnemyViews; // 적을 실제 클릭하는 연결 화면
        internal readonly CardData Card; // 고정 마법 피해를 가진 실제 카드 원본
        internal readonly BattleDeckRuntime Deck; // 일반 사용과 버림의 실제 덱
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(8); // 변동을 검사할 실제 공용 행동력
        internal readonly BattleTurnRuntime Turns; // 실제 조우와 전투 결과 관리자
        internal readonly BattleEnemyActionRuntime Actions; // 적 예고 및 실제 실행 관리자
        internal readonly BattleHandView Hand; // 플레이어 카드 선택 화면
        internal readonly BattleActionSequenceRunner Runner; // 카드 충돌과 이동 연출 관리자
        private readonly BattleCardActionController controller; // 실제 카드 입력과 타격 보정 연결
        private readonly BattleStatusEffectController statuses; // 일반 사망과 상태 알림의 기존 연결
        private readonly GameObject canvas; // 독립 전투 화면
        private readonly EnemyData enemyData; // 고정 테스트 적의 공통 원본
        private readonly DeckData deckData; // 이 검사에서만 만든 원본 덱

        internal Combat(BattleType battleType, int allyCount) // 실제 조우 등급과 참여 인원을 지정한 전투 구성
        { // 실제 전투와 카드 입력 준비 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 일반 게임의 실제 아르카나 원본
            Allies = roster.Characters.Take(allyCount).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 지정한 수의 실제 아군
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 동일 원본으로 구별되는 두 전투 적 생성
            JsonUtility.FromJsonOverwrite($"{{\"maxHealth\":200,\"basicAttackPower\":20,\"damageType\":{(int)BattleDamageType.Magical}}}", enemyData); // 고정 공격과 체력의 실제 적 원본
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData), BattleUnitRuntime.CreateEnemy(enemyData) }; // 첫 자리와 두 번째 자리의 서로 다른 실제 런타임
            Card = ScriptableObject.CreateInstance<CardData>(); // 기존 사용 경로를 지나갈 고정 수치 카드 원본
            JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"DAY260_REACTIVE_TEST\",\"displayName\":\"전투 유물 검사\",\"cardType\":{(int)CardType.Wand},\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)BattleDamageType.Magical},\"effectValue\":10,\"apCost\":1}}", Card); // 차례대로 명중할 고정 마법 열 피해 설정
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(Card, Allies[0].CharacterSource), new DeckCardEntry(Card, Allies[0].CharacterSource) }); // 실제 같은 카드 두 장의 전투 덱 원본
            Deck = BattleDeckRuntime.Create(deckData, Allies, 2, 260); // 같은 원본의 안정된 손패 순서
            canvas = new GameObject("AbyssReactiveRelics", typeof(RectTransform), typeof(Canvas)); // 실제 전투 입력용 임시 캔버스
            var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 카드 클릭 가능 화면
            handObject.transform.SetParent(canvas.transform, false); // 실제 전투 캔버스에 손패 연결
            Hand = handObject.GetComponent<BattleHandView>(); // 실제 손패의 입력 및 덱 바인딩 준비
            var allyViews = Allies.Select(ally => CreateView(canvas.transform, ally)).ToArray(); // 실제 생존 아군 및 사망 표시 연결
            EnemyViews = Enemies.Select(enemy => CreateView(canvas.transform, enemy)).ToArray(); // 실제 클릭할 적 유닛 보기 연결
            Turns = new BattleTurnRuntime(Deck, Points, Allies, Enemies, 0, battleType); // 테스트가 지정한 실제 조우 유형 결합
            Actions = new BattleEnemyActionRuntime(Enemies, Allies, Turns); // 실제 전투 등급의 적 행동 예고와 실행 연결
            Runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 사용의 실제 연출과 충돌 관리
            var processor = new BattleStatusEffectProcessor(); // 상태 종료와 진영 시작효과의 실제 처리
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, processor); // 실제 사망 및 상태 턴의 구독 연결
            Assert.IsTrue(Hand.Bind(Deck, Points, Turns)); // 실제 손패와 턴 및 행동력 연결
            controller = new BattleCardActionController(Deck, Points, Turns, Hand, Runner, processor, allyViews, EnemyViews, Actions); // 실제 카드 선택과 대상 공격 실행기
        } // 실제 전투와 카드 입력 준비 종료

        internal void Start() // 실제 첫 아군 턴과 유물 초기화
        { // 전투 시작 확인 시작
            Assert.IsTrue(Turns.StartBattle(2)); // 실제 두 장 지급과 게임의 시작 효과 처리
        } // 전투 시작 확인 종료

        public void Dispose() // 실제 전투 화면과 내부 이벤트 연결 제거
        { // 독립 실제 전투 정리 시작
            controller.Dispose(); // 카드와 선택 상태 이벤트 해제
            statuses.Dispose(); // 진영 턴과 실제 사망 상태 신호 해제
            Actions.Dispose(); // 적 행동과 조우 컨텍스트 해제
            Hand.Unbind(); // 실제 손패의 행동력과 덱 연결 해제
            Turns.Dispose(); // 원본 턴에 연결된 사망과 피해 이벤트 해제
            Object.Destroy(canvas); // 독립 전투 화면 제거
            Object.Destroy(Card); // 임시 카드 원본 제거
            Object.Destroy(enemyData); // 임시 적 원본 제거
            Object.Destroy(deckData); // 임시 덱 원본 제거
        } // 독립 실제 전투 정리 종료
    } // 임시 실제 전투 종료

    private sealed class OwnedRelics : IDisposable // 실제 보유 유물과 검사 전 난수 복원
    { // 회차 유물 준비 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 게임의 이전 보관함 확인
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 유물 보관함 사용
        private readonly List<RelicData> acquired = new List<RelicData>(); // 이번 검사에 추가한 원본 유물
        private readonly Random.State state = Random.state; // 검사 전 실제 게임의 난수 상태

        internal OwnedRelics(params string[] names) // 지정한 실물 원본 유물을 한 회차에 준비
        { // 원본 유물 획득 시작
            foreach (var name in names) // 검사에서 사용할 원본 이름 순회
            { // 검사 유물 원본 조회 시작
                var relic = Resources.Load<RelicData>("Relics/" + name); // 사용자와 같은 실제 획득 유물 원본
                Assert.IsNotNull(relic, name); // 등록 원본 경로 확인
                Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 회차 보관함의 원본 보유 획득
                acquired.Add(relic); // 검사 뒤 제거할 이번 유물 기록
            } // 검사 유물 원본 조회 종료
        } // 원본 유물 획득 종료

        public void Dispose() // 시험에서 얻은 유물과 게임 상태 복원
        { // 검사 유물 원상 정리 시작
            foreach (var relic in acquired) // 실제 추가했던 유물만 순회
            { // 획득 유물 제거 시작
                manager.TryRemove(relic.RelicId); // 이번 검사의 회차 유물 제거
            } // 획득 유물 제거 종료
            if (previous == null) // 검사에서 새로 만든 보관함인지 확인
            { // 임시 관리 객체 정리 시작
                Object.Destroy(manager.gameObject); // 원래 없던 유물 관리자만 제거
            } // 임시 관리 객체 정리 종료
            Random.state = state; // 게임의 검사 전 원래 난수 상태 복원
        } // 검사 유물 원상 정리 종료
    } // 회차 유물 준비 종료
} // 어비스 전투 유물 검사 종료
