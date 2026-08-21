using System.Collections.Generic; // 읽기 전용 목록 기능 사용
public sealed class BattleResultData // Scene 전달용 전투 결과 데이터
{ // 클래스 시작
    public BattleResult Result { get; } // 전투 최종 결과 조회
    public BattleType BattleType { get; } // 전투 유형 조회
    public int CompletedRound { get; } // 종료 라운드 조회
    public bool CanReceiveReward => Result == BattleResult.Victory; // 승리 보상 가능 여부 조회
    public IReadOnlyList<BattleUnitResultData> AllyStates { get; } // 아군 종료 상태 목록 조회
    public IReadOnlyList<string> DefeatedEnemyIds { get; } // 처치 적 ID 목록 조회
    public int LivingAllyCount { get; } // 생존 아군 수 조회
    public BattleResultData(BattleResult result, BattleType battleType, int completedRound, IReadOnlyList<BattleUnitRuntime> allies, IReadOnlyList<string> defeatedEnemies) // 전투 결과 스냅샷 생성
    { // 결과 생성 시작
        Result = result; // 최종 결과 저장
        BattleType = battleType; // 전투 유형 저장
        CompletedRound = completedRound; // 종료 라운드 저장
        List<BattleUnitResultData> allyStates = new List<BattleUnitResultData>(); // 아군 상태 복사 목록 생성
        int livingAllyCount = 0; // 생존 아군 수 초기화
        foreach (BattleUnitRuntime allyUnit in allies) // 아군 런타임 목록 순회
        { // 아군 상태 복사 시작
            if (allyUnit == null) // 빈 아군 런타임 확인
            { // 빈 아군 처리 시작
                continue; // 다음 아군 이동
            } // 빈 아군 처리 종료
            BattleUnitResultData allyState = new BattleUnitResultData(allyUnit); // 아군 종료 상태 생성
            allyStates.Add(allyState); // 아군 상태 목록 추가
            if (!allyState.IsDead) // 생존 아군 확인
            { // 생존 아군 처리 시작
                livingAllyCount++; // 생존 아군 수 증가
            } // 생존 아군 처리 종료
        } // 아군 상태 복사 종료
        AllyStates = allyStates; // 아군 상태 읽기 전용 목록 저장
        LivingAllyCount = livingAllyCount; // 생존 아군 수 저장
        List<string> defeatedEnemyIds = new List<string>(); // 처치 적 ID 목록 생성
        foreach (string enemyId in defeatedEnemies) // 처치 적 ID 목록 순회
        { // 처치 적 확인 시작
            if (!string.IsNullOrWhiteSpace(enemyId) && !defeatedEnemyIds.Contains(enemyId)) // 유효한 신규 처치 적 ID 확인
            { // 처치 적 처리 시작
                defeatedEnemyIds.Add(enemyId); // 처치 적 ID 목록 추가
            } // 처치 적 처리 종료
        } // 처치 적 확인 종료
        DefeatedEnemyIds = defeatedEnemyIds; // 처치 적 읽기 전용 목록 저장
    } // 결과 생성 종료
} // 클래스 종료
