using System.Collections; // 코루틴 자료형 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용

[DefaultExecutionOrder(220)] // 기존 전투와 유물 초기화 이후 실행 순서
public sealed class BattleConsumableBootstrap : MonoBehaviour // 전투 소모품 시스템 연결 컴포넌트
{
    [Header("전투 연결")] // 전투 연결 구역
    [SerializeField] private BattleSceneSetup battleSceneSetup; // 현재 전투 씬 초기화 컴포넌트
    [SerializeField] private Canvas targetCanvas; // 소모품 슬롯 배치 Canvas

    [Header("테스트 시작 소모품")] // 테스트 소모품 구역
    [SerializeField] private List<ConsumableItemData> debugStartingItems = new List<ConsumableItemData>(); // 첫 빈칸부터 테스트 소모품 배치

    private BattleConsumableController consumableController; // 전투 소모품 사용 관리자
    private ConsumableSlotBarView slotBarView; // 왼쪽 위 열 칸 슬롯 화면
    private Coroutine initializeCoroutine; // 전투 초기화 대기 코루틴

    private void Start() // 소모품 시스템 연결 시작
    {
        initializeCoroutine = StartCoroutine(InitializeWhenBattleReady()); // 전투 초기화 완료까지 대기
    }

    private IEnumerator InitializeWhenBattleReady() // 전투 초기화 완료 후 소모품 연결
    {
        if (battleSceneSetup == null) // 전투 초기화 연결 여부 확인
        {
            battleSceneSetup = GetComponent<BattleSceneSetup>(); // 같은 오브젝트에서 전투 초기화 조회
        }

        if (battleSceneSetup == null) // 같은 오브젝트 조회 실패 확인
        {
            battleSceneSetup = FindFirstObjectByType<BattleSceneSetup>(); // 현재 Scene 전투 초기화 조회
        }

        if (battleSceneSetup == null) // 전투 초기화 최종 확인
        {
            Debug.LogError("[BattleConsumableBootstrap] BattleSceneSetup을 찾을 수 없습니다.", this); // 전투 연결 오류 출력
            yield break; // 소모품 초기화 종료
        }

        while (!battleSceneSetup.IsInitialized) // 기존 전투 초기화 완료 대기
        {
            yield return null; // 다음 프레임까지 대기
        }

        ConsumableRunManager runManager = ConsumableRunManager.EnsureInstance(); // 탐사 회차 소모품 관리자 준비
        if (runManager.Inventory.Count == 0) // 기존 보유 소모품 없음 확인
        {
            AcquireDebugStartingItems(runManager); // Inspector 테스트 소모품 첫 빈칸 배치
        }

        consumableController = new BattleConsumableController(runManager.Inventory, battleSceneSetup); // 전투 포션 사용과 정리 관리자 생성
        Canvas canvas = targetCanvas == null ? FindBattleCanvas() : targetCanvas; // 소모품 UI Canvas 결정
        if (canvas == null) // Canvas 존재 확인
        {
            Debug.LogError("[BattleConsumableBootstrap] 소모품 슬롯을 배치할 Canvas를 찾을 수 없습니다.", this); // Canvas 누락 오류 출력
            consumableController.Dispose(); // 전투 관리자 연결 해제
            consumableController = null; // 전투 관리자 참조 제거
            yield break; // 소모품 초기화 종료
        }

        slotBarView = ConsumableSlotBarView.Create(canvas, consumableController, runManager.Inventory); // 왼쪽 위 가로 오 세로 이 슬롯 화면 생성
        initializeCoroutine = null; // 초기화 코루틴 완료 상태 저장
    }

    private void AcquireDebugStartingItems(ConsumableRunManager runManager) // 테스트 시작 소모품 획득
    {
        for (int index = 0; index < debugStartingItems.Count; index++) // 테스트 소모품 순회
        {
            ConsumableItemData itemData = debugStartingItems[index]; // 현재 테스트 소모품 조회
            if (itemData == null) // 빈 테스트 데이터 확인
            {
                continue; // 다음 테스트 소모품 이동
            }

            bool acquired = runManager.TryAcquire(itemData, out int slotIndex); // 첫 빈 슬롯 획득 시도
            Debug.Log($"[BattleConsumableBootstrap] 테스트 소모품 - {itemData.DisplayName} / 슬롯 {slotIndex + 1} / 성공 {acquired}", this); // 테스트 획득 결과 출력
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

    private void OnDestroy() // 전투 소모품 연결 해제
    {
        if (initializeCoroutine != null) // 초기화 대기 진행 확인
        {
            StopCoroutine(initializeCoroutine); // 초기화 대기 중단
            initializeCoroutine = null; // 코루틴 참조 제거
        }

        if (slotBarView != null) // 슬롯 화면 존재 확인
        {
            slotBarView.Dispose(); // 슬롯 화면 이벤트 연결 해제
            Destroy(slotBarView.gameObject); // 런타임 슬롯 화면 제거
            slotBarView = null; // 슬롯 화면 참조 제거
        }

        consumableController?.Dispose(); // 전투 소모품 이벤트 연결 해제
        consumableController = null; // 전투 소모품 관리자 참조 제거
    }
}
