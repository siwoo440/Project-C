using System.Collections; // 격리 플레이 모드 전환
using System.Linq; // 살아 있는 적과 반사 대상 집계
using NUnit.Framework; // 실제 승리와 상태 적용 결과 비교
using UnityEngine; // 게임 원본과 실제 난수 연결
using UnityEngine.TestTools; // 플레이 모드 회귀 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 윈터폴 전투 환경
using Random = UnityEngine.Random; // 상태 전이 난수 재현

public sealed class WinterfallCaravanRewardAndReflectRelicTests // 사막 반사판과 비밀장부의 실전 회귀 검사
{ // 개별 유물 검사 시작
    [UnityTest] // 실제 탐사 정산 검사 등록
    public IEnumerator LedgerBoostsOnlyVictoryResourcesAndRetainsEscapeDeduction() // 골드와 세 재료 및 도주 정산 일치
    { // 탐사와 실제 재화 관리자가 동작하는 모드 시작
        yield return new EnterPlayMode(); // 게임 플레이 모드 실행
        var ledger = Acquire("CaravanSecretLedger"); // 실제 비밀장부 원본 획득
        var purse = Acquire("WornCoinPurse"); // 기존 골드 유물의 동시 장착
        var flare = Acquire("DistressFlare"); // 최초 도주 보상의 실제 유물
        var wallet = PlayerResourceManager.EnsureInstance(); // 실제 획득 자원 지갑
        var facility = FacilityUpgradeManager.EnsureInstance(); // 실제 물자 창고 설비
        var session = ExplorationSessionManager.EnsureInstance(); // 승리와 도주를 정산하는 탐사 관리자
        int warehouseBefore = facility.GetLevel(FacilityType.WarehouseExpansion); // 기존 설비 단계 백업
        try // 시험 상태 정리 보장
        { // 탐사와 설비의 실제 보상 비교 시작
            session.ResetExploration(); // 시험 조우를 받을 새 탐사 준비
            wallet.ResetResources(); // 자원 차이의 독립 시작 값
            wallet.BeginExplorationResourceTracking(); // 실제 탐사 중 획득분 집계 활성화
            facility.RestoreFacilityLevel(FacilityType.WarehouseExpansion, 3); // 자료의 골드 및 별도 재료 배율 검사
            var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer"); // 윈터폴의 정상 보상 원본
            Assert.IsNotNull(encounter); // 실제 전투 보상 원본 확인
            using var fight = new Fight("RUNE_BURST"); // 실제 귀환 가능한 생존 출전 파티
            var escape = new BattleResultData(BattleResult.Escape, encounter.BattleType, 1, fight.Allies, new string[0]); // 실제 도주 결과 데이터
            var victory = new BattleResultData(BattleResult.Victory, encounter.BattleType, 1, fight.Allies, new string[0]); // 같은 방의 실제 승리 결과 데이터
            const string room = "day274-ledger-escape-victory"; // 같은 도주와 재시도 방의 고정 구분
            Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 첫 실제 조우 시작
            session.ResolveBattleResult(escape); // 도주의 실제 정산 경로
            int escapedGold = encounter.GoldReward * flare.EffectValue / 100; // 기존 도주 지급 기대값
            Assert.AreEqual(escapedGold, wallet.Gold); // 비밀장부와 주머니의 도주 골드 미가산
            Assert.AreEqual(0, wallet.Screw + wallet.IronPlate + wallet.Wire); // 도주의 재료 미지급
            Assert.IsNull(session.LastClearReward); // 승리용 보상 UI 없음
            Assert.IsTrue(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 같은 방의 정상 재시도 가능 확인
            session.ResolveBattleResult(victory); // 실제 승리 보상 기록과 지갑 지급
            var reward = session.LastClearReward; // 실제 게임에서 표시할 최종 보상
            int gold = encounter.GoldReward * (100 + purse.EffectValue + ledger.EffectValue) / 100; // 같은 기본 골드에 두 보너스를 더한 결과
            int screw = facility.ApplyResourceRewardBonus(encounter.ScrewReward * 115 / 100); // 비밀장부와 창고를 한 번씩 적용한 나사
            int iron = facility.ApplyResourceRewardBonus(encounter.IronPlateReward * 115 / 100); // 비밀장부와 창고를 한 번씩 적용한 철판
            int wire = facility.ApplyResourceRewardBonus(encounter.WireReward * 115 / 100); // 비밀장부와 창고를 한 번씩 적용한 전선
            Assert.AreEqual(gold - escapedGold, reward.Gold); // 이전 도주 지급액 차감 후의 실제 표시
            Assert.AreEqual(encounter.CharacterExperienceReward, reward.CharacterExperience); // 경험치 보상에서 비밀장부 제외
            CollectionAssert.AreEqual(new[] { screw, iron, wire }, new[] { reward.Screw, reward.IronPlate, reward.Wire }); // 세 재료의 보상 화면 수치
            CollectionAssert.AreEqual(new[] { gold, screw, iron, wire }, new[] { wallet.Gold, wallet.Screw, wallet.IronPlate, wallet.Wire }); // 실제 지갑 지급과 설비 계산 일치
            CollectionAssert.AreEqual(new[] { gold, screw, iron, wire }, new[] { session.RunGoldGained, session.RunScrewGained, session.RunIronPlateGained, session.RunWireGained }); // 탐사 합계의 실제 지급 수치 일치
            CollectionAssert.AreEqual(new[] { gold, screw, iron, wire }, new[] { wallet.ExplorationGold, wallet.ExplorationScrew, wallet.ExplorationIronPlate, wallet.ExplorationWire }); // 탐사 획득분 지갑의 표시 수치 일치
            Assert.IsFalse(session.BeginEncounter(room, encounter, Vector3.zero, Vector3.zero)); // 이미 지급한 방 재진입 차단
            session.ResolveBattleResult(victory); // 늦게 돌아온 같은 결과의 중복 정산 시도
            Assert.AreEqual(gold, wallet.Gold); // 재호출에서 추가 지급 없음
        } // 실제 보상과 정산 비교 종료
        finally // 시험 중 바뀐 원본 상태 정리
        { // 시험 획득과 임시 설정 해제 시작
            session.ResetExploration(); // 조우와 탐사 합계의 임시 기록 제거
            wallet.ResetResources(); // 시험용 재화 초기화
            facility.RestoreFacilityLevel(FacilityType.WarehouseExpansion, warehouseBefore); // 원래 설비 수준 복원
            RelicRunManager.Instance.TryRemove(ledger.RelicId); // 비밀장부 제거
            RelicRunManager.Instance.TryRemove(purse.RelicId); // 기존 골드 유물 제거
            RelicRunManager.Instance.TryRemove(flare.RelicId); // 도주 보상 시험 유물 제거
        } // 시험 상태 정리 종료
        yield return new ExitPlayMode(); // 실제 탐사 검사 종료
    } // 비밀장부의 실제 승리 보상 검사 종료

    [UnityTest] // 실제 상태 정산과 반사의 회귀 검사 등록
    public IEnumerator ReflectorRequiresActualEnemyBlindAndUsesOneLivingTarget() // 실패 및 수신 면역과 대상 선택의 실제 난수 비교
    { // 유물과 전투의 실제 모드 시작
        yield return new EnterPlayMode(); // 게임 플레이 모드 실행
        var reflector = Acquire("GlassDesertReflector"); // 실제 반사판의 확률 원본 획득
        try // 시험 획득 유물 정리 보장
        { // 실제 반사 전투 비교 시작
            using var fight = new Fight("RUNE_BURST"); // 정상 적 네 명과 아군 셋의 실제 전투
            var dead = fight.Enemies[0]; // 살아 있는 명단에서 제외될 첫 적
            dead.TakeDamage(dead.CurrentHealth, BattleDamageType.None); // 실제 사망 처리
            var source = fight.Enemies[1]; // 실제 원본 실명의 생존 적 부여자
            var ally = fight.Allies[0]; // 적 실명을 받는 대상
            var immune = fight.Allies[1]; // 면역 때문에 유물을 발동시킬 수 없는 아군
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 원래 게임의 약화 면역 부여
            var success = FindRoll(reflector.TriggerChancePercent, true, 1); // 죽은 적을 제외한 세 후보 중 공격자 다음 적 고정
            Random.InitState(success.Seed); // 실제 상태 적용 전에 똑같은 난수 시드 복원
            Random.State untouched = Random.state; // 유효하지 않은 상태의 난수 유지 비교
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Blind, 8, 2, 1, source)); // 실제 아군 면역에서 원래 실명 차단
            ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 8, 2, 1, ally); // 적 발생자가 아닌 아군 실명 적용
            ally.RemoveStatusEffect(BattleStatusEffectType.Blind); // 다음 적 실명의 독립 결과 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 2, 1, source); // 같은 적의 다른 약화 적용
            Assert.AreEqual(untouched, Random.state); // 면역과 다른 상태 및 아군 부여의 추가 난수 없음
            var failure = FindRoll(reflector.TriggerChancePercent, false, -1); // 실제 반사판의 실패 시드
            Random.InitState(failure.Seed); // 상태 적용 한 건의 확률 실패 재현
            ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 8, 2, 1, source); // 실제로는 아군에게 남은 적 실명
            Assert.AreEqual(failure.After, Random.state); // 실패시 확률만 한 번 사용
            Assert.IsFalse(fight.Enemies.Any(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Blind) != null)); // 실패한 반사의 적 실명 없음
            Random.InitState(success.Seed); // 실제 성공 여정과 대상 선택 재현
            Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 8, 2, 1, source)); // 정상 갱신도 유효 상태 사건으로 처리
            var reflected = fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Blind); // 같은 순서의 무작위 생존 적에게 적용된 결과
            Assert.IsNotNull(reflected); // 공격자가 아닌 생존 적으로 반사 가능
            Assert.AreEqual(reflector.EffectValue, reflected.EffectiveValue); // 반사판의 원본 실명 수치
            Assert.AreEqual(1, reflected.RemainingTurns); // 원래 아군 실명과 독립인 한 적 턴
            Assert.AreEqual(1, fight.Enemies.Count(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Blind) != null)); // 대상 하나에만 실제 적용
            Assert.AreEqual(success.After, Random.state); // 반사 대상의 재귀와 추가 추첨 없음
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.Blind).RemainingTurns); // 아군 원본 실명은 그대로 유지
        } // 실제 전투의 반사 검사 종료
        finally // 유물 보관함 원래 상태 복원
        { // 실제 획득한 시험 유물 제거 시작
            RelicRunManager.Instance.TryRemove(reflector.RelicId); // 별도 회귀 상태로 넘기지 않는 반사판
        } // 유물 상태 정리 종료
        yield return new ExitPlayMode(); // 반사판 검사 격리 종료
    } // 실제 아군 실명과 적 반환 검사 종료

    private static RelicData Acquire(string name) // 출시 대상 유물의 실제 원본 장착
    { // 원본 획득 시작
        var data = Resources.Load<RelicData>("Relics/" + name); // 게임에서 로드하는 정확한 유물 원본
        Assert.IsNotNull(data); // 실제 원본 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(data)); // 효과를 쓸 수 있는 실제 보유 상태
        return data; // 원본 확률과 수치 검사용 반환
    } // 원본 획득 종료

    private static (int Seed, Random.State After) FindRoll(int chance, bool success, int targetIndex) // 실제 확률과 대상 추첨을 재현하는 시드 확인
    { // 확률 검사 준비 시작
        for (int seed = 0; seed < 1000; seed++) // 가능한 실제 전투 난수 후보 범위
        { // 시드별 실제 선택 조건 확인 시작
            Random.InitState(seed); // 비교용 시드 설정
            if ((Random.Range(0, 100) < chance) == success && (targetIndex < 0 || Random.Range(0, 3) == targetIndex)) // 성공 및 필요한 단일 대상 확인
            { // 기대 상태 반환 시작
                return (seed, Random.state); // 원본 작동 후 정확한 난수 대조값
            } // 기대 상태 반환 종료
        } // 시드 순회 종료
        throw new System.InvalidOperationException("Reflection fixture seed missing."); // 고정 시험 난수 준비 실패 확인
    } // 실제 난수 후보 검사 종료
} // 신규 유물 실전 검사 종료
