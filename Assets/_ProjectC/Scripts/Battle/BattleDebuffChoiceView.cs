using System; // 선택 완료 알림 사용
using System.Collections.Generic; // 디버프 후보 목록 사용
using TMPro; // 한글 디버프 설명 사용
using UnityEngine; // 전투 선택 화면 생성 사용
using UnityEngine.UI; // 디버프 선택 버튼 사용
public sealed class BattleDebuffChoiceView : MonoBehaviour // 이전할 디버프 한 가지 선택 화면
{ // 디버프 선택 화면 시작
    public static BattleDebuffChoiceView Show(Transform parent, IReadOnlyList<BattleStatusEffectInstance> choices, Action<BattleStatusEffectInstance> selected) // 선택할 디버프 화면 생성
    { // 선택 화면 생성 시작
        GameObject root = new GameObject("DebuffChoicePanel", typeof(RectTransform), typeof(Image)); // 전체 화면 가림막 생성
        RectTransform rootRect = root.GetComponent<RectTransform>(); // 가림막 영역 조회
        rootRect.SetParent(parent, false); // 전투 화면 부모 연결
        rootRect.anchorMin = Vector2.zero; // 가림막 시작 앵커 설정
        rootRect.anchorMax = Vector2.one; // 가림막 끝 앵커 설정
        rootRect.offsetMin = Vector2.zero; // 가림막 시작 여백 제거
        rootRect.offsetMax = Vector2.zero; // 가림막 끝 여백 제거
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 배경 어둡게 표시
        BattleDebuffChoiceView view = root.AddComponent<BattleDebuffChoiceView>(); // 디버프 선택 동작 연결
        TMP_Text title = CreateText("Title", rootRect, "가져올 디버프 하나 선택", 28f); // 선택 안내 문구 생성
        title.rectTransform.anchoredPosition = new Vector2(0f, 130f); // 선택 안내 위치 설정
        title.rectTransform.sizeDelta = new Vector2(700f, 60f); // 선택 안내 크기 설정
        for (int index = 0; index < choices.Count; index++) // 이전할 디버프 후보 순회
        { // 디버프 버튼 생성 시작
            BattleStatusEffectInstance effect = choices[index]; // 현재 디버프 후보 조회
            GameObject target = new GameObject("Choice_" + index, typeof(RectTransform), typeof(Image), typeof(Button)); // 개별 디버프 버튼 생성
            RectTransform rect = target.GetComponent<RectTransform>(); // 디버프 버튼 영역 조회
            rect.SetParent(rootRect, false); // 선택 화면 자식 연결
            rect.anchoredPosition = new Vector2((index - (choices.Count - 1) * 0.5f) * 195f, -10f); // 후보 수에 맞춘 가로 배치
            rect.sizeDelta = new Vector2(180f, 160f); // 선택 버튼 크기 설정
            Image background = target.GetComponent<Image>(); // 선택 버튼 배경 조회
            background.color = new Color(0.16f, 0.19f, 0.27f, 1f); // 기존 전투 선택 화면 색상 적용
            Button button = target.GetComponent<Button>(); // 디버프 버튼 입력 조회
            button.targetGraphic = background; // 버튼 강조 배경 연결
            TMP_Text label = CreateText("Label", rect, $"{effect.DisplayName}\n\n수치 {effect.Value}  ·  {effect.StackCount}중첩\n남은 횟수 {effect.RemainingTurns}", 19f); // 디버프 수치와 중첩 표시
            label.rectTransform.anchorMin = Vector2.zero; // 문구 시작 앵커 설정
            label.rectTransform.anchorMax = Vector2.one; // 문구 끝 앵커 설정
            label.rectTransform.offsetMin = new Vector2(8f, 8f); // 문구 시작 여백 설정
            label.rectTransform.offsetMax = new Vector2(-8f, -8f); // 문구 끝 여백 설정
            button.onClick.AddListener(() => view.Select(effect, selected)); // 디버프 선택 동작 연결
        } // 디버프 버튼 생성 종료
        return view; // 생성된 선택 화면 반환
    } // 선택 화면 생성 종료
    public void Close() // 디버프 선택 화면 닫기
    { // 선택 화면 닫기 시작
        gameObject.SetActive(false); // 선택 화면 즉시 숨김
        Destroy(gameObject); // 선택 화면 객체 제거
    } // 선택 화면 닫기 종료
    private void Select(BattleStatusEffectInstance effect, Action<BattleStatusEffectInstance> selected) // 선택한 디버프 전달
    { // 디버프 선택 시작
        Close(); // 기존 선택 화면 닫기
        selected?.Invoke(effect); // 선택한 디버프 전달
    } // 디버프 선택 종료
    private static TMP_Text CreateText(string name, Transform parent, string value, float size) // 선택 화면 문구 생성
    { // 문구 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 글자 화면 객체 생성
        target.transform.SetParent(parent, false); // 선택 화면 자식 연결
        TMP_Text text = target.GetComponent<TMP_Text>(); // 글자 구성 요소 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 글꼴 적용
        text.text = value; // 안내 문구 적용
        text.fontSize = size; // 글자 크기 설정
        text.enableAutoSizing = true; // 긴 디버프 이름 크기 조정
        text.fontSizeMin = 12f; // 최소 글자 크기 설정
        text.fontSizeMax = size; // 최대 글자 크기 설정
        text.alignment = TextAlignmentOptions.Center; // 글자 가운데 정렬
        text.color = Color.white; // 글자 색상 설정
        text.raycastTarget = false; // 선택 버튼 입력 방해 방지
        return text; // 생성된 문구 반환
    } // 문구 생성 종료
} // 디버프 선택 화면 종료
