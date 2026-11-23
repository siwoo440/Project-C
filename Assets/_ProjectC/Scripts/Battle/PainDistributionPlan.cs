using System; // 피해 비율과 상한 계산
using System.Collections.Generic; // 고정 명단에 따른 분담자 목록

internal sealed class PainDistributionPlan // 한 직접 타격의 파티 피해 분배
{ // 피해 분배 계획 시작
    internal IReadOnlyList<BattleUnitRuntime> Recipients { get; } // 원래 피격자를 제외한 생존 분담자
    internal int Total { get; } // 원래 피해에서 뺄 전체 분담 피해

    private PainDistributionPlan(List<BattleUnitRuntime> recipients, int total) // 확정된 한 타격과 적용 대상 보관
    { // 확정 분배 생성 시작
        Recipients = recipients; // 실제 출전 명단 순서 유지
        Total = total; // 전체 피해의 내림한 분담 몫 저장
    } // 확정 분배 생성 종료

    internal static PainDistributionPlan Create(BattleUnitRuntime primary, IReadOnlyList<BattleUnitRuntime> party, int damage) // 직접 피격의 방어도 이전 파티 분담 계산
    { // 실제 분배 규칙 시작
        if (party == null || damage <= 0 || !IsProtectedLivingAlly(primary)) // 양수 피해와 실제 고통 분산이 있는 주 대상 확인
        { // 분담이 없는 경우 시작
            return null; // 기존 직접 피해 그대로 처리
        } // 분담이 없는 경우 종료
        int percent = Math.Min(100, primary.GetStatusEffect(BattleStatusEffectType.PainDistribution).EffectiveValue); // 원래 피격자 상태의 실제 비율
        int total = (int)((long)damage * percent / 100); // 피해 비율을 한 번만 내린 파티 분담량
        if (total == 0) // 소수점 미만인 작은 타격 확인
        { // 양수 분담 없음 처리 시작
            return null; // 작은 타격의 원래 피해 유지
        } // 양수 분담 없음 처리 종료
        var recipients = new List<BattleUnitRuntime>(); // 실제 살아 있는 다른 보호 대상 목록
        foreach (BattleUnitRuntime ally in party) // 고정된 출전 명단 순서로 후보 확인
        { // 현재 아군의 분담 조건 검사 시작
            if (ally != primary && IsProtectedLivingAlly(ally)) // 자기 타격에 자기 몫을 다시 추가하지 않는 조건
            { // 다른 생존 대상 등록 시작
                recipients.Add(ally); // 같은 상태가 남아 있는 실제 분담자 기록
            } // 다른 생존 대상 등록 종료
        } // 현재 아군의 분담 조건 검사 종료
        return recipients.Count == 0 ? null : new PainDistributionPlan(recipients, total); // 혼자일 때 분담 없는 원래 피해 보존
    } // 실제 분배 규칙 종료

    internal int GetPortion(int index) // 고정 명단 순서에 따른 개별 분담 피해
    { // 균등 분배 시작
        return Total / Recipients.Count + (index < Total % Recipients.Count ? 1 : 0); // 앞자리부터 한 점씩 나머지 배정
    } // 균등 분배 종료

    private static bool IsProtectedLivingAlly(BattleUnitRuntime unit) // 실제 전투에서 고통 분산을 가진 생존 아군 확인
    { // 고통 분산 대상 확인 시작
        return unit != null && unit.Team == BattleTeam.Ally && !unit.IsDead && unit.GetStatusEffect(BattleStatusEffectType.PainDistribution) is { IsExpired: false }; // 만료와 사망을 제외한 상태 보유 조건
    } // 고통 분산 대상 확인 종료
} // 피해 분배 계획 종료
