using System; // 입력 범위 보정 사용
using UnityEngine; // 전투 무작위 판정 사용
public static class BattleHitChance // 공격 명중 여부 계산
{ // 명중 판정 시작
    public static int GetPercent(BattleUnitRuntime attacker, BattleUnitRuntime target = null) // 실명과 대상 회피를 반영한 공격 명중률
    { // 기존 명중과 예측의 기본 함수 시작
        return GetPercentWithBlindOverride(attacker, target, false); // 원래 타격에서는 실명도 실제 판단에 포함
    } // 기존 명중과 예측의 기본 함수 종료

    internal static int GetPercentWithBlindOverride(BattleUnitRuntime attacker, BattleUnitRuntime target, bool ignoreBlind) // 원래 모든 명중 보정과 적용 가능한 실명 무시
    { // 명중률 조회 시작
        if (attacker == null || attacker.IsDead) // 공격자 생존 확인
        { // 공격 불가 처리 시작
            return 0; // 명중률 없음 반환
        } // 공격 불가 처리 종료
        BattleStatusEffectInstance blind = ignoreBlind ? null : attacker.GetStatusEffect(BattleStatusEffectType.Blind); // 같은 공격의 실명 감소만 제외하고 다른 명중 계산 유지
        int penalty = blind == null ? 0 : Math.Min(100, blind.EffectiveValue); // 실명 명중률 감소 범위 보정
        bool canEvade = target?.GetStatusEffect(BattleStatusEffectType.Fear) == null; // 기존 공포와 회피 가능 상태 확인
        int evasion = canEvade ? Math.Min(100, target?.GetStatusEffect(BattleStatusEffectType.Evasion)?.EffectiveValue ?? 0) : 0; // 회피 가능한 대상의 기존 회피율
        if (canEvade && target?.Team == BattleTeam.Ally && !target.IsDead && target.GetStatusEffect(BattleStatusEffectType.Stealth) is { IsExpired: false }) // 실제 은신이 유지되는 생존 아군의 회피 조건 확인
        { // 꺼진 등불 심지의 은신 회피 시작
            int bonus = BattleSupplyRelics.Find(RelicEffectType.StealthEvasionBonus)?.EffectValue ?? 0; // 현재 보유한 심지의 추가 회피 수치
            evasion = (int)Math.Min(100, (long)evasion + bonus); // 기존 회피와 퍼센트포인트 합산 및 백 상한
        } // 꺼진 등불 심지의 은신 회피 종료
        int evasionLoss = target?.GetStatusEffect(BattleStatusEffectType.EvasionDown)?.EffectiveValue ?? 0; // 대상 회피 감소 값 조회
        var magneticLoss = target?.GetStatusEffect(BattleStatusEffectType.MagneticEvasionDown); // 현재 대상의 자기장 회피 약화 조회
        int magneticPercent = magneticLoss is { IsExpired: false } ? magneticLoss.EffectiveValue : 0; // 적 차례가 끝나기 전 실제 활성 자기장 퍼센트포인트
        evasion = (int)Math.Clamp((long)evasion - evasionLoss - magneticPercent, 0L, 100L); // 두 별도 회피 감소의 합계와 정상 회피 범위 적용
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
    { // 원래 별도 공격의 명중률 적용 시작
        return RollWithBlindOverride(attacker, target, false); // 고유 공격 범위의 면제 없이 기본 판정 사용
    } // 원래 별도 공격의 명중률 적용 종료

    internal static bool RollWithBlindOverride(BattleUnitRuntime attacker, BattleUnitRuntime target, bool ignoreBlind) // 같은 실제 명중 판정에서 적용 가능한 실명 제외
    { // 명중 굴림 시작
        if (attacker == null || attacker.IsDead) // 공격자 생존 확인
        { // 공격 불가 처리 시작
            return false; // 공격 불가 반환
        } // 공격 불가 처리 종료
        int hitChance = GetPercentWithBlindOverride(attacker, target, ignoreBlind); // 원래 회피와 방향 감각을 보존한 실제 명중률 조회
        return hitChance == 100 || UnityEngine.Random.Range(0, 100) < hitChance; // 확정 명중 이외의 공격만 무작위 판정
    } // 명중 굴림 종료
} // 명중 판정 종료
