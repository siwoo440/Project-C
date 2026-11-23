using System.Collections; // 플레이 모드 실행 대기
using System.IO; // 독립 저장 파일 처리
using System.Linq; // 원본과 사건 후보 조회
using System.Reflection; // 게임의 고정 시드 함수 조회
using NUnit.Framework; // 검사 결과 비교
using UnityEngine; // 실제 회차와 전투 객체
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 행동자 선택 버튼

public sealed class CharredWellEventTests // 검게 탄 우물 회귀 검사
{ // 우물 검사 시작
    [Test] // 사건 원본과 봉인 확률 검사 지정
    public void NewPoolAndBothSealResultsRepeatOnlyForTheFirstEye() // 원문과 두 분기 및 층 재생성 확인
    { // 봉인 검사 시작
        var old = ExplorationEventCatalog.LoadEvents(6); // 불탄 수레까지의 이전 층 조회
        var pool = ExplorationEventCatalog.LoadEvents(7); // 검게 탄 우물까지의 층 조회
        CollectionAssert.AreEquivalent(old.Select(item => item.EventId).Append(CharredWellEvent.Id), pool.Select(item => item.EventId)); // 이전 목록에 우물 한 종 추가 확인
        var well = pool.Single(item => item.EventId == CharredWellEvent.Id); // 실제 우물 원본 조회
        Assert.IsNotNull(well.IllustrationSprite); // 우물 원화 연결 확인
        CollectionAssert.AreEqual(new[] { "재를 파낸다", "물을 뜨려 한다", "우물을 봉인한다" }, well.Choices.Select(item => item.ChoiceText)); // 선택지 원문 확인
        var eye = pool.Single(item => item.EventId == AshstormEyeEvent.Id); // 재폭풍의 눈 원본 조회
        Assert.IsTrue(eye != null, "재폭풍 원본이 파괴된 카탈로그"); // 유니티 엔진에 살아 있는 재폭풍 원본 확인
        int replaced = 0; // 실제 변경된 원본 수
        for (int i = 0; i < 128; i++) // 고정된 서로 다른 봉인 방 순회
        { // 개별 봉인 검사 시작
            string room = "well-seal-" + i; // 판정에 사용할 원본 방
            var state = new CharredWellRunState(); // 독립된 우물 상태 준비
            Assert.IsTrue(state.TrySealNextFloor(room)); // 다음 층 봉인 보관
            bool sawEye = false; // 현재 배치의 첫 재폭풍 표시
            Assert.AreSame(eye, state.SelectEvent(eye, pool, 914, ref sawEye, true)); // 봉인을 얻은 층에서는 원래 재폭풍 유지
            state.AdvanceFloor(); // 봉인이 적용되는 다음 층 진입
            Assert.AreSame(well, state.SelectEvent(well, pool, 914, ref sawEye, true)); // 다른 이벤트 무변경 확인
            Assert.IsFalse(state.CurrentSealUsed); // 다른 이벤트에서 판정 미소모 확인
            var preview = state.SelectEvent(eye, pool, 914, ref sawEye, false); // 나중에 제거되는 기본 배치의 미리보기
            Assert.IsFalse(state.CurrentSealUsed); // 임시 배치에서 판정 미소모 확인
            sawEye = false; // 실제 방 역할 배치 시작
            var first = state.SelectEvent(eye, pool, 914, ref sawEye, true); // 첫 실제 재폭풍 처리
            Assert.AreSame(preview, first); // 임시와 최종 배치의 같은 결과 확인
            Assert.AreEqual(ShouldReplace(room), first != eye); // 독립 난수의 백분율 열다섯과 대조
            if (first != eye) // 실제 대체 결과 확인
            { // 대체 후보 검사 시작
                replaced++; // 변경 성공 수 합산
                CollectionAssert.Contains(pool, first); // 같은 층 버전의 유효 이벤트 확인
            } // 대체 후보 검사 종료
            Assert.IsTrue(state.CurrentSealUsed); // 변경 성공과 실패 모두 한 번 소모 확인
            Assert.AreSame(eye, state.SelectEvent(eye, pool, 914, ref sawEye, true)); // 같은 층 두 번째 재폭풍 보존 확인
            sawEye = false; // 현재 층을 다시 생성
            Assert.AreSame(first, state.SelectEvent(eye, pool, 914, ref sawEye, true)); // 재생성에서도 최초 결과와 대체 종류 고정
            sawEye = false; // 같은 층의 다른 시드 지도 생성
            Assert.AreSame(eye, state.SelectEvent(eye, pool, 915, ref sawEye, true)); // 새 지도로 소모한 봉인 재적용 방지
            state.AdvanceFloor(); // 봉인 없이 한 층 더 이동
            sawEye = false; // 새 배치의 첫 재폭풍 표시
            Assert.AreSame(eye, state.SelectEvent(eye, pool, 914, ref sawEye, true)); // 다음 한 층을 지난 뒤 효과 만료 확인
        } // 개별 봉인 검사 종료
        Assert.That(replaced, Is.InRange(1, 127)); // 성공과 실패 모두 실제 재현 확인
        Object.DestroyImmediate(eye); // 플레이 모드 전환처럼 임시 재폭풍 원본 제거
        var refreshed = ExplorationEventCatalog.LoadEvents(7); // 같은 버전의 캐시에서 사건 목록 다시 조회
        Assert.IsTrue(refreshed.All(item => item != null)); // 모든 버전에서 파괴된 원본 복구 확인
        Assert.AreNotSame(eye, refreshed.Single(item => item.EventId == AshstormEyeEvent.Id)); // 같은 식별자의 새 재폭풍 원본 확인
    } // 봉인 검사 종료

