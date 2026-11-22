using System; // 실제 교전의 소유 자산과 수명
using System.Collections; // Unity 실제 플레이 모드의 검사 대기
using NUnit.Framework; // 실제 피해, 보호막 및 적용 경계의 확인
using UnityEngine; // 유물과 원래 전투 자산을 직접 사용
using UnityEngine.TestTools; // 플레이 모드에서 실제 전투 함수 실행
using Object = UnityEngine.Object; // 현재 검사에서만 만든 원본의 수명

public sealed class WinterfallSwampLateRelicTests // 아군 진행과 파티 저체력의 별도 방어 원래 경로
{ // 실제 차례와 체력 경계 및 선제 행동 예고의 회귀 확인 시작
    [UnityTest] // 여섯 번째 실제 아군 차례가 원래 피해와 예고에 공통으로 적용
    public IEnumerator GuardianStoneStartsOnSixthActualAllyTurnWithAmbushAndTimeCoin() // 선제는 제외하고 시간 동전의 실제 아군 시작은 포함
    { // 원래 적 피해와 무방어 실제 피해 및 체력 비용 확인 시작
        yield return new EnterPlayMode(); // 실제 원본 유물과 피해 계산 환경 시작
        using (var owned = new OwnedRelic("RottenGuardianStone")) // 실제 마지막 방어 유물의 원래 장착
        using (var fight = new Trial()) // 피해 방어가 영이고 체력이 충분한 실제 교전
        { // 첫 적 차례와 다섯 아군 시작 이후 계산 비교 시작
            Assert.IsTrue(fight.Turn.StartBattle(0, enemyFirst: true)); // 선제 적 차례는 원래 첫 아군이 아닌 경계
            Assert.AreEqual(20, PreviewDirect(fight, 20).AppliedDamage); // 앞선 적 선제에서는 일반 원래 예고
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 처음 실제 아군 차례 시작
            for (int start = 1; start < 5; start++) // 다섯 번째 실제 아군 차례까지 네 번 정상 이동
            { // 기존 실제 두 진영 차례 종료 시작
                Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 현재 실제 아군의 끝
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 다음 실제 아군 시작
            } // 기존 실제 두 진영 차례 종료 완료
            Assert.AreEqual(20, PreviewDirect(fight, 20).AppliedDamage); // 정확히 다섯 번째 아군 차례까지 일반 피해
            Assert.AreEqual(1, fight.Turn.QueueTimeCoinTurn(2)); // 여섯 번째 실제 아군 시작을 직접 예약
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 적 행동 없이 여섯 번째 아군 차례를 실제로 실행
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turn.CurrentPhase); // 실제 시간 동전의 새 아군 차례
            Assert.AreEqual(16, PreviewDirect(fight, 20).AppliedDamage); // 여섯 번째 시작부터 예고 피해 이십 퍼센트 경감
            Assert.AreEqual(16, fight.First.TakeDamage(20, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 같은 적 직접 공격의 실제 피해
            Assert.AreEqual(16, fight.First.TakeDamage(20, BattleDamageType.None).AppliedDamage); // 일반·지속 피해의 실제 기존 진입점
            Assert.AreEqual(16, fight.First.TakeUnblockedDamage(20, BattleDamageType.None, fight.Enemy)); // 방어도 우회의 별개 양수 피해 경감
            int before = fight.First.CurrentHealth; // 현재 원래 체력의 직접 지불 전 값
            Assert.IsTrue(fight.First.TrySpendHealth(10)); // 방어도와 피해 계산을 쓰지 않는 실제 체력 비용
            Assert.AreEqual(10, before - fight.First.CurrentHealth); // 고정 체력 비용의 원래 수치 유지
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 활성 유물의 다음 실제 적 차례
            Assert.AreEqual(16, PreviewDirect(fight, 20).AppliedDamage); // 아군 차례 뒤에도 지속하는 동일 경감
            var action = new BattleEnemyAction(fight.Enemy, fight.First, EnemyActionType.Attack, BattleDamageType.None, 20, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "share", 1, 1, 1, 0, percentRoll: () => 0, battleType: BattleType.Normal); // 원래 직접 적 한 명의 피해 분담 검사
            Assert.IsTrue(action.SetDamageShare(fight.Other)); // 같은 타격으로 체력을 다른 아군과 나누는 실제 보호
            Assert.AreEqual(8, action.PreviewSharedDamage()); // 이십에서 십육으로 한 번 경감한 공격의 절반
            int beforeShared = fight.Other.CurrentHealth; // 원래 분담자의 실제 체력 저장
            var resolved = action.Execute(); // 치료나 예고가 아닌 원래 적의 실제 공격 진행
            Assert.AreEqual(8, resolved.DamageResult.AppliedDamage); // 실제 첫 아군의 원래 한 번 경감 몫
            Assert.AreEqual(8, beforeShared - fight.Other.CurrentHealth); // 이미 경감된 분담 피해를 다시 줄이지 않는 확인
        } // 첫 적 차례와 다섯 아군 시작 이후 계산 비교 종료
        yield return new ExitPlayMode(); // 기존 Unity 편집기 상태로 실제 복원
    } // 원래 적 피해와 무방어 실제 피해 및 체력 비용 확인 종료

    [UnityTest] // 개별 서른 퍼센트와 실제 양수 피격 경계 및 전투당 한 번 확인
    public IEnumerator AltarChecksOnlyRealHealthLossOrActualOpeningAndOnlyOncePerFight() // 죽은 제외와 비용·예고 및 실제 체력 손실 구분
    { // 원래 파티 체력과 실제 방어도 지급 조건 확인 시작
        yield return new EnterPlayMode(); // 보유 유물과 실제 피해를 실행할 플레이 모드
        using (var owned = new OwnedRelic("SunkenAltarRoot")) // 가라앉은 제단의 뿌리 실제 보유
        using (var fight = new Trial()) // 최대 체력이 이백인 실제 파티
        { // 전투 도중 조건이 맞는 실제 타격과 한 번 지급 검증
            fight.First.ApplyPersistentHealth(62); // 서른 퍼센트보다 두 점 높은 첫 생존 아군
            fight.Other.ApplyPersistentHealth(60); // 정확히 서른 퍼센트인 다른 생존 아군
            fight.Third.ApplyPersistentHealth(0); // 실제 전투불능은 계산 및 보상에서 제외
            Assert.IsTrue(fight.Turn.StartBattle(0)); // 잘못된 개막 지급이 없는 실제 교전 시작
            Assert.AreEqual(0, fight.First.CurrentBlock); // 한 생존 아군이라도 높으면 현재 파티 보호 없음
            Assert.IsTrue(fight.First.TrySpendHealth(1)); // 전투 중 원래 체력 비용 한 점
            Assert.AreEqual(1, PreviewDirect(fight, 1).AppliedDamage); // 정확한 경계에 도착할 원래 순수 피해 예고
            Assert.AreEqual(0, fight.Other.CurrentBlock); // 예고만으로 새 보호막이 생기지 않음
            Assert.AreEqual(1, fight.First.TakeDamage(1, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 첫 아군을 실제 육십 체력으로 줄이는 적 타격
            Assert.AreEqual(60, fight.First.CurrentHealth); // 개별 서른 퍼센트 경계의 실제 체력
            Assert.AreEqual(25, fight.First.CurrentBlock); // 이미 맞은 원래 한 점 뒤 실제 보호막 지급
            Assert.AreEqual(25, fight.Other.CurrentBlock); // 최대 체력 대비 서른 퍼센트인 모든 생존 아군에게 지급
            Assert.AreEqual(0, fight.Third.CurrentBlock); // 기존 전투불능에는 실제 방어도 지급 없음
            fight.First.RemoveBlock(25); // 한 번의 실제 새 보호를 원래 처리로 제거
            fight.Other.RemoveBlock(25); // 다른 아군의 기존 보호도 원래대로 제거
            fight.First.RestoreHealth(10); // 교전 상태의 서른 퍼센트 조건을 다시 들어갈 준비
            fight.First.TakeDamage(10, BattleDamageType.None); // 같은 체력 조건을 두 번째 충족
            Assert.AreEqual(0, fight.First.CurrentBlock); // 전투당 한 번 사용 뒤 반복 보호 없음
            Assert.AreEqual(0, fight.Other.CurrentBlock); // 다른 생존 아군의 재지급도 없음
        } // 전투 도중 조건이 맞는 실제 타격과 한 번 지급 검증 종료
        using (var owned = new OwnedRelic("SunkenAltarRoot")) // 새 실제 교전의 별개 기회 유지
        using (var predator = new OwnedRelic("HollowPredatorStomach")) // 원래 개막 체력 비용을 쓰는 별개 보유 유물
        using (var fight = new Trial()) // 실제 선제 진입과 진짜 체력 비용 확인
        { // 적의 첫 의도 준비에 선행하는 파티 보호 검사
            fight.First.ApplyPersistentHealth(65); // 기존 개막 다섯 점 비용으로 정확히 육십이 될 상태
            fight.Other.ApplyPersistentHealth(65); // 두 번째 실제 생존 아군도 같은 개막 비용
            fight.Third.ApplyPersistentHealth(0); // 개막 보호에서 제외되는 실제 전투불능
            int protectedBeforeIntent = 0; // 실제 첫 적 행동 예고가 조회하는 방어도 값
            Assert.IsTrue(fight.Turn.StartBattle(0, enemyFirst: true, prepareOpeningEnemyIntents: () => protectedBeforeIntent = fight.First.CurrentBlock)); // 선제의 실제 첫 의도 준비와 체력 비용 시점
            Assert.AreEqual(60, fight.First.CurrentHealth); // 원래 비치명 개막 비용 다섯 점 그대로 적용
            Assert.AreEqual(25, protectedBeforeIntent); // 선제를 준비하는 정확한 실제 시점에 다른 아군과 함께 보호
            Assert.AreEqual(25, fight.Other.CurrentBlock); // 같은 개막 조건의 실제 두 번째 수신자
            Assert.AreEqual(0, fight.Third.CurrentBlock); // 개막에서도 전투불능은 제외
        } // 적의 첫 의도 준비에 선행하는 파티 보호 검사 종료
        yield return new ExitPlayMode(); // 실제 에디터 검사 환경 종료
    } // 원래 파티 체력과 실제 방어도 지급 조건 확인 종료

    [UnityTest] // 수호자의 실제 치명 전환과 비용과 다른 체력 손실 확인
    public IEnumerator AltarUsesTheActualGuardianRecipientAndIgnoresOrdinaryHpPayment() // 대신 맞는 실제 수신자와 원래 카드 비용 분리
    { // 실제 보호 전환과 새로운 교전 종료 환경 설정 시작
        yield return new EnterPlayMode(); // 실제 수호자의 심장 카드 상태와 유물리소스 모드
        using (var owned = new OwnedRelic("SunkenAltarRoot")) // 실제 가라앉은 제단의 뿌리 장착
        using (var fight = new Trial()) // 살아 있는 두 보호 참가자와 하나의 기존 적
        { // 대상 전환의 실제 결과와 지급 조건 확인 시작
            fight.First.ApplyPersistentHealth(60); // 보호되는 원래 수신자의 정확한 서른 퍼센트
            fight.Other.ApplyPersistentHealth(61); // 실제 피해를 대신 받을 다른 아군의 시작 체력
            fight.Third.ApplyPersistentHealth(0); // 죽은 참가자의 원래 제외 조건
            Assert.IsTrue(fight.Turn.StartBattle(0)); // 아직 실제 시작 보호가 진행되지 않는 경계
            Assert.IsTrue(fight.Other.TrySpendHealth(1)); // 기존 일반 체력 비용으로 전체 서른 퍼센트 도달
            Assert.AreEqual(0, fight.First.CurrentBlock); // 피해가 아닌 직접 지불에서 보호 없음
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, fight.First.ApplyGuardianHeart(fight.Other)); // 피격 시 기존 실제 수호자 전환 준비
            fight.First.TakeDamage(200, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true); // 처음 실제 치명 공격의 보호자 진행
            Assert.AreEqual(60, fight.First.CurrentHealth); // 원래 선택한 피해 대상 체력 보호
            Assert.AreEqual(1, fight.Other.CurrentHealth); // 실제 수호자의 원래 최소 체력 규칙
            Assert.AreEqual(25, fight.First.CurrentBlock); // 대신 받은 실제 피해 뒤 함께 받은 새 보호
            Assert.AreEqual(25, fight.Other.CurrentBlock); // 실제 살아남은 수호자의 새 보호
        } // 대상 전환의 실제 결과와 지급 조건 확인 종료
        yield return new ExitPlayMode(); // 실제 Unity 환경을 원래 상태로 복원
    } // 실제 보호 전환과 새로운 교전 종료 환경 설정 종료

    private static BattleDamageResult PreviewDirect(Trial fight, int amount) // 원래 실제 적과 같은 입력의 순수 피해 예고
    { // 기존 방어 계산의 내용과 결과 비교 시작
        return fight.First.PreviewDamage(amount, BattleDamageType.None, fight.Enemy, enemyDirectAttack: true); // 상태를 바꾸지 않는 실제 공용 피해 흐름
    } // 기존 방어 계산의 내용과 결과 비교 종료

    private sealed class Trial : IDisposable // 실제 조우 등급과 전투 참가를 준비할 검증용 수명
    { // 기존 실제 피해 환경 구성 시작
        internal readonly CharacterData FirstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 소유자의 실제 캐릭터 형식
        private readonly CharacterData otherData = ScriptableObject.CreateInstance<CharacterData>(); // 다른 소유자를 구분할 캐릭터 형식
        private readonly CharacterData thirdData = ScriptableObject.CreateInstance<CharacterData>(); // 전투불능 아군의 독립 캐릭터 형식
        internal readonly EnemyData EnemyData = ScriptableObject.CreateInstance<EnemyData>(); // 양수 직접 공격의 실제 적 원본
        private readonly CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 전투 런타임의 임시 카드 원본
        private readonly DeckData deckData; // 시작 뒤 정리할 실제 임시 덱
        internal BattleUnitRuntime First { get; } // 앞에 배치된 실제 아군 한 명
        internal BattleUnitRuntime Other { get; } // 같은 광역의 뒤쪽 실제 참가 아군
        internal BattleUnitRuntime Third { get; } // 전투불능의 제외 여부를 확인할 세 번째 아군
        internal BattleUnitRuntime Enemy { get; } // 원본 조우에 참가하는 실제 적
        internal BattleActionPointRuntime Points { get; } = new(3); // 실제 공용 행동력 지불과 감소 기록
        internal BattleTurnRuntime Turn { get; } // 턴과 현재 유물을 연결한 실제 전투 흐름
        internal Trial(BattleType type = BattleType.Normal) // 실제 정상 또는 보스 전투을 구성할 입력
        { // 원래 검증 내용을 위한 실제 전투 구성 시작
            JsonUtility.FromJsonOverwrite("{\"characterId\":\"day289_front\",\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", FirstData); // 고유 소유자와 첫 아군의 체력·방어력
            JsonUtility.FromJsonOverwrite("{\"characterId\":\"day289_guard\",\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", otherData); // 같은 체력을 쓰는 독립 수호 아군
            JsonUtility.FromJsonOverwrite("{\"characterId\":\"day289_fallen\",\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", thirdData); // 같은 체력으로 검증할 독립 전투불능 아군
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"basicAttackPower\":10,\"damageType\":1,\"targetRule\":" + "0" + "}", EnemyData); // 실제 고정 공격과 선택한 원래 대상 규칙
            First = BattleUnitRuntime.CreateAlly(FirstData); // 먼저 맞는 실제 파티의 생존 아군
            Other = BattleUnitRuntime.CreateAlly(otherData); // 두 번째 실제 파티의 생존 아군
            Enemy = BattleUnitRuntime.CreateEnemy(EnemyData); // 실제 적 공격의 원본 개체
            Third = BattleUnitRuntime.CreateAlly(thirdData); // 별도 소유자로 등록한 세 번째 참가자
            var allies = new[] { First, Other, Third }; // 실제로 공격받을 아군의 기존 배치 순서
            var enemies = new[] { Enemy }; // 공격 횟수를 구분할 기본 적 하나
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(cardData, FirstData) }); // 실제 아군 소유자와 임시 덱 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 289); // 자동 드로우에서 기대하는 기존 덱 형식
            Turn = new BattleTurnRuntime(deck, Points, allies, enemies, 0, type); // 실제 출전 참가자와 전투 유형 연결
        } // 원래 검증 내용을 위한 실제 전투 구성 종료
        public void Dispose() // 실제 검사 데이터와 런타임 공격 관리자의 종료
        { // 독립 검증의 원래 상태 복구 시작
            Turn.Dispose(); // 파티마다 연결한 현재 전투 자료 해제
            Object.Destroy(deckData); // 이 검사에서 생성한 임시 덱 정리
            Object.Destroy(cardData); // 실제 원본 파일과 분리해 만든 검사 카드 정리
            Object.Destroy(EnemyData); // 실제 보스 및 일반 전투 검사에 사용한 적 원본 정리
            Object.Destroy(FirstData); // 첫 아군의 독립 원본 정리
            Object.Destroy(otherData); // 두 번째 아군의 독립 원본 정리
            Object.Destroy(thirdData); // 세 번째 아군의 독립 원본 정리
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
