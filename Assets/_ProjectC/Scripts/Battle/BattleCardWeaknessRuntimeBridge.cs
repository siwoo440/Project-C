using System; // 구독 해제 자료형 사용
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능 사용

public sealed class BattleCardWeaknessRuntimeBridge : MonoBehaviour // 카드 사용과 약점 계산 연결
{
    private const string BattleSceneName = "40_Battle"; // 전투 Scene 이름

    private static BattleCardWeaknessRuntimeBridge instance; // 런타임 연결 인스턴스
    private BattleEventDispatcher subscribedDispatcher; // 현재 구독 전투 이벤트
    private IDisposable cardUsedSubscription; // 카드 사용 이벤트 구독

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 런타임 연결 자동 생성
    {
        if (instance != null)
        {
            return;
        }

        GameObject bridgeObject =
            new GameObject(nameof(BattleCardWeaknessRuntimeBridge)); // 연결 오브젝트 생성

        instance =
            bridgeObject.AddComponent<BattleCardWeaknessRuntimeBridge>(); // 연결 컴포넌트 추가

        DontDestroyOnLoad(bridgeObject); // Scene 이동 중 연결 유지
    }

    private void Awake() // 연결 인스턴스 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 연결 제거
            return;
        }

        instance = this; // 현재 연결 등록
        DontDestroyOnLoad(gameObject); // Scene 이동 유지
    }

    private void Update() // 전투 이벤트 연결 상태 갱신
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName)
        {
            Detach(); // 전투 Scene 외 이벤트 연결 해제
            return;
        }

        BattleSceneSetup battleSceneSetup =
            FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투 설정 탐색

        if (battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized ||
            battleSceneSetup.BattleEvents == null)
        {
            return; // 전투 초기화 완료까지 대기
        }

        if (subscribedDispatcher == battleSceneSetup.BattleEvents)
        {
            return; // 동일 이벤트 중복 구독 방지
        }

        Detach(); // 이전 전투 이벤트 연결 해제

        subscribedDispatcher =
            battleSceneSetup.BattleEvents; // 현재 전투 이벤트 저장

        cardUsedSubscription =
            subscribedDispatcher.Subscribe(
                BattleEventType.CardUsed,
                HandleCardUsed,
                1000); // 카드 사용 이벤트 우선 구독
    }

    private static void HandleCardUsed(
        BattleEventContext context) // 카드 사용 이벤트 처리
    {
        if (context == null ||
            context.Card == null ||
            context.Card.EffectType != CardEffectType.Damage ||
            context.TargetUnits == null ||
            context.TargetUnits.Count == 0)
        {
            BattleCardDamageContext.Clear(); // 비피해 카드 문맥 제거
            return;
        }

        BattleCardDamageContext.Begin(
            context.Card,
            context.TargetUnits); // 카드와 대상 순서 저장

        BattleDamageDebugView.EnsureInstance().BeginCard(
            context.Card,
            context.TargetUnits.Count); // 피해 계산 디버그 시작
    }

    private void Detach() // 현재 전투 이벤트 연결 해제
    {
        cardUsedSubscription?.Dispose(); // 카드 이벤트 구독 해제
        cardUsedSubscription = null; // 구독 참조 제거
        subscribedDispatcher = null; // 이벤트 참조 제거
        BattleCardDamageContext.Clear(); // 남은 카드 피해 문맥 제거
    }

    private void OnDestroy() // 연결 제거 처리
    {
        Detach(); // 이벤트 구독 해제

        if (instance == this)
        {
            instance = null; // 정적 인스턴스 제거
        }
    }
}
