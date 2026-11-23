using System; // 연결 해제 규약 사용
using System.Collections.Generic; // 대상과 발동 기록 사용
public sealed class StarRecoveryPassiveRuntime : IDisposable // 별의 턴당 추가 예약 회복 관리
{ // 별 패시브 관리 시작
    private const int ReservedHealing = 2; // 임시 추가 예약 회복 수치
    private readonly BattleTurnRuntime turns; // 현재 전투 턴 관리자
    private readonly Dictionary<BattleUnitRuntime, int> lastTriggeredRounds = new Dictionary<BattleUnitRuntime, int>(); // 별 소유자별 마지막 발동 턴
    private bool disposed; // 패시브 연결 해제 여부
    public StarRecoveryPassiveRuntime(BattleTurnRuntime battleTurns) // 별 패시브 관리자 생성
    { // 관리자 생성 시작
        turns = battleTurns ?? throw new ArgumentNullException(nameof(battleTurns)); // 전투 턴 관리자 저장
    } // 관리자 생성 종료
    public bool TryApply(CardInstance card, IReadOnlyList<BattleUnitRuntime> targets) // 별의 회복 카드 사용 시 추가 예약
    { // 추가 예약 처리 시작
        if (disposed || !turns.IsPlayerTurn || card == null || card.OwnerUnit == null || card.OwnerUnit.IsDead || card.OwnerUnit.CharacterSource == null || card.OwnerUnit.CharacterSource.CharacterId != "ARC_17" || targets == null || !IsHealingCard(card)) // 별 소유자의 실제 회복 카드 확인
        { // 추가 예약 불가 시작
            return false; // 패시브 미발동 반환
        } // 추가 예약 불가 종료
        BattleUnitRuntime owner = card.OwnerUnit; // 별 소유자 조회
        if (lastTriggeredRounds.TryGetValue(owner, out int lastRound) && lastRound == turns.CurrentRound) // 이번 턴의 이전 발동 확인
        { // 턴당 발동 제한 시작
            return false; // 중복 발동 차단 반환
        } // 턴당 발동 제한 종료
        bool applied = false; // 추가 예약 적용 여부 초기화
        HashSet<BattleUnitRuntime> unique = new HashSet<BattleUnitRuntime>(); // 중복 대상 방지 목록 생성
        foreach (BattleUnitRuntime target in targets) // 실제 회복 대상 순회
        { // 개별 대상 예약 시작
            if (target == null || target.IsDead || target.Team != BattleTeam.Ally || !unique.Add(target)) // 생존한 고유 아군 확인
            { // 유효하지 않은 대상 처리 시작
                continue; // 다음 회복 대상 이동
            } // 유효하지 않은 대상 처리 종료
            BattleStatusEffectApplyResult result = target.ApplyStatusEffect(BattleStatusEffectType.StarPassiveReservedHealing, ReservedHealing, 1, 1, owner); // 별도 회복 두 점 예약
            applied |= result == BattleStatusEffectApplyResult.Applied || result == BattleStatusEffectApplyResult.Stacked; // 실제 예약 성공 여부 기록
        } // 개별 대상 예약 종료
        if (applied) // 하나 이상의 실제 예약 확인
        { // 발동 기록 시작
            lastTriggeredRounds[owner] = turns.CurrentRound; // 현재 턴의 발동 기록 저장
        } // 발동 기록 종료
        return applied; // 추가 예약 성공 여부 반환
    } // 추가 예약 처리 종료
    private static bool IsHealingCard(CardInstance card) // 회복 또는 예약 회복 카드 확인
    { // 회복 카드 확인 시작
        return card.EffectType == CardEffectType.Heal || card.EffectType == CardEffectType.ApplyStatusEffect && (card.StatusEffectType == BattleStatusEffectType.Regeneration || card.StatusEffectType == BattleStatusEffectType.StarReservedHealing); // 직접 회복과 지속·예약 회복 카드 여부 반환
    } // 회복 카드 확인 종료
    public void Dispose() // 별 패시브 관리자 정리
    { // 패시브 정리 시작
        disposed = true; // 더 이상의 패시브 발동 차단
        lastTriggeredRounds.Clear(); // 소유자별 발동 기록 제거
    } // 패시브 정리 종료
} // 별 패시브 관리 종료
