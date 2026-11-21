using System; // 선택 완료 알림 사용
using System.Collections.Generic; // 선택 후보 목록 사용
using TMPro; // 한글 카드 설명 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 이미지 사용
public sealed class BattleDrawChoiceView : MonoBehaviour // 카드 선택 드로우 화면
{ // 선택 화면 시작
    private const int CardsPerPage = 2; // 페이지당 후보 카드 수
    private const int ChoiceSortingOrder = 480; // 전투 레벨 표시와 유닛 안내 위의 선택창 순서
    private const float FooterY = -266f; // 중앙 패널 아래 조작 버튼의 공통 높이
    private readonly List<GameObject> choiceObjects = new List<GameObject>(); // 후보 버튼 목록
    private readonly List<ChoiceDescription> descriptions = new List<ChoiceDescription>(); // 후보별 원문과 스크롤 레이아웃 유지
    private Button previousButton; // 이전 페이지 버튼
    private Button nextButton; // 다음 페이지 버튼
    private TMP_Text pageLabel; // 현재 페이지 안내
    private int currentPage; // 현재 페이지 번호
    public static BattleDrawChoiceView Show(Transform parent, IReadOnlyList<CardInstance> cards, Action<CardInstance> selected, string titleText = "가져올 카드 한 장 선택", Action cancelled = null) // 닫고 되돌아갈 수 있는 선택 화면 생성
    { // 화면 생성 시작
        GameObject root = new GameObject("DrawChoicePanel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image)); // 별도 그리기와 입력을 갖는 화면 가림막 생성
        RectTransform rootRect = root.GetComponent<RectTransform>(); // 가림막 영역 조회
        rootRect.SetParent(parent, false); // 전투 화면에 연결
        rootRect.anchorMin = Vector2.zero; // 가림막 시작점 설정
        rootRect.anchorMax = Vector2.one; // 가림막 끝점 설정
        rootRect.offsetMin = Vector2.zero; // 가림막 시작 여백 제거
        rootRect.offsetMax = Vector2.zero; // 가림막 끝 여백 제거
        Canvas choiceCanvas = root.GetComponent<Canvas>(); // 카드 선택을 그리는 독립 캔버스
        choiceCanvas.overrideSorting = true; // 아래 전투 화면의 계층과 독립된 순서 사용
        choiceCanvas.sortingOrder = ChoiceSortingOrder; // 설명을 가리지 않는 레벨 표시 위 배치
        Image overlay = root.GetComponent<Image>(); // 전체 화면 회색 가림막 조회
        overlay.color = new Color(0.19f, 0.19f, 0.19f, 0.82f); // 전투 화면을 은은하게 남기는 중립 회색 적용
        overlay.raycastTarget = true; // 선택 완료 전 뒤쪽 전투 입력 차단
        BattleDrawChoiceView view = root.AddComponent<BattleDrawChoiceView>(); // 선택 화면 컴포넌트 추가
        CreatePanel(rootRect); // 가림막 위 독립 중앙 프레임 구성
        TMP_Text title = CreateText("Title", rootRect, titleText, 32f); // 기존 목적 문구의 중앙 패널 제목 생성
        title.color = new Color(1f, 0.92f, 0.75f); // 프레임과 어울리는 밝은 금빛 제목
        title.fontStyle = FontStyles.Bold; // 후보 이름과 구분되는 상단 제목 강조
        title.rectTransform.anchoredPosition = new Vector2(0f, 265f); // 카드와 분리된 상단 제목 위치
        title.rectTransform.sizeDelta = new Vector2(800f, 64f); // 공용 선택 제목의 충분한 가로 영역
        for (int index = 0; index < cards.Count; index++) // 선택 후보 순회
        { // 후보 화면 생성 시작
            CardInstance card = cards[index]; // 현재 후보 카드 조회
            RectTransform buttonRect = CreateRect("Choice_" + index, rootRect); // 후보 버튼 영역 생성
            int pageCount = Math.Min(CardsPerPage, cards.Count - index / CardsPerPage * CardsPerPage); // 현재 페이지 후보 수 계산
            int slot = index % CardsPerPage; // 페이지 내 후보 위치 계산
            buttonRect.anchoredPosition = new Vector2((slot - (pageCount - 1) * 0.5f) * 358f, -4f); // 한 장 또는 두 장을 패널 가운데 배치
            buttonRect.sizeDelta = new Vector2(324f, 432f); // 제목·그림·설명의 분리된 후보 크기
            Button button = view.CreateChoice(buttonRect, card); // 기존 카드 데이터를 후보 버튼과 상세 영역에 연결
            button.onClick.AddListener(() => view.Select(card, selected)); // 후보 선택 동작 연결
            view.choiceObjects.Add(buttonRect.gameObject); // 후보 화면 목록 등록
        } // 후보 화면 생성 종료
        if (cards.Count > CardsPerPage) // 여러 페이지 필요 여부 확인
        { // 페이지 이동 화면 생성 시작
            view.previousButton = CreatePageButton("PreviousPage", rootRect, "이전", -174f); // 하단 왼쪽의 직접 이전 페이지 버튼 생성
            view.nextButton = CreatePageButton("NextPage", rootRect, "다음", 174f); // 하단 오른쪽의 직접 다음 페이지 버튼 생성
            view.pageLabel = CreateText("PageLabel", rootRect, string.Empty, 18f); // 페이지 안내 생성
            view.pageLabel.rectTransform.anchoredPosition = new Vector2(0f, FooterY); // 이전·다음 버튼 사이의 페이지 안내 배치
            view.pageLabel.rectTransform.sizeDelta = new Vector2(145f, 36f); // 페이지 위치와 전체 수를 표시할 영역
            view.previousButton.onClick.AddListener(() => view.SetPage(view.currentPage - 1)); // 이전 페이지 이동 연결
            view.nextButton.onClick.AddListener(() => view.SetPage(view.currentPage + 1)); // 다음 페이지 이동 연결
        } // 페이지 이동 화면 생성 종료
        if (cancelled != null) // 확인 없이 손패로 돌아갈 수 있는 선택 요청 확인
        { // 명시적인 취소 입력 추가 시작
            float cancelX = cards.Count > CardsPerPage ? 348f : 0f; // 페이지 조작과 떨어지는 취소 위치 결정
            var cancelButton = CreatePageButton("CancelChoice", rootRect, "취소", cancelX); // 기존 직접 취소 동작과 하단 위치 연결
            cancelButton.onClick.AddListener(() => view.Select(null, _ => cancelled())); // 선택 카드 없이 창을 닫은 뒤 취소 결과 전달
        } // 명시적인 취소 입력 추가 종료
        view.SetPage(0); // 첫 페이지 표시
        return view; // 생성 화면 반환
    } // 화면 생성 종료

    private static void CreatePanel(RectTransform root) // 선택 카드와 조작을 담는 별도 중앙 패널 구성
    { // 선택 패널 외형 적용 시작
        RectTransform panel = CreateRect("Panel", root); // 가림막 위의 중앙 패널 생성
        panel.sizeDelta = new Vector2(920f, 650f); // 제목·카드·하단 조작 전용 패널 크기
        Image frame = panel.gameObject.AddComponent<Image>(); // 패널 배경 이미지 생성
        frame.color = new Color(0.035f, 0.10f, 0.12f, 1f); // 원화 누락에 대비한 어두운 청록 배경
        LobbyUiArt.Apply(frame, "FormationPanel"); // 기존 청록색 금속 테두리 피부 적용
        frame.pixelsPerUnitMultiplier = 2f; // 중앙 패널 크기에 맞춘 테두리 두께
        frame.raycastTarget = false; // 모달 입력 차단을 전체 화면 가림막에 일임
        CreateDivider(panel, 226f); // 제목과 후보 카드 위쪽의 얇은 경계 표시
        CreateDivider(panel, -232f); // 카드와 하단 조작 사이의 얇은 경계 표시
    } // 선택 패널 외형 적용 종료

    private static void CreateDivider(RectTransform parent, float y) // 중앙 패널의 영역 구분선 생성
    { // 패널 구분선 생성 시작
        RectTransform rect = CreateRect("Divider", parent); // 금속 피부 내부 구분선 영역 생성
        rect.anchoredPosition = new Vector2(0f, y); // 패널 중심 기준 한 줄의 높이 지정
        rect.sizeDelta = new Vector2(820f, 2f); // 패널 안쪽의 가로 구분선 크기 지정
        Image divider = rect.gameObject.AddComponent<Image>(); // 단색 경계선 이미지 생성
        divider.color = new Color(0.69f, 0.53f, 0.31f, 0.70f); // 기존 프레임과 어울리는 금빛 경계
        divider.raycastTarget = false; // 패널 경계의 입력 영향 제거
    } // 패널 구분선 생성 종료

    private Button CreateChoice(RectTransform root, CardInstance card) // 그림과 세부 수치를 분리한 후보 선택 버튼 생성
    { // 후보 카드 구성 시작
        Image frame = root.gameObject.AddComponent<Image>(); // 클릭 범위와 일치하는 카드 외형 생성
        frame.color = new Color(0.04f, 0.12f, 0.15f, 1f); // 프레임이 없을 때 사용할 카드 배경
        BattleUiArt.Apply(frame, "CardFrame"); // 기존 제작 전투 카드의 금속 프레임 적용
        Button button = root.gameObject.AddComponent<Button>(); // 후보를 확정하는 실제 버튼 추가
        button.targetGraphic = frame; // 카드 전체 프레임에 포인터 밝기 반영
        ColorBlock colors = button.colors; // 기존 Unity 버튼 강조 색상 조회
        colors.highlightedColor = new Color(1.12f, 1.10f, 1.04f, 1f); // 가리킨 후보의 구분 가능한 프레임 강조
        colors.selectedColor = colors.highlightedColor; // 키보드 선택 상태에도 같은 강조색 반영
        button.colors = colors; // 카드 선택의 기존 버튼 시각 반응에 색상 적용

        TMP_Text name = CreateText("Name", root, card.DisplayName, 26f); // 강화 표시가 포함된 카드 이름 생성
        name.color = new Color(1f, 0.86f, 0.59f); // 이름의 금빛 강조 색상 지정
        name.fontStyle = FontStyles.Bold; // 카드 설명과 구분되는 이름 굵기
        name.rectTransform.anchoredPosition = new Vector2(0f, 176f); // 카드 위쪽의 독립 이름 위치
        name.rectTransform.sizeDelta = new Vector2(254f, 42f); // 장식을 피하는 이름 영역 크기

        TMP_Text owner = CreateText("Owner", root, card.OwnerUnit.DisplayName, 18f); // 기존 카드 소유자 이름 생성
        owner.color = new Color(0.75f, 0.88f, 0.86f); // 카드 수치와 구분되는 소유자 글자색
        owner.alignment = TextAlignmentOptions.MidlineLeft; // 소유자 이름의 한 줄 왼쪽 정렬
        owner.rectTransform.anchoredPosition = new Vector2(-51f, 135f); // 행동력 비용 왼쪽의 소유자 위치
        owner.rectTransform.sizeDelta = new Vector2(150f, 29f); // 비용 영역과 겹치지 않는 소유자 영역
        TMP_Text cost = CreateText("Cost", root, $"AP {card.CostLabel}", 18f); // 실제 할인과 가변 비용을 반영한 행동력 표시
        cost.color = new Color(0.96f, 0.79f, 0.53f); // 행동력 수치의 금빛 강조 색상
        cost.fontStyle = FontStyles.Bold; // 비용 수치 강조 굵기
        cost.alignment = TextAlignmentOptions.MidlineRight; // 카드 오른쪽의 비용 정렬
        cost.rectTransform.anchoredPosition = new Vector2(81f, 135f); // 소유자와 떨어진 비용 위치
        cost.rectTransform.sizeDelta = new Vector2(90f, 29f); // 가변 비용을 포함한 독립 비용 영역

        RectTransform artRect = CreateRect("Artwork", root); // 설명과 분리된 카드 일러스트 영역 생성
        artRect.anchoredPosition = new Vector2(0f, 56f); // 소유자 아래의 독립 그림 위치
        artRect.sizeDelta = new Vector2(252f, 116f); // 비율을 유지하며 그림을 담는 영역
        Image art = artRect.gameObject.AddComponent<Image>(); // 후보 일러스트 이미지 추가
        art.sprite = card.Artwork; // 실제 카드 원화 우선 적용
        art.enabled = art.sprite != null || BattleUiArt.ApplyCardType(art, card.CardType); // 원화가 비어 있을 때 기존 카드 종류 원화 적용
        art.preserveAspect = true; // 일러스트 원본 비율 유지
        art.raycastTarget = false; // 그림 클릭을 실제 후보 버튼에 전달

        RectTransform viewport = CreateRect("DescriptionViewport", root); // 카드 그림과 분리된 설명 표시 창 생성
        viewport.anchoredPosition = new Vector2(0f, -103f); // 그림 아래의 본문 위치 설정
        viewport.sizeDelta = new Vector2(256f, 174f); // 긴 설명을 충분히 담을 독립 본문 크기
        viewport.gameObject.AddComponent<RectMask2D>(); // 카드 본문 밖의 글자 그리기 차단
        TMP_Text details = CreateText("Details", viewport, card.SourceData.Description, 20f); // 기존 카드 설명 전체에 연결된 실제 글자 생성
        details.color = new Color(0.92f, 0.94f, 0.91f); // 길고 짧은 설명에 공통 적용할 읽기 쉬운 색상
        details.alignment = TextAlignmentOptions.TopLeft; // 스크롤 시작과 일치하는 왼쪽 상단 정렬
        details.enableAutoSizing = false; // 해상도 배율에 따라 함께 변하는 본문 글자 크기 유지
        details.textWrappingMode = TextWrappingModes.Normal; // 한글과 영문 모두 본문 너비에 따라 줄바꿈
        details.overflowMode = TextOverflowModes.Overflow; // 실제 전체 문구를 보존하고 화면 마스크에서만 가림
        details.rectTransform.pivot = new Vector2(0.5f, 1f); // 스크롤 본문의 왼쪽 위 시작 기준
        details.rectTransform.anchorMin = Vector2.up; // 본문 왼쪽 위 기준 연결
        details.rectTransform.anchorMax = Vector2.one; // 본문 오른쪽 위 기준 연결
        descriptions.Add(new ChoiceDescription(viewport, details)); // 표시 페이지의 본문 크기 계산 대상 추가
        return button; // 원래 선택 콜백을 연결할 실제 버튼 반환
    } // 후보 카드 구성 종료

    private void LateUpdate() // 표시 중인 본문의 프레임별 변경 확인
    { // 현재 페이지 확인 시작
        RefreshVisibleDescriptions(); // 변경된 현재 페이지 본문만 갱신
    } // 현재 페이지 확인 종료

    private void RefreshVisibleDescriptions() // 현재 페이지에서 다시 표시된 본문 길이 확인
    { // 현재 후보 화면 갱신 시작
        int start = currentPage * CardsPerPage; // 현재 페이지 첫 후보 위치
        int end = Math.Min(start + CardsPerPage, descriptions.Count); // 보이는 후보만 확인할 범위
        for (int index = start; index < end; index++) // 화면에 나온 후보만 확인
        { // 개별 후보 설명 갱신 시작
            descriptions[index].Refresh(); // 변경된 설명 문구의 길이만 다시 계산
        } // 개별 후보 설명 갱신 종료
    } // 현재 후보 화면 갱신 종료

    private void SetPage(int page) // 후보 페이지 표시
    { // 페이지 변경 시작
        int pageCount = Math.Max(1, (choiceObjects.Count + CardsPerPage - 1) / CardsPerPage); // 전체 페이지 수 계산
        currentPage = Mathf.Clamp(page, 0, pageCount - 1); // 유효 페이지 범위 보정
        for (int index = 0; index < choiceObjects.Count; index++) // 전체 후보 화면 순회
        { // 후보 표시 변경 시작
            choiceObjects[index].SetActive(index / CardsPerPage == currentPage); // 현재 페이지 후보만 표시
        } // 후보 표시 변경 종료
        if (previousButton != null && nextButton != null) // 페이지 이동 버튼 존재 확인
        { // 페이지 이동 상태 설정 시작
            previousButton.interactable = currentPage > 0; // 이전 페이지 가능 여부 설정
            nextButton.interactable = currentPage + 1 < pageCount; // 다음 페이지 가능 여부 설정
            pageLabel.text = $"{currentPage + 1} / {pageCount}"; // 현재 페이지 안내 설정
        } // 페이지 이동 상태 설정 종료
        RefreshVisibleDescriptions(); // 새 페이지의 긴 설명을 첫 표시와 함께 맞춤
    } // 페이지 변경 종료
    private static Button CreatePageButton(string objectName, Transform parent, string label, float x) // 페이지 이동 버튼 생성
    { // 이동 버튼 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 이동 버튼 영역 생성
        rect.anchoredPosition = new Vector2(x, FooterY); // 중앙 패널 아래에서 후보와 분리된 조작 위치
        rect.sizeDelta = new Vector2(132f, 53f); // 작은 해상도에서도 누르기 쉬운 조작 영역
        Image background = rect.gameObject.AddComponent<Image>(); // 이동 버튼 배경 추가
        background.color = new Color(0.28f, 0.32f, 0.42f, 1f); // 이동 버튼 색상 설정
        LobbyUiArt.Apply(background, "Button"); // 패널과 맞춘 기존 금속 조작 버튼 피부 적용
        Button button = rect.gameObject.AddComponent<Button>(); // 이동 버튼 추가
        button.targetGraphic = background; // 버튼 배경 연결
        TMP_Text text = CreateText("Label", rect, label, 18f); // 버튼 문구 생성
        text.rectTransform.sizeDelta = new Vector2(102f, 36f); // 버튼 장식 안쪽의 실제 문구 크기
        return button; // 생성 버튼 반환
    } // 이동 버튼 생성 종료
    public void Close() // 선택 화면 닫기
    { // 화면 닫기 시작
        gameObject.SetActive(false); // 화면 즉시 숨기기
        Destroy(gameObject); // 화면 객체 제거
    } // 화면 닫기 종료
    private void Select(CardInstance card, Action<CardInstance> selected) // 후보 카드 선택
    { // 후보 선택 시작
        Close(); // 선택 화면 닫기
        selected?.Invoke(card); // 선택 카드 전달
    } // 후보 선택 종료
    private static RectTransform CreateRect(string objectName, Transform parent) // 화면 영역 생성
    { // 영역 생성 시작
        GameObject item = new GameObject(objectName, typeof(RectTransform)); // 화면 객체 생성
        RectTransform rect = item.GetComponent<RectTransform>(); // 화면 영역 조회
        rect.SetParent(parent, false); // 부모 화면 연결
        return rect; // 생성 영역 반환
    } // 영역 생성 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string value, float size) // 화면 문자 생성
    { // 문자 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 문자 영역 생성
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); // 문자 컴포넌트 추가
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 적용
        text.text = value ?? string.Empty; // 비어 있을 수 있는 표시 문구 설정
        text.fontSize = size; // 글자 크기 설정
        text.enableAutoSizing = true; // 긴 문구 크기 자동 조정
        text.fontSizeMin = 12f; // 최소 글자 크기 설정
        text.fontSizeMax = size; // 최대 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 가운데 정렬 설정
        text.textWrappingMode = TextWrappingModes.NoWrap; // 제목과 조작 문구의 독립 한 줄 배치
        text.overflowMode = TextOverflowModes.Ellipsis; // 긴 이름과 제목의 이웃 영역 침범 방지
        text.raycastTarget = false; // 버튼 입력 방해 방지
        return text; // 생성 문자 반환
    } // 문자 생성 종료

