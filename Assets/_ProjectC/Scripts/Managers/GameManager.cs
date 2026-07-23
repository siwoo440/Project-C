using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(-1000)] // 최우선 실행 순서
public sealed class GameManager : MonoBehaviour // 게임 공용 관리자
{
    public static GameManager Instance { get; private set; } // 싱글턴 인스턴스

    public bool IsInitialized { get; private set; } // 초기화 완료 상태

    private void Awake() // 오브젝트 생성 초기화
    {
        if (Instance != null && Instance != this) // 기존 관리자 존재 확인
        {
            Debug.LogWarning("중복된 GameManager를 제거합니다.", this); // 중복 경고 출력
            Destroy(this); // 중복 관리자 컴포넌트만 제거
            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 관리자 등록
        Initialize(); // 공용 초기화 실행
    }

    private void Initialize() // 게임 공용 초기화
    {
        if (IsInitialized) // 중복 초기화 확인
        {
            return; // 중복 초기화 중단
        }

        IsInitialized = true; // 초기화 완료 기록
        Debug.Log("GameManager 초기화 완료", this); // 초기화 결과 출력
    }

    private void OnDestroy() // 오브젝트 제거 처리
    {
        if (Instance == this) // 현재 인스턴스 일치 확인
        {
            Instance = null; // 싱글턴 참조 해제
        }
    }
}