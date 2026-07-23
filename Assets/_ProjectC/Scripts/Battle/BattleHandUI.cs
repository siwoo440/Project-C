using System.Collections.Generic; // List 자료형 기능
using TMPro; // TextMeshPro UI 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class BattleHandUI : MonoBehaviour // 전투 손패 UI 관리자
{
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleDeckRuntime deckRuntime; // 전투 덱 관리자
    [SerializeField] private BattleActionPointRuntime actionPointRuntime; // 전투 AP 관리자
    [SerializeField] private BattleTargetSelectionRuntime targetSelectionRuntime; // 카드 대상 선택 관리자
    [SerializeField] private BattleTurnRuntime turnRuntime; // 전투 턴 관리자

    [Header("손패 UI 연결")] // 손패 UI 연결 구분
    [SerializeField] private BattleCardView cardPrefab; // 카드 UI 프리팹
    [SerializeField] private Transform cardContainer; // 카드 UI 생성 위치
    [SerializeField] private Button useSelectedCardButton; // 선택 카드 사용 버튼

    [Header("상태 UI 연결")] // 상태 UI 연결 구분
    [SerializeField] private TMP_Text actionPointText; // AP 표시 텍스트
    [SerializeField] private TMP_Text pileStateText; // 카드 더미 표시 텍스트
    [SerializeField] private TMP_Text selectedCardText; // 선택 카드 표시 텍스트

    private readonly List<BattleCardView> cardViews = new List<BattleCardView>(); // 생성된 카드 UI 목록
    private BattleCardView selectedCardView; // 현재 선택 카드 UI

    private void Awake() // 손패 UI 초기화
    {
        if (useSelectedCardButton != null) // 카드 사용 버튼 연결 확인
        {
            useSelectedCardButton.onClick.AddListener(TryUseSelectedCard); // 카드 사용 이벤트 연결
        }
    }

    private void OnEnable() // UI 이벤트 연결
    {
        if (deckRuntime != null) // 덱 관리자 연결 확인
        {
            deckRuntime.CardPilesChanged += HandleCardPilesChanged; // 카드 더미 이벤트 연결
        }

        if (actionPointRuntime != null) // AP 관리자 연결 확인
        {
            actionPointRuntime.ActionPointsChanged += HandleActionPointsChanged; // AP 변경 이벤트 연결
        }

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.TargetSelectionChanged += HandleTargetSelectionChanged; // 대상 선택 이벤트 연결
        }

        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.TurnStateChanged += HandleTurnStateChanged; // 턴 상태 변경 이벤트 연결
        }
    }

    private void Start() // 첫 UI 표시
    {
        RefreshAll(); // 전체 UI 갱신
    }

    private void OnDisable() // UI 이벤트 해제
    {
        if (deckRuntime != null) // 덱 관리자 연결 확인
        {
            deckRuntime.CardPilesChanged -= HandleCardPilesChanged; // 카드 더미 이벤트 해제
        }

        if (actionPointRuntime != null) // AP 관리자 연결 확인
        {
            actionPointRuntime.ActionPointsChanged -= HandleActionPointsChanged; // AP 변경 이벤트 해제
        }

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.TargetSelectionChanged -= HandleTargetSelectionChanged; // 대상 선택 이벤트 해제
        }

        if (turnRuntime != null) // 턴 관리자 연결 확인
        {
            turnRuntime.TurnStateChanged -= HandleTurnStateChanged; // 턴 상태 변경 이벤트 해제
        }
    }

    private void OnDestroy() // UI 제거
    {
        if (useSelectedCardButton != null) // 카드 사용 버튼 연결 확인
        {
            useSelectedCardButton.onClick.RemoveListener(TryUseSelectedCard); // 카드 사용 이벤트 해제
        }
    }

    public void SelectCard(BattleCardView targetView) // 손패 카드 선택
    {
        if (turnRuntime != null && turnRuntime.IsPlayerTurn == false) // 플레이어 턴 여부 확인
        {
            return; // 적 턴 카드 선택 중단
        }
        if (targetView == null) // 카드 UI 누락 확인
        {
            return; // 선택 처리 중단
        }
        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.CancelTargetSelection(); // 기존 대상 선택 모드 취소
        }

        for (int index = 0; index < cardViews.Count; index++) // 전체 카드 UI 순회
        {
            bool isSelected = cardViews[index] == targetView; // 현재 선택 카드 비교
            cardViews[index].SetSelected(isSelected); // 선택 표시 적용
        }

        selectedCardView = targetView; // 현재 선택 카드 저장
        RefreshStatusUI(); // 선택 정보 갱신
    }

    private void TryUseSelectedCard() // 선택 카드 사용 시도
    {
        if (turnRuntime != null && turnRuntime.IsPlayerTurn == false) // 플레이어 턴 여부 확인
        {
            Debug.LogWarning("Cards cannot be used outside the player turn.", this); // 적 턴 카드 사용 경고
            return; // 카드 사용 중단
        }
        if (selectedCardView == null) // 선택 카드 확인
        {
            Debug.LogWarning("No card is selected.", this); // 선택 카드 없음 경고
            return; // 카드 사용 중단
        }

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 덱 관리자 누락 오류
            return; // 카드 사용 중단
        }

        CardInstance selectedCard = selectedCardView.Card; // 선택 카드 인스턴스 가져오기

        if (selectedCard.TargetType == CardTargetType.SingleEnemy) // 적 단일 대상 카드 확인
        {
            if (targetSelectionRuntime == null) // 대상 선택 관리자 연결 확인
            {
                Debug.LogError("Target selection runtime is missing.", this); // 대상 선택 관리자 누락 오류
                return; // 카드 사용 중단
            }

            bool started = targetSelectionRuntime.BeginTargetSelection(selectedCard); // 적 대상 선택 시작

            if (started == false) // 대상 선택 시작 결과 확인
            {
                RefreshStatusUI(); // 카드 사용 상태 다시 표시
            }

            return; // 즉시 카드 소비 방지
        }

        bool used = deckRuntime.TryUseCard(selectedCard); // 대상 없는 카드 즉시 사용

        if (used == false) // 카드 사용 실패 확인
        {
            RefreshStatusUI(); // 사용 가능 상태 다시 표시
        }
    }

    private void RefreshAll() // 전체 전투 UI 갱신
    {
        RefreshHand(); // 손패 UI 갱신
        RefreshStatusUI(); // 상태 UI 갱신
    }

    private void RefreshHand() // 현재 손패 UI 생성
    {
        ClearCardViews(); // 기존 카드 UI 제거

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 연결 누락 오류
            return; // 손패 갱신 중단
        }

        if (cardPrefab == null) // 카드 프리팹 연결 확인
        {
            Debug.LogError("Battle card prefab is missing.", this); // 프리팹 누락 오류
            return; // 손패 갱신 중단
        }

        if (cardContainer == null) // 카드 생성 위치 연결 확인
        {
            Debug.LogError("Card container is missing.", this); // 생성 위치 누락 오류
            return; // 손패 갱신 중단
        }

        for (int index = 0; index < deckRuntime.Hand.Count; index++) // 현재 손패 순회
        {
            CardInstance card = deckRuntime.Hand[index]; // 현재 손패 카드 가져오기
            BattleCardView cardView = Instantiate(cardPrefab, cardContainer); // 카드 UI 생성
            cardView.Initialize(card, this); // 카드 정보 적용
            cardViews.Add(cardView); // 생성된 UI 목록 등록
        }
    }

    private void ClearCardViews() // 기존 카드 UI 제거
    {
        for (int index = 0; index < cardViews.Count; index++) // 생성된 카드 UI 순회
        {
            if (cardViews[index] != null) // 카드 UI 존재 확인
            {
                Destroy(cardViews[index].gameObject); // 카드 UI 오브젝트 제거
            }
        }

        cardViews.Clear(); // 카드 UI 목록 초기화
        selectedCardView = null; // 선택 카드 초기화
    }

    private void RefreshStatusUI() // AP와 카드 상태 갱신
    {
        bool isPlayerTurn = turnRuntime == null || turnRuntime.IsPlayerTurn; // 플레이어 입력 가능 여부 계산

        if (actionPointRuntime != null && actionPointText != null) // AP 표시 가능 여부 확인
        {
            actionPointText.text = $"AP {actionPointRuntime.CurrentActionPoints} / {actionPointRuntime.MaxActionPoints}"; // 현재 AP 표시
        }

        if (deckRuntime != null && pileStateText != null) // 카드 더미 표시 가능 여부 확인
        {
            pileStateText.text = $"DRAW {deckRuntime.DrawPileCount}  HAND {deckRuntime.HandCount}  DISCARD {deckRuntime.DiscardPileCount}  EXHAUST {deckRuntime.ExhaustPileCount}"; // 카드 더미 수 표시
        }

        for (int index = 0; index < cardViews.Count; index++) // 생성된 카드 UI 순회
        {
            CardInstance card = cardViews[index].Card; // 카드 인스턴스 가져오기
            bool isAffordable = isPlayerTurn && actionPointRuntime != null && actionPointRuntime.CanSpend(card.CurrentCost); // 카드 사용 가능 여부 확인
            cardViews[index].SetAffordable(isAffordable); // 비용 색상 적용
            cardViews[index].SetInteractable(isPlayerTurn); // 카드 선택 가능 상태 적용
        }

        bool hasSelection = selectedCardView != null; // 카드 선택 여부 확인
        bool selectingTarget = targetSelectionRuntime != null && targetSelectionRuntime.IsSelectingTarget; // 대상 선택 진행 여부 확인
        bool canUseSelection = isPlayerTurn && hasSelection && deckRuntime != null && deckRuntime.CanUseCard(selectedCardView.Card); // 선택 카드 사용 가능 여부 확인

        if (selectedCardText != null) // 선택 카드 텍스트 연결 확인
        {
            if (selectingTarget) // 대상 선택 진행 여부 확인
            {
                selectedCardText.text = "SELECT AN ENEMY"; // 적 선택 안내 표시
            }
            else // 일반 카드 선택 상태
            {
                selectedCardText.text = hasSelection ? $"SELECTED: {selectedCardView.Card.CardName}" : "SELECT A CARD"; // 선택 카드 이름 표시
            }
        }

        if (useSelectedCardButton != null) // 카드 사용 버튼 연결 확인
        {
            useSelectedCardButton.interactable = canUseSelection && selectingTarget == false; // 대상 선택 중 사용 버튼 비활성화
        }
    }
    private void HandleTurnStateChanged(BattleTurnPhase phase, int round) // 턴 상태 변경 처리
    {
        RefreshStatusUI(); // 카드 입력과 상태 UI 갱신
    }
    private void HandleCardPilesChanged() // 카드 더미 변경 처리
    {
        RefreshAll(); // 손패와 상태 UI 갱신
    }

    private void HandleActionPointsChanged(int currentPoints, int maxPoints) // AP 변경 처리
    {
        RefreshStatusUI(); // AP와 카드 사용 상태 갱신
    }
    private void HandleTargetSelectionChanged() // 대상 선택 상태 변경 처리
    {
        RefreshStatusUI(); // 대상 선택 안내 UI 갱신
    }
}