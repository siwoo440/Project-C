using System.Collections; // 코루틴 사용
using System.Collections.Generic; // 버튼 목록 사용
using TMPro; // 텍스트 메시 프로 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // uGUI 마우스 이벤트 시스템 사용
using UnityEngine.InputSystem.UI; // New Input System UI 입력 모듈 사용
using UnityEngine.UI; // UI 구성 요소 사용

public sealed class ExplorationEventPanelView : MonoBehaviour // 탐사 이벤트 패널 UI 표시기
{
    private const int BlurDownsample = 12; // 블러용 축소 비율
    private const float ChoiceHeight = 58f; // 선택 버튼 한 줄 높이
    private const float ChoiceGap = 12f; // 선택 버튼 사이 여백
    private const float ChoiceBottom = 34f; // 패널과 마지막 버튼 사이 여백

    private static ExplorationEventPanelView instance; // 싱글톤 인스턴스

    private readonly List<Button> choiceButtons =
        new List<Button>(); // 현재 선택지 버튼 목록

    private Canvas rootCanvas; // 루트 캔버스
    private RawImage blurBackgroundImage; // 블러 배경 이미지
    private Image dimOverlayImage; // 어두운 오버레이 이미지
    private RectTransform panelRoot; // 이벤트 패널 루트
    private RectTransform contentRect; // 삽화와 본문이 공유하는 기준 영역
    private TMP_Text titleText; // 이벤트 제목 텍스트
    private TMP_Text bodyText; // 이벤트 설명 텍스트
    private Image illustrationFrameImage; // 그림 프레임 이미지
    private Image illustrationImage; // 이벤트 그림 이미지
    private TMP_Text illustrationFallbackText; // 그림 대체 텍스트
    private const int AuthoredWinterfallChoices = 3; // 윈터폴 사건별로 정의한 전용 행동 수
    private const int VisibleChoiceRows = 3; // 한 번에 보여줄 선택지 줄 수
    private ScrollRect choiceScroll; // 선택지 세로 이동 제어
    private RectTransform storyBackground; // 결과 설명 뒤의 장식 패널
    private RectTransform choiceContainer; // 선택지 버튼 컨테이너

    private Coroutine showCoroutine; // 현재 표시 코루틴
    private Texture2D blurTexture; // 현재 블러 텍스처
    private ExplorationEventView currentEventView; // 현재 열린 이벤트 뷰
    private ExplorationEventData currentEventData; // 현재 열린 이벤트 데이터
    private string currentRuntimeEventId; // 현재 열린 이벤트 런타임 ID
    private bool choiceCommitted; // 선택 완료 여부

    public static ExplorationEventPanelView Instance => instance; // 현재 이벤트 패널 조회

    public static ExplorationEventPanelView EnsureInstance() // 이벤트 패널 존재 보장
    {
        if (instance != null)
        {
            return instance; // 기존 이벤트 패널 반환
        }

        instance =
            FindFirstObjectByType<ExplorationEventPanelView>(); // Scene 기존 이벤트 패널 탐색

        if (instance != null)
        {
            return instance; // 탐색한 이벤트 패널 반환
        }

        GameObject panelObject =
            new GameObject("ExplorationEventPanelView"); // 이벤트 패널 오브젝트 생성

        instance =
            panelObject.AddComponent<ExplorationEventPanelView>(); // 이벤트 패널 컴포넌트 추가

        return instance; // 생성한 이벤트 패널 반환
    }

