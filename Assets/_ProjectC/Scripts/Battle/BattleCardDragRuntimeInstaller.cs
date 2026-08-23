using UnityEngine; // 런타임 컴포넌트 설치 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능 사용

public sealed class BattleCardDragRuntimeInstaller : MonoBehaviour // 카드 드래그 입력 런타임 설치기
{
    private const string BattleSceneName = "40_Battle"; // 전투 Scene 이름

    private static BattleCardDragRuntimeInstaller instance; // 설치기 인스턴스

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 런타임 설치기 자동 생성
    {
        if (instance != null)
        {
            return;
        }

        GameObject installerObject =
            new GameObject(nameof(BattleCardDragRuntimeInstaller)); // 설치기 오브젝트 생성

        instance =
            installerObject.AddComponent<BattleCardDragRuntimeInstaller>(); // 설치기 컴포넌트 추가

        DontDestroyOnLoad(installerObject); // Scene 이동 중 설치기 유지
    }

    private void Awake() // 설치기 인스턴스 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 설치기 제거
            return;
        }

        instance = this; // 현재 설치기 등록
        DontDestroyOnLoad(gameObject); // Scene 이동 중 설치기 유지
    }

    private void Update() // 현재 손패 카드에 드래그 입력 연결
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName)
        {
            return;
        }

        BattleCardView[] cardViews =
            FindObjectsByType<BattleCardView>(
                FindObjectsSortMode.None); // 현재 손패 카드 화면 조회

        for (int index = 0; index < cardViews.Length; index += 1)
        {
            BattleCardView cardView =
                cardViews[index]; // 현재 카드 화면 조회

            if (cardView == null)
            {
                continue;
            }

            BattleCardDragHandler dragHandler =
                cardView.GetComponent<BattleCardDragHandler>(); // 기존 드래그 처리기 조회

            if (dragHandler == null)
            {
                dragHandler =
                    cardView.gameObject.AddComponent<BattleCardDragHandler>(); // 카드 드래그 처리기 추가
            }

            dragHandler.Initialize(
                cardView); // 현재 카드 화면 연결

            cardView.enabled = false; // 기존 클릭 직접 사용 입력 차단
        }
    }

    private void OnDestroy() // 설치기 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 설치기 참조 제거
        }
    }
}
