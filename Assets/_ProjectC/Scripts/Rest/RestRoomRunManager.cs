using UnityEngine; // 영구 관리자와 로그 사용

public sealed class RestRoomRunManager : MonoBehaviour // 57일차 휴식 방 회차 관리자
{
    private static RestRoomRunManager instance; // 현재 휴식 관리자
    private DeckData sourceDeck; // 현재 탐사 원본 덱
    private bool isUsed; // 현재 휴식 방 사용 여부

    public static RestRoomRunManager Instance => instance; // 현재 관리자 조회
    public bool IsUsed => isUsed; // 현재 휴식 방 사용 여부 조회
    public DeckData SourceDeck => sourceDeck; // 현재 원본 덱 조회

    public static RestRoomRunManager EnsureInstance() // 휴식 관리자 준비
    {
        if (instance != null) // 기존 관리자 확인
        {
            return instance; // 기존 관리자 반환
        }

        RestRoomRunManager existingManager = FindFirstObjectByType<RestRoomRunManager>(); // Scene 기존 관리자 탐색
        if (existingManager != null) // 기존 Scene 관리자 확인
        {
            instance = existingManager; // 기존 관리자 저장
            return instance; // 기존 관리자 반환
        }

        GameObject managerObject = new GameObject("RestRoomRunManager"); // 휴식 관리자 오브젝트 생성
        instance = managerObject.AddComponent<RestRoomRunManager>(); // 휴식 관리자 컴포넌트 추가
        return instance; // 신규 관리자 반환
    }

    private void Awake() // 휴식 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    }

    public bool Prepare(DeckData deckData) // 현재 탐사 휴식 기능 준비
    {
        if (deckData == null) // 원본 덱 누락 확인
        {
            return false; // 준비 실패 반환
        }

        sourceDeck = deckData; // 현재 원본 덱 저장
        RunDeckManager.EnsureInstance().GetActiveCards(sourceDeck); // 회차 덱 초기화 보장
        return true; // 준비 성공 반환
    }

    public bool TryUseRest(
        int cardIndex,
        int hazardLevel,
        out string message) // 위험도별 회복과 카드 강화 실행
    {
        message = string.Empty; // 결과 메시지 초기화

        if (isUsed) // 현재 휴식 사용 완료 확인
        {
            message = "이 휴식 방은 이미 사용했습니다."; // 재사용 차단 메시지
            return false; // 휴식 실패 반환
        }

        if (sourceDeck == null) // 원본 덱 준비 여부 확인
        {
            message = "휴식용 덱 정보가 준비되지 않았습니다."; // 덱 누락 메시지
            return false; // 휴식 실패 반환
        }

        BattleResultManager resultManager = BattleResultManager.EnsureInstance(); // 탐사 파티 상태 관리자 준비
        if (resultManager.ActiveParty == null) // 출전 파티 등록 여부 확인
        {
            message = "현재 탐사 출전 파티가 등록되지 않았습니다."; // 파티 누락 메시지
            return false; // 휴식 실패 반환
        }

        RunDeckManager runDeckManager = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        runDeckManager.GetActiveCards(sourceDeck); // 현재 회차 덱 상태 보장

        if (!runDeckManager.CanUpgradeAt(cardIndex)) // 선택 카드 강화 가능 여부 확인
        {
            message = "강화할 수 있는 카드를 선택하세요."; // 카드 선택 오류 메시지
            return false; // 휴식 실패 반환
        }

        if (!resultManager.TryApplyRestRecovery(
                hazardLevel,
                out int recoveredMemberCount)) // 정식 파티 상태 API로 휴식 회복 적용
        {
            message = "회복 가능한 생존 파티원이 없습니다."; // 회복 대상 없음 메시지
            return false; // 휴식 실패 반환
        }

        if (!runDeckManager.TryUpgradeAt(cardIndex)) // 선택 카드 강화 적용
        {
            message = "카드 강화에 실패했습니다."; // 강화 실패 메시지
            return false; // 휴식 실패 반환
        }

        isUsed = true; // 현재 휴식 방 사용 완료 기록
        int recoveryPercent = RestRoomRecoveryService.GetHealthRecoveryPercent(hazardLevel); // 위험도별 회복률 조회

        message =
            $"휴식 완료: 생존 {recoveredMemberCount}명 HP +{recoveryPercent}% / " +
            $"정신력 +{RestRoomRecoveryService.MentalRecoveryAmount} / 카드 1장 강화"; // 휴식 결과 메시지

        Debug.Log(
            $"[RestRoom][Day57] {message} / 위험도 Lv{Mathf.Clamp(hazardLevel, 0, 3)}"); // 휴식 결과 로그

        return true; // 휴식 성공 반환
    }

    public bool TryUseRest(
        int cardIndex,
        bool isHighRisk,
        out string message) // 기존 bool 호출 호환 휴식 실행
    {
        return TryUseRest(
            cardIndex,
            isHighRisk ? 2 : 0,
            out message); // 기존 고위험을 Lv2로 호환
    }

    public void ResetPrototypeUsage() // 새 휴식 방·테스트용 사용 상태 초기화
    {
        isUsed = false; // 휴식 사용 여부 초기화
        Debug.Log("[RestRoom][Day57] 휴식 사용 상태를 초기화했습니다."); // 초기화 로그
    }

    private void OnDestroy() // 휴식 관리자 제거 처리
    {
        if (instance == this) // 현재 관리자 여부 확인
        {
            instance = null; // 정적 관리자 참조 해제
        }
    }
}
