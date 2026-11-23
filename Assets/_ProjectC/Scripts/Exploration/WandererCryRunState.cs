using System; // 방 식별자와 저장 필드 사용
using System.Collections.Generic; // 카드 후보와 파티 목록 사용
using System.Linq; // 지급 후보 조회와 중복 제거
using UnityEngine; // 실제 카드와 물약 데이터 조회

[Serializable] // 탐사 저장에 포함하는 방랑자 상태
public sealed class WandererCryRunState // 다음 교전과 해당 승리 보상의 보관 정보
{ // 보관 정보 시작
    [SerializeField] private string followRoom; // 목소리를 따라간 원래 방
    [SerializeField] private bool coverEars; // 다음 전투 첫 아군 턴 명중 감소
    [SerializeField] private string[] cardIds; // 다시 불러와도 유지할 세 후보
    [NonSerialized] private string battleId; // 실제로 진입한 보상 대상 교전
    [NonSerialized] private bool victory; // 해당 교전의 승리 확정 여부
    [NonSerialized] private bool granting; // 지급 중 다시 누르기 차단
    public bool HasFollow => !string.IsNullOrEmpty(followRoom); // 목소리 추적 효과 보유 여부
    public bool HasCoveredEars => coverEars; // 귀 막기 효과 보유 여부
    public bool HasVictoryChoice => victory && HasFollow; // 지금 열 수 있는 승리 보상 여부

    public bool EnemyStartsFirst => battleId == null && HasFollow && Roll(followRoom, "initiative") < 15; // 교전 연결 전 한 번만 사용할 고정 선제 판정

    internal static int Roll(string room, string purpose) // 효과별 고정 확률 난수
    { // 난수 계산 시작
        return new System.Random(ExplorationTreasureRewardService.BuildStableHash(room + "|" + purpose)).Next(100); // 영부터 구십구까지 고정값 반환
    } // 난수 계산 종료

    internal bool TryFollow(string room) // 보상 가능한 세 카드와 다음 교전 예약
    { // 추적 예약 시작
        if (HasFollow) // 아직 끝나지 않은 추적 확인
        { // 중첩 차단 시작
            return false; // 이전 예약과 새 방 보존
        } // 중첩 차단 종료
        var sheet = CardSheetCatalog.Load(); // 실제 카드 기획 시트 조회
        var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 가능한 카드 목록 조회
        if (saved == null || sheet == null) // 실제 원본 누락 확인
        { // 미등록 상태 처리 시작
            return false; // 보상 없는 비용 차감 방지
        } // 미등록 상태 처리 종료
        var candidates = ExplorationTreasureRewardService.GetValidOffers(ShopOfferType.Card).Where(offer => offer.RequiresOwnerChoice && saved.Cards.Contains(offer.Card) && sheet.FindByName(offer.Card.DisplayName) != null).Select(offer => offer.Card.CardId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList(); // 지급과 복원이 가능한 서로 다른 공용 카드 조회
        if (candidates.Count < 3) // 서로 다른 세 카드 확보 확인
        { // 후보 부족 처리 시작
            return false; // 비용과 미처리 방 보존
        } // 후보 부족 처리 종료
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(room + "|cards")); // 방마다 고정한 보상 선택 순서
        var selected = new string[3]; // 실제 제시할 세 카드
        for (int index = 0; index < selected.Length; index++) // 서로 다른 세 후보 선택
        { // 개별 후보 선택 시작
            int pick = random.Next(candidates.Count); // 남은 후보 중 한 장 선택
            selected[index] = candidates[pick]; // 선택된 원본 식별자 보관
            candidates.RemoveAt(pick); // 같은 카드 재선택 방지
        } // 개별 후보 선택 종료
        cardIds = selected; // 확정된 세 후보 보관
        followRoom = room; // 보상과 위험의 원래 방 보관
        return true; // 추적 예약 성공
    } // 추적 예약 종료

    internal void CoverEars() // 다음 실제 전투의 명중 감소 예약
    { // 귀 막기 예약 시작
        coverEars = true; // 같은 효과는 한 턴분만 보관
    } // 귀 막기 예약 종료

