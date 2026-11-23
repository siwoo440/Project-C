using System; // 잘못된 인수 예외 사용
using System.Collections.Generic; // 파티 목록 사용
using System.Collections.ObjectModel; // 읽기 전용 목록 사용
public sealed class PartySelection // 출전 파티 선택 상태
{ // 선택 클래스 시작
    private readonly List<CharacterData> members = new List<CharacterData>(); // 선택 캐릭터 목록
    private readonly ReadOnlyCollection<CharacterData> visibleMembers; // 외부 읽기 전용 목록
    private readonly IPartyDeploymentStatusProvider statusProvider; // 출전 상태 제공자
    private readonly CharacterUnlockProgress unlockProgress; // 캐릭터 해금 상태
    public IReadOnlyList<CharacterData> Members => visibleMembers; // 선택 목록 조회
    public PartySelection(IPartyDeploymentStatusProvider provider, CharacterUnlockProgress progress = null) // 선택 상태 생성
    { // 생성자 시작
        statusProvider = provider ?? throw new ArgumentNullException(nameof(provider)); // 출전 상태 제공자 저장
        unlockProgress = progress ?? CharacterUnlockProgress.Shared; // 해금 상태 저장
        visibleMembers = members.AsReadOnly(); // 목록 변경 차단 설정
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
        members.Add(character); // 캐릭터 선택 목록 추가
        return true; // 선택 성공 반환
    } // 선택 처리 종료
    public bool TryRemove(CharacterData character) // 캐릭터 선택 해제 시도
    { // 해제 처리 시작
        return character != null && members.Remove(character); // 선택 해제 결과 반환
    } // 해제 처리 종료
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
