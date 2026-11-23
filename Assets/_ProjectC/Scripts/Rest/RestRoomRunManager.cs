using System.Collections.Generic; // 영속 상태 사전 사용
using System.Reflection; // Prototype 임시 영속 상태 연결 사용
using UnityEngine; // 영구 관리자와 로그 사용

public sealed class RestRoomRunManager : MonoBehaviour // Prototype 휴식 방 회차 관리자
{
    private static readonly FieldInfo SavedHealthField = typeof(BattleResultManager).GetField(
        "savedAllyHealth",
        BindingFlags.Instance | BindingFlags.NonPublic); // 51일차 저장 체력 사전 연결

    private static readonly FieldInfo SavedMentalField = typeof(BattleResultManager).GetField(
        "savedAllyMental",
        BindingFlags.Instance | BindingFlags.NonPublic); // 51일차 저장 정신력 사전 연결

    private static RestRoomRunManager instance; // 현재 휴식 관리자
    private DeckData sourceDeck; // 현재 탐사 원본 덱
    private bool isUsed; // Prototype 휴식 사용 여부

    public static RestRoomRunManager Instance => instance; // 현재 관리자 조회
    public bool IsUsed => isUsed; // 휴식 사용 여부 조회
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

    public bool TryUseRest(int cardIndex, bool isHighRisk, out string message) // 회복과 카드 강화 동시 실행
    {
        message = string.Empty; // 결과 메시지 초기화

        if (isUsed) // 이미 사용한 휴식 확인
        {
            message = "이 Prototype 휴식은 이미 사용했습니다."; // 재사용 차단 메시지
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
            message = "강화할 수 있는 미강화 카드를 선택하세요."; // 카드 선택 오류 메시지
            return false; // 휴식 실패 반환
        }

        if (!TryGetPersistentStateDictionaries(
                resultManager,
                out Dictionary<string, int> savedHealth,
                out Dictionary<string, int> savedMental)) // 임시 영속 상태 연결 확인
        {
            message = "기존 탐사 HP·정신력 저장소를 찾지 못했습니다."; // 임시 연결 실패 메시지
            return false; // 휴식 실패 반환
        }

        int recoveredMemberCount = ApplyPartyRecovery(
            resultManager.ActiveParty,
            savedHealth,
            savedMental,
            isHighRisk); // 생존 파티 회복 적용

        if (recoveredMemberCount < 1) // 생존 파티원 존재 확인
        {
            message = "회복 가능한 생존 파티원이 없습니다."; // 회복 대상 없음 메시지
            return false; // 휴식 실패 반환
        }

        if (!runDeckManager.TryUpgradeAt(cardIndex)) // 선택 카드 강화 적용
        {
            message = "카드 강화에 실패했습니다."; // 강화 실패 메시지
            return false; // 휴식 실패 반환
        }

        resultManager.RegisterParty(resultManager.ActiveParty); // 기존 파티 상태 변경 이벤트 재발행
        isUsed = true; // Prototype 휴식 사용 완료 기록

        int healthPercent = isHighRisk
            ? RestRoomRecoveryService.HighRiskHealthRecoveryPercent
            : RestRoomRecoveryService.NormalHealthRecoveryPercent; // 결과 표시용 회복 비율 선택

        message =
            $"휴식 완료: 생존 {recoveredMemberCount}명 HP +{healthPercent}% / 정신력 +{RestRoomRecoveryService.MentalRecoveryAmount} / 카드 1장 강화"; // 휴식 결과 메시지

        Debug.Log(
            $"[RestRoom][Day55] {message} / 고위험 {isHighRisk}"); // 55일차 휴식 결과 로그

        return true; // 휴식 성공 반환
    }

    public void ResetPrototypeUsage() // 테스트용 휴식 사용 상태 초기화
    {
        isUsed = false; // 휴식 사용 여부 초기화
        Debug.Log("[RestRoom][Day55] Prototype 휴식 사용 상태를 초기화했습니다."); // 테스트 초기화 로그
    }

    private static int ApplyPartyRecovery(
        PartyData partyData,
        Dictionary<string, int> savedHealth,
        Dictionary<string, int> savedMental,
        bool isHighRisk) // 현재 파티 영속 상태 회복 적용
    {
        int recoveredMemberCount = 0; // 회복 적용 생존 인원 수

        foreach (CharacterData member in partyData.Members) // 출전 파티원 순회
        {
            if (member == null || string.IsNullOrWhiteSpace(member.CharacterId)) // 잘못된 파티원 확인
            {
                continue; // 잘못된 파티원 제외
            }

            string unitId = member.CharacterId; // 파티원 ID 저장

            if (!savedHealth.TryGetValue(unitId, out int currentHealth)) // 저장 체력 조회
            {
                currentHealth = Mathf.Max(1, member.MaxHealth); // 미저장 체력 기본값 적용
                savedHealth[unitId] = currentHealth; // 기본 체력 저장
            }

            if (!savedMental.TryGetValue(unitId, out int currentMental)) // 저장 정신력 조회
            {
                currentMental = Mathf.Clamp(
                    member.InitialMental,
                    BattleMentalRuntime.MinimumMental,
                    BattleMentalRuntime.MaximumMental); // 미저장 정신력 기본값 적용

                savedMental[unitId] = currentMental; // 기본 정신력 저장
            }

            if (currentHealth <= 0) // 사망 파티원 확인
            {
                continue; // 휴식 부활과 정신력 회복 제외
            }

            savedHealth[unitId] = RestRoomRecoveryService.CalculateRecoveredHealth(
                currentHealth,
                member.MaxHealth,
                isHighRisk); // 위험도별 최대 HP 기준 체력 회복

            savedMental[unitId] = RestRoomRecoveryService.CalculateRecoveredMental(
                currentMental,
                false); // 생존 파티원 정신력 +15

            recoveredMemberCount += 1; // 회복 적용 인원 증가
        }

        return recoveredMemberCount; // 회복 적용 인원 반환
    }

    private static bool TryGetPersistentStateDictionaries(
        BattleResultManager resultManager,
        out Dictionary<string, int> savedHealth,
        out Dictionary<string, int> savedMental) // 기존 영속 상태 사전 조회
    {
        savedHealth = null; // 체력 사전 기본값
        savedMental = null; // 정신력 사전 기본값

        if (resultManager == null ||
            SavedHealthField == null ||
            SavedMentalField == null) // 임시 연결 대상 유효성 확인
        {
            return false; // 영속 상태 조회 실패 반환
        }

        savedHealth = SavedHealthField.GetValue(resultManager) as Dictionary<string, int>; // 저장 체력 사전 조회
        savedMental = SavedMentalField.GetValue(resultManager) as Dictionary<string, int>; // 저장 정신력 사전 조회

        return savedHealth != null && savedMental != null; // 두 저장소 연결 결과 반환
    }

    private void OnDestroy() // 휴식 관리자 제거 처리
    {
        if (instance == this) // 현재 관리자 여부 확인
        {
            instance = null; // 정적 관리자 참조 해제
        }
    }
}
