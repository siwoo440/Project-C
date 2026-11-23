using System; // 초기화 오류 사용
using UnityEngine; // 유니티 관리자 사용

public sealed class LobbyAutosaveController : MonoBehaviour // 거점 상태 자동 저장 관리자
{ // 자동 저장 관리자 시작
    private string savePath; // 현재 저장 파일 경로
    private bool dirty; // 저장 대기 상태
    private PlayerResourceManager resources; // 자원 변경 감시 관리자
    private FacilityUpgradeManager facilities; // 설비 변경 감시 관리자
    private CharacterProgressionManager progression; // 성장 변경 감시 관리자
    private CharacterAffinityManager affinity; // 호감도 변경 감시 관리자
    private BattleResultManager result; // 캐릭터 상태 감시 관리자
    private CharacterRecoveryManager recovery; // 회복 상태 감시 관리자
    public void Initialize(string path) // 자동 저장 경로와 이벤트 연결
    { // 자동 저장 초기화 시작
        if (savePath != null || string.IsNullOrWhiteSpace(path)) // 중복 초기화와 빈 경로 확인
        { // 잘못된 초기화 시작
            throw new ArgumentException("자동 저장 경로가 올바르지 않습니다.", nameof(path)); // 초기화 오류 알림
        } // 잘못된 초기화 종료
        savePath = path; // 저장 경로 보관
        resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
        facilities = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
        progression = CharacterProgressionManager.EnsureInstance(); // 성장 관리자 준비
        affinity = CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비
        result = BattleResultManager.EnsureInstance(); // 캐릭터 상태 관리자 준비
        recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
        resources.ResourcesChanged += MarkDirty; // 자원 변경 감시
        facilities.FacilityChanged += OnFacilityChanged; // 설비 변경 감시
        progression.ProgressChanged += MarkDirty; // 성장 변경 감시
        affinity.AffinityChanged += MarkDirty; // 호감도 변경 감시
        result.PartyStateChanged += MarkDirty; // 캐릭터 상태 변경 감시
        recovery.RecoveryStateChanged += MarkDirty; // 회복 상태 변경 감시
        dirty = true; // 거점 진입 상태 저장 예약
    } // 자동 저장 초기화 종료
    private void OnFacilityChanged(FacilityType type, int level) // 설비 변경 수신
    { // 설비 변경 처리 시작
        MarkDirty(); // 저장 예약
    } // 설비 변경 처리 종료
    private void MarkDirty() // 변경 상태 기록
    { // 변경 기록 시작
        dirty = true; // 프레임 종료 저장 예약
    } // 변경 기록 종료
    private void LateUpdate() // 프레임 종료 저장 처리
    { // 자동 저장 처리 시작
        if (!dirty || savePath == null) // 저장 대기와 경로 확인
        { // 저장 불필요 처리 시작
            return; // 저장 중단
        } // 저장 불필요 처리 종료
        dirty = false; // 현재 저장 예약 해제
        if (!LobbySaveService.Save(savePath)) // 거점 상태 저장 시도
        { // 저장 실패 처리 시작
            Debug.LogWarning("[LobbySave] 거점 자동 저장에 실패했습니다."); // 저장 실패 안내
        } // 저장 실패 처리 종료
    } // 자동 저장 처리 종료
    private void OnDestroy() // 저장 이벤트 연결 해제
    { // 이벤트 해제 시작
        if (resources != null) // 자원 관리자 존재 확인
        { // 자원 연결 해제 시작
            resources.ResourcesChanged -= MarkDirty; // 자원 감시 해제
        } // 자원 연결 해제 종료
        if (facilities != null) // 설비 관리자 존재 확인
        { // 설비 연결 해제 시작
            facilities.FacilityChanged -= OnFacilityChanged; // 설비 감시 해제
        } // 설비 연결 해제 종료
        if (progression != null) // 성장 관리자 존재 확인
        { // 성장 연결 해제 시작
            progression.ProgressChanged -= MarkDirty; // 성장 감시 해제
        } // 성장 연결 해제 종료
        if (affinity != null) // 호감도 관리자 존재 확인
        { // 호감도 연결 해제 시작
            affinity.AffinityChanged -= MarkDirty; // 호감도 감시 해제
        } // 호감도 연결 해제 종료
        if (result != null) // 캐릭터 상태 관리자 존재 확인
        { // 캐릭터 연결 해제 시작
            result.PartyStateChanged -= MarkDirty; // 캐릭터 감시 해제
        } // 캐릭터 연결 해제 종료
        if (recovery != null) // 회복 관리자 존재 확인
        { // 회복 연결 해제 시작
            recovery.RecoveryStateChanged -= MarkDirty; // 회복 감시 해제
        } // 회복 연결 해제 종료
    } // 이벤트 해제 종료
} // 자동 저장 관리자 종료
