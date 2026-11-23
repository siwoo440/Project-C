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
    public bool WaitsForAction => EffectType == BattleStatusEffectType.Stagger || EffectType == BattleStatusEffectType.NextAttackDown || EffectType == BattleStatusEffectType.ShieldBalance || EffectType == BattleStatusEffectType.DangerInterception || EffectType == BattleStatusEffectType.DamageNullification || EffectType == BattleStatusEffectType.GuardianHeart || EffectType == BattleStatusEffectType.DefeatWard || EffectType == BattleStatusEffectType.DelayedHealing || EffectType == BattleStatusEffectType.StealthBreakDamageUp || EffectType == BattleStatusEffectType.ManaConcentration || EffectType == BattleStatusEffectType.ConfessionDamageMark; // 정해진 행동과 실제 피해 또는 다음 아군 턴을 기다리는 상태
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
    internal BattleStatusEffectInstance CopyBlessingState() // 실제 수치와 지속을 가진 독립 강화 복제
    { // 상태 복제 시작
        return (BattleStatusEffectInstance)MemberwiseClone(); // 원본 상태 개체와 분리된 중첩 및 지속 예약 복사
    } // 상태 복제 종료
    internal void MergeBlessingCopy(BattleStatusEffectInstance blessing) // 활성 강화의 복사본과 수령자의 기존 상태 병합
    { // 같은 강화 병합 시작
        Value = Math.Max(Value, blessing.Value); // 기존 상태 규칙의 더 강한 한 중첩 수치 선택
        MaximumStacks = Math.Max(MaximumStacks, blessing.MaximumStacks); // 원본과 수령자 중 높은 중첩 상한 보존
        StackCount = (int)Math.Min(MaximumStacks, (long)StackCount + blessing.StackCount); // 원본의 실제 전체 중첩 가산
        long existingLifetime = (long)RemainingTurns + (skipNextDurationDecrease ? 1 : 0); // 수령자의 다음 감소 생략을 반영한 수명
        long blessingLifetime = (long)blessing.RemainingTurns + (blessing.skipNextDurationDecrease ? 1 : 0); // 복사할 강화의 다음 감소 생략을 반영한 수명
        if (blessingLifetime > existingLifetime) // 두 활성 강화 중 더 긴 기존 수명 확인
        { // 복사한 강화의 수명 적용 시작
            RemainingTurns = blessing.RemainingTurns; // 복사한 강화의 실제 남은 턴 사용
            skipNextDurationDecrease = blessing.skipNextDurationDecrease; // 원본의 다음 감소 예약 시간 사용
        } // 복사한 강화의 수명 적용 종료
    } // 같은 강화 병합 종료
    public void AdvanceDuration() // 지속 횟수 감소
    { // 지속 감소 시작
        if (IsProtectedMentalState) // 정신 상태의 외부 턴 감소 확인
        { // 정신 상태 외부 감소 차단 시작
            return; // 정신력 규칙에 의한 턴 진행 유지
        } // 정신 상태 외부 감소 차단 종료
        RemainingTurns = Math.Max(0, RemainingTurns - 1); // 남은 횟수 한 단계 감소
    } // 지속 감소 종료
    internal bool TryExtendActiveDuration(int turns) // 기존 활성 상태의 지속 시간만 연장
    { // 실제 상태 연장 검사 시작
        if (turns <= 0 || IsExpired || IsProtectedMentalState || RemainingTurns == int.MaxValue) // 유효 시간과 활성 시간제 상태 확인
        { // 잘못된 연장 시도 처리 시작
            return false; // 이미 끝났거나 더 늘릴 수 없는 상태 보존
        } // 잘못된 연장 시도 처리 종료
        RemainingTurns = (int)Math.Min(int.MaxValue, (long)RemainingTurns + turns); // 수치와 중첩 및 감소 예약을 유지한 남은 턴만 증가
        return true; // 동일 상태의 지속 연장 완료
    } // 실제 상태 연장 검사 종료
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
            case BattleStatusEffectType.Bleed: // 출혈 종류
                return "출혈"; // 출혈 표시
            case BattleStatusEffectType.Stagger: // 행동 취소 종류
                return "행동 취소"; // 행동 취소 표시
            case BattleStatusEffectType.Stun: // 기절 종류
                return "기절"; // 기절 표시
            case BattleStatusEffectType.BattlefieldResolve: // 전장의 결의 종류
                return "전장의 결의"; // 다음 턴 방어 예약 표시
            case BattleStatusEffectType.BladeFrenzy: // 칼날 폭주 종류
                return "칼날 폭주"; // 이번 턴 검 피해 강화 표시
            case BattleStatusEffectType.BloodOath: // 피의 맹세 종류
                return "피의 맹세"; // 피의 맹세 표시
            case BattleStatusEffectType.Fear: // 공포 종류
                return "공포"; // 공포 표시
            case BattleStatusEffectType.FinishingStance: // 마무리 자세 종류
                return "마무리 자세"; // 마무리 자세 표시
            case BattleStatusEffectType.HealingReceivedDown: // 회복량 감소 종류
                return "회복량 감소"; // 회복량 감소 표시
            case BattleStatusEffectType.BladeMaintenance: // 칼날 정비 종류
                return "칼날 정비"; // 칼날 정비 표시
            case BattleStatusEffectType.PhysicalRiposte: // 검격 반사 종류
                return "검격 반사"; // 검격 반사 표시
            case BattleStatusEffectType.NextAttackDown: // 다음 공격 약화 종류
                return "다음 공격 약화"; // 다음 공격 약화 표시
            case BattleStatusEffectType.AttackPowerDown: // 이번 적 턴 공격 약화
                return "공격력 감소"; // 청동 경보장치의 약화 표시
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
            case BattleStatusEffectType.DuelMark: // 결투 표식 정보
                return "결투 표식"; // 결투 상태 표시
            case BattleStatusEffectType.ShockAbsorption: // 충격 흡수 이름 확인
                return "충격 흡수"; // 충격 흡수 표시 이름
            case BattleStatusEffectType.ResistanceStance: // 저항 태세 이름 확인
                return "저항 태세"; // 저항 태세 표시 이름
            case BattleStatusEffectType.SwordSaintStance: // 검성의 자세 정보
                return "검성의 자세"; // 검 카드 강화 표시
            case BattleStatusEffectType.ActionSpeedDown: // 행동 속도 감소 종류
                return "속도 감소"; // 속도 감소 표시
            case BattleStatusEffectType.ShieldBalance: // 균형 잡기 종류
                return "균형 잡기"; // 균형 잡기의 표시 문구
            case BattleStatusEffectType.ShieldCounter: // 방패 반격 종류
                return "방패 반격"; // 방패 반격의 표시 문구
            case BattleStatusEffectType.GuardianCircle: // 수호의 원 종류
                return "수호의 원"; // 수호의 원의 표시 문구
            case BattleStatusEffectType.DangerInterception: // 위험 차단 종류
                return "위험 차단"; // 위험 차단의 표시 문구
            case BattleStatusEffectType.FocusedDefense: // 집중 방어 상태 구분
                return "집중 방어"; // 집중 방어의 실제 화면 문구
            case BattleStatusEffectType.BleedResistance: // 출혈 저항 상태 구분
                return "출혈 저항"; // 출혈 저항의 실제 화면 문구
            case BattleStatusEffectType.UndyingGuard: // 불굴 상태 구분
                return "불굴"; // 불굴의 실제 화면 문구
            case BattleStatusEffectType.DamageNullification: // 피해 무효 상태 분기
                return "피해 무효"; // 피해 무효 표시 문구 반환
            case BattleStatusEffectType.DistributedDefense: // 분산 방어 상태 분기
                return "분산 방어"; // 분산 방어 표시 문구 반환
            case BattleStatusEffectType.RetributionBulwark: // 응징 방벽 상태 분기
                return "응징 방벽"; // 응징 방벽 표시 문구 반환
            case BattleStatusEffectType.FortifyResistance: // 요새화 저항 상태 분기
                return "요새화 저항"; // 요새화 저항 표시 문구 반환
            case BattleStatusEffectType.EternalShield: // 영원의 방패 상태 분기
                return "영원의 방패"; // 영원의 방패 표시 문구 반환
            case BattleStatusEffectType.EternalShieldResistance: // 영원의 방패 저항 상태 분기
                return "영원의 방패 저항"; // 영원의 방패 저항 표시 문구 반환
            case BattleStatusEffectType.ReflectionCitadel: // 반사의 성채 상태 분기
                return "반사의 성채"; // 반사의 성채 표시 문구 반환
            case BattleStatusEffectType.GuardianHeart: // 수호자의 심장 상태 분기
                return "수호자의 심장"; // 수호자의 심장 표시 문구 반환
            case BattleStatusEffectType.DefeatWard: // 전투불능 방지 상태 확인
                return "전투불능 방지"; // 해당 화면의 전투불능 방지 문구
            case BattleStatusEffectType.SanctuaryWard: // 성역 종류 확인
                return "성역"; // 게임에 표시되는 성역 문구
            case BattleStatusEffectType.FearResistance: // 공포 저항 종류 확인
                return "공포 저항"; // 게임에 표시되는 공포 저항 문구
            case BattleStatusEffectType.ResurrectionSeed: // 부활의 씨앗 종류 확인
                return "부활의 씨앗"; // 게임에 표시되는 부활의 씨앗 문구
            case BattleStatusEffectType.PainDistribution: // 고통 분산 종류 확인
                return "고통 분산"; // 게임에 표시되는 고통 분산 문구
            case BattleStatusEffectType.CorrosionResistanceDown: // 침식 저항 약화 종류 확인
                return "침식 저항 약화"; // 게임에 표시되는 침식 저항 약화 문구
            case BattleStatusEffectType.PulseCordAttackPowerUp: // 침식된 맥박 종류 확인
                return "침식된 맥박"; // 게임에 표시되는 침식된 맥박 문구
            case BattleStatusEffectType.ArcaneWard: // 마력 보호막 종류 확인
                return "마력 보호막"; // 게임에 표시되는 마력 보호막 문구
            case BattleStatusEffectType.MagicAttackPowerUp: // 마력 증폭 종류 확인
                return "마력 증폭"; // 게임에 표시되는 마력 증폭 문구
            case BattleStatusEffectType.WandEcho: // 마법 반향 상태 확인
                return "마법 반향"; // 실제 전투와 도감의 상태 문구
            case BattleStatusEffectType.ConfessionDamageMark: // 고해의 표식 종류 확인
                return "고해의 표식"; // 전투와 도감의 고해 피해 표시
            case BattleStatusEffectType.IncomingMagicDamageUp: // 새 지팡이 약화 상태 확인
                return "마법 취약"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.EvasionDown: // 새 지팡이 약화 상태 확인
                return "회피율 감소"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.ManaConcentration: // 마력 집중 상태 확인
                return "마력 집중"; // 마력 집중의 실제 전투 화면 문구
            case BattleStatusEffectType.StealthBreakDamageUp: // 팔찌의 실제 다음 명중 강화 확인
                return "은신 해제 타격"; // 전투와 도감에서 조회할 은신 해제 표시
            case BattleStatusEffectType.Shock: // 감전 종류 확인
                return "감전"; // 게임에 표시되는 감전 문구
            case BattleStatusEffectType.StethoscopeMark: // 청진기 표식 확인
                return "청진기 표식"; // 전투 화면과 도감의 상태 안내
            case BattleStatusEffectType.CrystalMarkDefenseDown: // 표식 방어 약화 종류 확인
                return "표식 방어 약화"; // 게임에 표시되는 표식 방어 약화 문구
            case BattleStatusEffectType.MiracleEcho: // 기적의 잔향 종류 확인
                return "기적의 잔향"; // 게임에 표시되는 기적의 잔향 문구
            case BattleStatusEffectType.Stealth: // 은신 상태 확인
                return "은신"; // 현재 화면의 은신 설명
            case BattleStatusEffectType.DelayedHealing: // 다음 턴 회복 종류 확인
                return "다음 턴 회복"; // 게임에 표시되는 다음 턴 회복 문구
            case BattleStatusEffectType.HealingReceivedUp: // 받는 회복량 증가 종류 확인
                return "받는 회복량 증가"; // 게임에 표시되는 받는 회복량 증가 설명
            case BattleStatusEffectType.Renewal: // 재생 종류 확인
                return "재생"; // 게임에 표시되는 재생 설명
            case BattleStatusEffectType.ActionSpeedUp: // 행동 속도 증가 종류
                return "속도 증가"; // 속도 증가 이름
            case BattleStatusEffectType.Vulnerable: // 검은 왕관의 취약 상태
                return "취약"; // 취약의 표시 이름
            case BattleStatusEffectType.PublicMark: // 수신기의 공명 표식
                return "공명 표식"; // 공명 표식 표시
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
        return effectType == BattleStatusEffectType.ConfessionDamageMark || effectType == BattleStatusEffectType.IncomingMagicDamageUp || effectType == BattleStatusEffectType.EvasionDown || effectType == BattleStatusEffectType.Shock || effectType == BattleStatusEffectType.StethoscopeMark || effectType == BattleStatusEffectType.CorrosionResistanceDown || effectType == BattleStatusEffectType.CrystalMarkDefenseDown || effectType == BattleStatusEffectType.Vulnerable || effectType == BattleStatusEffectType.DuelMark || effectType == BattleStatusEffectType.ActionSpeedDown || effectType == BattleStatusEffectType.Fear || effectType == BattleStatusEffectType.HealingReceivedDown || effectType == BattleStatusEffectType.Poison || effectType == BattleStatusEffectType.Burn || effectType == BattleStatusEffectType.Disorientation || effectType == BattleStatusEffectType.Blind || effectType == BattleStatusEffectType.PhysicalDefenseDown || effectType == BattleStatusEffectType.MagicalResistanceDown || effectType == BattleStatusEffectType.Disrupt || effectType == BattleStatusEffectType.ChariotMark || effectType == BattleStatusEffectType.PublicMark || effectType == BattleStatusEffectType.Collapse || effectType == BattleStatusEffectType.ChargeDefenseDown || effectType == BattleStatusEffectType.Bleed || effectType == BattleStatusEffectType.Stagger || effectType == BattleStatusEffectType.Stun || effectType == BattleStatusEffectType.NextAttackDown || effectType == BattleStatusEffectType.AttackPowerDown; // 취약과 나머지 부정 상태의 디버프 분류
    } // 디버프 확인 종료
    public static string GetIconLabel(BattleStatusEffectType effectType) // 상태 아이콘 문구 조회
    { // 아이콘 문구 조회 시작
        switch (effectType) // 상태 이상 종류 분기
        { // 종류 분기 시작
            case BattleStatusEffectType.Poison: // 중독 종류
                return "독"; // 중독 아이콘 반환
            case BattleStatusEffectType.CriticalChanceUp: // 치명타 확률 증가 종류
                return "치"; // 치명타 증가 약칭
            case BattleStatusEffectType.Bleed: // 출혈 종류
                return "혈"; // 출혈 표시
            case BattleStatusEffectType.Stagger: // 행동 취소 종류
                return "취"; // 행동 취소 표시
            case BattleStatusEffectType.Stun: // 기절 종류
                return "절"; // 기절 표시
            case BattleStatusEffectType.BattlefieldResolve: // 전장의 결의 종류
                return "결"; // 다음 턴 방어 예약 표시
            case BattleStatusEffectType.BladeFrenzy: // 칼날 폭주 종류
                return "폭"; // 이번 턴 검 피해 강화 표시
            case BattleStatusEffectType.BloodOath: // 피의 맹세 종류
                return "맹"; // 피의 맹세 표시
            case BattleStatusEffectType.Fear: // 공포 종류
                return "공"; // 공포 표시
            case BattleStatusEffectType.FinishingStance: // 마무리 자세 종류
                return "마"; // 마무리 자세 표시
            case BattleStatusEffectType.HealingReceivedDown: // 회복량 감소 종류
                return "감"; // 회복량 감소 표시
            case BattleStatusEffectType.BladeMaintenance: // 칼날 정비 종류
                return "정"; // 칼날 정비 표시
            case BattleStatusEffectType.PhysicalRiposte: // 검격 반사 종류
                return "반"; // 검격 반사 표시
            case BattleStatusEffectType.NextAttackDown: // 다음 공격 약화 종류
                return "약"; // 다음 공격 약화 표시
            case BattleStatusEffectType.AttackPowerDown: // 이번 적 턴 공격 약화
                return "공−"; // 청동 경보장치의 약화 표시
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
            case BattleStatusEffectType.DuelMark: // 결투 표식 정보
                return "투"; // 결투 상태 표시
            case BattleStatusEffectType.ShockAbsorption: // 충격 흡수 아이콘 확인
                return "충격"; // 축약 상태 이름
            case BattleStatusEffectType.ResistanceStance: // 저항 태세 아이콘 확인
                return "저항"; // 축약 상태 이름
            case BattleStatusEffectType.SwordSaintStance: // 검성의 자세 정보
                return "성"; // 검 카드 강화 표시
            case BattleStatusEffectType.ActionSpeedDown: // 행동 속도 감소 종류
                return "속"; // 속도 감소 표시
            case BattleStatusEffectType.ShieldBalance: // 균형 잡기 종류
                return "균형"; // 균형 잡기의 표시 문구
            case BattleStatusEffectType.ShieldCounter: // 방패 반격 종류
                return "반격"; // 방패 반격의 표시 문구
            case BattleStatusEffectType.GuardianCircle: // 수호의 원 종류
                return "수호"; // 수호의 원의 표시 문구
            case BattleStatusEffectType.DangerInterception: // 위험 차단 종류
                return "차단"; // 위험 차단의 표시 문구
            case BattleStatusEffectType.FocusedDefense: // 집중 방어 상태 구분
                return "집중"; // 집중 방어의 실제 화면 문구
            case BattleStatusEffectType.BleedResistance: // 출혈 저항 상태 구분
                return "저항"; // 출혈 저항의 실제 화면 문구
            case BattleStatusEffectType.UndyingGuard: // 불굴 상태 구분
                return "불굴"; // 불굴의 실제 화면 문구
            case BattleStatusEffectType.DamageNullification: // 피해 무효 상태 분기
                return "무효"; // 피해 무효 표시 문구 반환
            case BattleStatusEffectType.DistributedDefense: // 분산 방어 상태 분기
                return "분산"; // 분산 방어 표시 문구 반환
            case BattleStatusEffectType.RetributionBulwark: // 응징 방벽 상태 분기
                return "응징"; // 응징 방벽 표시 문구 반환
            case BattleStatusEffectType.FortifyResistance: // 요새화 저항 상태 분기
                return "요새"; // 요새화 저항 표시 문구 반환
            case BattleStatusEffectType.EternalShield: // 영원의 방패 상태 분기
                return "영원"; // 영원의 방패 표시 문구 반환
            case BattleStatusEffectType.EternalShieldResistance: // 영원의 방패 저항 상태 분기
                return "영원 저항"; // 영원의 방패 저항 표시 문구 반환
            case BattleStatusEffectType.ReflectionCitadel: // 반사의 성채 상태 분기
                return "반사"; // 반사의 성채 표시 문구 반환
            case BattleStatusEffectType.GuardianHeart: // 수호자의 심장 상태 분기
                return "수호"; // 수호자의 심장 표시 문구 반환
            case BattleStatusEffectType.DefeatWard: // 전투불능 방지 상태 확인
                return "생존"; // 해당 화면의 전투불능 방지 문구
            case BattleStatusEffectType.SanctuaryWard: // 성역 종류 확인
                return "성역"; // 게임에 표시되는 성역 문구
            case BattleStatusEffectType.FearResistance: // 공포 저항 종류 확인
                return "공포저항"; // 게임에 표시되는 공포 저항 문구
            case BattleStatusEffectType.ResurrectionSeed: // 부활의 씨앗 종류 확인
                return "부활"; // 게임에 표시되는 부활의 씨앗 문구
            case BattleStatusEffectType.PainDistribution: // 고통 분산 종류 확인
                return "분산"; // 게임에 표시되는 고통 분산 문구
            case BattleStatusEffectType.CorrosionResistanceDown: // 침식 저항 약화 종류 확인
                return "저항↓"; // 게임에 표시되는 침식 저항 약화 문구
            case BattleStatusEffectType.PulseCordAttackPowerUp: // 침식된 맥박 종류 확인
                return "공격+"; // 게임에 표시되는 침식된 맥박 문구
            case BattleStatusEffectType.ArcaneWard: // 마력 보호막 종류 확인
                return "마법저항+"; // 게임에 표시되는 마력 보호막 문구
            case BattleStatusEffectType.MagicAttackPowerUp: // 마력 증폭 종류 확인
                return "마법피해+"; // 게임에 표시되는 마력 증폭 문구
            case BattleStatusEffectType.WandEcho: // 마법 반향 상태 확인
                return "반향"; // 실제 전투와 도감의 상태 문구
            case BattleStatusEffectType.ConfessionDamageMark: // 고해의 표식 종류 확인
                return "고해"; // 전투와 도감의 고해 피해 표시
            case BattleStatusEffectType.IncomingMagicDamageUp: // 새 지팡이 약화 상태 확인
                return "마법피해+"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.EvasionDown: // 새 지팡이 약화 상태 확인
                return "회피−"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.ManaConcentration: // 마력 집중 상태 확인
                return "집중"; // 마력 집중의 실제 전투 화면 문구
            case BattleStatusEffectType.StealthBreakDamageUp: // 팔찌의 실제 다음 명중 강화 확인
                return "다음타격+"; // 전투와 도감에서 조회할 은신 해제 표시
            case BattleStatusEffectType.Shock: // 감전 종류 확인
                return "감전"; // 게임에 표시되는 감전 문구
            case BattleStatusEffectType.StethoscopeMark: // 청진기 표식 확인
                return "표식"; // 전투 화면과 도감의 상태 안내
            case BattleStatusEffectType.CrystalMarkDefenseDown: // 표식 방어 약화 종류 확인
                return "방어↓"; // 게임에 표시되는 표식 방어 약화 문구
            case BattleStatusEffectType.MiracleEcho: // 기적의 잔향 종류 확인
                return "회복+"; // 게임에 표시되는 기적의 잔향 문구
            case BattleStatusEffectType.Stealth: // 은신 상태 확인
                return "은신"; // 현재 화면의 은신 설명
            case BattleStatusEffectType.DelayedHealing: // 다음 턴 회복 종류 확인
                return "예약회복"; // 게임에 표시되는 다음 턴 회복 문구
            case BattleStatusEffectType.HealingReceivedUp: // 받는 회복량 증가 종류 확인
                return "회복+"; // 게임에 표시되는 받는 회복량 증가 설명
            case BattleStatusEffectType.Renewal: // 재생 종류 확인
                return "재생"; // 게임에 표시되는 재생 설명
            case BattleStatusEffectType.ActionSpeedUp: // 행동 속도 증가 종류
                return "속+"; // 속도 증가 축약
            case BattleStatusEffectType.Vulnerable: // 검은 왕관의 취약 상태
                return "취약"; // 취약 아이콘의 대체 글자
            case BattleStatusEffectType.PublicMark: // 수신기의 공명 표식
                return "공명"; // 공명 표식 표시
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
            case BattleStatusEffectType.Bleed: // 출혈 종류
                return $"대상 턴 종료에 체력 {effectiveValue} 감소 · 방어도 무시"; // 출혈 표시
            case BattleStatusEffectType.Stagger: // 행동 취소 종류
                return "다음 예고 행동 한 번 취소"; // 행동 취소 표시
            case BattleStatusEffectType.Stun: // 기절 종류
                return "남은 턴 동안 적의 모든 행동 취소"; // 기절 표시
            case BattleStatusEffectType.BattlefieldResolve: // 전장의 결의 종류
                return $"다음 아군 턴 시작에 방어도 {effectiveValue} 획득"; // 다음 턴 방어 예약 표시
            case BattleStatusEffectType.BladeFrenzy: // 칼날 폭주 종류
                return $"이번 턴 소유자의 검 피해 카드 모든 타격에 피해 +{effectiveValue}"; // 이번 턴 검 피해 강화 표시
            case BattleStatusEffectType.BloodOath: // 피의 맹세 종류
                return $"검 카드의 모든 타격에 피해 +{effectiveValue}"; // 피의 맹세 표시
            case BattleStatusEffectType.Fear: // 공포 종류
                return "대상의 회피율 보정 무효. 공격자의 실명은 유지."; // 공포 표시
            case BattleStatusEffectType.FinishingStance: // 마무리 자세 종류
                return $"이번 턴 체력 절반 이하인 적에게 주는 카드 피해 +{effectiveValue}%"; // 마무리 자세 표시
            case BattleStatusEffectType.HealingReceivedDown: // 회복량 감소 종류
                return $"받는 체력 회복량 −{effectiveValue}% · 소수점 이하 내림"; // 회복량 감소 표시
            case BattleStatusEffectType.BladeMaintenance: // 칼날 정비 종류
                return $"이번 턴 다음 검 피해 카드의 모든 타격에 피해 +{effectiveValue}. 사용 시 소모."; // 칼날 정비 표시
            case BattleStatusEffectType.PhysicalRiposte: // 검격 반사 종류
                return $"적의 첫 직접 물리 체력 피해를 받고 생존하면 잃은 체력의 {effectiveValue}%를 고정 반격. 다음 아군 턴 시작에 해제."; // 검격 반사 표시
            case BattleStatusEffectType.NextAttackDown: // 다음 공격 약화 종류
                return $"다음 공격 한 번의 피해 {effectiveValue} 감소"; // 다음 공격 약화 표시
            case BattleStatusEffectType.AttackPowerDown: // 이번 적 턴 공격 약화
                return $"이번 적 턴의 물리·마법 직접 공격 피해 −{effectiveValue}"; // 청동 경보장치의 약화 표시
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
            case BattleStatusEffectType.DuelMark: // 결투 표식 정보
                return $"결투를 건 유닛이 상대에게 주는 직접 카드 피해 +{effectiveValue}%"; // 결투 상태 표시
            case BattleStatusEffectType.ShockAbsorption: // 충격 흡수 상세 확인
                return $"적의 직접 공격 피해 -{effectiveValue}"; // 적 턴에 타격마다 막는 피해
            case BattleStatusEffectType.ResistanceStance: // 저항 태세 상세 확인
                return $"적의 직접 마법 피해 -{effectiveValue}%"; // 추가 마법 피해 경감률
            case BattleStatusEffectType.SwordSaintStance: // 검성의 자세 정보
                return $"고정 비용 검 카드의 행동력 비용 −1, 검 피해 +{effectiveValue}"; // 검 카드 강화 표시
            case BattleStatusEffectType.ActionSpeedDown: // 행동 속도 감소 종류
                return $"적 행동 속도 −{effectiveValue}"; // 속도 감소 표시
            case BattleStatusEffectType.ShieldBalance: // 균형 잡기 종류
                return $"다음 유료 방패 카드의 기본 행동력 비용 −{effectiveValue}"; // 균형 잡기의 표시 문구
            case BattleStatusEffectType.ShieldCounter: // 방패 반격 종류
                return $"방어도가 직접 공격을 흡수하면 공격자에게 고정 피해 {effectiveValue}"; // 방패 반격의 표시 문구
            case BattleStatusEffectType.GuardianCircle: // 수호의 원 종류
                return $"적의 직접 공격 피해 −{effectiveValue}%"; // 수호의 원의 표시 문구
            case BattleStatusEffectType.DangerInterception: // 위험 차단 종류
                return $"적의 다음 직접 공격 피해 −{effectiveValue}%"; // 위험 차단의 표시 문구
            case BattleStatusEffectType.FocusedDefense: // 집중 방어 상태 구분
                return $"다음 적 턴에 적의 직접 피해 −{effectiveValue}%"; // 집중 방어의 실제 화면 문구
            case BattleStatusEffectType.BleedResistance: // 출혈 저항 상태 구분
                return $"적의 출혈 부여에 저항할 확률 +{effectiveValue}%p"; // 출혈 저항의 실제 화면 문구
            case BattleStatusEffectType.UndyingGuard: // 불굴 상태 구분
                return "피해로 체력이 1 이하가 될 때 한 번 버틴다."; // 불굴의 실제 화면 문구
            case BattleStatusEffectType.DamageNullification: // 피해 무효 상태 분기
                return "받을 다음 피해를 한 번 무효로 한다."; // 피해 무효 표시 문구 반환
            case BattleStatusEffectType.DistributedDefense: // 분산 방어 상태 분기
                return $"적의 광역 피해 {effectiveValue}% 감소"; // 분산 방어 표시 문구 반환
            case BattleStatusEffectType.RetributionBulwark: // 응징 방벽 상태 분기
                return $"직접 공격을 맞으면 공격자에게 고정 물리 피해 {effectiveValue}"; // 응징 방벽 표시 문구 반환
            case BattleStatusEffectType.FortifyResistance: // 요새화 저항 상태 분기
                return $"받는 마법 피해 {effectiveValue}% 감소"; // 요새화 저항 표시 문구 반환
            case BattleStatusEffectType.EternalShield: // 영원의 방패 상태 분기
                return $"받는 직접 피해 {effectiveValue}% 감소"; // 영원의 방패 표시 문구 반환
            case BattleStatusEffectType.EternalShieldResistance: // 영원의 방패 저항 상태 분기
                return $"받는 마법 피해 {effectiveValue}% 감소"; // 영원의 방패 저항 표시 문구 반환
            case BattleStatusEffectType.ReflectionCitadel: // 반사의 성채 상태 분기
                return $"다음 적 턴에 잃은 체력을 모아 그 {effectiveValue}%만큼 모든 적에게 반사"; // 반사의 성채 표시 문구 반환
            case BattleStatusEffectType.GuardianHeart: // 수호자의 심장 상태 분기
                return "이 아군이 적의 직접 피해로 쓰러지려 하면 사용자가 한 번 대신 맞고 체력 1 이상으로 생존"; // 수호자의 심장 표시 문구 반환
            case BattleStatusEffectType.DefeatWard: // 전투불능 방지 상태 확인
                return "치명 피해를 한 번 막아 체력 1로 생존"; // 해당 화면의 전투불능 방지 문구
            case BattleStatusEffectType.SanctuaryWard: // 성역 종류 확인
                return $"받는 피해 {effectiveValue}% 감소"; // 게임에 표시되는 성역 문구
            case BattleStatusEffectType.FearResistance: // 공포 저항 종류 확인
                return $"공포 적용을 {effectiveValue}% 확률로 저항"; // 게임에 표시되는 공포 저항 문구
            case BattleStatusEffectType.ResurrectionSeed: // 부활의 씨앗 종류 확인
                return $"쓰러지면 한 번 최대 체력 {effectiveValue}%로 부활"; // 게임에 표시되는 부활의 씨앗 문구
            case BattleStatusEffectType.PainDistribution: // 고통 분산 종류 확인
                return $"적 직접 피해의 {effectiveValue}%를 같은 상태의 생존 아군과 분담"; // 게임에 표시되는 고통 분산 문구
            case BattleStatusEffectType.CorrosionResistanceDown: // 침식 저항 약화 종류 확인
                return $"받는 마법 피해의 저항률 {effectiveValue}%p 감소"; // 게임에 표시되는 침식 저항 약화 문구
            case BattleStatusEffectType.PulseCordAttackPowerUp: // 침식된 맥박 종류 확인
                return $"침식된 맥박줄로 공격력 {effectiveValue} 증가"; // 게임에 표시되는 침식된 맥박 문구
            case BattleStatusEffectType.ArcaneWard: // 마력 보호막 종류 확인
                return $"다음 적 턴까지 마법 저항력 수치 {effectiveValue} 증가"; // 게임에 표시되는 마력 보호막 문구
            case BattleStatusEffectType.MagicAttackPowerUp: // 마력 증폭 종류 확인
                return $"마법 카드 공격력 {effectiveValue} 증가"; // 게임에 표시되는 마력 증폭 문구
            case BattleStatusEffectType.WandEcho: // 마법 반향 상태 확인
                return $"이번 아군 턴 다음 지팡이 마법 공격 복제, 복제 피해 {effectiveValue}%"; // 실제 전투와 도감의 상태 문구
            case BattleStatusEffectType.ConfessionDamageMark: // 고해의 표식 종류 확인
                return $"다음 유효한 직접 카드 명중 피해 {effectiveValue} 증가"; // 전투와 도감의 고해 피해 표시
            case BattleStatusEffectType.IncomingMagicDamageUp: // 새 지팡이 약화 상태 확인
                return $"받는 마법 피해 {effectiveValue}% 증가"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.EvasionDown: // 새 지팡이 약화 상태 확인
                return $"현재 회피율 {effectiveValue}%p 감소"; // 전투와 도감에 표시할 상태 문구
            case BattleStatusEffectType.ManaConcentration: // 마력 집중 상태 확인
                return $"다음으로 사용하는 지팡이 피해 카드의 각 명중 피해 {effectiveValue} 증가"; // 마력 집중의 실제 전투 화면 문구
            case BattleStatusEffectType.StealthBreakDamageUp: // 팔찌의 실제 다음 명중 강화 확인
                return $"은신이 풀린 뒤 다음 직접 카드 명중 피해 {effectiveValue} 증가"; // 전투와 도감에서 조회할 은신 해제 표시
            case BattleStatusEffectType.Shock: // 감전 종류 확인
                return $"남은 적 턴마다 턴 시작에 감전 피해 {effectiveValue}"; // 게임에 표시되는 감전 문구
            case BattleStatusEffectType.StethoscopeMark: // 청진기 표식 확인
                return "아군 턴 두 번 동안 청진기가 선택한 적에게 적용하는 표식"; // 전투 화면과 도감의 상태 안내
            case BattleStatusEffectType.CrystalMarkDefenseDown: // 표식 방어 약화 종류 확인
                return $"표식에 의해 물리 방어력 {effectiveValue} 감소"; // 게임에 표시되는 표식 방어 약화 문구
            case BattleStatusEffectType.MiracleEcho: // 기적의 잔향 종류 확인
                return $"이번 아군 턴의 직접 카드 체력 회복량 {effectiveValue} 증가"; // 게임에 표시되는 기적의 잔향 문구
            case BattleStatusEffectType.Stealth: // 은신 상태 확인
                return "은신 중. 장착한 유물에 따라 회피율이 증가합니다."; // 현재 화면의 은신 설명
            case BattleStatusEffectType.DelayedHealing: // 다음 턴 회복 종류 확인
                return $"다음 아군 턴 시작에 체력 {effectiveValue} 회복"; // 게임에 표시되는 다음 턴 회복 문구
            case BattleStatusEffectType.HealingReceivedUp: // 받는 회복량 증가 종류 확인
                return $"받는 체력 회복량 {effectiveValue}% 증가"; // 게임에 표시되는 받는 회복량 증가 설명
            case BattleStatusEffectType.Renewal: // 재생 종류 확인
                return $"아군 턴 종료마다 체력 {effectiveValue} 회복"; // 게임에 표시되는 재생 설명
            case BattleStatusEffectType.ActionSpeedUp: // 행동 속도 증가 종류
                return $"행동 속도 +{effectiveValue}. 첫 적 턴 종료에 해제."; // 증가량과 해제 시점
            case BattleStatusEffectType.Vulnerable: // 방어도 적용 전 받는 피해 증가
                return $"방어도를 적용할 수 있는 받는 원본 피해 +{effectiveValue}%"; // 상태 화면의 피해 증가 설명
            case BattleStatusEffectType.PublicMark: // 수신기의 공명 표식
                return "첫 아군 턴 종료까지 유지. 표식용 분필의 직접 카드 피해 증가 대상"; // 공명 표식 표시
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
