using TMPro; // 상태 수치 표시 사용
using UnityEngine; // 상태 아이콘 화면 객체 사용
using UnityEngine.UI; // 상태 아이콘 이미지 사용
public sealed class BattleStatusIconView : MonoBehaviour // 개별 상태 아이콘과 구석 수치 화면
{ // 개별 상태 아이콘 시작
    private Image icon; // 상태 종류 이미지
    private TMP_Text amount; // 오른쪽 아래 효과 수치
    private TMP_Text duration; // 왼쪽 아래 남은 턴
    public static BattleStatusIconView Create(Transform parent, int index) // 개별 상태 아이콘 생성
    { // 상태 아이콘 생성 시작
        GameObject target = new GameObject("Icon_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleStatusIconView)); // 정사각형 상태 아이콘 생성
        target.transform.SetParent(parent, false); // 상태 아이콘 영역에 연결
        BattleStatusIconView view = target.GetComponent<BattleStatusIconView>(); // 상태 아이콘 화면 조회
        view.Build(); // 상태 이미지와 구석 수치 준비
        return view; // 생성한 상태 아이콘 반환
    } // 상태 아이콘 생성 종료
    public void Bind(BattleStatusEffectInstance effect) // 실제 상태와 표시 수치 연결
    { // 상태 아이콘 연결 시작
        if (effect == null) // 표시할 상태 존재 확인
        { // 상태 누락 처리 시작
            gameObject.SetActive(false); // 상태 아이콘 숨김
            return; // 상태 연결 중단
        } // 상태 누락 처리 종료
        icon.sprite = BattleStatusIconArt.Get(effect.EffectType); // 상태 종류에 맞는 제작 이미지 적용
        icon.color = icon.sprite != null ? Color.white : new Color(0.22f, 0.22f, 0.22f, 1f); // 이미지 누락 시 구분 가능한 임시 배경 표시
        amount.text = effect.EffectiveValue.ToString(); // 오른쪽 아래에 실제 전체 효과 수치 표시
        duration.text = effect.RemainingTurns.ToString(); // 왼쪽 아래에 남은 지속 턴 표시
        gameObject.SetActive(true); // 현재 상태 아이콘 표시
    } // 상태 아이콘 연결 종료
    private void Build() // 상태 이미지와 수치 화면 구성
    { // 상태 아이콘 화면 구성 시작
        RectTransform rect = GetComponent<RectTransform>(); // 아이콘 사각형 조회
        rect.anchorMin = new Vector2(0f, 1f); // 왼쪽 위 최소 기준점
        rect.anchorMax = new Vector2(0f, 1f); // 왼쪽 위 최대 기준점
        rect.pivot = new Vector2(0f, 1f); // 왼쪽 위 배치 기준점
        rect.sizeDelta = new Vector2(34f, 34f); // 읽기 쉬운 정사각형 아이콘 크기
        icon = GetComponent<Image>(); // 상태 아이콘 이미지 조회
        icon.preserveAspect = true; // 상태 아이콘 원본 비율 유지
        icon.raycastTarget = false; // 전투 대상 선택 입력 통과
        GameObject backdrop = new GameObject("Counters", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 아래 수치 배경 생성
        backdrop.transform.SetParent(transform, false); // 상태 아이콘 자식 연결
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>(); // 수치 배경 사각형 조회
        backdropRect.anchorMin = Vector2.zero; // 왼쪽 아래 최소 앵커
        backdropRect.anchorMax = new Vector2(1f, 0.42f); // 아래쪽 수치 영역 최대 앵커
        backdropRect.offsetMin = Vector2.zero; // 수치 배경 시작 여백 제거
        backdropRect.offsetMax = Vector2.zero; // 수치 배경 끝 여백 제거
        Image backdropImage = backdrop.GetComponent<Image>(); // 수치 배경 이미지 조회
        backdropImage.color = new Color(0f, 0f, 0f, 0.78f); // 작은 수치 대비를 높이는 어두운 배경
        backdropImage.raycastTarget = false; // 전투 대상 선택 입력 통과
        duration = CreateCounter("Duration", new Vector2(0f, 0f), new Vector2(0.5f, 0.42f), TextAlignmentOptions.BottomLeft); // 왼쪽 아래 지속 턴 생성
        amount = CreateCounter("Amount", new Vector2(0.5f, 0f), new Vector2(1f, 0.42f), TextAlignmentOptions.BottomRight); // 오른쪽 아래 효과 수치 생성
    } // 상태 아이콘 화면 구성 종료
    private TMP_Text CreateCounter(string name, Vector2 minimum, Vector2 maximum, TextAlignmentOptions alignment) // 상태 아이콘 구석 수치 생성
    { // 상태 수치 생성 시작
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 수치 글자 객체 생성
        target.transform.SetParent(transform, false); // 상태 아이콘 자식 연결
        RectTransform rect = target.GetComponent<RectTransform>(); // 수치 사각형 조회
        rect.anchorMin = minimum; // 수치 최소 앵커 설정
        rect.anchorMax = maximum; // 수치 최대 앵커 설정
        rect.offsetMin = new Vector2(1f, 0f); // 수치 왼쪽 아래 여백 설정
        rect.offsetMax = new Vector2(-1f, 0f); // 수치 오른쪽 위 여백 설정
        TMP_Text text = target.GetComponent<TMP_Text>(); // 수치 글자 조회
        text.font = ProjectCFontProvider.KoreanFontAsset; // 공용 글꼴 적용
        text.fontSize = 14f; // 읽기 쉬운 수치 글자 크기 설정
        text.fontStyle = FontStyles.Bold; // 작은 수치의 굵은 글자 설정
        text.enableAutoSizing = true; // 수치 길이에 맞춘 글자 크기 적용
        text.fontSizeMin = 8f; // 긴 수치의 최소 글자 크기 설정
        text.fontSizeMax = 14f; // 수치 최대 글자 크기 설정
        text.alignment = alignment; // 각 수치의 아래 구석 정렬 설정
        text.color = Color.white; // 수치 밝은 색상 설정
        text.raycastTarget = false; // 전투 대상 선택 입력 통과
        return text; // 생성한 수치 글자 반환
    } // 상태 수치 생성 종료
} // 개별 상태 아이콘 종료
