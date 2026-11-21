
public enum PentacleCardRule // 펜타클 카드의 독립적인 전투 정산 규칙
{ // 직렬화 규칙 시작
    None, // 기존 원본 카드의 공통 효과 유지
    GoldenCoin, // 현재 행동력 한 점 회복
    Focus, // 현재 공용 손패에 카드 한 장 드로우
    TacticalExchange, // 손패에서 다른 카드 두 장을 버린 후 교체
    EnchantCard, // 지정 카드 한 장의 다음 사용 효과 증가
    EfficientUse, // 이번 아군 턴 동안 기본 행동력 비용 감소
    AncientRune, // 아군 공용 손패에 카드 두 장 드로우
    WheelOfFate, // 실제 지불한 행동력만큼 카드 소멸과 전체 회복
    Contract // 시전자 체력 지불과 현재 행동력 회복
} // 직렬화 규칙 종료
