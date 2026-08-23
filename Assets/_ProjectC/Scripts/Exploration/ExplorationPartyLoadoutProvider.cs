using UnityEngine; // 유니티 Scene 컴포넌트와 로그 사용

[DefaultExecutionOrder(-10000)] // 런타임 탐사 맵 생성보다 먼저 파티 상태 등록
public sealed class ExplorationPartyLoadoutProvider : MonoBehaviour // 탐사 Scene 출전 파티 Loadout 제공
{
    [Header("탐사 출전 파티")] // 탐사 파티 연결 구역
    [SerializeField] private BattleLoadoutData battleLoadout; // 전투 Scene과 동일한 출전 파티·덱 데이터

    public BattleLoadoutData BattleLoadout =>
        battleLoadout; // 현재 연결 Loadout 조회

    private void Awake() // 탐사 Scene 로드 시 출전 파티 등록
    {
        if (battleLoadout == null)
        {
            Debug.LogError(
                "[ExplorationPartyLoadoutProvider][Day51] Battle Loadout이 연결되지 않았습니다. " +
                "40_Battle의 BattleSceneSetup과 같은 BattleLoadoutData를 연결하세요.",
                this); // Loadout 연결 누락 안내

            return;
        }

        if (!battleLoadout.IsValidLoadout())
        {
            Debug.LogError(
                "[ExplorationPartyLoadoutProvider][Day51] 연결된 Battle Loadout 데이터가 유효하지 않습니다.",
                this); // Loadout 데이터 유효성 오류 안내

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
        }
    }
}
