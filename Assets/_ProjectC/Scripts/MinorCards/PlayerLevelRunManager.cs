using System; // 기본 이벤트 기능 사용
using UnityEngine; // 유니티 오브젝트 기능 사용

public sealed class PlayerLevelRunManager : MonoBehaviour // 게임 진행 플레이어 레벨 관리자
{
    private static PlayerLevelRunManager instance; // 현재 플레이어 레벨 관리자
    private PlayerLevelConfig levelConfig; // 적용 중인 레벨 설정
    private bool initialized; // 최초 진행 상태 생성 여부

    public static PlayerLevelRunManager Instance => instance; // 현재 관리자 조회
    public int Level { get; private set; } // 현재 플레이어 레벨
    public int CurrentExperience { get; private set; } // 현재 레벨 경험치
    public int PendingMinorCardChoices { get; private set; } // 아직 사용하지 않은 마이너 카드 선택권
    public int RequiredExperience => levelConfig == null ? 0 : levelConfig.GetRequiredExperience(Level); // 다음 레벨 필요 경험치

    public event Action ProgressChanged; // 레벨 또는 경험치 변경 알림
    public event Action<int> LevelUp; // 레벨 상승 알림

    public static PlayerLevelRunManager EnsureInstance(PlayerLevelConfig config) // 플레이어 레벨 관리자 준비
    {
        if (config == null) // 레벨 설정 누락 확인
        {
            throw new ArgumentNullException(nameof(config)); // 설정 누락 예외
        }

        if (instance == null) // 기존 관리자 없음 확인
        {
            instance = FindFirstObjectByType<PlayerLevelRunManager>(); // Scene에 존재하는 관리자 조회
        }

        if (instance == null) // 기존 관리자 최종 없음 확인
        {
            GameObject managerObject = new GameObject("PlayerLevelRunManager"); // 런타임 관리자 오브젝트 생성
            instance = managerObject.AddComponent<PlayerLevelRunManager>(); // 관리자 컴포넌트 생성
        }

        instance.Configure(config); // 현재 게임 레벨 설정 연결
        return instance; // 준비된 관리자 반환
    }

    private void Awake() // 관리자 단일 인스턴스 준비
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return;
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 후에도 플레이어 레벨 유지
    }

    public void Configure(PlayerLevelConfig config) // 레벨 설정 연결
    {
        if (config == null) // 잘못된 설정 확인
        {
            return;
        }

        levelConfig = config; // 현재 설정 저장
        if (!initialized) // 첫 게임 진행 상태 확인
        {
            ResetProgress(); // 시작 레벨과 경험치 생성
        }
        else // 기존 진행 상태 유지
        {
            ProgressChanged?.Invoke(); // 새 설정 기준 화면 갱신
        }
    }

    public int GainExperience(int amount) // 플레이어 경험치 획득
    {
        if (!initialized || levelConfig == null || amount <= 0) // 경험치 획득 가능 상태 확인
        {
            return 0;
        }

        CurrentExperience += amount; // 경험치 누적
        int gainedLevels = 0; // 이번 획득으로 오른 레벨 수
        int requiredExperience = RequiredExperience; // 현재 필요 경험치 조회

        while (requiredExperience > 0 && CurrentExperience >= requiredExperience) // 연속 레벨업 처리
        {
            CurrentExperience -= requiredExperience; // 사용한 경험치 차감
            Level++; // 플레이어 레벨 상승
            PendingMinorCardChoices++; // 레벨업당 마이너 카드 선택권 추가
            gainedLevels++; // 이번 레벨업 수 증가
            LevelUp?.Invoke(Level); // 레벨업 알림
            requiredExperience = RequiredExperience; // 다음 레벨 필요 경험치 갱신
        }

        ProgressChanged?.Invoke(); // 경험치와 선택권 화면 갱신
        return gainedLevels; // 오른 레벨 수 반환
    }

    public bool TryConsumeMinorCardChoice() // 마이너 카드 선택권 사용
    {
        if (PendingMinorCardChoices <= 0) // 남은 선택권 확인
        {
            return false;
        }

        PendingMinorCardChoices--; // 선택권 하나 소비
        ProgressChanged?.Invoke(); // 선택권 변경 알림
        return true;
    }

    public void ResetProgress() // 새 게임 기준 진행 상태 초기화
    {
        if (levelConfig == null) // 레벨 설정 확인
        {
            return;
        }

        Level = levelConfig.StartingLevel; // 시작 레벨 적용
        CurrentExperience = 0; // 경험치 초기화
        PendingMinorCardChoices = 0; // 대기 선택권 초기화
        initialized = true; // 진행 상태 생성 완료
        ProgressChanged?.Invoke(); // 초기 상태 알림
    }

    private void OnDestroy() // 관리자 제거 처리
    {
        if (instance == this) // 현재 전역 관리자 여부 확인
        {
            instance = null; // 전역 참조 제거
        }
    }
}
