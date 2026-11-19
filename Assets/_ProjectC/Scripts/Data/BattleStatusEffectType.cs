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
    HealingReceivedDown // 대상이 받는 체력 회복량 감소
} // 열거형 종료