    private void Awake() // 이벤트 패널 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 이벤트 패널 제거
            return;
        }

        instance = this; // 현재 이벤트 패널 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 시 패널 유지
        EnsureUi(); // 이벤트 UI 구성 준비
        HidePanelImmediate(); // 시작 시 패널 숨김
    }

    public void ShowEvent(
        ExplorationEventView eventView,
        ExplorationEventData eventData,
        string runtimeEventId) // 이벤트 표시 요청
    {
        if (eventView == null ||
            eventData == null ||
            string.IsNullOrWhiteSpace(runtimeEventId))
        {
            return; // 잘못된 표시 요청 차단
        }

        EnsureUi(); // UI 구성 보장
        EnsureEventSystem(); // 선택지 버튼용 New Input System 이벤트 시스템 보장

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine); // 이전 표시 코루틴 중단
        }

        currentEventView = eventView; // 현재 이벤트 뷰 저장
        currentEventData = eventData; // 현재 이벤트 데이터 저장
        currentRuntimeEventId = runtimeEventId; // 현재 이벤트 ID 저장
        choiceCommitted = false; // 선택 완료 상태 초기화

        ExplorationPlayerController.SetInputBlocked(true); // 이벤트 중 플레이어 이동 잠금

        showCoroutine =
            StartCoroutine(ShowEventRoutine()); // 이벤트 표시 코루틴 실행
    }

    private IEnumerator ShowEventRoutine() // 블러 캡처 후 이벤트 패널 표시
    {
        panelRoot.gameObject.SetActive(false); // 패널 본문 임시 숨김
        rootCanvas.enabled = true; // 배경 캔버스 활성화
        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.45f); // 캡처 전 어두운 오버레이 표시

        yield return new WaitForEndOfFrame(); // 현재 프레임 렌더 완료 대기

        ApplyBlurCapture(); // 화면 캡처 기반 블러 적용
        PopulateEventContent(); // 이벤트 패널 내용 구성

        panelRoot.gameObject.SetActive(true); // 패널 본문 표시
        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.3f); // 블러 위 오버레이 완화
        showCoroutine = null; // 코루틴 참조 정리
    }

    private void PopulateEventContent() // 이벤트 텍스트와 버튼 구성
    {
        ClearChoiceButtons(); // 이전 선택지 버튼 제거

        titleText.text =
            currentEventData != null
                ? currentEventData.DisplayName
                : "이벤트"; // 이벤트 제목 설정

        bodyText.text =
            currentEventData != null
                ? currentEventData.Description
                : string.Empty; // 이벤트 설명 설정

        if (currentEventData != null &&
            currentEventData.IllustrationSprite != null)
        {
            illustrationImage.enabled = true; // 그림 이미지 사용 활성화
            illustrationImage.sprite = currentEventData.IllustrationSprite; // 그림 스프라이트 적용
            illustrationImage.color = Color.white; // 그림 색상 기본값 지정
            illustrationFallbackText.gameObject.SetActive(false); // 대체 문구 숨김
        }
        else
        {
            illustrationImage.enabled = false; // 그림 이미지 숨김
            illustrationFallbackText.gameObject.SetActive(true); // 대체 문구 표시
            illustrationFallbackText.text = "?"; // 대체 문구 지정
        }

        IReadOnlyList<ExplorationEventChoiceData> choices =
            currentEventData != null
                ? currentEventData.Choices
                : null; // 이벤트 선택지 목록 조회

        if (choices == null)
        {
            CreateCloseButton("[닫기]"); // 비정상 데이터용 닫기 버튼 생성
            return;
        }

        for (int index = 0;
             index < choices.Count;
             index++)
        {
            ExplorationEventChoiceData choiceData =
                choices[index]; // 현재 선택지 조회

            if (choiceData == null ||
                !choiceData.IsValidData())
            {
                continue; // 유효하지 않은 선택지 제외
            }

            int capturedIndex = index; // 클릭용 인덱스 복사

            if (index >= AuthoredWinterfallChoices) // 전용 원문 뒤에 추가한 데이터 선택지 확인
            { // 데이터 선택지 등록 시작
                CreateChoiceButton(choiceData.ChoiceText, () => HandleChoiceSelected(capturedIndex)); // 추가 행동의 원문 표시
                continue; // 원래 세 행동의 전용 설명 생략
            } // 데이터 선택지 등록 종료
            string label = choiceData.ChoiceText; // 행동 이름 조회
            if (currentEventData.EventId == AshstormEyeEvent.Id) // 재폭풍의 눈 비용 안내 여부 확인
            { // 행동 효과 표시 시작
                string[] effects = // 재폭풍의 눈 선택별 비용과 보상
                { // 효과 표시 목록 시작
                    "정신력 -8 · 희귀 카드 1장", // 응시 비용과 보상
                    $"체력 -{AshstormEyeEvent.ShatterHealthDamage} · 철판 +1 · 나사 +1", // 파편 피해와 기존 재료 보상
                    "정신력 +1" // 시선 회피 보상
                }; // 효과 표시 목록 종료
                label += "   " + effects[index]; // 선택지 옆에 효과 표시
            } // 행동 효과 표시 종료
            else if (currentEventData.EventId == AshPileEvent.Id) // 잿더미 조사 비용 안내 여부 확인
            { // 잿더미 선택 효과 표시 시작
                string[] effects = { "체력 -6 · 유물 40%", "철판 -1 · 유물 70%", "정신력 +3" }; // 원문 선택 비용과 확률 표시
                label += "   " + effects[index]; // 행동 확정 전 효과 안내
            } // 잿더미 선택 효과 표시 종료
            else if (currentEventData.EventId == AshMapFragmentEvent.Id) // 지도 조각 선택 안내 여부 확인
            { // 지도 조각 효과 표시 시작
                string[] effects = { "다음 일반 전투 40% 회피 · 1회", "정신력 +2 · 나사 +1", "변화 없음" }; // 선택 전 확인할 실제 효과
                label += "   " + effects[index]; // 기획 행동과 결과 요약 표시
            } // 지도 조각 효과 표시 종료
            else if (currentEventData.EventId == AshWandererCryEvent.Id) // 방랑자의 다음 전투 조건과 기도 확률 안내
            { // 방랑자 선택 안내 시작
                string[] effects = { "정신력 -5 · 적 선제 15% · 승리 카드 1장", "아군 명중 -10%p · 1턴", "정신력 +2 · 회복 물약 30%" }; // 선택 전에 확인할 비용과 후속 결과
                label += "   " + effects[index]; // 선택지 옆 실제 효과 표시
            } // 방랑자 선택 안내 종료
            else if (currentEventData.EventId == GlassFieldEvent.Id) // 유리화된 들판의 비용과 다음 전투 효과 안내
            { // 들판 선택 안내 시작
                string[] effects = // 유리화된 들판 선택별 비용과 보상
                { // 효과 표시 목록 시작
                    $"체력 -{GlassFieldEvent.BreakGlassHealthDamage} · 희귀 또는 저주 각 50%", // 유리 파편 피해와 카드 확률
                    "아군 회피 +10%p · 2턴", // 안전 통과의 다음 전투 회피
                    "아군 명중 -15%p · 1턴" // 달리기의 다음 전투 명중 감소
                }; // 효과 표시 목록 종료
                label += "   " + effects[index]; // 행동 확정 전에 결과 안내
            } // 들판 선택 안내 종료
            else if (currentEventData.EventId == BurntScarecrowEvent.Id) // 허수아비 조사와 소각 비용 안내
            { // 허수아비 효과 표시 시작
                string[] effects = { "정신력 -4 · 희귀 카드 50% / 잿빛 저주 50%", "체력 -4 · 자신의 잿빛 저주 1장 제거", "변화 없음" }; // 원문 선택 비용과 획득 확률
                label += "   " + effects[index]; // 행동 확정 전 실제 효과 안내
            } // 허수아비 효과 표시 종료
            if (currentEventData.EventId == BurntCartEvent.Id) // 수레 선택의 비용과 결과 안내
            { // 수레 안내 시작
                string[] effects = { "철판·나사 각 1개 60% / 사냥꾼 추가 매복 40%", "지정 생존자 체력 -3 · 철판·나사·전선 각 1개", "다음 상점 첫 상품 10% 할인" }; // 수레 선택별 조건
                label += "   " + effects[index]; // 실제 선택 전에 비용과 보상 표시
            } // 수레 안내 종료
            if (currentEventData.EventId == CharredWellEvent.Id) // 우물 행동의 비용과 결과 안내
            { // 우물 안내 시작
                string[] effects = { "체력 -5 · 체력 회복 포션 1개", "정신력 -6 · 피격 생존 시 체력 8 회복 1회", "정신력 +4 · 다음 층 첫 재폭풍 15% 교체" }; // 우물 선택별 효과
                label += "   " + effects[index]; // 행동 확정 전 비용과 효과 표시
            } // 우물 안내 종료
            CreateChoiceButton(label, () => HandleChoiceSelected(capturedIndex)); // 선택지 버튼 생성
        }

        if (choiceButtons.Count == 0)
        {
            CreateCloseButton("[닫기]"); // 버튼이 없으면 닫기 버튼 생성
        }
    }

    private void HandleChoiceSelected(int choiceIndex) // 선택지 클릭 처리
    {
        if (choiceCommitted ||
            currentEventData == null ||
            choiceIndex < 0 ||
            choiceIndex >= currentEventData.Choices.Count)
        {
            return; // 잘못된 클릭 또는 중복 선택 차단
        }

        if (choiceIndex >= AuthoredWinterfallChoices) // 전용 처리 범위를 벗어난 추가 선택지 확인
        { // 추가 행동 처리 시작
            CommitDataChoice(choiceIndex); // 데이터에 정의된 변화와 결과 적용
            return; // 전용 행동 중복 실행 방지
        } // 추가 행동 처리 종료
        if (currentEventData.EventId == AshWandererCryEvent.Id) // 잿빛 방랑자의 울음 확인
        { // 방랑자 선택 시작
            string hint = choiceIndex == AshWandererCryEvent.Follow ? "목소리를 따라가며 정신력 5를 잃을 생존자를 선택하세요." : choiceIndex == AshWandererCryEvent.Pray ? "기도하고 정신력 2를 회복할 생존자를 선택하세요." : null; // 원문 행동별 생존자 선택 안내
            SelectActorEventChoice(choiceIndex, hint, AshWandererCryEvent.TryResolve); // 선택 대상과 실제 효과 한 번 확정
            return; // 다른 방 처리 제외
        } // 방랑자 선택 종료
        if (currentEventData.EventId == GlassFieldEvent.Id) // 유리화된 들판의 원문 선택 확인
        { // 들판 선택 시작
            string hint = choiceIndex == GlassFieldEvent.BreakGlass ? $"유리 파편으로 체력 피해를 {GlassFieldEvent.BreakGlassHealthDamage} 받고 희귀 또는 저주 카드를 직접 받을 생존자를 선택하세요." : null; // 유리 파편의 실제 피해 안내
            SelectActorEventChoice(choiceIndex, hint, GlassFieldEvent.TryResolve); // 대상 또는 다음 전투 효과 확정
            return; // 일반 자원 선택 처리 제외
        } // 들판 선택 종료
        if (currentEventData.EventId == BurntScarecrowEvent.Id) // 타버린 허수아비 원본 확인
        { // 허수아비 선택 시작
            string hint = choiceIndex == BurntScarecrowEvent.Investigate ? "정신력 4를 잃고 카드 한 장을 받을 생존자를 선택하세요." : choiceIndex == BurntScarecrowEvent.Burn ? "체력 4를 잃고 자신의 잿빛 저주 한 장을 제거할 생존자를 선택하세요." : null; // 허수아비 비용에 맞는 대상 선택 안내
            SelectActorEventChoice(choiceIndex, hint, BurntScarecrowEvent.TryResolve); // 생존자 지정 또는 지나침 확정
            return; // 기본 자원 선택 처리 제외
        } // 허수아비 선택 종료
        if (currentEventData.EventId == BurntCartEvent.Id) // 수레의 세 선택 확인
        { // 수레 선택 시작
            string hint = choiceIndex == BurntCartEvent.Salvage ? "체력 3을 잃고 수레를 해체할 생존자를 선택하세요." : null; // 해체할 때만 행동자 지정
            SelectActorEventChoice(choiceIndex, hint, BurntCartEvent.TryResolve); // 현재 방의 선택과 결말 확정
            return; // 기본 보상 처리 제외
        } // 수레 선택 종료
        if (currentEventData.EventId == CharredWellEvent.Id) // 검게 탄 우물의 생존자 행동 확인
        { // 우물 선택 시작
            string[] hints = { "체력 5를 잃고 포션을 꺼낼 생존자를 선택하세요.", "정신력 6을 잃고 자신의 피격 회복을 보관할 생존자를 선택하세요.", "정신력 4를 회복하고 다음 층을 봉인할 생존자를 선택하세요." }; // 각 행동자에게 적용할 비용과 효과
            SelectActorEventChoice(choiceIndex, hints[choiceIndex], CharredWellEvent.TryResolve); // 행동자 한 명 지정 후 우물 결과 확정
            return; // 일반 자원 선택 처리 제외
        } // 우물 선택 종료
        if (currentEventData.EventId == AshstormEyeEvent.Id) // 행동자 지정이 필요한 재폭풍의 눈 확인
        { // 행동자 선택 시작
            SelectAshstormActor(choiceIndex); // 행동 확정 전 대상 선택 표시
            return; // 선택 대기 중 비용과 완료 처리 보류
        } // 행동자 선택 종료
        if (currentEventData.EventId == AshPileEvent.Id) // 윈터폴 잿더미 조사 확인
        { // 잿더미 선택 시작
            SelectAshPileChoice(choiceIndex); // 비용과 대상 확인 후 조사 확정
            return; // 자원 전용 기본 선택 처리 제외
        } // 잿더미 선택 종료
        if (currentEventData.EventId == AshMapFragmentEvent.Id) // 윈터폴 지도 조각 원본 확인
        { // 지도 조각 선택 시작
            SelectMapFragmentChoice(choiceIndex); // 행동별 대상 및 결과 처리
            return; // 다른 이벤트 결과 처리 제외
        } // 지도 조각 선택 종료
        CommitDataChoice(choiceIndex); // 일반 이벤트의 데이터 행동 처리
    } // 선택 분기 종료

    private void CommitDataChoice(int choiceIndex) // 데이터 기반 선택 결과 반영
    { // 데이터 행동 시작
        choiceCommitted = true; // 선택 완료 상태 저장

        ExplorationEventChoiceData choiceData =
            currentEventData.Choices[choiceIndex]; // 선택한 선택지 조회

        string resultText =
            ResolveChoice(
                currentRuntimeEventId,
                choiceIndex,
                choiceData); // 선택 결과 계산

        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance(); // 탐사 세션 관리자 준비

        sessionManager.MarkEventResolved(currentRuntimeEventId); // 현재 이벤트 처리 완료 기록
        if (FindFirstObjectByType<ExplorationPlayerController>() != null && !ExplorationAutoSave.SaveCurrentExploration()) // 이벤트 결과 자동 저장 확인
        { // 이벤트 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 이벤트 결과를 저장하지 못했습니다."); // 기존 저장 유지와 저장 실패 안내
        } // 이벤트 저장 실패 처리 종료

        if (currentEventView != null)
        {
            currentEventView.LockInteraction(); // 현재 이벤트 상호작용 잠금 유지
        }

        bodyText.text = resultText; // 결과 문구로 본문 교체
        ClearChoiceButtons(); // 기존 선택지 버튼 제거
        CreateCloseButton("[확인]"); // 결과 확인 버튼 생성
    }

    private void SelectAshstormActor(int choice) // 선택한 행동을 수행할 생존자 지정
    { // 행동자 선택 시작
        var allies = BattleResultManager.EnsureInstance(); // 현재 탐사 파티 상태 조회
        string runtimeId = currentRuntimeEventId; // 현재 선택 대상 방 식별자 보관
        string heading = currentEventData.Choices[choice].ChoiceText; // 행동 제목 조회
        if (choice == AshstormEyeEvent.Gaze) // 응시 후 별도 카드 소유자 선택 확인
        { // 응시자 선택 시작
            ExplorationRewardOwnerView.OpenPartyMember(transform, allies.ActiveParty, heading, "정신력 8을 잃고 눈을 들여다볼 생존자를 선택하세요.", "선택 취소", allies.IsLivingPartyMember, allies.IsLivingPartyMember, actor => SelectAshstormOwner(runtimeId, actor)); // 응시자 선택 후 소유자 화면으로 이동
            return; // 카드 소유자 확정 전 결과 보류
        } // 응시자 선택 종료
        string hint = choice == AshstormEyeEvent.Shatter ? $"돌을 던져 파편으로 체력 피해를 {AshstormEyeEvent.ShatterHealthDamage} 받을 생존자를 선택하세요." : "시선을 피하고 정신력 1을 회복할 생존자를 선택하세요."; // 눈 파편의 실제 피해 안내
        ExplorationRewardOwnerView.OpenPartyMember(transform, allies.ActiveParty, heading, hint, "선택 취소", allies.IsLivingPartyMember, actor => CommitAshstormChoice(runtimeId, choice, actor, null)); // 생존자 선택 후 비용과 결과 반영
    } // 행동자 선택 종료

    private void SelectAshstormOwner(string runtimeId, CharacterData actor) // 응시한 사람과 별도로 카드 소유자 지정
    { // 카드 소유자 선택 시작
        ShopOfferData reward = AshstormEyeEvent.GetRareReward(runtimeId); // 현재 방의 희귀 카드 조회
        if (reward == null) // 실제 희귀 카드 지급 가능 여부 확인
        { // 지급 불가 안내 시작
            bodyText.text = "지급 가능한 희귀 카드가 없습니다. 다른 행동을 선택하세요."; // 비용 없는 선택 복귀 안내
            return; // 다른 행동 선택 허용
        } // 지급 불가 안내 종료
        PartyData party = BattleResultManager.EnsureInstance().ActiveParty; // 현재 파티 소유자 목록 조회
        string hint = $"{actor.DisplayName}의 정신력 -8 · 카드를 받을 파티원을 선택하세요."; // 비용을 치르는 사람과 소유자 구분 안내
        ExplorationRewardOwnerView.OpenPartyMember(transform, party, $"희귀 카드 · {reward.DisplayName}", hint, "선택 취소", null, owner => CommitAshstormChoice(runtimeId, AshstormEyeEvent.Gaze, actor, owner)); // 기존 공용 카드 규칙으로 소유자 선택
    } // 카드 소유자 선택 종료

    private bool CommitAshstormChoice(string runtimeId, int choice, CharacterData actor, CharacterData owner) // 최종 대상 확정 후 결과 표시
    { // 결과 확정 시작
        if (choiceCommitted || currentRuntimeEventId != runtimeId || currentEventData == null || !AshstormEyeEvent.TryResolve(runtimeId, choice, actor, owner, out string summary)) // 현재 방의 미처리 선택 성공 확인
        { // 지급 실패 처리 시작
            return false; // 선택 취소 또는 재시도 허용
        } // 지급 실패 처리 종료
        choiceCommitted = true; // 결과 화면의 중복 입력 차단
        string story = BuildResultMessage(currentEventData.Choices[choice].ResultText, summary); // 원문 결말과 실제 변화량 결합
        SaveStoryResult(story); // 확정된 결과 저장과 표시
        return true; // 열린 대상 선택 화면 닫기
    } // 결과 확정 종료

    private void SelectAshPileChoice(int choice) // 잿더미 행동에 맞는 비용과 대상 확인
    { // 잿더미 행동 시작
        string runtimeId = currentRuntimeEventId; // 행동할 방 식별자 보관
        if (choice == AshPileEvent.UseTool) // 파티 철판으로 조사하는 행동 확인
        { // 도구 조사 시작
            CommitAshPileChoice(runtimeId, choice, null); // 행동자 비용 없이 파티 자원으로 조사
            return; // 도구 선택 처리 종료
        } // 도구 조사 종료
        var allies = BattleResultManager.EnsureInstance(); // 현재 파티 상태 조회
        string hint = choice == AshPileEvent.BareHands ? "체력 6을 잃고 잿더미를 뒤질 생존자를 선택하세요." : "잿더미를 덮고 정신력 3을 회복할 생존자를 선택하세요."; // 행동 비용과 대상 안내
        ExplorationRewardOwnerView.OpenPartyMember(transform, allies.ActiveParty, currentEventData.Choices[choice].ChoiceText, hint, "선택 취소", allies.IsLivingPartyMember, actor => CommitAshPileChoice(runtimeId, choice, actor)); // 대상 확정 전 비용과 추첨 보류
    } // 잿더미 행동 종료

    private bool CommitAshPileChoice(string runtimeId, int choice, CharacterData actor, RelicData selected = null) // 잿더미 비용과 보상 처리
    { // 잿더미 확정 시작
        if (choiceCommitted || currentEventData == null || currentEventData.EventId != AshPileEvent.Id || currentRuntimeEventId != runtimeId) // 오래된 선택 창과 중복 입력 확인
        { // 이전 입력 처리 시작
            return false; // 현재 방 이외의 결과 적용 차단
        } // 이전 입력 처리 종료
        if (selected == null && choice != AshPileEvent.Cover && RelicRunManager.Instance?.Inventory.FindEffect(RelicEffectType.ExtraEventRelicChoice) != null && (choice != AshPileEvent.UseTool || PlayerResourceManager.EnsureInstance().CanAfford(0, 0, 1, 0))) // 지급 가능한 조사에서 핀셋 보유 확인
        { // 지급 전 유물 선택 시작
            var options = AshPileEvent.GetRewardChoices(runtimeId, choice, out _); // 현재 방과 일치하는 후보 조회
            if (options.Count > 1) // 두 후보 중 하나를 고르는 경우 확인
            { // 선택 화면 표시 시작
                ClearChoiceButtons(); // 이전 조사 행동 정리
                bodyText.text = "핀셋으로 유물 두 개를 발견했습니다. 하나를 선택하세요.\n\n" + options[0].DisplayName + " · " + options[0].Description + "\n\n" + options[1].DisplayName + " · " + options[1].Description; // 실제 후보의 이름과 효과 안내
                foreach (var relic in options) // 실제 미보유 후보 순회
                { // 후보 버튼 생성 시작
                    CreateChoiceButton(relic.DisplayName, () => CommitAshPileChoice(runtimeId, choice, actor, relic)); // 누른 후보만 지급
                } // 후보 버튼 생성 종료
                CreateChoiceButton("[선택 취소]", PopulateEventContent); // 비용 없이 원래 세 행동으로 복귀
                return true; // 행동자 선택 창 닫기
            } // 선택 화면 표시 종료
        } // 지급 전 유물 선택 종료
        if (!AshPileEvent.TryResolve(runtimeId, choice, actor, selected, out string story, out string summary)) // 실제 비용과 선택 유물 검증
        { // 비용 또는 보상 누락 처리 시작
            if (!string.IsNullOrEmpty(summary)) // 미확정 사유 확인
            { // 재선택 안내 시작
                bodyText.text = summary; // 이벤트 화면에서 비용 없는 재선택 안내
                return true; // 대상 선택 창을 닫고 원래 선택지로 복귀
            } // 재선택 안내 종료
            return false; // 잘못된 대상의 비용과 보상 보존
        } // 비용 또는 보상 누락 처리 종료
        choiceCommitted = true; // 완료된 행동의 추가 입력 차단
        SaveStoryResult(BuildResultMessage(story, summary)); // 비용과 보상 및 방 완료 기록 함께 저장
        return true; // 대상 선택 완료
    } // 잿더미 확정 종료

    private void SelectMapFragmentChoice(int choice) // 지도 조각의 대상 선택과 즉시 행동 처리
    { // 지도 행동 선택 시작
        string runtimeId = currentRuntimeEventId; // 현재 선택할 이벤트 방 보관
        if (choice == AshMapFragmentEvent.Burn) // 생존 행동자 지정 여부 확인
        { // 태울 생존자 선택 시작
            var allies = BattleResultManager.EnsureInstance(); // 출전 파티 상태 조회
            ExplorationRewardOwnerView.OpenPartyMember(transform, allies.ActiveParty, "지도 조각을 태운다", "정신력 2를 회복할 생존 파티원을 선택하세요.", "선택 취소", allies.IsLivingPartyMember, actor => CommitMapFragmentChoice(runtimeId, choice, actor)); // 취소할 수 있는 생존자 선택 표시
            return; // 행동자 확정 전 나사와 정신력 지급 보류
        } // 태울 생존자 선택 종료
        CommitMapFragmentChoice(runtimeId, choice, null); // 대상이 없는 줍기 또는 찢기 처리
    } // 지도 행동 선택 종료

    private bool CommitMapFragmentChoice(string runtimeId, int choice, CharacterData actor) // 지도 조각 선택 확정과 결과 저장
    { // 지도 선택 확정 시작
        if (choiceCommitted || currentEventData == null || currentEventData.EventId != AshMapFragmentEvent.Id || currentRuntimeEventId != runtimeId) // 원래 선택 방과 입력 유효성 확인
        { // 잘못된 선택 확인 시작
            return false; // 이전 화면 중복 입력 차단
        } // 잘못된 선택 확인 종료
        if (!AshMapFragmentEvent.TryResolve(runtimeId, choice, actor, out string summary)) // 지도 보관 또는 소각 결과 확인
        { // 미확정 처리 시작
            if (!string.IsNullOrEmpty(summary)) // 중복 지도 보관 사유 확인
            { // 선택 안내 시작
                bodyText.text = summary; // 이전 효과를 보존하고 다른 선택 안내
            } // 선택 안내 종료
            return false; // 방을 사용하지 않은 상태 유지
        } // 미확정 처리 종료
        choiceCommitted = true; // 이번 이벤트 중복 입력 차단
        SaveStoryResult(BuildResultMessage(currentEventData.Choices[choice].ResultText, summary)); // 원문과 실제 상태 변경을 함께 저장
        return true; // 지도 조각 행동 완료
    } // 지도 선택 확정 종료

    private delegate bool ActorEventResolver(string runtimeId, int choice, CharacterData actor, out string summary); // 생존자 한 명이 수행하는 사건 결과 처리

    private void SelectActorEventChoice(int choice, string hint, ActorEventResolver resolve) // 행동자 선택 또는 대상 없는 선택 확정
    { // 사건 선택 시작
        string runtimeId = currentRuntimeEventId; // 선택을 받은 방 식별자 보관
        string eventId = currentEventData.EventId; // 입력을 받은 사건 원본 보관
        if (string.IsNullOrEmpty(hint)) // 행동자 없는 선택 확인
        { // 대상 없는 확정 시작
            CommitActorEventChoice(runtimeId, eventId, choice, null, resolve); // 생존자 선택 없이 확정
            return; // 파티원 선택 생략
        } // 대상 없는 확정 종료
        var party = BattleResultManager.EnsureInstance(); // 현재 파티 생존 상태 조회
        ExplorationRewardOwnerView.OpenPartyMember(transform, party.ActiveParty, currentEventData.Choices[choice].ChoiceText, hint, "선택 취소", party.IsLivingPartyMember, actor => CommitActorEventChoice(runtimeId, eventId, choice, actor, resolve)); // 행동자 확정 후 결과 반영
    } // 사건 선택 종료

    private bool CommitActorEventChoice(string runtimeId, string eventId, int choice, CharacterData actor, ActorEventResolver resolve) // 방과 사건을 다시 확인한 뒤 선택 확정
    { // 행동자 선택 확정 시작
        if (choiceCommitted || currentRuntimeEventId != runtimeId || currentEventData == null || currentEventData.EventId != eventId) // 현재 방과 사건 및 처리 여부 확인
        { // 이전 입력 처리 시작
            return false; // 오래된 선택과 중복 입력 차단
        } // 이전 입력 처리 종료
        if (!resolve(runtimeId, choice, actor, out string summary)) // 사건별 지급과 비용 처리 확인
        { // 결과 실패 처리 시작
            if (!string.IsNullOrEmpty(summary)) // 선택 실패 사유 확인
            { // 안내 표시 시작
                bodyText.text = summary; // 다른 선택이나 재시도 안내
            } // 안내 표시 종료
            return false; // 미처리 상태 보존
        } // 결과 실패 처리 종료
        choiceCommitted = true; // 이번 방 중복 입력 차단
        SaveStoryResult(BuildResultMessage(currentEventData.Choices[choice].ResultText, summary)); // 원문 결말과 실제 결과 저장
        return true; // 행동자 선택 창 닫기
    } // 행동자 선택 확정 종료

    private void SaveStoryResult(string story) // 확정 결과만 저장하고 실패 시 같은 상태로 재시도
    { // 결과 저장 시작
        bool defeated = ExplorationSessionManager.EnsureInstance().IsExplorationCompleted; // 마지막 생존자 사망 확인
        bool saved = defeated ? ExplorationAutoSave.SaveAfterBattle(Vector3.zero) : ExplorationAutoSave.SaveCurrentExploration(); // 전멸은 거점에 생존은 현재 탐사에 저장
        if (defeated) // 탐사 결과 화면 전환 확인
        { // 전멸 화면 전환 시작
            if (!saved) // 종료 상태 저장 실패 확인
            { // 실패 기록 시작
                Debug.LogWarning("[ExplorationAutoSave] 탐사 이벤트 전멸 상태를 저장하지 못했습니다."); // 거점 복귀 시 재시도할 저장 실패 안내
            } // 실패 기록 종료
            CloseCurrentEvent(); // 거점 복귀 결과 화면에 입력 반환
            return; // 종료된 탐사 화면 정리
        } // 전멸 화면 전환 종료
        ClearChoiceButtons(); // 이전 행동 버튼 제거
        bodyText.text = saved ? story : story + "\n결과를 저장하지 못했습니다."; // 저장 결과와 원문 결말 표시
        if (saved) // 탐사 자동 저장 성공 확인
        { // 완료 안내 시작
            CreateCloseButton("[확인]"); // 탐사 이동으로 복귀
        } // 완료 안내 종료
        else // 동일 결과 재저장 처리
        { // 재시도 버튼 생성 시작
            CreateChoiceButton("[다시 저장]", () => SaveStoryResult(story)); // 비용과 보상의 재적용 없는 저장 재시도
            CreateCloseButton("[계속]"); // 메모리 진행 상태로 탐사 계속
        } // 재시도 버튼 생성 종료
    } // 결과 저장 종료

    private string ResolveChoice(
        string runtimeEventId,
        int choiceIndex,
        ExplorationEventChoiceData choiceData) // 선택지 결과 계산
    {
        if (choiceData == null)
        {
            return "아무 일도 일어나지 않았습니다."; // 잘못된 선택지 기본 문구 반환
        }

        if (!choiceData.HasRandomOutcome)
        {
            string directSummary =
                ApplyResourceChange(
                    choiceData.DirectChange); // 즉시 변화량 적용

            return BuildResultMessage(
                choiceData.ResultText,
                directSummary); // 즉시 결과 문구 반환
        }

        int seed =
            ComputeStableHash(runtimeEventId) ^
            (choiceIndex * 486187739); // 이벤트 ID 기반 확률 시드 생성

        System.Random random =
            new System.Random(seed); // 확률 판정 난수 생성기 준비

        int roll =
            random.Next(0, 100); // 0~99 범위 판정 값 생성

        bool isSuccess =
            roll < choiceData.SuccessChancePercent; // 성공 여부 판정

        string resultText =
            isSuccess
                ? choiceData.SuccessText
                : choiceData.FailureText; // 결과 문구 선택

        ExplorationEventResourceChange resultChange =
            isSuccess
                ? choiceData.SuccessChange
                : choiceData.FailureChange; // 결과 변화량 선택

        string randomSummary =
            ApplyResourceChange(resultChange); // 결과 변화량 적용

        return BuildResultMessage(resultText, randomSummary); // 최종 결과 문구 반환
    }

    private static int ComputeStableHash(string text) // 문자열 기반 고정 해시 계산
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0; // 빈 문자열 기본 해시 반환
        }

        int hash = 17; // 초기 해시값 설정

        for (int index = 0;
             index < text.Length;
             index++)
        {
            hash = unchecked(hash * 31 + text[index]); // 문자 누적 해시 계산
        }

        return hash; // 최종 해시 반환
    }

    private static string BuildResultMessage(
        string baseMessage,
        string resourceSummary) // 결과 문구와 자원 변화 문구 결합
    {
        string safeMessage =
            string.IsNullOrWhiteSpace(baseMessage)
                ? "이벤트가 마무리되었습니다."
                : baseMessage; // 기본 결과 문구 보정

        if (string.IsNullOrWhiteSpace(resourceSummary) ||
            resourceSummary == "변화 없음")
        {
            return safeMessage; // 자원 변화가 없으면 기본 문구만 반환
        }

        return safeMessage +
               "\n\n" +
               $"<color=#9FDFFF>결과 : {resourceSummary}</color>"; // 자원 변화 포함 결과 반환
    }

    private static string ApplyResourceChange(ExplorationEventResourceChange change) // 자원 변화량 적용
    {
        if (change == null ||
            change.IsEmpty())
        {
            return "변화 없음"; // 변화량이 없으면 기본 문구 반환
        }

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비

        int addGold = Mathf.Max(0, change.Gold); // 골드 증가량 계산
        int addScrew = Mathf.Max(0, change.Screw); // 나사 증가량 계산
        int addIronPlate = Mathf.Max(0, change.IronPlate); // 철판 증가량 계산
        int addWire = Mathf.Max(0, change.Wire); // 전선 증가량 계산

        if (addGold > 0 ||
            addScrew > 0 ||
            addIronPlate > 0 ||
            addWire > 0)
        {
            resourceManager.AddResources(
                addGold,
                addScrew,
                addIronPlate,
                addWire); // 증가 자원 즉시 지급
        }

        int spendGold = Mathf.Min(resourceManager.Gold, Mathf.Max(0, -change.Gold)); // 골드 감소량 계산
        int spendScrew = Mathf.Min(resourceManager.Screw, Mathf.Max(0, -change.Screw)); // 나사 감소량 계산
        int spendIronPlate = Mathf.Min(resourceManager.IronPlate, Mathf.Max(0, -change.IronPlate)); // 철판 감소량 계산
        int spendWire = Mathf.Min(resourceManager.Wire, Mathf.Max(0, -change.Wire)); // 전선 감소량 계산

        if (spendGold > 0 ||
            spendScrew > 0 ||
            spendIronPlate > 0 ||
            spendWire > 0)
        {
            resourceManager.TrySpend(
                spendGold,
                spendScrew,
                spendIronPlate,
                spendWire); // 보유량 기준 손실 자원 차감
        }

        return change.BuildSummary(); // 표시용 변화량 요약 반환
    }

    private void CreateCloseButton(string label) // 닫기 버튼 생성
    {
        CreateChoiceButton(label, CloseCurrentEvent); // 닫기 버튼 생성 호출
    }

    public static void CloseForLoad() // 저장 복원 전 열려 있는 이벤트 정리
    { // 열려 있는 이벤트 정리 시작
        if (instance == null || instance.currentEventData == null) // 현재 열린 이벤트 확인
        { // 닫힌 화면 처리 시작
            return; // 기존 입력 상태 유지
        } // 닫힌 화면 처리 종료
        if (instance.showCoroutine != null) // 화면 캡처 대기 여부 확인
        { // 표시 대기 정리 시작
            instance.StopCoroutine(instance.showCoroutine); // 이전 이벤트 표시 취소
            instance.showCoroutine = null; // 코루틴 참조 해제
        } // 표시 대기 정리 종료
        if (instance.currentEventView != null) // 이전 이벤트 오브젝트 존재 확인
        { // 이벤트 잠금 해제 시작
            instance.currentEventView.ResetInteraction(); // 복원 전 오브젝트 상호작용 해제
        } // 이벤트 잠금 해제 종료
        instance.choiceCommitted = false; // 저장 교체 중 이전 수레 결말의 전투 예약 차단
        instance.CloseCurrentEvent(); // 열린 선택지와 탐사 이동 잠금 해제
    } // 열려 있는 이벤트 정리 종료

    private void CloseCurrentEvent() // 현재 이벤트 패널 닫기
    {
        HidePanelImmediate(); // 패널 즉시 닫기
        ExplorationPlayerController.SetInputBlocked(false); // 플레이어 이동 잠금 해제
        var session = ExplorationSessionManager.Instance; // 결말을 확정한 수레 방 확인
        if (choiceCommitted && session != null && currentRuntimeEventId != null && session.Cart.AmbushRoom == currentRuntimeEventId) // 매복 결말 확인 후 전투 진입
        { // 매복 진입 예약 시작
            StartCoroutine(BurntCartEvent.ResumeAmbush()); // 저장한 일반 조우와 사냥꾼의 전투 시작
        } // 매복 진입 예약 종료
        currentEventView = null; // 현재 이벤트 뷰 참조 정리
        currentEventData = null; // 현재 이벤트 데이터 정리
        currentRuntimeEventId = null; // 현재 이벤트 ID 정리
        choiceCommitted = false; // 선택 상태 초기화
    }

    private void HidePanelImmediate() // 패널 즉시 숨김
    {
        ClearChoiceButtons(); // 기존 버튼 즉시 제거

        if (rootCanvas != null)
        {
            rootCanvas.enabled = false; // 캔버스 비활성화
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false); // 패널 본문 비활성화
        }

        ReleaseBlurTexture(); // 블러 텍스처 해제
    }

    private void ApplyBlurCapture() // 현재 화면을 캡처해 블러 텍스처 생성
    {
        ReleaseBlurTexture(); // 이전 블러 텍스처 정리

        Texture2D sourceTexture =
            ScreenCapture.CaptureScreenshotAsTexture(); // 현재 화면 캡처

        if (sourceTexture == null)
        {
            blurBackgroundImage.texture = null; // 캡처 실패 시 텍스처 제거
            blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 대체 어두운 색상 적용
            return;
        }

        int width =
            Mathf.Max(1, sourceTexture.width / BlurDownsample); // 축소 블러 폭 계산

        int height =
            Mathf.Max(1, sourceTexture.height / BlurDownsample); // 축소 블러 높이 계산

        blurTexture =
            new Texture2D(width, height, TextureFormat.RGB24, false); // 축소 블러 텍스처 생성

        blurTexture.filterMode = FilterMode.Bilinear; // 확대 시 부드럽게 보이도록 설정

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                float u = (x + 0.5f) / width; // 원본 샘플 U 좌표 계산
                float v = (y + 0.5f) / height; // 원본 샘플 V 좌표 계산
                Color sampledColor = sourceTexture.GetPixelBilinear(u, v); // 원본 색상 샘플링
                blurTexture.SetPixel(x, y, sampledColor); // 축소 텍스처 픽셀 설정
            }
        }

        blurTexture.Apply(); // 축소 텍스처 적용
        blurBackgroundImage.texture = blurTexture; // 블러 텍스처 표시
        blurBackgroundImage.color = Color.white; // 블러 텍스처 색상 적용

        Destroy(sourceTexture); // 원본 캡처 텍스처 정리
    }

    private void ReleaseBlurTexture() // 블러 텍스처 메모리 해제
    {
        if (blurTexture != null)
        {
            Destroy(blurTexture); // 블러 텍스처 제거
            blurTexture = null; // 블러 텍스처 참조 정리
        }

        if (blurBackgroundImage != null)
        {
            blurBackgroundImage.texture = null; // 배경 텍스처 참조 제거
            blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 대체 색상 적용
        }
    }

    private void ClearChoiceButtons() // 현재 버튼 목록 제거
    {
        for (int index = 0;
             index < choiceButtons.Count;
             index++)
        {
            if (choiceButtons[index] != null)
            {
                choiceButtons[index].gameObject.SetActive(false); // 레이아웃에서 이전 버튼 즉시 제외
                Destroy(choiceButtons[index].gameObject); // 버튼 오브젝트 제거
            }
        }

        choiceButtons.Clear(); // 버튼 목록 초기화
        if (choiceScroll != null) // 목록 초기화 여부 확인
        { // 스크롤 초기화 시작
            choiceScroll.StopMovement(); // 남은 이동 정지
            choiceContainer.anchoredPosition = Vector2.zero; // 다음 목록의 첫 줄로 복귀
        } // 스크롤 초기화 종료
    }

    private void CreateChoiceButton(
        string label,
        UnityEngine.Events.UnityAction onClick) // 선택지 버튼 생성
    {
        GameObject buttonObject =
            new GameObject(
                "ChoiceButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)); // 버튼 오브젝트 생성

        buttonObject.transform.SetParent(choiceContainer, false); // 버튼을 컨테이너 하위에 배치

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회

        buttonRect.sizeDelta = new Vector2(0f, ChoiceHeight); // 선택 버튼 한 줄 높이 설정

        Image buttonImage =
            buttonObject.GetComponent<Image>(); // 버튼 이미지 조회

        buttonImage.color = new Color(0.13f, 0.29f, 0.33f, 0.95f); // 버튼 배경 색상 지정

        Button button =
            buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회

        ColorBlock colors =
            button.colors; // 버튼 색상 블록 조회

        colors.normalColor = new Color(0.13f, 0.29f, 0.33f, 0.95f); // 기본 버튼 색상 지정
        colors.highlightedColor = new Color(0.18f, 0.38f, 0.43f, 1f); // 하이라이트 색상 지정
        colors.pressedColor = new Color(0.10f, 0.22f, 0.26f, 1f); // 눌림 색상 지정
        colors.selectedColor = colors.highlightedColor; // 선택 색상 지정
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.75f); // 비활성 색상 지정
        button.colors = colors; // 버튼 색상 블록 적용
        button.onClick.AddListener(onClick); // 버튼 클릭 이벤트 연결

        GameObject textObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 버튼 텍스트 오브젝트 생성

        textObject.transform.SetParent(buttonObject.transform, false); // 텍스트를 버튼 하위에 배치

        RectTransform textRect =
            textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회

        textRect.anchorMin = Vector2.zero; // 텍스트 왼쪽 아래 앵커 설정
        textRect.anchorMax = Vector2.one; // 텍스트 오른쪽 위 앵커 설정
        textRect.offsetMin = new Vector2(18f, 6f); // 왼쪽 아래 여백 설정
        textRect.offsetMax = new Vector2(-18f, -6f); // 오른쪽 위 여백 설정

        TMP_Text buttonText =
            textObject.GetComponent<TMP_Text>(); // 버튼 텍스트 조회

        buttonText.text = label; // 버튼 문구 지정
        buttonText.fontSize = 23f; // 버튼 글자 크기 지정
        buttonText.color = new Color(0.86f, 0.95f, 0.97f, 1f); // 버튼 글자 색상 지정
        buttonText.alignment = TextAlignmentOptions.MidlineLeft; // 버튼 글자 정렬 지정
        buttonText.textWrappingMode = TextWrappingModes.Normal; // 버튼 줄바꿈 모드 설정

        choiceButtons.Add(button); // 버튼 목록에 현재 버튼 등록
        int rows = Mathf.Min(VisibleChoiceRows, choiceButtons.Count); // 화면에 필요한 줄 수
        float height = rows * (ChoiceHeight + ChoiceGap) - ChoiceGap; // 보이는 버튼들의 총높이
        float fullHeight = VisibleChoiceRows * (ChoiceHeight + ChoiceGap) - ChoiceGap; // 삽화 아래 고정 여백 기준
        var list = (RectTransform)choiceScroll.transform; // 목록 배경 영역 조회
        list.offsetMax = new Vector2(-48f, ChoiceBottom + height + 8f); // 현재 버튼 수에 맞춘 목록 높이
        bool overflow = choiceButtons.Count > VisibleChoiceRows; // 추가 선택지 존재 여부
        choiceScroll.verticalScrollbar.gameObject.SetActive(overflow); // 필요한 경우에만 손잡이 표시
        choiceScroll.viewport.offsetMax = new Vector2(overflow ? -40f : -12f, -8f); // 스크롤바와 버튼 사이 여백
        contentRect.offsetMin = new Vector2(56f, ChoiceBottom + fullHeight + 24f); // 삽화 위치 고정
        storyBackground.offsetMin = new Vector2(20f, height - fullHeight); // 남은 높이를 설명 배경에 배정
        bodyText.rectTransform.offsetMin = storyBackground.offsetMin + new Vector2(26f, 18f); // 테두리 안쪽에 설명 배치
    }

    private void EnsureEventSystem() // New Input System 기반 UI 클릭 이벤트 시스템 보장
    {
        EventSystem eventSystem =
            EventSystem.current; // 현재 활성 EventSystem 조회

        if (eventSystem == null)
        {
            GameObject eventSystemObject =
                new GameObject(
                    "ExplorationEventSystem",
                    typeof(EventSystem)); // 런타임 EventSystem 오브젝트 생성

            eventSystem =
                eventSystemObject.GetComponent<EventSystem>(); // 생성 EventSystem 조회
        }

        InputSystemUIInputModule inputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>(); // New Input System UI 모듈 조회

        if (inputModule != null)
        {
            inputModule.enabled = true; // 기존 New Input System UI 모듈 활성화
            return;
        }

        BaseInputModule existingInputModule =
            eventSystem.GetComponent<BaseInputModule>(); // 기존 입력 모듈 조회

        if (existingInputModule != null)
        {
            existingInputModule.enabled = false; // 구 Input Manager 기반 모듈 충돌 방지
        }

        inputModule =
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>(); // New Input System UI 모듈 추가

        inputModule.AssignDefaultActions(); // 마우스 Point·Click 등 기본 UI 액션 연결
    }

    private void EnsureUi() // 이벤트 패널 UI 구성 보장
    {
        if (rootCanvas != null)
        {
            return; // 이미 구성된 UI 재사용
        }

        rootCanvas =
            gameObject.GetComponent<Canvas>(); // 기존 Canvas 조회

        if (rootCanvas == null)
        {
            rootCanvas =
                gameObject.AddComponent<Canvas>(); // 루트 Canvas 추가
        }

        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 방식 사용
        rootCanvas.sortingOrder = 5000; // 다른 UI보다 위에 표시

        EnsureEventSystem(); // 런타임 생성 UI의 마우스 클릭 이벤트 시스템 준비

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>(); // UI 클릭 처리 추가
        }

        CanvasScaler scaler =
            gameObject.GetComponent<CanvasScaler>(); // 기존 CanvasScaler 조회

        if (scaler == null)
        {
            scaler =
                gameObject.AddComponent<CanvasScaler>(); // CanvasScaler 추가
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 비례 스케일 사용
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 지정
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 매치 모드 지정
        scaler.matchWidthOrHeight = 0.5f; // 가로·세로 절충값 지정

        GameObject blurObject =
            new GameObject(
                "BlurBackground",
                typeof(RectTransform),
                typeof(RawImage)); // 블러 배경 오브젝트 생성

        blurObject.transform.SetParent(transform, false); // 블러 배경을 루트 하위에 배치

        RectTransform blurRect =
            blurObject.GetComponent<RectTransform>(); // 블러 배경 RectTransform 조회

        StretchFullScreen(blurRect); // 블러 배경 전체 화면 확장

        blurBackgroundImage =
            blurObject.GetComponent<RawImage>(); // 블러 배경 이미지 저장

        blurBackgroundImage.color = new Color(0f, 0f, 0f, 0.65f); // 기본 어두운 배경 색상 지정
        blurBackgroundImage.raycastTarget = true; // 배경 클릭 차단 활성화

        GameObject dimObject =
            new GameObject(
                "DimOverlay",
                typeof(RectTransform),
                typeof(Image)); // 오버레이 오브젝트 생성

        dimObject.transform.SetParent(transform, false); // 오버레이를 루트 하위에 배치

        RectTransform dimRect =
            dimObject.GetComponent<RectTransform>(); // 오버레이 RectTransform 조회

        StretchFullScreen(dimRect); // 오버레이 전체 화면 확장

        dimOverlayImage =
            dimObject.GetComponent<Image>(); // 어두운 오버레이 이미지 저장

        dimOverlayImage.color = new Color(0f, 0f, 0f, 0.35f); // 오버레이 색상 지정
        dimOverlayImage.raycastTarget = true; // 오버레이 클릭 차단 활성화

        GameObject panelObject =
            new GameObject(
                "EventPanel",
                typeof(RectTransform),
                typeof(Image)); // 패널 루트 오브젝트 생성

        panelObject.transform.SetParent(transform, false); // 패널 루트를 캔버스 하위에 배치

        panelRoot =
            panelObject.GetComponent<RectTransform>(); // 패널 루트 RectTransform 저장

        PreparationUi.Stretch(panelRoot, new Vector2(0.022f, 0.117f), new Vector2(0.976f, 0.965f)); // 화면 위쪽 확장과 아래 파티 정보 여백 확보

        Image panelImage =
            panelObject.GetComponent<Image>(); // 패널 배경 이미지 조회

        panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.96f); // 패널 배경 색상 지정
        panelImage.raycastTarget = false; // 패널 배경이 선택지 버튼 Raycast를 가로채지 않도록 설정

        CreateTitleArea(panelObject.transform); // 상단 제목 영역 생성
        CreateContentArea(panelObject.transform); // 본문 내용 영역 생성
        CreateChoiceArea(panelObject.transform); // 하단 선택지 영역 생성
    }

    private void CreateTitleArea(Transform parent) // 상단 제목 영역 생성
    {
        GameObject titleObject =
            new GameObject(
                "Title",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 제목 텍스트 오브젝트 생성

        titleObject.transform.SetParent(parent, false); // 제목 텍스트를 패널 하위에 배치

        RectTransform titleRect =
            titleObject.GetComponent<RectTransform>(); // 제목 RectTransform 조회

        titleRect.anchorMin = new Vector2(0f, 1f); // 제목 상단 좌측 앵커 설정
        titleRect.anchorMax = new Vector2(1f, 1f); // 제목 상단 우측 앵커 설정
        titleRect.pivot = new Vector2(0.5f, 1f); // 제목 상단 피벗 설정
        titleRect.offsetMin = new Vector2(40f, -86f); // 제목 좌측·하단 여백 설정
        titleRect.offsetMax = new Vector2(-40f, -20f); // 제목 우측·상단 여백 설정

        titleText =
            titleObject.GetComponent<TMP_Text>(); // 제목 텍스트 저장

        titleText.text = "이벤트"; // 제목 기본 문구 지정
        titleText.fontSize = 36f; // 제목 글자 크기 지정
        titleText.color = new Color(0.95f, 0.84f, 0.42f, 1f); // 제목 글자 색상 지정
        titleText.alignment = TextAlignmentOptions.Center; // 제목 정렬 지정
    }

    private void CreateContentArea(Transform parent) // 본문 내용 영역 생성
    {
        GameObject contentObject =
            new GameObject(
                "Content",
                typeof(RectTransform)); // 내용 영역 오브젝트 생성

        contentObject.transform.SetParent(parent, false); // 내용 영역을 패널 하위에 배치

        contentRect = contentObject.GetComponent<RectTransform>(); // 현재 패널의 본문 영역 보관

        contentRect.anchorMin = new Vector2(0f, 0f); // 내용 영역 하단 좌측 앵커 설정
        contentRect.anchorMax = new Vector2(1f, 1f); // 내용 영역 상단 우측 앵커 설정
        contentRect.offsetMin = new Vector2(56f, ChoiceBottom); // 선택지 생성 전 좌하단 여백 지정
        contentRect.offsetMax = new Vector2(-56f, -100f); // 제목 아래 본문 여백 지정

        var artColumn = new GameObject("IllustrationColumn", typeof(RectTransform)).GetComponent<RectTransform>(); // 삽화를 배치할 왼쪽 열 생성
        artColumn.SetParent(contentRect, false); // 고정된 본문 기준 영역에 배치
        PreparationUi.Stretch(artColumn, Vector2.zero, new Vector2(0.32f, 1f)); // 기존 왼쪽 열 너비와 여백 유지
        GameObject frameObject =
            new GameObject(
                "IllustrationFrame",
                typeof(RectTransform),
                typeof(Image)); // 일러스트 프레임 생성

        frameObject.transform.SetParent(artColumn, false); // 삽화 프레임을 왼쪽 열에 배치

        RectTransform frameRect =
            frameObject.GetComponent<RectTransform>(); // 프레임 RectTransform 조회

        var square = frameRect.gameObject.AddComponent<AspectRatioFitter>(); // 화면 크기에 맞는 정사각형 배치
        square.aspectRatio = 1f; // 삽화 프레임 가로세로 비율 고정
        square.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 가로와 세로 중 작은 길이에 맞춰 중앙 정렬

        illustrationFrameImage =
            frameObject.GetComponent<Image>(); // 일러스트 프레임 이미지 저장

        ApplyEventPanel(illustrationFrameImage); // 삽화에 금속 테두리와 청록색 배경 적용

        GameObject illustrationObject =
            new GameObject(
                "Illustration",
                typeof(RectTransform),
                typeof(Image)); // 일러스트 이미지 생성

        illustrationObject.transform.SetParent(frameObject.transform, false); // 그림 이미지를 프레임 하위에 배치

        RectTransform illustrationRect =
            illustrationObject.GetComponent<RectTransform>(); // 그림 RectTransform 조회

        StretchInsideParent(illustrationRect, 16f); // 확대된 삽화 프레임 안쪽 여백 지정

        illustrationImage =
            illustrationObject.GetComponent<Image>(); // 그림 이미지 저장

        illustrationImage.preserveAspect = true; // 그림 비율 유지 설정

        GameObject placeholderObject =
            new GameObject(
                "IllustrationFallback",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 대체 문자 생성

        placeholderObject.transform.SetParent(frameObject.transform, false); // 대체 문자를 프레임 하위에 배치

        RectTransform placeholderRect =
            placeholderObject.GetComponent<RectTransform>(); // 대체 문자 RectTransform 조회

        StretchInsideParent(placeholderRect, 24f); // 대체 문자를 프레임 안쪽으로 확장

        illustrationFallbackText =
            placeholderObject.GetComponent<TMP_Text>(); // 대체 문자 텍스트 저장

        illustrationFallbackText.text = "?"; // 대체 문자 기본값 지정
        illustrationFallbackText.fontSize = 110f; // 대체 문자 크기 지정
        illustrationFallbackText.color = new Color(0.92f, 0.46f, 0.28f, 1f); // 대체 문자 색상 지정
        illustrationFallbackText.alignment = TextAlignmentOptions.Center; // 대체 문자 정렬 지정

        storyBackground = PreparationUi.CreatePanel("StoryBackground", contentRect, new Vector2(0.32f, 0f), Vector2.one); // 본문 전용 배경 생성
        ApplyEventPanel(storyBackground.GetComponent<Image>()); // 설명 패널 테두리와 질감 적용
        storyBackground.GetComponent<Image>().raycastTarget = false; // 장식의 입력 가로채기 방지
        GameObject bodyObject =
            new GameObject(
                "BodyText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)); // 본문 텍스트 생성

        bodyObject.transform.SetParent(contentObject.transform, false); // 본문 텍스트를 내용 영역 하위에 배치

        RectTransform bodyRect =
            bodyObject.GetComponent<RectTransform>(); // 본문 RectTransform 조회

        bodyRect.anchorMin = new Vector2(0.32f, 0f); // 삽화 오른쪽에서 본문 시작
        bodyRect.anchorMax = new Vector2(1f, 1f); // 본문 상단 우측 앵커 설정
        bodyRect.offsetMin = new Vector2(34f, 0f); // 삽화와 본문 사이 여백 지정
        bodyRect.offsetMax = new Vector2(-26f, -20f); // 본문 우상단 여백 설정

        bodyText =
            bodyObject.GetComponent<TMP_Text>(); // 본문 텍스트 저장

        bodyText.text = string.Empty; // 본문 초기 문구 지정
        bodyText.fontSize = 29f; // 본문 글자 크기 지정
        bodyText.color = new Color(0.92f, 0.92f, 0.92f, 1f); // 본문 글자 색상 지정
        bodyText.alignment = TextAlignmentOptions.TopLeft; // 본문 글자 정렬 지정
        bodyText.textWrappingMode = TextWrappingModes.Normal; // 본문 줄바꿈 모드 설정
        bodyText.overflowMode = TextOverflowModes.Overflow; // 본문 오버플로 설정
        bodyText.lineSpacing = 8f; // 본문 줄 간격 설정
    }

    private static void ApplyEventPanel(Image target) // 이벤트 전용 직각 배경 적용
    { // 배경 설정 시작
        target.sprite = Resources.Load<Sprite>("UI/EventPanel"); // 무광 청록색 직사각형 원화 연결
        target.type = Image.Type.Sliced; // 패널 크기와 무관하게 네 변의 두께 유지
        target.color = Color.white; // 원본 색상 표시
    } // 배경 설정 종료

    private void CreateChoiceArea(Transform parent) // 하단 선택지 영역 생성
    {
        var list = PreparationUi.CreatePanel("Choices", parent, Vector2.zero, Vector2.right); // 하단 목록 패널 생성
        list.offsetMin = new Vector2(48f, ChoiceBottom - 8f); // 목록 좌하단 여백
        ApplyEventPanel(list.GetComponent<Image>()); // 목록 뒤 금속 테두리 배경 적용
        choiceScroll = list.gameObject.AddComponent<ScrollRect>(); // 휠과 드래그 입력 연결
        choiceScroll.horizontal = false; // 수평 이동 차단
        choiceScroll.movementType = ScrollRect.MovementType.Clamped; // 첫 줄과 끝 줄에서 이동 제한
        choiceScroll.scrollSensitivity = ChoiceHeight + ChoiceGap; // 휠 이동량을 한 줄로 설정
        var viewport = PreparationUi.CreatePanel("Viewport", list, Vector2.zero, Vector2.one); // 선택지가 보이는 영역
        viewport.GetComponent<Image>().color = Color.clear; // 배경을 보여주는 투명 입력 영역
        viewport.offsetMin = new Vector2(12f, 8f); // 목록 안쪽 여백
        viewport.gameObject.AddComponent<RectMask2D>(); // 보이지 않는 버튼의 렌더와 클릭 차단
        choiceScroll.viewport = viewport; // 스크롤 표시 영역 지정
        GameObject choiceObject = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); // 높이가 늘어나는 버튼 목록
        choiceContainer = choiceObject.GetComponent<RectTransform>(); // 선택지 목록 영역 저장
        choiceContainer.SetParent(viewport, false); // 잘림 영역 안에 목록 배치
        PreparationUi.Stretch(choiceContainer, Vector2.up, Vector2.one); // 상단 기준으로 가로 폭 채우기
        choiceContainer.pivot = new Vector2(0.5f, 1f); // 첫 줄부터 아래로 버튼 배치
        choiceScroll.content = choiceContainer; // 이동 대상 연결
        var track = PreparationUi.CreatePanel("ScrollTrack", list, Vector2.right, Vector2.one); // 오른쪽 스크롤 홈
        track.offsetMin = new Vector2(-32f, 10f); // 홈의 폭과 아래 여백
        track.offsetMax = new Vector2(-10f, -10f); // 위와 오른쪽 여백
        var grip = PreparationUi.CreatePanel("Handle", track, Vector2.zero, Vector2.one); // 드래그 손잡이 생성
        grip.GetComponent<Image>().color = new Color(0.82f, 0.65f, 0.34f); // 금빛 손잡이 색상
        var bar = track.gameObject.AddComponent<Scrollbar>(); // 수직 스크롤바 생성
        bar.direction = Scrollbar.Direction.BottomToTop; // 맨 위 값을 목록 시작에 연결
        bar.handleRect = grip; // 움직일 손잡이 지정
        bar.targetGraphic = grip.GetComponent<Image>(); // 손잡이 입력 강조 연결
        choiceScroll.verticalScrollbar = bar; // 목록과 스크롤바 값 동기화
        choiceScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; // 다음 프레임에도 짧은 목록의 손잡이 숨김 유지

        VerticalLayoutGroup layoutGroup =
            choiceObject.GetComponent<VerticalLayoutGroup>(); // 세로 레이아웃 그룹 조회

        layoutGroup.spacing = ChoiceGap; // 선택 버튼 사이 여백 적용
        layoutGroup.childControlWidth = true; // 버튼 폭 자동 제어 활성화
        layoutGroup.childControlHeight = false; // 버튼 높이 자동 제어 비활성화
        layoutGroup.childForceExpandWidth = true; // 버튼 폭 확장 활성화
        layoutGroup.childForceExpandHeight = false; // 버튼 높이 확장 비활성화

        ContentSizeFitter fitter =
            choiceObject.GetComponent<ContentSizeFitter>(); // 컨텐츠 크기 조정기 조회

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로 자동 맞춤 비활성화
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 세로 자동 맞춤 활성화
    }

    private static void StretchFullScreen(RectTransform rectTransform) // 전체 화면 확장 설정
    {
        rectTransform.anchorMin = Vector2.zero; // 좌하단 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 우상단 앵커 설정
        rectTransform.offsetMin = Vector2.zero; // 좌하단 오프셋 제거
        rectTransform.offsetMax = Vector2.zero; // 우상단 오프셋 제거
    }

    private static void StretchInsideParent(
        RectTransform rectTransform,
        float padding) // 부모 안쪽 여백 확장 설정
    {
        rectTransform.anchorMin = Vector2.zero; // 좌하단 앵커 설정
        rectTransform.anchorMax = Vector2.one; // 우상단 앵커 설정
        rectTransform.offsetMin = new Vector2(padding, padding); // 좌하단 여백 설정
        rectTransform.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 설정
    }

    private void OnDestroy() // 이벤트 패널 제거 처리
    {
        ReleaseBlurTexture(); // 블러 텍스처 정리

        if (instance == this)
        {
            if (currentEventData != null) // 열린 이벤트를 남긴 채 저장 씬 교체 확인
            { // 이전 화면 입력 잠금 정리 시작
                ExplorationPlayerController.SetInputBlocked(false); // 사라진 이벤트 화면의 이동 잠금 해제
            } // 이전 화면 입력 잠금 정리 종료
            instance = null; // 정적 인스턴스 정리
        }
    }
}
