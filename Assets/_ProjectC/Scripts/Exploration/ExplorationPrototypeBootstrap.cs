using TMPro; // 탐사 HUD 텍스트 사용
using UnityEngine; // 탐사 프로토타입 오브젝트 사용
using UnityEngine.InputSystem; // 디버그 입력 사용
using UnityEngine.UI; // Canvas UI 사용

[DefaultExecutionOrder(-400)]
public sealed class ExplorationPrototypeBootstrap : MonoBehaviour // 44일차 탐사 성공 HUD 및 테스트 연결
{
    private static Sprite runtimeSquareSprite; // 플레이어 런타임 스프라이트
    private TMP_Text floorText; // 현재 층을 크게 표시할 중앙 문구
    private TMP_Text progressText; // 영구 진행 HUD 텍스트
    private TMP_Text explorationSuccessText; // 탐사 성공 결과 HUD 텍스트
    private CharacterProgressionManager progression; // 레벨과 경험치 변경을 알리는 성장 관리자
    private RectTransform hudRect; // 화면 너비를 확인할 탐사 캔버스 영역
    private (RectTransform Rect, Vector2 Offset)[] headerControls; // 좁은 창에 맞출 상단 요소와 원래 좌표
    private float headerScale = -1f; // 상단 요소에 마지막으로 적용한 크기 배율
    private int lastDisplayedFloor = -1; // 마지막 HUD 표시 층
    private bool lastDisplayedCompleted; // 마지막 HUD 탐사 완료 상태

    private void Start() // 탐사 프로토타입 시작
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        progression = CharacterProgressionManager.EnsureInstance(); // 캐릭터 성장 관리자 연결
        progression.ProgressChanged += RefreshProgressText; // 현재 화면에서 레벨과 경험치 즉시 갱신
        CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비
        PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비

