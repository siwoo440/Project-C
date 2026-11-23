using System.Collections.Generic; // 실제 덱과 선택 행 목록 사용
using TMPro; // 휴식 문구와 상태 표시
using UnityEngine; // 휴식 모달과 일시 정지 사용
using UnityEngine.EventSystems; // 모달 이전 선택 복원
using UnityEngine.UI; // 카드 목록과 실행 버튼 사용

public sealed partial class RestRoomPrototypeView : MonoBehaviour // 탐사 휴식 방 모달
{ // 휴식 화면 시작
    private readonly List<CardRow> cardRows = new List<CardRow>(); // 현재 카드 슬롯에 연결된 행
    private RestRoomRunManager manager; // 휴식 회차 관리자
    private RunDeckManager deck; // 표시 중인 실제 탐사 덱
    private BattleResultManager party; // 표시 중인 영속 파티 상태
    private RunDeckCardEntry selectedEntry; // 같은 카드 원본을 구분할 선택 항목
    private int selectedIndex = -1; // 선택한 실제 덱 위치
    private int hazardLevel; // 현재 휴식 방에 고정된 퇴색 단계
    private float previousTimeScale; // 모달 전 탐사 시간 배율
    private GameObject previousFocus; // 모달을 열기 전 UI 선택
    private bool isOpen; // 휴식 모달 표시 여부
    private bool deckDirty; // 다음 프레임에 반영할 덱 변경
    private bool partyDirty; // 다음 프레임에 반영할 파티 변경

    public void Initialize(RestRoomRunManager runManager) // 실제 탐사와 기존 부트스트랩 초기화
    { // 휴식 관리자 초기화 시작
        manager = runManager; // 휴식 회차 관리자 연결
    } // 휴식 관리자 초기화 종료

    public void OpenFromRoom(int roomHazardLevel) // 생성된 휴식 방에서 실제 모달 열기
    { // 모달 열기 시작
        if (isOpen || manager == null || !isActiveAndEnabled) // 중복 호출과 준비 여부 확인
        { // 열기 제한 시작
            return; // 기존 위험도와 시간 배율 유지
        } // 열기 제한 종료

        EnsureUi(); // 재사용할 휴식 캔버스 준비
        hazardLevel = Mathf.Clamp(roomHazardLevel, 0, 3); // 실제 방 위험도 고정
        previousTimeScale = Time.timeScale; // 모달 전 시간 배율 보관
        previousFocus = EventSystem.current?.currentSelectedGameObject; // 모달 전 탐사 UI 선택 보관
        EventSystem.current?.SetSelectedGameObject(null); // 뒤쪽 UI에 전송될 선택 입력 해제
        Time.timeScale = 0f; // 탐사 진행 정지
        isOpen = true; // 휴식 모달 활성 상태 표시
        selectedIndex = -1; // 이전 방의 선택 위치 제거
        selectedEntry = null; // 이전 방의 선택 항목 해제
        deck = RunDeckManager.EnsureInstance(); // 현재 회차 덱 준비
        if (manager.SourceDeck != null) // 현재 출전 덱 등록 여부 확인
        { // 원본 덱 연동 시작
            deck.GetActiveCards(manager.SourceDeck); // 저장 또는 초기 회차 덱 획득
        } // 원본 덱 연동 종료
        party = BattleResultManager.EnsureInstance(); // 현재 파티 상태 연결
        deck.Changed += MarkDeckDirty; // 열린 동안 영속 덱 수정 감시
        party.PartyStateChanged += MarkPartyDirty; // 열린 동안 파티 상태 수정 감시
        modal.SetActive(true); // 실제 휴식 모달 표시
        RefreshRecovery(); // 실제 고정 위험도와 회복 효과 표시
        RefreshParty(); // 현재 파티 수치 표시
        RefreshCards(); // 실제 카드 슬롯별 버튼 표시
        cardScroll.StopMovement(); // 전 방문의 스크롤 관성 해제
        cardScroll.verticalNormalizedPosition = 1f; // 새로 연 목록의 첫 행 표시
        ProjectCLanguageDisplay.SetCaption(resultText, manager.IsUsed ? "이 휴식 방은 이미 사용했습니다." : "강화할 카드를 선택한 뒤 휴식을 실행하세요."); // 현재 휴식 사용 가능 안내
    } // 모달 열기 종료

    public void OpenFromRoom(bool highRisk) // 기존 고위험 호출 호환
    { // 기존 위험도 변환 시작
        OpenFromRoom(highRisk ? 2 : 0); // 기존 고위험을 퇴색 2단계로 연결
    } // 기존 위험도 변환 종료

