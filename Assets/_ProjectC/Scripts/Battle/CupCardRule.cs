public enum CupCardRule // 성배 카드의 개별 전투 규칙
{ // 성배 기술 목록 시작
    None, // 기본 카드 효과 적용
    HolyJudgment, // 명중한 적의 전투불능 방지 상태 해제
    CelestialGrace, // 전체 회복과 이번 턴 공격력 추가
    Warmth, // 아군 화상 해제와 방어도 지급
    Calming, // 선택한 아군의 공포 해제와 정신력 회복
    HolyWater, // 불사형 적을 실제 명중한 경우 추가 피해
    PurifyingTouch, // 하나의 약화 제거와 한 적 턴 마법 저항
    SacrificialHealing, // 시전자의 실제 체력 비용과 대상 회복
    WaterOfLife, // 즉시 회복과 다음 아군 턴 체력 예약
    BlessingTransfer, // 지정한 생존 아군 둘 사이의 강화 복사
    WhiteVow, // 대상의 다음 피해 무효와 직접 정신력 회복
    JudgmentWater, // 명중 뒤 생존 적의 해제 가능한 강화 제거
    LifeContract, // 시전자의 정신력 소모와 지정 아군의 회복
    MartyrsLight, // 실제 시전자의 체력을 하나로 만든 뒤 아군 회복
    GrailMiracle, // 현재 잔여 행동력 모두 사용과 비례 회복
    ArchangelBlessing // 체력 회복 전에 제거 가능한 모든 약화 정화
} // 성배 기술 목록 종료
