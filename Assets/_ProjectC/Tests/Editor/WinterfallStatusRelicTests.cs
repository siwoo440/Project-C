using System.Collections; // 실제 전투 모드와 카드 연출 대기
using NUnit.Framework; // 원본 유물의 발동 결과 확인
using UnityEngine; // 실제 전투 유물 원본 조회
using UnityEngine.TestTools; // 플레이 모드 입력 테스트 실행
using Random = UnityEngine.Random; // 발동 확률 경계 재현
using static WinterfallSwordCombatTests; // 실제 전투와 카드 입력 환경 재사용

public sealed class WinterfallStatusRelicTests // 적 턴 약화 피해와 정화 및 은신 유물 검사
{ // 상태 유물 회귀 검사 시작
    [UnityTest] // 약화 종류와 실제 적 턴에서의 피해 검사
    public IEnumerator CageUsesThreeDistinctActiveDebuffsAtTheActualEnemyTurnStart() // 적 턴 피해와 마지막 적 처치 순서 확인
    { // 적 턴 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 배포 유물과 첫 턴을 연결하는 모드 진입
        Acquire("RiftEaterCage"); // 균열 먹는 새장의 실제 원본 획득
        using (var fight = new Fight("THRUST")) // 실제 첫 아군 턴과 네 생존 적 생성
        { // 개별 약화와 실제 일반 피해 비교 시작
            var eligible = fight.Enemies[0]; // 서로 다른 활성 약화 세 종류를 받을 적
            var stacked = fight.Enemies[1]; // 같은 약화가 세 번 중첩될 비교 적
            var expired = fight.Enemies[2]; // 세 약화 중 한 종류가 이미 만료된 비교 적
            var blocked = fight.Enemies[3]; // 같은 시작 피해를 방어도로 받을 별도 적
            ApplyThree(eligible); // 활성 약화 세 종류 부여
            ApplyThree(expired); // 곧 만료될 효과를 포함한 세 종류 부여
            ApplyThree(blocked); // 두 번째 생존 대상에 독립 약화 세 종류 부여
            expired.GetStatusEffect(BattleStatusEffectType.Fear).AdvanceDuration(); // 만료 카운터를 남겨 둔 상태 경계 준비
            for (int index = 0; index < 3; index++) // 한 종류의 세 차례 실제 약화 적용
            { // 같은 약화 중첩 시작
                stacked.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedDown, 1, 3, 3); // 다른 종류를 추가하지 않는 세 중첩
            } // 같은 약화 중첩 종료
            blocked.GainBlock(3); // 실제 일반 피해에서 차감되는 기존 방어도
            Assert.AreEqual(200, eligible.CurrentHealth); // 최초 아군 턴에는 새장 피해 미발동 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 시작으로 전환
            Assert.AreEqual(195, eligible.CurrentHealth); // 첫 대상의 고정 일반 피해 다섯 점 확인
            Assert.AreEqual(198, blocked.CurrentHealth); // 두 번째 대상의 방어도 세 점 적용 확인
            Assert.AreEqual(200, stacked.CurrentHealth); // 같은 종류의 중첩을 세 약화로 세지 않음 확인
            Assert.AreEqual(200, expired.CurrentHealth); // 만료한 효과를 세 번째 활성 약화로 세지 않음 확인
        } // 개별 약화와 실제 일반 피해 비교 종료
        using (var fight = new Fight("THRUST")) // 마지막 실제 적만 남길 두 번째 전투 생성
        { // 적 전멸 결과 순서 검사 시작
            var last = fight.Enemies[0]; // 턴 시작 피해로 쓰러질 마지막 적
            for (int index = 1; index < fight.Enemies.Length; index++) // 마지막 적 이외의 상대 순회
            { // 나머지 생존 적 제거 시작
                fight.Enemies[index].TakeDamage(1000, BattleDamageType.None); // 실제 적 제외 처리
            } // 나머지 생존 적 제거 종료
            last.TakeDamage(195, BattleDamageType.None); // 새장의 시작 공격으로 쓰러질 체력 다섯 점 설정
            ApplyThree(last); // 마지막 적에 활성 약화 세 종류 부여
            int phaseEvents = 0; // 승리 뒤 이어지면 잘못된 적 턴 알림 횟수
            fight.Turns.PhaseStarted += (_, __) => phaseEvents++; // 실제 턴 시작 발행을 직접 감시
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 턴으로 가는 실제 플레이어 입력
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 적 전멸의 기존 실제 승리 정산 확인
            Assert.AreEqual(0, phaseEvents); // 승리한 뒤 적 행동과 상태 처리 알림 발행 방지 확인
        } // 적 전멸 결과 순서 검사 종료
        yield return new ExitPlayMode(); // 독립 플레이 검사 모드 종료
    } // 적 턴 피해 검사 종료

    [UnityTest] // 상태 면역과 일반 확률 정화의 실행 순서 검사
    public IEnumerator SealRollsOnlyAfterValidIncomingDebuffsAndClearsOldStacks() // 신규 약화 및 누적 약화의 즉시 제거 확인
    { // 봉인핵 발동과 난수 경계 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 원본을 보유할 모드 진입
        Acquire("NecrateSealCore"); // 네크레이트 봉인핵 실제 원본 획득
        using (var fight = new Fight("THRUST")) // 생존 피격 아군이 있는 실제 전투 시작
        { // 아군과 적에게 오는 상태 처리 비교 시작
            var ally = fight.Allies[0]; // 확률 약화 정화 대상 아군
            var enemy = fight.Enemies[0]; // 정상 상태 적용의 원인과 비교 적
            int failed = SeedForFirstRoll(20); // 이십 퍼센트보다 처음으로 높은 실패 경계
            int success = SeedForFirstRoll(19); // 이십 퍼센트 판정의 마지막 성공 경계
            Random.InitState(failed); // 실패 경계에서 최초 정상 약화 부여 준비
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 3, 3, enemy)); // 경계 바로 밖의 첫 약화는 유지 확인
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Bleed).StackCount); // 먼저 걸린 약화의 원래 한 중첩 확인
            Random.InitState(success); // 같은 약화의 유효 중첩을 정화할 경계 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 3, 3, enemy); // 새 약화 중첩과 기존 상태 전체 제거
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Bleed)); // 먼저 걸린 중첩도 정화되는지 확인
            Random.InitState(success); // 신규 약화도 정화될 독립 성공 경계 지정
            ally.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedDown, 1, 1, 1, enemy); // 정상 면역 뒤 신규 약화 정화 시도
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.ActionSpeedDown)); // 활성 상태에 남지 않는 신규 약화 확인
            ally.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 약화보다 먼저 작동하는 일반 면역 활성화
            Random.InitState(success); // 무효 시도 후 보존될 실제 난수 상태 지정
            var unchanged = Random.state; // 유효 상태만 난수를 쓰는지 비교할 기준
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 3, 1, enemy)); // 면역 단계에서 정화보다 먼저 종료 확인
            ally.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 독립 공포 저항 시험을 위해 일반 면역 해제
            ally.ApplyStatusEffect(BattleStatusEffectType.FearResistance, 100, 2, 1); // 기존 공포 전용 선행 차단 효과 설정
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, ally.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 3, 1, enemy)); // 공포 저항에 이미 차단된 약화 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, ally.ApplyStatusEffect(BattleStatusEffectType.Collapse, 1, 3, 1, enemy)); // 보호 정신 상태의 일반 부여 불가 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, ally.ApplyStatusEffect(BattleStatusEffectType.Bleed, 0, 3, 1, enemy)); // 무효 수치의 약화 발동 제외 확인
            enemy.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedDown, 1, 1, 1, ally); // 적에게 적용한 정상 약화 비교
            Assert.AreEqual(unchanged, Random.state); // 모든 제외 분기에서 빈 정화 난수 소비 방지 확인
        } // 아군과 적에게 오는 상태 처리 비교 종료
        yield return new ExitPlayMode(); // 독립 플레이 검사 모드 종료
    } // 봉인핵 발동과 난수 경계 검사 종료

    [UnityTest] // 실제 은신 종료 뒤 한 번의 직접 명중 피해 검사
    public IEnumerator BraceletRefreshesOnEachStealthExitAndWaitsForALandedDirectHit() // 은신 만료와 해제 및 명중 소비 확인
    { // 어둠에 잠긴 팔찌 발동 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 선택과 피해 이벤트를 연결하는 모드 진입
        Acquire("DarkenedBracelet"); // 어둠에 잠긴 팔찌의 실제 원본 획득
        using (var fight = new Fight("THRUST", "THRUST", "THRUST")) // 정확한 원본 피해가 정해진 물리 타격 세 장 준비
        { // 은신과 실제 카드 명중 연결 확인 시작
            var owner = fight.Allies[0]; // 다음 공격 시 한 번만 팔찌 피해를 쓰는 아군
            var fallen = fight.Allies[1]; // 은신 도중 사망한 비교 아군
            var enemy = fight.Enemies[0]; // 가산 수치를 기록할 실제 적
            var bonus = BattleStatusEffectType.StealthBreakDamageUp; // 팔찌의 독립 일회 상태 종류
            owner.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 1, 1); // 실제 아군 은신 지속 시작
            Assert.IsNull(owner.GetStatusEffect(bonus)); // 앞선 은신 획득 시점에는 예약 없음 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 자신의 턴에 부여된 첫 기간 감소 생략 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 아군 턴 종료에서 은신 만료
            Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.Stealth)); // 실제 은신이 사라진 정상 만료 확인
            Assert.AreEqual(8, owner.GetStatusEffect(bonus).EffectiveValue); // 정상 만료로 정확히 여덟 점 부여 확인
            owner.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 3, 1); // 두 번째 실제 은신 상태 적용
            Assert.IsTrue(owner.RemoveStatusEffect(BattleStatusEffectType.Stealth)); // 이번에는 기간 종료 전 수동 제거
            Assert.IsFalse(owner.RemoveStatusEffect(BattleStatusEffectType.Stealth)); // 이미 없는 은신의 두 번째 제거 방지 확인
            Assert.AreEqual(1, owner.GetStatusEffect(bonus).StackCount); // 미사용 예약의 중첩 없이 한 번만 갱신 확인
            fallen.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 3, 1); // 다른 아군의 사망 전에 실제 은신 적용
            fallen.TakeDamage(1000, BattleDamageType.None, enemy); // 사망으로 모든 일반 상태가 함께 제거되는 실제 타격
            Assert.IsNull(fallen.GetStatusEffect(bonus)); // 사망 정리에서 별도 공격 예약이 생기지 않음 확인
            enemy.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 3, 1); // 비교 적에게 실제 은신 상태 적용
            enemy.RemoveStatusEffect(BattleStatusEffectType.Stealth); // 적의 은신 해제에 발동하지 않는지 비교
            Assert.IsNull(enemy.GetStatusEffect(bonus)); // 아군 전용 유물의 적 제외 확인
            enemy.TakeDamage(3, BattleDamageType.None, owner); // 같은 아군이 낸 비카드 일반 피해 비교
            Assert.AreEqual(8, owner.GetStatusEffect(bonus).EffectiveValue); // 비카드 피해에 첫 공격 예약이 남음 확인
            enemy.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 다음 카드가 빗나갈 원래 회피 적용
            int printed = fight.View("THRUST").RuntimeCard.EffectValue; // 피해 가산 전 실제 원본 카드 수치
            yield return fight.Use("THRUST", 0); // 실제 카드에서 빗나간 공격 실행
            Assert.AreEqual(8, owner.GetStatusEffect(bonus).EffectiveValue); // 빗나가면 공격 예약이 소모되지 않는지 확인
            enemy.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 같은 적에게 실제 명중할 비교 상태 복원
            enemy.GainBlock(100); // 명중과 체력 손실이 다른 방어도 상황 준비
            int raw = 0; // 실제 적 피해 처리의 방어 전 수치 기록
            enemy.DamageTaken += (_, result) => raw = result.RawDamage; // 카드가 전달한 진짜 방어 전 피해 감시
            yield return fight.Use("THRUST", 0); // 첫 유효 명중이지만 체력 피해는 모두 막히는 공격
            Assert.AreEqual(printed + 8, raw); // 첫 유효 명중에서 방어도 앞 피해 여덟 점 가산 확인
            Assert.IsNull(owner.GetStatusEffect(bonus)); // 실제 명중에서 한 번의 팔찌 상태 소모 확인
            yield return fight.Use("THRUST", 0); // 뒤이은 정상 카드 명중 실행
            Assert.AreEqual(printed, raw); // 같은 은신 해제의 추가 소비가 없는 정상 피해 확인
            owner.ApplyStatusEffect(BattleStatusEffectType.Stealth, 1, 3, 1); // 전투 종료 전 마지막 별도 은신 상태 적용
            Assert.IsTrue(fight.Turns.TryEscape()); // 진행 중 전투를 벗어나 종료 상태로 전환
            owner.RemoveStatusEffect(BattleStatusEffectType.Stealth); // 전투 종료 뒤 남은 상태 정리
            Assert.IsNull(owner.GetStatusEffect(bonus)); // 전투 밖 정리에는 팔찌 상태 미지급 확인
        } // 은신과 실제 카드 명중 연결 확인 종료
        yield return new ExitPlayMode(); // 팔찌 실제 재생 모드 종료
    } // 어둠에 잠긴 팔찌 발동 검사 종료

    private static void ApplyThree(BattleUnitRuntime enemy) // 상태 피해 없는 별도 세 종류의 약화 부여
    { // 비교 적 약화 준비 시작
        enemy.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 1, 3, 1); // 첫째 보통 물리 방어 약화
        enemy.ApplyStatusEffect(BattleStatusEffectType.ActionSpeedDown, 1, 3, 1); // 둘째 독립 행동 속도 약화
        enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 1, 1); // 셋째 한 차례 지정 공포
    } // 비교 적 약화 준비 종료

    private static void Acquire(string key) // 게임 빌드와 같은 탐사 유물 등록
    { // 실제 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 원본 유물의 실제 리소스 경로 조회
        Assert.IsNotNull(relic, key); // 지정 유물의 빌드 등록 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 탐사 보관함에 해당 원본 등록
    } // 실제 유물 획득 종료

    private static int SeedForFirstRoll(int expected) // 지정한 정수 확률을 먼저 내는 실제 게임 난수 조회
    { // 이십 퍼센트 시험용 난수 전제 탐색 시작
        for (int seed = 0; seed < 5000; seed++) // 재현 가능한 유한 시드 후보 순회
        { // 각 난수 원본 검사 시작
            Random.InitState(seed); // 해당 시드의 실제 엔진 난수 초기화
            if (Random.Range(0, 100) == expected) // 예상한 무작위 정수가 첫 결과인지 확인
            { // 확률 경계 시드 발견 시작
                return seed; // 다음 입력에 사용할 씨앗 반환
            } // 확률 경계 시드 발견 종료
        } // 각 난수 원본 검사 종료
        Assert.Fail("지정한 약화 정화 확률 경계 시드 미발견"); // 검사의 난수 전제에 대한 실패 알림
        return 0; // 단언 실패 뒤의 정수 반환 계약
    } // 이십 퍼센트 시험용 난수 전제 탐색 종료
} // 상태 유물 회귀 검사 종료
