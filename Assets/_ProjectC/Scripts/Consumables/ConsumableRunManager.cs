using UnityEngine; // 유니티 기본 기능 사용

public sealed class ConsumableRunManager : MonoBehaviour // 탐사 회차 소모품 상태 관리자
{
    private static ConsumableRunManager instance; // 현재 소모품 관리자 인스턴스
    private ConsumableInventoryRuntime inventoryRuntime; // 고정 슬롯 소모품 보관함

    public static ConsumableRunManager Instance => instance; // 현재 소모품 관리자 조회
    public ConsumableInventoryRuntime Inventory => inventoryRuntime; // 소모품 보관함 조회

    public static ConsumableRunManager EnsureInstance() // 소모품 관리자 준비
    {
        if (instance != null) // 기존 관리자 확인
        {
            instance.EnsureRuntimeData(); // 런타임 데이터 준비
            return instance; // 기존 관리자 반환
        }

        ConsumableRunManager existingManager = FindFirstObjectByType<ConsumableRunManager>(); // 씬 기존 관리자 조회
        if (existingManager != null) // 기존 씬 관리자 확인
        {
            instance = existingManager; // 기존 관리자 참조 저장
            instance.EnsureRuntimeData(); // 런타임 데이터 준비
            return instance; // 기존 씬 관리자 반환
        }

        GameObject managerObject = new GameObject("ConsumableRunManager"); // 소모품 관리자 오브젝트 생성
        instance = managerObject.AddComponent<ConsumableRunManager>(); // 소모품 관리자 컴포넌트 추가
        instance.EnsureRuntimeData(); // 런타임 데이터 준비
        return instance; // 새 관리자 반환
    }

    private void Awake() // 소모품 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 관리자 전역 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 유지 적용
        EnsureRuntimeData(); // 런타임 데이터 준비
    }

    public bool TryAcquire(ConsumableItemData itemData, out int slotIndex) // 소모품 획득 요청
    {
        EnsureRuntimeData(); // 소모품 보관함 준비
        return inventoryRuntime.TryAcquire(itemData, out slotIndex); // 첫 빈칸 획득 결과 반환
    }

    private void EnsureRuntimeData() // 내부 런타임 데이터 준비
    {
        if (inventoryRuntime == null) // 보관함 존재 확인
        {
            inventoryRuntime = new ConsumableInventoryRuntime(); // 고정 슬롯 보관함 생성
        }
    }

    private void OnDestroy() // 소모품 관리자 제거 처리
    {
        if (instance == this) // 현재 전역 관리자 여부 확인
        {
            instance = null; // 전역 관리자 참조 제거
        }
    }
}
