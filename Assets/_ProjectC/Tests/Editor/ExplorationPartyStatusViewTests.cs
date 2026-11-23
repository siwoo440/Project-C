using System.Collections; // 실제 Unity 플레이 모드 진입 대기
using System.Linq; // 편성 인원만큼 원본 캐릭터 선택
using NUnit.Framework; // 파티 HUD 회귀 검사 사용
using TMPro; // 표시된 실제 수치와 이름 조회
using UnityEngine; // 독립 시험 객체와 자원 생성
using UnityEngine.SceneManagement; // 기존 활성 씬 조회와 복구
using UnityEngine.TestTools; // 원래 편집기 씬을 보존한 플레이 모드 검사
using UnityEngine.UI; // 파티 피부와 게이지 이미지 조회

public sealed class ExplorationPartyStatusViewTests // 탐사 파티 게이지의 저장 상태 연동 검사
{ // 파티 HUD 검사 시작
    [UnityTest] // 네 명의 프로필과 저장 상태 갱신 검사 등록
    public IEnumerator PartyProfiles_RenderAndRefreshActualHealthAndMental() // 실제 저장 수치, 채움 비율, 죽음 표시 확인
    { // 독립 플레이 모드 검증 시작
        yield return new EnterPlayMode(); // 편집기의 저장되지 않은 장면을 건드리지 않는 실제 실행
        VerifyProfiles(); // 현재 게임 상태와 게이지의 수치 비교
        yield return new ExitPlayMode(); // 시험 모드와 임시 장면 종료
    } // 독립 플레이 모드 검증 종료

