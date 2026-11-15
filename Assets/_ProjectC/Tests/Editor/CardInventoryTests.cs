using System.Collections.Generic; // 고유 카드 식별자 집합 사용
using NUnit.Framework; // 시작 덱과 저장 원본 검증 사용
using UnityEngine; // 실제 캐릭터와 카드 리소스 사용
public sealed class CardInventoryTests // 전체 캐릭터 전용 카드 연결 검사
{ // 카드 연결 검사 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 독립 출전 가능 상태 제공자
    { // 출전 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 출전 상태 제공자 종료
    [Test] // 전체 캐릭터 시작 카드와 저장 연결 검사 지정
    public void EveryCharacter_StartsWithUniqueExclusiveCardInSaveCatalog() // 스물두 명의 전용 카드와 소유자 확인
    { // 전용 카드 연결 검사 시작
        string key = "ProjectC.Test.CardInventory." + System.Guid.NewGuid().ToString("N"); // 독립 세계 해금 저장 키 생성
        try // 독립 진행 상태와 편성 정리 보장
        { // 실제 카드 연결 검사 시작
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
            SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장 원본 목록 조회
            Assert.IsNotNull(roster); // 캐릭터 목록 존재 확인
            Assert.IsNotNull(catalog); // 저장 원본 목록 존재 확인
            Assert.AreEqual(22, roster.Characters.Count); // 전체 캐릭터 스물두 명 확인
            CharacterUnlockProgress progress = new CharacterUnlockProgress(key); // 독립 세계 해금 상태 생성
            progress.RecordFinalClear(); // 독립 최종 탐사 승리 기록
            HashSet<string> exclusiveIds = new HashSet<string>(); // 전용 카드 식별자 중복 검사 집합 생성
            foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
            { // 전용 카드 연결 확인 시작
                PartySelection selection = new PartySelection(new AvailableStatus(), progress); // 독립 캐릭터 편성 생성
                Assert.IsTrue(selection.TryAdd(character), character.CharacterId); // 해당 캐릭터 편성 확인
                Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection), character.CharacterId); // 실제 시작 덱 생성 확인
                DeckData deck = PartyLoadoutRuntime.Active.Deck; // 생성된 시작 덱 조회
                Assert.AreEqual(12, deck.Cards.Count, character.CharacterId); // 시작 덱 열두 장 확인
                DeckCardEntry first = deck.Cards[0]; // 첫 번째 시작 카드 조회
                Assert.AreSame(character, first.Owner, character.CharacterId); // 전용 카드 소유자 일치 확인
                StringAssert.StartsWith("CRD_" + character.CharacterId + "_", first.Card.CardId); // 소유자에 맞는 전용 카드 식별자 확인
                Assert.IsTrue(exclusiveIds.Add(first.Card.CardId), first.Card.CardId); // 전용 카드 식별자 중복 없음 확인
                CollectionAssert.Contains(catalog.Cards, first.Card, first.Card.CardId); // 전용 카드 저장 원본 등록 확인
                foreach (DeckCardEntry entry in deck.Cards) // 시작 덱의 모든 카드 순회
                { // 카드 소유자 확인 시작
                    Assert.AreSame(character, entry.Owner, character.CharacterId); // 단일 출전 캐릭터의 카드 소유 확인
                } // 카드 소유자 확인 종료
                PartyLoadoutRuntime.Clear(); // 해당 캐릭터의 임시 편성 정리
            } // 전용 카드 연결 확인 종료
            Assert.AreEqual(22, exclusiveIds.Count); // 서로 다른 전용 카드 스물두 종 확인
        } // 실제 카드 연결 검사 종료
        finally // 테스트 상태 정리
        { // 독립 상태 정리 시작
            PartyLoadoutRuntime.Clear(); // 남은 임시 편성 정리
            PlayerPrefs.DeleteKey(key); // 독립 해금 상태 제거
            PlayerPrefs.Save(); // 독립 해금 상태 제거 저장
        } // 독립 상태 정리 종료
    } // 전용 카드 연결 검사 종료
} // 카드 연결 검사 종료
