using System; // 더 선택할 손패가 없는 빈 배열 사용
using System.Collections.Generic; // 손패 비용 후보와 선택 목록 사용
using UnityEngine; // 카드 비용 선택 캔버스 사용

public sealed partial class BattleCardActionController // 실제 카드 행동 관리자의 일부
{ // 부분 동작 시작
    private void BeginWheelPayment(CardInstance source) // 실제 가용 행동력 수만큼 다른 손패 선택 시작
    { // 수레바퀴 선택 준비 시작
        var canvas = handView.GetComponentInParent<Canvas>(); // 원래 카드 선택 모달이 연결될 실제 전투 화면
        if (canvas == null || !turnRuntime.TryBeginPlayerCardDecision()) // 현재 실제 아군 입력 및 모달 환경 확인
        { // 선택을 열 수 없는 경우 시작
            return; // 비용 차감 없이 이전 상태 유지
        } // 선택을 열 수 없는 경우 종료
        CancelSelection(); // 다른 카드의 대상 선택을 지운 뒤 실제 카드 비용 입력
        wheelPaymentSource = source; // 화면 후보와 실제 사용할 원본 연결
        wheelPaymentAmount = sharedActionPoints.CurrentActionPoints; // 후보를 열 실제 행동력과 필요 장수 보관
        if (!CanContinueWheelPayment()) // 입력 잠금 알림 뒤에도 같은 실제 비용과 후보가 유효한지 확인
        { // 알림 도중 바뀐 개시 후보 정리 시작
            CloseWheelPayment(); // 유효한 손패 대신 낡은 선택을 보여 주지 않는 취소
            return; // 변경된 가변 비용에서 모달 생성 생략
        } // 알림 도중 바뀐 개시 후보 정리 종료
        handView.SetInteractionLocked(true); // 선택 중 일반 손패 및 턴과 도주 입력 화면 잠금
        ShowWheelPaymentChoices(canvas); // 현재 실제 손패에 있는 별도 비용 카드 표시
    } // 수레바퀴 선택 준비 종료
    private void ShowWheelPaymentChoices(Canvas canvas) // 아직 고르지 않은 실물 카드만 선택 화면에 표시
    { // 다음 실제 비용 선택 시작
        var candidates = new List<CardInstance>(); // 현재 선택 가능한 실물 손패 카드 목록
        foreach (var card in runtimeDeck.Hand) // 지원 카드의 실제 손패 순서로 비용 후보 순회
        { // 별도의 실물 카드 확인 시작
            if (card != wheelPaymentSource && !wheelPaymentCards.Contains(card)) // 사용할 원본과 앞서 고른 카드의 중복 방지
            { // 실제 다른 카드 후보 추가 시작
                candidates.Add(card); // 손패의 물리 카드 한 장 등록
            } // 실제 다른 카드 후보 추가 종료
        } // 별도의 실물 카드 확인 종료
        int step = wheelPaymentCards.Count; // 현재 새 모달에 대응하는 실제 선택 단계 보관
        string title = $"운명의 수레바퀴 · 제외할 카드 {step + 1}/{wheelPaymentAmount}"; // 필요 장수와 현재 선택을 보여 주는 안내
        wheelPaymentView = BattleDrawChoiceView.Show(canvas.transform, candidates, choice => SelectWheelPaymentCard(canvas, choice, step), title, CloseWheelPayment); // 비용 선택과 취소를 기존 모달 입력에 연결
    } // 다음 실제 비용 선택 종료
    private void SelectWheelPaymentCard(Canvas canvas, CardInstance choice, int step) // 실제 단계의 손패 비용 카드 한 장 선택
    { // 다음 물리 카드 선택 정산 시작
        if (wheelPaymentSource == null || step != wheelPaymentCards.Count) // 이미 종료되거나 앞선 화면의 낡은 입력 확인
        { // 이전 선택 이벤트 차단 시작
            return; // 결제에 오래된 버튼 입력 사용 방지
        } // 이전 선택 이벤트 차단 종료
        wheelPaymentView = null; // 이미 자체 종료된 현재 화면 참조 초기화
        if (!CanContinueWheelPayment() || choice == wheelPaymentSource || !ContainsHandCard(choice) || wheelPaymentCards.Contains(choice)) // 현재 비용과 실제 다른 손패 후보의 유효성 확인
        { // 변경된 원본 또는 비용 입력 처리 시작
            CloseWheelPayment(); // 어떤 비용도 쓰지 않은 원래 손패 상태로 복귀
            return; // 전체 카드 비용 결제 방지
        } // 변경된 원본 또는 비용 입력 처리 종료
        wheelPaymentCards.Add(choice); // 실제로 움직이지 않은 후보를 선택 목록에만 추가
        if (wheelPaymentCards.Count < wheelPaymentAmount) // 아직 더 골라야 할 실제 비용 장수 확인
        { // 다음 카드 선택 시작
            ShowWheelPaymentChoices(canvas); // 앞서 고른 실제 카드를 제외한 후보로 다음 모달 표시
            return; // 전체 비용이 모이기 전 실제 결제 생략
        } // 다음 카드 선택 종료
        var source = wheelPaymentSource; // 입력 잠금을 해제한 뒤 사용할 실제 원본 보관
        var payment = wheelPaymentCards.ToArray(); // 완료한 정확한 실물 카드 선택을 제어 흐름에 고정
        CloseWheelPayment(); // 실제 카드 처리 전에 선택 상태와 모달 잠금 정리
        ExecuteCard(source, CollectValidTargets(source, allyUnitViews), payment); // 현재 유효 아군과 모두 고른 비용으로 원래 카드 사용 확정
    } // 다음 물리 카드 선택 정산 종료
    private bool CanContinueWheelPayment() // 비용 선택 중 실제 턴 및 손패 상태 확인
    { // 유효한 가변 비용 유지 확인 시작
        return turnRuntime.IsPlayerTurn && !turnRuntime.IsBattleEnded && !turnRuntime.HasPendingBossPhaseTransition && // 정상 사용이 가능한 전투 상태와 실제 아군 턴
            wheelPaymentSource != null && !wheelPaymentSource.OwnerUnit.IsDead && ContainsHandCard(wheelPaymentSource) && // 선택이 끝날 때도 기존 원본과 소유자 유지
            sharedActionPoints.CurrentActionPoints == wheelPaymentAmount && turnRuntime.HasHandPaymentOptions(wheelPaymentAmount) && // 여전히 같은 실제 행동력과 충분한 다른 손패
            wheelPaymentCards.TrueForAll(ContainsHandCard); // 이미 고른 모든 물리 카드의 현재 손패 확인
    } // 유효한 가변 비용 유지 확인 종료
    private void RecheckWheelPayment() // 비용 후보가 대기하는 동안의 실제 자원과 턴 변경 확인
    { // 중간 변경 확인 시작
        if (wheelPaymentSource != null && !CanContinueWheelPayment()) // 실제 선택을 마치기 전 자원 또는 원본이 변경되었는지 확인
        { // 더 이상 유효하지 않은 선택 정리 시작
            CloseWheelPayment(); // 부분 선택이나 선택 조건 변경에는 어떠한 비용도 지불하지 않는 취소
        } // 더 이상 유효하지 않은 선택 정리 종료
    } // 중간 변경 확인 종료
    private void CloseWheelPayment() // 실제 가변 비용 선택 또는 취소의 입력 잠금 정리
    { // 원래 일반 턴 입력 복구 시작
        if (wheelPaymentSource == null) // 이미 정리된 다른 카드 상태 확인
        { // 다른 모달 잠금 유지 시작
            return; // 수레바퀴 원본이 없으면 기존 다른 선택 상태 보존
        } // 다른 모달 잠금 유지 종료
        wheelPaymentSource = null; // 상태 이벤트의 재진입 전에 선택 원본 해제
        wheelPaymentAmount = 0; // 이전 선택의 실제 비용 수치 초기화
        wheelPaymentCards.Clear(); // 실제 비용 후보가 이미 넘어갔거나 취소된 뒤 선택 목록 제거
        if (wheelPaymentView != null && wheelPaymentView.gameObject.activeSelf) // 기존 모달에서 아직 열린 실제 선택창 확인
        { // 남은 모달 닫기 시작
            wheelPaymentView.Close(); // 더 이상 유효하지 않은 선택창 즉시 숨기기
        } // 남은 모달 닫기 종료
        wheelPaymentView = null; // 종료한 선택 모달 참조 해제
        turnRuntime.EndPlayerCardDecision(); // 다른 일반 전투 입력의 실제 잠금 해제
        handView.SetInteractionLocked(false); // 원래 손패와 턴 종료 및 도주 버튼 상태 복구
    } // 원래 일반 턴 입력 복구 종료

