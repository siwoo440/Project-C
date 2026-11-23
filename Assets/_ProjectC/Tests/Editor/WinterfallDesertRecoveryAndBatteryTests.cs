using System; // 네이티브 회복 신호 검증과 검사 정리
using System.Collections; // 실제 플레이 모드 전환
using System.Linq; // 전투용 원본과 캐릭터 명단 선택
using NUnit.Framework; // 행동력과 체력의 실제 값 비교
using UnityEngine; // 배포 리소스와 적 원본 준비
using UnityEngine.TestTools; // Unity 플레이 검사 등록
using Object = UnityEngine.Object; // 임시 자산의 실제 Unity 정리

public sealed class WinterfallDesertRecoveryAndBatteryTests // 사막 유물의 회복과 행동력 양면 거래 검사
{ // 두 유물의 실제 전투 검사 시작
    [UnityTest] // 첫 부상 이후의 정상 실제 아군 턴과 회복 검사
    public IEnumerator LastWaterDropWaitsForInjuryAndWeakensHealingAfterWholePulse() // 온전한 첫 턴 뒤 실제 회복과 종료 정리 검증
    { // 지연 회복 검사 시작
        yield return new EnterPlayMode(); // 배포 유물과 전투 런타임 환경 진입
        Acquire("DesertLastWaterDrop"); // 사막의 마지막 물방울 원본 획득
        using (var fight = new Fight()) // 두 생존 아군과 하나의 전투불능 대상 준비
        { // 물방울 실제 대기 처리 검사 시작
            var first = fight.Allies[0]; // 물방울의 첫 생존 회복 대상
            var second = fight.Allies[1]; // 첫 대상 이후에도 보정 전 회복을 받을 아군
            var down = fight.Allies[2]; // 대기 조건과 치료에서 제외할 아군
            down.ApplyPersistentHealth(0); // 쓰러진 채 등장한 아군 설정
            Assert.IsTrue(fight.Turn.StartBattle(1)); // 생존자가 전원 최대 체력인 실제 첫 아군 차례
            Assert.AreEqual(0, down.CurrentHealth); // 초기 부활과 효과 소모 없음 확인
            first.ApplyPersistentHealth(first.MaxHealth - 40); // 현재 실제 턴 중의 첫 부상 유닛
            second.ApplyPersistentHealth(second.MaxHealth - 40); // 첫 회복 전 두 번째 부상 유닛
            int firstBefore = first.CurrentHealth; // 첫 아군의 시작 체력 보관
            int secondBefore = second.CurrentHealth; // 두 번째 아군의 시작 체력 보관
            Assert.AreEqual(10, first.RestoreHealth(10)); // 실제 발동 전 원래 양수 회복 확인
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 부상자가 생긴 아군 턴 종료
            Assert.AreEqual(firstBefore + 10, first.CurrentHealth); // 적 턴 진입 시 아직 물방울 미발동
            Assert.AreEqual(secondBefore, second.CurrentHealth); // 아직 광역 회복 없음
            Action<BattleUnitRuntime, int> duringPulse = (_, amount) => // 회복 처리 중 별개 치유의 적용 시점 검사
            { // 첫 대상의 실제 회복 신호 확인 시작
                Assert.AreEqual(20, amount); // 첫 유물 회복의 실제 스무 점 확인
                Assert.AreEqual(5, second.RestoreHealth(5)); // 전체 회복 전에는 별개 치유 감소 없음
            }; // 첫 대상의 실제 회복 신호 확인 종료
            first.HealthRestored += duringPulse; // 첫 정상 회복의 실제 조건 연결
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 부상자가 처음 있는 실제 아군 시작
            first.HealthRestored -= duringPulse; // 한 차례만 필요한 검사 신호 해제
            Assert.AreEqual(firstBefore + 30, first.CurrentHealth); // 실제 일반 회복과 물방울의 전체 스무 점 확인
            Assert.AreEqual(secondBefore + 25, second.CurrentHealth); // 첫 알림의 별개 다섯 점과 두 번째 스무 점 확인
            Assert.AreEqual(0, down.CurrentHealth); // 쓰러진 아군은 여전히 전투불능
            Assert.AreEqual(8, first.RestoreHealth(10)); // 정상 전체 회복 뒤 대상과 상관없는 이십 퍼센트 감소
            second.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 25, 3, 1); // 원래 받는 회복 증가와 새 감소 결합
            Assert.AreEqual(3, second.RestoreHealth(3)); // 증가와 감소를 먼저 곱한 뒤 내림하는 기존 정확도
            second.RemoveStatusEffect(BattleStatusEffectType.HealingReceivedUp); // 후속 차례 검사를 위한 증가 제거
            second.ApplyPersistentHealth(second.MaxHealth - 40); // 이후 아군 차례의 새 부상 설정
            int laterHealth = second.CurrentHealth; // 다음 차례 자동 재발동 검사 기준
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 물방울이 이미 발동한 실제 아군 종료
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 다음 실제 아군 시작 도착
            Assert.AreEqual(laterHealth, second.CurrentHealth); // 새 부상이 있어도 자동 회복 재발동 없음
            Assert.AreEqual(8, second.RestoreHealth(10)); // 이십 퍼센트 감소는 이후 차례에도 유지
            Assert.IsTrue(fight.Turn.TryEscape()); // 현재 조우를 실제로 종료
            Assert.AreEqual(10, second.RestoreHealth(10)); // 끝난 교전 밖에서는 해당 회복 거래 제거
        } // 물방울 실제 대기 처리 검사 종료
        yield return new ExitPlayMode(); // 검사 중 획득한 실제 유물 환경 정리
    } // 지연 회복 검사 종료

    [UnityTest] // 적 선제에서 최초 아군 개막 보상 검사
    public IEnumerator LastWaterDropFirstWoundedAllyStartWorksWithOrWithoutAmbush() // 전투에 이미 부상한 아군과 적 회복 대조
    { // 선제 물방울 검사 시작
        yield return new EnterPlayMode(); // 리소스가 있는 실제 유니티 환경 시작
        Acquire("DesertLastWaterDrop"); // 공식 배포 물방울 원본 획득
        foreach (bool enemyFirst in new[] { false, true }) // 일반 개막과 적 선제의 첫 실제 아군 시작 대조
        { // 선제와 첫 아군 차례의 경계 확인 시작
            using var fight = new Fight(); // 실제 두 진영의 조우와 부상 아군 구성
            var ally = fight.Allies[0]; // 탐사 중 다친 채 개막한 실제 아군
            ally.ApplyPersistentHealth(ally.MaxHealth - 40); // 처음부터 남은 충분한 회복 구간
            int baseline = ally.CurrentHealth; // 적 선제 전 부상 체력
            Assert.IsTrue(fight.Turn.StartBattle(1, enemyFirst)); // 지정한 첫 진영의 실제 전투 시작
            if (enemyFirst) // 실제 첫 아군 시작이 아직 오지 않은 전투 확인
            { // 선제 중 아군 회복 없음 확인 시작
                Assert.AreEqual(baseline, ally.CurrentHealth); // 선제에는 아직 아군 회복 없음
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 첫 실제 아군 시작 도착
            } // 선제 중 아군 회복 없음 확인 종료
            Assert.AreEqual(baseline + 20, ally.CurrentHealth); // 탐사 부상에도 즉시 전체 회복 적용
            Assert.AreEqual(8, ally.RestoreHealth(10)); // 선제 뒤 아군의 회복 감소 활성 확인
            var enemy = fight.Enemies[0]; // 같은 전투의 실제 상대 진영 테스트 대상
            enemy.TakeDamage(25, BattleDamageType.None); // 적의 비교용 정상 부상
            Assert.AreEqual(10, enemy.RestoreHealth(10)); // 상대 진영의 수신 회복에는 효과 없음
        } // 선제와 첫 아군 차례의 경계 확인 종료
        yield return new ExitPlayMode(); // 다음 검사에 독립 환경 전달
    } // 선제 물방울 검사 종료

    [UnityTest] // 첫 두 실제 아군 턴의 양수와 음수 행동력 거래 검사
    public IEnumerator UsedBatteryShardAdjustsFirstTwoActualAllyTurnsAndExpires() // 적 선제와 일반 조우에서 임시 상한 경계 검증
    { // 배터리 조각 검사 시작
        yield return new EnterPlayMode(); // 실제 플레이와 보유 유물 환경 진입
        Acquire("UsedBatteryShard"); // 공식 소모된 배터리 조각 원본 획득
        foreach (bool enemyFirst in new[] { false, true }) // 일반과 선제 두 진입 흐름 대조
        { // 각 교전의 실제 행동력 검사 시작
            using var fight = new Fight(); // 기본 최대 행동력 삼의 독립 실제 전투
            Assert.IsTrue(fight.Turn.StartBattle(1, enemyFirst)); // 원하는 진영 순서의 실제 전투 시작
            if (enemyFirst) // 아군 차례가 아직 오지 않은 적 선제 검사
            { // 적 선제 행동력 기준 확인 시작
                Assert.AreEqual(3, fight.Points.MaxActionPoints); // 적 선제에 조각으로 늘지 않는 원래 최대치
                Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 실제 입력 없이 비활성화한 선제 행동력
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 적 선제 이후 첫 실제 아군 차례 시작
            } // 적 선제 행동력 기준 확인 종료
            Assert.AreEqual(4, fight.Points.MaxActionPoints); // 첫 아군 턴만 원본 최대치에서 한 점 증가
            Assert.AreEqual(4, fight.Points.CurrentActionPoints); // 첫 아군 턴의 실제 가용 행동력 증가
            Assert.IsTrue(fight.Points.Spend(1)); // 기능을 읽는 UI와 실제 소비를 분리할 한 점 사용
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 현재 표시 조회의 보상 중복 적용 없음
            Assert.AreEqual(4, fight.Points.MaxActionPoints); // 현재 표시를 읽어도 최대치 그대로 유지
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 첫 번째 아군 턴 종료
            Assert.AreEqual(3, fight.Points.MaxActionPoints); // 첫 임시 행동력은 적 턴까지 이월되지 않음
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 두 번째 실제 아군 턴 도착
            Assert.AreEqual(2, fight.Points.MaxActionPoints); // 두 번째 실제 턴의 임시 최대치 한 점 감소
            Assert.AreEqual(2, fight.Points.CurrentActionPoints); // 기본 회복 이후 가용 행동력에서 한 점 차감
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 둘째 실제 아군 턴 종료
            Assert.AreEqual(3, fight.Points.MaxActionPoints); // 음의 임시 상한 복원 확인
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 세 번째 실제 아군 턴 도착
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 한 차례 대가 이후 일반 회복
            Assert.AreEqual(3, fight.Points.MaxActionPoints); // 이후 아군 턴에 배터리 거래 반복 없음
        } // 각 교전의 실제 행동력 검사 종료
        using (var fight = new Fight()) // 교전 결과 없이 떠날 때 임시 행동력을 확인할 별도 전투
        { // 실제 첫 턴에서 떠나는 검사 시작
            Assert.IsTrue(fight.Turn.StartBattle(1)); // 임시 행동력 보상이 있는 실제 첫 아군 턴
            Assert.AreEqual(4, fight.Points.MaxActionPoints); // 전투 해제 직전 조각의 실제 증가
            fight.Turn.Dispose(); // 조우의 결과를 남기지 않는 실제 전투 관리자 해제
            Assert.AreEqual(3, fight.Points.MaxActionPoints); // 교전 해제 후 기존 최대 행동력 복원
        } // 실제 첫 턴에서 떠나는 검사 종료
        yield return new ExitPlayMode(); // 유물 보유와 전투 수명 정리
    } // 배터리 조각 검사 종료

    private static void Acquire(string key) // 실제 배포 원본과 일반 인벤토리 획득
    { // 유물 획득 검사 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 실제 리소스 경로로 읽는 유물
        Assert.IsNotNull(relic, key); // 실제 Unity 원본 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 원본 유물의 일반 회차 획득
    } // 유물 획득 검사 종료

    private sealed class Fight : IDisposable // 실제 턴과 원래 전투 자원을 직접 생성하는 회귀 환경
    { // 전투 구성 시작
        private readonly DeckData source; // 전투에서 사용하는 기존 카드 덱 원본
        private readonly EnemyData enemyData; // 독립 검사에서 사용하는 임시 적 원본
        private readonly BattleStatusEffectController statuses; // 실제 진영의 상태 지속 연결
        internal readonly BattleUnitRuntime[] Allies; // 진영 피격과 회복의 원본 아군
        internal readonly BattleUnitRuntime[] Enemies; // 상대 진영의 원본 적
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(3); // 실제 임시 최대치 변화 비교에 쓰는 원래 행동력
        internal readonly BattleTurnRuntime Turn; // 선제와 턴 변경 및 전투 결과를 실제 실행할 관리자

        internal Fight() // 유물에서 필요한 전투 수명의 실제 초기 구성
        { // 아군과 적 및 덱 생성 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 배포 원본 아군 명단
            Allies = roster.Characters.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 아군과 다운 비교용 세 명 생성
            foreach (var ally in Allies) // 회복 여유가 같은 검사 체력 기준
            { // 아군별 실제 최대 체력 조정 시작
                ally.ModifyMaxHealth(100 - ally.MaxHealth); // 독립 원본에 백 점 비교 최대치 적용
                ally.ApplyPersistentHealth(100); // 모두 최대치에서 개별 부상 검사 준비
            } // 아군별 실제 최대 체력 조정 종료
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 실제 조우를 대신할 기본 적 원본
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemyData); // 실제 피해와 회복 비교 구간 설정
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData) }; // 전투 결과의 원래 조건에 맞춘 적 생성
            var thrust = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 초반 사용 가능한 실제 카드 원본
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(thrust, Allies[0].CharacterSource) }); // 실제 아군의 고정 전투 덱
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 278); // 실제 아군 손패 드로우 경계를 사용하는 덱
            Turn = new BattleTurnRuntime(deck, Points, Allies, Enemies, 1, BattleType.Normal); // 일반 조우의 원래 행동력 회복과 결과 연결
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 원래 상태 지원과 턴의 경계 연결
        } // 아군과 적 및 덱 생성 종료

        public void Dispose() // 실제 전투의 등록 및 임시 원본 정리
        { // 검사 환경 정리 시작
            statuses.Dispose(); // 진영 상태 처리의 기존 이벤트 해제
            Turn.Dispose(); // 유물과 전투 유닛 연결 정리
            Object.Destroy(source); // 임시 카드 덱 원본 해제
            Object.Destroy(enemyData); // 임시 적 원본 해제
        } // 검사 환경 정리 종료
    } // 전투 구성 종료
} // 두 유물의 실제 전투 검사 종료
