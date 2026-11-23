using System; // 중첩 한도 계산 사용
using System.Collections.Generic; // 이전 후보 목록 사용
public static class BattleDebuffTransfer // 아군 사이의 단일 디버프 이전
{ // 디버프 이전 기능 시작
    public static IReadOnlyList<BattleStatusEffectInstance> GetChoices(BattleUnitRuntime source, BattleUnitRuntime recipient) // 현재 이전 가능한 디버프 후보 조회
    { // 이전 후보 조회 시작
        List<BattleStatusEffectInstance> choices = new List<BattleStatusEffectInstance>(); // 빈 디버프 후보 목록 생성
        if (source == null) // 원래 동료 존재 확인
        { // 동료 없음 처리 시작
            return choices.AsReadOnly(); // 빈 후보 목록 반환
        } // 동료 없음 처리 종료
        foreach (BattleStatusEffectInstance effect in source.StatusEffects) // 동료의 전체 상태 순회
        { // 이전 후보 확인 시작
            if (CanTransfer(source, recipient, effect)) // 디버프 이전 가능 여부 확인
            { // 이전 후보 추가 시작
                choices.Add(effect); // 이전 가능한 디버프 추가
            } // 이전 후보 추가 종료
        } // 이전 후보 확인 종료
        return choices.AsReadOnly(); // 외부 변경을 막은 후보 목록 반환
    } // 이전 후보 조회 종료
    public static bool CanTransfer(BattleUnitRuntime source, BattleUnitRuntime recipient, BattleStatusEffectInstance effect) // 지정 디버프의 이전 가능 여부 확인
    { // 이전 가능 확인 시작
        if (source == null || recipient == null || source == recipient || source.IsDead || recipient.IsDead || source.Team != BattleTeam.Ally || recipient.Team != BattleTeam.Ally || recipient.HasStatusImmunity || effect == null || !effect.IsDebuff || !source.ContainsStatusEffectInstance(effect)) // 생존한 서로 다른 아군과 실제 디버프 확인
        { // 이전 불가 처리 시작
            return false; // 이전 불가 반환
        } // 이전 불가 처리 종료
        BattleStatusEffectInstance existing = recipient.GetStatusEffect(effect.EffectType); // 받는 쪽의 동일 디버프 조회
        if (existing == null) // 동일 디버프 없음 확인
        { // 신규 이전 처리 시작
            return true; // 신규 상태 이전 허용
        } // 신규 이전 처리 종료
        int maximum = Math.Max(existing.MaximumStacks, effect.MaximumStacks); // 기존 규칙 기준 중첩 한도 확인
        return (long)existing.StackCount + effect.StackCount <= maximum; // 중첩 손실 없는 병합만 허용
    } // 이전 가능 확인 종료
    public static bool TryTransfer(BattleUnitRuntime source, BattleUnitRuntime recipient, BattleStatusEffectInstance effect) // 선택한 디버프 하나를 아군에게 이전
    { // 디버프 이전 시작
        if (!CanTransfer(source, recipient, effect)) // 디버프 이전 가능 여부 재확인
        { // 이전 불가 처리 시작
            return false; // 원래 상태 유지와 실패 반환
        } // 이전 불가 처리 종료
        return source.MoveDebuffTo(recipient, effect); // 양쪽 상태를 함께 갱신한 결과 반환
    } // 디버프 이전 종료
} // 디버프 이전 기능 종료
