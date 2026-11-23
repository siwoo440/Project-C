using System.Collections.Generic; // 조우와 이벤트 상태 목록 사용
using UnityEngine; // 영구 오브젝트와 위치 기능 사용

public sealed class ExplorationSessionManager : MonoBehaviour // 49일차 탐사 런 상태 관리자
{
    private const int ExplorationSuccessAffinityReward = 1; // 탐사 성공 기본 호감도 보상
    public const int LatestEncounterPoolVersion = 2; // 새 탐사의 최신 조우 목록 버전

    private static ExplorationSessionManager instance; // 탐사 관리자 인스턴스
    private CharacterUnlockProgress unlockProgress = CharacterUnlockProgress.Shared; // 캐릭터 해금 상태

    private readonly HashSet<string> clearedEncounterIds =
        new HashSet<string>(); // 클리어 런타임 조우 ID 목록

    private readonly HashSet<string> resolvedEventIds =
        new HashSet<string>(); // 처리 완료 이벤트 ID 목록
    private readonly HashSet<string> avoidedEncounterIds = new HashSet<string>(); // 승리와 보상을 제외한 지도 조각 회피 기록
    private readonly Dictionary<string, int> escapeGoldClaims = new Dictionary<string, int>(); // 런타임 조우별 선지급 도주 골드
    private string pendingMapFragmentId; // 다음 일반 전투에 사용할 지도 조각 원본 방
    public bool UsedSealedMap { get; private set; } // 현재 탐사에서 봉인된 지도 사용 여부
    public BurntCartRunState Cart { get; private set; } = new BurntCartRunState(); // 회차별 수레 매복과 할인 상태
    public string PendingMapFragmentId => pendingMapFragmentId; // 현재 보관 중인 지도 조각 조회
    public GlassFieldEffect PendingGlassFieldEffects { get; internal set; } // 다음 실제 전투에 적용할 유리 들판 효과
    internal int PendingSpecimenDraw { get; private set; } // 괴생체 승리로 예약한 다음 전투 드로우
    internal int PendingSpecimenActionPoints { get; private set; } // 괴생체 승리로 예약한 다음 전투 행동력
    public WandererCryRunState WandererCry { get; private set; } = new WandererCryRunState(); // 추적과 귀 막기의 다음 교전 상태
    public CrackedCompassRunState Compass { get; private set; } = new CrackedCompassRunState(); // 첫 일반 방과 발견된 사건 기록
    public CharredWellRunState Well { get; private set; } = new CharredWellRunState(); // 회차의 우물 물과 봉인 상태
    public IReadOnlyCollection<string> AvoidedEncounterIds => avoidedEncounterIds; // 저장할 회피 조우 조회
    internal IReadOnlyDictionary<string, int> EscapeGoldClaims => escapeGoldClaims; // 저장에 포함할 조우별 도주 골드 조회

    private EncounterData activeEncounter; // 현재 전투 조우 데이터
    private object activeEncounterIdentity; // 이름이 같아도 각각 구분되는 실제 조우 시작 토큰
    private string activeRuntimeEncounterId; // 현재 전투 런타임 조우 ID
    private Vector3 returnPosition; // 전투 후 탐사 복귀 위치
    private bool hasReturnPosition; // 복귀 위치 존재 여부
    private int currentFloor = 1; // 현재 탐사 층
    private int currentFloorSeed; // 현재 층 절차 생성 Seed
    private int encounterPoolVersion = LatestEncounterPoolVersion; // 현재 탐사의 고정 조우 목록 버전
    private bool hasCurrentFloorSeed; // 현재 층 Seed 존재 여부
    private bool isExplorationCompleted; // 탐사 완료 여부
    private int completionVersion; // 탐사 종료별 고유 처리 순번
    private bool isExplorationSuccess; // 탐사 성공 여부
    private int completedFloor; // 탐사 완료 층
    private int completedEncounterCount; // 완료 시점 클리어 조우 수
    private int lastExplorationSuccessAffinity; // 마지막 탐사 성공 호감도 보상
    private int runExperienceGained; // 이번 탐사 실제 획득 경험치
    private int runGoldGained; // 이번 탐사 실제 획득 골드
    private int runScrewGained; // 이번 탐사 실제 획득 나사
    private int runIronPlateGained; // 이번 탐사 실제 획득 철판
    private int runWireGained; // 이번 탐사 실제 획득 전선
    private int pendingHazardHealthDamage; // 다음 전투에 적용할 누적 환경 체력 피해
    private int pendingHazardMentalDamage; // 다음 전투에 적용할 누적 환경 정신력 피해
    private int hazardPenaltyAppliedAllyCount; // 현재 전투에서 환경 피해 적용 완료 아군 수
    private string lastExplorationFailureReason; // 마지막 탐사 실패 원인
    private float restoredHazardExposure; // 복원 대기 위험 노출도
    private bool hasRestoredHazardExposure; // 복원 대기 노출도 존재 여부

    public static ExplorationSessionManager Instance => instance; // 현재 탐사 관리자 조회
    public EncounterData ActiveEncounter => activeEncounter; // 현재 조우 데이터 조회
    public string ActiveRuntimeEncounterId => activeRuntimeEncounterId; // 현재 런타임 조우 ID 조회
    internal object ActiveEncounterIdentity => activeEncounter != null ? activeEncounterIdentity : null; // 활성 조우 여부까지 확인한 이번 전투 토큰
    internal bool ActiveEncounterInFade { get; private set; } // 현재 조우가 시작된 방의 퇴색 여부
    public int CurrentFloor => currentFloor; // 현재 층 조회
    public ExpeditionZoneId CurrentZone { get; private set; } // 이번 회차의 해당 탐사 구역
    public int CurrentZoneLevel { get; private set; } = 1; // 이번 회차에서 선택한 구역 난이도
    public bool IsZoneBossReservedForRun { get; private set; } // 이번 출발 시점에 확정된 관문 보스 예약
    public int EncounterPoolVersion => encounterPoolVersion; // 저장된 탐사 조우 목록 버전 조회
    public int EventPoolVersion { get; private set; } = ExplorationEventCatalog.CurrentVersion; // 현재 층의 고정 이벤트 목록 버전
    public bool HasReturnPosition => hasReturnPosition; // 전투 복귀 위치 존재 여부
    internal Vector2Int? CurrentFloorOptionalBossRoom { get; private set; } // 현재 층 최초 생성 때 확정한 선택 보스 방
    public int CurrentFloorSeed => currentFloorSeed; // 현재 층 Seed 조회
    public bool HasCurrentFloorSeed => hasCurrentFloorSeed; // 현재 층 Seed 존재 여부 조회
    public bool IsExplorationCompleted => isExplorationCompleted; // 탐사 완료 여부 조회
    public int CompletionVersion => completionVersion; // 탐사 종료 처리 순번 조회
    public bool IsExplorationSuccess => isExplorationSuccess; // 탐사 성공 여부 조회
    public int CompletedFloor => completedFloor; // 탐사 완료 층 조회
    public int CompletedEncounterCount => completedEncounterCount; // 완료 시점 클리어 조우 수 조회
    public int LastExplorationSuccessAffinity => lastExplorationSuccessAffinity; // 마지막 성공 호감도 조회
    internal int PermanentAttackPower { get; private set; } // 유물 보유 여부와 별개로 유지하는 이번 탐사의 영구 공격력
    internal int GrantPermanentAttackPower(int amount) // 실제 정예 및 보스 처치의 영구 공격력 누적
    { // 영구 공격력 정산 시작
        int gained = System.Math.Min(System.Math.Max(0, amount), int.MaxValue - PermanentAttackPower); // 회차 공격력의 실제 증가량과 정수 상한 확인
        PermanentAttackPower += gained; // 새 탐사 전까지 유지할 영구 공격력 누적 기록
        return gained; // 실제로 지급한 영구 수치 반환
    } // 영구 공격력 정산 종료
    public int RunExperienceGained => runExperienceGained; // 이번 탐사 실제 경험치 조회
    public int RunGoldGained => runGoldGained; // 이번 탐사 실제 골드 조회
    public int RunScrewGained => runScrewGained; // 이번 탐사 실제 나사 조회
    public int RunIronPlateGained => runIronPlateGained; // 이번 탐사 실제 철판 조회
    public int RunWireGained => runWireGained; // 이번 탐사 실제 전선 조회
    public int PendingHazardHealthDamage => pendingHazardHealthDamage; // 대기 환경 체력 피해 조회
    public int PendingHazardMentalDamage => pendingHazardMentalDamage; // 대기 환경 정신력 피해 조회
    public string LastExplorationFailureReason => lastExplorationFailureReason; // 마지막 탐사 실패 원인 조회

