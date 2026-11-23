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
    BattleEnded, // 전투 종료 시점
    BattleVictory, // 전투 승리 결과 저장 직전
    FirstNormalRoom, // 탐사 출발 뒤 첫 일반 방 진입
    CardRandomTarget, // 카드의 무작위 대상 추첨
    BeforeHandDiscard, // 손패 한 장이 버린 더미로 이동하기 직전
    CampRecovery, // 캠프 회복 선택 시점
    ShopCardRemoval, // 상점 카드 제거 확정 시점
    BeforeHealthDamage, // 방어도 처리 후 체력 피해 직전
    WhileOwned, // 회차 동안 보유할 때
    PotionUsed, // 포션을 마시는 시점
    CardAttackHit // 카드 직접 공격이 명중한 시점
}
