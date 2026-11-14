using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
using UnityEngine.UI; // 버튼 컴포넌트 사용
public sealed class PartyRosterViewTests // 거점 파티 목록 화면 테스트
{ // 테스트 클래스 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 상태 제공자 종료
    [Test] // 테스트 지정
    public void RosterView_ShowsCharactersAndAllowsSelection() // 캐릭터 목록과 선택 확인
    { // 테스트 시작
        GameObject root = new GameObject("PartyViewTest", typeof(RectTransform), typeof(Canvas)); // 테스트 화면 생성
        PartyRosterView view = root.AddComponent<PartyRosterView>(); // 파티 화면 추가
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        view.Initialize(roster, new AvailableStatus()); // 목록 화면 초기화
        Button[] buttons = root.GetComponentsInChildren<Button>(); // 생성된 선택 버튼 조회
        Assert.AreEqual(23, buttons.Length); // 전체 캐릭터와 출발 버튼 확인
        Assert.IsFalse(buttons[22].interactable); // 빈 파티 출발 차단 확인
        buttons[0].onClick.Invoke(); // 첫 캐릭터 선택
        PartySelection selection = view.Selection; // 현재 파티 선택 조회
        Assert.AreEqual(1, selection.Members.Count); // 선택 캐릭터 수 확인
        Assert.AreEqual("ARC_00", selection.Members[0].CharacterId); // 선택 캐릭터 확인
        Assert.IsTrue(buttons[22].interactable); // 유효한 파티 출발 허용 확인
        Assert.IsFalse(buttons[21].interactable); // 세계 해금 전 선택 차단 확인
        buttons[0].onClick.Invoke(); // 첫 캐릭터 선택 해제
        Assert.AreEqual(0, selection.Members.Count); // 선택 해제 결과 확인
        Object.DestroyImmediate(root); // 테스트 화면 정리
    } // 테스트 종료
} // 테스트 클래스 종료