    private void SelectCard(int index, RunDeckCardEntry entry) // 표시한 카드 슬롯 선택
    { // 휴식 카드 선택 시작
        if (!isOpen || manager.IsUsed || !IsCurrentSlot(index, entry) || !entry.CanUpgrade) // 현재 슬롯과 강화 가능 여부 확인
        { // 사용할 수 없는 선택 처리 시작
            return; // 이미 바뀌거나 잠긴 카드의 선택 차단
        } // 사용할 수 없는 선택 처리 종료
        selectedIndex = index; // 실제 회차 덱 위치 보관
        selectedEntry = entry; // 선택한 카드 복제본 참조 보관
        ProjectCLanguageDisplay.SetCaption(resultText, $"{entry.Card.DisplayName} 카드를 강화 대상으로 선택했습니다."); // 정확한 선택 카드 안내
        RefreshControls(); // 선택 표시와 실행 버튼 상태 갱신
    } // 휴식 카드 선택 종료

    private void ExecuteRest() // 선택한 실물 카드와 파티로 휴식 한 번 실행
    { // 휴식 실행 시작
        if (!isOpen || manager.IsUsed || selectedEntry == null) // 열린 방과 실행 가능 여부 확인
        { // 중복 또는 미선택 처리 시작
            return; // 아직 실행할 수 없는 요청 차단
        } // 중복 또는 미선택 처리 종료
        if (!IsCurrentSlot(selectedIndex, selectedEntry) || !selectedEntry.CanUpgrade) // 화면 선택 뒤 다른 곳에서 변경된 덱 검사
        { // 오래된 카드 슬롯 처리 시작
            RefreshCards(); // 변경된 실제 슬롯의 화면 갱신
            ProjectCLanguageDisplay.SetCaption(resultText, "강화할 수 있는 카드를 선택하세요."); // 바뀐 덱에서 다시 선택 안내
            return; // 같은 위치의 다른 카드 강화 방지
        } // 오래된 카드 슬롯 처리 종료
        manager.TryUseRest(selectedIndex, hazardLevel, out string message); // 정식 파티 회복과 정확한 카드 강화 실행
        RefreshRecovery(); // 실제 사용 완료 상태 표시
        RefreshParty(); // 휴식 결과의 체력과 정신력 표시
        RefreshControls(); // 방 사용 완료와 강화 표시 즉시 반영
        ProjectCLanguageDisplay.SetCaption(resultText, message); // 정식 휴식 성공 또는 실패 안내
    } // 휴식 실행 종료

    private bool IsCurrentSlot(int index, RunDeckCardEntry entry) // 표시 항목이 원래 위치에 남아 있는지 확인
    { // 실제 덱 위치 확인 시작
        return index >= 0 && index < deck.Cards.Count && ReferenceEquals(entry, deck.Cards[index]); // 위치와 항목의 실제 일치 반환
    } // 실제 덱 위치 확인 종료

    private void RefreshCards() // 회차 덱 상태로 스크롤 목록 갱신
    { // 카드 목록 갱신 시작
        bool lostSelection = selectedEntry != null && (!IsCurrentSlot(selectedIndex, selectedEntry) || (!manager.IsUsed && !selectedEntry.CanUpgrade)); // 열린 동안 바뀐 선택의 유효성 확인
        if (lostSelection) // 덱에서 더 이상 고를 수 없는 선택 확인
        { // 선택 해제 시작
            selectedIndex = -1; // 무효한 위치 초기화
            selectedEntry = null; // 무효한 카드 참조 해제
            ProjectCLanguageDisplay.SetCaption(resultText, "강화할 수 있는 카드를 선택하세요."); // 실제 덱에서 다시 선택 안내
        } // 선택 해제 종료

        ClearChildren(cardContent); // 갱신할 기존 카드 행 제거
        cardRows.Clear(); // 기존 UI 카드 슬롯 참조 비우기
        IReadOnlyList<RunDeckCardEntry> entries = deck.Cards; // 실제 저장 데이터 순서 조회
        for (int index = 0; index < entries.Count; index++) // 필터링 전 실제 슬롯별 순회
        { // 실제 카드 슬롯 조회 시작
            RunDeckCardEntry entry = entries[index]; // 원본 슬롯의 현재 카드 항목
            if (entry != null && entry.IsValid()) // 표시할 수 있는 원본 카드 확인
            { // 유효한 카드 행 표시 시작
                cardRows.Add(BuildCardRow(index, entry, cardRows.Count)); // 부여한 표시 행과 원래 슬롯 분리
            } // 유효한 카드 행 표시 종료
        } // 실제 카드 슬롯 조회 종료
        cardContent.sizeDelta = new Vector2(0f, cardRows.Count * RowStep); // 잘리지 않는 전체 스크롤 길이 반영
        cardEmpty.gameObject.SetActive(cardRows.Count == 0); // 실제 목록에 표시할 카드가 없는 경우 안내
        RefreshControls(); // 최신 카드 상태와 버튼 잠금 반영
        deckDirty = false; // 반영한 덱 변경 표시 해제
    } // 카드 목록 갱신 종료

