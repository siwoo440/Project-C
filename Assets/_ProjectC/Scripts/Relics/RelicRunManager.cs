using UnityEngine; // 유니티 기본 기능 사용

public sealed class RelicRunManager : MonoBehaviour // 탐사 회차 유물 상태 관리자
{
    private static RelicRunManager instance; // 현재 유물 관리자 인스턴스
    private RelicGoldRuntime goldRuntime; // 임시 골드 지갑
    private RelicInventoryRuntime inventoryRuntime; // 획득 순서 기반 유물 보관함

    public static RelicRunManager Instance => instance; // 현재 유물 관리자 조회
    public RelicGoldRuntime Gold => goldRuntime; // 임시 골드 지갑 조회
    public RelicInventoryRuntime Inventory => inventoryRuntime; // 유물 보관함 조회

    public static RelicRunManager EnsureInstance() // 유물 관리자 준비
    {
        if (instance != null) // 기존 관리자 존재 확인
        {
            instance.EnsureRuntimeData(); // 런타임 데이터 준비
            return instance; // 기존 관리자 반환
        }

        RelicRunManager existingManager = FindFirstObjectByType<RelicRunManager>(); // 씬에 존재하는 관리자 조회
        if (existingManager != null) // 기존 씬 관리자 존재 확인
        {
            instance = existingManager; // 기존 관리자 인스턴스 저장
            instance.EnsureRuntimeData(); // 런타임 데이터 준비
            return instance; // 기존 씬 관리자 반환
        }

        GameObject managerObject = new GameObject("RelicRunManager"); // 유물 관리자 오브젝트 생성
        instance = managerObject.AddComponent<RelicRunManager>(); // 유물 관리자 컴포넌트 추가
        instance.EnsureRuntimeData(); // 런타임 데이터 준비
        return instance; // 새 관리자 반환
    }

    private void Awake() // 유물 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 존재 확인
        {
            Destroy(gameObject); // 중복 관리자 오브젝트 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 객체 전역 관리자 저장
        DontDestroyOnLoad(gameObject); // 씬 전환 시 관리자 유지
        EnsureRuntimeData(); // 런타임 데이터 준비
    }

    public RelicAcquireResult TryAcquire(RelicData relicData) // 유물 획득 요청
    {
        EnsureRuntimeData(); // 유물 런타임 준비
        var result = inventoryRuntime.TryAcquire(relicData); // 실제 획득과 중복 보상 처리
        if (result == RelicAcquireResult.Acquired && relicData.EffectType == RelicEffectType.ExtraPotionSlot && inventoryRuntime.ContainsRelic(relicData.RelicId)) // 획득 알림 이후에도 보유한 포션 보관대 확인
        { // 보관대 획득 시작
            ConsumableRunManager.EnsureInstance().Inventory.SetPotionRack(true); // 탐사와 전투의 포션칸 즉시 개방
        } // 보관대 획득 종료
        return result; // 실제 유물 획득 결과
    }

    public bool TryRemove(string relicId) // 유물 제거 요청
    {
        EnsureRuntimeData(); // 유물 런타임 준비
        var rack = inventoryRuntime.FindEffect(RelicEffectType.ExtraPotionSlot); // 보유한 포션 보관대 조회
        if (rack != null && rack.RelicId == relicId && !ConsumableRunManager.EnsureInstance().Inventory.SetPotionRack(false)) // 기본칸 이전 가능 여부와 용량 전환
        { // 제거 중단 시작
            return false; // 보관대와 포션 모두 보존
        } // 제거 중단 종료
        return inventoryRuntime.TryRemove(relicId); // 용량 확인 후 유물 제거
    }

    private void EnsureRuntimeData() // 내부 런타임 데이터 준비
    {
        if (goldRuntime == null) // 골드 지갑 존재 확인
        {
            goldRuntime = new RelicGoldRuntime(); // 임시 골드 지갑 생성
        }

        if (inventoryRuntime == null) // 유물 보관함 존재 확인
        {
            inventoryRuntime = new RelicInventoryRuntime(goldRuntime); // 골드 지갑 연결 유물 보관함 생성
        }
    }

    private void OnDestroy() // 유물 관리자 제거 처리
    {
        if (instance == this) // 현재 전역 관리자 여부 확인
        {
            instance = null; // 전역 관리자 참조 해제
        }
    }
}
