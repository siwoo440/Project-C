public enum BattleMentalChangeReason // 정신력 변화 원인 종류
{ // 열거형 시작
    None, // 원인 없음
    DealDamage, // 피해 가함
    ReceiveDamage, // 피해 받음
    HeavyDamage, // 큰 피해 받음
    DefeatEnemy, // 적 처치
    AllyDied, // 아군 사망
    TeammateDied, // 동료 사망
    HealingReceived, // 회복 받음
    LastSurvivor, // 최후 생존
    DisruptReceived, // 교란 받음
    DisruptInflicted, // 교란 가함
    CardEffect, // 카드 직접 효과
    StateDurationEnded, // 특수 상태 종료
    BattleEnded, // 전투 종료
    PersistentState // 저장 상태 적용
} // 열거형 종료
