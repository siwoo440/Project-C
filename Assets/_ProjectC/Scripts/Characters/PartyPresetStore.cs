using System; // 직렬화와 변경 이벤트 사용
using System.Collections.Generic; // 편성 목록과 중복 검사 사용
using UnityEngine; // 게임 시작 시 초기화 사용

[Serializable] // 편성별 저장 직렬화 지정
public sealed class PartyPresetEntry // 한 편성의 캐릭터 식별자
{ // 편성 항목 시작
    public string[] members; // 편성 캐릭터 식별자 목록
    public string[] slots; // 빈 칸을 포함한 왼쪽부터 네 슬롯
} // 편성 항목 종료

[Serializable] // 전체 편성 저장 직렬화 지정
public sealed class PartyPresetSnapshot // 네 편성과 선택 번호
{ // 전체 편성 정보 시작
    public int selected; // 현재 선택 편성 번호
    public PartyPresetEntry[] entries; // 네 개의 편성 목록
} // 전체 편성 정보 종료

public static class PartyPresetStore // 네 개의 파티 편성 보관소
{ // 편성 보관소 시작
    public const int PresetCount = 4; // 사용 가능한 편성 수
    private static readonly string[][] members = new string[PresetCount][]; // 편성별 캐릭터 식별자 목록
    private static readonly string[][] slots = new string[PresetCount][]; // 편성별 빈 칸 포함 슬롯 목록
    private static int selected; // 현재 선택 편성 번호
    public static event Action Changed; // 편성 변경 알림
    public static int Selected => selected; // 현재 선택 편성 번호 조회
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 게임 시작 시 상태 초기화
    private static void InitializeRuntime() // 이전 실행 상태 정리
    { // 실행 상태 정리 시작
        Changed = null; // 이전 실행의 이벤트 연결 제거
        Reset(); // 빈 편성 상태 준비
    } // 실행 상태 정리 종료
    public static void Reset() // 전체 편성 초기화
    { // 전체 편성 초기화 시작
        selected = 0; // 첫 번째 편성 선택
        for (int index = 0; index < PresetCount; index++) // 전체 편성 순회
        { // 편성 초기화 시작
            members[index] = Array.Empty<string>(); // 빈 캐릭터 목록 설정
            slots[index] = CreateEmptySlots(); // 네 편성 칸 초기화
        } // 편성 초기화 종료
        Changed?.Invoke(); // 편성 초기화 알림
    } // 전체 편성 초기화 종료
    public static bool Select(int index) // 현재 편성 선택
    { // 편성 선택 시작
        if (!IsIndexValid(index)) // 편성 번호 범위 확인
        { // 잘못된 편성 처리 시작
            return false; // 편성 변경 실패 반환
        } // 잘못된 편성 처리 종료
        if (selected != index) // 현재 편성과 다른 번호 확인
        { // 편성 변경 시작
            selected = index; // 선택 편성 번호 변경
            Changed?.Invoke(); // 편성 변경 알림
        } // 편성 변경 종료
        return true; // 편성 선택 성공 반환
    } // 편성 선택 종료
    public static bool SetMembers(int index, IReadOnlyList<CharacterData> characters) // 편성 캐릭터 목록 변경
    { // 편성 목록 변경 시작
        if (!IsIndexValid(index) || characters == null || characters.Count > PartyData.MaxPartySize) // 편성 번호와 인원 확인
        { // 잘못된 편성 처리 시작
            return false; // 편성 변경 실패 반환
        } // 잘못된 편성 처리 종료
        string[] next = new string[characters.Count]; // 새 캐릭터 식별자 목록 생성
        HashSet<string> unique = new HashSet<string>(); // 캐릭터 중복 검사 목록 생성
        for (int memberIndex = 0; memberIndex < characters.Count; memberIndex++) // 선택 캐릭터 순회
        { // 캐릭터 식별자 검사 시작
            CharacterData character = characters[memberIndex]; // 선택 캐릭터 조회
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId) || !unique.Add(character.CharacterId)) // 유효한 고유 캐릭터 확인
            { // 잘못된 캐릭터 처리 시작
                return false; // 기존 편성 유지
            } // 잘못된 캐릭터 처리 종료
            next[memberIndex] = character.CharacterId; // 캐릭터 식별자 보관
        } // 캐릭터 식별자 검사 종료
        members[index] = next; // 지정 편성 캐릭터 교체
        slots[index] = ConvertMembersToSlots(next); // 기존 순서를 새 번호별 슬롯으로 변환
        Changed?.Invoke(); // 편성 변경 알림
        return true; // 편성 변경 성공 반환
    } // 편성 목록 변경 종료
    public static bool SetSlots(int index, IReadOnlyList<CharacterData> characters) // 빈 칸 포함 편성 슬롯 변경
    { // 슬롯 저장 시작
        if (!IsIndexValid(index) || characters == null || characters.Count != PartyData.MaxPartySize) // 편성 번호와 슬롯 수 확인
        { // 슬롯 저장 불가 처리 시작
            return false; // 기존 편성 유지
        } // 슬롯 저장 불가 처리 종료
        string[] nextSlots = CreateEmptySlots(); // 빈 슬롯 식별자 목록 생성
        List<string> nextMembers = new List<string>(); // 번호 순서의 캐릭터 목록 생성
        HashSet<string> unique = new HashSet<string>(); // 캐릭터 중복 검사 목록 생성
        for (int slotIndex = characters.Count - 1; slotIndex >= 0; slotIndex--) // 일 번부터 슬롯 순회
        { // 개별 슬롯 저장 시작
            CharacterData character = characters[slotIndex]; // 현재 슬롯 캐릭터 조회
            if (character == null) // 빈 슬롯 확인
            { // 빈 슬롯 처리 시작
                continue; // 빈 슬롯 유지
            } // 빈 슬롯 처리 종료
            if (string.IsNullOrWhiteSpace(character.CharacterId) || !unique.Add(character.CharacterId)) // 고유 캐릭터 식별자 확인
            { // 잘못된 캐릭터 처리 시작
                return false; // 기존 편성 유지
            } // 잘못된 캐릭터 처리 종료
            nextSlots[slotIndex] = character.CharacterId; // 지정 슬롯의 캐릭터 식별자 저장
            nextMembers.Add(character.CharacterId); // 번호 순서의 캐릭터 식별자 추가
        } // 개별 슬롯 저장 종료
        slots[index] = nextSlots; // 빈 칸 포함 슬롯 적용
        members[index] = nextMembers.ToArray(); // 번호 순서의 출전 목록 적용
        Changed?.Invoke(); // 편성 변경 알림
        return true; // 슬롯 저장 성공 반환
    } // 슬롯 저장 종료
    public static string[] GetSlots(int index) // 빈 칸 포함 편성 슬롯 조회
    { // 슬롯 조회 시작
        if (!IsIndexValid(index)) // 편성 번호 범위 확인
        { // 잘못된 편성 처리 시작
            return CreateEmptySlots(); // 빈 슬롯 목록 반환
        } // 잘못된 편성 처리 종료
        return slots[index] != null ? (string[])slots[index].Clone() : CreateEmptySlots(); // 외부 변경을 막는 슬롯 복사 반환
    } // 슬롯 조회 종료
    public static string[] GetMembers(int index) // 편성 캐릭터 식별자 조회
    { // 편성 목록 조회 시작
        if (!IsIndexValid(index)) // 편성 번호 범위 확인
        { // 잘못된 편성 처리 시작
            return Array.Empty<string>(); // 빈 목록 반환
        } // 잘못된 편성 처리 종료
        return members[index] != null ? (string[])members[index].Clone() : Array.Empty<string>(); // 외부 변경을 막는 목록 복사 반환
    } // 편성 목록 조회 종료
    public static PartyPresetSnapshot Capture() // 전체 편성 저장 정보 생성
    { // 편성 저장 정보 생성 시작
        PartyPresetEntry[] entries = new PartyPresetEntry[PresetCount]; // 편성 저장 항목 목록 생성
        for (int index = 0; index < PresetCount; index++) // 전체 편성 순회
        { // 편성 저장 항목 생성 시작
            entries[index] = new PartyPresetEntry // 개별 편성 저장 항목 생성
            { // 개별 편성 정보 설정 시작
                members = GetMembers(index), // 번호 순서 캐릭터 복사 설정
                slots = GetSlots(index) // 빈 칸 포함 슬롯 복사 설정
            }; // 개별 편성 정보 설정 종료
        } // 편성 저장 항목 생성 종료
        return new PartyPresetSnapshot // 전체 편성 저장 정보 생성
        { // 전체 편성 정보 설정 시작
            selected = selected, // 현재 선택 편성 설정
            entries = entries // 네 개의 편성 목록 설정
        }; // 전체 편성 정보 설정 종료
    } // 편성 저장 정보 생성 종료
    public static bool IsValid(PartyPresetSnapshot snapshot, CharacterRosterData roster) // 저장된 편성 정보 검증
    { // 편성 정보 검증 시작
        if (snapshot == null) // 이전 형식 저장 여부 확인
        { // 이전 형식 처리 시작
            return true; // 빈 편성으로 복원 허용
        } // 이전 형식 처리 종료
        if (roster == null || !IsIndexValid(snapshot.selected) || snapshot.entries == null || snapshot.entries.Length != PresetCount) // 필수 목록과 번호 확인
        { // 편성 형식 오류 처리 시작
            return false; // 잘못된 편성 차단
        } // 편성 형식 오류 처리 종료
        foreach (PartyPresetEntry entry in snapshot.entries) // 저장된 네 편성 순회
        { // 개별 편성 검증 시작
            if (entry == null || entry.members == null || entry.members.Length > PartyData.MaxPartySize) // 편성 인원과 목록 확인
            { // 편성 목록 오류 처리 시작
                return false; // 잘못된 편성 차단
            } // 편성 목록 오류 처리 종료
            HashSet<string> unique = new HashSet<string>(); // 편성별 중복 검사 목록 생성
            foreach (string id in entry.members) // 편성 캐릭터 식별자 순회
            { // 캐릭터 식별자 검사 시작
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id) || FindCharacter(roster, id) == null) // 중복과 원본 존재 확인
                { // 캐릭터 식별자 오류 처리 시작
                    return false; // 잘못된 편성 차단
                } // 캐릭터 식별자 오류 처리 종료
            } // 캐릭터 식별자 검사 종료
            if (entry.slots != null && entry.slots.Length > 0 && !SlotsMatchMembers(entry.slots, entry.members)) // 신규 슬롯 위치와 출전 목록 일치 확인
            { // 슬롯 형식 오류 처리 시작
                return false; // 잘못된 슬롯 정보 차단
            } // 슬롯 형식 오류 처리 종료
        } // 개별 편성 검증 종료
        return true; // 전체 편성 검증 성공 반환
    } // 편성 정보 검증 종료
    public static bool Restore(PartyPresetSnapshot snapshot, CharacterRosterData roster) // 저장된 편성 정보 복원
    { // 편성 복원 시작
        if (!IsValid(snapshot, roster)) // 전체 편성 정보 유효성 확인
        { // 잘못된 편성 처리 시작
            return false; // 기존 편성 유지
        } // 잘못된 편성 처리 종료
        if (snapshot == null) // 이전 형식 저장 여부 확인
        { // 이전 저장 처리 시작
            Reset(); // 빈 편성으로 초기화
            return true; // 이전 저장 복원 성공 반환
        } // 이전 저장 처리 종료
        selected = snapshot.selected; // 저장된 선택 편성 적용
        for (int index = 0; index < PresetCount; index++) // 전체 편성 순회
        { // 개별 편성 복원 시작
            members[index] = (string[])snapshot.entries[index].members.Clone(); // 저장된 캐릭터 목록 복사
            string[] savedSlots = snapshot.entries[index].slots; // 저장된 슬롯 위치 조회
            slots[index] = savedSlots != null && savedSlots.Length > 0 ? (string[])savedSlots.Clone() : ConvertMembersToSlots(members[index]); // 신규 슬롯 또는 이전 순서 변환 적용
        } // 개별 편성 복원 종료
        Changed?.Invoke(); // 편성 복원 알림
        return true; // 편성 복원 성공 반환
    } // 편성 복원 종료
    private static bool SlotsMatchMembers(string[] savedSlots, string[] savedMembers) // 저장 슬롯과 출전 순서 일치 확인
    { // 슬롯 일치 확인 시작
        if (savedSlots == null || savedSlots.Length != PartyData.MaxPartySize || savedMembers == null) // 저장 슬롯 수 확인
        { // 슬롯 형식 오류 처리 시작
            return false; // 일치 실패 반환
        } // 슬롯 형식 오류 처리 종료
        int memberIndex = 0; // 출전 목록 순번 초기화
        for (int slotIndex = savedSlots.Length - 1; slotIndex >= 0; slotIndex--) // 일 번부터 슬롯 순회
        { // 개별 슬롯 비교 시작
            string id = savedSlots[slotIndex]; // 슬롯 식별자 조회
            if (string.IsNullOrEmpty(id)) // 빈 슬롯 확인
            { // 빈 슬롯 처리 시작
                continue; // 다음 슬롯 이동
            } // 빈 슬롯 처리 종료
            if (string.IsNullOrWhiteSpace(id) || memberIndex >= savedMembers.Length || savedMembers[memberIndex] != id) // 출전 목록 식별자 일치 확인
            { // 슬롯 불일치 처리 시작
                return false; // 일치 실패 반환
            } // 슬롯 불일치 처리 종료
            memberIndex++; // 다음 출전 캐릭터 이동
        } // 개별 슬롯 비교 종료
        return memberIndex == savedMembers.Length; // 전체 출전 캐릭터 일치 여부 반환
    } // 슬롯 일치 확인 종료
    private static string[] CreateEmptySlots() // 빈 편성 슬롯 생성
    { // 빈 슬롯 생성 시작
        string[] empty = new string[PartyData.MaxPartySize]; // 네 개 슬롯 생성
        for (int index = 0; index < empty.Length; index++) // 전체 슬롯 순회
        { // 빈 슬롯 설정 시작
            empty[index] = string.Empty; // 빈 식별자 설정
        } // 빈 슬롯 설정 종료
        return empty; // 빈 슬롯 반환
    } // 빈 슬롯 생성 종료
    private static string[] ConvertMembersToSlots(IReadOnlyList<string> savedMembers) // 이전 출전 순서를 슬롯으로 변환
    { // 이전 순서 변환 시작
        string[] converted = CreateEmptySlots(); // 빈 슬롯 목록 생성
        for (int index = 0; index < savedMembers.Count && index < converted.Length; index++) // 이전 출전 순서 순회
        { // 이전 캐릭터 배치 시작
            converted[converted.Length - 1 - index] = savedMembers[index]; // 일 번부터 기존 순서대로 배치
        } // 이전 캐릭터 배치 종료
        return converted; // 변환된 슬롯 반환
    } // 이전 순서 변환 종료
    public static CharacterData FindCharacter(CharacterRosterData roster, string id) // 식별자로 캐릭터 원본 조회
    { // 캐릭터 조회 시작
        if (roster == null || string.IsNullOrWhiteSpace(id)) // 캐릭터 목록과 식별자 확인
        { // 조회 불가 처리 시작
            return null; // 캐릭터 없음 반환
        } // 조회 불가 처리 종료
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 캐릭터 비교 시작
            if (character != null && character.CharacterId == id) // 캐릭터 식별자 일치 확인
            { // 캐릭터 발견 처리 시작
                return character; // 일치 캐릭터 반환
            } // 캐릭터 발견 처리 종료
        } // 캐릭터 비교 종료
        return null; // 일치 캐릭터 없음 반환
    } // 캐릭터 조회 종료
    private static bool IsIndexValid(int index) // 편성 번호 범위 확인
    { // 편성 번호 검사 시작
        return index >= 0 && index < PresetCount; // 네 편성 범위 반환
    } // 편성 번호 검사 종료
} // 편성 보관소 종료
