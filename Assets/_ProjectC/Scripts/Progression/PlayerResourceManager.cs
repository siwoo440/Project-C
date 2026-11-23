using System; // 기본 이벤트 기능 사용
using UnityEngine; // 유니티 기본 기능 사용

public sealed class PlayerResourceManager : MonoBehaviour // 플레이어 영구 자원 관리자
{
    private static PlayerResourceManager instance; // 영구 관리자 인스턴스
    private RelicGoldRuntime goldRuntime; // 기존 유물 골드 런타임 참조
    private bool suppressGoldEvent; // 골드 이벤트 중복 전달 방지
    private bool trackingExplorationResources; // 탐사 획득 재화 추적 여부
    private int observedGold; // 골드 이벤트 직전 보유량
    private int explorationGold; // 현재 남은 탐사 획득 골드
    private int explorationScrew; // 현재 남은 탐사 획득 나사
    private int explorationIronPlate; // 현재 남은 탐사 획득 철판
    private int explorationWire; // 현재 남은 탐사 획득 전선

    public static PlayerResourceManager Instance => instance; // 현재 자원 관리자 조회

    public int Gold // 현재 골드 조회
    {
        get // 골드 값 반환
        {
            EnsureGoldRuntime(); // 골드 런타임 연결 보장
            return goldRuntime.Gold; // 현재 골드 반환
        }
    }

    public int Screw { get; private set; } // 현재 나사 수량
    public int IronPlate { get; private set; } // 현재 철판 수량
    public int Wire { get; private set; } // 현재 전선 수량
    public bool IsTrackingExplorationResources => trackingExplorationResources; // 탐사 재화 추적 상태 조회
    public int ExplorationGold => explorationGold; // 남은 탐사 골드 조회
    public int ExplorationScrew => explorationScrew; // 남은 탐사 나사 조회
    public int ExplorationIronPlate => explorationIronPlate; // 남은 탐사 철판 조회
    public int ExplorationWire => explorationWire; // 남은 탐사 전선 조회

    public event Action ResourcesChanged; // 자원 변경 이벤트

    public static PlayerResourceManager EnsureInstance() // 자원 관리자 존재 보장
    {
        if (instance != null) // 기존 관리자 존재 확인
        {
            instance.EnsureGoldRuntime(); // 기존 골드 런타임 연결 확인
            return instance; // 기존 관리자 반환
        }

        PlayerResourceManager existingManager = FindFirstObjectByType<PlayerResourceManager>(); // Scene 내 기존 관리자 탐색
        if (existingManager != null) // 기존 관리자 발견 확인
        {
            instance = existingManager; // 기존 관리자 저장
            instance.EnsureGoldRuntime(); // 골드 런타임 연결
            return instance; // 기존 관리자 반환
        }

        GameObject managerObject = new GameObject("PlayerResourceManager"); // 자원 관리자 오브젝트 생성
        instance = managerObject.AddComponent<PlayerResourceManager>(); // 자원 관리자 컴포넌트 추가
        instance.EnsureGoldRuntime(); // 골드 런타임 연결
        return instance; // 생성 관리자 반환
    }

