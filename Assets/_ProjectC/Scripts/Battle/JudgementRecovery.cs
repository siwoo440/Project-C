using System; // 회복 수치 범위 계산 사용
public static class JudgementRecovery // 심판의 위기 아군 회복 계산
{ // 위기 회복 기능 시작
    public static int GetHealingAmount(BattleUnitRuntime target, int baseHealing, int bonusHealing) // 회복 전 체력에 따른 회복량 계산
    { // 회복량 계산 시작
        if (target == null || target.IsDead || target.Team != BattleTeam.Ally) // 생존 아군 대상 확인
        { // 회복 불가 처리 시작
            return 0; // 회복 없음 반환
        } // 회복 불가 처리 종료
        bool atRisk = (long)target.CurrentHealth * 2 <= target.MaxHealth; // 최대 체력 절반 이하 확인
        long amount = Math.Max(0, baseHealing) + (atRisk ? (long)Math.Max(0, bonusHealing) : 0L); // 기본 회복과 위험 추가 회복 합산
        return (int)Math.Min(int.MaxValue, amount); // 정수 범위 내 회복량 반환
    } // 회복량 계산 종료
} // 위기 회복 기능 종료
