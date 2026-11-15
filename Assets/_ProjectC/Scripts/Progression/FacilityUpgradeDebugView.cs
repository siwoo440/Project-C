using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.InputSystem; // 단축키 입력 사용
using UnityEngine.SceneManagement; // 현재 씬 확인 사용
using UnityEngine.UI; // 로비 캔버스 사용
public sealed class FacilityUpgradeDebugView : MonoBehaviour // 설비 강화 화면 열기 관리
{ // 설비 화면 관리 시작
    private const string LobbySceneName = "20_Lobby"; // 설비 화면 허용 로비 씬
    private FacilityUpgradePanel panel; // 현재 설비 강화 화면
    private bool panelVisible; // 설비 화면 표시 여부
    public void OpenPanel() // 로비에서 설비 강화 화면 열기
    { // 설비 화면 열기 시작
        if (!IsLobbyScene()) // 로비 씬 여부 확인
        { // 열기 불가 처리 시작
            return; // 화면 열기 중단
        } // 열기 불가 처리 종료
        if (panel != null) // 기존 설비 화면 확인
        { // 기존 화면 처리 시작
            panel.transform.SetAsLastSibling(); // 기존 화면을 맨 앞으로 이동
            return; // 중복 생성 방지
        } // 기존 화면 처리 종료
        LobbyLayoutView lobby = FindFirstObjectByType<LobbyLayoutView>(); // 실제 로비 화면 조회
        Canvas canvas = lobby == null ? null : lobby.GetComponentInParent<Canvas>(); // 로비 캔버스 조회
        if (canvas == null) // 로비 캔버스 존재 확인
        { // 캔버스 없음 처리 시작
            Debug.LogWarning("[FacilityUpgrade] 로비 화면을 찾을 수 없습니다."); // 화면 누락 안내
            return; // 설비 화면 열기 중단
        } // 캔버스 없음 처리 종료
        panel = FacilityUpgradePanel.Create(canvas, HandlePanelClosed); // 설비 강화 화면 생성
        panelVisible = panel != null; // 실제 화면 표시 상태 저장
    } // 설비 화면 열기 종료
    private void Update() // 설비 화면 단축키 처리
    { // 단축키 처리 시작
        if (!IsLobbyScene()) // 로비 씬 여부 확인
        { // 다른 씬 처리 시작
            panelVisible = false; // 화면 표시 상태 해제
            panel = null; // 이전 화면 참조 해제
            return; // 단축키 처리 중단
        } // 다른 씬 처리 종료
        if (panelVisible && panel == null) // 씬 전환 등으로 제거된 화면 확인
        { // 제거된 화면 정리 시작
            panelVisible = false; // 화면 표시 상태 해제
        } // 제거된 화면 정리 종료
        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회
        if (keyboard == null || !keyboard.f7Key.wasPressedThisFrame) // 설비 단축키 입력 확인
        { // 입력 없음 처리 시작
            return; // 단축키 처리 중단
        } // 입력 없음 처리 종료
        if (panelVisible && panel != null) // 열린 설비 화면 확인
        { // 단축키 닫기 시작
            panel.Close(); // 현재 설비 화면 닫기
        } // 단축키 닫기 종료
        else // 닫힌 화면 처리
        { // 단축키 열기 시작
            OpenPanel(); // 설비 강화 화면 열기
        } // 단축키 열기 종료
    } // 단축키 처리 종료
    private void HandlePanelClosed() // 설비 화면 닫힘 처리
    { // 닫힘 처리 시작
        panel = null; // 기존 화면 참조 제거
        panelVisible = false; // 화면 표시 상태 해제
    } // 닫힘 처리 종료
    private static bool IsLobbyScene() // 현재 로비 씬 확인
    { // 로비 씬 확인 시작
        return SceneManager.GetActiveScene().name == LobbySceneName; // 로비 씬 일치 여부 반환
    } // 로비 씬 확인 종료
} // 설비 화면 관리 종료
