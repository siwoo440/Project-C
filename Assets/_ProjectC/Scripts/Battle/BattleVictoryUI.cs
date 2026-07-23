using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleVictoryUI : MonoBehaviour // 전투 승리 UI 관리자
{
    [Header("승리 시스템 연결")] // 승리 시스템 연결 구분
    [SerializeField] private BattleVictoryRuntime victoryRuntime; // 승리 판정 관리자

    [Header("승리 UI 연결")] // 승리 UI 연결 구분
    [SerializeField] private GameObject victoryPanel; // 승리 화면
    [SerializeField] private TMP_Text victoryTitleText; // 승리 제목 문구

    private void Awake() // 승리 UI 초기화
    {
        if (victoryRuntime == null) // 승리 관리자 연결 확인
        {
            victoryRuntime = FindFirstObjectByType<BattleVictoryRuntime>(); // 씬 승리 관리자 검색
        }

        if (victoryPanel != null) // 승리 화면 연결 확인
        {
            victoryPanel.SetActive(false); // 시작 승리 화면 숨김
        }

        if (victoryTitleText != null) // 승리 문구 연결 확인
        {
            victoryTitleText.text = "VICTORY"; // 승리 제목 설정
        }
    }

    private void OnEnable() // 승리 이벤트 연결
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon += ShowVictory; // 승리 이벤트 등록
        }
    }

    private void Start() // 현재 승리 상태 확인
    {
        if (victoryRuntime != null && victoryRuntime.IsBattleWon) // 기존 승리 여부 확인
        {
            ShowVictory(); // 승리 화면 표시
        }
    }

    private void OnDisable() // 승리 이벤트 해제
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon -= ShowVictory; // 승리 이벤트 제거
        }
    }

    private void ShowVictory() // 승리 화면 표시
    {
        if (victoryPanel != null) // 승리 화면 연결 확인
        {
            victoryPanel.SetActive(true); // 승리 화면 활성화
            victoryPanel.transform.SetAsLastSibling(); // 승리 화면 최상단 배치
        }
    }
}