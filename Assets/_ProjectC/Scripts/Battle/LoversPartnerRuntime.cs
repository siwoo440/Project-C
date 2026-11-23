using System; // 이벤트 기능 사용
using System.Collections.Generic; // 아군 목록 사용
public sealed class LoversPartnerRuntime : IDisposable // 연인 파트너와 턴별 기록 관리
{ // 파트너 관리 시작
    private readonly BattleTurnRuntime turns; // 전투 턴 관리자
    private readonly IReadOnlyList<BattleUnitRuntime> allies; // 전투 아군 목록
    private bool disposed; // 연결 해제 여부
    public BattleUnitRuntime Owner { get; private set; } // 연인 유닛 조회
    public BattleUnitRuntime Partner { get; private set; } // 현재 파트너 조회
    public bool PartnerUsedCardThisTurn { get; private set; } // 현재 턴 파트너 카드 사용 여부
    public event Action Changed; // 파트너 상태 변경 알림
    public LoversPartnerRuntime(BattleTurnRuntime battleTurns, IReadOnlyList<BattleUnitRuntime> allyUnits) // 파트너 관리자 생성
    { // 관리자 생성 시작
        turns = battleTurns ?? throw new ArgumentNullException(nameof(battleTurns)); // 턴 관리자 저장
        allies = allyUnits ?? throw new ArgumentNullException(nameof(allyUnits)); // 아군 목록 저장
        turns.PhaseEnded += HandlePhaseEnded; // 턴 종료 이벤트 연결
    } // 관리자 생성 종료
    public bool CanSetPartner(BattleUnitRuntime owner, BattleUnitRuntime target) // 파트너 지정 가능 확인
    { // 지정 가능 확인 시작
        return !disposed && owner != null && !owner.IsDead && owner.CharacterSource != null && owner.CharacterSource.CharacterId == "ARC_06" && target != null && !target.IsDead && target != owner && target.Team == BattleTeam.Ally && Contains(owner) && Contains(target); // 생존한 다른 아군 여부 반환
    } // 지정 가능 확인 종료
    public bool TrySetPartner(BattleUnitRuntime owner, BattleUnitRuntime target) // 파트너 지정 또는 변경
    { // 파트너 지정 시작
        if (!CanSetPartner(owner, target)) // 지정 가능 조건 확인
        { // 지정 불가 처리 시작
            return false; // 지정 실패 반환
        } // 지정 불가 처리 종료
        DetachUnits(); // 이전 사망 이벤트 연결 해제
        Owner = owner; // 연인 유닛 저장
        Partner = target; // 새 파트너 저장
        PartnerUsedCardThisTurn = false; // 이전 파트너 사용 기록 제거
        Owner.Died += HandleUnitDied; // 연인 사망 이벤트 연결
        Partner.Died += HandleUnitDied; // 파트너 사망 이벤트 연결
        Changed?.Invoke(); // 파트너 변경 알림
        return true; // 지정 성공 반환
    } // 파트너 지정 종료
    public void RecordCardUsed(CardInstance card) // 파트너 카드 사용 기록
    { // 카드 기록 시작
        if (disposed || !turns.IsPlayerTurn || Owner == null || Owner.IsDead || Partner == null || Partner.IsDead || card == null || card.OwnerUnit != Partner || PartnerUsedCardThisTurn) // 기록 가능 조건 확인
        { // 기록 불가 처리 시작
            return; // 카드 기록 중단
        } // 기록 불가 처리 종료
        PartnerUsedCardThisTurn = true; // 현재 턴 사용 기록 저장
        Changed?.Invoke(); // 연계 상태 변경 알림
    } // 카드 기록 종료
    private bool Contains(BattleUnitRuntime unit) // 아군 목록 포함 확인
    { // 아군 포함 확인 시작
        foreach (BattleUnitRuntime ally in allies) // 아군 목록 순회
        { // 아군 비교 시작
            if (ally == unit) // 동일 유닛 확인
            { // 일치 유닛 처리 시작
                return true; // 아군 포함 반환
            } // 일치 유닛 처리 종료
        } // 아군 비교 종료
        return false; // 아군 미포함 반환
    } // 아군 포함 확인 종료
    private void HandlePhaseEnded(BattleTurnPhase phase, int round) // 턴 종료 기록 정리
    { // 턴 종료 처리 시작
        if (phase == BattleTurnPhase.PlayerTurn && PartnerUsedCardThisTurn) // 플레이어 턴 사용 기록 확인
        { // 사용 기록 정리 시작
            PartnerUsedCardThisTurn = false; // 이전 턴 기록 초기화
            Changed?.Invoke(); // 연계 상태 변경 알림
        } // 사용 기록 정리 종료
    } // 턴 종료 처리 종료
    private void HandleUnitDied(BattleUnitRuntime unit) // 연결 유닛 사망 처리
    { // 사망 처리 시작
        DetachUnits(); // 사망 이벤트 연결 해제
        Owner = null; // 연인 참조 제거
        Partner = null; // 파트너 참조 제거
        PartnerUsedCardThisTurn = false; // 카드 사용 기록 제거
        Changed?.Invoke(); // 파트너 해제 알림
    } // 사망 처리 종료
    private void DetachUnits() // 연결된 사망 이벤트 해제
    { // 이벤트 해제 시작
        if (Owner != null) // 기존 연인 확인
        { // 연인 해제 시작
            Owner.Died -= HandleUnitDied; // 연인 사망 이벤트 해제
        } // 연인 해제 종료
        if (Partner != null) // 기존 파트너 확인
        { // 파트너 해제 시작
            Partner.Died -= HandleUnitDied; // 파트너 사망 이벤트 해제
        } // 파트너 해제 종료
    } // 이벤트 해제 종료
    public void Dispose() // 파트너 관리자 종료
    { // 관리자 종료 시작
        if (disposed) // 중복 종료 확인
        { // 중복 종료 처리 시작
            return; // 종료 처리 중단
        } // 중복 종료 처리 종료
        disposed = true; // 종료 상태 저장
        turns.PhaseEnded -= HandlePhaseEnded; // 턴 종료 이벤트 해제
        DetachUnits(); // 사망 이벤트 연결 해제
        Owner = null; // 연인 참조 제거
        Partner = null; // 파트너 참조 제거
        PartnerUsedCardThisTurn = false; // 카드 사용 기록 제거
    } // 관리자 종료 종료
} // 파트너 관리 종료
