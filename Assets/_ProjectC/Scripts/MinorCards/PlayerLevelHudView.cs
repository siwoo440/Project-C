using System; // 기본 인터페이스 기능 사용
using System.Collections.Generic; // 경험치 블록 목록 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 UI 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용

public sealed class PlayerLevelHudView : MonoBehaviour, IDisposable // 전투 화면 플레이어 레벨 표시
{
    private PlayerLevelRunManager levelManager; // 플레이어 레벨 관리자
    private TMP_Text levelText; // 레벨과 경험치 텍스트
    private RectTransform experienceBlocks; // 경험치 블록 배치 영역
    private readonly List<Image> blockImages = new List<Image>(); // 실제 경험치 블록 이미지 목록
    private bool disposed; // 화면 종료 여부

    public static PlayerLevelHudView Create(Canvas parentCanvas, PlayerLevelRunManager playerLevelManager) // 레벨 HUD 코드 생성
    {
        if (parentCanvas == null) // 부모 Canvas 확인
        {
            throw new ArgumentNullException(nameof(parentCanvas)); // Canvas 누락 예외
        }

        GameObject rootObject = new GameObject("PlayerLevelHud", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(PlayerLevelHudView)); // HUD 루트 생성
        BattleHandView hand = parentCanvas.GetComponentInChildren<BattleHandView>(); // 실제 손패 화면 조회
        rootObject.transform.SetParent(hand != null ? hand.transform : parentCanvas.transform, false); // 손패 또는 전투 캔버스에 배치
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 루트 사각형 조회
        rootRect.anchorMin = hand != null ? new Vector2(0.42f, 1f) : new Vector2(0.5f, 1f); // 손패 중앙 최소 앵커
        rootRect.anchorMax = hand != null ? new Vector2(0.62f, 1f) : new Vector2(0.5f, 1f); // 손패 중앙 최대 앵커
        rootRect.pivot = new Vector2(0.5f, 1f); // 상단 중앙 피벗
        rootRect.sizeDelta = hand != null ? new Vector2(0f, 62f) : new Vector2(360f, 62f); // 레벨과 경험치 칸을 담는 HUD 크기
        rootRect.anchoredPosition = new Vector2(0f, hand != null ? -32f : -18f); // 손패 패널 위쪽 중앙의 레벨 위치

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
        if (!BattleUiArt.Apply(backgroundImage, "LevelFrame")) // 제작한 레벨 프레임 연결 확인
        { // 레벨 프레임 대체 시작
            LobbyUiArt.Apply(backgroundImage, "Button"); // 이미지 누락 시 기존 프레임 적용
        } // 레벨 프레임 대체 종료
        backgroundImage.raycastTarget = false; // 입력 차단 해제

        GameObject textObject = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI)); // 레벨 텍스트 생성
        textObject.transform.SetParent(transform, false); // HUD 아래 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 사각형 조회
        textRect.anchorMin = new Vector2(0f, 0.34f); // 경험치 칸 위쪽의 글자 최소 앵커
        textRect.anchorMax = Vector2.one; // 글자 위쪽 최대 앵커
        textRect.offsetMin = new Vector2(8f, 0f); // 글자 왼쪽 내부 여백
        textRect.offsetMax = new Vector2(-8f, -2f); // 글자 오른쪽 위 내부 여백

        levelText = textObject.GetComponent<TMP_Text>(); // TMP 텍스트 조회
        levelText.fontSize = 20f; // 글자 크기 설정
        levelText.color = Color.white; // 글자 색 설정
        levelText.alignment = TextAlignmentOptions.Center; // 중앙 정렬
        levelText.raycastTarget = false; // 입력 차단 해제
        levelText.enableAutoSizing = true; // 긴 레벨 정보의 글자 크기 자동 조정
        levelText.fontSizeMin = 12f; // 레벨 글자 최소 크기
        levelText.fontSizeMax = 20f; // 레벨 글자 최대 크기
        GameObject blocksObject = new GameObject("ExperienceBlocks", typeof(RectTransform)); // 경험치 칸 배치 영역 생성
        blocksObject.transform.SetParent(transform, false); // 레벨 프레임 내부에 경험치 칸 배치
        experienceBlocks = blocksObject.GetComponent<RectTransform>(); // 경험치 칸 사각형 조회
        experienceBlocks.anchorMin = new Vector2(0.025f, 0.085f); // 경험치 트랙 왼쪽 아래 앵커
        experienceBlocks.anchorMax = new Vector2(0.975f, 0.305f); // 경험치 트랙 오른쪽 위 앵커
        experienceBlocks.offsetMin = Vector2.zero; // 경험치 트랙 왼쪽 아래 여백 제거
        experienceBlocks.offsetMax = Vector2.zero; // 경험치 트랙 오른쪽 위 여백 제거

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
        RefreshExperienceBlocks(); // 실제 경험치에 맞춘 노란 칸 갱신
    }

    private void RefreshExperienceBlocks() // 실제 경험치에 따른 블록 갱신
    { // 경험치 블록 갱신 시작
        int required = Mathf.Max(1, levelManager.RequiredExperience); // 현재 레벨의 필요 경험치 확인
        int count = Mathf.Min(24, required); // 과도한 UI 생성을 막는 최대 칸 수 설정
        while (blockImages.Count < count) // 부족한 경험치 칸 확인
        { // 경험치 칸 추가 시작
            GameObject blockObject = new GameObject("Block", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 개별 경험치 칸 생성
            blockObject.transform.SetParent(experienceBlocks, false); // 경험치 트랙에 칸 연결
            Image block = blockObject.GetComponent<Image>(); // 경험치 칸 이미지 조회
            block.raycastTarget = false; // 경험치 칸의 입력 차단 방지
            blockImages.Add(block); // 생성한 칸 목록 등록
        } // 경험치 칸 추가 종료
        while (blockImages.Count > count) // 초과 경험치 칸 확인
        { // 초과 경험치 칸 정리 시작
            Image block = blockImages[blockImages.Count - 1]; // 마지막 경험치 칸 조회
            blockImages.RemoveAt(blockImages.Count - 1); // 목록에서 초과 칸 제거
            block.transform.SetParent(null, false); // 화면에서 초과 칸 즉시 분리
            Destroy(block.gameObject); // 초과 경험치 칸 제거
        } // 초과 경험치 칸 정리 종료
        int filled = (int)((long)levelManager.CurrentExperience * count / required); // 실제 경험치 비율의 채워진 칸 수 계산
        for (int index = 0; index < count; index++) // 경험치 칸 순서대로 배치
        { // 개별 경험치 칸 배치 시작
            RectTransform rect = blockImages[index].rectTransform; // 현재 경험치 칸 사각형 조회
            rect.anchorMin = new Vector2((float)index / count, 0f); // 경험치 칸 왼쪽 아래 앵커
            rect.anchorMax = new Vector2((float)(index + 1) / count, 1f); // 경험치 칸 오른쪽 위 앵커
            rect.offsetMin = new Vector2(1f, 1f); // 경험치 칸 왼쪽 아래 간격
            rect.offsetMax = new Vector2(-1f, -1f); // 경험치 칸 오른쪽 위 간격
            blockImages[index].color = index < filled ? new Color(0.98f, 0.78f, 0.18f, 1f) : new Color(0.12f, 0.16f, 0.14f, 1f); // 채운 칸 노랑과 빈 칸 어두운색 구분
        } // 개별 경험치 칸 배치 종료
    } // 경험치 블록 갱신 종료

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
