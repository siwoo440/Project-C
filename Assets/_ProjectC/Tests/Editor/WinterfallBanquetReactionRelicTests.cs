using System; // 시험 보관함 정리와 실제 난수 조건
using System.Collections; // 실제 전투와 플레이 전환 대기
using System.Collections.Generic; // 시험 중에 얻은 유물의 회차 상태
using NUnit.Framework; // 실제 피해와 반환 상태의 검증
using UnityEngine; // 실제 유물 원본과 기존 퍼센트 계산 확인
using UnityEngine.TestTools; // 실제 플레이 모드 진입과 종료
using Fight = WinterfallSwordCombatTests.Fight; // 정상 전투 사건과 위치를 가진 기존 검사 환경
using Object = UnityEngine.Object; // 실제로 만든 회차 보관함의 제거
using Random = UnityEngine.Random; // 실제 유물 확률 검사의 고정 난수

public sealed class WinterfallBanquetReactionRelicTests // 초청 전투 유물의 실제 반응 검사
{ // 전투 반응 검사 시작
    [UnityTest] // 살아 있는 적의 정상 약화와 기존 두 거울의 실제 조합
    public IEnumerator MirrorsCopyRealStacksAndDoNotBothApplyTheSameIncomingDebuff() // 기존 한 번의 거울못과 다음 매번의 거울 발동 비교
    { // 거울 뒤의 웃음 검사 시작
        yield return new EnterPlayMode(); // 실제 게임의 상태 반응과 보관함 시작
        CheckMirrors(); // 약화 중첩과 실제 두 보유 유물의 난수 사용 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 거울 뒤의 웃음 검사 종료

    private static void CheckMirrors() // 실제 화상 상태 복제와 이후 독립 확률의 게임 유물 검사
    { // 원본 약화와 두 거울 준비 시작
        using var owned = new OwnedRelics("CloudedMirrorNail", "LaughBehindMirror"); // 게임에 등록된 두 반사 유물 동시 보유
        using var fight = new Fight("RUNE_BURST"); // 실제 등록된 적과 살아 있는 아군이 있는 시작 전투
        var ally = fight.Allies[0]; // 두 번 화상을 받아 중첩될 실제 아군
        var witness = fight.Allies[1]; // 면역 처리로 확률을 소비하지 않을 아군
        var enemy = fight.Enemies[0]; // 실제 약화를 적용하고 되돌려 받는 원래 적
        ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 3); // 발생자 없는 원본 화상과 최초 한 중첩
        ally.GetStatusEffect(BattleStatusEffectType.Burn).DecayBurn(); // 실제 이미 소모된 원본 화상의 피해값 대조
        int first = FindSeed(() => Random.Range(0, 100) < 20 && Random.Range(0, 100) < 40); // 두 거울이 굴리면 모두 성공할 시드
        var usedOneRoll = StateAfterRolls(first, 1); // 흐린 거울못의 첫 확률만 소비한 예상 상태
        Random.InitState(first); // 실제 적이 두 번째 화상 중첩을 부여할 고정 난수
        Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, ally.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 3, 3, enemy)); // 실제 원래 화상의 두 번째 성공 적용
        var original = ally.GetStatusEffect(BattleStatusEffectType.Burn); // 원래 게임에서 진행 중인 유효 화상
        var mirrored = enemy.GetStatusEffect(BattleStatusEffectType.Burn); // 첫 거울이 복제한 실제 적의 화상
        Assert.IsNotNull(mirrored); // 활성 새 약화가 실제로 원래 적에게 돌아왔는지 확인
        Assert.AreNotSame(original, mirrored); // 원본 상태와 분리된 독립 개체 확인
        Assert.AreEqual(2, mirrored.StackCount); // 기존 첫 거울의 실제 두 중첩 복제
        Assert.AreEqual(original.Value, mirrored.Value); // 원래 중첩당 수치 보존
        Assert.AreEqual(original.EffectiveValue, mirrored.EffectiveValue); // 사용된 지속 피해 감소까지 보존
        Assert.AreEqual(original.RemainingTurns, mirrored.RemainingTurns); // 현재 실제 남은 지속 기간 보존
        Assert.AreEqual(usedOneRoll, Random.state); // 첫 성공 이후 다른 거울을 굴리거나 상태를 중복 복제하지 않음 확인
        int second = FindSeed(() => Random.Range(0, 100) < 40); // 웃음의 실제 사십 퍼센트 안에 드는 다음 시드
        var noRoll = StateAfterRolls(second, 0); // 면역으로 막힌 피해에 판정하지 않는 상태
        witness.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 들어온 새 약화를 실제로 막는 원래 면역
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, witness.ApplyStatusEffect(BattleStatusEffectType.Blind, 2, 1, 1, enemy)); // 실제 부여되지 않은 적의 직접 실명
        Assert.AreEqual(noRoll, Random.state); // 이미 면역 처리된 아군에 대해 두 거울 난수 없음
        var usedNextRoll = StateAfterRolls(second, 1); // 웃음 한 번만 실제 소비한 다음 난수 상태
        Random.InitState(second); // 최초 거울못의 기회가 지난 뒤 독립 반사 준비
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 2, 2, 1, enemy)); // 새 실제 적의 실명 효과가 정상 적용
        Assert.AreEqual(2, enemy.GetStatusEffect(BattleStatusEffectType.Blind)?.EffectiveValue); // 웃는 거울이 되돌려준 실제 실명 수치
        Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.Blind)?.StackCount); // 반사 뒤 같은 적과 아군을 재반사하지 않은 확인
        Assert.AreEqual(usedNextRoll, Random.state); // 매 사건 반사의 확률 한 번과 기존 거울 시도 소비 확인
    } // 원본 약화와 두 거울 준비 종료

    [UnityTest] // 실제 공포 적의 지속 피해 사망과 생존 적의 정신력
    public IEnumerator OperaMaskUsesActiveFearAtRealDefeatEvenWithNoDirectAttacker() // 실제 마지막 출처 없는 화상과 살아 있는 적의 정신 상실 검사
    { // 파손된 오페라 가면 검사 시작
        yield return new EnterPlayMode(); // 실제 전투와 적 턴의 플레이 모드
        CheckOperaMask(); // 공포 중 사망과 생존 및 만료된 공포를 대조
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 파손된 오페라 가면 검사 종료

    private static void CheckOperaMask() // 활성 공포인 적의 실제 사망 전후 생존자 변화 확인
    { // 화상에 의한 실제 처치 준비 시작
        using var owned = new OwnedRelics("DamagedOperaMask"); // 파손된 오페라 가면의 정식 원본
        using var fight = new Fight("RUNE_BURST"); // 기존 적 진영과 사망 이벤트가 있는 실전 전투
        var feared = fight.Enemies[0]; // 활성 공포인 채 지속 피해로 죽을 적
        var survivor = fight.Enemies[1]; // 실제 정신력 넷을 잃을 남은 적
        var later = fight.Enemies[2]; // 다른 생존 적의 동시 반응과 만료 상태 대조
        var absent = fight.Enemies[3]; // 이미 사망해 가면의 영향을 받지 않는 적
        absent.TakeDamage(200, BattleDamageType.None); // 회복이나 정신 손실에 앞서 실제 사망한 적
        int before = survivor.CurrentMental; // 처치 전 실제 생존 적의 정신력
        int laterBefore = later.CurrentMental; // 두 번째 실제 생존 적의 원래 정신력
        BattleMentalChangeResult loss = null; // 실제 유물 정신력 사건의 근거
        survivor.MentalChanged += (unit, change) => // 생존 적의 실제 정신 상태 사건 관찰
        { // 유물의 실제 정신 변화 확인 시작
            if (change.Reason == BattleMentalChangeReason.RelicEffect) // 이전 피해와 다른 가면의 실제 원인만 기록
            { // 실제 유물 사건 저장 시작
                loss = change; // 오페라 가면이 적용한 정상 정신력 이벤트 결과 기록
            } // 실제 유물 사건 저장 종료
        }; // 유물의 실제 정신 변화 확인 종료
        feared.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1); // 화상 사망 때 실제로 활성일 공포
        feared.TakeDamage(198, BattleDamageType.None); // 실제 살아 있는 체력 둘까지 피해
        Assert.AreEqual(before, survivor.CurrentMental); // 공포 상태 적이 아직 살아서는 발동하지 않음 확인
        feared.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, fight.Allies[0]); // 직접 공격자 없이 사망시킬 기존 화상 틱
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 진입
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 화상 적의 실제 사망과 유물 처리
        Assert.IsTrue(feared.IsDead); // 상태 피해의 실제 처치 확인
        Assert.AreEqual(before - 4, survivor.CurrentMental); // 살아 있는 다른 적의 실제 정신력 네 점 감소
        Assert.AreEqual(laterBefore - 4, later.CurrentMental); // 두 번째 생존 적에게도 같은 유물 수치 확인
        Assert.AreEqual(-4, loss?.AppliedDelta); // 기본 신체 피해와 별개의 실제 정신 피해 결과 확인
        Assert.IsNull(loss?.SourceUnit); // 직접 공격자가 없는 화상 처치에도 발동 확인
        later.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1); // 남은 적의 실제 과거 공포 상태 준비
        later.GetStatusEffect(BattleStatusEffectType.Fear).AdvanceDuration(); // 사망 전에 이미 기간이 끝난 상태 구성
        later.TakeDamage(200, BattleDamageType.None, fight.Allies[0]); // 만료 공포 적의 정상 직접 피해 사망
        Assert.AreEqual(before - 4, survivor.CurrentMental); // 더 이상 활성 아닌 과거 상태에서는 추가 정신력 감소 없음
    } // 화상에 의한 실제 처치 준비 종료

    [UnityTest] // 실제 적의 실명과 일반 전투 왕관못의 예고 및 실행
    public IEnumerator ColdWindowUsesTheSameLandedDamageAsPreviewAfterTheNormalCrownPenalty() // 실제 양수 직접 피해와 빗나감 및 비활성 실명 비교
    { // 서늘한 창문못 검사 시작
        yield return new EnterPlayMode(); // 실제 적 행동과 원본 유물 인벤토리 시작
        CheckColdWindow(); // 같은 명중의 예고와 실행 수치 대조
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 서늘한 창문못 검사 종료

    private static void CheckColdWindow() // 실제 실명 적 타격의 조우 보정 순서 검사
    { // 두 피해 보정과 살아 있는 공격 대상 준비 시작
        using var owned = new OwnedRelics("NecrateCrownNail", "ColdWindowNail"); // 일반 조우 왕관못과 실명 피해 감소 유물 동시 보유
        using var fight = new Fight("RUNE_BURST"); // 실제 아군과 적 공격 대상 준비
        var ally = fight.Allies[0]; // 공격의 고정 실제 아군 대상
        var enemy = fight.Enemies[0]; // 실명 중이어도 낮은 난수에서 실제 맞힐 적
        ally.GainBlock(300); // 큰 타격 뒤에도 아군이 쓰러지지 않는 원래 방어도
        var landed = MakeEnemyAttack(enemy, ally, () => 0); // 실명 명중률에서도 맞는 고정 적 공격
        int withCrown = Mathf.RoundToInt(landed.Amount * 1.1f); // 기존 일반 전투 왕관못의 방어 전 수치
        Assert.AreEqual(withCrown, landed.PreviewDamage().RawDamage); // 실명이 없을 때 원래 예고 수치 보존
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 1, 2, 1, ally); // 기본 공격에 실제 실명 상태 부여
        int withProtection = Mathf.RoundToInt(withCrown * 0.9f); // 기존 왕관못 계산 다음의 실명 피해 경감 수치
        Assert.AreEqual(withProtection, landed.PreviewDamage().RawDamage); // 실명한 적의 피해 예고에 정확한 순서 적용
        Assert.AreEqual(withProtection, landed.Execute().DamageResult.RawDamage); // 같은 타격의 실제 방어 전 양에 동일 감소 적용
        var missed = MakeEnemyAttack(enemy, ally, () => 99); // 같은 실명 확률에서 실제로 빗나갈 적 공격
        int block = ally.CurrentBlock; // 빗나감의 실제 방어도 비교량
        Assert.IsTrue(missed.Execute().WasMissed); // 일반적인 적 명중 실패의 확인
        Assert.AreEqual(block, ally.CurrentBlock); // 빗나감은 방어도나 체력을 소모하지 않음 확인
        var self = MakeEnemyAttack(enemy, enemy, () => 0); // 아군을 대상으로 하지 않는 실제 공격 개체
        Assert.AreEqual(withCrown, self.AttackAmount); // 적 자신을 때리는 행동에는 아군 보호 유물 미적용
        enemy.GetStatusEffect(BattleStatusEffectType.Blind).AdvanceDuration(); // 다른 효과는 유지하고 실명을 만료시키는 첫 감소
        enemy.GetStatusEffect(BattleStatusEffectType.Blind).AdvanceDuration(); // 상태 목록에는 있지만 실제로 비활성인 실명
        Assert.AreEqual(withCrown, landed.PreviewDamage().RawDamage); // 현재 만료된 실명에 피해 감소 없음
    } // 두 피해 보정과 살아 있는 공격 대상 준비 종료

    private static BattleEnemyAction MakeEnemyAttack(BattleUnitRuntime enemy, BattleUnitRuntime ally, Func<int> roll) // 실제 기존 적 공격과 고정 명중 시드 생성
    { // 기존 단일 적 행동 생성 시작
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 80, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "창문못 검사", 1, 1, 5, 0, percentRoll: roll, battleType: BattleType.Normal); // 현재 일반 조우의 직접 적 공격 구성
    } // 기존 단일 적 행동 생성 종료

    private static int FindSeed(Func<bool> accepts) // 원래 전투 난수로 조건에 맞는 시드 탐색
    { // 실제 난수 시드 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 제한 범위 안의 유효 확률 조건 비교
        { // 현재 난수 준비 시작
            Random.InitState(seed); // 실제 유니티 난수 발생기 상태 설정
            if (accepts()) // 두 유물의 원하는 실제 굴림 경계 확인
            { // 일치한 시드 반환 시작
                return seed; // 실제 전투 사건 직전 재현할 시작점
            } // 일치한 시드 반환 종료
        } // 현재 난수 준비 종료
        throw new InvalidOperationException("Expected reflection rolls were not found."); // 제한 범위에서 실제 난수 실패 경계
    } // 실제 난수 시드 검색 종료

    private static Random.State StateAfterRolls(int seed, int count) // 실제 굴림 횟수를 대조할 유니티 난수 상태
    { // 원본 난수 횟수 준비 시작
        Random.InitState(seed); // 현재 테스트 후보의 원래 실제 난수 상태
        for (int index = 0; index < count; index++) // 유효 사건에서만 쓸 굴림 개수
        { // 실제 확률의 양수 횟수 시작
            Random.Range(0, 100); // 원래 유물이 쓰는 정수 확률 한 번
        } // 실제 확률의 양수 횟수 종료
        return Random.state; // 원본 실제 난수 후보와 비교할 상태
    } // 원본 난수 횟수 준비 종료

    private sealed class OwnedRelics : IDisposable // 현재 실제 전투 유물의 장착 및 정리
    { // 원본 유물 보유 환경 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 테스트 이전 회차 보관함 존재 확인
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 회차 유물 관리자 조회
        private readonly List<RelicData> acquired = new List<RelicData>(); // 이 검사에서 장착한 실제 원본 참조

        internal OwnedRelics(params string[] names) // 실제 전투에 필요한 유물만 획득 처리
        { // 등록 원본의 보유 처리 시작
            foreach (var name in names) // 지정된 원래 리소스 이름들 조회
            { // 실제 유물 장착 시작
                var relic = Resources.Load<RelicData>("Relics/" + name); // 정상 실제 회차 원본 파일
                Assert.IsNotNull(relic, name); // 정식 등록 유물 원본 여부 확인
                Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 원본 획득 처리
                acquired.Add(relic); // 검사 뒤 제거할 이번 원본 참조
            } // 실제 유물 장착 종료
        } // 등록 원본의 보유 처리 종료

        public void Dispose() // 실제 획득분과 테스트 관리자 정리
        { // 전투 유물 보관함 정리 시작
            foreach (var relic in acquired) // 이번 테스트가 장착한 유물만 순회
            { // 실제 테스트 유물 제거 시작
                manager.TryRemove(relic.RelicId); // 이 전투의 임시 유물 보유 해제
            } // 실제 테스트 유물 제거 종료
            if (previous == null) // 테스트 전 보관함이 없었던 경우 확인
            { // 현재 시험의 임시 보관함 제거 시작
                Object.Destroy(manager.gameObject); // 이번 검사에서 만든 보관함 객체 정리
            } // 현재 시험의 임시 보관함 제거 종료
        } // 전투 유물 보관함 정리 종료
    } // 원본 유물 보유 환경 종료
} // 전투 반응 검사 종료
