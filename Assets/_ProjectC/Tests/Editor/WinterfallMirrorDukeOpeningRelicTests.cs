using System; // 독립 테스트 전투의 실제 자원 정리
using System.Collections; // 에디터 모드와 플레이 모드 전환
using System.Linq; // 실제 원본 카드와 적 상태 탐색
using NUnit.Framework; // 원래 행동력과 피해 및 상태 단언
using UnityEngine; // 실제 원본 유물과 Unity 자료 생성
using UnityEngine.TestTools; // 실제 플레이 모드 검사의 실행 진입점
using Object = UnityEngine.Object; // 검사에서 생성한 실제 Unity 객체 해제
using Random = UnityEngine.Random; // 정확한 사전 전투 상태 확률 재현

public sealed class WinterfallMirrorDukeOpeningRelicTests // 보스 시작 행동력과 전체 공포 및 파티 치명 보호 검사
{ // 거울 공작 전투 개막 유물의 회귀 검사 시작
    [UnityTest] // 보스 분류와 적 선제 뒤 실제 행동력 검증
    public IEnumerator BossScrewStacksWithBottleOnlyOnFirstActualBossAllyTurn() // 개막 행동력 상한과 실제 행동력 및 만료 검증
    { // 우두머리 개막 행동력 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보유와 양 진영 정산 환경 진입
        VerifyBossPoints(); // 보스전과 일반 및 정예전의 실제 행동력 대조
        yield return new ExitPlayMode(); // 개막 전투 자원 정리 뒤 편집 모드 복귀
    } // 우두머리 개막 행동력 검사 종료

    [UnityTest] // 실제 첫 적 행동과 단일 난수 및 포스터 호환 검증
    public IEnumerator PortraitEyeRollsOnceForAllLivingEnemiesAndExpiresAfterTheirFirstTurn() // 성공 확률과 지속 시간 경계의 실제 검사
    { // 전체 공포 개막 검사 시작
        yield return new EnterPlayMode(); // 실제 상태 적용과 선제 진영 정산 환경 진입
        VerifyPortrait(); // 원래 면역과 난수 절약 및 첫 적 턴 유지 확인
        yield return new ExitPlayMode(); // 실제 전투 해제 뒤 편집 모드 복귀
    } // 전체 공포 개막 검사 종료

    [UnityTest] // 실제 개별 아군의 치명 피해 및 우선 보호 검증
    public IEnumerator HiddenNamePersistsForEachAllysFirstActualLethalHit() // 제때 소모되는 개별 치명 보호와 다른 피해 경로 검증
    { // 고해실 시작 치명 보호 검사 시작
        yield return new EnterPlayMode(); // 실제 생존 룰과 상태 수명 환경 진입
        VerifyWard(); // 선제 아군 보호와 방어 및 일회 실제 소모 대조
        yield return new ExitPlayMode(); // 실제 전투 자원 정리 뒤 편집 모드 복귀
    } // 고해실 시작 치명 보호 검사 종료

    private static void VerifyBossPoints() // 이전 병 보상에 더해지는 보스전의 첫 행동력 검증
    { // 보스전 행동력 검증 시작
        Acquire("CollapsedThroneScrew"); // 붕괴 왕좌의 나사 실제 유물 보유
        Acquire("BottomlessBottle"); // 기존 정신력 비용의 독립 행동력 보상 장착
        using (var boss = new OpeningFight(BattleType.Boss)) // 원래 강제 보스와 선택 보스가 사용하는 진짜 전투 분류
        { // 보스 선제 전투 검증 시작
            Assert.IsTrue(boss.Turn.StartBattle(1, true)); // 적 진영의 실제 선제 턴 시작
            Assert.AreEqual(3, boss.Points.MaxActionPoints); // 적 턴에는 아직 추가 상한이 없는지 확인
            Assert.Zero(boss.Points.CurrentActionPoints); // 선제 중 실제 플레이 행동력 미지급 확인
            Assert.IsTrue(boss.Turn.CompleteEnemyTurn()); // 적 선제 직후 정상 첫 아군 턴과 드로우 시작
            Assert.AreEqual(7, boss.Points.MaxActionPoints); // 기존 행동력 3과 병 2 및 나사 2의 실제 합
            Assert.AreEqual(7, boss.Points.CurrentActionPoints); // 확장된 첫 행동력의 실제 지급 확인
            Assert.IsTrue(boss.Turn.EndPlayerTurn()); // 아군 첫 실제 턴 종료와 임시 상한 회수
            Assert.AreEqual(3, boss.Points.MaxActionPoints); // 턴 종료에서 원래 최대 행동력 복구 확인
            Assert.IsTrue(boss.Turn.CompleteEnemyTurn()); // 보스전 두 번째 정상 아군 턴 시작
            Assert.AreEqual(3, boss.Points.CurrentActionPoints); // 같은 전투에서 첫 행동력 증가가 다시 지급되지 않음 확인
        } // 보스 선제 전투 검증 종료
        foreach (var type in new[] { BattleType.Normal, BattleType.Elite }) // 나사 확대가 없는 두 실제 분류 대조
        { // 나사 제외 전투 검증 시작
            using var other = new OpeningFight(type); // 원래 일반 또는 정예 조우 전투
            Assert.IsTrue(other.Turn.StartBattle(1)); // 선제 없는 실제 첫 아군 손패를 준비
            Assert.AreEqual(5, other.Points.MaxActionPoints); // 비보스에서는 병의 두 점만 실제 최대치에 반영
            Assert.AreEqual(5, other.Points.CurrentActionPoints); // 병 보상은 일반과 정예에서도 그대로 지급
        } // 나사 제외 전투 검증 종료
    } // 보스전 행동력 검증 종료

