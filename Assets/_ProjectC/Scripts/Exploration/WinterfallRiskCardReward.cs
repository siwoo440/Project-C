using System.Linq; // 저장 가능한 저주 원본 조회
using UnityEngine; // 저장 원본 리소스 조회

public static class WinterfallRiskCardReward // 윈터폴 희귀 카드와 저주 추첨
{ // 보상 처리 시작
    public const string CurseCardId = "CRD_WF_ASH_CURSE"; // 사건 전용 저주 카드 식별자

    public static CardData GetReward(string runtimeId) // 현재 방에서 받을 카드 원본 조회
    { // 보상 조회 시작
        if (string.IsNullOrWhiteSpace(runtimeId)) // 고정 방 식별자 누락 확인
        { // 잘못된 방 처리 시작
            return null; // 지급 가능한 카드 없음
        } // 잘못된 방 처리 종료
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(runtimeId)); // 저장과 재방문에 같은 분기 사용
        if (random.Next(2) == 0) // 두 결과 중 희귀 카드 절반 선택
        { // 희귀 카드 조회 시작
            return AshstormEyeEvent.GetRareReward(runtimeId + "|rare")?.Card; // 별도 시드로 등록된 희귀 공용 카드 선택
        } // 희귀 카드 조회 종료
        var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 가능한 원본 목록 조회
        return saved?.Cards.FirstOrDefault(card => card != null && card.CardId == CurseCardId); // 사건 전용 저주 원본 반환
    } // 보상 조회 종료

    public static bool TryGrant(string runtimeId, CharacterData actor, out CardData reward) // 행동한 생존자의 회차 덱에 한 장 지급
    { // 카드 지급 시작
        reward = null; // 지급 전 결과 초기화
        if (!BattleResultManager.EnsureInstance().IsLivingPartyMember(actor)) // 출전 생존자 확인
        { // 잘못된 소유자 처리 시작
            return false; // 사망자와 외부 캐릭터 지급 차단
        } // 잘못된 소유자 처리 종료
        CardData selected = GetReward(runtimeId); // 현재 방의 실제 보상 선택
        if (selected == null || !RunDeckManager.EnsureInstance().TryAddCard(selected, actor)) // 회차 덱 등록 성공 확인
        { // 지급 실패 처리 시작
            return false; // 미지급 결과 반환
        } // 지급 실패 처리 종료
        reward = selected; // 지급한 카드 원본 전달
        return true; // 카드 한 장 지급 성공
    } // 카드 지급 종료
} // 보상 처리 종료
