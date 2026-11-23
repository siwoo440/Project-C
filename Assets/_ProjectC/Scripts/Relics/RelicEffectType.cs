public enum RelicEffectType // 유물 효과 종류
{
    None, // 효과 없음
    IncreaseMaxHealth, // 최대 체력 증가
    RestoreHealth, // 현재 체력 회복
    GainGold, // 골드 획득
    DealDamage, // 피해 적용
    ApplyStatusEffect, // 상태 이상 적용
    DistributeBlock, // 생존 대상에게 고정 방어도 배분
    GainBlock, // 개별 대상의 방어도 획득
    ChangeMental, // 개별 대상의 정신력 증가
    DrawCards // 전투 덱 카드 드로우
}
