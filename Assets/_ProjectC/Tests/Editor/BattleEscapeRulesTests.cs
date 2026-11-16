using System.Collections.Generic; // 빈 카드 목록 사용
using NUnit.Framework; // 전투 도주 규칙 검증 사용
using UnityEngine; // 임시 전투 데이터 생성 사용

public sealed class BattleEscapeRulesTests // 전투 등급별 도주 검사
{ // 도주 검사 시작
    [TestCase(BattleType.Normal, true)] // 일반 전투 도주 가능 지정
    [TestCase(BattleType.Elite, true)] // 정예 전투 도주 가능 지정
    [TestCase(BattleType.Boss, false)] // 보스 전투 도주 불가 지정
    public void Escape_OnlyNormalAndEliteAllowPlayerTurn(BattleType battleType, bool expectedAllowed) // 기획서의 등급별 도주 확인
    { // 전투 도주 검사 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckSource = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 생존 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 생존 적 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckSource, new List<IDeckCardEntry>(), new[] { ally }, 20, 1); // 빈 카드 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally }, new[] { enemy }, 0, battleType); // 지정 등급 전투 생성
        try // 임시 데이터 정리 보장
        { // 도주 규칙 검증 시작
            Assert.IsFalse(turns.CanEscape); // 전투 시작 전 도주 불가 확인
            Assert.IsTrue(turns.StartBattle(0)); // 실제 전투 시작 확인
            Assert.AreEqual(expectedAllowed, turns.CanEscape); // 플레이어 턴 도주 가능 여부 확인
            Assert.AreEqual(expectedAllowed, turns.TryEscape()); // 전투 등급별 도주 결과 확인
            if (expectedAllowed) // 도주 가능 전투 확인
            { // 도주 완료 검사 시작
                Assert.AreEqual(BattleResult.Escape, turns.Result); // 도주 결과 확정 확인
                Assert.IsFalse(turns.TryEscape()); // 종료 후 중복 도주 차단 확인
            } // 도주 완료 검사 종료
            else // 도주 불가 전투 확인
            { // 보스 전투 유지 검사 시작
                Assert.AreEqual(BattleResult.None, turns.Result); // 보스 전투 유지 확인
                Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 전환 확인
                Assert.IsFalse(turns.CanEscape); // 적 턴 도주 불가 확인
            } // 보스 전투 유지 검사 종료
        } // 도주 규칙 검증 종료
        finally // 검사 객체 정리
        { // 임시 데이터 정리 시작
            turns.Dispose(); // 전투 이벤트 연결 해제
            Object.DestroyImmediate(deckSource); // 임시 덱 원본 제거
            Object.DestroyImmediate(enemySource); // 임시 적 원본 제거
            Object.DestroyImmediate(character); // 임시 아군 원본 제거
        } // 임시 데이터 정리 종료
    } // 전투 도주 검사 종료
} // 도주 검사 종료
