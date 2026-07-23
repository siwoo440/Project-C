using System; // 이벤트 자료형 기능
using System.Collections.Generic; // List 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleTargetSelectionRuntime : MonoBehaviour // 카드 대상 선택 관리자
{
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleDeckRuntime deckRuntime; // 전투 덱 관리자

    private readonly List<BattleTargetView> registeredTargets = new List<BattleTargetView>(); // 등록된 전투 대상 목록
    private CardInstance pendingCard; // 대상 선택 대기 카드

    public bool IsSelectingTarget => pendingCard != null; // 대상 선택 진행 여부 반환
    public CardInstance PendingCard => pendingCard; // 대상 선택 대기 카드 반환

    public event Action TargetSelectionChanged; // 대상 선택 상태 변경 이벤트

    private void OnDisable() // 대상 선택 관리자 비활성화
    {
        CancelTargetSelection(); // 진행 중인 대상 선택 취소
    }

    public void RegisterTarget(BattleTargetView targetView) // 전투 대상 등록
    {
        if (targetView == null) // 대상 누락 확인
        {
            return; // 대상 등록 중단
        }

        if (registeredTargets.Contains(targetView)) // 중복 등록 확인
        {
            return; // 중복 등록 중단
        }

        registeredTargets.Add(targetView); // 대상 목록 추가

        if (IsSelectingTarget) // 대상 선택 진행 여부 확인
        {
            targetView.SetTargetable(CanSelectTarget(targetView)); // 현재 선택 가능 상태 적용
        }
    }

    public void UnregisterTarget(BattleTargetView targetView) // 전투 대상 등록 해제
    {
        if (targetView == null) // 대상 누락 확인
        {
            return; // 등록 해제 중단
        }

        targetView.SetTargetable(false); // 대상 윤곽 숨김
        registeredTargets.Remove(targetView); // 대상 목록 제거
    }

    public bool BeginTargetSelection(CardInstance card) // 카드 대상 선택 시작
    {
        CancelTargetSelection(); // 이전 대상 선택 상태 초기화

        if (card == null) // 카드 누락 확인
        {
            Debug.LogWarning("Target card is missing.", this); // 카드 누락 경고
            return false; // 대상 선택 시작 실패
        }

        if (card.TargetType != CardTargetType.SingleEnemy) // 적 단일 대상 카드 확인
        {
            Debug.LogWarning($"Unsupported target type: {card.TargetType}", this); // 지원하지 않는 대상 종류 경고
            return false; // 대상 선택 시작 실패
        }

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 덱 관리자 누락 오류
            return false; // 대상 선택 시작 실패
        }

        if (deckRuntime.CanUseCard(card) == false) // 카드 사용 가능 여부 확인
        {
            Debug.LogWarning($"Card cannot enter target selection: {card.CardName}", this); // 카드 사용 불가 경고
            return false; // 대상 선택 시작 실패
        }

        pendingCard = card; // 대상 선택 대기 카드 저장
        bool targetFound = false; // 선택 가능한 대상 존재 여부 초기화

        for (int index = 0; index < registeredTargets.Count; index++) // 등록 대상 순회
        {
            BattleTargetView targetView = registeredTargets[index]; // 현재 대상 가져오기
            bool targetable = CanSelectTarget(targetView); // 선택 가능 여부 계산
            targetView.SetTargetable(targetable); // 사각형 윤곽 상태 적용

            if (targetable) // 선택 가능 대상 확인
            {
                targetFound = true; // 선택 가능 대상 존재 설정
            }
        }

        if (targetFound == false) // 선택 가능한 적 존재 확인
        {
            pendingCard = null; // 대상 선택 대기 카드 제거
            NotifyTargetSelectionChanged(); // 대상 선택 상태 변경 전달
            Debug.LogWarning("No selectable enemy target exists.", this); // 선택 가능한 적 없음 경고
            return false; // 대상 선택 시작 실패
        }

        NotifyTargetSelectionChanged(); // 대상 선택 시작 상태 전달
        Debug.Log($"Select an enemy for card: {card.CardName}", this); // 대상 선택 시작 출력
        return true; // 대상 선택 시작 성공
    }

    public bool CanSelectTarget(BattleTargetView targetView) // 지정 대상 선택 가능 여부 확인
    {
        if (IsSelectingTarget == false) // 대상 선택 진행 여부 확인
        {
            return false; // 선택 불가 반환
        }

        if (targetView == null) // 대상 표시 누락 확인
        {
            return false; // 선택 불가 반환
        }

        if (targetView.isActiveAndEnabled == false) // 대상 활성화 여부 확인
        {
            return false; // 선택 불가 반환
        }

        BattleUnit targetUnit = targetView.BattleUnit; // 대상 전투 유닛 가져오기

        if (targetUnit == null) // 전투 유닛 누락 확인
        {
            return false; // 선택 불가 반환
        }

        if (targetUnit.IsDefeated) // 대상 전투 불능 확인
        {
            return false; // 선택 불가 반환
        }

        if (pendingCard.TargetType == CardTargetType.SingleEnemy) // 적 단일 대상 카드 확인
        {
            return targetUnit.UnitTeam == BattleUnitTeam.Enemy; // 적 진영 여부 반환
        }

        return false; // 지원하지 않는 대상 선택 불가
    }

    public bool TrySelectTarget(BattleTargetView targetView) // 전투 대상 선택 시도
    {
        if (CanSelectTarget(targetView) == false) // 대상 선택 가능 여부 확인
        {
            Debug.LogWarning("Selected target is not valid.", this); // 잘못된 대상 경고
            return false; // 대상 선택 실패
        }

        if (deckRuntime == null) // 덱 관리자 연결 확인
        {
            Debug.LogError("Battle deck runtime is missing.", this); // 덱 관리자 누락 오류
            return false; // 대상 선택 실패
        }

        CardInstance cardToUse = pendingCard; // 사용할 카드 임시 저장
        BattleUnit selectedUnit = targetView.BattleUnit; // 선택한 전투 유닛 저장
        bool used = deckRuntime.TryUseCard(cardToUse); // 대상 확정 후 카드 사용

        if (used == false) // 카드 사용 결과 확인
        {
            Debug.LogWarning($"Targeted card use failed: {cardToUse.CardName}", this); // 카드 사용 실패 경고
            return false; // 대상 선택 실패
        }

        Debug.Log($"Target selected: {selectedUnit.UnitName} / Card: {cardToUse.CardName}", this); // 선택 대상 결과 출력
        CancelTargetSelection(); // 대상 선택 모드 종료
        return true; // 대상 선택 성공
    }

    public void CancelTargetSelection() // 대상 선택 취소
    {
        for (int index = 0; index < registeredTargets.Count; index++) // 등록 대상 순회
        {
            BattleTargetView targetView = registeredTargets[index]; // 현재 대상 가져오기

            if (targetView != null) // 대상 존재 확인
            {
                targetView.SetTargetable(false); // 대상 윤곽 숨김
            }
        }

        pendingCard = null; // 대상 선택 대기 카드 제거
        NotifyTargetSelectionChanged(); // 대상 선택 취소 상태 전달
    }

    private void NotifyTargetSelectionChanged() // 대상 선택 상태 변경 전달
    {
        TargetSelectionChanged?.Invoke(); // 연결된 UI에 변경 알림
    }
}