using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 전투 배치 검증 사용
using TMPro; // 적 행동 순번 글자 검사 사용
using UnityEngine; // 화면 좌표와 전투 객체 사용
using UnityEngine.SceneManagement; // 실제 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 화살표 표시 버튼 검사 사용
public sealed class BattlefieldPlacementTests // 배경에 맞춘 전투 유닛 배치 검사
{ // 전투 배치 검사 시작
    [UnityTest] // 실제 전투 화면 검사 지정
    public IEnumerator UnitsAndStatusFitBattlefieldAboveHand() // 유닛과 상태 표시의 전투 영역 배치 확인
    { // 실제 전투 배치 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 화면 대기
        { // 거점 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 화면 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 파티로 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 화면 대기
        { // 탐사 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 화면 대기 종료
        ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
        Assert.IsNotNull(player); // 실제 탐사 플레이어 확인
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && SceneFlowManager.Instance.IsLoadingScene;) // 씬 이동 완료 대기
        { // 씬 이동 대기 시작
            yield return null; // 다음 프레임 대기
        } // 씬 이동 대기 종료
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore"); // 실제 보스 조우 조회
        Assert.IsTrue(ExplorationSessionManager.Instance.BeginEncounter("battlefield_placement_test", encounter, player.transform.position, player.transform.position)); // 실제 보스 조우 시작
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && (Object.FindFirstObjectByType<BattleSceneSetup>() == null || !Object.FindFirstObjectByType<BattleSceneSetup>().IsInitialized);) // 전투 초기화 대기
        { // 전투 초기화 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 초기화 대기 종료
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        Assert.IsNotNull(battle); // 실제 전투 관리자 확인
        Assert.IsTrue(battle.IsInitialized); // 전투 초기화 완료 확인
        Assert.IsTrue(battle.AllyUnits[0].ApplyPersistentMental(100)); // 실제 상태 아이콘 표시 설정
        yield return null; // 상태 아이콘 배치 대기
        Canvas.ForceUpdateCanvases(); // 실제 전투 화면 배치 갱신
        BattleHandView hand = Object.FindFirstObjectByType<BattleHandView>(); // 실제 손패 화면 조회
        RectTransform canvas = hand.GetComponentInParent<Canvas>().transform as RectTransform; // 전투 화면 영역 조회
        RectTransform handRect = hand.transform as RectTransform; // 손패 화면 영역 조회
        Vector3[] corners = new Vector3[4]; // 화면 경계 계산 배열 생성
        handRect.GetWorldCorners(corners); // 실제 손패 화면 꼭짓점 조회
        float handTop = canvas.InverseTransformPoint(corners[1]).y; // 손패 위쪽 화면 경계 계산
        float battlefieldTop = canvas.rect.yMin + canvas.rect.height * 0.83f; // 배경에서 유닛이 시작할 위쪽 경계
        BattleUnitView[] units = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None); // 실제 아군과 적 화면 조회
        Assert.GreaterOrEqual(units.Length, 5); // 네 아군과 적 화면 확인
        bool intentVisible = false; // 적 행동 표시 확인 여부 초기화
        foreach (BattleUnitView unit in units) // 실제 전투 유닛 순회
        { // 유닛 화면 범위 확인 시작
            RectTransform portrait = unit.transform.Find("Portrait") as RectTransform; // 캐릭터 그림 영역 조회
            portrait.GetWorldCorners(corners); // 캐릭터 그림 꼭짓점 조회
            Assert.LessOrEqual(canvas.InverseTransformPoint(corners[1]).y, battlefieldTop + 1f); // 그림의 배경 상단 영역 포함 확인
            Assert.GreaterOrEqual(canvas.InverseTransformPoint(corners[0]).x, canvas.rect.xMin - 1f); // 그림의 화면 왼쪽 경계 확인
            Assert.LessOrEqual(canvas.InverseTransformPoint(corners[2]).x, canvas.rect.xMax + 1f); // 그림의 화면 오른쪽 경계 확인
            RectTransform mental = unit.transform.Find("MentalView") as RectTransform; // 정신력 바 영역 조회
            mental.GetWorldCorners(corners); // 정신력 바 꼭짓점 조회
            Assert.Greater(canvas.InverseTransformPoint(corners[0]).y, handTop); // 정신력 바와 손패 겹침 방지 확인
            Transform intent = unit.transform.Find("Day47IntentIcon"); // 현재 적 행동 아이콘 조회
            if (intent != null && intent.gameObject.activeSelf) // 표시 중인 적 행동 아이콘 확인
            { // 적 행동 아이콘 경계 검사 시작
                intentVisible = true; // 실제 적 행동 아이콘 표시 기록
                (intent as RectTransform).GetWorldCorners(corners); // 적 행동 아이콘 꼭짓점 조회
                RectTransform round = canvas.Find("TurnStatusFrame") as RectTransform; // 실제 상단 라운드 패널 영역 조회
                Assert.IsNotNull(round); // 실제 라운드 패널 존재 확인
                float intentTop = canvas.InverseTransformPoint(corners[1]).y; // 행동 아이콘의 위쪽 높이 계산
                round.GetWorldCorners(corners); // 라운드 패널 화면 꼭짓점 조회
                Assert.Less(intentTop, canvas.InverseTransformPoint(corners[0]).y); // 행동 아이콘의 라운드 패널 아래 위치 확인
            } // 적 행동 아이콘 경계 검사 종료
        } // 유닛 화면 범위 확인 종료
        Assert.IsTrue(intentVisible); // 실제 적 행동 아이콘 표시 확인
        BattleUnitView first = null; // 상태 아이콘 표시 유닛 초기화
        foreach (BattleUnitView unit in units) // 실제 전투 유닛 순회
        { // 상태 아이콘 유닛 탐색 시작
            if (unit.RuntimeUnit == battle.AllyUnits[0]) // 정신 상태를 설정한 아군 확인
            { // 상태 아이콘 유닛 저장 시작
                first = unit; // 실제 아군 화면 저장
                break; // 아군 화면 탐색 종료
            } // 상태 아이콘 유닛 저장 종료
        } // 상태 아이콘 유닛 탐색 종료
        Assert.IsNotNull(first); // 상태 아이콘 아군 화면 확인
        RectTransform backlight = first.transform.Find("MentalStateBacklight") as RectTransform; // 실제 각성 역광 영역 조회
        backlight.GetWorldCorners(corners); // 각성 역광 꼭짓점 조회
        Assert.LessOrEqual(canvas.InverseTransformPoint(corners[1]).y, battlefieldTop + 1f); // 역광의 배경 상단 영역 포함 확인
        RectTransform status = first.transform.Find("StatusEffects") as RectTransform; // 실제 상태 아이콘 영역 조회
        status.GetWorldCorners(corners); // 상태 아이콘 꼭짓점 조회
        Assert.Greater(canvas.InverseTransformPoint(corners[0]).y, handTop); // 상태 아이콘과 손패 겹침 방지 확인
        EncounterData summonEncounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 소환 적 원본 조회
        Assert.IsTrue(battle.TrySummonEnemy(summonEncounter.Enemies[0])); // 실제 전투 중 적 소환 확인
        yield return null; // 소환 적 화면 배치 대기
        Canvas.ForceUpdateCanvases(); // 소환 적 전투 화면 배치 갱신
        BattleUnitRuntime summoned = battle.EnemyUnits[battle.EnemyUnits.Count - 1]; // 실제 소환된 적 조회
        BattleUnitView summonedView = null; // 소환 적 화면 초기화
        foreach (BattleUnitView unit in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 현재 전투 유닛 화면 순회
        { // 소환 적 화면 탐색 시작
            if (unit.RuntimeUnit == summoned) // 실제 소환 적 화면 확인
            { // 소환 적 화면 저장 시작
                summonedView = unit; // 실제 소환 적 화면 저장
                break; // 소환 적 화면 탐색 종료
            } // 소환 적 화면 저장 종료
        } // 소환 적 화면 탐색 종료
        Assert.IsNotNull(summonedView); // 소환 적 화면 존재 확인
        RectTransform summonedPortrait = summonedView.transform.Find("Portrait") as RectTransform; // 소환 적 그림 영역 조회
        summonedPortrait.GetWorldCorners(corners); // 소환 적 그림 꼭짓점 조회
        Assert.LessOrEqual(canvas.InverseTransformPoint(corners[1]).y, battlefieldTop + 1f); // 소환 적의 배경 상단 영역 포함 확인
        Assert.GreaterOrEqual(canvas.InverseTransformPoint(corners[0]).x, canvas.rect.xMin - 1f); // 소환 적의 화면 왼쪽 경계 확인
        Assert.LessOrEqual(canvas.InverseTransformPoint(corners[2]).x, canvas.rect.xMax + 1f); // 소환 적의 화면 오른쪽 경계 확인
        Assert.GreaterOrEqual(battle.EnemyActionRuntime.PrepareActions(), 2); // 실제 보스와 소환 적의 행동 준비 확인
        yield return new WaitForSecondsRealtime(0.1f); // 실제 게임 프레임의 적 행동 화면 갱신 대기
        Canvas.ForceUpdateCanvases(); // 적 행동 화면 배치 갱신
        BattleEnemyArrowsView arrows = Object.FindFirstObjectByType<BattleEnemyArrowsView>(); // 실제 대상 화살표 화면 조회
        Assert.IsNotNull(arrows); // 대상 화살표 화면 존재 확인
        Assert.IsFalse(arrows.raycastTarget); // 화살표의 카드와 유닛 입력 차단 방지 확인
        Assert.AreEqual(battle.EnemyActionRuntime.PlannedActions.Count, arrows.ArrowCount); // 실제 예정 행동마다 화살표 표시 확인
        for (int index = 0; index < battle.EnemyActionRuntime.PlannedActions.Count; index++) // 실제 예정 적 행동 순회
        { // 적 행동과 화살표 검사 시작
            BattleEnemyAction action = battle.EnemyActionRuntime.PlannedActions[index]; // 현재 예정 행동 조회
            BattleUnitView actor = null; // 실제 행동 적 화면 초기화
            foreach (BattleUnitView unit in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 실제 전투 유닛 화면 순회
            { // 행동 적 화면 탐색 시작
                if (unit.RuntimeUnit == action.Actor) // 현재 행동 적 확인
                { // 행동 적 저장 시작
                    actor = unit; // 실제 행동 적 화면 저장
                    break; // 행동 적 화면 탐색 종료
                } // 행동 적 저장 종료
            } // 행동 적 화면 탐색 종료
            Assert.IsNotNull(actor); // 행동 적 화면 존재 확인
            Transform order = actor.transform.Find("EnemyActionOrder"); // 적 머리 위 행동 순번 표시 조회
            Assert.IsNotNull(order); // 적 행동 순번 화면 존재 확인
            Assert.IsTrue(order.gameObject.activeSelf); // 실제 행동 적의 순번 표시 확인
            Assert.AreEqual(action.ActionOrder.ToString(), order.GetComponentInChildren<TMP_Text>().text); // 실제 행동 순서와 표시 숫자 일치 확인
            Canvas orderLayer = order.GetComponent<Canvas>(); // 행동 순번의 독립 표시 순서 조회
            Assert.IsTrue(orderLayer.overrideSorting); // 행동 순번 독립 표시 순서 확인
            Assert.Greater(orderLayer.sortingOrder, arrows.GetComponent<Canvas>().sortingOrder); // 화살표가 행동 순번을 가리지 않는지 확인
            (order as RectTransform).GetWorldCorners(corners); // 적 행동 순번 화면 꼭짓점 조회
            float orderBottom = canvas.InverseTransformPoint(corners[0]).y; // 행동 순번 아래쪽 높이 계산
            RectTransform actorPortrait = actor.transform.Find("Portrait") as RectTransform; // 적 캐릭터 그림 영역 조회
            actorPortrait.GetWorldCorners(corners); // 적 그림 화면 꼭짓점 조회
            Assert.Greater(orderBottom, canvas.InverseTransformPoint(corners[1]).y); // 행동 순번이 적 머리 위에 표시되는지 확인
            RectTransform actorIcon = actor.transform.Find("Day47IntentIcon") as RectTransform; // 실제 적 행동 예고 아이콘 조회
            Assert.IsNotNull(actorIcon); // 적 행동 예고 아이콘 존재 확인
            actorIcon.GetWorldCorners(corners); // 행동 예고 아이콘 화면 꼭짓점 조회
            Assert.Greater(orderBottom, canvas.InverseTransformPoint(corners[1]).y); // 행동 순번이 공격 예고 위에 위치하는지 확인
            Assert.Less(Mathf.Abs(actorIcon.anchoredPosition.x), 1f); // 적 머리 위 중앙에 공격 예고 배치 확인
            Assert.IsTrue(arrows.TryGetArrow(index, out Vector2 start, out Vector2 end)); // 실제 행동 대상 화살표 좌표 조회 확인
            Rect bounds = arrows.rectTransform.rect; // 전투 화면 화살표 영역 조회
            Assert.IsTrue(bounds.Contains(start)); // 화살표 시작점 화면 내부 확인
            Assert.IsTrue(bounds.Contains(end)); // 화살표 종료점 화면 내부 확인
            Assert.Greater(Vector2.Distance(start, end), 1f); // 실제 적과 대상 사이 화살표 길이 확인
            Vector3 iconWorld = actorIcon.TransformPoint(new Vector3(actorIcon.rect.xMin, actorIcon.rect.center.y, 0f)); // 행동 아이콘 왼쪽 가장자리 실제 좌표 계산
            Vector2 expectedStart = arrows.rectTransform.InverseTransformPoint(iconWorld); // 화살표 화면 기준 실제 행동 아이콘 위치 계산
            Assert.Less(Vector2.Distance(expectedStart, start), 1f); // 화살표 시작점과 실제 행동 아이콘 연결 확인
            Assert.IsTrue(arrows.TryGetCurvePoint(index, 0.5f, out Vector2 middle)); // 실제 곡선 화살표 중간점 조회
            Assert.Greater(middle.y, (start.y + end.y) * 0.5f + 1f); // 직선보다 위로 휘어진 포물선 확인
            float highest = float.MinValue; // 실제 곡선의 최고 높이 초기화
            float roundBottom = float.MinValue; // 라운드 패널 아래쪽 높이 초기화
            for (int sample = 0; sample <= 10; sample++) // 곡선 화살표 전체 위치 검사
            { // 곡선 높이 제한 검사 시작
                Assert.IsTrue(arrows.TryGetCurvePoint(index, sample / 10f, out Vector2 point)); // 실제 곡선 위치 조회
                RectTransform round = canvas.Find("TurnStatusFrame") as RectTransform; // 실제 상단 라운드 패널 영역 조회
                round.GetWorldCorners(corners); // 라운드 패널 화면 꼭짓점 조회
                roundBottom = arrows.rectTransform.InverseTransformPoint(corners[0]).y; // 화살표 화면 기준 라운드 패널 아래쪽 높이
                highest = Mathf.Max(highest, point.y); // 곡선에서 가장 높은 위치 저장
                Assert.Less(point.y, roundBottom); // 곡선 전체가 라운드 패널 아래에 있는지 확인
            } // 곡선 높이 제한 검사 종료
            if (index == 0) // 첫 번째 곡선 화살표 확인
            { // 라운드 패널 부근 곡선 검사 시작
                Assert.Greater(highest, roundBottom - 35f); // 곡선 최고점이 라운드 패널 바로 아래에 위치하는지 확인
            } // 라운드 패널 부근 곡선 검사 종료
        } // 적 행동과 화살표 검사 종료
        Button arrowButton = null; // 실제 화살표 표시 방식 버튼 초기화
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None)) // 전투 화면 버튼 순회
        { // 화살표 버튼 탐색 시작
            if (button.name == "Day47ArrowModeButton") // 기존 화살표 표시 방식 버튼 확인
            { // 화살표 버튼 저장 시작
                arrowButton = button; // 실제 화살표 표시 방식 버튼 저장
                break; // 화살표 버튼 탐색 종료
            } // 화살표 버튼 저장 종료
        } // 화살표 버튼 탐색 종료
        Assert.IsNotNull(arrowButton); // 실제 화살표 표시 방식 버튼 존재 확인
        arrowButton.onClick.Invoke(); // 하나씩 표시 방식 선택
        yield return new WaitForSecondsRealtime(0.1f); // 실제 표시 방식 화면 갱신 대기
        Assert.AreEqual(1, arrows.ArrowCount); // 하나씩 표시 시 화살표 하나만 표시 확인
        arrowButton.onClick.Invoke(); // 전체 화살표 표시 방식 복원
        yield return new WaitForSecondsRealtime(0.1f); // 전체 화살표 화면 갱신 대기
        Assert.AreEqual(battle.EnemyActionRuntime.PlannedActions.Count, arrows.ArrowCount); // 전체 표시 시 모든 행동 화살표 복원 확인
        BattleEnemyAction redirected = battle.EnemyActionRuntime.PlannedActions[0]; // 첫 번째 예정 행동 조회
        BattleUnitRuntime replacement = redirected.Target == battle.AllyUnits[0] ? battle.AllyUnits[1] : battle.AllyUnits[0]; // 기존 대상과 다른 아군 선택
        Assert.IsTrue(redirected.ChangeTarget(replacement)); // 실제 예정 행동의 대상 변경 확인
        yield return new WaitForSecondsRealtime(0.1f); // 변경된 대상 화살표 갱신 대기
        BattleUnitView replacementView = null; // 변경된 대상 화면 초기화
        foreach (BattleUnitView unit in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 실제 전투 유닛 화면 순회
        { // 변경된 대상 화면 탐색 시작
            if (unit.RuntimeUnit == replacement) // 변경된 대상 아군 확인
            { // 변경된 대상 화면 저장 시작
                replacementView = unit; // 실제 변경 대상 화면 저장
                break; // 변경 대상 화면 탐색 종료
            } // 변경된 대상 화면 저장 종료
        } // 변경된 대상 화면 탐색 종료
        Assert.IsNotNull(replacementView); // 변경된 대상 화면 존재 확인
        RectTransform replacementPortrait = replacementView.transform.Find("Portrait") as RectTransform; // 변경된 대상 그림 영역 조회
        Vector3 expectedWorld = replacementPortrait.TransformPoint(new Vector3(0f, replacementPortrait.rect.yMax - replacementPortrait.rect.height * 0.12f, 0f)); // 변경된 대상 머리 위치 계산
        Vector2 expectedEnd = arrows.rectTransform.InverseTransformPoint(expectedWorld); // 화살표 화면 기준 대상 위치 계산
        Assert.IsTrue(arrows.TryGetArrow(0, out Vector2 ignoredStart, out Vector2 changedEnd)); // 변경된 첫 행동 화살표 조회
        Assert.Less(Vector2.Distance(expectedEnd, changedEnd), 1f); // 변경된 실제 대상에 화살표 연결 확인
        Assert.IsTrue(battle.EnemyActionRuntime.UnregisterEnemy(summoned)); // 소환 적 예정 행동 제거 확인
        yield return new WaitForSecondsRealtime(0.1f); // 실제 게임 프레임의 행동 제거 화면 갱신 대기
        Assert.IsFalse(summonedView.transform.Find("EnemyActionOrder").gameObject.activeSelf); // 행동이 없는 적의 순번 숨김 확인
        Assert.AreEqual(battle.EnemyActionRuntime.PlannedActions.Count, arrows.ArrowCount); // 남은 행동에 맞춘 화살표 갱신 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 전투 배치 검사 종료
} // 전투 배치 검사 종료