    private sealed class ChoiceDescription // 길이에 맞춰 설명에만 독립 스크롤을 제공하는 후보 내용
    { // 후보 본문 조정 시작
        private const float ScrollGutter = 12f; // 스크롤 위치 표시를 위한 본문 우측 여백
        private readonly RectTransform viewport; // 설명이 실제로 보이는 고정 영역
        private readonly TMP_Text details; // 실제 설명과 스크롤되는 영역
        private string sizedText; // 현재 크기에 반영된 설명 원문
        private ScrollRect scroll; // 본문이 넘칠 때만 추가하는 스크롤 입력 영역
        private Image hitArea; // 본문 휠을 수신할 투명 이미지

        public ChoiceDescription(RectTransform targetViewport, TMP_Text targetDetails) // 후보의 본문과 표시 영역 연결
        { // 설명 조정 객체 생성 시작
            viewport = targetViewport; // 설명이 보이는 고정 사각형 저장
            details = targetDetails; // 설명 글자와 실제 내용 영역 저장
        } // 설명 조정 객체 생성 종료

        public void Refresh() // 설명이 실제로 넘칠 때만 독립 스크롤 구성
        { // 설명 크기 갱신 시작
            string text = details.text; // 현재 표시 중인 설명 문구 조회
            if (string.Equals(sizedText, text, StringComparison.Ordinal)) // 문구가 바뀌지 않은 경우 확인
            { // 이미 크기가 맞는 본문 처리 시작
                return; // 불필요한 TMP 레이아웃 계산 생략
            } // 이미 크기가 맞는 본문 처리 종료

            sizedText = text; // 이번 크기 계산에 사용한 문구 기록
            float height = MeasureHeight(viewport.rect.width); // 원래 설명 너비의 실제 문구 높이 계산
            bool needsScroll = height > viewport.rect.height; // 본문이 고정 표시 범위를 넘는지 확인
            if (needsScroll) // 긴 설명에 스크롤 지원 확인
            { // 스크롤 설명 구성 시작
                EnsureScroll(); // 최초 넘침에만 스크롤 컴포넌트 생성
                height = MeasureHeight(viewport.rect.width - ScrollGutter); // 위치 표시를 뺀 너비에서 높이 재계산
            } // 스크롤 설명 구성 종료

            details.rectTransform.offsetMin = new Vector2(0f, -height); // 전체 문구만큼 실제 본문 높이 적용
            details.rectTransform.offsetMax = new Vector2(needsScroll ? -ScrollGutter : 0f, 0f); // 첫 줄을 표시하고 스크롤 필요 시 오른쪽 여백 확보
            if (scroll != null) // 스크롤을 이전에 생성했는지 확인
            { // 본문 스크롤 활성 상태 반영 시작
                scroll.StopMovement(); // 문구 변경 후 기존 스크롤 움직임 정지
                scroll.enabled = needsScroll; // 실제로 넘치는 설명에서만 스크롤 입력 허용
                scroll.verticalScrollbar.gameObject.SetActive(needsScroll); // 넘치는 본문의 현재 스크롤 위치만 표시
                hitArea.raycastTarget = needsScroll; // 짧은 설명은 기존 카드 버튼에 직접 입력 전달
            } // 본문 스크롤 활성 상태 반영 종료
        } // 설명 크기 갱신 종료

