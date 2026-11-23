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
                string cardPath = GetCharacterCardPath(member.CharacterId); // 캐릭터 전용 카드 경로 선택
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
    private static string GetCharacterCardPath(string characterId) // 캐릭터 전용 카드 경로 조회
    { // 경로 조회 시작
        switch (characterId) // 캐릭터 식별자 분기
        { // 식별자 분기 시작
            case "ARC_00": // 바보 캐릭터
                return "StarterCards/FoolChoice"; // 바보 카드 경로 반환
            case "ARC_01": // 마법사 캐릭터
                return "StarterCards/MagicianRepair"; // 마법사 카드 경로 반환
            case "ARC_02": // 여사제 캐릭터
                return "StarterCards/PriestessWard"; // 여사제 카드 경로 반환
            case "ARC_03": // 여황제 캐릭터
                return "StarterCards/EmpressFeast"; // 여황제 카드 경로 반환
            case "ARC_04": // 황제 캐릭터
                return "StarterCards/EmperorGuard"; // 황제 카드 경로 반환
            case "ARC_05": // 교황 캐릭터
                return "StarterCards/HierophantPreparation"; // 교황 카드 경로 반환
            case "ARC_06": // 연인 캐릭터
                return "StarterCards/LoversPartner"; // 연인 파트너 카드 경로 반환
            case "ARC_07": // 전차 캐릭터
                return "StarterCards/ChariotMark"; // 전차 카드 경로 반환
            case "ARC_08": // 힘 캐릭터
                return "StarterCards/StrengthGuard"; // 힘의 동료 보호 카드 경로 반환
            case "ARC_09": // 은둔자 캐릭터
                return "StarterCards/HermitArrange"; // 은둔자의 덱 순서 카드 경로 반환
            case "ARC_10": // 운명 캐릭터
                return "StarterCards/FateReroll"; // 운명의 재굴림 카드 경로 반환
            case "ARC_11": // 정의 캐릭터
                return "StarterCards/JusticeShare"; // 정의의 피해 분담 카드 경로 반환
            case "ARC_17": // 별 캐릭터
                return "StarterCards/StarHealing"; // 별 카드 경로 반환
            default: // 전용 카드 미구현 캐릭터
                return null; // 공용 카드 사용 반환
        } // 식별자 분기 종료
    } // 경로 조회 종료
    internal static void Restore(BattleLoadoutData loadout) // 저장된 탐사 편성 복원
    { // 탐사 편성 복원 시작
        Clear(); // 이전 런타임 편성 정리
        Active = loadout; // 복원 편성 보관
    } // 탐사 편성 복원 종료
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