    internal static int GetPentacleHandChoiceCount(CardInstance card) // 두 실제 손패 선택 카드의 대상 수 확인
    { // 실제 원본 효과와 고를 장수 구분 시작
        return card?.EffectType == CardEffectType.PentacleUtility ? card.SourceData.PentacleRule switch // 원본의 명시된 펜타클 지원 효과 확인
        { // 손패 선택 규칙 구분 시작
            PentacleCardRule.TacticalExchange => 2, // 다른 실물 두 장을 고르는 전술 교환
            PentacleCardRule.EnchantCard => 1, // 다른 실물 한 장을 고르는 카드 강화
            PentacleCardRule.CardCopy => 1, // 이번 실제 턴에 완료한 다른 실물 한 장 복제
            PentacleCardRule.ExchangeProposal => 1, // 먼저 다른 손패 한 장을 버리는 교환 제안
            PentacleCardRule.StrategicStorage => 1, // 보관할 다른 손패 한 장의 실제 선택
            PentacleCardRule.HandSorting => 1, // 카드 한 장 이상을 직접 완료해야 하는 손패 정리
            PentacleCardRule.Alchemy or PentacleCardRule.FateManipulation => 1, // 다른 일반 손패나 현재 뽑을 더미 실물 한 장의 명시 선택
            _ => 0 // 즉시 사용하거나 각자 별도 입력을 하는 다른 카드
        } : 0; // 다른 종류 카드의 기존 사용 경로
    } // 실제 원본 효과와 고를 장수 구분 종료