    private void Awake() // 자원 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 시 자원 유지
        EnsureGoldRuntime(); // 골드 런타임 연결
    }

    public void AddClearReward(int gold, int screw, int ironPlate, int wire) // 전투 클리어 보상 지급
    {
        int safeGold = Mathf.Max(0, gold); // 음수 골드 방지
        int safeScrew = Mathf.Max(0, screw); // 음수 나사 방지
        int safeIronPlate = Mathf.Max(0, ironPlate); // 음수 철판 방지
        int safeWire = Mathf.Max(0, wire); // 음수 전선 방지
        FacilityUpgradeManager facilityManager = FacilityUpgradeManager.EnsureInstance(); // 설비 강화 관리자 준비
        int rewardedScrew = facilityManager.ApplyResourceRewardBonus(safeScrew); // 물자 창고 나사 보너스 적용
        int rewardedIronPlate = facilityManager.ApplyResourceRewardBonus(safeIronPlate); // 물자 창고 철판 보너스 적용
        int rewardedWire = facilityManager.ApplyResourceRewardBonus(safeWire); // 물자 창고 전선 보너스 적용
        AddResources(safeGold, rewardedScrew, rewardedIronPlate, rewardedWire); // 최종 보상 자원 지급
    }

    public void AddResources(int gold, int screw, int ironPlate, int wire) // 보정 없는 자원 직접 지급
    {
        EnsureGoldRuntime(); // 골드 런타임 연결 보장
        int safeGold = Mathf.Max(0, gold); // 음수 골드 방지
        int safeScrew = Mathf.Max(0, screw); // 음수 나사 방지
        int safeIronPlate = Mathf.Max(0, ironPlate); // 음수 철판 방지
        int safeWire = Mathf.Max(0, wire); // 음수 전선 방지
        suppressGoldEvent = true; // 골드 단독 변경 이벤트 억제
        goldRuntime.AddGold(safeGold); // 골드 지급
        suppressGoldEvent = false; // 골드 이벤트 억제 해제
        Screw += safeScrew; // 나사 지급
        IronPlate += safeIronPlate; // 철판 지급
        Wire += safeWire; // 전선 지급
        if (trackingExplorationResources) // 탐사 획득 재화 추적 여부 확인
        { // 탐사 자원 증가 처리 시작
            explorationScrew = AddExplorationResource(explorationScrew, safeScrew, Screw); // 탐사 나사 획득량 반영
            explorationIronPlate = AddExplorationResource(explorationIronPlate, safeIronPlate, IronPlate); // 탐사 철판 획득량 반영
            explorationWire = AddExplorationResource(explorationWire, safeWire, Wire); // 탐사 전선 획득량 반영
        } // 탐사 자원 증가 처리 종료
        ResourcesChanged?.Invoke(); // 전체 자원 변경 알림
    }

    public bool CanAfford(int gold, int screw, int ironPlate, int wire) // 지정 비용 지불 가능 여부 확인
    {
        EnsureGoldRuntime(); // 골드 런타임 연결 보장
        int safeGold = Mathf.Max(0, gold); // 음수 골드 비용 방지
        int safeScrew = Mathf.Max(0, screw); // 음수 나사 비용 방지
        int safeIronPlate = Mathf.Max(0, ironPlate); // 음수 철판 비용 방지
        int safeWire = Mathf.Max(0, wire); // 음수 전선 비용 방지
        return goldRuntime.CanAfford(safeGold) && Screw >= safeScrew && IronPlate >= safeIronPlate && Wire >= safeWire; // 전체 자원 충족 여부 반환
    }

    public bool TrySpend(int gold, int screw, int ironPlate, int wire) // 지정 비용 차감 시도
    {
        if (!CanAfford(gold, screw, ironPlate, wire)) // 전체 비용 보유 여부 확인
        {
            return false; // 자원 부족 실패 반환
        }

        int safeGold = Mathf.Max(0, gold); // 음수 골드 비용 방지
        int safeScrew = Mathf.Max(0, screw); // 음수 나사 비용 방지
        int safeIronPlate = Mathf.Max(0, ironPlate); // 음수 철판 비용 방지
        int safeWire = Mathf.Max(0, wire); // 음수 전선 비용 방지
        suppressGoldEvent = true; // 골드 단독 변경 이벤트 억제
        if (!goldRuntime.TrySpend(safeGold)) // 골드 차감 시도
        {
            suppressGoldEvent = false; // 골드 이벤트 억제 해제
            return false; // 골드 차감 실패 반환
        }

        suppressGoldEvent = false; // 골드 이벤트 억제 해제
        Screw -= safeScrew; // 나사 차감
        IronPlate -= safeIronPlate; // 철판 차감
        Wire -= safeWire; // 전선 차감
        if (trackingExplorationResources) // 탐사 재화 추적 여부 확인
        { // 기존 재화 우선 차감 시작
            explorationScrew = Mathf.Min(explorationScrew, Screw); // 남은 탐사 나사 계산
            explorationIronPlate = Mathf.Min(explorationIronPlate, IronPlate); // 남은 탐사 철판 계산
            explorationWire = Mathf.Min(explorationWire, Wire); // 남은 탐사 전선 계산
        } // 기존 재화 우선 차감 종료
        ResourcesChanged?.Invoke(); // 전체 자원 변경 알림
        return true; // 자원 차감 성공 반환
    }

    public void RestoreResources(int gold, int screw, int ironPlate, int wire) // 저장 자원 복원
    { // 자원 복원 시작
        ResetResources(); // 기존 자원 초기화
        AddResources(gold, screw, ironPlate, wire); // 저장 자원 적용
    } // 자원 복원 종료

    public void ResetResources() // 모든 영구 자원 초기화
    {
        EndExplorationResourceTracking(); // 이전 탐사 재화 구분 해제
        EnsureGoldRuntime(); // 골드 런타임 연결 보장
        suppressGoldEvent = true; // 골드 단독 변경 이벤트 억제
        goldRuntime.ResetGold(); // 골드 초기화
        suppressGoldEvent = false; // 골드 이벤트 억제 해제
        Screw = 0; // 나사 초기화
        IronPlate = 0; // 철판 초기화
        Wire = 0; // 전선 초기화
        ResourcesChanged?.Invoke(); // 전체 자원 변경 알림
    }

    private void EnsureGoldRuntime() // 기존 골드 런타임 연결 보장
    {
        RelicGoldRuntime currentGoldRuntime = RelicRunManager.EnsureInstance().Gold; // 현재 골드 런타임 조회
        if (goldRuntime == currentGoldRuntime) // 동일 골드 런타임 여부 확인
        {
            return; // 재연결 불필요 처리
        }

        if (goldRuntime != null) // 기존 골드 런타임 존재 확인
        {
            goldRuntime.GoldChanged -= HandleGoldChanged; // 기존 골드 이벤트 해제
        }

        goldRuntime = currentGoldRuntime; // 새 골드 런타임 저장
        observedGold = goldRuntime != null ? goldRuntime.Gold : 0; // 현재 골드 이벤트 기준값 저장
        if (goldRuntime != null) // 새 골드 런타임 존재 확인
        {
            goldRuntime.GoldChanged += HandleGoldChanged; // 새 골드 이벤트 등록
        }
    }

    private void HandleGoldChanged(int currentGold) // 기존 골드 변경 이벤트 처리
    {
        if (trackingExplorationResources) // 탐사 골드 추적 여부 확인
        { // 탐사 골드 갱신 시작
            explorationGold = currentGold > observedGold ? AddExplorationResource(explorationGold, currentGold - observedGold, currentGold) : Mathf.Min(explorationGold, currentGold); // 획득분 증가와 기존 골드 우선 소비 반영
        } // 탐사 골드 갱신 종료
        observedGold = currentGold; // 다음 골드 이벤트 기준값 갱신
        if (!suppressGoldEvent) // 이벤트 억제 상태 확인
        {
            ResourcesChanged?.Invoke(); // 전체 자원 변경 알림
        }
    }

    public void BeginExplorationResourceTracking() // 새 탐사의 획득 재화 구분 시작
    { // 탐사 재화 구분 시작
        EnsureGoldRuntime(); // 현재 골드 연결 확인
        trackingExplorationResources = true; // 탐사 획득분 추적 활성화
        explorationGold = 0; // 탐사 골드 초기화
        explorationScrew = 0; // 탐사 나사 초기화
        explorationIronPlate = 0; // 탐사 철판 초기화
        explorationWire = 0; // 탐사 전선 초기화
        observedGold = goldRuntime.Gold; // 기존 골드 이벤트 기준값 설정
    } // 탐사 재화 구분 종료

    public bool RestoreExplorationResourceTracking(int gold, int screw, int ironPlate, int wire) // 저장된 탐사 획득 재화 복원
    { // 탐사 재화 복원 시작
        if (gold < 0 || screw < 0 || ironPlate < 0 || wire < 0 || gold > Gold || screw > Screw || ironPlate > IronPlate || wire > Wire) // 저장 획득분과 전체 자원 범위 확인
        { // 잘못된 저장 재화 처리 시작
            return false; // 잘못된 탐사 재화 복원 차단
        } // 잘못된 저장 재화 처리 종료
        BeginExplorationResourceTracking(); // 현재 자원 기준 추적 준비
        explorationGold = gold; // 저장 탐사 골드 적용
        explorationScrew = screw; // 저장 탐사 나사 적용
        explorationIronPlate = ironPlate; // 저장 탐사 철판 적용
        explorationWire = wire; // 저장 탐사 전선 적용
        return true; // 탐사 재화 복원 성공 반환
    } // 탐사 재화 복원 종료

    public void EndExplorationResourceTracking() // 탐사 자원 구분만 종료
    { // 탐사 자원 확정 시작
        trackingExplorationResources = false; // 탐사 획득분 추적 해제
        explorationGold = 0; // 탐사 골드 구분 제거
        explorationScrew = 0; // 탐사 나사 구분 제거
        explorationIronPlate = 0; // 탐사 철판 구분 제거
        explorationWire = 0; // 탐사 전선 구분 제거
    } // 탐사 자원 확정 종료

    public void DiscardExplorationResources() // 전멸 시 남은 탐사 획득분 제거
    { // 탐사 획득분 제거 시작
        if (!trackingExplorationResources) // 추적 중인 탐사 확인
        { // 획득분 없음 처리 시작
            return; // 기존 자원 유지
        } // 획득분 없음 처리 종료
        int savedGold = Gold - explorationGold; // 기존 골드 잔액 계산
        int savedScrew = Screw - explorationScrew; // 기존 나사 잔액 계산
        int savedIronPlate = IronPlate - explorationIronPlate; // 기존 철판 잔액 계산
        int savedWire = Wire - explorationWire; // 기존 전선 잔액 계산
        RestoreResources(savedGold, savedScrew, savedIronPlate, savedWire); // 기존 재화만 복원
    } // 탐사 획득분 제거 종료

    public void RefundShopGold(int amount, int explorationSpent) // 실패한 상점 거래의 원래 재화 구분 복원
    { // 상점 환불 시작
        int previousExplorationGold = explorationGold; // 환불 전 탐사 골드 보관
        AddResources(amount, 0, 0, 0); // 전체 골드 환불
        if (trackingExplorationResources) // 탐사 중 거래 여부 확인
        { // 탐사 골드 환불 구분 시작
            explorationGold = AddExplorationResource(previousExplorationGold, Mathf.Clamp(explorationSpent, 0, amount), Gold); // 실제 사용한 탐사 골드만 복원
        } // 탐사 골드 환불 구분 종료
    } // 상점 환불 종료

    private static int AddExplorationResource(int previous, int gained, int total) // 안전한 탐사 획득 재화 증가
    { // 탐사 획득 재화 계산 시작
        return (int)Math.Min(Math.Max(0, (long)total), (long)previous + Math.Max(0, gained)); // 전체 잔액을 넘지 않는 획득분 반환
    } // 탐사 획득 재화 계산 종료

    private void OnDestroy() // 자원 관리자 제거 처리
    {
        if (goldRuntime != null) // 골드 런타임 연결 확인
        {
            goldRuntime.GoldChanged -= HandleGoldChanged; // 골드 변경 이벤트 해제
        }

        if (instance == this) // 현재 영구 관리자 제거 여부 확인
        {
            instance = null; // 정적 관리자 참조 초기화
        }
    }
}
