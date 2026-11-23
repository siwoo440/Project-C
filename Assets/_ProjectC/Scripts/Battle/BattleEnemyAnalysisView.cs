using System.Text; // 적 정보 문자열 구성 사용
using TMPro; // 적 정보 텍스트 사용
using UnityEngine; // 런타임 UI 기능 사용
using UnityEngine.UI; // 적 정보 패널 이미지 사용

public sealed class BattleEnemyAnalysisView : MonoBehaviour // 적 클릭 상세 정보 패널
{
    public static BattleEnemyAnalysisView Instance
    {
        get;
        private set;
    } // 현재 적 정보 패널 조회

    private Image backgroundImage; // 적 정보 패널 배경
    private TMP_Text detailText; // 적 정보 텍스트
    private BattleUnitRuntime selectedEnemy; // 현재 선택 적
    private BattleUnitView selectedEnemyView; // 현재 선택 적 화면
    private Canvas battleCanvas; // 전투 Canvas
    private bool visualCreated; // 적 정보 화면 생성 여부

    public static BattleEnemyAnalysisView EnsureInstance(
        Canvas canvas) // 적 정보 패널 존재 보장
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (canvas == null)
        {
            return null;
        }

        GameObject panelObject =
            new GameObject(
                "Day47EnemyAnalysis",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)); // 적 정보 패널 오브젝트 생성

        panelObject.transform.SetParent(
            canvas.transform,
            false); // 전투 Canvas에 적 정보 패널 배치

        BattleEnemyAnalysisView analysisView =
            panelObject.AddComponent<BattleEnemyAnalysisView>(); // 적 정보 기능 추가

        analysisView.battleCanvas = canvas; // 전투 Canvas 저장
        analysisView.EnsureVisual(); // 적 정보 화면 생성
        analysisView.gameObject.SetActive(false); // 시작 시 적 정보 숨김

        return analysisView;
    }

    private void Awake() // 적 정보 패널 인스턴스 초기화
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject); // 중복 적 정보 패널 제거
            return;
        }

        Instance = this; // 현재 적 정보 패널 등록
    }

    public void Toggle(
        BattleUnitView enemyView) // 적 클릭 정보 표시 전환
    {
        if (enemyView == null ||
            enemyView.RuntimeUnit == null ||
            enemyView.RuntimeUnit.Team != BattleTeam.Enemy)
        {
            return;
        }

        if (gameObject.activeSelf &&
            selectedEnemy == enemyView.RuntimeUnit)
        {
            Hide(); // 같은 적 재클릭 시 정보 숨김
            return;
        }

        selectedEnemy =
            enemyView.RuntimeUnit; // 현재 선택 적 저장

        selectedEnemyView =
            enemyView; // 현재 선택 적 화면 저장

        RefreshText(); // 현재 적 정보 갱신
        UpdatePanelPosition(); // 선택 적 주변으로 패널 이동
        gameObject.SetActive(true); // 적 정보 패널 표시
        transform.SetAsLastSibling(); // 적 정보 패널 최상단 표시
    }

    public void ValidateSelectedUnit() // 선택 적 상태 유효성 갱신
    {
        if (!gameObject.activeSelf ||
            selectedEnemy == null ||
            selectedEnemyView == null ||
            selectedEnemy.IsDead)
        {
            if (gameObject.activeSelf &&
                (selectedEnemy == null ||
                 selectedEnemyView == null ||
                 selectedEnemy.IsDead))
            {
                Hide(); // 제거되거나 사망한 적 정보 숨김
            }

            return;
        }

        RefreshText(); // 체력·상태 변화 실시간 반영
    }

    private void RefreshText() // 적 정보 문자열 갱신
    {
        if (detailText == null ||
            selectedEnemy == null)
        {
            return;
        }

        EnemyData enemyData =
            selectedEnemy.EnemySource; // 적 원본 데이터 조회

        StringBuilder builder =
            new StringBuilder(); // 적 정보 문자열 생성기

        builder.AppendLine(
            "[적 정보 · 같은 적 다시 클릭 시 닫기]"); // 조작 안내

        builder.AppendLine(
            $"적 : {selectedEnemy.DisplayName}"); // 적 이름 표시

        builder.AppendLine(
            $"HP : {selectedEnemy.CurrentHealth} / {selectedEnemy.MaxHealth}"); // 체력 표시

        builder.AppendLine(
            $"물리 방어 : {selectedEnemy.PhysicalDefense}"); // 물리 방어 표시

        builder.AppendLine(
            $"마법 저항 : {selectedEnemy.MagicalResistance}"); // 마법 저항 표시

        builder.AppendLine(
            $"정신력 : {selectedEnemy.CurrentMental} / {selectedEnemy.MentalState}"); // 정신력 상태 표시

        builder.AppendLine(
            $"약점 : {GetWeaknessText(enemyData)}"); // 카드 계열 약점 표시

        if (enemyData != null &&
            !string.IsNullOrWhiteSpace(enemyData.Description))
        {
            builder.AppendLine(); // 설명 전 여백
            builder.AppendLine(
                enemyData.Description); // 적 설명 표시
        }

        detailText.text =
            builder.ToString(); // 적 정보 문자열 적용
    }

    private void UpdatePanelPosition() // 선택 적 주변 패널 위치 계산
    {
        if (battleCanvas == null ||
            selectedEnemyView == null)
        {
            return;
        }

        RectTransform canvasRect =
            battleCanvas.transform as RectTransform; // Canvas RectTransform 조회

        RectTransform enemyRect =
            selectedEnemyView.transform as RectTransform; // 적 RectTransform 조회

        RectTransform panelRect =
            transform as RectTransform; // 적 정보 패널 RectTransform 조회

        if (canvasRect == null ||
            enemyRect == null ||
            panelRect == null)
        {
            return;
        }

        Camera canvasCamera =
            battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : battleCanvas.worldCamera; // Canvas 카메라 조회

        Vector2 enemyScreen =
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                enemyRect.position); // 적 중심 화면 좌표 계산

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                enemyScreen,
                canvasCamera,
                out Vector2 enemyLocal))
        {
            return;
        }

        float side =
            enemyLocal.x >= 0f
                ? -1f
                : 1f; // 화면 오른쪽 적은 왼쪽에 패널 표시

        Vector2 desiredPosition =
            enemyLocal +
            new Vector2(
                side * 245f,
                25f); // 적 옆 패널 위치 계산

        float halfWidth =
            panelRect.sizeDelta.x * 0.5f; // 패널 반너비 계산

        float halfHeight =
            panelRect.sizeDelta.y * 0.5f; // 패널 반높이 계산

        float minX =
            canvasRect.rect.xMin + halfWidth + 10f; // Canvas 왼쪽 제한

        float maxX =
            canvasRect.rect.xMax - halfWidth - 10f; // Canvas 오른쪽 제한

        float minY =
            canvasRect.rect.yMin + halfHeight + 10f; // Canvas 아래 제한

        float maxY =
            canvasRect.rect.yMax - halfHeight - 10f; // Canvas 위 제한

        panelRect.anchoredPosition =
            new Vector2(
                Mathf.Clamp(desiredPosition.x, minX, maxX),
                Mathf.Clamp(desiredPosition.y, minY, maxY)); // 화면 안쪽으로 패널 위치 보정
    }

    private static string GetWeaknessText(
        EnemyData enemyData) // 적 약점 문자열 생성
    {
        if (enemyData == null ||
            enemyData.WeaknessCardTypes == null ||
            enemyData.WeaknessCardTypes.Count < 1)
        {
            return "없음";
        }

        StringBuilder builder =
            new StringBuilder(); // 약점 문자열 생성기

        for (int index = 0;
             index < enemyData.WeaknessCardTypes.Count;
             index += 1)
        {
            if (index > 0)
            {
                builder.Append(", "); // 약점 구분자 추가
            }

            builder.Append(
                GetCardTypeText(
                    enemyData.WeaknessCardTypes[index])); // 약점 이름 추가
        }

        return builder.ToString(); // 완성 약점 문자열 반환
    }

    private static string GetCardTypeText(
        CardType cardType) // 카드 계열 한글 이름 조회
    {
        switch (cardType)
        {
            case CardType.Sword:
                return "검";

            case CardType.Wand:
                return "지팡이";

            case CardType.Cup:
                return "성배";

            case CardType.Pentacle:
                return "팬타클";

            case CardType.Shield:
                return "방패";

            default:
                return cardType.ToString();
        }
    }

    private void EnsureVisual() // 적 정보 패널 화면 생성
    {
        if (visualCreated)
        {
            return;
        }

        RectTransform rootRect =
            transform as RectTransform; // 적 정보 패널 RectTransform 조회

        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f); // Canvas 중앙 앵커 설정

        rootRect.anchorMax =
            rootRect.anchorMin; // 앵커 고정

        rootRect.pivot =
            new Vector2(
                0.5f,
                0.5f); // 패널 중앙 피벗 설정

        rootRect.sizeDelta =
            new Vector2(
                360f,
                250f); // 적 정보 패널 크기 설정

        backgroundImage =
            GetComponent<Image>(); // 적 정보 배경 조회

        if (backgroundImage == null)
        {
            backgroundImage =
                gameObject.AddComponent<Image>(); // 누락 적 정보 배경 추가
        }

        backgroundImage.color =
            new Color(
                0.035f,
                0.045f,
                0.07f,
                0.96f); // 적 정보 배경 색상 설정

        backgroundImage.raycastTarget = false; // 적 정보 패널 입력 차단 해제

        GameObject textObject =
            new GameObject(
                "EnemyInfoText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // 적 정보 텍스트 생성

        textObject.transform.SetParent(
            transform,
            false); // 적 정보 패널에 텍스트 배치

        detailText =
            textObject.GetComponent<TMP_Text>(); // 적 정보 TMP 조회

        detailText.fontSize = 18f; // 적 정보 글자 크기 설정
        detailText.alignment = TextAlignmentOptions.TopLeft; // 왼쪽 위 정렬
        detailText.color = Color.white; // 적 정보 글자 색상 설정
        detailText.textWrappingMode = TextWrappingModes.Normal; // 최신 TMP 자동 줄바꿈 적용
        detailText.overflowMode = TextOverflowModes.Overflow; // 적 정보 넘침 허용
        detailText.raycastTarget = false; // 적 정보 텍스트 입력 차단 해제
        detailText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용

        RectTransform textRect =
            detailText.rectTransform; // 적 정보 텍스트 RectTransform 조회

        textRect.anchorMin = Vector2.zero; // 최소 앵커 설정
        textRect.anchorMax = Vector2.one; // 최대 앵커 설정
        textRect.offsetMin =
            new Vector2(
                14f,
                12f); // 왼쪽 아래 여백 설정

        textRect.offsetMax =
            new Vector2(
                -14f,
                -12f); // 오른쪽 위 여백 설정

        visualCreated = true; // 적 정보 화면 생성 완료
    }

    private void Hide() // 적 정보 패널 숨김
    {
        selectedEnemy = null; // 선택 적 제거
        selectedEnemyView = null; // 선택 적 화면 제거
        gameObject.SetActive(false); // 적 정보 패널 숨김
    }

    private void OnDestroy() // 적 정보 패널 제거 처리
    {
        if (Instance == this)
        {
            Instance = null; // 정적 적 정보 패널 참조 제거
        }
    }
}
