using System.Collections.Generic; // 고유 식別자 확인
using NUnit.Framework; // 테스트 기능 사용
using UnityEditor; // 에셋 조회 기능 사용
public sealed class CharacterCatalogAssetTests // 캐릭터 에셋 검증
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void ArcanaAssets_AllHaveUniqueIdsAndValidStats() // 전체 아르카나 데이터 확인
    { // 테스트 시작
        HashSet<string> identifiers = new HashSet<string>(); // 식별자 중복 확인 목록
        for (int index = 0; index <= 21; index++) // 전체 아르카나 순회
        { // 개별 에셋 확인 시작
            string identifier = $"ARC_{index:00}"; // 기대 식별자 생성
            string path = $"Assets/_ProjectC/ScriptableObjects/Characters/{identifier}.asset"; // 에셋 경로 생성
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 캐릭터 에셋 조회
            Assert.IsNotNull(character, path); // 에셋 불러오기 확인
            Assert.AreEqual(identifier, character.CharacterId); // 식별자 일치 확인
            Assert.IsTrue(identifiers.Add(character.CharacterId)); // 식별자 중복 방지 확인
            Assert.IsFalse(string.IsNullOrWhiteSpace(character.DisplayName)); // 캐릭터 이름 확인
            Assert.Greater(character.MaxHealth, 0); // 양수 체력 확인
            Assert.That(character.InitialMental, Is.InRange(0, 100)); // 정신력 범위 확인
        } // 개별 에셋 확인 종료
        Assert.AreEqual(22, identifiers.Count); // 전체 캐릭터 수 확인
    } // 테스트 종료
    [TestCase("ARC_00")] // 바보 초상화 검사 지정
    [TestCase("ARC_02")] // 여사제 초상화 검사 지정
    [TestCase("ARC_04")] // 황제 초상화 검사 지정
    [TestCase("ARC_07")] // 전차 초상화 검사 지정
    public void PrototypeParty_HasPortrait(string identifier) // 시제품 파티 초상화 연결 확인
    { // 테스트 시작
        string path = $"Assets/_ProjectC/ScriptableObjects/Characters/{identifier}.asset"; // 캐릭터 에셋 경로 생성
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path); // 캐릭터 에셋 조회
        Assert.IsNotNull(character); // 캐릭터 에셋 존재 확인
        Assert.IsNotNull(character.Portrait); // 초상화 스프라이트 연결 확인
    } // 테스트 종료
} // 테스트 클래스 종료
