using System.Collections.Generic; // 보상 후보 목록 사용
using UnityEngine; // 난수 보정과 런타임 관리자 사용

public enum ExplorationTreasureRewardType // 보물 방 랜덤 보상 종류
{
    Gold = 0, // 골드 보상
    Card = 1, // 카드 보상
    Relic = 2, // 유물 보상
    Potion = 3, // 포션 보상
    Resource = 4, // 제작 자원 보상
    CardUpgrade = 5 // 카드 강화 보상
}

public static class ExplorationTreasureRewardService // 57일차 보물 방 랜덤 보상 서비스
{
    internal const int GoldRewardAmount = 150; // Prototype 골드 보상
    internal const int ResourceRewardAmount = 40; // Prototype 제작 자원 보상

    public static bool TryGrantReward(string runtimeId, out string message) // 보물 방 랜덤 보상 지급
    { // 기존 보물 보상 시작
        return TryGrantRewardInternal(runtimeId, false, out message, out _); // 직접 선택 없는 기존 보상 처리
    } // 기존 보물 보상 종료

    public static bool TryGrantReward(string runtimeId, out string message, out ShopOfferData pendingOffer) // 선택 가능한 보물 보상 지급
    { // 선택 가능한 보물 보상 시작
        return TryGrantRewardInternal(runtimeId, true, out message, out pendingOffer); // 공용 카드 선택을 포함한 보상 처리
    } // 선택 가능한 보물 보상 종료

