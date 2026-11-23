using System; // 상태 변경 이벤트 사용
using System.Collections.Generic; // 회복 상태 사전과 임시 키 목록 사용
using UnityEngine; // 유니티 런타임 관리자 기능 사용

[DefaultExecutionOrder(-8500)] // 탐사 일반 런타임보다 먼저 회복 관리자 준비
public sealed class CharacterRecoveryManager : MonoBehaviour // 사망 캐릭터의 거점 회복 진행 상태 관리
{
    public const int PrototypeRecoveryExpeditionCount = 2; // Prototype v0.1 임시 회복 필요 탐사 횟수
    public const int PrototypeRecoveredHealthPercent = 100; // Prototype v0.1 회복 완료 체력 비율

    private sealed class RecoveryState // 캐릭터 한 명의 회복 상태
    {
        public CharacterData Character; // 회복 대상 캐릭터
        public int RemainingExpeditions; // 남은 회복 필요 탐사 횟수
    }

    private readonly Dictionary<string, RecoveryState> recoveryStates =
        new Dictionary<string, RecoveryState>(); // 캐릭터별 회복 상태 목록

    private ExplorationSessionManager observedSession; // 현재 감시 중인 탐사 세션
    private bool completionProcessed; // 현재 탐사 종료 처리 완료 여부

    public static CharacterRecoveryManager Instance
    {
        get;
        private set;
    } // 전역 회복 관리자 조회

    public int RecoveryCount =>
        recoveryStates.Count; // 현재 회복 중 캐릭터 수 조회

    public event Action RecoveryStateChanged; // 회복 등록·진행·완료 상태 변경 알림

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeRuntime() // Scene 로드 후 회복 관리자 자동 준비
    {
        EnsureInstance(); // 회복 관리자 존재 보장
    }

    public static CharacterRecoveryManager EnsureInstance() // 회복 관리자 존재 보장
    {
        if (Instance != null)
        {
            return Instance; // 기존 관리자 반환
        }

        CharacterRecoveryManager existingManager =
            UnityEngine.Object.FindFirstObjectByType<CharacterRecoveryManager>(); // Scene 기존 관리자 조회

        if (existingManager != null)
        {
            return existingManager; // Scene 기존 관리자 반환
        }

        GameObject managerObject =
            new GameObject(
                "CharacterRecoveryManager",
                typeof(CharacterRecoveryManager)); // 영구 회복 관리자 생성

        return managerObject.GetComponent<CharacterRecoveryManager>(); // 생성 관리자 반환
    }

