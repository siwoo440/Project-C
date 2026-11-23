using UnityEngine; // Prototype IMGUI와 화면 제어 사용

public sealed class RestRoomPrototypeView : MonoBehaviour // 55일차 휴식 Prototype 화면
{
    private const float WindowWidth = 680f; // 휴식 창 기준 너비
    private const float WindowHeight = 720f; // 휴식 창 기준 높이

    private RestRoomRunManager manager; // 휴식 회차 관리자
    private bool isOpen; // 휴식 창 표시 여부
    private bool isHighRisk; // 테스트용 고위험 지역 여부
    private int selectedCardIndex = -1; // 강화 선택 카드 위치
    private Vector2 cardScroll; // 카드 목록 스크롤 위치
    private string resultMessage = "미강화 카드 1장을 선택한 뒤 휴식을 실행하세요."; // 현재 안내 메시지
    private float previousTimeScale = 1f; // 휴식 창 이전 시간 배율

    public void Initialize(RestRoomRunManager runManager) // Prototype 화면 초기화
    {
        manager = runManager; // 휴식 관리자 저장
    }

    private void OnGUI() // Prototype 휴식 UI 표시
    {
        if (manager == null) // 관리자 준비 여부 확인
        {
            return; // 화면 표시 중단
        }

        Rect openButtonRect = new Rect(
            Mathf.Max(10f, Screen.width - 190f),
            20f,
            170f,
            42f); // 우측 상단 휴식 버튼 위치

        if (!isOpen && GUI.Button(openButtonRect, "휴식 방 테스트")) // 휴식 창 열기 입력
        {
            OpenWindow(); // 휴식 창 표시
        }

        if (!isOpen) // 휴식 창 표시 여부 확인
        {
            return; // 창 내용 표시 중단
        }

        Rect windowRect = new Rect(
            Mathf.Max(10f, (Screen.width - WindowWidth) * 0.5f),
            Mathf.Max(10f, (Screen.height - WindowHeight) * 0.5f),
            Mathf.Min(WindowWidth, Screen.width - 20f),
            Mathf.Min(WindowHeight, Screen.height - 20f)); // 화면 중앙 휴식 창 위치

        GUI.ModalWindow(
            GetInstanceID(),
            windowRect,
            DrawRestWindow,
            "55일차 - Prototype 휴식 방"); // 휴식 모달 창 표시
    }

    private void DrawRestWindow(int windowId) // 휴식 창 내용 표시
    {
        GUILayout.Space(8f); // 상단 간격 추가

        GUILayout.Label(
            manager.IsUsed
                ? "상태: 사용 완료"
                : "상태: 사용 가능"); // 휴식 사용 상태 표시

        GUILayout.BeginHorizontal(); // 위험도 선택 행 시작
        GUILayout.Label(
            isHighRisk
                ? "테스트 지역: 고위험 (HP 15%)"
                : "테스트 지역: 일반 (HP 25%)",
            GUILayout.Width(330f)); // 현재 테스트 위험도 표시

        GUI.enabled = !manager.IsUsed; // 사용 전 위험도 변경 허용
        if (GUILayout.Button("일반 / 고위험 전환", GUILayout.Width(180f))) // 테스트 위험도 변경 입력
        {
            isHighRisk = !isHighRisk; // 테스트 위험도 반전
        }
        GUI.enabled = true; // UI 입력 상태 복구
        GUILayout.EndHorizontal(); // 위험도 선택 행 종료

        GUILayout.Space(8f); // 구역 간격 추가
        GUILayout.Label("파티 상태"); // 파티 상태 제목 표시
        DrawPartyState(); // 현재 영속 파티 상태 표시

        GUILayout.Space(8f); // 구역 간격 추가
        GUILayout.Label("강화할 카드 1장 선택"); // 카드 선택 제목 표시

        cardScroll = GUILayout.BeginScrollView(
            cardScroll,
            GUILayout.Height(330f)); // 카드 목록 스크롤 시작

        DrawCardList(); // 현재 회차 카드 목록 표시

        GUILayout.EndScrollView(); // 카드 목록 스크롤 종료

        GUILayout.Space(8f); // 구역 간격 추가
        GUILayout.Label(resultMessage); // 현재 실행 결과 표시

        GUI.enabled = !manager.IsUsed && selectedCardIndex >= 0; // 휴식 실행 가능 상태 적용
        if (GUILayout.Button("휴식 실행", GUILayout.Height(42f))) // 휴식 실행 입력
        {
            if (manager.TryUseRest(selectedCardIndex, isHighRisk, out string message)) // 회복과 강화 실행
            {
                resultMessage = message; // 성공 결과 표시
            }
            else
            {
                resultMessage = message; // 실패 원인 표시
            }
        }
        GUI.enabled = true; // UI 입력 상태 복구

        GUILayout.BeginHorizontal(); // 하단 버튼 행 시작
        if (GUILayout.Button("닫기", GUILayout.Height(34f))) // 휴식 창 닫기 입력
        {
            CloseWindow(); // 휴식 창 숨김
        }

        if (GUILayout.Button("사용 상태만 초기화 (테스트)", GUILayout.Height(34f))) // Prototype 반복 테스트 입력
        {
            manager.ResetPrototypeUsage(); // 휴식 사용 여부만 초기화
            resultMessage = "휴식 사용 상태만 초기화했습니다. 이미 강화된 카드는 유지됩니다."; // 테스트 초기화 안내
        }
        GUILayout.EndHorizontal(); // 하단 버튼 행 종료
    }

