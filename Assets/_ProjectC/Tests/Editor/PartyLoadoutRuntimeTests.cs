using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class PartyLoadoutRuntimeTests // 출발 편성 유지 테스트
{ // 테스트 클래스 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 상태 제공자 종료
    [Test] // 테스트 지정
    public void Prepare_StoresSelectedPartyAndCanClear() // 출발 편성 저장과 해제 확인
    { // 테스트 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 캐릭터 생성
        PartySelection selection = new PartySelection(new AvailableStatus()); // 테스트 선택 상태 생성
        Assert.IsTrue(selection.TryAdd(character)); // 캐릭터 선택 확인
        bool prepared = PartyLoadoutRuntime.Prepare(selection); // 출발 편성 준비
        Assert.IsTrue(prepared); // 편성 준비 성공 확인
        BattleLoadoutData active = PartyLoadoutRuntime.Active; // 현재 출발 편성 조회
        Assert.IsNotNull(active); // 현재 편성 존재 확인
        Assert.AreSame(character, active.Party.Members[0]); // 선택 캐릭터 유지 확인
        Assert.AreEqual(12, active.Deck.CardCount); // 임시 시작 카드 수 확인
        GameObject providerObject = new GameObject("ExplorationProviderTest"); // 탐사 편성 제공자 객체 생성
        ExplorationPartyLoadoutProvider provider = providerObject.AddComponent<ExplorationPartyLoadoutProvider>(); // 탐사 편성 제공자 연결
        Assert.AreSame(active, provider.BattleLoadout); // 탐사 화면의 선택 편성 조회 확인
        Object.DestroyImmediate(providerObject); // 테스트 제공자 정리
        PartyLoadoutRuntime.Clear(); // 출발 편성 정리
        Assert.IsNull(PartyLoadoutRuntime.Active); // 현재 편성 해제 확인
        Object.DestroyImmediate(character); // 테스트 캐릭터 정리
    } // 테스트 종료
} // 테스트 클래스 종료
