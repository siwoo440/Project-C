using System; // 상태 변경 이벤트 사용
using System.Collections.Generic; // 사전 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용

public sealed class BattleResultManager : MonoBehaviour // Scene 간 전투 결과와 아군 상태 보관
{
    private readonly Dictionary<string, int> savedAllyHealth =
        new Dictionary<string, int>(); // 아군별 저장 체력 목록

    private readonly Dictionary<string, int> savedAllyMental =
        new Dictionary<string, int>(); // 아군별 저장 정신력 목록

    private readonly Dictionary<string, int> allyDeathCounts =
        new Dictionary<string, int>(); // 탐사 런 아군별 사망 횟수

    private BattleResultData pendingResult; // 탐사 전달 대기 결과

    public static BattleResultManager Instance
    {
        get;
        private set;
    } // 전역 결과 관리자 조회

    public bool HasPendingResult =>
        pendingResult != null; // 전달 대기 결과 존재 여부 조회

    public PartyData ActiveParty
    {
        get;
        private set;
    } // 현재 탐사 출전 파티 조회

    public event Action PartyStateChanged; // 탐사 파티 HP·정신력·사망 상태 변경 알림

    public static BattleResultManager EnsureInstance() // 결과 관리자 준비
    {
        if (Instance != null)
        {
            return Instance; // 기존 관리자 반환
        }

        BattleResultManager existingManager =
            UnityEngine.Object.FindFirstObjectByType<BattleResultManager>(); // Scene 기존 관리자 조회

        if (existingManager != null)
        {
            Instance = existingManager; // 기존 관리자 전역 등록
            return existingManager; // Scene 관리자 반환
        }

        GameObject managerObject =
            new GameObject(
                "BattleResultManager",
                typeof(BattleResultManager)); // 영구 결과 관리자 오브젝트 생성

        Instance = managerObject.GetComponent<BattleResultManager>(); // 생성 관리자 전역 등록
        return Instance; // 생성 관리자 반환
    }

