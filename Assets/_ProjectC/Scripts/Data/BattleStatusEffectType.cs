public enum BattleStatusEffectType // 전투 상태 이상 종류
{ // 열거형 시작
    None, // 상태 이상 없음
    Poison, // 중독 지속 피해
    Regeneration, // 재생 지속 회복
    AttackPowerUp, // 공격력 증가
    StatusImmunity, // 디버프 면역
    PhysicalDefenseUp, // 물리 방어력 증가
    PhysicalDefenseDown, // 물리 방어력 감소
    MagicalResistanceUp, // 마법 저항력 증가
    MagicalResistanceDown, // 마법 저항력 감소
    Disrupt, // 정신력 교란
    ChariotMark, // 전차 집중 공격 표식
    GuardPreparation, // 다음 적 공격 대비
    StarReservedHealing, // 별의 다음 턴 예약 회복
    StarPassiveReservedHealing, // 별 패시브의 별도 예약 회복
    Burn, // 턴 종료 시 감소하는 화상 피해
    Blind, // 한 턴 공격 명중률 감소
    Awakening, // 정신력 최대 각성 상태
    Collapse, // 정신력 최소 붕괴 상태
    Evasion, // 적 공격 회피율 증가
    Disorientation, // 아군 첫 턴의 방향 감각 상실
    CriticalChanceUp, // 직접 카드 공격의 치명타 확률 증가
    ChargeDefenseDown, // 돌진에 의한 다음 공격 턴 물리 방어 감소
    Bleed, // 턴 종료 고정 체력 손실
    Stagger, // 다음 예고 행동 한 번 취소
    Stun, // 자기 턴 동안 행동 금지
    NextAttackDown, // 다음 공격의 원본 피해 감소
    AttackPowerDown, // 해당 적 턴의 직접 공격 피해 감소
    BladeMaintenance, // 이번 턴 다음 검 피해 카드의 타격 강화
    PhysicalRiposte, // 직접 받은 첫 물리 체력 피해 반사
    BloodOath, // 소유자의 모든 검 피해 증가
    Fear, // 공격 대상의 회피 보정 무효
    FinishingStance, // 체력이 절반 이하인 적에게 카드 피해 증가
    HealingReceivedDown, // 대상이 받는 체력 회복량 감소
    PublicMark, // 첫 아군 턴이 끝날 때까지 유지하는 공명 표식
    BattlefieldResolve, // 다음 아군 턴 시작 방어도 예약
    BladeFrenzy, // 이번 턴 소유자의 모든 검 타격 강화
    ActionSpeedDown, // 적의 행동 속도 감소
    DuelMark, // 시전자와 상대가 공유하는 결투 표식
    SwordSaintStance, // 검 카드 비용 감소와 직접 타격 강화
    ShockAbsorption, // 지정 적 턴 동안의 직접 피해 고정 경감
    ResistanceStance, // 지정 적 턴 동안의 마법 피해 비율 경감
    ActionSpeedUp, // 시전자가 보유한 행동 속도 상승
    ShieldBalance, // 다음 유료 방패 카드 기본 비용 감소
    ShieldCounter, // 방어도로 적 직접 공격 흡수 시 고정 반격
    GuardianCircle, // 받는 직접 피해 비율 경감
    DangerInterception, // 다음 적의 직접 공격 한 번에 대한 비율 경감
    FocusedDefense, // 다음 적 턴 동안 직접 피해 비율 감소
    BleedResistance, // 적의 출혈 부여 시도 방어 확률
    UndyingGuard // 체력이 한 점 이하가 될 때 한 번 버티는 효과
} // 열거형 종료
