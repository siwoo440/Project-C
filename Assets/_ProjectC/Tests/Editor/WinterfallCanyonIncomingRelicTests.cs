using System; // 실제 전투 데이터의 독립 수명
using System.Collections; // 실제 플레이 모드 검사와 종료 입력
using NUnit.Framework; // 실제 전투 예고와 결과 및 자원 확인
using UnityEngine; // 현재 유물 원본과 정식 전투 객체 생성
using UnityEngine.TestTools; // 기존 플레이 모드 기반의 회귀 처리
using Object = UnityEngine.Object; // 검사에서 생성한 임시 객체 정리
using Random = UnityEngine.Random; // 원래 전투 확률의 상태 및 재현

public sealed class WinterfallCanyonIncomingRelicTests // 협곡의 적 명중과 아군 회피 및 실제 반격 검사
{ // 실제 유물 세 종의 회귀 시작
    [UnityTest] // 생존 적의 명중 예고와 실제 공격 경계 확인
    public IEnumerator AshDustLowersOnlyRegisteredEnemyAccuracy() // 현재 교전의 적 전체에만 다섯 퍼센트포인트 감소
    { // 활성 출전과 미등록 대상의 실제 명중 입력 확인 시작
        yield return new EnterPlayMode(); // 원본 유물과 전투가 있는 플레이 모드 시작
        using (var owned = new OwnedRelic("AshDustStatueEye")) // 정식 재먼지 석상 눈의 원래 획득
        using (var fight = new Trial(BattleType.Normal)) // 정상 교전의 실제 아군 및 적 배치
        { // 원래 공격자의 현재 명중률과 실제 명중 비교 시작
            var outside = BattleUnitRuntime.CreateEnemy(fight.EnemyData); // 같은 원본이지만 다른 미등록 적
            Assert.AreEqual(95, BattleHitChance.GetPercent(fight.Enemy)); // 실제 교전 적의 공용 명중 예고
            Assert.AreEqual(95, BattleHitChance.GetPercent(fight.Enemy, fight.First)); // 실제 아군 대상의 동일 예고
            Assert.AreEqual(100, BattleHitChance.GetPercent(fight.First, fight.Enemy)); // 아군 공격에 불이익 없음
            Assert.AreEqual(100, BattleHitChance.GetPercent(outside, fight.First)); // 미등록 적에 불이익 없음
            Assert.IsTrue(CreateAttack(fight, 95).Execute().WasMissed); // 정확히 구십오에서 실제 공격 실패
            Assert.IsFalse(CreateAttack(fight, 94).Execute().WasMissed); // 한 점 낮은 실제 적 공격 명중
        } // 원래 공격자의 현재 명중률과 실제 명중 비교 종료
        yield return new ExitPlayMode(); // 실제 전투 관리자의 원본 상태 복원
    } // 활성 출전과 미등록 대상의 실제 명중 입력 확인 종료

