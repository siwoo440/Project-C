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
    CardAttackHit, // 카드 직접 공격이 명중한 시점
    EventRelicReward, // 탐사 이벤트의 미보유 유물 보상 확정 직전
    InspectionDraw, // 확인 효과로 고른 카드가 손으로 들어온 시점
    DecisionDraw, // 확인으로 고른 카드의 결정 추가 효과가 성공한 시점
    DirectCardHealing, // 직접 카드가 체력을 회복하는 시점
    ExplorationRouteChoice, // 플레이어가 탐사 경로를 다시 고르는 시점
    ReceiveDefenseLoss, // 물리 방어 감소를 받을 때
    AreaGoldReward, // 지역 보상 골드 확정 전
    VolatileCardLost, // 손패에서 휘발 카드 소멸 직후
    CardExcluded, // 카드가 전투에서 제외된 직후
    BeforePlayerTurnDraw, // 아군의 턴 드로우 직전
    OpeningHandReady, // 첫 아군 손패 지급 직후
    ShopFirstStock, // 상점의 첫 진열 준비
    BeforeBattleActions, // 전투의 첫 적 예고 생성 전
    BeforeHealthRecovery, // 전투 중 회복량을 확정하기 직전
    OptionalCardEffectRoll, // 명중 시 발동하는 약화의 무작위 판정
    CardDamageRoll, // 카드의 두 기본 피해 중 하나를 고를 때
    AfterCardResolved, // 사용한 카드의 효과와 이동 완료 시점
    BeforeStatusDuration, // 기간제 상태의 남은 턴 지정 직전
    BattleEscaped, // 실제 조우에서 전투 도주 성공 시점
    CardDrawn, // 드로우한 카드가 손패에 들어갈 때
    CardDrawEffect, // 카드 자체 효과의 드로우 수량을 확정할 때
    BeforeInspectDrawPile, // 덱 위에서 확인할 후보 수량을 정하는 시점
    HandEmptyAtCleanup, // 턴 종료 손패를 자동으로 정리하기 직전
    BeforeEventTrapDamage, // 지정한 탐사 함정에 피해를 적용하기 직전
    ExplorationForcedBattleRoll, // 탐사의 확률 강제 전투 판정 직전
    BeforeShopPurchase, // 현재 상점에서 상품 골드 결제 직전
    ExplorationCompleted, // 성공한 탐사 완료를 정산할 때
    BeforeMentalLoss // 실제 정신력 손실을 확정하기 직전
}
