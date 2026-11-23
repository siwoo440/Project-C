using System; // 빈 목록 기능 사용
using System.Collections.Generic; // 읽기 전용 목록 사용
public sealed class BattleEventContext // 전투 공용 이벤트 정보
{ // 클래스 시작
    public long Sequence { get; private set; } // 전투 내 발행 순번
    public BattleEventType EventType { get; } // 이벤트 종류
    public int Round { get; } // 발생 라운드
    public BattleTurnPhase Phase { get; } // 발생 턴 단계
    public BattleUnitRuntime SourceUnit { get; } // 행동 발생 유닛
    public BattleUnitRuntime TargetUnit { get; } // 대표 대상 유닛
    public IReadOnlyList<BattleUnitRuntime> TargetUnits { get; } // 전체 대상 유닛 목록
    public CardInstance Card { get; } // 사용 카드
    public int AppliedAmount { get; } // 실제 적용 수치
    public BattleDamageResult? DamageResult { get; } // 피해 상세 결과
    public BattleStatusEffectType StatusEffectType { get; } // 상태 이상 종류
    public BattleStatusEffectApplyResult StatusApplyResult { get; } // 상태 이상 적용 결과
    public BattleMentalChangeResult MentalResult { get; } // 정신력 변화 결과
    public BattleResult Result { get; } // 전투 종료 결과
    public BattleEventContext(BattleEventType eventType, int round, BattleTurnPhase phase, BattleUnitRuntime sourceUnit = null, BattleUnitRuntime targetUnit = null, IReadOnlyList<BattleUnitRuntime> targetUnits = null, CardInstance card = null, int appliedAmount = 0, BattleDamageResult? damageResult = null, BattleStatusEffectType statusEffectType = BattleStatusEffectType.None, BattleStatusEffectApplyResult statusApplyResult = BattleStatusEffectApplyResult.Invalid, BattleMentalChangeResult mentalResult = null, BattleResult result = BattleResult.None) // 이벤트 정보 생성자
    { // 생성자 시작
        EventType = eventType; // 이벤트 종류 저장
        Round = Math.Max(0, round); // 음수 없는 라운드 저장
        Phase = phase; // 턴 단계 저장
        SourceUnit = sourceUnit; // 발생 유닛 저장
        TargetUnit = targetUnit; // 대표 대상 저장
        TargetUnits = targetUnits == null ? Array.Empty<BattleUnitRuntime>() : new List<BattleUnitRuntime>(targetUnits); // 변경 방지 전체 대상 목록 복사
        Card = card; // 사용 카드 저장
        AppliedAmount = appliedAmount; // 실제 적용 수치 저장
        DamageResult = damageResult; // 피해 상세 결과 저장
        StatusEffectType = statusEffectType; // 상태 이상 종류 저장
        StatusApplyResult = statusApplyResult; // 상태 적용 결과 저장
        MentalResult = mentalResult; // 정신력 변화 결과 저장
        Result = result; // 전투 결과 저장
    } // 생성자 종료
    internal bool AssignSequence(long sequence) // 발행 순번 지정
    { // 순번 지정 시작
        if (Sequence > 0 || sequence <= 0) // 기존 순번과 입력 범위 확인
        { // 지정 불가 처리 시작
            return false; // 순번 지정 실패 반환
        } // 지정 불가 처리 종료
        Sequence = sequence; // 발행 순번 저장
        return true; // 순번 지정 성공 반환
    } // 순번 지정 종료
} // 클래스 종료