        EnsureRuntimeSquareSprite(); // 플레이어 사각형 스프라이트 준비
        CreatePlayer(sessionManager); // 탐사 플레이어 생성
        CreateHud(sessionManager); // 탐사 HUD 생성
    }

    private void Update() // 탐사 디버그 입력 처리
    {
        FitHeader(); // 창 비율에 맞춰 상단 요소의 간격 보존
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 현재 탐사 층 조회

        if (lastDisplayedFloor != sessionManager.CurrentFloor ||
            lastDisplayedCompleted != sessionManager.IsExplorationCompleted)
        {
            RefreshProgressText(); // 층·완료 상태 변경 시 진행 HUD 갱신
            RefreshExplorationSuccessText(); // 탐사 성공 결과 HUD 갱신
        }

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard == null ||
            !keyboard.f8Key.wasPressedThisFrame)
        {
            return;
        }

        bool completed =
            sessionManager.CompleteExplorationSuccess(); // 실제 탐사 성공 처리 흐름 강제 실행

        RefreshProgressText(); // 진행 HUD 갱신
        RefreshExplorationSuccessText(); // 성공 결과 HUD 갱신

        Debug.Log(
            completed
                ? "[Exploration][Day44][DEBUG] F8로 탐사 성공 처리 흐름을 실행했습니다."
                : "[Exploration][Day44][DEBUG] 이미 탐사가 완료되어 F8 성공 처리를 무시했습니다."); // 성공 처리 테스트 로그
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
        playerObject.AddComponent<ExplorationActorVisual>().Bind(spriteRenderer, ExplorationActorStyle.Hero); // 이동 방향을 따라 실제 주인공 원화 선택

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

        var resources = ExplorationResourceHud.Create(canvas, PlayerResourceManager.EnsureInstance()); // 오른쪽 위 재화 아이콘과 숫자 연결
        var inventory = ExplorationInventoryView.Create(canvas, ConsumableRunManager.EnsureInstance().Inventory); // 왼쪽 위의 확대된 소모품 보관함
        var relics = ExplorationRelicWindow.Create(canvas, RelicRunManager.EnsureInstance().Inventory); // 소모품 오른쪽의 유물 버튼과 목록
        RectTransform progressPlate = CreateProgressText(canvasObject.transform); // 중앙의 현재 층과 캐릭터 성장 패널

        CreateLastRewardText(
            canvasObject.transform,
            sessionManager); // 마지막 보상 HUD 생성

        CreateExplorationSuccessText(
            canvasObject.transform); // 탐사 성공 결과 HUD 생성

        ExplorationPlayerController tutorialPlayer = FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        RectTransform prompt = PreparationUi.CreatePanel("ExplorationInteractionPrompt", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)); // 캐릭터 옆 F 키 안내 바탕
        prompt.pivot = new Vector2(0f, 0.5f); // 캐릭터 오른쪽 원화와 맞출 안내 왼쪽 기준
        PreparationUi.Place(prompt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(224f, 52f)); // 창 크기에 비례하는 짧은 안내 너비와 높이
        Image promptArt = prompt.GetComponent<Image>(); // 원래 탐사 UI와 어울리는 안내 바탕 조회
        LobbyUiArt.Apply(promptArt, "Button"); // 기존 금속 테두리와 청록 배경 적용
        promptArt.raycastTarget = false; // 탐사 HUD의 마우스 클릭을 가리지 않는 안내
        TMP_Text promptText = PreparationUi.CreateText("Prompt", prompt, "F : 상호작용", 26f); // 누를 키와 동작을 보여 주는 실제 표시
        promptText.fontStyle = FontStyles.Bold; // 배경과 구분하는 짧은 안내 글자 굵기
        PreparationUi.Stretch(promptText.rectTransform, new Vector2(0.09f, 0f), new Vector2(0.91f, 1f)); // 테두리 장식을 피하는 가운데 문구 영역
        tutorialPlayer.BindInteractionPrompt(prompt); // 캐릭터가 가까워질 때 오른쪽에 표시할 안내 연결
        WorldTutorialProgress tutorial = new WorldTutorialProgress(WorldTutorialKind.Exploration, new PlayerPrefsSettingsStore()); // 기기별 탐사 안내 진행 생성
        WorldTutorialView tutorialView = WorldTutorialView.Create(canvas, tutorial, () => tutorialPlayer != null ? (Vector2)tutorialPlayer.transform.position : Vector2.zero, step => null, true); // 실제 이동을 확인하는 탐사 안내 화면 생성
        tutorialView.BindPlayer(tutorialPlayer); // 맵 자동 배치와 직접 이동 구분 연결
        hudRect = (RectTransform)canvas.transform; // 상단 요소의 사용 가능 너비 확인
        RectTransform[] controls = { (RectTransform)inventory.transform, relics.ToggleRect, tutorialView.ReplayRect, progressPlate, (RectTransform)resources.transform }; // 각 상단 영역의 원래 부모 보존
        headerControls = new (RectTransform, Vector2)[controls.Length]; // 기준 위치를 저장할 상단 요소 목록
        for (int index = 0; index < controls.Length; index++) // 창을 줄이기 전의 위치와 영역 순회
        { // 상단 기준 위치 저장 시작
            headerControls[index] = (controls[index], controls[index].anchoredPosition); // 원래 기준 해상도의 위치 저장
        } // 상단 기준 위치 저장 종료
        FitHeader(); // 처음 표시할 화면 크기에 맞춰 배율 적용
        tutorial.StartIfNeeded(GraphicsAudioSettingsRuntime.Current.GetIndex("tutorial") == 0, tutorialPlayer != null ? (Vector2)tutorialPlayer.transform.position : Vector2.zero); // 사용자 설정에 따른 탐사 안내 자동 시작
    }

    private RectTransform CreateProgressText(Transform parent) // 탐사 상단 중앙의 층과 경험치 패널 생성
    { // 중앙 상태판 구성 시작
        var anchor = new Vector2(0.515f, 1f); // 참고 화면의 상단 중앙 위치
        RectTransform plate = PreparationUi.CreatePanel("ExplorationProgressPlate", parent, anchor, anchor); // 층과 성장 정보가 들어갈 패널
        PreparationUi.Place(plate, anchor, new Vector2(0f, -48f), new Vector2(244f, 80f)); // 참고 영역의 위치와 크기 적용
        Image image = plate.GetComponent<Image>(); // 중앙 패널 바탕 이미지 조회
        image.sprite = Resources.Load<Sprite>("UI/Lobby/ExplorationProgressPlate"); // 제작한 회색 금속 패널 원화 연결
        image.color = image.sprite != null ? Color.white : new Color(0.17f, 0.18f, 0.19f, 0.96f); // 원화 색상 또는 회색 대체 배경
        image.raycastTarget = false; // 상태판을 통한 탐사 입력 허용
        floorText = PreparationUi.CreateText("Floor", plate, string.Empty, 39f); // 흰색 큰 층 번호 표시
        floorText.fontStyle = FontStyles.Bold; // 현재 층 글자의 굵은 스타일
        floorText.color = Color.white; // 회색 바탕의 흰색 층 번호
        floorText.textWrappingMode = TextWrappingModes.NoWrap; // 여러 자리 층 번호의 줄바꿈 방지
        PreparationUi.Stretch(floorText.rectTransform, new Vector2(0.07f, 0.43f), new Vector2(0.93f, 0.97f)); // 상태판 윗줄의 층 번호 영역
        progressText = PreparationUi.CreateText("PersistentProgress", plate, string.Empty, 20f); // 레벨과 경험치의 별도 아랫줄
        progressText.textWrappingMode = TextWrappingModes.NoWrap; // 좁은 해상도에서 성장 정보 한 줄 유지
        PreparationUi.Stretch(progressText.rectTransform, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.46f)); // 상태판 아랫줄의 성장 영역
        RefreshProgressText(); // 현재 층과 성장 정보 최초 표시
        return plate; // 좁은 창에 맞출 중앙 상태판 반환
    } // 중앙 상태판 구성 종료

    private void FitHeader() // 상단 요소를 한 화면에 유지할 가로 공간 확인
    { // 좁은 화면 대응 시작
        if (hudRect == null || headerControls == null || hudRect.rect.width <= 0f) // 실제 캔버스 크기 준비 여부 확인
        { // 배치 대기 시작
            return; // 캔버스 초기화가 끝날 때까지 기존 위치 유지
        } // 배치 대기 종료
        float scale = Mathf.Min(1f, hudRect.rect.width / 1920f); // 기준보다 좁을 때만 상단을 균등 축소
        if (Mathf.Approximately(scale, headerScale)) // 직전과 같은 가로 공간 확인
        { // 중복 갱신 생략 시작
            return; // 변경 없는 RectTransform 쓰기 생략
        } // 중복 갱신 생략 종료
        headerScale = scale; // 적용할 가로 배율 저장
        foreach (var control in headerControls) // 미리 저장한 다섯 상단 요소 순회
        { // 각 요소의 배율 반영 시작
            control.Rect.localScale = new Vector3(scale, scale, 1f); // 이미지와 텍스트의 비율 유지
            control.Rect.anchoredPosition = control.Offset * scale; // 각 기준점 주변의 여백 유지
        } // 각 요소의 배율 반영 종료
    } // 좁은 화면 대응 종료

    private void RefreshProgressText() // 영구 진행 HUD 갱신
    {
        if (progressText == null)
        {
            return;
        }

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비
        floorText.text = $"F{sessionManager.CurrentFloor}"; // 탐사 접두어 없이 현재 층 번호 표시
        ProjectCLanguageDisplay.SetCaption(progressText, $"캐릭터 Lv.{progression.Level}  EXP {progression.CurrentExperience}/{progression.RequiredExperience}"); // 현재 언어의 레벨과 경험치 표시

        lastDisplayedFloor =
            sessionManager.CurrentFloor; // 현재 HUD 표시 층 저장

        lastDisplayedCompleted =
            sessionManager.IsExplorationCompleted; // 현재 HUD 완료 상태 저장
    }

    private void OnDestroy() // 탐사 화면을 떠날 때 성장 알림 정리
    { // HUD 성장 알림 구독 해제 시작
        if (progression != null) // 연결한 성장 관리자 존재 확인
        { // 성장 알림 정리 시작
            progression.ProgressChanged -= RefreshProgressText; // 종료된 화면의 경험치 갱신 차단
        } // 성장 알림 정리 종료
    } // HUD 성장 알림 구독 해제 종료

    private void CreateExplorationSuccessText(
        Transform parent) // 탐사 성공 결과 HUD 생성
    {
        explorationSuccessText =
            CreateText(
                "ExplorationSuccess",
                parent,
                36f,
                TextAlignmentOptions.Center); // 탐사 성공 결과 텍스트 생성

        RectTransform successRect =
            explorationSuccessText.rectTransform; // 성공 결과 RectTransform 조회

        successRect.anchorMin =
            new Vector2(0.5f, 0.5f); // 화면 중앙 최소 앵커 설정

        successRect.anchorMax =
            new Vector2(0.5f, 0.5f); // 화면 중앙 최대 앵커 설정

        successRect.pivot =
            new Vector2(0.5f, 0.5f); // 화면 중앙 Pivot 설정

        successRect.sizeDelta =
            new Vector2(1000f, 300f); // 성공 결과 영역 크기 설정

        successRect.anchoredPosition =
            Vector2.zero; // 화면 중앙 위치 설정

        RefreshExplorationSuccessText(); // 성공 결과 최초 갱신
    }

    private void RefreshExplorationSuccessText() // 탐사 성공 결과 HUD 갱신
    {
        if (explorationSuccessText == null)
        {
            return;
        }

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 성공 상태 조회

        if (!sessionManager.IsExplorationCompleted ||
            !sessionManager.IsExplorationSuccess)
        {
            explorationSuccessText.text = string.Empty; // 진행 중에는 성공 결과 숨김
            return;
        }

        explorationSuccessText.text =
            "탐사 성공\n\n" +
            $"완료 층 {sessionManager.CompletedFloor}F\n" +
            $"클리어 조우 {sessionManager.CompletedEncounterCount}개\n" +
            $"호감도 +{sessionManager.LastExplorationSuccessAffinity}\n\n" +
            "추가 조우와 다음 층 이동이 종료되었습니다."; // 탐사 성공 결과 표시
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
        string relicText = reward.CoreRelic != null ? $" · 유물 [{reward.CoreRelic.DisplayName}]" : string.Empty; // 실제 침식핵 유물 획득 문구

        rewardText.text =
            $"{reward.EncounterName} 클리어 보상\n" +
            $"캐릭터 EXP +{reward.CharacterExperience}" +
            $"{levelUpText}\n" +
            $"Gold +{reward.Gold} · " +
            $"나사 +{reward.Screw} · " +
            $"철판 +{reward.IronPlate} · " +
            $"전선 +{reward.Wire}{relicText}"; // 기존 재료와 추가 유물 보상 표시
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
