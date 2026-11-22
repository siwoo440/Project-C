using System; // 현재 유물 검사 정리 범위
using System.Collections; // 실제 손패 입력과 플레이 모드 검사
using NUnit.Framework; // 타격과 상태 수치 비교
using UnityEngine; // Unity 원본과 확률 시드
using UnityEngine.TestTools; // 실제 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 검사 구성
using Object = UnityEngine.Object; // 임시 유물과 관리자 정리
using Random = UnityEngine.Random; // 실제 전투 정수 난수

public sealed class WinterfallFactoryEnemyControlTests // 고철 공장 적 제어 유물 세 종의 실제 전투 검사
{ // 원본 적 제어 검사 시작
    [UnityTest] // 행동 제한 유물을 장착한 실제 피해 경로
    public IEnumerator BadgeIncreasesOnlyLiveStunOrActionPointLossDamage() // 기절과 행동력 감소의 예고 및 실제 명중
    { // 피해 증가 환경 시작
        yield return new EnterPlayMode(); // 실제 손패 카드가 있는 녹색 전투 환경
        using (var owned = new OwnedRelic("SecurityEnforcerBadge")) // 보안 집행체 배지의 실제 장착
        using (var fight = new Fight("RUNE_BURST", "RUNE_BURST")) // 유효 상태와 일반 상태 비교 카드 두 장
        { // 실제 직접 피해 비교 시작
            var ally = fight.Allies[0]; // 원본 마법 카드 소유자
            var stunned = fight.Enemies[0]; // 기절 상태를 받을 생존 적
            var reduced = fight.Enemies[1]; // 행동력 감소를 받을 생존 적
            var staggered = fight.Enemies[2]; // 일반 경직을 받을 생존 적
            var expired = fight.Enemies[3]; // 만료된 행동력 감소의 대조 적
            stunned.ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1, ally); // 실제 기절 부여
            reduced.ApplyStatusEffect(BattleStatusEffectType.ActionPointDown, 1, 1, 1, ally); // 별도 행동력 감소 부여
            staggered.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 1, 1, ally); // 별개인 일반 경직 부여
            expired.ApplyStatusEffect(BattleStatusEffectType.ActionPointDown, 1, 1, 1, ally); // 만료할 실제 행동력 감소 준비
            expired.GetStatusEffect(BattleStatusEffectType.ActionPointDown).AdvanceDuration(); // 목록에 남은 종료 상태의 직접 만료
            Assert.AreEqual(23, stunned.PreviewDamage(20, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 기절의 직접 피해 증가 15퍼센트
            Assert.AreEqual(23, reduced.PreviewDamage(20, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 행동력 감소의 같은 직접 피해 증가
            Assert.AreEqual(20, staggered.PreviewDamage(20, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 일반 경직의 원래 직접 피해
            Assert.AreEqual(20, expired.PreviewDamage(20, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 만료 상태의 원래 직접 피해
            Assert.AreEqual(20, stunned.TakeDamage(20, BattleDamageType.None, ally).AppliedDamage); // 일반 피해의 원래 수치 유지
            BattleDamageResult first = default; // 실제 기절 타격의 원본 피해 기록
            BattleDamageResult second = default; // 실제 경직 타격의 원본 피해 기록
            stunned.DamageTaken += (_, result) => first = result; // 첫 실제 카드 피해 확인
            staggered.DamageTaken += (_, result) => second = result; // 둘째 실제 카드 피해 확인
            yield return fight.Use("RUNE_BURST", 0); // 기절 적에 정상 카드 명중
            yield return fight.Use("RUNE_BURST", 2); // 일반 경직 적의 같은 정상 명중
            Assert.AreEqual(12, first.RawDamage); // 실제 기존 11 피해에 비율을 내림한 결과
            Assert.AreEqual(11, second.RawDamage); // 일반 경직의 실제 카드 피해 유지
        } // 실제 직접 피해 비교 종료
        yield return new ExitPlayMode(); // 독립 검사 정리
    } // 피해 증가 환경 종료

    [UnityTest] // 실제 연속 카드와 적 행동 한 건의 취소 확인
    public IEnumerator DroneRollsOncePerMarkedFoeAndConsumesOneEnemyAction() // 같은 카드의 중복 적중과 새 카드의 독립 기회
    { // 표식과 적 행동 검사 시작
        yield return new EnterPlayMode(); // 원래 플레이 모드 진입
        using (var owned = new OwnedRelic("RecoveryDroneHook")) // 회수 드론 갈고리 원본 장착
        using (var fight = new Fight("DOUBLE_SLASH", "DOUBLE_SLASH")) // 같은 대상 두 연속 타격과 이후 원본 카드
        { // 실제 적중과 한 번의 행동 취소 시작
            var ally = fight.Allies[0]; // 실제 검 카드의 아군 공격자
            var marked = fight.Enemies[0]; // 정상 표식 대상
            marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 3, 1, ally); // 실제 카드 피해 표식 부여
            int attempts = 0; // 실제 약화 성공 및 갱신 판정 횟수
            int hits = 0; // 원본의 실제 명중 횟수 확인
            marked.DamageTaken += (_, result) => hits++; // 두 실제 피해 처리가 일어났는지 기록
            marked.StatusEffectResolved += (_, _, type, _) => // 실제 행동력 감소 결과 확인
            { // 같은 적의 새 약화와 갱신 수 집계 시작
                if (type == BattleStatusEffectType.ActionPointDown) // 회수 드론의 실제 상태 출처 확인
                { // 행동 취소 시도 집계 시작
                    attempts++; // 성공 또는 중첩 시 새 약화 이벤트 수
                } // 행동 취소 시도 집계 종료
            }; // 같은 적의 새 약화와 갱신 수 집계 종료
            int seed = FindTwoSuccessfulRolls(owned.Data.TriggerChancePercent); // 두 번 굴리면 모두 성공할 실제 확률 시드
            fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 두 원본 카드에 사용할 실제 난수의 동일한 초기 상태
            yield return fight.Use("DOUBLE_SLASH", 0); // 표식 적에게 동일 공격의 두 타격 적중
            Assert.AreEqual(2, hits); // 같은 적을 친 원본 연속 피해 두 건
            Assert.AreEqual(1, attempts); // 원래 카드 한 장 내 한 상태 발동 제한
            Assert.AreEqual(1, marked.GetStatusEffect(BattleStatusEffectType.ActionPointDown)?.RemainingTurns); // 다음 적 행동 또는 실제 적 턴의 기간
            marked.RemoveStatusEffect(BattleStatusEffectType.PublicMark); // 실제 표식을 해제한 뒤 새 원본 카드 비교
            yield return fight.Use("DOUBLE_SLASH", 0); // 표식 없는 같은 적의 연속 공격
            Assert.AreEqual(4, hits); // 실제 두 번째 카드의 연속 피해 발생
            Assert.AreEqual(1, attempts); // 표식이 없는 공격은 행동력 감소 발동 제외
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 이전 실제 공격을 소비할 적 차례 시작
            var first = MakeAttack(marked, ally); // 실제 적 공격의 원래 예정 행동
            Assert.AreEqual(BattleStatusEffectType.ActionPointDown, first.BlockingStatus); // 실제 적 예고의 행동 취소 원인 표시
            Assert.AreEqual(0, first.Execute().DamageResult.AppliedDamage); // 한 차례 취소의 실제 피해 없음
            Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 실제 취소 즉시 한 번만 상태 소모
            var next = MakeAttack(marked, ally); // 같은 적의 다음 새로운 정상 공격
            Assert.AreEqual(BattleStatusEffectType.None, next.BlockingStatus); // 소모 뒤의 원래 공격 실행 가능
            Assert.Greater(next.Execute().DamageResult.AppliedDamage, 0); // 같은 상태가 다음 실제 행동에 반복되지 않음
        } // 실제 적중과 한 번의 행동 취소 종료
        yield return new ExitPlayMode(); // 실제 테스트의 독립 전투 종료
    } // 표식과 적 행동 검사 종료

    [UnityTest] // 적 선제 및 추가 아군 차례를 포함한 실제 회피 약화 검사
    public IEnumerator MagneticCoreCountsExtraAllyTurnsAndAgesOnRealEnemyTurns() // 세 번째 아군 차례와 약화의 실제 적 턴 수명
    { // 실제 주기와 적 회피 검사 시작
        yield return new EnterPlayMode(); // 실제 턴 이벤트가 있는 전투 환경
        using (var owned = new OwnedRelic("MagneticCompressionCore")) // 자기장 압축핵 원본 장착
        using (var fight = new Fight(true, "TIME_COIN")) // 적 선제와 실제 추가 턴 카드 준비
        { // 턴별 상태 검사 시작
            var ally = fight.Allies[0]; // 회피율 변화를 확인할 실제 아군
            var first = fight.Enemies[0]; // 일반 회피가 있는 생존 적
            var second = fight.Enemies[1]; // 같은 광역 효과를 받을 두 번째 적
            var immune = fight.Enemies[2]; // 기존 상태 면역으로 제외될 적
            var dead = fight.Enemies[3]; // 상태를 받지 않을 쓰러진 적
            first.ApplyStatusEffect(BattleStatusEffectType.Evasion, 30, 10, 1); // 기존 독립 회피 버프의 실제 명중 수치
            first.ApplyStatusEffect(BattleStatusEffectType.EvasionDown, 5, 8, 1); // 함께 유지할 별도 원본 회피 약화
            dead.TakeDamage(dead.MaxHealth * 3, BattleDamageType.None, ally); // 실제 쓰러진 적의 정상 처리
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)); // 선제 적 차례에서 발생하지 않는 유물
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 아군 차례 시작
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 두 번째 아군 차례 시작
            Assert.AreEqual(2, fight.Turns.CurrentRound); // 최초 적 선제가 아군 차례 집계에서 제외
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)); // 첫 두 아군 턴에서 약화 없음 확인
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 세 번째 차례의 원래 약화 면역 처리
            yield return fight.Use("TIME_COIN"); // 실제로 다음 아군 차례를 먼저 여는 원본 카드
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 세 번째 실제 아군 차례 시작
            Assert.AreEqual(3, fight.Turns.CurrentRound); // 시간 동전의 추가 차례도 실제 아군 시작
            Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)?.RemainingTurns); // 첫 생존 적에게 두 적 턴의 약화 적용
            Assert.AreEqual(10, second.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)?.EffectiveValue); // 두 번째 생존 적의 동일한 회피 감소
            Assert.AreEqual(85, BattleHitChance.GetPercent(ally, first)); // 기존 회피 30과 별도 약화 5 및 자기장 약화 10의 합계
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)); // 기존 약화 면역의 정상 적용
            Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)); // 쓰러진 적에게 신규 약화 없음
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 약화의 첫 실제 적 차례 시작
            Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)?.RemainingTurns); // 적 종료에만 수명을 줄이는 규칙
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 차례 종료
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)?.RemainingTurns); // 첫 적 차례만 차감한 실제 수명
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 둘째 실제 적 차례 완료
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown)); // 두 적 차례 뒤의 만료 확인
            Assert.AreEqual(75, BattleHitChance.GetPercent(ally, first)); // 기존 별도 회피 약화만 남은 원래 명중 수치
        } // 턴별 상태 검사 종료
        yield return new ExitPlayMode(); // 별도 전투와 유물 정리
    } // 실제 주기와 적 회피 검사 종료

    private static BattleEnemyAction MakeAttack(BattleUnitRuntime enemy, BattleUnitRuntime ally) // 실제 적 공격의 독립 예정 행동 구성
    { // 정상 적 공격 구성 시작
        return new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Physical, 25, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "회수 드론 검사", 1, 1, 1, 0); // 기본 피해와 대상인 정상 공격
    } // 정상 적 공격 구성 종료

    private static int FindTwoSuccessfulRolls(int chance) // 유물 확률의 실제 성공 난수 두 개를 연속 준비
    { // 확률 시드 검색 시작
        for (int seed = 0; seed < 20000; seed++) // 원래 Unity 난수의 실제 시드 후보
        { // 다음 시드의 성공 조건 확인 시작
            Random.InitState(seed); // 같은 카드에 돌아올 실제 첫 난수
            if (Random.Range(0, 100) < chance && Random.Range(0, 100) < chance) // 두 무작위 시도가 모두 성공하는 정확한 경계 안
            { // 성공 시드의 원래 값 반환 시작
                return seed; // 실제 카드 발동 검사의 공통 난수 초기값
            } // 성공 시드의 원래 값 반환 종료
        } // 다음 시드의 성공 조건 확인 종료
        throw new InvalidOperationException("회수 드론의 연속 성공 난수 시드 미발견"); // 확률 검사 환경의 명시 오류
    } // 확률 시드 검색 종료

    private sealed class OwnedRelic : IDisposable // 실제 회차 인벤토리의 단일 유물 설치와 해제
    { // 같은 실제 보유 관리와 원본 처리 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 검사 전 유물 회차 관리자
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 현재 원래 획득 경로의 회차 관리자
        internal RelicData Data { get; } // 이번 테스트의 실제 원본 발동 데이터

        internal OwnedRelic(string key) // 실제 리소스 이름의 유물 장착
        { // 실물 원본 조회와 획득 시작
            Data = Resources.Load<RelicData>("Relics/" + key); // 실제 상점 및 도감에 쓰는 유물 원본
            Assert.IsNotNull(Data, key); // 원본 유물 존재 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Data)); // 현재 회차의 정상 유물 획득
        } // 실물 원본 조회와 획득 종료

        public void Dispose() // 매 전투 뒤 원래 유물 보유 상태 복구
        { // 테스트 임시 원본 정리 시작
            manager.TryRemove(Data.RelicId); // 이번 검사에서 실제 얻은 유물만 제거
            if (previous == null) // 원래 없던 유물 인벤토리 관리자 확인
            { // 새 임시 관리자 제거 시작
                Object.Destroy(manager.gameObject); // 독립 테스트가 만든 유니티 객체 제거
            } // 새 임시 관리자 제거 종료
        } // 테스트 임시 원본 정리 종료
    } // 같은 실제 보유 관리와 원본 처리 종료
} // 원본 적 제어 검사 종료