    public IReadOnlyCollection<string> ClearedEncounterIds =>
        clearedEncounterIds; // 클리어 조우 목록 조회

    public IReadOnlyCollection<string> ResolvedEventIds =>
        resolvedEventIds; // 처리 완료 이벤트 목록 조회

    public ExplorationClearRewardResult LastClearReward
    {
        get;
        private set;
    }

    public static ExplorationSessionManager EnsureInstance() // 탐사 관리자 존재 보장
    {
        if (instance != null)
        {
            return instance;
        }

        instance =
            FindFirstObjectByType<ExplorationSessionManager>(); // Scene 기존 관리자 탐색

        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject =
            new GameObject("ExplorationSessionManager"); // 탐사 관리자 오브젝트 생성

        instance =
            managerObject.AddComponent<ExplorationSessionManager>(); // 탐사 관리자 컴포넌트 추가

        return instance;
    }

    private void Awake() // 탐사 관리자 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 관리자 제거
            return;
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    }

    internal bool BeginZoneExpedition(ExpeditionZoneId zone, int level) // 초기화된 새 탐사의 출발 구역 고정
    { // 출전 가능한 실제 구역과 난이도 검사 시작
        if (currentFloor != 1 || hasCurrentFloorSeed || activeEncounter != null || isExplorationCompleted || // 이미 시작했거나 종료된 회차 확인
            !ExpeditionZoneProgress.TrySelect(zone, level)) // 해금하지 않은 구역이나 난이도 요청 확인
        { // 잘못된 출발의 차단 시작
            return false; // 기존 회차의 구역 선택 보존
        } // 잘못된 출발의 차단 종료
        CurrentZone = zone; // 선택한 구역을 현재 회차 전체에 적용
        CurrentZoneLevel = level; // 이번 회차에서 선택한 난이도 적용
        IsZoneBossReservedForRun = ExpeditionZoneProgress.CanReserveBoss(zone, level); // 출발 당시의 백 퍼센트 보스 예약 고정
        return true; // 실제 새 탐사의 출발 정보 준비
    } // 출전 가능한 실제 구역과 난이도 검사 종료

    public bool CanBeginEncounter(string runtimeEncounterId, EncounterData encounterData) // 전투 조우 시작 가능 여부 확인
    { // 조우 시작 가능 여부 확인 시작
        return !string.IsNullOrWhiteSpace(runtimeEncounterId) && encounterData != null && encounterData.IsValidData() && activeEncounter == null && !isExplorationCompleted && !IsEncounterRemoved(runtimeEncounterId) && (Cart.AmbushRoom == null || runtimeEncounterId == Cart.AmbushBattleId); // 미완료 매복 우선 처리와 완료 조우 재진입 차단
    } // 조우 시작 가능 여부 확인 종료

    public bool IsEncounterRemoved(string runtimeEncounterId) // 현재 층에서 제거할 조우 여부 확인
    { // 제거 여부 조회 시작
        return IsEncounterCleared(runtimeEncounterId) || avoidedEncounterIds.Contains(runtimeEncounterId) || (!string.IsNullOrEmpty(runtimeEncounterId) && runtimeEncounterId == Compass.EncounterId); // 승리와 회피 및 발견 사건의 전투 재생성 방지
    } // 제거 여부 조회 종료

    internal bool TryKeepMapFragment(string runtimeId) // 다음 일반 전투에 사용할 지도 조각 한 장 보관
    { // 조각 보관 시작
        if (!string.IsNullOrEmpty(pendingMapFragmentId)) // 사용 대기 중인 조각 확인
        { // 기존 조각 보존 시작
            return false; // 중첩 보관과 기존 효과 교체 차단
        } // 기존 조각 보존 종료
        pendingMapFragmentId = runtimeId; // 획득 방의 고정 추첨 식별자 보관
        return true; // 보관 성공 반환
    } // 조각 보관 종료

    public bool CanUseSealedMap(ExplorationMapRuntime map) // 현재 층에서 출발 방으로 귀환 가능한지 확인
    { // 지도 이동 검사 시작
        if (UsedSealedMap || IsExplorationCompleted || ActiveEncounter != null || !HasCurrentFloorSeed || map?.CurrentMap == null || map.CurrentMap.Seed != CurrentFloorSeed || RelicRunManager.Instance?.Inventory.FindEffect(RelicEffectType.ReselectExplorationRoute) == null) // 진행 중인 층과 미소모 지도 확인
        { // 사용할 수 없는 지도 처리 시작
            return false; // 탐사 종료와 전투 및 재획득 후 재사용 차단
        } // 사용할 수 없는 지도 처리 종료
        return map.TryGetPlayerLogicalPosition(out _) && (!map.TryGetPlayerRoomCoordinate(out var room) || room != map.CurrentMap.StartCoordinate); // 출발 방 밖의 방이나 복도에서 사용 허용
    } // 지도 이동 검사 종료
    public bool TryUseSealedMap(ExplorationMapRuntime map, string runPath = null) // 같은 층의 출발 방으로 한 번 이동
    { // 지도 사용 시작
        var player = FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        if (player == null || !CanUseSealedMap(map)) // 이동 직전 보유와 현재 위치 재검사
        { // 잘못된 이동 처리 시작
            return false; // 비용 없는 위치와 사용 기회 보존
        } // 잘못된 이동 처리 종료
        var relics = RelicRunManager.Instance; // 실제 보유 관리자
        var sealedMap = relics.Inventory.FindEffect(RelicEffectType.ReselectExplorationRoute); // 소모할 지도 원본
        Vector2 start = map.GetWorldPosition(map.CurrentMap.StartCoordinate); // 현재 층의 기존 출발 방 중심
        if (!ExplorationSaveService.Save(runPath ?? ExplorationSaveService.DefaultPath, start, sealedMap)) // 이동 위치와 소모 결과를 한 번에 저장
        { // 저장 실패 처리 시작
            return false; // 지도와 기존 보유 순서 및 위치 유지
        } // 저장 실패 처리 종료
        UsedSealedMap = true; // 유물 제거 알림 전 재사용 차단
        relics.TryRemove(sealedMap.RelicId); // 저장과 동일한 지도 한 개 소모
        player.Teleport(start); // 기존 층과 완료 기록을 유지한 채 실제 플레이어 이동
        return true; // 귀환 성공 반환
    } // 지도 사용 종료
    public MapFragmentContact ResolveMapFragmentContact(string runtimeId, EncounterData encounter, Vector3 position, string runPath = null) // 일반 조우의 일회성 회피 판정과 저장
    { // 지도 조각 사용 시작
        if (string.IsNullOrEmpty(pendingMapFragmentId) || !CanBeginEncounter(runtimeId, encounter) || encounter.BattleType != BattleType.Normal) // 사용 가능한 일반 전투와 조각 확인
        { // 회피 판정 제외 시작
            return MapFragmentContact.NoEffect; // 특수 조우와 잘못된 접촉에서 조각 보존
        } // 회피 판정 제외 종료
        string source = pendingMapFragmentId; // 저장 실패 복구와 고정 추첨용 원본 방
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(source)); // 불러오기와 경로 변경에도 같은 조각 결과 유지
        bool avoided = random.Next(100) < 40; // 사십 퍼센트 회피 판정
        pendingMapFragmentId = null; // 성공과 실패 모두 일회성 조각 소모
        if (avoided) // 조우 회피 성공 여부 확인
        { // 조우 회피 기록 시작
            avoidedEncounterIds.Add(runtimeId); // 클리어 횟수와 별도로 통과한 조우 기록
        } // 조우 회피 기록 종료
        if (ExplorationAutoSave.SaveBeforeBattle(position, runPath)) // 전투 진입 또는 표식 제거 전 일회성 결과 저장
        { // 판정 확정 시작
            return avoided ? MapFragmentContact.Avoided : MapFragmentContact.Battle; // 저장된 회피 또는 전투 진입 반환
        } // 판정 확정 종료
        pendingMapFragmentId = source; // 저장 실패 시 획득한 조각 복구
        avoidedEncounterIds.Remove(runtimeId); // 미확정 조우 표식 보존
        return MapFragmentContact.SaveFailed; // 복구된 상태에서 다시 접촉 허용
    } // 지도 조각 사용 종료

    public bool BeginEncounter(
        string runtimeEncounterId,
        EncounterData encounterData,
        Vector3 playerPosition,
        Vector3 encounterPosition) // 절차 조우 시작
    {
        if (!CanBeginEncounter(runtimeEncounterId, encounterData)) // 실제 조우 시작 가능 여부 확인
        {
            return false;
        }

        activeEncounter = encounterData; // 현재 전투 데이터 저장
        activeEncounterIdentity = new object(); // 같은 탐사 방의 재시도와 구분할 실제 조우 생성
        activeRuntimeEncounterId = runtimeEncounterId; // 런타임 조우 ID 저장
        ActiveEncounterInFade = FindFirstObjectByType<ExplorationMapRuntime>()?.IsFadeAtWorldPosition(encounterPosition) == true; // 진입 위치의 퇴색 여부를 전투까지 저장
        LastClearReward = null; // 이전 보상 표시 초기화

        Vector2 escapeDirection =
            (Vector2)(playerPosition - encounterPosition); // 조우 반대 방향 계산

        if (escapeDirection.sqrMagnitude < 0.01f)
        {
            escapeDirection = Vector2.left; // 겹친 위치 기본 복귀 방향 지정
        }

        escapeDirection.Normalize(); // 복귀 방향 정규화

        returnPosition =
            playerPosition +
            (Vector3)(escapeDirection * 1.25f); // 전투 후 복귀 위치 계산

        returnPosition.z = 0f; // Z 위치 초기화
        hasReturnPosition = true; // 복귀 위치 활성화

        Debug.Log(
            $"[Exploration][Day49] 조우 시작 - " +
            $"{runtimeEncounterId} / " +
            $"{encounterData.DisplayName} / " +
            $"적 {encounterData.Enemies.Count}명"); // 절차 조우 시작 로그

        return true;
    }

    public bool BeginEncounter(
        EncounterData encounterData,
        Vector3 playerPosition,
        Vector3 encounterPosition) // 기존 호출 호환 조우 시작
    {
        string fallbackRuntimeId =
            encounterData != null
                ? encounterData.EncounterId
                : string.Empty; // 기존 Encounter ID를 임시 런타임 ID로 사용

        return BeginEncounter(
            fallbackRuntimeId,
            encounterData,
            playerPosition,
            encounterPosition); // 신규 조우 시작 기능 호출
    }

    public void ResolveBattleResult(BattleResultData resultData) // 전투 결과 처리
    {
        if (resultData == null || activeEncounter == null)
        {
            return;
        }

        string runtimeEncounterId =
            activeRuntimeEncounterId; // 현재 런타임 조우 ID 보관
        string coreRewardRoom = null; // 첫 침식핵 승리의 별도 보상 대상
        Cart.FinishAmbush(runtimeEncounterId); // 해결한 매복의 저장 재진입 해제

        string encounterName =
            activeEncounter.DisplayName; // 현재 조우 이름 보관

        BattleType clearedBattleType =
            activeEncounter.BattleType; // 클리어 대상 조우 등급 보관

        if (resultData.Result == BattleResult.Victory)
        {
            escapeGoldClaims.TryGetValue(runtimeEncounterId, out int previousEscapeGold); // 같은 조우에서 먼저 보관한 도주 골드 조회
            escapeGoldClaims.Remove(runtimeEncounterId); // 승리한 조우의 도주 골드 정산 기록 제거
            GrantVictoryRewards(activeEncounter, previousEscapeGold); // 차액과 나머지 승리 보상 지급
            if (activeEncounter.EncounterId == "ENC_WF_ASHMAGGOT") // 실제 괴생체 원본 조우의 승리 확인
            { // 표본병 보상 예약 시작
                var relics = RelicRunManager.Instance?.Inventory; // 승리 시 보유한 실제 유물 조회
                int draw = relics?.FindEffect(RelicEffectType.AberrationVictoryDraw)?.EffectValue ?? 0; // 오염 표본병의 시작 드로우 수
                int points = relics?.FindEffect(RelicEffectType.AberrationVictoryActionPoint)?.EffectValue ?? 0; // 균열 표본병의 시작 행동력
                PendingSpecimenDraw = (int)System.Math.Min(int.MaxValue, (long)PendingSpecimenDraw + draw); // 다음 교전의 미사용 드로우 누적
                PendingSpecimenActionPoints = (int)System.Math.Min(int.MaxValue, (long)PendingSpecimenActionPoints + points); // 다음 교전의 미사용 행동력 누적
            } // 표본병 보상 예약 종료

            if (!string.IsNullOrWhiteSpace(runtimeEncounterId))
            {
                if (clearedEncounterIds.Add(runtimeEncounterId)) // 같은 회차에서 처음 클리어한 실제 조우 확인
                { // 이번 조우의 독립 진행과 보상 준비 시작
                    ExpeditionZoneProgress.RecordBattleVictory(CurrentZone, clearedBattleType); // 일반과 정예 전투의 구역 수치 반영
                    if (activeEncounter.EncounterId == "ENC_WF_ASHSTORM_CORE") // 첫 침식핵 전투의 기존 보상 대상 확인
                    { // 기존 침식핵의 추가 보상 준비 시작
                        coreRewardRoom = runtimeEncounterId; // 같은 배치에서 한 번만 추첨할 방 기록
                    } // 기존 침식핵의 추가 보상 준비 종료
                } // 이번 조우의 독립 진행과 보상 준비 종료
            }

            Debug.Log(
                $"[Exploration][Day49] 조우 클리어 - " +
                $"{runtimeEncounterId} / {encounterName} / {clearedBattleType}"); // 조우 클리어 로그

            bool clearedBossGate = clearedBattleType == BattleType.Boss && ExplorationFloorRules.IsBossFloor(currentFloor) && activeEncounter.EncounterId != OptionalBossPlacement.EncounterId; // 필수 보스층의 실제 진행 관문 승리
            bool clearedZoneEliteGate = ExpeditionZoneEncounters.IsRegionalEliteGate(CurrentZone, activeEncounter); // 예약 전 일반 지역 플레이의 정예 최종 관문 확인
            if ((clearedBossGate || clearedZoneEliteGate) && ExplorationFloorRules.IsFinalBossFloor(currentFloor)) // 실제 보스나 지역 정예의 마지막 관문 확인
            { // 구역 진행과 기존 최종 탐사 정산 시작
                if (clearedBossGate && !isExplorationCompleted && ExpeditionZoneEncounters.IsMandatoryBoss(CurrentZone, activeEncounter)) // 선택 보스와 다른 실제 해당 구역 관문 확인
                { // 정식 구역 보스 처치 정산 시작
                    ExpeditionZoneProgress.RecordBossVictory(CurrentZone, CurrentZoneLevel, IsZoneBossReservedForRun); // 실제 최고 난이도 예약과 고유 구역 기록 반영
                } // 정식 구역 보스 처치 정산 종료
                if (CompleteExplorationSuccess() && clearedBossGate) // 최종 탐사와 기존 보스 클리어 보상 확인
                { // 캐릭터 세계 해금 기록 시작
                    unlockProgress.RecordFinalClear(); // 최초 최종 탐사 보스 승리 해금 기록
                } // 캐릭터 세계 해금 기록 종료
            } // 구역 진행과 기존 최종 탐사 정산 종료
            else if (clearedBossGate) // 중간 필수 관문에만 다음 층 진행 안내
            {
                Debug.Log(
                    $"[Exploration][Day57] 중간 보스 클리어 - " +
                    $"{currentFloor}F / 다음 층 진행 가능"); // 5층 중간 보스 승리 진행 로그
            }
        }
        else
        {
            LastClearReward = null; // 비승리 보상 제거
            if (resultData.Result == BattleResult.Escape && resultData.LivingAllyCount > 0) // 실제 생존자와 전투의 도주 결과 확인
            { // 두 손실의 독립 구역 보정 시작
                ExpeditionZoneProgress.RecordRetreat(CurrentZone); // 이전 원정까지 포함한 이 구역 첫 후퇴 적용
            } // 두 손실의 독립 구역 보정 종료
            int escapeGold = GetEscapeGoldReward(resultData); // 성공한 첫 도주의 정산 골드 조회
            if (escapeGold > 0) // 정산할 골드 확인
            { // 도주 골드 지급 시작
                escapeGoldClaims.Add(runtimeEncounterId, escapeGold); // 같은 조우의 중복 지급 방지 기록
                runGoldGained += escapeGold; // 실제 지급 골드 탐사 합계 누적
                PlayerResourceManager.EnsureInstance().AddResources(escapeGold, 0, 0, 0); // 탐사 추적 경로로 골드만 지급
            } // 도주 골드 지급 종료

            if (resultData.LivingAllyCount <= 0)
            {
                CompleteExplorationFailure(
                    "전투에서 출전 파티가 전멸했습니다."); // 전투 전멸 탐사 실패 처리
            }

            Debug.Log(
                $"[Exploration][Day51] 조우 비승리 - " +
                $"{runtimeEncounterId} / " +
                $"{encounterName} / " +
                $"결과 {resultData.Result} / " +
                $"생존 {resultData.LivingAllyCount}명"); // 조우 비승리 로그
        }

        activeEncounter = null; // 현재 전투 데이터 초기화
        activeRuntimeEncounterId = null; // 현재 런타임 조우 ID 초기화
        ActiveEncounterInFade = false; // 완료된 교전의 퇴색 정보 해제
        if (coreRewardRoom != null && LastClearReward != null) // 정리된 침식핵 조우의 별도 보상 확인
        { // 침식핵 유물 지급 시작
            LastClearReward.CoreRelic = ExplorationTreasureRewardService.TryGrantCoreRelic(coreRewardRoom); // 유물 획득과 귀환 보상 표시 연결
        } // 침식핵 유물 지급 종료
    } // 전투 결과 처리 종료
    internal void ApplyOpeningSpecimenRewards(BattleTurnRuntime turns) // 유효한 다음 탐사 전투의 시작 보상 전달
    { // 표본병 시작 보상 전달 시작
        if (activeEncounter == null || turns == null || (PendingSpecimenDraw == 0 && PendingSpecimenActionPoints == 0)) // 실제 탐사 조우와 미사용 보상 확인
        { // 보상 전달 생략 시작
            return; // 보상 없음 또는 전투 미성립 상태 보존
        } // 보상 전달 생략 종료
        int draw = PendingSpecimenDraw; // 이번 전투에 전달할 드로우 양
        int actionPoints = PendingSpecimenActionPoints; // 이번 전투에 전달할 행동력 양
        PendingSpecimenDraw = 0; // 이전 괴생체 승리의 드로우 중복 전달 방지
        PendingSpecimenActionPoints = 0; // 이전 괴생체 승리의 행동력 중복 전달 방지
        turns.QueueSpecimenOpeningRewards(draw, actionPoints); // 첫 아군 턴에 적용할 두 보상 전달
    } // 표본병 시작 보상 전달 종료

    internal int GetEscapeGoldReward(BattleResultData resultData) // 결과 화면과 정산에 사용할 도주 골드 계산
    { // 도주 골드 계산 시작
        if (resultData == null || resultData.Result != BattleResult.Escape || resultData.LivingAllyCount <= 0 // 도주 성공과 생존자 확인
            || isExplorationCompleted || activeEncounter == null || activeEncounter.BattleType != resultData.BattleType // 종료 상태와 현재 조우 확인
            || (resultData.BattleType != BattleType.Normal && resultData.BattleType != BattleType.Elite) // 일반·정예 조우 확인
            || string.IsNullOrWhiteSpace(activeRuntimeEncounterId) || escapeGoldClaims.ContainsKey(activeRuntimeEncounterId)) // 조우 식별자와 중복 지급 확인
        { // 지급 불가 처리 시작
            return 0; // 보존할 골드 없음
        } // 지급 불가 처리 종료
        int percent = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.EscapePartialGold)?.EffectValue ?? 0; // 보유 조난 신호탄의 보존 비율 조회
        return (int)((long)activeEncounter.GoldReward * Mathf.Clamp(percent, 0, 100) / 100L); // 층과 등급을 반영한 기본 골드의 소수점 절사
    } // 도주 골드 계산 종료

    public bool CompleteExplorationSuccess() // 탐사 성공 처리 시도
    {
        if (isExplorationCompleted)
        {
            return false;
        }

        PlayerResourceManager wallet = PlayerResourceManager.Instance; // 탐사에서 획득해 남은 골드를 보관한 지갑
        int percent = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.ExplorationSuccessGoldPercent)?.EffectValue ?? 0; // 보유한 여행자의 부적 정산 가산율
        int bonus = wallet != null && wallet.IsTrackingExplorationResources ? (int)((long)wallet.ExplorationGold * Mathf.Clamp(percent, 0, 100) / 100L) : 0; // 기존 보유 골드가 빠진 남은 탐사 수입의 내림 가산액
        wallet?.EndExplorationResourceTracking(); // 정산 가산액이 탐사 수입에 재계상되지 않도록 기존 획득분 확정
        isExplorationCompleted = true; // 탐사 완료 상태 기록
        completionVersion++; // 새 탐사 종료 처리 순번 증가
        isExplorationSuccess = true; // 탐사 성공 상태 기록
        completedFloor = currentFloor; // 성공 층 기록
        completedEncounterCount = clearedEncounterIds.Count; // 성공 시점 클리어 조우 수 기록
        lastExplorationSuccessAffinity = ExplorationSuccessAffinityReward; // 성공 호감도 보상 기록
        if (bonus > 0) // 성공 정산에서 지급할 부적 골드 확인
        { // 여행자의 부적 정산 시작
            int paid = Mathf.Min(bonus, int.MaxValue - wallet.Gold); // 지갑 범위를 넘지 않는 실제 지급액
            runGoldGained = (int)System.Math.Min(int.MaxValue, (long)runGoldGained + paid); // 기존 골드를 다시 더하지 않는 실제 신규 지급액만 기록
            if (paid > 0) // 지갑에 더할 수 있는 골드 확인
            { // 성공 골드 지급 시작
                wallet.AddResources(paid, 0, 0, 0); // 거점 저장에 남을 정산 골드 한 번 지급
            } // 성공 골드 지급 종료
        } // 여행자의 부적 정산 종료

        CharacterAffinityManager affinityManager =
            CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비

        affinityManager.GrantExplorationSuccessAffinity(
            lastExplorationSuccessAffinity); // 탐사 성공 호감도 지급

        Debug.Log(
            $"[Exploration][Day49] 탐사 성공 - " +
            $"{completedFloor}F / " +
            $"클리어 조우 {completedEncounterCount}개 / " +
            $"처리 이벤트 {resolvedEventIds.Count}개 / " +
            $"EXP +{runExperienceGained} / " +
            $"Gold +{runGoldGained} / " +
            $"나사 +{runScrewGained} / " +
            $"철판 +{runIronPlateGained} / " +
            $"전선 +{runWireGained} / " +
            $"호감도 +{lastExplorationSuccessAffinity}"); // 탐사 성공 정산 로그

        return true;
    }

    public bool CompleteExplorationFailure(string reason) // 파티 전멸 등 탐사 실패 처리
    {
        if (isExplorationCompleted)
        {
            return false;
        }

        ExpeditionZoneProgress.RecordDefeat(CurrentZone); // 최초 전멸의 구역 보정을 기존 실패 정산에 반영
        PlayerResourceManager.Instance?.DiscardExplorationResources(); // 전멸 시 남은 탐사 획득 재화 제거
        isExplorationCompleted = true; // 탐사 완료 상태 기록
        completionVersion++; // 새 탐사 종료 처리 순번 증가
        isExplorationSuccess = false; // 탐사 실패 상태 기록
        completedFloor = currentFloor; // 실패 층 기록
        completedEncounterCount = clearedEncounterIds.Count; // 실패 시점 클리어 조우 수 기록
        lastExplorationSuccessAffinity = 0; // 실패 호감도 보상 없음
        lastExplorationFailureReason =
            string.IsNullOrWhiteSpace(reason)
                ? "출전 파티가 전멸했습니다."
                : reason; // 탐사 실패 원인 저장
        LastClearReward = null; // 실패 시 마지막 전투 보상 제거

        Debug.LogWarning(
            $"[Exploration][Day51] 탐사 실패 - " +
            $"{completedFloor}F / " +
            $"생존 0명 / " +
            $"원인 {lastExplorationFailureReason}"); // 탐사 실패 로그

        return true;
    }

    public bool IsEncounterCleared(string runtimeEncounterId) // 런타임 조우 클리어 여부 확인
    {
        return !string.IsNullOrWhiteSpace(runtimeEncounterId) &&
               clearedEncounterIds.Contains(runtimeEncounterId); // 클리어 목록 포함 여부 반환
    }

    public bool IsEventResolved(string runtimeEventId) // 이벤트 처리 완료 여부 확인
    {
        return !string.IsNullOrWhiteSpace(runtimeEventId) &&
               resolvedEventIds.Contains(runtimeEventId); // 처리 이벤트 목록 포함 여부 반환
    }

    public bool MarkEventResolved(string runtimeEventId, bool? successful = null) // 방의 완료와 실제 사건의 성공 또는 실패 기록
    {
        if (string.IsNullOrWhiteSpace(runtimeEventId))
        {
            return false;
        }

        bool added =
            resolvedEventIds.Add(runtimeEventId); // 처리 완료 이벤트 목록 등록

        if (added) // 실제로 이번 회차에서 처음 해결한 방 확인
        { // 원래 방 완료와 사건 진행도 처리 시작
            if (!isExplorationCompleted && successful.HasValue) // 상점과 휴식이 아닌 실제 사건의 판정 확인
            { // 실제 사건의 성공 및 실패 보정 시작
                ExpeditionZoneProgress.RecordEvent(CurrentZone, successful.Value); // 현재 구역의 사건 판정에 맞춘 수치 증가
            } // 실제 사건의 성공 및 실패 보정 종료
            Debug.Log(
                $"[Exploration][Day49] 이벤트 처리 완료 - {runtimeEventId}"); // 이벤트 완료 로그
        } // 원래 방 완료와 사건 진행도 처리 종료

        return added;
    }

    public void AddPendingHazardPenalty(
        int healthDamage,
        int mentalDamage) // 탐사 환경 피해 누적
    {
        int safeHealthDamage =
            Mathf.Max(
                0,
                healthDamage); // 음수 체력 피해 방지

        int safeMentalDamage =
            Mathf.Max(
                0,
                mentalDamage); // 음수 정신력 피해 방지

        if (safeHealthDamage == 0 &&
            safeMentalDamage == 0)
        {
            return;
        }

        pendingHazardHealthDamage +=
            safeHealthDamage; // 다음 전투용 체력 피해 누적

        pendingHazardMentalDamage +=
            safeMentalDamage; // 다음 전투용 정신력 피해 누적

        hazardPenaltyAppliedAllyCount = 0; // 새 환경 피해 발생 시 적용 카운트 초기화

        Debug.Log(
            $"[Exploration][Day50] 환경 피해 누적 - " +
            $"HP -{safeHealthDamage}, 정신 -{safeMentalDamage} / " +
            $"대기 합계 HP -{pendingHazardHealthDamage}, " +
            $"정신 -{pendingHazardMentalDamage}"); // 환경 피해 누적 로그
    }

    public bool ApplyPendingHazardPenalty(
        BattleUnitRuntime allyUnit,
        int expectedPartyCount) // 다음 전투 아군에 대기 환경 피해 적용
    {
        if (allyUnit == null ||
            allyUnit.Team != BattleTeam.Ally ||
            (pendingHazardHealthDamage <= 0 &&
             pendingHazardMentalDamage <= 0))
        {
            return false;
        }

        bool applied =
            false; // 현재 아군 실제 환경 피해 적용 여부

        if (!allyUnit.IsDead)
        {
            int targetHealth =
                Mathf.Max(
                    1,
                    allyUnit.CurrentHealth -
                    pendingHazardHealthDamage); // 51일차 사망 확장 전까지 최소 체력 1 보장

            int targetMental =
                allyUnit.CurrentMental -
                pendingHazardMentalDamage; // 누적 정신력 감소 목표값 계산

            allyUnit.ApplyPersistentHealth(
                targetHealth); // 환경 체력 피해를 전투 시작 상태에 반영

            allyUnit.ApplyPersistentMental(
                targetMental); // 환경 정신력 피해를 전투 시작 상태에 반영

            applied = true; // 생존 아군 환경 피해 적용 완료
        }

        hazardPenaltyAppliedAllyCount += 1; // 사망 여부와 관계없이 파티 처리 수 증가

        int safePartyCount =
            Mathf.Max(
                1,
                expectedPartyCount); // 잘못된 파티 수 최소값 보정

        if (hazardPenaltyAppliedAllyCount >=
            safePartyCount)
        {
            ClearPendingHazardPenalty(); // 전체 파티 처리 후 대기 피해 제거
        }

        return applied;
    }

    private void ClearPendingHazardPenalty() // 다음 전투용 환경 피해 초기화
    {
        pendingHazardHealthDamage = 0; // 대기 체력 피해 초기화
        pendingHazardMentalDamage = 0; // 대기 정신력 피해 초기화
        hazardPenaltyAppliedAllyCount = 0; // 적용 완료 수 초기화
    }

    public Vector3 GetPlayerSpawnPosition(
        Vector3 defaultPosition) // 탐사 플레이어 생성 위치 조회
    {
        return hasReturnPosition
            ? returnPosition
            : defaultPosition; // 전투 복귀 위치 우선 반환
    }

    public bool TryGetCurrentFloorSeed(
        out int seed) // 현재 층 Seed 조회 시도
    {
        seed = currentFloorSeed; // 현재 Seed 반환값 지정
        return hasCurrentFloorSeed; // Seed 존재 여부 반환
    }

    public void SetCurrentFloorSeed(int seed) // 현재 층 Seed 저장
    {
        if (!hasCurrentFloorSeed || currentFloorSeed != seed) // 새로 지정된 현재 층 지도 확인
        { // 이전 지도 상태 제거 시작
            CurrentFloorOptionalBossRoom = null; // 다른 시드로 생성되는 지도의 이전 선택 좌표 제거
        } // 이전 지도 상태 제거 종료
        currentFloorSeed = seed; // 현재 층 Seed 저장
        hasCurrentFloorSeed = true; // Seed 존재 상태 활성화
    }

    internal void FreezeOptionalBossRoom(ExplorationMapData map, RelicInventoryRuntime relics) // 신규 지도에서 보유한 표지판 확률 확정
    { // 현재 층 선택 보스 판정 시작
        int chance = relics?.FindEffect(RelicEffectType.OptionalBossNodeChance)?.EffectValue ?? 0; // 생성 시점의 실제 보유 효과 수치
        CurrentFloorOptionalBossRoom = OptionalBossPlacement.Select(map, currentFloor, chance); // 독립 추첨의 결과 좌표 보관
    } // 현재 층 선택 보스 판정 종료

    public void ClearReturnPosition() // 전투 복귀 위치 초기화
    {
        returnPosition = Vector3.zero; // 복귀 위치 초기화
        hasReturnPosition = false; // 복귀 위치 사용 해제
    }

    private void ClearCurrentFloorSeed() // 현재 층 Seed 초기화
    {
        currentFloorSeed = 0; // Seed 값 초기화
        CurrentFloorOptionalBossRoom = null; // 다음 층의 선택 보스 판정 초기화
        EventPoolVersion = ExplorationEventCatalog.CurrentVersion; // 다음 층에 최신 이벤트 목록 배정
        hasCurrentFloorSeed = false; // Seed 존재 상태 해제
    }

    public int AdvanceFloor() // 다음 층 진행
    {
        if (isExplorationCompleted)
        {
            Debug.Log(
                $"[Exploration][Day49] 탐사 완료 상태라 다음 층으로 이동하지 않습니다. " +
                $"완료 층 {completedFloor}F"); // 완료 후 층 이동 차단 로그

            return currentFloor;
        }

        currentFloor += 1; // 현재 층 증가
        Compass = new CrackedCompassRunState(); // 첫 층을 떠날 때 미사용 기회와 이전 발견 기록 만료
        Well.AdvanceFloor(); // 이전 층 봉인 만료와 새 층 봉인 적용
        Cart.LeaveFloor(); // 미구매 할인을 다음 층 첫 상점으로 이월
        activeEncounter = null; // 진행 중 조우 초기화
        activeRuntimeEncounterId = null; // 진행 중 런타임 조우 초기화
        ActiveEncounterInFade = false; // 층 이동 때 퇴색 조우 정보 해제
        returnPosition = Vector3.zero; // 이전 층 복귀 위치 초기화
        hasReturnPosition = false; // 이전 층 복귀 위치 해제
        LastClearReward = null; // 이전 보상 표시 초기화
        ClearCurrentFloorSeed(); // 다음 층용 Seed 생성 준비

        Debug.Log(
            $"[Exploration][Day49] 다음 층 진입 - {currentFloor}F"); // 층 진행 로그

        return currentFloor; // 변경된 층 반환
    }

    internal void RestoreRunState(ExplorationSaveService.RunSnapshot snapshot) // 저장된 탐사 상태 복원
    { // 탐사 상태 복원 시작
        ResetExploration(); // 이전 탐사 상태 제거
        if (snapshot.zoneVersion > 0) // 실제 출발 구역을 보관한 현재 형식 확인
        { // 탐사 선택과 이미 고정한 보스 예약의 복원 시작
            CurrentZone = (ExpeditionZoneId)snapshot.zone; // 저장한 회차의 실제 구역
            CurrentZoneLevel = snapshot.zoneLevel; // 저장한 회차의 실제 선택 레벨
            IsZoneBossReservedForRun = snapshot.zoneBossReserved; // 도중 백 퍼센트와 구분한 실제 출발 시점 상태
        } // 탐사 선택과 이미 고정한 보스 예약의 복원 종료
        PermanentAttackPower = System.Math.Max(0, snapshot.permanentAttackPower); // 기존 저장에서 생략된 영 기본값과 정상 비음수 보너스 복원
        encounterPoolVersion = snapshot.encounterPoolVersion; // 저장 당시 조우 목록 버전 복원
        EventPoolVersion = snapshot.eventPoolVersion; // 저장 당시 층의 이벤트 목록 복원
        currentFloor = snapshot.floor; // 저장 탐사 층 적용
        currentFloorSeed = snapshot.seed; // 저장 맵 시드 적용
        CurrentFloorOptionalBossRoom = snapshot.hasOptionalBossRoom ? snapshot.optionalBossRoom : (Vector2Int?)null; // 실패한 추첨과 구버전 지도는 선택 방 없이 복원
        hasCurrentFloorSeed = true; // 저장 시드 존재 표시
        returnPosition = snapshot.position; // 저장 플레이어 위치 적용
        hasReturnPosition = true; // 저장 위치 사용 표시
        clearedEncounterIds.UnionWith(snapshot.cleared); // 클리어 조우 목록 복원
        resolvedEventIds.UnionWith(snapshot.events); // 처리 이벤트 목록 복원
        pendingMapFragmentId = snapshot.pendingMapFragment; // 이전 저장의 빈 값 또는 보관한 조각 복원
        UsedSealedMap = snapshot.usedSealedMap; // 기존 저장의 기본값 또는 사용한 지도 기록 복원
        Cart = snapshot.cart ?? new BurntCartRunState(); // 이전 저장 또는 수레 이벤트 상태 복원
        PendingGlassFieldEffects = snapshot.glassFieldEffects; // 사용 전 들판 효과 복원
        WandererCry = snapshot.wandererCry ?? new WandererCryRunState(); // 이전 저장은 방랑자 효과 없이 복원
        Compass = snapshot.compass ?? new CrackedCompassRunState(); // 구버전 저장의 기존 방 배치 보존
        Well = snapshot.well ?? new CharredWellRunState(); // 이전 저장의 빈 기본값 또는 우물 상태 복원
        if (snapshot.avoidedEncounters != null) // 지도 조각을 지원하는 저장 여부 확인
        { // 회피 목록 복원 시작
            avoidedEncounterIds.UnionWith(snapshot.avoidedEncounters); // 회피 조우 재등장 방지 기록 복원
        } // 회피 목록 복원 종료
        if (snapshot.escapeGoldClaims != null) // 도주 골드 기록이 있는 저장인지 확인
        { // 도주 골드 복원 시작
            foreach (var claim in snapshot.escapeGoldClaims) // 저장된 도주 지급 기록 순회
            { // 개별 지급 기록 복원 시작
                escapeGoldClaims.Add(claim.encounter, claim.gold); // 조우별 중복 방지와 승리 차감 복원
            } // 개별 지급 기록 복원 종료
        } // 도주 골드 복원 종료
        pendingHazardHealthDamage = snapshot.hazardHealth; // 대기 체력 피해 복원
        pendingHazardMentalDamage = snapshot.hazardMental; // 대기 정신력 피해 복원
        restoredHazardExposure = snapshot.hazardExposure; // 위험 노출도 복원 예약
        hasRestoredHazardExposure = true; // 위험 노출도 복원 대기 표시
        runExperienceGained = snapshot.runExperience; // 탐사 경험치 합계 복원
        PendingSpecimenDraw = snapshot.pendingSpecimenDraw; // 이전 저장의 기본값 또는 미사용 드로우 복원
        PendingSpecimenActionPoints = snapshot.pendingSpecimenActionPoints; // 이전 저장의 기본값 또는 미사용 행동력 복원
        runGoldGained = snapshot.runGold; // 탐사 골드 합계 복원
        runScrewGained = snapshot.runScrew; // 탐사 나사 합계 복원
        runIronPlateGained = snapshot.runIronPlate; // 탐사 철판 합계 복원
        runWireGained = snapshot.runWire; // 탐사 전선 합계 복원
    } // 탐사 상태 복원 종료
    public bool TryConsumeRestoredHazardExposure(out float exposure) // 복원 대기 위험 노출도 조회
    { // 위험 노출도 조회 시작
        exposure = restoredHazardExposure; // 저장 노출도 전달
        bool available = hasRestoredHazardExposure; // 복원 대기 여부 보관
        hasRestoredHazardExposure = false; // 중복 복원 방지
        return available; // 복원 대기 여부 반환
    } // 위험 노출도 조회 종료
    public void ResetExploration() // 다음 탐사를 위한 런 상태 초기화
    {
        PlayerResourceManager.Instance?.EndExplorationResourceTracking(); // 이전 탐사 재화 구분 해제
        CurrentZone = ExpeditionZoneId.WinterfallAshPlains; // 이전 호출과 저장의 기본 탐사 구역
        CurrentZoneLevel = 1; // 원래 탐사의 기본 첫 난이도
        IsZoneBossReservedForRun = false; // 이전 회차에서 고정한 보스 예약 제거
        PermanentAttackPower = 0; // 회차가 바뀌면 이전 처치의 영구 수치 초기화
        clearedEncounterIds.Clear(); // 클리어 조우 목록 초기화
        resolvedEventIds.Clear(); // 처리 이벤트 목록 초기화
        pendingMapFragmentId = null; // 신규 탐사에서 이전 조각 제거
        UsedSealedMap = false; // 새 탐사에서 지도 사용 기회 복원
        Cart = new BurntCartRunState(); // 새 탐사의 매복과 할인 초기화
        PendingGlassFieldEffects = GlassFieldEffect.None; // 새 탐사의 들판 효과 초기화
        WandererCry = new WandererCryRunState(); // 새 탐사의 추적과 카드 후보 초기화
        Compass = CrackedCompassRunState.ForDeparture(RelicRunManager.Instance?.Inventory); // 출발 시 보유한 나침반의 기회 기록
        Well = new CharredWellRunState(); // 새 탐사의 우물 회복과 봉인 초기화
        avoidedEncounterIds.Clear(); // 신규 탐사의 회피 조우 목록 초기화
        escapeGoldClaims.Clear(); // 신규 탐사의 도주 골드 지급 기록 초기화
        activeEncounter = null; // 현재 조우 초기화
        activeRuntimeEncounterId = null; // 런타임 조우 초기화
        ActiveEncounterInFade = false; // 신규 탐사의 퇴색 조우 정보 초기화
        returnPosition = Vector3.zero; // 복귀 위치 초기화
        hasReturnPosition = false; // 복귀 위치 해제
        LastClearReward = null; // 마지막 보상 초기화
        currentFloor = 1; // 탐사 층 초기화
        encounterPoolVersion = LatestEncounterPoolVersion; // 새 탐사에는 최신 조우 목록 적용
        isExplorationCompleted = false; // 탐사 완료 상태 초기화
        isExplorationSuccess = false; // 탐사 성공 상태 초기화
        completedFloor = 0; // 완료 층 초기화
        completedEncounterCount = 0; // 완료 조우 수 초기화
        lastExplorationSuccessAffinity = 0; // 성공 호감도 표시 초기화
        lastExplorationFailureReason = null; // 탐사 실패 원인 초기화
        restoredHazardExposure = 0f; // 복원 대기 노출도 초기화
        hasRestoredHazardExposure = false; // 노출도 복원 대기 해제
        runExperienceGained = 0; // 런 경험치 합계 초기화
        runGoldGained = 0; // 런 골드 합계 초기화
        runScrewGained = 0; // 런 나사 합계 초기화
        runIronPlateGained = 0; // 런 철판 합계 초기화
        runWireGained = 0; // 런 전선 합계 초기화
        PendingSpecimenDraw = 0; // 새 탐사에 이전 표본병 드로우 이전 방지
        PendingSpecimenActionPoints = 0; // 새 탐사에 이전 표본병 행동력 이전 방지
        ClearPendingHazardPenalty(); // 탐사 환경 피해 대기 상태 초기화
        ClearCurrentFloorSeed(); // 현재 층 Seed 초기화

        Debug.Log(
            "[Exploration][Day49] 다음 탐사를 위해 런 상태를 초기화했습니다."); // 새 탐사 초기화 로그
    }

    private void GrantVictoryRewards(EncounterData encounterData, int previousEscapeGold) // 남은 승리 보상 지급 및 탐사 합계 기록
    {
        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance(); // 캐릭터 성장 관리자 준비

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 영구 자원 관리자 준비

        FacilityUpgradeManager facilityManager =
            FacilityUpgradeManager.EnsureInstance(); // 실제 자원 보너스 계산용 설비 관리자 준비

        int previousLevel =
            progressionManager.Level; // 보상 전 레벨 저장

        int experienceReward =
            encounterData.CharacterExperienceReward; // 현재 조우 경험치 보상 저장

        int bonusPercent = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.VictoryGoldPercent)?.EffectValue ?? 0; // 보유한 동전지갑의 승리 골드 증가율
        int goldReward = (int)System.Math.Min(int.MaxValue, (long)encounterData.GoldReward * (100L + bonusPercent) / 100L); // 기본 골드에만 비율 가산 후 소수점 버림
        goldReward = Mathf.Max(0, goldReward - previousEscapeGold); // 이미 도주에서 받은 골드를 실제 승리 지급량에서 차감

        int baseScrewReward =
            encounterData.ScrewReward; // 현재 조우 기본 나사 보상 저장

        int baseIronPlateReward =
            encounterData.IronPlateReward; // 현재 조우 기본 철판 보상 저장

        int baseWireReward =
            encounterData.WireReward; // 현재 조우 기본 전선 보상 저장

        int rewardedScrew =
            facilityManager.ApplyResourceRewardBonus(baseScrewReward); // 실제 나사 획득량 계산

        int rewardedIronPlate =
            facilityManager.ApplyResourceRewardBonus(baseIronPlateReward); // 실제 철판 획득량 계산

        int rewardedWire =
            facilityManager.ApplyResourceRewardBonus(baseWireReward); // 실제 전선 획득량 계산

        int appliedExperience =
            progressionManager.AddExperience(experienceReward); // 캐릭터 경험치 지급

        resourceManager.AddClearReward(
            goldReward,
            baseScrewReward,
            baseIronPlateReward,
            baseWireReward); // 기존 경로로 실제 클리어 자원 지급

        runExperienceGained += appliedExperience; // 실제 경험치 런 합계 누적
        runGoldGained += goldReward; // 실제 골드 런 합계 누적
        runScrewGained += rewardedScrew; // 실제 나사 런 합계 누적
        runIronPlateGained += rewardedIronPlate; // 실제 철판 런 합계 누적
        runWireGained += rewardedWire; // 실제 전선 런 합계 누적

        LastClearReward =
            new ExplorationClearRewardResult(
                encounterData.DisplayName,
                appliedExperience,
                goldReward,
                rewardedScrew,
                rewardedIronPlate,
                rewardedWire,
                previousLevel,
                progressionManager.Level); // 실제 지급량 기준 마지막 클리어 보상 생성

        Debug.Log(
            $"[Exploration][Day49] 클리어 보상 - " +
            $"EXP +{appliedExperience}, " +
            $"Gold +{goldReward}, " +
            $"나사 +{rewardedScrew}, " +
            $"철판 +{rewardedIronPlate}, " +
            $"전선 +{rewardedWire} / " +
            $"런 누적 Gold {runGoldGained}"); // 실제 지급량과 런 누적 로그
    }

    private void OnDestroy() // 탐사 관리자 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 관리자 참조 초기화
        }
    }
}
