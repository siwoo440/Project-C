using System; // 실제 장착 유물 범위 정리
using System.Collections; // 실제 손패 타격 코루틴
using System.Collections.Generic; // 여러 적과 타격별 실제 피해값 수집
using System.Linq; // 등록 유물과 실제 원본 조회
using NUnit.Framework; // 피해 예고와 실제 결과 확인
using UnityEngine; // 정식 등록 유물 조회
using UnityEngine.TestTools; // 실제 전투 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 실제 아군 덱과 전투 카드 입력 환경
using Object = UnityEngine.Object; // 검사 전용 회차 관리자 정리

public sealed class WinterfallDesertOutgoingRelicTests // 사막의 세 직접 공격 유물 검사
{ // 사막 공격 유물 검사 시작
    [UnityTest] // 실명 마법과 저체력 조건의 예고 및 실제 타격 검사
    public IEnumerator LensAndStrapShareTheSameDirectPreviewAndDamageBeforeDefense() // 정확한 절반 경계와 피해 종류 및 활성 실명 확인
    { // 등록된 두 공격 유물의 실제 전투 시작
        yield return new EnterPlayMode(); // 실제 덱과 유물 경로 실행
        using (var held = new OwnedRelics(RelicEffectType.GlassSandLensMagicVsBlind, RelicEffectType.RuinedLeatherStrapLowHealthEnemyDamage)) // 새 등록 유물 두 개의 실제 보유
        { // 실제 수치 및 피해 종류 검사 시작
            yield return CheckLensAndStrap(); // 순수 예고와 카드 실제 명중 확인
        } // 실제 수치 및 피해 종류 검사 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 등록된 두 공격 유물의 실제 전투 종료

    private static IEnumerator CheckLensAndStrap() // 서로 다른 체력과 상태 가진 적의 실제 피해 검사
    { // 선택된 세 적 준비 시작
        using var fight = new Fight("RUNE_BURST", "RUNE_BURST", "RUNE_BURST"); // 일반 마법 피해 열하나의 실제 원본 세 장
        var source = fight.Allies[0]; // 아군 카드의 실제 공격자
        var full = fight.Enemies[0]; // 활성 실명이고 체력이 가득 찬 적
        var half = fight.Enemies[1]; // 최대 체력의 정확히 절반인 적
        var above = fight.Enemies[2]; // 절반보다 한 점 많은 적
        full.ApplyStatusEffect(BattleStatusEffectType.Blind, 10, 3, 1, source); // 마법 렌즈에만 해당하는 실제 활성 실명
        half.ApplyStatusEffect(BattleStatusEffectType.Blind, 10, 3, 1, source); // 두 사막 유물 효과가 동시에 해당하는 실제 상태
        above.ApplyStatusEffect(BattleStatusEffectType.Blind, 10, 1, 1, source); // 일반 정리 전에 직접 만료할 비교 상태
        above.GetStatusEffect(BattleStatusEffectType.Blind).AdvanceDuration(); // 목록에 남아도 이미 비활성인 실명
        half.TakeDamage(100, BattleDamageType.None, source); // 직접 공격이 아닌 일반 피해가 두 유물을 적용하지 않는 비교
        above.TakeDamage(99, BattleDamageType.None, source); // 체력 절반보다 한 점 높은 정확한 경계 준비
        Assert.AreEqual(100, half.CurrentHealth); // 일반 피해에서 실제 저체력 증가나 실명 렌즈 없음
        Assert.AreEqual(101, above.CurrentHealth); // 추가 피해 조건 이전의 실제 경계값
        Assert.AreEqual(15, full.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 활성 실명 대상 마법의 원본 가산 네 점
        Assert.AreEqual(11, full.PreviewDamage(11, BattleDamageType.Physical, source, alliedDirectAttack: true).RawDamage); // 실명인 적이라도 물리 직접 타격에는 렌즈 미적용
        Assert.AreEqual(16, half.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 정확히 절반에서 가산 뒤 십 퍼센트 내림
        Assert.AreEqual(12, half.PreviewDamage(11, BattleDamageType.Physical, source, alliedDirectAttack: true).RawDamage); // 물리 직접 피해에는 저체력 증가만 적용
        Assert.AreEqual(18, half.PreviewDamage(17, BattleDamageType.Physical, source, alliedDirectAttack: true).RawDamage); // 십팔 점 칠을 반올림하지 않는 원본 내림 확인
        Assert.AreEqual(11, above.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 체력 한 점 초과와 만료 실명의 두 제외 조건
        yield return fight.Use("RUNE_BURST", 0); // 활성 실명만 있는 대상의 실제 마법 카드
        yield return fight.Use("RUNE_BURST", 1); // 실명과 정확한 절반 조건에 맞는 실제 카드
        yield return fight.Use("RUNE_BURST", 2); // 두 조건에 맞지 않는 실제 카드
        Assert.AreEqual(185, full.CurrentHealth); // 실명에 대한 실제 마법 가산과 예고의 같은 수치
        Assert.AreEqual(84, half.CurrentHealth); // 실제 착지 시점의 두 피해 효과 합산
        Assert.AreEqual(90, above.CurrentHealth); // 최초 명중 직전 조건으로 계산한 원본 피해
    } // 선택된 세 적 준비 종료

    [UnityTest] // 첫 공격 전체 빗나감과 선택한 공격의 다대상 타격 확인
    public IEnumerator AntennaConsumesFirstDeclaredAttackAndPaysAtMostOneLandedHit() // 전체 빗나감과 다단 광역 첫 실제 명중 비교
    { // 첫 아군 공격의 실제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 현재 아군 턴 카드 사용 활성화
        using (var held = new OwnedRelics(RelicEffectType.AmbushBugAntennaFirstTurnFirstAttackDamage)) // 사막의 첫 공격 유물 실제 보유
        { // 같은 유물의 구분된 세 전투 확인 시작
            yield return CheckMiss(); // 빗나감이 전투 최초 기회를 실제로 사용
            yield return CheckMultiHit(); // 광역 연타에서 실제 명중 한 번만 가산
            yield return CheckLaterTurn(); // 공격 없이 첫 아군 턴을 마치면 이후 불발 확인
        } // 같은 유물의 구분된 세 전투 확인 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 첫 아군 공격의 실제 전투 검사 종료

    private static IEnumerator CheckMiss() // 실제 전체 빗나감의 선언 소비와 다음 공격 검증
    { // 첫 카드의 빗나감 조건 측정 시작
        using var fight = new Fight("GOLDEN_COIN", "RUNE_BURST", "RUNE_BURST"); // 비공격 하나와 실제 직접 피해 두 장
        var source = fight.Allies[0]; // 담당 아군 카드 소유자
        var miss = fight.Enemies[0]; // 첫 공격을 전부 피할 비교 적
        var next = fight.Enemies[1]; // 다음 실제 명중의 생존 적
        miss.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 3, 1); // 확정 회피의 현재 정상 상태
        yield return fight.Use("GOLDEN_COIN"); // 실제 비공격 카드 사용으로 첫 공격 기회 보존
        Assert.AreEqual(17, next.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 예정 첫 직접 명중의 보너스 예고
        yield return fight.Use("RUNE_BURST", 0); // 첫 선언 카드가 실제로 단일 대상에 빗나감
        Assert.AreEqual(200, miss.CurrentHealth); // 기존 명중 판정에서 실제 피해 없음
        Assert.AreEqual(11, next.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 첫 선언 뒤 같은 아군 턴의 예고가 원본만 유지
        yield return fight.Use("RUNE_BURST", 1); // 같은 첫 아군 턴의 두 번째 실제 공격
        Assert.AreEqual(189, next.CurrentHealth); // 첫 공격의 보너스를 다음 카드로 이어받지 않는 확인
    } // 첫 카드의 빗나감 조건 측정 종료

    private static IEnumerator CheckMultiHit() // 같은 첫 카드의 두 차례 광역 중 한 번만 가산
    { // 독립 전투의 첫 광역 카드 준비 시작
        using var fight = new Fight("EYE_OF_STORM"); // 실제 두 차례 적 전체 공격의 원본
        var first = fight.Enemies[0]; // 두 차례를 모두 피하는 첫 배치 적
        var second = fight.Enemies[1]; // 첫 차례의 최초 실제 명중을 받는 적
        var third = fight.Enemies[2]; // 같은 차례 다음의 원본 피해만 받을 적
        first.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 3, 1); // 첫 배치에 원래 확정 회피
        var secondHits = new List<int>(); // 한 적 두 차례의 실제 피해 전 수치
        second.DamageResolved += (source, target, damage) => secondHits.Add(damage.RawDamage); // 실제 명중 이벤트에서 피해량 기록
        yield return fight.Use("EYE_OF_STORM"); // 새 전투 첫 공격을 실제로 전체에 두 차례 적용
        CollectionAssert.AreEqual(new[] { 20, 14 }, secondHits); // 첫 실제 명중에만 원본 열넷과 가산 여섯
        Assert.AreEqual(200, first.CurrentHealth); // 각 차례에 모두 적용한 실제 첫 배치 회피
        Assert.AreEqual(172, third.CurrentHealth); // 같은 전체 공격의 모든 다른 타격은 원래 피해
    } // 독립 전투의 첫 광역 카드 준비 종료

    private static IEnumerator CheckLaterTurn() // 실제 적 턴 후 새 아군 턴의 첫 공격에 미지급
    { // 첫 아군 턴을 공격 없이 종료 시작
        using var fight = new Fight("RUNE_BURST"); // 실제 첫 아군 턴을 사용할 원본 준비
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 공격 없이 실제 첫 아군 턴 종료
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴 및 손패 시작
        var target = fight.Enemies[0]; // 새로운 턴에서 처음으로 공격할 적
        Assert.AreEqual(11, target.PreviewDamage(11, BattleDamageType.Magical, fight.Allies[0], alliedDirectAttack: true).RawDamage); // 나중 턴에는 첫 공격 보너스 예고 없음
        yield return fight.Use("RUNE_BURST", 0); // 실제 첫 공격이지만 전투의 두 번째 아군 턴
        Assert.AreEqual(189, target.CurrentHealth); // 나중 턴의 처음 공격에는 정상 피해만 적용
    } // 첫 아군 턴을 공격 없이 종료 마감

    private sealed class OwnedRelics : IDisposable // 실제 등록 원본의 회차 획득과 정리
    { // 현재 검사 보유 범위 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 검사 시작 전의 기존 보관함 여부
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 현재 전투가 조회할 실제 보관함
        private readonly List<RelicData> relics = new List<RelicData>(); // 이 검사에서 실제로 넣은 원본 목록

        internal OwnedRelics(params RelicEffectType[] types) // 실제 데이터 효과 이름으로 등록 유물 조회
        { // 정식 유물 장착 시작
            foreach (var type in types) // 검사 전투에 필요한 실제 유물 순회
            { // 개별 보유 원본 적용 시작
                var relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == type); // 실제 리소스의 정확한 유물 조회
                Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 회차의 새 유물 획득 결과
                relics.Add(relic); // 검사 뒤 제거할 실제 보유 항목 저장
            } // 개별 보유 원본 적용 종료
        } // 정식 유물 장착 종료

        public void Dispose() // 현재 전투의 실제 장착 원본 해제
        { // 보유함 정리 시작
            foreach (var relic in relics) // 검사에서만 넣은 실제 원본 확인
            { // 각 검사 항목 정리 시작
                manager.TryRemove(relic.RelicId); // 정확한 실제 보유 목록의 자료 제거
            } // 각 검사 항목 정리 종료
            if (previous == null) // 원래 다른 회차 보관함이 없었던 검사 확인
            { // 검사 전용 관리자 제거 시작
                Object.Destroy(manager.gameObject); // 현재 검사에서 생성한 회차 관리자 해제
            } // 검사 전용 관리자 제거 종료
        } // 보유함 정리 종료
    } // 현재 검사 보유 범위 종료
} // 사막 공격 유물 검사 종료
