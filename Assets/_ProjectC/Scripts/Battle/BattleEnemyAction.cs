using System; // 기본 예외 기능 사용
public sealed class BattleEnemyAction // 적 예정 행동 정보
{ // 클래스 시작
    public BattleUnitRuntime Actor { get; } // 행동 적 조회
    public BattleUnitRuntime Target { get; private set; } // 대상 아군 조회
    public EnemyActionType ActionType { get; } // 행동 종류 조회
    public BattleDamageType DamageType { get; } // 피해 유형 조회
    public int Amount { get; } // 예정 수치 조회
    public int ActionSpeed { get; } // 결정된 행동 속도 조회
    public int CreationOrder { get; } // 행동 생성 순서 조회
    public int ActionOrder { get; private set; } // 최종 행동 순번 조회
    public bool IsExecutable => Actor != null && !Actor.IsDead && Target != null && !Target.IsDead && ActionType == EnemyActionType.Attack && Amount > 0; // 실행 가능 여부 조회
    public BattleEnemyAction(BattleUnitRuntime actor, BattleUnitRuntime target, EnemyActionType actionType, BattleDamageType damageType, int amount, int actionSpeed, int creationOrder) // 예정 행동 생성
    { // 생성자 시작
        Actor = actor ?? throw new ArgumentNullException(nameof(actor)); // 행동 적 저장
        Target = target ?? throw new ArgumentNullException(nameof(target)); // 대상 아군 저장
        ActionType = actionType; // 행동 종류 저장
        DamageType = damageType; // 피해 유형 저장
        Amount = amount; // 예정 수치 저장
        ActionSpeed = Math.Max(1, actionSpeed); // 보정된 행동 속도 저장
        CreationOrder = Math.Max(0, creationOrder); // 보정된 생성 순서 저장
    } // 생성자 종료
    public void SetActionOrder(int actionOrder) // 최종 행동 순번 지정
    { // 순번 지정 시작
        ActionOrder = Math.Max(1, actionOrder); // 보정된 행동 순번 저장
    } // 순번 지정 종료
    public bool ChangeTarget(BattleUnitRuntime target) // 대상 변경
    { // 대상 변경 시작
        if (target == null || target.IsDead || target.Team != BattleTeam.Ally) // 대상 유효성 확인
        { // 잘못된 대상 처리 시작
            return false; // 대상 변경 실패 반환
        } // 잘못된 대상 처리 종료
        Target = target; // 새 대상 저장
        return true; // 대상 변경 성공 반환
    } // 대상 변경 종료
    public BattleDamageResult PreviewDamage() // 예정 피해 계산
    { // 예정 피해 계산 시작
        if (Target == null || Target.IsDead) // 대상 유효성 확인
        { // 대상 없음 처리 시작
            return BattleDamageResult.Empty(DamageType); // 피해 없음 결과 반환
        } // 대상 없음 처리 종료
        return Target.PreviewDamage(Amount, DamageType); // 대상 방어력 포함 예상 피해 반환
    } // 예정 피해 계산 종료
    public BattleDamageResult Execute() // 예정 행동 실행
    { // 행동 실행 시작
        if (!IsExecutable) // 실행 가능 여부 확인
        { // 실행 불가 처리 시작
            return BattleDamageResult.Empty(DamageType); // 적용 수치 없음 반환
        } // 실행 불가 처리 종료
        return Target.TakeDamage(Amount, DamageType); // 대상 피해 적용 결과 반환
    } // 행동 실행 종료
} // 클래스 종료
