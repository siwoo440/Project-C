using System; // 시간과 임시 저장 식별자
using System.Collections; // 실제 사용 연출 대기
using System.IO; // 검사 전용 탐사 저장
using System.Linq; // 실제 대상과 목록 선택
using NUnit.Framework; // 기대 결과 비교
using UnityEngine; // 전투와 탐사 원본 사용
using UnityEngine.SceneManagement; // 실제 탐사 출발 진입
using UnityEngine.TestTools; // 플레이 모드 검사
using Object = UnityEngine.Object; // 씬의 실제 화면 조회
using Random = UnityEngine.Random; // 정제 확률의 재현 가능한 난수

public sealed class WinterfallMarkedRecoveryRelicTests // 표식과 회복 및 회차 유물 여덟 종 검사
{ // 유물 검사 시작
    private static RelicData Relic(string name) // 정상 획득과 저장에 사용하는 유물 조회
    { // 실제 유물 조회 시작
        return Resources.Load<RelicData>("Relics/" + name); // 등록된 원본 반환
    } // 실제 유물 조회 종료
    private static ShopCatalogData Shop => Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test"); // 실제 상품과 보상 목록
    [TestCase("CoolingCoil", 33)] // 보호막 코일 원본 행
    [TestCase("HalfBurnedJournal", 34)] // 소모성 일지 원본 행
    [TestCase("ResonanceReceiver", 35)] // 공명 수신기 원본 행
    [TestCase("MarkingChalk", 36)] // 표식용 분필 원본 행
    [TestCase("BandagePile", 37)] // 붕대 더미 원본 행
    [TestCase("SquareMetalPlate", 38)] // 금속판 원본 행
    [TestCase("PortablePurifier", 39)] // 캠프 정수기 원본 행
    [TestCase("ErosionMeter", 40)] // 침식지대 측정기 원본 행
    public void SheetArtAndSaveMatch(string name, int row) // 실제 획득 원본과 시트 설명 및 저장 대조
    { // 등록 검사 시작
        var data = Relic(name); // 공식 유물 자산
        Assert.IsNotNull(data, name); // 자산 존재 확인
        var sheet = RelicSheetCatalog.Load().FindByName(data.DisplayName); // 같은 이름의 원본 행
        Assert.AreEqual(row, sheet.SourceRow); // 실제 원문 행 일치
        Assert.AreEqual(sheet.Description, data.Description); // 확정 설명 일치
        Assert.IsNotNull(data.Icon); // 독립 원화 존재
        Assert.IsTrue(Shop.Offers.Any(item => item.Relic == data)); // 실제 획득과 판매 상품 등록
        CollectionAssert.Contains(Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Relics, data); // 저장 복원 원본 등록
    } // 등록 검사 종료
    [UnityTest] // 명중과 피해 분담 및 상태 부여 검사
    public IEnumerator CoilAndPlateApplyToTheirSpecifiedHitsOnly() // 적 직접 공격과 방어도 경계 및 물리 약화 검사
    { // 플레이 검사 시작
        yield return new EnterPlayMode(); // 실제 보관함 활성화
        CheckCoilAndPlate(); // 방어량과 실제 체력 차이 확인
        yield return new ExitPlayMode(); // 플레이 검사 종료
    } // 플레이 검사 종료
    private static void CheckCoilAndPlate() // 방어도를 가진 아군의 직접 피격 검증
    { // 방어 유물 검사 시작
        var relics = RelicRunManager.EnsureInstance(); // 실제 유물 획득 관리자
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(Relic("CoolingCoil"))); // 실제 코일 획득
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(Relic("SquareMetalPlate"))); // 실제 금속판 획득
        var raw = ScriptableObject.CreateInstance<EnemyData>(); // 직접 공격할 적 원본
        var foe = BattleUnitRuntime.CreateEnemy(raw); // 전투 적
        var ally = BattleUnitRuntime.CreateAlly(Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]); // 방어 효과를 받을 아군
        try // 검사 원본 정리 보장
        { // 실전 피격 검사 시작
            foreach (var type in new[] { BattleDamageType.Physical, BattleDamageType.Magical, BattleDamageType.None }) // 두 직접 유형과 일반 피해 비교
            { // 피해 유형 검사 시작
                ally.ClearBlock(); // 방어도 없는 대조 조건
                var hit = new BattleEnemyAction(foe, ally, EnemyActionType.Attack, type, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, string.Empty, 0, 1, 0, 0); // 실제 예고와 명중 객체
                int ordinary = hit.PreviewDamage().FinalDamage; // 방어도 없는 기본 유형별 피해
                ally.GainBlock(3); // 피격 직전 방어도 생성
                int expected = ordinary - (type == BattleDamageType.None ? 0 : 1); // 물리와 마법만 고정 한 점 감소
                Assert.AreEqual(expected, hit.PreviewDamage().FinalDamage); // 예고 수치에 같은 감소 반영
                var result = hit.Execute().DamageResult; // 실제 적 직접 공격
                Assert.AreEqual(expected, result.FinalDamage); // 실제 명중과 예고 일치
                Assert.AreEqual(3, result.AbsorbedDamage); // 줄어든 피해에서 방어도 차감
                Assert.AreEqual(expected - 3, result.AppliedDamage); // 남은 체력 피해 확인
            } // 피해 유형 검사 종료
            ally.GainBlock(7); // 소형 직접 공격 전 방어도 확인
            var weak = new BattleEnemyAction(foe, ally, EnemyActionType.Attack, BattleDamageType.Physical, 1, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, string.Empty, 0, 1, 0, 0); // 한 점 직접 공격
            Assert.AreEqual(0, weak.Execute().DamageResult.FinalDamage); // 코일로 완전히 사라지는 피해
            Assert.AreEqual(7, ally.CurrentBlock); // 차감 전 피해가 없어 방어도 유지
            Assert.AreEqual(2, ally.TakeDamage(2, BattleDamageType.None).FinalDamage); // 화상 등 별도 피해는 코일 무시
            Assert.IsTrue(ally.TrySpendHealth(1)); // 체력 대가 정상 지불
            ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 2, 2, 1, foe); // 물리 방어력 두 점 감소 부여
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).EffectiveValue); // 아군이 받을 감소를 한 점으로 완화
            ally.RemoveStatusEffect(BattleStatusEffectType.PhysicalDefenseDown); // 원래 크기가 다른 약화 비교 준비
            ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1, foe); // 세 점 물리 약화
            Assert.AreEqual(3, ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).EffectiveValue); // 두 점 외 크기는 유지
            ally.ApplyStatusEffect(BattleStatusEffectType.MagicalResistanceDown, 2, 2, 1, foe); // 마법 저항 감소 부여
            Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.MagicalResistanceDown).EffectiveValue); // 저항 약화 제외
            foe.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 2, 2, 1, ally); // 적에게 거는 같은 약화
            Assert.AreEqual(2, foe.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown).EffectiveValue); // 적을 보호하지 않음 확인
        } // 실전 피격 검사 종료
        finally // 임시 적 정리
        { // 원본 해제 시작
            Object.Destroy(raw); // 검사 적 원본 해제
        } // 원본 해제 종료
    } // 방어 유물 검사 종료
    [UnityTest] // 정상 카드 사용 연출 검사
    public IEnumerator MarksChalkAndBandagesFollowRealCardTargets() // 두 표식의 중복과 첫 회복 카드의 대상별 추가량
    { // 실제 입력 검사 시작
        yield return new EnterPlayMode(); // 전투 연출 진입
        yield return CheckCards(); // 전투 시작과 손패 클릭 확인
        yield return new ExitPlayMode(); // 검사 전투 종료
    } // 실제 입력 검사 종료
    private static IEnumerator CheckCards() // 실제 수신기와 분필과 붕대 사용
    { // 카드 효과 검사 시작
        var manager = RelicRunManager.EnsureInstance(); // 실제 유물 보관함
        foreach (var name in new[] { "ResonanceReceiver", "MarkingChalk", "BandagePile" }) // 서로 연계되는 유물 획득
        { // 유물 지급 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(Relic(name))); // 정상 회차에 원본 지급
        } // 유물 지급 종료
        using (var fight = new WinterfallSwordCombatTests.Fight("SWORD_DANCE", "SLASH", "HOLY_STRIKE", "SMALL_HEAL")) // 실제 손패와 적 위치 및 사용 연출
        using (var events = new BattleEventDispatcher()) // 전투 공용 이벤트 연결
        using (var effects = new BattleRelicEffectController(events, manager.Inventory, manager.Gold, fight.Allies, fight.Enemies)) // 실제 전투 시작 유물 연결
        { // 표식과 치료 확인 시작
            var fallen = fight.Enemies[3]; // 전투불능 표식 제외를 확인할 적
            fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None); // 실제 피해로 적 처치
            Assert.IsTrue(fallen.IsDead); // 전투 시작 전 처치 확인
            effects.ProcessInitialBattleState(BattleTurnPhase.EnemyTurn, 1); // 적 선공 상황의 전투 시작
            Assert.AreEqual(3, fight.Enemies.Count(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark) != null)); // 생존 적 모두에게만 공명 표식
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.ChariotMark) == null)); // 전차 표식을 임의 생성하지 않음
            new BattleStatusEffectProcessor().ProcessPhaseEnd(fight.Enemies[0], 1); // 선공한 적 턴의 일반 상태 처리
            Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.PublicMark)); // 첫 아군 행동 전에 공명 표식 보존
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, 1); // 두 표식이 동시에 있는 적
            int hp = fight.Enemies[0].CurrentHealth; // 중복 표식의 실제 타격 전 체력
            int slash = fight.View("SLASH").RuntimeCard.EffectValue; // 실제 카드의 기초 피해
            yield return fight.Use("SLASH", 0); // 정상 검 카드의 적 직접 타격
            Assert.AreEqual(slash + 2, hp - fight.Enemies[0].CurrentHealth); // 표식 두 개여도 분필은 한 번만 증가
            int total = fight.Enemies.Sum(enemy => enemy.CurrentHealth); // 무작위 연타 전 총 체력
            yield return fight.Use("SWORD_DANCE", 1); // 공명 표식 대상에게 실제 세 번 연타
            Assert.AreEqual(24, total - fight.Enemies.Sum(enemy => enemy.CurrentHealth)); // 각 직접 타격마다 기본 여섯에 두 점
            foreach (var ally in fight.Allies) // 원래 후속 회복 대상 준비
            { // 생존자 체력 설정 시작
                ally.ApplyPersistentHealth(5); // 추가 회복을 볼 수 있는 체력
            } // 생존자 체력 설정 종료
            var holy = fight.View("HOLY_STRIKE").RuntimeCard; // 사용 후 모든 생존 아군을 직접 회복하는 공격
            int heal = holy.SourceData.SecondaryEffectValue; // 카드 자체의 후속 회복 수치
            yield return fight.Use("HOLY_STRIKE", 1); // 실제 피해와 후속 회복
            Assert.IsTrue(fight.Allies.All(ally => ally.CurrentHealth == 5 + heal + 4)); // 처음 회복 카드의 원래 대상 각각에 붕대 네 점
            var card = fight.View("SMALL_HEAL").RuntimeCard; // 같은 전투의 두 번째 회복 카드
            var target = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).First(v => v.RuntimeUnit == fight.Allies[1]); // 직접 회복받을 생존 아군
            hp = target.RuntimeUnit.CurrentHealth; // 두 번째 회복 전 체력
            yield return WinterfallCommonCardExpansionTests.Play(card.SourceData, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 실제 아군 선택과 카드 소비
            Assert.AreEqual(hp + card.EffectValue, target.RuntimeUnit.CurrentHealth); // 같은 전투 두 번째 회복에는 붕대 미적용
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 턴 종료
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.PublicMark) == null)); // 원래 약속한 아군 턴 종료 만료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 진입
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, 1); // 전차 표식만 있는 적 준비
            hp = fight.Enemies[0].CurrentHealth; // 단독 전차 표식 타격 전 체력
            yield return fight.Use("SLASH", 0); // 다음 턴에 전차 표식 대상으로 정상 검 사용
            Assert.AreEqual(slash + 2, hp - fight.Enemies[0].CurrentHealth); // 공명 표식이 없어도 전차 표식 분필 적용
        } // 표식과 치료 확인 종료
        using (var fight = new WinterfallSwordCombatTests.Fight("HOLY_STRIKE", "SMALL_HEAL")) // 모두 건강한 신규 전투
        { // 최대 체력에서 첫 회복 처리 시작
            Assert.IsTrue(fight.Allies.All(ally => ally.CurrentHealth == ally.MaxHealth)); // 첫 카드 전 아군 최대 체력
            yield return fight.Use("HOLY_STRIKE", 0); // 실회복이 없는 광역 후속 회복 사용
            var unit = fight.Allies[1]; // 후속 검사 대상
            unit.ApplyPersistentHealth(5); // 실제 단일 회복량 비교용 낮은 체력
            var target = Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).First(view => view.RuntimeUnit == unit); // 현재 아군 대상 화면
            var card = fight.View("SMALL_HEAL").RuntimeCard; // 전투의 두 번째 회복 카드
            yield return WinterfallCommonCardExpansionTests.Play(card.SourceData, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 두 번째 회복 정상 사용
            Assert.AreEqual(5 + card.EffectValue, unit.CurrentHealth); // 최대 체력에서도 소모된 첫 사용 기회
        } // 최대 체력에서 첫 회복 처리 종료
    } // 카드 효과 검사 종료
    [UnityTest] // 승리 정제와 실제 탐사 저장 확인
    public IEnumerator VictoryJournalAndCampPurifierPersist() // 승리 확률과 하한 및 소모 상태 저장 검사
    { // 회차 검사 시작
        if (Application.companyName != Application.productName || (Application.productName != "ProjectCVerification" && Application.productName != "ProjectCDay232Checks")) // 실제 지갑 저장 위치 분리
        { // 검사 계정 확인 시작
            Assert.Ignore("별도 저장 경로를 가진 Unity 검증 사본에서 실행"); // 개발자 회차와 일반 지갑 보호
        } // 검사 계정 확인 종료
        yield return new EnterPlayMode(); // 실제 탐사 진입
        yield return CheckRun(); // 파티 정신력과 회차 덱 정제 확인
        yield return new ExitPlayMode(); // 실제 탐사 종료
    } // 회차 검사 종료
    private static IEnumerator CheckRun() // 실제 출전과 결과 이벤트 및 저장 복원
    { // 탐사 흐름 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null); // 메인 메뉴 초기화 대기
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 거점 시작
        yield return WaitFor(() => Object.FindFirstObjectByType<LobbyLayoutView>() != null); // 편성 거점 대기
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 네 명 편성 출전
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationPlayerController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 탐사 주인공 준비
        var relics = RelicRunManager.EnsureInstance(); // 탐사 유물 보관함
        var result = BattleResultManager.EnsureInstance(); // 실제 파티 영속 체력과 정신력
        var members = result.ActiveParty.Members; // 출전 파티원 원본
        var journal = Relic("HalfBurnedJournal"); // 승리 정제 유물 원본
        var purifier = Relic("PortablePurifier"); // 캠프 정수기 원본
        var meter = Relic("ErosionMeter"); // 침식지대 대기 중인 원본
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(purifier)); // 실제 캠프 보너스 유물 획득
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(meter)); // 현재 탐사에서도 수집 가능한 측정기
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(journal)); // 승리 시도용 일지 획득
        result.RestoreSavedAllyState(members[0], 5, 30, 0); // 보통 정신력의 생존자
        result.RestoreSavedAllyState(members[1], 5, 99, 0); // 정신력 상한에 가까운 생존자
        result.RestoreSavedAllyState(members[2], 0, 10, 1); // 쓰러진 파티원
        var room = RestRoomRunManager.EnsureInstance(); // 현재 탐사 캠프
        room.Prepare(PartyLoadoutRuntime.Active.Deck); // 실제 카드 강화 서비스 준비
        Assert.IsTrue(room.TryUseRest(0, 1, out string note), note); // 실제 캠프 휴식 실행
        StringAssert.Contains("정신력 +17", note); // 화면 결과의 실제 회복량
        Assert.IsTrue(result.TryGetSavedAllyState(members[0], out _, out int mental, out _)); // 첫 생존자 조회
        Assert.AreEqual(47, mental); // 공통 열다섯과 추가 두 점
        Assert.IsTrue(result.TryGetSavedAllyState(members[1], out _, out mental, out _)); // 상한 근처 파티원 조회
        Assert.AreEqual(100, mental); // 최대 정신력 상한
        Assert.IsTrue(result.TryGetSavedAllyState(members[2], out _, out mental, out _)); // 사망 파티원 조회
        Assert.AreEqual(10, mental); // 휴식으로 사망자 정신력 변경 방지
        var deck = RunDeckManager.Instance; // 실제 회차 덱
        while (deck.CardCount < RunDeckManager.MinimumCardsAfterRemoval) // 검사 시작 덱을 최소 열 장까지 채우기
        { // 부족한 검사 카드 보충 시작
            Assert.IsTrue(deck.TryAddCard(deck.Cards[0].Card, members[0])); // 실제 소유자의 카드 한 장 추가
        } // 부족한 검사 카드 보충 종료
        while (deck.CardCount > RunDeckManager.MinimumCardsAfterRemoval) // 최소 덱 조건 준비
        { // 하한 확인 준비 시작
            Assert.IsTrue(deck.TryRemoveAt(0)); // 기존 제거 API를 거쳐 열 장 유지
        } // 하한 확인 준비 종료
        Assert.AreEqual(25, journal.TriggerChancePercent); // 확정 승리 확률
        RunJournal(relics, BattleResult.Victory, true); // 확률이 성공하여도 하한에서 제거 불가
        Assert.IsTrue(relics.Inventory.ContainsRelic(journal.RelicId)); // 하한일 때 일지 보존
        Assert.IsTrue(deck.TryAddCard(deck.Cards[0].Card, members[0])); // 제거 가능한 회차 카드 추가
        int before = deck.CardCount; // 승리 정제 전 실제 장수
        RunJournal(relics, BattleResult.Escape, true); // 도주에는 정제 미발동
        RunJournal(relics, BattleResult.Defeat, true); // 패배에도 정제 미발동
        RunJournal(relics, BattleResult.Victory, false); // 승리 추첨 실패 뒤 중복 종료 신호도 재추첨 금지
        Assert.AreEqual(before, deck.CardCount); // 실패한 시도의 원래 덱 보존
        Assert.IsTrue(relics.Inventory.ContainsRelic(journal.RelicId)); // 실패 시 일지 보존
        RunJournal(relics, BattleResult.Victory, true); // 성공하는 승리에서 실제 회차 카드 한 장 제거
        Assert.AreEqual(before - 1, deck.CardCount); // 중복 승리 신호에도 한 장만 제거
        Assert.IsFalse(relics.Inventory.ContainsRelic(journal.RelicId)); // 실제 제거 성공 시 일지 소진
        var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 저장 위치
        var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 독립 임시 저장
        try // 임시 저장 정리 보장
        { // 정제 상태 복원 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 소모 상태와 정제 덱 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 저장 복원
            Assert.AreEqual(before - 1, RunDeckManager.Instance.CardCount); // 복원 후 회차 카드 수 보존
            Assert.IsFalse(relics.Inventory.ContainsRelic(journal.RelicId)); // 이미 쓴 유물이 다시 생기지 않음
            Assert.IsTrue(relics.Inventory.ContainsRelic(meter.RelicId)); // 발동 대기 측정기도 원본으로 복원
            var session = ExplorationSessionManager.Instance; // 현재 윈터폴 조우 결과 정산
            var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshWanderer"); // 현재 지역의 일반 전투 원본
            var wallet = PlayerResourceManager.EnsureInstance(); // 실제 승리 골드를 받는 지갑
            int gold = wallet.Gold; // 보상 정산 전 골드
            Assert.IsTrue(session.BeginEncounter("meter-ordinary", encounter, player.transform.position, player.transform.position)); // 측정기 소유 상태의 일반 조우
            var party = new[] { BattleUnitRuntime.CreateAlly(members[0]) }; // 실제 생존 아군의 승리 결과
            session.ResolveBattleResult(new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, party, Array.Empty<string>())); // 일반 전투 승리 보상 수령
            Assert.AreEqual(gold + encounter.GoldReward, wallet.Gold); // 침식지대 전용 보너스가 일반 승리에 붙지 않음
            Assert.IsTrue(relics.TryRemove(purifier.RelicId)); // 판매와 같은 보관함 제거
            Assert.AreEqual(15, RestRoomRecoveryService.CampMentalRecovery); // 제거 후 캠프 정상 회복량 복원
        } // 정제 상태 복원 종료
        finally // 독립 파일 삭제
        { // 파일 정리 시작
            if (File.Exists(path)) // 검사 파일 존재 확인
            { // 삭제 시작
                File.Delete(path); // 검사 파일만 삭제
            } // 삭제 종료
        } // 파일 정리 종료
    } // 탐사 흐름 종료
    private static void RunJournal(RelicRunManager relics, BattleResult result, bool succeeds) // 승리와 도주 및 실패 난수 조건 재현
    { // 전투 종료 재현 시작
        var saved = Random.state; // 테스트 전 난수 상태 보관
        try // 게임 난수 복원 보장
        { // 확률 재현 시작
            using (var events = new BattleEventDispatcher()) // 전투당 독립 공용 이벤트
            using (var effects = new BattleRelicEffectController(events, relics.Inventory, relics.Gold, Array.Empty<BattleUnitRuntime>(), Array.Empty<BattleUnitRuntime>())) // 실제 승리 유물 처리기
            { // 전투 결과 처리 시작
                int seed = 0; // 재현할 첫 난수 후보
                while (true) // 실제 유물과 같은 난수 범위에서 원하는 고정 표본 찾기
                { // 고정 난수 확인 시작
                    Random.InitState(seed); // 비교 난수의 첫 상태 설정
                    bool first = Random.Range(0, 100) < 25; // 첫 승리 추첨 결과
                    bool repeated = Random.Range(0, 100) < 25; // 중복 통지가 다시 추첨되면 발생할 결과
                    if (first == succeeds && (succeeds || repeated)) // 실패 표본은 재추첨했을 때 성공할 시드 사용
                    { // 표본 확정 시작
                        break; // 일치하는 시드 보존
                    } // 표본 확정 종료
                    seed++; // 다음 시드 검사
                } // 고정 난수 확인 종료
                Random.InitState(seed); // 실제 유물에 같은 첫 추첨 공급
                events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 1, BattleTurnPhase.Victory, result: result)); // 정식 전투 종료 신호
                events.Publish(new BattleEventContext(BattleEventType.BattleEnded, 1, BattleTurnPhase.Victory, result: result)); // 중복 종료 통지 검사
            } // 전투 결과 처리 종료
        } // 확률 재현 종료
        finally // 난수 상태 복원
        { // 난수 복원 시작
            Random.state = saved; // 다른 실전 검사에 영향 방지
        } // 난수 복원 종료
    } // 전투 종료 재현 종료
    private static IEnumerator WaitFor(Func<bool> ready) // 실제 씬 초기화 대기
    { // 초기화 확인 시작
        float end = Time.realtimeSinceStartup + 30f; // 준비 대기 기한
        while (!ready() && Time.realtimeSinceStartup < end) // 필수 게임 객체 준비 여부 확인
        { // 다음 프레임 대기 시작
            yield return null; // 실제 씬 초기화 한 프레임 대기
        } // 다음 프레임 대기 종료
        Assert.IsTrue(ready()); // 초기화 완료 확인
    } // 초기화 확인 종료
} // 유물 검사 종료
