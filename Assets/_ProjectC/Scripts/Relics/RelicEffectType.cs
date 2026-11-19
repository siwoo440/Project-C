public enum RelicEffectType // 유물 효과 종류
{
    None, // 효과 없음
    IncreaseMaxHealth, // 최대 체력 증가
    RestoreHealth, // 현재 체력 회복
    GainGold, // 골드 획득
    DealDamage, // 피해 적용
    ApplyStatusEffect, // 상태 이상 적용
    DistributeBlock, // 생존 대상에게 고정 방어도 배분
    GainBlock, // 개별 대상의 방어도 획득
    ChangeMental, // 개별 대상의 정신력 증가
    DrawCards, // 전투 덱 카드 드로우
    ShopDiscount, // 상점 구매와 서비스 할인
    DiscoverFirstRoomEvent, // 첫 일반 방의 이벤트 발견 확률 증가
    RerollCardTarget, // 카드 무작위 대상 한 번 재추첨
    GainActionPoints, // 공용 행동력 회복
    CopyPlayedCard, // 처음 사용한 카드의 전투용 복사본 지급
    QueueNextTurnDraw, // 다음 아군 턴에 추가 드로우 예약
    RetainDiscardedHandCard, // 버려질 손패 한 장 유지
    GainFirstTurnCapacity, // 첫 아군 턴 최대 행동력과 현재 행동력 증가
    CampHealthRecoveryBonus, // 캠프 체력 회복량 증가
    FreeCardRemoval, // 상점 카드 제거 비용 면제
    PreventFirstHealthDamage, // 첫 체력 피해 방지
    ExtraPotionSlot, // 포션 전용 보관칸 개방
    HealthPotionBonus, // 회복 포션의 대상별 체력 추가
    EnemyTurnAttackLoss, // 적 턴 시작 무작위 적 공격 약화
    HealingCardBlock, // 회복 카드 사용자의 방어도 획득
    ChaliceCardHealing, // 성배 카드 사용자의 체력 회복
    FirstSwordDamage, // 첫 검 공격의 명중 보너스
    FirstWandDamage, // 첫 지팡이 마법 명중 보너스
    VictoryGoldPercent, // 승리 보상 골드 비율 증가
    ExtraEventRelicChoice, // 이벤트의 유물 선택 후보 추가
    InspectCardDraw, // 덱 위 카드 확인 성공 후 추가 드로우
    DecisionActionPoint, // 결정 추가 효과 성공 시 행동력 증가
    ReduceIncomingAttack, // 적 직접 공격의 체력 피해 경감
    HighMentalCardHealing, // 높은 정신력의 직접 카드 회복 추가
    LowMentalCardDamage, // 낮은 정신력의 첫 직접 명중 피해 추가
    ReselectExplorationRoute // 소모하여 탐사 경로 재선택
}
