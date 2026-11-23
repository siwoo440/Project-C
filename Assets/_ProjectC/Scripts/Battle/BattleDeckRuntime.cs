using System; // 기본 예외와 무작위 기능 사용
using System.Collections.Generic; // 목록과 집합 자료형 사용
public sealed class BattleDeckRuntime // 전투용 런타임 덱
{ // 클래스 시작
    private readonly List<CardInstance> cards = new List<CardInstance>(); // 전체 카드 목록
    private readonly List<CardInstance> drawPile = new List<CardInstance>(); // 뽑을 카드 더미
    private readonly List<CardInstance> hand = new List<CardInstance>(); // 현재 손패
    private readonly List<CardInstance> discardPile = new List<CardInstance>(); // 버린 카드 더미
    private readonly Random random; // 셔플 무작위 생성기
    public DeckData SourceData { get; } // 덱 원본 데이터
    public IReadOnlyList<CardInstance> Cards => cards; // 전체 카드 목록 조회
    public IReadOnlyList<CardInstance> DrawPile => drawPile; // 뽑을 카드 더미 조회
    public IReadOnlyList<CardInstance> Hand => hand; // 현재 손패 조회
    public IReadOnlyList<CardInstance> DiscardPile => discardPile; // 버린 카드 더미 조회
    public int CardCount => cards.Count; // 전체 카드 수 조회
    public int DrawPileCount => drawPile.Count; // 뽑을 카드 수 조회
    public int HandCount => hand.Count; // 손패 카드 수 조회
    public int DiscardPileCount => discardPile.Count; // 버린 카드 수 조회
    public int MaxHandSize { get; } // 최대 손패 수
    public event Action StateChanged; // 덱 상태 변경 이벤트
    private BattleDeckRuntime(DeckData sourceData, int maxHandSize, int? shuffleSeed) // 런타임 덱 생성자
    { // 생성자 시작
        SourceData = sourceData; // 덱 원본 저장
        MaxHandSize = maxHandSize; // 최대 손패 수 저장
        random = shuffleSeed.HasValue ? new Random(shuffleSeed.Value) : new Random(); // 셔플 무작위 생성기 준비
    } // 생성자 종료
    public static BattleDeckRuntime Create(DeckData deckData, IReadOnlyList<BattleUnitRuntime> allyUnits, int maxHandSize = 5, int? shuffleSeed = null) // 런타임 덱 생성
    { // 덱 생성 시작
        if (deckData == null) // 덱 원본 누락 확인
        { // 덱 누락 처리 시작
            throw new ArgumentNullException(nameof(deckData)); // 덱 누락 예외
        } // 덱 누락 처리 종료
        if (!deckData.IsValidDeck()) // 덱 유효성 확인
        { // 잘못된 덱 처리 시작
            throw new ArgumentException("유효하지 않은 덱 데이터입니다.", nameof(deckData)); // 잘못된 덱 예외
        } // 잘못된 덱 처리 종료
        if (allyUnits == null) // 아군 목록 누락 확인
        { // 아군 누락 처리 시작
            throw new ArgumentNullException(nameof(allyUnits)); // 아군 누락 예외
        } // 아군 누락 처리 종료
        if (maxHandSize < 1) // 최대 손패 범위 확인
        { // 잘못된 손패 처리 시작
            throw new ArgumentOutOfRangeException(nameof(maxHandSize)); // 잘못된 손패 예외
        } // 잘못된 손패 처리 종료
        Dictionary<CharacterData, BattleUnitRuntime> ownerUnits = CreateOwnerMap(allyUnits); // 카드 소유자 검색표 생성
        BattleDeckRuntime runtimeDeck = new BattleDeckRuntime(deckData, maxHandSize, shuffleSeed); // 빈 런타임 덱 생성
        int sequence = 0; // 카드 순번 초기화
        foreach (DeckCardEntry entry in deckData.Cards) // 원본 덱 카드 순회
        { // 카드 변환 시작
            if (!ownerUnits.TryGetValue(entry.Owner, out BattleUnitRuntime ownerUnit)) // 소유 전투 유닛 검색
            { // 소유자 없음 처리 시작
                throw new InvalidOperationException($"카드 소유자 {entry.Owner.DisplayName}의 전투 유닛을 찾을 수 없습니다."); // 소유자 연결 실패 예외
            } // 소유자 없음 처리 종료
            CardInstance cardInstance = CardInstance.Create(entry.Card, ownerUnit, sequence); // 카드 인스턴스 생성
            runtimeDeck.cards.Add(cardInstance); // 전체 카드 목록 등록
            sequence++; // 다음 카드 순번 이동
        } // 카드 변환 종료
        if (runtimeDeck.CardCount != deckData.CardCount) // 카드 수 일치 확인
        { // 카드 수 오류 처리 시작
            throw new InvalidOperationException("원본 덱과 런타임 덱의 카드 수가 일치하지 않습니다."); // 카드 수 불일치 예외
        } // 카드 수 오류 처리 종료
        runtimeDeck.drawPile.AddRange(runtimeDeck.cards); // 전체 카드를 뽑을 더미로 이동
        runtimeDeck.ShuffleList(runtimeDeck.drawPile); // 시작 카드 더미 셔플
        runtimeDeck.EnsureValidState(); // 시작 덱 상태 검증
        return runtimeDeck; // 생성된 런타임 덱 반환
    } // 덱 생성 종료
    public int DrawCards(int requestedCount) // 지정 수만큼 카드 드로우
    { // 카드 드로우 시작
        if (requestedCount <= 0) // 잘못된 요청 수 확인
        { // 잘못된 요청 처리 시작
            return 0; // 드로우 없음 반환
        } // 잘못된 요청 처리 종료
        int availableHandSpace = MaxHandSize - hand.Count; // 남은 손패 공간 계산
        int targetDrawCount = Math.Min(requestedCount, availableHandSpace); // 실제 목표 드로우 수 계산
        int drawnCount = 0; // 실제 드로우 수 초기화
        while (drawnCount < targetDrawCount) // 목표 수까지 반복
        { // 반복 드로우 시작
            if (drawPile.Count < 1 && !RefillDrawPile()) // 빈 카드 더미 재구성 시도
            { // 드로우 불가 처리 시작
                break; // 반복 드로우 중단
            } // 드로우 불가 처리 종료
            int topIndex = drawPile.Count - 1; // 카드 더미 맨 위 위치 계산
            CardInstance drawnCard = drawPile[topIndex]; // 맨 위 카드 선택
            drawPile.RemoveAt(topIndex); // 뽑을 카드 더미에서 제거
            hand.Add(drawnCard); // 손패에 카드 추가
            drawnCount++; // 실제 드로우 수 증가
        } // 반복 드로우 종료
        EnsureValidState(); // 드로우 후 덱 상태 검증
        if (drawnCount > 0) // 실제 드로우 확인
        { // 상태 알림 시작
            StateChanged?.Invoke(); // 덱 상태 변경 알림
        } // 상태 알림 종료
        return drawnCount; // 실제 드로우 수 반환
    } // 카드 드로우 종료
    public bool DiscardCard(CardInstance cardInstance) // 손패 카드 버리기
    { // 카드 버리기 시작
        if (cardInstance == null) // 빈 카드 확인
        { // 빈 카드 처리 시작
            return false; // 버리기 실패 반환
        } // 빈 카드 처리 종료
        if (!hand.Remove(cardInstance)) // 손패 카드 제거 시도
        { // 손패에 없음 처리 시작
            return false; // 버리기 실패 반환
        } // 손패에 없음 처리 종료
        discardPile.Add(cardInstance); // 버린 카드 더미에 추가
        EnsureValidState(); // 버리기 후 덱 상태 검증
        StateChanged?.Invoke(); // 덱 상태 변경 알림
        return true; // 버리기 성공 반환
    } // 카드 버리기 종료
    public void ShuffleDrawPile() // 뽑을 카드 더미 셔플
    { // 카드 더미 셔플 시작
        ShuffleList(drawPile); // 뽑을 카드 순서 변경
        EnsureValidState(); // 셔플 후 덱 상태 검증
        StateChanged?.Invoke(); // 덱 상태 변경 알림
    } // 카드 더미 셔플 종료
    public bool IsStateValid() // 현재 덱 상태 유효성 확인
    { // 상태 검사 시작
        int locatedCardCount = drawPile.Count + hand.Count + discardPile.Count; // 영역별 카드 총수 계산
        if (locatedCardCount != cards.Count) // 전체 카드 수 일치 확인
        { // 카드 수 불일치 처리 시작
            return false; // 잘못된 상태 반환
        } // 카드 수 불일치 처리 종료
        HashSet<CardInstance> locatedCards = new HashSet<CardInstance>(); // 영역 등록 카드 집합 생성
        if (!AddZoneCards(drawPile, locatedCards)) // 뽑을 카드 중복 확인
        { // 뽑을 카드 오류 처리 시작
            return false; // 잘못된 상태 반환
        } // 뽑을 카드 오류 처리 종료
        if (!AddZoneCards(hand, locatedCards)) // 손패 카드 중복 확인
        { // 손패 카드 오류 처리 시작
            return false; // 잘못된 상태 반환
        } // 손패 카드 오류 처리 종료
        if (!AddZoneCards(discardPile, locatedCards)) // 버린 카드 중복 확인
        { // 버린 카드 오류 처리 시작
            return false; // 잘못된 상태 반환
        } // 버린 카드 오류 처리 종료
        if (locatedCards.Count != cards.Count) // 고유 카드 수 일치 확인
        { // 고유 카드 오류 처리 시작
            return false; // 잘못된 상태 반환
        } // 고유 카드 오류 처리 종료
        foreach (CardInstance cardInstance in cards) // 전체 카드 목록 순회
        { // 전체 카드 검사 시작
            if (cardInstance == null || !locatedCards.Contains(cardInstance)) // 카드 위치 존재 확인
            { // 카드 위치 오류 처리 시작
                return false; // 잘못된 상태 반환
            } // 카드 위치 오류 처리 종료
        } // 전체 카드 검사 종료
        return hand.Count <= MaxHandSize; // 최대 손패 범위 결과 반환
    } // 상태 검사 종료
    private bool RefillDrawPile() // 버린 카드 더미 재구성
    { // 카드 더미 재구성 시작
        if (discardPile.Count < 1) // 버린 카드 존재 확인
        { // 버린 카드 없음 처리 시작
            return false; // 재구성 실패 반환
        } // 버린 카드 없음 처리 종료
        drawPile.AddRange(discardPile); // 버린 카드를 뽑을 더미로 이동
        discardPile.Clear(); // 버린 카드 더미 비우기
        ShuffleList(drawPile); // 재구성된 카드 더미 셔플
        return true; // 재구성 성공 반환
    } // 카드 더미 재구성 종료
    private void ShuffleList(List<CardInstance> targetCards) // 카드 목록 셔플
    { // 셔플 시작
        for (int index = targetCards.Count - 1; index > 0; index--) // 뒤에서 앞으로 카드 순회
        { // 카드 교환 시작
            int randomIndex = random.Next(index + 1); // 교환할 무작위 위치 선택
            CardInstance temporaryCard = targetCards[index]; // 현재 카드 임시 저장
            targetCards[index] = targetCards[randomIndex]; // 무작위 카드를 현재 위치로 이동
            targetCards[randomIndex] = temporaryCard; // 현재 카드를 무작위 위치로 이동
        } // 카드 교환 종료
    } // 셔플 종료
    private void EnsureValidState() // 덱 상태 강제 검증
    { // 강제 검증 시작
        if (!IsStateValid()) // 덱 상태 유효성 확인
        { // 잘못된 상태 처리 시작
            throw new InvalidOperationException("런타임 덱의 카드 상태가 올바르지 않습니다."); // 덱 상태 오류 예외
        } // 잘못된 상태 처리 종료
    } // 강제 검증 종료
    private static bool AddZoneCards(IReadOnlyList<CardInstance> zoneCards, HashSet<CardInstance> locatedCards) // 카드 영역 집합 등록
    { // 영역 등록 시작
        foreach (CardInstance cardInstance in zoneCards) // 영역 카드 순회
        { // 카드 등록 시작
            if (cardInstance == null || !locatedCards.Add(cardInstance)) // 빈 카드 또는 중복 카드 확인
            { // 카드 등록 실패 처리 시작
                return false; // 영역 등록 실패 반환
            } // 카드 등록 실패 처리 종료
        } // 카드 등록 종료
        return true; // 영역 등록 성공 반환
    } // 영역 등록 종료
    private static Dictionary<CharacterData, BattleUnitRuntime> CreateOwnerMap(IReadOnlyList<BattleUnitRuntime> allyUnits) // 카드 소유자 검색표 생성
    { // 검색표 생성 시작
        Dictionary<CharacterData, BattleUnitRuntime> ownerUnits = new Dictionary<CharacterData, BattleUnitRuntime>(); // 빈 소유자 검색표 생성
        foreach (BattleUnitRuntime allyUnit in allyUnits) // 아군 전투 유닛 순회
        { // 아군 등록 시작
            if (allyUnit == null) // 빈 아군 확인
            { // 빈 아군 처리 시작
                throw new ArgumentException("아군 목록에 빈 전투 유닛이 있습니다.", nameof(allyUnits)); // 빈 아군 예외
            } // 빈 아군 처리 종료
            if (allyUnit.Team != BattleTeam.Ally || allyUnit.CharacterSource == null) // 아군 원본 연결 확인
            { // 잘못된 아군 처리 시작
                throw new ArgumentException("카드 소유자로 사용할 수 없는 전투 유닛이 있습니다.", nameof(allyUnits)); // 잘못된 아군 예외
            } // 잘못된 아군 처리 종료
            if (ownerUnits.ContainsKey(allyUnit.CharacterSource)) // 동일 소유자 중복 확인
            { // 중복 소유자 처리 시작
                throw new InvalidOperationException($"아군 전투 유닛 {allyUnit.DisplayName}이 중복 생성되었습니다."); // 중복 소유자 예외
            } // 중복 소유자 처리 종료
            ownerUnits.Add(allyUnit.CharacterSource, allyUnit); // 소유자와 전투 유닛 연결
        } // 아군 등록 종료
        return ownerUnits; // 완성된 검색표 반환
    } // 검색표 생성 종료
} // 클래스 종료
