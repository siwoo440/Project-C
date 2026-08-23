using System.Collections.Generic; // 대상 화면 목록 사용
using TMPro; // 드래그 카드 이름 표시 사용
using UnityEngine; // UI와 런타임 기능 사용
using UnityEngine.EventSystems; // 드래그 포인터 이벤트 사용
using UnityEngine.InputSystem; // ESC 취소 입력 사용
using UnityEngine.UI; // 드래그 카드 이미지 사용

public sealed class BattleCardDragHandler :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler // 카드 드래그 타겟팅 입력 처리기
{
    private readonly List<BattleUnitView> highlightedUnitViews =
        new List<BattleUnitView>(); // 현재 대상 후보 강조 목록

    private BattleCardView cardView; // 연결된 카드 화면
    private Canvas battleCanvas; // 전투 UI Canvas
    private GameObject dragGhostObject; // 드래그 중 카드 복제 표시
    private RectTransform dragGhostRect; // 드래그 복제 위치
    private BattleUnitView currentTargetView; // 현재 포인터 대상
    private Vector2 currentPointerPosition; // 현재 포인터 화면 위치
    private bool dragging; // 현재 드래그 여부
    private bool cancelRequested; // ESC 취소 요청 여부

    public void Initialize(
        BattleCardView targetCardView) // 카드 화면 연결
    {
        if (targetCardView == null)
        {
            return;
        }

        cardView = targetCardView; // 카드 화면 저장
        battleCanvas =
            cardView.GetComponentInParent<Canvas>(); // 전투 Canvas 조회
    }

    private void Awake() // 카드 화면 자동 탐색
    {
        cardView =
            GetComponent<BattleCardView>(); // 같은 오브젝트 카드 화면 조회

        if (cardView != null)
        {
            battleCanvas =
                cardView.GetComponentInParent<Canvas>(); // 전투 Canvas 조회
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData) // 카드 마우스 진입 처리
    {
        if (dragging || cardView == null)
        {
            return;
        }

        cardView.OnPointerEnter(
            eventData); // 기존 카드 확대·툴팁 기능 재사용
    }

    public void OnPointerExit(
        PointerEventData eventData) // 카드 마우스 이탈 처리
    {
        if (dragging || cardView == null)
        {
            return;
        }

        cardView.OnPointerExit(
            eventData); // 기존 카드 확대·툴팁 해제 재사용
    }

    public void OnBeginDrag(
        PointerEventData eventData) // 카드 드래그 시작
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            !CanBeginDrag())
        {
            return;
        }

        dragging = true; // 드래그 상태 활성화
        cancelRequested = false; // 취소 상태 초기화
        currentPointerPosition = eventData.position; // 시작 포인터 위치 저장

        cardView.OnPointerExit(
            eventData); // 드래그 시작 시 기존 카드 확대 해제

        ShowValidTargetHighlights(); // 유효 대상 후보 강조
        CreateDragGhost(); // 드래그 카드 복제 생성
        UpdateDragGhostPosition(
            currentPointerPosition); // 복제 카드를 포인터 위치로 이동

