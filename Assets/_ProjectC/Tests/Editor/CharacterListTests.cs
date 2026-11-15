using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 캐릭터 화면 검증 사용
using TMPro; // 캐릭터 이름 조회 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 버튼과 목록 조회 사용

public sealed class CharacterListTests // 캐릭터 목록 화면 검증
{ // 캐릭터 목록 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator LobbyCharacterButton_OpensScrollableSixColumnRosterAndReturns() // 로비 목록 열기와 복귀 확인
    { // 캐릭터 목록 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 메인 메뉴 준비 대기
        { // 메뉴 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 준비 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 새 거점 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 화면 준비 대기
        { // 거점 준비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 준비 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 거점 화면 조회
        Assert.IsNotNull(lobby); // 거점 화면 존재 확인
        Button button = lobby.transform.Find("CharacterButton").GetComponent<Button>(); // 캐릭터 버튼 조회
        Assert.IsTrue(button.interactable); // 캐릭터 버튼 활성 확인
        button.onClick.Invoke(); // 캐릭터 목록 열기
        CharacterListView view = Object.FindFirstObjectByType<CharacterListView>(); // 캐릭터 목록 화면 조회
        Assert.IsNotNull(view); // 캐릭터 화면 표시 확인
        ScrollRect scroll = view.transform.Find("ListPanel").GetComponent<ScrollRect>(); // 목록 스크롤 조회
        Assert.IsTrue(scroll.vertical); // 세로 스크롤 허용 확인
        Assert.IsFalse(scroll.horizontal); // 가로 스크롤 차단 확인
        GridLayoutGroup grid = view.transform.Find("ListPanel/Viewport/Grid").GetComponent<GridLayoutGroup>(); // 캐릭터 격자 조회
        Assert.AreEqual(6, grid.constraintCount); // 여섯 열 배치 확인
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        Assert.AreEqual(roster.Characters.Count, grid.transform.childCount); // 전체 캐릭터 표시 확인
        Assert.AreEqual(roster.Characters[0].Portrait, grid.transform.Find("Character_0/Portrait").GetComponent<Image>().sprite); // 실제 캐릭터 초상화 확인
        Assert.AreEqual(roster.Characters[0].DisplayName, grid.transform.Find("Character_0/Name").GetComponent<TMP_Text>().text); // 실제 캐릭터 이름 확인
        Canvas.ForceUpdateCanvases(); // 스크롤 영역 배치 갱신
        float initialPosition = scroll.content.anchoredPosition.y; // 스크롤 시작 위치 보관
        scroll.verticalNormalizedPosition = 0f; // 목록 맨 아래로 이동
        Assert.Greater(scroll.content.anchoredPosition.y, initialPosition); // 긴 목록의 실제 스크롤 이동 확인
        CharacterData last = roster.Characters[roster.Characters.Count - 1]; // 마지막 캐릭터 조회
        Transform lastCard = grid.transform.Find("Character_" + (roster.Characters.Count - 1)); // 마지막 캐릭터 카드 조회
        Assert.AreEqual(!CharacterUnlockProgress.Shared.IsUnlocked(last), lastCard.Find("Locked") != null); // 실제 해금 상태 표시 확인
        view.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 로비 복귀 실행
        yield return null; // 목록 화면 제거 대기
        Assert.IsNull(Object.FindFirstObjectByType<CharacterListView>()); // 캐릭터 목록 닫힘 확인
        Assert.IsTrue(lobby.gameObject.activeInHierarchy); // 기존 로비 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 캐릭터 목록 검증 종료
} // 캐릭터 목록 테스트 종료
