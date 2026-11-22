using System; // 실제 전투 검증 구간과 원본 수명
using System.Collections; // Unity 플레이 모드 시작과 종료 검증
using NUnit.Framework; // 실제 행동력과 피해 및 회복 산술 검증
using UnityEngine; // 새 정식 유물과 참가자 원본 조회
using UnityEngine.TestTools; // 기존 플레이 모드의 런타임 회귀 확인
using Object = UnityEngine.Object; // 실제 검사에서 생성한 원본 객체 해제

public sealed class WinterfallCanyonDefenseAndLossRelicTests // 협곡 방어 유물의 세 실제 런타임 진입점
{ // 명시 행동력 감소와 물리 방어 및 저항 회복 검사 시작
    [UnityTest] // 명시 행동력 감소가 실제 등록 수신자의 방어도에만 적용
    public IEnumerator MudCharmAwardsOnlyTheActualLivingReductionRecipient() // 거인의 방패 출처와 영 적용 및 시간 동전 비교
    { // 실제 공용 행동력 경계별 보상 검증 시작
        yield return new EnterPlayMode(); // 실제 전투 자원과 현재 유물 보유 환경
        using (var mud = new OwnedRelic("MudCharm")) // 실제 진흙 부적의 원본 보유
        using (var anklet = new OwnedRelic("BlackSandAnklet")) // 기존 검은 모래 발목고리의 실제 출처 보존 확인
        using (var fight = new Trial(BattleType.Normal)) // 실제 파티의 공용 행동력과 양쪽 아군 배치
        { // 기존 보통 지불과 실제 정산 때의 개별 방어도 비교
            Assert.IsTrue(fight.Points.Spend(1)); // 정상 카드처럼 현재 공용 행동력 한 점 결제
            Assert.AreEqual(0, fight.First.CurrentBlock); // 일반 비용 결제에는 유물 보호 없음
            fight.Turn.QueueNextPlayerTurnActionPointLoss(fight.First, 3); // 다음 실제 아군 차례에 먼저 받아야 할 명시 감소
            fight.Turn.QueueNextPlayerTurnActionPointLoss(fight.Other, 1); // 이미 잘린 공용량의 명시 감소 요청
            Assert.AreEqual(0, fight.First.CurrentBlock); // 원래 예약의 순수 현재 상태 보존
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 기존 첫 아군 차례 종료
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 같은 실제 기회에 정상 아군 턴과 감소 발생
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 공용 행동력 세 점 실제 감소
            Assert.AreEqual(5, fight.First.CurrentBlock); // 차감 크기와 별개로 실제 첫 당사자에게 방어도 다섯
            Assert.AreEqual(195, fight.First.CurrentHealth); // 발목고리의 고정 체력 피해와 원래 시전자 귀속 보존
            Assert.AreEqual(0, fight.Other.CurrentBlock); // 실제 영 감소인 뒤 요청에 방어도 없음
            Assert.AreEqual(200, fight.Other.CurrentHealth); // 실제 영 감소인 뒤 요청에 피해 없음
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 임시 행동력 하락과 방어도를 정리할 정상 적 단계 진입
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 이전 회차와 다른 정상 아군 시작
            Assert.AreEqual(1, fight.Turn.QueueTimeCoinTurn(2)); // 원래 시간 동전의 당사자 없는 특수 행동력 대가
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 적을 거치지 않고 추가 아군 차례 시작
            Assert.AreEqual(1, fight.Points.CurrentActionPoints); // 정상 최대치 세 점 중 동전 대가 두 점 감소
            Assert.AreEqual(0, fight.First.CurrentBlock); // 시간 동전을 실제 수신자 감소로 귀속하지 않는 확인
            Assert.AreEqual(0, fight.Other.CurrentBlock); // 공용 비용에 다른 아군 방어도 없음
        } // 기존 보통 지불과 실제 정산 때의 개별 방어도 비교 끝
        yield return new ExitPlayMode(); // 원래 회차와 행동력 변경 상태의 종료
    } // 실제 공용 행동력 경계별 보상 검증 종료