    private void DrawPartyState() // 현재 탐사 파티 상태 표시
    {
        BattleResultManager resultManager = BattleResultManager.EnsureInstance(); // 파티 영속 상태 관리자 준비
        if (resultManager.ActiveParty == null) // 등록 파티 확인
        {
            GUILayout.Label("출전 파티가 등록되지 않았습니다."); // 파티 누락 표시
            return; // 파티 표시 중단
        }

        foreach (CharacterData member in resultManager.ActiveParty.Members) // 현재 파티원 순회
        {
            if (member == null) // 빈 파티원 확인
            {
                continue; // 빈 파티원 제외
            }

            if (!resultManager.TryGetSavedAllyState(
                    member,
                    out int currentHealth,
                    out int currentMental,
                    out int deathCount)) // 저장 파티 상태 조회
            {
                continue; // 조회 실패 파티원 제외
            }

            string lifeState = currentHealth <= 0 ? "사망" : "생존"; // 현재 생존 상태 계산

            GUILayout.Label(
                $"{member.DisplayName} | HP {currentHealth}/{member.MaxHealth} | 정신 {currentMental} | {lifeState} | 사망 {deathCount}회"); // 파티원 상태 표시
        }
    }

    private void DrawCardList() // 현재 회차 카드 선택 목록 표시
    {
        RunDeckManager runDeckManager = RunDeckManager.EnsureInstance(); // 회차 덱 관리자 준비
        if (manager.SourceDeck != null) // 원본 덱 연결 확인
        {
            runDeckManager.GetActiveCards(manager.SourceDeck); // 현재 회차 덱 초기화 보장
        }

        if (runDeckManager.CardCount < 1) // 회차 카드 존재 확인
        {
            GUILayout.Label("현재 회차 덱에 카드가 없습니다."); // 카드 없음 표시
            return; // 카드 목록 표시 중단
        }

        for (int index = 0; index < runDeckManager.Cards.Count; index++) // 회차 카드 순회
        {
            RunDeckCardEntry entry = runDeckManager.Cards[index]; // 현재 카드 항목 조회
            if (entry == null || !entry.IsValid()) // 카드 항목 유효성 확인
            {
                continue; // 잘못된 카드 제외
            }

            bool selected = selectedCardIndex == index; // 현재 선택 카드 여부 계산
            string upgradeText = entry.IsUpgraded ? "강화됨" : "미강화"; // 강화 상태 문구 계산
            string selectionText = selected ? "▶ " : string.Empty; // 선택 표시 계산
            string ownerName = entry.Owner != null ? entry.Owner.DisplayName : "소유자 없음"; // 카드 소유자 이름 계산

            GUI.enabled = !manager.IsUsed && entry.CanUpgrade; // 미사용 미강화 카드만 선택 허용
            if (GUILayout.Button(
                    $"{selectionText}{entry.Card.DisplayName} | {ownerName} | {upgradeText}",
                    GUILayout.Height(36f))) // 카드 선택 입력
            {
                selectedCardIndex = index; // 강화 대상 카드 위치 저장
                resultMessage = $"{entry.Card.DisplayName} 카드를 강화 대상으로 선택했습니다."; // 카드 선택 결과 표시
            }
            GUI.enabled = true; // UI 입력 상태 복구
        }
    }

    private void OpenWindow() // 휴식 창 열기
    {
        if (isOpen) // 중복 열기 확인
        {
            return; // 중복 열기 중단
        }

        previousTimeScale = Time.timeScale; // 기존 시간 배율 저장
        Time.timeScale = 0f; // 탐사 진행 일시 정지
        isOpen = true; // 휴식 창 표시
    }

    private void CloseWindow() // 휴식 창 닫기
    {
        if (!isOpen) // 닫힌 상태 확인
        {
            return; // 중복 닫기 중단
        }

        Time.timeScale = previousTimeScale; // 기존 시간 배율 복구
        isOpen = false; // 휴식 창 숨김
    }

    private void OnDisable() // 화면 비활성화 처리
    {
        if (isOpen) // 열린 창 여부 확인
        {
            Time.timeScale = previousTimeScale; // 비활성화 시 시간 배율 복구
            isOpen = false; // 창 표시 상태 초기화
        }
    }
}
