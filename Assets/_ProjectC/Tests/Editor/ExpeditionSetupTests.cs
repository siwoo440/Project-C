using System.Collections; // 플레이 모드 코루틴 사용
using NUnit.Framework; // 탐사 준비 검증 사용
using TMPro; // 편성 이름 조회 사용
using UnityEngine; // 화면 객체 조회 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 탐사 준비 버튼 사용

public sealed class ExpeditionSetupTests // 맵 선택과 새 편성 흐름 검증
{ // 탐사 준비 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator MapSelection_OpensFormationAndStartsSelectedParty() // 맵 선택과 파티 출발 확인
    { // 탐사 준비 흐름 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬 시작
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<MainMenuController>() == null; frame++) // 메인 메뉴 준비 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 새 로비 진입
        for (int frame = 0; frame < 180 && Object.FindFirstObjectByType<LobbyLayoutView>() == null; frame++) // 로비 준비 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 로비 화면 조회
        Assert.IsNotNull(lobby); // 로비 화면 존재 확인
        Assert.IsNull(GameObject.Find("FormationSetup")); // 로비 진입 직후 편성 창 미표시 확인
        Button expedition = lobby.transform.Find("ExpeditionButton").GetComponent<Button>(); // 탐사 버튼 조회
        Assert.IsTrue(expedition.interactable); // 탐사 버튼 활성 확인
        expedition.onClick.Invoke(); // 맵 선택 화면 열기
        GameObject map = GameObject.Find("MapSelection"); // 맵 선택 화면 조회
        Assert.IsNotNull(map); // 맵 선택 화면 표시 확인
        map.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 맵 선택 취소 실행
        yield return null; // 맵 선택 화면 제거 대기
        Assert.IsNull(GameObject.Find("MapSelection")); // 로비 복귀 후 맵 화면 제거 확인
        Assert.IsNull(PartyLoadoutRuntime.Active); // 맵 선택 취소 후 파티 상태 유지 확인
        expedition.onClick.Invoke(); // 맵 선택 화면 다시 열기
        map = GameObject.Find("MapSelection"); // 다시 열린 맵 선택 화면 조회
        Button proceed = map.transform.Find("ConfirmButton").GetComponent<Button>(); // 맵 선택 확인 버튼 조회
        Assert.IsFalse(proceed.interactable); // 맵 선택 전 진행 차단 확인
        map.transform.Find("MapOption_0").GetComponent<Button>().onClick.Invoke(); // 기본 탐사 맵 선택
        Assert.IsTrue(proceed.interactable); // 맵 선택 후 진행 허용 확인
        proceed.onClick.Invoke(); // 선택 맵의 편성 화면 열기
        GameObject formation = GameObject.Find("FormationSetup"); // 새 편성 화면 조회
        Assert.IsNotNull(formation); // 새 편성 화면 존재 확인
        formation.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 맵 선택으로 돌아가기 실행
        yield return null; // 이전 편성 화면 제거 대기
        Assert.IsNull(GameObject.Find("FormationSetup")); // 이전 편성 화면 제거 확인
        map = GameObject.Find("MapSelection"); // 돌아간 맵 선택 화면 조회
        Assert.IsNotNull(map); // 맵 선택 화면 복귀 확인
        map.transform.Find("MapOption_0").GetComponent<Button>().onClick.Invoke(); // 기본 탐사 맵 재선택
        map.transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 편성 화면 다시 열기
        formation = GameObject.Find("FormationSetup"); // 다시 열린 편성 화면 조회
        Assert.IsNotNull(formation); // 편성 화면 재진입 확인
        Transform slots = formation.transform.Find("Slots"); // 가운데 편성 칸 조회
        for (int index = 0; index < 4; index++) // 네 개의 편성 칸 순회
        { // 편성 칸 검사 시작
            Assert.IsNotNull(slots.Find("Slot_" + index)); // 개별 편성 칸 존재 확인
            Assert.IsNotNull(slots.Find("Slot_" + index).GetComponent<Image>().sprite); // 제작된 편성 칸 이미지 확인
            Assert.IsNotNull(formation.transform.Find("Preset_" + index)); // 편성 선택 영역 존재 확인
        } // 편성 칸 검사 종료
        RectTransform detail = formation.transform.Find("DetailPanel").GetComponent<RectTransform>(); // 오른쪽 정보 영역 조회
        Assert.LessOrEqual(detail.anchorMax.x - detail.anchorMin.x, 0.19f); // 축소된 정보 영역 너비 확인
        Assert.IsNotNull(detail.GetComponent<Image>().sprite); // 제작된 상세 패널 이미지 확인
        Button start = formation.transform.Find("StartButton").GetComponent<Button>(); // 탐사 출발 버튼 조회
        Assert.IsFalse(start.interactable); // 빈 파티의 출발 차단 확인
        Transform grid = formation.transform.Find("CharacterList/Viewport/Grid"); // 왼쪽 캐릭터 목록 조회
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>(); // 프로필 격자 배치 조회
        Assert.Greater(layout.spacing.x, 7f); // 프로필 가로 간격 확대 확인
        Assert.Greater(layout.spacing.y, 7f); // 프로필 세로 간격 확대 확인
        Image border = grid.Find("Character_0/BorderTop").GetComponent<Image>(); // 첫 프로필 흰색 구분선 조회
        Assert.AreEqual(Color.white, border.color); // 프로필 흰색 구분선 색상 확인
        grid.Find("Character_0").GetComponent<Button>().onClick.Invoke(); // 첫 캐릭터 선택
        Assert.IsTrue(start.interactable); // 유효한 파티 출발 허용 확인
        Assert.AreEqual("바보", slots.Find("Slot_0/Name").GetComponent<TMP_Text>().text); // 첫 편성 칸 이름 확인
        for (int index = 1; index < 4; index++) // 나머지 편성 가능 칸 순회
        { // 캐릭터 선택 시작
            grid.Find("Character_" + index).GetComponent<Button>().onClick.Invoke(); // 각 캐릭터 선택
        } // 캐릭터 선택 종료
        Assert.IsFalse(grid.Find("Character_4").GetComponent<Button>().interactable); // 네 명 초과 선택 차단 확인
        StringAssert.Contains("4/4", formation.transform.Find("Message").GetComponent<TMP_Text>().text); // 현재 편성 인원 표시 확인
        StringAssert.DoesNotContain("임시 능력치", detail.Find("Description").GetComponent<TMP_Text>().text); // 개발용 설명 문구 미표시 확인
        start.onClick.Invoke(); // 선택 파티로 탐사 출발
        for (int frame = 0; frame < 180 && SceneManager.GetActiveScene().name != "30_Exploration"; frame++) // 탐사 씬 진입 대기
        { // 탐사 씬 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 씬 대기 종료
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 탐사 씬 진입 확인
        Assert.AreEqual(4, PartyLoadoutRuntime.Active.Party.Members.Count); // 선택한 네 명의 탐사 편성 확인
        Assert.AreEqual("ARC_00", PartyLoadoutRuntime.Active.Party.Members[0].CharacterId); // 첫 선택 캐릭터 유지 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 탐사 준비 흐름 검증 종료
} // 탐사 준비 테스트 종료
