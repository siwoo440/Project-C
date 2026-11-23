public enum PartyMemberDeploymentState // 파티원 출전 표시 상태
{
    Available = 0, // 출전 가능
    Dead = 1, // 사망 상태
    Recovering = 2, // 회복 진행 상태
    Unavailable = 3 // 기타 출전 불가
}

public readonly struct PartyMemberDeploymentViewState // 파티 편성 UI용 상태 값
{
    public PartyMemberDeploymentState State { get; } // 현재 표시 상태
    public bool CanSelect { get; } // 편성 선택 가능 여부
    public bool IsDimmed { get; } // 초상화 흐림 여부
    public string StatusText { get; } // 상태 표시 문구
    public int RemainingRecoveryExpeditions { get; } // 남은 회복 탐사 횟수

    public PartyMemberDeploymentViewState(
        PartyMemberDeploymentState state,
        bool canSelect,
        bool isDimmed,
        string statusText,
        int remainingRecoveryExpeditions)
    {
        State = state; // 표시 상태 저장
        CanSelect = canSelect; // 선택 가능 상태 저장
        IsDimmed = isDimmed; // 흐림 상태 저장
        StatusText = statusText; // 상태 문구 저장
        RemainingRecoveryExpeditions = remainingRecoveryExpeditions; // 남은 회복 횟수 저장
    }
}

public static class PartyDeploymentViewStateFactory // 파티 편성 UI 상태 생성기
{
    public static PartyMemberDeploymentViewState Create(CharacterData characterData) // 실제 런타임 상태 기반 표시 생성
    {
        CharacterRecoveryManager recoveryManager = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
        CharacterRecoveryDeploymentStatusProvider statusProvider =
            new CharacterRecoveryDeploymentStatusProvider(recoveryManager); // 출전 상태 제공자 준비

        int remainingRecoveryExpeditions =
            recoveryManager != null
                ? recoveryManager.GetRemainingRecoveryExpeditions(characterData)
                : 0; // 남은 회복 탐사 횟수 조회

        return Create(
            characterData,
            statusProvider,
            remainingRecoveryExpeditions); // 공통 표시 상태 생성
    }

    public static PartyMemberDeploymentViewState Create(
        CharacterData characterData,
        IPartyDeploymentStatusProvider statusProvider,
        int remainingRecoveryExpeditions) // 지정 상태 제공자 기반 표시 생성
    {
        int safeRemainingExpeditions =
            remainingRecoveryExpeditions < 0
                ? 0
                : remainingRecoveryExpeditions; // 음수 회복 횟수 방지

        if (statusProvider != null && statusProvider.IsRecovering(characterData))
        {
            string recoveryText =
                safeRemainingExpeditions > 0
                    ? $"회복 중 · {safeRemainingExpeditions}회"
                    : "회복 중"; // 회복 상태 문구 결정

            return new PartyMemberDeploymentViewState(
                PartyMemberDeploymentState.Recovering,
                false,
                true,
                recoveryText,
                safeRemainingExpeditions); // 회복 상태 반환
        }

        if (statusProvider != null && statusProvider.IsDead(characterData))
        {
            return new PartyMemberDeploymentViewState(
                PartyMemberDeploymentState.Dead,
                false,
                true,
                "사망",
                0); // 사망 상태 반환
        }

        if (statusProvider != null && !statusProvider.CanDeploy(characterData))
        {
            return new PartyMemberDeploymentViewState(
                PartyMemberDeploymentState.Unavailable,
                false,
                true,
                "출전 불가",
                0); // 기타 출전 불가 상태 반환
        }

        return new PartyMemberDeploymentViewState(
            PartyMemberDeploymentState.Available,
            true,
            false,
            "출전 가능",
            0); // 정상 출전 가능 상태 반환
    }
}
