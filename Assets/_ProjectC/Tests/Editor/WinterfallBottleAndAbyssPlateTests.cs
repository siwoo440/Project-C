using System; // 독립 전투 자료와 턴 관리자 연결 해제
using System.Collections; // 실제 플레이 모드의 전투 위치 초기화 대기
using System.Linq; // 서로 다른 아군 원본 자료 구성
using NUnit.Framework; // 첫 행동력과 정신 피해 결과 검증
using UnityEngine; // 실제 보유 유물과 임시 전투 원본 사용
using UnityEngine.TestTools; // 도메인 전환 이후 실제 전투 검사 실행
using Object = UnityEngine.Object; // 생성한 Unity 원본의 유효한 해제

public sealed class WinterfallBottleAndAbyssPlateTests // 바닥 없는 병과 검은 심연판의 전투 경계 검사
{ // 개막 유물 회귀 검사 시작
    [UnityTest] // 적 선제 유무별 실제 전투 첫 행동력 검사
    public IEnumerator BottlePaysBeforeOpeningActionsAndStacksOnlyInFirstAllyTurn() // 시작 비용과 실제 첫 턴의 일시 행동력 검증
    { // 실제 전투 환경 진입 시작
        yield return new EnterPlayMode(); // 기존 유물 회차와 덱 엔진이 동작하는 플레이 모드 시작
        VerifyBottle(); // 선제 턴과 빈 첫 손패를 각각 사용한 실제 전투 흐름 확인
        yield return new ExitPlayMode(); // 생성물을 정리한 뒤 편집 모드 복귀
    } // 실제 전투 환경 진입 종료

    private static void VerifyBottle() // 전투 시작 정신력 비용과 첫 행동력 적용의 순서 확인
    { // 바닥 없는 병 결과 검증 시작
        Acquire("BottomlessBottle"); // 실제 시트에서 연결한 바닥 없는 병 유물 획득
        Acquire("TornEnvelope"); // 검 카드 첫 손패의 기존 임시 행동력 유물 획득
        foreach (bool ambush in new[] { false, true }) // 선제 없음과 있음의 현재 진영 차이를 실제로 비교
        { // 전투 진입 방식별 동작 검사 시작
            using var battle = new OpeningBattle(); // 첫 손패 여부를 바꿀 수 있는 같은 전투 자료 준비
            var low = battle.Allies[1]; // 정신력 네 점 비용보다 적은 실제 생존 아군
            var fallen = battle.Allies[2]; // 체력 영으로 시작 비용에서 제외할 아군
            Assert.IsTrue(low.ApplyPersistentMental(2)); // 정상 비용에 필요한 정신력 하한 검증 상태 설정
            Assert.IsTrue(fallen.ApplyPersistentHealth(0)); // 개막 비용에서 제외할 전투불능 상태 설정
            int drawCapacity = -1; // 첫 정상 카드 도착 시점의 임시 행동력 상한 기록
            battle.Deck.StateChanged += () => // 카드 영역의 실제 첫 드로우 결과 알림
            { // 첫 손패에 도착한 행동력 기록 시작
                if (drawCapacity < 0 && battle.Deck.HandCount > 0) // 기존 편지 효과보다 앞선 첫 정상 카드 도착 확인
                { // 개막 드로우의 상한 보관 시작
                    drawCapacity = battle.Points.MaxActionPoints; // 해당 턴이 실제 준비된 기본값과 병의 증가 합계
                } // 개막 드로우의 상한 보관 종료
            }; // 첫 손패에 도착한 행동력 기록 종료
            Assert.IsTrue(battle.Turns.StartBattle(ambush ? 1 : 0, ambush, () => // 실제 첫 행동 예고를 준비하는 기존 개막 호출
            { // 실제 선제 행동 준비 전 검사 시작
                Assert.AreEqual(46, battle.Allies[0].CurrentMental); // 첫 행동 준비가 시작되기 전 생존 아군의 원본 네 점 비용 확인
                Assert.Zero(low.CurrentMental); // 한 번의 정신 비용이 영 하한을 넘어가지 않음 확인
                Assert.AreEqual(50, fallen.CurrentMental); // 전투불능 아군의 기존 정신력이 유지되는지 확인
                Assert.AreEqual(3, battle.Points.MaxActionPoints); // 실제 아군 턴 도착 전 임시 상한이 없는지 확인
                Assert.Zero(battle.Deck.HandCount); // 코스트를 적용하기 전 실제 첫 손패가 비어 있음을 확인
                Assert.IsFalse(battle.Turns.StartBattle(1)); // 같은 개막 중 다시 들어온 시작이 비용을 중복 차감하지 않음 확인
            })); // 실제 선제 행동 준비 전 검사 종료
            if (ambush) // 적의 첫 실제 행동이 먼저 진행되는 경우 확인
            { // 적 선제의 행동력 경계 검사 시작
                Assert.AreEqual(BattleTurnPhase.EnemyTurn, battle.Turns.CurrentPhase); // 적 선제의 실제 전투 턴 확인
                Assert.AreEqual(3, battle.Points.MaxActionPoints); // 선제 중 병의 일시 상한이 아직 반영되지 않았음 확인
                Assert.Zero(battle.Points.CurrentActionPoints); // 적 선제 진행 중 행동력이 미리 지급되지 않음 확인
                Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 선제 다음의 실제 첫 아군 턴 진입
                Assert.AreEqual(5, drawCapacity); // 첫 카드를 뽑기 전 기본 행동력과 병의 보상 적용 확인
            } // 적 선제의 행동력 경계 검사 종료
            int expected = ambush ? 6 : 5; // 병의 두 점과 실제 검 손패의 기존 우편봉투 한 점 합산
            Assert.AreEqual(expected, battle.Points.MaxActionPoints); // 첫 아군 턴의 원본 및 일시 상한 합계 확인
            Assert.AreEqual(expected, battle.Points.CurrentActionPoints); // 첫 아군 턴에서 실제 사용 가능한 공용 행동력 확인
            Assert.AreEqual(46, battle.Allies[0].CurrentMental); // 첫 아군 턴 시작에서 비용 재적용 없음 확인
            Assert.IsTrue(battle.Turns.EndPlayerTurn()); // 임시 행동력을 회수하는 실제 아군 턴 종료
            Assert.AreEqual(3, battle.Points.MaxActionPoints); // 다음 적 턴에 일시 한도를 유지하지 않는지 확인
            Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 두 번째 아군 턴의 정상 행동력 회복 진행
            Assert.AreEqual(3, battle.Points.CurrentActionPoints); // 다음 아군 턴은 기존 행동력만 사용 확인
            Assert.AreEqual(46, battle.Allies[0].CurrentMental); // 후속 턴에 시작 비용을 반복하지 않음을 확인
        } // 전투 진입 방식별 동작 검사 종료
    } // 바닥 없는 병 결과 검증 종료

