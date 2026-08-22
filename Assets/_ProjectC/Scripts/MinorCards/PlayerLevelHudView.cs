using System; // 기본 인터페이스 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 UI 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class PlayerLevelHudView : MonoBehaviour, IDisposable // 전투 화면 플레이어 레벨 표시
{
    private PlayerLevelRunManager levelManager; // 플레이어 레벨 관리자
    private TMP_Text levelText; // 레벨과 경험치 텍스트
    private bool disposed; // 화면 종료 여부

    public static PlayerLevelHudView Create(Canvas parentCanvas, PlayerLevelRunManager playerLevelManager) // 레벨 HUD 코드 생성
    {
        if (parentCanvas == null) // 부모 Canvas 확인
        {
            throw new ArgumentNullException(nameof(parentCanvas)); // Canvas 누락 예외
        }

        GameObject rootObject = new GameObject("PlayerLevelHud", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(PlayerLevelHudView)); // HUD 루트 생성
        rootObject.transform.SetParent(parentCanvas.transform, false); // 전투 Canvas 아래 배치
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 루트 사각형 조회
        rootRect.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
        rootRect.anchorMax = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
        rootRect.pivot = new Vector2(0.5f, 1f); // 상단 중앙 피벗
        rootRect.sizeDelta = new Vector2(360f, 48f); // HUD 크기
        rootRect.anchoredPosition = new Vector2(0f, -16f); // 상단 여백

        Canvas hudCanvas = rootObject.GetComponent<Canvas>(); // HUD Canvas 조회
        hudCanvas.overrideSorting = true; // 부모 정렬과 분리
        hudCanvas.sortingOrder = 430; // 일반 전투 UI 위 배치

        PlayerLevelHudView view = rootObject.GetComponent<PlayerLevelHudView>(); // HUD 컴포넌트 조회
        view.Initialize(playerLevelManager); // 레벨 관리자 연결
        return view; // 생성 HUD 반환
    }

    private void Initialize(PlayerLevelRunManager playerLevelManager) // HUD 초기화
    {
        levelManager = playerLevelManager ?? throw new ArgumentNullException(nameof(playerLevelManager)); // 레벨 관리자 저장

        Image backgroundImage = gameObject.AddComponent<Image>(); // HUD 배경 추가
        backgroundImage.color = new Color(0.04f, 0.045f, 0.065f, 0.94f); // HUD 배경색 적용
        backgroundImage.raycastTarget = false; // 입력 차단 해제

        GameObject textObject = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI)); // 레벨 텍스트 생성
        textObject.transform.SetParent(transform, false); // HUD 아래 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 사각형 조회
        textRect.anchorMin = Vector2.zero; // 전체 영역 앵커
        textRect.anchorMax = Vector2.one; // 전체 영역 앵커
        textRect.offsetMin = new Vector2(8f, 4f); // 내부 여백
        textRect.offsetMax = new Vector2(-8f, -4f); // 내부 여백

        levelText = textObject.GetComponent<TMP_Text>(); // TMP 텍스트 조회
        levelText.fontSize = 20f; // 글자 크기 설정
        levelText.color = Color.white; // 글자 색 설정
        levelText.alignment = TextAlignmentOptions.Center; // 중앙 정렬
        levelText.raycastTarget = false; // 입력 차단 해제

        levelManager.ProgressChanged += Refresh; // 레벨 진행 변경 연결
        Refresh(); // 첫 상태 표시
    }

    private void Refresh() // 레벨과 경험치 표시 갱신
    {
        if (disposed || levelManager == null || levelText == null) // 갱신 가능 상태 확인
        {
            return;
        }

        string pendingLabel = levelManager.PendingMinorCardChoices > 0 ? $"  · 선택 {levelManager.PendingMinorCardChoices}" : string.Empty; // 대기 선택권 문구
        levelText.text = $"LV {levelManager.Level}   EXP {levelManager.CurrentExperience} / {levelManager.RequiredExperience}{pendingLabel}"; // 현재 진행 상태 표시
    }

    public void Dispose() // HUD 연결 해제
    {
        if (disposed) // 중복 해제 확인
        {
            return;
        }

        disposed = true; // 화면 종료 상태 저장
        if (levelManager != null) // 레벨 관리자 존재 확인
        {
            levelManager.ProgressChanged -= Refresh; // 진행 상태 연결 해제
        }
    }
}
