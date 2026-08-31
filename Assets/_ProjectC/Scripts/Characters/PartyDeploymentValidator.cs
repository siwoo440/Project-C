public enum PartyDeploymentBlockReason // 파티 출전 차단 사유
{
    None = 0, // 출전 가능
    InvalidParty = 1, // 파티 데이터 오류
    InvalidLoadout = 2, // 전투 편성 데이터 오류
    DeadMember = 3, // 사망 캐릭터 포함
    RecoveringMember = 4, // 회복 중 캐릭터 포함
    UnavailableMember = 5 // 기타 출전 불가 캐릭터 포함
}

public readonly struct PartyDeploymentValidationResult // 파티 출전 검증 결과
{
    public bool CanDeploy { get; } // 전체 파티 출전 가능 여부
    public PartyDeploymentBlockReason BlockReason { get; } // 출전 차단 사유
    public CharacterData BlockedCharacter { get; } // 최초 차단 캐릭터
    public int DeployableMemberCount { get; } // 검증 완료 출전 가능 인원

    public PartyDeploymentValidationResult(
        bool canDeploy,
        PartyDeploymentBlockReason blockReason,
        CharacterData blockedCharacter,
        int deployableMemberCount) // 출전 검증 결과 생성
    {
        CanDeploy = canDeploy; // 출전 가능 여부 저장
        BlockReason = blockReason; // 차단 사유 저장
        BlockedCharacter = blockedCharacter; // 차단 캐릭터 저장
        DeployableMemberCount = deployableMemberCount; // 출전 가능 인원 저장
    }
}

public interface IPartyDeploymentStatusProvider // 캐릭터 출전 상태 공급 규약
{
    bool IsDead(CharacterData characterData); // 사망 여부 조회
    bool IsRecovering(CharacterData characterData); // 회복 중 여부 조회
    bool CanDeploy(CharacterData characterData); // 최종 출전 가능 여부 조회
}

public sealed class CharacterRecoveryDeploymentStatusProvider : IPartyDeploymentStatusProvider // 기존 회복 시스템 출전 상태 연결
{
    private readonly CharacterRecoveryManager recoveryManager; // 기존 회복 관리자

    public CharacterRecoveryDeploymentStatusProvider(
        CharacterRecoveryManager targetRecoveryManager) // 회복 관리자 연결
    {
        recoveryManager = targetRecoveryManager; // 회복 관리자 저장
    }

    public bool IsDead(CharacterData characterData) // 사망 여부 조회
    {
        return recoveryManager != null &&
               recoveryManager.IsDead(characterData); // 기존 저장 HP 사망 판정 사용
    }

    public bool IsRecovering(CharacterData characterData) // 회복 중 여부 조회
    {
        return recoveryManager != null &&
               recoveryManager.IsRecovering(characterData); // 기존 회복 상태 사용
    }

    public bool CanDeploy(CharacterData characterData) // 최종 출전 가능 여부 조회
    {
        return recoveryManager == null ||
               recoveryManager.CanDeploy(characterData); // 기존 출전 판정 사용
    }
}

public static class PartyDeploymentValidator // 파티 편성 출전 검증 공통 규칙
{
    public static PartyDeploymentValidationResult Validate(
        PartyData partyData) // 런타임 회복 상태 기반 파티 검증
    {
        CharacterRecoveryManager recoveryManager =
            CharacterRecoveryManager.EnsureInstance(); // 기존 회복 관리자 준비

        CharacterRecoveryDeploymentStatusProvider statusProvider =
            new CharacterRecoveryDeploymentStatusProvider(
                recoveryManager); // 기존 회복 상태 공급자 생성

        return Validate(
            partyData,
            statusProvider); // 공통 파티 검증 실행
    }

    public static PartyDeploymentValidationResult Validate(
        PartyData partyData,
        IPartyDeploymentStatusProvider statusProvider) // 지정 상태 공급자 기반 파티 검증
    {
        if (partyData == null ||
            !partyData.IsValidParty())
        {
            return new PartyDeploymentValidationResult(
                false,
                PartyDeploymentBlockReason.InvalidParty,
                null,
                0); // 잘못된 파티 차단
        }

        int deployableMemberCount = 0; // 출전 가능 인원 초기화

        foreach (CharacterData member in partyData.Members)
        {
            if (statusProvider != null &&
                statusProvider.IsRecovering(member))
            {
                return new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.RecoveringMember,
                    member,
                    deployableMemberCount); // 회복 중 캐릭터 차단
            }

            if (statusProvider != null &&
                statusProvider.IsDead(member))
            {
                return new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.DeadMember,
                    member,
                    deployableMemberCount); // 사망 캐릭터 차단
            }

            if (statusProvider != null &&
                !statusProvider.CanDeploy(member))
            {
                return new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.UnavailableMember,
                    member,
                    deployableMemberCount); // 기타 출전 불가 상태 차단
            }

            deployableMemberCount += 1; // 출전 가능 인원 누적
        }

        return new PartyDeploymentValidationResult(
            true,
            PartyDeploymentBlockReason.None,
            null,
            deployableMemberCount); // 전체 파티 출전 가능 반환
    }
}
