using System.Collections; // 코루틴 테스트 사용
using NUnit.Framework; // 테스트 검증 기능 사용
using UnityEngine; // 유니티 객체 조회 사용
using UnityEngine.SceneManagement; // 씬 상태 조회 사용
using UnityEngine.TestTools; // 플레이 모드 전환 테스트 사용
using UnityEngine.UI; // 출발 버튼 조회 사용
public sealed class PartyDeparturePlayModeTests // 파티 출발 실제 씬 흐름 테스트
{ // 테스트 클래스 시작
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator SelectedParty_ReachesExplorationAndBattle() // 선택 파티의 탐사와 전투 진입 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "10_MainMenu"; frame++) // 메인 메뉴 전환 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Assert.AreEqual("10_MainMenu", SceneManager.GetActiveScene().name); // 메인 메뉴 진입 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 이동 요청
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<PartyRosterView>() == null; frame++) // 편성 화면 생성 대기
        { // 편성 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 편성 화면 대기 종료
        PartyRosterView view = Object.FindFirstObjectByType<PartyRosterView>(); // 거점 편성 화면 조회
        Assert.IsNotNull(view); // 편성 화면 생성 확인
        Button[] buttons = view.GetComponentsInChildren<Button>(); // 캐릭터와 출발 버튼 조회
        buttons[0].onClick.Invoke(); // 첫 캐릭터 선택
        Assert.IsTrue(buttons[22].interactable); // 유효한 파티 출발 가능 확인
        buttons[22].onClick.Invoke(); // 탐사 출발 요청
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "30_Exploration"; frame++) // 탐사 씬 전환 대기
        { // 탐사 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 대기 종료
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 탐사 진입 확인
        yield return null; // 탐사 초기화 대기
        ExplorationPartyLoadoutProvider provider = Object.FindFirstObjectByType<ExplorationPartyLoadoutProvider>(); // 탐사 편성 제공자 조회
        Assert.IsNotNull(provider); // 탐사 편성 제공자 존재 확인
        Assert.AreEqual("ARC_00", provider.BattleLoadout.Party.Members[0].CharacterId); // 선택 캐릭터 전달 확인
        SceneFlowManager.Instance.LoadScene("40_Battle"); // 전투 씬 직접 진입
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<BattleSceneSetup>() == null; frame++) // 전투 씬 전환 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 전투 관리자 조회
        Assert.IsNotNull(battle); // 전투 관리자 존재 확인
        Assert.IsTrue(battle.IsInitialized); // 전투 초기화 완료 확인
        Assert.AreEqual("ARC_00", battle.AllyUnits[0].CharacterSource.CharacterId); // 선택 캐릭터 전투 출전 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
} // 테스트 클래스 종료
