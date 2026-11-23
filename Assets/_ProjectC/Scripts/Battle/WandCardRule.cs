public enum WandCardRule // 지팡이 카드의 별도 지불 및 피해 규칙
{ // 지팡이 카드 규칙 시작
    None, // 가변 피해 규칙 없음
    ManaRelease, // 지불한 실제 행동력과 마법 피해의 비례 증가
    EmberSpread, // 화상 상태가 걸린 적만 대상으로 허용
    ManaSiphon, // 실제 명중에서 시트 확률에 따른 행동력 회복
    ChainLightning // 생존 적을 매번 다시 골라 최대 세 번 직접 타격
} // 지팡이 카드 규칙 종료
