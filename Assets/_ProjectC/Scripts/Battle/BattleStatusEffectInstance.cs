using System; // 기본 수학 기능 사용
public sealed class BattleStatusEffectInstance // 개별 상태 이상 런타임 정보
{ // 클래스 시작
    public BattleStatusEffectType EffectType { get; } // 상태 이상 종류
    public int Value { get; private set; } // 중첩당 효과 수치
    public int RemainingTurns { get; private set; } // 남은 발동 횟수
    public int StackCount { get; private set; } // 현재 중첩 수
    public int MaximumStacks { get; private set; } // 최대 중첩 수
    private int damageDecay; // 전체 지속 피해 수치에서 감소한 누적량
    private bool skipNextDurationDecrease; // 자신의 턴에 받은 상태의 첫 감소 생략 여부
    public int EffectiveValue => (int)Math.Max(0L, Math.Min(int.MaxValue, (long)Value * StackCount) - damageDecay); // 표시 범위와 누적 감소를 반영한 전체 수치
    public bool IsExpired => RemainingTurns <= 0 || (EffectType == BattleStatusEffectType.Burn || EffectType == BattleStatusEffectType.Poison) && EffectiveValue <= 0; // 지속 종료 또는 지속 피해 수치 소진 여부
    public string DisplayName => GetDisplayName(EffectType); // 화면 표시 이름
    public string IconLabel => GetIconLabel(EffectType); // 상태 아이콘 문구
    public string Description => GetDescription(EffectType, EffectiveValue); // 상태 상세 설명
    public bool IsDebuff => IsDebuffType(EffectType); // 디버프 여부
    public bool IsProtectedMentalState => EffectType == BattleStatusEffectType.Awakening || EffectType == BattleStatusEffectType.Collapse; // 강제 해제 불가 정신 상태 여부
    public BattleStatusEffectInstance(BattleStatusEffectType effectType, int value, int duration, int maximumStacks) // 상태 이상 생성
    { // 생성자 시작
        if (effectType == BattleStatusEffectType.None) // 상태 이상 종류 확인
        { // 잘못된 종류 처리 시작
            throw new ArgumentException("상태 이상 종류가 필요합니다.", nameof(effectType)); // 종류 누락 예외
        } // 잘못된 종류 처리 종료
        EffectType = effectType; // 상태 이상 종류 저장
        Value = Math.Max(1, value); // 최소 효과 수치 보정
        RemainingTurns = Math.Max(1, duration); // 최소 지속 횟수 보정
        MaximumStacks = Math.Max(1, maximumStacks); // 최소 최대 중첩 보정
        StackCount = 1; // 첫 중첩 적용
    } // 생성자 종료
    public void Refresh(int value, int duration, int maximumStacks) // 동일 상태 이상 재적용
    { // 재적용 시작
        if (IsProtectedMentalState) // 정신 상태의 외부 재적용 확인
        { // 정신 상태 재적용 차단 시작
            return; // 정신력 규칙에 의한 상태 유지
        } // 정신 상태 재적용 차단 종료
        Value = Math.Max(Value, Math.Max(1, value)); // 높은 효과 수치 유지
        RemainingTurns = Math.Max(RemainingTurns, Math.Max(1, duration)); // 긴 지속 횟수 유지
        MaximumStacks = Math.Max(MaximumStacks, Math.Max(1, maximumStacks)); // 높은 최대 중첩 유지
        StackCount = Math.Min(MaximumStacks, StackCount + 1); // 최대 범위 중첩 증가
        if (EffectType == BattleStatusEffectType.Poison || EffectType == BattleStatusEffectType.Burn) // 지속 피해 재부여 여부 확인
        { // 지속 피해 갱신 처리 시작
            damageDecay = (int)Math.Min(damageDecay, Math.Max(0L, Math.Min(int.MaxValue, (long)Value * StackCount) - Math.Max(1, value))); // 새 효과 수치 이상이 되도록 감소량 보정
        } // 지속 피해 갱신 처리 종료
    } // 재적용 종료
    public void AdvanceDuration() // 지속 횟수 감소
    { // 지속 감소 시작
        if (IsProtectedMentalState) // 정신 상태의 외부 턴 감소 확인
        { // 정신 상태 외부 감소 차단 시작
            return; // 정신력 규칙에 의한 턴 진행 유지
        } // 정신 상태 외부 감소 차단 종료
        RemainingTurns = Math.Max(0, RemainingTurns - 1); // 남은 횟수 한 단계 감소
    } // 지속 감소 종료
    internal void SyncMentalDuration(int remainingTurns) // 정신 상태의 실제 남은 턴 연결
    { // 정신 상태 턴 연결 시작
        if (IsProtectedMentalState) // 보호된 정신 상태 확인
        { // 정신 상태 턴 동기화 시작
            RemainingTurns = Math.Max(1, remainingTurns); // 실제 정신 상태 남은 턴 저장
        } // 정신 상태 턴 동기화 종료
    } // 정신 상태 턴 연결 종료
    public void SkipNextDurationDecrease() // 자기 턴의 첫 지속 감소 생략 예약
    { // 지속 감소 생략 시작
        skipNextDurationDecrease = true; // 현재 상태의 생략 여부 기록
    } // 지속 감소 생략 종료
    public bool ConsumeDurationDecreaseSkip() // 첫 지속 감소 생략 여부 소비
    { // 생략 여부 소비 시작
        bool skip = skipNextDurationDecrease; // 현재 생략 여부 저장
        skipNextDurationDecrease = false; // 사용한 생략 예약 제거
        return skip; // 이전 생략 여부 반환
    } // 생략 여부 소비 종료
    public void DecayPoison() // 전체 중독 수치 한 점 감소
    { // 중독 감소 시작
        if (EffectType == BattleStatusEffectType.Poison && EffectiveValue > 0) // 감소 가능한 중독 확인
        { // 중독 감소 처리 시작
            damageDecay = (int)Math.Min(int.MaxValue, (long)damageDecay + 1); // 전체 피해 수치의 감소량 누적
        } // 중독 감소 처리 종료
    } // 중독 감소 종료
    public void MergeTransferredState(BattleStatusEffectInstance transferred) // 이전한 디버프의 감소 상태 병합
    { // 이전 상태 병합 시작
        if (transferred == null || transferred.EffectType != EffectType) // 동일 상태 확인
        { // 병합 불가 처리 시작
            return; // 상태 병합 중단
        } // 병합 불가 처리 종료
        damageDecay = (int)Math.Min(int.MaxValue, (long)damageDecay + transferred.damageDecay); // 이전한 지속 피해 감소량 보존
        skipNextDurationDecrease |= transferred.skipNextDurationDecrease; // 이전한 지속 감소 생략 여부 보존
    } // 이전 상태 병합 종료
    public void DecayBurn() // 화상 수치 한 단계 감소
    { // 화상 감소 시작
        if (EffectType == BattleStatusEffectType.Burn && EffectiveValue > 0) // 감소 가능한 화상 확인
        { // 화상 수치 감소 시작
            damageDecay = (int)Math.Min(int.MaxValue, (long)damageDecay + 1); // 전체 화상 수치의 감소량 누적
        } // 화상 수치 감소 종료
    } // 화상 감소 종료
    public static string GetDisplayName(BattleStatusEffectType effectType) // 상태 이상 표시 이름 조회
    { // 표시 이름 조회 시작
        switch (effectType) // 상태 이상 종류 분기
        { // 종류 분기 시작
            case BattleStatusEffectType.Poison: // 중독 종류
                return "중독"; // 중독 이름 반환
            case BattleStatusEffectType.CriticalChanceUp: // 치명타 확률 증가 종류
                return "치명타 확률 증가"; // 전의 고취 상태 이름
            case BattleStatusEffectType.ChargeDefenseDown: // 돌진 약화 종류
                return "돌진 방어 감소"; // 돌진 약화 상태 이름
            case BattleStatusEffectType.Burn: // 화상 종류
                return "화상"; // 화상 이름 반환
            case BattleStatusEffectType.Blind: // 실명 종류
                return "실명"; // 실명 이름 반환
            case BattleStatusEffectType.Evasion: // 회피 종류
                return "회피"; // 회피 이름 반환
            case BattleStatusEffectType.Disorientation: // 방향 감각 상실 종류
                return "방향 감각 상실"; // 방향 감각 상실 이름 반환
            case BattleStatusEffectType.Regeneration: // 재생 종류
                return "재생"; // 재생 이름 반환
            case BattleStatusEffectType.AttackPowerUp: // 공격력 증가 종류
                return "공격 증가"; // 공격력 증가 이름 반환
            case BattleStatusEffectType.StatusImmunity: // 상태 면역 종류
                return "상태 면역"; // 상태 면역 이름 반환
            case BattleStatusEffectType.PhysicalDefenseUp: // 물리 방어 증가 종류
                return "물리 방어 증가"; // 물리 방어 증가 이름 반환
            case BattleStatusEffectType.PhysicalDefenseDown: // 물리 방어 감소 종류
                return "물리 방어 약화"; // 물리 방어 감소 이름 반환
            case BattleStatusEffectType.MagicalResistanceUp: // 마법 저항 증가 종류
                return "마법 저항 증가"; // 마법 저항 증가 이름 반환
            case BattleStatusEffectType.MagicalResistanceDown: // 마법 저항 감소 종류
                return "마법 저항 약화"; // 마법 저항 감소 이름 반환
            case BattleStatusEffectType.ChariotMark: // 전차 표식 종류
                return "표식"; // 전차 표식 이름 반환
            case BattleStatusEffectType.StarReservedHealing: // 별 예약 회복 종류
                return "예약 회복"; // 예약 회복 이름 반환
            case BattleStatusEffectType.StarPassiveReservedHealing: // 별 패시브 예약 회복 종류
                return "추가 예약 회복"; // 별도 예약 회복 이름 반환
            case BattleStatusEffectType.GuardPreparation: // 적 공격 대비 종류
                return "방어 준비"; // 방어 준비 이름 반환
            case BattleStatusEffectType.Disrupt: // 정신력 교란 종류
                return "교란"; // 정신력 교란 이름 반환
            case BattleStatusEffectType.Awakening: // 각성 종류
                return "각성"; // 각성 이름 반환
            case BattleStatusEffectType.Collapse: // 붕괴 종류
                return "붕괴"; // 붕괴 이름 반환
            default: // 알 수 없는 종류
                return "없음"; // 기본 이름 반환
        } // 종류 분기 종료
    } // 표시 이름 조회 종료
    public static bool IsDebuffType(BattleStatusEffectType effectType) // 디버프 종류 확인
    { // 디버프 확인 시작
        return effectType == BattleStatusEffectType.Poison || effectType == BattleStatusEffectType.Burn || effectType == BattleStatusEffectType.Disorientation || effectType == BattleStatusEffectType.Blind || effectType == BattleStatusEffectType.PhysicalDefenseDown || effectType == BattleStatusEffectType.MagicalResistanceDown || effectType == BattleStatusEffectType.Disrupt || effectType == BattleStatusEffectType.ChariotMark || effectType == BattleStatusEffectType.Collapse || effectType == BattleStatusEffectType.ChargeDefenseDown; // 감소 상태와 붕괴 디버프 결과 반환
    } // 디버프 확인 종료
    public static string GetIconLabel(BattleStatusEffectType effectType) // 상태 아이콘 문구 조회
    { // 아이콘 문구 조회 시작
        switch (effectType) // 상태 이상 종류 분기
        { // 종류 분기 시작
            case BattleStatusEffectType.Poison: // 중독 종류
                return "독"; // 중독 아이콘 반환
            case BattleStatusEffectType.CriticalChanceUp: // 치명타 확률 증가 종류
                return "치"; // 치명타 증가 약칭
            case BattleStatusEffectType.ChargeDefenseDown: // 돌진 약화 종류
                return "방"; // 물리 방어 감소 약칭
            case BattleStatusEffectType.Burn: // 화상 종류
                return "화"; // 화상 아이콘 반환
            case BattleStatusEffectType.Blind: // 실명 종류
                return "암"; // 실명 아이콘 반환
            case BattleStatusEffectType.Evasion: // 회피 종류
                return "회"; // 회피 아이콘 문구
            case BattleStatusEffectType.Disorientation: // 방향 감각 상실 종류
                return "혼"; // 방향 감각 상실 아이콘 문구
            case BattleStatusEffectType.Regeneration: // 재생 종류
                return "재"; // 재생 아이콘 반환
            case BattleStatusEffectType.AttackPowerUp: // 공격 증가 종류
                return "공"; // 공격 증가 아이콘 반환
            case BattleStatusEffectType.StatusImmunity: // 상태 면역 종류
                return "면"; // 상태 면역 아이콘 반환
            case BattleStatusEffectType.PhysicalDefenseUp: // 물리 방어 증가 종류
                return "물+"; // 물리 방어 증가 아이콘 반환
            case BattleStatusEffectType.PhysicalDefenseDown: // 물리 방어 감소 종류
                return "물-"; // 물리 방어 감소 아이콘 반환
            case BattleStatusEffectType.MagicalResistanceUp: // 마법 저항 증가 종류
                return "마+"; // 마법 저항 증가 아이콘 반환
            case BattleStatusEffectType.MagicalResistanceDown: // 마법 저항 감소 종류
                return "마-"; // 마법 저항 감소 아이콘 반환
            case BattleStatusEffectType.ChariotMark: // 전차 표식 종류
                return "표"; // 전차 표식 아이콘 반환
            case BattleStatusEffectType.StarReservedHealing: // 별 예약 회복 종류
                return "별"; // 예약 회복 아이콘 반환
            case BattleStatusEffectType.StarPassiveReservedHealing: // 별 패시브 예약 회복 종류
                return "빛"; // 추가 회복 아이콘 반환
            case BattleStatusEffectType.GuardPreparation: // 적 공격 대비 종류
                return "준"; // 방어 준비 아이콘 반환
            case BattleStatusEffectType.Disrupt: // 정신력 교란 종류
                return "란"; // 정신력 교란 아이콘 반환
            case BattleStatusEffectType.Awakening: // 각성 종류
                return "각"; // 각성 아이콘 반환
            case BattleStatusEffectType.Collapse: // 붕괴 종류
                return "붕"; // 붕괴 아이콘 반환
            default: // 알 수 없는 종류
                return "?"; // 기본 아이콘 반환
        } // 종류 분기 종료
    } // 아이콘 문구 조회 종료
    public static string GetDescription(BattleStatusEffectType effectType, int effectiveValue) // 상태 상세 설명 조회
    { // 상세 설명 조회 시작
        switch (effectType) // 상태 이상 종류 분기
        { // 종류 분기 시작
            case BattleStatusEffectType.Awakening: // 각성 종류
                return "정신력 최대 발동, 캐릭터별 피해·회복 변화, 강제 해제 불가"; // 각성 상태 설명 반환
            case BattleStatusEffectType.Collapse: // 붕괴 종류
                return "정신력 최소 발동, 캐릭터별 피해·회복 변화, 강제 해제 불가"; // 붕괴 상태 설명 반환
            case BattleStatusEffectType.Poison: // 중독 종류
                return $"진영 턴 시작에 피해 {effectiveValue}, 중독 수치 1 감소"; // 중독 피해와 감소 설명 반환
            case BattleStatusEffectType.CriticalChanceUp: // 치명타 확률 증가 종류
                return $"카드 타격의 치명타 확률 +{effectiveValue}%"; // 치명타 확률 보정 표시
            case BattleStatusEffectType.ChargeDefenseDown: // 돌진 약화 종류
                return $"이번 아군 턴 물리 방어력 −{effectiveValue}"; // 돌진의 적용 턴과 감소량 표시
            case BattleStatusEffectType.Burn: // 화상 종류
                return $"진영 턴 종료에 피해 {effectiveValue}, 화상 수치 1 감소"; // 화상 설명 반환
            case BattleStatusEffectType.Blind: // 실명 종류
                return $"공격 명중률 {effectiveValue}%p 감소"; // 실명 설명 반환
            case BattleStatusEffectType.Evasion: // 회피 종류
                return $"적 공격 회피율 {effectiveValue}%p 증가 · 적 턴 종료에 지속 감소"; // 회피 설명 반환
            case BattleStatusEffectType.Disorientation: // 방향 감각 상실 종류
                return $"첫 아군 턴 공격 명중률 {effectiveValue}%p 감소"; // 귀를 막은 효과 설명
            case BattleStatusEffectType.Regeneration: // 재생 종류
                return $"진영 턴 시작에 체력 회복 {effectiveValue}"; // 재생 설명 반환
            case BattleStatusEffectType.AttackPowerUp: // 공격 증가 종류
                return $"피해 카드 원본 피해 증가 {effectiveValue}"; // 공격 증가 설명 반환
            case BattleStatusEffectType.StatusImmunity: // 상태 면역 종류
                return "새 일반 디버프 차단, 정신력 붕괴 제외"; // 상태 면역과 붕괴 예외 설명 반환
            case BattleStatusEffectType.PhysicalDefenseUp: // 물리 방어 증가 종류
                return $"물리 방어력 증가 {effectiveValue}"; // 물리 방어 증가 설명 반환
            case BattleStatusEffectType.PhysicalDefenseDown: // 물리 방어 감소 종류
                return $"물리 방어력 감소 {effectiveValue}"; // 물리 방어 감소 설명 반환
            case BattleStatusEffectType.MagicalResistanceUp: // 마법 저항 증가 종류
                return $"마법 저항력 증가 {effectiveValue}"; // 마법 저항 증가 설명 반환
            case BattleStatusEffectType.MagicalResistanceDown: // 마법 저항 감소 종류
                return $"마법 저항력 감소 {effectiveValue}"; // 마법 저항 감소 설명 반환
            case BattleStatusEffectType.ChariotMark: // 전차 표식 종류
                return $"이번 턴 돌파 표식 {effectiveValue}개. 대상 변경 또는 턴 종료 시 제거"; // 전차 표식 설명 반환
            case BattleStatusEffectType.StarReservedHealing: // 별 예약 회복 종류
                return $"다음 아군 턴 시작 체력 회복 {effectiveValue}"; // 예약 회복 설명 반환
            case BattleStatusEffectType.StarPassiveReservedHealing: // 별 패시브 예약 회복 종류
                return $"다음 아군 턴 시작 추가 체력 회복 {effectiveValue}"; // 추가 예약 회복 설명 반환
            case BattleStatusEffectType.GuardPreparation: // 적 공격 대비 종류
                return $"다음 적 공격 직전 방어도 {effectiveValue} 획득"; // 방어 준비 설명 반환
            case BattleStatusEffectType.Disrupt: // 정신력 교란 종류
                return "적용 시 정신력 4 감소"; // 정신력 교란 설명 반환
            default: // 알 수 없는 종류
                return "효과 없음"; // 기본 설명 반환
        } // 종류 분기 종료
    } // 상세 설명 조회 종료
} // 클래스 종료
