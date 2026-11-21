public enum WandCardRule // 지팡이 카드의 별도 지불 및 피해 규칙
{ // 지팡이 카드 규칙 시작
    None, // 가변 피해 규칙 없음
    ManaRelease, // 지불한 실제 행동력과 마법 피해의 비례 증가
    EmberSpread, // 화상 상태가 걸린 적만 대상으로 허용
    ManaSiphon, // 실제 명중에서 시트 확률에 따른 행동력 회복
    ChainLightning, // 생존 적을 매번 다시 골라 최대 세 번 직접 타격
    MentalShock, // 명중한 생존 적의 정신 피해 추가
    BurnAmplification, // 생존 적의 현재 화상만 한 적 턴 연장
    ShockTransfer, // 감전 적을 타격한 뒤 다른 생존 적에게 감전
    RuneBurst, // 활성 표식이 있는 적의 마법 피해 추가
    FateFreeze, // 기절과 마법 저항 감소의 별도 적용
    FireDownpour, // 생존 적을 매번 추첨하는 최대 여섯 타격
    ManaOverload, // 이번 아군 턴 마법 피해 증가와 정신력 손실
    VoidHand, // 유효 명중 뒤 오래된 적 강화 한 개 제거
    ManaBackflow, // 광역 마법 정산 뒤 시전자의 공격력 감소
    ArcaneSurge, // 고정 마법 피해의 적 저항력 일부 무시
    IllusionSplit // 회피율 강화와 다음 직접 피해 한 번 감소
} // 지팡이 카드 규칙 종료
