public enum ShieldCardRule // 방패 카드의 전용 전투 규칙
{ // 방패 규칙 목록 시작
    None, // 기존 카드 효과 사용
    BodyGuard, // 동료의 다음 직접 공격 대신 받기
    ShockAbsorption, // 다음 적 턴에 공격마다 받는 피해 경감
    ShieldBash, // 명중 후 확률로 다음 적 행동 취소
    ResistanceStance, // 마법 피해의 비율 경감
    SplitBarrier, // 서로 다른 생존 아군 두 명에게 방어도 부여
    BalanceGuard, // 다음 유료 방패 카드 할인
    Taunt, // 적의 다음 단일 공격 우선 대상 지정
    RendingShield, // 현재 방어도 절반의 기본 피해
    FrontlineProtection, // 아군 전체 방어도와 출혈 저항
    SuppressionStrike, // 명중한 적의 공격력 약화
    ShieldOath, // 지정 아군의 두 적 턴 동안 직접 피해 대신 받기
    IroncladRush, // 적 공격 뒤 카드 소유자에게 별도의 방어도 지급
    Fortify, // 생존 아군 전체의 물리 방어력과 마법 피해 저항 증가
    IndomitableFront, // 생존 아군 전체 방어도와 공포 제거
    Impenetrable, // 생존 아군 전체 방어도와 개별 피해 무효
    GiantsShield // 자기 방어도와 다음 아군 턴 행동력 감소
} // 방패 규칙 목록 종료
