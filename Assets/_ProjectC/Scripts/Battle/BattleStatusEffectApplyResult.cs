public enum BattleStatusEffectApplyResult // 상태 이상 적용 결과
{ // 열거형 시작
    Invalid, // 잘못된 적용 요청
    Applied, // 신규 상태 적용
    Stacked, // 기존 상태 중첩
    BlockedByImmunity, // 면역 상태 차단
    CleansedByRelic // 성공한 약화를 보유 유물이 즉시 제거
} // 열거형 종료