    private void RefreshControls() // 선택 항목과 회차 사용 가능 상태 표시
    { // 카드 선택 표시 갱신 시작
        bool used = manager.IsUsed; // 같은 모달의 실제 회복 사용 여부
        executeButton.interactable = !used && selectedEntry != null && IsCurrentSlot(selectedIndex, selectedEntry) && selectedEntry.CanUpgrade; // 현재 사용 가능한 정확한 카드가 있을 때 실행 허용
        foreach (CardRow row in cardRows) // 실제 덱 항목에 연결된 모든 화면 행 순회
        { // 카드 행의 선택 상태 표시 시작
            bool selected = selectedIndex == row.Index; // 덱 위치를 기준으로 선택 표시
            row.Button.interactable = !used && row.Entry.CanUpgrade; // 사용 완료 또는 최대 강화 카드 차단
            row.Button.image.color = selected ? SelectedRow : (row.Button.interactable ? CardSurface : LockedRow); // 선택과 잠김에 따른 행 배경 적용
            row.SelectionMark.SetActive(selected); // 선택한 슬롯의 금색 테두리 표시
            row.SelectedText.gameObject.SetActive(selected); // 선택한 행의 상태 문구 표시
            string level = row.Entry.IsUpgraded ? $"강화 +{row.Entry.UpgradeLevel}" : "미강화"; // 저장 카드의 현재 강화 단계 표기
            ProjectCLanguageDisplay.SetCaption(row.UpgradeText, level); // 저장 카드의 실제 강화 단계 표시
            ProjectCLanguageDisplay.SetCaption(row.LockText, row.Entry.CanUpgrade ? string.Empty : "최대 강화"); // 최고 단계에 도달한 카드 표시
        } // 카드 행의 선택 상태 표시 종료
    } // 카드 선택 표시 갱신 종료

    private void MarkDeckDirty() // 저장 덱 변경 수신
    { // 덱 변경 예약 시작
        deckDirty = true; // 휴식 사용 기록 뒤 카드 목록 갱신 예약
    } // 덱 변경 예약 종료

    private void MarkPartyDirty() // 저장 파티 변경 수신
    { // 파티 변경 예약 시작
        partyDirty = true; // 다음 프레임 파티 표시 갱신 예약
    } // 파티 변경 예약 종료

    private void LateUpdate() // 열린 화면의 영속 상태 변경 반영
    { // 예약 갱신 처리 시작
        if (deckDirty) // 덱 수정 여부 확인
        { // 예약 카드 표시 시작
            RefreshCards(); // 변경한 카드의 현재 행 표시
        } // 예약 카드 표시 종료
        if (partyDirty) // 파티 상태 수정 여부 확인
        { // 예약 파티 표시 시작
            RefreshParty(); // 변경한 체력과 정신력 표시
        } // 예약 파티 표시 종료
    } // 예약 갱신 처리 종료

    private void CloseWindow() // 모달 입력과 탐사 시간 되돌리기
    { // 휴식 화면 닫기 시작
        if (!isOpen) // 이미 닫힌 화면 확인
        { // 중복 닫기 제한 시작
            return; // 복구한 시간과 탐사 UI 유지
        } // 중복 닫기 제한 종료
        isOpen = false; // 같은 프레임의 버튼 재실행 차단
        if (deck != null) // 씬 정리 중 덱 관리자 생존 확인
        { // 덱 변경 등록 해제 시작
            deck.Changed -= MarkDeckDirty; // 덱 변경 수신 해제
        } // 덱 변경 등록 해제 종료
        if (party != null) // 씬 정리 중 파티 관리자 생존 확인
        { // 파티 변경 등록 해제 시작
            party.PartyStateChanged -= MarkPartyDirty; // 파티 변경 수신 해제
        } // 파티 변경 등록 해제 종료
        deckDirty = false; // 닫힌 모달의 예약된 덱 갱신 취소
        partyDirty = false; // 닫힌 모달의 예약된 파티 갱신 취소
        modal.SetActive(false); // 휴식 모달과 뒤쪽 클릭 차단 해제
        Time.timeScale = previousTimeScale; // 모달 이전 시간 배율 복원
        GameObject focus = previousFocus != null && previousFocus.activeInHierarchy ? previousFocus : null; // 활성 상태의 기존 선택 대상 확인
        EventSystem.current?.SetSelectedGameObject(focus); // 뒤쪽 UI의 모달 이전 선택 복원
        previousFocus = null; // 이전 선택 참조 해제
    } // 휴식 화면 닫기 종료

    private void OnDisable() // 씬이 바뀌거나 화면이 비활성된 경우 정리
    { // 비활성화 시 휴식 모달 복구 시작
        CloseWindow(); // 화면과 일시 정지 상태의 공통 정리 호출
    } // 비활성화 시 휴식 모달 복구 종료
} // 휴식 화면 종료