        currentTargetView =
            FindPointerTarget(
                eventData); // 시작 위치의 대상 조회
    }

    public void OnDrag(
        PointerEventData eventData) // 카드 드래그 진행
    {
        if (!dragging || eventData == null)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame)
        {
            cancelRequested = true; // ESC 취소 상태 저장
            currentTargetView = null; // 현재 대상 제거
            DestroyDragGhost(); // 드래그 복제 숨김
            ClearTargetHighlights(); // 대상 후보 강조 해제
            return;
        }

        currentPointerPosition =
            eventData.position; // 현재 포인터 위치 저장

        UpdateDragGhostPosition(
            currentPointerPosition); // 복제 카드 위치 갱신

        currentTargetView =
            FindPointerTarget(
                eventData); // 현재 포인터 대상 갱신
    }

    public void OnEndDrag(
        PointerEventData eventData) // 카드 드래그 종료
    {
        if (!dragging)
        {
            return;
        }

        BattleUnitView dropTarget =
            cancelRequested || eventData == null
                ? null
                : FindPointerTarget(
                    eventData); // 드롭 위치 대상 조회

        CardInstance draggedCard =
            cardView != null
                ? cardView.RuntimeCard
                : null; // 드래그 카드 저장

        bool validDrop =
            draggedCard != null &&
            dropTarget != null &&
            IsValidTarget(
                draggedCard,
                dropTarget.RuntimeUnit); // 드롭 대상 유효성 확인

        dragging = false; // 드래그 상태 해제
        cancelRequested = false; // 취소 상태 초기화
        currentTargetView = null; // 현재 대상 초기화

        DestroyDragGhost(); // 드래그 복제 제거
        ClearTargetHighlights(); // 대상 후보 강조 해제

        if (!validDrop)
        {
            Debug.Log(
                "[Battle][Day47] 유효하지 않은 위치에 카드를 놓아 사용을 취소했습니다."); // 잘못된 드롭 취소 로그
            return;
        }

        ExecuteDrop(
            draggedCard,
            dropTarget,
            eventData); // 기존 카드 사용 흐름으로 전달
    }

    private bool CanBeginDrag() // 카드 드래그 시작 가능 여부 확인
    {
        if (cardView == null ||
            cardView.RuntimeCard == null ||
            cardView.RuntimeCard.OwnerUnit == null ||
            cardView.RuntimeCard.OwnerUnit.IsDead)
        {
            return false;
        }

        BattleSceneSetup battleSceneSetup =
            FindFirstObjectByType<BattleSceneSetup>(); // 현재 전투 설정 조회

        if (battleSceneSetup == null ||
            !battleSceneSetup.IsInitialized ||
            battleSceneSetup.BattleTurn == null ||
            battleSceneSetup.SharedActionPoints == null ||
            !battleSceneSetup.BattleTurn.IsPlayerTurn ||
            battleSceneSetup.BattleTurn.IsBattleEnded ||
            !battleSceneSetup.SharedActionPoints.CanSpend(
                cardView.RuntimeCard.ApCost))
        {
            return false;
        }

        BattleActionSequenceRunner sequenceRunner =
            battleSceneSetup.GetComponent<BattleActionSequenceRunner>(); // 행동 연출 실행기 조회

        return sequenceRunner == null ||
               !sequenceRunner.IsBusy; // 행동 연출 중 드래그 차단
    }

    private void ShowValidTargetHighlights() // 현재 카드 대상 후보 강조
    {
        ClearTargetHighlights(); // 기존 후보 강조 초기화

        CardInstance card =
            cardView != null
                ? cardView.RuntimeCard
                : null; // 현재 카드 조회

        if (card == null)
        {
            return;
        }

        BattleUnitView[] unitViews =
            FindObjectsByType<BattleUnitView>(
                FindObjectsSortMode.None); // 전체 전투 유닛 화면 조회

        for (int index = 0; index < unitViews.Length; index += 1)
        {
            BattleUnitView unitView =
                unitViews[index]; // 현재 유닛 화면 조회

            if (unitView == null ||
                unitView.RuntimeUnit == null ||
                !IsValidTarget(
                    card,
                    unitView.RuntimeUnit))
            {
                continue;
            }

            unitView.SetTargetable(
                true); // 유효 대상 테두리 강조

            highlightedUnitViews.Add(
                unitView); // 강조 목록 등록
        }
    }

    private void ClearTargetHighlights() // 카드 대상 후보 강조 해제
    {
        for (int index = 0;
             index < highlightedUnitViews.Count;
             index += 1)
        {
            BattleUnitView unitView =
                highlightedUnitViews[index]; // 강조 유닛 화면 조회

            if (unitView != null)
            {
                unitView.SetTargetable(
                    false); // 기존 진영 테두리 복원
            }
        }

        highlightedUnitViews.Clear(); // 강조 목록 초기화
    }

    private BattleUnitView FindPointerTarget(
        PointerEventData eventData) // 포인터 아래 전투 유닛 조회
    {
        if (eventData == null ||
            eventData.pointerCurrentRaycast.gameObject == null)
        {
            return null;
        }

        BattleUnitView unitView =
            eventData.pointerCurrentRaycast.gameObject
                .GetComponentInParent<BattleUnitView>(); // 포인터 아래 유닛 화면 탐색

        if (unitView == null ||
            unitView.RuntimeUnit == null ||
            cardView == null ||
            cardView.RuntimeCard == null ||
            !IsValidTarget(
                cardView.RuntimeCard,
                unitView.RuntimeUnit))
        {
            return null;
        }

        return unitView; // 유효 포인터 대상 반환
    }

    private static bool IsValidTarget(
        CardInstance card,
        BattleUnitRuntime targetUnit) // 카드 대상 규칙 검사
    {
        if (card == null ||
            targetUnit == null ||
            targetUnit.IsDead)
        {
            return false;
        }

        switch (card.TargetType)
        {
            case CardTargetType.Self:
                return targetUnit == card.OwnerUnit;

            case CardTargetType.SingleAlly:
            case CardTargetType.AllAllies:
                return targetUnit.Team == BattleTeam.Ally;

            case CardTargetType.SingleEnemy:
            case CardTargetType.AllEnemies:
                return targetUnit.Team == BattleTeam.Enemy;

            default:
                return false;
        }
    }

    private void ExecuteDrop(
        CardInstance card,
        BattleUnitView targetView,
        PointerEventData eventData) // 유효 드롭 카드 사용
    {
        if (cardView == null ||
            card == null ||
            targetView == null ||
            targetView.RuntimeUnit == null)
        {
            return;
        }

        cardView.OnPointerClick(
            eventData); // 기존 카드 선택·AP 검사 흐름 호출

        if (card.TargetType == CardTargetType.SingleAlly ||
            card.TargetType == CardTargetType.SingleEnemy)
        {
            targetView.OnPointerClick(
                eventData); // 단일 대상 카드를 드롭 대상에게 사용
        }

        Debug.Log(
            $"[Battle][Day47] 카드 드래그 사용 - " +
            $"{card.DisplayName} → {targetView.RuntimeUnit.DisplayName}"); // 드래그 카드 사용 로그
    }

    private void CreateDragGhost() // 마우스를 따라가는 카드 복제 생성
    {
        DestroyDragGhost(); // 기존 복제 제거

        if (battleCanvas == null)
        {
            battleCanvas =
                GetComponentInParent<Canvas>(); // Canvas 재탐색
        }

        if (battleCanvas == null ||
            cardView == null ||
            cardView.RuntimeCard == null)
        {
            return;
        }

        dragGhostObject =
            new GameObject(
                "Day47CardDragGhost",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup)); // 카드 복제 오브젝트 생성

        dragGhostObject.transform.SetParent(
            battleCanvas.transform,
            false); // 전투 Canvas에 복제 배치

        dragGhostObject.transform.SetAsLastSibling(); // 복제 카드를 최상단 표시

        dragGhostRect =
            dragGhostObject.GetComponent<RectTransform>(); // 복제 RectTransform 조회

        dragGhostRect.sizeDelta =
            new Vector2(
                150f,
                200f); // 복제 카드 크기 설정

        Image backgroundImage =
            dragGhostObject.GetComponent<Image>(); // 복제 카드 배경 조회

        backgroundImage.color =
            new Color(
                0.12f,
                0.17f,
                0.26f,
                0.92f); // 복제 카드 배경 색상 설정

        backgroundImage.raycastTarget = false; // 복제 카드 포인터 차단 해제

        CanvasGroup canvasGroup =
            dragGhostObject.GetComponent<CanvasGroup>(); // 복제 카드 CanvasGroup 조회

        canvasGroup.alpha = 0.9f; // 복제 카드 반투명 처리
        canvasGroup.blocksRaycasts = false; // 복제 카드 Raycast 차단 해제
        canvasGroup.interactable = false; // 복제 카드 상호작용 해제

        CreateGhostArtwork(); // 카드 일러스트 복제
        CreateGhostName(); // 카드 이름 복제
    }

    private void CreateGhostArtwork() // 드래그 카드 일러스트 생성
    {
        GameObject artworkObject =
            new GameObject(
                "Artwork",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)); // 드래그 일러스트 오브젝트 생성

        artworkObject.transform.SetParent(
            dragGhostObject.transform,
            false); // 복제 카드에 일러스트 배치

        RectTransform artworkRect =
            artworkObject.GetComponent<RectTransform>(); // 일러스트 RectTransform 조회

        artworkRect.anchorMin =
            new Vector2(
                0.5f,
                1f); // 일러스트 위쪽 중앙 앵커 설정

        artworkRect.anchorMax =
            artworkRect.anchorMin; // 일러스트 앵커 고정

        artworkRect.pivot =
            new Vector2(
                0.5f,
                1f); // 일러스트 위쪽 중앙 피벗 설정

        artworkRect.anchoredPosition =
            new Vector2(
                0f,
                -12f); // 일러스트 위치 설정

        artworkRect.sizeDelta =
            new Vector2(
                126f,
                112f); // 일러스트 크기 설정

        Image artworkImage =
            artworkObject.GetComponent<Image>(); // 일러스트 Image 조회

        artworkImage.sprite =
            cardView.RuntimeCard.Artwork; // 카드 원본 일러스트 적용

        artworkImage.color =
            artworkImage.sprite != null
                ? Color.white
                : new Color(
                    0.25f,
                    0.3f,
                    0.38f,
                    1f); // 일러스트 유무별 색상 적용

        artworkImage.raycastTarget = false; // 일러스트 포인터 차단 해제
    }

    private void CreateGhostName() // 드래그 카드 이름 생성
    {
        GameObject textObject =
            new GameObject(
                "Name",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)); // 카드 이름 오브젝트 생성

        textObject.transform.SetParent(
            dragGhostObject.transform,
            false); // 복제 카드에 이름 배치

        TMP_Text nameText =
            textObject.GetComponent<TMP_Text>(); // 카드 이름 텍스트 조회

        nameText.text =
            cardView.RuntimeCard.DisplayName; // 카드 이름 적용

        nameText.fontSize = 18f; // 카드 이름 글자 크기 설정
        nameText.alignment = TextAlignmentOptions.Center; // 카드 이름 중앙 정렬
        nameText.color = Color.white; // 카드 이름 색상 설정
        nameText.raycastTarget = false; // 카드 이름 포인터 차단 해제
        nameText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용

        RectTransform nameRect =
            nameText.rectTransform; // 카드 이름 RectTransform 조회

        nameRect.anchorMin =
            new Vector2(
                0f,
                0f); // 카드 이름 최소 앵커 설정

        nameRect.anchorMax =
            new Vector2(
                1f,
                0f); // 카드 이름 최대 앵커 설정

        nameRect.pivot =
            new Vector2(
                0.5f,
                0f); // 카드 이름 아래쪽 피벗 설정

        nameRect.anchoredPosition =
            new Vector2(
                0f,
                14f); // 카드 이름 아래쪽 위치 설정

        nameRect.sizeDelta =
            new Vector2(
                -16f,
                48f); // 카드 이름 영역 설정
    }

    private void UpdateDragGhostPosition(
        Vector2 screenPosition) // 드래그 카드 복제 위치 갱신
    {
        if (dragGhostRect == null ||
            battleCanvas == null)
        {
            return;
        }

        RectTransform canvasRect =
            battleCanvas.transform as RectTransform; // Canvas RectTransform 조회

        if (canvasRect == null)
        {
            return;
        }

        Camera canvasCamera =
            battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : battleCanvas.worldCamera; // Canvas 렌더 모드별 카메라 조회

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            return;
        }

        dragGhostRect.anchoredPosition =
            localPosition; // 복제 카드를 포인터 위치로 이동
    }

    private void DestroyDragGhost() // 드래그 카드 복제 제거
    {
        if (dragGhostObject != null)
        {
            Destroy(
                dragGhostObject); // 복제 카드 오브젝트 제거
        }

        dragGhostObject = null; // 복제 카드 참조 초기화
        dragGhostRect = null; // 복제 RectTransform 초기화
    }

    private void OnGUI() // 드래그 중 카드 타겟 화살표 출력
    {
        if (!dragging ||
            cancelRequested ||
            cardView == null)
        {
            return;
        }

        RectTransform cardRect =
            cardView.transform as RectTransform; // 원본 카드 RectTransform 조회

        if (cardRect == null)
        {
            return;
        }

        Camera canvasCamera =
            battleCanvas != null &&
            battleCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? battleCanvas.worldCamera
                : null; // Canvas 카메라 조회

        Vector2 startScreen =
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                cardRect.position); // 카드 중심 화면 좌표 계산

        Vector2 startGui =
            new Vector2(
                startScreen.x,
                Screen.height - startScreen.y); // IMGUI 좌표로 변환

        Vector2 endGui =
            new Vector2(
                currentPointerPosition.x,
                Screen.height - currentPointerPosition.y); // 포인터 IMGUI 좌표 변환

        Color arrowColor =
            currentTargetView != null
                ? new Color(
                    0.25f,
                    1f,
                    0.45f,
                    0.95f)
                : new Color(
                    1f,
                    0.7f,
                    0.2f,
                    0.8f); // 유효 대상 여부별 화살표 색상 결정

        DrawArrow(
            startGui,
            endGui,
            4f,
            16f,
            arrowColor); // 카드에서 포인터까지 타겟 화살표 출력
    }

    private static void DrawArrow(
        Vector2 start,
        Vector2 end,
        float thickness,
        float headSize,
        Color color) // IMGUI 직선 화살표 출력
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
            length; // 화살표 방향 정규화

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
            start); // 선 시작점 기준 회전

        GUI.DrawTexture(
            new Rect(
                start.x,
                start.y - thickness * 0.5f,
                delta.magnitude,
                thickness),
            Texture2D.whiteTexture); // 회전된 직선 출력

        GUI.matrix =
            previousMatrix; // 기존 GUI 행렬 복원

        GUI.color =
            previousColor; // 기존 GUI 색상 복원
    }

    private static Vector2 Rotate(
        Vector2 vector,
        float degrees) // 2D 벡터 회전
    {
        float radians =
            degrees *
            Mathf.Deg2Rad; // 회전 각도 라디안 변환

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

    private void OnDisable() // 드래그 처리기 비활성화 정리
    {
        dragging = false; // 드래그 상태 해제
        cancelRequested = false; // 취소 상태 해제
        currentTargetView = null; // 현재 대상 초기화
        DestroyDragGhost(); // 드래그 복제 제거
        ClearTargetHighlights(); // 대상 강조 해제
    }
}
