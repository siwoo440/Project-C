using TMPro; // 확대 카드 글자 표시 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 확대 카드 이미지 사용
public sealed class BattleCardPreviewView : MonoBehaviour // 선택 카드 확대 화면
{ // 확대 카드 화면 시작
    private const float PreviewWidth = 300f; // 확대 카드 기본 너비
    private const float PreviewHeight = 420f; // 확대 카드 기본 높이
    private RectTransform previewRect; // 확대 카드 영역
    private RectTransform sourceRect; // 실제 손패 카드 영역
    private RectTransform handRect; // 손패 패널 영역
    private Image artwork; // 확대 카드 일러스트
    private Image costBadge; // 확대 카드 종류와 비용 배경 아이콘
    private TMP_Text title; // 확대 카드 이름
    private TMP_Text description; // 확대 카드 전체 설명
    private TMP_Text info; // 카드 소유자 표시
    private TMP_Text cost; // 카드 행동력 비용
    private void Awake() // 확대 카드 화면 준비
    { // 화면 준비 시작
        previewRect = transform as RectTransform; // 확대 카드 영역 조회
        previewRect.anchorMin = new Vector2(0.5f, 0f); // 손패 아래 중앙 최소 앵커 설정
        previewRect.anchorMax = new Vector2(0.5f, 0f); // 손패 아래 중앙 최대 앵커 설정
        previewRect.pivot = new Vector2(0.5f, 0f); // 확대 카드 아래 중앙 기준점 설정
        previewRect.sizeDelta = new Vector2(PreviewWidth, PreviewHeight); // 확대 카드 기본 크기 설정
        Image frame = GetComponent<Image>(); // 카드 프레임 이미지 조회
        frame.color = new Color(0.04f, 0.06f, 0.08f, 1f); // 프레임 이미지 누락 시 대체 색상
        BattleUiArt.Apply(frame, "CardFrame"); // 기존 카드 금속 프레임 적용
        frame.raycastTarget = false; // 실제 카드와 버튼 입력 차단 해제
        gameObject.AddComponent<Canvas>(); // 확대 카드 독립 화면 순서 생성
        CanvasGroup group = gameObject.AddComponent<CanvasGroup>(); // 확대 카드 입력 그룹 생성
        group.interactable = false; // 확대 카드 자체 상호작용 해제
        group.blocksRaycasts = false; // 확대 카드 전체 입력 통과 설정
        Image face = CreateImage("PreviewFace", transform); // 불투명 카드 바탕 생성
        face.color = new Color(0.025f, 0.04f, 0.055f, 0.99f); // 카드 설명 대비용 어두운 바탕
        SetStretch(face.rectTransform, 7f); // 금속 프레임 안쪽에 카드 바탕 배치
        title = CreateText("PreviewName", 25f, TextAlignmentOptions.Center); // 확대 카드 이름 생성
        SetTopRect(title.rectTransform, new Vector2(0f, -24f), new Vector2(210f, 36f)); // 카드 이름 상단 위치 설정
        artwork = CreateImage("PreviewArtwork", transform); // 확대 카드 일러스트 생성
        artwork.preserveAspect = true; // 원본 일러스트 비율 유지
        SetTopRect(artwork.rectTransform, new Vector2(0f, -122f), new Vector2(260f, 145f)); // 확대 일러스트 위치 설정
        description = CreateText("PreviewDescription", 23f, TextAlignmentOptions.Center); // 전체 카드 설명 생성
        description.enableAutoSizing = true; // 설명 길이에 맞춘 글자 크기 조정
        description.fontSizeMin = 9f; // 긴 설명의 최소 글자 크기 설정
        description.fontSizeMax = 23f; // 설명의 최대 글자 크기 설정
        description.textWrappingMode = TextWrappingModes.Normal; // 카드 설명 자동 줄바꿈 설정
        description.overflowMode = TextOverflowModes.Overflow; // 설명 생략 부호 없이 전체 내용 표시
        SetTopRect(description.rectTransform, new Vector2(0f, -278f), new Vector2(260f, 160f)); // 카드 아래쪽의 넓은 설명 영역 설정
        info = CreateText("PreviewInfo", 16f, TextAlignmentOptions.Center); // 카드 소유자 표시 생성
        SetTopRect(info.rectTransform, new Vector2(0f, -389f), new Vector2(260f, 30f)); // 카드 아래 정보 위치 설정
        costBadge = CreateImage("PreviewCostBadge", transform); // 카드 비용 배지 생성
        BattleUiArt.Apply(costBadge, "CostBadge"); // 기본 비용 배지 이미지 적용
        SetTopRect(costBadge.rectTransform, new Vector2(-122f, -24f), new Vector2(42f, 42f)); // 카드 왼쪽 위 비용 배지 배치
        cost = CreateText("PreviewCost", 24f, TextAlignmentOptions.Center); // 카드 행동력 숫자 생성
        cost.transform.SetParent(costBadge.transform, false); // 비용 배지 안에 숫자 연결
        cost.outlineWidth = 0.25f; // 밝은 아이콘 위 비용 숫자 외곽선 설정
        cost.outlineColor = Color.black; // 비용 숫자 어두운 외곽선 설정
        SetStretch(cost.rectTransform, 0f); // 배지 안쪽 중앙에 비용 표시
    } // 화면 준비 종료
    public void Show(CardInstance card, RectTransform source, RectTransform hand) // 실제 카드의 확대 화면 표시
    { // 확대 화면 표시 시작
        if (card == null || source == null || hand == null) // 확대 카드와 화면 영역 확인
        { // 확대 불가 처리 시작
            Hide(); // 기존 확대 화면 숨김
            return; // 확대 화면 표시 중단
        } // 확대 불가 처리 종료
        sourceRect = source; // 실제 손패 카드 위치 저장
        handRect = hand; // 실제 손패 화면 영역 저장
        title.text = card.DisplayName; // 실제 카드 이름 표시
        description.text = card.SourceData.Description; // 실제 카드의 전체 설명 표시
        info.text = card.OwnerUnit.DisplayName; // 카드 소유자만 표시
        if (!BattleUiArt.ApplyCardType(costBadge, card.CardType)) // 카드 종류 이미지 누락 확인
        { // 기본 비용 배경 복원 시작
            BattleUiArt.Apply(costBadge, "CostBadge"); // 이미지 누락 시 기존 비용 배경 적용
        } // 기본 비용 배경 복원 종료
        cost.text = card.ApCost.ToString(); // 실제 행동력 비용 표시
        artwork.sprite = card.Artwork; // 실제 카드 일러스트 표시
        artwork.color = card.Artwork != null ? Color.white : new Color(0.2f, 0.2f, 0.22f, 1f); // 일러스트 누락 시 대체 배경 설정
        gameObject.SetActive(true); // 확대 카드 표시
        Canvas overlay = GetComponent<Canvas>(); // 활성화된 확대 카드 화면 조회
        overlay.overrideSorting = true; // 레벨 표시와 독립된 화면 순서 설정
        overlay.sortingOrder = 431; // 레벨 표시보다 앞, 전투 선택 창보다 뒤에 배치
        Reposition(); // 손패 카드 근처와 화면 내부에 확대 카드 배치
    } // 확대 화면 표시 종료
    public void Hide() // 확대 카드 숨김
    { // 확대 카드 숨김 시작
        sourceRect = null; // 이전 손패 카드 참조 제거
        handRect = null; // 이전 손패 영역 참조 제거
        gameObject.SetActive(false); // 확대 카드 화면 숨김
    } // 확대 카드 숨김 종료
    private void LateUpdate() // 화면 크기와 손패 위치 변경 감시
    { // 확대 카드 위치 갱신 시작
        Reposition(); // 현재 손패와 화면 크기에 맞춘 위치 갱신
    } // 확대 카드 위치 갱신 종료
    private void Reposition() // 실제 손패 카드 근처의 안전한 위치 계산
    { // 확대 카드 위치 계산 시작
        if (sourceRect == null || handRect == null || !sourceRect.gameObject.activeInHierarchy) // 실제 카드와 손패 영역 확인
        { // 위치 계산 불가 처리 시작
            Hide(); // 제거되거나 숨겨진 카드의 확대 화면 정리
            return; // 위치 갱신 중단
        } // 위치 계산 불가 처리 종료
        Canvas canvas = handRect.GetComponentInParent<Canvas>(); // 실제 전투 캔버스 조회
        RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null; // 최상위 전투 화면 영역 조회
        float availableWidth = Mathf.Max(1f, handRect.rect.width - 16f); // 손패 화면의 확대 카드 가능 너비
        float availableHeight = canvasRect != null ? Mathf.Max(1f, canvasRect.rect.height - 16f) : PreviewHeight; // 전체 화면의 확대 카드 가능 높이
        float scale = Mathf.Min(1f, Mathf.Min(availableWidth / PreviewWidth, availableHeight / PreviewHeight)); // 작은 화면의 확대 카드 배율 계산
        previewRect.localScale = Vector3.one * scale; // 화면에 맞춘 확대 카드 배율 적용
        float halfWidth = PreviewWidth * scale * 0.5f; // 확대 카드 절반 너비 계산
        Vector3 cardCenter = handRect.InverseTransformPoint(sourceRect.TransformPoint(sourceRect.rect.center)); // 실제 카드의 손패 기준 가운데 좌표 계산
        float left = handRect.rect.xMin + halfWidth + 8f; // 확대 카드 왼쪽 안전 위치
        float right = handRect.rect.xMax - halfWidth - 8f; // 확대 카드 오른쪽 안전 위치
        float x = Mathf.Clamp(cardCenter.x, left, right); // 실제 카드 근처의 화면 내부 가로 위치 계산
        float bottom = 16f; // 손패 아래쪽 기본 여백
        if (canvasRect != null) // 전체 화면 영역 존재 확인
        { // 화면 세로 범위 보정 시작
            Vector3 bottomWorld = handRect.TransformPoint(new Vector3(0f, handRect.rect.yMin, 0f)); // 손패 아래쪽의 실제 위치 계산
            float bottomCanvas = canvasRect.InverseTransformPoint(bottomWorld).y; // 전체 화면 기준 손패 아래쪽 좌표 계산
            float safeBottom = Mathf.Clamp(bottomCanvas + bottom, canvasRect.rect.yMin + 8f, canvasRect.rect.yMax - PreviewHeight * scale - 8f); // 확대 카드의 화면 내부 아래 위치 계산
            bottom = safeBottom - bottomCanvas; // 손패 기준 확대 카드 아래 여백 보정
        } // 화면 세로 범위 보정 종료
        previewRect.anchoredPosition = new Vector2(x, bottom); // 확대 카드의 최종 위치 적용
    } // 확대 카드 위치 계산 종료
    private static Image CreateImage(string name, Transform parent) // 확대 카드 이미지 생성
    { // 카드 이미지 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 카드 이미지 객체 생성
        target.transform.SetParent(parent, false); // 확대 카드에 이미지 연결
        Image image = target.GetComponent<Image>(); // 카드 이미지 조회
        image.raycastTarget = false; // 이미지의 포인터 입력 차단 해제
        return image; // 생성한 카드 이미지 반환
    } // 카드 이미지 생성 종료
    private TMP_Text CreateText(string name, float fontSize, TextAlignmentOptions alignment) // 확대 카드 글자 생성
    { // 카드 글자 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 카드 글자 객체 생성
        target.transform.SetParent(transform, false); // 확대 카드에 글자 연결
        TMP_Text text = target.GetComponent<TMP_Text>(); // 카드 글자 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        text.fontSize = fontSize; // 카드 글자 크기 설정
        text.alignment = alignment; // 카드 글자 정렬 설정
        text.color = Color.white; // 카드 글자 밝은 색상 설정
        text.raycastTarget = false; // 글자의 포인터 입력 차단 해제
        return text; // 생성한 카드 글자 반환
    } // 카드 글자 생성 종료
    private static void SetTopRect(RectTransform rect, Vector2 position, Vector2 size) // 확대 카드 상단 기준 요소 배치
    { // 상단 기준 요소 배치 시작
        rect.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 최소 앵커 설정
        rect.anchorMax = new Vector2(0.5f, 1f); // 상단 중앙 최대 앵커 설정
        rect.pivot = new Vector2(0.5f, 0.5f); // 요소 중앙 기준점 설정
        rect.anchoredPosition = position; // 카드 안쪽 요소 위치 설정
        rect.sizeDelta = size; // 카드 안쪽 요소 크기 설정
    } // 상단 기준 요소 배치 종료
    private static void SetStretch(RectTransform rect, float inset) // 확대 카드 내부 전체 영역 배치
    { // 전체 영역 배치 시작
        rect.anchorMin = Vector2.zero; // 내부 영역 최소 앵커 설정
        rect.anchorMax = Vector2.one; // 내부 영역 최대 앵커 설정
        rect.offsetMin = new Vector2(inset, inset); // 내부 영역 왼쪽 아래 여백 설정
        rect.offsetMax = new Vector2(-inset, -inset); // 내부 영역 오른쪽 위 여백 설정
    } // 전체 영역 배치 종료
} // 확대 카드 화면 종료
