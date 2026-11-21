using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using NUnit.Framework; // 상태와 확률의 경계 검증
using UnityEngine; // 실제 리소스 및 전투 자료 접근
using UnityEngine.TestTools; // 기존 전투 카드 입력의 플레이 모드 실행
using Fight = WinterfallSwordCombatTests.Fight; // 원본 손패와 유닛 화면이 연결된 전투 재사용
using Random = UnityEngine.Random; // 상태와 명중의 실제 난수 경계 제어

public sealed class WinterfallServantStatusRelicTests // 공포, 출혈 및 첫 약화 반사의 실제 유물 회귀 검사
{ // 전투 상태 유물 확인 시작
    [UnityTest] // 공포의 성공과 회피 약화의 실제 적 턴 연결 확인
    public IEnumerator InvitationUsesSuccessfulAllyFearAndLastsOneEnemyTurn() // 기존 공포와 회피 감소의 공통 상태 수명 확인
    { // 초대장 실제 검사 시작
        yield return new EnterPlayMode(); // 현재 회차 유물과 전투 처리가 동작하는 모드로 진입
        VerifyInvitation(); // 실제 상태 부여와 진영 턴 종료 검증
        yield return new ExitPlayMode(); // 독립 전투 정리 뒤 편집 모드 복귀
    } // 초대장 실제 검사 종료

