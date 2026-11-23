using System.Collections; // 실제 카드 연출과 플레이 모드 대기
using System.Collections.Generic; // 순서대로 적용된 원본 타격 수치 기록
using NUnit.Framework; // 실제 비용 및 피해와 상태 결과 검증
using UnityEngine; // 전투 연출 시간 및 원본 자산 사용
using UnityEngine.TestTools; // 실제 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 카드 입력과 진영 흐름 재사용
using Random = UnityEngine.Random; // 독립 명중과 원본 상태 확률 경계 재현

public sealed class WinterfallWandMagicEchoTests // 마법 반향의 실제 카드와 피해 경계 검사
{ // 마법 반향 회귀 검사 시작
    [UnityTest] // 실제 시전자 준비와 지정 두 대상의 독립 피해 검사
    public IEnumerator EchoRepeatsBothChosenHitsWithoutRepeatingCostsOrStatus() // 같은 카드의 두 대상에 절반 기본 피해만 적용 확인
    { // 지정 대상 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 손패와 소유자가 준비되는 모드 시작
        yield return VerifySelectedTargets(); // 취소 및 검 카드 이후 두 대상의 실제 반복 반향 확인
        yield return new ExitPlayMode(); // 전투 해제 뒤 편집 모드 복귀
    } // 지정 대상 실제 전투 검사 종료

