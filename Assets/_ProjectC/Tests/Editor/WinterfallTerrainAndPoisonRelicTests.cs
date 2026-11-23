using System.Collections; // 실제 플레이 모드 전환과 원본 처리 대기
using NUnit.Framework; // 지역별 함정 피해와 상태 틱 검사
using UnityEngine; // 실제 파티 원본과 유물 자료 사용
using UnityEngine.TestTools; // 플레이 모드 진입과 종료 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 진영 차례와 상태 처리 환경

public sealed class WinterfallTerrainAndPoisonRelicTests // 협곡의 탐사 함정과 전투 중독 유물 검사
{ // 두 가지 실제 원본 경로의 회귀 검사 시작
    [UnityTest] // 실제 지역 선택의 함정 표시와 저장 피해 및 기존 지역 값 확인
    public IEnumerator CanyonTrapKeepsActualPreviewAndSettlementInSync() // 일반 환경 피해와 지역 함정의 독립 적용 검사
    { // 협곡 함정 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 저장과 분리된 검증 프로젝트 확인
        { // 기본 회차 저장 보호 시작
            Assert.Ignore("저장 경로가 분리된 검증 환경에서 실행"); // 활성 탐사 회차 보호
        } // 기본 회차 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 탐사 파티와 선택 창 환경 진입
        var actor = PrepareExploration(); // 정상 출전 상태의 실제 행동자 준비
        var party = BattleResultManager.EnsureInstance(); // 표시된 피해를 저장하는 실제 파티 관리자
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.BurningCanyon)); // 경고패 획득 전 기존 파편 피해 확인
        var plaque = Acquire("RockfallWarningPlaque"); // 출시 낙석 경고패의 실제 획득
        Assert.AreEqual(15, plaque.EffectValue); // 실제 원본의 지역 비율 확인
        Assert.AreEqual(0, (int)ExplorationRegion.Winterfall); // 기존 윈터폴 저장 지역 번호 유지 확인
        Assert.AreEqual(1, (int)ExplorationRegion.Desert); // 기존 사막 저장 지역 번호 유지 확인
        Assert.AreEqual(2, (int)ExplorationRegion.Fortress); // 기존 성채 저장 지역 번호 유지 확인
        Assert.AreEqual(3, (int)ExplorationRegion.Bunker); // 기존 벙커 저장 지역 번호 유지 확인
        Assert.AreEqual(4, (int)ExplorationRegion.BurningCanyon); // 기존 지역 뒤에 추가한 실제 협곡 번호 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Winterfall)); // 윈터폴 선택의 기본 함정 피해 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Desert)); // 사막 선택에 협곡 유물 미적용 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Fortress)); // 성채 선택의 원래 지역 피해 확인
        Assert.AreEqual(7, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.Bunker)); // 벙커 선택의 원래 지역 피해 확인
        Assert.AreEqual(5, AshstormEyeEvent.GetShatterHealthDamage(ExplorationRegion.BurningCanyon)); // 실제 협곡 피해 미리보기의 공용 정수 내림 확인
        Assert.AreEqual(5, GlassFieldEvent.GetBreakGlassHealthDamage(ExplorationRegion.BurningCanyon)); // 두 번째 실제 함정 미리보기의 공용 지역 처리 확인
        party.RestoreSavedAllyState(actor, 14, 30, 0); // 확정 뒤 남은 체력을 비교할 원본 상태 준비
        Assert.IsTrue(AshstormEyeEvent.TryResolve("286-canyon-shatter", AshstormEyeEvent.Shatter, actor, null, out string summary, ExplorationRegion.BurningCanyon)); // 실제 파편 선택으로 협곡 함정 피해 확정
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out int health, out _, out _)); // 실제 결과 저장의 아군 체력 확인
        Assert.AreEqual(9, health); // 미리보기와 같은 다섯 점의 실제 차감
        StringAssert.Contains("14 → 9", summary); // 탐사 결과 화면의 실제 반영 체력 확인
        party.ApplyExplorationHazardToActiveParty(2, 0); // 원본 선택 함정 이외의 실제 환경 피해 적용
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out health, out _, out _)); // 환경 피해 완료 후 실제 저장 체력 확인
        Assert.AreEqual(7, health); // 지역 함정 전용 효과의 일반 환경 피해 미적용 확인
        var lantern = Acquire("AfterglowLantern"); // 기존 고정 함정 감소 원본 보유
        Assert.AreEqual((20 - lantern.EffectValue) * 85 / 100, BattleResultManager.GetEventTrapHealthDamage(20, ExplorationRegion.BurningCanyon)); // 공용 고정 감소 뒤 지역 비율의 기존 정산 순서 확인
        Assert.AreEqual(0, BattleResultManager.GetEventTrapHealthDamage(0, ExplorationRegion.BurningCanyon)); // 함정 피해 영 하한 확인
        yield return new ExitPlayMode(); // 실제 파티와 탐사 환경 종료
    } // 협곡 함정 검사 종료

    [UnityTest] // 실제 적과 아군 차례에서 중독 감산과 화상 및 자연 감소 확인
    public IEnumerator WetReedReducesOnlyLivingAlliedPoisonDamage() // 양쪽 중독 출처와 한 점 하한 및 기존 유물 조합 검사
    { // 실제 전투 중독 검사 시작
        yield return new EnterPlayMode(); // 정상 상태 효과 구동 환경 진입
        var reed = Acquire("WetReedKnot"); // 실제 젖은 갈대매듭 획득
        var cactus = Acquire("DriedCactusSap"); // 기존 아군 출처 중독 가산 원본 획득
        var cord = Acquire("CharredStoneCord"); // 기존 받은 화상 감산 원본 획득
        Assert.AreEqual(1, reed.EffectValue); // 실제 원본의 중독 피해 감소량 확인
        using (var fight = new Fight("THRUST")) // 실제 두 진영과 상태 처리기를 연결한 공용 전투 환경
        { // 적과 아군의 실제 상태 정산 확인 시작
            var poisoned = fight.Allies[0]; // 일반 적 출처 중독을 받을 아군
            var floor = fight.Allies[1]; // 중독 한 점의 피해 영 하한을 확인할 아군
            var burned = fight.Allies[2]; // 기존 화상 감소에 영향을 받는 다른 아군
            var enemy = fight.Enemies[0]; // 선인장 수액의 발동도 비교할 살아 있는 적
            int poisonedHealth = poisoned.CurrentHealth; // 일반 중독 대상의 시작 체력 저장
            int floorHealth = floor.CurrentHealth; // 영 하한 대상의 시작 체력 저장
            int burnHealth = burned.CurrentHealth; // 화상 대상의 시작 체력 저장
            int enemyHealth = enemy.CurrentHealth; // 적 중독 대상의 시작 체력 저장
            poisoned.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1, enemy); // 적 출처의 실제 중독 세 점 적용
            floor.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 1); // 환경 출처의 실제 중독 한 점 적용
            burned.ApplyStatusEffect(BattleStatusEffectType.Burn, 4, 2, 1, enemy); // 같은 차례의 실제 화상 네 점 적용
            enemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 1, poisoned); // 아군 출처 가산을 받는 실제 적 중독
            Assert.AreEqual(poisonedHealth, poisoned.CurrentHealth); // 중독 상태 부여 시점의 체력 유지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 화상 종료와 적 중독 시작의 실제 전환
            Assert.AreEqual(burnHealth - (4 - cord.EffectValue), burned.CurrentHealth); // 기존 돌끈만 적용하는 화상 피해 확인
            Assert.AreEqual(enemyHealth - 3 - cactus.EffectValue, enemy.CurrentHealth); // 적 중독 피해의 원래 수액 가산 유지 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 아군의 실제 다음 차례와 중독 처리
            Assert.AreEqual(poisonedHealth - 2, poisoned.CurrentHealth); // 아군의 실제 중독 피해에서 한 점 감소 확인
            Assert.AreEqual(2, poisoned.GetStatusEffect(BattleStatusEffectType.Poison).EffectiveValue); // 피해 감소와 독 중첩 자연 감소의 분리 확인
            Assert.AreEqual(floorHealth, floor.CurrentHealth); // 한 점 중독의 실제 피해 영 하한 확인
            Assert.IsNull(floor.GetStatusEffect(BattleStatusEffectType.Poison)); // 영 피해에서도 기존 한 점 독의 만료 확인
        } // 적과 아군의 실제 상태 정산 확인 종료
        yield return new ExitPlayMode(); // 실제 전투 폐기와 검사 환경 복원
    } // 실제 전투 중독 검사 종료

    private static CharacterData PrepareExploration() // 기존 파티 편성과 탐사 초기화의 실제 경로 준비
    { // 정식 탐사 파티 구성 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 출시 아군 명단 로드
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 회복 상태와 시작 명단을 연결한 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 출전 아군 두 명 편성 확인
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 정상 출발 덱과 파티 구성 확인
        var party = BattleResultManager.EnsureInstance(); // 전투와 탐사 체력을 공유하는 파티 관리자
        party.ResetSavedPartyState(); // 기존 검사 체력과 정신 상태 초기화
        Assert.IsTrue(party.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 원래 출발 파티의 실제 출전 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 출발 덱의 실제 원본 설정
        ExplorationSessionManager.EnsureInstance().ResetExploration(); // 실제 탐사와 방의 해결 상태 초기화
        PlayerResourceManager.EnsureInstance().ResetResources(); // 파편 보상의 검사 지급 이전 실제 자원 초기화
        return party.ActiveParty.Members[0]; // 실제 탐사 선택의 첫 행동자 반환
    } // 정식 탐사 파티 구성 종료

    private static RelicData Acquire(string name) // 출시 자원에서 같은 회차의 보유 유물 획득
    { // 유물 원본의 실제 보유 확인 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 게임 내 등록 유물 자료 조회
        Assert.IsNotNull(relic, name); // 출시 유물의 실제 등록 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기존 회차 인벤토리의 정상 보유 확인
        return relic; // 실제 받은 유물 원본 반환
    } // 유물 원본의 실제 보유 확인 종료
} // 두 가지 실제 원본 경로의 회귀 검사 종료