    private static void VerifyInvitation() // 성공한 공포와 기존 회피 억제의 구분 확인
    { // 공포와 한 적 턴의 경계 확인 시작
        Acquire("TornInvitation"); // 실제 유물 원본을 정상 보유 방식으로 획득
        using var fight = new Fight("THRUST"); // 실제 상태 처리기가 연결된 독립 전투
        var ally = fight.Allies[0]; // 회피를 낮출 공포의 실제 아군 발생자
        var enemy = fight.Enemies[0]; // 정상 공포와 회피 감소를 받을 생존 적
        var immune = fight.Enemies[1]; // 실제 디버프 면역을 가진 적
        var foreign = fight.Enemies[2]; // 적을 발생자로 쓰는 공포 대조 대상
        enemy.ApplyStatusEffect(BattleStatusEffectType.Evasion, 20, 3, 1); // 상태의 실제 명중 반영을 확인할 기존 회피 증가
        Assert.AreEqual(80, BattleHitChance.GetPercent(ally, enemy)); // 회피 증가가 실제 명중률에 반영되는 기준 확인
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 다른 적에게 실제 공포 면역 준비
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, ally)); // 면역에 걸린 공포의 실제 실패 확인
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 실패한 공포에는 회피 감소가 없음 확인
        foreign.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, immune); // 아군 발생자가 아닌 같은 적 진영의 공포
        Assert.IsNull(foreign.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 아군이 부여한 공포만 유물 대상으로 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 3, 1, ally)); // 정상 아군의 실제 공포 신규 적용 확인
        Assert.AreEqual(5, enemy.GetStatusEffect(BattleStatusEffectType.EvasionDown)?.EffectiveValue); // 원본의 오 퍼센트포인트 회피 감소 확인
        Assert.AreEqual(100, BattleHitChance.GetPercent(ally, enemy)); // 활성 공포의 기존 전체 회피 억제 보존 확인
        Assert.IsTrue(enemy.RemoveStatusEffect(BattleStatusEffectType.EvasionDown)); // 공포 갱신의 별도 신규 회피 약화를 구분할 상태 준비
        Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 3, 1, ally)); // 지속 중인 실제 공포 정상 갱신 확인
        Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.EvasionDown)?.RemainingTurns); // 갱신도 한 적 턴의 회피 약화 생성 확인
        Assert.IsTrue(enemy.RemoveStatusEffect(BattleStatusEffectType.Fear)); // 기존 공포 억제와 별도 회피 감소를 구분할 상태 정리
        Assert.AreEqual(85, BattleHitChance.GetPercent(ally, enemy)); // 공포가 없을 때 회피율을 실제 오 퍼센트포인트 낮춘 결과 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적의 행동 턴 시작
        Assert.AreEqual(1, enemy.GetStatusEffect(BattleStatusEffectType.EvasionDown)?.RemainingTurns); // 적 턴 시작에서 회피 약화가 유지되는지 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적의 실제 첫 행동 턴 종료
        Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.EvasionDown)); // 한 적 턴을 마친 뒤 회피 감소 만료 확인
    } // 공포와 한 적 턴의 경계 확인 종료

    [UnityTest] // 실제 찌르기 명중의 출혈 확률 및 면역에서의 난수 검사
    public IEnumerator ForkRaisesOnlyOptionalBleedByFivePercentagePoints() // 실제 카드 출혈 확률의 경계 확인
    { // 포크 장식 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 피해와 보유 유물 회차 준비
        yield return VerifyFork(); // 출혈 확률과 직접 부여 경로 대조
        yield return new ExitPlayMode(); // 전투 객체를 해제하고 편집 모드 복귀
    } // 포크 장식 플레이 모드 검사 종료

    private static IEnumerator VerifyFork() // 실제 출혈 카드의 이십오 퍼센트 경계 확인
    { // 실제 찌르기 카드와 명중 상태 검증 시작
        Acquire("RustedForkOrnament"); // 실제 녹슨 포크 장식 원본 유물 획득
        using var fight = new Fight("THRUST", "THRUST", "THRUST"); // 실제 출혈 확률의 원본 카드 세 장을 손패에 연결
        var owner = fight.Allies[0]; // 원본 출혈 카드의 실제 아군 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 일반 명중과 정수 피해의 정신력 조건 준비
        Assert.AreEqual(20, fight.View("THRUST").RuntimeCard.SourceData.SecondaryEffectValue); // 찌르기 원본의 이십 퍼센트 확인
        int seed = FindSeed(24); // 오 퍼센트포인트 증가분의 마지막 성공 값
        fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 실제 카드 충돌 직전의 전투 난수 재현
        yield return fight.Use("THRUST", 0); // 원래 확률보다 높지만 보정 한도 안의 실제 찌르기
        Assert.AreEqual(2, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Bleed)?.EffectiveValue); // 기존 출혈 피해 수치가 유지되며 성공 확인
        seed = FindSeed(25); // 이십오 퍼센트 영역의 첫 실패 값
        yield return fight.Use("THRUST", 1); // 다른 생존 적에게 실제 실패 경계 확인
        Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Bleed)); // 보정된 확률을 넘는 난수의 출혈 실패 확인
        var immune = fight.Enemies[2]; // 정상 체력 피해와 상태 면역을 구분할 실제 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 출혈 보정 전에 이미 차단한 디버프 면역
        seed = FindSeed(4); // 상태 확률 굴림이 실행되면 크게 성공할 입력
        Random.InitState(seed); // 실제 충돌 시 초기화할 동일한 난수 상태
        Random.State beforeImmune = Random.state; // 면역 대상 출혈 시도에 보존해야 할 난수 기록
        yield return fight.Use("THRUST", 2); // 면역이 남아 있는 적에게 일반 공격 한 번 사용
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Bleed)); // 포크 장식이 적의 일반 면역을 뚫지 않음 확인
        Assert.AreEqual(beforeImmune, Random.state); // 실제 면역에서 출혈 확률 난수가 소비되지 않음 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, fight.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1, owner)); // 별도 확률 굴림을 쓰지 않는 실제 확정 출혈 확인
        Assert.AreEqual(beforeImmune, Random.state); // 확정 출혈의 정상 경로에는 포크 판정이 없음을 확인
    } // 실제 찌르기 카드와 명중 상태 검증 종료

    [UnityTest] // 실제 약화 적용 및 즉시 정화와 독립인 공용 반사 검사
    public IEnumerator MirrorUsesOnlyFirstRetainedEnemyDebuffEvenWhenRollFails() // 최초 반사 시도와 정확한 원본 복사 확인
    { // 거울못 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 원본 유물과 전투의 상태 신호 연결
        VerifyMirror(); // 실제 정화, 확률 성공·실패 및 반사 면역 검증
        yield return new ExitPlayMode(); // 모든 독립 전투를 마친 뒤 편집 모드 복귀
    } // 거울못 플레이 모드 검사 종료

    private static void VerifyMirror() // 기존 정화 순서와 전투별 첫 반사 판정 검증
    { // 원본 약화 복사 검사 시작
        Acquire("CloudedMirrorNail"); // 실제 흐린 거울못 획득
        var cleanse = Acquire("NecrateSealCore"); // 새 약화를 즉시 제거하는 기존 봉인핵 획득
        int success = FindSeed(19); // 이십 퍼센트 확률의 마지막 성공 시드
        int failure = FindSeed(20); // 이십 퍼센트 확률 밖의 첫 실패 시드
        using (var fight = new Fight("THRUST")) // 정화 성공과 반사 성공을 독립 확인할 전투
        { // 기존 정화가 먼저 적용되는 경계 검사 시작
            var relicRun = RelicRunManager.Instance; // 실제로 두 유물을 모두 보유한 현재 회차
            using var dispatcher = new BattleEventDispatcher(); // 실제 전투의 공용 상태 신호 발행기 수명 관리
            using var signals = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 원래 상태 적용을 공용 유물 연결에 전달
            using var relicEvents = new BattleRelicEffectController(dispatcher, relicRun.Inventory, relicRun.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 실제 전투와 같은 공용 유물 발동 확인
            var source = fight.Enemies[0]; // 아군에게 실제 약화를 주고 그 약화를 돌려받을 적
            var target = fight.Allies[0]; // 기존 봉인핵과 거울못이 있는 실제 아군
            Random.InitState(success); // 한 번의 유효한 유물 판정 기대값을 만들 동일 시작 상태
            Random.Range(0, 100); // 현재 효과에 허용된 정확한 추첨 한 번 소비
            Random.State afterSingleRoll = Random.state; // 기존 발동기에서도 그대로 유지해야 할 난수 상태
            Random.InitState(success); // 기존 봉인핵의 이십 퍼센트 즉시 정화 성공 난수
            Assert.AreEqual(BattleStatusEffectApplyResult.CleansedByRelic, target.ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, 2, 2, 2, source)); // 실제 수신 뒤 기존 유물로 즉시 정화한 결과 확인
            Assert.IsNull(source.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 제거된 약화의 반사 미발동 확인
            Assert.AreEqual(afterSingleRoll, Random.state); // 정화 시 공용 상태 신호가 같은 유물을 다시 추첨하지 않음 확인
            Assert.IsTrue(relicRun.TryRemove(cleanse.RelicId)); // 이후에 정확한 반사 결과를 검증할 기존 유물 해제
            Random.InitState(success); // 보존된 최초 반사 기회의 실제 경계 난수
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, target.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 3, source)); // 첫 번째로 유지된 실제 적 약화 확인
            Assert.AreEqual(afterSingleRoll, Random.state); // 반사 때문에 발생한 추가 상태 이벤트에도 기존 난수 한 번만 사용 확인
            var reflected = source.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown); // 실제 발생자에게 생성된 새 약화
            Assert.IsNotNull(reflected); // 정화된 약화가 거울못 기회를 사용하지 않았음 확인
            Assert.AreEqual(3, reflected.Value); // 상태의 실제 보정 이후 원본 수치 복사 확인
            Assert.AreEqual(2, reflected.RemainingTurns); // 상태의 현재 남은 횟수 복사 확인
            Assert.AreEqual(3, reflected.MaximumStacks); // 원본 최대 중첩 수 복사 확인
            Assert.IsNotNull(target.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 반사가 기존에 받은 약화를 지우지 않는지 확인
        } // 기존 정화가 먼저 적용되는 경계 검사 종료
        using (var fight = new Fight("THRUST")) // 다음 전투에서 첫 시도가 실패하는 상황 확인
        { // 실패한 한 번으로 파티 기회가 종료되는지 확인 시작
            var source = fight.Enemies[0]; // 확률 실패에 따른 실제 첫 약화 발생자
            var other = fight.Enemies[1]; // 이후 다른 아군에게 약화를 줄 적
            fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, 2, 2, 1); // 적 발생자가 없는 실제 약화의 반사 조건 제외
            Random.InitState(failure); // 새 전투의 첫 실제 적 약화에 실패 경계 사용
            fight.Allies[0].ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1, source); // 첫 확률 실패 뒤 남은 일반 약화 적용
            Assert.IsNull(source.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 이십 퍼센트의 첫 실패 값 확인
            Random.InitState(success); // 다시 추첨하면 성공할 다음 아군의 입력 준비
            Random.State prior = Random.state; // 재시도가 발생하지 않았는지 확인할 기존 난수 기록
            fight.Allies[1].ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1, other); // 이미 확률 시도를 끝낸 별개 아군의 실제 약화
            Assert.IsNull(other.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 이전 실패 뒤 다른 아군으로 반사 기회를 다시 사용하지 않음 확인
            Assert.AreEqual(prior, Random.state); // 두 번째 실제 약화는 추가 반사 난수를 소비하지 않음 확인
        } // 실패한 한 번으로 파티 기회가 종료되는지 확인 종료
        using var fresh = new Fight("THRUST"); // 같은 회차에서 다시 유효한 새 전투 구성
        var immune = fresh.Enemies[0]; // 반사에 성공해도 원래 약화 면역을 적용할 공격자
        var reserve = fresh.Enemies[1]; // 반사 대체 대상으로 잘못 선택할 수 있는 다른 적
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 발생자만 가진 정상 상태 면역
        Random.InitState(success); // 새 전투의 재사용 가능한 첫 반사 난수 성공
        fresh.Allies[0].ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1, immune); // 적의 입력은 성공하되 반사 수신자는 면역인 실제 약화
        Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 반사도 실제 발생자의 원래 면역을 존중하는지 확인
        Assert.IsNull(reserve.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 발생자 문제를 다른 적으로 대체하지 않음 확인
    } // 원본 약화 복사 검사 종료

    private static RelicData Acquire(string key) // 실제 게임 자산의 회차 유물 획득
    { // 유물 정상 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 리소스에 등록된 실제 원본 조회
        Assert.IsNotNull(relic, key); // 실제 유물 자산과 이름 연결 검증
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기존 획득 진입의 정상 보유 성공 확인
        return relic; // 제거와 원본 확인에 쓸 실제 자산 반환
    } // 유물 정상 획득 종료

    private static int FindSeed(int desired) // 실제 첫 정수 난수를 재현할 시드 조회
    { // 확률 시드 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 경계값을 만들 시드 후보 반복
        { // 개별 실제 난수 확인 시작
            Random.InitState(seed); // 실제 전투에 쓰는 후보 난수 초기화
            if (Random.Range(0, 100) == desired) // 요구한 상태 확률의 정확한 경계 비교
            { // 첫 경계 후보 확정 시작
                return seed; // 실제 조건을 만족하는 재현 시드 반환
            } // 첫 경계 후보 확정 종료
        } // 개별 실제 난수 확인 종료
        throw new AssertionException("전투 상태 확률 경계 시드 없음"); // 요청한 정수 난수의 미발견 오류
    } // 확률 시드 검색 종료
} // 전투 상태 유물 확인 종료