    private static IEnumerator VerifySelectedTargets() // 지정, 비용, 부가 상태 및 정신력의 정상 경계 대조
    { // 실제 화염 쇄도와 반향 검사 시작
        using var fight = new Fight("MAGIC_ECHO", "FLAME_SURGE", "CHARGE"); // 원본 준비와 두 대상 및 다른 계열 카드 구성
        var actor = fight.Allies[0]; // 실제 반향를 시전하는 현재 손패 카드의 소유자
        Assert.IsTrue(actor.ApplyPersistentMental(50)); // 직접 타격의 정신 상태 가산 없는 초기화
        yield return fight.Use("MAGIC_ECHO"); // 실제 행동력 두 점을 지불하고 소유자에게 준비 상태 적용
        Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 원본 준비 카드의 정상 실제 비용 확인
        Assert.AreEqual(50, actor.GetStatusEffect(BattleStatusEffectType.WandEcho)?.EffectiveValue); // 플레이 화면에서 확인할 실제 반향 상태 수치
        var pending = fight.View("FLAME_SURGE"); // 공격을 확정하지 않은 정상 두 대상 선택 화면
        pending.OnPointerClick(null); // 상태나 피해를 바꾸지 않고 대상 선택만 시작
        fight.EnemyViews[0].OnPointerClick(null); // 첫 적만 고르고 두 번째 입력 전에 대기
        pending.OnPointerClick(null); // 같은 원본 카드 입력을 다시 눌러 정상 선택 취소
        Assert.IsNotNull(actor.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 확정 전에 취소한 피해 카드가 준비를 소모하지 않음 확인
        yield return fight.Use("CHARGE", 3); // 같은 시전자의 다른 카드 계열 실제 타격
        Assert.IsNotNull(actor.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 검 피해로 지팡이 전용 준비를 소모하지 않음 확인
        var other = fight.Allies[1]; // 실제 공격 소유자가 아닌 준비 가능한 별개 아군
        other.ApplyStatusEffect(BattleStatusEffectType.WandEcho, 50, 1, 1, other); // 다른 아군의 실제 반향 상태 범위 확인 준비
        var first = fight.Enemies[0]; // 첫 번째로 실제 선택할 지정 적
        var second = fight.Enemies[1]; // 두 번째로 실제 선택할 지정 적
        var firstHits = new List<int>(); // 첫 적의 원본과 반향 타격 수치
        var secondHits = new List<int>(); // 두 번째 적의 원본과 반향 타격 수치
        first.DamageTaken += (_, result) => firstHits.Add(result.RawDamage); // 첫 대상의 실제 계산 직전 피해 관찰
        second.DamageTaken += (_, result) => secondHits.Add(result.RawDamage); // 두 번째 대상의 실제 계산 직전 피해 관찰
        int firstBurns = 0; // 첫 적에게 들어온 실제 화상 부여 횟수
        int secondBurns = 0; // 두 번째 적에게 들어온 실제 화상 부여 횟수
        first.StatusEffectResolved += (_, __, type, result) => firstBurns += type == BattleStatusEffectType.Burn ? 1 : 0; // 중첩 제한과 별개인 실제 첫 화상 이벤트 기록
        second.StatusEffectResolved += (_, __, type, result) => secondBurns += type == BattleStatusEffectType.Burn ? 1 : 0; // 중첩 제한과 별개인 실제 두 번째 화상 이벤트 기록
        int seed = FindSeed((firstRoll, secondRoll) => firstRoll < 35 && secondRoll < 35); // 원래 화염 카드의 두 실제 화상 성공 시드
        int uses = 0; // 별도의 카드 사용 신호가 발생하는지 확인할 실제 횟수
        int before = fight.Points.CurrentActionPoints; // 두 대상 공격 확정 직전의 실제 행동력
        int mental = actor.CurrentMental; // 원본 두 타격만 정신력에 더해지는지 볼 가해자 수치
        fight.Controller.CardUsed += (_, __) => // 실제 카드 사용이 확정된 충돌 시점 확인
        { // 원본 타격 확률과 카드 사용 횟수 관찰 시작
            uses++; // 반향 재생과 구별할 카드 사용 횟수 증가
            Random.InitState(seed); // 원본 두 대상의 화상 확률을 동시에 제어
        }; // 원본 타격 확률과 카드 사용 횟수 관찰 종료
        yield return Use(fight, "FLAME_SURGE", 0, 1); // 실제 손패에서 서로 다른 생존 적을 골라 타격
        CollectionAssert.AreEqual(new[] { 9, 4 }, firstHits); // 첫 적에게 원본 아홉과 절반 내림 네 점의 실제 순서 확인
        CollectionAssert.AreEqual(new[] { 9, 4 }, secondHits); // 두 번째 적에게도 같은 반복 타격 확인
        Assert.AreEqual(before - 2, fight.Points.CurrentActionPoints); // 같은 두 점 비용을 한 번만 지불 확인
        Assert.AreEqual(1, uses); // 반사된 두 피해가 자체 카드 사용을 발생시키지 않음 확인
        Assert.AreEqual(1, firstBurns); // 원본 카드의 실제 화상만 첫 적에게 한 번 적용 확인
        Assert.AreEqual(1, secondBurns); // 원본 카드의 실제 화상만 두 번째 적에게 한 번 적용 확인
        Assert.AreEqual(mental + 2, actor.CurrentMental); // 원본의 두 타격 외 추가 정신력 증가 없음 확인
        Assert.AreEqual(49, first.CurrentMental); // 원본 타격의 한 점만 받는 적 정신력 확인
        Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 실제 지팡이 공격이 한 번의 준비를 소모 확인
        Assert.IsNotNull(other.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 같은 파티 다른 아군의 준비 독립 유지 확인
    } // 실제 화염 쇄도와 반향 검사 종료

    [UnityTest] // 빗나간 원본과 독립 명중으로 이어지는 실제 피해 검사
    public IEnumerator MissedOriginalStillReplaysAccuracyWithoutInflictingMentalOrStatus() // 원본 실패 뒤 실제 복제 타격 및 부가 효과 제외 확인
    { // 빗나감 경로의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 손패의 사용과 충돌 모드 시작
        yield return VerifyMissThenHit(); // 원본 빗나감과 다음 독립 명중의 경계 확인
        yield return new ExitPlayMode(); // 실제 전투 정리 뒤 편집 모드 복귀
    } // 빗나감 경로의 실제 전투 검사 종료

    private static IEnumerator VerifyMissThenHit() // 회피가 남아 있는 대상의 두 실제 명중 확률 대조
    { // 일반 공격 실패와 반향 명중 검증 시작
        var run = RelicRunManager.EnsureInstance(); // 마법 통상 피해에 확률 정신 피해를 추가하는 실제 회차
        var stardust = Resources.Load<RelicData>("Relics/BlackStardust"); // 기존 검은 별가루 원본을 실제 리소스에서 조회
        Assert.IsNotNull(stardust); // 기존 마법 피해 유물의 회귀 검사 자산 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, run.TryAcquire(stardust)); // 실제 획득과 함께 일반 마법 피해에 별가루 효과 등록
        using var fight = new Fight("MAGIC_ECHO", "COLD_TOUCH"); // 회피와 둔화를 확인할 원본 지팡이 피해 카드 구성
        using var dispatcher = new BattleEventDispatcher(); // 실제 원본 피해를 유물에 알리는 공용 발행기
        using var signals = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 마법 피해의 실제 공용 효과 연결
        using var relics = new BattleRelicEffectController(dispatcher, run.Inventory, run.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 기존 별가루의 실제 전투 신호 구독
        var actor = fight.Allies[0]; // 정신력 변화 확인에 사용할 실제 마법 피해 소유자
        Assert.IsTrue(actor.ApplyPersistentMental(50)); // 원본 명중 이전의 일반 정신력 고정
        var enemy = fight.Enemies[0]; // 원본이 빗나가지만 반향는 맞는 적
        enemy.ApplyStatusEffect(BattleStatusEffectType.Evasion, 50, 2, 1); // 각각 독립적인 명중 결과를 유도할 실제 회피 증가
        enemy.GainBlock(1); // 반향가 실제 명중 뒤 사용해야 할 기존 적 방어도
        var observed = new List<int>(); // 기존 피해 이벤트의 원본과 반향 타격 관찰
        enemy.DamageTaken += (_, result) => observed.Add(result.RawDamage); // 명중한 타격의 정확한 실제 피해 수치 기록
        yield return fight.Use("MAGIC_ECHO"); // 이미 지불한 원본 반향 준비 카드를 정상 사용
        int seed = FindSeed((first, second) => first >= 50 && second < 50); // 원본 빗나감과 다음 명중을 만드는 실제 두 난수
        Random.InitState(seed); // 원본 명중과 반향 명중만을 확인하는 난수 출발점
        Random.Range(0, 100); // 빗나간 원본 명중의 실제 난수 소비 대조
        Random.Range(0, 100); // 이어서 명중한 반향의 실제 난수 소비 대조
        Random.State afterAccuracy = Random.state; // 별가루의 추가 정신력 확률 전까지의 기대 난수 상태
        fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 원본 충돌에서 각각 독립적인 명중 난수 설정
        yield return Use(fight, "COLD_TOUCH", 0); // 실제 적 지정 원본의 빗나감 이후 같은 적 복제
        CollectionAssert.AreEqual(new[] { 2 }, observed); // 원본 다섯 점 없이 절반 내림 두 점만 실제 명중 확인
        Assert.AreEqual(199, enemy.CurrentHealth); // 원본 실패 뒤 반향 피해에서 실제 방어도가 흡수한 한 점 확인
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 반향가 빗나간 원본의 상태 효과를 새로 부여하지 않는지 확인
        Assert.AreEqual(50, actor.CurrentMental); // 반향 명중의 별도 가해 정신력 증가 없음 확인
        Assert.AreEqual(50, enemy.CurrentMental); // 반향 명중의 별도 피격 정신력 손실 없음 확인
        Assert.AreEqual(afterAccuracy, Random.state); // 별가루가 반향의 별도 정신력 난수를 소비하지 않음 확인
        Assert.IsNull(actor.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 원본의 명중 여부와 무관한 실제 준비 소모 확인
        Assert.IsTrue(run.TryRemove(stardust.RelicId)); // 다음 독립 전투에 추가한 검사 유물의 정상 회수
    } // 일반 공격 실패와 반향 명중 검증 종료

    [UnityTest] // 연쇄 타격, 실제 X 지불 및 아군 턴 종료 만료 검사
    public IEnumerator ChainAndPaidXReuseDamagePlansAndTheBonusExpiresOnCastTurn() // 원본 타격 방식과 지속 기간을 실제 전투에서 검증
    { // 연쇄와 X의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 독립 회차 원본 카드와 전투 연출 모드 시작
        yield return VerifyChainsAndCost(); // 여러 실제 전투에서 타격과 만료 조건을 확인
        yield return new ExitPlayMode(); // 모든 전투를 정리한 뒤 편집 모드 복귀
    } // 연쇄와 X의 실제 전투 검사 종료

    private static IEnumerator VerifyChainsAndCost() // 독립 연쇄의 대상 규칙과 실제 지불 행동력의 피해 검증
    { // 연쇄 타격 및 실사용 행동력 검사 시작
        using (var chain = new Fight("MAGIC_ECHO", "CHAIN_LIGHTNING")) // 같은 생존 적에게 연쇄가 반복 가능한 원본 전투
        { // 세 타격씩 두 피해 구간 확인 시작
            for (int index = 1; index < chain.Enemies.Length; index++) // 실제 원본 명단에서 대상 하나만 남기기
            { // 제외할 실제 적 정리 시작
                chain.Enemies[index].TakeDamage(10000, BattleDamageType.None); // 빈 지원 대상도 포함하지 않도록 실제 적 처치
            } // 제외할 실제 적 정리 종료
            var wounds = new List<int>(); // 연쇄가 원래 세 번과 반복 세 번 적용한 실제 원본 수치
            chain.Enemies[0].DamageTaken += (_, result) => wounds.Add(result.RawDamage); // 남은 적의 각 정상 실제 타격 기록
            yield return chain.Use("MAGIC_ECHO"); // 그 턴 동안 연쇄 카드를 복제할 준비
            yield return chain.Use("CHAIN_LIGHTNING"); // 원래 지팡이의 생존 적 자동 연쇄 실행
            CollectionAssert.AreEqual(new[] { 6, 6, 6, 3, 3, 3 }, wounds); // 같은 살아 있는 적을 선택하는 원래 연쇄의 독립 반복 확인
            Assert.AreEqual(16, chain.Points.CurrentActionPoints); // 마법 반향와 연쇄 번개를 각각 한 번만 지불 확인
        } // 세 타격씩 두 피해 구간 확인 종료
        using (var variable = new Fight("MAGIC_ECHO", "MANA_RELEASE")) // 실제 남은 X를 전부 쓰는 원본 지팡이 사용
        { // 현재 행동력이 영인 재생 결과 확인 시작
            var raw = new List<int>(); // 실제 지불량과 독립 재생으로 맞은 피해 순서
            variable.Enemies[0].DamageTaken += (_, result) => raw.Add(result.RawDamage); // 현재 카드의 정상 원본 수치 기록
            yield return variable.Use("MAGIC_ECHO"); // 원본 시전 비용 두 점 지불
            Assert.IsTrue(variable.Points.Spend(15)); // X 공격의 실제 가용 행동력을 세 점으로 제한
            yield return Use(variable, "MANA_RELEASE", 0); // 남은 세 점을 사용해 지정 적을 타격하는 실제 원본
            CollectionAssert.AreEqual(new[] { 21, 10 }, raw); // 처음 지불한 세 점 곱하기 칠과 절반 내림 확인
            Assert.Zero(variable.Points.CurrentActionPoints); // 비용을 다시 요구하거나 행동력을 환급하지 않는지 확인
        } // 현재 행동력이 영인 재생 결과 확인 종료
        using (var rune = new Fight("MAGIC_ECHO", "RUNE_BURST")) // 표식에 따라 원본 피해가 달라지는 실제 카드
        { // 조건부 시트 수치의 절반 확인 시작
            var marked = rune.Enemies[0]; // 룬 폭발의 원본 추가 피해 조건을 만족할 적
            marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 2, 1); // 실제 표식 분류에 속하는 기존 상태 준비
            var raw = new List<int>(); // 원본 화력과 반향의 조건부 기본 피해 기록
            marked.DamageTaken += (_, result) => raw.Add(result.RawDamage); // 기존 실제 피해 경로에서 원본과 복제 수치 관찰
            rune.Allies[0].ApplyStatusEffect(BattleStatusEffectType.ManaConcentration, 4, 1, 1); // 원래 한 장에만 추가될 기존 마력 집중 준비
            yield return rune.Use("MAGIC_ECHO"); // 표식 지팡이 피해에 적용할 실제 준비
            yield return Use(rune, "RUNE_BURST", 0); // 표식 있는 적의 조건부 피해를 실제로 재생
            CollectionAssert.AreEqual(new[] { 21, 8 }, raw); // 시트의 열한 점과 표식 여섯 점은 양쪽에 포함하고 집중 네 점은 원본에만 적용 확인
            Assert.IsNull(rune.Allies[0].GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 별개 한 번 효과도 원본 피해 카드에서 소모 확인
        } // 조건부 시트 수치의 절반 확인 종료
        using (var terminal = new Fight("MAGIC_ECHO", "CHAIN_LIGHTNING")) // 연쇄 원본이 마지막 생존 적을 처치하는 실제 전투
        { // 실제 대상 고갈 뒤 반향 중단 확인 시작
            var last = terminal.Enemies[0]; // 원본 첫 타격에서 처치될 실제 마지막 적
            last.TakeDamage(last.MaxHealth - 5, BattleDamageType.None); // 원본 한 타격에 체력이 영이 될 마지막 적 준비
            for (int index = 1; index < terminal.Enemies.Length; index++) // 원본 카드 확정 전에 다른 대상의 실제 체력 제거
            { // 교전 종료에 필요 없는 실제 적 처치 시작
                terminal.Enemies[index].TakeDamage(10000, BattleDamageType.None); // 연쇄 이후 대상이 남지 않도록 실제 정리
            } // 교전 종료에 필요 없는 실제 적 처치 종료
            int hits = 0; // 정상 카드로 실제 피해를 입은 전체 횟수
            last.DamageTaken += (_, result) => hits++; // 처치 뒤 추가 피해가 예약되는지 확인할 실제 신호
            yield return terminal.Use("MAGIC_ECHO"); // 아직 생존 적이 한 명인 상태의 실제 반향 준비
            yield return terminal.Use("CHAIN_LIGHTNING"); // 원본 첫 타격에서 마지막 적의 실제 전투불능
            Assert.AreEqual(1, hits); // 남은 원래 연쇄와 반향 타격의 중단 확인
            Assert.AreEqual(BattleResult.Victory, terminal.Turns.Result); // 일반 카드 이동 이후 실제 승리 정산 확인
        } // 실제 대상 고갈 뒤 반향 중단 확인 종료
        using var expire = new Fight("MAGIC_ECHO", "COLD_TOUCH"); // 원본 상태를 사용하지 않고 적 턴으로 넘어갈 전투
        yield return expire.Use("MAGIC_ECHO"); // 현재 아군 턴에만 유지할 준비 부여
        Assert.IsTrue(expire.Turns.EndPlayerTurn()); // 처음 시전한 실제 아군 턴 종료
        Assert.IsNull(expire.Allies[0].GetStatusEffect(BattleStatusEffectType.WandEcho)); // 시전한 아군 턴 종료에서 아직 사용하지 않은 상태 만료 확인
    } // 연쇄 타격 및 실사용 행동력 검사 종료

    private static IEnumerator Use(Fight fight, string id, params int[] targets) // 실제 비용과 상태를 선택별로 관찰할 카드 실행
    { // 손패 카드 및 대상 입력 시작
        var view = fight.View(id); // 현재 실제 손패의 원본 표시 카드
        var selected = view.RuntimeCard; // 손패 이동을 확인할 정확한 개별 카드
        view.OnPointerClick(null); // 원본 카드의 실제 선택
        foreach (int target in targets) // 원래 규칙에 따라 눌러야 하는 지정 적 순회
        { // 실제 적 화면 클릭 시작
            fight.EnemyViews[target].OnPointerClick(null); // 현재 위치의 실제 지정 적 입력
        } // 실제 적 화면 클릭 종료
        float deadline = Time.realtimeSinceStartup + 10f; // 실제 연출 및 피해 정산 완료를 위한 최대 대기 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 기존 카드 처리 애니메이션의 실제 완료 대기
        { // 실제 전투 프레임 대기 시작
            yield return null; // 다른 기능의 대체 호출 없이 다음 화면 갱신 대기
        } // 실제 전투 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 전투 카드의 정상 연출 종료 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, selected); // 한 번 사용된 실제 카드의 버린 더미 이동 확인
    } // 손패 카드 및 대상 입력 종료

    private static int FindSeed(System.Func<int, int, bool> matches) // 실제 처음 두 명중과 상태 결과를 재현할 난수 시드 구하기
    { // 정수 난수 경계 조회 시작
        for (int seed = 0; seed < 10000; seed++) // 부여 확률이나 명중 경계를 만족할 실제 시드 탐색
        { // 각 후보의 처음 두 난수 비교 시작
            Random.InitState(seed); // 후보의 실제 전투 난수 순서 시작
            if (matches(Random.Range(0, 100), Random.Range(0, 100))) // 원래 정수 굴림 두 번의 경계 확인
            { // 재현할 첫 시드 확보 시작
                return seed; // 현재 요구한 난수 순서를 재현하는 실제 시드 반환
            } // 재현할 첫 시드 확보 종료
        } // 각 후보의 처음 두 난수 비교 종료
        throw new AssertionException("마법 반향의 전투 난수 경계 시드 없음"); // 재현이 불가능한 확률 경계 보고
    } // 정수 난수 경계 조회 종료
} // 마법 반향 회귀 검사 종료