    private void ShowPentacleHandChoice(CardInstance source) // 확정 전 다른 실제 손패 후보 선택 창
    { // 손패 선택의 모달 시작 전 조건 검사
        var canvas = handView.GetComponentInParent<Canvas>(); // 기존 선택 화면을 연결할 전투 캔버스
        if (canvas == null || GetPentacleChoiceCandidates(source).Count < GetPentacleHandChoiceCount(source) || !turnRuntime.TryBeginPlayerCardDecision()) // 실제 후보 수와 중첩 없는 공용 입력 잠금 확인
        { // 무효 손패 선택 시작 생략
            return; // 행동력과 원래 카드 목록 보존
        } // 무효 손패 선택 시작 생략 종료
        CancelSelection(); // 남아 있는 다른 카드의 대상 강조 초기화
        pentacleHandChoiceSource = source; // 잠금을 연 실제 원본을 현재 선택에 기록
        handView.SetInteractionLocked(true); // 다른 손패와 턴 종료 조작을 선택하는 동안 차단
        OpenPentacleHandChoice(canvas, source); // 첫 선택 또는 사전에 정해진 단일 선택 표시
    } // 손패 선택의 모달 시작 전 조건 검사 종료

    private void OpenPentacleHandChoice(Canvas canvas, CardInstance source) // 다른 인스턴스만 보여 주는 각 선택 단계
    { // 실제 현재 손패 후보 준비 시작
        var candidates = GetPentacleChoiceCandidates(source); // 현재 모달의 실제 손패 또는 이번 턴 사용 후보
        int stillNeeded = GetPentacleHandChoiceCount(source) - (firstTacticalDiscard == null ? 0 : 1); // 손패 정리의 첫 선택 이후에는 추가 필수 후보 없음
        if (candidates.Count < stillNeeded) // 항상 시작할 때 정한 필요 장수를 채울 수 있는지 확인
        { // 현재 손패 후보 부족 처리 시작
            ClosePentacleHandChoice(); // 비용과 효과 없이 전체 선택 취소
            return; // 부족한 후보로 선택을 이어 가지 않음
        } // 현재 손패 후보 부족 처리 종료
        string title = source.SourceData.PentacleRule switch // 현재 모달에 필요한 실제 출처와 장수 표시
        { // 입력할 카드 종류별 원래 표시 시작
            PentacleCardRule.CardCopy => "이번 턴에 사용한 카드 선택", // 완료한 사용 기록의 실제 후보 안내
            PentacleCardRule.EnchantCard => "강화할 다른 카드 선택", // 다음 효과가 강화될 실제 손패 안내
            PentacleCardRule.Alchemy => "연금술로 바꿀 일반 카드 선택", // 손패에서 희귀로 변환할 일반 실물 안내
            PentacleCardRule.FateManipulation => "뽑을 더미에서 가져올 카드 선택", // 현재 덱의 카드 목록에서 옮길 실물 안내
            PentacleCardRule.ExchangeProposal => "버릴 다른 카드 한 장 선택", // 교환 제안의 필수 한 장 안내
            PentacleCardRule.StrategicStorage => "다음 턴까지 보관할 다른 카드 선택", // 실제 이월할 다른 물리 손패 안내
            PentacleCardRule.HandSorting => firstTacticalDiscard == null ? "교체할 카드 최대 두 장 선택" : secondSortingDiscard == null ? "한 장 선택 · 더 고르거나 완료" : "두 장 선택 · 완료", // 확인 또는 추가 선택이 가능한 현재 손패 정리 단계
            _ => firstTacticalDiscard == null ? "버릴 첫 번째 카드 선택" : "버릴 두 번째 카드 선택" // 서로 다른 두 비용 카드 단계 구분
        }; // 입력할 카드 종류별 원래 표시 종료
        BattleDrawChoiceView opened = null; // 뒤늦은 이전 선택 신호를 거를 현재 모달 참조
        Action confirmed = source.SourceData.PentacleRule == PentacleCardRule.HandSorting && firstTacticalDiscard != null // 최소 실제 한 장을 고른 손패 정리에서만 완료 허용
            ? () => FinishHandSorting(opened, source) : null; // 완료 버튼에만 공용 행동력 결제 연결
        opened = BattleDrawChoiceView.Show(canvas.transform, candidates, candidate => FinishPentacleHandChoice(opened, canvas, source, candidate), title, // 실제 선택한 손패의 현재 시점 확인과 단계 안내
            () => FinishPentacleHandChoice(opened, canvas, source, null), confirmed: confirmed); // 어느 단계든 취소와 명시 완료를 구분해 연결
        pentacleHandChoiceView = opened; // 실제 선택 창을 현재 전투에 연결
    } // 실제 현재 손패 후보 준비 종료

