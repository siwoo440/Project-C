using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) // 기존 GameManager 확인
        {
            GameObject currentRoot = transform.root.gameObject; // 현재 Systems 루트 확인
            GameObject existingRoot = Instance.transform.root.gameObject; // 기존 Systems 루트 확인

            if (currentRoot != existingRoot) // 서로 다른 Systems 루트 확인
            {
                Destroy(currentRoot); // 새 중복 Systems 제거
            }
            else // 같은 Systems 내부 중복 확인
            {
                Destroy(gameObject); // 중복 GameManager만 제거
            }

            return; // 중복 초기화 중단
        }

        Instance = this; // 현재 인스턴스 등록
        DontDestroyOnLoad(transform.root.gameObject); // Systems 씬 유지
        Initialize(); // 게임 관리자 초기화
    }

    private void Initialize()
    {
        if (IsInitialized) // 중복 초기화 확인
        {
            return; // 중복 초기화 중단
        }

        IsInitialized = true; // 초기화 완료 기록
        Debug.Log("[GameManager] 초기화 완료"); // 초기화 확인 로그
    }

    private void OnDestroy()
    {
        if (Instance == this) // 현재 인스턴스 확인
        {
            Instance = null; // 인스턴스 참조 해제
        }
    }
}
