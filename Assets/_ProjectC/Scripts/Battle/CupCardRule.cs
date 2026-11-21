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
    BlessingTransfer // 지정한 생존 아군 둘 사이의 강화 복사
} // 성배 기술 목록 종료
