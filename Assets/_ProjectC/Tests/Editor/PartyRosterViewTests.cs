using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
using UnityEngine.UI; // 버튼 컴포넌트 사용
public sealed class PartyRosterViewTests // 거점 파티 목록 화면 테스트
{ // 테스트 클래스 시작
    private string progressKey; // 테스트 전용 진행 상태 키
    private CharacterUnlockProgress progress; // 테스트 전용 해금 상태
    [SetUp] // 테스트 초기화 지정
    public void SetUp() // 격리된 진행 상태 생성
    { // 초기화 시작
        progressKey = "ProjectC.Test.Unlock." + System.Guid.NewGuid().ToString("N"); // 고유 저장 키 생성
        progress = new CharacterUnlockProgress(progressKey); // 테스트 전용 저장소 연결
    } // 초기화 종료
    [TearDown] // 테스트 정리 지정
    public void TearDown() // 테스트 진행 상태 삭제
    { // 정리 시작
        PlayerPrefs.DeleteKey(progressKey); // 테스트 전용 키 삭제
        PlayerPrefs.Save(); // 테스트 키 삭제 저장
    } // 정리 종료
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 상태 제공자 종료
    [Test] // 테스트 지정
    public void WorldUnlock_PersistsAndAllowsPartySelection() // 세계 해금 저장과 편성 허용 확인
    { // 테스트 시작
        GameObject root = new GameObject("WorldUnlockTest", typeof(RectTransform), typeof(Canvas)); // 테스트 화면 생성
        PartyRosterView view = root.AddComponent<PartyRosterView>(); // 파티 화면 추가
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        CharacterData world = roster.Characters[21]; // 세계 캐릭터 조회
        view.Initialize(roster, new AvailableStatus(), progress); // 격리된 상태로 화면 초기화
        Button worldButton = root.GetComponentsInChildren<Button>()[21]; // 세계 선택 버튼 조회
        Assert.IsFalse(worldButton.interactable); // 해금 전 버튼 차단 확인
        Assert.IsFalse(view.Selection.TryAdd(world)); // 해금 전 직접 편성 차단 확인
        progress.RecordFinalClear(); // 최종 탐사 성공 기록
        CharacterUnlockProgress reloaded = new CharacterUnlockProgress(progressKey); // 저장된 진행 상태 재조회
        Assert.IsTrue(reloaded.IsUnlocked(world)); // 재생성 후 세계 해금 유지 확인
        view.Refresh(); // 해금 상태 화면 갱신
        Assert.IsTrue(worldButton.interactable); // 해금 후 버튼 허용 확인
        worldButton.onClick.Invoke(); // 세계 캐릭터 선택
        Assert.AreSame(world, view.Selection.Members[0]); // 세계 편성 결과 확인
        Assert.IsTrue(view.Selection.Validate().CanDeploy); // 세계 출전 허용 확인
        Object.DestroyImmediate(root); // 테스트 화면 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void RosterView_ShowsPortraitsWithoutBlockingButtonInput() // 초상화 표시와 버튼 입력 확인
    { // 테스트 시작
        GameObject root = new GameObject("PortraitViewTest", typeof(RectTransform), typeof(Canvas)); // 테스트 화면 생성
        PartyRosterView view = root.AddComponent<PartyRosterView>(); // 파티 화면 추가
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        view.Initialize(roster, new AvailableStatus(), progress); // 목록 화면 초기화
        Button[] buttons = root.GetComponentsInChildren<Button>(); // 생성된 버튼 목록 조회
        for (int index = 0; index < roster.Characters.Count; index++) // 전체 캐릭터 순회
        { // 초상화 검증 시작
            Transform portraitObject = buttons[index].transform.Find("Portrait"); // 초상화 화면 조회
            Assert.IsNotNull(portraitObject, roster.Characters[index].CharacterId); // 초상화 화면 존재 확인
            Image portrait = portraitObject.GetComponent<Image>(); // 초상화 이미지 조회
            Assert.IsNotNull(portrait); // 이미지 컴포넌트 확인
            Assert.AreSame(roster.Characters[index].Portrait, portrait.sprite); // 캐릭터 초상화 연결 확인
            Assert.IsFalse(portrait.raycastTarget); // 버튼 입력 방해 방지 확인
            Assert.IsTrue(portrait.preserveAspect); // 초상화 비율 유지 확인
        } // 초상화 검증 종료
        Object.DestroyImmediate(root); // 테스트 화면 정리
    } // 테스트 종료
[Test] // 테스트 지정
    public void RosterView_ShowsCharactersAndAllowsSelection() // 캐릭터 목록과 선택 확인
    { // 테스트 시작
        GameObject root = new GameObject("PartyViewTest", typeof(RectTransform), typeof(Canvas)); // 테스트 화면 생성
        PartyRosterView view = root.AddComponent<PartyRosterView>(); // 파티 화면 추가
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        view.Initialize(roster, new AvailableStatus(), progress); // 목록 화면 초기화
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
