using System.Collections.Generic; // 빈 카드 목록 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
public sealed class StarHealingTests // 별 예약 회복 상태 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void ReservedHealing_TriggersAtNextAlliedTurn() // 다음 아군 턴의 회복 발동 확인
    { // 테스트 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), new[] { ally }, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally }, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, new[] { ally }, new[] { enemy }, new BattleStatusEffectProcessor()); // 상태 처리 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        ally.TakeDamage(20, BattleDamageType.None); // 아군 체력 감소
        int healthBefore = ally.CurrentHealth; // 회복 전 체력 저장
        ally.ApplyStatusEffect(BattleStatusEffectType.StarReservedHealing, 6, 1, 1); // 다음 턴 회복 예약
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 시작 확인
        Assert.AreEqual(healthBefore, ally.CurrentHealth); // 적 턴 시작 시 회복 없음 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 아군 턴 시작 확인
        Assert.AreEqual(healthBefore + 6, ally.CurrentHealth); // 다음 아군 턴 회복 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 회복 예약 소멸 확인
        statuses.Dispose(); // 상태 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(deckData); // 덱 원본 정리
        Object.DestroyImmediate(allyData); // 아군 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 테스트 종료
    [Test] // 테스트 지정
    public void ReservedHealing_DoesNotReviveDefeatedAlly() // 전투 불능 아군 회복 제외 확인
    { // 테스트 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 아군 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 아군 유닛 생성
        ally.ApplyStatusEffect(BattleStatusEffectType.StarReservedHealing, 6, 1, 1); // 다음 턴 회복 예약
        ally.TakeDamage(100000, BattleDamageType.None); // 아군 전투 불능 처리
        new BattleStatusEffectProcessor().ProcessPhaseStart(ally, 2); // 아군 턴 시작 처리
        Assert.IsTrue(ally.IsDead); // 전투 불능 상태 유지 확인
        Assert.AreEqual(0, ally.CurrentHealth); // 체력 회복 없음 확인
        Object.DestroyImmediate(allyData); // 아군 원본 정리
    } // 테스트 종료
} // 테스트 클래스 종료
