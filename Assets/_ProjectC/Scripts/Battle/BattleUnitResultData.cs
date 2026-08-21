public sealed class BattleUnitResultData // 전투 종료 아군 상태 데이터
{ // 클래스 시작
    public string UnitId { get; } // 아군 고유 ID 조회
    public string DisplayName { get; } // 아군 표시 이름 조회
    public int CurrentHealth { get; } // 종료 현재 체력 조회
    public int MaximumHealth { get; } // 종료 최대 체력 조회
    public bool IsDead => CurrentHealth <= 0; // 종료 사망 여부 조회
    public BattleUnitResultData(BattleUnitRuntime runtimeUnit) // 런타임 아군 상태 복사
    { // 상태 복사 시작
        UnitId = runtimeUnit.UnitId; // 아군 고유 ID 저장
        DisplayName = runtimeUnit.DisplayName; // 아군 표시 이름 저장
        CurrentHealth = runtimeUnit.CurrentHealth; // 종료 현재 체력 저장
        MaximumHealth = runtimeUnit.MaxHealth; // 종료 최대 체력 저장
    } // 상태 복사 종료
} // 클래스 종료
