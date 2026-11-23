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
    Evasion // 적 공격 회피율 증가
} // 열거형 종료
