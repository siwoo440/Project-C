using System; // 피해 수치 범위 계산 사용
using System.Collections.Generic; // 생존 적 후보 목록 사용
using UnityEngine; // 무작위 대상 선택 사용
public static class SunChainAttack // 태양의 다른 적 연속 공격
{ // 연속 공격 기능 시작
    private const string CardId = "CRD_ARC_19_CHAIN"; // 태양 전용 공격 카드 식별자
    public static bool IsChainCard(CardInstance card) // 태양 전용 연속 공격 카드 확인
    { // 전용 카드 확인 시작
        return card != null && card.SourceData != null && card.SourceData.CardId == CardId; // 태양 카드 식별자 일치 여부 반환
    } // 전용 카드 확인 종료
    public static BattleUnitRuntime TryAttack(CardInstance card, BattleUnitRuntime primary, IReadOnlyList<BattleUnitRuntime> enemies) // 다른 생존 적 하나에게 두 번째 공격 적용
    { // 기본 연속 공격 시작
        return TryAttack(card, primary, enemies, out _); // 명중 결과를 생략한 연속 공격 반환
    } // 기본 연속 공격 종료
    public static BattleUnitRuntime TryAttack(CardInstance card, BattleUnitRuntime primary, IReadOnlyList<BattleUnitRuntime> enemies, out bool missed, Func<int, int> applyHitBonus = null, int preparedSwordDamage = 0, Func<BattleUnitRuntime, int, int> applyTargetBonus = null) // 추가 대상과 빗나감 결과 조회
    { // 연속 공격 시작
        missed = false; // 빗나감 결과 초기화
        if (!IsChainCard(card) || card.OwnerUnit == null || card.OwnerUnit.IsDead || primary == null || enemies == null) // 유효한 카드와 공격자 확인
        { // 연속 공격 불가 시작
            return null; // 추가 대상 없음 반환
        } // 연속 공격 불가 종료
        List<BattleUnitRuntime> candidates = new List<BattleUnitRuntime>(); // 다른 생존 적 후보 생성
        HashSet<BattleUnitRuntime> unique = new HashSet<BattleUnitRuntime>(); // 동일 적 중복 선택 방지 목록 생성
        foreach (BattleUnitRuntime enemy in enemies) // 전투 적 목록 순회
        { // 추가 대상 확인 시작
            if (enemy != null && enemy != primary && !enemy.IsDead && enemy.Team == BattleTeam.Enemy && unique.Add(enemy)) // 첫 대상이 아닌 생존 적 확인
            { // 추가 후보 등록 시작
                candidates.Add(enemy); // 무작위 선택 후보 추가
            } // 추가 후보 등록 종료
        } // 추가 대상 확인 종료
        if (candidates.Count == 0) // 다른 생존 적 존재 확인
        { // 추가 대상 없음 처리 시작
            return null; // 연속 공격 생략 반환
        } // 추가 대상 없음 처리 종료
        BattleUnitRuntime target = candidates[RelicRandomCardTarget.PickIndex(candidates.Count, count => UnityEngine.Random.Range(0, count))]; // 태양의 추가 대상 추첨에 보유 주사위 반영
        if (!BattleHitChance.Roll(card.OwnerUnit, target)) // 두 번째 공격의 독립 명중 판정
        { // 추가 공격 빗나감 처리 시작
            missed = true; // 빗나감 결과 저장
            return target; // 빗나간 추가 대상 반환
        } // 추가 공격 빗나감 처리 종료
        int baseDamage = (int)Math.Min(int.MaxValue, (long)card.SourceData.SecondaryEffectValue + card.OwnerUnit.AttackPowerBonus + preparedSwordDamage); // 두 번째 기본 피해와 공격력 증가 계산
        baseDamage = SwordCardCombat.ApplyAttackStances(card, target, baseDamage); // 독립적인 둘째 대상의 체력과 검 강화 적용
        int damage = card.OwnerUnit.ModifyOutgoingDamage(baseDamage); // 공격자의 현재 정신 상태 피해 보정
        damage = applyTargetBonus?.Invoke(target, damage) ?? applyHitBonus?.Invoke(damage) ?? damage; // 첫 타격 빗나감 뒤 연속 공격 첫 적중 보너스 적용
        BattleCardDamageContext.Begin(card, new[] { target }); // 두 번째 적의 카드 약점 판정 문맥 설정
        try // 피해 판정 문맥 정리 보장
        { // 두 번째 공격 적용 시작
            target.TakeDamage(SwordCardCombat.RollCritical(card, damage, out _), card.DamageType, card.OwnerUnit); // 다른 적의 방어도와 약점을 반영한 피해 적용
        } // 두 번째 공격 적용 종료
        finally // 카드 피해 문맥 정리
        { // 피해 문맥 정리 시작
            BattleCardDamageContext.Clear(); // 사용된 추가 공격 문맥 제거
        } // 피해 문맥 정리 종료
        return target; // 실제 추가 공격 대상 반환
    } // 연속 공격 종료
} // 연속 공격 기능 종료
