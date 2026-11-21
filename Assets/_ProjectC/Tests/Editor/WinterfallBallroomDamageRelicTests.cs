using System; // 실제 전투 원본 보관함의 정리
using System.Collections; // 실제 카드 연출 및 플레이 모드 대기
using NUnit.Framework; // 수치와 전투 승리 결과 검증
using UnityEngine; // 정식 유물 원본의 실제 리소스 조회
using UnityEngine.TestTools; // 플레이 모드 진입과 종료
using Fight = WinterfallSwordCombatTests.Fight; // 실제 카드와 적 선택 검사 환경
using Object = UnityEngine.Object; // 새로 만든 실제 회차 관리자 제거
using Random = UnityEngine.Random; // 카드 확률의 재현 난수

public sealed class WinterfallBallroomDamageRelicTests // 은신과 행동 취소 및 화상 처치 유물 검사
{ // 무도회 유물 검사 시작
    [UnityTest] // 은신이 실제로 남은 적의 직접 카드 피해와 예고
    public IEnumerator CurtainPinAddsFiveOnlyToDirectHitsAgainstActiveEnemyStealth() // 기존 은신과 직접 카드 피해의 실제 연결 확인
    { // 커튼 고정핀 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 손패와 유물 보유 처리 시작
        yield return CheckCurtainPin(); // 직접 공격과 유효 은신의 조합 비교
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 커튼 고정핀 검사 종료

    private static IEnumerator CheckCurtainPin() // 활성 은신의 순수 예고와 실제 지팡이 명중 검사
    { // 활성 은신 대상 준비 시작
        using var owned = new OwnedRelic("CurtainPin"); // 커튼 고정핀의 실제 원본 회차 장착
        using var fight = new Fight("RUNE_BURST", "RUNE_BURST", "RUNE_BURST"); // 고정 수치의 기존 실제 지팡이 카드 세 장
        var ally = fight.Allies[0]; // 실제 지팡이 공격을 할 아군
        var hidden = fight.Enemies[0]; // 명중과 현재 은신을 함께 검사할 적
        var expired = fight.Enemies[1]; // 만료 표식만 남은 비교 적
        var evader = fight.Enemies[2]; // 은신 상태에 있지만 정상 공격은 피할 적
        hidden.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 별도 적 행동 없이 기존 활성 은신 부여
        expired.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 1, 1); // 만료 후 남아 있는 데이터의 비교 상태
        expired.GetStatusEffect(BattleStatusEffectType.Stealth).AdvanceDuration(); // 실제 활성 기간을 모두 소모
        evader.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 2, 1); // 빗나가는 생존 적의 활성 은신 확인
        evader.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 기존 명중률이 영이 되는 정상 회피
        Assert.AreEqual(16, hidden.PreviewDamage(11, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 활성 은신 적에게만 더한 실제 예고 피해
        Assert.AreEqual(11, expired.PreviewDamage(11, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 남아 있으나 만료된 은신에는 가산 없음
        Assert.AreEqual(11, hidden.PreviewDamage(11, BattleDamageType.Magical, ally).RawDamage); // 별도 일반 피해의 순수 예고 보존
        Assert.AreEqual(4, hidden.TakeDamage(4, BattleDamageType.None, ally).AppliedDamage); // 같은 출처의 별도 고정 피해 가산 제외
        yield return fight.Use("RUNE_BURST", 0); // 정상 손패와 대상 선택으로 활성 은신 적 명중
        Assert.AreEqual(180, hidden.CurrentHealth); // 원래 별도 피해 넷 이후 커튼 고정핀을 포함한 열여섯 피해
        Assert.IsNotNull(hidden.GetStatusEffect(BattleStatusEffectType.Stealth)); // 타격에 사용할 은신 상태를 임의로 소모하지 않은 확인
        yield return fight.Use("RUNE_BURST", 1); // 활성 은신이 없는 같은 원본의 실제 타격
        Assert.AreEqual(189, expired.CurrentHealth); // 만료된 은신 적의 실제 원본 마법 피해 열하나
        yield return fight.Use("RUNE_BURST", 2); // 활성 은신 대상이지만 실제 타격에는 빗나감
        Assert.AreEqual(200, evader.CurrentHealth); // 일반 명중이 없는 추가 은신 피해 없음
    } // 활성 은신 대상 준비 종료

    [UnityTest] // 정상 경직 성공과 갱신의 독립 일반 피해 확인
    public IEnumerator PianoWireDamagesOncePerSuccessfulStaggerAndIgnoresStunSpeedAndMisses() // 실제 상태 사건의 발생자와 면역 결과 확인
    { // 피아노 줄 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 상태 이벤트 수신의 플레이 모드
        yield return CheckPianoWire(); // 확률 성공과 면역 및 잘못된 상태 비교
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 피아노 줄 검사 종료

    private static IEnumerator CheckPianoWire() // 실제 서리 감옥과 별도의 상태 재부여 검사
    { // 상태 성공과 추가 피해 준비 시작
        using var owned = new OwnedRelic("PianoWire"); // 피아노 줄 정식 원본의 실제 장착
        using var fight = new Fight("FROST_PRISON", "FROST_PRISON", "FROST_PRISON"); // 기존 경직을 실제로 시도하는 카드 세 장
        var dispatcher = new BattleEventDispatcher(); // 게임과 같은 실제 공용 이벤트 버스
        using var events = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 실제 상태 결과를 받는 공용 연결
        using var relics = new BattleRelicEffectController(dispatcher, owned.Manager.Inventory, owned.Manager.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 실전 보유 유물의 실제 실행 경로
        var ally = fight.Allies[0]; // 실제 경직을 부여하는 카드 소유자
        var target = fight.Enemies[0]; // 실제 경직으로 추가 피해를 받을 적
        var immune = fight.Enemies[1]; // 경직 면역이어도 기본 피해는 받을 적
        var evasive = fight.Enemies[2]; // 실제 공격과 경직을 모두 피하는 적
        var other = fight.Enemies[3]; // 행동 속도와 기절 및 발생자 없는 상태의 대조 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 경직을 막는 일반 약화 면역
        evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 해당 실제 카드가 확정 빗나가게 할 회피
        int seed = FindSuccessfulStaggerSeed(); // 기존 카드의 실제 사십 퍼센트 성공 난수
        fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 공격 직전 씨앗을 쓰는 기존 확률
        yield return fight.Use("FROST_PRISON", 0); // 마법 피해와 실제 경직이 적용되는 정상 공격
        Assert.AreEqual(188, target.CurrentHealth); // 카드 여덟과 별도 일반 피해 넷 한 번 확인
        Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.Stagger)); // 다음 실제 행동을 막는 활성 경직
        Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, target.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 1, 1, ally)); // 같은 남은 행동 취소를 정상 갱신
        Assert.AreEqual(184, target.CurrentHealth); // 실제 성공한 두 번째 상태 적용에서만 추가 피해 넷
        yield return fight.Use("FROST_PRISON", 1); // 실제 경직 면역 적에게 정상 명중
        Assert.AreEqual(192, immune.CurrentHealth); // 면역 때문에 기본 마법 피해만 적용
        yield return fight.Use("FROST_PRISON", 2); // 실제 빗나간 경직 카드 처리
        Assert.AreEqual(200, evasive.CurrentHealth); // 빗나감의 기본과 추가 피해 없음
        other.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedDown, 1, 1, 1, ally); // 다음 행동의 순서만 늦추는 효과
        other.ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1, ally); // 별도 기절 규칙의 실제 성공
        other.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 1, 1); // 실제 아군 발생자가 없는 경직
        Assert.AreEqual(200, other.CurrentHealth); // 아군의 한 행동 취소 이외에는 피해 없음 확인
    } // 상태 성공과 추가 피해 준비 종료

    [UnityTest] // 실제 화상 상태의 처치 순간과 승리 전 회복 검사
    public IEnumerator SilverSnufferHealsInRosterOrderOnActualBurningDefeatsIncludingTheFinalBurnTick() // 살아 있는 최저 체력과 실제 피해 후 생존의 대조
    { // 은촛불집게 검사 시작
        yield return new EnterPlayMode(); // 실제 상점 유물과 전투 처치 신호 연결
        CheckSnuffer(); // 일반 피해와 마지막 화상 틱 및 결과 순서 확인
        yield return new ExitPlayMode(); // 격리 플레이 모드 종료
    } // 은촛불집게 검사 종료

    private static void CheckSnuffer() // 정상 생존과 화상 처치 및 낮은 체력 대상의 실제 동률 확인
    { // 화상 처치 피해 준비 시작
        using var owned = new OwnedRelic("DamagedSilverCandleSnuffer"); // 망가진 은촛불집게의 정식 원본 장착
        using var fight = new Fight("RUNE_BURST"); // 실제 아군 명단과 전투 결과 런타임 준비
        var first = fight.Allies[0]; // 동률일 때 명단 첫 위치의 아군
        var second = fight.Allies[1]; // 처음 동률이며 다음 처치의 현재 최저 체력 아군
        var dead = fight.Allies[2]; // 체력이 영이어도 회복 대상에서 제외할 아군
        var burning = fight.Enemies[0]; // 화상 활성 상태로 일반 타격에 죽을 적
        var periodic = fight.Enemies[1]; // 직접 공격자 없이 최종 화상 틱에 죽을 적
        first.TakeDamage(first.CurrentHealth - 10, BattleDamageType.None); // 동률의 실제 현재 체력 열
        second.TakeDamage(second.CurrentHealth - 10, BattleDamageType.None); // 같은 수치의 다른 아군 현재 체력 열
        dead.TakeDamage(dead.CurrentHealth, BattleDamageType.None); // 생존자 계산에서 빼야 할 실제 전투불능 아군
        burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, first); // 쓰러질 시점에 읽을 활성 화상
        burning.ApplyStatusEffect(BattleStatusEffectType.UndyingGuard, 1, 1, 1); // 치명 피해를 한 번 체력 하나로 버티는 기존 상태
        burning.PreviewDamage(200, BattleDamageType.None, first); // 내용만 계산하는 원래 피해 예고
        Assert.AreEqual(10, first.CurrentHealth); // 예고만으로 화상 처치 회복 없음 확인
        burning.TakeDamage(200, BattleDamageType.None, first); // 실제 체력 하나로 생존한 피해 사건
        Assert.AreEqual(1, burning.CurrentHealth); // 실제 사망하지 않은 체력 경계 확인
        Assert.AreEqual(10, first.CurrentHealth); // 체력 한 점의 생존 경계를 처치로 오인하지 않는 확인
        burning.TakeDamage(1, BattleDamageType.None, first); // 활성 화상 적의 실제 사망 발생
        Assert.AreEqual(15, first.CurrentHealth); // 동률에서 실제 명단 첫 생존자에게 회복 다섯
        Assert.AreEqual(10, second.CurrentHealth); // 같은 실제 처치가 다른 동률 아군을 회복하지 않음
        Assert.IsTrue(dead.IsDead); // 체력 영의 전투불능에게 회복 없음 확인
        fight.Enemies[2].TakeDamage(200, BattleDamageType.None); // 화상 상태가 없는 실제 적 처치
        fight.Enemies[3].TakeDamage(200, BattleDamageType.None); // 마지막 화상 적을 남기는 실제 적 처치
        Assert.AreEqual(10, second.CurrentHealth); // 화상 없는 처치의 회복 없음 확인
        periodic.TakeDamage(198, BattleDamageType.None); // 실제 다음 화상 틱에 죽을 체력 둘
        periodic.ApplyStatusEffect(BattleStatusEffectType.Burn, 2, 2, 1, first); // 마지막 적이 쓰러질 전투 화상 틱
        second.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 40, 2, 1); // 보상 회복량도 정상 약화로 줄어드는 대조
        int healthAtVictory = -1; // 실제 전투 결과 알림 안의 승리 체력
        fight.Turns.BattleCompleted += result => healthAtVictory = result == BattleResult.Victory ? second.CurrentHealth : -1; // 승리 결과가 전달될 때의 생존 체력
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 마지막 적 턴 시작
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 화상 종료 피해로 마지막 적의 실제 사망
        Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 마지막 화상 처치 뒤 원래 승리 결과
        Assert.AreEqual(13, second.CurrentHealth); // 원래 회복 다섯에 받는 회복 감소를 적용한 실제 셋
        Assert.AreEqual(13, healthAtVictory); // 승리 결과 기록 전에 실제 회복 정산 완료
        Assert.AreEqual(15, first.CurrentHealth); // 회복 중복 없이 이미 높은 첫 아군의 체력 보존
    } // 화상 처치 피해 준비 종료

    private static int FindSuccessfulStaggerSeed() // 원래 서리 감옥의 사십 퍼센트 안에 드는 실제 시드 탐색
    { // 카드 상태 난수 검색 시작
        for (int seed = 0; seed < 1000; seed++) // 제한된 실제 게임 난수에서 성공 후보 확인
        { // 후보 난수 준비 시작
            Random.InitState(seed); // 현재 검사에 사용할 실제 게임 상태
            if (Random.Range(0, 100) < 40) // 실제 서리 감옥의 기존 발동 확률 확인
            { // 실제 성공 후보 반환 시작
                return seed; // 다음 카드 명중에 고정할 실제 시드
            } // 실제 성공 후보 반환 종료
        } // 후보 난수 준비 종료
        throw new InvalidOperationException("No successful Stagger seed found."); // 정해진 범위의 검사 실패
    } // 카드 상태 난수 검색 종료

    private sealed class OwnedRelic : IDisposable // 실제 보유 유물과 회차 상태 정리
    { // 임시 유물 준비 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 테스트 전 이미 있던 회차 관리자
        private readonly RelicData relic; // 실제 보유할 현재 정식 유물
        internal RelicRunManager Manager { get; } = RelicRunManager.EnsureInstance(); // 게임의 실제 현재 유물 인벤토리 조회

        internal OwnedRelic(string name) // 보유를 확인할 정식 리소스 원본 준비
        { // 실제 회차 유물 획득 시작
            relic = Resources.Load<RelicData>("Relics/" + name); // 게임에 등록된 실제 유물 원본
            Assert.IsNotNull(relic, name); // 이전 저장과 공유하는 정식 리소스 위치 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, Manager.TryAcquire(relic)); // 테스트 전투의 실제 획득과 착용
        } // 실제 회차 유물 획득 종료

        public void Dispose() // 현재 테스트에서만 만든 유물과 인벤토리 정리
        { // 실제 테스트 보유 유물 정리 시작
            Manager.TryRemove(relic.RelicId); // 현재 검사에서 직접 얻었던 유물만 제거
            if (previous == null) // 원래 실제 관리자 객체가 없던 경우 확인
            { // 임시 원본 보관함 제거 시작
                Object.Destroy(Manager.gameObject); // 검사에서 만든 현재 회차 관리자 제거
            } // 임시 원본 보관함 제거 종료
        } // 실제 테스트 보유 유물 정리 종료
    } // 임시 유물 준비 종료
} // 무도회 유물 검사 종료