    private void Awake() // 결과 관리자 초기화
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject); // 중복 관리자 오브젝트 제거
            return;
        }

        Instance = this; // 전역 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 유지 설정
    }

    public bool RegisterParty(PartyData partyData) // 탐사 출전 파티 등록과 기본 상태 준비
    {
        if (partyData == null ||
            !partyData.IsValidParty())
        {
            return false;
        }

        ActiveParty =
            partyData; // 현재 탐사 파티 저장

        foreach (CharacterData member in partyData.Members)
        {
            InitializeMemberState(
                member); // 저장값 없는 파티원 기본 상태 준비
        }

        PartyStateChanged?.Invoke(); // 파티 UI 갱신 알림
        return true;
    }

    public bool StoreResult(BattleResultData battleResultData) // 전투 결과와 아군 상태 저장
    {
        if (battleResultData == null ||
            battleResultData.Result == BattleResult.None ||
            pendingResult != null)
        {
            return false;
        }

        pendingResult =
            battleResultData; // 탐사 전달 결과 저장

        foreach (BattleUnitResultData allyState in battleResultData.AllyStates)
        {
            if (allyState == null ||
                string.IsNullOrWhiteSpace(allyState.UnitId))
            {
                continue;
            }

            int previousHealth =
                GetPreviousHealth(
                    allyState.UnitId,
                    allyState.MaximumHealth); // 전투 전 저장 체력 조회

            savedAllyHealth[allyState.UnitId] =
                Mathf.Clamp(
                    allyState.CurrentHealth,
                    0,
                    Mathf.Max(1, allyState.MaximumHealth)); // 전투 종료 체력 저장

            savedAllyMental[allyState.UnitId] =
                Mathf.Clamp(
                    allyState.CurrentMental,
                    BattleMentalRuntime.MinimumMental,
                    BattleMentalRuntime.MaximumMental); // 전투 종료 정신력 저장

            RegisterDeathTransition(
                allyState.UnitId,
                previousHealth,
                allyState.CurrentHealth); // 생존에서 사망으로 변한 경우 사망 횟수 기록
        }

        PartyStateChanged?.Invoke(); // 탐사 파티 상태 갱신 알림
        return true;
    }

    public bool TryConsumeResult(out BattleResultData battleResultData) // 대기 전투 결과 한 번 소비
    {
        battleResultData =
            pendingResult; // 대기 결과 반환값 저장

        if (battleResultData == null)
        {
            return false;
        }

        pendingResult = null; // 소비한 대기 결과 제거
        return true;
    }

    public void DiscardPendingResult() // 이전 대기 결과 제거
    {
        pendingResult = null; // 대기 결과 초기화
    }

    public bool ApplySavedAllyState(BattleUnitRuntime allyUnit) // 저장된 아군 상태 적용
    {
        if (allyUnit == null ||
            allyUnit.Team != BattleTeam.Ally)
        {
            return false;
        }

        bool healthApplied =
            savedAllyHealth.TryGetValue(
                allyUnit.UnitId,
                out int currentHealth) &&
            allyUnit.ApplyPersistentHealth(
                currentHealth); // 저장 체력 적용 여부 계산

        bool mentalApplied =
            savedAllyMental.TryGetValue(
                allyUnit.UnitId,
                out int currentMental) &&
            allyUnit.ApplyPersistentMental(
                currentMental); // 저장 정신력 적용 여부 계산

        return healthApplied ||
               mentalApplied; // 저장 상태 적용 결과 반환
    }

    public int ApplyExplorationHazardToActiveParty(
        int healthDamage,
        int mentalDamage) // 탐사 환경 피해를 현재 파티 영구 상태에 즉시 반영
    {
        if (ActiveParty == null)
        {
            return -1; // 등록 파티 없음 반환
        }

        foreach (CharacterData member in ActiveParty.Members) // 출전 파티원 순회
        { // 환경 피해 적용 시작
            ApplyExplorationChange(member, Mathf.Max(0, healthDamage), -Mathf.Max(0, mentalDamage)); // 생존 파티원 환경 피해 반영
        } // 환경 피해 적용 종료

        PartyStateChanged?.Invoke(); // 탐사 파티 HUD 즉시 갱신
        return GetLivingAllyCount(); // 환경 피해 후 생존 인원 반환
    }

    private void ApplyExplorationChange(CharacterData member, int healthDamage, int mentalChange) // 탐사 중 생존자 체력과 정신력 변경
    { // 생존자 상태 변경 시작
        if (!TryGetSavedAllyState(member, out int health, out int mental, out _) || health <= 0) // 생존 여부 확인
        { // 사망 파티원 제외 시작
            return; // 사망 중 추가 피해와 회복 방지
        } // 사망 파티원 제외 종료
        string id = member.CharacterId; // 파티원 식별자 조회
        int nextHealth = Mathf.Max(0, health - healthDamage); // 체력 영점까지 피해 허용
        savedAllyHealth[id] = nextHealth; // 변경 체력 저장
        savedAllyMental[id] = Mathf.Clamp(mental + mentalChange, BattleMentalRuntime.MinimumMental, BattleMentalRuntime.MaximumMental); // 허용 범위의 정신력 저장
        RegisterDeathTransition(id, health, nextHealth); // 첫 사망과 누적 사망 횟수 반영
    } // 생존자 상태 변경 종료

    public bool IsLivingPartyMember(CharacterData member) // 행동할 수 있는 출전 생존자 확인
    { // 출전 생존자 확인 시작
        return ActiveParty != null && ActiveParty.ContainsCharacter(member) && TryGetSavedAllyState(member, out int health, out _, out _) && health > 0; // 파티 소속과 현재 체력 확인
    } // 출전 생존자 확인 종료

    internal void ApplyEventChange(CharacterData member, int healthDamage, int mentalChange) // 선택한 출전자의 탐사 이벤트 상태 반영
    { // 이벤트 상태 변경 시작
        ApplyExplorationChange(member, healthDamage, mentalChange); // 행동자의 체력과 정신력 갱신
        PartyStateChanged?.Invoke(); // 변경된 파티 상태 표시
    } // 이벤트 상태 변경 종료

    public bool TryGetSavedAllyState(
        CharacterData characterData,
        out int currentHealth,
        out int currentMental,
        out int deathCount) // 탐사 HUD용 현재 캐릭터 상태 조회
    {
        currentHealth = 0; // 기본 체력 초기화
        currentMental = 0; // 기본 정신력 초기화
        deathCount = 0; // 기본 사망 횟수 초기화

        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        InitializeMemberState(
            characterData); // 저장 상태 없으면 원본 수치로 준비

        currentHealth =
            savedAllyHealth[characterData.CharacterId]; // 현재 체력 조회

        currentMental =
            savedAllyMental[characterData.CharacterId]; // 현재 정신력 조회

        allyDeathCounts.TryGetValue(
            characterData.CharacterId,
            out deathCount); // 현재 사망 횟수 조회

        return true;
    }

    public bool TryGetSavedAllyHealth(
        string unitId,
        out int currentHealth) // 저장 아군 체력 조회
    {
        return savedAllyHealth.TryGetValue(
            unitId,
            out currentHealth); // 저장 체력 조회 결과 반환
    }

    public bool TryGetSavedAllyMental(
        string unitId,
        out int currentMental) // 저장 아군 정신력 조회
    {
        return savedAllyMental.TryGetValue(
            unitId,
            out currentMental); // 저장 정신력 조회 결과 반환
    }

    public int GetDeathCount(string unitId) // 현재 탐사 런 사망 횟수 조회
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return 0;
        }

        return allyDeathCounts.TryGetValue(
            unitId,
            out int deathCount)
                ? deathCount
                : 0; // 저장 사망 횟수 또는 0 반환
    }

    public int GetLivingAllyCount() // 현재 출전 파티 생존 인원 조회
    {
        if (ActiveParty == null)
        {
            return 0;
        }

        int livingCount = 0; // 생존 인원 초기화

        foreach (CharacterData member in ActiveParty.Members)
        {
            if (member == null)
            {
                continue;
            }

            InitializeMemberState(
                member); // 현재 상태 보장

            if (savedAllyHealth[member.CharacterId] > 0)
            {
                livingCount += 1; // 생존 파티원 수 증가
            }
        }

        return livingCount; // 현재 생존 파티원 수 반환
    }

    public bool IsActivePartyWiped() // 현재 파티 전멸 여부 조회
    {
        return ActiveParty != null &&
               ActiveParty.MemberCount > 0 &&
               GetLivingAllyCount() <= 0; // 출전 파티 전원 사망 판정
    }

    public bool RestoreRecoveredAlly(
        CharacterData characterData,
        int healthPercent) // 회복 설비 완료 캐릭터의 사망 상태 해제
    {
        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        InitializeMemberState(
            characterData); // 회복 대상 저장 상태 보장

        string unitId =
            characterData.CharacterId; // 회복 대상 ID 저장

        if (savedAllyHealth[unitId] > 0)
        {
            return false; // 이미 생존 상태인 캐릭터 복구 차단
        }

        int safeHealthPercent =
            Mathf.Clamp(
                healthPercent,
                1,
                100); // 회복 완료 체력 비율 범위 보정

        int recoveredHealth =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    characterData.MaxHealth *
                    safeHealthPercent /
                    100f)); // 회복 완료 HP 계산

        savedAllyHealth[unitId] =
            recoveredHealth; // 회복 완료 체력 저장

        PartyStateChanged?.Invoke(); // 탐사 파티 상태 UI 갱신

        Debug.Log(
            $"[BattleResultManager][Day53] 회복 완료 상태 반영 - " +
            $"{characterData.DisplayName} / " +
            $"HP {recoveredHealth}/{characterData.MaxHealth} / " +
            $"정신 {savedAllyMental[unitId]}"); // 회복 완료 결과 로그

        return true;
    }

    public bool TryApplyRestRecovery(
        int hazardLevel,
        out int recoveredMemberCount) // 57일차 위험도별 휴식 회복을 영속 파티 상태에 적용
    {
        recoveredMemberCount = 0; // 회복 적용 생존 인원 초기화
        int mentalRecovery = RestRoomRecoveryService.CampMentalRecovery; // 같은 휴식에 적용할 정수기 포함 정신력 회복량
        int bonus = RestRoomRecoveryService.CampRecoveryBonus; // 이번 휴식의 식량팩 회복 비율 조회
        int flatBonus = RestRoomRecoveryService.CampFlatHealthBonus; // 이번 휴식의 접이식 침대 추가 회복량 조회

        if (ActiveParty == null) // 등록 파티 존재 확인
        {
            return false; // 회복 실패 반환
        }

        foreach (CharacterData member in ActiveParty.Members) // 현재 출전 파티원 순회
        {
            if (member == null ||
                string.IsNullOrWhiteSpace(member.CharacterId)) // 잘못된 파티원 확인
            {
                continue; // 잘못된 파티원 제외
            }

            InitializeMemberState(member); // 저장 HP·정신력 상태 보장

            string unitId = member.CharacterId; // 현재 파티원 ID 저장
            int currentHealth = savedAllyHealth[unitId]; // 현재 저장 HP 조회

            if (currentHealth <= 0) // 사망 파티원 확인
            {
                continue; // 휴식 부활·정신력 회복 제외
            }

            savedAllyHealth[unitId] =
                RestRoomRecoveryService.CalculateRecoveredHealth(
                    currentHealth,
                    member.MaxHealth,
                    hazardLevel, bonus, flatBonus); // 위험도와 식량팩 보정 뒤 침대 회복량 적용

            savedAllyMental[unitId] =
                RestRoomRecoveryService.CalculateRecoveredMental(
                    savedAllyMental[unitId],
                    false, mentalRecovery); // 생존 파티원의 기본 회복과 정수기 두 점 적용

            recoveredMemberCount += 1; // 회복 적용 생존 인원 증가
        }

        if (recoveredMemberCount < 1) // 회복 대상 생존자 존재 확인
        {
            return false; // 회복 실패 반환
        }

        PartyStateChanged?.Invoke(); // 탐사 파티 HUD 갱신 알림
        return true; // 휴식 회복 성공 반환
    }

    public void RestoreSavedAllyState(CharacterData character, int health, int mental, int deaths) // 저장 캐릭터 상태 복원
    { // 캐릭터 상태 복원 시작
        savedAllyHealth[character.CharacterId] = health; // 저장 체력 적용
        savedAllyMental[character.CharacterId] = mental; // 저장 정신력 적용
        allyDeathCounts[character.CharacterId] = deaths; // 저장 사망 횟수 적용
        PartyStateChanged?.Invoke(); // 파티 상태 변경 알림
    } // 캐릭터 상태 복원 종료

    public void ResetSavedPartyState() // 저장 아군 상태 전체 초기화
    {
        savedAllyHealth.Clear(); // 저장 아군 체력 비우기
        savedAllyMental.Clear(); // 저장 아군 정신력 비우기
        allyDeathCounts.Clear(); // 사망 횟수 비우기
        ActiveParty = null; // 현재 출전 파티 참조 초기화
        PartyStateChanged?.Invoke(); // 파티 상태 초기화 알림
    }

    private void InitializeMemberState(CharacterData characterData) // 저장값 없는 파티원 기본 상태 준비
    {
        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return;
        }

        if (!savedAllyHealth.ContainsKey(characterData.CharacterId))
        {
            savedAllyHealth[characterData.CharacterId] =
                Mathf.Max(
                    1,
                    characterData.MaxHealth); // 기본 최대 체력으로 시작
        }

        if (!savedAllyMental.ContainsKey(characterData.CharacterId))
        {
            savedAllyMental[characterData.CharacterId] =
                Mathf.Clamp(
                    characterData.InitialMental,
                    BattleMentalRuntime.MinimumMental,
                    BattleMentalRuntime.MaximumMental); // 캐릭터 초기 정신력으로 시작
        }

        if (!allyDeathCounts.ContainsKey(characterData.CharacterId))
        {
            allyDeathCounts[characterData.CharacterId] =
                0; // 탐사 런 사망 횟수 초기화
        }
    }

    private int GetPreviousHealth(
        string unitId,
        int fallbackMaximumHealth) // 상태 저장 전 기존 체력 조회
    {
        if (savedAllyHealth.TryGetValue(
                unitId,
                out int currentHealth))
        {
            return currentHealth; // 기존 저장 체력 반환
        }

        return Mathf.Max(
            1,
            fallbackMaximumHealth); // 저장값 없으면 최대 체력 기준 반환
    }

    private void RegisterDeathTransition(
        string unitId,
        int previousHealth,
        int currentHealth) // 생존에서 사망으로 바뀐 순간 기록
    {
        if (string.IsNullOrWhiteSpace(unitId) ||
            previousHealth <= 0 ||
            currentHealth > 0)
        {
            return;
        }

        int previousDeathCount =
            GetDeathCount(
                unitId); // 기존 사망 횟수 조회

        allyDeathCounts[unitId] =
            previousDeathCount + 1; // 탐사 런 아군별 사망 횟수 증가
    }

    private void OnDestroy() // 결과 관리자 제거 처리
    {
        if (Instance == this)
        {
            Instance = null; // 전역 관리자 참조 제거
        }
    }
}
