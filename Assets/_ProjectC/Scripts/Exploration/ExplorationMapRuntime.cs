using System; // 현재 시간 기반 시드 생성
using UnityEngine; // 런타임 오브젝트 기능 사용
using UnityEngine.InputSystem; // 디버그 재생성 입력 사용
using UnityEngine.SceneManagement; // 탐사 Scene 감지 기능 사용

public sealed class ExplorationMapRuntime : MonoBehaviour // 36일차 절차 탐사 맵 런타임
{
    private const int DefaultCellCount = 14; // 기본 생성 셀 수

    public ExplorationMapData CurrentMap { get; private set; } // 현재 생성된 논리 맵

    private void Awake() // 탐사 맵 런타임 초기화
    {
        GenerateNewMap(); // 최초 절차 맵 생성
        EnsureDebugView(); // 디버그 맵 화면 추가
    }

    private void Update() // 디버그 입력 처리
    {
        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회

        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) // F9 재생성 입력 확인
        {
            GenerateNewMap(); // 새로운 절차 맵 생성
        }
    }

    public void GenerateNewMap() // 새 탐사 논리 맵 생성
    {
        int seed = unchecked((int)DateTime.UtcNow.Ticks ^ Time.frameCount); // 현재 시점 기반 임시 시드 생성
        CurrentMap = ExplorationMapGenerator.Generate(DefaultCellCount, seed); // 기본 셀 수 절차 맵 생성

        Debug.Log(
            $"[Exploration][Day36] 논리 맵 생성 완료 - " +
            $"Seed {CurrentMap.Seed} / " +
            $"Cells {CurrentMap.Cells.Count} / " +
            $"Start {CurrentMap.StartCoordinate} / " +
            $"Stairs {CurrentMap.StairsCoordinate}"); // 생성 결과 로그 출력
    }

    private void EnsureDebugView() // 맵 디버그 화면 존재 보장
    {
        if (GetComponent<ExplorationMapDebugView>() == null) // 디버그 화면 존재 확인
        {
            gameObject.AddComponent<ExplorationMapDebugView>(); // 디버그 화면 컴포넌트 추가
        }
    }
}

public static class ExplorationMapRuntimeBootstrap // 탐사 Scene 절차 맵 자동 생성기
{
    private const string ExplorationSceneName = "30_Exploration"; // 탐사 Scene 이름

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 Scene 로드 전 자동 등록
    private static void InitializeRuntime() // Scene 로드 이벤트 초기화
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 Scene 이벤트 등록 제거
        SceneManager.sceneLoaded += HandleSceneLoaded; // Scene 로드 이벤트 등록
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) // Scene 로드 완료 처리
    {
        if (scene.name != ExplorationSceneName) // 탐사 Scene 여부 확인
        {
            return; // 다른 Scene 처리 생략
        }

        if (UnityEngine.Object.FindFirstObjectByType<ExplorationMapRuntime>() != null) // 기존 맵 런타임 존재 확인
        {
            return; // 중복 런타임 생성 생략
        }

        GameObject runtimeObject = new GameObject("ExplorationMapRuntime"); // 절차 맵 런타임 오브젝트 생성
        runtimeObject.AddComponent<ExplorationMapRuntime>(); // 절차 맵 런타임 컴포넌트 추가
    }
}
