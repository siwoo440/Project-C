public enum ShieldCardRule // 방패 카드의 전용 전투 규칙
{ // 방패 규칙 목록 시작
    None, // 기존 카드 효과 사용
    BodyGuard, // 동료의 다음 직접 공격 대신 받기
    ShockAbsorption, // 다음 적 턴에 공격마다 받는 피해 경감
    ShieldBash, // 명중 후 확률로 다음 적 행동 취소
    ResistanceStance // 마법 피해의 비율 경감
} // 방패 규칙 목록 종료
