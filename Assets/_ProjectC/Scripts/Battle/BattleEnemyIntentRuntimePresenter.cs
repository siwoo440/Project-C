using System.Collections.Generic; // 행동 아이콘 관리 자료형 사용
using TMPro; // 화살표 옵션 버튼 텍스트 사용
using UnityEngine; // 런타임 UI와 IMGUI 기능 사용
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능 사용
using UnityEngine.UI; // 화살표 옵션 버튼 사용

[DefaultExecutionOrder(900)]
public sealed class BattleEnemyIntentRuntimePresenter : MonoBehaviour // 적 행동 아이콘·타겟 화살표 관리자
{
    private const string BattleSceneName = "40_Battle"; // 전투 Scene 이름

    private enum ArrowDisplayMode // 적 행동 화살표 표시 방식
    {
        All, // 모든 행동 화살표 표시
        Single // 한 행동 화살표만 표시
    }

    private static BattleEnemyIntentRuntimePresenter instance; // 행동 표시 관리자 인스턴스

    private readonly Dictionary<BattleUnitRuntime, BattleEnemyIntentIconView> iconViews =
        new Dictionary<BattleUnitRuntime, BattleEnemyIntentIconView>(); // 적별 행동 아이콘 화면

    private BattleSceneSetup battleSceneSetup; // 현재 전투 설정
    private Button arrowModeButton; // 화살표 표시 옵션 버튼
    private TMP_Text arrowModeButtonText; // 화살표 표시 옵션 문구
    private ArrowDisplayMode arrowDisplayMode = ArrowDisplayMode.All; // 기본 모든 화살표 표시
    private bool arrowButtonAligned; // 현재 강화 버튼 옆 정렬 여부

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 행동 표시 관리자 자동 생성
    {
        if (instance != null)
        {
            return;
        }

        GameObject presenterObject =
            new GameObject(nameof(BattleEnemyIntentRuntimePresenter)); // 행동 표시 관리자 오브젝트 생성

        instance =
            presenterObject.AddComponent<BattleEnemyIntentRuntimePresenter>(); // 행동 표시 관리자 추가

        DontDestroyOnLoad(presenterObject); // Scene 이동 중 관리자 유지
    }

