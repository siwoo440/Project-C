using System.Collections; // 플레이 모드 대기 사용
using System.Reflection; // 배경 재연결 검사 사용
using NUnit.Framework; // 실제 전투 배경 검사 사용
using UnityEngine; // 전투 화면과 리소스 사용
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
        int beforeChildren = canvas.transform.childCount; // 전투 캔버스 자식 수 보관
        typeof(BattleSceneSetup).GetMethod("InstallBattleBackground", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, null); // 배경 연결 재실행
        battle.InitializeBattle(); // 전투 중복 초기화 시도
        Assert.AreEqual(beforeChildren, canvas.transform.childCount); // 전투 배경 중복 생성 방지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 배경 검사 종료
} // 배경 검사 종료
