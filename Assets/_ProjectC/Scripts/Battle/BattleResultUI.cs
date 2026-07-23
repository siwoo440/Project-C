using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class BattleResultUI : MonoBehaviour // 전투 결과 UI 관리자
{
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleVictoryRuntime victoryRuntime; // 승리 판정 관리자
    [SerializeField] private BattleTurnRuntime turnRuntime; // 전투 턴 관리자

    [Header("결과 UI 연결")] // 결과 UI 연결 구분
    [SerializeField] private GameObject resultPanel; // 전투 결과 화면
    [SerializeField] private TMP_Text resultTitleText; // 전투 결과 제목
    [SerializeField] private TMP_Text resultDescriptionText; // 전투 결과 설명
    [SerializeField] private Button retryButton; // 전투 재시작 버튼
    [SerializeField] private Button returnButton; // 탐사 복귀 버튼

    private bool resultShown; // 전투 결과 표시 여부

    private void Awake() // 전투 결과 UI 초기화
    {
        if (victoryRuntime == null) // 승리 관리자 연결 확인
        {
            victoryRuntime = FindFirstObjectByType<BattleVictoryRuntime>(); // 씬 승리 관리자 검색
        }

        if (turnRuntime == null) // 턴 관리자 연결 확인
        {
            turnRuntime = FindFirstObjectByType<BattleTurnRuntime>(); // 씬 턴 관리자 검색
        }

        if (resultPanel != null) // 결과 화면 연결 확인
        {
            resultPanel.SetActive(false); // 시작 결과 화면 숨김
        }

        if (retryButton != null) // 재시작 버튼 연결 확인
        {
            retryButton.onClick.AddListener(HandleRetryClicked); // 재시작 버튼 이벤트 연결
        }

        if (returnButton != null) // 복귀 버튼 연결 확인
        {
            returnButton.onClick.AddListener(HandleReturnClicked); // 복귀 버튼 이벤트 연결
        }

        resultShown = false; // 결과 표시 상태 초기화
    }

    private void OnEnable() // 전투 결과 이벤트 연결
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon += ShowVictory; // 승리 이벤트 등록
        }

        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.BattleDefeated += ShowDefeat; // 패배 이벤트 등록
        }
    }

    private void Start() // 기존 전투 결과 확인
    {
        if (victoryRuntime != null && victoryRuntime.IsBattleWon) // 기존 승리 상태 확인
        {
            ShowVictory(); // 승리 결과 표시
        }
    }

    private void OnDisable() // 전투 결과 이벤트 해제
    {
        if (victoryRuntime != null) // 승리 관리자 연결 확인
        {
            victoryRuntime.BattleWon -= ShowVictory; // 승리 이벤트 제거
        }

        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.BattleDefeated -= ShowDefeat; // 패배 이벤트 제거
        }
    }

    private void OnDestroy() // 전투 결과 UI 제거
    {
        if (retryButton != null) // 재시작 버튼 연결 확인
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked); // 재시작 버튼 이벤트 제거
        }

        if (returnButton != null) // 복귀 버튼 연결 확인
        {
            returnButton.onClick.RemoveListener(HandleReturnClicked); // 복귀 버튼 이벤트 제거
        }
    }

    private void ShowVictory() // 승리 결과 표시
    {
        ShowResult("VICTORY", "ALL ENEMIES DEFEATED"); // 승리 문구 전달
    }

    private void ShowDefeat() // 패배 결과 표시
    {
        ShowResult("DEFEAT", "THE PARTY HAS FALLEN"); // 패배 문구 전달
    }

    private void ShowResult(string title, string description) // 공통 결과 화면 표시
    {
        if (resultShown) // 기존 결과 표시 확인
        {
            return; // 중복 결과 표시 중단
        }

        resultShown = true; // 결과 표시 상태 설정

        if (resultTitleText != null) // 결과 제목 연결 확인
        {
            resultTitleText.text = title; // 결과 제목 적용
        }

        if (resultDescriptionText != null) // 결과 설명 연결 확인
        {
            resultDescriptionText.text = description; // 결과 설명 적용
        }

        if (resultPanel != null) // 결과 화면 연결 확인
        {
            resultPanel.SetActive(true); // 결과 화면 활성화
            resultPanel.transform.SetAsLastSibling(); // 결과 화면 최상단 배치
        }

        SetButtonsInteractable(true); // 결과 버튼 활성화
    }

    private void HandleRetryClicked() // 전투 재시작 버튼 처리
    {
        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance; // 공용 씬 관리자 가져오기

        if (sceneFlowManager == null) // 씬 관리자 존재 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 전투 재시작 중단
        }

        SetButtonsInteractable(false); // 중복 버튼 입력 차단
        sceneFlowManager.ReloadCurrentScene(); // 현재 전투 씬 다시 불러오기
    }

    private void HandleReturnClicked() // 탐사 복귀 버튼 처리
    {
        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance; // 공용 씬 관리자 가져오기

        if (sceneFlowManager == null) // 씬 관리자 존재 확인
        {
            Debug.LogError("SceneFlowManager를 찾을 수 없습니다.", this); // 관리자 누락 오류
            return; // 탐사 복귀 중단
        }

        SetButtonsInteractable(false); // 중복 버튼 입력 차단
        sceneFlowManager.LoadScene(SceneFlowManager.ExpeditionSceneName); // 탐사 씬 이동
    }

    private void SetButtonsInteractable(bool isInteractable) // 결과 버튼 입력 상태 설정
    {
        if (retryButton != null) // 재시작 버튼 연결 확인
        {
            retryButton.interactable = isInteractable; // 재시작 버튼 상태 적용
        }

        if (returnButton != null) // 복귀 버튼 연결 확인
        {
            returnButton.interactable = isInteractable; // 복귀 버튼 상태 적용
        }
    }
}