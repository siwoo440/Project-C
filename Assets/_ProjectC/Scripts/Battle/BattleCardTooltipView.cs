using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleCardTooltipView : MonoBehaviour // 카드 상세 툴팁 화면
{ // 클래스 시작
    private TMP_Text titleText; // 카드 이름 텍스트
    private TMP_Text detailText; // 카드 상세 정보 텍스트
    private bool visualStructureCreated; // 화면 구조 생성 여부
    private void Awake() // 툴팁 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 툴팁 내부 UI 자동 생성
        Hide(); // 시작 툴팁 숨김
    } // 화면 준비 종료
    public void Show(CardInstance cardInstance) // 카드 상세 정보 표시
    { // 툴팁 표시 시작
        if (cardInstance == null) // 카드 연결 여부 확인
        { // 카드 누락 처리 시작
            Hide(); // 툴팁 숨김
            return; // 표시 처리 중단
        } // 카드 누락 처리 종료
        EnsureVisualStructure(); // 툴팁 내부 UI 확인
        titleText.text = cardInstance.DisplayName; // 카드 이름 표시
        string targetLabel = GetTargetLabel(cardInstance.TargetType); // 대상 표시 문구 생성
        string effectDetail = GetEffectDetail(cardInstance); // 효과 상세 문구 생성
        detailText.text = $"소유자 {cardInstance.OwnerUnit.DisplayName}  |  AP {cardInstance.ApCost}  |  대상 {targetLabel}\n효과 {effectDetail}\n{cardInstance.SourceData.Description}"; // 카드 상세 정보 표시
        gameObject.SetActive(true); // 툴팁 오브젝트 표시
        transform.SetAsLastSibling(); // 툴팁 최상단 표시
    } // 툴팁 표시 종료
    public void Hide() // 카드 상세 정보 숨김
    { // 툴팁 숨김 시작
        gameObject.SetActive(false); // 툴팁 오브젝트 숨김
    } // 툴팁 숨김 종료
    private void EnsureVisualStructure() // 툴팁 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        Image backgroundImage = GetComponent<Image>(); // 툴팁 배경 이미지 조회
        if (backgroundImage == null) // 툴팁 배경 누락 확인
        { // 툴팁 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 툴팁 배경 이미지 추가
        } // 툴팁 배경 추가 종료
        backgroundImage.color = new Color(0.035f, 0.045f, 0.07f, 0.98f); // 툴팁 배경 색상 설정
        backgroundImage.raycastTarget = false; // 툴팁 입력 차단 해제
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>(); // 툴팁 입력 제어 조회
        if (canvasGroup == null) // 입력 제어 누락 확인
        { // 입력 제어 추가 시작
            canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 입력 제어 컴포넌트 추가
        } // 입력 제어 추가 종료
        canvasGroup.interactable = false; // 툴팁 상호작용 차단
        canvasGroup.blocksRaycasts = false; // 툴팁 포인터 차단 해제
        titleText = CreateText("TitleText", transform, 22f, Color.white, TextAlignmentOptions.Center); // 카드 이름 텍스트 생성
        SetRect(titleText.rectTransform, new Vector2(0f, 0.72f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -8f)); // 카드 이름 영역 설정
        detailText = CreateText("DetailText", transform, 15f, new Color(0.86f, 0.9f, 1f, 1f), TextAlignmentOptions.TopLeft); // 카드 상세 텍스트 생성
        detailText.overflowMode = TextOverflowModes.Ellipsis; // 넘친 상세 정보 생략 설정
        SetRect(detailText.rectTransform, Vector2.zero, new Vector2(1f, 0.72f), new Vector2(14f, 10f), new Vector2(-14f, -2f)); // 카드 상세 영역 설정
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private static string GetTargetLabel(CardTargetType targetType) // 카드 대상 문구 조회
    { // 대상 문구 조회 시작
        switch (targetType) // 카드 대상 종류 분기
        { // 대상 종류 분기 시작
            case CardTargetType.Self: // 자신 대상
                return "자신"; // 자신 문구 반환
            case CardTargetType.SingleAlly: // 단일 아군 대상
                return "아군 1명"; // 단일 아군 문구 반환
            case CardTargetType.AllAllies: // 전체 아군 대상
                return "모든 아군"; // 전체 아군 문구 반환
            case CardTargetType.SingleEnemy: // 단일 적 대상
                return "적 1명"; // 단일 적 문구 반환
            case CardTargetType.AllEnemies: // 전체 적 대상
                return "모든 적"; // 전체 적 문구 반환
            default: // 알 수 없는 대상
                return "알 수 없음"; // 기본 대상 문구 반환
        } // 대상 종류 분기 종료
    } // 대상 문구 조회 종료
    private static string GetEffectLabel(CardInstance cardInstance) // 카드 효과 문구 조회
    { // 효과 문구 조회 시작
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 효과 처리 시작
            return "체력 회복"; // 회복 문구 반환
        } // 회복 효과 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            return BattleStatusEffectInstance.GetDisplayName(cardInstance.StatusEffectType); // 상태 이상 이름 반환
        } // 상태 이상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs) // 디버프 해제 효과 확인
        { // 디버프 해제 처리 시작
            return "디버프 해제"; // 디버프 해제 문구 반환
        } // 디버프 해제 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 효과 확인
        { // 정신력 효과 처리 시작
            return "정신력 변화"; // 정신력 효과 문구 반환
        } // 정신력 효과 처리 종료
        if (cardInstance.DamageType == BattleDamageType.Magical) // 마법 피해 확인
        { // 마법 피해 처리 시작
            return "마법 피해"; // 마법 피해 문구 반환
        } // 마법 피해 처리 종료
        return "물리 피해"; // 기본 물리 피해 문구 반환
    } // 효과 문구 조회 종료
    private static string GetEffectDetail(CardInstance cardInstance) // 카드 효과 상세 문구 조회
    { // 효과 상세 조회 시작
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs) // 디버프 해제 효과 확인
        { // 디버프 해제 상세 처리 시작
            return "모든 디버프 해제"; // 디버프 해제 상세 반환
        } // 디버프 해제 상세 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 상세 처리 시작
            string effectName = GetEffectLabel(cardInstance); // 상태 이상 이름 조회
            string valueLabel = cardInstance.StatusEffectType == BattleStatusEffectType.StatusImmunity ? string.Empty : $" {cardInstance.EffectValue}"; // 면역 외 효과 수치 생성
            return $"{effectName}{valueLabel}  |  지속 {cardInstance.StatusDuration}T  |  최대 {cardInstance.StatusMaximumStacks}중첩"; // 상태 이상 상세 반환
        } // 상태 이상 상세 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 효과 확인
        { // 정신력 상세 처리 시작
            string sign = cardInstance.MentalChangeValue > 0 ? "+" : string.Empty; // 양수 부호 문구 계산
            return $"정신력 {sign}{cardInstance.MentalChangeValue}"; // 정신력 상세 반환
        } // 정신력 상세 처리 종료
        return $"{GetEffectLabel(cardInstance)} {cardInstance.EffectValue}"; // 피해와 회복 상세 반환
    } // 효과 상세 조회 종료
    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color textColor, TextAlignmentOptions alignment) // 공용 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = alignment; // 텍스트 정렬 설정
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // 사각 영역 설정
    { // 사각 영역 설정 시작
        rectTransform.anchorMin = anchorMin; // 최소 앵커 설정
        rectTransform.anchorMax = anchorMax; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.offsetMin = offsetMin; // 왼쪽 아래 여백 설정
        rectTransform.offsetMax = offsetMax; // 오른쪽 위 여백 설정
    } // 사각 영역 설정 종료
} // 클래스 종료
