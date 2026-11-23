using System; // 이벤트와 열거형 기능 사용
using System.Collections.Generic; // 설비 레벨 사전 사용
using UnityEngine; // 유니티 오브젝트와 수학 기능 사용

public sealed class FacilityUpgradeManager : MonoBehaviour // 세룰리온 설비 영구 강화 관리자
{
    public const int MaximumLevel = 5; // 설비 최대 강화 레벨

    private static FacilityUpgradeManager instance; // 영구 관리자 인스턴스
    private readonly Dictionary<FacilityType, int> facilityLevels = new Dictionary<FacilityType, int>(); // 설비별 현재 레벨

    public static FacilityUpgradeManager Instance => instance; // 현재 관리자 조회
    public event Action<FacilityType, int> FacilityChanged; // 설비 강화 변경 이벤트

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 게임 시작 전 관리자 자동 준비
    private static void InitializeRuntime() // 런타임 자동 초기화
    {
        EnsureInstance(); // 영구 설비 관리자 생성
    }

    public static FacilityUpgradeManager EnsureInstance() // 설비 관리자 존재 보장
    {
        if (instance != null) // 기존 관리자 존재 확인
        {
            return instance; // 기존 관리자 반환
        }

        FacilityUpgradeManager existingManager = FindFirstObjectByType<FacilityUpgradeManager>(); // Scene 내 기존 관리자 탐색
        if (existingManager != null) // 기존 관리자 발견 확인
        {
            instance = existingManager; // 기존 관리자 저장
            instance.EnsureFacilityEntries(); // 설비 레벨 데이터 보장
            instance.EnsureDebugView(); // 임시 강화 UI 보장
            return instance; // 기존 관리자 반환
        }

        GameObject managerObject = new GameObject("FacilityUpgradeManager"); // 영구 관리자 오브젝트 생성
        instance = managerObject.AddComponent<FacilityUpgradeManager>(); // 관리자 컴포넌트 추가
        return instance; // 생성 관리자 반환
    }

