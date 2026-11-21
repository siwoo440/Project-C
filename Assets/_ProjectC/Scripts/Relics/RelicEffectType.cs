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
    ReselectExplorationRoute, // 소모하여 탐사 경로 재선택
    ShieldedDamageReduction, // 방어도를 보유한 아군의 직접 피격 감소
    JournalCardRemoval, // 전투 종료 회차 카드 한 장 제거
    OpeningMark, // 전투 시작 적 전체 표식
    MarkedCardDamage, // 표식 대상의 직접 카드 피해 가산
    FirstHealingCardBonus, // 첫 회복 카드의 대상별 추가 회복
    SoftenDefenseLoss, // 물리 방어력 감소 약화 완화
    CampMentalBonus, // 캠프의 생존자 정신력 추가 회복
    ErosionGoldBonus, // 침식지대 보상 골드 가산
    OpeningSpeedLoss, // 전투 시작 무작위 적 속도 감소
    VolatileCardDraw, // 휘발 카드 소멸 뒤 드로우
    ExcludedCardBlock, // 카드 제외 뒤 소유자 방어도
    LargeDiscardDraw, // 버린 더미가 큰 턴의 추가 드로우
    ExtraShopRelic, // 첫 진열 확률로 유물 한 칸 추가
    OpeningCardDiscount, // 소모성 첫 아군 턴 비용 할인
    OpeningPentacleDraw, // 첫 펜타클 손패의 추가 드로우
    OpeningSwordCapacity, // 첫 검 손패의 추가 행동력
    LowHealthHealing, // 낮은 체력인 아군의 받는 회복량 증가
    HighHealthCardDamage, // 높은 체력인 아군의 첫 카드 명중 피해 증가
    DefeatDraw, // 아군 턴 적 처치에 따른 카드 드로우
    DiscountNextPaidCard, // 행동력을 사용한 뒤 다음 유료 고정 카드 할인
    GuaranteeOptionalCardEffect, // 명중 시 확률 약화 한 번 보장
    FavorTwelveDamage, // 피해 6·12 중 12의 정수 확률 증가
    ExtraHandCapacity, // 해당 전투의 최대 손패 증가
    SingleAttackDamage, // 단일 한 타격 카드의 피해 증가
    ChaliceMentalOnUse, // 아군 턴의 첫 성배 카드 소유자 정신력
    PentacleDrawOnUse, // 아군 턴의 첫 펜타클 카드 사용 뒤 드로우
    OpeningAllySpeed, // 전투 시작 전체 아군의 행동 속도 증가
    ShorterFadeDebuffs, // 침식지대 전투의 기간제 디버프 지속 감소
    EscapePartialGold, // 해당 조우에서 처음 도주할 때 일부 골드 보상
    OpeningDroneWeakness, // 전투 시작 무작위 적의 공격력 감소
    FirstDrawCardDiscount, // 전투 첫 손패 도착 카드의 일시 사용 비용 감소
    FirstCardDrawBonus, // 첫 장수 기반 카드 드로우 증가
    FirstCardDamageBonus, // 전투 첫 유효 카드 직접 피해 증가
    InspectionChoiceBonus, // 덱 상단 확인 후보 수 증가
    DiscardFirstUnplayableDraw, // 최초 사용 불가 드로우 카드를 버린 더미로 이동
    RecoverRandomDiscard, // 아군 턴 첫 회수 가능한 무작위 한 장 지급
    RetainedHandHealing, // 유지 카드가 남은 첫 유효 카드 회복 증가
    RetainedHandDamage, // 유지 카드가 남은 첫 유효 카드 직접 피해 증가
    FoldingCotRecovery, // 캠프의 생존 아군별 고정 체력 회복량 증가
    EmptyHandNextTurnDraw, // 정리 이전 빈 손패의 다음 턴 추가 드로우
    ErosionCoreRelicChance, // 침식핵 첫 승리의 추가 유물 확률
    EventTrapDamageReduction, // 탐사 이벤트 함정의 직접 체력 피해 감소
    HealthDamageBlock, // 살아남은 아군이 체력 피해를 받은 뒤 방어도 획득
    OpeningRandomCardDiscount, // 첫 손패의 무작위 고정 비용 할인
    OpeningVulnerable, // 전투 시작 시 무작위 적에게 취약 부여
    AberrationVictoryDraw, // 괴생체 격파 뒤 다음 전투의 시작 카드 추가
    AberrationVictoryActionPoint, // 괴생체 격파 뒤 다음 전투의 시작 행동력 추가
    CardRemovalDiscount, // 상점의 회차 카드 제거 비용 할인
}
