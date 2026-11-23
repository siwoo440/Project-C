using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 테스트 객체 사용
public sealed class PartyLoadoutFactoryTests // 런타임 전투 편성 생성 테스트
{ // 테스트 클래스 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 테스트 출전 상태 제공자
    { // 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 상태 제공자 종료
    [Test] // 테스트 지정
    public void Factory_IsAvailableForSelectedParty() // 선택 파티 편성 생성 기능 확인
    { // 테스트 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 캐릭터 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 임시 카드 생성
        PartySelection selection = new PartySelection(new AvailableStatus()); // 선택 상태 생성
        Assert.IsTrue(selection.TryAdd(character)); // 캐릭터 선택 확인
        CardData[] roleCards = { card, card, card, card }; // 역할별 카드 목록 생성
        BattleLoadoutData loadout = PartyLoadoutFactory.Create(selection, roleCards); // 선택 파티 전투 편성 생성
        Assert.IsNotNull(loadout); // 전투 편성 생성 확인
        Assert.IsTrue(loadout.IsValidLoadout()); // 생성 편성 유효성 확인
        Assert.AreEqual(12, loadout.Deck.CardCount); // 임시 시작 덱 열두 장 확인
        Assert.AreSame(character, loadout.Party.Members[0]); // 선택 캐릭터 전달 확인
        Assert.AreSame(character, loadout.Deck.Cards[0].Owner); // 카드 소유자 연결 확인
        selection.TryRemove(character); // 원본 선택 상태 변경
        Assert.AreEqual(1, loadout.Party.MemberCount); // 생성 편성 독립 상태 확인
        Object.DestroyImmediate(loadout.Party); // 임시 파티 정리
        Object.DestroyImmediate(loadout.Deck); // 임시 덱 정리
        Object.DestroyImmediate(loadout); // 임시 편성 정리
        Object.DestroyImmediate(card); // 테스트 카드 정리
        Object.DestroyImmediate(character); // 테스트 캐릭터 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void Factory_RejectsEmptyPartyAndMissingRoleCard() // 빈 파티와 누락 카드 차단 확인
    { // 테스트 시작
        PartySelection selection = new PartySelection(new AvailableStatus()); // 빈 선택 상태 생성
        CardData[] roleCards = new CardData[4]; // 카드 누락 역할 목록 생성
        Assert.IsNull(PartyLoadoutFactory.Create(selection, roleCards)); // 빈 파티 편성 차단 확인
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 캐릭터 생성
        Assert.IsTrue(selection.TryAdd(character)); // 캐릭터 선택 확인
        Assert.IsNull(PartyLoadoutFactory.Create(selection, roleCards)); // 역할 카드 누락 차단 확인
        Object.DestroyImmediate(character); // 테스트 캐릭터 정리
    } // 테스트 종료
} // 테스트 클래스 종료
