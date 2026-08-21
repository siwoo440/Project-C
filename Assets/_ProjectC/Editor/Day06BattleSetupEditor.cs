using TMPro; // 텍스트 메시 기능 사용
using UnityEditor; // 유니티 편집기 기능 사용
using UnityEditor.SceneManagement; // 편집기 씬 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.SceneManagement; // 씬 자료형 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public static class Day06BattleSetupEditor // 6일차 전투 화면 자동 구성
{ // 클래스 시작
    private const string BattleScenePath = "Assets/_ProjectC/Scenes/40_Battle.unity"; // 전투 씬 경로
    private const string UnitPrefabFolderPath = "Assets/_ProjectC/Prefabs/Common"; // 유닛 프리팹 폴더 경로
    private const string UnitPrefabPath = "Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"; // 유닛 프리팹 경로
    private const string BattleLoadoutPath = "Assets/_ProjectC/ScriptableObjects/BattleLoadouts/BattleLoadout_Test.asset"; // 테스트 전투 편성 경로
    private const string EnemyDataPath = "Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset"; // 테스트 적 경로
    [MenuItem("Tools/Project C/Day 06/Build Battle Scene")] // 자동 구성 메뉴 등록
    public static void BuildBattleScene() // 전투 화면 자동 구성
    { // 자동 구성 시작
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) // 현재 씬 저장 여부 확인
        { // 저장 취소 처리 시작
            return; // 자동 구성 중단
        } // 저장 취소 처리 종료
        SceneAsset battleSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath); // 전투 씬 에셋 조회
        if (battleSceneAsset == null) // 전투 씬 존재 확인
        { // 씬 누락 처리 시작
            Debug.LogError($"[Day06BattleSetupEditor] 전투 씬을 찾을 수 없습니다: {BattleScenePath}"); // 씬 누락 출력
            return; // 자동 구성 중단
        } // 씬 누락 처리 종료
        Scene battleScene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single); // 전투 씬 열기
        Canvas battleCanvas = GetOrCreateBattleCanvas(); // 전투 캔버스 준비
        GetOrCreateCardAreaRoot(battleCanvas.transform); // 하단 카드 예약 영역 준비
        RectTransform allyUnitRoot = GetOrCreateUnitRoot(battleCanvas.transform, "AllyUnitRoot", true); // 아군 배치 영역 준비
        RectTransform enemyUnitRoot = GetOrCreateUnitRoot(battleCanvas.transform, "EnemyUnitRoot", false); // 적 배치 영역 준비
        BattleUnitView unitViewPrefab = CreateOrUpdateUnitPrefab(); // 전투 유닛 프리팹 준비
        if (unitViewPrefab == null) // 프리팹 생성 결과 확인
        { // 프리팹 오류 처리 시작
            Debug.LogError("[Day06BattleSetupEditor] 전투 유닛 프리팹 생성에 실패했습니다."); // 프리팹 오류 출력
            return; // 자동 구성 중단
        } // 프리팹 오류 처리 종료
        BattleSceneSetup battleSceneSetup = GetOrCreateBattleSceneSetup(battleScene); // 전투 초기화 오브젝트 준비
        if (!AssignBattleSceneSetup(battleSceneSetup, unitViewPrefab, allyUnitRoot, enemyUnitRoot)) // 초기화 참조 연결 확인
        { // 연결 오류 처리 시작
            return; // 자동 구성 중단
        } // 연결 오류 처리 종료
        EditorSceneManager.MarkSceneDirty(battleScene); // 전투 씬 변경 표시
        EditorSceneManager.SaveScene(battleScene); // 전투 씬 저장
        AssetDatabase.SaveAssets(); // 생성 에셋 저장
        AssetDatabase.Refresh(); // 프로젝트 에셋 갱신
        Selection.activeGameObject = battleSceneSetup.gameObject; // 전투 초기화 오브젝트 선택
        EditorGUIUtility.PingObject(battleSceneSetup.gameObject); // 생성 오브젝트 강조
        Debug.Log("[Day06BattleSetupEditor] Canvas, HP UI, 전투 유닛 프리팹, 테스트 데이터 연결을 완료했습니다."); // 자동 구성 완료 출력
    } // 자동 구성 종료
    private static Canvas GetOrCreateBattleCanvas() // 전투 캔버스 준비
    { // 캔버스 준비 시작
        Canvas[] existingCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 기존 캔버스 조회
        Canvas battleCanvas = existingCanvases.Length > 0 ? existingCanvases[0] : null; // 사용할 캔버스 선택
        if (battleCanvas == null) // 기존 캔버스 없음 확인
        { // 캔버스 생성 시작
            GameObject canvasObject = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 캔버스 오브젝트 생성
            battleCanvas = canvasObject.GetComponent<Canvas>(); // 캔버스 컴포넌트 조회
        } // 캔버스 생성 종료
        battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 전체 캔버스 설정
        CanvasScaler canvasScaler = battleCanvas.GetComponent<CanvasScaler>(); // 캔버스 스케일러 조회
        if (canvasScaler == null) // 스케일러 누락 확인
        { // 스케일러 추가 시작
            canvasScaler = battleCanvas.gameObject.AddComponent<CanvasScaler>(); // 스케일러 추가
        } // 스케일러 추가 종료
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준 스케일 설정
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로세로 혼합 설정
        canvasScaler.matchWidthOrHeight = 0.5f; // 가로세로 혼합 비율 설정
        canvasScaler.referencePixelsPerUnit = 100f; // 기준 픽셀 단위 설정
        if (battleCanvas.GetComponent<GraphicRaycaster>() == null) // 그래픽 레이캐스터 누락 확인
        { // 레이캐스터 추가 시작
            battleCanvas.gameObject.AddComponent<GraphicRaycaster>(); // 그래픽 레이캐스터 추가
        } // 레이캐스터 추가 종료
        return battleCanvas; // 전투 캔버스 반환
    } // 캔버스 준비 종료
    private static RectTransform GetOrCreateCardAreaRoot(Transform canvasTransform) // 카드 예약 영역 준비
    { // 카드 예약 영역 준비 시작
        Transform existingRoot = canvasTransform.Find("CardAreaRoot"); // 기존 카드 영역 조회
        GameObject rootObject = existingRoot != null ? existingRoot.gameObject : null; // 기존 카드 영역 저장
        if (rootObject == null) // 기존 카드 영역 없음 확인
        { // 카드 영역 생성 시작
            rootObject = new GameObject("CardAreaRoot", typeof(RectTransform)); // 카드 영역 오브젝트 생성
            rootObject.transform.SetParent(canvasTransform, false); // 캔버스 자식 연결
        } // 카드 영역 생성 종료
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 카드 영역 RectTransform 조회
        rootRect.anchorMin = new Vector2(0.03f, 0.03f); // 카드 영역 왼쪽 아래 경계 설정
        rootRect.anchorMax = new Vector2(0.97f, 0.42f); // 카드 영역 오른쪽 위 경계 설정
        rootRect.pivot = new Vector2(0.5f, 0.5f); // 카드 영역 중앙 피벗 설정
        rootRect.anchoredPosition = Vector2.zero; // 카드 영역 위치 초기화
        rootRect.sizeDelta = Vector2.zero; // 카드 영역 여분 크기 제거
        rootRect.SetAsFirstSibling(); // 카드 영역을 유닛보다 뒤에 배치
        return rootRect; // 카드 예약 영역 반환
    } // 카드 예약 영역 준비 종료
    private static RectTransform GetOrCreateUnitRoot(Transform canvasTransform, string rootName, bool isAlly) // 유닛 배치 영역 준비
    { // 배치 영역 준비 시작
        Transform existingRoot = canvasTransform.Find(rootName); // 기존 배치 영역 조회
        GameObject rootObject = existingRoot != null ? existingRoot.gameObject : null; // 기존 오브젝트 저장
        if (rootObject == null) // 기존 배치 영역 없음 확인
        { // 배치 영역 생성 시작
            rootObject = new GameObject(rootName, typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 배치 영역 오브젝트 생성
            rootObject.transform.SetParent(canvasTransform, false); // 캔버스 자식 연결
        } // 배치 영역 생성 종료
        RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 배치 영역 RectTransform 조회
        HorizontalLayoutGroup layoutGroup = rootObject.GetComponent<HorizontalLayoutGroup>(); // 가로 배치 컴포넌트 조회
        if (layoutGroup == null) // 가로 배치 누락 확인
        { // 가로 배치 추가 시작
            layoutGroup = rootObject.AddComponent<HorizontalLayoutGroup>(); // 가로 배치 컴포넌트 추가
        } // 가로 배치 추가 종료
        layoutGroup.padding = new RectOffset(8, 8, 8, 8); // 배치 영역 내부 여백 설정
        layoutGroup.spacing = 12f; // 유닛 사이 간격 설정
        layoutGroup.childAlignment = TextAnchor.MiddleCenter; // 유닛 중앙 정렬 설정
        layoutGroup.childControlWidth = false; // 자식 너비 자동 제어 해제
        layoutGroup.childControlHeight = false; // 자식 높이 자동 제어 해제
        layoutGroup.childForceExpandWidth = false; // 자식 너비 확장 해제
        layoutGroup.childForceExpandHeight = false; // 자식 높이 확장 해제
        if (isAlly) // 아군 배치 영역 확인
        { // 아군 위치 설정 시작
            rootRect.anchorMin = new Vector2(0.03f, 0.47f); // 아군 최소 앵커 설정
            rootRect.anchorMax = new Vector2(0.49f, 0.96f); // 아군 최대 앵커 설정
            rootRect.pivot = new Vector2(0.5f, 0.5f); // 아군 피벗 설정
            rootRect.anchoredPosition = Vector2.zero; // 아군 화면 위치 설정
            rootRect.sizeDelta = Vector2.zero; // 아군 영역 크기 설정
        } // 아군 위치 설정 종료
        else // 적 배치 영역 확인
        { // 적 위치 설정 시작
            rootRect.anchorMin = new Vector2(0.51f, 0.47f); // 적 최소 앵커 설정
            rootRect.anchorMax = new Vector2(0.97f, 0.96f); // 적 최대 앵커 설정
            rootRect.pivot = new Vector2(0.5f, 0.5f); // 적 피벗 설정
            rootRect.anchoredPosition = Vector2.zero; // 적 화면 위치 설정
            rootRect.sizeDelta = Vector2.zero; // 적 영역 크기 설정
        } // 적 위치 설정 종료
        return rootRect; // 배치 영역 반환
    } // 배치 영역 준비 종료
    private static BattleUnitView CreateOrUpdateUnitPrefab() // 유닛 프리팹 생성
    { // 프리팹 생성 시작
        EnsureAssetFolder(UnitPrefabFolderPath); // 프리팹 폴더 확인
        GameObject unitObject = new GameObject("PF_BattleUnit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(BattleUnitView)); // 유닛 루트 생성
        RectTransform unitRect = unitObject.GetComponent<RectTransform>(); // 유닛 RectTransform 조회
        unitRect.sizeDelta = new Vector2(175f, 250f); // 유닛 기본 크기 설정
        Image teamFrameImage = unitObject.GetComponent<Image>(); // 진영 테두리 이미지 조회
        teamFrameImage.sprite = GetDefaultUiSprite(); // 기본 UI 스프라이트 설정
        teamFrameImage.type = Image.Type.Sliced; // 테두리 이미지 방식 설정
        teamFrameImage.color = new Color(0.2f, 0.55f, 1f, 1f); // 기본 아군 색상 설정
        LayoutElement layoutElement = unitObject.GetComponent<LayoutElement>(); // 레이아웃 크기 컴포넌트 조회
        layoutElement.preferredWidth = 175f; // 권장 너비 설정
        layoutElement.preferredHeight = 250f; // 권장 높이 설정
        layoutElement.minWidth = 175f; // 최소 너비 설정
        layoutElement.minHeight = 250f; // 최소 높이 설정
        Image panelImage = CreateImage("Panel", unitObject.transform, new Color(0.07f, 0.07f, 0.09f, 0.96f)); // 내부 배경 생성
        SetStretchRect(panelImage.rectTransform, 6f, 6f, 6f, 6f); // 내부 배경 여백 설정
        Image portraitImage = CreateImage("Portrait", unitObject.transform, new Color(0.25f, 0.25f, 0.28f, 1f)); // 초상화 이미지 생성
        SetCenteredRect(portraitImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -69f), new Vector2(104f, 110f)); // 초상화 위치 설정
        TMP_Text nameText = CreateText("NameText", unitObject.transform, "Unit Name", 20f, Color.white); // 이름 텍스트 생성
        SetCenteredRect(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -137f), new Vector2(155f, 30f)); // 이름 위치 설정
        Slider healthSlider = CreateHealthSlider(unitObject.transform); // 체력 게이지 생성
        SetCenteredRect(healthSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -177f), new Vector2(145f, 22f)); // 체력 게이지 위치 설정
        TMP_Text healthText = CreateText("HealthText", unitObject.transform, "100 / 100", 18f, Color.white); // 체력 숫자 생성
        SetCenteredRect(healthText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -209f), new Vector2(145f, 28f)); // 체력 숫자 위치 설정
        GameObject deadMarker = CreateDeadMarker(unitObject.transform); // 사망 표시 생성
        BattleUnitView unitView = unitObject.GetComponent<BattleUnitView>(); // 유닛 화면 컴포넌트 조회
        SerializedObject unitViewObject = new SerializedObject(unitView); // 유닛 화면 직렬화 객체 생성
        SetObjectReference(unitViewObject, "portraitImage", portraitImage); // 초상화 참조 연결
        SetObjectReference(unitViewObject, "nameText", nameText); // 이름 텍스트 참조 연결
        SetObjectReference(unitViewObject, "teamFrameImage", teamFrameImage); // 진영 테두리 참조 연결
        SetObjectReference(unitViewObject, "healthSlider", healthSlider); // 체력 게이지 참조 연결
        SetObjectReference(unitViewObject, "healthText", healthText); // 체력 숫자 참조 연결
        SetObjectReference(unitViewObject, "deadMarker", deadMarker); // 사망 표시 참조 연결
        unitViewObject.ApplyModifiedPropertiesWithoutUndo(); // 유닛 화면 참조 적용
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(unitObject, UnitPrefabPath); // 유닛 프리팹 저장
        Object.DestroyImmediate(unitObject); // 임시 유닛 오브젝트 제거
        AssetDatabase.SaveAssets(); // 프리팹 에셋 저장
        return prefabAsset != null ? prefabAsset.GetComponent<BattleUnitView>() : null; // 프리팹 컴포넌트 반환
    } // 프리팹 생성 종료
    private static Image CreateImage(string objectName, Transform parent, Color imageColor) // UI 이미지 생성
    { // 이미지 생성 시작
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이미지 오브젝트 생성
        imageObject.transform.SetParent(parent, false); // 부모 오브젝트 연결
        Image image = imageObject.GetComponent<Image>(); // 이미지 컴포넌트 조회
        image.sprite = GetDefaultUiSprite(); // 기본 UI 스프라이트 설정
        image.type = Image.Type.Sliced; // 이미지 표시 방식 설정
        image.color = imageColor; // 이미지 색상 설정
        return image; // 이미지 반환
    } // 이미지 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string initialText, float fontSize, Color textColor) // 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 부모 오브젝트 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.text = initialText; // 초기 문자열 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 텍스트 중앙 정렬
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        return text; // 텍스트 반환
    } // 텍스트 생성 종료
    private static Slider CreateHealthSlider(Transform parent) // 체력 게이지 생성
    { // 체력 게이지 생성 시작
        GameObject sliderObject = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider)); // 슬라이더 루트 생성
        sliderObject.transform.SetParent(parent, false); // 부모 오브젝트 연결
        Image backgroundImage = CreateImage("Background", sliderObject.transform, new Color(0.15f, 0.15f, 0.17f, 1f)); // 게이지 배경 생성
        SetStretchRect(backgroundImage.rectTransform, 0f, 0f, 0f, 0f); // 게이지 배경 채우기
        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform)); // 채우기 영역 생성
        fillAreaObject.transform.SetParent(sliderObject.transform, false); // 채우기 영역 부모 연결
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>(); // 채우기 영역 RectTransform 조회
        SetStretchRect(fillAreaRect, 3f, 3f, 3f, 3f); // 채우기 영역 여백 설정
        Image fillImage = CreateImage("Fill", fillAreaObject.transform, new Color(0.2f, 0.85f, 0.35f, 1f)); // 체력 채우기 이미지 생성
        SetStretchRect(fillImage.rectTransform, 0f, 0f, 0f, 0f); // 체력 채우기 영역 설정
        Slider slider = sliderObject.GetComponent<Slider>(); // 슬라이더 컴포넌트 조회
        slider.fillRect = fillImage.rectTransform; // 채우기 이미지 연결
        slider.targetGraphic = fillImage; // 대상 그래픽 연결
        slider.direction = Slider.Direction.LeftToRight; // 왼쪽부터 채우기 설정
        slider.minValue = 0f; // 최소 체력 설정
        slider.maxValue = 100f; // 임시 최대 체력 설정
        slider.value = 100f; // 임시 현재 체력 설정
        slider.wholeNumbers = true; // 정수 체력 설정
        slider.interactable = false; // 사용자 조작 비활성화
        return slider; // 체력 게이지 반환
    } // 체력 게이지 생성 종료
    private static GameObject CreateDeadMarker(Transform parent) // 사망 표시 생성
    { // 사망 표시 생성 시작
        Image markerImage = CreateImage("DeadMarker", parent, new Color(0.25f, 0f, 0f, 0.78f)); // 사망 배경 생성
        SetStretchRect(markerImage.rectTransform, 6f, 6f, 6f, 6f); // 사망 배경 영역 설정
        TMP_Text markerText = CreateText("DeadText", markerImage.transform, "DEAD", 38f, Color.white); // 사망 텍스트 생성
        SetStretchRect(markerText.rectTransform, 0f, 0f, 0f, 0f); // 사망 텍스트 영역 설정
        markerImage.gameObject.SetActive(false); // 기본 사망 표시 비활성화
        return markerImage.gameObject; // 사망 표시 반환
    } // 사망 표시 생성 종료
    private static BattleSceneSetup GetOrCreateBattleSceneSetup(Scene battleScene) // 전투 초기화 오브젝트 준비
    { // 초기화 오브젝트 준비 시작
        GameObject systemsObject = null; // 전투 시스템 오브젝트 변수
        foreach (GameObject rootObject in battleScene.GetRootGameObjects()) // 씬 루트 오브젝트 순회
        { // 루트 확인 시작
            if (rootObject.name == "BattleSystems") // 전투 시스템 이름 확인
            { // 전투 시스템 발견 처리 시작
                systemsObject = rootObject; // 기존 전투 시스템 저장
                break; // 루트 검색 종료
            } // 전투 시스템 발견 처리 종료
        } // 루트 확인 종료
        if (systemsObject == null) // 기존 전투 시스템 없음 확인
        { // 전투 시스템 생성 시작
            systemsObject = new GameObject("BattleSystems"); // 전투 시스템 오브젝트 생성
            SceneManager.MoveGameObjectToScene(systemsObject, battleScene); // 전투 씬에 오브젝트 배치
        } // 전투 시스템 생성 종료
        BattleSceneSetup battleSceneSetup = systemsObject.GetComponent<BattleSceneSetup>(); // 전투 초기화 컴포넌트 조회
        if (battleSceneSetup == null) // 전투 초기화 컴포넌트 누락 확인
        { // 전투 초기화 추가 시작
            battleSceneSetup = systemsObject.AddComponent<BattleSceneSetup>(); // 전투 초기화 컴포넌트 추가
        } // 전투 초기화 추가 종료
        return battleSceneSetup; // 전투 초기화 컴포넌트 반환
    } // 초기화 오브젝트 준비 종료
    private static bool AssignBattleSceneSetup(BattleSceneSetup battleSceneSetup, BattleUnitView unitViewPrefab, RectTransform allyUnitRoot, RectTransform enemyUnitRoot) // 전투 초기화 참조 연결
    { // 참조 연결 시작
        BattleLoadoutData battleLoadout = AssetDatabase.LoadAssetAtPath<BattleLoadoutData>(BattleLoadoutPath); // 테스트 전투 편성 조회
        EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath); // 테스트 적 데이터 조회
        if (battleLoadout == null) // 테스트 전투 편성 존재 확인
        { // 편성 누락 처리 시작
            Debug.LogError($"[Day06BattleSetupEditor] 테스트 전투 편성을 찾을 수 없습니다: {BattleLoadoutPath}"); // 편성 누락 출력
            return false; // 참조 연결 실패 반환
        } // 편성 누락 처리 종료
        if (enemyData == null) // 테스트 적 존재 확인
        { // 적 누락 처리 시작
            Debug.LogError($"[Day06BattleSetupEditor] 테스트 적을 찾을 수 없습니다: {EnemyDataPath}"); // 적 누락 출력
            return false; // 참조 연결 실패 반환
        } // 적 누락 처리 종료
        SerializedObject setupObject = new SerializedObject(battleSceneSetup); // 전투 초기화 직렬화 객체 생성
        SetObjectReference(setupObject, "battleLoadout", battleLoadout); // 테스트 전투 편성 연결
        SetObjectReference(setupObject, "unitViewPrefab", unitViewPrefab); // 유닛 프리팹 연결
        SetObjectReference(setupObject, "allyUnitRoot", allyUnitRoot); // 아군 배치 영역 연결
        SetObjectReference(setupObject, "enemyUnitRoot", enemyUnitRoot); // 적 배치 영역 연결
        SerializedProperty enemiesProperty = setupObject.FindProperty("enemies"); // 적 목록 속성 조회
        if (enemiesProperty == null) // 적 목록 속성 존재 확인
        { // 적 목록 속성 누락 처리 시작
            Debug.LogError("[Day06BattleSetupEditor] BattleSceneSetup의 enemies 속성을 찾을 수 없습니다."); // 속성 누락 출력
            return false; // 참조 연결 실패 반환
        } // 적 목록 속성 누락 처리 종료
        enemiesProperty.arraySize = 1; // 테스트 적 목록 크기 설정
        enemiesProperty.GetArrayElementAtIndex(0).objectReferenceValue = enemyData; // 테스트 적 데이터 연결
        setupObject.ApplyModifiedPropertiesWithoutUndo(); // 전투 초기화 참조 적용
        EditorUtility.SetDirty(battleSceneSetup); // 전투 초기화 변경 표시
        return true; // 참조 연결 성공 반환
    } // 참조 연결 종료
    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object referenceValue) // 직렬화 참조 설정
    { // 참조 설정 시작
        SerializedProperty property = serializedObject.FindProperty(propertyName); // 대상 속성 조회
        if (property == null) // 대상 속성 존재 확인
        { // 대상 속성 누락 처리 시작
            Debug.LogError($"[Day06BattleSetupEditor] 직렬화 속성을 찾을 수 없습니다: {propertyName}"); // 속성 누락 출력
            return; // 참조 설정 중단
        } // 대상 속성 누락 처리 종료
        property.objectReferenceValue = referenceValue; // 오브젝트 참조 적용
    } // 참조 설정 종료
    private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta) // 중앙 기준 RectTransform 설정
    { // 중앙 RectTransform 설정 시작
        rectTransform.anchorMin = anchor; // 최소 앵커 설정
        rectTransform.anchorMax = anchor; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.anchoredPosition = anchoredPosition; // 기준 위치 설정
        rectTransform.sizeDelta = sizeDelta; // 크기 설정
    } // 중앙 RectTransform 설정 종료
    private static void SetStretchRect(RectTransform rectTransform, float left, float right, float top, float bottom) // 전체 채움 RectTransform 설정
    { // 전체 채움 설정 시작
        rectTransform.anchorMin = Vector2.zero; // 최소 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.offsetMin = new Vector2(left, bottom); // 왼쪽 아래 여백 설정
        rectTransform.offsetMax = new Vector2(-right, -top); // 오른쪽 위 여백 설정
    } // 전체 채움 설정 종료
    private static Sprite GetDefaultUiSprite() // 기본 UI 스프라이트 조회
    { // 스프라이트 조회 시작
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // 기본 UI 스프라이트 반환
    } // 스프라이트 조회 종료
    private static void EnsureAssetFolder(string folderPath) // 에셋 폴더 확인
    { // 폴더 확인 시작
        string[] folderParts = folderPath.Split('/'); // 폴더 경로 분리
        string currentPath = folderParts[0]; // 시작 폴더 설정
        for (int index = 1; index < folderParts.Length; index++) // 하위 폴더 순회
        { // 하위 폴더 처리 시작
            string nextPath = $"{currentPath}/{folderParts[index]}"; // 다음 폴더 경로 생성
            if (!AssetDatabase.IsValidFolder(nextPath)) // 다음 폴더 존재 확인
            { // 폴더 생성 시작
                AssetDatabase.CreateFolder(currentPath, folderParts[index]); // 하위 폴더 생성
            } // 폴더 생성 종료
            currentPath = nextPath; // 현재 폴더 경로 갱신
        } // 하위 폴더 처리 종료
    } // 폴더 확인 종료
} // 클래스 종료
