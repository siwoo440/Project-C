using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(-1100)] // 공용 루트 최우선 실행 순서
public sealed class AppRootLifetime : MonoBehaviour // 공용 루트 생명주기 관리자
{
    public static AppRootLifetime Instance { get; private set; } // 공용 루트 인스턴스

    private void Awake() // 공용 루트 초기화
    {
        if (Instance != null && Instance != this) // 기존 공용 루트 존재 확인
        {
            Debug.LogWarning("중복된 AppRoot를 제거합니다.", this); // 중복 공용 루트 경고
            Destroy(gameObject); // 현재 중복 공용 루트 제거
            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 공용 루트 등록
        DontDestroyOnLoad(gameObject); // 씬 이동 시 공용 루트 유지
        Debug.Log("AppRoot 영구 유지 설정 완료", this); // 초기화 결과 출력
    }

    private void OnDestroy() // 공용 루트 제거 처리
    {
        if (Instance == this) // 현재 공용 루트 일치 확인
        {
            Instance = null; // 공용 루트 참조 해제
        }
    }
}