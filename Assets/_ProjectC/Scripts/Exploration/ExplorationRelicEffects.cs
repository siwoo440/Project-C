using System; // 정신력 손실 하한 처리

internal static class ExplorationRelicEffects // 퇴색지대의 탐사와 전투 공통 유물 규칙
{ // 퇴색 유물 규칙 시작
    internal static int AdjustFadeMentalChange(int delta) // 퇴색지대에서 받는 정신력 손실 완화
    { // 퇴색 정신력 손실 확인 시작
        if (delta >= 0) // 아무 변화가 없거나 회복하는 효과 확인
        { // 완화 제외 시작
            return delta; // 기존 정신력 회복량 유지
        } // 완화 제외 종료
        int reduction = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.FadeMentalLossReduction)?.EffectValue ?? 0; // 실제 보유한 침식 방울귀걸이의 손실 정수 감소량
        return (int)Math.Min(0L, (long)delta + Math.Max(0, reduction)); // 손실만 줄인 뒤 정신력 증가로 넘어가지 않는 수치 보정
    } // 퇴색 정신력 손실 확인 종료
} // 퇴색 유물 규칙 종료
