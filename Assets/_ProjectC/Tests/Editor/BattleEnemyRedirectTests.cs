using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class BattleEnemyRedirectTests // 적 공격 유도 기능 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void RedirectAttack_ChangesOnlyValidPlannedAttackAndNotifiesView() // 유효한 공격 대상 변경 확인
    { // 테스트 시작
        CharacterData firstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 아군 데이터 생성
        CharacterData guardData = ScriptableObject.CreateInstance<CharacterData>(); // 방어 아군 데이터 생성
        CharacterData outsiderData = ScriptableObject.CreateInstance<CharacterData>(); // 외부 아군 데이터 생성
        EnemyData attackerData = ScriptableObject.CreateInstance<EnemyData>(); // 공격 적 데이터 생성
        EnemyData statusData = ScriptableObject.CreateInstance<EnemyData>(); // 상태 효과 적 데이터 생성
        typeof(EnemyData).GetField("actionType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(statusData, EnemyActionType.ApplyStatusEffect); // 상태 효과 행동 설정
        typeof(EnemyData).GetField("statusEffectType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(statusData, BattleStatusEffectType.Poison); // 중독 효과 설정
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(firstData); // 첫 아군 생성
        BattleUnitRuntime guard = BattleUnitRuntime.CreateAlly(guardData); // 방어 아군 생성
        BattleUnitRuntime outsider = BattleUnitRuntime.CreateAlly(outsiderData); // 외부 아군 생성
        BattleUnitRuntime attacker = BattleUnitRuntime.CreateEnemy(attackerData); // 공격 적 생성
        BattleUnitRuntime statusEnemy = BattleUnitRuntime.CreateEnemy(statusData); // 상태 효과 적 생성
        BattleEnemyActionRuntime runtime = new BattleEnemyActionRuntime(new[] { attacker, statusEnemy }, new[] { first, guard }); // 적 행동 관리자 생성
        Assert.AreEqual(2, runtime.PrepareActions()); // 두 적 행동 준비 확인
        int notifications = 0; // 행동 변경 알림 횟수
        runtime.StateChanged += () => notifications++; // 행동 변경 알림 감시
        Assert.IsFalse(runtime.TryRedirectAttack(attacker, outsider)); // 외부 아군 대상 차단 확인
        Assert.IsFalse(runtime.TryRedirectAttack(statusEnemy, guard)); // 상태 효과 유도 차단 확인
        Assert.IsFalse(runtime.TryRedirectAttack(attacker, first)); // 동일 대상 변경 차단 확인
        Assert.IsTrue(runtime.TryRedirectAttack(attacker, guard)); // 공격 유도 성공 확인
        Assert.AreSame(guard, runtime.FindAction(attacker).Target); // 공격 대상 변경 확인
        Assert.AreSame(first, runtime.FindAction(statusEnemy).Target); // 다른 적 행동 유지 확인
        Assert.AreEqual(1, notifications); // 유효 변경 시 알림 한 번 확인
        int previousHealth = guard.CurrentHealth; // 방어 아군 체력 저장
        runtime.ExecuteAction(runtime.FindAction(attacker)); // 유도된 적 공격 실행
        Assert.Less(guard.CurrentHealth, previousHealth); // 변경된 대상의 피해 확인
        Assert.AreEqual(first.MaxHealth, first.CurrentHealth); // 기존 대상 피해 없음 확인
        runtime.PrepareActions(); // 다음 적 행동 준비
        guard.TakeDamage(100000, BattleDamageType.Physical); // 방어 아군 사망 처리
        Assert.IsFalse(runtime.TryRedirectAttack(attacker, guard)); // 사망 아군 대상 차단 확인
        Assert.AreSame(first, runtime.FindAction(attacker).Target); // 생존 아군 대상 유지 확인
        runtime.Dispose(); // 적 행동 관리자 정리
        Assert.IsFalse(runtime.TryRedirectAttack(statusEnemy, guard)); // 정리된 관리자 변경 차단 확인
        Object.DestroyImmediate(firstData); // 첫 아군 데이터 정리
        Object.DestroyImmediate(guardData); // 방어 아군 데이터 정리
        Object.DestroyImmediate(outsiderData); // 외부 아군 데이터 정리
        Object.DestroyImmediate(attackerData); // 공격 적 데이터 정리
        Object.DestroyImmediate(statusData); // 상태 효과 적 데이터 정리
    } // 테스트 종료
} // 테스트 클래스 종료