    [UnityTest] // 정확히 두 실제 적 턴의 아군 별개 회피 보호 확인
    public IEnumerator CanyonWhistleStartsBeforeAmbushAndExpiresAfterTwoRealEnemyTurns() // 적 선제 차례와 시간 동전의 아군 차례 분리
    { // 기본 회피의 합산과 두 실제 적 턴의 수명 확인 시작
        yield return new EnterPlayMode(); // 정식 협곡 바람피리의 실제 전투 진입
        using (var owned = new OwnedRelic("CanyonWindWhistle")) // 정식 휘슬의 원본 효과 획득
        using (var fight = new Trial(BattleType.Normal, enemyFirst: true)) // 첫 실제 행동이 적 선제인 전투
        { // 두 실제 적 차례와 아군 추가 차례 비교 시작
            var effect = (BattleStatusEffectType)97; // 원본에 등록된 협곡 바람 상태의 실제 번호
            Assert.AreEqual(8, fight.First.GetStatusEffect(effect)?.EffectiveValue); // 첫 생존 아군의 개막 상태
            Assert.AreEqual(8, fight.Other.GetStatusEffect(effect)?.EffectiveValue); // 두 번째 아군에도 같은 개막 상태
            Assert.AreEqual(2, fight.First.GetStatusEffect(effect)?.RemainingTurns); // 처음 두 번의 실제 적 차례 예약
            fight.First.ApplyStatusEffect(BattleStatusEffectType.Evasion, 15, 3, 1); // 별개인 원래 회피 강화 부여
            Assert.AreEqual(77, BattleHitChance.GetPercent(fight.Enemy, fight.First)); // 15와 8의 실제 퍼센트포인트 합산
            Assert.AreEqual(92, BattleHitChance.GetPercent(fight.Enemy, fight.Other)); // 다른 아군에는 협곡 바람만 적용
            Assert.AreEqual(100, BattleHitChance.GetPercent(fight.First, fight.Enemy)); // 적에게 부여되지 않은 개막 강화
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 실제 선제 공격 차례 완료
            Assert.AreEqual(1, fight.First.GetStatusEffect(effect)?.RemainingTurns); // 선제도 실제 차례 한 번으로 계산
            new BattleStatusEffectProcessor().ProcessPhaseEnd(fight.First, 1); // 플레이어의 원래 개별 상태 종료 처리
            Assert.AreEqual(1, fight.First.GetStatusEffect(effect)?.RemainingTurns); // 개별 아군 턴에는 휘슬 유지
            Assert.AreEqual(1, fight.Turn.QueueTimeCoinTurn(2)); // 적 차례가 없는 실제 추가 아군 턴 예약
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 시간 동전의 실제 아군 추가 차례 진입
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turn.CurrentPhase); // 적 행동을 실제로 시작하지 않은 확인
            Assert.AreEqual(1, fight.Other.GetStatusEffect(effect)?.RemainingTurns); // 추가 아군 턴에도 휘슬 유지
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 두 번째 실제 적 차례 진입
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turn.CurrentPhase); // 정상 두 번째 적 차례 확인
            Assert.AreEqual(92, BattleHitChance.GetPercent(fight.Enemy, fight.Other)); // 마지막 실제 적 공격에도 휘슬 방어
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 두 번째 실제 적 차례 완료
            Assert.IsNull(fight.First.GetStatusEffect(effect)); // 첫 아군에서 두 번째 적 턴 뒤 회피 만료
            Assert.IsNull(fight.Other.GetStatusEffect(effect)); // 다른 아군에서도 같은 경계에 회피 만료
            Assert.AreEqual(85, BattleHitChance.GetPercent(fight.Enemy, fight.First)); // 별개의 기본 회피 정상 유지
        } // 두 실제 적 차례와 아군 추가 차례 비교 종료
        yield return new ExitPlayMode(); // 실제 원본 전투 종료
    } // 기본 회피의 합산과 두 실제 적 턴의 수명 확인 종료

    [UnityTest] // 돌가시의 실제 20퍼센트 경계와 유효 타격 확인
    public IEnumerator StoneThornsUseOneChancePerRealImpactAndRetaliateAgainstItsAttacker() // 무효와 다른 출처를 배제한 체력 및 방어도 반격
    { // 실제 원래 난수를 보존한 반격 한 번 비교 시작
        yield return new EnterPlayMode(); // 실제 전투의 유물과 피해 처리 준비
        using (var owned = new OwnedRelic("StoneThornBundle")) // 실제 돌가시 묶음 자원 획득
        using (var fight = new Trial(BattleType.Normal)) // 첫 아군과 수호자가 함께 있는 실제 전투
        { // 무효 이후 피격과 수호 전환의 정확한 반격 비교 시작
            Random.State saved = Random.state; // 원래 게임의 난수 상태 보관
            try // 이번 검증과 다른 전투의 무작위 입력 분리
            { // 실제 원래 공격과 반격의 성공 또는 실패 구간
                fight.First.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 실제 첫 양수 공격 무효화
                SetNextRoll(19); // 유효하면 실제 반격이 성공할 난수 고정
                Assert.IsTrue(fight.First.TakeDamage(10, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true).WasDamageNullified); // 완전 무효화의 실제 공격
                fight.First.TakeDamage(1, BattleDamageType.None, fight.Enemy); // 적 출처가 있어도 직접 공격이 아닌 원래 피해
                fight.First.TakeDamage(1, BattleDamageType.None, null, enemyDirectAttack: true); // 실제 적 출처 없는 직접 표기 피해
                fight.First.PreviewDamage(10, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true); // 난수를 소비하지 않는 실제 순수 예고
                Assert.AreEqual(19, Random.Range(0, 100)); // 무효와 기타 피해 및 예고의 난수 미사용
                Assert.AreEqual(200, fight.Enemy.CurrentHealth); // 무효와 출처 없는 피격 동안 적 반격 없음
                fight.First.GainBlock(10); // 실제 방어도만 잃는 정상 양수 직격 준비
                SetNextRoll(19); // 실제 반격 한 번 성공 경계
                Assert.AreEqual(10, fight.First.TakeDamage(10, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true).AbsorbedDamage); // 방어도만 줄어드는 실제 피격
                Assert.AreEqual(197, fight.Enemy.CurrentHealth); // 방어도 흡수도 실제 돌가시 피해 세 점 적용
                SetNextRoll(20); // 정확히 이십인 실패 경계
                fight.First.TakeDamage(10, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true); // 실제 주 대상이 순체력 피해
                Assert.AreEqual(197, fight.Enemy.CurrentHealth); // 이십 퍼센트 경계에서 반격 피해 없음
                Assert.AreEqual(BattleStatusEffectApplyResult.Applied, fight.First.ApplyGuardianHeart(fight.Other)); // 치명 공격을 대신 받을 실제 수호자 지정
                int protectedHealth = fight.First.CurrentHealth; // 보호된 원래 아군의 현재 체력 기록
                SetNextRoll(19); // 전환한 한 건의 실제 반격 성공 경계
                fight.First.TakeDamage(300, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true); // 실제 양수 치명 공격의 수호자 전환
                Assert.AreEqual(protectedHealth, fight.First.CurrentHealth); // 원래 대상은 전환에 따라 체력 보존
                Assert.AreEqual(1, fight.Other.CurrentHealth); // 실제 대신 맞은 수호자의 기존 생존 보호
                Assert.AreEqual(194, fight.Enemy.CurrentHealth); // 전환 전 원래 대상의 중복 없이 세 점만 반격
            } // 실제 원래 공격과 반격의 성공 또는 실패 구간 종료
            finally // 다른 검증이나 실제 전투에서 사용한 난수 상태 분리
            { // 정상 원래 난수 복원 시작
                Random.state = saved; // 원래 Unity 무작위 상태로 되돌림
            } // 정상 원래 난수 복원 종료
        } // 무효 이후 피격과 수호 전환의 정확한 반격 비교 종료
        yield return new ExitPlayMode(); // 실제 유물 회차와 피해 처리 환경 종료
    } // 실제 원래 난수를 보존한 반격 한 번 비교 종료

    private static BattleEnemyAction CreateAttack(Trial fight, int roll) // 지정 난수와 실제 아군 상대를 갖춘 원래 적 공격
    { // 조건이 없는 실제 공격 입력 준비 시작
        return new BattleEnemyAction(fight.Enemy, fight.First, EnemyActionType.Attack, BattleDamageType.None, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "test", 1, 1, 1, 0, percentRoll: () => roll, battleType: BattleType.Normal); // 동일한 원래 명중 및 피해 코드 사용
    } // 조건이 없는 실제 공격 입력 준비 종료

    private static void SetNextRoll(int expected) // 특정 명중 난수의 재현 가능한 원래 Unity 상태 확인
    { // 테스트를 위한 원래 난수 상태 탐색 시작
        for (int seed = 0; seed < 10000; seed++) // 충분한 작은 시드 범위의 결정적 순회
        { // 각 시드의 첫 난수 확인 시작
            Random.InitState(seed); // 현재 시드에 대한 실제 Unity 난수 복원
            if (Random.Range(0, 100) == expected) // 요청한 반격 경계의 실제 동일 난수 확인
            { // 찾은 최초 난수의 직전 상태 준비 시작
                Random.InitState(seed); // 바로 다음 원래 확률 판정에서 사용
                return; // 조건을 충족하는 난수 상태 설정 완료
            } // 찾은 최초 난수의 직전 상태 준비 종료
        } // 각 시드의 첫 난수 확인 종료
        Assert.Fail("요청한 반격 난수 상태 누락"); // 실제 검증 조건 누락의 즉시 실패
    } // 테스트를 위한 원래 난수 상태 탐색 종료

    private sealed class Trial : IDisposable // 실제 조우 등급과 전투 참가를 준비할 검증용 수명
    { // 기존 실제 피해 환경 구성 시작
        private readonly CharacterData firstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 소유자의 실제 캐릭터 형식
        private readonly CharacterData otherData = ScriptableObject.CreateInstance<CharacterData>(); // 다른 소유자를 구분할 캐릭터 형식
        internal readonly EnemyData EnemyData = ScriptableObject.CreateInstance<EnemyData>(); // 양수 직접 공격의 실제 적 원본
        private readonly CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 전투 런타임의 임시 카드 원본
        private readonly DeckData deckData; // 시작 뒤 정리할 실제 임시 덱
        internal BattleUnitRuntime First { get; } // 앞에 배치된 실제 아군 한 명
        internal BattleUnitRuntime Other { get; } // 같은 광역의 뒤쪽 실제 참가 아군
        internal BattleUnitRuntime Enemy { get; } // 원본 조우에 참가하는 실제 적
        internal BattleTurnRuntime Turn { get; } // 턴과 현재 유물을 연결한 실제 전투 흐름
        internal Trial(BattleType type, bool enemyFirst = false) // 실제 정상 또는 보스 전투을 구성할 입력
        { // 원래 검증 내용을 위한 실제 전투 구성 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", firstData); // 방어력 없이 충분히 생존하는 첫 아군 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", otherData); // 동일한 두 번째 아군 원래 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"basicAttackPower\":10,\"damageType\":1,\"targetRule\":" + "0" + "}", EnemyData); // 실제 고정 공격과 선택한 원래 대상 규칙
            First = BattleUnitRuntime.CreateAlly(firstData); // 먼저 맞는 실제 파티의 생존 아군
            Other = BattleUnitRuntime.CreateAlly(otherData); // 두 번째 실제 파티의 생존 아군
            Enemy = BattleUnitRuntime.CreateEnemy(EnemyData); // 실제 적 공격의 원본 개체
            var allies = new[] { First, Other }; // 실제로 공격받을 아군의 기존 배치 순서
            var enemies = new[] { Enemy }; // 공격 횟수를 구분할 기본 적 하나
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(cardData, firstData) }); // 실제 아군 소유자와 임시 덱 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 286); // 자동 드로우에서 기대하는 기존 덱 형식
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, type); // 실제 출전 참가자와 전투 유형 연결
            Assert.IsTrue(Turn.StartBattle(0, enemyFirst)); // 원본 선제 상태에 따라 유물 보유 및 실제 차례 초기화
        } // 원래 검증 내용을 위한 실제 전투 구성 종료
        public void Dispose() // 실제 검사 데이터와 런타임 공격 관리자의 종료
        { // 독립 검증의 원래 상태 복구 시작
            Turn.Dispose(); // 파티마다 연결한 현재 전투 자료 해제
            Object.Destroy(deckData); // 이 검사에서 생성한 임시 덱 정리
            Object.Destroy(cardData); // 실제 원본 파일과 분리해 만든 검사 카드 정리
            Object.Destroy(EnemyData); // 실제 보스 및 일반 전투 검사에 사용한 적 원본 정리
            Object.Destroy(firstData); // 첫 아군의 독립 원본 정리
            Object.Destroy(otherData); // 두 번째 아군의 독립 원본 정리
        } // 독립 검증의 원래 상태 복구 종료
    } // 기존 실제 피해 환경 구성 종료

    private sealed class OwnedRelic : IDisposable // 실제 유물 획득과 검증 환경 종료
    { // 독립 유물 관리 시작
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 기존 실제 유물을 보관하는 회차 관리자
        private readonly RelicData relic; // 이번 검증에서만 소유할 정식 유물
        internal OwnedRelic(string stem) // 실제 리소스 이름으로 원래 획득
        { // 원본 확인과 유물 정상 획득 시작
            relic = Resources.Load<RelicData>("Relics/" + stem); // 새 게임의 실제 등록 리소스
            Assert.IsNotNull(relic, stem); // 저장 큐와 게임에 필요한 실제 유물 존재
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 정상 회차의 실제 유물 획득
        } // 원본 확인과 유물 정상 획득 종료
        public void Dispose() => manager.TryRemove(relic.RelicId); // 검사에서 획득한 정식 유물만 해제
    } // 독립 유물 관리 종료
} // 전투 방어 유물 검사 종료