    private void Awake() // 회복 관리자 초기화
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject); // 중복 관리자 제거
            return;
        }

        Instance = this; // 전역 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 간 회복 상태 유지
    }

    private void Update() // 탐사 종료 상태 감시
    {
        ExplorationSessionManager currentSession =
            ExplorationSessionManager.Instance; // 현재 탐사 세션 조회

        if (currentSession == null)
        {
            observedSession = null; // 감시 세션 초기화
            completionProcessed = false; // 종료 처리 상태 초기화
            return;
        }

        if (observedSession != currentSession)
        {
            observedSession = currentSession; // 새 탐사 세션 추적
            completionProcessed = false; // 새 세션 종료 처리 허용
        }

        if (!currentSession.IsExplorationCompleted)
        {
            completionProcessed = false; // 새 탐사 진행 중 종료 처리 대기
            return;
        }

        if (completionProcessed)
        {
            return; // 같은 탐사 종료 중복 처리 차단
        }

        BattleResultManager resultManager =
            BattleResultManager.Instance; // 저장 파티 상태 관리자 조회

        if (resultManager == null)
        {
            return; // 파티 상태 관리자 준비 전 다음 프레임 재시도
        }

        completionProcessed = true; // 현재 탐사 종료 처리 예약

        ProcessCompletedExploration(
            resultManager); // 회복 진행과 신규 사망자 등록
    }

    public int ProcessCompletedExploration(
        BattleResultManager resultManager) // 탐사 한 번 종료에 따른 회복 처리
    {
        if (resultManager == null)
        {
            return 0;
        }

        int changedCount =
            AdvanceExistingRecoveries(
                resultManager); // 기존 회복 중 캐릭터부터 탐사 1회 진행

        changedCount +=
            RegisterDeadPartyMembers(
                resultManager); // 이번 탐사 신규 사망자를 그 다음에 등록

        if (changedCount > 0)
        {
            RecoveryStateChanged?.Invoke(); // 회복 상태 변경 알림
        }

        return changedCount; // 상태가 변경된 캐릭터 수 반환
    }

    public bool RegisterDeadCharacter(
        CharacterData characterData,
        BattleResultManager resultManager) // 사망 캐릭터 회복 설비 등록
    {
        bool registered =
            RegisterDeadCharacterInternal(
                characterData,
                resultManager); // 실제 등록 처리

        if (registered)
        {
            RecoveryStateChanged?.Invoke(); // 개별 등록 상태 변경 알림
        }

        return registered; // 등록 결과 반환
    }

    public bool IsRecovering(CharacterData characterData) // 캐릭터 회복 중 여부 조회
    {
        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        return recoveryStates.ContainsKey(
            characterData.CharacterId); // 회복 상태 존재 여부 반환
    }

    public bool IsRecovering(string characterId) // 캐릭터 ID 기반 회복 중 여부 조회
    {
        return !string.IsNullOrWhiteSpace(characterId) &&
               recoveryStates.ContainsKey(characterId); // 회복 상태 존재 여부 반환
    }

    public bool IsDead(CharacterData characterData) // 저장 상태 기준 사망 여부 조회
    {
        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        BattleResultManager resultManager =
            BattleResultManager.Instance; // 현재 파티 상태 관리자 조회

        return resultManager != null &&
               resultManager.TryGetSavedAllyHealth(
                   characterData.CharacterId,
                   out int currentHealth) &&
               currentHealth <= 0; // 저장 HP 0 이하 사망 판정
    }

    public bool CanDeploy(CharacterData characterData) // 현재 캐릭터 출전 가능 여부 조회
    {
        if (characterData == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        if (IsRecovering(characterData))
        {
            return false; // 회복 중 캐릭터 출전 불가
        }

        BattleResultManager resultManager =
            BattleResultManager.Instance; // 저장 파티 상태 관리자 조회

        if (resultManager == null ||
            !resultManager.TryGetSavedAllyHealth(
                characterData.CharacterId,
                out int currentHealth))
        {
            return true; // 아직 저장 상태 없는 신규 캐릭터는 출전 가능
        }

        return currentHealth > 0; // 사망 상태가 아니면 출전 가능
    }

    public bool CanDeployParty(PartyData partyData) // 파티 전체 출전 가능 여부 조회
    {
        if (partyData == null ||
            !partyData.IsValidParty())
        {
            return false;
        }

        foreach (CharacterData member in partyData.Members)
        {
            if (!CanDeploy(member))
            {
                return false; // 사망 또는 회복 중 파티원 포함 시 출전 불가
            }
        }

        return true; // 모든 파티원 출전 가능
    }

    public int GetRemainingRecoveryExpeditions(
        CharacterData characterData) // 캐릭터 남은 회복 필요 탐사 횟수 조회
    {
        if (characterData == null)
        {
            return 0;
        }

        return GetRemainingRecoveryExpeditions(
            characterData.CharacterId); // ID 기반 조회 호출
    }

    public int GetRemainingRecoveryExpeditions(
        string characterId) // 캐릭터 ID 기반 남은 회복 필요 탐사 횟수 조회
    {
        if (string.IsNullOrWhiteSpace(characterId) ||
            !recoveryStates.TryGetValue(
                characterId,
                out RecoveryState recoveryState))
        {
            return 0;
        }

        return Mathf.Max(
            0,
            recoveryState.RemainingExpeditions); // 남은 횟수 음수 방지 반환
    }

    public bool TryGetRecoveryState(
        CharacterData characterData,
        out int remainingExpeditions) // UI용 회복 상태 조회
    {
        remainingExpeditions =
            GetRemainingRecoveryExpeditions(
                characterData); // 남은 회복 횟수 조회

        return IsRecovering(
            characterData); // 회복 상태 존재 여부 반환
    }

    public void ResetRecoveryState() // 회복 상태 전체 초기화
    {
        if (recoveryStates.Count == 0)
        {
            return;
        }

        recoveryStates.Clear(); // 모든 회복 상태 제거
        RecoveryStateChanged?.Invoke(); // 회복 상태 초기화 알림
    }

    private int AdvanceExistingRecoveries(
        BattleResultManager resultManager) // 기존 회복 중 캐릭터 탐사 1회 진행
    {
        if (recoveryStates.Count == 0)
        {
            return 0;
        }

        List<string> characterIds =
            new List<string>(
                recoveryStates.Keys); // 제거 안전성을 위한 현재 ID 복사

        int changedCount = 0; // 변경 캐릭터 수 초기화

        foreach (string characterId in characterIds)
        {
            if (!recoveryStates.TryGetValue(
                    characterId,
                    out RecoveryState recoveryState))
            {
                continue;
            }

            if (recoveryState == null ||
                recoveryState.Character == null)
            {
                recoveryStates.Remove(
                    characterId); // 잘못된 회복 상태 제거

                changedCount += 1; // 상태 변경 수 증가
                continue;
            }

            recoveryState.RemainingExpeditions =
                Mathf.Max(
                    0,
                    recoveryState.RemainingExpeditions - 1); // 탐사 종료 1회 차감

            changedCount += 1; // 회복 진행 상태 변경 수 증가

            if (recoveryState.RemainingExpeditions > 0)
            {
                Debug.Log(
                    $"[CharacterRecovery][Day53] 회복 진행 - " +
                    $"{recoveryState.Character.DisplayName} / " +
                    $"남은 탐사 {recoveryState.RemainingExpeditions}회"); // 회복 진행 로그

                continue;
            }

            bool restored =
                resultManager.RestoreRecoveredAlly(
                    recoveryState.Character,
                    PrototypeRecoveredHealthPercent); // 회복 완료 HP 복구 시도

            if (restored)
            {
                recoveryStates.Remove(
                    characterId); // 회복 완료 상태 제거

                Debug.Log(
                    $"[CharacterRecovery][Day53] 회복 설비 완료 - " +
                    $"{recoveryState.Character.DisplayName} / " +
                    $"출전 가능"); // 회복 완료 로그

                continue;
            }

            bool alreadyLiving =
                resultManager.TryGetSavedAllyHealth(
                    characterId,
                    out int currentHealth) &&
                currentHealth > 0; // 외부 경로로 이미 생존 상태인지 확인

            if (alreadyLiving)
            {
                recoveryStates.Remove(
                    characterId); // 이미 생존 상태면 회복 상태 정리

                Debug.Log(
                    $"[CharacterRecovery][Day53] 회복 상태 정리 - " +
                    $"{recoveryState.Character.DisplayName} / " +
                    $"이미 생존 상태"); // 외부 복구 상태 정리 로그
            }
            else
            {
                Debug.LogWarning(
                    $"[CharacterRecovery][Day53] 회복 완료 상태 반영 실패 - " +
                    $"{recoveryState.Character.DisplayName}"); // 다음 탐사 종료에 재시도할 상태 유지
            }
        }

        return changedCount; // 변경 캐릭터 수 반환
    }

    private int RegisterDeadPartyMembers(
        BattleResultManager resultManager) // 이번 탐사 종료 시 신규 사망 파티원 자동 등록
    {
        PartyData activeParty =
            resultManager.ActiveParty; // 마지막 출전 파티 조회

        if (activeParty == null)
        {
            return 0;
        }

        int registeredCount = 0; // 신규 등록 인원 초기화

        foreach (CharacterData member in activeParty.Members)
        {
            if (RegisterDeadCharacterInternal(
                    member,
                    resultManager))
            {
                registeredCount += 1; // 신규 회복 등록 인원 증가
            }
        }

        return registeredCount; // 신규 등록 수 반환
    }

    private bool RegisterDeadCharacterInternal(
        CharacterData characterData,
        BattleResultManager resultManager) // 사망 캐릭터 실제 회복 등록
    {
        if (characterData == null ||
            resultManager == null ||
            string.IsNullOrWhiteSpace(characterData.CharacterId))
        {
            return false;
        }

        string characterId =
            characterData.CharacterId; // 등록 대상 ID 저장

        if (recoveryStates.ContainsKey(characterId))
        {
            return false; // 이미 회복 중인 캐릭터 중복 등록 차단
        }

        if (!resultManager.TryGetSavedAllyState(
                characterData,
                out int currentHealth,
                out int currentMental,
                out int deathCount) ||
            currentHealth > 0)
        {
            return false; // 사망 상태가 아니면 회복 등록 차단
        }

        recoveryStates[characterId] =
            new RecoveryState
            {
                Character = characterData,
                RemainingExpeditions = PrototypeRecoveryExpeditionCount
            }; // Prototype v0.1 회복 상태 생성

        Debug.Log(
            $"[CharacterRecovery][Day53] 회복 설비 등록 - " +
            $"{characterData.DisplayName} / " +
            $"HP {currentHealth} / " +
            $"정신 {currentMental} / " +
            $"사망 횟수 {deathCount} / " +
            $"필요 탐사 {PrototypeRecoveryExpeditionCount}회"); // 신규 회복 등록 로그

        return true; // 회복 등록 성공
    }

    private void OnDestroy() // 회복 관리자 제거 처리
    {
        if (Instance == this)
        {
            Instance = null; // 전역 회복 관리자 참조 제거
        }
    }
}
