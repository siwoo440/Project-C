using UnityEngine; // 설정 씬 객체 사용

public sealed class SettingsController : MonoBehaviour // 설정 씬 화면 연결
{ // 설정 씬 관리자 시작
    private void Start() // 설정 화면 초기화
    { // 설정 화면 초기화 시작
        Canvas canvas = FindFirstObjectByType<Canvas>(); // 현재 설정 씬 캔버스 조회
        if (canvas == null) // 설정 캔버스 존재 확인
        { // 캔버스 누락 처리 시작
            Debug.LogError("[Settings] 설정 화면을 찾을 수 없습니다."); // 설정 화면 누락 안내
            return; // 설정 화면 생성 중단
        } // 캔버스 누락 처리 종료
        SettingsView.Create(canvas, Back); // 기획서 기반 설정 화면 생성
    } // 설정 화면 초기화 종료
    public void Back() // 이전 씬 복귀
    { // 이전 씬 복귀 시작
        if (SceneFlowManager.Instance == null) // SceneFlowManager 존재 여부 확인
        { // 씬 관리자 누락 처리 시작
            Debug.LogError("[SettingsController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 출력
            return; // 이전 씬 이동 중단
        } // 씬 관리자 누락 처리 종료

        SceneFlowManager.Instance.LoadPreviousScene(); // 이전 씬 복귀
    } // 이전 씬 복귀 종료
} // 설정 씬 관리자 종료
