using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class BattleCardView : MonoBehaviour // 전투 카드 UI
{
    [Header("카드 연결")] // 카드 연결 구분
    [SerializeField] private Image cardBackground; // 카드 배경 이미지
    [SerializeField] private Image cardImage; // 카드 일러스트 이미지
    [SerializeField] private TMP_Text cardNameText; // 카드 이름 텍스트
    [SerializeField] private TMP_Text costText; // 카드 비용 텍스트
    [SerializeField] private TMP_Text descriptionText; // 카드 설명 텍스트
    [SerializeField] private TMP_Text typeText; // 카드 종류 텍스트
    [SerializeField] private TMP_Text rankText; // 카드 등급 텍스트
    [SerializeField] private Button selectButton; // 카드 선택 버튼

    [Header("선택 표현")] // 선택 표현 구분
    [SerializeField, Min(1f)] private float selectedScale = 1.12f; // 선택 확대 배율
    [SerializeField] private Color normalColor = Color.white; // 기본 배경 색상
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.35f, 1f); // 선택 배경 색상
    [SerializeField] private Color affordableColor = Color.white; // 사용 가능 비용 색상
    [SerializeField] private Color unaffordableColor = new Color(1f, 0.3f, 0.3f, 1f); // AP 부족 비용 색상

    private CardInstance card; // 연결된 카드 인스턴스
    private BattleHandUI handUI; // 손패 UI 관리자

    public CardInstance Card => card; // 연결된 카드 반환

    private void Awake() // 카드 UI 초기화
    {
        if (selectButton == null) // 선택 버튼 연결 확인
        {
            selectButton = GetComponent<Button>(); // 현재 오브젝트 버튼 검색
        }

        if (selectButton != null) // 선택 버튼 존재 확인
        {
            selectButton.onClick.AddListener(HandleSelectRequested); // 클릭 이벤트 연결
        }
    }

    private void OnDestroy() // 카드 UI 제거
    {
        if (selectButton != null) // 선택 버튼 존재 확인
        {
            selectButton.onClick.RemoveListener(HandleSelectRequested); // 클릭 이벤트 해제
        }
    }

    public void Initialize(CardInstance targetCard, BattleHandUI owner) // 카드 UI 정보 설정
    {
        card = targetCard; // 카드 인스턴스 저장
        handUI = owner; // 손패 UI 관리자 저장

        if (card == null) // 카드 누락 확인
        {
            Debug.LogError("Card instance is missing.", this); // 카드 누락 오류
            return; // 초기화 중단
        }

        cardNameText.text = card.CardName; // 카드 이름 표시
        costText.text = card.CurrentCost.ToString(); // 카드 비용 표시
        descriptionText.text = card.Description; // 카드 설명 표시
        typeText.text = card.CardType.ToString(); // 카드 종류 표시
        rankText.text = card.CardRank.ToString(); // 카드 등급 표시
        cardImage.sprite = card.CardImage; // 카드 일러스트 표시
        cardImage.enabled = card.CardImage != null; // 일러스트 존재 여부 반영
        SetSelected(false); // 기본 선택 해제
    }

    public void SetSelected(bool isSelected) // 선택 상태 표시
    {
        float scale = isSelected ? selectedScale : 1f; // 표시 배율 결정
        transform.localScale = Vector3.one * scale; // 카드 크기 적용

        if (cardBackground != null) // 배경 이미지 존재 확인
        {
            cardBackground.color = isSelected ? selectedColor : normalColor; // 배경 색상 적용
        }
    }

    public void SetAffordable(bool isAffordable) // AP 지불 가능 상태 표시
    {
        if (costText == null) // 비용 텍스트 연결 확인
        {
            return; // 색상 변경 중단
        }

        costText.color = isAffordable ? affordableColor : unaffordableColor; // 비용 색상 적용
    }

    private void HandleSelectRequested() // 카드 선택 요청
    {
        if (handUI == null) // 손패 UI 관리자 연결 확인
        {
            Debug.LogWarning("Battle hand UI is missing.", this); // 연결 누락 경고
            return; // 선택 처리 중단
        }

        handUI.SelectCard(this); // 현재 카드 선택 요청
    }
}