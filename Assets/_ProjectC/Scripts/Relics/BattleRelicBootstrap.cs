using System.Collections; // 코루틴 자료형 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용

[DefaultExecutionOrder(200)] // 기존 전투 초기화 이후 유물 연결 순서
public sealed class BattleRelicBootstrap : MonoBehaviour // 전투 씬 유물 시스템 연결 컴포넌트
{
    [Header("전투 연결")] // 전투 연결 구역
    [SerializeField] private BattleSceneSetup battleSceneSetup; // 현재 전투 씬 초기화 컴포넌트
    [SerializeField] private Canvas debugCanvas; // 유물 디버그 창 배치 Canvas

    [Header("유물 디버그")] // 유물 디버그 구역
    [SerializeField] private bool showDebugWindowAtStart = true; // 시작 시 디버그 패널 표시 여부
    [SerializeField] private List<RelicData> debugStartingRelics = new List<RelicData>(); // 테스트용 시작 유물 획득 순서

    private BattleRelicEffectController relicEffectController; // 전투 유물 효과 관리자
    private RelicDebugWindow relicDebugWindow; // 유물 순서 디버그 창
    private Coroutine initializeCoroutine; // 유물 연결 대기 코루틴

    private void Start() // 유물 시스템 연결 시작
    {
        initializeCoroutine = StartCoroutine(InitializeWhenBattleReady()); // 전투 초기화 완료까지 대기 시작
    }

    private IEnumerator InitializeWhenBattleReady() // 전투 초기화 완료 후 유물 연결
    {
        if (battleSceneSetup == null) // 전투 초기화 컴포넌트 연결 여부 확인
        {
            battleSceneSetup = GetComponent<BattleSceneSetup>(); // 같은 오브젝트 전투 초기화 컴포넌트 조회
        }

        if (battleSceneSetup == null) // 전투 초기화 컴포넌트 최종 확인
        {
            Debug.LogError("[BattleRelicBootstrap] BattleSceneSetup을 찾을 수 없습니다.", this); // 전투 연결 오류 출력
            yield break; // 유물 초기화 종료
        }

        while (!battleSceneSetup.IsInitialized) // 기존 전투 초기화 완료 대기
        {
            yield return null; // 다음 프레임까지 대기
        }

        RelicRunManager runManager = RelicRunManager.EnsureInstance(); // 탐사 회차 유물 관리자 준비
        relicEffectController = new BattleRelicEffectController(battleSceneSetup.BattleEvents, runManager.Inventory, runManager.Gold, battleSceneSetup.AllyUnits, battleSceneSetup.EnemyUnits); // 현재 전투에 유물 효과 관리자 연결

        if (runManager.Inventory.Count == 0) // 기존 보유 유물 없음 확인
        {
            AcquireDebugStartingRelics(runManager); // Inspector 순서대로 테스트 유물 획득
        }

        relicEffectController.ProcessInitialBattleState(battleSceneSetup.BattleTurn.CurrentPhase, battleSceneSetup.BattleTurn.CurrentRound); // 이미 지나간 전투 시작과 첫 턴 유물 효과 보정
        Canvas targetCanvas = debugCanvas == null ? FindBattleCanvas() : debugCanvas; // 디버그 창 배치 Canvas 결정
        if (targetCanvas != null) // 디버그 Canvas 존재 확인
        {
            relicDebugWindow = RelicDebugWindow.Create(targetCanvas, runManager, showDebugWindowAtStart); // 5열 유물 순서 디버그 창 생성
        }
        else // 디버그 Canvas 누락 처리
        {
            Debug.LogError("[BattleRelicBootstrap] 유물 디버그 창을 배치할 Canvas를 찾을 수 없습니다.", this); // Canvas 누락 오류 출력
        }

        initializeCoroutine = null; // 초기화 코루틴 완료 상태 저장
    }

    private void AcquireDebugStartingRelics(RelicRunManager runManager) // 테스트 시작 유물 순서 적용
    {
        foreach (RelicData relicData in debugStartingRelics) // Inspector 등록 순서대로 유물 순회
        {
            if (relicData == null) // 빈 테스트 유물 확인
            {
                continue; // 다음 테스트 유물 확인
            }

            RelicAcquireResult acquireResult = runManager.TryAcquire(relicData); // 현재 순서 유물 획득 또는 중복 변환
            Debug.Log($"[BattleRelicBootstrap] 테스트 유물 처리 - {relicData.DisplayName} / {acquireResult}", this); // 테스트 획득 결과 출력
        }
    }

    private static Canvas FindBattleCanvas() // 전투 UI Canvas 자동 조회
    {
        BattleHandView handView = FindFirstObjectByType<BattleHandView>(); // 현재 전투 손패 화면 조회
        if (handView == null) // 손패 화면 존재 확인
        {
            return null; // Canvas 조회 실패 반환
        }

        return handView.GetComponentInParent<Canvas>(); // 손패 화면 부모 Canvas 반환
    }

    private void OnDestroy() // 전투 유물 연결 해제
    {
        if (initializeCoroutine != null) // 실행 중인 초기화 대기 확인
        {
            StopCoroutine(initializeCoroutine); // 초기화 대기 코루틴 중단
            initializeCoroutine = null; // 코루틴 참조 제거
        }

        relicEffectController?.Dispose(); // 전투 유물 효과 이벤트 연결 해제
        relicEffectController = null; // 전투 유물 효과 관리자 참조 제거
        if (relicDebugWindow != null) // 디버그 창 존재 확인
        {
            relicDebugWindow.Dispose(); // 디버그 창 이벤트 연결 해제
            Destroy(relicDebugWindow.gameObject); // 디버그 창 오브젝트 제거
            relicDebugWindow = null; // 디버그 창 참조 제거
        }
    }
}
