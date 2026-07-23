using UnityEngine; // Unity 기본 기능

public sealed class BattleCardEffectResolver : MonoBehaviour // 카드 효과 처리 관리자
{
    public bool CanResolve(CardInstance card, BattleUnit targetUnit) // 효과 처리 가능 여부
    {
        if (card == null) // 카드 누락 확인
        {
            return false; // 처리 불가 반환
        }

        if (targetUnit == null) // 대상 유닛 누락 확인
        {
            return false; // 처리 불가 반환
        }

        if (targetUnit.IsDefeated) // 대상 전투 불능 확인
        {
            return false; // 처리 불가 반환
        }

        switch (card.EffectType) // 카드 효과 종류 확인
        {
            case CardEffectType.Damage: // 피해 효과 확인
                return card.CurrentEffectValue > 0; // 양수 피해 여부 반환

            default: // 지원하지 않는 효과
                return false; // 처리 불가 반환
        }
    }

    public bool TryResolve(CardInstance card, BattleUnit targetUnit) // 카드 효과 적용 시도
    {
        if (CanResolve(card, targetUnit) == false) // 효과 처리 가능 여부 확인
        {
            Debug.LogWarning("Card effect cannot be resolved.", this); // 효과 처리 실패 경고
            return false; // 효과 적용 실패 반환
        }

        switch (card.EffectType) // 카드 효과 종류 확인
        {
            case CardEffectType.Damage: // 피해 효과 확인
                targetUnit.TakeDamage(card.CurrentEffectValue); // 대상 피해 적용
                Debug.Log($"Card damage applied: {card.CardName} / Target: {targetUnit.UnitName} / Damage: {card.CurrentEffectValue}", this); // 피해 적용 결과 출력
                return true; // 효과 적용 성공 반환

            default: // 지원하지 않는 효과
                Debug.LogWarning($"Unsupported card effect: {card.EffectType}", this); // 미지원 효과 경고
                return false; // 효과 적용 실패 반환
        }
    }
}