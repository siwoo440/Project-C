using System; // 이벤트 자료형 기능
using System.Collections.Generic; // List 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleDeckRuntime : MonoBehaviour // 전투 덱 런타임 관리자
{
    [Header("원본 덱")] // 원본 덱 구분
    [SerializeField] private List<CardData> originalCards = new List<CardData>(); // 원본 카드 목록
   
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleActionPointRuntime actionPointRuntime; // 전투 AP 관리자
    [SerializeField] private BattleTurnRuntime turnRuntime; // 전투 턴 관리자

    [Header("전체 전투 카드")] // 전체 카드 구분
    [SerializeField] private List<CardInstance> runtimeCards = new List<CardInstance>(); // 전체 전투 카드 목록

    [Header("카드 더미")] // 카드 더미 구분
    [SerializeField] private List<CardInstance> drawPile = new List<CardInstance>(); // 드로우 더미
    [SerializeField] private List<CardInstance> hand = new List<CardInstance>(); // 손패
    [SerializeField] private List<CardInstance> discardPile = new List<CardInstance>(); // 버린 카드 더미
    [SerializeField] private List<CardInstance> exhaustPile = new List<CardInstance>(); // 제외 카드 더미
   
    [Header("드로우 설정")] // 드로우 설정 구분
    [SerializeField] private int initialHandSize = 8; // 전투 시작 드로우 수
    [SerializeField] private int cardsPerTurn = 4; // 턴 시작 드로우 수
    public IReadOnlyList<CardInstance> RuntimeCards => runtimeCards; // 전체 전투 카드 반환
    public IReadOnlyList<CardInstance> DrawPile => drawPile; // 드로우 더미 반환
    public IReadOnlyList<CardInstance> Hand => hand; // 손패 반환
    public IReadOnlyList<CardInstance> DiscardPile => discardPile; // 버린 카드 더미 반환
    public IReadOnlyList<CardInstance> ExhaustPile => exhaustPile; // 제외 카드 더미 반환

    public int RuntimeCardCount => runtimeCards.Count; // 전체 전투 카드 수 반환
    public int DrawPileCount => drawPile.Count; // 드로우 더미 카드 수 반환
    public int HandCount => hand.Count; // 손패 카드 수 반환
    public int DiscardPileCount => discardPile.Count; // 버린 카드 수 반환
    public int ExhaustPileCount => exhaustPile.Count; // 제외 카드 수 반환
    public event Action CardPilesChanged; // 카드 더미 변경 이벤트
    private void Start() // 전투 덱 초기화
    {
        BuildRuntimeDeck(); // 원본 덱 기반 전투 덱 생성
    }

    [ContextMenu("Rebuild Runtime Deck")] // 전투 덱 재생성 메뉴
    public void BuildRuntimeDeck() // 전투 덱 생성
    {
        runtimeCards.Clear(); // 기존 전체 전투 카드 제거
        ClearAllPiles(); // 기존 카드 더미 초기화

        if (originalCards.Count == 0) // 원본 덱 비어 있음 확인
        {
            Debug.LogWarning("Original deck is empty.", this); // 빈 덱 경고
            NotifyCardPilesChanged(); // 빈 손패 UI 갱신
            return; // 덱 생성 중단
        }

        for (int index = 0; index < originalCards.Count; index++) // 원본 카드 순회
        {
            CardData originalCard = originalCards[index]; // 현재 원본 카드 가져오기

            if (originalCard == null) // 원본 카드 누락 확인
            {
                Debug.LogWarning($"Original card at index {index} is missing.", this); // 누락 위치 경고
                continue; // 다음 카드 처리
            }

            CardInstance runtimeCard = originalCard.CreateRuntimeInstance(); // 전투 카드 생성
            runtimeCards.Add(runtimeCard); // 전체 전투 카드에 추가
        }

        InitializeCardPiles(); // 모든 카드를 드로우 더미에 배치
        ShuffleDrawPile(); // 전투 시작 덱 셔플
        DrawCards(initialHandSize); // 시작 손패 드로우
        ResetActionPoints(); // 전투 시작 AP 초기화
        LogRuntimeDeck(); // 전투 덱 생성 결과 출력
        LogPileState(); // 카드 더미 상태 출력
        ValidateCardPiles(); // 카드 더미 무결성 검사
        NotifyCardPilesChanged(); // 전투 시작 손패 UI 갱신
    }

    [ContextMenu("Reset Card Piles")] // 카드 더미 초기화 메뉴
    public void ResetCardPiles() // 전투 시작 카드 상태 복구
    {
        InitializeCardPiles(); // 모든 카드를 드로우 더미로 복구
        ShuffleDrawPile(); // 복구한 드로우 더미 셔플
        DrawCards(initialHandSize); // 시작 손패 다시 드로우
        ResetActionPoints(); // 현재 AP 최대치 복구
        LogPileState(); // 복구 결과 출력
        NotifyCardPilesChanged(); // 초기화된 손패 UI 갱신
    }
    private void ResetActionPoints() // 연결된 AP 관리자 초기화
    {
        if (actionPointRuntime == null) // AP 관리자 연결 확인
        {
            Debug.LogWarning("Battle action point runtime is not assigned.", this); // 연결 누락 경고
            return; // AP 초기화 중단
        }

        actionPointRuntime.ResetForPlayerTurn(); // 현재 AP 최대치 복구
    }

    public void StartPlayerTurn() // 플레이어 턴 시작 처리
    {
        ResetActionPoints(); // 플레이어 AP 초기화
        DrawCards(cardsPerTurn); // 턴 시작 카드 드로우
        Debug.Log($"Player turn deck prepared: Draw {cardsPerTurn} cards.", this); // 턴 시작 덱 결과 출력
    }

    public void EndPlayerTurn() // 플레이어 턴 종료 처리
    {
        DiscardEntireHand(); // 남은 손패 전체 버리기
        Debug.Log("Player turn hand discarded.", this); // 턴 종료 손패 결과 출력
    }


    [ContextMenu("Shuffle Draw Pile")] // 드로우 더미 셔플 메뉴
    public void ShuffleDrawPile() // 드로우 더미 무작위 정렬
    {
        if (drawPile.Count <= 1) // 셔플 가능한 카드 수 확인
        {
            Debug.Log($"Draw pile shuffle skipped: {drawPile.Count} card.", this); // 셔플 생략 결과 출력
            return; // 셔플 중단
        }

        for (int currentIndex = drawPile.Count - 1; currentIndex > 0; currentIndex--) // 뒤쪽 카드부터 순회
        {
            int randomIndex = UnityEngine.Random.Range(0, currentIndex + 1); // Unity 무작위 위치 선택
            CardInstance temporaryCard = drawPile[currentIndex]; // 현재 카드 임시 보관
            drawPile[currentIndex] = drawPile[randomIndex]; // 무작위 카드를 현재 위치로 이동
            drawPile[randomIndex] = temporaryCard; // 현재 카드를 무작위 위치로 이동
        }

        Debug.Log($"Draw pile shuffled: {drawPile.Count} cards.", this); // 셔플 완료 출력
    }
    public int DrawCards(int requestedCount) // 지정된 수만큼 카드 드로우
    {
        if (requestedCount <= 0) // 잘못된 드로우 수 확인
        {
            Debug.LogWarning($"Draw count must be greater than zero: {requestedCount}", this); // 잘못된 수 경고
            return 0; // 드로우 수 0 반환
        }

        int drawnCount = 0; // 실제 드로우 수 초기화

        while (drawnCount < requestedCount) // 요청 수만큼 반복
        {
            if (drawPile.Count == 0) // 드로우 더미 소진 확인
            {
                bool recycled = RecycleDiscardPile(); // 버린 카드 재사용 시도

                if (recycled == false) // 재사용 실패 확인
                {
                    break; // 드로우 반복 중단
                }
            }

            CardInstance card = drawPile[0]; // 드로우 더미 첫 카드 가져오기
            bool moved = MoveCard(card, CardPileType.Hand); // 카드를 손패로 이동

            if (moved == false) // 카드 이동 실패 확인
            {
                break; // 드로우 반복 중단
            }

            drawnCount++; // 실제 드로우 수 증가
        }

        Debug.Log($"Cards drawn: {drawnCount} / Requested: {requestedCount}", this); // 드로우 결과 출력
        LogPileState(); // 카드 더미 상태 출력
        NotifyCardPilesChanged(); // 드로우 결과 UI 갱신
        return drawnCount; // 실제 드로우 수 반환
    }

    [ContextMenu("Draw Turn Cards")] // 턴 드로우 테스트 메뉴
    private void DrawTurnCards() // 턴 시작 카드 드로우
    {
        DrawCards(cardsPerTurn); // 턴당 설정 수만큼 드로우
    }
   
    [ContextMenu("Discard Entire Hand")] // 손패 전체 버리기 메뉴
    public void DiscardEntireHand() // 손패 전체를 버린 카드로 이동
    {
        int discardedCount = 0; // 버린 카드 수 초기화

        while (hand.Count > 0) // 손패 카드 존재 확인
        {
            CardInstance card = hand[0]; // 첫 번째 손패 카드 가져오기
            bool moved = MoveCard(card, CardPileType.DiscardPile); // 버린 카드 더미로 이동

            if (moved == false) // 카드 이동 실패 확인
            {
                break; // 손패 정리 중단
            }

            discardedCount++; // 버린 카드 수 증가
        }

        Debug.Log($"Entire hand discarded: {discardedCount} cards.", this); // 손패 정리 결과 출력
        LogPileState(); // 카드 더미 상태 출력
        NotifyCardPilesChanged(); // 비워진 손패 UI 갱신
    }

    private bool RecycleDiscardPile() // 버린 카드를 드로우 더미로 재사용
    {
        if (discardPile.Count == 0) // 버린 카드 존재 확인
        {
            Debug.LogWarning("Draw pile and discard pile are empty.", this); // 사용 가능한 카드 없음 경고
            return false; // 재사용 실패 반환
        }

        int recycledCount = discardPile.Count; // 재사용 카드 수 저장

        while (discardPile.Count > 0) // 버린 카드가 남아 있는 동안 반복
        {
            CardInstance card = discardPile[0]; // 첫 번째 버린 카드 가져오기
            bool moved = MoveCard(card, CardPileType.DrawPile); // 드로우 더미로 이동

            if (moved == false) // 카드 이동 실패 확인
            {
                Debug.LogError("Discard pile recycling failed.", this); // 재사용 실패 출력
                return false; // 재사용 실패 반환
            }
        }

        ShuffleDrawPile(); // 재사용 카드 무작위 셔플
        Debug.Log($"Discard pile recycled: {recycledCount} cards.", this); // 재사용 결과 출력
        return true; // 재사용 성공 반환
    }
    public bool CanUseCard(CardInstance card) // 카드 사용 가능 여부 확인
    {
        if (card == null) // 카드 누락 확인
        {
            return false; // 사용 불가 반환
        }

        if (actionPointRuntime == null) // AP 관리자 연결 확인
        {
            return false; // 사용 불가 반환
        }

        if (runtimeCards.Contains(card) == false) // 전투 카드 등록 확인
        {
            return false; // 사용 불가 반환
        }

        if (hand.Contains(card) == false) // 손패 포함 여부 확인
        {
            return false; // 사용 불가 반환
        }

        if (CountCardLocations(card) != 1) // 카드 위치 무결성 확인
        {
            return false; // 사용 불가 반환
        }

        if (turnRuntime != null && turnRuntime.IsPlayerTurn == false) // 플레이어 턴 여부 확인
        {
            return false; // 카드 사용 불가 반환
        }

        return actionPointRuntime.CanSpend(card.CurrentCost); // 현재 비용 지불 가능 여부 반환
    }
    public bool TryUseCard(CardInstance card) // 카드 사용 시도
    {
        if (card == null) // 카드 누락 확인
        {
            Debug.LogWarning("Card to use is missing.", this); // 카드 누락 경고
            return false; // 사용 실패 반환
        }

        if (actionPointRuntime == null) // AP 관리자 연결 확인
        {
            Debug.LogError("Battle action point runtime is not assigned.", this); // AP 관리자 누락 오류
            return false; // 사용 실패 반환
        }

        if (runtimeCards.Contains(card) == false) // 전투 카드 등록 확인
        {
            Debug.LogWarning($"Card is not registered in runtime deck: {card.CardName}", this); // 미등록 카드 경고
            return false; // 사용 실패 반환
        }

        if (hand.Contains(card) == false) // 손패 포함 여부 확인
        {
            Debug.LogWarning($"Card is not in hand: {card.CardName}", this); // 손패 미포함 경고
            return false; // 사용 실패 반환
        }

        int locationCount = CountCardLocations(card); // 카드 위치 개수 계산

        if (locationCount != 1) // 카드 위치 오류 확인
        {
            Debug.LogError($"Invalid card location count: {card.CardName} / {locationCount}", this); // 카드 위치 오류 출력
            return false; // 사용 실패 반환
        }

        if (actionPointRuntime.TrySpend(card.CurrentCost) == false) // 카드 비용 지불 시도
        {
            Debug.LogWarning($"Card use failed: {card.CardName} / Cost {card.CurrentCost}", this); // 비용 지불 실패 출력
            return false; // 사용 실패 반환
        }

        if (turnRuntime != null && turnRuntime.IsPlayerTurn == false) // 플레이어 턴 여부 확인
        {
            Debug.LogWarning("Cards can only be used during the player turn.", this); // 적 턴 카드 사용 경고
            return false; // 카드 사용 실패 반환
        }

        bool moved = MoveCard(card, CardPileType.DiscardPile); // 사용 카드를 버린 더미로 이동

        if (moved == false) // 카드 이동 실패 확인
        {
            Debug.LogError($"Used card could not move to discard pile: {card.CardName}", this); // 카드 이동 실패 출력
            return false; // 사용 실패 반환
        }

        Debug.Log($"Card used: {card.CardName} / Cost {card.CurrentCost} / AP {actionPointRuntime.CurrentActionPoints}", this); // 카드 사용 결과 출력
        LogPileState(); // 카드 더미 상태 출력
        NotifyCardPilesChanged(); // 카드 사용 결과 UI 갱신
        return true; // 사용 성공 반환
    }

    [ContextMenu("Use First Hand Card")] // 첫 손패 카드 사용 메뉴
    private void UseFirstHandCard() // 첫 손패 카드 사용 테스트
    {
        if (hand.Count == 0) // 손패 카드 존재 확인
        {
            Debug.LogWarning("Hand is empty.", this); // 빈 손패 경고
            return; // 카드 사용 중단
        }

        CardInstance card = hand[0]; // 첫 번째 손패 카드 가져오기
        TryUseCard(card); // 카드 사용 시도
    }
    public bool MoveCard(CardInstance card, CardPileType destinationPile) // 카드를 지정한 더미로 이동
    {
        if (card == null) // 이동할 카드 누락 확인
        {
            Debug.LogWarning("Card to move is missing.", this); // 카드 누락 경고
            return false; // 이동 실패 반환
        }

        if (runtimeCards.Contains(card) == false) // 전체 전투 카드 등록 여부 확인
        {
            Debug.LogWarning($"Card is not registered in runtime deck: {card.CardName}", this); // 미등록 카드 경고
            return false; // 이동 실패 반환
        }

        int currentLocationCount = CountCardLocations(card); // 현재 카드 위치 개수 계산

        if (currentLocationCount != 1) // 카드 위치 오류 확인
        {
            Debug.LogError($"Invalid card location count: {card.CardName} / {currentLocationCount}", this); // 카드 위치 오류 출력
            return false; // 이동 실패 반환
        }

        RemoveCardFromAllPiles(card); // 기존 카드 위치에서 제거
        List<CardInstance> destination = GetMutablePile(destinationPile); // 목적지 더미 가져오기
        destination.Add(card); // 목적지 더미에 카드 추가

        Debug.Log($"Card moved: {card.CardName} → {destinationPile}", this); // 카드 이동 결과 출력
        return true; // 이동 성공 반환
    }

    [ContextMenu("Move First Draw Card To Hand")] // 드로우 더미에서 손패 이동 메뉴
    private void MoveFirstDrawCardToHand() // 첫 드로우 카드 손패 이동
    {
        if (drawPile.Count == 0) // 드로우 더미 비어 있음 확인
        {
            Debug.LogWarning("Draw pile is empty.", this); // 빈 드로우 더미 경고
            return; // 이동 중단
        }

        CardInstance card = drawPile[0]; // 첫 번째 드로우 카드 가져오기
        MoveCard(card, CardPileType.Hand); // 카드를 손패로 이동
        LogPileState(); // 이동 결과 출력
    }

    [ContextMenu("Move First Hand Card To Discard")] // 손패에서 버린 더미 이동 메뉴
    private void MoveFirstHandCardToDiscard() // 첫 손패 카드 버린 더미 이동
    {
        if (hand.Count == 0) // 손패 비어 있음 확인
        {
            Debug.LogWarning("Hand is empty.", this); // 빈 손패 경고
            return; // 이동 중단
        }

        CardInstance card = hand[0]; // 첫 번째 손패 카드 가져오기
        MoveCard(card, CardPileType.DiscardPile); // 카드를 버린 더미로 이동
        LogPileState(); // 이동 결과 출력
    }

    [ContextMenu("Move First Draw Card To Exhaust")] // 드로우 더미에서 제외 더미 이동 메뉴
    private void MoveFirstDrawCardToExhaust() // 첫 드로우 카드 제외 처리
    {
        if (drawPile.Count == 0) // 드로우 더미 비어 있음 확인
        {
            Debug.LogWarning("Draw pile is empty.", this); // 빈 드로우 더미 경고
            return; // 이동 중단
        }

        CardInstance card = drawPile[0]; // 첫 번째 드로우 카드 가져오기
        MoveCard(card, CardPileType.ExhaustPile); // 카드를 제외 더미로 이동
        LogPileState(); // 이동 결과 출력
    }

    [ContextMenu("Validate Card Piles")] // 카드 더미 검증 메뉴
    public void ValidateCardPiles() // 카드 중복과 누락 검증
    {
        int totalPileCardCount = drawPile.Count + hand.Count + discardPile.Count + exhaustPile.Count; // 전체 더미 카드 수 계산
        bool isValid = true; // 검증 결과 초기화

        if (totalPileCardCount != runtimeCards.Count) // 전체 카드 수 일치 확인
        {
            Debug.LogError($"Pile total mismatch: Runtime {runtimeCards.Count} / Piles {totalPileCardCount}", this); // 카드 수 불일치 출력
            isValid = false; // 검증 실패 설정
        }

        for (int index = 0; index < runtimeCards.Count; index++) // 전체 전투 카드 순회
        {
            CardInstance card = runtimeCards[index]; // 현재 카드 가져오기

            if (card == null) // 런타임 카드 누락 확인
            {
                Debug.LogError($"Runtime card at index {index} is missing.", this); // 누락 카드 오류 출력
                isValid = false; // 검증 실패 설정
                continue; // 다음 카드 처리
            }

            int locationCount = CountCardLocations(card); // 현재 카드 위치 개수 계산

            if (locationCount != 1) // 카드 위치 개수 확인
            {
                Debug.LogError($"Card must exist in exactly one pile: {card.CardName} / {locationCount}", this); // 중복 또는 누락 오류 출력
                isValid = false; // 검증 실패 설정
            }
        }

        if (isValid) // 전체 검증 성공 확인
        {
            Debug.Log($"Card piles are valid. Total cards: {runtimeCards.Count}", this); // 검증 성공 출력
        }
    }

    private void InitializeCardPiles() // 카드 더미 시작 상태 구성
    {
        ClearAllPiles(); // 모든 카드 더미 비우기

        for (int index = 0; index < runtimeCards.Count; index++) // 전체 전투 카드 순회
        {
            CardInstance card = runtimeCards[index]; // 현재 카드 가져오기

            if (card == null) // 런타임 카드 누락 확인
            {
                continue; // 누락 카드 건너뛰기
            }

            card.ResetBattleValues(); // 카드 임시 전투 수치 초기화
            drawPile.Add(card); // 드로우 더미에 카드 추가
        }
    }

    private void ClearAllPiles() // 모든 카드 더미 비우기
    {
        drawPile.Clear(); // 드로우 더미 비우기
        hand.Clear(); // 손패 비우기
        discardPile.Clear(); // 버린 카드 더미 비우기
        exhaustPile.Clear(); // 제외 카드 더미 비우기
    }

    private void RemoveCardFromAllPiles(CardInstance card) // 모든 더미에서 카드 제거
    {
        drawPile.Remove(card); // 드로우 더미에서 제거
        hand.Remove(card); // 손패에서 제거
        discardPile.Remove(card); // 버린 카드 더미에서 제거
        exhaustPile.Remove(card); // 제외 카드 더미에서 제거
    }

    private List<CardInstance> GetMutablePile(CardPileType pileType) // 수정 가능한 카드 더미 반환
    {
        switch (pileType) // 카드 더미 종류 확인
        {
            case CardPileType.DrawPile: // 드로우 더미 확인
                return drawPile; // 드로우 더미 반환

            case CardPileType.Hand: // 손패 확인
                return hand; // 손패 반환

            case CardPileType.DiscardPile: // 버린 카드 더미 확인
                return discardPile; // 버린 카드 더미 반환

            case CardPileType.ExhaustPile: // 제외 카드 더미 확인
                return exhaustPile; // 제외 카드 더미 반환

            default: // 지원하지 않는 값 확인
                Debug.LogError($"Unsupported card pile type: {pileType}", this); // 지원하지 않는 값 출력
                return drawPile; // 안전한 기본 더미 반환
        }
    }

    private int CountCardLocations(CardInstance card) // 카드가 들어 있는 전체 위치 수 계산
    {
        int locationCount = 0; // 위치 수 초기화
        locationCount += CountCardOccurrences(drawPile, card); // 드로우 더미 위치 수 추가
        locationCount += CountCardOccurrences(hand, card); // 손패 위치 수 추가
        locationCount += CountCardOccurrences(discardPile, card); // 버린 카드 위치 수 추가
        locationCount += CountCardOccurrences(exhaustPile, card); // 제외 카드 위치 수 추가
        return locationCount; // 전체 위치 수 반환
    }

    private int CountCardOccurrences(List<CardInstance> pile, CardInstance card) // 더미 내부 카드 개수 계산
    {
        int occurrenceCount = 0; // 카드 개수 초기화

        for (int index = 0; index < pile.Count; index++) // 카드 더미 순회
        {
            if (pile[index] == card) // 동일 카드 인스턴스 확인
            {
                occurrenceCount++; // 동일 카드 개수 증가
            }
        }

        return occurrenceCount; // 동일 카드 개수 반환
    }

    private void LogRuntimeDeck() // 전투 덱 생성 결과 출력
    {
        Debug.Log($"Runtime deck created: {runtimeCards.Count} cards.", this); // 전체 카드 수 출력

        for (int index = 0; index < runtimeCards.Count; index++) // 전체 전투 카드 순회
        {
            CardInstance card = runtimeCards[index]; // 현재 전투 카드 가져오기
            Debug.Log($"[{index}] {card.CardName} / {card.InstanceId} / Cost {card.CurrentCost}", this); // 카드 정보 출력
        }
    }

    private void LogPileState() // 카드 더미 상태 출력
    {
        Debug.Log($"Draw {drawPile.Count} / Hand {hand.Count} / Discard {discardPile.Count} / Exhaust {exhaustPile.Count}", this); // 더미별 카드 수 출력
    }

    private void NotifyCardPilesChanged() // 카드 더미 변경 전달
    {
        CardPilesChanged?.Invoke(); // 연결된 UI에 변경 알림
    }
}