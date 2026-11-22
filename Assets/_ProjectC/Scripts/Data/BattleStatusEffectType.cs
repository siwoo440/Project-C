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
    UndyingGuard, // 체력이 한 점 이하가 될 때 한 번 버티는 효과
    Vulnerable = 49, // 방어도 적용 가능 원본 피해 증가
    DamageNullification = 50, // 양수 다음 피해 한 번 무효
    DistributedDefense = 51, // 지정 적 턴의 광역 피해만 경감
    RetributionBulwark = 52, // 생존 아군을 직접 공격한 적에게 고정 피해
    FortifyResistance = 53, // 요새화의 독립적인 마법 피해 경감
    EternalShield = 54, // 정해진 적 턴 동안 받는 직접 피해 경감
    EternalShieldResistance = 55, // 영원의 방패의 독립적인 받는 마법 피해 경감
    ReflectionCitadel = 56, // 실제 받은 피해를 해당 적 턴 종료에 한 번 반사
    GuardianHeart = 57, // 아군이 쓰러지려 할 때 생존하는 치명 피해 대체
    DefeatWard = 58, // 실제 전투불능이 될 피해를 한 번 막는 보호
    HealingReceivedUp = 59, // 대상이 받는 체력 회복량의 비율 증가
    Renewal = 60, // 아군의 실제 턴이 끝날 때 지정 체력 회복
    SanctuaryWard = 61, // 적 턴을 기준으로 받는 피해 비율 경감
    FearResistance = 62, // 적 턴을 기준으로 아군의 공포 확률 저항
    DelayedHealing = 63, // 다음 아군 턴 시작 한 번의 별도 체력 회복
    Stealth = 64, // 실제 전투에서 은신 중인 상태
    ResurrectionSeed = 65, // 실제 전투불능에서 아군을 한 번 되살리는 상태
    PainDistribution = 66, // 적 직접 피해를 생존 아군과 나누는 상태
    MiracleEcho = 67, // 현재 아군 턴의 직접 카드 회복 증가
    CorrosionResistanceDown = 68, // 실금석의 마법 피해 저항률 감소 상태
    PulseCordAttackPowerUp = 69, // 맥박줄이 지정한 아군의 별도 공격력 증가
    CrystalMarkDefenseDown = 70, // 결정눈이 부여하는 별도 표식 방어 약화
    StethoscopeMark = 71, // 청진기가 고른 적의 독립 전투 시작 표식
    ArcaneWard = 72, // 다음 적 턴까지 고정 마법 저항력 상승
    MagicAttackPowerUp = 73, // 실제 아군 마법 카드의 독립 공격력 증가
    Shock = 74, // 대상의 다음 실제 적 턴 시작에 정산하는 감전
    StealthBreakDamageUp = 75, // 은신 종료 뒤 다음 직접 카드 명중 보정
    ManaConcentration = 76, // 다음 지팡이 피해 카드의 실제 피해 증가
    IncomingMagicDamageUp = 77, // 대상이 받는 실제 마법 피해 증가
    EvasionDown = 78, // 적의 현재 회피율 감소
    ConfessionDamageMark = 79, // 실제 적 정신력 피해 이후 다음 명중의 표식
    WandEcho = 80, // 이번 아군 턴 사용자의 다음 마법 공격 복제
    DanceShoeDamageUp = 81, // 실제 회피 뒤 다음 직접 공격의 피해 증가
    WandOverload = 82, // 이번 턴 시전자의 실제 지팡이 마법 공격 피해 증가
    IllusionHitReduction = 83, // 다음 유효 적 직접 공격의 한 번 피해 감소
    ServantContractMark = 84, // 전투 시작 한 아군의 계약에 따른 공격력 증가
    LordFinalOrderAttack = 85, // 처음 두 아군 턴의 명령서 공격력 증가
    LordFinalOrderWeakenedDefense = 86, // 뒤이어 오는 적 턴의 물리 방어 감소
    DistributionAttackUp = 87, // 전투가 지속되는 동안 분배 대상의 공격력 증가
    DistributionPhysicalDefenseDown = 88, // 전투가 지속되는 동안 분배 대상의 물리 방어 감소
    ToolMaintenanceAttackUp = 89, // 같은 아군의 다음 실제 직접 명중을 기다리는 피해 증가
    SandQueenSummonAttackGrow = 90, // 실제 적 소환 완료에 따른 전투 지속 누적 공격력 증가
    BattlefieldMapAccuracyUp = 91, // 두 아군 턴의 전장 지도 명중률 증가
    GoldenLureAttackPercentDown = 92, // 두 적 턴의 금빛 미끼 직접 공격 피해 감소
    ActionPointDown = 93, // 적 원본 또는 카드가 명시적으로 부여한 행동력 감소
    MagneticEvasionDown = 94, // 적이 받는 두 턴의 자기장 회피율 약화
    WasteHeatAttackPowerUp = 95, // 실제 화상 피해 뒤 분리되어 유지되는 공격 강화
    FlameVeinResistanceDown = 96, // 화염 광맥으로 약해진 마법 피해 저항률
    CanyonWindEvasionUp = 97, // 협곡 바람피리로 증가한 아군 회피율
    SwampFairyEvasionUp = 98 // 늪의 요정 날개가루로 증가한 아군 회피율
} // 열거형 종료
