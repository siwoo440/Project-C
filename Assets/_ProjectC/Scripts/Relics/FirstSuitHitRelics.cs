using System; // 피해 합계의 정수 범위 보호

public sealed class FirstSuitHitRelics // 한 전투의 계열별 첫 피해 카드와 적중 기록
{ // 첫 적중 보너스 시작
    private int lastSwordRound; // 첫 검 피해 카드를 사용한 턴
    private int lastWandRound; // 첫 지팡이 마법 피해 카드를 사용한 턴
    private int remainingBonus; // 현재 카드의 다음 직접 적중에 더할 피해

    public void BeginCard(CardInstance card, int round, RelicInventoryRuntime inventory) // 실제 카드 사용 시 이번 턴의 대상 카드 결정
    { // 카드 사용 검사 시작
        remainingBonus = 0; // 직전 카드가 모두 빗나간 경우 남은 기회 종료
        if (round < 1 || inventory == null || card?.EffectType != CardEffectType.Damage) // 실제 피해 카드와 보유함 확인
        { // 비공격 사용 처리 시작
            return; // 계열별 첫 피해 기회 보존
        } // 비공격 사용 처리 종료
        if (card.CardType == CardType.Sword && lastSwordRound != round) // 이번 턴의 첫 검 피해 카드 확인
        { // 검집 처리 시작
            lastSwordRound = round; // 같은 턴의 다른 검 카드 제외
            remainingBonus = inventory.FindEffect(RelicEffectType.FirstSwordDamage)?.EffectValue ?? 0; // 보유 검집의 가산량
        } // 검집 처리 종료
        else if (card.CardType == CardType.Wand && card.DamageType == BattleDamageType.Magical && lastWandRound != round) // 이번 턴의 첫 지팡이 마법 피해 카드 확인
        { // 지팡이 심 처리 시작
            lastWandRound = round; // 같은 턴의 다른 지팡이 마법 제외
            remainingBonus = inventory.FindEffect(RelicEffectType.FirstWandDamage)?.EffectValue ?? 0; // 보유 지팡이 심의 가산량
        } // 지팡이 심 처리 종료
    } // 카드 사용 검사 종료

    public int ApplyHitBonus(int damage) // 명중이 확정된 직접 공격의 방어 전 수치 계산
    { // 첫 적중 처리 시작
        int result = (int)Math.Min(int.MaxValue, (long)damage + remainingBonus); // 정수 범위 안에서 피해 가산
        remainingBonus = 0; // 같은 카드의 다음 대상과 다음 타격에서 재발동 방지
        return result; // 방어 계산에 사용할 피해량
    } // 첫 적중 처리 종료
} // 첫 적중 보너스 종료
