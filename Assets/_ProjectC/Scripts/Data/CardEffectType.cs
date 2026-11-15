public enum CardEffectType // 카드 효과 종류
{ // 열거형 시작
    Damage, // 피해 효과
    Heal, // 회복 효과
    ApplyStatusEffect, // 상태 이상 적용 효과
    RemoveDebuffs, // 디버프 전체 해제 효과
    ChangeMental, // 정신력 직접 변화 효과
    RedirectAttackAndDefend, // 적 공격 유도와 방어 효과
    ProtectAttackTarget, // 공격 예고 대상 보호 효과
    ChooseDraw, // 덱 위 카드 선택 드로우 효과
    RecoverDiscard, // 버린 카드 선택 회수 효과
    GainBlock, // 피해 흡수 방어도 획득 효과
    SelectPartner // 연인의 동료 파트너 지정 효과
} // 열거형 종료
