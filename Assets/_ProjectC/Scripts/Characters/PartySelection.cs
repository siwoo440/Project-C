using System; // 잘못된 인수 예외 사용
using System.Collections.Generic; // 파티 목록 사용
using System.Collections.ObjectModel; // 읽기 전용 목록 사용
public sealed class PartySelection // 출전 파티 선택 상태
{ // 선택 클래스 시작
    private readonly List<CharacterData> members = new List<CharacterData>(); // 선택 캐릭터 목록
    private readonly CharacterData[] slots = new CharacterData[PartyData.MaxPartySize]; // 왼쪽부터 네 편성 칸
    private readonly ReadOnlyCollection<CharacterData> visibleMembers; // 외부 읽기 전용 목록
    private readonly ReadOnlyCollection<CharacterData> visibleSlots; // 외부 읽기 전용 슬롯
    private readonly IPartyDeploymentStatusProvider statusProvider; // 출전 상태 제공자
    private readonly CharacterUnlockProgress unlockProgress; // 캐릭터 해금 상태
    public IReadOnlyList<CharacterData> Members => visibleMembers; // 선택 목록 조회
    public IReadOnlyList<CharacterData> Slots => visibleSlots; // 빈 칸을 포함한 슬롯 조회
    public PartySelection(IPartyDeploymentStatusProvider provider, CharacterUnlockProgress progress = null) // 선택 상태 생성
    { // 생성자 시작
        statusProvider = provider ?? throw new ArgumentNullException(nameof(provider)); // 출전 상태 제공자 저장
        unlockProgress = progress ?? CharacterUnlockProgress.Shared; // 해금 상태 저장
        visibleMembers = members.AsReadOnly(); // 목록 변경 차단 설정
        visibleSlots = Array.AsReadOnly(slots); // 슬롯 변경 차단 설정
    } // 생성자 종료
    public bool TryAdd(CharacterData character) // 캐릭터 선택 시도
    { // 선택 처리 시작
        if (character == null || !unlockProgress.IsUnlocked(character) || members.Count >= PartyData.MaxPartySize || members.Contains(character)) // 유효성과 인원 확인
        { // 잘못된 선택 처리 시작
            return false; // 선택 실패 반환
        } // 잘못된 선택 처리 종료
        if (statusProvider.IsDead(character) || statusProvider.IsRecovering(character) || !statusProvider.CanDeploy(character)) // 출전 가능 상태 확인
        { // 출전 불가 처리 시작
            return false; // 선택 실패 반환
        } // 출전 불가 처리 종료
        for (int index = slots.Length - 1; index >= 0; index--) // 일 번부터 빈 슬롯 탐색
        { // 빈 슬롯 탐색 시작
            if (slots[index] == null) // 비어 있는 슬롯 확인
            { // 빈 슬롯 채우기 시작
                slots[index] = character; // 해당 슬롯에 캐릭터 배치
                RebuildMembers(); // 번호 순서대로 출전 목록 갱신
                return true; // 선택 성공 반환
            } // 빈 슬롯 채우기 종료
        } // 빈 슬롯 탐색 종료
        return false; // 빈 슬롯 없음 반환
    } // 선택 처리 종료
    public bool TryPlaceAtSlot(CharacterData character, int slotIndex) // 지정 슬롯에 캐릭터 배치
    { // 슬롯 배치 시작
        if (character == null || slotIndex < 0 || slotIndex >= slots.Length) // 캐릭터와 슬롯 범위 확인
        { // 배치 불가 처리 시작
            return false; // 배치 실패 반환
        } // 배치 불가 처리 종료
        int previousIndex = Array.IndexOf(slots, character); // 기존 편성 위치 조회
        if (previousIndex < 0 && (!unlockProgress.IsUnlocked(character) || statusProvider.IsDead(character) || statusProvider.IsRecovering(character) || !statusProvider.CanDeploy(character))) // 신규 출전 가능 여부 확인
        { // 출전 불가 처리 시작
            return false; // 배치 실패 반환
        } // 출전 불가 처리 종료
        if (previousIndex == slotIndex) // 같은 슬롯 재배치 확인
        { // 동일 위치 처리 시작
            return true; // 기존 위치 유지 반환
        } // 동일 위치 처리 종료
        if (previousIndex >= 0) // 기존 편성 캐릭터 확인
        { // 기존 위치 교환 시작
            slots[previousIndex] = slots[slotIndex]; // 기존 위치에 대상 슬롯 캐릭터 이동
        } // 기존 위치 교환 종료
        slots[slotIndex] = character; // 지정 슬롯에 캐릭터 배치
        RebuildMembers(); // 번호 순서대로 출전 목록 갱신
        return true; // 배치 성공 반환
    } // 슬롯 배치 종료
    internal bool TryRestoreMember(CharacterData character) // 저장된 캐릭터 편성 복원
    { // 저장 캐릭터 복원 시작
        if (character == null || members.Count >= PartyData.MaxPartySize || members.Contains(character)) // 캐릭터와 편성 중복 확인
        { // 잘못된 복원 처리 시작
            return false; // 기존 편성 유지
        } // 잘못된 복원 처리 종료
        for (int index = slots.Length - 1; index >= 0; index--) // 일 번부터 빈 슬롯 탐색
        { // 복원 슬롯 탐색 시작
            if (slots[index] == null) // 빈 슬롯 확인
            { // 저장 캐릭터 복원 시작
                return TryRestoreAtSlot(character, index); // 지정 슬롯 복원 결과 반환
            } // 저장 캐릭터 복원 종료
        } // 복원 슬롯 탐색 종료
        return false; // 복원할 빈 슬롯 없음 반환
    } // 저장 캐릭터 복원 종료
    internal bool TryRestoreAtSlot(CharacterData character, int slotIndex) // 저장된 슬롯 위치 복원
    { // 슬롯 위치 복원 시작
        if (character == null || slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] != null || members.Contains(character)) // 캐릭터와 빈 슬롯 확인
        { // 슬롯 복원 불가 처리 시작
            return false; // 슬롯 복원 실패 반환
        } // 슬롯 복원 불가 처리 종료
        slots[slotIndex] = character; // 저장된 슬롯에 캐릭터 복원
        RebuildMembers(); // 번호 순서대로 출전 목록 갱신
        return true; // 슬롯 복원 성공 반환
    } // 슬롯 위치 복원 종료
    public bool TryRemove(CharacterData character) // 캐릭터 선택 해제 시도
    { // 해제 처리 시작
        int slotIndex = character == null ? -1 : Array.IndexOf(slots, character); // 선택 캐릭터 슬롯 조회
        if (slotIndex < 0) // 선택된 캐릭터 확인
        { // 선택 없음 처리 시작
            return false; // 선택 해제 실패 반환
        } // 선택 없음 처리 종료
        slots[slotIndex] = null; // 기존 슬롯 비우기
        RebuildMembers(); // 번호 순서대로 출전 목록 갱신
        return true; // 선택 해제 성공 반환
    } // 해제 처리 종료
    private void RebuildMembers() // 슬롯 번호에 따른 출전 목록 갱신
    { // 출전 목록 갱신 시작
        members.Clear(); // 기존 출전 목록 비우기
        for (int index = slots.Length - 1; index >= 0; index--) // 일 번부터 슬롯 순회
        { // 출전 캐릭터 등록 시작
            if (slots[index] != null) // 배치된 캐릭터 확인
            { // 출전 캐릭터 추가 시작
                members.Add(slots[index]); // 번호 순서대로 캐릭터 추가
            } // 출전 캐릭터 추가 종료
        } // 출전 캐릭터 등록 종료
    } // 출전 목록 갱신 종료
    public PartyDeploymentValidationResult Validate() // 출발 직전 파티 상태 검증
    { // 파티 검증 시작
        if (members.Count == 0) // 빈 파티 확인
        { // 빈 파티 처리 시작
            return new PartyDeploymentValidationResult(false, PartyDeploymentBlockReason.InvalidParty, null, 0); // 빈 파티 차단 결과
        } // 빈 파티 처리 종료
        int deployableCount = 0; // 출전 가능 인원 초기화
        foreach (CharacterData member in members) // 선택 캐릭터 순회
        { // 캐릭터 검증 시작
            if (statusProvider.IsRecovering(member)) // 회복 중 상태 확인
            { // 회복 중 처리 시작
                return new PartyDeploymentValidationResult(false, PartyDeploymentBlockReason.RecoveringMember, member, deployableCount); // 회복 중 차단 결과
            } // 회복 중 처리 종료
            if (statusProvider.IsDead(member)) // 사망 상태 확인
            { // 사망 처리 시작
                return new PartyDeploymentValidationResult(false, PartyDeploymentBlockReason.DeadMember, member, deployableCount); // 사망 차단 결과
            } // 사망 처리 종료
            if (!unlockProgress.IsUnlocked(member) || !statusProvider.CanDeploy(member)) // 해금과 기타 출전 제한 확인
            { // 출전 제한 처리 시작
                return new PartyDeploymentValidationResult(false, PartyDeploymentBlockReason.UnavailableMember, member, deployableCount); // 출전 제한 결과
            } // 출전 제한 처리 종료
            deployableCount++; // 출전 가능 인원 증가
        } // 캐릭터 검증 종료
        return new PartyDeploymentValidationResult(true, PartyDeploymentBlockReason.None, null, deployableCount); // 파티 출전 가능 결과
    } // 파티 검증 종료
} // 선택 클래스 종료