    internal void BeginBattle(string actualBattle, IReadOnlyList<BattleUnitRuntime> allies) // 실제 교전과 출전자를 대기 효과에 연결
    { // 교전 연결 시작
        if (string.IsNullOrWhiteSpace(actualBattle) || allies == null || !allies.Any(ally => ally != null && !ally.IsDead && ally.Team == BattleTeam.Ally)) // 디버그 전투와 빈 편성 제외
        { // 교전 전 보존 시작
            return; // 유효한 교전까지 대기 보존
        } // 교전 전 보존 종료
        if (HasFollow && battleId == null) // 이번 전투의 추적 확인
        { // 보상 연결 시작
            battleId = actualBattle; // 승리 보상을 받을 정확한 교전 기록
        } // 보상 연결 종료
        if (!coverEars) // 명중 감소 예약 존재 확인
        { // 명중 보존 시작
            return; // 귀를 막지 않았거나 이미 사용한 효과 제외
        } // 명중 보존 종료
        coverEars = false; // 적용 전 중복 호출 차단
        foreach (var ally in allies) // 실제 출전 아군 순회
        { // 아군별 적용 시작
            if (ally != null && !ally.IsDead && ally.Team == BattleTeam.Ally) // 생존 아군 확인
            { // 첫 턴 효과 적용 시작
                ally.ApplyStatusEffect(BattleStatusEffectType.Disorientation, 10, 1, 1); // 방향 감각 상실로 첫 턴 공격 명중 감소
                ally.GetStatusEffect(BattleStatusEffectType.Disorientation)?.ConsumeDurationDecreaseSkip(); // 첫 아군 턴 종료에 효과 만료
            } // 첫 턴 효과 적용 종료
        } // 아군별 적용 종료
    } // 교전 연결 종료

    internal void RecordResult(string actualBattle, BattleResult result) // 같은 전투의 확정 결과 기록
    { // 승패 처리 시작
        if (battleId == null || actualBattle != battleId) // 추적이 연결된 교전 확인
        { // 무관한 결과 처리 시작
            return; // 다른 전투의 보상 차단
        } // 무관한 결과 처리 종료
        if (result == BattleResult.Victory) // 실제 승리 확인
        { // 승리 상태 처리 시작
            victory = true; // 세 후보 중 하나를 받을 수 있는 상태
        } // 승리 상태 처리 종료
        else // 도주와 패배 처리
        { // 보상 소멸 시작
            Finish(); // 해당 전투로 추적과 보상 종료
        } // 보상 소멸 종료
    } // 승패 처리 종료

    public IReadOnlyList<CardData> GetCards() // 최초 저장한 순서의 세 카드 원본 조회
    { // 원본 조회 시작
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장용 실제 원본 조회
        if (cardIds == null || catalog == null) // 대상 후보 존재 확인
        { // 후보 없음 처리 시작
            return Array.Empty<CardData>(); // 빈 목록 반환
        } // 후보 없음 처리 종료
        return cardIds.Select(id => catalog.Cards.FirstOrDefault(card => card != null && card.CardId == id)).ToArray(); // 확정했던 원본만 순서대로 복원
    } // 원본 조회 종료

    internal bool IsValid(string[] completedRooms) // 저장의 원래 방과 세 카드 확인
    { // 저장 일관성 검사 시작
        if (!HasFollow) // 추적이 없는 저장 확인
        { // 빈 추적 검사 시작
            return cardIds == null || cardIds.Length == 0; // 출처 없는 카드 후보 차단
        } // 빈 추적 검사 종료
        return completedRooms != null && Array.IndexOf(completedRooms, followRoom) >= 0 && cardIds?.Length == 3 && cardIds.Distinct().Count() == 3 && GetCards().All(card => card != null); // 원래 완료 방과 서로 다른 등록 카드 세 개 확인
    } // 저장 일관성 검사 종료

    public bool TryClaim(string actualBattle, CardData card, CharacterData owner) // 같은 교전 승리 직후 생존자에게 한 장 지급
    { // 지급 검사 시작
        if (granting || !HasVictoryChoice || actualBattle != battleId || card == null || !GetCards().Contains(card) || !BattleResultManager.EnsureInstance().IsLivingPartyMember(owner)) // 승리와 후보 및 실제 생존자 확인
        { // 잘못된 지급 처리 시작
            return false; // 카드와 선택 상태 보존
        } // 잘못된 지급 처리 종료
        granting = true; // 재입력 잠금
        try // 지급 결과 처리 보장
        { // 카드 지급 시작
            if (!RunDeckManager.EnsureInstance().TryAddCard(card, owner)) // 실제 소유자 덱에 한 장 추가
            { // 지급 실패 처리 시작
                return false; // 다시 고를 수 있는 상태 보존
            } // 지급 실패 처리 종료
            Finish(); // 같은 승리의 재지급 차단
            return true; // 카드 선택 완료
        } // 카드 지급 종료
        finally // 입력 복구
        { // 잠금 해제 시작
            granting = false; // 보상 처리 잠금 해제
        } // 잠금 해제 종료
    } // 지급 검사 종료

    private void Finish() // 완료하거나 잃은 추적과 보상 제거
    { // 추적 정리 시작
        followRoom = null; // 다음 전투에 이전 추적 이월 방지
        battleId = null; // 이전 승패 다시 받기 차단
        cardIds = null; // 재지급할 카드 후보 제거
        victory = false; // 승리 보상 종료
    } // 추적 정리 종료
} // 보관 정보 종료
