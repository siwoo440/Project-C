using UnityEngine; // 런타임 UI 위치 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능 사용

public sealed class BattleEnemyIntentOffsetRuntime : MonoBehaviour // 적 행동 예고 위치 보정
{
    private const string BattleSceneName = "40_Battle"; // 전투 Scene 이름
    private const float IntentYPosition = 18f; // 행동 예고 상향 위치

    private static BattleEnemyIntentOffsetRuntime instance; // 위치 보정 인스턴스

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 위치 보정 자동 생성
    {
        if (instance != null)
        {
            return;
        }

        GameObject offsetObject =
            new GameObject(nameof(BattleEnemyIntentOffsetRuntime)); // 위치 보정 오브젝트 생성

        instance =
            offsetObject.AddComponent<BattleEnemyIntentOffsetRuntime>(); // 위치 보정 컴포넌트 추가

        DontDestroyOnLoad(offsetObject); // Scene 이동 유지
    }

    private void Awake() // 위치 보정 인스턴스 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 보정 오브젝트 제거
            return;
        }

        instance = this; // 현재 인스턴스 등록
        DontDestroyOnLoad(gameObject); // Scene 이동 유지
    }

    private void LateUpdate() // 런타임 생성 행동 UI 위치 보정
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName)
        {
            return;
        }

        BattleUnitView[] unitViews =
            FindObjectsByType<BattleUnitView>(
                FindObjectsSortMode.None); // 현재 전투 유닛 화면 조회

        for (int index = 0; index < unitViews.Length; index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView == null)
            {
                continue;
            }

            Transform intentTransform =
                unitView.transform.Find("EnemyIntent"); // 런타임 행동 예고 탐색

            if (intentTransform == null)
            {
                continue;
            }

            RectTransform intentRect =
                intentTransform as RectTransform; // 행동 예고 RectTransform 변환

            if (intentRect == null)
            {
                continue;
            }

            Vector2 position =
                intentRect.anchoredPosition; // 현재 행동 예고 위치 조회

            if (Mathf.Approximately(
                    position.y,
                    IntentYPosition))
            {
                continue;
            }

            intentRect.anchoredPosition =
                new Vector2(
                    position.x,
                    IntentYPosition); // 기존 -6 위치를 +18로 상향 보정
        }
    }

    private void OnDestroy() // 위치 보정 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 인스턴스 제거
        }
    }
}
