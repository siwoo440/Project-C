using System.Collections.Generic; // 카드 목록 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 리소스 사용
public sealed class MagicianPassiveTests // 마법사 회수 패시브 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void FirstRecovery_DiscountsOneCardUntilTurnEnds() // 첫 회수 카드의 턴 한정 할인 확인
    { // 테스트 시작
        CharacterData magician = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[1]; // 마법사 원본 조회
        CardData card = Resources.Load<CardData>("StarterCards/Attack"); // 행동력 일 카드 원본 조회
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(magician); // 마법사 유닛 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, magician), new RunDeckCardEntry(card, magician), new RunDeckCardEntry(card, magician) }; // 카드 세 장 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { unit }, 5, 1); // 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { unit }, new BattleUnitRuntime[0], 0, BattleType.Normal); // 턴 관리자 생성
        MagicianPassiveRuntime passive = new MagicianPassiveRuntime(deck, turns, new[] { unit }); // 마법사 패시브 연결
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        deck.DrawCards(2); // 카드 두 장 드로우
        CardInstance first = deck.Hand[0]; // 첫 회수 카드 저장
        CardInstance second = deck.Hand[1]; // 두 번째 회수 카드 저장
        deck.DiscardCard(first); // 첫 카드 버리기
        deck.DiscardCard(second); // 두 번째 카드 버리기
        Assert.IsTrue(deck.TryRecoverDiscard(first)); // 첫 카드 회수 확인
        Assert.AreEqual(0, first.ApCost); // 첫 회수 카드 행동력 감소 확인
        Assert.IsTrue(deck.TryRecoverDiscard(second)); // 두 번째 카드 회수 확인
        Assert.AreEqual(1, second.ApCost); // 같은 턴 두 번째 회수 할인 제외 확인
        second.ApplyTemporaryApDiscount(5); // 비용보다 큰 임시 할인 적용
        Assert.AreEqual(0, second.ApCost); // 행동력 비용 영 미만 방지 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 플레이어 턴 종료 확인
        Assert.AreEqual(1, first.ApCost); // 턴 종료 시 할인 제거 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 플레이어 턴 시작 확인
        deck.DiscardCard(first); // 첫 카드 다시 버리기
        Assert.IsTrue(deck.TryRecoverDiscard(first)); // 다음 턴 카드 회수 확인
        Assert.AreEqual(0, first.ApCost); // 다음 턴 첫 회수 할인 재사용 확인
        passive.Dispose(); // 패시브 연결 해제
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void Recovery_WithoutLivingMagicianDoesNotDiscount() // 생존 마법사 없는 회수 확인
    { // 테스트 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        CharacterData magician = roster.Characters[1]; // 마법사 원본 조회
        CharacterData ally = roster.Characters[0]; // 다른 아군 원본 조회
        CardData card = Resources.Load<CardData>("StarterCards/Attack"); // 행동력 일 카드 원본 조회
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        BattleUnitRuntime magicianUnit = BattleUnitRuntime.CreateAlly(magician); // 마법사 유닛 생성
        BattleUnitRuntime allyUnit = BattleUnitRuntime.CreateAlly(ally); // 다른 아군 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 생존 적 유닛 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(card, ally) }; // 다른 아군 카드 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { magicianUnit, allyUnit }, 5, 1); // 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { magicianUnit, allyUnit }, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        MagicianPassiveRuntime passive = new MagicianPassiveRuntime(deck, turns, new[] { magicianUnit, allyUnit }); // 마법사 패시브 연결
        Assert.IsTrue(turns.StartBattle(1)); // 전투와 카드 드로우 시작
        CardInstance recovered = deck.Hand[0]; // 회수할 카드 저장
        deck.DiscardCard(recovered); // 회수할 카드 버리기
        magicianUnit.TakeDamage(100000, BattleDamageType.Physical); // 마법사 전투 불능 처리
        Assert.IsTrue(turns.IsPlayerTurn); // 아군과 적 생존으로 전투 지속 확인
        Assert.IsTrue(deck.TryRecoverDiscard(recovered)); // 다른 아군 카드 회수 확인
        Assert.AreEqual(1, recovered.ApCost); // 전투 불능 마법사 할인 제외 확인
        passive.Dispose(); // 패시브 연결 해제
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 테스트 종료
} // 테스트 클래스 종료
