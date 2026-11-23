using UnityEngine; // 유니티 Scene 컴포넌트와 로그 사용

[DefaultExecutionOrder(-10000)] // 런타임 탐사 맵 생성보다 먼저 파티 상태 등록
public sealed class ExplorationPartyLoadoutProvider : MonoBehaviour // 탐사 Scene 출전 파티 Loadout 제공
{
    [Header("탐사 출전 파티")] // 탐사 파티 연결 구역
    [SerializeField] private BattleLoadoutData battleLoadout; // 전투 Scene과 동일한 출전 파티·덱 데이터

    public BattleLoadoutData BattleLoadout =>
        battleLoadout; // 현재 연결 Loadout 조회

    public PartyDeploymentValidationResult DeploymentValidation
    {
        get;
        private set;
    } // 최신 출전 검증 결과 조회

    public bool CanStartNewExploration =>
        DeploymentValidation.CanDeploy; // 신규 탐사 출전 가능 여부 조회

    private void Awake() // 탐사 Scene 로드 시 출전 파티 등록
    {
        if (battleLoadout == null)
        {
            Debug.LogError(
                "[ExplorationPartyLoadoutProvider][Day51] Battle Loadout이 연결되지 않았습니다. " +
                "40_Battle의 BattleSceneSetup과 같은 BattleLoadoutData를 연결하세요.",
                this); // Loadout 연결 누락 안내

            DeploymentValidation =
                new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.InvalidLoadout,
                    null,
                    0); // Loadout 누락 출전 차단 저장

            return;
        }

        if (!battleLoadout.IsValidLoadout())
        {
            Debug.LogError(
                "[ExplorationPartyLoadoutProvider][Day51] 연결된 Battle Loadout 데이터가 유효하지 않습니다.",
                this); // Loadout 데이터 유효성 오류 안내

            DeploymentValidation =
                new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.InvalidLoadout,
                    null,
                    0); // 잘못된 Loadout 출전 차단 저장

            return;
        }

        BattleResultManager resultManager =
            BattleResultManager.EnsureInstance(); // 전투·탐사 공용 파티 상태 관리자 준비

        if (!resultManager.RegisterParty(
                battleLoadout.Party))
        {
            Debug.LogError(
                "[ExplorationPartyLoadoutProvider][Day51] 탐사 출전 파티 등록에 실패했습니다.",
                this); // 파티 등록 실패 안내

            DeploymentValidation =
                new PartyDeploymentValidationResult(
                    false,
                    PartyDeploymentBlockReason.InvalidParty,
                    null,
                    0); // 파티 등록 실패 상태 저장

            return;
        }

        DeploymentValidation =
            battleLoadout.ValidateDeployment(); // 저장 HP·회복 상태 포함 출전 검증

        if (!DeploymentValidation.CanDeploy)
        {
            string blockedCharacterName =
                DeploymentValidation.BlockedCharacter != null
                    ? DeploymentValidation.BlockedCharacter.DisplayName
                    : "알 수 없음"; // 차단 캐릭터 표시 이름 결정

            Debug.LogWarning(
                $"[ExplorationPartyLoadoutProvider][Day58] 신규 탐사 편성 출전 제한 - " +
                $"{DeploymentValidation.BlockReason} / {blockedCharacterName}. " +
                "현재 진행 중 탐사의 상태 유지는 계속 허용하며, 신규 편성 UI에서는 해당 캐릭터를 선택 불가로 처리하세요.",
                this); // 신규 탐사 편성 차단 상태 안내
        }
    }
}
