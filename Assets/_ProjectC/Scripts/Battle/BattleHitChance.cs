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
        int evasionLoss = target?.GetStatusEffect(BattleStatusEffectType.EvasionDown)?.EffectiveValue ?? 0; // 대상 회피 감소 값 조회
        evasion = Math.Max(0, evasion - evasionLoss); // 회피 감소를 실명 보정과 분리해 영점까지 적용
        int disorientation = Math.Min(100, attacker.GetStatusEffect(BattleStatusEffectType.Disorientation)?.EffectiveValue ?? 0); // 실명과 별개인 방랑자 명중 감소
        int poisonPenalty = attacker.Team == BattleTeam.Enemy && attacker.GetStatusEffect(BattleStatusEffectType.Poison) is { IsExpired: false } // 실제 적 공격자의 현재 활성 중독 확인
            ? Math.Clamp(BattleSupplyRelics.Find(RelicEffectType.WineStainPoisonAccuracyLoss)?.EffectValue ?? 0, 0, 100) // 실제 보유한 포도주 얼룩의 고정 퍼센트포인트 조회
            : 0; // 중독 없는 적과 아군의 기존 명중 보정 유지
        var accuracy = attacker.GetStatusEffect(BattleStatusEffectType.BattlefieldMapAccuracyUp); // 공격자의 현재 지도 명중 강화 조회
        int mapAccuracyBonus = accuracy is { IsExpired: false } ? Math.Clamp(accuracy.EffectiveValue, 0, 100) : 0; // 실제 활성 지도 강화의 정수 퍼센트포인트
        int markedBonus = attacker.Team == BattleTeam.Ally && target is { Team: BattleTeam.Enemy, IsDead: false } && // 실제 아군 공격과 살아 있는 적 대상 확인
            (target.HasActiveCardDamageMark || target.GetStatusEffect(BattleStatusEffectType.DuelMark) is { IsExpired: false }) // 기존 카드 표식 또는 현재 활성 결투 표식
            ? Math.Clamp(BattleSupplyRelics.Find(RelicEffectType.BrokenLensCapMarkedAccuracy)?.EffectValue ?? 0, 0, 100) : 0; // 원본 렌즈캡의 고정 명중 퍼센트포인트
        return Math.Clamp(100 + mapAccuracyBonus + markedBonus - penalty - evasion - disorientation - poisonPenalty, 0, 100); // 모든 가감의 최종 명중 백분율을 예고와 실제에서 공용 계산
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
