using System.Collections.Generic; // 전용 카드 검색표 사용
using UnityEngine; // 리소스와 런타임 객체 사용
public static class PartyLoadoutRuntime // 탐사와 전투 공용 출발 편성
{ // 편성 관리자 시작
    public static BattleLoadoutData Active { get; private set; } // 현재 탐사 편성 조회
    public static bool Prepare(PartySelection selection) // 선택한 파티로 출발 편성 준비
    { // 편성 준비 시작
        CardData[] cards = new CardData[4]; // 역할별 임시 카드 목록 생성
        cards[0] = Resources.Load<CardData>("StarterCards/Attack"); // 공격 역할 카드 조회
        cards[1] = Resources.Load<CardData>("StarterCards/Defense"); // 방어 역할 카드 조회
        cards[2] = Resources.Load<CardData>("StarterCards/Support"); // 지원 역할 카드 조회
        cards[3] = Resources.Load<CardData>("StarterCards/Strategy"); // 전략 역할 카드 조회
        Dictionary<string, CardData> characterCards = new Dictionary<string, CardData>(); // 전용 카드 검색표 생성
        if (selection != null) // 파티 선택 상태 확인
        { // 전용 카드 준비 시작
            foreach (CharacterData member in selection.Members) // 선택 캐릭터 순회
            { // 전용 카드 확인 시작
                string cardPath = member.CharacterId == "ARC_04" ? "StarterCards/EmperorGuard" : member.CharacterId == "ARC_02" ? "StarterCards/PriestessWard" : member.CharacterId == "ARC_00" ? "StarterCards/FoolChoice" : null; // 캐릭터 전용 카드 경로 선택
                if (cardPath == null) // 전용 카드 미구현 캐릭터 확인
                { // 공용 카드 처리 시작
                    continue; // 다음 캐릭터 확인
                } // 공용 카드 처리 종료
                CardData characterCard = Resources.Load<CardData>(cardPath); // 캐릭터 전용 카드 조회
                if (characterCard == null) // 전용 카드 리소스 확인
                { // 카드 누락 처리 시작
                    return false; // 편성 준비 실패 반환
                } // 카드 누락 처리 종료
                characterCards.Add(member.CharacterId, characterCard); // 캐릭터 전용 카드 등록
            } // 전용 카드 확인 종료
        } // 전용 카드 준비 종료
        BattleLoadoutData next = PartyLoadoutFactory.Create(selection, cards, characterCards); // 새 출발 편성 생성
        if (next == null) // 편성 생성 실패 확인
        { // 생성 실패 처리 시작
            return false; // 기존 편성 유지와 실패 반환
        } // 생성 실패 처리 종료
        Clear(); // 이전 런타임 편성 정리
        Active = next; // 새 출발 편성 저장
        return true; // 편성 준비 성공 반환
    } // 편성 준비 종료
    public static void Clear() // 현재 출발 편성 정리
    { // 편성 정리 시작
        if (Active == null) // 현재 편성 존재 확인
        { // 편성 없음 처리 시작
            return; // 정리 중단
        } // 편성 없음 처리 종료
        Release(Active.Party); // 런타임 파티 정리
        Release(Active.Deck); // 런타임 덱 정리
        Release(Active); // 런타임 편성 정리
        Active = null; // 현재 편성 참조 해제
    } // 편성 정리 종료
    private static void Release(Object target) // 런타임 데이터 객체 정리
    { // 객체 정리 시작
        if (target == null) // 객체 존재 확인
        { // 객체 없음 처리 시작
            return; // 객체 정리 중단
        } // 객체 없음 처리 종료
        if (Application.isPlaying) // 게임 실행 상태 확인
        { // 실행 중 정리 시작
            Object.Destroy(target); // 프레임 종료 시 객체 제거
        } // 실행 중 정리 종료
        else // 편집기 테스트 상태 확인
        { // 편집기 정리 시작
            Object.DestroyImmediate(target); // 테스트 객체 즉시 제거
        } // 편집기 정리 종료
    } // 객체 정리 종료
} // 편성 관리자 종료
