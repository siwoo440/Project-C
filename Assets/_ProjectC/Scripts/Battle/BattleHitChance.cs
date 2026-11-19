using System; // 입력 범위 보정 사용
using UnityEngine; // 전투 무작위 판정 사용
public static class BattleHitChance // 공격 명중 여부 계산
{ // 명중 판정 시작
    public static int GetPercent(BattleUnitRuntime attacker, BattleUnitRuntime target = null) // 실명과 대상 회피를 반영한 공격 명중률
    { // 명중률 조회 시작
        if (attacker == null || attacker.IsDead) // 공격자 생존 확인
        { // 공격 불가 처리 시작
            return 0; // 명중률 없음 반환
        } // 공격 불가 처리 종료
        BattleStatusEffectInstance blind = attacker.GetStatusEffect(BattleStatusEffectType.Blind); // 공격자의 실명 상태 조회
        int penalty = blind == null ? 0 : Math.Min(100, blind.EffectiveValue); // 실명 명중률 감소 범위 보정
        int evasion = Math.Min(100, target?.GetStatusEffect(BattleStatusEffectType.Evasion)?.EffectiveValue ?? 0); // 방어 대상의 회피율 조회
        return Math.Max(0, 100 - penalty - evasion); // 합산 감소 후 남은 공격 명중률
    } // 명중률 조회 종료
    public static bool Hits(BattleUnitRuntime attacker, int roll, BattleUnitRuntime target = null) // 공격자와 대상의 지정 난수 판정
    { // 명중 계산 시작
        if (attacker == null || attacker.IsDead || roll < 0 || roll >= 100) // 유효한 공격자와 난수 확인
        { // 명중 불가 처리 시작
            return false; // 잘못된 입력 실패 반환
        } // 명중 불가 처리 종료
        return roll < GetPercent(attacker, target); // 명중률과 동일한 경계 판정
    } // 명중 계산 종료
    public static bool Roll(BattleUnitRuntime attacker) // 공격 명중 무작위 판정
    { // 명중 굴림 시작
        if (attacker == null || attacker.IsDead) // 공격자 생존 확인
        { // 공격 불가 처리 시작
            return false; // 공격 불가 반환
        } // 공격 불가 처리 종료
        return attacker.GetStatusEffect(BattleStatusEffectType.Blind) == null || Hits(attacker, UnityEngine.Random.Range(0, 100)); // 실명 시에만 무작위 명중 판정
    } // 명중 굴림 종료
} // 명중 판정 종료
