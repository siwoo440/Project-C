using TMPro; // 탐사 HUD 텍스트 사용
using UnityEngine; // 탐사 프로토타입 오브젝트 사용
using UnityEngine.InputSystem; // 디버그 입력 사용
using UnityEngine.UI; // Canvas UI 사용

[DefaultExecutionOrder(-400)]
public sealed class ExplorationPrototypeBootstrap : MonoBehaviour // 탐사 프로토타입 기본 오브젝트 생성
{
    private static Sprite runtimeSquareSprite; // 플레이어 런타임 스프라이트
    private TMP_Text progressText; // 영구 진행 HUD 텍스트
    private int lastDisplayedFloor = -1; // 마지막 HUD 표시 층

    private void Start() // 탐사 프로토타입 시작
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        CharacterProgressionManager.EnsureInstance(); // 캐릭터 성장 관리자 준비
        CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비
        PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비

        EnsureRuntimeSquareSprite(); // 플레이어 사각형 스프라이트 준비
        CreatePlayer(sessionManager); // 탐사 플레이어 생성
        CreateHud(sessionManager); // 탐사 HUD 생성
    }

    private void Update() // 탐사 디버그 입력 처리
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 현재 탐사 층 조회

        if (lastDisplayedFloor != sessionManager.CurrentFloor)
        {
            RefreshProgressText(); // 층 변경 시 진행 HUD 갱신
        }

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard == null ||
            !keyboard.f8Key.wasPressedThisFrame)
        {
            return;
        }

        CharacterAffinityManager affinityManager =
            CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비

        affinityManager.GrantExplorationSuccessAffinity(1); // 탐사 성공 호감도 테스트 적용
        RefreshProgressText(); // 진행 HUD 갱신

        Debug.Log(
            "[Exploration][DEBUG] 탐사 성공을 가정해 호감도 +1을 적용했습니다."); // 호감도 테스트 로그
    }

    private static void CreatePlayer(
        ExplorationSessionManager sessionManager) // 탐사 플레이어 생성
    {
        GameObject playerObject =
            new GameObject(
                "ExplorationPlayer",
                typeof(SpriteRenderer),
                typeof(Rigidbody2D),
                typeof(BoxCollider2D),
                typeof(ExplorationPlayerController)); // 플레이어 오브젝트 생성

        playerObject.transform.position =
            sessionManager.GetPlayerSpawnPosition(
                new Vector3(-3.5f, -2.7f, 0f)); // 플레이어 초기 위치 지정

        SpriteRenderer spriteRenderer =
            playerObject.GetComponent<SpriteRenderer>(); // 플레이어 SpriteRenderer 조회

        spriteRenderer.sprite =
            runtimeSquareSprite; // 임시 플레이어 스프라이트 지정

        spriteRenderer.color =
            new Color(0.2f, 0.7f, 1f, 1f); // 플레이어 파란색 표시

        spriteRenderer.sortingOrder = 5; // 플레이어 표시 순서 지정

        playerObject.transform.localScale =
            new Vector3(0.55f, 0.55f, 1f); // 플레이어 크기 지정

        Rigidbody2D body =
            playerObject.GetComponent<Rigidbody2D>(); // 플레이어 Rigidbody2D 조회

        body.gravityScale = 0f; // 중력 제거
        body.freezeRotation = true; // 회전 고정

        BoxCollider2D collider =
            playerObject.GetComponent<BoxCollider2D>(); // 플레이어 Collider 조회

        collider.size = Vector2.one; // 플레이어 충돌 크기 지정
    }

    private void CreateHud(
        ExplorationSessionManager sessionManager) // 탐사 HUD 생성
    {
        GameObject canvasObject =
            new GameObject(
                "ExplorationPrototypeHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)); // 탐사 HUD Canvas 생성

        Canvas canvas =
            canvasObject.GetComponent<Canvas>(); // Canvas 조회

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay; // 화면 오버레이 방식 지정

        canvas.sortingOrder = 100; // HUD 최상위 표시

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 대응 설정

        scaler.referenceResolution =
            new Vector2(1920f, 1080f); // 기준 해상도 설정

        scaler.matchWidthOrHeight = 0.5f; // 가로세로 중간 비율 설정

        CreateInstructionText(
            canvasObject.transform,
            sessionManager); // 조작 안내 생성

        CreateProgressText(
            canvasObject.transform); // 영구 진행 HUD 생성

        CreateLastRewardText(
            canvasObject.transform,
            sessionManager); // 마지막 보상 HUD 생성
    }

    private static void CreateInstructionText(
        Transform parent,
        ExplorationSessionManager sessionManager) // 탐사 안내 텍스트 생성
    {
        TMP_Text instructionText =
            CreateText(
                "Instructions",
                parent,
                24f,
                TextAlignmentOptions.TopLeft); // 안내 텍스트 생성

        RectTransform instructionRect =
            instructionText.rectTransform; // 안내 RectTransform 조회

        instructionRect.anchorMin =
            new Vector2(0f, 1f); // 왼쪽 위 최소 앵커 설정

        instructionRect.anchorMax =
            new Vector2(0f, 1f); // 왼쪽 위 최대 앵커 설정

        instructionRect.pivot =
            new Vector2(0f, 1f); // 왼쪽 위 Pivot 설정

        instructionRect.sizeDelta =
            new Vector2(980f, 220f); // 안내 영역 크기 설정

        instructionRect.anchoredPosition =
            new Vector2(24f, -24f); // 안내 위치 설정

        instructionText.text =
            "42일차 층 난이도 상승 테스트\n" +
            "1F 기준 · 층마다 적 HP +12% · 공격 +8% · 보상 +5%\n" +
            "계단으로 다음 층 진입 시 난이도 자동 상승\n" +
            "F9는 Seed만 변경하며 현재 층 난이도는 유지\n" +
            "미니맵 P는 현재 플레이어 위치를 계속 추적\n" +
            "F8: 탐사 성공 호감도 +1"; // 탐사 안내 문구 지정
    }

    private void CreateProgressText(
        Transform parent) // 영구 진행 HUD 생성
    {
        progressText =
            CreateText(
                "PersistentProgress",
                parent,
                24f,
                TextAlignmentOptions.TopRight); // 진행 텍스트 생성

        RectTransform progressRect =
            progressText.rectTransform; // 진행 RectTransform 조회

        progressRect.anchorMin =
            new Vector2(1f, 1f); // 오른쪽 위 최소 앵커 설정

        progressRect.anchorMax =
            new Vector2(1f, 1f); // 오른쪽 위 최대 앵커 설정

        progressRect.pivot =
            new Vector2(1f, 1f); // 오른쪽 위 Pivot 설정

        progressRect.sizeDelta =
            new Vector2(700f, 240f); // 진행 영역 크기 설정

        progressRect.anchoredPosition =
            new Vector2(-24f, -24f); // 진행 텍스트 위치 설정

        RefreshProgressText(); // 진행 정보 최초 갱신
    }

    private void RefreshProgressText() // 영구 진행 HUD 갱신
    {
        if (progressText == null)
        {
            return;
        }

        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance(); // 캐릭터 성장 관리자 준비

        CharacterAffinityManager affinityManager =
            CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        progressText.text =
            $"탐사 {sessionManager.CurrentFloor}F\n" +
            $"캐릭터 Lv.{progressionManager.Level}  " +
            $"EXP {progressionManager.CurrentExperience}" +
            $"/{progressionManager.RequiredExperience}\n" +
            $"호감도 {affinityManager.Affinity}\n" +
            $"Gold {resourceManager.Gold}\n" +
            $"나사 {resourceManager.Screw}  " +
            $"철판 {resourceManager.IronPlate}  " +
            $"전선 {resourceManager.Wire}\n" +
            $"이번 탐사 클리어 조우 {sessionManager.ClearedEncounterIds.Count}"; // 영구 진행 상태 출력

        lastDisplayedFloor =
            sessionManager.CurrentFloor; // 현재 HUD 표시 층 저장
    }

    private static void CreateLastRewardText(
        Transform parent,
        ExplorationSessionManager sessionManager) // 마지막 클리어 보상 표시
    {
        ExplorationClearRewardResult reward =
            sessionManager.LastClearReward; // 마지막 클리어 보상 조회

        if (reward == null)
        {
            return;
        }

        TMP_Text rewardText =
            CreateText(
                "LastClearReward",
                parent,
                25f,
                TextAlignmentOptions.BottomLeft); // 보상 텍스트 생성

        RectTransform rewardRect =
            rewardText.rectTransform; // 보상 RectTransform 조회

        rewardRect.anchorMin =
            new Vector2(0f, 0f); // 왼쪽 아래 최소 앵커 설정

        rewardRect.anchorMax =
            new Vector2(0f, 0f); // 왼쪽 아래 최대 앵커 설정

        rewardRect.pivot =
            new Vector2(0f, 0f); // 왼쪽 아래 Pivot 설정

        rewardRect.sizeDelta =
            new Vector2(1100f, 140f); // 보상 영역 크기 설정

        rewardRect.anchoredPosition =
            new Vector2(24f, 24f); // 보상 위치 설정

        string levelUpText =
            reward.LeveledUp
                ? $" · LEVEL UP! Lv.{reward.CurrentCharacterLevel}"
                : string.Empty; // 레벨업 문구 생성

        rewardText.text =
            $"{reward.EncounterName} 클리어 보상\n" +
            $"캐릭터 EXP +{reward.CharacterExperience}" +
            $"{levelUpText}\n" +
            $"Gold +{reward.Gold} · " +
            $"나사 +{reward.Screw} · " +
            $"철판 +{reward.IronPlate} · " +
            $"전선 +{reward.Wire}"; // 마지막 보상 문구 지정
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment) // TMP 텍스트 생성
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // TMP 텍스트 오브젝트 생성

        textObject.transform.SetParent(
            parent,
            false); // 부모 Canvas 연결

        TMP_Text text =
            textObject.GetComponent<TMP_Text>(); // TMP_Text 조회

        text.font =
            ProjectCFontProvider.KoreanFontAsset; // 프로젝트 한글 폰트 지정

        text.fontSize = fontSize; // 폰트 크기 지정
        text.color = Color.white; // 텍스트 색상 지정
        text.alignment = alignment; // 텍스트 정렬 지정
        text.raycastTarget = false; // UI Raycast 비활성화

        return text;
    }

    private static void EnsureRuntimeSquareSprite() // 플레이어 임시 스프라이트 준비
    {
        if (runtimeSquareSprite != null)
        {
            return;
        }

        Texture2D texture =
            Texture2D.whiteTexture; // 기본 흰색 Texture 조회

        runtimeSquareSprite =
            Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f); // 런타임 사각형 Sprite 생성
    }
}
