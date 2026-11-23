using System; // 기본 예외와 무작위 기능 사용
using System.Collections.Generic; // 목록과 집합 자료형 사용
public sealed class BattleDeckRuntime // 전투용 런타임 덱
{ // 클래스 시작
    private readonly List<CardInstance> cards = new List<CardInstance>(); // 전체 카드 목록
    private readonly List<CardInstance> drawPile = new List<CardInstance>(); // 뽑을 카드 더미
    private readonly List<CardInstance> hand = new List<CardInstance>(); // 현재 손패
    private readonly List<CardInstance> discardPile = new List<CardInstance>(); // 버린 카드 더미
    private readonly List<CardInstance> excludedPile = new List<CardInstance>(); // 이번 전투에서만 제외된 카드 목록
    private int relicApDiscount; // 현재 턴의 모든 카드에 적용하는 비용 감소량
    private int nextCardRelicApDiscount; // 다음 유료 카드 한 장에 적용하는 비용 감소량
    private int firstDefensiveRelicApDiscount; // 첫 유료 방어 카드에 남은 전투 한정 비용 감소량
    private bool firstDrawCompleted; // 이번 전투 첫 실제 손패 드로우 기록
    private bool discardedUnplayableDraw; // 봉인테이프의 전투 발동 기록
    private bool recoveredRandomDiscard; // 회수용 자석의 전투 발동 기록
    private CardInstance firstDrawDiscountCard; // 현재 턴에 개별 드로우 할인을 받은 카드
    private CardInstance openingRandomDiscountCard; // 퇴색한 왕관이 첫 손패에서 선택한 카드
    private CardInstance openingRetainedCard; // 핸드벨이 첫 손패에서 유지할 카드
    internal event Action<CardInstance, bool> CardExcluded; // 실제 제외 카드와 휘발 여부 전달
    public IReadOnlyList<CardInstance> ExcludedPile => excludedPile; // 전투 한정 제외 목록 조회
    private readonly Random random; // 셔플 무작위 생성기
    private CardInstance battleCopy; // 전투 종료 시 모든 더미에서 지울 복사본
    private CardInstance pendingHandDiscard; // 현재 한 번의 버림 판정을 기다리는 손패
    private int nextPlayerTurnDraw; // 다음 아군 턴 시작에 추가할 드로우 수
    internal int HandCountBeforeCleanup { get; private set; } // 자동 버리기 전의 실제 손패 수
    internal event Action<CardInstance> BeforeHandDiscard; // 효과 버림과 턴 종료 버림 직전 신호
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
        int bonus = BattleSupplyRelics.Find(RelicEffectType.ExtraHandCapacity)?.EffectValue ?? 0; // 전투에 가져온 얇은 금속 실의 손패 가산량
        MaxHandSize = (int)Math.Min(int.MaxValue, (long)maxHandSize + bonus); // 전투가 끝날 때까지 유지할 최대 손패 수 저장
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
            CardInstance cardInstance = entry is RunDeckCardEntry runCard // 전달된 회차 카드의 개별 강화 단계 확인
                ? CardInstance.Create(entry.Card, ownerUnit, sequence, runCard.UpgradeLevel) // 실제 회차 카드의 강화 단계 반영
                : CardInstance.Create(entry.Card, ownerUnit, sequence); // 편성 원본만 전달된 기존 호출의 회차 강화 조회
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
    { // 일반 카드 드로우 연결 시작
        return DrawCards(requestedCount, 0, out _); // 기본 아군 턴 집계 없이 드로우 실행
    } // 일반 카드 드로우 연결 종료
    private int DrawCards(int requestedCount, int baseDrawLimit, out int baseDrawnCount) // 실제 손패 도착 및 기본 아군 턴 드로우 수 집계
    { // 카드 드로우 시작
        baseDrawnCount = 0; // 기본 드로우 실제 도착 수 초기화
        if (requestedCount <= 0) // 잘못된 요청 수 확인
        { // 잘못된 요청 처리 시작
            return 0; // 드로우 없음 반환
        } // 잘못된 요청 처리 종료
        int drawnCount = DrawCardsCore(requestedCount, out int processedCount, baseDrawLimit, out baseDrawnCount); // 같은 요청에서 일반 및 추가 드로우 구분
        EnsureValidState(); // 드로우 후 덱 상태 검증
        if (processedCount > 0) // 카드 이동 여부 확인
        { // 상태 알림 시작
            StateChanged?.Invoke(); // 덱 상태 변경 알림
        } // 상태 알림 종료
        return drawnCount; // 실제 드로우 수 반환
    } // 카드 드로우 종료
    public bool TryPlayAndDraw(CardInstance playedCard, int requestedCount, out int drawnCount) // 사용 카드를 제외하고 드로우
    { // 사용과 드로우 처리 시작
        drawnCount = 0; // 실제 드로우 수 초기화
        if (playedCard == null || !hand.Remove(playedCard)) // 사용 카드의 손패 위치 확인
        { // 사용 불가 처리 시작
            return false; // 사용 실패 반환
        } // 사용 불가 처리 종료
        try // 사용 카드 위치 복구 보장
        { // 드로우 처리 시작
            drawnCount = DrawCardsCore(Math.Max(0, requestedCount), out _, 0, out _); // 기본 턴 집계가 없는 카드 효과 드로우
        } // 드로우 처리 종료
        finally // 사용한 카드 이동 보장
        { // 사용 카드 이동 시작
            PlacePlayedCard(playedCard); // 드로우 완료 후 사용 카드의 버림 또는 제외 처리
        } // 사용 카드 이동 종료
        EnsureValidState(); // 전체 카드 위치와 손패 제한 검증
        StateChanged?.Invoke(); // 사용과 드로우 결과 알림
        return true; // 사용 성공 반환
    } // 사용과 드로우 처리 종료
    private int DrawCardsCore(int requestedCount, out int processedCount, int baseDrawLimit, out int baseDrawnCount) // 카드 영역 변경과 원래 기본 시도 안의 실제 도착 집계
    { // 공통 드로우 시작
        int drawnCount = 0; // 실제 드로우 수 초기화
        baseDrawnCount = 0; // 실제 기본 드로우 수 초기화
        processedCount = 0; // 처리한 카드 수 초기화
        List<CardInstance> discardedCards = new List<CardInstance>(); // 같은 요청의 재순환에서 제외할 카드 목록
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
            if (TryAddDrawnCardToHand(drawnCard)) // 상한과 봉인 판정 후 손패 이동 확인
            { // 손패 추가 시작
                drawnCount++; // 실제 드로우 수 증가
                if (processedCount < baseDrawLimit) // 보너스 이전의 원래 기본 시도 확인
                { // 기본 도착 수 집계 시작
                    baseDrawnCount++; // 유효 손패에 도착한 기본 드로우만 기록
                } // 기본 도착 수 집계 종료
            } // 손패 추가 종료
            else // 초과 또는 봉인 카드 확인
            { // 드로우 버림 처리 시작
                discardedCards.Add(drawnCard); // 현재 요청 중 같은 카드의 재드로우 방지
            } // 드로우 버림 처리 종료
            processedCount++; // 처리 카드 수 증가
        } // 반복 드로우 종료
        discardPile.AddRange(discardedCards); // 초과 및 봉인 카드의 버린 더미 이동
        return drawnCount; // 실제 드로우 수 반환
    } // 공통 드로우 종료
    private bool TryAddDrawnCardToHand(CardInstance card) // 손패 공간과 봉인 판정 및 최초 할인 처리
    { // 실제 손패 드로우 처리 시작
        if (hand.Count >= MaxHandSize) // 일반 초과 드로우 확인
        { // 손패 상한 처리 시작
            return false; // 봉인 기회를 유지한 채 일반 버림 요청
        } // 손패 상한 처리 종료
        if (!discardedUnplayableDraw && card.EffectType == CardEffectType.Unplayable && BattleSupplyRelics.Find(RelicEffectType.DiscardFirstUnplayableDraw) != null) // 손패로 들어갈 첫 사용불가 드로우 확인
        { // 봉인테이프 발동 시작
            discardedUnplayableDraw = true; // 같은 전투의 봉인 재발동 차단
            return false; // 현재 드로우 횟수의 버림 요청
        } // 봉인테이프 발동 종료
        hand.Add(card); // 손패에 드로우 카드 추가
        if (firstDrawCompleted) // 이번 전투 첫 손패 드로우 확인
        { // 최초 이후 드로우 처리 시작
            return true; // 이후 카드의 기존 비용 보존
        } // 최초 이후 드로우 처리 종료
        firstDrawCompleted = true; // 빈 덱과 손패 초과로 소비되지 않는 첫 드로우 기록
        int discount = BattleSupplyRelics.Find(RelicEffectType.FirstDrawCardDiscount)?.EffectValue ?? 0; // 보유한 첫 드로우 유물의 할인량
        if (discount > 0) // 적용할 실제 할인 확인
        { // 첫 카드 할인 시작
            firstDrawDiscountCard = card; // 턴 종료 때 복원할 카드 저장
            card.FirstDrawRelicApDiscount = discount; // 다른 카드와 별개의 턴 한정 비용 감소
        } // 첫 카드 할인 종료
        return true; // 실제 손패 드로우 완료
    } // 실제 손패 드로우 처리 종료
    internal void ClearFirstDrawRelicDiscount() // 종료된 턴의 첫 드로우 카드 할인 복원
    { // 첫 드로우 할인 정리 시작
        if (firstDrawDiscountCard == null) // 실제로 할인한 카드 확인
        { // 복원 대상 없음 처리 시작
            return; // 비용 변경 생략
        } // 복원 대상 없음 처리 종료
        firstDrawDiscountCard.FirstDrawRelicApDiscount = 0; // 카드 위치와 무관하게 개별 비용 복원
        firstDrawDiscountCard = null; // 종료된 턴의 할인 참조 해제
        StateChanged?.Invoke(); // 유지된 손패의 비용 표시 갱신
    } // 첫 드로우 할인 정리 종료
    internal void DiscountRandomOpeningHandCard(int amount) // 첫 손패의 유료 고정 카드 한 장 할인
    { // 왕관의 무작위 카드 선택 시작
        if (amount <= 0 || openingRandomDiscountCard != null) // 유효한 할인량과 첫 선택 여부 확인
        { // 선택 생략 처리 시작
            return; // 기존 손패 카드 비용 보존
        } // 선택 생략 처리 종료
        var candidates = hand.FindAll(card => IsDrawable(card) && card.EffectType != CardEffectType.Unplayable && !card.SourceData.SpendsAllActionPoints && card.ApCostBeforeNextCardDiscount > 0); // 생존 소유자의 줄일 수 있는 고정 비용 카드 후보
        if (candidates.Count == 0) // 줄일 수 있는 실제 손패 확인
        { // 후보 없음 처리 시작
            return; // 빈 손패나 무료 카드의 비용 보존
        } // 후보 없음 처리 종료
        openingRandomDiscountCard = candidates[random.Next(candidates.Count)]; // 기존 덱 무작위로 후보 하나 선택
        openingRandomDiscountCard.OpeningRandomRelicApDiscount = amount; // 선택 카드에 독립 첫 턴 할인 적용
        StateChanged?.Invoke(); // 할인된 카드의 손패 화면 갱신
    } // 왕관의 무작위 카드 선택 종료
    internal void ClearOpeningRandomRelicDiscount() // 맨 처음 아군 턴의 퇴색 왕관 비용 복원
    { // 왕관 비용 정리 시작
        if (openingRandomDiscountCard == null) // 실제 할인 카드 존재 확인
        { // 복원 없음 처리 시작
            return; // 다른 카드 할인 유지
        } // 복원 없음 처리 종료
        openingRandomDiscountCard.OpeningRandomRelicApDiscount = 0; // 카드 영역과 무관한 개별 비용 복원
        openingRandomDiscountCard = null; // 이전 턴의 할인 카드 참조 해제
        StateChanged?.Invoke(); // 유지된 손패의 비용 표시 갱신
    } // 왕관 비용 정리 종료
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
        if (!TryAddDrawnCardToHand(choice)) // 상한과 봉인 판정 후 버림 확인
        { // 초과 및 봉인 처리 시작
            discardPile.Add(choice); // 선택 카드를 버린 더미로 이동
        } // 초과 및 봉인 처리 종료
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
    internal bool TryRecoverRandomDiscardOnce() // 회수용 자석의 전투당 한 번 회수
    { // 무작위 회수 시작
        if (recoveredRandomDiscard || hand.Count >= MaxHandSize) // 사용 기회와 손패 공간 확인
        { // 회수 유예 시작
            return false; // 회수할 수 없는 상태 반환
        } // 회수 유예 종료
        List<CardInstance> candidates = discardPile.FindAll(IsDrawable); // 생존 소유자의 버린 카드 후보
        if (candidates.Count == 0) // 회수 후보 유무 확인
        { // 후보 없음 처리 시작
            return false; // 회수 기회 보존
        } // 후보 없음 처리 종료
        recoveredRandomDiscard = true; // 카드 회수 알림 전에 전투 기회 소비
        return TryRecoverDiscard(candidates[random.Next(candidates.Count)]); // 기존 무작위와 카드 객체의 회수 처리
    } // 무작위 회수 종료
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
    internal bool TryAddBattleCopy(CardInstance source) // 첫 사용 카드의 독립 전투용 복사본 생성
    { // 복사 시작
        if (source == null || battleCopy != null || hand.Count >= MaxHandSize || !hand.Contains(source)) // 복사할 손패와 빈자리 확인
        { // 복사 불가 시작
            return false; // 원래 손패와 미사용 기회 유지
        } // 복사 불가 종료
        battleCopy = CardInstance.Create(source.SourceData, source.OwnerUnit, cards.Count, source.UpgradeLevel); // 원래 비용과 소유자 및 강화 단계 복사
        battleCopy.RelicApDiscount = relicApDiscount; // 현재 첫 턴의 유물 할인 물려주기
        battleCopy.NextCardRelicApDiscount = nextCardRelicApDiscount; // 다음 카드 유물 할인 물려주기
        battleCopy.FirstDefensiveRelicApDiscount = firstDefensiveRelicApDiscount; // 아직 남은 전투의 첫 방어 할인 물려주기
        cards.Add(battleCopy); // 전투 전용 카드 등록
        hand.Add(battleCopy); // 복사본을 실제 손패에 추가
        StateChanged?.Invoke(); // 변경된 손패 표시
        return true; // 전투당 한 번 복사 성공
    } // 복사 종료
    internal void ClearBattleRelicCards() // 전투가 끝난 뒤 임시 카드와 예약 정리
    { // 전투 카드 정리 시작
        ClearFirstDrawRelicDiscount(); // 종료된 전투의 개별 카드 할인 복원
        ClearOpeningRandomRelicDiscount(); // 종료된 전투의 왕관 할인 복원
        SetFirstDefensiveRelicApDiscount(0); // 전투가 종료될 때 미사용 방어 할인 정리
        openingRetainedCard = null; // 전투를 끝낸 미사용 시작 손패 유지 해제
        nextPlayerTurnDraw = 0; // 미사용 드로우 예약 해제
        if (battleCopy != null && cards.Remove(battleCopy)) // 실제로 생성한 전투 한정 카드 확인
        { // 복사본 제거 시작
            hand.Remove(battleCopy); // 손패 복사본 제거
            drawPile.Remove(battleCopy); // 뽑을 더미 복사본 제거
            discardPile.Remove(battleCopy); // 버린 더미 복사본 제거
            excludedPile.Remove(battleCopy); // 제외된 전투 복사본도 정리
            battleCopy = null; // 제거한 전투 카드 참조 해제
            StateChanged?.Invoke(); // 실제 더미 변화 알림
        } // 복사본 제거 종료
    } // 전투 카드 정리 종료
    internal void QueuePlayerTurnDraw(int count) // 다음 아군 턴에 추가 드로우 보관
    { // 드로우 예약 시작
        nextPlayerTurnDraw = (int)Math.Clamp((long)nextPlayerTurnDraw + count, 0L, int.MaxValue); // 정수 범위에서 추가 드로우 합산
    } // 드로우 예약 종료
    internal int DrawForPlayerTurn(int baseCount, int additionalCount, out int baseDrawnCount) // 기본과 추가 및 예약 드로우를 한 번에 적용
    { // 턴 드로우 시작
        int bonus = nextPlayerTurnDraw; // 이번 턴 보너스 보관
        nextPlayerTurnDraw = 0; // 드로우 알림 전 중복 예약 소비 차단
        int requested = (int)Math.Clamp((long)baseCount + additionalCount + bonus, 0L, int.MaxValue); // 세 종류의 드로우를 안전한 한 요청으로 합산
        return DrawCards(requested, baseCount, out baseDrawnCount); // 카드가 손패에 도착한 실제 기본 및 전체 수 반환
    } // 턴 드로우 종료
    internal void RetainRandomOpeningHandCard() // 첫 실제 손패의 일반 버림을 한 번 면제할 카드 선택
    { // 핸드벨 카드 선택 시작
        var candidates = hand.FindAll(card => IsDrawable(card) && card.EffectType != CardEffectType.Unplayable && !card.SourceData.VolatileInHand); // 살아 있는 소유자의 사용 가능 일반 손패 후보
        if (candidates.Count > 0) // 선택할 실제 손패 존재 확인
        { // 균등 유지 대상 선택 시작
            openingRetainedCard = candidates[random.Next(candidates.Count)]; // 기존 덱 무작위로 같은 확률의 카드 한 장 저장
        } // 균등 유지 대상 선택 종료
    } // 핸드벨 카드 선택 종료
    private void ForgetOpeningRetention(CardInstance card) // 실제로 손패를 떠난 카드의 유물 유지 해제
    { // 시작 손패 유지 해제 확인 시작
        if (card == openingRetainedCard) // 핸드벨이 선택했던 인스턴스인지 확인
        { // 유지 해제 시작
            openingRetainedCard = null; // 이후 다시 뽑거나 회수한 카드의 예약 복구 차단
        } // 유지 해제 종료
    } // 시작 손패 유지 해제 확인 종료
    internal bool RetainHandCard(CardInstance card) // 현재 한 번의 손패 버림 취소
    { // 손패 유지 시작
        if (card == null || card != pendingHandDiscard) // 현재 버리려는 손패와 일치 여부 확인
        { // 손패 없음 시작
            return false; // 보존할 카드 없음
        } // 손패 없음 종료
        pendingHandDiscard = null; // 이번 버림 한 번만 취소
        return true; // 손패 유지 성공
    } // 손패 유지 종료
    private bool IsHandDiscardPrevented(CardInstance card) // 현재 버림 한 번의 유지 여부 확인
    { // 버림 판정 시작
        var previous = pendingHandDiscard; // 이벤트 처리 중 바깥 버림 대상 보관
        pendingHandDiscard = card; // 이번 버림 대상 지정
        try // 겹친 버림 판정 복구 보장
        { // 버림 신호 시작
            BeforeHandDiscard?.Invoke(card); // 효과 버림이나 턴 종료 버림 직전 알림
            return pendingHandDiscard == null; // 이번 신호에서 취소한 버림만 면제
        } // 버림 신호 종료
        finally // 한 번의 버림 판정 정리
        { // 대상 복구 시작
            pendingHandDiscard = previous; // 바깥 버림 대상 복구
        } // 대상 복구 종료
    } // 버림 판정 종료
    public bool DiscardCard(CardInstance cardInstance) // 카드 효과 또는 수동 선택으로 손패 버리기
    { // 효과 버림 시작
        if (cardInstance == null || !hand.Contains(cardInstance)) // 실제 버릴 카드 확인
        { // 유효 대상 없음 시작
            return false; // 잘못된 버리기 거부
        } // 유효 대상 없음 종료
        if (!IsHandDiscardPrevented(cardInstance)) // 실제 효과 버림 확인
        { // 손패 이동 시작
            hand.Remove(cardInstance); // 원래 손패에서 제거
            ForgetOpeningRetention(cardInstance); // 실제 수동 및 효과 버림으로 개시 유지 해제
            discardPile.Add(cardInstance); // 사용과 구분해 버림 더미에 추가
            EnsureValidState(); // 이동 뒤 카드 영역 확인
            StateChanged?.Invoke(); // 손패 변경 알림
        } // 손패 이동 종료
        return true; // 실제 버림 또는 유지 완료
    } // 효과 버림 종료
    internal bool DiscardPlayedCard(CardInstance cardInstance) // 사용을 끝낸 카드의 일반 버림 이동
    { // 카드 버리기 시작
        if (cardInstance == null) // 빈 카드 확인
        { // 빈 카드 처리 시작
            return false; // 버리기 실패 반환
        } // 빈 카드 처리 종료
        if (!hand.Remove(cardInstance)) // 손패 카드 제거 시도
        { // 손패에 없음 처리 시작
            return false; // 버리기 실패 반환
        } // 손패에 없음 처리 종료
        PlacePlayedCard(cardInstance); // 사용 카드의 버림 또는 제외 처리
        EnsureValidState(); // 버리기 후 덱 상태 검증
        StateChanged?.Invoke(); // 덱 상태 변경 알림
        return true; // 버리기 성공 반환
    } // 카드 버리기 종료
    private void PlacePlayedCard(CardInstance card) // 손패에서 빠진 사용 카드 정산
    { // 사용 위치 처리 시작
        ForgetOpeningRetention(card); // 정상 사용 정산 후 같은 카드의 개시 유지 해제
        if (card.SourceData.ExcludeOnUse) // 사용 후 제외 규칙 확인
        { // 전투 제외 시작
            excludedPile.Add(card); // 제외 더미에 사용 카드 보관
            CardExcluded?.Invoke(card, false); // 실제 제외 알림
            return; // 일반 버림 생략
        } // 전투 제외 종료
        discardPile.Add(card); // 일반 사용은 버림 더미에 보관
    } // 사용 위치 처리 종료
    internal void SetRelicApDiscount(int amount) // 현재 턴의 모든 카드 비용 갱신
    { // 유물 할인 시작
        if (amount == relicApDiscount) // 현재 표시와 같은 할인 확인
        { // 중복 갱신 방지 시작
            return; // 불필요한 비용 갱신 생략
        } // 중복 갱신 방지 종료
        relicApDiscount = amount; // 복사본에도 전달할 할인 기록
        foreach (CardInstance card in cards) // 현재 전투 카드 전체 조회
        { // 개별 비용 갱신 시작
            card.RelicApDiscount = amount; // 카드별 유물 할인 저장
        } // 개별 비용 갱신 종료
        StateChanged?.Invoke(); // 손패 비용 표시 갱신
    } // 유물 할인 종료
    internal void SetNextCardRelicApDiscount(int amount) // 첫 유료 사용 이후 다음 카드 할인 적용 또는 제거
    { // 다음 카드 할인 갱신 시작
        if (nextCardRelicApDiscount == amount) // 기존 표시와 같은 할인인지 확인
        { // 중복 갱신 방지 시작
            return; // 비용에 변화가 없으면 갱신 생략
        } // 중복 갱신 방지 종료
        nextCardRelicApDiscount = amount; // 복사본에도 적용할 다음 카드 비용 기록
        foreach (var card in cards) // 보관 중인 모든 카드에 다음 사용 할인 반영
        { // 카드의 다음 사용 표시 갱신 시작
            card.NextCardRelicApDiscount = amount; // 해당 카드가 다음 사용일 때의 유물 할인
        } // 카드의 다음 사용 표시 갱신 종료
        StateChanged?.Invoke(); // 현재 손패의 비용 숫자와 사용 가능 여부 갱신
    } // 다음 카드 할인 갱신 종료
    internal void SetFirstDefensiveRelicApDiscount(int amount) // 첫 유료 방어 카드 할인 표시 갱신
    { // 방어 할인 갱신 시작
        if (firstDefensiveRelicApDiscount == amount) // 기존 방어 할인과 갱신값 비교
        { // 같은 할인 처리 시작
            return; // 중복 손패 갱신 생략
        } // 같은 할인 처리 종료
        firstDefensiveRelicApDiscount = amount; // 새 복사본에 넘길 전투 한정 할인 기록
        foreach (CardInstance card in cards) // 현재 전투 전체 카드 순회
        { // 카드별 유물 할인 갱신 시작
            card.FirstDefensiveRelicApDiscount = amount; // 손패 밖 카드도 동일한 사용 비용 적용
        } // 카드별 유물 할인 갱신 종료
        StateChanged?.Invoke(); // 실제 손패의 행동력 배지와 사용 가능 여부 갱신
    } // 방어 할인 갱신 종료
    public int DiscardHand() // 일반 손패 전체 버리기
    { // 손패 정리 시작
        CardInstance retainedAtOpening = openingRetainedCard; // 첫 자동 버림에서만 확인할 유물 유지 대상
        openingRetainedCard = null; // 손패가 비어도 첫 버림 정리에서 예약 소모
        int count = hand.Count; // 정리할 손패 수 기록
        HandCountBeforeCleanup = count; // 유지와 버리기 전 손패 상태 기록
        if (count == 0) // 빈 손패 확인
        { // 정리 불필요 시작
            return 0; // 이동 카드 없음 반환
        } // 정리 불필요 종료
        List<CardInstance> volatileCards = new List<CardInstance>(); // 정리 뒤 효과를 처리할 휘발 카드 목록
        foreach (CardInstance card in hand.ToArray()) // 현재 손패 순서대로 실제 버림 판정
        { // 카드별 정리 시작
            if (card.SourceData.VolatileInHand) // 손패에 남은 휘발 카드 확인
            { // 휘발 카드 소멸 시작
                hand.Remove(card); // 손패에서 휘발 카드 제거
                excludedPile.Add(card); // 전투 동안 제외된 위치에 보관
                volatileCards.Add(card); // 손패 정리 뒤 발동 예약
                continue; // 일반 버림 검사 생략
            } // 휘발 카드 소멸 종료
            bool retainedByBell = card == retainedAtOpening && IsDrawable(card); // 여전히 손에 있는 생존 소유자의 개시 유지 카드 확인
            if (!retainedByBell && !IsRetainedHandCard(card) && !IsHandDiscardPrevented(card)) // 유물 유지와 카드 고유 유지 및 이번 버림 취소 확인
            { // 실제 버림 시작
                hand.Remove(card); // 원래 손패에서 제거
                discardPile.Add(card); // 버린 더미 끝에 추가
            } // 실제 버림 종료
        } // 카드별 정리 종료
        int moved = count - hand.Count; // 보너스 드로우 전 이동 수 보관
        EnsureValidState(); // 카드 위치 유효성 확인
        foreach (CardInstance card in volatileCards) // 소멸한 카드만 알림
        { // 휘발 알림 시작
            CardExcluded?.Invoke(card, true); // 제외와 손패 소멸 신호 전달
        } // 휘발 알림 종료
        StateChanged?.Invoke(); // 변경된 손패 화면 알림
        return moved; // 실제로 버린 카드 수 반환
    } // 손패 정리 종료
    public void ShuffleDrawPile() // 뽑을 카드 더미 셔플
    { // 카드 더미 셔플 시작
        ShuffleList(drawPile); // 뽑을 카드 순서 변경
        EnsureValidState(); // 셔플 후 덱 상태 검증
        StateChanged?.Invoke(); // 덱 상태 변경 알림
    } // 카드 더미 셔플 종료
    public bool IsStateValid() // 현재 덱 상태 유효성 확인
    { // 상태 검사 시작
        int locatedCardCount = drawPile.Count + hand.Count + discardPile.Count + excludedPile.Count; // 영역별 카드 총수 계산
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
        if (!AddZoneCards(discardPile, locatedCards) || !AddZoneCards(excludedPile, locatedCards)) // 버린 카드 중복 확인
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
    private static bool IsRetainedHandCard(CardInstance card) // 일반 턴 정리를 통과하는 유지 카드 확인
    { // 유지 조건 확인 시작
        return IsDrawable(card) && card.SourceData.RetainInHand && !card.SourceData.VolatileInHand; // 생존 소유자의 휘발하지 않는 유지 카드 확인
    } // 유지 조건 확인 종료
    internal bool HasRetainedHandCardOtherThan(CardInstance resolving) // 사용 정산 중인 카드를 제외한 유지 손패 조회
    { // 유지 손패 조회 시작
        return hand.Exists(card => card != resolving && IsRetainedHandCard(card)); // 사용 카드 외 실제 유지 카드 확인
    } // 유지 손패 조회 종료
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
