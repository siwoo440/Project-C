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
        bool canEvade = target?.GetStatusEffect(BattleStatusEffectType.Fear) == null; // 기존 공포와 회피 가능 상태 확인
        int evasion = canEvade ? Math.Min(100, target?.GetStatusEffect(BattleStatusEffectType.Evasion)?.EffectiveValue ?? 0) : 0; // 회피 가능한 대상의 기존 회피율
        if (canEvade && target?.Team == BattleTeam.Ally && !target.IsDead && target.GetStatusEffect(BattleStatusEffectType.Stealth) is { IsExpired: false }) // 실제 은신이 유지되는 생존 아군의 회피 조건 확인
        { // 꺼진 등불 심지의 은신 회피 시작
            int bonus = BattleSupplyRelics.Find(RelicEffectType.StealthEvasionBonus)?.EffectValue ?? 0; // 현재 보유한 심지의 추가 회피 수치
            evasion = (int)Math.Min(100, (long)evasion + bonus); // 기존 회피와 퍼센트포인트 합산 및 백 상한
        } // 꺼진 등불 심지의 은신 회피 종료
        int disorientation = Math.Min(100, attacker.GetStatusEffect(BattleStatusEffectType.Disorientation)?.EffectiveValue ?? 0); // 실명과 별개인 방랑자 명중 감소
        return Math.Max(0, 100 - penalty - evasion - disorientation); // 모든 원인의 퍼센트포인트를 합산한 명중률
    } // 명중률 조회 종료
    public static bool Hits(BattleUnitRuntime attacker, int roll, BattleUnitRuntime target = null) // 공격자와 대상의 지정 난수 판정
    { // 명중 계산 시작
        if (attacker == null || attacker.IsDead || roll < 0 || roll >= 100) // 유효한 공격자와 난수 확인
        { // 명중 불가 처리 시작
            return false; // 잘못된 입력 실패 반환
        } // 명중 불가 처리 종료
        return roll < GetPercent(attacker, target); // 명중률과 동일한 경계 판정
    } // 명중 계산 종료
    public static bool Roll(BattleUnitRuntime attacker, BattleUnitRuntime target = null) // 공격 명중 무작위 판정
    { // 명중 굴림 시작
        if (attacker == null || attacker.IsDead) // 공격자 생존 확인
        { // 공격 불가 처리 시작
            return false; // 공격 불가 반환
        } // 공격 불가 처리 종료
        int hitChance = GetPercent(attacker, target); // 실명과 방향 감각을 반영한 아군 공격 명중률
        return hitChance == 100 || UnityEngine.Random.Range(0, 100) < hitChance; // 확정 명중 이외의 공격만 무작위 판정
    } // 명중 굴림 종료
} // 명중 판정 종료
