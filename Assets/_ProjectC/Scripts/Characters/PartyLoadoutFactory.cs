using System.Collections.Generic; // 카드와 파티 목록 사용
public static class PartyLoadoutFactory // 선택 파티 전투 편성 생성기
{ // 생성기 시작
    public const int PrototypeStartingCardCount = 12; // 임시 시작 덱 장수
    public static BattleLoadoutData Create(PartySelection selection, IReadOnlyList<CardData> roleCards, IReadOnlyDictionary<string, CardData> characterCards = null) // 선택 파티와 임시 카드 편성 생성
    { // 편성 생성 시작
        if (selection == null || !selection.Validate().CanDeploy || roleCards == null) // 출전 가능 파티와 카드 목록 확인
        { // 생성 불가 처리 시작
            return null; // 편성 생성 실패 반환
        } // 생성 불가 처리 종료
        foreach (CharacterData member in selection.Members) // 선택 캐릭터 순회
        { // 역할 카드 검사 시작
            int roleIndex = (int)member.Role; // 캐릭터 역할 번호 조회
            if (roleIndex < 0 || roleIndex >= roleCards.Count || roleCards[roleIndex] == null) // 역할 카드 존재 확인
            { // 카드 누락 처리 시작
                return null; // 편성 생성 실패 반환
            } // 카드 누락 처리 종료
        } // 역할 카드 검사 종료
        List<DeckCardEntry> cards = new List<DeckCardEntry>(); // 임시 시작 카드 목록 생성
        for (int index = 0; index < PrototypeStartingCardCount; index++) // 시작 카드 장수 순회
        { // 카드 생성 시작
            CharacterData owner = selection.Members[index % selection.Members.Count]; // 순서대로 카드 소유자 선택
            CardData card = roleCards[(int)owner.Role]; // 기본 역할 카드 선택
            if (index < selection.Members.Count && characterCards != null && !string.IsNullOrEmpty(owner.CharacterId) && characterCards.TryGetValue(owner.CharacterId, out CardData characterCard) && characterCard != null) // 첫 장의 전용 카드 확인
            { // 전용 카드 선택 시작
                card = characterCard; // 캐릭터 전용 카드 적용
            } // 전용 카드 선택 종료
            cards.Add(new DeckCardEntry(card, owner)); // 선택 카드와 소유자 연결
        } // 카드 생성 종료
        PartyData party = PartyData.CreateRuntime(selection.Members); // 선택 파티 복사 생성
        DeckData deck = DeckData.CreateRuntime(cards); // 임시 시작 덱 생성
        return BattleLoadoutData.CreateRuntime(party, deck); // 전투 편성 생성 반환
    } // 편성 생성 종료
} // 생성기 종료
