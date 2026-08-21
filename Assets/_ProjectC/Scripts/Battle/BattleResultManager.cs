using System.Collections.Generic; // 사전 자료형 사용
using UnityEngine; // 유니티 기본 기능 사용
public sealed class BattleResultManager : MonoBehaviour // Scene 간 전투 결과와 아군 상태 보관
{ // 클래스 시작
    private readonly Dictionary<string, int> savedAllyHealth = new Dictionary<string, int>(); // 아군별 저장 체력 목록
    private BattleResultData pendingResult; // 탐사 전달 대기 결과
    public static BattleResultManager Instance { get; private set; } // 전역 결과 관리자 조회
    public bool HasPendingResult => pendingResult != null; // 전달 대기 결과 존재 여부 조회
    public static BattleResultManager EnsureInstance() // 결과 관리자 준비
    { // 관리자 준비 시작
        if (Instance != null) // 기존 관리자 확인
        { // 기존 관리자 처리 시작
            return Instance; // 기존 관리자 반환
        } // 기존 관리자 처리 종료
        BattleResultManager existingManager = Object.FindFirstObjectByType<BattleResultManager>(); // Scene 기존 관리자 조회
        if (existingManager != null) // Scene 관리자 존재 확인
        { // Scene 관리자 처리 시작
            return existingManager; // Scene 관리자 반환
        } // Scene 관리자 처리 종료
        GameObject managerObject = new GameObject("BattleResultManager", typeof(BattleResultManager)); // 영구 결과 관리자 오브젝트 생성
        return managerObject.GetComponent<BattleResultManager>(); // 생성 관리자 반환
    } // 관리자 준비 종료
    private void Awake() // 결과 관리자 초기화
    { // 초기화 시작
        if (Instance != null && Instance != this) // 중복 관리자 확인
        { // 중복 관리자 처리 시작
            Destroy(gameObject); // 중복 관리자 오브젝트 제거
            return; // 초기화 중단
        } // 중복 관리자 처리 종료
        Instance = this; // 전역 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 전환 유지 설정
    } // 초기화 종료
    public bool StoreResult(BattleResultData battleResultData) // 전투 결과와 아군 상태 저장
    { // 결과 저장 시작
        if (battleResultData == null || battleResultData.Result == BattleResult.None || pendingResult != null) // 저장 결과 유효성과 중복 확인
        { // 저장 불가 처리 시작
            return false; // 결과 저장 실패 반환
        } // 저장 불가 처리 종료
        pendingResult = battleResultData; // 탐사 전달 결과 저장
        foreach (BattleUnitResultData allyState in battleResultData.AllyStates) // 아군 종료 상태 목록 순회
        { // 아군 체력 저장 시작
            if (allyState == null || string.IsNullOrWhiteSpace(allyState.UnitId)) // 아군 상태 유효성 확인
            { // 잘못된 상태 처리 시작
                continue; // 다음 아군 상태 이동
            } // 잘못된 상태 처리 종료
            savedAllyHealth[allyState.UnitId] = allyState.CurrentHealth; // 아군 현재 체력 영구 상태 저장
        } // 아군 체력 저장 종료
        return true; // 결과 저장 성공 반환
    } // 결과 저장 종료
    public bool TryConsumeResult(out BattleResultData battleResultData) // 대기 전투 결과 한 번 소비
    { // 결과 소비 시작
        battleResultData = pendingResult; // 대기 결과 반환값 저장
        if (battleResultData == null) // 대기 결과 존재 확인
        { // 결과 없음 처리 시작
            return false; // 결과 소비 실패 반환
        } // 결과 없음 처리 종료
        pendingResult = null; // 소비한 대기 결과 제거
        return true; // 결과 소비 성공 반환
    } // 결과 소비 종료
    public void DiscardPendingResult() // 이전 대기 결과 제거
    { // 대기 결과 제거 시작
        pendingResult = null; // 대기 결과 초기화
    } // 대기 결과 제거 종료
    public bool ApplySavedAllyState(BattleUnitRuntime allyUnit) // 저장된 아군 체력 적용
    { // 아군 상태 적용 시작
        if (allyUnit == null || allyUnit.Team != BattleTeam.Ally || !savedAllyHealth.TryGetValue(allyUnit.UnitId, out int currentHealth)) // 저장 체력 존재 확인
        { // 저장 체력 없음 처리 시작
            return false; // 아군 상태 적용 실패 반환
        } // 저장 체력 없음 처리 종료
        return allyUnit.ApplyPersistentHealth(currentHealth); // 저장 체력 적용 결과 반환
    } // 아군 상태 적용 종료
    public bool TryGetSavedAllyHealth(string unitId, out int currentHealth) // 저장 아군 체력 조회
    { // 저장 체력 조회 시작
        return savedAllyHealth.TryGetValue(unitId, out currentHealth); // 저장 체력 조회 결과 반환
    } // 저장 체력 조회 종료
    public void ResetSavedPartyState() // 저장 아군 상태 전체 초기화
    { // 파티 상태 초기화 시작
        savedAllyHealth.Clear(); // 저장 아군 체력 비우기
    } // 파티 상태 초기화 종료
    private void OnDestroy() // 결과 관리자 제거 처리
    { // 제거 처리 시작
        if (Instance == this) // 현재 전역 관리자 확인
        { // 전역 참조 해제 시작
            Instance = null; // 전역 관리자 참조 제거
        } // 전역 참조 해제 종료
    } // 제거 처리 종료
} // 클래스 종료