    private void Awake() // 행동 표시 관리자 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 관리자 제거
            return;
        }

        instance = this; // 현재 관리자 등록
        DontDestroyOnLoad(gameObject); // Scene 이동 중 관리자 유지
    }

    private void LateUpdate() // 행동 아이콘·분석·화살표 UI 갱신
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName)
        {
            iconViews.Clear(); // Scene 변경 시 이전 적 캐시 제거
            battleSceneSetup = null; // 이전 전투 설정 제거
            arrowModeButton = null; // 이전 화살표 버튼 참조 제거
            arrowModeButtonText = null; // 이전 화살표 문구 참조 제거
            arrowButtonAligned = false; // 버튼 정렬 상태 초기화
            return;
        }

        if (battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized)
        {
            battleSceneSetup =
                FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투 설정 탐색
        }

        if (battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized ||
            battleSceneSetup.EnemyActionRuntime == null)
        {
            return;
        }

        Canvas battleCanvas =
            FindBattleCanvas(); // 현재 전투 Canvas 조회

        EnsureArrowModeButton(
            battleCanvas); // 화살표 전체·하나씩 옵션 버튼 준비

        BattleUnitView[] unitViews =
            FindObjectsByType<BattleUnitView>(
                FindObjectsSortMode.None); // 현재 전투 유닛 화면 조회

        EnsureEnemyAnalysisHandlers(
            unitViews); // 적 본체 클릭 정보 처리기 연결

        HideLegacyIntentViews(
            unitViews); // 기존 장문 행동 예고 UI 숨김

        IReadOnlyList<BattleEnemyAction> plannedActions =
            battleSceneSetup.EnemyActionRuntime.PlannedActions; // 현재 적 예정 행동 조회

        HashSet<BattleUnitRuntime> activeActors =
            new HashSet<BattleUnitRuntime>(); // 현재 행동이 있는 적 목록

        for (int index = 0;
             index < plannedActions.Count;
             index += 1)
        {
            BattleEnemyAction action =
                plannedActions[index]; // 현재 예정 행동 조회

            if (action == null ||
                action.Actor == null ||
                action.Actor.IsDead)
            {
                continue;
            }

            BattleUnitView actorView =
                FindUnitView(
                    unitViews,
                    action.Actor); // 행동 적 화면 조회

            if (actorView == null)
            {
                continue;
            }

            activeActors.Add(
                action.Actor); // 현재 행동 적 등록

            BattleEnemyIntentIconView iconView =
                EnsureIconView(
                    action.Actor,
                    actorView); // 적 행동 아이콘 준비

            bool specialPattern =
                IsBossSpecialPattern(
                    action); // 보스 특수 패턴 여부 판정

            iconView.Bind(
                action,
                actorView,
                specialPattern); // 행동 아이콘 데이터 갱신
        }

        foreach (KeyValuePair<BattleUnitRuntime, BattleEnemyIntentIconView> pair in iconViews)
        {
            if (pair.Value == null)
            {
                continue;
            }

            pair.Value.gameObject.SetActive(
                pair.Key != null &&
                activeActors.Contains(
                    pair.Key)); // 행동 없는 적 아이콘 숨김
        }

        if (BattleEnemyIntentDetailView.Instance != null)
        {
            BattleEnemyIntentDetailView.Instance.ValidateActions(
                plannedActions); // 고정 행동 정보 유효성 갱신
        }

        if (BattleEnemyAnalysisView.Instance != null)
        {
            BattleEnemyAnalysisView.Instance.ValidateSelectedUnit(); // 클릭 적 정보 실시간 갱신
        }
    }

    private void OnGUI() // 적 행동 대상 화살표 출력
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName ||
            battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized ||
            battleSceneSetup.EnemyActionRuntime == null)
        {
            return;
        }

        IReadOnlyList<BattleEnemyAction> plannedActions =
            battleSceneSetup.EnemyActionRuntime.PlannedActions; // 현재 예정 행동 조회

        BattleEnemyAction singleAction =
            arrowDisplayMode == ArrowDisplayMode.Single
                ? SelectSingleArrowAction(
                    plannedActions)
                : null; // 하나씩 모드에서 표시할 행동 선택

        BattleUnitView[] unitViews =
            FindObjectsByType<BattleUnitView>(
                FindObjectsSortMode.None); // 현재 전투 유닛 화면 조회

        for (int index = 0;
             index < plannedActions.Count;
             index += 1)
        {
            BattleEnemyAction action =
                plannedActions[index]; // 현재 행동 조회

            if (action == null ||
                action.Actor == null ||
                action.Target == null ||
                action.Actor.IsDead ||
                action.Target.IsDead)
            {
                continue;
            }

            if (arrowDisplayMode == ArrowDisplayMode.Single &&
                action != singleAction)
            {
                continue; // 하나씩 모드의 비선택 행동 화살표 숨김
            }

            BattleUnitView actorView =
                FindUnitView(
                    unitViews,
                    action.Actor); // 행동 적 화면 조회

            BattleUnitView targetView =
                FindUnitView(
                    unitViews,
                    action.Target); // 행동 대상 화면 조회

            if (actorView == null ||
                targetView == null)
            {
                continue;
            }

            bool specialPattern =
                IsBossSpecialPattern(
                    action); // 특수 패턴 여부 확인

            bool focused =
                BattleEnemyIntentDetailView.Instance != null &&
                BattleEnemyIntentDetailView.Instance.IsFocused(
                    action); // 현재 행동 설명 선택 여부 확인

            DrawTargetArrow(
                actorView,
                targetView,
                specialPattern,
                focused); // 행동 적에서 대상까지 화살표 출력
        }
    }

    private BattleEnemyAction SelectSingleArrowAction(
        IReadOnlyList<BattleEnemyAction> plannedActions) // 하나씩 모드 표시 행동 선택
    {
        if (plannedActions == null ||
            plannedActions.Count < 1)
        {
            return null;
        }

        if (BattleEnemyIntentDetailView.Instance != null)
        {
            for (int index = 0;
                 index < plannedActions.Count;
                 index += 1)
            {
                BattleEnemyAction action =
                    plannedActions[index]; // 현재 행동 조회

                if (BattleEnemyIntentDetailView.Instance.IsFocused(
                        action))
                {
                    return action; // 마우스 오버·고정된 행동의 화살표 우선 표시
                }
            }
        }

        for (int index = 0;
             index < plannedActions.Count;
             index += 1)
        {
            BattleEnemyAction action =
                plannedActions[index]; // 기본 표시 후보 조회

            if (action != null &&
                action.Actor != null &&
                action.Target != null &&
                !action.Actor.IsDead &&
                !action.Target.IsDead)
            {
                return action; // 선택 행동이 없으면 첫 행동 화살표 표시
            }
        }

        return null; // 표시 가능한 행동 없음 반환
    }

    private void EnsureEnemyAnalysisHandlers(
        BattleUnitView[] unitViews) // 적 본체 클릭 분석 처리기 연결
    {
        for (int index = 0;
             index < unitViews.Length;
             index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView == null ||
                unitView.RuntimeUnit == null ||
                unitView.RuntimeUnit.Team != BattleTeam.Enemy)
            {
                continue;
            }

            BattleEnemyAnalysisClickHandler clickHandler =
                unitView.GetComponent<BattleEnemyAnalysisClickHandler>(); // 기존 적 분석 클릭 처리기 조회

            if (clickHandler == null)
            {
                clickHandler =
                    unitView.gameObject.AddComponent<BattleEnemyAnalysisClickHandler>(); // 적 분석 클릭 처리기 추가
            }

            clickHandler.Initialize(
                unitView); // 현재 적 화면 연결
        }
    }

    private void EnsureArrowModeButton(
        Canvas battleCanvas) // 화살표 표시 옵션 버튼 준비
    {
        if (battleCanvas == null)
        {
            return;
        }

        if (arrowModeButton == null)
        {
            GameObject buttonObject =
                new GameObject(
                    "Day47ArrowModeButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)); // 화살표 옵션 버튼 생성

            buttonObject.transform.SetParent(
                battleCanvas.transform,
                false); // 전투 Canvas에 버튼 배치

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회

            buttonRect.anchorMin =
                new Vector2(
                    1f,
                    1f); // 오른쪽 위 앵커 설정

            buttonRect.anchorMax =
                buttonRect.anchorMin; // 앵커 고정

            buttonRect.pivot =
                new Vector2(
                    1f,
                    1f); // 오른쪽 위 피벗 설정

            buttonRect.anchoredPosition =
                new Vector2(
                    -205f,
                    -8f); // 현재 강화 버튼 왼쪽 기본 위치 설정

            buttonRect.sizeDelta =
                new Vector2(
                    130f,
                    30f); // 화살표 옵션 버튼 크기 설정

            Image buttonImage =
                buttonObject.GetComponent<Image>(); // 버튼 배경 조회

            buttonImage.color =
                new Color(
                    0.14f,
                    0.2f,
                    0.31f,
                    1f); // 버튼 배경 색상 설정

            arrowModeButton =
                buttonObject.GetComponent<Button>(); // 화살표 옵션 Button 조회

            arrowModeButton.onClick.AddListener(
                ToggleArrowDisplayMode); // 화살표 표시 방식 전환 연결

            GameObject textObject =
                new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)); // 버튼 문구 오브젝트 생성

            textObject.transform.SetParent(
                buttonObject.transform,
                false); // 버튼에 문구 배치

            arrowModeButtonText =
                textObject.GetComponent<TMP_Text>(); // 버튼 문구 TMP 조회

            arrowModeButtonText.fontSize = 14f; // 버튼 문구 글자 크기 설정
            arrowModeButtonText.alignment = TextAlignmentOptions.Center; // 버튼 문구 중앙 정렬
            arrowModeButtonText.color = Color.white; // 버튼 문구 색상 설정
            arrowModeButtonText.raycastTarget = false; // 문구 포인터 차단 해제
            arrowModeButtonText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용

            RectTransform textRect =
                arrowModeButtonText.rectTransform; // 버튼 문구 RectTransform 조회

            textRect.anchorMin = Vector2.zero; // 최소 앵커 설정
            textRect.anchorMax = Vector2.one; // 최대 앵커 설정
            textRect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
            textRect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거

            RefreshArrowModeButtonText(); // 기본 버튼 문구 적용
            arrowButtonAligned = false; // 현재 강화 버튼 탐색 대기
        }

        if (!arrowButtonAligned)
        {
            arrowButtonAligned =
                TryAlignArrowButtonToUpgradeButton(); // 현재 강화 버튼 왼쪽으로 자동 정렬 시도
        }
    }

    private bool TryAlignArrowButtonToUpgradeButton() // 현재 강화 버튼 기준 화살표 옵션 버튼 정렬
    {
        if (arrowModeButton == null)
        {
            return false;
        }

        Button[] buttons =
            FindObjectsByType<Button>(
                FindObjectsSortMode.None); // 현재 전투 버튼 전체 조회

        for (int index = 0;
             index < buttons.Length;
             index += 1)
        {
            Button candidate =
                buttons[index]; // 현재 버튼 후보 조회

            if (candidate == null ||
                candidate == arrowModeButton)
            {
                continue;
            }

            TMP_Text candidateText =
                candidate.GetComponentInChildren<TMP_Text>(
                    true); // 버튼 문구 조회

            if (candidateText == null ||
                string.IsNullOrWhiteSpace(candidateText.text) ||
                !candidateText.text.Contains(
                    "현재 강화"))
            {
                continue;
            }

            RectTransform sourceRect =
                candidate.transform as RectTransform; // 현재 강화 버튼 RectTransform 조회

            RectTransform targetRect =
                arrowModeButton.transform as RectTransform; // 화살표 버튼 RectTransform 조회

            if (sourceRect == null ||
                targetRect == null)
            {
                continue;
            }

            targetRect.SetParent(
                sourceRect.parent,
                false); // 현재 강화 버튼과 같은 부모로 이동

            targetRect.anchorMin =
                sourceRect.anchorMin; // 현재 강화 버튼 앵커 복사

            targetRect.anchorMax =
                sourceRect.anchorMax; // 현재 강화 버튼 앵커 복사

            targetRect.pivot =
                sourceRect.pivot; // 현재 강화 버튼 피벗 복사

            float targetWidth =
                Mathf.Max(
                    130f,
                    sourceRect.rect.width); // 옵션 버튼 최소 너비 보정

            float targetHeight =
                Mathf.Max(
                    28f,
                    sourceRect.rect.height); // 옵션 버튼 최소 높이 보정

            targetRect.sizeDelta =
                new Vector2(
                    targetWidth,
                    targetHeight); // 현재 강화 버튼 높이에 맞춰 크기 설정

            targetRect.anchoredPosition =
                sourceRect.anchoredPosition +
                new Vector2(
                    -(sourceRect.rect.width + targetWidth) * 0.5f - 8f,
                    0f); // 현재 강화 버튼 바로 왼쪽에 배치

            targetRect.SetAsLastSibling(); // 화살표 옵션 버튼 최상단 표시

            return true; // 현재 강화 버튼 기준 정렬 성공
        }

        return false; // 현재 강화 버튼 아직 탐색되지 않음
    }

    private void ToggleArrowDisplayMode() // 화살표 전체·하나씩 표시 전환
    {
        arrowDisplayMode =
            arrowDisplayMode == ArrowDisplayMode.All
                ? ArrowDisplayMode.Single
                : ArrowDisplayMode.All; // 표시 방식 토글

        RefreshArrowModeButtonText(); // 버튼 문구 갱신
    }

    private void RefreshArrowModeButtonText() // 화살표 옵션 버튼 문구 갱신
    {
        if (arrowModeButtonText == null)
        {
            return;
        }

        arrowModeButtonText.text =
            arrowDisplayMode == ArrowDisplayMode.All
                ? "화살표 : 전체"
                : "화살표 : 하나씩"; // 현재 표시 방식 문구 적용
    }

    private Canvas FindBattleCanvas() // 전투 UI Canvas 조회
    {
        if (battleSceneSetup == null)
        {
            return null;
        }

        BattleUnitView[] unitViews =
            FindObjectsByType<BattleUnitView>(
                FindObjectsSortMode.None); // 현재 유닛 화면 조회

        for (int index = 0;
             index < unitViews.Length;
             index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView == null)
            {
                continue;
            }

            Canvas canvas =
                unitView.GetComponentInParent<Canvas>(); // 유닛 소속 Canvas 조회

            if (canvas != null)
            {
                return canvas; // 첫 전투 Canvas 반환
            }
        }

        return null; // 전투 Canvas 없음 반환
    }

    private BattleEnemyIntentIconView EnsureIconView(
        BattleUnitRuntime actor,
        BattleUnitView actorView) // 적 행동 아이콘 준비
    {
        if (iconViews.TryGetValue(
                actor,
                out BattleEnemyIntentIconView existingIcon) &&
            existingIcon != null)
        {
            existingIcon.gameObject.SetActive(
                true); // 기존 아이콘 재활성화

            return existingIcon;
        }

        GameObject iconObject =
            new GameObject(
                "Day47IntentIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)); // 행동 아이콘 오브젝트 생성

        iconObject.transform.SetParent(
            actorView.transform,
            false); // 적 UI에 행동 아이콘 배치

        BattleEnemyIntentIconView iconView =
            iconObject.AddComponent<BattleEnemyIntentIconView>(); // 행동 아이콘 상호작용 추가

        iconViews[actor] =
            iconView; // 적별 아이콘 등록

        return iconView; // 생성 아이콘 반환
    }

    private static void HideLegacyIntentViews(
        BattleUnitView[] unitViews) // 기존 텍스트 행동 예고 숨김
    {
        for (int index = 0;
             index < unitViews.Length;
             index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView == null ||
                unitView.RuntimeUnit == null ||
                unitView.RuntimeUnit.Team != BattleTeam.Enemy)
            {
                continue;
            }

            Transform legacyIntent =
                unitView.transform.Find(
                    "EnemyIntent"); // 기존 행동 예고 오브젝트 탐색

            if (legacyIntent != null &&
                legacyIntent.gameObject.activeSelf)
            {
                legacyIntent.gameObject.SetActive(
                    false); // 기존 장문 행동 예고 숨김
            }
        }
    }

    private bool IsBossSpecialPattern(
        BattleEnemyAction action) // 보스 특수 패턴 표시 여부 판정
    {
        if (action == null ||
            battleSceneSetup == null ||
            battleSceneSetup.BattleTurn == null ||
            battleSceneSetup.BattleTurn.BattleType != BattleType.Boss)
        {
            return false;
        }

        return action.PatternCount > 1 &&
               action.PatternIndex == action.PatternCount; // 보스 패턴 순환 마지막 행동을 특수 예고로 표시
    }

    private static BattleUnitView FindUnitView(
        BattleUnitView[] unitViews,
        BattleUnitRuntime runtimeUnit) // 런타임 유닛 화면 조회
    {
        if (runtimeUnit == null)
        {
            return null;
        }

        for (int index = 0;
             index < unitViews.Length;
             index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView != null &&
                unitView.RuntimeUnit == runtimeUnit)
            {
                return unitView; // 일치 유닛 화면 반환
            }
        }

        return null; // 일치 유닛 화면 없음 반환
    }

    private static void DrawTargetArrow(
        BattleUnitView actorView,
        BattleUnitView targetView,
        bool specialPattern,
        bool focused) // 적 행동 대상 화살표 출력
    {
        RectTransform actorRect =
            actorView.transform as RectTransform; // 행동 적 RectTransform 조회

        RectTransform targetRect =
            targetView.transform as RectTransform; // 대상 RectTransform 조회

        if (actorRect == null ||
            targetRect == null)
        {
            return;
        }

        Canvas canvas =
            actorView.GetComponentInParent<Canvas>(); // 전투 Canvas 조회

        Camera canvasCamera =
            canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null; // Canvas 렌더 카메라 조회

        Vector3 actorWorld =
            actorRect.TransformPoint(
                new Vector3(
                    0f,
                    actorRect.rect.yMax + 42f,
                    0f)); // 행동 아이콘 근처 시작 위치 계산

        Vector3 targetWorld =
            targetRect.TransformPoint(
                new Vector3(
                    0f,
                    targetRect.rect.yMax + 18f,
                    0f)); // 대상 머리 위 종료 위치 계산

        Vector2 actorScreen =
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                actorWorld); // 행동 적 화면 좌표 계산

        Vector2 targetScreen =
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                targetWorld); // 대상 화면 좌표 계산

        Vector2 start =
            new Vector2(
                actorScreen.x,
                Screen.height - actorScreen.y); // 행동 적 IMGUI 좌표 변환

        Vector2 end =
            new Vector2(
                targetScreen.x,
                Screen.height - targetScreen.y); // 대상 IMGUI 좌표 변환

        Color arrowColor =
            specialPattern
                ? new Color(
                    1f,
                    0.55f,
                    0.12f,
                    focused ? 1f : 0.85f)
                : new Color(
                    1f,
                    0.2f,
                    0.2f,
                    focused ? 1f : 0.65f); // 일반·특수 행동별 화살표 색상

        DrawArrow(
            start,
            end,
            focused ? 5f : 3f,
            focused ? 20f : 16f,
            arrowColor); // 대상 화살표 출력
    }

    private static void DrawArrow(
        Vector2 start,
        Vector2 end,
        float thickness,
        float headSize,
        Color color) // IMGUI 화살표 출력
    {
        Vector2 direction =
            end - start; // 화살표 방향 계산

        float length =
            direction.magnitude; // 화살표 길이 계산

        if (length < 1f)
        {
            return;
        }

        direction /=
            length; // 방향 정규화

        DrawLine(
            start,
            end,
            thickness,
            color); // 화살표 본선 출력

        Vector2 leftDirection =
            Rotate(
                -direction,
                28f); // 왼쪽 화살촉 방향 계산

        Vector2 rightDirection =
            Rotate(
                -direction,
                -28f); // 오른쪽 화살촉 방향 계산

        DrawLine(
            end,
            end + leftDirection * headSize,
            thickness,
            color); // 왼쪽 화살촉 출력

        DrawLine(
            end,
            end + rightDirection * headSize,
            thickness,
            color); // 오른쪽 화살촉 출력
    }

    private static void DrawLine(
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color) // IMGUI 선 출력
    {
        Matrix4x4 previousMatrix =
            GUI.matrix; // 기존 GUI 행렬 저장

        Color previousColor =
            GUI.color; // 기존 GUI 색상 저장

        Vector2 delta =
            end - start; // 선 방향 계산

        float angle =
            Mathf.Atan2(
                delta.y,
                delta.x) *
            Mathf.Rad2Deg; // 선 회전 각도 계산

        GUI.color = color; // 선 색상 적용

        GUIUtility.RotateAroundPivot(
            angle,
            start); // 시작점 기준 선 회전

        GUI.DrawTexture(
            new Rect(
                start.x,
                start.y - thickness * 0.5f,
                delta.magnitude,
                thickness),
            Texture2D.whiteTexture); // 선 출력

        GUI.matrix =
            previousMatrix; // GUI 행렬 복원

        GUI.color =
            previousColor; // GUI 색상 복원
    }

    private static Vector2 Rotate(
        Vector2 vector,
        float degrees) // 2D 벡터 회전
    {
        float radians =
            degrees *
            Mathf.Deg2Rad; // 라디안 변환

        float cosine =
            Mathf.Cos(
                radians); // 코사인 계산

        float sine =
            Mathf.Sin(
                radians); // 사인 계산

        return new Vector2(
            vector.x * cosine - vector.y * sine,
            vector.x * sine + vector.y * cosine); // 회전 벡터 반환
    }

    private void OnDestroy() // 행동 표시 관리자 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 관리자 참조 제거
        }
    }
}