    private void Awake() // 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 오브젝트 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 시 관리자 유지
        EnsureFacilityEntries(); // 전체 설비 레벨 초기화
        EnsureDebugView(); // 임시 강화 UI 추가
    }

    public int GetLevel(FacilityType type) // 현재 설비 레벨 조회
    {
        EnsureFacilityEntries(); // 설비 데이터 존재 보장
        return facilityLevels[type]; // 현재 레벨 반환
    }

    public FacilityDefinition GetDefinition(FacilityType type) // 설비 기본 데이터 조회
    {
        return FacilityCatalog.Get(type); // 카탈로그 설비 데이터 반환
    }

    public FacilityLevelDefinition GetCurrentLevelDefinition(FacilityType type) // 현재 레벨 효과 데이터 조회
    {
        return GetDefinition(type).GetLevelDefinition(GetLevel(type)); // 현재 레벨 데이터 반환
    }

    public FacilityLevelDefinition GetNextLevelDefinition(FacilityType type) // 다음 강화 데이터 조회
    {
        int nextLevel = GetLevel(type) + 1; // 다음 강화 레벨 계산
        return GetDefinition(type).GetLevelDefinition(nextLevel); // 다음 강화 데이터 반환
    }

    public bool CanUpgrade(FacilityType type) // 현재 자원으로 강화 가능 여부 확인
    {
        FacilityLevelDefinition nextLevel = GetNextLevelDefinition(type); // 다음 강화 데이터 조회
        if (nextLevel == null) // 최대 레벨 여부 확인
        {
            return false; // 최대 레벨 강화 불가 반환
        }

        PlayerResourceManager resourceManager = PlayerResourceManager.EnsureInstance(); // 플레이어 자원 관리자 준비
        return resourceManager.CanAfford(nextLevel.GoldCost, nextLevel.ScrewCost, nextLevel.IronPlateCost, nextLevel.WireCost); // 강화 비용 보유 여부 반환
    }

    public bool TryUpgrade(FacilityType type) // 설비 강화 시도
    {
        FacilityLevelDefinition nextLevel = GetNextLevelDefinition(type); // 다음 강화 데이터 조회
        if (nextLevel == null) // 최대 레벨 여부 확인
        {
            return false; // 최대 레벨 강화 실패 반환
        }

        PlayerResourceManager resourceManager = PlayerResourceManager.EnsureInstance(); // 플레이어 자원 관리자 준비
        if (!resourceManager.TrySpend(nextLevel.GoldCost, nextLevel.ScrewCost, nextLevel.IronPlateCost, nextLevel.WireCost)) // 강화 비용 차감 시도
        {
            return false; // 자원 부족 강화 실패 반환
        }

        facilityLevels[type] = nextLevel.Level; // 강화된 레벨 저장
        FacilityChanged?.Invoke(type, nextLevel.Level); // 설비 변경 이벤트 전달
        return true; // 강화 성공 반환
    }

    public void ResetFacilities() // 모든 설비 강화 초기화
    {
        Array facilityTypes = Enum.GetValues(typeof(FacilityType)); // 전체 설비 종류 목록 조회
        foreach (FacilityType type in facilityTypes) // 전체 설비 종류 순회
        {
            facilityLevels[type] = 0; // 설비 레벨 미강화 상태로 초기화
            FacilityChanged?.Invoke(type, 0); // 초기화 변경 이벤트 전달
        }
    }

    public int ApplyResourceRewardBonus(int baseAmount) // 물자 창고 일반 자원 보너스 적용
    {
        int safeAmount = Mathf.Max(0, baseAmount); // 음수 자원량 방지
        int bonusPercent = GetWarehouseResourceBonusPercent(); // 물자 창고 보너스 조회
        return Mathf.FloorToInt(safeAmount * (100f + bonusPercent) / 100f); // 보정된 일반 자원량 반환
    }

    public int GetPowerSupplyMentalBonus() // 전력 공급기 시작 정신력 보너스 조회
    {
        return GetLevel(FacilityType.PowerSupply) * 3; // 레벨당 정신력 3 보너스 반환
    }

    public int GetPowerSupplyFirstTurnActionPointBonus() // 전력 공급기 첫 턴 AP 보너스 조회
    {
        return GetLevel(FacilityType.PowerSupply) >= MaximumLevel ? 1 : 0; // 최대 레벨 첫 턴 AP 보너스 반환
    }

    public int GetDefenseBarrierDamageReductionPercent() // 방어 차폐 장치 피해 감소율 조회
    {
        return GetLevel(FacilityType.DefenseBarrier) * 3; // 레벨당 피해 감소율 3 반환
    }

    public int GetMagicConverterDamageBonusPercent() // 마력 변환기 피해 증가율 조회
    {
        int level = GetLevel(FacilityType.MagicConverter); // 마력 변환기 레벨 조회
        return level <= 0 ? 0 : 5 + (level - 1) * 3; // 레벨별 마법 피해 보너스 반환
    }

    public int GetAutoRecoveryPercent() // 자율 회복 장치 회복률 조회
    {
        int level = GetLevel(FacilityType.AutoRecovery); // 자율 회복 장치 레벨 조회
        int[] values = { 0, 3, 5, 5, 7, 10 }; // 레벨별 회복률 표
        return values[Mathf.Clamp(level, 0, MaximumLevel)]; // 현재 레벨 회복률 반환
    }

    public int GetDataAnalyzerWeaknessBonusPercent() // 데이터 분석기 약점 피해 보너스 조회
    {
        int level = GetLevel(FacilityType.DataAnalyzer); // 데이터 분석기 레벨 조회
        return level >= 5 ? 15 : level >= 4 ? 10 : 0; // 약점 피해 보너스 반환
    }

    public int GetWarehouseResourceBonusPercent() // 물자 창고 일반 자원 획득 보너스 조회
    {
        return GetLevel(FacilityType.WarehouseExpansion) * 5; // 레벨당 일반 자원 보너스 5 반환
    }

    public int GetCombatTrainingAttackBonusPercent() // 전투 훈련실 공격력 보너스 조회
    {
        return GetLevel(FacilityType.CombatTraining) * 3; // 레벨당 공격력 보너스 3 반환
    }

    public int GetEmergencyRepairReviveChancePercent() // 응급 수복장치 부활 확률 조회
    {
        int level = GetLevel(FacilityType.EmergencyRepair); // 응급 수복장치 레벨 조회
        int[] values = { 0, 20, 35, 50, 75, 100 }; // 레벨별 부활 확률 표
        return values[Mathf.Clamp(level, 0, MaximumLevel)]; // 현재 레벨 부활 확률 반환
    }

    public int GetCommunicationEventDiscoveryBonusPercent() // 통신 기지국 이벤트 발견 보너스 조회
    {
        return GetLevel(FacilityType.CommunicationStation) * 5; // 레벨당 이벤트 발견 보너스 5 반환
    }

    public int GetEnvironmentDamageReductionPercent() // 환경 정화 장치 환경 피해 감소율 조회
    {
        int level = GetLevel(FacilityType.EnvironmentPurifier); // 환경 정화 장치 레벨 조회
        int[] values = { 0, 10, 15, 20, 25, 30 }; // 레벨별 환경 피해 감소율 표
        return values[Mathf.Clamp(level, 0, MaximumLevel)]; // 현재 레벨 환경 피해 감소율 반환
    }

    private void EnsureFacilityEntries() // 모든 설비 레벨 항목 보장
    {
        Array facilityTypes = Enum.GetValues(typeof(FacilityType)); // 전체 설비 종류 조회
        foreach (FacilityType type in facilityTypes) // 전체 설비 종류 순회
        {
            if (!facilityLevels.ContainsKey(type)) // 설비 레벨 항목 존재 확인
            {
                facilityLevels.Add(type, 0); // 미강화 레벨 항목 추가
            }
        }
    }

    private void EnsureDebugView() // 임시 설비 강화 UI 존재 보장
    {
        if (GetComponent<FacilityUpgradeDebugView>() == null) // 임시 강화 UI 존재 확인
        {
            gameObject.AddComponent<FacilityUpgradeDebugView>(); // 임시 강화 UI 컴포넌트 추가
        }
    }

    private void OnDestroy() // 관리자 제거 처리
    {
        if (instance == this) // 현재 영구 관리자 제거 여부 확인
        {
            instance = null; // 정적 관리자 참조 초기화
        }
    }
}