    private List<CardInstance> GetPentacleChoiceCandidates(CardInstance source) // 원래 손패와 실제 이번 턴 사용 후보 조회
    { // 원본에 맞는 후보 영역 선택 시작
        IReadOnlyList<CardInstance> possible = source.SourceData.PentacleRule switch // 현재 선택에서 참조할 실제 카드 영역
        { // 원본에 맞춘 후보 영역 구분 시작
            PentacleCardRule.CardCopy => usedCardsThisTurn, // 실제 이번 턴의 원래 사용 기록 반환
            PentacleCardRule.FateManipulation => runtimeDeck.PrepareDrawChoices(runtimeDeck.DrawPileCount, recycleDiscard: false), // 현재 뽑을 더미 전체를 원래 위쪽부터 조회
            PentacleCardRule.HandSorting when secondSortingDiscard != null => Array.Empty<CardInstance>(), // 두 장을 이미 골랐으면 확인과 취소만 노출
            _ => runtimeDeck.Hand // 다른 일반 선택은 물리 손패만 조회
        }; // 원본에 맞춘 후보 영역 구분 종료
        var candidates = new List<CardInstance>(); // 현재 승인할 다른 실제 카드 목록
        foreach (var candidate in possible) // 실제 원본 영역의 기존 후보 순서 순회
        { // 현재 고를 수 있는 다른 실물인지 확인
            if (CanChoosePentacleCard(source, candidate)) // 후보의 현재 유효 영역과 원래 카드 제외 확인
            { // 모달에 실제 선택 후보 추가 시작
                candidates.Add(candidate); // 정상 완료하거나 실제 손패에 남은 같은 실물만 추가
            } // 모달에 실제 선택 후보 추가 종료
        } // 현재 고를 수 있는 다른 실물인지 확인 종료
        return candidates; // 원래 순서의 안전한 모달 후보 반환
    } // 원본에 맞는 후보 영역 선택 종료

