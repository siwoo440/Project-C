using System.Collections; // 플레이 모드 대기 사용
using System.Reflection; // 배경 재연결 검사 사용
using NUnit.Framework; // 실제 전투 배경 검사 사용
using UnityEngine; // 전투 화면과 리소스 사용
using UnityEngine.EventSystems; // 실제 카드 드래그 입력 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 배경과 손패 화면 사용

public sealed class BattleStatueBackgroundTests // 상단 전장과 하단 카드 배경 및 접지 검사
{ // 배경 검사 시작
    [UnityTest] // 실제 전투 화면 검사 지정
    public IEnumerator BattleShowsSeparateStageAndHudWithContactShadowsBehindInput() // 배경 분리와 유닛 그림자 및 실제 카드 입력 확인
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
        var stage = canvas.transform.Find("BattleStageBackground")?.GetComponent<RawImage>(); // 상단 지하 전장의 실제 이미지
        var footer = canvas.transform.Find("BattleHudBackground")?.GetComponent<RawImage>(); // 카드 뒤의 독립 검은 원화
        Assert.IsNotNull(stage); // 실제 상단 배경 배치 확인
        Assert.IsNotNull(footer); // 실제 하단 배경 배치 확인
        Assert.AreSame(Resources.Load<Texture2D>("UI/Battle/SunkenVaultStage"), stage.texture); // 실제 생성한 상단 이미지 확인
        Assert.AreSame(Resources.Load<Texture2D>("UI/Battle/SunkenVaultHud"), footer.texture); // 실제 생성한 하단 이미지 확인
        Assert.IsNotNull(stage.texture); // 원화 누락에 따른 단색 대체 여부 확인
        Assert.IsNotNull(footer.texture); // 검은 배경 원화 임포트 확인
        Assert.AreEqual(0, stage.transform.GetSiblingIndex()); // 원화가 실제 모든 전투 UI 뒤에 있는지 확인
        Assert.AreEqual(1, footer.transform.GetSiblingIndex()); // 검은 배경과 카드 화면의 실제 표시 순서 확인
        Assert.AreEqual(1f, stage.rectTransform.anchorMax.y); // 상단 배경의 화면 위 끝 확인
        Assert.AreEqual(0f, footer.rectTransform.anchorMin.y); // 하단 배경의 화면 아래 끝 확인
        Assert.AreEqual(stage.rectTransform.anchorMin.y, footer.rectTransform.anchorMax.y); // 전투와 카드 배경이 틈 없이 맞닿는지 확인
        Assert.AreEqual(Vector2.zero, stage.rectTransform.offsetMin); // 화면 전체 폭의 상단 배치 확인
        Assert.AreEqual(Vector2.zero, footer.rectTransform.offsetMax); // 화면 전체 폭의 하단 배치 확인
        Assert.IsFalse(stage.raycastTarget || footer.raycastTarget); // 카드와 버튼 입력을 가리지 않는 배경 확인
        Assert.Greater(battle.AllyUnits.Count, 0); // 실제 아군 표시 대상 유지 확인
        Assert.Greater(battle.EnemyUnits.Count, 0); // 실제 적 표시 대상 유지 확인
        Assert.Greater(battle.BattleDeck.HandCount, 0); // 실제 손패 표시 데이터 유지 확인
        yield return null; // 실제 카드 드래그 처리기 연결 대기
        BattleUnitView tracked = null; // 공격 움직임과의 실제 접지를 확인할 아군
        foreach (var unit in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 진영마다 표시하는 실제 전투 개체 조사
        { // 원화 접점 검사 시작
            if (unit.RuntimeUnit == null || unit.RuntimeUnit.IsDead) // 그림자를 그리지 않는 전투불능 대상 제외
            { // 생존 대상 선택 시작
                continue; // 다음 표시 유닛 확인
            } // 생존 대상 선택 종료
            var portrait = unit.transform.Find("Portrait"); // 실제 캐릭터나 적의 그림
            var shadow = unit.transform.Find("GroundContactShadow")?.GetComponent<RawImage>(); // 생존 상태에 연결된 그림자
            Assert.IsNotNull(shadow, unit.name); // 실제 원화마다 접지 그림자 확인
            Assert.IsTrue(shadow.enabled); // 살아 있는 화면에서 그림자 활성화 확인
            Assert.IsFalse(shadow.raycastTarget); // 캐릭터와 적 선택 방해 없음 확인
            Assert.Less(shadow.transform.GetSiblingIndex(), portrait.GetSiblingIndex()); // 캐릭터 뒤에 그림자 배치 확인
            Assert.Less(shadow.transform.position.y, portrait.position.y); // 원화 아래의 발 주변으로 배치 확인
            Assert.IsTrue(stage.rectTransform.rect.Contains(stage.rectTransform.InverseTransformPoint(shadow.transform.position))); // 검은 HUD 영역이 아닌 상단 전장 안에 그림자 확인
            if (unit.RuntimeUnit.Team == BattleTeam.Ally && tracked == null) // 실제 공격 전진을 할 첫 생존 아군 조회
            { // 아군 공격 동작 선택 시작
                tracked = unit; // 그림자 이동을 검사할 아군 기록
            } // 아군 공격 동작 선택 종료
        } // 원화 접점 검사 종료
        Assert.IsNotNull(tracked); // 공격 움직임 검증의 실제 아군 확인
        Transform movedShadow = tracked.transform.Find("GroundContactShadow"); // 이동 전 실제 그림자 좌표
        var movingPortrait = (RectTransform)tracked.transform.Find("Portrait"); // 동작 중 움직이는 실제 원화 좌표
        float startX = movedShadow.position.x; // 실제 발 위치에서 처음 가로값 저장
        Vector2 start = movingPortrait.anchoredPosition; // 원화의 애니메이션 시작 위치
        movingPortrait.anchoredPosition += Vector2.right * 22f; // 공격 전진에서 쓰는 원화 좌표를 검사 프레임에 적용
        tracked.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver); // 원화 이동 직후 실제 접지 레이어 갱신
        Assert.Greater(movedShadow.position.x, startX); // 원화의 실제 가로 이동을 따라가는 그림자 확인
        movingPortrait.anchoredPosition = start; // 공격 프레임 전의 원화 위치로 복원
        tracked.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver); // 복귀한 원화와 같은 위치로 접지 갱신
        Assert.AreEqual(startX, movedShadow.position.x, 0.05f); // 같은 전투 위치로 돌아온 접지 확인
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