        private float MeasureHeight(float width) // 현재 설명의 줄바꿈 반영 높이 조회
        { // 높이 계산 시작
            return Mathf.Ceil(details.GetPreferredValues(details.text, width, Mathf.Infinity).y); // 실제 TMP 본문을 자르지 않을 정수 높이 반환
        } // 높이 계산 종료

        private void EnsureScroll() // 긴 설명의 휠과 드래그 및 위치 표시 구성
        { // 본문 스크롤 생성 시작
            if (scroll != null) // 이미 스크롤을 구성한 본문 확인
            { // 기존 스크롤 사용 시작
                return; // 같은 본문에 스크롤 중복 생성 방지
            } // 기존 스크롤 사용 종료

            hitArea = viewport.gameObject.AddComponent<Image>(); // 본문 위치의 휠과 드래그를 받을 이미지 생성
            hitArea.color = Color.clear; // 카드 외형을 가리지 않는 투명 입력 영역
            scroll = viewport.gameObject.AddComponent<ScrollRect>(); // 본문에만 적용되는 실제 스크롤 연결
            scroll.viewport = viewport; // 고정된 본문 표시 범위 지정
            scroll.content = details.rectTransform; // 실제로 이동할 설명 글자 영역 지정
            scroll.horizontal = false; // 가로 스크롤 차단
            scroll.inertia = false; // 입력이 끝날 때 본문의 추가 이동 중단
            scroll.movementType = ScrollRect.MovementType.Clamped; // 첫 줄과 마지막 줄 안에서만 본문 이동
            scroll.scrollSensitivity = GameplaySpeedSettings.ScrollSensitivity(28f); // 실제 스크롤 속도 설정 적용

            RectTransform track = PreparationUi.CreatePanel("DescriptionScrollbar", viewport, Vector2.right, Vector2.one); // 영역 오른쪽에 얇은 스크롤 위치 표시 생성
            track.offsetMin = new Vector2(-5f, 3f); // 스크롤 위치 표시의 폭과 아래 여백 지정
            track.offsetMax = new Vector2(-1f, -3f); // 스크롤 위치 표시의 오른쪽과 위 여백 지정
            Image trackImage = track.GetComponent<Image>(); // 얇은 위치 표시 배경 조회
            trackImage.color = new Color(0.04f, 0.10f, 0.12f); // 현재 위치와 대비되는 어두운 배경
            trackImage.raycastTarget = false; // 위치 표시를 눌러도 원래의 카드 클릭 경로 유지
            RectTransform marker = PreparationUi.CreatePanel("Handle", track, Vector2.zero, Vector2.one); // 본문 스크롤 위치 손잡이 표시 생성
            Image handle = marker.GetComponent<Image>(); // 위치 표시 색상을 갖는 이미지 조회
            handle.color = new Color(0.74f, 0.60f, 0.39f); // 금속 테두리와 맞춘 본문 위치 색상
            handle.raycastTarget = false; // 위치 표시의 입력 가로채기 방지
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>(); // 본문과 같은 위치를 보여주는 세로 표시 연결
            bar.direction = Scrollbar.Direction.BottomToTop; // 위에서부터 본문 진행을 표시하는 방향 설정
            bar.handleRect = marker; // 현재 위치를 나타낼 실제 손잡이 연결
            bar.interactable = false; // 카드 클릭을 가로채지 않는 표시 전용 모드
            scroll.verticalScrollbar = bar; // 본문 위치에 따른 손잡이 자동 이동 연결
        } // 본문 스크롤 생성 종료
    } // 후보 본문 조정 종료
} // 선택 화면 종료
