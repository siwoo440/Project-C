using System.Collections.Generic; // 화면 레이캐스트 결과 목록
using System.Linq; // 손패 클릭 영역과 캔버스 조회
using UnityEngine.EventSystems; // 실제 화면 순서의 포인터 클릭 사용
using System.Collections; // 실제 전투 대기 사용
using NUnit.Framework; // 실제 전투 안내 확인 사용
using TMPro; // 안내 제목 확인 사용
using UnityEngine; // 실제 게임 객체 조회 사용
using UnityEngine.SceneManagement; // 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 안내 버튼 사용
public sealed class BattleTutorialSceneTests // 실제 전투 튜토리얼 연결 검사
{ // 실제 전투 안내 검사 시작
    [UnityTest] // 실제 전투 장면 안내 검사 지정
    public IEnumerator RealBattle_CardAndEnemyTurnAdvanceTutorial() // 실제 카드와 턴 이벤트 연결 확인
    { // 전투 안내 연결 검사 시작
        yield return new EnterPlayMode(); // 실제 게임 플레이 시작
        yield return PlayTutorial(); // 에디터 도메인 재시작 뒤 실제 입력 상태 생성
        yield return new ExitPlayMode(); // 실제 게임 플레이 종료
    } // 전투 안내 연결 검사 종료
    private static IEnumerator PlayTutorial() // 플레이 모드 안에서 실제 전투 안내 검사
    { // 실제 조작 확인 시작
        SceneManager.LoadScene("00_Boot"); // 게임 시작 화면 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 메뉴 준비 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 열기
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
        { // 거점 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 거점 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 준비 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티로 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 대기
        { // 탐사 플레이어 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 탐사 플레이어 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && SceneFlowManager.Instance.IsLoadingScene;) // 탐사 씬 이동 완료 대기
        { // 씬 이동 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 씬 이동 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 충분한 체력의 실제 보스 조우 조회
        Assert.IsNotNull(encounter); // 실제 보스 조우 확인
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("tutorial_integration_test", encounter, player.transform.position, player.transform.position)); // 실제 전투 조우 준비
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 전투 초기화 대기
        { // 전투 초기화 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 전투 초기화 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        BattleTutorialView tutorial = Object.FindFirstObjectByType<BattleTutorialView>(); // 실제 전투 안내 화면 조회
        Assert.IsNotNull(battle); // 전투 관리자 존재 확인
        Assert.IsNotNull(tutorial); // 전투 안내 생성 확인
        var hand = Object.FindFirstObjectByType<BattleHandView>(); // 대상 카드와 턴 종료 버튼을 보유한 실제 손패
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (SceneFlowManager.Instance.IsLoadingScene || hand.IsDealingOpeningHand);) // 로딩과 첫 손패의 작업 완료 대기
        { // 전투 조작 준비 시작
            yield return null; // 실제 입력 가능한 프레임 대기
        } // 전투 조작 준비 종료
        yield return null; // 로딩 화면 비활성화와 첫 레이아웃 반영
        Transform guide = tutorial.transform.Find("Guide"); // 실제 안내 화면 조회
        if (!guide.gameObject.activeSelf) // 자동 안내 미표시 확인
        { // 명시적 다시 보기 시작
            yield return Click(tutorial.transform.Find("ReplayButton").gameObject); // 실제 화면에서 도움말 다시 열기
        } // 명시적 다시 보기 종료
        TMP_Text title = guide.Find("Panel/Title").GetComponent<TMP_Text>(); // 안내 제목 조회
        Button next = guide.Find("Panel/NextButton").GetComponent<Button>(); // 안내 다음 버튼 조회
        yield return Click(next.gameObject); // 안내 위의 실제 버튼으로 카드 사용 페이지 열기
        Assert.AreEqual("카드 사용", title.text); // 실제 카드 사용 안내 확인
        Assert.IsNotNull(EventSystem.current.currentSelectedGameObject, "도움말 키보드 입력 초점"); // 설명을 읽는 동안 선택된 버튼 존재 확인
        Assert.IsTrue(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(guide)); // 가려진 게임 버튼에서 안내로 키보드 확인 초점 이동
        Assert.AreEqual(Navigation.Mode.Explicit, next.navigation.mode); // 방향키 이동을 안내 버튼 안으로 제한
        var layer = guide.GetComponent<Canvas>(); // 실제 도움말의 독립 정렬층 조회
        var level = Object.FindFirstObjectByType<PlayerLevelHudView>(); // 독립 캔버스의 실제 레벨 표시 조회
        var arrows = Object.FindFirstObjectByType<BattleEnemyArrowsView>(FindObjectsInactive.Include); // 표시 모드에 따른 비활성 화살표도 포함한 실제 독립 캔버스 확인
        Assert.IsNotNull(level, "레벨 표시 캔버스"); // 레벨 후처리 완료 확인
        Assert.IsNotNull(arrows, "적 행동 화살표 캔버스"); // 실제 전투의 화살표 생성 확인
        Assert.IsTrue(layer.overrideSorting); // 도움말 독립 정렬 확인
        Assert.Greater(layer.sortingOrder, level.GetComponentInParent<Canvas>().sortingOrder); // 도움말이 실제 레벨 표시보다 앞에 표시
        Assert.Greater(layer.sortingOrder, arrows.GetComponentInParent<Canvas>().sortingOrder); // 도움말이 실제 화살표보다 앞에 표시
        Assert.IsNotNull(guide.Find("Panel"), "예시 패널"); // 실제 도움말의 패널 연결 확인
        Assert.AreEqual(1f, guide.Find("Panel").GetComponent<Image>().color.a); // 완전히 불투명한 실제 회색 패널 확인
        BattleCardView card = null; // 사용할 실제 카드 초기화
        foreach (BattleCardView candidate in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 실제 손패 카드 순회
        { // 공격 카드 확인 시작
            if (candidate.RuntimeCard != null && battle.BattleDeck.Hand.Contains(candidate.RuntimeCard) && candidate.RuntimeCard.EffectType == CardEffectType.Damage && candidate.RuntimeCard.TargetType == CardTargetType.SingleEnemy && candidate.RuntimeCard.ApCost <= battle.SharedActionPoints.CurrentActionPoints) // 사용 가능한 단일 적 공격 확인
            { // 공격 카드 선택 시작
                card = candidate; // 실제 공격 카드 보관
                break; // 공격 카드 검색 종료
            } // 공격 카드 선택 종료
        } // 공격 카드 확인 종료
        Assert.IsNotNull(card); // 실제 손패 공격 카드 확인
        BattleUnitView enemy = null; // 실제 적 화면 초기화
        foreach (BattleUnitView candidate in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 실제 유닛 화면 순회
        { // 적 화면 확인 시작
            if (candidate.RuntimeUnit == battle.EnemyUnits[0]) // 보스 유닛 일치 확인
            { // 적 화면 보관 시작
                enemy = candidate; // 실제 보스 화면 보관
                break; // 적 화면 검색 종료
            } // 적 화면 보관 종료
        } // 적 화면 확인 종료
        Assert.IsNotNull(enemy); // 실제 적 화면 확인
        var region = hand.GetComponentsInChildren<BattleCardHitRegion>().Single(hit => hit.CardView == card); // 겹친 카드의 실제 클릭 영역
        var endButton = hand.transform.Find("EndTurnButton").GetComponent<Button>(); // 실제 턴 종료 버튼
        AssertCovered(guide, region.transform); // 예시 뒤 카드 영역의 첫 입력은 도움말에서 수신
        AssertCovered(guide, endButton.transform); // 예시 뒤 턴 종료 영역의 첫 입력도 도움말에서 수신
        yield return Click(next.gameObject); // 카드 실습을 위해 예시 확인
        Assert.IsFalse(guide.gameObject.activeSelf); // 실제 손패와 적이 보이는 상태 확인
        for (float end = Time.realtimeSinceStartup + 8f; Time.realtimeSinceStartup < end && !endButton.interactable;) // 시작 연출과 실제 손패 입력 잠금 해제 대기
        { // 첫 조작 준비 시작
            yield return null; // 전투 조작을 받는 프레임까지 대기
        } // 첫 조작 준비 종료
        Assert.IsTrue(endButton.interactable, "실제 아군 입력 가능 상태"); // 안내를 접은 뒤 정상 전투 입력 확인
        CardInstance clicked = null; // 실제 손패가 전달한 카드 보관
        hand.CardClicked += value => clicked = value; // 포인터 입력이 실제 손패까지 도달하는지 확인
        int before = battle.SharedActionPoints.CurrentActionPoints; // 클릭 전 행동력 보관
        yield return Click(region.gameObject, enemy); // 실제 레이캐스트 손패에서 적에게 끌어 놓기
        Assert.AreSame(card.RuntimeCard, clicked, card.RuntimeCard.DisplayName + " 드래그 사용"); // 유효한 적에게 놓은 카드만 실제 처리기에 전달
        Assert.AreEqual(before - clicked.ApCost, battle.SharedActionPoints.CurrentActionPoints, clicked.DisplayName + " 대상 클릭"); // 실제 공격 대상 선택과 행동력 지불 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (title.text != "행동력 AP" || !guide.gameObject.activeSelf);) // 실제 카드 사용 안내 이동 대기
        { // 카드 사용 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 카드 사용 대기 종료
        Assert.AreEqual("행동력 AP", title.text); // 실제 카드 사용 이벤트 안내 반영 확인
        Assert.IsTrue(guide.gameObject.activeSelf); // 실제 카드 사용과 복귀 연출 뒤 안내 다시 표시
        yield return Click(next.gameObject); // 정신력 안내 이동
        yield return Click(next.gameObject); // 적 행동 예고 안내 이동
        yield return Click(next.gameObject); // 턴 종료 안내 이동
        Assert.AreEqual("턴 종료", title.text); // 실제 턴 종료 안내 확인
        Button endTurn = Object.FindFirstObjectByType<BattleHandView>().transform.Find("EndTurnButton").GetComponent<Button>(); // 실제 턴 종료 버튼 조회
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && !endTurn.interactable;) // 카드 연출 완료 대기
        { // 카드 연출 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 카드 연출 대기 종료
        Assert.IsTrue(endTurn.interactable); // 실제 턴 종료 가능 확인
        AssertCovered(guide, endTurn.transform); // 턴 종료 실습을 시작하기 전 전투 입력 차단
        yield return Click(next.gameObject); // 예시 패널을 접고 전투 화면 표시
        Assert.IsFalse(guide.gameObject.activeSelf); // 턴 종료 버튼이 보이는 상태 확인
        yield return Click(endTurn.gameObject); // 실제 포인터로 턴 종료 버튼 클릭
        Assert.AreEqual("적 행동 관찰", title.text); // 실제 적 턴 시작 안내 반영 확인
        Assert.IsFalse(guide.gameObject.activeSelf); // 실제 적 행동을 안내로 가리지 않는 상태 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && !guide.gameObject.activeSelf;) // 실제 적 행동 종료 대기
        { // 적 행동 대기 시작
            yield return null; // 다음 화면 프레임 대기
        } // 적 행동 대기 종료
        Assert.IsTrue(guide.gameObject.activeSelf); // 적 행동 뒤 마지막 안내의 자동 복귀 확인
        Assert.IsTrue(next.interactable); // 다음 아군 턴에서 안내 완료 가능 확인
        yield return Click(next.gameObject); // 마지막 안내를 실제 버튼으로 닫기
        Assert.IsFalse(guide.gameObject.activeSelf); // 설명 종료 뒤 전투 입력 복구 확인
    } // 실제 조작 확인 종료
    private static GameObject TopHit(Vector2 point, out PointerEventData pointer) // 지정 화면 위치의 실제 첫 레이캐스트 조회
    { // 화면 좌표 검사 시작
        var events = EventSystem.current; // 실제 전투 입력 시스템 조회
        Assert.IsNotNull(events); // 실제 이벤트 시스템 확인
        pointer = new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }; // 왼쪽 포인터 입력 구성
        var hits = new List<RaycastResult>(); // 모든 화면 레이캐스트 결과 준비
        events.RaycastAll(pointer, hits); // 독립 캔버스를 포함한 실제 정렬 순서 조회
        pointer.pointerCurrentRaycast = hits.Count > 0 ? hits[0] : default; // 실제 드래그가 읽을 맨 앞의 레이캐스트 보관
        return pointer.pointerCurrentRaycast.gameObject; // 가장 앞에 표시되는 실제 입력 객체 반환
    } // 화면 좌표 검사 종료
    private static Vector2 Point(Transform target, float x, float y) // 화면 요소 안쪽의 지정한 위치 계산
    { // 화면 위치 계산 시작
        var rect = (RectTransform)target; // 대상의 실제 화면 사각형 조회
        var camera = rect.GetComponentInParent<Canvas>().rootCanvas.worldCamera; // 화면과 카메라 방식에 맞춘 투영 카메라 조회
        return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(Rect.NormalizedToPoint(rect.rect, new Vector2(x, y)))); // 화면 픽셀 좌표 반환
    } // 화면 위치 계산 종료
    private static void AssertCovered(Transform guide, Transform target) // 안내 아래 게임 입력 차단 검사
    { // 입력 가림 검사 시작
        Canvas.ForceUpdateCanvases(); // 현재 프레임의 캔버스 깊이와 위치 반영
        var hit = TopHit(Point(target, 0.5f, 0.5f), out _); // 실제 게임 버튼 중앙의 맨 앞 입력 대상 확인
        Assert.IsNotNull(hit, target.name); // 대상 위치에서 입력을 받는 화면 존재 확인
        Assert.IsTrue(hit.transform.IsChildOf(guide), target.name + " / " + hit.name); // 전체 안내나 그 위 조작 요소가 입력을 먼저 받는지 확인
    } // 입력 가림 검사 종료
    private static IEnumerator Click(GameObject target, BattleUnitView drop = null) // 일부가 겹친 실제 게임 화면의 보이는 영역 클릭
    { // 포인터 입력 시작
        float[] samples = { 0.5f, 0.25f, 0.75f, 0.1f, 0.9f }; // 중앙과 네 방향의 실제 클릭 후보
        float deadline = Time.realtimeSinceStartup + 8f; // 새 캔버스가 실제로 렌더링될 때까지의 제한
        GameObject front = null; // 버튼을 가린 마지막 객체 기록
        do // 새 화면의 첫 렌더링과 입력 영역 갱신 대기
        { // 실제 입력 탐색 시작
            Canvas.ForceUpdateCanvases(); // 현재 프레임의 화면 위치와 레이캐스트 정렬 갱신
            foreach (float y in samples) // 대상의 세로 위치 순회
            { // 세로 위치 확인 시작
                foreach (float x in samples) // 대상의 가로 위치 순회
                { // 앞쪽 입력 검사 시작
                    front = TopHit(Point(target.transform, x, y), out var pointer); // 실제 화면 맨 앞의 입력 요소 조회
                    if (front != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(front) == target) // 사용자가 클릭할 수 있는 영역 확인
                    { // 실제 클릭 전달 시작
                        if (drop == null) // 일반 버튼 입력 확인
                        { // 화면 버튼 클릭 시작
                            ExecuteEvents.ExecuteHierarchy(front, pointer, ExecuteEvents.pointerClickHandler); // 맨 앞의 실제 게임 요소 클릭
                        } // 화면 버튼 클릭 종료
                        else // 실제 게임에서 카드를 끌어 놓는 입력
                        { // 카드 드래그 시작
                            var drag = ExecuteEvents.GetEventHandler<IDragHandler>(front); // 손패의 실제 드래그 입력 영역 조회
                            Assert.IsNotNull(drag, "손패 드래그 영역"); // 실제 카드 입력 연결 확인
                            ExecuteEvents.Execute(drag, pointer, ExecuteEvents.beginDragHandler); // 앞에 보이는 손패에서 드래그 시작
                            var hit = TopHit(Point(drop.transform, 0.5f, 0.5f), out var end); // 실제 적 중앙의 드롭 대상 레이캐스트
                            Assert.IsNotNull(hit, "적 드롭 영역"); // 전투 화면의 적 입력 영역 확인
                            Assert.AreSame(drop, hit.GetComponentInParent<BattleUnitView>()); // 최상단 레이캐스트가 선택 적에 속하는지 확인
                            pointer.position = end.position; // 같은 포인터의 적 화면 위치 갱신
                            pointer.pointerCurrentRaycast = end.pointerCurrentRaycast; // 실제 드롭 위치의 맨 앞 충돌 정보 갱신
                            ExecuteEvents.Execute(drag, pointer, ExecuteEvents.dragHandler); // 카드 이동과 실제 적 강조 처리
                            ExecuteEvents.Execute(drag, pointer, ExecuteEvents.endDragHandler); // 적 위에서 카드를 놓고 정상 사용
                        } // 카드 드래그 종료
                        yield break; // 실제 입력 완료
                    } // 실제 클릭 전달 종료
                } // 앞쪽 입력 검사 종료
            } // 세로 위치 확인 종료
            yield return null; // 새 캔버스 표시와 그래픽 깊이 반영 대기
        } // 실제 입력 탐색 종료
        while (Time.realtimeSinceStartup < deadline); // 제한 시간까지 실제 렌더 프레임 재검사
        var image = target.GetComponent<Graphic>(); // 클릭 실패 시 그래픽 상태 기록
        Assert.Fail($"클릭 화면 없음 {target.name}, 앞 {front?.name}, 위치 {Point(target.transform, 0.5f, 0.5f)}, 화면 {Screen.width}×{Screen.height}, 깊이 {image?.depth}, 표시 {target.activeInHierarchy}"); // 가려짐과 미렌더링 원인 구분
    } // 포인터 입력 종료
} // 실제 전투 안내 검사 종료