    private bool CanChoosePentacleCard(CardInstance source, CardInstance candidate) // 카드별 확정 직전 실제 후보 재검증
    { // 원본과 다른 실물 조건 확인 시작
        if (candidate == null || candidate == source || candidate == firstTacticalDiscard || candidate == secondSortingDiscard) // 무효 카드 및 원본과 이미 선택한 두 실물 제외
        { // 중복 및 빈 후보 처리 시작
            return false; // 같은 실물의 자기 선택 차단
        } // 중복 및 빈 후보 처리 종료
        return source.SourceData.PentacleRule switch // 카드별로 허용하는 실제 후보 영역 구분
        { // 후보 선택 종류 확인 시작
            PentacleCardRule.CardCopy => usedCardsRound == turnRuntime.CurrentRound && usedCardsThisTurn.Contains(candidate), // 이번 턴의 실제 완료 이력에 같은 실물 확인
            PentacleCardRule.StrategicStorage => runtimeDeck.CanStoreHandCard(candidate), // 생존 사용자의 휘발하지 않는 다른 실물만 보관
            PentacleCardRule.Alchemy => ContainsHandCard(candidate) && GetAlchemyCandidates(candidate).Count > 0, // 손패의 생존 소유자 일반 카드와 현재 희귀 후보 확인
            PentacleCardRule.FateManipulation => runtimeDeck.CanTakeSelectedDrawCard(candidate), // 가상 카드나 버린 카드 대신 현재 덱에 남은 실물 확인
            _ => ContainsHandCard(candidate) // 기존 다른 실물의 정상 손패 확인
        }; // 후보 선택 종류 확인 종료
    } // 원본과 다른 실물 조건 확인 종료

    private void FinishPentacleHandChoice(BattleDrawChoiceView opened, Canvas canvas, CardInstance source, CardInstance candidate) // 실제 모달 확인과 취소 및 다음 단계
    { // 닫힌 이전 모달의 이벤트를 제외하는 현재 단계 처리 시작
        if (opened != pentacleHandChoiceView || source != pentacleHandChoiceSource) // 현재 실제 모달과 원본 카드 일치 확인
        { // 오래된 모달 이벤트 생략 시작
            return; // 지금의 손패나 선택 상태 유지
        } // 오래된 모달 이벤트 생략 종료
        pentacleHandChoiceView = null; // 선택한 이전 창을 다시 닫지 않도록 참조 정리
        if (!CanChoosePentacleCard(source, candidate) || !ContainsHandCard(source) || source.OwnerUnit.IsDead) // 원본과 선택 실물 및 생존 시전자의 최신 상태 확인
        { // 취소 또는 무효 선택 처리 시작
            ClosePentacleHandChoice(); // 지불이나 버림 없이 입력 잠금 복구
            return; // 정상 카드 실행을 시작하지 않는 전체 취소
        } // 취소 또는 무효 선택 처리 종료
        if (source.SourceData.PentacleRule == PentacleCardRule.HandSorting) // 명시 확인 버튼이 필요한 선택 묶음의 실물 카드 구분
        { // 현재 선택만 보관하고 결제는 기다리는 처리 시작
            if (firstTacticalDiscard == null) // 아직 선택하지 않은 처음 카드 확인
            { // 실제 첫 물리 카드 기록 시작
                firstTacticalDiscard = candidate; // 현재 손패를 움직이지 않고 첫 선택 보관
            } // 실제 첫 물리 카드 기록 종료
            else // 앞선 한 장이 이미 현재 선택에 있는 경우
            { // 두 번째 다른 실물 기록 시작
                secondSortingDiscard = candidate; // 완료 확인 전 두 번째 물리 카드 기억
            } // 두 번째 다른 실물 기록 종료
            OpenPentacleHandChoice(canvas, source); // 추가 선택 또는 명시 완료 입력 화면으로 진행
            return; // 후보를 누르기만 한 경우 비용 및 드로우 없음
        } // 현재 선택만 보관하고 결제는 기다리는 처리 종료
        if (GetPentacleHandChoiceCount(source) == 2 && firstTacticalDiscard == null) // 전술 교환 첫 장의 미지급 선택 확인
        { // 같은 화면 규칙으로 다른 두 번째 카드 선택 시작
            firstTacticalDiscard = candidate; // 원본 손패의 동일 실물과 다른 후보만 남기기
            OpenPentacleHandChoice(canvas, source); // 비용을 내지 않은 상태로 두 번째 후보 표시
            return; // 최종 확인 이후에만 실제 카드 사용
        } // 같은 화면 규칙으로 다른 두 번째 카드 선택 종료
        var choices = firstTacticalDiscard == null ? new[] { candidate } : new[] { firstTacticalDiscard, candidate }; // 실제 사용할 때 재검사할 고유 실물 선택 확보
        ClosePentacleHandChoice(); // 실제 공용 턴 입력 복구 후 일반 카드 사용 진입
        ExecuteCard(source, CreateSingleTargetList(source.OwnerUnit), pentacleHandCards: choices); // 모든 실제 후보를 확인한 정상 자기 대상 사용
    } // 닫힌 이전 모달의 이벤트를 제외하는 현재 단계 처리 종료

