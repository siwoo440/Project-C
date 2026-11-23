public enum RelicTriggerType // 유물 발동 시점 종류
{
    None, // 발동 시점 없음
    OnAcquire, // 유물 획득 직후
    BattleStarted, // 전투 시작 시점
    TurnStarted, // 턴 시작 시점
    TurnEnded, // 턴 종료 시점
    CardUsed, // 카드 사용 시점
    DamageApplied, // 피해 적용 시점
    HealingApplied, // 회복 적용 시점
    StatusApplied, // 상태 이상 적용 시점
    MentalChanged, // 정신력 변화 시점
    UnitDefeated, // 유닛 처치 시점
    BattleEnded // 전투 종료 시점
}
