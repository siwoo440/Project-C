using System; // 실제 전투 결과와 검증 객체 정리
using System.Collections; // 플레이 모드 진입과 종료의 검사 프레임
using NUnit.Framework; // 실제 피해 및 예고 값 비교
using UnityEngine; // 신규 유물 원본과 독립 캐릭터 및 적 생성
using UnityEngine.TestTools; // 실제 게임 처리 경로의 플레이 모드 확인
using Object = UnityEngine.Object; // 검사에서 생성한 데이터와 관리자 정리

public sealed class WinterfallAbyssDefensiveRelicTests // 심연 방어 유물의 실제 단계와 피해 예고 회귀 검사
{ // 전투 방어 유물 검사 시작
    [UnityTest] // 보스전 첫 실제 적 턴 두 번의 보호와 추가 아군 차례 확인
    public IEnumerator HangarProtectsTwoRealBossEnemyPhasesIncludingAmbush() // 실제 보스 선제와 시간 동전의 턴 분리
    { // 보호 조건의 실제 피해 및 턴 비교 시작
        yield return new EnterPlayMode(); // 실제 보스전 유물 환경 진입
        using (var owned = new OwnedRelic("HangarSeal")) // 정식 격납고 봉인장치 실제 획득
        using (var fight = new Trial(BattleType.Boss, enemyFirst: true)) // 첫 행동이 적의 실제 선제 보스전
        { // 보스의 두 실제 차례 전후 예고 확인 시작
            var ally = fight.First; // 첫 실제 보스 피해를 맞을 아군
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turn.CurrentPhase); // 실제 선제 적 차례 시작 확인
            ally.GainBlock(3); // 피해 경감보다 뒤에서 흡수할 실제 기존 방어도
            var preview = ally.PreviewDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true); // 같은 보스 피해의 순수 방어 예고
            var current = ally.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true); // 정식 적 직접 피해 한 건 정산
            Assert.AreEqual(8, current.FinalDamage); // 기존 피해 수치 열에 20% 보스 경감
            Assert.AreEqual(5, current.AppliedDamage); // 그 뒤에 방어도 세 점 흡수
            Assert.AreEqual(current.AppliedDamage, preview.AppliedDamage); // 같은 보호와 방어도를 사용하는 예고
            int before = ally.CurrentHealth; // 실제 공격과 별개인 정해진 체력 지불의 시작값
            Assert.IsTrue(ally.TrySpendHealth(10)); // 고정 아군 비용은 실제 보스 피격과 별도
            Assert.AreEqual(10, before - ally.CurrentHealth); // 보호 없이 원래 열 점을 지불
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 첫 실제 적 선제 차례 완료
            Assert.AreEqual(1, fight.Turn.QueueTimeCoinTurn(2)); // 실제 시간 동전의 추가 아군 턴 예약
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 적을 시작하지 않는 실제 추가 아군 턴
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turn.CurrentPhase); // 정상 적 차례를 소모하지 않은 경계 확인
            Assert.AreEqual(8, ally.PreviewDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 추가 아군 차례에 보스 방어가 유지
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 두 번째 실제 적 행동 시작
            Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turn.CurrentPhase); // 추가 아군 턴 뒤 정상 적 차례 확인
            Assert.AreEqual(8, ally.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 두 번째 실제 적 턴까지 경감
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 두 번째 실제 적 차례 종료
            Assert.AreEqual(10, ally.PreviewDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 완료된 두 적 턴 뒤 정식 피해 복구
        } // 보스의 두 실제 차례 전후 예고 확인 종료
        using (var owned = new OwnedRelic("HangarSeal")) // 일반전에서 같은 유물 보유
        using (var fight = new Trial(BattleType.Normal)) // 보스 유형을 갖지 않는 실제 별도 전투
        { // 일반 적 피해에서 보스 보호 제외 확인 시작
            Assert.AreEqual(10, fight.First.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 일반 전투에 원래 피해 유지
        } // 일반 적 피해에서 보스 보호 제외 확인 종료
        yield return new ExitPlayMode(); // 실제 보스전 원본 상태 복원
    } // 보호 조건의 실제 피해 및 턴 비교 종료

    [UnityTest] // 아군 화상만 한 점 줄이고 폐열 강화 조건을 보존
    public IEnumerator CharredCordReducesOnlyGenuineAllyBurnAndKeepsRadiatorGatedOnLoss() // 영 하한과 원래 화상 피해 및 강화 확인
    { // 실제 화상과 피해 제외 대상 비교 시작
        yield return new EnterPlayMode(); // 원래 화상 처리와 실제 유물 보유 환경
        using (var cord = new OwnedRelic("CharredStoneCord")) // 정식 그을린 돌끈 획득
        using (var radiator = new OwnedRelic("WasteHeatRadiator")) // 원래 실제 체력 피해 뒤 폐열 강화의 상호작용
        using (var fight = new Trial(BattleType.Normal)) // 다른 종류가 아닌 실제 일반 전투
        { // 아군·적의 실제 화상 결과 확인 시작
            var processor = new BattleStatusEffectProcessor(); // 기존 턴 종료의 실제 화상 처리기
            fight.First.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1, fight.Enemy); // 정수 경계가 영인 아군 화상
            fight.Other.ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 2, 1, fight.Enemy); // 양수 실제 체력 피해가 남는 화상
            fight.Enemy.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 2, 1, fight.First); // 유물 감소 범위 밖 적 화상
            processor.ProcessPhaseEnd(fight.First, 1); // 한 점 아군 화상에 영 하한 적용
            processor.ProcessPhaseEnd(fight.Other, 1); // 세 점 중 한 점만 줄인 아군 화상
            processor.ProcessPhaseEnd(fight.Enemy, 1); // 원래 적 화상 틱 적용
            Assert.AreEqual(200, fight.First.CurrentHealth); // 영 체력 피해 확인
            Assert.IsNull(fight.First.GetStatusEffect(BattleStatusEffectType.WasteHeatAttackPowerUp)); // 체력 영 손실에서 폐열 발동 제외
            Assert.AreEqual(198, fight.Other.CurrentHealth); // 실제 두 점만 아군에게 적용
            Assert.AreEqual(199, fight.Enemy.CurrentHealth); // 적에게는 원래 한 점 전부 적용
            fight.First.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, fight.Enemy); // 다른 원래 정기 피해
            processor.ProcessPhaseEnd(fight.First, 2); // 실제 다음 턴의 출혈 처리
            Assert.AreEqual(199, fight.First.CurrentHealth); // 아군 출혈이 영향을 받지 않는지 확인
        } // 아군·적의 실제 화상 결과 확인 종료
        yield return new ExitPlayMode(); // 유물 원본을 반영한 플레이 모드 검사 종료
    } // 실제 화상과 피해 제외 대상 비교 종료

    [UnityTest] // 첫 원래 실제 직격의 공용 방어와 광역 배치 순서 예고
    public IEnumerator BasaltOnlySpendsOnTheFirstRealDirectImpactAcrossTheParty() // 회피·무효 이후 광역에 한 번만 적용
    { // 실제 한 번의 공용 타격 방어 검사 시작
        yield return new EnterPlayMode(); // 실제 적 행동과 예정 피해를 비교할 환경 시작
        using (var owned = new OwnedRelic("BlackBasaltShard")) // 정식 검은 현무암 조각의 실제 보유
        using (var fight = new Trial(BattleType.Normal, enemyFirst: true, area: true)) // 두 참가 아군을 가진 실제 적 선제 광역
        { // 무효가 진행되지 않는 첫 공용 피격 검증 시작
            Assert.AreEqual(1, fight.First.TakeDamage(1, BattleDamageType.None, fight.Enemy).AppliedDamage); // 직접 출처 없는 일반 피해가 원래 한 점 적용
            Assert.AreEqual(0, fight.First.TakeDamage(0, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 양수 원래 피해가 없는 직접 입력
            fight.Other.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 실제 먼저 실패할 일반 적 공격
            var miss = new BattleEnemyAction(fight.Enemy, fight.Other, EnemyActionType.Attack, BattleDamageType.Physical, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "miss", 1, 1, 1, 0, percentRoll: () => 99, battleType: BattleType.Normal); // 원래 명중 절차에서 확정 빗나갈 적 공격
            Assert.IsTrue(miss.Execute().WasMissed); // 실제 빗나감으로 공용 기회 보존
            fight.Other.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 이후 정상 양수 피해의 광역 대상 원복
            fight.First.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 앞선 기존 피해 무효 보호 설정
            Assert.IsTrue(fight.First.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).WasDamageNullified); // 실제 기존 무효 뒤 공용 차감 기회 보존
            fight.First.GainBlock(10); // 보호 후에도 전부 막을 실제 첫 아군의 방어도
            Assert.AreEqual(1, fight.Actions.PrepareActions()); // 같은 실제 광역 적 행동을 예정 목록에 등록
            var attack = fight.Actions.PlannedActions[0]; // 실제 생존 순서가 고정된 그 광역 공격
            Assert.IsTrue(attack.IsAreaAttack); // 단일 예고 대신 실제 광역 예정 확인
            Assert.AreEqual(7, attack.PreviewDamage(fight.First).FinalDamage); // 첫 적격 광역 명중의 공용 세 점 경감
            Assert.AreEqual(10, attack.PreviewDamage(fight.Other).FinalDamage); // 다음 실제 광역 대상에게 공용 경감 복제 제외
            Assert.AreEqual(7, attack.PreviewDamage(fight.First).FinalDamage); // 반복 예고에 실제 경감 소모 없음
            fight.Actions.ExecuteAction(attack); // 같은 광역 계획의 실제 명중 순서 실행
            Assert.AreEqual(7, attack.AreaHitResults[0].Result.DamageResult.AbsorbedDamage); // 방어도까지의 양수 최초 피격에서 공용 사용 확정
            Assert.AreEqual(3, fight.First.CurrentBlock); // 실제 첫 피격에서 줄어든 방어도
            Assert.AreEqual(10, attack.AreaHitResults[1].Result.DamageResult.AppliedDamage); // 같은 공격 두 번째 아군은 원래 피해 적용
            Assert.AreEqual(10, fight.Other.PreviewDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 실제 소모 뒤 더 이상 반복 경감 없음
        } // 무효가 진행되지 않는 첫 공용 피격 검증 종료
        using (var owned = new OwnedRelic("BlackBasaltShard")) // 별도 전투의 같은 원래 일회성 효과 비교
        using (var fight = new Trial(BattleType.Normal)) // 이전 소모 기록이 없어야 할 독립 조우
        { // 새 실제 전투의 영 피해와 공용 소비 확인 시작
            Assert.AreEqual(0, fight.First.TakeDamage(2, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).FinalDamage); // 자체 차감만으로 영인 첫 양수 직격
            Assert.AreEqual(10, fight.Other.TakeDamage(10, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true).AppliedDamage); // 처음 차감으로 영인 경우에도 한 번 소모
        } // 새 실제 전투의 영 피해와 공용 소비 확인 종료
        yield return new ExitPlayMode(); // 실제 적 행동을 사용한 플레이 모드 검사 복구
    } // 실제 한 번의 공용 타격 방어 검사 종료

    private sealed class Trial : IDisposable // 실제 조우 등급과 전투 참가를 준비할 검증용 수명
    { // 기존 실제 피해 환경 구성 시작
        private readonly CharacterData firstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 소유자의 실제 캐릭터 형식
        private readonly CharacterData otherData = ScriptableObject.CreateInstance<CharacterData>(); // 다른 소유자를 구분할 캐릭터 형식
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 양수 직접 공격의 실제 적 원본
        private readonly CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 전투 런타임의 임시 카드 원본
        private readonly DeckData deckData; // 시작 뒤 정리할 실제 임시 덱
        internal BattleUnitRuntime First { get; } // 앞에 배치된 실제 아군 한 명
        internal BattleUnitRuntime Other { get; } // 같은 광역의 뒤쪽 실제 참가 아군
        internal BattleUnitRuntime Enemy { get; } // 원본 조우에 참가하는 실제 적
        internal BattleTurnRuntime Turn { get; } // 턴과 현재 유물을 연결한 실제 전투 흐름
        internal BattleEnemyActionRuntime Actions { get; } // 실제 예정 공격의 예고 순서와 실행
        internal Trial(BattleType type, bool enemyFirst = false, bool area = false) // 실제 정상 또는 보스 전투을 구성할 입력
        { // 원래 검증 내용을 위한 실제 전투 구성 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", firstData); // 방어력 없이 충분히 생존하는 첫 아군 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", otherData); // 동일한 두 번째 아군 원래 수치
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"basicAttackPower\":10,\"damageType\":1,\"targetRule\":" + (area ? "3" : "0") + "}", enemyData); // 실제 고정 공격과 선택한 원래 대상 규칙
            First = BattleUnitRuntime.CreateAlly(firstData); // 먼저 맞는 실제 파티의 생존 아군
            Other = BattleUnitRuntime.CreateAlly(otherData); // 두 번째 실제 파티의 생존 아군
            Enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 적 공격의 원본 개체
            var allies = new[] { First, Other }; // 실제로 공격받을 아군의 기존 배치 순서
            var enemies = new[] { Enemy }; // 공격 횟수를 구분할 기본 적 하나
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(cardData, firstData) }); // 실제 아군 소유자와 임시 덱 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 285); // 자동 드로우에서 기대하는 기존 덱 형식
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, type); // 실제 출전 참가자와 전투 유형 연결
            Actions = new BattleEnemyActionRuntime(enemies, allies, Turn); // 예정 및 실제 피해 계산에 같은 활성 전투 연결
            Assert.IsTrue(Turn.StartBattle(0, enemyFirst)); // 원본 선제 상태에 따라 유물 보유 및 실제 차례 초기화
        } // 원래 검증 내용을 위한 실제 전투 구성 종료
        public void Dispose() // 실제 검사 데이터와 런타임 공격 관리자의 종료
        { // 독립 검증의 원래 상태 복구 시작
            Actions.Dispose(); // 실제 공격 예고와 참가 유닛 알림 해제
            Turn.Dispose(); // 파티마다 연결한 현재 전투 자료 해제
            Object.Destroy(deckData); // 이 검사에서 생성한 임시 덱 정리
            Object.Destroy(cardData); // 실제 원본 파일과 분리해 만든 검사 카드 정리
            Object.Destroy(enemyData); // 실제 보스 및 일반 전투 검사에 사용한 적 원본 정리
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
