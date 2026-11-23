using System.Collections.Generic; // 빈 덱 목록 사용
using NUnit.Framework; // 자동 검증 기능 사용
using UnityEngine; // 유니티 데이터 사용
public sealed class ChariotConcentrationTests // 전차 돌파 표식 규칙 검사
{ // 검사 시작
    [Test] // 표식 누적 검사 지정
    public void Marks_TrackSecondAttackTargetChangesAndTurnEnd() // 공격 순서와 표식 정리 확인
    { // 표식 규칙 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime chariot = BattleUnitRuntime.CreateAlly(roster.Characters[7]); // 전차 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 적 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 생성
        BattleUnitRuntime[] allies = { chariot }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { first, second }; // 적 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, enemies, 0, BattleType.Normal); // 턴 관리자 생성
        ChariotConcentrationRuntime marks = new ChariotConcentrationRuntime(turns, enemies); // 표식 관리자 생성
        CardData starter = Resources.Load<CardData>("StarterCards/ChariotMark"); // 전차 전용 카드 조회
        CardData attack = Resources.Load<CardData>("StarterCards/Attack"); // 공용 공격 카드 조회
        CardInstance starterCard = CardInstance.Create(starter, chariot, 0); // 전용 카드 생성
        CardInstance attackCard = CardInstance.Create(attack, chariot, 1); // 공용 공격 카드 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        marks.RecordAttack(starterCard, first); // 첫 적에게 전용 카드 공격 기록
        Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.ChariotMark).StackCount); // 전용 카드 표식 한 개 확인
        marks.RecordAttack(attackCard, first); // 같은 적 두 번째 공격 기록
        Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.ChariotMark).StackCount); // 패시브 표식 한 개 추가 확인
        marks.RecordAttack(attackCard, first); // 같은 적 세 번째 공격 기록
        Assert.AreEqual(2, first.GetStatusEffect(BattleStatusEffectType.ChariotMark).StackCount); // 턴당 패시브 한 번 확인
        marks.RecordAttack(starterCard, second); // 다른 적 공격 기록
        Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 이전 대상 표식 제거 확인
        Assert.AreEqual(1, second.GetStatusEffect(BattleStatusEffectType.ChariotMark).StackCount); // 새 대상 카드 표식 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 플레이어 턴 종료 확인
        Assert.IsNull(second.GetStatusEffect(BattleStatusEffectType.ChariotMark)); // 턴 종료 표식 제거 확인
        marks.Dispose(); // 표식 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
        Object.DestroyImmediate(deckData); // 임시 덱 원본 정리
    } // 표식 규칙 검사 종료
} // 검사 종료
