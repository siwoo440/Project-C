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
    RendingShield // 현재 방어도 절반의 기본 피해
} // 방패 규칙 목록 종료
