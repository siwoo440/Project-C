using System; // 대기 시간과 조건 함수 사용
using System.Collections; // 실제 씬과 연출 진행 대기
using System.IO; // 독립 체크포인트 파일 사용
using System.Linq; // 카드 순서와 생존자 검사
using NUnit.Framework; // 게임 진행 결과 확인
using UnityEngine; // 실제 파티와 씬 원본 사용
using UnityEngine.EventSystems; // 화면의 실제 포인터 입력 순서 검사
using UnityEngine.SceneManagement; // 부트와 전투 씬 진입
using UnityEngine.TestTools; // 에디터 플레이 모드 검사
using UnityEngine.UI; // 실제 승리 보상 버튼 클릭
using Object = UnityEngine.Object; // 게임 화면과 객체 조회

public sealed class AshWandererCryEventTests // 방랑자 선제와 실제 승패 및 저장 검사
{ // 검사 시작
    private static readonly int[] Outcomes = { 0, 1, 2 }; // 승리와 도주와 패배의 독립 회차
    private const string Preempt = "EV_F1_X130_Y1_S214"; // 선제 난수 십사인 고정 방
    private const string NoPreempt = "EV_F1_X22_Y1_S214"; // 선제 난수 십오인 고정 방

    [Test] // 여덟 번째 이벤트 목록 버전 확인
    public void VersionEightAddsOnlyTheAuthoredWandererAndItsThreeChoices() // 이전 층과 방랑자의 원문 확인
    { // 목록 검사 시작
        var old = ExplorationEventCatalog.LoadEvents(7); // 직전 층의 이벤트 목록
        var current = ExplorationEventCatalog.LoadEvents(8); // 방랑자가 추가된 층 목록
        CollectionAssert.AreEqual(old.Select(item => item.EventId).Append(AshWandererCryEvent.Id).OrderBy(id => id, StringComparer.Ordinal), current.Select(item => item.EventId)); // 새 원본만 추가한 정렬 확인
        var data = current.Single(item => item.EventId == AshWandererCryEvent.Id); // 실제 방랑자 원본 조회
        Assert.IsNotNull(data.IllustrationSprite); // 전용 삽화 연결 확인
        CollectionAssert.AreEqual(new[] { "목소리를 따라간다", "귀를 막고 이동한다", "짧게 기도한다" }, data.Choices.Select(choice => choice.ChoiceText)); // 원문 선택 순서 확인
        StringAssert.Contains("이미 돌아갈 길이 옅어졌다.", data.Choices[0].ResultText); // 목소리 추적 결말 확인
        StringAssert.Contains("먼저 다가오는 소리가 들린다.", data.Choices[1].ResultText); // 귀 막기 결말 확인
        StringAssert.Contains("작은 병이 굴러 나온다.", data.Choices[2].ResultText); // 기도 결말 확인
        Object.DestroyImmediate(data); // 플레이 종료 시 사라지는 방랑자 임시 원본 재현
        var restored = ExplorationEventCatalog.LoadEvents(8).Single(item => item.EventId == AshWandererCryEvent.Id); // 손실된 버전 여덟 캐시 재생성
        Assert.IsNotNull(restored.IllustrationSprite); // 복구한 원본의 실제 삽화 확인
    } // 목록 검사 종료

    [UnityTest] // 회복 물약과 정신력 및 구버전 저장 검사
    public IEnumerator PrayerPaysOnlyOneLivingActorAndKeepsFullInventoryRoomOpen() // 기도 삼십 퍼센트의 경계와 실패 원자성 확인
    { // 기도 검사 시작
        yield return new EnterPlayMode(); // 실제 회차 관리자 시작
        CheckPrayer(); // 모드 진입 뒤 독립 저장 검사
        yield return new ExitPlayMode(); // 독립 회차 종료
    } // 기도 검사 종료

