
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
    Contract, // 시전자 체력 지불과 현재 행동력 회복
    Distribution, // 선택 아군의 전투 지속 공격 강화와 물리 방어 약화
    CardCopy, // 현재 아군 턴에 실제 사용한 카드의 독립 인스턴스 복제
    RunePower, // 한 아군의 손패 카드 하나의 기본 행동력 비용 제거
    Ritual, // 비치명 체력 지불과 행동력 회복
    FusionOfPower, // 공용 드로우와 아군 회복 및 적 전체 피해
    PickUpCoin, // 행동력 회복과 사용 카드의 전투 제외
    SecureSupplies, // 한 장 드로우와 시전자 방어도
    TemporarySupplies, // 지정 아군 회복과 확률 행동력 회복
    ExchangeProposal, // 다른 손패 한 장을 버린 뒤 두 장 드로우
    RuneRecording, // 소유자의 다음 실제 기본 비용 한 점 감소
    TacticalMark, // 지정 적에게 두 적 턴 동안 일반 표식 부여
    Improvisation, // 일반 등급 카드 하나의 전투 전용 생성
    EmergencyFunds, // 다음 아군 턴의 최고치와 현재 행동력 증가
    ToolMaintenance, // 다음 실제 명중한 아군 직접 공격 피해 증가
    HandSorting, // 하나 또는 둘을 골라 같은 수의 공용 드로우
    SeizeOpportunity, // 이 카드가 실제 첫 사용 카드일 때 행동력 회복
    ResourceCycle, // 무작위 실제 전투 제외 카드 회수
    StrategicStorage, // 지정 손패 보존과 기본 비용 감소
    WeaknessAnalysis, // 지정 생존 적의 두 턴 피해 증가
    CalculatedInvestment, // 현재 행동력 전체와 다음 턴 투자 환급
    RuneAmplification, // 지정 아군의 시간제 강화 연장
    TemporaryContract, // 시전자 체력 지불과 두 장 드로우
    GambleMove, // 행동력 회복 또는 직접 정신력 비용의 단일 판정
    MechanicalCalculation, // 현재 아군 턴의 펜타클 기본 비용 감소
    BattlefieldMap, // 적의 표식과 아군의 명중률 강화
    SecretVault, // 행동력 회복과 다음 턴의 기본 드로우 감소
    RuneSeal, // 적의 강화 해제와 마법 저항 감소
    Redistribution, // 생존 아군 체력 균등 분배와 방어도 증가
    PayThePrice, // 정해진 체력 지불과 행동력 회복
    Alchemy, // 일반 손패 카드의 전투 중 희귀 변환
    CompoundEffect, // 현재 턴 행동력 회복의 추가량
    FateManipulation, // 선택한 실제 뽑기 더미 카드의 손패 이동
    RuneBarrier, // 파티 개별 방어도와 다음 실제 턴 행동력
    GoldenLure, // 적의 공명 표식과 직접 공격 비율 약화
    ContractBreach, // 고정 추가 행동력을 사용하는 전체 단일 약화 정화
    AncientFormula, // 일반 드로우 세 장 및 이번 턴 실물 비용 증가
    RunawayRuneDevice, // 원본 결제 직후 잔여 행동력의 광역 마법 피해
    GoldenRule, // 원본 결제 직후 잔여 행동력의 광역 회복
    RewriteFate, // 기존 손패의 소비와 서로 다른 전설 카드 두 장 생성
    InfiniteContract // 전액 행동력과 시전자 체력을 함께 쓰는 무한 계약
} // 직렬화 규칙 종료
