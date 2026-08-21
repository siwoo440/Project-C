using System; // 기본 예외 기능 사용
using System.Collections.Generic; // 목록과 사전 자료형 사용
public sealed class BattleDeckRuntime // 전투용 런타임 덱
{ // 클래스 시작
    private readonly List<CardInstance> cards = new List<CardInstance>(); // 생성된 카드 목록
    public DeckData SourceData { get; } // 덱 원본 데이터
    public IReadOnlyList<CardInstance> Cards => cards; // 카드 목록 조회
    public int CardCount => cards.Count; // 카드 수 조회
    private BattleDeckRuntime(DeckData sourceData) // 런타임 덱 생성자
    { // 생성자 시작
        SourceData = sourceData; // 덱 원본 저장
    } // 생성자 종료
    public static BattleDeckRuntime Create(DeckData deckData, IReadOnlyList<BattleUnitRuntime> allyUnits) // 런타임 덱 생성
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
        Dictionary<CharacterData, BattleUnitRuntime> ownerUnits = CreateOwnerMap(allyUnits); // 카드 소유자 검색표 생성
        BattleDeckRuntime runtimeDeck = new BattleDeckRuntime(deckData); // 빈 런타임 덱 생성
        int sequence = 0; // 카드 순번 초기화
        foreach (DeckCardEntry entry in deckData.Cards) // 원본 덱 카드 순회
        { // 카드 변환 시작
            if (!ownerUnits.TryGetValue(entry.Owner, out BattleUnitRuntime ownerUnit)) // 소유 전투 유닛 검색
            { // 소유자 없음 처리 시작
                throw new InvalidOperationException($"카드 소유자 {entry.Owner.DisplayName}의 전투 유닛을 찾을 수 없습니다."); // 소유자 연결 실패 예외
            } // 소유자 없음 처리 종료
            CardInstance cardInstance = CardInstance.Create(entry.Card, ownerUnit, sequence); // 카드 인스턴스 생성
            runtimeDeck.cards.Add(cardInstance); // 런타임 덱에 카드 등록
            sequence++; // 다음 카드 순번 이동
        } // 카드 변환 종료
        if (runtimeDeck.CardCount != deckData.CardCount) // 카드 수 일치 확인
        { // 카드 수 오류 처리 시작
            throw new InvalidOperationException("원본 덱과 런타임 덱의 카드 수가 일치하지 않습니다."); // 카드 수 불일치 예외
        } // 카드 수 오류 처리 종료
        return runtimeDeck; // 생성된 런타임 덱 반환
    } // 덱 생성 종료
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
