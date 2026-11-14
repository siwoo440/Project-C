using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 리소스 조회 기능 사용
public sealed class CharacterRosterAssetTests // 캐릭터 목록 에셋 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void RosterResource_LoadsAllArcanaInOrder() // 전체 아르카나 목록 조회 확인
    { // 테스트 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 공용 캐릭터 목록 조회
        Assert.IsNotNull(roster); // 목록 리소스 존재 확인
        Assert.AreEqual(22, roster.Characters.Count); // 전체 캐릭터 수 확인
        for (int index = 0; index < roster.Characters.Count; index++) // 캐릭터 목록 순회
        { // 개별 캐릭터 확인 시작
            Assert.IsNotNull(roster.Characters[index]); // 캐릭터 에셋 참조 확인
            Assert.AreEqual($"ARC_{index:00}", roster.Characters[index].CharacterId); // 캐릭터 순서와 식별자 확인
        } // 개별 캐릭터 확인 종료
    } // 테스트 종료
} // 테스트 클래스 종료
