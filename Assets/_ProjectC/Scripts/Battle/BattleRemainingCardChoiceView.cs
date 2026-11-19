using System; // 선택 완료 알림 사용
using TMPro; // 한글 화면 문자 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 이미지 사용
public sealed class BattleRemainingCardChoiceView : MonoBehaviour // 남은 카드 처리 선택 화면
{ // 선택 화면 시작
    public static BattleRemainingCardChoiceView Show(Transform parent, CardInstance card, Action<bool> selected) // 선택 화면 생성
    { // 화면 생성 시작
        GameObject root = new GameObject("RemainingCardChoicePanel", typeof(RectTransform), typeof(Image)); // 화면 가림막 생성
        RectTransform rootRect = root.GetComponent<RectTransform>(); // 가림막 영역 조회
        rootRect.SetParent(parent, false); // 전투 화면에 연결
        rootRect.anchorMin = Vector2.zero; // 가림막 시작점 설정
        rootRect.anchorMax = Vector2.one; // 가림막 끝점 설정
        rootRect.offsetMin = Vector2.zero; // 가림막 시작 여백 제거
        rootRect.offsetMax = Vector2.zero; // 가림막 끝 여백 제거
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 가림막 색상 설정
        BattleRemainingCardChoiceView view = root.AddComponent<BattleRemainingCardChoiceView>(); // 선택 화면 컴포넌트 추가
        TMP_Text title = CreateText("Title", rootRect, "남은 카드 처리 선택", 26f); // 선택 제목 생성
        title.rectTransform.anchoredPosition = new Vector2(0f, 145f); // 제목 위치 설정
        title.rectTransform.sizeDelta = new Vector2(480f, 45f); // 제목 크기 설정
        TMP_Text details = CreateText("CardDetails", rootRect, $"{card.DisplayName}\n{card.OwnerUnit.DisplayName} · AP {card.CostLabel}\n{card.SourceData.Description}", 20f); // 남은 카드 정보 표시
        details.rectTransform.anchoredPosition = new Vector2(0f, 35f); // 카드 정보 위치 설정
        details.rectTransform.sizeDelta = new Vector2(420f, 130f); // 카드 정보 크기 설정
        Button keep = CreateButton("KeepRemaining", rootRect, "덱 맨 위에 두기", -120f); // 덱 유지 버튼 생성
        Button discard = CreateButton("DiscardRemaining", rootRect, "버린 카드로 보내기", 120f); // 카드 버리기 버튼 생성
        keep.onClick.AddListener(() => view.Select(false, selected)); // 덱 유지 선택 연결
        discard.onClick.AddListener(() => view.Select(true, selected)); // 카드 버리기 선택 연결
        return view; // 생성 화면 반환
    } // 화면 생성 종료
    public void Close() // 선택 화면 닫기
    { // 화면 닫기 시작
        gameObject.SetActive(false); // 화면 즉시 숨기기
        Destroy(gameObject); // 화면 객체 제거
    } // 화면 닫기 종료
    private void Select(bool discard, Action<bool> selected) // 남은 카드 처리 선택
    { // 선택 처리 시작
        Close(); // 선택 화면 닫기
        selected?.Invoke(discard); // 선택 결과 전달
    } // 선택 처리 종료
    private static Button CreateButton(string objectName, Transform parent, string label, float x) // 선택 버튼 생성
    { // 버튼 생성 시작
        RectTransform rect = CreateRect(objectName, parent); // 버튼 영역 생성
        rect.anchoredPosition = new Vector2(x, -95f); // 버튼 위치 설정
        rect.sizeDelta = new Vector2(210f, 55f); // 버튼 크기 설정
        Image background = rect.gameObject.AddComponent<Image>(); // 버튼 배경 추가
        background.color = new Color(0.25f, 0.30f, 0.40f, 1f); // 버튼 배경 색상 설정
        Button button = rect.gameObject.AddComponent<Button>(); // 선택 버튼 추가
        button.targetGraphic = background; // 버튼 배경 연결
        TMP_Text text = CreateText("Label", rect, label, 18f); // 버튼 문구 생성
        text.rectTransform.sizeDelta = rect.sizeDelta; // 버튼 문구 크기 설정
        return button; // 생성 버튼 반환
    } // 버튼 생성 종료
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