    [UnityTest] // 비용과 첫 실제 정신 피해의 경로 구분 검사
    public IEnumerator PlateUsesOneTeamChargeForActualIncomingLossAndResetsWithBattle() // 개막 비용 보존과 첫 정신 피해 한 번 방어 확인
    { // 검은 심연판 실제 비교 진입 시작
        yield return new EnterPlayMode(); // 실제 전투 및 유물 회차 상태 준비
        VerifyPlate(); // 실제 교란, 피해와 정신력 비용의 기존 호출로 검증
        yield return new ExitPlayMode(); // 전투 검사 종료 후 편집 모드 복귀
    } // 검은 심연판 실제 비교 진입 종료

    private static void VerifyPlate() // 개막 비용과 실제 공용 보호의 실제 이벤트 경로 확인
    { // 검은 심연판 동작 확인 시작
        Acquire("BottomlessBottle"); // 보호를 소모해선 안 되는 전투 시작 비용 유물 획득
        Acquire("BlackAbyssPlate"); // 검사할 원본 보호 유물 획득
        using (var battle = new OpeningBattle()) // 보호가 있는 적 선제 전투의 실제 공유 상태 구성
        { // 비용과 정신 피해 입력의 차이 검증 시작
            var source = battle.Enemies[0]; // 실제 정신 피해의 적 발생자
            var first = battle.Allies[0]; // 교란의 첫 유효한 정신 피해를 받을 아군
            var second = battle.Allies[1]; // 이미 소모된 같은 보호의 다음 실제 아군
            var floor = battle.Allies[2]; // 영 하한 때문에 실제 정신력이 달라지지 않는 아군
            Assert.IsTrue(floor.ApplyPersistentMental(0)); // 표시된 실제 정신력 하한에 도달한 참가자 설정
            Assert.IsTrue(battle.Turns.StartBattle(0, true)); // 아군 턴을 기다리기 전부터 유효한 실제 선제 교전 시작
            Assert.AreEqual(46, first.CurrentMental); // 검은 심연판이 바닥 없는 병의 실제 비용을 가리지 않음 확인
            Assert.AreEqual(46, second.CurrentMental); // 둘째 아군 역시 전체 개막 비용 지불 확인
            Assert.AreEqual(-2, first.ChangeMental(-2, BattleMentalChangeReason.CardEffect).AppliedDelta); // 명시적인 카드 정신력 비용 정상 적용 확인
            Assert.AreEqual(-1, first.ChangeMental(-1, BattleMentalChangeReason.None).AppliedDelta); // 피해 출처가 아닌 일반 수치 감소 정상 적용 확인
            Assert.IsTrue(first.ApplyPersistentMental(42)); // 저장 상태 적용이 보호를 소모하지 않는지 확인
            floor.TakeDamage(1, BattleDamageType.None, source); // 체력은 변하지만 정신력은 바닥에서 바뀌지 않는 적 명중
            Assert.Zero(floor.CurrentMental); // 영에서 변할 수 없는 정신 피해가 별도 보호를 소모하지 않음 확인
            first.GainBlock(2); // 보호 이전 실제 체력 손실이 없는 명중 상태 구성
            Assert.Zero(first.TakeDamage(1, BattleDamageType.None, source).AppliedDamage); // 방어도가 전부 흡수한 공격에는 정신 피해가 없음 확인
            Assert.AreEqual(-4, first.PreviewMentalChange(-4)); // 읽기 전용 기존 예고 결과가 발동 기회를 소모하지 않음 확인
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, first.ApplyStatusEffect(BattleStatusEffectType.Disrupt, 1, 2, 1, source)); // 현재 적에게서 실제 교란 상태를 적용한 확인
            Assert.AreEqual(42, first.CurrentMental); // 최초로 실제 감소할 교란의 정신력 네 점만 방어 확인
            Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.Disrupt)); // 정신 피해 보호가 교란 상태 자체를 막지 않음 확인
            Assert.AreEqual(1, second.TakeDamage(1, BattleDamageType.None, source).AppliedDamage); // 다른 아군이 뒤따라 받은 실제 체력 피해의 정신 피해 경로 확인
            Assert.AreEqual(45, second.CurrentMental); // 아군별 보호가 아닌 전투당 한 번의 공유 보호 확인
        } // 비용과 정신 피해 입력의 차이 검증 종료
        using var fresh = new OpeningBattle(); // 같은 유물을 든 다음 전투의 새로운 보호 결과 확인
        Assert.IsTrue(fresh.Turns.StartBattle(0)); // 실제 두 번째 전투의 정상 시작
        var recipient = fresh.Allies[0]; // 새 전투에서 첫 피해를 입을 실제 아군
        recipient.TakeDamage(12, BattleDamageType.None, fresh.Enemies[0]); // 일반 피격과 큰 피해 정신 정산을 모두 발생시키는 명중
        Assert.AreEqual(41, recipient.CurrentMental); // 첫 피격 한 점은 무효화하고 같은 공격의 별개 큰 피해 다섯 점은 적용
    } // 검은 심연판 동작 확인 종료

    [UnityTest] // 실제 정신력 횟수 제한과 전투 종료 보호 경계 검사
    public IEnumerator PlatePreservesOrdinaryHeavyDamageLimitAndDoesNotProtectAfterBattle() // 기존 한도에서 막힌 피해와 전투 밖 손실을 구분 확인
    { // 정신 피해 한계 검사 진입 시작
        yield return new EnterPlayMode(); // 실제 전투 관리자가 생성되는 모드 진입
        VerifyTurnLimit(); // 정신 피해가 이미 일반 제한에 걸리는지 보호 이전 확인
        yield return new ExitPlayMode(); // 격리 전투를 정리한 뒤 편집 모드 복귀
    } // 정신 피해 한계 검사 진입 종료

    private static void VerifyTurnLimit() // 일반 큰 피해 횟수 한도와 보호 사용 여부 비교
    { // 일반 한도 및 종료 경계 확인 시작
        RelicRunManager.EnsureInstance(); // 탐사에서 전투로 넘어올 때 이미 있는 보유 유물 관리자 준비
        using (var battle = new OpeningBattle()) // 첫 아군 턴의 실제 큰 피해 초기 상태 구성
        { // 한도 이전과 이후의 보호 비교 시작
            Assert.IsTrue(battle.Turns.StartBattle(0)); // 실제 전투 중 일반 큰 피해 정신력 제한 준비
            var limited = battle.Allies[0]; // 먼저 일반 횟수 제한에 도달할 아군 선택
            var covered = battle.Allies[1]; // 횟수 제한이 비어 있어 보호받을 다음 아군 선택
            Assert.AreEqual(-5, limited.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 큰 피해 한도 한 번 사용 확인
            Assert.AreEqual(-5, limited.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 큰 피해 한도 두 번째 사용 확인
            Acquire("BlackAbyssPlate"); // 제한이 이미 찬 피해를 검사하기 위한 실제 보유 변경
            Assert.Zero(limited.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 기존 한도가 보호 사용 이전에 실제 차단됨 확인
            Assert.Zero(covered.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 첫 가능한 큰 피해의 실제 보호 사용 확인
            Assert.AreEqual(-5, covered.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 보호된 큰 피해가 기존 횟수를 차감하지 않음 확인
            Assert.AreEqual(-5, covered.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 같은 턴에 두 번째 적용 가능한 실제 감소 확인
            Assert.Zero(covered.ChangeMental(-5, BattleMentalChangeReason.HeavyDamage).AppliedDelta); // 세 번째 적용은 기존 큰 피해 제한 유지 확인
        } // 한도 이전과 이후의 보호 비교 종료
        using var escaped = new OpeningBattle(); // 보호를 사용하기 전에 끝나는 별개 전투 구성
        Assert.IsTrue(escaped.Turns.StartBattle(0)); // 보호 보유 상태의 정상 전투 시작
        Assert.IsTrue(escaped.Turns.TryEscape()); // 아직 실제 손실이 없는 시점의 전투 종료
        Assert.AreEqual(-4, escaped.Allies[0].ChangeMental(-4, BattleMentalChangeReason.DisruptReceived).AppliedDelta); // 전투가 끝나면 미사용 보호도 적용되지 않는지 확인
    } // 일반 한도 및 종료 경계 확인 종료

    private static void Acquire(string key) // 실제 자원과 보유 유물 상태에 원본 획득
    { // 실제 원본의 보유 요청 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 연결된 리소스의 원본 유물 읽기
        Assert.IsNotNull(relic, key); // 원본 자산과 메타 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 회차에서 일반 보유 방식으로 유물 상태 확보
    } // 실제 원본의 보유 요청 종료

    private sealed class OpeningBattle : IDisposable // 실제 전투 시작 전 참가 상태를 조정할 공용 독립 환경
    { // 개막 검사 전투 환경 시작
        private readonly CharacterData[] characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 실제로 다른 카드 소유자와 아군 원본 준비
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 실제 적 생성에 사용할 임시 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 첫 드로우의 원본 검 카드 재현용 자료
        private readonly DeckData source; // 실제 손패 초기화에 필요한 임시 원본 덱
        private readonly BattleStatusEffectController statuses; // 진영 턴으로 발동하는 실제 상태 처리기
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(3); // 정상 시작 행동력과 일시 상한 증가량 비교
        internal BattleUnitRuntime[] Allies { get; } // 실제 참가 아군 목록
        internal BattleUnitRuntime[] Enemies { get; } // 실제 정신 피해를 발생시키는 적 목록
        internal BattleDeckRuntime Deck { get; } // 실제 첫 손패와 적 선제의 드로우를 확인할 덱
        internal BattleTurnRuntime Turns { get; } // 정상 전투 시작과 진영 턴 정산

        internal OpeningBattle() // 실제 덱과 전투 상태 관리자로 독립 전투 준비
        { // 전투용 임시 원본 연결 시작
            foreach (var character in characters) // 기존 외부 아군 원본과 독립인 실제 기본 전투력 지정
            { // 개별 아군 원본 설정 시작
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50}", character); // 큰 피해 한도와 정신 피해 기준 지정
            } // 개별 아군 원본 설정 종료
            JsonUtility.FromJsonOverwrite($"{{\"cardType\":{(int)CardType.Sword}}}", card); // 찢긴 우편봉투의 실제 첫 손패 조건용 검 계열 설정
            Allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 기본 체력과 정신력이 있는 전투 아군 생성
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy) }; // 턴을 넘길 생존 적 한 명 생성
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, characters[0]) }); // 실제 첫 소유자의 검 카드가 연결된 원본 덱
            Deck = BattleDeckRuntime.Create(source, Allies, 4, 258); // 손패 상한과 실제 재현 가능한 덱 구성
            Turns = new BattleTurnRuntime(Deck, Points, Allies, Enemies, 0, BattleType.Normal); // 실제 전투 시작과 턴 변경의 런타임 관리자 구성
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 턴 시작 및 종료에 상태 효과 묶음 연결
        } // 전투용 임시 원본 연결 종료

        public void Dispose() // 임시 전투 참가자와 원본 및 등록 신호 해제
        { // 전투 상태 연결 해제 시작
            statuses.Dispose(); // 기존 상태 관리자의 턴 구독 해제
            Turns.Dispose(); // 전투 참가자와 회차 보호 연결 종료
            Object.Destroy(source); // 검사에서만 사용한 원본 덱 해제
            Object.Destroy(enemy); // 정신 피해를 발생시킨 임시 적 데이터 해제
            Object.Destroy(card); // 검사 손패로 사용한 임시 검 카드 해제
            foreach (var character in characters) // 테스트에서만 생성한 서로 다른 아군 원본 정리
            { // 개별 아군 자료 삭제 시작
                Object.Destroy(character); // 임시 아군 원본 해제
            } // 개별 아군 자료 삭제 종료
        } // 전투 상태 연결 해제 종료
    } // 개막 검사 전투 환경 종료
} // 개막 유물 회귀 검사 종료
