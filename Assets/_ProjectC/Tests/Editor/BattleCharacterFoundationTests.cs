using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 빈 카드 목록 사용
using System.Reflection; // 테스트 원본 약점 설정 사용
using NUnit.Framework; // 공통 전투 규칙 검증 사용
using UnityEngine; // 캐릭터와 적 테스트 원본 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용

public sealed class BattleCharacterFoundationTests // 캐릭터 공통 전투 규칙 검사
{ // 공통 전투 검사 시작
    [TestCase(100, BattleDamageType.Physical, 100, 0, 50)] // 물리 방어 비율 검사
    [TestCase(100, BattleDamageType.Magical, 0, 300, 25)] // 마법 저항 비율 검사
    [TestCase(10, BattleDamageType.Physical, 3, 0, 9)] // 소수점 버림 검사
    [TestCase(1, BattleDamageType.Physical, 9999, 0, 1)] // 양수 피해 최솟값 검사
    [TestCase(0, BattleDamageType.Physical, 100, 0, 0)] // 원본 피해 없음 검사
    [TestCase(10, BattleDamageType.None, 100, 100, 10)] // 방어 미적용 피해 검사
    [TestCase(int.MaxValue, BattleDamageType.Physical, 0, 0, int.MaxValue)] // 큰 피해의 정수 범위 검사
    public void Damage_UsesConfirmedDefenseRatio(int raw, BattleDamageType type, int physical, int magical, int expected) // 확정 방어 비율 적용 확인
    { // 방어 비율 검사 시작
        BattleDamageResult result = BattleDamageCalculator.Calculate(raw, type, physical, magical); // 실제 공통 피해 계산
        Assert.AreEqual(expected, result.FinalDamage); // 기획서 기준 최종 피해 확인
    } // 방어 비율 검사 종료
    [UnityTest] // 실제 약점 문맥 검사 지정
    public IEnumerator Weakness_MultipliesBeforeFinalRounding() // 약점 배율과 최종 버림 순서 확인
    { // 약점 배율 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 원본 생성
        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 임시 카드 원본 생성
        try // 임시 원본 정리 보장
        { // 약점 계산 검증 시작
            FieldInfo weaknesses = typeof(EnemyData).GetField("weaknessCardTypes", BindingFlags.Instance | BindingFlags.NonPublic); // 적 약점 데이터 조회
            ((List<CardType>)weaknesses.GetValue(enemy)).Add(card.CardType); // 테스트 카드 속성을 약점으로 등록
            BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(character); // 카드 사용자 생성
            BattleUnitRuntime target = BattleUnitRuntime.CreateEnemy(enemy); // 약점 대상 생성
            CardInstance instance = CardInstance.Create(card, owner, 0); // 전투 카드 생성
            BattleCardDamageContext.Begin(instance, new[] { target }); // 실제 카드 피해 문맥 생성
            BattleDamageResult result = BattleDamageCalculator.Calculate(4, BattleDamageType.Physical, 100, 0); // 방어와 약점 포함 피해 계산
            Assert.IsTrue(result.IsWeakness); // 약점 판정 확인
            Assert.AreEqual(2, result.DefenseAdjustedDamage); // 표시용 방어 결과 확인
            Assert.AreEqual(3, result.FinalDamage); // 방어 비율과 약점 배율 확인
            BattleCardDamageContext.Begin(instance, new[] { target }); // 두 번째 카드 피해 문맥 생성
            BattleDamageResult fractional = BattleDamageCalculator.Calculate(3, BattleDamageType.Physical, 100, 0); // 소수 방어값과 약점 포함 피해 계산
            Assert.AreEqual(1, fractional.DefenseAdjustedDamage); // 표시용 방어 결과 버림 확인
            Assert.AreEqual(2, fractional.FinalDamage); // 배율 적용 후 최종 한 번 버림 확인
        } // 약점 계산 검증 종료
        finally // 임시 원본 정리
        { // 약점 검사 정리 시작
            BattleCardDamageContext.Clear(); // 카드 피해 문맥 제거
            Object.DestroyImmediate(card); // 임시 카드 원본 제거
            Object.DestroyImmediate(enemy); // 임시 적 원본 제거
            Object.DestroyImmediate(character); // 임시 캐릭터 원본 제거
        } // 약점 검사 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 약점 배율 검사 종료
    [Test] // 정신 상태 지속 검사 지정
    public void SpecialState_RecordsChangesWithoutRestartAndReturnsToFifty() // 특수 상태 중 정신력 기록 확인
    { // 특수 상태 기록 검사 시작
        BattleMentalRuntime mental = new BattleMentalRuntime(99); // 각성 직전 정신력 생성
        mental.ChangeMental(1, BattleMentalChangeReason.None); // 각성 진입
        Assert.AreEqual(BattleMentalState.Awakening, mental.CurrentState); // 각성 상태 확인
        mental.ChangeMental(-100, BattleMentalChangeReason.None); // 반대 임계값까지 정신력 감소
        Assert.AreEqual(0, mental.CurrentMental); // 특수 상태 중 정신력 변화 기록 확인
        Assert.AreEqual(BattleMentalState.Awakening, mental.CurrentState); // 반대 임계값의 상태 변경 방지 확인
        Assert.AreEqual(3, mental.RemainingTurns); // 특수 상태 지속 시간 재시작 방지 확인
        mental.AdvanceStateTurn(); // 첫 지속 턴 경과
        mental.ChangeMental(25, BattleMentalChangeReason.None); // 특수 상태 중 정신력 증가
        Assert.AreEqual(25, mental.CurrentMental); // 증가한 정신력 기록 확인
        Assert.AreEqual(2, mental.RemainingTurns); // 정신력 변화의 남은 턴 유지 확인
        mental.AdvanceStateTurn(); // 두 번째 지속 턴 경과
        mental.AdvanceStateTurn(); // 세 번째 지속 턴 경과
        Assert.AreEqual(BattleMentalState.Neutral, mental.CurrentState); // 특수 상태 종료 확인
        Assert.AreEqual(50, mental.CurrentMental); // 종료 후 정신력 50 복귀 확인
    } // 특수 상태 기록 검사 종료
    [Test] // 붕괴 상태 지속 검사 지정
    public void Collapse_RecordsLargeChangesWithoutSwitchingState() // 붕괴 중 큰 정신력 변화 확인
    { // 붕괴 상태 기록 검사 시작
        BattleMentalRuntime mental = new BattleMentalRuntime(1); // 붕괴 직전 정신력 생성
        mental.ChangeMental(-1, BattleMentalChangeReason.None); // 붕괴 진입
        mental.ChangeMental(int.MaxValue, BattleMentalChangeReason.None); // 큰 정신력 증가 적용
        Assert.AreEqual(100, mental.CurrentMental); // 큰 증가량의 최대 정신력 제한 확인
        Assert.AreEqual(BattleMentalState.Collapse, mental.CurrentState); // 붕괴 중 각성 전환 방지 확인
        Assert.AreEqual(3, mental.RemainingTurns); // 붕괴 지속 시간 재시작 방지 확인
        mental.ResolveAtBattleEnd(); // 전투 종료 상태 정리
        Assert.AreEqual(BattleMentalState.Neutral, mental.CurrentState); // 전투 종료 후 일반 상태 확인
        Assert.AreEqual(50, mental.CurrentMental); // 전투 종료 후 정신력 50 복귀 확인
    } // 붕괴 상태 기록 검사 종료
    [Test] // 저장 정신력 검사 지정
    public void RestoredMental_ActivatesBoundaryStateForBattle() // 저장 정신력 전투 시작 판정 확인
    { // 저장 정신력 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 원본 생성
        try // 임시 원본 정리 보장
        { // 저장 정신력 검증 시작
            BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(source); // 실제 아군 유닛 생성
            Assert.IsTrue(unit.ApplyPersistentMental(100)); // 저장 정신력 100 적용
            Assert.AreEqual(BattleMentalState.Awakening, unit.MentalState); // 각성 초기 판정 확인
            Assert.AreEqual(3, unit.MentalRemainingTurns); // 각성 초기 지속 턴 확인
            Assert.IsTrue(unit.ApplyPersistentMental(0)); // 저장 정신력 0 적용
            Assert.AreEqual(BattleMentalState.Collapse, unit.MentalState); // 붕괴 초기 판정 확인
            Assert.IsTrue(unit.ApplyPersistentMental(50)); // 일반 정신력 적용
            Assert.AreEqual(BattleMentalState.Neutral, unit.MentalState); // 일반 정신 상태 확인
        } // 저장 정신력 검증 종료
        finally // 임시 원본 정리
        { // 원본 정리 시작
            Object.DestroyImmediate(source); // 임시 캐릭터 원본 제거
        } // 원본 정리 종료
    } // 저장 정신력 검사 종료
    [TestCase(true)] // 자기 턴 발동 사례
    [TestCase(false)] // 상대 턴 발동 사례
    public void SpecialState_FirstDurationTickDependsOnOwnPhase(bool startsDuringOwnTurn) // 발동 시점별 첫 지속 턴 감소 확인
    { // 상태 첫 감소 검사 시작
        CharacterData allySource = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckSource = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        try // 전투 테스트 자원 정리 보장
        { // 상태 첫 감소 검증 시작
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allySource); // 첫 아군 생성
            BattleUnitRuntime companion = BattleUnitRuntime.CreateAlly(allySource); // 두 번째 생존 아군 생성
            BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 적 유닛 생성
            BattleDeckRuntime deck = BattleDeckRuntime.Create(deckSource, new List<IDeckCardEntry>(), new[] { ally }, 5, 1); // 빈 전투 덱 생성
            BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally, companion }, new[] { enemy }, 0, BattleType.Normal); // 실제 턴 관리자 생성
            BattleMentalController controller = new BattleMentalController(turns, new[] { ally, companion }, new[] { enemy }); // 실제 정신력 턴 처리 연결
            Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
            ally.ApplyPersistentMental(99); // 각성 직전 저장 정신력 설정
            if (startsDuringOwnTurn) // 자기 진영 턴 발동 확인
            { // 자기 턴 발동 시작
                ally.ChangeMental(1, BattleMentalChangeReason.None); // 자기 턴 각성 발동
                turns.EndPlayerTurn(); // 현재 아군 턴 종료
                Assert.AreEqual(3, ally.MentalRemainingTurns); // 같은 턴 첫 감소 생략 확인
                turns.CompleteEnemyTurn(); // 다음 아군 턴 시작
                turns.EndPlayerTurn(); // 다음 아군 턴 종료
                Assert.AreEqual(2, ally.MentalRemainingTurns); // 다음 아군 턴부터 감소 확인
            } // 자기 턴 발동 종료
            else // 상대 진영 턴 발동 확인
            { // 상대 턴 발동 시작
                turns.EndPlayerTurn(); // 상대 턴 시작
                ally.ChangeMental(1, BattleMentalChangeReason.None); // 상대 턴 중 아군 각성 발동
                turns.CompleteEnemyTurn(); // 아군 턴 시작
                turns.EndPlayerTurn(); // 첫 아군 턴 종료
                Assert.AreEqual(2, ally.MentalRemainingTurns); // 첫 아군 턴 종료 시 감소 확인
            } // 상대 턴 발동 종료
            controller.Dispose(); // 정신력 관리자 연결 해제
            turns.Dispose(); // 턴 관리자 연결 해제
        } // 상태 첫 감소 검증 종료
        finally // 임시 원본 정리
        { // 테스트 원본 정리 시작
            Object.DestroyImmediate(deckSource); // 임시 덱 원본 제거
            Object.DestroyImmediate(enemySource); // 임시 적 원본 제거
            Object.DestroyImmediate(allySource); // 임시 아군 원본 제거
        } // 테스트 원본 정리 종료
    } // 상태 첫 감소 검사 종료
    [Test] // 턴 종료 중 발동 경계 검사 지정
    public void SpecialState_StartingAfterDurationTickDoesNotSkipNextTurn() // 턴 종료 후반 상태 발동 확인
    { // 턴 종료 경계 검사 시작
        CharacterData allySource = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckSource = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        try // 테스트 자원 정리 보장
        { // 턴 종료 경계 검증 시작
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allySource); // 대상 아군 생성
            BattleUnitRuntime companion = BattleUnitRuntime.CreateAlly(allySource); // 두 번째 생존 아군 생성
            BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 적 유닛 생성
            BattleDeckRuntime deck = BattleDeckRuntime.Create(deckSource, new List<IDeckCardEntry>(), new[] { ally }, 5, 1); // 빈 전투 덱 생성
            BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally, companion }, new[] { enemy }, 0, BattleType.Normal); // 실제 턴 관리자 생성
            BattleMentalController controller = new BattleMentalController(turns, new[] { ally, companion }, new[] { enemy }); // 정신력 턴 처리 연결
            Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
            ally.ApplyPersistentMental(99); // 각성 직전 정신력 설정
            bool triggered = false; // 턴 종료 효과 발동 여부 보관
            turns.PhaseEnded += (phase, round) => // 지속 시간 처리 뒤의 턴 종료 효과 연결
            { // 턴 종료 효과 시작
                if (phase == BattleTurnPhase.PlayerTurn && !triggered) // 첫 아군 턴 종료 확인
                { // 턴 종료 정신력 변화 시작
                    triggered = true; // 중복 발동 방지 기록
                    ally.ChangeMental(1, BattleMentalChangeReason.None); // 턴 종료 후반 각성 발동
                } // 턴 종료 정신력 변화 종료
            }; // 턴 종료 효과 연결 종료
            turns.EndPlayerTurn(); // 첫 아군 턴 종료
            Assert.AreEqual(3, ally.MentalRemainingTurns); // 발동 직후 지속 시간 확인
            turns.CompleteEnemyTurn(); // 다음 아군 턴 시작
            turns.EndPlayerTurn(); // 다음 아군 턴 종료
            Assert.AreEqual(2, ally.MentalRemainingTurns); // 다음 턴에서 정상 감소 확인
            controller.Dispose(); // 정신력 관리자 연결 해제
            turns.Dispose(); // 턴 관리자 연결 해제
        } // 턴 종료 경계 검증 종료
        finally // 임시 원본 정리
        { // 테스트 원본 정리 시작
            Object.DestroyImmediate(deckSource); // 임시 덱 원본 제거
            Object.DestroyImmediate(enemySource); // 임시 적 원본 제거
            Object.DestroyImmediate(allySource); // 임시 아군 원본 제거
        } // 테스트 원본 정리 종료
    } // 턴 종료 경계 검사 종료
} // 공통 전투 검사 종료
