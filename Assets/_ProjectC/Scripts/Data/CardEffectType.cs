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
    SelectPartner, // 연인의 동료 파트너 지정 효과
    GuardAllyAttack, // 동료의 예정 공격 대신 받기 효과
    ArrangeDrawTop, // 덱 위 카드 순서 정리 효과
    RerollRandomAttack, // 적 무작위 공격 대상 재굴림 효과
    ShareAllyAttack, // 동료의 예정 공격 피해 분담 효과
    TransferAllyDebuff, // 동료의 디버프 하나를 자신에게 이전하는 효과
    DrawThenDiscard, // 덱에서 뽑은 뒤 손패 하나를 선택해 버리는 효과
    RemoveOneDebuff, // 먼저 적용된 디버프 하나 해제
    Unplayable, // 손패에서 사용할 수 없는 카드
    ReviveAlly, // 현재 전투에서 쓰러진 아군의 체력 회복과 생존 상태 복구
    CopyAllyBlessing // 지정 아군 사이에 활성 강화 하나 복사
} // 열거형 종료
