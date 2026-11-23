using System.Collections; // 코루틴 자료형 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 유니티 이벤트 시스템 사용
using UnityEngine.InputSystem.UI; // 유니티 입력 시스템 UI 사용
public sealed class BattleSceneSetup : MonoBehaviour // 전투 씬 초기 구성
{ // 클래스 시작
    private const int MaximumEnemyCount = 4; // 최대 동시 적 수
    [Header("전투 데이터")] // 전투 데이터 구역
    [SerializeField] private BattleLoadoutData battleLoadout; // 출전 파티와 덱 데이터
    [SerializeField] private List<EnemyData> enemies = new List<EnemyData>(); // 출전 적 목록
    [SerializeField] private BattleType battleType = BattleType.Normal; // 현재 전투 유형
    [Header("유닛 생성")] // 유닛 생성 구역
    [SerializeField] private BattleUnitView unitViewPrefab; // 공용 유닛 프리팹
    [SerializeField] private Transform allyUnitRoot; // 아군 유닛 부모
    [SerializeField] private Transform enemyUnitRoot; // 적 유닛 부모
    [Header("카드 시스템")] // 카드 시스템 구역
    [SerializeField] private BattleHandView handView; // 전투 손패 화면
    [Min(1)] // 공용 행동력 최소값
    [SerializeField] private int sharedMaximumActionPoints = 3; // 최대 공용 행동력
    [Min(1)] // 최대 손패 최소값
    [SerializeField] private int maximumHandSize = 5; // 최대 손패 수
    [Min(0)] // 시작 손패 최소값
    [SerializeField] private int initialHandSize = 3; // 시작 손패 수
    [Min(0)] // 턴 드로우 최소값
    [SerializeField] private int cardsPerPlayerTurn = 1; // 플레이어 턴당 드로우 수
    [Min(0f)] // 적 턴 대기 최소값
    [SerializeField] private float enemyTurnDelay = 0.75f; // 적 행동 사이 대기 시간
    [SerializeField] private bool useFixedShuffleSeed; // 고정 셔플 시드 사용 여부
    [SerializeField] private int fixedShuffleSeed = 12345; // 테스트용 고정 셔플 시드
    [Header("테스트")] // 테스트 구역
    [Min(1)] // 테스트 피해 최소값
    [SerializeField] private int testDamage = 10; // 테스트 피해량
    private readonly List<BattleUnitRuntime> allyUnits = new List<BattleUnitRuntime>(); // 생성된 아군 목록
    private readonly List<BattleUnitRuntime> enemyUnits = new List<BattleUnitRuntime>(); // 생성된 적 목록
    private readonly List<string> defeatedEnemyIds = new List<string>(); // 전투 중 처치 적 ID 목록
    private readonly List<BattleUnitView> allyUnitViews = new List<BattleUnitView>(); // 생성된 아군 화면 목록
    private readonly List<BattleUnitView> enemyUnitViews = new List<BattleUnitView>(); // 생성된 적 화면 목록
    private BattleDeckRuntime battleDeck; // 생성된 런타임 덱
    private BattleActionPointRuntime sharedActionPoints; // 생성된 공용 행동력
    private BattleTurnRuntime battleTurn; // 생성된 전투 턴 관리자
    private BattleCardActionController cardActionController; // 카드 행동 관리자
    private BattleStatusEffectProcessor statusEffectProcessor; // 상태 발동과 정화 공통 처리기
    private BattleStatusEffectController statusEffectController; // 상태 이상 발동 관리자
    private BattleMentalController mentalController; // 정신력 흐름 관리자
    private BattleEnemyActionRuntime enemyActionRuntime; // 적 행동 관리자
    private BattleActionSequenceRunner actionSequenceRunner; // 전투 행동 연출 실행기
    private BattleResultManager resultManager; // Scene 간 전투 결과 관리자
    private BattleResultView resultView; // 전투 종료 결과 화면
    private Coroutine enemyTurnCoroutine; // 실행 중인 적 턴 코루틴
    private bool resultStored; // 전투 결과 저장 여부
    public IReadOnlyList<BattleUnitRuntime> AllyUnits => allyUnits; // 아군 목록 조회
    public IReadOnlyList<BattleUnitRuntime> EnemyUnits => enemyUnits; // 적 목록 조회
    public BattleDeckRuntime BattleDeck => battleDeck; // 런타임 덱 조회
    public BattleActionPointRuntime SharedActionPoints => sharedActionPoints; // 공용 행동력 조회
    public BattleTurnRuntime BattleTurn => battleTurn; // 전투 턴 관리자 조회
    public BattleEnemyActionRuntime EnemyActionRuntime => enemyActionRuntime; // 적 행동 관리자 조회
    public bool IsInitialized { get; private set; } // 전투 초기화 여부
    private void Start() // 씬 시작 처리
    { // 시작 처리 시작
        InitializeBattle(); // 전투 전체 초기화
    } // 시작 처리 종료
    public void InitializeBattle() // 전투 초기화
    { // 초기화 시작
        if (IsInitialized) // 중복 초기화 확인
        { // 중복 처리 시작
            return; // 중복 초기화 중단
        } // 중복 처리 종료
        if (!ValidateSetup()) // 설정 유효성 확인
        { // 설정 오류 처리 시작
            return; // 초기화 중단
        } // 설정 오류 처리 종료
        EnsureEventSystem(); // UI 클릭 이벤트 시스템 준비
        resultManager = BattleResultManager.EnsureInstance(); // Scene 간 결과 관리자 준비
        resultManager.DiscardPendingResult(); // 이전 미소비 전투 결과 제거
        CreateAllyUnits(); // 아군 유닛 생성
        CreateEnemyUnits(); // 적 유닛 생성
        int? shuffleSeed = useFixedShuffleSeed ? fixedShuffleSeed : (int?)null; // 적용할 셔플 시드 결정
        battleDeck = BattleDeckRuntime.Create(battleLoadout.Deck, allyUnits, maximumHandSize, shuffleSeed); // 전투용 카드 더미 생성
        sharedActionPoints = new BattleActionPointRuntime(sharedMaximumActionPoints); // 전투 공용 행동력 생성
        battleTurn = new BattleTurnRuntime(battleDeck, sharedActionPoints, allyUnits, enemyUnits, cardsPerPlayerTurn, battleType); // 전투 유형 포함 턴 관리자 생성
        if (!handView.Bind(battleDeck, sharedActionPoints, battleTurn)) // 손패 화면 연결 확인
        { // 손패 화면 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 전투 손패 화면 연결에 실패했습니다.", this); // 손패 화면 오류 출력
            battleTurn.Dispose(); // 턴 관리자 이벤트 연결 해제
            battleTurn = null; // 턴 관리자 참조 제거
            return; // 초기화 중단
        } // 손패 화면 오류 처리 종료
        handView.EscapeClicked += HandleEscapeClicked; // 손패 도주 요청 연결
        actionSequenceRunner = GetComponent<BattleActionSequenceRunner>(); // 기존 행동 연출 실행기 조회
        if (actionSequenceRunner == null) // 행동 연출 실행기 누락 확인
        { // 행동 연출 실행기 생성 시작
            actionSequenceRunner = gameObject.AddComponent<BattleActionSequenceRunner>(); // 런타임 행동 연출 실행기 추가
        } // 행동 연출 실행기 생성 종료
        actionSequenceRunner.BusyStateChanged += handView.SetInteractionLocked; // 행동 연출 입력 잠금 연결
        statusEffectProcessor = new BattleStatusEffectProcessor(); // 공통 상태 처리기 생성
        cardActionController = new BattleCardActionController(battleDeck, sharedActionPoints, battleTurn, handView, actionSequenceRunner, statusEffectProcessor, allyUnitViews, enemyUnitViews); // 카드 행동 관리자 생성
        mentalController = new BattleMentalController(battleTurn, allyUnits, enemyUnits); // 정신력 흐름 관리자 생성
        statusEffectController = new BattleStatusEffectController(battleTurn, allyUnits, enemyUnits, statusEffectProcessor); // 상태 이상 발동 관리자 생성
        statusEffectController.StatusEffectsProcessed += HandleStatusEffectsProcessed; // 상태 처리 결과 화면 연결
        enemyActionRuntime = new BattleEnemyActionRuntime(enemyUnits, allyUnits); // 적 행동 관리자 생성
        enemyActionRuntime.StateChanged += HandleEnemyActionStateChanged; // 적 행동 변경 이벤트 등록
        RegisterAllyStatusIntentEvents(); // 아군 상태 변경 예고 갱신 등록
        RegisterMentalIntentEvents(); // 정신 상태 변경 예고 갱신 등록
        battleTurn.StateChanged += HandleTurnStateChanged; // 턴 상태 변경 이벤트 등록
        IsInitialized = true; // 초기화 완료 저장
        if (!battleTurn.StartBattle(initialHandSize)) // 전투 시작 처리 확인
        { // 전투 시작 실패 처리 시작
            Debug.LogError("[BattleSceneSetup] 전투 턴 시작에 실패했습니다.", this); // 전투 시작 오류 출력
            IsInitialized = false; // 초기화 실패 저장
            return; // 초기화 중단
        } // 전투 시작 실패 처리 종료
        int drawnCardCount = battleTurn.LastDrawnCardCount; // 실제 시작 손패 수 조회
        int preparedActionCount = enemyActionRuntime.PrepareActions(); // 첫 적 행동 준비
        Debug.Log($"[BattleSceneSetup] 전투 초기화 완료 - 아군 {allyUnits.Count}명, 적 {enemyUnits.Count}명, 전체 카드 {battleDeck.CardCount}장, 시작 손패 {drawnCardCount}장, 공용 AP {sharedActionPoints.CurrentActionPoints}", this); // 생성 완료 출력
        Debug.Log($"[BattleSceneSetup] 적 행동 예고 준비 - {preparedActionCount}개", this); // 첫 행동 준비 결과 출력
        LogDeckState(); // 시작 카드 상태 출력
    } // 초기화 종료
    private bool ValidateSetup() // 설정 유효성 검사
    { // 유효성 검사 시작
        if (battleLoadout == null) // 전투 편성 누락 확인
        { // 편성 누락 처리 시작
            Debug.LogError("[BattleSceneSetup] BattleLoadoutData가 연결되지 않았습니다.", this); // 편성 누락 출력
            return false; // 검사 실패 반환
        } // 편성 누락 처리 종료
        if (!battleLoadout.IsValidLoadout()) // 전투 편성 유효성 확인
        { // 잘못된 편성 처리 시작
            Debug.LogError("[BattleSceneSetup] BattleLoadoutData가 올바르지 않습니다.", this); // 편성 오류 출력
            return false; // 검사 실패 반환
        } // 잘못된 편성 처리 종료
        if (unitViewPrefab == null) // 유닛 프리팹 누락 확인
        { // 프리팹 누락 처리 시작
            Debug.LogError("[BattleSceneSetup] BattleUnitView 프리팹이 연결되지 않았습니다.", this); // 프리팹 누락 출력
            return false; // 검사 실패 반환
        } // 프리팹 누락 처리 종료
        if (allyUnitRoot == null || enemyUnitRoot == null) // 유닛 부모 누락 확인
        { // 부모 누락 처리 시작
            Debug.LogError("[BattleSceneSetup] 아군 또는 적 유닛 부모가 연결되지 않았습니다.", this); // 부모 누락 출력
            return false; // 검사 실패 반환
        } // 부모 누락 처리 종료
        if (handView == null) // 손패 화면 누락 확인
        { // 손패 화면 누락 처리 시작
            Debug.LogError("[BattleSceneSetup] BattleHandView가 연결되지 않았습니다.", this); // 손패 화면 누락 출력
            return false; // 검사 실패 반환
        } // 손패 화면 누락 처리 종료
        if (maximumHandSize < 1) // 최대 손패 범위 확인
        { // 최대 손패 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 최대 손패 수는 1 이상이어야 합니다.", this); // 최대 손패 오류 출력
            return false; // 검사 실패 반환
        } // 최대 손패 오류 처리 종료
        if (sharedMaximumActionPoints < 1) // 공용 행동력 범위 확인
        { // 공용 행동력 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 최대 공용 AP는 1 이상이어야 합니다.", this); // 공용 행동력 오류 출력
            return false; // 검사 실패 반환
        } // 공용 행동력 오류 처리 종료
        if (initialHandSize < 0 || initialHandSize > maximumHandSize) // 시작 손패 범위 확인
        { // 시작 손패 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 시작 손패 수는 0 이상이며 최대 손패 수 이하여야 합니다.", this); // 시작 손패 오류 출력
            return false; // 검사 실패 반환
        } // 시작 손패 오류 처리 종료
        if (cardsPerPlayerTurn < 0) // 턴당 드로우 범위 확인
        { // 턴 드로우 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 플레이어 턴당 드로우 수는 0 이상이어야 합니다.", this); // 턴 드로우 오류 출력
            return false; // 검사 실패 반환
        } // 턴 드로우 오류 처리 종료
        if (enemyTurnDelay < 0f) // 적 턴 대기 범위 확인
        { // 적 턴 대기 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 적 행동 대기 시간은 0 이상이어야 합니다.", this); // 적 턴 대기 오류 출력
            return false; // 검사 실패 반환
        } // 적 턴 대기 오류 처리 종료
        if (enemies.Count < 1) // 적 목록 비어 있음 확인
        { // 빈 적 목록 처리 시작
            Debug.LogError("[BattleSceneSetup] 출전할 적이 없습니다.", this); // 적 누락 출력
            return false; // 검사 실패 반환
        } // 빈 적 목록 처리 종료
        if (enemies.Count > MaximumEnemyCount) // 최대 적 수 초과 확인
        { // 적 수 초과 처리 시작
            Debug.LogError($"[BattleSceneSetup] 출전 적은 최대 {MaximumEnemyCount}명입니다.", this); // 적 수 초과 오류 출력
            return false; // 검사 실패 반환
        } // 적 수 초과 처리 종료
        foreach (EnemyData enemyData in enemies) // 적 목록 순회
        { // 적 검사 시작
            if (enemyData == null) // 빈 적 데이터 확인
            { // 빈 적 처리 시작
                Debug.LogError("[BattleSceneSetup] 적 목록에 빈 데이터가 있습니다.", this); // 적 오류 출력
                return false; // 검사 실패 반환
            } // 빈 적 처리 종료
        } // 적 검사 종료
        return true; // 검사 성공 반환
    } // 유효성 검사 종료
    private static void EnsureEventSystem() // UI 이벤트 시스템 준비
    { // 이벤트 시스템 준비 시작
        if (EventSystem.current != null) // 기존 이벤트 시스템 확인
        { // 기존 시스템 처리 시작
            return; // 이벤트 시스템 생성 중단
        } // 기존 시스템 처리 종료
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 입력 이벤트 시스템 생성
        eventSystemObject.transform.SetParent(null); // 이벤트 시스템 최상위 배치
    } // 이벤트 시스템 준비 종료
    private void CreateAllyUnits() // 아군 유닛 생성
    { // 아군 생성 시작
        foreach (CharacterData characterData in battleLoadout.Party.Members) // 파티원 순회
        { // 파티원 생성 시작
            BattleUnitRuntime runtimeUnit = BattleUnitRuntime.CreateAlly(characterData); // 아군 런타임 생성
            resultManager?.ApplySavedAllyState(runtimeUnit); // 이전 전투에서 저장한 아군 체력 적용
            BattleUnitView unitView = Instantiate(unitViewPrefab, allyUnitRoot); // 아군 화면 오브젝트 생성
            unitView.name = $"Ally_{runtimeUnit.UnitId}"; // 아군 오브젝트 이름 적용
            unitView.Bind(runtimeUnit); // 아군 화면 연결
            allyUnits.Add(runtimeUnit); // 아군 목록 등록
            allyUnitViews.Add(unitView); // 아군 화면 목록 등록
        } // 파티원 생성 종료
    } // 아군 생성 종료
    private void CreateEnemyUnits() // 적 유닛 생성
    { // 적 생성 시작
        foreach (EnemyData enemyData in enemies) // 적 데이터 순회
        { // 적 생성 시작
            CreateEnemyUnit(enemyData); // 초기 적 런타임과 화면 생성
        } // 적 생성 종료
    } // 적 유닛 생성 종료
    private BattleUnitRuntime CreateEnemyUnit(EnemyData enemyData) // 적 런타임과 화면 생성
    { // 적 유닛 생성 시작
        BattleUnitRuntime runtimeUnit = BattleUnitRuntime.CreateEnemy(enemyData); // 적 런타임 생성
        runtimeUnit.Died += HandleEnemyResultDied; // 처치 적 결과 기록 이벤트 등록
        BattleUnitView unitView = Instantiate(unitViewPrefab, enemyUnitRoot); // 적 화면 오브젝트 생성
        unitView.name = $"Enemy_{runtimeUnit.UnitId}_{enemyUnits.Count + 1}"; // 적 오브젝트 고유 이름 적용
        unitView.Bind(runtimeUnit); // 적 화면 연결
        enemyUnits.Add(runtimeUnit); // 적 목록 등록
        enemyUnitViews.Add(unitView); // 적 화면 목록 등록
        return runtimeUnit; // 생성 적 런타임 반환
    } // 적 유닛 생성 종료
    public bool TrySummonEnemy(EnemyData enemyData) // 전투 중 적 소환
    { // 적 소환 시작
        if (!IsInitialized || battleTurn == null || battleTurn.IsBattleEnded || enemyActionRuntime == null || enemyData == null) // 소환 기본 조건 확인
        { // 소환 불가 처리 시작
            return false; // 적 소환 실패 반환
        } // 소환 불가 처리 종료
        if (CountLivingEnemies() >= MaximumEnemyCount) // 최대 생존 적 수 확인
        { // 적 수 제한 처리 시작
            Debug.LogWarning($"[BattleSceneSetup] 생존 적은 최대 {MaximumEnemyCount}명입니다.", this); // 적 수 제한 안내
            return false; // 적 소환 실패 반환
        } // 적 수 제한 처리 종료
        RemoveDefeatedEnemySlotIfNeeded(); // 최대 화면 수를 위한 사망 적 슬롯 정리
        BattleUnitRuntime summonedEnemy = CreateEnemyUnit(enemyData); // 소환 적 런타임과 화면 생성
        bool turnRegistered = battleTurn.RegisterSummonedEnemy(summonedEnemy); // 승패 판정에 소환 적 등록
        bool actionRegistered = enemyActionRuntime.RegisterSummonedEnemy(summonedEnemy); // 적 행동 흐름에 소환 적 등록
        bool mentalRegistered = mentalController != null && mentalController.RegisterSummonedEnemy(summonedEnemy); // 정신력 흐름에 소환 적 등록
        if (!turnRegistered || !actionRegistered || !mentalRegistered) // 소환 연결 결과 확인
        { // 연결 실패 복구 시작
            RemoveEnemyUnit(summonedEnemy); // 불완전 소환 적 제거
            Debug.LogError("[BattleSceneSetup] 소환 적의 전투 연결에 실패했습니다.", this); // 소환 연결 오류 출력
            return false; // 적 소환 실패 반환
        } // 연결 실패 복구 종료
        summonedEnemy.MentalChanged += HandleMentalIntentChanged; // 소환 적 정신 상태 예고 갱신 등록
        Debug.Log($"[BattleSceneSetup] 적 소환 - {summonedEnemy.DisplayName} / 현재 생존 적 {CountLivingEnemies()}명 / 다음 행동 준비부터 참여", this); // 적 소환 결과 출력
        return true; // 적 소환 성공 반환
    } // 적 소환 종료
    private int CountLivingEnemies() // 생존 적 수 계산
    { // 생존 적 계산 시작
        int livingCount = 0; // 생존 적 수 초기화
        foreach (BattleUnitRuntime enemyUnit in enemyUnits) // 적 런타임 목록 순회
        { // 적 생존 확인 시작
            if (enemyUnit != null && !enemyUnit.IsDead) // 생존 적 확인
            { // 생존 적 처리 시작
                livingCount++; // 생존 적 수 증가
            } // 생존 적 처리 종료
        } // 적 생존 확인 종료
        return livingCount; // 생존 적 수 반환
    } // 생존 적 계산 종료
    private void RemoveDefeatedEnemySlotIfNeeded() // 사망 적 화면 슬롯 정리
    { // 사망 슬롯 정리 시작
        if (enemyUnits.Count < MaximumEnemyCount) // 전체 적 화면 수 확인
        { // 슬롯 여유 처리 시작
            return; // 사망 슬롯 정리 중단
        } // 슬롯 여유 처리 종료
        for (int enemyIndex = 0; enemyIndex < enemyUnits.Count; enemyIndex++) // 적 목록 순회
        { // 사망 적 검색 시작
            BattleUnitRuntime enemyUnit = enemyUnits[enemyIndex]; // 현재 적 런타임 조회
            if (enemyUnit != null && enemyUnit.IsDead) // 사망 적 확인
            { // 사망 적 슬롯 처리 시작
                RemoveEnemyUnit(enemyUnit); // 사망 적 런타임과 화면 제거
                return; // 한 개 슬롯 정리 종료
            } // 사망 적 슬롯 처리 종료
        } // 사망 적 검색 종료
    } // 사망 슬롯 정리 종료
    private void RemoveEnemyUnit(BattleUnitRuntime enemyUnit) // 적 런타임과 화면 제거
    { // 적 제거 시작
        if (enemyUnit == null) // 제거 적 존재 확인
        { // 적 없음 처리 시작
            return; // 적 제거 중단
        } // 적 없음 처리 종료
        battleTurn?.UnregisterEnemy(enemyUnit); // 승패 판정 사망 연결 해제
        enemyActionRuntime?.UnregisterEnemy(enemyUnit); // 행동 흐름 사망 연결 해제
        mentalController?.UnregisterEnemy(enemyUnit); // 정신력 흐름 사망 연결 해제
        enemyUnit.MentalChanged -= HandleMentalIntentChanged; // 적 정신 상태 예고 갱신 해제
        enemyUnit.Died -= HandleEnemyResultDied; // 처치 적 결과 기록 이벤트 해제
        BattleUnitView enemyView = FindUnitView(enemyUnit, enemyUnitViews); // 제거 적 화면 조회
        if (enemyView != null) // 제거 적 화면 존재 확인
        { // 적 화면 제거 시작
            enemyView.Unbind(); // 적 화면 런타임 연결 해제
            enemyUnitViews.Remove(enemyView); // 적 화면 목록 제거
            Destroy(enemyView.gameObject); // 적 화면 오브젝트 제거
        } // 적 화면 제거 종료
        enemyUnits.Remove(enemyUnit); // 적 런타임 목록 제거
    } // 적 제거 종료
    private void HandleEnemyResultDied(BattleUnitRuntime enemyUnit) // 처치 적 결과 기록
    { // 처치 적 기록 시작
        if (enemyUnit == null || string.IsNullOrWhiteSpace(enemyUnit.UnitId) || defeatedEnemyIds.Contains(enemyUnit.UnitId)) // 처치 적 ID 유효성과 중복 확인
        { // 기록 불가 처리 시작
            return; // 처치 적 기록 중단
        } // 기록 불가 처리 종료
        defeatedEnemyIds.Add(enemyUnit.UnitId); // 처치 적 ID 결과 목록 추가
    } // 처치 적 기록 종료
    private bool CanUseBattleDeckTest() // 카드 테스트 가능 여부 확인
    { // 카드 테스트 검사 시작
        if (!Application.isPlaying) // 플레이 모드 확인
        { // 비플레이 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 플레이 모드에서 실행해야 합니다.", this); // 플레이 모드 안내
            return false; // 카드 테스트 불가 반환
        } // 비플레이 처리 종료
        if (!IsInitialized || battleDeck == null) // 런타임 덱 생성 여부 확인
        { // 덱 없음 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 런타임 덱이 생성되지 않았습니다.", this); // 덱 없음 출력
            return false; // 카드 테스트 불가 반환
        } // 덱 없음 처리 종료
        return true; // 카드 테스트 가능 반환
    } // 카드 테스트 검사 종료
    private void LogDeckState() // 카드 영역 상태 출력
    { // 카드 상태 출력 시작
        Debug.Log($"[BattleDeckRuntime] 전체 {battleDeck.CardCount}장 / 뽑을 카드 {battleDeck.DrawPileCount}장 / 손패 {battleDeck.HandCount}장 / 버린 카드 {battleDeck.DiscardPileCount}장 / 상태 정상 {battleDeck.IsStateValid()}", this); // 카드 영역 수량 출력
    } // 카드 상태 출력 종료
    [ContextMenu("테스트/첫 번째 아군 피해")] // 아군 피해 메뉴
    private void DamageFirstAlly() // 첫 번째 아군 피해 테스트
    { // 아군 피해 시작
        if (!Application.isPlaying) // 플레이 모드 확인
        { // 비플레이 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 플레이 모드에서 실행해야 합니다.", this); // 플레이 모드 안내
            return; // 피해 테스트 중단
        } // 비플레이 처리 종료
        if (allyUnits.Count < 1) // 아군 존재 확인
        { // 아군 없음 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 피해를 받을 아군이 없습니다.", this); // 아군 없음 출력
            return; // 피해 테스트 중단
        } // 아군 없음 처리 종료
        allyUnits[0].TakeDamage(testDamage, BattleDamageType.Physical); // 첫 번째 아군 물리 피해 적용
    } // 아군 피해 종료
    [ContextMenu("테스트/첫 번째 적 피해")] // 적 피해 메뉴
    private void DamageFirstEnemy() // 첫 번째 적 피해 테스트
    { // 적 피해 시작
        if (!Application.isPlaying) // 플레이 모드 확인
        { // 비플레이 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 플레이 모드에서 실행해야 합니다.", this); // 플레이 모드 안내
            return; // 피해 테스트 중단
        } // 비플레이 처리 종료
        if (enemyUnits.Count < 1) // 적 존재 확인
        { // 적 없음 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 피해를 받을 적이 없습니다.", this); // 적 없음 출력
            return; // 피해 테스트 중단
        } // 적 없음 처리 종료
        enemyUnits[0].TakeDamage(testDamage, BattleDamageType.Physical); // 첫 번째 적 물리 피해 적용
    } // 적 피해 종료
    [ContextMenu("테스트/카드 인스턴스 출력")] // 카드 출력 메뉴
    private void LogCardInstances() // 카드 인스턴스 출력
    { // 카드 출력 시작
        if (!CanUseBattleDeckTest()) // 카드 테스트 가능 여부 확인
        { // 카드 테스트 불가 처리 시작
            return; // 카드 출력 중단
        } // 카드 테스트 불가 처리 종료
        foreach (CardInstance cardInstance in battleDeck.Cards) // 카드 인스턴스 순회
        { // 카드 출력 시작
            Debug.Log($"[CardInstance] {cardInstance.InstanceId} / {cardInstance.DisplayName} / 소유자 {cardInstance.OwnerUnit.DisplayName}", this); // 카드 정보 출력
        } // 카드 출력 종료
    } // 카드 출력 종료
    [ContextMenu("테스트/카드 상태 출력")] // 카드 상태 메뉴
    private void LogCardState() // 카드 상태 테스트
    { // 카드 상태 테스트 시작
        if (!CanUseBattleDeckTest()) // 카드 테스트 가능 여부 확인
        { // 카드 테스트 불가 처리 시작
            return; // 카드 상태 출력 중단
        } // 카드 테스트 불가 처리 종료
        LogDeckState(); // 카드 영역 수량 출력
        foreach (CardInstance cardInstance in battleDeck.Hand) // 손패 카드 순회
        { // 손패 출력 시작
            Debug.Log($"[Hand] {cardInstance.InstanceId} / {cardInstance.DisplayName} / 소유자 {cardInstance.OwnerUnit.DisplayName}", this); // 손패 카드 출력
        } // 손패 출력 종료
    } // 카드 상태 테스트 종료
    [ContextMenu("테스트/카드 한 장 드로우")] // 카드 드로우 메뉴
    private void DrawOneCard() // 카드 한 장 드로우 테스트
    { // 카드 드로우 테스트 시작
        if (!CanUseBattleDeckTest()) // 카드 테스트 가능 여부 확인
        { // 카드 테스트 불가 처리 시작
            return; // 카드 드로우 중단
        } // 카드 테스트 불가 처리 종료
        int drawnCardCount = battleDeck.DrawCards(1); // 카드 한 장 드로우 시도
        Debug.Log($"[BattleSceneSetup] 카드 드로우 결과 - {drawnCardCount}장", this); // 드로우 결과 출력
        LogDeckState(); // 카드 영역 수량 출력
    } // 카드 드로우 테스트 종료
    [ContextMenu("테스트/손패 첫 카드 버리기")] // 카드 버리기 메뉴
    private void DiscardFirstHandCard() // 손패 첫 카드 버리기 테스트
    { // 카드 버리기 테스트 시작
        if (!CanUseBattleDeckTest()) // 카드 테스트 가능 여부 확인
        { // 카드 테스트 불가 처리 시작
            return; // 카드 버리기 중단
        } // 카드 테스트 불가 처리 종료
        if (battleDeck.HandCount < 1) // 손패 카드 존재 확인
        { // 빈 손패 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 버릴 손패 카드가 없습니다.", this); // 빈 손패 출력
            return; // 카드 버리기 중단
        } // 빈 손패 처리 종료
        CardInstance cardInstance = battleDeck.Hand[0]; // 손패 첫 카드 선택
        bool discarded = battleDeck.DiscardCard(cardInstance); // 선택 카드 버리기 시도
        Debug.Log($"[BattleSceneSetup] 카드 버리기 결과 - {discarded} / {cardInstance.DisplayName}", this); // 버리기 결과 출력
        LogDeckState(); // 카드 영역 수량 출력
    } // 카드 버리기 테스트 종료
    [ContextMenu("테스트/뽑을 카드 더미 섞기")] // 카드 셔플 메뉴
    private void ShuffleCurrentDrawPile() // 카드 더미 셔플 테스트
    { // 카드 셔플 테스트 시작
        if (!CanUseBattleDeckTest()) // 카드 테스트 가능 여부 확인
        { // 카드 테스트 불가 처리 시작
            return; // 카드 셔플 중단
        } // 카드 테스트 불가 처리 종료
        battleDeck.ShuffleDrawPile(); // 현재 뽑을 카드 더미 셔플
        Debug.Log("[BattleSceneSetup] 뽑을 카드 더미를 다시 섞었습니다.", this); // 셔플 완료 출력
        LogDeckState(); // 카드 영역 수량 출력
    } // 카드 셔플 테스트 종료
    [ContextMenu("테스트/공용 AP 회복")] // 공용 행동력 회복 메뉴
    private void RestoreSharedActionPoints() // 공용 행동력 회복 테스트
    { // 행동력 회복 테스트 시작
        if (!CanUseBattleDeckTest()) // 전투 테스트 가능 여부 확인
        { // 테스트 불가 처리 시작
            return; // 행동력 회복 중단
        } // 테스트 불가 처리 종료
        bool restored = sharedActionPoints.Restore(); // 공용 행동력 최대 회복
        Debug.Log($"[BattleSceneSetup] 공용 AP 회복 결과 - {restored} / 현재 {sharedActionPoints.CurrentActionPoints}", this); // 행동력 회복 결과 출력
    } // 행동력 회복 테스트 종료
    [ContextMenu("테스트/첫 번째 적 데이터 소환")] // 적 소환 테스트 메뉴
    private void SummonFirstEnemyData() // 첫 적 데이터 소환 테스트
    { // 적 소환 테스트 시작
        if (!Application.isPlaying) // 플레이 모드 확인
        { // 비플레이 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 플레이 모드에서 실행해야 합니다.", this); // 플레이 모드 안내
            return; // 적 소환 테스트 중단
        } // 비플레이 처리 종료
        if (enemies.Count < 1 || enemies[0] == null) // 소환 원본 데이터 확인
        { // 소환 데이터 없음 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 소환할 테스트 적 데이터가 없습니다.", this); // 소환 데이터 없음 안내
            return; // 적 소환 테스트 중단
        } // 소환 데이터 없음 처리 종료
        bool summoned = TrySummonEnemy(enemies[0]); // 첫 적 데이터 소환 시도
        Debug.Log($"[BattleSceneSetup] 적 소환 테스트 결과 - {summoned}", this); // 적 소환 테스트 결과 출력
    } // 적 소환 테스트 종료
    private void HandleEscapeClicked() // 손패 도주 요청 처리
    { // 도주 요청 처리 시작
        if (battleTurn == null) // 턴 관리자 존재 확인
        { // 관리자 없음 처리 시작
            return; // 도주 요청 처리 중단
        } // 관리자 없음 처리 종료
        bool escaped = battleTurn.TryEscape(); // 전투 도주 결과 확정 시도
        if (!escaped && battleTurn.BattleType == BattleType.Boss) // 보스전 도주 거부 확인
        { // 보스전 거부 처리 시작
            Debug.LogWarning("[BattleSceneSetup] 보스 전투에서는 도주할 수 없습니다.", this); // 보스전 도주 불가 안내
        } // 보스전 거부 처리 종료
    } // 도주 요청 처리 종료
    private void HandleTurnStateChanged() // 전투 턴 상태 변경 처리
    { // 턴 상태 처리 시작
        if (battleTurn == null) // 턴 관리자 존재 확인
        { // 턴 관리자 없음 처리 시작
            return; // 턴 상태 처리 중단
        } // 턴 관리자 없음 처리 종료
        if (battleTurn.CurrentPhase == BattleTurnPhase.EnemyTurn) // 적 턴 진입 확인
        { // 적 턴 처리 시작
            if (enemyTurnCoroutine == null) // 기존 적 턴 실행 여부 확인
            { // 적 턴 시작 처리 시작
                enemyTurnCoroutine = StartCoroutine(ExecuteEnemyTurn()); // 적 행동 실행 시작
            } // 적 턴 시작 처리 종료
            return; // 턴 상태 처리 종료
        } // 적 턴 처리 종료
        if (battleTurn.IsBattleEnded && enemyTurnCoroutine != null) // 전투 종료와 적 턴 실행 확인
        { // 적 턴 중단 처리 시작
            actionSequenceRunner?.CancelCurrentAction(); // 실행 중인 유닛 움직임 복구
            StopCoroutine(enemyTurnCoroutine); // 실행 중인 적 턴 중단
            enemyTurnCoroutine = null; // 적 턴 코루틴 참조 제거
        } // 적 턴 중단 처리 종료
        if (battleTurn.IsBattleEnded && enemyActionRuntime != null) // 전투 종료와 적 행동 관리자 확인
        { // 적 행동 제거 시작
            enemyActionRuntime.ClearActions(); // 남은 적 행동 제거
        } // 적 행동 제거 종료
        if (battleTurn.IsBattleEnded) // 전투 결과 확정 확인
        { // 결과 저장과 표시 시작
            StoreAndShowBattleResult(); // 전투 결과 스냅샷 저장과 화면 표시
        } // 결과 저장과 표시 종료
        if (battleTurn.CurrentPhase == BattleTurnPhase.Victory) // 승리 상태 확인
        { // 승리 처리 시작
            Debug.Log("[BattleSceneSetup] 전투 승리", this); // 승리 결과 출력
        } // 승리 처리 종료
        else if (battleTurn.CurrentPhase == BattleTurnPhase.Defeat) // 패배 상태 확인
        { // 패배 처리 시작
            Debug.Log("[BattleSceneSetup] 전투 패배", this); // 패배 결과 출력
        } // 패배 처리 종료
        else if (battleTurn.CurrentPhase == BattleTurnPhase.Escaped) // 도주 상태 확인
        { // 도주 처리 시작
            Debug.Log("[BattleSceneSetup] 전투 도주 - 보상 없음 / 현재 아군 HP와 정신력 유지", this); // 도주 결과 출력
        } // 도주 처리 종료
    } // 턴 상태 처리 종료
    private void StoreAndShowBattleResult() // 전투 결과 저장과 화면 표시
    { // 결과 저장 시작
        if (resultStored || battleTurn == null || !battleTurn.IsBattleEnded) // 결과 저장 상태와 종료 여부 확인
        { // 저장 불가 처리 시작
            return; // 결과 저장 중단
        } // 저장 불가 처리 종료
        resultManager = resultManager == null ? BattleResultManager.EnsureInstance() : resultManager; // 영구 결과 관리자 확인
        BattleResultData resultData = new BattleResultData(battleTurn.Result, battleTurn.BattleType, battleTurn.CurrentRound, allyUnits, defeatedEnemyIds); // 현재 전투 결과 스냅샷 생성
        if (!resultManager.StoreResult(resultData)) // 영구 결과 저장 확인
        { // 저장 실패 처리 시작
            Debug.LogError("[BattleSceneSetup] 전투 결과 저장에 실패했습니다.", this); // 결과 저장 오류 출력
            return; // 결과 화면 표시 중단
        } // 저장 실패 처리 종료
        resultStored = true; // 결과 저장 완료 표시
        EnsureResultView(); // 전투 결과 화면 준비
        if (resultView == null) // 결과 화면 생성 여부 확인
        { // 결과 화면 없음 처리 시작
            return; // 결과 화면 표시 중단
        } // 결과 화면 없음 처리 종료
        resultView.Show(resultData, ConfirmBattleResult); // 전투 결과와 확인 처리 표시
        Debug.Log($"[BattleSceneSetup] 전투 결과 저장 - {resultData.Result} / 라운드 {resultData.CompletedRound} / 생존 아군 {resultData.LivingAllyCount}명 / 보상 {resultData.CanReceiveReward}", this); // 결과 저장 내용 출력
    } // 결과 저장 종료
    private void EnsureResultView() // 전투 결과 화면 준비
    { // 결과 화면 준비 시작
        if (resultView != null) // 기존 결과 화면 확인
        { // 기존 화면 처리 시작
            return; // 결과 화면 준비 중단
        } // 기존 화면 처리 종료
        Canvas battleCanvas = handView == null ? null : handView.GetComponentInParent<Canvas>(); // 전투 Canvas 조회
        if (battleCanvas == null) // 전투 Canvas 존재 확인
        { // Canvas 없음 처리 시작
            Debug.LogError("[BattleSceneSetup] 전투 결과 화면을 배치할 Canvas가 없습니다.", this); // Canvas 누락 오류 출력
            return; // 결과 화면 준비 중단
        } // Canvas 없음 처리 종료
        resultView = BattleResultView.Create(battleCanvas.transform); // Canvas 아래 결과 화면 코드 생성
    } // 결과 화면 준비 종료
    private bool ConfirmBattleResult() // 전투 결과 확인과 탐사 복귀
    { // 결과 확인 시작
        if (SceneFlowManager.Instance == null) // Scene 전환 관리자 확인
        { // 관리자 없음 처리 시작
            Debug.LogError("[BattleSceneSetup] 탐사 Scene으로 이동할 SceneFlowManager가 없습니다.", this); // Scene 관리자 누락 오류 출력
            return false; // 결과 확인 실패 반환
        } // 관리자 없음 처리 종료
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 탐사 Scene 복귀 요청
        return SceneFlowManager.Instance.IsLoadingScene; // Scene 전환 시작 여부 반환
    } // 결과 확인 종료
    private void HandleEnemyActionStateChanged() // 적 행동 변경 처리
    { // 적 행동 변경 처리 시작
        foreach (BattleUnitView enemyView in enemyUnitViews) // 적 화면 목록 순회
        { // 적 행동 예고 갱신 시작
            BattleEnemyAction plannedAction = enemyActionRuntime == null ? null : enemyActionRuntime.FindAction(enemyView.RuntimeUnit); // 화면 적 예정 행동 조회
            enemyView.SetEnemyIntent(plannedAction); // 적 행동 예고 적용
        } // 적 행동 예고 갱신 종료
    } // 적 행동 변경 처리 종료
    private void HandleAllyStatusEffectsChanged(BattleUnitRuntime allyUnit) // 아군 상태 변경 처리
    { // 아군 상태 변경 시작
        HandleEnemyActionStateChanged(); // 방어력과 면역 포함 적 예고 갱신
    } // 아군 상태 변경 종료
    private void HandleMentalIntentChanged(BattleUnitRuntime runtimeUnit, BattleMentalChangeResult changeResult) // 정신 상태 변경 예고 처리
    { // 정신 상태 예고 처리 시작
        HandleEnemyActionStateChanged(); // 정신 상태 포함 적 예고 갱신
    } // 정신 상태 예고 처리 종료
    private void HandleStatusEffectsProcessed(BattleUnitRuntime runtimeUnit, IReadOnlyList<BattleStatusEffectProcessResult> processResults) // 상태 일괄 처리 결과 표시
    { // 상태 결과 표시 시작
        IReadOnlyList<BattleUnitView> unitViews = runtimeUnit.Team == BattleTeam.Ally ? allyUnitViews : enemyUnitViews; // 진영별 유닛 화면 목록 선택
        BattleUnitView unitView = FindUnitView(runtimeUnit, unitViews); // 처리 유닛 화면 조회
        foreach (BattleStatusEffectProcessResult processResult in processResults) // 상태 처리 결과 순회
        { // 개별 결과 표시 시작
            string effectName = BattleStatusEffectInstance.GetDisplayName(processResult.EffectType); // 상태 표시 이름 조회
            if (processResult.WasExpired) // 자연 만료 결과 확인
            { // 만료 피드백 시작
                unitView?.ShowStatusFeedback($"{effectName} 만료", BattleStatusEffectInstance.IsDebuffType(processResult.EffectType)); // 상태 만료 문구 표시
            } // 만료 피드백 종료
            string triggerLabel = processResult.WasTriggered ? $"적용 {processResult.AppliedAmount}" : "지속"; // 발동 또는 유지 문구 계산
            Debug.Log($"[BattleStatus] 턴 시작 / 라운드 {processResult.Round} / 대상 {runtimeUnit.DisplayName} / {effectName} / {triggerLabel} / 지속 {processResult.PreviousRemainingTurns}→{processResult.RemainingTurns} / 제거 {processResult.RemovalReason}", this); // 통합 상태 처리 결과 출력
        } // 개별 결과 표시 종료
    } // 상태 결과 표시 종료
    private void RegisterAllyStatusIntentEvents() // 아군 상태 변경 이벤트 등록
    { // 상태 이벤트 등록 시작
        foreach (BattleUnitRuntime allyUnit in allyUnits) // 아군 런타임 목록 순회
        { // 개별 이벤트 등록 시작
            if (allyUnit != null) // 아군 존재 확인
            { // 유효 아군 처리 시작
                allyUnit.StatusEffectsChanged += HandleAllyStatusEffectsChanged; // 상태 변경 예고 갱신 연결
            } // 유효 아군 처리 종료
        } // 개별 이벤트 등록 종료
    } // 상태 이벤트 등록 종료
    private void UnregisterAllyStatusIntentEvents() // 아군 상태 변경 이벤트 해제
    { // 상태 이벤트 해제 시작
        foreach (BattleUnitRuntime allyUnit in allyUnits) // 아군 런타임 목록 순회
        { // 개별 이벤트 해제 시작
            if (allyUnit != null) // 아군 존재 확인
            { // 유효 아군 처리 시작
                allyUnit.StatusEffectsChanged -= HandleAllyStatusEffectsChanged; // 상태 변경 예고 갱신 해제
            } // 유효 아군 처리 종료
        } // 개별 이벤트 해제 종료
    } // 상태 이벤트 해제 종료
    private void RegisterMentalIntentEvents() // 정신 상태 예고 이벤트 등록
    { // 정신 상태 이벤트 등록 시작
        RegisterMentalIntentEvents(allyUnits); // 아군 정신 상태 이벤트 등록
        RegisterMentalIntentEvents(enemyUnits); // 적 정신 상태 이벤트 등록
    } // 정신 상태 이벤트 등록 종료
    private void RegisterMentalIntentEvents(IReadOnlyList<BattleUnitRuntime> units) // 지정 유닛 정신 상태 이벤트 등록
    { // 지정 이벤트 등록 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 개별 등록 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유효 유닛 처리 시작
                runtimeUnit.MentalChanged += HandleMentalIntentChanged; // 정신 상태 예고 갱신 연결
            } // 유효 유닛 처리 종료
        } // 개별 등록 종료
    } // 지정 이벤트 등록 종료
    private void UnregisterMentalIntentEvents() // 정신 상태 예고 이벤트 해제
    { // 정신 상태 이벤트 해제 시작
        UnregisterMentalIntentEvents(allyUnits); // 아군 정신 상태 이벤트 해제
        UnregisterMentalIntentEvents(enemyUnits); // 적 정신 상태 이벤트 해제
    } // 정신 상태 이벤트 해제 종료
    private void UnregisterMentalIntentEvents(IReadOnlyList<BattleUnitRuntime> units) // 지정 유닛 정신 상태 이벤트 해제
    { // 지정 이벤트 해제 시작
        foreach (BattleUnitRuntime runtimeUnit in units) // 유닛 목록 순회
        { // 개별 해제 시작
            if (runtimeUnit != null) // 유닛 존재 확인
            { // 유효 유닛 처리 시작
                runtimeUnit.MentalChanged -= HandleMentalIntentChanged; // 정신 상태 예고 갱신 해제
            } // 유효 유닛 처리 종료
        } // 개별 해제 종료
    } // 지정 이벤트 해제 종료
    private IEnumerator ExecuteEnemyTurn() // 적 턴 행동 실행
    { // 적 턴 실행 시작
        if (enemyActionRuntime == null) // 적 행동 관리자 확인
        { // 관리자 없음 처리 시작
            enemyTurnCoroutine = null; // 적 턴 코루틴 참조 제거
            yield break; // 적 턴 실행 종료
        } // 관리자 없음 처리 종료
        List<BattleEnemyAction> actionSnapshot = new List<BattleEnemyAction>(enemyActionRuntime.PlannedActions); // 실행할 행동 목록 복사
        foreach (BattleEnemyAction enemyAction in actionSnapshot) // 적 행동 목록 순회
        { // 개별 적 행동 시작
            if (battleTurn == null || battleTurn.CurrentPhase != BattleTurnPhase.EnemyTurn || battleTurn.IsBattleEnded) // 적 턴 지속 여부 확인
            { // 적 턴 중단 처리 시작
                break; // 남은 적 행동 중단
            } // 적 턴 중단 처리 종료
            if (enemyTurnDelay > 0f) // 적 행동 대기 시간 확인
            { // 적 행동 대기 시작
                yield return new WaitForSeconds(enemyTurnDelay); // 설정 시간만큼 대기
            } // 적 행동 대기 종료
            BattleEnemyActionResult actionResult = BattleEnemyActionResult.Empty(enemyAction.ActionType, enemyAction.DamageType); // 적 행동 결과 초기화
            BattleUnitView actorView = FindUnitView(enemyAction.Actor, enemyUnitViews); // 행동 적 화면 조회
            BattleUnitView targetView = FindUnitView(enemyAction.Target, allyUnitViews); // 대상 아군 화면 조회
            List<BattleUnitView> targetViews = new List<BattleUnitView>(); // 적 행동 대상 화면 목록 생성
            if (targetView != null) // 대상 아군 화면 확인
            { // 대상 화면 추가 시작
                targetViews.Add(targetView); // 적 행동 대상 화면 추가
            } // 대상 화면 추가 종료
            System.Action impactAction = () => actionResult = enemyActionRuntime.ExecuteAction(enemyAction); // 충돌 시 적 행동 처리 생성
            if (actionSequenceRunner != null) // 행동 연출 실행기 확인
            { // 적 행동 연출 시작
                yield return actionSequenceRunner.RunEnemyAction(actorView, targetViews, impactAction); // 적 공격 순서 연출 실행
            } // 적 행동 연출 종료
            else // 행동 연출 실행기 없음
            { // 즉시 공격 처리 시작
                impactAction.Invoke(); // 적 공격 즉시 적용
            } // 즉시 공격 처리 종료
            if (actionResult.IsStatusAction) // 상태 적용 행동 결과 확인
            { // 상태 결과 처리 시작
                targetView?.ShowStatusApplyFeedback(enemyAction.StatusEffectType, actionResult.StatusApplyResult); // 상태 적용 결과 화면 표시
                string effectName = BattleStatusEffectInstance.GetDisplayName(enemyAction.StatusEffectType); // 상태 이상 이름 조회
                Debug.Log($"[BattleStatus] 적 / {enemyAction.Actor.DisplayName} / 패턴 {enemyAction.PatternIndex}/{enemyAction.PatternCount} {enemyAction.PatternDisplayName} / 대상 {enemyAction.Target.DisplayName} / {effectName} / 수치 {enemyAction.Amount} / 지속 {enemyAction.StatusDuration} / 결과 {actionResult.StatusApplyResult}", this); // 적 상태 적용 상세 출력
            } // 상태 결과 처리 종료
            else // 공격 행동 결과 처리
            { // 피해 결과 처리 시작
                BattleDamageResult damageResult = actionResult.DamageResult; // 적 피해 결과 조회
                string damageLabel = enemyAction.DamageType == BattleDamageType.Magical ? "마법" : enemyAction.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
                Debug.Log($"[BattleDamage] 적 / {enemyAction.Actor.DisplayName} / 패턴 {enemyAction.PatternIndex}/{enemyAction.PatternCount} {enemyAction.PatternDisplayName} / 대상 {enemyAction.Target.DisplayName} / {damageLabel} / 원본 {damageResult.RawDamage} / 방어 {damageResult.DefenseValue} / 감소 {damageResult.ReducedDamage} / 최종 {damageResult.FinalDamage} / 실제 {damageResult.AppliedDamage}", this); // 적 피해 상세 출력
            } // 피해 결과 처리 종료
        } // 개별 적 행동 종료
        enemyTurnCoroutine = null; // 적 턴 코루틴 참조 제거
        if (battleTurn == null || battleTurn.IsBattleEnded) // 전투 종료 여부 확인
        { // 전투 종료 처리 시작
            enemyActionRuntime.ClearActions(); // 남은 적 행동 제거
            yield break; // 적 턴 실행 종료
        } // 전투 종료 처리 종료
        if (!battleTurn.CompleteEnemyTurn()) // 적 턴 완료 처리 확인
        { // 적 턴 완료 실패 처리 시작
            yield break; // 적 턴 실행 종료
        } // 적 턴 완료 실패 처리 종료
        int preparedActionCount = enemyActionRuntime.PrepareActions(); // 다음 적 행동 준비
        Debug.Log($"[BattleSceneSetup] 플레이어 턴 시작 - 라운드 {battleTurn.CurrentRound}, 드로우 {battleTurn.LastDrawnCardCount}장, 공용 AP {sharedActionPoints.CurrentActionPoints}, 적 예고 {preparedActionCount}개", this); // 플레이어 턴 시작 출력
    } // 적 턴 실행 종료
    private static BattleUnitView FindUnitView(BattleUnitRuntime runtimeUnit, IReadOnlyList<BattleUnitView> unitViews) // 런타임 유닛 화면 조회
    { // 유닛 화면 조회 시작
        foreach (BattleUnitView unitView in unitViews) // 유닛 화면 목록 순회
        { // 유닛 화면 비교 시작
            if (unitView != null && unitView.RuntimeUnit == runtimeUnit) // 런타임 유닛 일치 확인
            { // 일치 화면 처리 시작
                return unitView; // 일치 유닛 화면 반환
            } // 일치 화면 처리 종료
        } // 유닛 화면 비교 종료
        return null; // 일치 유닛 화면 없음 반환
    } // 유닛 화면 조회 종료
    private void OnDestroy() // 전투 씬 제거 처리
    { // 씬 제거 처리 시작
        foreach (BattleUnitRuntime enemyUnit in enemyUnits) // 생성 적 목록 순회
        { // 결과 기록 이벤트 해제 시작
            if (enemyUnit != null) // 적 런타임 존재 확인
            { // 적 이벤트 해제 시작
                enemyUnit.Died -= HandleEnemyResultDied; // 처치 적 결과 기록 이벤트 해제
            } // 적 이벤트 해제 종료
        } // 결과 기록 이벤트 해제 종료
        if (handView != null) // 손패 화면 존재 확인
        { // 손패 이벤트 해제 시작
            handView.EscapeClicked -= HandleEscapeClicked; // 손패 도주 요청 연결 해제
        } // 손패 이벤트 해제 종료
        if (enemyTurnCoroutine != null) // 실행 중인 적 턴 확인
        { // 적 턴 중단 시작
            StopCoroutine(enemyTurnCoroutine); // 적 턴 코루틴 중단
            enemyTurnCoroutine = null; // 적 턴 코루틴 참조 제거
        } // 적 턴 중단 종료
        if (statusEffectController != null) // 상태 이상 관리자 존재 확인
        { // 상태 관리자 해제 시작
            statusEffectController.StatusEffectsProcessed -= HandleStatusEffectsProcessed; // 상태 처리 결과 화면 연결 해제
            statusEffectController.Dispose(); // 상태 이상 관리자 연결 해제
        } // 상태 관리자 해제 종료
        statusEffectController = null; // 상태 이상 관리자 참조 제거
        statusEffectProcessor = null; // 공통 상태 처리기 참조 제거
        mentalController?.Dispose(); // 정신력 관리자 연결 해제
        mentalController = null; // 정신력 관리자 참조 제거
        UnregisterAllyStatusIntentEvents(); // 아군 상태 변경 예고 갱신 해제
        UnregisterMentalIntentEvents(); // 정신 상태 변경 예고 갱신 해제
        if (actionSequenceRunner != null) // 행동 연출 실행기 확인
        { // 행동 연출 연결 해제 시작
            actionSequenceRunner.BusyStateChanged -= handView.SetInteractionLocked; // 손패 입력 잠금 연결 해제
            actionSequenceRunner.CancelCurrentAction(); // 실행 중인 행동 연출 취소
            actionSequenceRunner = null; // 행동 연출 실행기 참조 제거
        } // 행동 연출 연결 해제 종료
        cardActionController?.Dispose(); // 카드 행동 이벤트 연결 해제
        cardActionController = null; // 카드 행동 관리자 참조 제거
        if (enemyActionRuntime != null) // 적 행동 관리자 확인
        { // 적 행동 관리자 해제 시작
            enemyActionRuntime.StateChanged -= HandleEnemyActionStateChanged; // 적 행동 변경 이벤트 해제
            enemyActionRuntime.Dispose(); // 적 행동 사망 이벤트 해제
            enemyActionRuntime = null; // 적 행동 관리자 참조 제거
        } // 적 행동 관리자 해제 종료
        if (battleTurn != null) // 전투 턴 관리자 확인
        { // 턴 관리자 해제 시작
            battleTurn.StateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
            battleTurn.Dispose(); // 유닛 사망 이벤트 연결 해제
            battleTurn = null; // 턴 관리자 참조 제거
        } // 턴 관리자 해제 종료
    } // 씬 제거 처리 종료
} // 클래스 종료
