using System.Collections.Generic; // 대상 목록 사용
public static class BattleBlockDistribution // 고정 방어도 배분 기능
{ // 배분 기능 시작
    public static int Apply(IReadOnlyList<BattleUnitRuntime> targets, int amount) // 생존 대상에게 방어도 배분
    { // 배분 처리 시작
        if (targets == null || amount <= 0) // 배분 가능 조건 확인
        { // 배분 불가 처리 시작
            return 0; // 적용량 없음 반환
        } // 배분 불가 처리 종료
        int livingCount = 0; // 생존 대상 수 초기화
        foreach (BattleUnitRuntime target in targets) // 대상 목록 순회
        { // 생존 대상 확인 시작
            if (target != null && !target.IsDead) // 생존 대상 여부 확인
            { // 생존 대상 집계 시작
                livingCount++; // 생존 대상 수 증가
            } // 생존 대상 집계 종료
        } // 생존 대상 확인 종료
        if (livingCount == 0) // 생존 대상 없음 확인
        { // 대상 없음 처리 시작
            return 0; // 적용량 없음 반환
        } // 대상 없음 처리 종료
        int share = amount / livingCount; // 대상별 기본 배분량 계산
        int remainder = amount % livingCount; // 나누고 남은 방어도 계산
        int applied = 0; // 실제 적용량 초기화
        foreach (BattleUnitRuntime target in targets) // 대상 목록 순회
        { // 개별 방어도 적용 시작
            if (target == null || target.IsDead) // 무효 대상 확인
            { // 무효 대상 처리 시작
                continue; // 다음 대상 이동
            } // 무효 대상 처리 종료
            int extra = remainder > 0 ? 1 : 0; // 앞 순서의 나머지 배분량 계산
            remainder -= extra; // 나머지 배분량 차감
            applied += target.GainBlock(share + extra); // 실제 획득량 누적
        } // 개별 방어도 적용 종료
        return applied; // 실제 전체 적용량 반환
    } // 배분 처리 종료
} // 배분 기능 종료
