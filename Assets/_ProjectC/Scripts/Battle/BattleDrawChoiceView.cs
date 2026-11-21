using System; // 선택 완료 알림 사용
using System.Collections.Generic; // 선택 후보 목록 사용
using TMPro; // 한글 카드 설명 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 이미지 사용
public sealed class BattleDrawChoiceView : MonoBehaviour // 카드 선택 드로우 화면
{ // 선택 화면 시작
    private const int CardsPerPage = 2; // 페이지당 후보 카드 수
    private const int ChoiceSortingOrder = 480; // 전투 레벨 표시와 유닛 안내 위의 선택창 순서
    private readonly List<GameObject> choiceObjects = new List<GameObject>(); // 후보 버튼 목록
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
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 가림막 색상 설정
        BattleDrawChoiceView view = root.AddComponent<BattleDrawChoiceView>(); // 선택 화면 컴포넌트 추가
        TMP_Text title = CreateText("Title", rootRect, titleText, 26f); // 선택 화면 제목 생성
        title.rectTransform.anchoredPosition = new Vector2(0f, 190f); // 제목 위치 설정
        title.rectTransform.sizeDelta = new Vector2(500f, 50f); // 제목 크기 설정
        for (int index = 0; index < cards.Count; index++) // 선택 후보 순회
        { // 후보 화면 생성 시작
            CardInstance card = cards[index]; // 현재 후보 카드 조회
            RectTransform buttonRect = CreateRect("Choice_" + index, rootRect); // 후보 버튼 영역 생성
            int pageCount = Math.Min(CardsPerPage, cards.Count - index / CardsPerPage * CardsPerPage); // 현재 페이지 후보 수 계산
            int slot = index % CardsPerPage; // 페이지 내 후보 위치 계산
            buttonRect.anchoredPosition = new Vector2((slot - (pageCount - 1) * 0.5f) * 230f, -15f); // 후보 버튼 위치 설정
            buttonRect.sizeDelta = new Vector2(210f, 310f); // 후보 버튼 크기 설정
            Image background = buttonRect.gameObject.AddComponent<Image>(); // 후보 버튼 배경 생성
            background.color = new Color(0.16f, 0.19f, 0.27f, 1f); // 후보 버튼 배경 색상 설정
            Button button = buttonRect.gameObject.AddComponent<Button>(); // 후보 선택 버튼 추가
            button.targetGraphic = background; // 선택 버튼 배경 연결
            RectTransform artRect = CreateRect("Artwork", buttonRect); // 후보 일러스트 영역 생성
            artRect.anchoredPosition = new Vector2(0f, 80f); // 후보 일러스트 위치 설정
            artRect.sizeDelta = new Vector2(170f, 110f); // 후보 일러스트 크기 설정
            Image art = artRect.gameObject.AddComponent<Image>(); // 후보 일러스트 추가
            art.sprite = card.Artwork; // 카드 일러스트 연결
            art.preserveAspect = true; // 일러스트 비율 유지
            art.raycastTarget = false; // 버튼 입력 방해 방지
            art.enabled = card.Artwork != null; // 일러스트가 있을 때만 표시
            TMP_Text label = CreateText("Details", buttonRect, $"{card.DisplayName}\n{card.OwnerUnit.DisplayName} · AP {card.CostLabel}\n\n{card.SourceData.Description}", 18f); // 카드 이름과 설명 표시
            label.rectTransform.anchoredPosition = new Vector2(0f, -65f); // 카드 설명 위치 설정
            label.rectTransform.sizeDelta = new Vector2(185f, 150f); // 카드 설명 크기 설정
            button.onClick.AddListener(() => view.Select(card, selected)); // 후보 선택 동작 연결
            view.choiceObjects.Add(buttonRect.gameObject); // 후보 화면 목록 등록
        } // 후보 화면 생성 종료
        if (cards.Count > CardsPerPage) // 여러 페이지 필요 여부 확인
        { // 페이지 이동 화면 생성 시작
            view.previousButton = CreatePageButton("PreviousPage", rootRect, "이전", -350f); // 이전 페이지 버튼 생성
            view.nextButton = CreatePageButton("NextPage", rootRect, "다음", 350f); // 다음 페이지 버튼 생성
            view.pageLabel = CreateText("PageLabel", rootRect, string.Empty, 18f); // 페이지 안내 생성
            view.pageLabel.rectTransform.anchoredPosition = new Vector2(0f, -190f); // 페이지 안내 위치 설정
            view.pageLabel.rectTransform.sizeDelta = new Vector2(200f, 32f); // 페이지 안내 크기 설정
            view.previousButton.onClick.AddListener(() => view.SetPage(view.currentPage - 1)); // 이전 페이지 이동 연결
            view.nextButton.onClick.AddListener(() => view.SetPage(view.currentPage + 1)); // 다음 페이지 이동 연결
        } // 페이지 이동 화면 생성 종료
        if (cancelled != null) // 확인 없이 손패로 돌아갈 수 있는 선택 요청 확인
        { // 명시적인 취소 입력 추가 시작
            var cancelButton = CreatePageButton("CancelChoice", rootRect, "취소", 0f); // 기존 버튼 스타일의 취소 동작 생성
            cancelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -240f); // 카드 후보와 페이지 표시 아래 위치
            cancelButton.onClick.AddListener(() => view.Select(null, _ => cancelled())); // 선택 카드 없이 창을 닫은 뒤 취소 결과 전달
        } // 명시적인 취소 입력 추가 종료
        view.SetPage(0); // 첫 페이지 표시
        return view; // 생성 화면 반환
    } // 화면 생성 종료
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
    } // 페이지 변경 종료
    private static Button CreatePageButton(string objectName, Transform parent, string label, float x) // 페이지 이동 버튼 생성
    { // 이동 버튼 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 이동 버튼 영역 생성
        rect.anchoredPosition = new Vector2(x, -15f); // 이동 버튼 위치 설정
        rect.sizeDelta = new Vector2(95f, 48f); // 이동 버튼 크기 설정
        Image background = rect.gameObject.AddComponent<Image>(); // 이동 버튼 배경 추가
        background.color = new Color(0.28f, 0.32f, 0.42f, 1f); // 이동 버튼 색상 설정
        Button button = rect.gameObject.AddComponent<Button>(); // 이동 버튼 추가
        button.targetGraphic = background; // 버튼 배경 연결
        TMP_Text text = CreateText("Label", rect, label, 18f); // 버튼 문구 생성
        text.rectTransform.sizeDelta = rect.sizeDelta; // 버튼 문구 크기 설정
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
        text.text = value; // 표시 문구 설정
        text.fontSize = size; // 글자 크기 설정
        text.enableAutoSizing = true; // 긴 문구 크기 자동 조정
        text.fontSizeMin = 12f; // 최소 글자 크기 설정
        text.fontSizeMax = size; // 최대 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 가운데 정렬 설정
        text.raycastTarget = false; // 버튼 입력 방해 방지
        return text; // 생성 문자 반환
    } // 문자 생성 종료
} // 선택 화면 종료
