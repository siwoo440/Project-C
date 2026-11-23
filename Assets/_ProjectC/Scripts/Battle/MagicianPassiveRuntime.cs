using System; // 이벤트 해제 기능 사용
using System.Collections.Generic; // 아군 목록 사용
public sealed class MagicianPassiveRuntime : IDisposable // 마법사 임시 수리 패시브
{ // 패시브 시작
    private readonly BattleDeckRuntime deck; // 전투 카드 더미
    private readonly BattleTurnRuntime turns; // 전투 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allies; // 아군 유닛 목록
    private bool triggeredThisTurn; // 이번 턴 발동 여부
    private bool disposed; // 이벤트 해제 여부
    public MagicianPassiveRuntime(BattleDeckRuntime battleDeck, BattleTurnRuntime battleTurns, IReadOnlyList<BattleUnitRuntime> allyUnits) // 패시브 연결
    { // 패시브 연결 시작
        deck = battleDeck ?? throw new ArgumentNullException(nameof(battleDeck)); // 전투 덱 저장
        turns = battleTurns ?? throw new ArgumentNullException(nameof(battleTurns)); // 턴 관리자 저장
        allies = allyUnits ?? throw new ArgumentNullException(nameof(allyUnits)); // 아군 목록 저장
        deck.CardRecovered += HandleCardRecovered; // 카드 회수 이벤트 연결
        turns.PhaseStarted += HandlePhaseStarted; // 턴 시작 이벤트 연결
        turns.PhaseEnded += HandlePhaseEnded; // 턴 종료 이벤트 연결
    } // 패시브 연결 종료
    private void HandleCardRecovered(CardInstance card) // 카드 회수 처리
    { // 카드 회수 처리 시작
        if (disposed || triggeredThisTurn || !turns.IsPlayerTurn || card == null || !HasLivingMagician()) // 발동 조건 확인
        { // 발동 불가 처리 시작
            return; // 패시브 처리 중단
        } // 발동 불가 처리 종료
        triggeredThisTurn = true; // 이번 턴 발동 기록
        card.ApplyTemporaryApDiscount(1); // 회수 카드의 이번 턴 행동력 일 감소
    } // 카드 회수 처리 종료
    private bool HasLivingMagician() // 생존 마법사 확인
    { // 마법사 확인 시작
        foreach (BattleUnitRuntime ally in allies) // 아군 유닛 순회
        { // 마법사 검사 시작
            if (ally != null && !ally.IsDead && ally.CharacterSource != null && ally.CharacterSource.CharacterId == "ARC_01") // 생존 마법사 일치 확인
            { // 마법사 발견 시작
                return true; // 패시브 보유 반환
            } // 마법사 발견 종료
        } // 마법사 검사 종료
        return false; // 생존 마법사 없음 반환
    } // 마법사 확인 종료
    private void HandlePhaseStarted(BattleTurnPhase phase, int round) // 턴 시작 처리
    { // 턴 시작 처리 시작
        if (phase == BattleTurnPhase.PlayerTurn) // 플레이어 턴 확인
        { // 발동 횟수 초기화 시작
            triggeredThisTurn = false; // 새 턴 발동 가능 상태 저장
        } // 발동 횟수 초기화 종료
    } // 턴 시작 처리 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 턴 종료 처리
    { // 턴 종료 처리 시작
        if (phase == BattleTurnPhase.PlayerTurn) // 플레이어 턴 확인
        { // 임시 비용 정리 시작
            deck.ClearTemporaryCardCosts(); // 이번 턴 카드 할인 제거
        } // 임시 비용 정리 종료
    } // 턴 종료 처리 종료
    public void Dispose() // 패시브 연결 해제
    { // 연결 해제 시작
        if (disposed) // 중복 해제 확인
        { // 중복 처리 시작
            return; // 연결 해제 중단
        } // 중복 처리 종료
        disposed = true; // 연결 해제 상태 저장
        deck.CardRecovered -= HandleCardRecovered; // 카드 회수 이벤트 해제
        turns.PhaseStarted -= HandlePhaseStarted; // 턴 시작 이벤트 해제
        turns.PhaseEnded -= HandlePhaseEnded; // 턴 종료 이벤트 해제
        deck.ClearTemporaryCardCosts(); // 남은 임시 카드 할인 제거
    } // 연결 해제 종료
} // 패시브 종료
