using System; // 이벤트 기능 사용
using System.Collections.Generic; // 적 목록과 공격 기록 사용
public sealed class ChariotConcentrationRuntime : IDisposable // 전차의 턴별 집중 공격 관리
{ // 집중 공격 관리 시작
    private readonly BattleTurnRuntime turns; // 전투 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> enemies; // 전투 적 목록
    private readonly Dictionary<BattleUnitRuntime, Dictionary<BattleUnitRuntime, int>> attackCounts = new Dictionary<BattleUnitRuntime, Dictionary<BattleUnitRuntime, int>>(); // 전차별 대상 공격 횟수
    private readonly HashSet<BattleUnitRuntime> passiveTriggered = new HashSet<BattleUnitRuntime>(); // 이번 턴 패시브 발동 전차
    private bool disposed; // 연결 해제 여부
    public ChariotConcentrationRuntime(BattleTurnRuntime battleTurns, IReadOnlyList<BattleUnitRuntime> enemyUnits) // 집중 공격 관리자 생성
    { // 관리자 생성 시작
        turns = battleTurns ?? throw new ArgumentNullException(nameof(battleTurns)); // 턴 관리자 저장
        enemies = enemyUnits ?? throw new ArgumentNullException(nameof(enemyUnits)); // 적 목록 저장
        turns.PhaseEnded += HandlePhaseEnded; // 턴 종료 이벤트 연결
    } // 관리자 생성 종료
    public void RecordAttack(CardInstance card, BattleUnitRuntime target) // 전차의 카드 공격 기록
    { // 공격 기록 시작
        if (disposed || !turns.IsPlayerTurn || card == null || card.EffectType != CardEffectType.Damage || card.OwnerUnit == null || card.OwnerUnit.IsDead || card.OwnerUnit.CharacterSource == null || card.OwnerUnit.CharacterSource.CharacterId != "ARC_07" || target == null || target.Team != BattleTeam.Enemy) // 유효 전차 공격 확인
        { // 기록 불가 처리 시작
            return; // 공격 기록 중단
        } // 기록 불가 처리 종료
        foreach (BattleUnitRuntime enemy in enemies) // 전투 적 목록 순회
        { // 이전 대상 표식 정리 시작
            if (enemy != null && enemy != target) // 다른 적 확인
            { // 이전 표식 제거 시작
                enemy.RemoveStatusEffect(BattleStatusEffectType.ChariotMark); // 다른 적의 돌파 표식 제거
            } // 이전 표식 제거 종료
        } // 이전 대상 표식 정리 종료
        if (target.IsDead) // 공격 대상 생존 확인
        { // 사망 대상 처리 시작
            return; // 표식 적용 중단
        } // 사망 대상 처리 종료
        BattleUnitRuntime owner = card.OwnerUnit; // 전차 소유자 조회
        if (!attackCounts.TryGetValue(owner, out Dictionary<BattleUnitRuntime, int> counts)) // 소유자 공격 기록 확인
        { // 새 공격 기록 생성 시작
            counts = new Dictionary<BattleUnitRuntime, int>(); // 대상별 공격 기록 생성
            attackCounts.Add(owner, counts); // 소유자 기록 등록
        } // 새 공격 기록 생성 종료
        counts.TryGetValue(target, out int previousCount); // 해당 대상 기존 공격 횟수 조회
        int nextCount = previousCount + 1; // 새 공격 횟수 계산
        counts[target] = nextCount; // 해당 대상 공격 횟수 저장
        if (card.StatusEffectType == BattleStatusEffectType.ChariotMark) // 전용 표식 카드 확인
        { // 전용 표식 적용 시작
            target.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, int.MaxValue, owner); // 전용 카드 표식 한 개 적용
        } // 전용 표식 적용 종료
        if (nextCount == 2 && passiveTriggered.Add(owner)) // 같은 적 두 번째 공격과 턴당 최초 발동 확인
        { // 집중 패시브 적용 시작
            target.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, int.MaxValue, owner); // 패시브 표식 한 개 적용
        } // 집중 패시브 적용 종료
    } // 공격 기록 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 턴 종료 표식 정리
    { // 턴 종료 처리 시작
        if (phase != BattleTurnPhase.PlayerTurn) // 플레이어 턴 종료 확인
        { // 다른 턴 처리 시작
            return; // 표식 정리 중단
        } // 다른 턴 처리 종료
        foreach (BattleUnitRuntime enemy in enemies) // 전투 적 목록 순회
        { // 개별 표식 정리 시작
            enemy?.RemoveStatusEffect(BattleStatusEffectType.ChariotMark); // 해당 턴의 돌파 표식 제거
        } // 개별 표식 정리 종료
        attackCounts.Clear(); // 대상별 공격 횟수 초기화
        passiveTriggered.Clear(); // 턴당 패시브 발동 기록 초기화
    } // 턴 종료 처리 종료
    public void Dispose() // 집중 공격 관리자 종료
    { // 관리자 종료 시작
        if (disposed) // 중복 종료 확인
        { // 중복 종료 처리 시작
            return; // 종료 처리 중단
        } // 중복 종료 처리 종료
        disposed = true; // 종료 상태 저장
        turns.PhaseEnded -= HandlePhaseEnded; // 턴 종료 이벤트 해제
        attackCounts.Clear(); // 대상별 공격 기록 제거
        passiveTriggered.Clear(); // 패시브 발동 기록 제거
    } // 관리자 종료 종료
} // 집중 공격 관리 종료
