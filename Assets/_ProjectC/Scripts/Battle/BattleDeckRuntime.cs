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
    public bool HasDrawableCards => CountDrawable(drawPile) > 0 || CountDrawable(discardPile) > 0; // 생존 소유자 카드의 드로우 가능 여부
    public int MaxHandSize { get; } // 최대 손패 수
    public event Action StateChanged; // 덱 상태 변경 이벤트
    public event Action<CardInstance> CardRecovered; // 버린 카드 회수 이벤트
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
        return Create(deckData, deckData.Cards, allyUnits, maxHandSize, shuffleSeed); // 원본 카드 목록 기반 생성 반환
    } // 덱 생성 종료
    public static BattleDeckRuntime Create(DeckData deckData, IReadOnlyList<IDeckCardEntry> deckEntries, IReadOnlyList<BattleUnitRuntime> allyUnits, int maxHandSize = 5, int? shuffleSeed = null) // 보유 카드 목록 기반 런타임 덱 생성
    { // 보유 카드 덱 생성 시작
        if (deckData == null) // 덱 원본 누락 확인
        { // 덱 누락 처리 시작
            throw new ArgumentNullException(nameof(deckData)); // 덱 누락 예외
        } // 덱 누락 처리 종료
        if (deckEntries == null) // 보유 카드 목록 존재 확인
        { // 잘못된 카드 목록 처리 시작
            throw new ArgumentException("유효하지 않은 보유 카드 목록입니다.", nameof(deckEntries)); // 잘못된 카드 목록 예외
        } // 잘못된 카드 목록 처리 종료
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
        foreach (IDeckCardEntry entry in deckEntries) // 현재 보유 카드 순회
        { // 카드 변환 시작
            if (entry == null || !entry.IsValid()) // 보유 카드 항목 유효성 확인
            { // 잘못된 카드 항목 처리 시작
                throw new ArgumentException("보유 카드 목록에 잘못된 항목이 있습니다.", nameof(deckEntries)); // 잘못된 카드 항목 예외
            } // 잘못된 카드 항목 처리 종료
            if (!ownerUnits.TryGetValue(entry.Owner, out BattleUnitRuntime ownerUnit)) // 소유 전투 유닛 검색
            { // 소유자 없음 처리 시작
                throw new InvalidOperationException($"카드 소유자 {entry.Owner.DisplayName}의 전투 유닛을 찾을 수 없습니다."); // 소유자 연결 실패 예외
            } // 소유자 없음 처리 종료
            CardInstance cardInstance = CardInstance.Create(entry.Card, ownerUnit, sequence); // 카드 인스턴스 생성
            runtimeDeck.cards.Add(cardInstance); // 전체 카드 목록 등록
            sequence++; // 다음 카드 순번 이동
        } // 카드 변환 종료
        if (runtimeDeck.CardCount != deckEntries.Count) // 보유 카드 수 일치 확인
        { // 카드 수 오류 처리 시작
            throw new InvalidOperationException("보유 덱과 런타임 덱의 카드 수가 일치하지 않습니다."); // 카드 수 불일치 예외
        } // 카드 수 오류 처리 종료
        runtimeDeck.drawPile.AddRange(runtimeDeck.cards); // 전체 카드를 뽑을 더미로 이동
        runtimeDeck.ShuffleList(runtimeDeck.drawPile); // 시작 카드 더미 셔플
        runtimeDeck.EnsureValidState(); // 시작 덱 상태 검증
        return runtimeDeck; // 생성된 런타임 덱 반환
    } // 보유 카드 덱 생성 종료
    public int DrawCards(int requestedCount) // 지정 수만큼 카드 드로우
    { // 카드 드로우 시작
        if (requestedCount <= 0) // 잘못된 요청 수 확인
        { // 잘못된 요청 처리 시작
            return 0; // 드로우 없음 반환
        } // 잘못된 요청 처리 종료
        int drawnCount = 0; // 실제 드로우 수 초기화
        int processedCount = 0; // 처리한 카드 수 초기화
        List<CardInstance> overflowCards = new List<CardInstance>(); // 초과 카드 임시 목록
        while (processedCount < requestedCount) // 요청 수까지 반복
        { // 반복 드로우 시작
            int topIndex = FindTopDrawableIndex(); // 사망 소유자를 건너뛴 맨 위 카드 조회
            if (topIndex < 0 && RefillDrawPile()) // 드로우 가능 카드 부족 시 버린 더미 재구성
            { // 재구성 카드 확인 시작
                topIndex = FindTopDrawableIndex(); // 재구성 후 유효 카드 재조회
            } // 재구성 카드 확인 종료
            if (topIndex < 0) // 더 이상 드로우할 생존 카드 확인
            { // 드로우 불가 처리 시작
                break; // 반복 드로우 중단
            } // 드로우 불가 처리 종료
            CardInstance drawnCard = drawPile[topIndex]; // 맨 위 카드 선택
            drawPile.RemoveAt(topIndex); // 뽑을 카드 더미에서 제거
            if (hand.Count < MaxHandSize) // 손패 공간 확인
            { // 손패 추가 시작
                hand.Add(drawnCard); // 손패에 카드 추가
                drawnCount++; // 실제 드로우 수 증가
            } // 손패 추가 종료
            else // 손패 가득 참 확인
            { // 초과 카드 처리 시작
                overflowCards.Add(drawnCard); // 초과 카드 임시 보관
            } // 초과 카드 처리 종료
            processedCount++; // 처리 카드 수 증가
        } // 반복 드로우 종료
        discardPile.AddRange(overflowCards); // 초과 카드 버린 더미 이동
        EnsureValidState(); // 드로우 후 덱 상태 검증
        if (processedCount > 0) // 카드 이동 여부 확인
        { // 상태 알림 시작
            StateChanged?.Invoke(); // 덱 상태 변경 알림
        } // 상태 알림 종료
        return drawnCount; // 실제 드로우 수 반환
    } // 카드 드로우 종료
    public IReadOnlyList<CardInstance> PeekDrawTop(int maximumChoices) // 현재 덱 위의 생존 카드 확인
    { // 덱 위 확인 시작
        List<CardInstance> choices = new List<CardInstance>(); // 덱 위 후보 목록 생성
        for (int index = drawPile.Count - 1; index >= 0 && choices.Count < maximumChoices; index--) // 맨 위부터 후보 수까지 순회
        { // 카드 후보 확인 시작
            CardInstance candidate = drawPile[index]; // 현재 덱 카드 조회
            if (IsDrawable(candidate)) // 생존 소유자 카드 확인
            { // 후보 추가 시작
                choices.Add(candidate); // 덱 위 순서대로 후보 추가
            } // 후보 추가 종료
        } // 카드 후보 확인 종료
        return choices.AsReadOnly(); // 외부 수정이 불가능한 후보 반환
    } // 덱 위 확인 종료
    public bool TryMoveDrawTop(CardInstance choice, int maximumChoices) // 선택 후보를 덱 맨 위로 이동
    { // 덱 순서 변경 시작
        if (choice == null || maximumChoices <= 0) // 선택 카드와 후보 수 확인
        { // 잘못된 선택 처리 시작
            return false; // 변경 실패 반환
        } // 잘못된 선택 처리 종료
        IReadOnlyList<CardInstance> choices = PeekDrawTop(maximumChoices); // 현재 덱 위 후보 재확인
        bool found = false; // 선택 카드 포함 여부 초기화
        foreach (CardInstance candidate in choices) // 현재 후보 순회
        { // 선택 카드 비교 시작
            found |= candidate == choice; // 선택 카드 일치 기록
        } // 선택 카드 비교 종료
        if (!found || !drawPile.Remove(choice)) // 유효 후보와 실제 덱 위치 확인
        { // 후보 이탈 처리 시작
            return false; // 변경 실패 반환
        } // 후보 이탈 처리 종료
        drawPile.Add(choice); // 선택 카드를 덱 맨 위에 배치
        EnsureValidState(); // 카드 영역 보존 확인
        StateChanged?.Invoke(); // 변경된 덱 상태 알림
        return true; // 변경 성공 반환
    } // 덱 순서 변경 종료
    public IReadOnlyList<CardInstance> PrepareDrawChoices(int maximumChoices) // 덱 위 선택 후보 준비
    { // 선택 후보 준비 시작
        List<CardInstance> choices = new List<CardInstance>(); // 빈 선택 후보 목록 생성
        if (maximumChoices <= 0) // 잘못된 후보 수 확인
        { // 후보 없음 처리 시작
            return choices.AsReadOnly(); // 빈 후보 목록 반환
        } // 후보 없음 처리 종료
        if (CountDrawable(drawPile) < maximumChoices && CountDrawable(discardPile) > 0) // 생존 후보 부족과 버린 카드 확인
        { // 버린 카드 재구성 시작
            List<CardInstance> recycled = new List<CardInstance>(discardPile); // 버린 카드 복사
            ShuffleList(recycled); // 재구성 카드 순서 섞기
            drawPile.InsertRange(0, recycled); // 기존 카드 아래에 재구성 카드 배치
            discardPile.Clear(); // 버린 카드 더미 비우기
            EnsureValidState(); // 재구성 후 덱 상태 검증
            StateChanged?.Invoke(); // 카드 영역 변경 알림
        } // 버린 카드 재구성 종료
        for (int index = drawPile.Count - 1; index >= 0 && choices.Count < maximumChoices; index--) // 덱 맨 위부터 유효 후보 순회
        { // 후보 추가 시작
            CardInstance candidate = drawPile[index]; // 현재 덱 후보 조회
            if (IsDrawable(candidate)) // 카드 소유자의 생존 상태 확인
            { // 생존 후보 추가 시작
                choices.Add(candidate); // 유효 카드만 맨 위 순서로 추가
            } // 생존 후보 추가 종료
        } // 후보 추가 종료
        return choices.AsReadOnly(); // 외부 변경을 막은 후보 목록 반환
    } // 선택 후보 준비 종료
    public bool TryDrawChoice(CardInstance choice, int maximumChoices) // 덱 위 후보 하나 선택 드로우
    { // 선택 드로우 시작
        if (choice == null || maximumChoices <= 0) // 선택 카드와 후보 수 확인
        { // 잘못된 선택 처리 시작
            return false; // 선택 실패 반환
        } // 잘못된 선택 처리 종료
        int index = -1; // 유효한 선택 카드 위치 초기화
        int eligibleSeen = 0; // 생존 후보 확인 수 초기화
        for (int candidateIndex = drawPile.Count - 1; candidateIndex >= 0 && eligibleSeen < maximumChoices; candidateIndex--) // 맨 위 유효 후보 순회
        { // 유효 후보 검사 시작
            CardInstance candidate = drawPile[candidateIndex]; // 현재 후보 카드 조회
            if (!IsDrawable(candidate)) // 소유자 사망 여부 확인
            { // 사망 카드 건너뛰기 시작
                continue; // 다음 유효 후보 확인
            } // 사망 카드 건너뛰기 종료
            eligibleSeen++; // 유효 후보 확인 수 증가
            if (candidate == choice) // 선택 카드 일치 확인
            { // 선택 카드 확인 시작
                index = candidateIndex; // 선택 카드 위치 기록
                break; // 후보 검색 종료
            } // 선택 카드 확인 종료
        } // 유효 후보 검사 종료
        if (index < 0) // 현재 유효 후보 범위 확인
        { // 후보 이탈 처리 시작
            return false; // 선택 실패 반환
        } // 후보 이탈 처리 종료
        drawPile.RemoveAt(index); // 뽑을 카드 더미에서 선택 카드 제거
        if (hand.Count < MaxHandSize) // 손패 공간 확인
        { // 정상 드로우 시작
            hand.Add(choice); // 선택 카드를 손패에 추가
        } // 정상 드로우 종료
        else // 손패 가득 참 확인
        { // 초과 카드 처리 시작
            discardPile.Add(choice); // 선택 카드를 버린 더미로 이동
        } // 초과 카드 처리 종료
        EnsureValidState(); // 선택 후 덱 상태 검증
        StateChanged?.Invoke(); // 카드 영역 변경 알림
        return true; // 선택 처리 성공 반환
    } // 선택 드로우 종료
    public bool TryDiscardDrawTop(CardInstance choice) // 뽑을 더미 맨 위 카드 버리기
    { // 맨 위 카드 버리기 시작
        int topIndex = FindTopDrawableIndex(); // 사망 카드를 제외한 맨 위 위치 조회
        if (choice == null || topIndex < 0 || drawPile[topIndex] != choice) // 현재 맨 위 유효 카드 확인
        { // 버리기 불가 처리 시작
            return false; // 버리기 실패 반환
        } // 버리기 불가 처리 종료
        drawPile.RemoveAt(topIndex); // 뽑을 더미의 맨 위 유효 카드 제거
        discardPile.Add(choice); // 버린 카드 더미에 추가
        EnsureValidState(); // 카드 영역 상태 검증
        StateChanged?.Invoke(); // 카드 영역 변경 알림
        return true; // 버리기 성공 반환
    } // 맨 위 카드 버리기 종료
    public bool IsTopDrawable(CardInstance card) // 현재 맨 위 유효 카드 확인
    { // 맨 위 유효 카드 확인 시작
        int index = FindTopDrawableIndex(); // 생존 소유자의 맨 위 카드 위치 조회
        return card != null && index >= 0 && drawPile[index] == card; // 현재 맨 위 유효 카드 일치 여부 반환
    } // 맨 위 유효 카드 확인 종료
    public bool TryRecoverDiscard(CardInstance choice) // 버린 카드 한 장 회수 시도
    { // 카드 회수 시작
        if (choice == null || hand.Count >= MaxHandSize || !discardPile.Contains(choice)) // 카드 위치와 손패 공간 확인
        { // 회수 불가 처리 시작
            return false; // 회수 실패 반환
        } // 회수 불가 처리 종료
        discardPile.Remove(choice); // 버린 더미에서 카드 제거
        hand.Add(choice); // 같은 카드를 손패에 추가
        CardRecovered?.Invoke(choice); // 카드 회수 효과 처리 알림
        EnsureValidState(); // 회수 후 덱 상태 검증
        StateChanged?.Invoke(); // 카드 영역 변경 알림
        return true; // 회수 성공 반환
    } // 카드 회수 종료
    public void ClearTemporaryCardCosts() // 모든 카드의 턴 한정 비용 초기화
    { // 임시 비용 초기화 시작
        bool changed = false; // 비용 변경 여부 초기화
        foreach (CardInstance card in cards) // 전투 카드 전체 순회
        { // 카드 비용 초기화 시작
            changed |= card.ClearTemporaryApDiscount(); // 카드별 임시 할인 제거
        } // 카드 비용 초기화 종료
        if (changed) // 실제 비용 변경 확인
        { // 변경 알림 시작
            StateChanged?.Invoke(); // 손패 비용 화면 갱신 알림
        } // 변경 알림 종료
    } // 임시 비용 초기화 종료
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
        if (CountDrawable(discardPile) < 1) // 버린 더미의 생존 소유자 카드 확인
        { // 버린 카드 없음 처리 시작
            return false; // 재구성 실패 반환
        } // 버린 카드 없음 처리 종료
        List<CardInstance> recycled = new List<CardInstance>(discardPile); // 재구성할 버린 카드 복사
        ShuffleList(recycled); // 재구성 카드 순서 섞기
        drawPile.InsertRange(0, recycled); // 기존 사망 카드 아래에 재구성 카드 배치
        discardPile.Clear(); // 버린 카드 더미 비우기
        return true; // 재구성 성공 반환
    } // 카드 더미 재구성 종료
    private int FindTopDrawableIndex() // 사망 카드를 제외한 맨 위 카드 위치 조회
    { // 맨 위 유효 카드 검색 시작
        for (int index = drawPile.Count - 1; index >= 0; index--) // 덱 맨 위부터 순회
        { // 덱 카드 확인 시작
            if (IsDrawable(drawPile[index])) // 카드 소유자 생존 여부 확인
            { // 유효 카드 반환 시작
                return index; // 맨 위 유효 카드 위치 반환
            } // 유효 카드 반환 종료
        } // 덱 카드 확인 종료
        return -1; // 유효한 카드 없음 반환
    } // 맨 위 유효 카드 검색 종료
    private static int CountDrawable(IReadOnlyList<CardInstance> pile) // 생존 소유자의 카드 수 계산
    { // 유효 카드 수 계산 시작
        int count = 0; // 유효 카드 수 초기화
        foreach (CardInstance candidate in pile) // 카드 더미 순회
        { // 소유자 생존 확인 시작
            if (IsDrawable(candidate)) // 유효 카드 여부 확인
            { // 유효 카드 집계 시작
                count++; // 유효 카드 수 증가
            } // 유효 카드 집계 종료
        } // 소유자 생존 확인 종료
        return count; // 유효 카드 수 반환
    } // 유효 카드 수 계산 종료
    private static bool IsDrawable(CardInstance candidate) // 카드 소유자의 드로우 가능 여부 확인
    { // 카드 드로우 조건 시작
        return candidate != null && candidate.OwnerUnit != null && !candidate.OwnerUnit.IsDead; // 카드와 소유자 생존 여부 반환
    } // 카드 드로우 조건 종료
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
