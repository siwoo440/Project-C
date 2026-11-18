using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 전투 배치 검증 사용
using UnityEngine; // 화면 좌표와 전투 객체 사용
using UnityEngine.SceneManagement; // 실제 게임 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
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
                Assert.LessOrEqual(canvas.InverseTransformPoint(corners[1]).y, battlefieldTop + 1f); // 행동 아이콘의 배경 영역 포함 확인
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
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 전투 배치 검사 종료
} // 전투 배치 검사 종료
