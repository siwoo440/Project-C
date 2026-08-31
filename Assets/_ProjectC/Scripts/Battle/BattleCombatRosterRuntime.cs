using UnityEngine.SceneManagement; // 현재 Scene 이름 조회

public static class BattleCombatRosterRuntime // Battle Scene 전용 출전 필터 활성 상태
{
    public const string BattleSceneName = "40_Battle"; // 프로젝트 전투 Scene 이름

    public static bool ShouldFilterCurrentScene =>
        SceneManager.GetActiveScene().name == BattleSceneName; // 전투 Scene에서만 실제 출전 필터 활성화
}
