using System; // 최대 체력과 정수 범위 계산
using System.Collections.Generic; // 생존 목록과 화면 변경 목록 보관

internal static class BattleHealthRedistribution // 파티 체력을 보존하는 전투 재분배
{ // 실제 체력 평준화 시작
    internal static long Apply(IReadOnlyList<BattleUnitRuntime> party, int block) // 현재 살아 있는 아군의 체력과 방어도 처리
    { // 아군 체력과 최대치 수집 시작
        var living = new List<BattleUnitRuntime>(); // 원래 파티 순서를 유지한 생존 목록
        long total = 0; // 개별 정수보다 큰 파티의 현재 체력 합계
        int highestCap = 0; // 살아 있는 아군 중 가장 높은 최대 체력
        foreach (var ally in party) // 카드가 닿을 수 있는 현재 파티 순회
        { // 생존 대상 검사 시작
            if (ally is { Team: BattleTeam.Ally, IsDead: false }) // 실제 살아 있는 아군 확인
            { // 재분배 입력 추가 시작
                living.Add(ally); // 같은 파티 순서로 생존 대상 추가
                total += ally.CurrentHealth; // 전투에서 실제 보유한 체력 합산
                highestCap = Math.Max(highestCap, ally.MaxHealth); // 가장 높은 최대치 보관
            } // 재분배 입력 추가 종료
        } // 생존 대상 검사 종료
        if (living.Count == 0) // 나눌 체력이 없는 파티 확인
        { // 빈 생존 목록 처리 시작
            return 0; // 체력과 방어도 변화 없음
        } // 빈 생존 목록 처리 종료

        int level = 1; // 살아 있는 모든 아군의 체력 최소값
        while (level < highestCap) // 합계를 넘지 않는 가장 큰 공통 체력 탐색
        { // 공통 체력의 정수 이분 탐색 시작
            int candidate = level + (int)(((long)highestCap - level + 1) / 2); // 최대 정수까지 안전한 상향 중간값
            if (SumAtLevel(living, candidate) <= total) // 후보 높이와 각 상한으로 나눌 수 있는 합계 확인
            { // 유효한 후보 처리 시작
                level = candidate; // 가능한 공통 체력의 하한 상향
            } // 유효한 후보 처리 종료
            else // 현재 파티 체력보다 높은 후보
            { // 과한 후보 처리 시작
                highestCap = candidate - 1; // 불가능한 후보보다 낮은 범위 선택
            } // 과한 후보 처리 종료
        } // 공통 체력의 정수 이분 탐색 종료

        long remainder = total - SumAtLevel(living, level); // 상한과 균등 체력 적용 뒤 남는 정수 점수
        var changed = new List<BattleUnitRuntime>(); // 실제 체력 표시를 갱신할 생존 아군
        foreach (var ally in living) // 동점 나머지를 원래 파티 순서대로 부여
        { // 개별 체력 확정 시작
            int assigned = Math.Min(level, ally.MaxHealth); // 최대치를 넘지 않는 공통 체력 보장
            if (remainder > 0 && assigned < ally.MaxHealth) // 나머지 한 점을 받을 여유가 있는지 확인
            { // 정수 나머지 지급 시작
                assigned++; // 현재 차례에 한 점 추가
                remainder--; // 이미 배정한 한 점 소모
            } // 정수 나머지 지급 종료
            if (assigned != ally.CurrentHealth && ally.ApplyPersistentHealth(assigned)) // 일반 피해나 회복 없이 최종 체력 반영
            { // 화면 변화가 있는 유닛 기록 시작
                changed.Add(ally); // 전체 합계 적용 뒤 알릴 실제 변경 대상
            } // 화면 변화가 있는 유닛 기록 종료
        } // 개별 체력 확정 종료
        foreach (var ally in changed) // 모든 실제 수치 반영 뒤 체력 표시 알림
        { // 화면 표시 동기화 시작
            ally.NotifyRedistributedHealth(); // 일반 회복과 피해를 거치지 않는 화면 변경 신호
        } // 화면 표시 동기화 종료
        long gained = 0; // 각 아군의 실제 획득 방어도 합계
        foreach (var ally in living) // 실제 생존 파티에 원본 방어도 지급
        { // 원래 방어도 경로 호출 시작
            gained += ally.GainBlock(block); // 기존 방어도 상한과 화면 변경 유지
        } // 원래 방어도 경로 호출 종료
        return gained; // 실제 늘어난 방어도의 합계
    } // 아군 체력과 최대치 수집 종료

    private static long SumAtLevel(IReadOnlyList<BattleUnitRuntime> living, int level) // 개별 상한까지 공통 수치를 적용한 체력 합계
    { // 체력 합계 계산 시작
        long sum = 0; // 정수 합의 넘침을 방지하는 누적값
        foreach (var ally in living) // 각 생존자의 개별 최대 체력 확인
        { // 상한 보정 합산 시작
            sum += Math.Min(level, ally.MaxHealth); // 상한을 넘지 않는 체력 합산
        } // 상한 보정 합산 종료
        return sum; // 현재 균등 체력의 전체 합계
    } // 체력 합계 계산 종료
} // 실제 체력 평준화 종료
