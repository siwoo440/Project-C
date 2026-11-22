using System; // 전투 검사 자원 정리 도구
using System.Collections; // 실제 플레이 모드 전환
using System.Linq; // 실제 상태 검사 대상 검색
using NUnit.Framework; // 전투 결과 검증
using UnityEngine; // 실제 카드와 유물 리소스 조회
using UnityEngine.TestTools; // 플레이 모드 실행 진입점
using Object = UnityEngine.Object; // 테스트 전용 Unity 원본 해제
using Random = UnityEngine.Random; // 전투 확률 경계 재현

public sealed class WinterfallDesertOpeningRelicTests // 사막 유물 네 종의 전투 개막과 주기
{ // 실제 개막 유물 검사 시작
    [UnityTest] // 첫 행동 진영과 실제 지속 시간 검사 등록
    public IEnumerator TrackerAndBottleUseActualTurnClocks() // 선제 중 회피와 첫 아군 명중 및 표식 만료
    { // 양 진영 개막 시간 검사 시작
        yield return new EnterPlayMode(); // 기존 리소스를 사용하는 플레이 모드 진입
        Acquire("BlackDesertTrackerPin"); // 검은 사막 추적자의 실제 유물 보유
        Acquire("SandstormBottle"); // 모래폭풍 병의 실제 유물 보유
        foreach (bool enemyFirst in new[] { false, true }) // 일반 개막과 적 선제를 같은 상태로 대조
        { // 개별 실제 조우 검사 시작
            using var fight = new OpeningFight(BattleType.Normal); // 실제 상태 처리기가 연결된 독립 조우
            var ally = fight.Allies[0]; // 첫 생존 아군
            var deadAlly = fight.Allies[2]; // 쓰러진 아군 검사 대상
            var marked = fight.Enemies[1]; // 체력이 같은 두 적 중 배치가 빠른 원소
            fight.Enemies[1].TakeDamage(30, BattleDamageType.None); // 낮은 현재 체력 설정
            fight.Enemies[2].TakeDamage(30, BattleDamageType.None); // 두 번째로 배치된 동점 적
            fight.Enemies[3].TakeDamage(10000, BattleDamageType.None); // 살아 있지 않은 최저 체력 적
            Assert.IsTrue(deadAlly.ApplyPersistentHealth(0)); // 전투 전 쓰러진 실제 아군 상태
            Assert.IsTrue(fight.Turn.StartBattle(1, enemyFirst)); // 실제 선제 조건과 원래 시작 정산
            Assert.AreEqual(3, marked.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns); // 현재 체력 최저의 선배치 생존 적 표식
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 최대 현재 체력 적 제외
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 같은 체력의 후배치 적 제외
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 전투불능 적 제외
            Assert.IsNull(deadAlly.GetStatusEffect(BattleStatusEffectType.Evasion)); // 전투불능 아군의 회피 상승 제외
            Assert.IsNull(deadAlly.GetStatusEffect(BattleStatusEffectType.Disorientation)); // 전투불능 아군의 명중 감소 제외
            Assert.AreEqual(90, BattleHitChance.GetPercent(fight.Enemies[0], ally)); // 첫 적 행동 전 실제 명중률에 반영한 회피
            if (enemyFirst) // 적 선제 때 아군 명중 감소 미지급 확인
            { // 적 선제 지속 검사 시작
                Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Disorientation)); // 첫 실제 아군 턴 전 별도 명중 감소 없음
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 첫 적 행동 종료 이후 첫 아군 진입
                Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Evasion)?.RemainingTurns); // 적 선제 한 번을 포함한 남은 회피 기간
                Assert.AreEqual(2, marked.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns); // 적 선제 한 번만 차감한 표식
            } // 적 선제 지속 검사 종료
            Assert.AreEqual(95, BattleHitChance.GetPercent(ally)); // 첫 실제 아군 행동의 명중률 감소
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 첫 실제 아군 턴 종료
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Disorientation)); // 첫 아군 턴의 의도한 종료와 즉시 해제
            Assert.AreEqual(enemyFirst ? 2 : 3, marked.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns); // 아군 종료에서 표식 기간 유지
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 다음 실제 적 턴 종료
            if (!enemyFirst) // 일반 개막의 첫 번째 적 행동 종료 검사
            { // 첫 번째 적 종료의 남은 상태 확인 시작
                Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Evasion)?.RemainingTurns); // 적 한 턴 종료 이후 회피 한 턴 보존
                Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 두 번째 아군 차례 종료
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 두 번째 실제 적 행동 종료
            } // 첫 번째 적 종료의 남은 상태 확인 종료
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Evasion)); // 첫 두 실제 적 턴을 모두 마친 회피 만료
            Assert.AreEqual(1, marked.GetStatusEffect(BattleStatusEffectType.PublicMark)?.RemainingTurns); // 두 적 턴 이후 남은 표식 한 턴
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 세 번째 적 행동 진입
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 세 번째 적 행동 종료
            Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 세 번째 실제 적 턴의 표식 만료
        } // 개별 실제 조우 검사 종료
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 양 진영 개막 시간 검사 종료

    [UnityTest] // 실제 체력 회복과 독립 정신력 검사 등록
    public IEnumerator OasisPaysMentalForFullAndWoundedLivingAllies() // 회복 유무와 관계없이 생존 아군의 정신력 감소
    { // 생존 아군의 개막 회복 검사 시작
        yield return new EnterPlayMode(); // 게임 유물 보유와 턴 전환 준비
        Acquire("DryOasisHeart"); // 마른 오아시스 심장의 실제 리소스 보유
        using (var fight = new OpeningFight(BattleType.Normal)) // 생존 두 명과 전투불능 한 명의 실제 조우
        { // 개막 회복의 독립 결과 비교 시작
            var wounded = fight.Allies[0]; // 체력 상실 상태의 생존 아군
            var full = fight.Allies[1]; // 이미 최대 체력인 생존 아군
            var dead = fight.Allies[2]; // 효과에서 제외할 전투불능 아군
            Assert.IsTrue(wounded.ApplyPersistentHealth(wounded.MaxHealth - 12)); // 실제 저장 체력에서 시작하는 부상 상태
            Assert.IsTrue(dead.ApplyPersistentHealth(0)); // 이미 쓰러진 최대 체력 이하의 아군
            int woundedMental = wounded.CurrentMental; // 실제 회복 보너스 전 정신력 기록
            int fullMental = full.CurrentMental; // 회복이 불필요한 아군의 이전 정신력
            int deadMental = dead.CurrentMental; // 전투불능 아군의 이전 정신력
            Assert.IsTrue(fight.Turn.StartBattle(1, true)); // 첫 적 행동 전에 실제 유물 정산
            Assert.AreEqual(wounded.MaxHealth - 6, wounded.CurrentHealth); // 열린 실제 회복 여유에서 정확히 체력 여섯 증가
            Assert.AreEqual(woundedMental - 1, wounded.CurrentMental); // 원래의 회복 보너스 한 점과 별도 비용 두 점
            Assert.AreEqual(full.MaxHealth, full.CurrentHealth); // 이미 찬 아군은 원래 체력 상한 유지
            Assert.AreEqual(fullMental - 2, full.CurrentMental); // 치료량 영점이어도 정신 비용 두 점
            Assert.Zero(dead.CurrentHealth); // 개막 회복으로 전투불능 아군이 살아나지 않음
            Assert.AreEqual(deadMental, dead.CurrentMental); // 전투불능 아군의 정신력 유지
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 적 선제 뒤 첫 실제 아군 진입
            Assert.AreEqual(wounded.MaxHealth - 6, wounded.CurrentHealth); // 아군 턴에 개막 회복 반복 없음
        } // 개막 회복의 독립 결과 비교 종료
        yield return new ExitPlayMode(); // 실제 게임 검사 모드 종료
    } // 생존 아군의 개막 회복 검사 종료

    [UnityTest] // 실제 난수의 성공과 실패 경계 검사 등록
    public IEnumerator EggshellRollsOnceEachThirdAllyTurn() // 적 선제와 실패 구간에서 올바른 난수 소비
    { // 알껍질 주기 검사 시작
        yield return new EnterPlayMode(); // 실제 Unity 난수 및 리소스 초기화
        Acquire("SandQueenEggshell"); // 실제 주기와 확률을 가진 유물 보유
        foreach (bool enemyFirst in new[] { false, true }) // 일반과 적 선제 조우를 각각 독립 비교
        { // 실제 세 번째 아군 턴 진입 시작
            using var fight = new OpeningFight(BattleType.Normal); // 실제 아군 및 적 턴을 가지는 조우
            fight.Enemies[0].TakeDamage(10000, BattleDamageType.None); // 무작위 후보에서 빠질 쓰러진 적 한 명
            Assert.IsTrue(fight.Turn.StartBattle(1, enemyFirst)); // 선제 여부가 실제로 적용되는 전투 시작
            if (enemyFirst) // 첫 아군 턴 진입 전 적 선제 구분
            { // 선제 턴 빠른 종료 시작
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 선제 종료 뒤 첫 실제 아군 턴
            } // 선제 턴 빠른 종료 종료
            Random.State undrawn = Random.state; // 확률을 소비하지 않아야 할 첫 두 아군 차례
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 첫 실제 아군 차례 종료
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 두 번째 실제 아군 차례 시작
            Assert.AreEqual(undrawn, Random.state); // 두 번째 아군 턴에는 확률 난수 미사용
            Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 두 번째 실제 아군 차례 종료
            Random.State expected = SetBoundaryRoll(enemyFirst ? 29 : 30); // 정확한 30퍼센트 성공 및 실패 경계 준비
            if (enemyFirst) // 성공의 확률 뒤 살아 있는 적 한 명 선정 예상
            { // 무작위 후보 난수 소비 준비 시작
                Random.State seed = Random.state; // 처음 확률을 계산하기 전 실제 난수 상태
                Random.Range(0, 100); // 유물 한 번의 원래 확률 굴림
                Random.Range(0, 3); // 세 생존 적의 한 번 대상 결정
                expected = Random.state; // 두 번의 필수 호출이 끝난 난수 상태
                Random.state = seed; // 같은 확률과 대상이 재현되도록 저장 상태 복구
            } // 무작위 후보 난수 소비 준비 종료
            Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 세 번째 실제 아군 차례 시작과 유물 발동
            Assert.AreEqual(expected, Random.state); // 한 번 확률 판정과 성공 시에만 무작위 대상 선택
            var affected = fight.Enemies.Where(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null).ToArray(); // 실제 약화 상태를 받은 적
            Assert.AreEqual(enemyFirst ? 1 : 0, affected.Length); // 성공한 한 마리 또는 실패한 무적용 확인
            if (enemyFirst) // 성공 조우만 실제 약화량과 만료 비교
            { // 성공한 무작위 약화 시작
                Assert.IsFalse(affected[0].IsDead); // 살아 있는 적의 원래 난수 후보
                Assert.AreEqual(2, affected[0].GetStatusEffect(BattleStatusEffectType.AttackPowerDown).EffectiveValue); // 실제 원본 공격력 감소 두 점
                Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 원래 약화 대상의 적 차례 시작
                Assert.IsNotNull(affected[0].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 실제 한 적 턴의 공격까지 유지
                Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 실제 적 차례 종료로 약화 만료
                Assert.IsNull(affected[0].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 정확한 한 실제 적 차례 이후 제거
            } // 성공한 무작위 약화 종료
        } // 실제 세 번째 아군 턴 진입 종료
        yield return new ExitPlayMode(); // 테스트 전용 플레이 세션 종료
    } // 알껍질 주기 검사 종료

    private static Random.State SetBoundaryRoll(int value) // 성공 및 실패 경계로 이어지는 정확한 난수 준비
    { // 실제 Unity 난수 경계 생성 시작
        for (int seed = 0; seed < 10000; seed++) // 첫 결과의 정확한 값이 나오는 실제 제한 시드 순회
        { // 개별 시드 비교 시작
            Random.InitState(seed); // 이후 실제 전투가 같은 난수를 얻을 초기 상태 생성
            if (Random.Range(0, 100) == value) // 요구한 성공 또는 실패의 정확한 실제 확률 확인
            { // 비교 후 상태 저장 및 복구 시작
                var expected = Random.state; // 실제 한 번만 굴렸을 때 전투가 끝날 난수 상태
                Random.InitState(seed); // 제어한 원본 상태로 되돌려 실제 전투에서 사용
                return expected; // 전투 내 중복 난수 여부를 확인할 상태 전달
            } // 비교 후 상태 저장 및 복구 종료
        } // 개별 시드 비교 종료
        Assert.Fail("실제 Unity 개막 확률 경계 시드 누락"); // 단일 확률 재현을 못 한 검사 구성 오류
        return default; // 검사 중단 뒤 형식상 필요한 기본값
    } // 실제 Unity 난수 경계 생성 종료

    private static void Acquire(string key) // 실제 Unity 원본 유물의 일반 보유 등록
    { // 유물 획득 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 원본 유물의 실제 고유 리소스
        Assert.IsNotNull(relic, key); // 실제 메타와 리소스 연결 검증
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 유효 탐사 보유의 실제 획득 확인
    } // 유물 획득 준비 종료

    private sealed class OpeningFight : IDisposable // 실제 진영 시작과 상태 만료를 사용하는 독립 전투
    { // 테스트 전투 생성 시작
        private readonly Random.State randomState = Random.state; // 독립 전투 외부의 원래 난수 복원을 위한 상태
        private readonly DeckData source; // 원래 카드 지급을 위한 실제 전투 원본 덱
        private readonly EnemyData enemyData; // 독립 충돌에 사용할 일시 적 원본
        private readonly BattleStatusEffectController statuses; // 턴에 따라 상태를 자연스럽게 제거하는 실제 관리자
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(3); // 고정 기본값과 임시 한도를 보여 주는 실제 행동력
        internal readonly BattleUnitRuntime[] Allies; // 개별 치명 보호의 실제 생존 검사 대상
        internal readonly BattleUnitRuntime[] Enemies; // 독립 공포 장면을 나타내는 실제 네 적
        internal readonly BattleTurnRuntime Turn; // 실제 시작과 선제 및 상태 수명을 처리하는 관리자

        internal OpeningFight(BattleType type) // 기존 일반 및 정예와 보스 전투 분류 연결
        { // 실제 턴 검사 구성 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 평소 캐릭터의 실제 명단
            Allies = roster.Characters.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 서로 다른 실제 아군 목록
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 다른 게임 원본을 수정하지 않는 검사 자료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemyData); // 비교 임계치를 위한 적 체력 설정
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemyData)).ToArray(); // 모두 개별 상태 슬롯을 가진 실제 적
            var thrust = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 실제 손패에 들어가는 표준 원본 카드
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(thrust, Allies[0].CharacterSource) }); // 같은 진영의 실제 카드 원본 덱
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 274); // 일정한 초기 덱으로 재생할 실제 카드 더미 생성
            Turn = new BattleTurnRuntime(deck, Points, Allies, Enemies, 1, type); // 요청한 조우 분류의 실제 이전 턴 처리
            statuses = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 원래 턴 시작과 만료 상태 계산 연결
        } // 실제 턴 검사 구성 종료

        public void Dispose() // 실제 전투의 자원 및 검사 전 난수 복구
        { // 원본 없는 테스트 객체 해제 시작
            statuses.Dispose(); // 실제 상태의 진영 이벤트 등록 해제
            Turn.Dispose(); // 개막 전투 및 런타임 덱 연결 정리
            Object.Destroy(source); // 생성한 테스트 전용 덱 해제
            Object.Destroy(enemyData); // 만든 검사 전용 적 원본 해제
            Random.state = randomState; // 다음 독립 검사에 영향을 주지 않는 난수 초기화
        } // 원본 없는 테스트 객체 해제 종료
    } // 테스트 전투 생성 종료
} // 사막 유물 네 종의 전투 개막과 주기 검사 종료