    private void FinishHandSorting(BattleDrawChoiceView opened, CardInstance source) // 최소 한 장 정리의 실제 완료 버튼 입력 처리
    { // 현재 모달의 고른 카드와 기존 실물 위치 확인 시작
        if (opened != pentacleHandChoiceView || source != pentacleHandChoiceSource) // 이미 닫힌 오래된 확인 이벤트 제외
        { // 오래된 확인 무시 시작
            return; // 진행 중인 다른 카드와 원래 손패 유지
        } // 오래된 확인 무시 종료
        pentacleHandChoiceView = null; // 완료 버튼이 닫은 원래 모달의 참조 해제
        var choices = secondSortingDiscard == null ? new[] { firstTacticalDiscard } : new[] { firstTacticalDiscard, secondSortingDiscard }; // 확인한 첫 카드와 선택한 두 번째 실제 실물 확정
        ClosePentacleHandChoice(); // 현재 손패 후보 상태를 비우고 일반 실행 잠금 복구
        ExecuteCard(source, CreateSingleTargetList(source.OwnerUnit), pentacleHandCards: choices); // 실제 물리 카드와 원본 비용을 한 번 더 확인한 뒤 정산
    } // 현재 모달의 고른 카드와 기존 실물 위치 확인 종료

    private void ClosePentacleHandChoice() // 미지불 펜타클 손패 모달과 실제 입력 상태 해제
    { // 현재 모달을 소유한 경우만 잠금 해제 시작
        if (pentacleHandChoiceSource == null) // 다른 모달에는 손대지 않을 실제 소유 확인
        { // 우리 선택이 없을 때 처리 시작
            return; // 기존 카드 사용과 입력 상태 유지
        } // 우리 선택이 없을 때 처리 종료
        var opened = pentacleHandChoiceView; // 정상 닫을 화면 참조 확보
        pentacleHandChoiceView = null; // 잠금 변경 이벤트 전에 선택 화면 참조 제거
        pentacleHandChoiceSource = null; // 다시 호출돼도 현재 사용을 중복 종료하지 않는 원본 해제
        firstTacticalDiscard = null; // 첫 선택만 된 카드의 미정 상태 해제
        secondSortingDiscard = null; // 명시 확인 전의 두 번째 손패 정리 실물을 잊기
        opened?.Close(); // 화면에서 이전 미지불 후보 숨기기
        turnRuntime.EndPlayerCardDecision(); // 현재 턴의 공유 카드 선택 잠금 반환
        handView.SetInteractionLocked(false); // 다른 손패와 정상 전투 입력 복구
    } // 현재 모달을 소유한 경우만 잠금 해제 종료
} // 부분 동작 종료
