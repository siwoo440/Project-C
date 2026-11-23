using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 유니티 이벤트 시스템 사용
using UnityEngine.InputSystem.UI; // 유니티 입력 시스템 UI 사용
public sealed class BattleSceneSetup : MonoBehaviour // 전투 씬 초기 구성
{ // 클래스 시작
    [Header("전투 데이터")] // 전투 데이터 구역
    [SerializeField] private BattleLoadoutData battleLoadout; // 출전 파티와 덱 데이터
    [SerializeField] private List<EnemyData> enemies = new List<EnemyData>(); // 출전 적 목록
    [Header("유닛 생성")] // 유닛 생성 구역
    [SerializeField] private BattleUnitView unitViewPrefab; // 공용 유닛 프리팹
    [SerializeField] private Transform allyUnitRoot; // 아군 유닛 부모
    [SerializeField] private Transform enemyUnitRoot; // 적 유닛 부모
    [Header("카드 시스템")] // 카드 시스템 구역
    [SerializeField] private BattleHandView handView; // 전투 손패 화면
    [Min(1)] // 최대 손패 최소값
    [SerializeField] private int maximumHandSize = 5; // 최대 손패 수
    [Min(0)] // 시작 손패 최소값
    [SerializeField] private int initialHandSize = 3; // 시작 손패 수
    [SerializeField] private bool useFixedShuffleSeed; // 고정 셔플 시드 사용 여부
    [SerializeField] private int fixedShuffleSeed = 12345; // 테스트용 고정 셔플 시드
    [Header("테스트")] // 테스트 구역
    [Min(1)] // 테스트 피해 최소값
    [SerializeField] private int testDamage = 10; // 테스트 피해량
    private readonly List<BattleUnitRuntime> allyUnits = new List<BattleUnitRuntime>(); // 생성된 아군 목록
    private readonly List<BattleUnitRuntime> enemyUnits = new List<BattleUnitRuntime>(); // 생성된 적 목록
    private readonly List<BattleUnitView> allyUnitViews = new List<BattleUnitView>(); // 생성된 아군 화면 목록
    private readonly List<BattleUnitView> enemyUnitViews = new List<BattleUnitView>(); // 생성된 적 화면 목록
    private BattleDeckRuntime battleDeck; // 생성된 런타임 덱
    private BattleCardActionController cardActionController; // 카드 행동 관리자
    public IReadOnlyList<BattleUnitRuntime> AllyUnits => allyUnits; // 아군 목록 조회
    public IReadOnlyList<BattleUnitRuntime> EnemyUnits => enemyUnits; // 적 목록 조회
    public BattleDeckRuntime BattleDeck => battleDeck; // 런타임 덱 조회
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
        CreateAllyUnits(); // 아군 유닛 생성
        CreateEnemyUnits(); // 적 유닛 생성
        int? shuffleSeed = useFixedShuffleSeed ? fixedShuffleSeed : (int?)null; // 적용할 셔플 시드 결정
        battleDeck = BattleDeckRuntime.Create(battleLoadout.Deck, allyUnits, maximumHandSize, shuffleSeed); // 전투용 카드 더미 생성
        if (!handView.Bind(battleDeck)) // 손패 화면 연결 확인
        { // 손패 화면 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 전투 손패 화면 연결에 실패했습니다.", this); // 손패 화면 오류 출력
            return; // 초기화 중단
        } // 손패 화면 오류 처리 종료
        cardActionController = new BattleCardActionController(battleDeck, handView, allyUnitViews, enemyUnitViews); // 카드 행동 관리자 생성
        int drawnCardCount = battleDeck.DrawCards(initialHandSize); // 시작 손패 드로우
        IsInitialized = true; // 초기화 완료 저장
        Debug.Log($"[BattleSceneSetup] 전투 초기화 완료 - 아군 {allyUnits.Count}명, 적 {enemyUnits.Count}명, 전체 카드 {battleDeck.CardCount}장, 시작 손패 {drawnCardCount}장", this); // 생성 완료 출력
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
        if (initialHandSize < 0 || initialHandSize > maximumHandSize) // 시작 손패 범위 확인
        { // 시작 손패 오류 처리 시작
            Debug.LogError("[BattleSceneSetup] 시작 손패 수는 0 이상이며 최대 손패 수 이하여야 합니다.", this); // 시작 손패 오류 출력
            return false; // 검사 실패 반환
        } // 시작 손패 오류 처리 종료
        if (enemies.Count < 1) // 적 목록 비어 있음 확인
        { // 빈 적 목록 처리 시작
            Debug.LogError("[BattleSceneSetup] 출전할 적이 없습니다.", this); // 적 누락 출력
            return false; // 검사 실패 반환
        } // 빈 적 목록 처리 종료
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
            BattleUnitRuntime runtimeUnit = BattleUnitRuntime.CreateEnemy(enemyData); // 적 런타임 생성
            BattleUnitView unitView = Instantiate(unitViewPrefab, enemyUnitRoot); // 적 화면 오브젝트 생성
            unitView.name = $"Enemy_{runtimeUnit.UnitId}"; // 적 오브젝트 이름 적용
            unitView.Bind(runtimeUnit); // 적 화면 연결
            enemyUnits.Add(runtimeUnit); // 적 목록 등록
            enemyUnitViews.Add(unitView); // 적 화면 목록 등록
        } // 적 생성 종료
    } // 적 유닛 생성 종료
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
        allyUnits[0].TakeDamage(testDamage); // 첫 번째 아군 피해 적용
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
        enemyUnits[0].TakeDamage(testDamage); // 첫 번째 적 피해 적용
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
    private void OnDestroy() // 전투 씬 제거 처리
    { // 씬 제거 처리 시작
        cardActionController?.Dispose(); // 카드 행동 이벤트 연결 해제
        cardActionController = null; // 카드 행동 관리자 참조 제거
    } // 씬 제거 처리 종료
} // 클래스 종료
