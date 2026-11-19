internal sealed class BattleDuelRuntime // 시전자와 적 한 명의 결투 연결
{ // 결투 연결 시작
    private const BattleStatusEffectType Mark = BattleStatusEffectType.DuelMark; // 양쪽에 적용하는 상태 종류
    private readonly BattleUnitRuntime source; // 결투를 신청한 아군
    private readonly BattleUnitRuntime target; // 결투 대상으로 지정한 적
    private readonly BattleStatusEffectInstance sourceMark; // 아군의 실제 상태
    private readonly BattleStatusEffectInstance targetMark; // 적의 실제 상태

    private BattleDuelRuntime(BattleUnitRuntime source, BattleUnitRuntime target, BattleStatusEffectInstance sourceMark, BattleStatusEffectInstance targetMark) // 새 결투에 양쪽 참가자 연결
    { // 참가자 연결 시작
        this.source = source; // 신청 아군 보관
        this.target = target; // 지정 적 보관
        this.sourceMark = sourceMark; // 신청 아군의 표식 보관
        this.targetMark = targetMark; // 지정 적의 표식 보관
        source.ActiveDuel = this; // 신청 아군의 활성 결투 지정
        target.ActiveDuel = this; // 지정 적의 활성 결투 지정
        source.StatusEffectsChanged += OnStatusChanged; // 신청 아군의 정화 감지
        target.StatusEffectsChanged += OnStatusChanged; // 지정 적의 정화 감지
        source.Died += OnParticipantDied; // 신청 아군의 전투불능 감지
        target.Died += OnParticipantDied; // 지정 적의 전투불능 감지
    } // 참가자 연결 종료

    internal static BattleStatusEffectApplyResult Start(BattleUnitRuntime source, BattleUnitRuntime target, int value, int turns) // 현재 참가자의 결투 개시
    { // 결투 개시 시작
        if (source == null || target == null || source.IsDead || target.IsDead || source.Team != BattleTeam.Ally || target.Team != BattleTeam.Enemy || value <= 0 || turns <= 0) // 생존한 서로 다른 진영과 유효한 규칙 확인
        { // 결투 불가 처리 시작
            return BattleStatusEffectApplyResult.Invalid; // 잘못된 대상 또는 수치 반환
        } // 결투 불가 처리 종료
        if (source.HasStatusImmunity || target.HasStatusImmunity) // 양쪽 중 한 명의 상태 면역 확인
        { // 면역 처리 시작
            return BattleStatusEffectApplyResult.BlockedByImmunity; // 현재 결투를 유지하고 새 결투 차단
        } // 면역 처리 종료
        source.ActiveDuel?.End(); // 신청 아군의 이전 결투 종료
        target.ActiveDuel?.End(); // 지정 적의 이전 결투 종료
        source.ApplyStatusEffect(Mark, value, turns, 1, source); // 신청 아군의 표식 적용
        target.ApplyStatusEffect(Mark, value, turns, 1, source); // 지정 적의 표식 적용
        var sourceMark = source.GetStatusEffect(Mark); // 실제 적용된 아군 표식 확인
        var targetMark = target.GetStatusEffect(Mark); // 실제 적용된 적 표식 확인
        if (sourceMark == null || targetMark == null || source.IsDead || target.IsDead) // 적용 중 상태 제거 또는 전투불능 확인
        { // 한쪽 표식 정리 시작
            source.RemoveStatusEffect(Mark); // 남은 아군 표식 제거
            target.RemoveStatusEffect(Mark); // 남은 적 표식 제거
            return BattleStatusEffectApplyResult.Invalid; // 두 참가자의 결투 미성립
        } // 한쪽 표식 정리 종료
        _ = new BattleDuelRuntime(source, target, sourceMark, targetMark); // 두 상태의 동일한 결투 연결
        return BattleStatusEffectApplyResult.Applied; // 두 참가자의 표식 적용 알림
    } // 결투 개시 종료

    internal int GetDamageBonus(BattleUnitRuntime attacker, BattleUnitRuntime defender) // 지정한 공격자와 대상의 보너스 조회
    { // 피해 보너스 조회 시작
        return attacker == source && defender == target && !source.IsDead && !target.IsDead ? sourceMark.EffectiveValue : 0; // 결투 신청자가 지정 대상에게 입히는 피해만 증가
    } // 피해 보너스 조회 종료

    internal void AdvancePlayerTurn() // 아군 턴 종료에 양쪽 표식 지속 시간 감소
    { // 표식 지속 처리 시작
        sourceMark.AdvanceDuration(); // 아군 표식 지속 시간 감소
        targetMark.AdvanceDuration(); // 적 표식 지속 시간 감소
        if (sourceMark.IsExpired) // 같은 시간에 적용한 양쪽 표식 만료 확인
        { // 결투 만료 처리 시작
            End(); // 양쪽 표식과 결투 연결 제거
            return; // 만료한 표식의 갱신 생략
        } // 결투 만료 처리 종료
        source.NotifyStatusEffectsChanged(); // 아군 화면의 남은 턴 갱신
        target.NotifyStatusEffectsChanged(); // 적 화면의 남은 턴 갱신
    } // 표식 지속 처리 종료

    private void OnStatusChanged(BattleUnitRuntime unit) // 참가자 정화 또는 표식 해제 감지
    { // 표식 해제 확인 시작
        var mark = unit == source ? sourceMark : targetMark; // 변경된 참가자의 원래 표식
        if (!unit.ContainsStatusEffectInstance(mark)) // 원래 표식의 제거 여부 확인
        { // 연결 해제 시작
            End(); // 다른 참가자에게 남아 있는 표식 정리
        } // 연결 해제 종료
    } // 표식 해제 확인 종료

    private void OnParticipantDied(BattleUnitRuntime unit) // 참가자 전투불능 감지
    { // 참가자 전투불능 처리 시작
        End(); // 양쪽의 표식과 결투 관계 종료
    } // 참가자 전투불능 처리 종료

    private void End() // 현재 결투에 연결된 양쪽 상태 해제
    { // 양쪽 결투 해제 시작
        if (source.ActiveDuel != this) // 이미 종료한 연결인지 확인
        { // 중복 결투 해제 방지 시작
            return; // 같은 결투의 재처리 생략
        } // 중복 결투 해제 방지 종료
        source.ActiveDuel = null; // 아군 결투 참조 제거
        target.ActiveDuel = null; // 적 결투 참조 제거
        source.StatusEffectsChanged -= OnStatusChanged; // 아군 정화 감지 해제
        target.StatusEffectsChanged -= OnStatusChanged; // 적 정화 감지 해제
        source.Died -= OnParticipantDied; // 아군 전투불능 감지 해제
        target.Died -= OnParticipantDied; // 적 전투불능 감지 해제
        source.RemoveStatusEffect(Mark); // 신청 아군 표식 해제
        target.RemoveStatusEffect(Mark); // 지정 대상 표식 해제
    } // 양쪽 결투 해제 종료
} // 결투 연결 종료