    private static void VerifyPortrait() // 일반 적과 면역 및 포스터의 원래 시간 동시 검증
    { // 초상화 속 붉은 눈의 개막 검사 시작
        Acquire("RedEyeInPortrait"); // 실제 대상별 아닌 전투 한 번의 초상화 유물
        using (var success = new OpeningFight(BattleType.Normal)) // 적 선제와 여러 상태 대상의 전투
        { // 단일 성공 및 원래 면역 검사 시작
            success.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 실제 일반 공포 면역의 생존 적 준비
            success.Enemies[3].TakeDamage(10000, BattleDamageType.None); // 행동하지 않는 전투불능 적 준비
            Random.State expect = SetBoundaryRoll(34); // 전투 시작 한 번의 실제 35퍼센트 성공 경계
            Assert.IsTrue(success.Turn.StartBattle(1, true)); // 첫 실제 적 진영이 시작되는 원래 시작 호출
            Assert.AreEqual(expect, Random.state); // 적 대상 수와 관계없는 한 번의 확률 상태 확인
            Assert.AreEqual(1, success.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)?.RemainingTurns); // 적 선제 전에 확인되는 첫 생존 적 공포
            Assert.AreEqual(1, success.Enemies[1].GetStatusEffect(BattleStatusEffectType.Fear)?.RemainingTurns); // 같은 단일 결과로 적용된 나머지 생존 적 공포
            Assert.IsNull(success.Enemies[2].GetStatusEffect(BattleStatusEffectType.Fear)); // 기존 면역 처리의 실제 공포 제외
            Assert.IsNull(success.Enemies[3].GetStatusEffect(BattleStatusEffectType.Fear)); // 이미 쓰러진 목록 원소의 적용 제외
            Assert.IsTrue(success.Turn.CompleteEnemyTurn()); // 초상화 공포의 실제 첫 적 턴 만료
            Assert.IsNull(success.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)); // 정확한 한 적 턴 뒤 공포 제거 확인
        } // 단일 성공 및 원래 면역 검사 종료
        using (var failure = new OpeningFight(BattleType.Elite)) // 기회 실패가 전체에 공유되는 별개 전투
        { // 정확한 확률 실패 경계 확인 시작
            Random.State expect = SetBoundaryRoll(35); // 35 자체는 실패하는 실제 RNG 결과 준비
            Assert.IsTrue(failure.Turn.StartBattle(1, true)); // 기존 적 선제 의도 단계 전 전투 시작
            Assert.AreEqual(expect, Random.state); // 실패에서도 기회를 한 번만 소비하는 확인
            Assert.IsTrue(failure.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Fear) == null)); // 실패 결과에 전체 아군 앞 적의 공포 없음
        } // 정확한 확률 실패 경계 확인 종료
        Acquire("CrumpledPoster"); // 첫 아군 턴까지만 공포를 주는 이전 유물 장착
        using var both = new OpeningFight(BattleType.Normal); // 서로 다른 지속 종료 시점을 가진 실제 시작
        SetBoundaryRoll(34); // 초상화와 포스터가 같이 있는 성공 개막의 실제 난수
        Assert.IsTrue(both.Turn.StartBattle(1)); // 아군이 먼저 시작되는 실제 조우 진입
        Assert.IsTrue(both.Turn.EndPlayerTurn()); // 일반 포스터가 자신의 공포를 회수하는 아군 종료
        Assert.IsNotNull(both.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)); // 초상화의 기존 독립 공포가 첫 적 행동에도 남는지 확인
        Assert.IsTrue(both.Turn.CompleteEnemyTurn()); // 실제 적 진영의 지속 효과 만료 정산
        Assert.IsNull(both.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)); // 첫 적 턴 종료에 정확히 사라지는 초상화 확인
    } // 초상화 속 붉은 눈의 개막 검사 종료

    private static void VerifyWard() // 선제 이전 실제 전투 보호와 각자 독립 소모 검증
    { // 개별 생존 상태의 검증 시작
        Acquire("HiddenNameOfConfessional"); // 첫 개별 치명 피해를 막는 원본 유물 획득
        using var fight = new OpeningFight(BattleType.Normal); // 두 생존 아군과 쓰러진 아군의 전투
        var caster = fight.Allies[0]; // 먼저 실제 보호를 검사할 생존 아군
        var partner = fight.Allies[1]; // 별개의 개별 치명 보호를 검사할 다른 아군
        var dead = fight.Allies[2]; // 시작 전에 쓰러진 배치 아군
        var attacker = fight.Enemies[0]; // 기존 보호 순서에 전달할 실제 적 출처
        Assert.IsTrue(dead.ApplyPersistentHealth(0)); // 전투에 등장해도 살아 있지 않은 대상 준비
        caster.ApplyStatusEffect(BattleStatusEffectType.DefeatWard, 1, 1, 1, caster); // 개막 시 이미 가지고 있는 같은 종류의 원래 상태 준비
        var prior = caster.GetStatusEffect(BattleStatusEffectType.DefeatWard); // 중복 충전을 판별할 실제 상태 인스턴스
        Assert.IsTrue(fight.Turn.StartBattle(1, true)); // 첫 실제 적 공격 전에 개별 보호 발급
        Assert.AreSame(prior, caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 기존 같은 보호의 중복 생성이나 재설정 없음
        Assert.IsNotNull(partner.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 살아 있는 다른 아군의 독립 보호 발급 확인
        Assert.IsNull(dead.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 쓰러진 아군의 개막 효과 제외 확인
        caster.GainBlock(100); // 진짜 피해를 미리 흡수하는 실제 방어도 부여
        Assert.Zero(caster.TakeDamage(1, BattleDamageType.Physical, attacker).AppliedDamage); // 완전히 막은 실타격의 체력 손실 확인
        Assert.AreSame(prior, caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 방어로 막은 피해에서 개별 보호 유지
        caster.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1, caster); // 방어 이전 무효화에 사용할 실제 보호 부여
        Assert.Zero(caster.TakeDamage(10000, BattleDamageType.Physical, attacker).AppliedDamage); // 다른 피해 무효화가 우선 소비되는 실제 피격
        Assert.AreSame(prior, caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 앞선 보호로 막은 공격에서 개별 치명 보호 유지
        Assert.IsTrue(fight.Turn.CompleteEnemyTurn()); // 선제 적 턴을 실제 종료하여 아군 턴 도달
        Assert.IsTrue(fight.Turn.EndPlayerTurn()); // 아군 정상 턴의 지속 감소 정산 통과
        Assert.IsNotNull(partner.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 실제 치명 피해 전에는 아군 턴 만료 없음
        caster.TakeDamage(10000, BattleDamageType.Physical, attacker); // 방어와 앞선 보호를 지나 들어오는 치명 타격
        Assert.AreEqual(1, caster.CurrentHealth); // 처음 생존 보호가 정확히 체력 한 점 유지
        Assert.IsNull(caster.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 실제로 소비한 아군 자신의 보호만 제거
        Assert.IsNotNull(partner.GetStatusEffect(BattleStatusEffectType.DefeatWard)); // 생존 동료의 별개 일회 보호 유지
        caster.TakeDamage(10000, BattleDamageType.Physical, attacker); // 같은 아군의 두 번째 실제 치명 타격
        Assert.IsTrue(caster.IsDead); // 기존 보호가 두 번 쌓이지 않았는지 확인
        partner.TakeDamage(10000, BattleDamageType.Physical, attacker); // 다른 아군의 최초 실제 치명 타격
        Assert.AreEqual(1, partner.CurrentHealth); // 동료도 별도로 체력 한 점에 생존하는지 확인
    } // 개별 생존 상태의 검증 종료

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
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 268); // 일정한 초기 덱으로 재생할 실제 카드 더미 생성
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
} // 거울 공작 전투 개막 유물의 회귀 검사 종료
