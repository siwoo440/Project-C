using NUnit.Framework; // 테스트 검증 사용
using UnityEngine; // 캐릭터 리소스 사용

public static class PartyDepartureTestSupport // 탐사 테스트용 파티 준비
{ // 테스트 파티 준비 시작
    public static void StartExploration(params int[] indexes) // 지정 캐릭터로 탐사 시작
    { // 탐사 테스트 준비 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 전체 캐릭터 목록 조회
        Assert.IsNotNull(roster); // 캐릭터 목록 존재 확인
        PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 출전 가능 상태 기준 선택 생성
        foreach (int index in indexes) // 테스트 캐릭터 번호 순회
        { // 테스트 캐릭터 선택 시작
            Assert.IsTrue(selection.TryAdd(roster.Characters[index])); // 지정 캐릭터 선택 확인
        } // 테스트 캐릭터 선택 종료
        Assert.IsTrue(selection.Validate().CanDeploy); // 테스트 파티 출전 가능 확인
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 테스트 파티와 덱 준비
        ExplorationSessionManager.EnsureInstance().ResetExploration(); // 새 탐사 상태 초기화
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 선택한 시작 덱 적용
        SceneFlowManager.Instance.LoadScene("30_Exploration"); // 탐사 씬 이동
    } // 탐사 테스트 준비 종료
} // 테스트 파티 준비 종료
