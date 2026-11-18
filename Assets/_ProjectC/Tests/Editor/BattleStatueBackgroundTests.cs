using System.Collections; // 플레이 모드 대기 사용
using System.Reflection; // 배경 재연결 검사 사용
using NUnit.Framework; // 실제 전투 배경 검사 사용
using UnityEngine; // 전투 화면과 리소스 사용
using UnityEngine.EventSystems; // 실제 카드 드래그 입력 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 배경과 손패 화면 사용

public sealed class BattleStatueBackgroundTests // 전투 석상 배경 검사
{ // 배경 검사 시작
    [UnityTest] // 실제 전투 화면 검사 지정
    public IEnumerator BattleShowsStatueBehindUnitsAndCardsWithoutBlockingInput() // 전투 배경 연결 확인
    { // 전투 배경 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 대기
        { // 거점 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0); // 실제 파티 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 대기
        { // 탐사 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 탐사 플레이어 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && SceneFlowManager.Instance.IsLoadingScene;) // 씬 이동 완료 대기
        { // 씬 이동 대기 시작
            yield return null; // 다음 프레임 대기
        } // 씬 이동 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 전투 조우 조회
        Assert.IsNotNull(encounter); // 실제 전투 조우 확인
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("statue_background_test", encounter, player.transform.position, player.transform.position)); // 독립 전투 조우 시작
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 실제 전투 초기화 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        Assert.IsNotNull(battle); // 실제 전투 화면 확인
        Assert.IsTrue(battle.IsInitialized); // 실제 전투 초기화 확인
        BattleHandView hand = Object.FindFirstObjectByType<BattleHandView>(); // 실제 카드 손패 조회
        Assert.IsNotNull(hand); // 실제 손패 존재 확인
        Canvas canvas = hand.GetComponentInParent<Canvas>(); // 실제 전투 캔버스 조회
        Assert.IsNotNull(canvas); // 실제 전투 캔버스 확인
        Transform levelHud = hand.transform.Find("PlayerLevelHud"); // 손패 안의 레벨 표시 조회
        Assert.IsNotNull(levelHud); // 레벨 표시의 손패 중앙 이동 확인
        Transform turnFrame = canvas.transform.Find("TurnStatusFrame"); // 화면 위의 턴 표시 조회
        Assert.IsNotNull(turnFrame); // 턴 표시의 화면 상단 이동 확인
        Assert.IsNotNull(turnFrame.Find("TurnStatusText")); // 상단 프레임의 실제 턴 글자 확인
        Texture2D texture = Resources.Load<Texture2D>("UI/Battle/StatueBackground"); // 제작한 석상 배경 리소스 조회
        Assert.IsNotNull(texture); // 석상 배경 리소스 확인
        Transform background = canvas.transform.Find("BattleStatueBackground"); // 실제 전투 배경 조회
        Assert.IsNotNull(background); // 실제 전투 배경 존재 확인
        Assert.AreEqual(0, background.GetSiblingIndex()); // 캐릭터와 카드 뒤 배치 확인
        RawImage image = background.GetComponent<RawImage>(); // 실제 배경 이미지 조회
        Assert.IsNotNull(image); // 배경 이미지 구성 확인
        Assert.AreSame(texture, image.texture); // 실제 석상 이미지 연결 확인
        Assert.IsFalse(image.raycastTarget); // 카드와 버튼 입력 차단 방지 확인
        Assert.AreEqual(Vector2.zero, image.rectTransform.anchorMin); // 배경 화면 시작점 확인
        Assert.AreEqual(Vector2.one, image.rectTransform.anchorMax); // 배경 화면 끝점 확인
        Assert.AreEqual(Vector2.zero, image.rectTransform.offsetMin); // 배경 화면 왼쪽 아래 여백 확인
        Assert.AreEqual(Vector2.zero, image.rectTransform.offsetMax); // 배경 화면 오른쪽 위 여백 확인
        Assert.Greater(battle.AllyUnits.Count, 0); // 실제 아군 표시 대상 유지 확인
        Assert.Greater(battle.EnemyUnits.Count, 0); // 실제 적 표시 대상 유지 확인
        Assert.Greater(battle.BattleDeck.HandCount, 0); // 실제 손패 표시 데이터 유지 확인
        yield return null; // 실제 카드 드래그 처리기 연결 대기
        Transform hitRoot = hand.transform.Find("HandHitRoot"); // 실제 손패 입력 영역 조회
        BattleCardHitRegion selectedHit = null; // 드래그할 카드 입력 초기화
        BattleCardHitRegion leftHit = null; // 선택 카드 왼쪽 입력 초기화
        for (int index = 1; index < hitRoot.childCount; index++) // 왼쪽 이웃 카드가 있는 손패 순회
        { // 드래그 가능한 카드 탐색 시작
            BattleCardHitRegion candidate = hitRoot.GetChild(index).GetComponent<BattleCardHitRegion>(); // 현재 입력 카드 조회
            if (candidate.CardView.GetComponent<CanvasGroup>().alpha > 0.9f && candidate.CardView.GetComponent<BattleCardDragHandler>() != null) // 사용 가능한 실제 드래그 카드 확인
            { // 드래그 카드 저장 시작
                selectedHit = candidate; // 선택 카드 입력 저장
                leftHit = hitRoot.GetChild(index - 1).GetComponent<BattleCardHitRegion>(); // 왼쪽 이웃 카드 입력 저장
                break; // 드래그 카드 탐색 종료
            } // 드래그 카드 저장 종료
        } // 드래그 가능한 카드 탐색 종료
        Assert.IsNotNull(selectedHit); // 실제 드래그 가능한 손패 확인
        float leftBefore = ((RectTransform)leftHit.CardView.transform).anchoredPosition.x; // 왼쪽 카드 기본 위치 저장
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(); // 실제 포인터 입력 시스템 조회
        Assert.IsNotNull(eventSystem); // 실제 포인터 입력 시스템 확인
        PointerEventData pointer = new PointerEventData(eventSystem); // 카드 드래그 포인터 생성
        pointer.button = PointerEventData.InputButton.Left; // 왼쪽 마우스 입력 설정
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, selectedHit.transform.position); // 카드 입력 위치 설정
        selectedHit.OnPointerEnter(pointer); // 카드 마우스 진입 처리
        selectedHit.OnBeginDrag(pointer); // 실제 카드 드래그 시작 처리
        Assert.IsNotNull(GameObject.Find("Day47CardDragGhost")); // 실제 드래그 카드 복제 확인
        Assert.Less(((RectTransform)leftHit.CardView.transform).anchoredPosition.x, leftBefore); // 드래그 중 이웃 카드 간격 유지 확인
        selectedHit.OnEndDrag(null); // 유효 대상 없는 드래그 취소
        Assert.AreEqual(leftBefore, ((RectTransform)leftHit.CardView.transform).anchoredPosition.x, 0.01f); // 드래그 취소 후 간격 복원 확인
        int beforeChildren = canvas.transform.childCount; // 전투 캔버스 자식 수 보관
        typeof(BattleSceneSetup).GetMethod("InstallBattleBackground", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, null); // 배경 연결 재실행
        battle.InitializeBattle(); // 전투 중복 초기화 시도
        Assert.AreEqual(beforeChildren, canvas.transform.childCount); // 전투 배경 중복 생성 방지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 배경 검사 종료
} // 배경 검사 종료
