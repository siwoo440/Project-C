using System.Collections.Generic; // 테스트 목록 사용
using NUnit.Framework; // Unity Editor 테스트 기능 사용
using UnityEngine; // 테스트 ScriptableObject 생성 사용

public sealed class BattleCombatRosterBuilderTests // 58일차 Part 3 실제 전투 출전 명단 테스트
{
    private sealed class FakeStatusProvider : IPartyDeploymentStatusProvider // 테스트용 출전 상태 제공자
    {
        private readonly HashSet<CharacterData> blockedCharacters = new HashSet<CharacterData>(); // 출전 차단 캐릭터 목록

        public void Block(CharacterData characterData)
        {
            blockedCharacters.Add(characterData); // 지정 캐릭터 출전 차단
        }

        public bool IsDead(CharacterData characterData)
        {
            return blockedCharacters.Contains(characterData); // 테스트 차단 대상을 사망 상태로 반환
        }

        public bool IsRecovering(CharacterData characterData)
        {
            return false; // 테스트 회복 상태 없음
        }

        public bool CanDeploy(CharacterData characterData)
        {
            return !blockedCharacters.Contains(characterData); // 차단되지 않은 캐릭터 출전 허용
        }
    }

    [Test]
    public void BuildMembers_RemovesBlockedMembersAndKeepsOrder() // 출전 불가 제외와 순서 유지 확인
    {
        CharacterData first = ScriptableObject.CreateInstance<CharacterData>(); // 첫 캐릭터 생성
        CharacterData second = ScriptableObject.CreateInstance<CharacterData>(); // 둘째 캐릭터 생성
        CharacterData third = ScriptableObject.CreateInstance<CharacterData>(); // 셋째 캐릭터 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 제공자 생성
        provider.Block(second); // 둘째 캐릭터 출전 차단

        List<CharacterData> result = BattleCombatRosterBuilder.BuildDeployableMembers(
            new[] { first, second, third },
            provider); // 실제 전투 출전 명단 생성

        Assert.AreEqual(2, result.Count); // 두 명 생존 출전 확인
        Assert.AreSame(first, result[0]); // 첫 캐릭터 순서 유지 확인
        Assert.AreSame(third, result[1]); // 셋째 캐릭터 순서 유지 확인

        Object.DestroyImmediate(first); // 테스트 캐릭터 정리
        Object.DestroyImmediate(second); // 테스트 캐릭터 정리
        Object.DestroyImmediate(third); // 테스트 캐릭터 정리
    }

    [Test]
    public void FilterCards_RemovesCardsOwnedByExcludedMembers() // 제외 캐릭터 소유 카드 제거 확인
    {
        CharacterData living = ScriptableObject.CreateInstance<CharacterData>(); // 생존 캐릭터 생성
        CharacterData dead = ScriptableObject.CreateInstance<CharacterData>(); // 사망 캐릭터 생성
        CardData livingCard = ScriptableObject.CreateInstance<CardData>(); // 생존 캐릭터 카드 생성
        CardData deadCard = ScriptableObject.CreateInstance<CardData>(); // 사망 캐릭터 카드 생성
        List<RunDeckCardEntry> entries = new List<RunDeckCardEntry>
        {
            new RunDeckCardEntry(livingCard, living),
            new RunDeckCardEntry(deadCard, dead),
            new RunDeckCardEntry(livingCard, living)
        }; // 소유자 혼합 회차 덱 생성

        List<RunDeckCardEntry> result = BattleCombatRosterBuilder.FilterRunDeckCards(
            entries,
            new[] { living }); // 생존 출전 캐릭터 카드만 필터

        Assert.AreEqual(2, result.Count); // 생존 캐릭터 카드 수 확인
        Assert.AreSame(entries[0], result[0]); // 첫 카드 순서 유지 확인
        Assert.AreSame(entries[2], result[1]); // 마지막 카드 순서 유지 확인

        Object.DestroyImmediate(living); // 테스트 캐릭터 정리
        Object.DestroyImmediate(dead); // 테스트 캐릭터 정리
        Object.DestroyImmediate(livingCard); // 테스트 카드 정리
        Object.DestroyImmediate(deadCard); // 테스트 카드 정리
    }

    [Test]
    public void FilterDeployableCards_UsesSameDeploymentRuleAsRoster() // 런타임 카드 출전 규칙 일치 확인
    {
        CharacterData living = ScriptableObject.CreateInstance<CharacterData>(); // 생존 캐릭터 생성
        CharacterData blocked = ScriptableObject.CreateInstance<CharacterData>(); // 출전 불가 캐릭터 생성
        CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 테스트 카드 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 제공자 생성
        provider.Block(blocked); // 지정 캐릭터 출전 차단
        List<RunDeckCardEntry> entries = new List<RunDeckCardEntry>
        {
            new RunDeckCardEntry(cardData, living),
            new RunDeckCardEntry(cardData, blocked)
        }; // 혼합 카드 목록 생성

        List<RunDeckCardEntry> result = BattleCombatRosterBuilder.FilterDeployableRunDeckCards(
            entries,
            provider); // 출전 가능 소유자 카드 필터

        Assert.AreEqual(1, result.Count); // 출전 카드 한 장 확인
        Assert.AreSame(living, result[0].Owner); // 생존 소유자 카드 확인

        Object.DestroyImmediate(living); // 테스트 캐릭터 정리
        Object.DestroyImmediate(blocked); // 테스트 캐릭터 정리
        Object.DestroyImmediate(cardData); // 테스트 카드 정리
    }

    [Test]
    public void BuildMembers_AllBlocked_ReturnsEmptyRoster() // 전원 출전 불가 빈 명단 확인
    {
        CharacterData first = ScriptableObject.CreateInstance<CharacterData>(); // 첫 캐릭터 생성
        CharacterData second = ScriptableObject.CreateInstance<CharacterData>(); // 둘째 캐릭터 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 제공자 생성
        provider.Block(first); // 첫 캐릭터 출전 차단
        provider.Block(second); // 둘째 캐릭터 출전 차단

        List<CharacterData> result = BattleCombatRosterBuilder.BuildDeployableMembers(
            new[] { first, second },
            provider); // 전원 차단 전투 명단 생성

        Assert.AreEqual(0, result.Count); // 빈 출전 명단 확인

        Object.DestroyImmediate(first); // 테스트 캐릭터 정리
        Object.DestroyImmediate(second); // 테스트 캐릭터 정리
    }
}