    [UnityTest] // 포션 지급과 실제 입력 검사 지정
    public IEnumerator DigRequiresLivingActorAndEmptySlotBeforeTakingHealth() // 실제 포션과 행동자 체력 비용 확인
    { // 포션 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyDig(); // 독립 탐사 파일과 실제 행동자 선택 사용
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 포션 검사 종료

    private static void VerifyDig() // 가득 찬 보관함과 정상 지급 및 전멸 확인
    { // 포션 흐름 시작
        var session = PrepareParty(); // 출전 파티와 회차 덱 준비
        var party = BattleResultManager.Instance; // 실제 파티 상태 조회
        var actor = party.ActiveParty.Members[1]; // 재를 파낼 파티원 선택
        var other = party.ActiveParty.Members[0]; // 상태를 보존할 동료 조회
        var inventory = ConsumableRunManager.EnsureInstance().Inventory; // 실제 열 칸 보관함 조회
        var potion = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Consumables.Single(item => item.ItemId == CharredWellEvent.PotionId); // 체력 회복 포션 원본 조회
        inventory.Clear(); // 다른 검사 소모품 정리
        for (int i = 0; i < ConsumableInventoryRuntime.SlotCount; i++) // 모든 슬롯 채우기
        { // 슬롯 채움 시작
            Assert.IsTrue(inventory.TryAcquire(potion, out _)); // 실제 포션으로 각 슬롯 점유
        } // 슬롯 채움 종료
        party.RestoreSavedAllyState(actor, 12, 50, 0); // 행동자의 초기 상태 지정
        party.RestoreSavedAllyState(other, 17, 40, 0); // 다른 파티원의 초기 상태 지정
        Assert.IsFalse(CharredWellEvent.TryResolve("well-dig", CharredWellEvent.Dig, actor, out string failure)); // 빈 슬롯 없는 지급 차단 확인
        StringAssert.Contains("빈 슬롯", failure); // 보관함 실패 사유 확인
        AssertState(actor, 12, 50); // 지급 실패 때 체력 보존 확인
        Assert.IsFalse(session.IsEventResolved("well-dig")); // 재시도 가능한 사건 유지 확인
        Assert.IsTrue(inventory.TryRemoveAt(0)); // 포션 한 칸 확보
        var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == CharredWellEvent.Id); // 실제 우물 원본 조회
        var marker = new GameObject("WellDigTest").AddComponent<ExplorationEventView>(); // 실제 사건 표식 생성
        var panel = ExplorationEventPanelView.EnsureInstance(); // 탐사 선택 화면 준비
        string path = TempPath(); // 독립 저장 파일 경로
        try // 파일과 표식 정리 보장
        { // 실제 행동 시작
            marker.Initialize(data, "well-dig"); // 우물 방과 실제 표식 연결
            panel.ShowEvent(marker, data, "well-dig"); // 선택 화면 열기
            panel.SendMessage("HandleChoiceSelected", (object)CharredWellEvent.Dig); // 실제 행동자 선택 열기
            panel.transform.Find("TreasureOwnerCanvas/Panel/CancelButton").GetComponent<Button>().onClick.Invoke(); // 행동자 지정 취소
            AssertState(actor, 12, 50); // 취소 뒤 체력 보존 확인
            panel.SendMessage("HandleChoiceSelected", (object)CharredWellEvent.Dig); // 재를 파낼 행동 다시 선택
            panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + actor.CharacterId).GetComponent<Button>().onClick.Invoke(); // 지정 파티원 버튼 클릭
            Assert.IsTrue(session.IsEventResolved("well-dig")); // 실제 행동 확정 확인
            Assert.IsFalse(CharredWellEvent.TryResolve("well-dig", CharredWellEvent.Dig, actor, out _)); // 같은 우물 중복 지급 차단 확인
            AssertState(actor, 7, 50); // 선택한 행동자의 체력만 다섯 차감 확인
            AssertState(other, 17, 40); // 다른 파티원 수치 보존 확인
            Assert.AreSame(potion, inventory.GetItem(0)); // 실제 체력 회복 포션 지급 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 지급 상태 파일 작성
            inventory.Clear(); // 보관함을 비운 상태 구성
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 파일에서 우물 결과 복원
            Assert.AreSame(potion, ConsumableRunManager.Instance.Inventory.GetItem(0)); // 포션 원본 복원 확인
            AssertState(actor, 7, 50); // 지급 비용 복원 확인
            party.RestoreSavedAllyState(other, 0, 40, 1); // 동료 사망 상태 설정
            party.RestoreSavedAllyState(actor, 5, 50, 0); // 마지막 생존자 체력 다섯 설정
            Assert.IsFalse(CharredWellEvent.TryResolve("invalid", CharredWellEvent.Dig, other, out _)); // 사망자 선택 차단 확인
            Assert.IsTrue(ConsumableRunManager.Instance.Inventory.TryRemoveAt(0)); // 마지막 지급 공간 확보
            Assert.IsTrue(CharredWellEvent.TryResolve("well-fatal", CharredWellEvent.Dig, actor, out _)); // 체력 비용으로 마지막 생존자 사망
            Assert.IsTrue(session.IsExplorationCompleted); // 전멸 정산 확인
        } // 실제 행동 종료
        finally // 파일과 화면 정리
        { // 정리 시작
            ExplorationRewardOwnerView.ClosePending(); // 열린 선택 종료
            ExplorationSaveService.Remove(path); // 독립 파일 제거
            Object.Destroy(marker.gameObject); // 검사 표식 정리
        } // 정리 종료
    } // 포션 흐름 종료

    [UnityTest] // 실제 전투 피해와 저장 검사 지정
    public IEnumerator ReservedWaterUsesOnlyItsOwnersFirstSurvivedHealthDamage() // 소유자를 저장하고 피해와 흡수 및 치명상 구분
    { // 물 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifyWater(); // 실제 전투의 피해 신호 사용
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 물 검사 종료

    private static void VerifyWater() // 행동자 전용 예약과 피격 후 회복 확인
    { // 물 흐름 시작
        var session = PrepareParty(); // 실제 출전 파티 준비
        var party = BattleResultManager.Instance; // 지속 상태 조회
        var actor = party.ActiveParty.Members[0]; // 물을 받을 생존자
        var other = party.ActiveParty.Members[1]; // 물을 받지 않은 생존자
        party.RestoreSavedAllyState(actor, 20, 32, 0); // 행동자 초기 상태 지정
        Assert.IsTrue(CharredWellEvent.TryResolve("well-water", CharredWellEvent.DrawWater, actor, out _)); // 정신력 비용과 회복 보관
        AssertState(actor, 20, 26); // 정신력 여섯 감소 확인
        Assert.IsFalse(CharredWellEvent.TryResolve("well-duplicate", CharredWellEvent.DrawWater, actor, out _)); // 같은 행동자의 미사용 효과 중복 방지
        AssertState(actor, 20, 26); // 재선택 뒤 정신력 보존 확인
        string path = TempPath(); // 독립 저장 경로
        try // 파일 정리 보장
        { // 저장 및 전투 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 회복 사용 전 저장
            session.ResetExploration(); // 이전 효과 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 저장 파일에서 행동자와 회복 복원
            Assert.IsTrue(session.Well.HasWater(actor.CharacterId)); // 지정 행동자 효과 복원 확인
            Assert.IsFalse(session.Well.HasWater(other.CharacterId)); // 다른 파티원 미보유 확인
            var encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 일반 조우 원본 조회
            Assert.IsTrue(session.BeginEncounter("well-battle", encounter, Vector3.zero, Vector3.one)); // 실제 탐사 조우 시작
            var unit = BattleUnitRuntime.CreateAlly(actor); // 행동자 전투 상태 생성
            var friend = BattleUnitRuntime.CreateAlly(other); // 비교용 동료 생성
            var allies = new[] { unit, friend }; // 실제 파티 유닛 배열
            var enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 독립 적 원본 생성
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData) }; // 실제 적 유닛 생성
            var source = PartyLoadoutRuntime.Active.Deck; // 실제 출전 덱 원본 조회
            var deck = BattleDeckRuntime.Create(source, allies, 5, 213); // 소유자가 연결된 전투 덱 생성
            var points = new BattleActionPointRuntime(3); // 기본 행동력 생성
            var canvas = new GameObject("WellBattle", typeof(RectTransform), typeof(Canvas)); // 실제 전투 화면 생성
            var handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 손패 처리기 준비
            handObject.transform.SetParent(canvas.transform, false); // 손패와 전투 화면 연결
            var hand = handObject.GetComponent<BattleHandView>(); // 손패 입력 조회
            var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 전투 연출 연결
            try // 전투 객체 정리 보장
            { // 전투 피해 검사 시작
                using (var turns = new BattleTurnRuntime(deck, points, allies, enemies, 1, BattleType.Normal)) // 실제 전투 턴 생성
                using (var actions = new BattleEnemyActionRuntime(enemies, allies)) // 적 행동 연결
                using (var controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new BattleUnitView[0], new BattleUnitView[0], actions)) // 카드 이벤트 중계용 전투 행동 연결
                using (var events = new BattleEventDispatcher()) // 실제 전투 신호 발행기
                using (var signals = new BattleEventController(events, turns, controller, allies, enemies)) // 실제 체력 피해 신호 연결
                { // 피해 입력 시작
                    Assert.IsTrue(turns.StartBattle(1)); // 생존 유닛의 실제 전투 시작
                    unit.ApplyPersistentHealth(20); // 검사 행동자의 전투 체력 지정
                    friend.ApplyPersistentHealth(12); // 동료 체력 지정
                    friend.TakeDamage(3, BattleDamageType.None); // 미보유 동료 체력 피해
                    Assert.AreEqual(9, friend.CurrentHealth); // 다른 파티원 자동 회복 제외 확인
                    unit.GainBlock(4); // 첫 공격을 완전히 흡수할 방어도 설정
                    unit.TakeDamage(4, BattleDamageType.None); // 실제 체력 피해가 없는 공격
                    Assert.AreEqual(20, unit.CurrentHealth); // 방어도만 소모한 체력 확인
                    Assert.IsTrue(session.Well.HasWater(actor.CharacterId)); // 흡수 뒤 미사용 회복 보존 확인
                    unit.TakeDamage(9, BattleDamageType.None); // 살아남을 실제 체력 피해
                    Assert.AreEqual(19, unit.CurrentHealth); // 체력 아홉 손실 뒤 즉시 여덟 회복 확인
                    Assert.IsFalse(session.Well.HasWater(actor.CharacterId)); // 첫 생존 피격에서 한 번 소모 확인
                    unit.TakeDamage(4, BattleDamageType.None); // 보관 효과가 없는 후속 피격
                    Assert.AreEqual(15, unit.CurrentHealth); // 두 번째 자동 회복 방지 확인
                    Assert.IsTrue(session.Well.TryReserveWater(other.CharacterId)); // 치명상 조건을 검사할 동료에게 물 보관
                    friend.TakeDamage(friend.CurrentHealth, BattleDamageType.None); // 동료의 남은 체력만큼 피격
                    Assert.IsTrue(friend.IsDead && session.Well.HasWater(other.CharacterId)); // 부활 없이 생존 조건 불충족 확인
                } // 피해 입력 종료
            } // 전투 피해 검사 종료
            finally // 독립 화면과 원본 정리
            { // 정리 시작
                Object.Destroy(canvas); // 검사 전투 화면 제거
                Object.Destroy(enemyData); // 독립 적 원본 제거
            } // 정리 종료
            session.ResolveBattleResult(new BattleResultData(BattleResult.Escape, BattleType.Normal, 1, allies, new string[0])); // 실제 조우 정산으로 탐사 복귀
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero) && ExplorationSaveService.Load(path)); // 회복 사용 이후 파일 왕복
            Assert.IsFalse(session.Well.HasWater(actor.CharacterId)); // 저장 복원으로 이미 쓴 물 재생성 방지
        } // 저장 및 전투 종료
        finally // 파일 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 독립 탐사 파일 제거
        } // 정리 종료
    } // 물 흐름 종료

    [UnityTest] // 봉인 상태 파일 복원 검사 지정
    public IEnumerator SealSurvivesFloorGenerationAndSaveBeforeAndAfterItsRoll() // 대기 및 사용 완료 봉인의 파일 왕복 확인
    { // 봉인 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입
        VerifySealSave(); // 실제 탐사 파일 생성과 복원
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 봉인 저장 검사 종료

    private static void VerifySealSave() // 다음 층 봉인과 사용 기록 저장
    { // 봉인 파일 검사 시작
        var session = PrepareParty(); // 출발 파티와 1층 준비
        var party = BattleResultManager.Instance; // 행동자 상태 조회
        var actor = party.ActiveParty.Members[1]; // 정신력을 회복할 생존자 지정
        party.RestoreSavedAllyState(actor, 20, 60, 0); // 행동자 초기 정신력 설정
        string room = Enumerable.Range(0, 256).Select(i => "well-save-" + i).First(ShouldReplace); // 실제 15퍼센트 변경이 나오는 고정 방
        string path = TempPath(); // 독립 저장 파일 경로
        var pool = ExplorationEventCatalog.LoadEvents(7); // 다음 층의 실제 사건 후보
        var eye = pool.Single(item => item.EventId == AshstormEyeEvent.Id); // 재폭풍 원본 조회
        try // 파일 정리 보장
        { // 파일 왕복 시작
            Assert.IsTrue(CharredWellEvent.TryResolve(room, CharredWellEvent.Seal, actor, out _)); // 행동자의 정신력 회복과 봉인 보관
            AssertState(actor, 20, 64); // 행동자 정신력 네 점 회복 확인
            Assert.IsFalse(CharredWellEvent.TryResolve("extra-seal", CharredWellEvent.Seal, actor, out _)); // 같은 다음 층에 봉인 중복 방지
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero) && ExplorationSaveService.Load(path)); // 우물 방에서 봉인 대기 저장 왕복
            Assert.IsTrue(session.Well.HasNextSeal); // 다음 층 대기 상태 복원 확인
            session.AdvanceFloor(); // 봉인 대상 층으로 하강
            session.SetCurrentFloorSeed(914); // 동일한 다음 층 재생성 시드 설정
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero) && ExplorationSaveService.Load(path)); // 다음 층 판정 이전 저장 왕복
            bool sawEye = false; // 첫 지도 생성 시작
            var replacement = session.Well.SelectEvent(eye, pool, 914, ref sawEye, true); // 최초 재폭풍 실제 교체
            Assert.AreNotSame(eye, replacement); // 원본 재폭풍 제외 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 사용한 봉인 판정 파일 기록
            session.ResetExploration(); // 메모리 상태 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 판정 이후 탐사 복원
            Assert.IsTrue(session.Well.CurrentSealUsed); // 성공한 판정 소모 상태 복원 확인
            sawEye = false; // 파일에서 되살린 층 새로 구성
            Assert.AreSame(replacement, session.Well.SelectEvent(eye, pool, 914, ref sawEye, true)); // 같은 대체 이벤트 유지 확인
            Assert.AreSame(eye, session.Well.SelectEvent(eye, pool, 914, ref sawEye, true)); // 나머지 재폭풍 정상 배치 확인
            sawEye = false; // 같은 층의 새 시드 배치 시작
            Assert.AreSame(eye, session.Well.SelectEvent(eye, pool, 915, ref sawEye, true)); // 파일 복원 후 다른 시드에서 봉인 재사용 방지
        } // 파일 왕복 종료
        finally // 파일 정리
        { // 정리 시작
            ExplorationSaveService.Remove(path); // 독립 저장 파일 제거
        } // 정리 종료
    } // 봉인 파일 검사 종료

    private static bool ShouldReplace(string room) // 독립 백분율 기대값 계산
    { // 판정 기준 시작
        var hash = typeof(ExplorationTreasureRewardService).GetMethod("BuildStableHash", BindingFlags.Static | BindingFlags.NonPublic); // 게임의 재현 가능한 시드 계산 조회
        int seed = (int)hash.Invoke(null, new object[] { room + "|well-seal" }); // 같은 우물 방 시드 계산
        return new System.Random(seed).Next(100) < 15; // 정확히 0부터 14까지 대체 기준 적용
    } // 판정 기준 종료

    private static ExplorationSessionManager PrepareParty() // 두 명이 출전한 독립 탐사 구성
    { // 편성 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 출전 조건 준비
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 두 파티원 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출발 덱과 파티 편성 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 준비
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 조회
        session.ResetExploration(); // 이전 회차 초기화
        session.SetCurrentFloorSeed(213); // 독립 1층 시드 지정
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 생존 파티 지속 상태 등록
        return session; // 준비한 탐사 반환
    } // 편성 종료

    private static void AssertState(CharacterData member, int health, int mental) // 탐사 파티 수치 확인
    { // 수치 비교 시작
        Assert.IsTrue(BattleResultManager.Instance.TryGetSavedAllyState(member, out int hp, out int mind, out _)); // 실제 저장 상태 조회
        Assert.AreEqual(health, hp); // 선택한 체력 확인
        Assert.AreEqual(mental, mind); // 선택한 정신력 확인
    } // 수치 비교 종료

    private static string TempPath() // 독립 탐사 파일 경로 생성
    { // 경로 생성 시작
        return Path.Combine(Application.temporaryCachePath, "well-" + System.Guid.NewGuid().ToString("N") + ".json"); // 충돌 없는 파일 이름 반환
    } // 경로 생성 종료
} // 우물 검사 종료