    private static void CheckPrayer() // 빈 슬롯과 가득 찬 슬롯의 기도 처리 확인
    { // 기도 처리 검사 시작
        var session = PrepareParty(); // 정상 출전 파티 준비
        var party = BattleResultManager.Instance; // 생존 상태 조회
        var actor = party.ActiveParty.Members[0]; // 정신력 회복을 받을 대상
        var other = party.ActiveParty.Members[1]; // 행동하지 않을 동료
        party.RestoreSavedAllyState(actor, 80, 93, 0); // 행동자의 정신력 회복 여유 설정
        party.RestoreSavedAllyState(other, 0, 48, 1); // 죽은 동료의 선택 차단 준비
        var inventory = ConsumableRunManager.EnsureInstance().Inventory; // 실제 소모품 보관함 조회
        int before = inventory.Count; // 기존 보유 수량 보관
        string won = "EV_F1_X87_Y1_S214"; // 기도 난수 이십구 경계
        string missed = "EV_F1_X1064_Y1_S214"; // 기도 난수 삼십 경계
        Assert.IsFalse(AshWandererCryEvent.TryResolve(won, AshWandererCryEvent.Pray, other, out _)); // 죽은 행동자의 선택 거부
        Assert.IsFalse(AshWandererCryEvent.TryResolve(won, AshWandererCryEvent.Pray, null, out _)); // 대상을 고르지 않은 기도 차단
        Assert.IsFalse(session.IsEventResolved(won)); // 비용 없는 취소 뒤 같은 방 유지
        Assert.IsTrue(AshWandererCryEvent.TryResolve(won, AshWandererCryEvent.Pray, actor, out _)); // 삼십 퍼센트 경계 안에서 회복 아이템 수령
        Assert.AreEqual(before + 1, inventory.Count); // 아이템 정확히 하나 증가
        var potion = Enumerable.Range(0, ConsumableInventoryRuntime.SlotCount).Select(slot => inventory.GetItem(slot)).OfType<PotionData>().First(item => item.EffectType == PotionEffectType.RestoreHealth); // 기존 체력 회복 원본 확인
        Assert.IsTrue(AshWandererCryEvent.TryResolve(missed, AshWandererCryEvent.Pray, actor, out _)); // 삼십 퍼센트 경계 밖에서 기도
        Assert.AreEqual(before + 1, inventory.Count); // 미당첨 기도에서 아이템 없음
        Assert.IsFalse(AshWandererCryEvent.TryResolve(won, AshWandererCryEvent.Pray, actor, out _)); // 같은 기도 보상 중복 차단
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out _, out int mental, out _)); // 두 기도 뒤 정신력 조회
        Assert.AreEqual(97, mental); // 대상에게만 두 번 각 두 점 회복
        Assert.IsTrue(party.TryGetSavedAllyState(other, out _, out int otherMental, out _)); // 비행동자 정신력 조회
        Assert.AreEqual(48, otherMental); // 다른 파티원 상태 유지
        while (inventory.Count < ConsumableInventoryRuntime.SlotCount) // 남은 모든 슬롯 채우기
        { // 보관함 준비 시작
            Assert.IsTrue(inventory.TryAcquire(potion, out _)); // 실제 획득 경로로 빈 슬롯 채우기
        } // 보관함 준비 종료
        string full = "EV_F1_X5_Y1_S214"; // 다시 당첨될 독립 방
        Assert.IsFalse(AshWandererCryEvent.TryResolve(full, AshWandererCryEvent.Pray, actor, out _)); // 당첨 물약을 넣을 수 없는 상황 확인
        Assert.IsFalse(session.IsEventResolved(full)); // 가득 찬 보관함에서 미처리 방 보존
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out _, out mental, out _)); // 지급 실패 뒤 정신력 확인
        Assert.AreEqual(97, mental); // 실패한 기도의 부분 회복 방지
        Assert.IsTrue(inventory.TryRemoveAt(0)); // 보관함 한 칸 비우기
        Assert.IsTrue(AshWandererCryEvent.TryResolve(full, AshWandererCryEvent.Pray, actor, out _)); // 기존 당첨 결과로 같은 방 재시도
        Assert.AreEqual(ConsumableInventoryRuntime.SlotCount, inventory.Count); // 재시도에서도 같은 물약 한 개 지급
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 기도 후 독립 저장
        try // 저장 정리 보장
        { // 기도 복원 검사 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 완료한 기도와 아이템 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 같은 체크포인트 복원
            Assert.IsFalse(AshWandererCryEvent.TryResolve(full, AshWandererCryEvent.Pray, actor, out _)); // 저장된 완료 방의 물약 중복 차단
            string modern = File.ReadAllText(path); // 최신 저장 원본 읽기
            string legacy = System.Text.RegularExpressions.Regex.Replace(modern, "\"wandererCry\"\\s*:\\s*\\{[^}]*\\}\\s*,", string.Empty); // 이전 형식의 누락 필드 재현
            Assert.AreNotEqual(modern, legacy); // 저장 필드 제거 확인
            File.WriteAllText(path, legacy); // 방랑자 필드 없는 이전 저장 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 저장의 정상 복원
            Assert.IsFalse(session.WandererCry.HasFollow || session.WandererCry.HasCoveredEars); // 이전 저장의 미예약 기본 상태 확인
        } // 기도 복원 검사 종료
        finally // 파일 정리 보장
        { // 임시 파일 정리 시작
            ExplorationSaveService.Remove(path); // 기도 저장과 복구본 정리
        } // 임시 파일 정리 종료
    } // 기도 처리 검사 종료

    [UnityTest] // 선제와 세 승패 경로를 실제 전투에서 검사
    public IEnumerator ActualBattleKeepsFirstHandAccuracyAndOnlyVictoryGrantsOnce([ValueSource(nameof(Outcomes))] int mode) // 저장 전후 같은 위험과 실제 승리 선택 확인
    { // 전투 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 자동 저장 격리 확인
        { // 사용자 저장 보호 시작
            Assert.Ignore("ProjectCVerification 검증 복제본에서 실행하세요."); // 일반 사용자 자동 저장 보호
        } // 사용자 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 시작
        yield return CheckBattle(mode); // 진입 후 시험할 실제 회차 구성
        yield return new ExitPlayMode(); // 전투 회차 종료
    } // 전투 검사 종료

    private static IEnumerator CheckBattle(int mode) // 실제 첫 진영과 승리 보상 UI 및 복귀 검사
    { // 실제 플레이 검사 시작
        SceneManager.LoadScene("00_Boot"); // 게임의 정상 관리자 시작
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 시작 메뉴 준비 대기
        var session = PrepareParty(); // 회차와 파티 생성
        var party = BattleResultManager.Instance; // 실제 파티 지속 상태 조회
        var actor = party.ActiveParty.Members[0]; // 목소리를 따라갈 행동자
        var other = party.ActiveParty.Members[1]; // 생존 보상 소유자로 선택할 동료
        party.RestoreSavedAllyState(actor, 80, 60, 0); // 행동자 비용 기준 설정
        party.RestoreSavedAllyState(other, 80, 60, 0); // 동료 상태 보존 기준 설정
        string room = mode == 0 ? Preempt : NoPreempt; // 성공 경계와 실패 경계 선택
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 전투 전 체크포인트
        try // 파일 정리 보장
        { // 탐사 선택과 전투 진행 시작
            Assert.IsTrue(AshWandererCryEvent.TryResolve(room, AshWandererCryEvent.Follow, actor, out _)); // 실제 세 카드와 선제 위험 예약
            Assert.IsFalse(AshWandererCryEvent.TryResolve(room, AshWandererCryEvent.Follow, actor, out _)); // 같은 방의 정신력 중복 차감 방지
            Assert.IsFalse(AshWandererCryEvent.TryResolve("another-follow", AshWandererCryEvent.Follow, actor, out _)); // 대기 추적을 덮어쓰지 않는 처리 확인
            Assert.IsFalse(session.IsEventResolved("another-follow")); // 두 번째 추적 방의 다른 선택 보존
            Assert.IsTrue(party.TryGetSavedAllyState(actor, out _, out int mental, out _)); // 행동자의 비용 확인
            Assert.AreEqual(55, mental); // 한 번만 정신력 다섯 차감
            Assert.IsTrue(party.TryGetSavedAllyState(other, out _, out mental, out _)); // 비행동자 상태 확인
            Assert.AreEqual(60, mental); // 동료 정신력 보존
            if (mode != 2) // 귀 막기 단독 및 들판과 중첩 검사
            { // 방향 감각 예약 시작
                Assert.IsTrue(AshWandererCryEvent.TryResolve("ears", AshWandererCryEvent.CoverEars, null, out _)); // 행동자 없이 전체 첫 턴 명중 감소 예약
            } // 방향 감각 예약 종료
            if (mode != 1) // 들판 단독 및 귀 막기와 중첩 검사
            { // 들판 예약 시작
                Assert.IsTrue(GlassFieldEvent.TryResolve("glass", GlassFieldEvent.RunBlind, null, out _)); // 별도 원인의 실명 예약
            } // 들판 예약 종료
            var cards = session.WandererCry.GetCards().ToArray(); // 원래 방에서 확정한 공용 후보
            Assert.AreEqual(3, cards.Distinct().Count()); // 서로 다른 카드 세 장 확인
            Assert.IsTrue(cards.All(card => card != null && CardSheetCatalog.Load().FindByName(card.DisplayName) != null)); // 실제 시트에 등록된 원본만 확인
            var normal = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 일반 조우 원본
            Assert.IsTrue(AshMapFragmentEvent.TryResolve("EV_F1_X86_Y1_S208", AshMapFragmentEvent.PickUp, null, out _)); // 전투 회피가 고정된 지도 조각 준비
            Assert.AreEqual(MapFragmentContact.Avoided, session.ResolveMapFragmentContact("skipped", normal, Vector3.right, path)); // 실제 전투 없이 지나친 조우 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 회피 직후 실전 체크포인트 복원
            CollectionAssert.AreEqual(cards, session.WandererCry.GetCards()); // 재로드 뒤 후보와 순서 고정 확인
            Assert.AreEqual(mode == 0, session.WandererCry.EnemyStartsFirst); // 복원 뒤 십오 퍼센트 선제 경계 고정 확인
            Assert.IsTrue(session.BeginEncounter("wanderer-fight", normal, Vector3.zero, Vector3.right)); // 실제 다음 교전 진입
            SceneFlowManager.Instance.LoadScene("40_Battle"); // 실제 전투 화면 열기
            yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true); // 전투 시작 대기
            var setup = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 씬의 턴과 유닛 조회
            if (mode == 0) // 적 선제 성공 확인
            { // 적 선제 검사 시작
                Assert.AreEqual(BattleTurnPhase.EnemyTurn, setup.BattleTurn.CurrentPhase); // 아군 턴 없이 실제 적부터 행동
                Assert.AreEqual(0, setup.BattleDeck.HandCount); // 선제 전에 아군 손패 미소비 확인
                yield return WaitFor(() => setup.BattleTurn.IsPlayerTurn); // 실제 적 행동과 첫 아군 턴 대기
                Assert.Less(setup.AllyUnits.Sum(unit => unit.CurrentHealth), 160); // 선제 적 공격이 실제 파티에 준 피해 확인
            } // 적 선제 검사 종료
            Assert.AreEqual(1, setup.BattleTurn.CurrentRound); // 첫 플레이어 턴은 항상 일 라운드
            Assert.AreEqual(8, setup.BattleDeck.HandCount); // 선제 여부와 무관한 정상 시작 손패 여덟 장
            Assert.AreEqual(6, setup.SharedActionPoints.CurrentActionPoints); // 정상 첫 아군 행동력 확인
            int chance = mode == 0 ? 75 : mode == 1 ? 90 : 85; // 중첩과 귀 단독 및 들판 단독 경계
            foreach (var ally in setup.AllyUnits) // 모든 출전 아군 검사
            { // 개별 명중 확인 시작
                Assert.AreEqual(chance, BattleHitChance.GetPercent(ally)); // 원인별 실제 명중률 확인
                Assert.IsTrue(BattleHitChance.Hits(ally, chance - 1)); // 명중 성공 상한 확인
                Assert.IsFalse(BattleHitChance.Hits(ally, chance)); // 명중 실패 하한 확인
            } // 개별 명중 확인 종료
            var enemy = setup.EnemyUnits[0]; // 기존 단독 실명 시험 대상
            enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1); // 기존 윈터폴 적 실명 부여
            Assert.AreEqual(75, BattleHitChance.GetPercent(enemy)); // 단독 실명 이십오 퍼센트포인트 유지
            Assert.IsFalse(session.WandererCry.TryClaim("wanderer-fight", cards[0], other)); // 승리 전 카드 지급 차단
            if (mode == 0) // 추적 전투 승리 경로 확인
            { // 실제 승리 선택 시작
                Assert.IsTrue(setup.BattleTurn.EndPlayerTurn()); // 첫 아군 턴 종료
                yield return WaitFor(() => setup.BattleTurn.IsPlayerTurn && setup.BattleTurn.CurrentRound == 2); // 실제 적 턴과 두 번째 아군 턴 대기
                Assert.AreEqual(100, BattleHitChance.GetPercent(setup.AllyUnits[0])); // 한 턴 뒤 양쪽 명중 감소 만료
                setup.AllyUnits[0].TakeDamage(10000, BattleDamageType.None); // 보상을 받을 수 없는 전사자 생성
                enemy.TakeDamage(10000, BattleDamageType.None); // 실제 사망 판정으로 승리 처리
                yield return WaitFor(() => GameObject.Find("WandererCryReward") != null); // 실제 세 카드 선택 화면 대기
                var reward = GameObject.Find("WandererCryReward").transform; // 실제 승리 추가 보상 창 조회
                var cardCanvas = reward.GetComponent<Canvas>(); // 전투 HUD와 분리한 보상 입력 층 조회
                var hud = Object.FindFirstObjectByType<PlayerLevelHudView>().GetComponent<Canvas>(); // 실제 레벨과 경험치 표시 층 조회
                Assert.IsTrue(cardCanvas.overrideSorting && cardCanvas.sortingOrder > hud.sortingOrder); // 보상 카드가 실제 레벨 HUD 위에 표시되는지 확인
                var firstButton = reward.Find("Card_" + cards[0].CardId).GetComponent<Button>(); // 화면 중앙의 첫 후보 버튼 조회
                yield return ClickOnTop(firstButton); // 화면 위 실제 포인터 입력으로 첫 후보 선택
                var owners = reward.Find("TreasureOwnerCanvas/Panel"); // 실제 소유자 선택 창 조회
                Assert.IsNotNull(owners); // 실제 소유자 선택 창 열림 확인
                Assert.IsNull(owners.Find("Owner_" + actor.CharacterId)); // 전투용 생존 명단에서 방금 사망한 파티원 제외
                Assert.Greater(owners.GetComponentInParent<Canvas>().sortingOrder, cardCanvas.sortingOrder); // 소유자 모달이 카드와 HUD 위에 표시되는지 확인
                var livingButton = owners.Find("Owner_" + other.CharacterId).GetComponent<Button>(); // 실제 생존 보상 수령자 조회
                Assert.IsTrue(livingButton.interactable); // 살아 있는 소유자만 선택 가능
                yield return ClickOnTop(owners.Find("CancelButton").GetComponent<Button>()); // 실제 소유자 창에서 카드 후보 선택으로 복귀
                Assert.IsTrue(session.WandererCry.HasVictoryChoice); // 소유자 선택 취소 시 승리 보상 유지
                int before = Count(other, cards[1]); // 선택한 동료의 해당 카드 수
                yield return ClickOnTop(reward.Find("Card_" + cards[1].CardId).GetComponent<Button>()); // 실제 포인터로 다른 후보 선택
                yield return ClickOnTop(reward.Find("TreasureOwnerCanvas/Panel/Owner_" + other.CharacterId).GetComponent<Button>()); // 실제 생존자 버튼에 입력해 확정 지급
                Assert.AreEqual(before + 1, Count(other, cards[1])); // 확정한 카드 한 장과 소유자 확인
                Assert.IsFalse(session.WandererCry.TryClaim("wanderer-fight", cards[2], other)); // 다른 후보의 중복 지급 차단
            } // 실제 승리 선택 종료
            else if (mode == 1) // 추적한 전투 도주 경로
            { // 도주 검사 시작
                Assert.IsTrue(setup.BattleTurn.TryEscape()); // 실제 일반 전투 도주
            } // 도주 검사 종료
            else // 추적한 전투 패배 경로
            { // 패배 검사 시작
                foreach (var ally in setup.AllyUnits) // 출전 아군 전원 순회
                { // 실제 사망 처리 시작
                    ally.TakeDamage(10000, BattleDamageType.None); // 남은 아군 쓰러뜨리기
                } // 실제 사망 처리 종료
            } // 패배 검사 종료
            yield return WaitFor(() => Object.FindFirstObjectByType<BattleResultView>()?.transform.Find("ConfirmButton") != null); // 기존 승패 결과 화면 대기
            Assert.IsFalse(session.WandererCry.HasFollow || session.WandererCry.HasVictoryChoice); // 승리 지급과 도주 및 패배 뒤 추적 소모
            Assert.IsFalse(session.WandererCry.TryClaim("wanderer-fight", cards[0], other)); // 모든 종료 경로의 재지급 차단
            Object.FindFirstObjectByType<BattleResultView>().transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 원래 결과 확인으로 탐사 복귀
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "30_Exploration" && session.ActiveEncounter == null); // 탐사 승패와 위치 복원 대기
            if (mode != 2) // 전멸 이외의 탐사 저장 확인
            { // 전투 뒤 저장 검사 시작
                int total = Count(other, cards[1]); // 복귀 후 실제 카드 수 기록
                Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 소비 결과와 소유권 저장
                Assert.IsTrue(ExplorationSaveService.Load(path)); // 복귀 후 탐사 상태 복원
                Assert.IsFalse(session.WandererCry.HasFollow || session.WandererCry.HasVictoryChoice || session.WandererCry.HasCoveredEars); // 사용한 효과와 선택권 재생성 차단
                Assert.AreEqual(total, Count(other, cards[1])); // 재로드 후 카드 수 유지
            } // 전투 뒤 저장 검사 종료
            else // 전멸 정산 확인
            { // 전멸 검사 시작
                Assert.IsTrue(session.IsExplorationCompleted && !session.IsExplorationSuccess); // 원래 탐사 실패 처리 확인
            } // 전멸 검사 종료
        } // 탐사 선택과 전투 진행 종료
        finally // 임시 저장 정리 보장
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 검사 체크포인트와 복구본 정리
        } // 파일 정리 종료
    } // 실제 플레이 검사 종료

    private static IEnumerator ClickOnTop(Button button) // 실제 렌더링과 입력 순서를 확인한 뒤 버튼 클릭
    { // 화면 입력 검사 시작
        Canvas.ForceUpdateCanvases(); // 방금 열린 모달의 실제 위치와 크기 갱신
        var graphic = button.targetGraphic; // 입력할 버튼의 실제 그래픽 조회
        var raycaster = graphic.GetComponentInParent<GraphicRaycaster>(); // 이 모달의 입력 카메라와 표시 영역 조회
        float deadline = Time.realtimeSinceStartup + 10f; // 첫 렌더링 대기 제한
        while (graphic.depth < 0 && Time.realtimeSinceStartup < deadline) // 아직 그려지지 않은 버튼 대기
        { // 프레임 대기 시작
            yield return null; // 게임 화면의 실제 렌더링 진행
        } // 프레임 대기 종료
        Assert.GreaterOrEqual(graphic.depth, 0, button.name + " 첫 렌더링"); // 실제 입력 대상으로 등록된 버튼 확인
        var rect = (RectTransform)button.transform; // 실제 클릭 영역 조회
        var screen = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera, rect.TransformPoint(rect.rect.center)); // 이 캔버스의 실제 카메라로 버튼 중앙 투영
        var pointer = new PointerEventData(EventSystem.current); // 실제 화면 입력 기록 생성
        pointer.position = screen; // 버튼 중앙에 포인터 배치
        var hits = new System.Collections.Generic.List<RaycastResult>(); // 입력을 받을 순서대로 정렬된 그래픽 목록
        EventSystem.current.RaycastAll(pointer, hits); // 실제 모달과 뒤쪽 HUD의 입력 우선순위 조회
        Assert.IsNotEmpty(hits, $"{button.name} · 좌표 {screen} · 화면 {Screen.width}×{Screen.height} · 캔버스 {graphic.canvas.pixelRect} · 표시 장치 {graphic.canvas.targetDisplay} · 깊이 {graphic.depth}"); // 입력 실패 시 실제 표시 좌표와 크기 확인
        Transform first = hits[0].gameObject.transform; // 실제로 먼저 입력을 받는 그래픽 조회
        Assert.IsTrue(first == rect || first.IsChildOf(rect), first.name); // 선택 버튼 또는 그 안의 그림이 최상단인지 확인
        ExecuteEvents.ExecuteHierarchy(first.gameObject, pointer, ExecuteEvents.pointerClickHandler); // 실제 최상단 그래픽에 포인터 클릭 전달
    } // 화면 입력 검사 종료

    private static int Count(CharacterData owner, CardData card) // 실제 소유자별 회차 카드 수 확인
    { // 카드 수 조회 시작
        return RunDeckManager.Instance.GetActiveCards(PartyLoadoutRuntime.Active.Deck).Count(entry => entry.Owner == owner && entry.Card == card); // 같은 카드와 소유자 수 반환
    } // 카드 수 조회 종료

    private static ExplorationSessionManager PrepareParty() // 출전 가능한 실제 파티 두 명 준비
    { // 편성 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 정상 출전 규칙 사용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전자 선택
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 둘째 출전자 선택
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 정상 출전 덱 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 덱 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 세션 관리자 준비
        session.ResetExploration(); // 이전 방과 전투 효과 초기화
        session.SetCurrentFloorSeed(214); // 독립 층 시드 준비
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 생존자 상태 연결
        return session; // 출전 가능한 탐사 반환
    } // 편성 준비 종료

    private static IEnumerator WaitFor(Func<bool> ready) // 실제 씬과 연출 조건 대기
    { // 대기 시작
        float deadline = Time.realtimeSinceStartup + 30f; // 실행 제한 시각
        while (!ready() && Time.realtimeSinceStartup < deadline) // 실제 결과 조건 확인
        { // 프레임 대기 시작
            yield return null; // 게임 연출과 화면 생성 진행
        } // 프레임 대기 종료
        Assert.IsTrue(ready()); // 게임 진행 완료 확인
    } // 대기 종료
} // 검사 종료