    [UnityTest] // 절반 경계의 물리 방어가 체력 변동에 따라 현재 값으로 갱신
    public IEnumerator BasaltFingerAddsPhysicalDefenseAtInclusiveHalfHealth() // 살아 있는 출전 아군의 물리 방어력과 방어도 분리
    { // 실제 체력과 피해 예고 및 적중 비교 시작
        yield return new EnterPlayMode(); // 실제 원래 덱과 체력 정보 시작
        using (var owned = new OwnedRelic("BasaltGiantFinger")) // 화산암 거한의 손가락을 실제 자원에서 획득
        using (var fight = new Trial(BattleType.Normal)) // 물리 방어력 영에서 시작하는 실제 두 출전 아군
        { // 엄격한 절반과 절반 초과 및 전투 밖 비교 시작
            var outside = BattleUnitRuntime.CreateAlly(fight.FirstData); // 같은 원본의 실제 미등록 아군
            Assert.IsTrue(outside.TrySpendHealth(100)); // 활성 전투에 속하지 않은 동일한 절반 체력
            Assert.AreEqual(0, outside.PhysicalDefense); // 같은 유물에도 미등록 아군의 보정 제외
            Assert.IsTrue(fight.First.TrySpendHealth(99)); // 최대 체력 절반보다 정확히 한 점 높은 체력
            Assert.AreEqual(0, fight.First.PhysicalDefense); // 절반 바로 위의 실제 정상 방어력
            Assert.IsTrue(fight.First.TrySpendHealth(1)); // 최대 체력 절반과 같은 현재 체력
            Assert.AreEqual(4, fight.First.PhysicalDefense); // 정수 비율에 정확히 들어온 실제 물리 방어 증가
            Assert.AreEqual(0, fight.First.MagicalResistance); // 원래 마법 저항 값의 보존
            Assert.AreEqual(0, fight.First.CurrentBlock); // 기존 임시 방어도와 물리 방어력의 독립
            Assert.AreEqual(9, fight.First.PreviewDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 현재 물리 방어력 네 점을 사용하는 실제 예고
            Assert.AreEqual(9, fight.First.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 같은 물리 공격의 실제 피해
            Assert.AreEqual(10, fight.First.RestoreHealth(10)); // 다시 절반을 넘는 구간까지 실제 회복
            Assert.AreEqual(0, fight.First.PhysicalDefense); // 회복 직후의 원래 방어력 값 실시간 복원
            Assert.AreEqual(0, fight.Other.PhysicalDefense); // 별개 최대 체력 아군의 방어력 보존
        } // 엄격한 절반과 절반 초과 및 전투 밖 비교 종료
        yield return new ExitPlayMode(); // 실제 체력과 전투 원본 정리
    } // 실제 체력과 피해 예고 및 적중 비교 종료

    [UnityTest] // 마법 피해의 원본 퍼센트포인트와 수신 회복의 독립 곱셈
    public IEnumerator AshSigilSubtractsRawMagicPointsAndFloorsCombinedHealingOnce() // 마법 저항 감소와 합산하고 물리 및 고정 피해 유지
    { // 예고와 피해 정산 및 원래 회복의 수치 비교 시작
        yield return new EnterPlayMode(); // 실제 정식 유물과 일반 전투 환경 시작
        using (var owned = new OwnedRelic("AshSealSigil")) // 재의 봉인문양의 실제 원본 획득
        using (var fight = new Trial(BattleType.Normal)) // 원래 마법 저항과 물리 방어가 영인 실제 파티
        { // 원래 유형별 피해와 두 저항 약화 및 받는 회복 비교 시작
            var outside = BattleUnitRuntime.CreateAlly(fight.FirstData); // 같은 원본의 현재 교전 미등록 아군
            Assert.AreEqual(100, outside.PreviewDamage(100, BattleDamageType.Magical, fight.Enemy).FinalDamage); // 미등록 아군의 원래 마법 피해 보존
            Assert.AreEqual(90, fight.First.PreviewDamage(100, BattleDamageType.Magical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 원래 마법 피해의 십 퍼센트포인트 감소
            Assert.AreEqual(90, fight.Other.PreviewDamage(100, BattleDamageType.Magical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 출전한 다른 생존 아군의 동일 경감
            Assert.AreEqual(100, fight.First.PreviewDamage(100, BattleDamageType.Physical, fight.Enemy).FinalDamage); // 물리 피해의 원래 수치
            Assert.AreEqual(100, fight.First.PreviewDamage(100, BattleDamageType.None, fight.Enemy).FinalDamage); // 고정 유형 피해의 원래 수치
            Assert.AreEqual(18, fight.First.TakeDamage(20, BattleDamageType.Magical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 예고한 동일한 실제 마법 적용
            fight.First.ApplyStatusEffect(BattleStatusEffectType.CorrosionResistanceDown, 20, 2, 1, fight.Enemy); // 원래 독립 실금석 저항 손실
            fight.First.ApplyStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown, 5, 2, 1, fight.Enemy); // 광맥의 별개 퍼센트포인트 손실
            Assert.AreEqual(115, fight.First.PreviewDamage(100, BattleDamageType.Magical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 기존 약화의 합 25에서 방어 10을 뺀 실제 합산
            Assert.AreEqual(23, fight.First.TakeDamage(20, BattleDamageType.Magical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 같은 두 약화의 실제 정수 피해
            Assert.AreEqual(0, fight.First.RestoreHealth(1)); // 회복 감쇠 뒤 원래 영점으로 정수 내림
            Assert.IsTrue(fight.First.TrySpendHealth(50)); // 이후 충분한 원래 회복 여유 확보
            fight.First.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 10, 2, 1); // 이전 수신 회복 증가 10퍼센트
            fight.First.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 20, 2, 1); // 이전 수신 회복 감소 20퍼센트
            Assert.AreEqual(41, fight.First.RestoreHealth(50)); // 50에 110%와 80% 및 95%를 모두 곱한 뒤 한 번 내림
            Assert.IsTrue(outside.TrySpendHealth(25)); // 등록하지 않은 아군의 별개 체력 회복 여유
            Assert.AreEqual(20, outside.RestoreHealth(20)); // 전투 밖의 같은 아군에게 회복 불이익 없음
        } // 원래 유형별 피해와 두 저항 약화 및 받는 회복 비교 종료
        yield return new ExitPlayMode(); // 플레이 모드의 유물과 기존 실행 환경 정리
    } // 예고와 피해 정산 및 원래 회복의 수치 비교 종료

    private sealed class Trial : IDisposable // 실제 조우 등급과 전투 참가를 준비할 검증용 수명
    { // 기존 실제 피해 환경 구성 시작
        internal readonly CharacterData FirstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 소유자의 실제 캐릭터 형식
        private readonly CharacterData otherData = ScriptableObject.CreateInstance<CharacterData>(); // 다른 소유자를 구분할 캐릭터 형식
        internal readonly EnemyData EnemyData = ScriptableObject.CreateInstance<EnemyData>(); // 양수 직접 공격의 실제 적 원본
        private readonly CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 전투 런타임의 임시 카드 원본
        private readonly DeckData deckData; // 시작 뒤 정리할 실제 임시 덱
        internal BattleUnitRuntime First { get; } // 앞에 배치된 실제 아군 한 명
        internal BattleUnitRuntime Other { get; } // 같은 광역의 뒤쪽 실제 참가 아군
        internal BattleUnitRuntime Enemy { get; } // 원본 조우에 참가하는 실제 적
        internal BattleActionPointRuntime Points { get; } = new(3); // 실제 공용 행동력 지불과 감소 기록
        internal BattleTurnRuntime Turn { get; } // 턴과 현재 유물을 연결한 실제 전투 흐름
        internal Trial(BattleType type, bool enemyFirst = false) // 실제 정상 또는 보스 전투을 구성할 입력
        { // 원래 검증 내용을 위한 실제 전투 구성 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", FirstData); // 방어력 없이 충분히 생존하는 첫 아군 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", otherData); // 동일한 두 번째 아군 원래 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"basicAttackPower\":10,\"damageType\":1,\"targetRule\":" + "0" + "}", EnemyData); // 실제 고정 공격과 선택한 원래 대상 규칙
            First = BattleUnitRuntime.CreateAlly(FirstData); // 먼저 맞는 실제 파티의 생존 아군
            Other = BattleUnitRuntime.CreateAlly(otherData); // 두 번째 실제 파티의 생존 아군
            Enemy = BattleUnitRuntime.CreateEnemy(EnemyData); // 실제 적 공격의 원본 개체
            var allies = new[] { First, Other }; // 실제로 공격받을 아군의 기존 배치 순서
            var enemies = new[] { Enemy }; // 공격 횟수를 구분할 기본 적 하나
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(cardData, FirstData) }); // 실제 아군 소유자와 임시 덱 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 287); // 자동 드로우에서 기대하는 기존 덱 형식
            Turn = new BattleTurnRuntime(deck, Points, allies, enemies, 0, type); // 실제 출전 참가자와 전투 유형 연결
            Assert.IsTrue(Turn.StartBattle(0, enemyFirst)); // 원본 선제 상태에 따라 유물 보유 및 실제 차례 초기화
        } // 원래 검증 내용을 위한 실제 전투 구성 종료
        public void Dispose() // 실제 검사 데이터와 런타임 공격 관리자의 종료
        { // 독립 검증의 원래 상태 복구 시작
            Turn.Dispose(); // 파티마다 연결한 현재 전투 자료 해제
            Object.Destroy(deckData); // 이 검사에서 생성한 임시 덱 정리
            Object.Destroy(cardData); // 실제 원본 파일과 분리해 만든 검사 카드 정리
            Object.Destroy(EnemyData); // 실제 보스 및 일반 전투 검사에 사용한 적 원본 정리
            Object.Destroy(FirstData); // 첫 아군의 독립 원본 정리
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
