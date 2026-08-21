using System.Collections.Generic; // 목록 자료형 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleHandView : MonoBehaviour // 전투 손패 화면 관리
{ // 클래스 시작
    private readonly List<BattleCardView> spawnedCardViews = new List<BattleCardView>(); // 생성된 카드 화면 목록
    private RectTransform handLayoutRoot; // 손패 가로 배치 영역
    private TMP_Text deckStatusText; // 카드 영역 수량 텍스트
    private BattleDeckRuntime runtimeDeck; // 연결된 런타임 덱
    private bool visualStructureCreated; // 손패 화면 구조 생성 여부
    public BattleDeckRuntime RuntimeDeck => runtimeDeck; // 연결된 런타임 덱 조회
    private void Awake() // 손패 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 손패 내부 UI 자동 생성
        Refresh(); // 초기 화면 갱신
    } // 화면 준비 종료
    public bool Bind(BattleDeckRuntime battleDeck) // 런타임 덱 연결
    { // 덱 연결 시작
        if (battleDeck == null) // 런타임 덱 누락 확인
        { // 덱 누락 처리 시작
            Debug.LogError("[BattleHandView] 연결할 런타임 덱이 없습니다.", this); // 덱 누락 출력
            return false; // 덱 연결 실패 반환
        } // 덱 누락 처리 종료
        Unbind(); // 기존 런타임 덱 연결 해제
        runtimeDeck = battleDeck; // 새 런타임 덱 저장
        runtimeDeck.StateChanged += HandleDeckStateChanged; // 덱 상태 변경 이벤트 등록
        Refresh(); // 현재 덱 상태 표시
        return true; // 덱 연결 성공 반환
    } // 덱 연결 종료
    public void Unbind() // 런타임 덱 연결 해제
    { // 덱 연결 해제 시작
        if (runtimeDeck != null) // 기존 런타임 덱 확인
        { // 이벤트 해제 시작
            runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        } // 이벤트 해제 종료
        runtimeDeck = null; // 런타임 덱 참조 제거
        Refresh(); // 빈 손패 화면 표시
    } // 덱 연결 해제 종료
    private void EnsureVisualStructure() // 손패 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        Image backgroundImage = GetComponent<Image>(); // 카드 영역 배경 조회
        if (backgroundImage == null) // 카드 영역 배경 누락 확인
        { // 카드 영역 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 카드 영역 배경 추가
        } // 카드 영역 배경 추가 종료
        backgroundImage.color = new Color(0.035f, 0.04f, 0.06f, 0.92f); // 카드 영역 배경 색상 설정
        backgroundImage.raycastTarget = false; // 카드 영역 입력 차단 해제
        deckStatusText = CreateText("DeckStatusText", transform, "Waiting for deck", 20f, Color.white); // 카드 수량 텍스트 생성
        RectTransform statusRect = deckStatusText.rectTransform; // 수량 텍스트 RectTransform 조회
        statusRect.anchorMin = new Vector2(0f, 1f); // 수량 텍스트 최소 앵커 설정
        statusRect.anchorMax = new Vector2(1f, 1f); // 수량 텍스트 최대 앵커 설정
        statusRect.pivot = new Vector2(0.5f, 1f); // 수량 텍스트 위쪽 피벗 설정
        statusRect.anchoredPosition = new Vector2(0f, -4f); // 수량 텍스트 위치 설정
        statusRect.sizeDelta = new Vector2(0f, 34f); // 수량 텍스트 높이 설정
        GameObject handRootObject = new GameObject("HandLayoutRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 손패 배치 오브젝트 생성
        handRootObject.transform.SetParent(transform, false); // 손패 배치 부모 연결
        handLayoutRoot = handRootObject.GetComponent<RectTransform>(); // 손패 배치 RectTransform 조회
        handLayoutRoot.anchorMin = Vector2.zero; // 손패 배치 최소 앵커 설정
        handLayoutRoot.anchorMax = Vector2.one; // 손패 배치 최대 앵커 설정
        handLayoutRoot.pivot = new Vector2(0.5f, 0.5f); // 손패 배치 중앙 피벗 설정
        handLayoutRoot.offsetMin = new Vector2(8f, 8f); // 손패 배치 왼쪽 아래 여백 설정
        handLayoutRoot.offsetMax = new Vector2(-8f, -42f); // 손패 배치 오른쪽 위 여백 설정
        HorizontalLayoutGroup layoutGroup = handRootObject.GetComponent<HorizontalLayoutGroup>(); // 손패 가로 배치 조회
        layoutGroup.padding = new RectOffset(8, 8, 8, 8); // 손패 내부 여백 설정
        layoutGroup.spacing = 12f; // 카드 사이 간격 설정
        layoutGroup.childAlignment = TextAnchor.MiddleCenter; // 카드 중앙 정렬 설정
        layoutGroup.childControlWidth = false; // 카드 너비 자동 제어 해제
        layoutGroup.childControlHeight = false; // 카드 높이 자동 제어 해제
        layoutGroup.childForceExpandWidth = false; // 카드 너비 확장 해제
        layoutGroup.childForceExpandHeight = false; // 카드 높이 확장 해제
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private void Refresh() // 손패 화면 갱신
    { // 화면 갱신 시작
        EnsureVisualStructure(); // 손패 내부 UI 확인
        ClearSpawnedCardViews(); // 기존 카드 화면 제거
        if (runtimeDeck == null) // 런타임 덱 연결 확인
        { // 덱 미연결 처리 시작
            deckStatusText.text = "Waiting for deck"; // 덱 미연결 상태 표시
            return; // 화면 갱신 중단
        } // 덱 미연결 처리 종료
        deckStatusText.text = $"Deck {runtimeDeck.DrawPileCount} | Hand {runtimeDeck.HandCount}/{runtimeDeck.MaxHandSize} | Discard {runtimeDeck.DiscardPileCount}"; // 카드 영역 수량 표시
        foreach (CardInstance cardInstance in runtimeDeck.Hand) // 현재 손패 카드 순회
        { // 카드 화면 생성 시작
            BattleCardView cardView = CreateCardView(cardInstance); // 카드 화면 코드 생성
            spawnedCardViews.Add(cardView); // 생성 카드 화면 목록 등록
        } // 카드 화면 생성 종료
    } // 화면 갱신 종료
    private BattleCardView CreateCardView(CardInstance cardInstance) // 카드 화면 코드 생성
    { // 카드 화면 생성 시작
        string objectName = $"Card_{cardInstance.InstanceId}"; // 카드 오브젝트 이름 생성
        GameObject cardObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(BattleCardView)); // 카드 오브젝트 생성
        cardObject.transform.SetParent(handLayoutRoot, false); // 카드 부모 연결
        BattleCardView cardView = cardObject.GetComponent<BattleCardView>(); // 카드 화면 컴포넌트 조회
        cardView.Bind(cardInstance); // 카드 인스턴스 화면 연결
        return cardView; // 생성 카드 화면 반환
    } // 카드 화면 생성 종료
    private void ClearSpawnedCardViews() // 생성 카드 화면 제거
    { // 카드 화면 제거 시작
        foreach (BattleCardView cardView in spawnedCardViews) // 생성 카드 화면 순회
        { // 카드 화면 제거 시작
            if (cardView == null) // 제거된 카드 화면 확인
            { // 제거된 화면 처리 시작
                continue; // 다음 카드 화면 이동
            } // 제거된 화면 처리 종료
            cardView.gameObject.SetActive(false); // 카드 화면 즉시 숨김
            Destroy(cardView.gameObject); // 카드 화면 오브젝트 제거
        } // 카드 화면 제거 종료
        spawnedCardViews.Clear(); // 생성 카드 화면 목록 비우기
    } // 카드 화면 제거 종료
    private void HandleDeckStateChanged() // 덱 상태 변경 처리
    { // 덱 상태 처리 시작
        Refresh(); // 손패 화면 자동 갱신
    } // 덱 상태 처리 종료
    private static TMP_Text CreateText(string objectName, Transform parent, string initialText, float fontSize, Color textColor) // 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.text = initialText; // 초기 텍스트 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = TextAlignmentOptions.Center; // 텍스트 중앙 정렬
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        if (TMP_Settings.defaultFontAsset != null) // 기본 글꼴 존재 확인
        { // 기본 글꼴 적용 시작
            text.font = TMP_Settings.defaultFontAsset; // 기본 글꼴 적용
        } // 기본 글꼴 적용 종료
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private void OnDestroy() // 손패 화면 제거 처리
    { // 제거 처리 시작
        if (runtimeDeck != null) // 연결된 런타임 덱 확인
        { // 이벤트 해제 시작
            runtimeDeck.StateChanged -= HandleDeckStateChanged; // 덱 상태 변경 이벤트 해제
        } // 이벤트 해제 종료
    } // 제거 처리 종료
} // 클래스 종료
