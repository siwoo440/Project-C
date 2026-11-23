public enum BattleEventType // 전투 공용 이벤트 종류
{ // 열거형 시작
    BattleStarted, // 전투 시작
    TurnStarted, // 진영 턴 시작
    TurnEnded, // 진영 턴 종료
    CardUsed, // 카드 사용 완료
    DamageApplied, // 피해 적용 완료
    HealingApplied, // 회복 적용 완료
    StatusApplied, // 상태 이상 적용 완료
    MentalChanged, // 정신력 변화 완료
    UnitDefeated, // 유닛 처치 완료
    BattleEnded, // 전투 종료
    BeforeHandDiscard, // 카드 효과나 턴 종료의 손패 버림 직전
    DecisionResolved // 확인으로 가져온 카드의 결정 추가 효과 성공
} // 열거형 종료
