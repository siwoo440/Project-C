using System; // 이벤트 연결 해제 규약 사용
using System.Collections.Generic; // 아군과 전용 카드 소유자 목록 사용
public sealed class WorldCoordinationRuntime : IDisposable // 세계의 동료 전용 카드 연계 관리
{ // 연계 관리자 시작
    private readonly BattleTurnRuntime turns; // 현재 전투 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allies; // 실제 전투 아군 목록
    private readonly HashSet<string> usedOwners = new HashSet<string>(); // 이번 턴 전용 카드를 사용한 동료 식별자
    private bool disposed; // 관리자 종료 여부
    public bool IsReady => !disposed && turns.IsPlayerTurn && usedOwners.Count >= 2; // 서로 다른 동료 두 명의 전용 카드 사용 확인
    public WorldCoordinationRuntime(BattleTurnRuntime battleTurns, IReadOnlyList<BattleUnitRuntime> battleAllies) // 세계 연계 관리자 생성
    { // 관리자 생성 시작
        turns = battleTurns ?? throw new ArgumentNullException(nameof(battleTurns)); // 전투 턴 관리자 저장
        allies = battleAllies ?? throw new ArgumentNullException(nameof(battleAllies)); // 전투 아군 목록 저장
        turns.PhaseEnded += HandlePhaseEnded; // 턴 종료 시 기록 초기화 연결
    } // 관리자 생성 종료
    public void RecordCardUsed(CardInstance card) // 이번 턴 실제 전용 카드 사용 기록
    { // 전용 카드 기록 시작
        BattleUnitRuntime owner = card == null ? null : card.OwnerUnit; // 카드 소유자 조회
        string ownerId = owner == null || owner.CharacterSource == null ? null : owner.CharacterSource.CharacterId; // 소유자 캐릭터 식별자 조회
        if (disposed || !turns.IsPlayerTurn || owner == null || owner.IsDead || owner.Team != BattleTeam.Ally || string.IsNullOrEmpty(ownerId) || ownerId == "ARC_21" || card.SourceData == null || string.IsNullOrEmpty(card.SourceData.CardId) || !card.SourceData.CardId.StartsWith("CRD_" + ownerId + "_", StringComparison.Ordinal)) // 생존 동료의 본인 전용 카드 확인
        { // 기록 불가 처리 시작
            return; // 카드 사용 기록 생략
        } // 기록 불가 처리 종료
        foreach (BattleUnitRuntime ally in allies) // 전투 아군 목록 순회
        { // 실제 참가 동료 확인 시작
            if (ally == owner) // 실제 참가한 소유자 확인
            { // 사용 기록 시작
                usedOwners.Add(ownerId); // 서로 다른 소유자 식별자 기록
                return; // 카드 기록 종료
            } // 사용 기록 종료
        } // 실제 참가 동료 확인 종료
    } // 전용 카드 기록 종료
    public BattleUnitRuntime FindAdditionalTarget(BattleUnitRuntime owner, BattleUnitRuntime primary) // 체력 비율이 가장 낮은 다른 생존 아군 선택
    { // 추가 대상 검색 시작
        if (!IsReady || owner == null || owner.IsDead || owner.CharacterSource == null || owner.CharacterSource.CharacterId != "ARC_21" || primary == null || primary.IsDead || primary.Team != BattleTeam.Ally) // 세계 카드와 기본 대상 확인
        { // 추가 대상 불가 시작
            return null; // 추가 대상 없음 반환
        } // 추가 대상 불가 종료
        BattleUnitRuntime selected = null; // 추가 대상 초기화
        foreach (BattleUnitRuntime ally in allies) // 실제 아군 목록 순회
        { // 추가 대상 비교 시작
            if (ally == null || ally == primary || ally.IsDead || ally.Team != BattleTeam.Ally) // 기본 대상과 사망 유닛 제외
            { // 제외 대상 처리 시작
                continue; // 다음 아군 확인
            } // 제외 대상 처리 종료
            if (selected == null || (long)ally.CurrentHealth * selected.MaxHealth < (long)selected.CurrentHealth * ally.MaxHealth) // 현재 최저 체력 비율 비교
            { // 최저 대상 교체 시작
                selected = ally; // 낮은 체력 비율 아군 기록
            } // 최저 대상 교체 종료
        } // 추가 대상 비교 종료
        return selected; // 실제 추가 대상 반환
    } // 추가 대상 검색 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 플레이어 턴 종료 처리
    { // 턴 종료 처리 시작
        if (phase == BattleTurnPhase.PlayerTurn) // 아군 턴 종료 확인
        { // 연계 기록 초기화 시작
            usedOwners.Clear(); // 이전 턴 전용 카드 기록 제거
        } // 연계 기록 초기화 종료
    } // 턴 종료 처리 종료
    public void Dispose() // 세계 연계 관리자 정리
    { // 관리자 정리 시작
        if (disposed) // 중복 정리 확인
        { // 중복 정리 처리 시작
            return; // 정리 중단
        } // 중복 정리 처리 종료
        disposed = true; // 관리자 종료 표시
        turns.PhaseEnded -= HandlePhaseEnded; // 턴 종료 이벤트 연결 해제
        usedOwners.Clear(); // 남은 사용 기록 제거
    } // 관리자 정리 종료
} // 연계 관리자 종료