    private static void VerifyProfiles() // 독립 실행 장면에 실제 프로필과 상태 변화 연결
    { // 프로필과 수치 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 원본 목록 조회
        Assert.IsNotNull(roster); // 파티 원본 캐릭터 목록 확인
        CharacterData[] members = roster.Characters.Take(PartyData.MaxPartySize).ToArray(); // 출전 한도만큼 원본 캐릭터 선택
        Assert.AreEqual(PartyData.MaxPartySize, members.Length); // 네 명의 실제 파티원 확인
        PartyData party = PartyData.CreateRuntime(members); // 임시 출전 파티 생성
        Scene previousScene = SceneManager.GetActiveScene(); // 시험 전에 열린 활성 씬 기록
        Scene testScene = SceneManager.CreateScene("ExplorationPartyProfilesTest"); // 기존 씬을 보존하는 격리 공간 생성
        GameObject stateObject = null; // 독립 상태 객체의 정리 참조 준비
        GameObject host = null; // 독립 파티 화면의 정리 참조 준비

        try // 시험 객체 정리 보장
        { // 출전 파티 화면 검사 시작
            SceneManager.SetActiveScene(testScene); // 전투 출전 필터가 없는 탐사 조건 활성화
            stateObject = new GameObject("PartyProfileTestState"); // 전역과 별개의 데이터 객체 생성
            stateObject.SetActive(false); // 시험 중 전역 결과 관리자 등록 차단
            BattleResultManager result = stateObject.AddComponent<BattleResultManager>(); // 실제 저장 상태 로직 연결
            host = new GameObject("PartyProfileTestView", typeof(ExplorationPartyStatusView)); // 독립 탐사 파티 화면 생성
            Assert.IsTrue(result.RegisterParty(party)); // 정상 출전 편성 등록 확인
            CharacterData actor = members[0]; // 수치를 갱신할 첫 출전 멤버
            int health = actor.MaxHealth / 2; // 체력 게이지 확인용 중간 수치
            Assert.Greater(health, 0); // 초기에 살아 있는 멤버 확인
            result.RestoreSavedAllyState(actor, health, 63, 0); // 실제 저장 체력과 정신력 준비
            host.GetComponent<ExplorationPartyStatusView>().Configure(result, null); // 상태 관리자와 실제 파티 HUD 연결

            Transform root = host.transform.Find("ExplorationPartyStatusCanvas/PartyRoot"); // 실제 탐사 좌하단 프로필 영역 조회
            Assert.AreEqual(PartyData.MaxPartySize, root.childCount); // 최대 네 명의 프로필 표시 확인
            for (int index = 0; index < members.Length; index++) // 출전 순서대로 프로필 구성 확인
            { // 개별 파티 프로필 검사 시작
                Transform profile = root.GetChild(index); // 출전 순서의 파티 프로필 조회
                Assert.IsNotNull(profile.GetComponent<Image>().sprite); // 금속 테두리 프로필 피부 확인
                Assert.AreEqual(members[index].Portrait, profile.Find("Portrait").GetComponent<Image>().sprite); // 기존 초상화 유지 확인
                Assert.AreEqual(members[index].DisplayName, profile.Find("Name").GetComponent<TMP_Text>().text); // 원본 이름 유지 확인
                Assert.IsNotNull(profile.Find("Health/Icon").GetComponent<Image>().sprite); // 체력 아이콘 연결 확인
                Assert.IsNotNull(profile.Find("Mental/Icon").GetComponent<Image>().sprite); // 정신력 아이콘 연결 확인
            } // 개별 파티 프로필 검사 종료

            Transform target = root.GetChild(0); // 상태가 바뀌는 첫 번째 프로필 조회
            AssertGauge(target.Find("Health"), health, actor.MaxHealth); // 캐릭터 최대 체력 기준의 수치와 채움 확인
            AssertGauge(target.Find("Mental"), 63, BattleMentalRuntime.MaximumMental); // 게임 최대 정신력 기준의 수치와 채움 확인
            result.RestoreSavedAllyState(actor, 0, BattleMentalRuntime.MaximumMental, 1); // 사망 체력과 가득 찬 정신력 저장
            AssertGauge(target.Find("Health"), 0, actor.MaxHealth); // 상태 변경 이벤트에 따른 빈 체력 확인
            AssertGauge(target.Find("Mental"), BattleMentalRuntime.MaximumMental, BattleMentalRuntime.MaximumMental); // 최대 정신력의 끝까지 채워지는 게이지 확인
            Assert.IsTrue(target.Find("DeathShade").gameObject.activeSelf); // 기존 사망 오버레이 표시 확인
            Assert.IsTrue(target.Find("DeathText").gameObject.activeSelf); // 기존 사망 문구 표시 확인
        } // 출전 파티 화면 검사 종료
        finally // 시험 객체 제거
        { // 화면과 시험 파티 정리 시작
            Object.DestroyImmediate(host); // HUD와 상태 이벤트 연결 제거
            Object.DestroyImmediate(stateObject); // 독립 결과 관리자 제거
            Object.DestroyImmediate(party); // 임시 파티 데이터 제거
            SceneManager.SetActiveScene(previousScene); // 기존 편집기 활성 씬 복구
            SceneManager.UnloadSceneAsync(testScene); // 플레이 모드에서 만든 임시 시험 씬 제거
        } // 화면과 시험 파티 정리 종료
    } // 프로필과 수치 검사 종료

    private static void AssertGauge(Transform gauge, int current, int maximum) // 게이지 값과 채움 비율 검증
    { // 개별 게이지 검사 시작
        Assert.IsNotNull(gauge.Find("Frame").GetComponent<Image>().sprite); // 공통 투명 금속 프레임 확인
        Assert.AreEqual($"{current}/{maximum}", gauge.Find("Value").GetComponent<TMP_Text>().text); // 현재와 최대의 실제 표시 확인
        RectTransform fill = (RectTransform)gauge.Find("Track/Fill"); // 실제 채움 사각형 조회
        Assert.AreEqual((float)current / maximum, fill.anchorMax.x, 0.001f); // 값 비율에 따른 가로 채움 확인
        Assert.IsFalse(fill.GetComponent<Image>().raycastTarget); // 탐사 화면의 기존 클릭 입력 보존 확인
    } // 개별 게이지 검사 종료
} // 파티 HUD 검사 종료