    internal static int GrantLockedDrawerBonus(string runtimeId) // 완료를 확정한 실제 보물방의 별도 골드 판정
    { // 서랍 열쇠의 독립 보상 처리 시작
        var key = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.LockedDrawerTreasureBonus); // 지금 회차에 보유한 잠긴 서랍 열쇠 조회
        if (string.IsNullOrWhiteSpace(runtimeId) || key == null || key.EffectValue <= 0 || key.TriggerChancePercent <= 0) // 실제 방과 유효한 별도 골드 및 확률 확인
        { // 추가 골드 후보 제외 시작
            return 0; // 기본 보상 난수와 지급액 보존
        } // 추가 골드 후보 제외 종료
        var random = new System.Random(BuildStableHash(runtimeId + ":drawer-bonus")); // 방 식별자에서 기본 보물과 분리한 고정 난수
        if (random.Next(100) >= key.TriggerChancePercent) // 기존 기본 골드 판정과 별도의 백분율 확인
        { // 추가 서랍 보상 확률 실패 시작
            return 0; // 완료 표시 외 추가 지급 없음
        } // 추가 서랍 보상 확률 실패 종료
        PlayerResourceManager.EnsureInstance().AddResources(key.EffectValue, 0, 0, 0); // 기존 회차 자원 경로로 저장 전에 추가 골드 지급
        return key.EffectValue; // 기본 보물 결과에 이어 표시할 실제 추가 골드
    } // 서랍 열쇠의 독립 보상 처리 종료

    private static bool TryGrantRewardInternal(string runtimeId, bool allowOwnerChoice, out string message, out ShopOfferData pendingOffer) // 보물 보상 공통 처리
    {
        pendingOffer = null; // 대기 중 카드 초기화
        int seed = BuildStableHash(runtimeId); // Runtime ID 기반 고정 Seed 생성
        System.Random random = new System.Random(seed); // 보상 전용 난수 준비
        List<ExplorationTreasureRewardType> attempts = BuildRewardAttemptOrder(random); // 가중치 기반 보상 시도 순서 생성

        foreach (ExplorationTreasureRewardType rewardType in attempts) // 보상 종류 순차 시도
        {
            if (TryGrantRewardType(rewardType, random, allowOwnerChoice, out message, out pendingOffer)) // 현재 보상 지급 또는 선택 준비 확인
            {
                return true; // 지급 성공 반환
            }
        }

        PlayerResourceManager.EnsureInstance().AddResources(GoldRewardAmount, 0, 0, 0); // 모든 전용 보상 실패 시 골드 지급
        message = $"보물 획득: Gold +{GoldRewardAmount}"; // Fallback 보상 안내
        return true; // Fallback 지급 성공 반환
    }

    private static List<ExplorationTreasureRewardType> BuildRewardAttemptOrder(System.Random random) // 가중치 기반 보상 시도 순서 생성
    {
        List<ExplorationTreasureRewardType> result = new List<ExplorationTreasureRewardType>(); // 보상 시도 목록 생성
        ExplorationTreasureRewardType firstType = RollRewardType(random.Next(100)); // 첫 보상 가중치 추첨
        result.Add(firstType); // 첫 보상 후보 등록

        ExplorationTreasureRewardType[] fallbackOrder = // 실패 시 대체 보상 우선순위
        {
            ExplorationTreasureRewardType.Relic, // 핵심 유물 보상 우선
            ExplorationTreasureRewardType.Card, // 카드 보상
            ExplorationTreasureRewardType.Potion, // 포션 보상
            ExplorationTreasureRewardType.CardUpgrade, // 카드 강화 보상
            ExplorationTreasureRewardType.Resource, // 제작 자원 보상
            ExplorationTreasureRewardType.Gold // 골드 보상
        };

        foreach (ExplorationTreasureRewardType rewardType in fallbackOrder) // 대체 보상 순회
        {
            if (!result.Contains(rewardType)) // 중복 보상 종류 확인
            {
                result.Add(rewardType); // 미등록 대체 보상 추가
            }
        }

        return result; // 전체 시도 순서 반환
    }

    public static ExplorationTreasureRewardType RollRewardType(int roll) // Prototype 보물 가중치 판정
    {
        int safeRoll = Mathf.Clamp(roll, 0, 99); // 0~99 범위 보정

        if (safeRoll < 20) // 골드 20% 확인
        {
            return ExplorationTreasureRewardType.Gold; // 골드 반환
        }

        if (safeRoll < 35) // 카드 15% 확인
        {
            return ExplorationTreasureRewardType.Card; // 카드 반환
        }

        if (safeRoll < 65) // 유물 30% 확인
        {
            return ExplorationTreasureRewardType.Relic; // 핵심 유물 반환
        }

        if (safeRoll < 75) // 포션 10% 확인
        {
            return ExplorationTreasureRewardType.Potion; // 포션 반환
        }

        if (safeRoll < 90) // 자원 15% 확인
        {
            return ExplorationTreasureRewardType.Resource; // 제작 자원 반환
        }

        return ExplorationTreasureRewardType.CardUpgrade; // 카드 강화 10% 반환
    }

    private static bool TryGrantRewardType(
        ExplorationTreasureRewardType rewardType,
        System.Random random,
        bool allowOwnerChoice, // 공용 카드 선택 허용 여부
        out string message, // 보상 안내 결과
        out ShopOfferData pendingOffer) // 선택 대기 카드 결과
    {
        pendingOffer = null; // 대기 카드 기본값 설정
        switch (rewardType) // 보상 종류 분기
        {
            case ExplorationTreasureRewardType.Gold:
                PlayerResourceManager.EnsureInstance().AddResources(GoldRewardAmount, 0, 0, 0); // 골드 지급
                message = $"보물 획득: Gold +{GoldRewardAmount}"; // 골드 결과 안내
                return true; // 골드 지급 성공

            case ExplorationTreasureRewardType.Card:
                return TryGrantCard(random, allowOwnerChoice, out message, out pendingOffer); // 카드 보상 지급 또는 선택 준비

            case ExplorationTreasureRewardType.Relic:
                return TryGrantRelic(random, out message, out _); // 보물 방의 기존 유물 보상 지급

            case ExplorationTreasureRewardType.Potion:
                return TryGrantPotion(random, out message); // 포션 보상 지급 시도

            case ExplorationTreasureRewardType.Resource:
                return TryGrantResource(random, out message); // 제작 자원 지급

            case ExplorationTreasureRewardType.CardUpgrade:
                return TryGrantCardUpgrade(random, out message); // 카드 강화 지급 시도

            default:
                message = "보상 종류가 올바르지 않습니다."; // 잘못된 보상 안내
                return false; // 지급 실패 반환
        }
    }

    private static bool TryGrantCard(System.Random random, bool allowOwnerChoice, out string message, out ShopOfferData pendingOffer) // 상점 카탈로그 카드 기반 보상 지급
    {
        pendingOffer = null; // 대기 카드 기본값 설정
        List<ShopOfferData> offers = GetValidOffers(ShopOfferType.Card); // 카드 상품 후보 조회
        if (!allowOwnerChoice || PartyLoadoutRuntime.Active == null || PartyLoadoutRuntime.Active.Party == null || PartyLoadoutRuntime.Active.Party.MemberCount == 0) // 직접 선택 불가 상황 확인
        { // 선택 불가 카드 제외 시작
            offers.RemoveAll(offer => offer.RequiresOwnerChoice); // 소유자 선택이 필요한 카드 제외
        } // 선택 불가 카드 제외 종료
        if (offers.Count == 0) // 카드 후보 존재 확인
        {
            message = "지급 가능한 카드 데이터가 없습니다."; // 카드 누락 안내
            return false; // 지급 실패 반환
        }

        ShopOfferData offer = offers[random.Next(offers.Count)]; // 카드 상품 무작위 선택
        if (offer.RequiresOwnerChoice) // 공용 카드 소유자 선택 확인
        { // 공용 카드 보류 시작
            pendingOffer = offer; // 선택 대기 카드 저장
            message = $"카드 [{offer.DisplayName}]의 소유자를 선택하세요."; // 선택 안내 준비
            return true; // 카드 선택 준비 성공 반환
        } // 공용 카드 보류 종료
        bool added = RunDeckManager.EnsureInstance().TryAddCard(offer.Card, offer.CardOwner); // 회차 덱 카드 추가

        message = added
            ? $"보물 획득: 카드 [{offer.DisplayName}]"
            : "현재 회차 덱에 카드를 추가할 수 없습니다."; // 카드 지급 결과 안내

        return added; // 카드 지급 결과 반환
    }

    internal static RelicData TryGrantCoreRelic(string runtimeId) // 침식핵 조우에 한한 별도 유물 추첨
    { // 침식핵 유물 추첨 시작
        int chance = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.ErosionCoreRelicChance)?.EffectValue ?? 0; // 보유한 코어 조각의 추가 유물 확률
        if (chance <= 0) // 코어 조각 보유와 발동 확률 확인
        { // 추가 확률 없음 처리 시작
            return null; // 기존 클리어 보상만 유지
        } // 추가 확률 없음 처리 종료
        var random = new System.Random(BuildStableHash(runtimeId + ":core-relic")); // 배치 조우의 독립 고정 보상 난수
        return random.Next(100) < Mathf.Min(chance, 100) && TryGrantRelic(random, out _, out var relic) ? relic : null; // 당첨 시 미보유 유물 한 종 지급
    } // 침식핵 유물 추첨 종료

    private static bool TryGrantRelic(System.Random random, out string message, out RelicData awardedRelic) // 상점 카탈로그 유물 기반 보상 지급
    { // 미보유 유물 지급 시작
        awardedRelic = null; // 신규 유물 지급 전 기본 결과
        List<ShopOfferData> offers = GetValidOffers(ShopOfferType.Relic); // 유물 상품 후보 조회
        RelicRunManager relics = RelicRunManager.EnsureInstance(); // 현재 유물 보관함 조회

        while (offers.Count > 0) // 획득 가능한 유물 탐색
        {
            int index = random.Next(offers.Count); // 유물 후보 위치 선택
            ShopOfferData offer = offers[index]; // 현재 유물 상품 조회
            offers.RemoveAt(index); // 동일 유물 재시도 방지

            if (offer.Relic == null || relics.Inventory.ContainsRelic(offer.Relic.RelicId)) // 빈 유물 또는 이미 보유한 유물 확인
            { // 지급 불가 후보 처리 시작
                continue; // 골드 지급 없이 다음 후보 검색
            } // 지급 불가 후보 처리 종료

            RelicAcquireResult result = relics.TryAcquire(offer.Relic); // 신규 유물 획득 시도
            if (result == RelicAcquireResult.Acquired) // 신규 유물 획득 확인
            {
                awardedRelic = offer.Relic; // 이번에 실제 획득한 유물 원본 기록
                message = $"보물 획득: 유물 [{offer.DisplayName}]"; // 유물 결과 안내
                return true; // 유물 지급 성공
            }
        }

        message = "획득 가능한 신규 유물이 없습니다."; // 유물 지급 불가 안내
        return false; // 지급 실패 반환
    }

    private static bool TryGrantPotion(System.Random random, out string message) // 상점 카탈로그 포션 기반 보상 지급
    {
        List<ShopOfferData> offers = GetValidOffers(ShopOfferType.Potion); // 포션 상품 후보 조회
        if (offers.Count == 0) // 포션 후보 존재 확인
        {
            message = "지급 가능한 포션 데이터가 없습니다."; // 포션 누락 안내
            return false; // 지급 실패 반환
        }

        ShopOfferData offer = offers[random.Next(offers.Count)]; // 포션 상품 무작위 선택
        bool acquired = ConsumableRunManager.EnsureInstance().TryAcquire(
            offer.Potion,
            out int acquiredSlotIndex); // 포션 빈 슬롯 획득 시도

        message = acquired && acquiredSlotIndex >= 0
            ? $"보물 획득: 포션 [{offer.DisplayName}]"
            : "포션 보관 공간이 부족합니다."; // 포션 지급 결과 안내

        return acquired && acquiredSlotIndex >= 0; // 포션 지급 결과 반환
    }

    private static bool TryGrantResource(System.Random random, out string message) // 제작 자원 랜덤 지급
    {
        int resourceIndex = random.Next(3); // 나사·철판·전선 중 하나 선택

        if (resourceIndex == 0) // 나사 선택 확인
        {
            PlayerResourceManager.EnsureInstance().AddResources(0, ResourceRewardAmount, 0, 0); // 나사 지급
            message = $"보물 획득: 나사 +{ResourceRewardAmount}"; // 나사 결과 안내
            return true; // 지급 성공 반환
        }

        if (resourceIndex == 1) // 철판 선택 확인
        {
            PlayerResourceManager.EnsureInstance().AddResources(0, 0, ResourceRewardAmount, 0); // 철판 지급
            message = $"보물 획득: 철판 +{ResourceRewardAmount}"; // 철판 결과 안내
            return true; // 지급 성공 반환
        }

        PlayerResourceManager.EnsureInstance().AddResources(0, 0, 0, ResourceRewardAmount); // 전선 지급
        message = $"보물 획득: 전선 +{ResourceRewardAmount}"; // 전선 결과 안내
        return true; // 지급 성공 반환
    }

    private static bool TryGrantCardUpgrade(System.Random random, out string message) // 현재 회차 카드 랜덤 강화
    {
        RunDeckManager runDeck = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        List<int> candidates = new List<int>(); // 강화 가능 카드 위치 목록

        for (int index = 0; index < runDeck.CardCount; index++) // 현재 카드 전체 순회
        {
            if (runDeck.CanUpgradeAt(index)) // 강화 가능 여부 확인
            {
                candidates.Add(index); // 강화 후보 등록
            }
        }

        if (candidates.Count == 0) // 강화 가능 카드 존재 확인
        {
            message = "강화 가능한 카드가 없습니다."; // 강화 후보 없음 안내
            return false; // 지급 실패 반환
        }

        int cardIndex = candidates[random.Next(candidates.Count)]; // 강화 카드 무작위 선택
        string cardName = runDeck.Cards[cardIndex].Card.DisplayName; // 강화 카드 이름 저장

        if (!runDeck.TryUpgradeAt(cardIndex)) // 카드 강화 실행 확인
        {
            message = "카드 강화 보상 적용에 실패했습니다."; // 강화 실패 안내
            return false; // 지급 실패 반환
        }

        message = $"보물 획득: 카드 강화 [{cardName}]"; // 강화 결과 안내
        return true; // 강화 지급 성공 반환
    }

    internal static List<ShopOfferData> GetValidOffers(ShopOfferType offerType) // 탐사 보물과 이벤트의 실제 보상 후보 조회
    {
        List<ShopOfferData> result = new List<ShopOfferData>(); // 유효 상품 목록 생성
        ShopRunManager shopManager = ShopRunManager.Instance; // 현재 상점 관리자 조회
        ShopCatalogData catalog = shopManager != null ? shopManager.Catalog : null; // 준비된 상점 카탈로그 조회
        if (catalog == null) // 상점 방문 전 또는 복원 직후 확인
        { // 기본 보상 카탈로그 준비 시작
            catalog = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 보상 원본 목록 조회
        } // 기본 보상 카탈로그 준비 종료

        if (catalog == null || !catalog.IsValidData()) // 보상 카탈로그 유효성 확인
        {
            return result; // 빈 목록 반환
        }

        foreach (ShopOfferData offer in catalog.Offers) // 전체 상점 상품 순회
        {
            if (offer != null && offer.OfferType == offerType && offer.IsValidData() && offer.CanOfferToDeck(RunDeckManager.Instance)) // 종류와 현재 덱의 지급 가능 여부 확인
            { // 지급 가능 후보 처리 시작
                result.Add(offer); // 보상 후보 등록
            } // 지급 가능 후보 처리 종료
        }

        return result; // 유효 보상 후보 반환
    }

    internal static int BuildStableHash(string text) // 탐사 보상용 문자열 고정 시드 계산
    {
        unchecked
        {
            int hash = 17; // 해시 초기값
            string safeText = text ?? string.Empty; // 빈 문자열 보정

            for (int index = 0; index < safeText.Length; index++) // 전체 문자 순회
            {
                hash = hash * 31 + safeText[index]; // 고정 해시 누적
            }

            return hash; // 최종 해시 반환
        }
    }
}
