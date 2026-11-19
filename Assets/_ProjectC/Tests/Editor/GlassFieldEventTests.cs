using System; // 시간 제한과 고정 방 번호 사용
using System.Collections; // 플레이 모드와 씬 대기
using System.IO; // 독립 저장 파일 사용
using System.Linq; // 원본과 소유 카드 조회
using NUnit.Framework; // 행동과 전투 결과 검증
using UnityEngine; // 실제 씬과 게임 리소스 사용
using UnityEngine.SceneManagement; // 실제 전투 씬 이동
using UnityEngine.TestTools; // 플레이 모드 테스트
using UnityEngine.UI; // 행동자 선택과 취소 버튼 사용
using Object = UnityEngine.Object; // 실제 유닛과 화면 조회

public sealed class GlassFieldEventTests // 유리화된 들판의 선택과 다음 교전 검사
{ // 검사 시작
    [Test] // 기존 층과 원문 선택지 검사
    public void VersionFiveAddsOnlyGlassFieldAndKeepsAuthoredEndings() // 이전 이벤트 순서와 새 방 기획 원문 확인
    { // 목록 검사 시작
        var old = ExplorationEventCatalog.LoadEvents(4); // 허수아비까지 있는 이전 층
        var current = ExplorationEventCatalog.LoadEvents(5); // 들판을 포함한 새 층
        CollectionAssert.AreEqual(old.Select(item => item.EventId).Append(GlassFieldEvent.Id).OrderBy(id => id, StringComparer.Ordinal), current.Select(item => item.EventId)); // 새 후보 한 개와 고정 정렬 확인
        var data = current.Single(item => item.EventId == GlassFieldEvent.Id); // 등록된 들판 원본 조회
        Assert.IsNotNull(data.IllustrationSprite); // 들판 전용 삽화 확인
        CollectionAssert.AreEqual(new[] { "유리를 깨서 꺼낸다", "조심히 지나간다", "눈을 감고 달린다" }, data.Choices.Select(choice => choice.ChoiceText)); // 원문 선택지 순서 확인
        StringAssert.Contains("다만 당신의 손에 남았다.", data.Choices[0].ResultText); // 유리 깨기 결말 확인
        StringAssert.Contains("익숙한 공포가 몸을 가볍게 만든다.", data.Choices[1].ResultText); // 조심히 통과한 결말 확인
        StringAssert.Contains("당신은 잠깐 여기가 어디인지 잊는다.", data.Choices[2].ResultText); // 눈감고 달린 결말 확인
    } // 목록 검사 종료

    [UnityTest] // 두 카드 분기와 사망 및 취소 검사
    public IEnumerator BreakPaysOnlyTheLivingActorAndEachBranchPersistsOnce() // 비용과 카드가 모두 같은 행동자에게 한 번 적용되는지 확인
    { // 카드 검사 시작
        yield return new EnterPlayMode(); // 실제 파티와 덱 관리자 시작
        CheckBreakChoices(); // 모드 진입 후 행동자별 보상 확인
        yield return new ExitPlayMode(); // 검사 회차 종료
    } // 카드 검사 종료

    private static void CheckBreakChoices() // 독립 저장에서 희귀와 저주 분기 검증
    { // 카드 검증 시작
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 실제 플레이어와 분리한 저장 경로
        try // 저장 정리 보장
        { // 분기 검증 시작
            var session = PrepareParty(); // 실제 출전 파티 준비
            var party = BattleResultManager.Instance; // 지속 체력 관리자 조회
            var actor = party.ActiveParty.Members[0]; // 유리를 깰 생존자
            var other = party.ActiveParty.Members[1]; // 직접 행동하지 않는 동료
            party.RestoreSavedAllyState(actor, 15, 50, 0); // 두 번의 체력 비용 확인 준비
            party.RestoreSavedAllyState(other, 0, 50, 1); // 선택 불가능한 사망 동료 지정
            string rareId = FindRoom(false); // 희귀 카드가 나오는 고정 방 조회
            string curseId = FindRoom(true); // 저주가 나오는 고정 방 조회
            var data = ExplorationEventCatalog.LoadEvents(5).Single(item => item.EventId == GlassFieldEvent.Id); // 실제 이벤트 화면 원본
            var marker = new GameObject("GlassFieldTest").AddComponent<ExplorationEventView>(); // 선택 창을 열 실제 표식 생성
            marker.Initialize(data, rareId); // 희귀 분기 방 연결
            var panel = ExplorationEventPanelView.EnsureInstance(); // 실제 이벤트 선택 화면 조회
            panel.ShowEvent(marker, data, rareId); // 유리를 깨는 선택지 열기
            panel.SendMessage("HandleChoiceSelected", (object)GlassFieldEvent.BreakGlass); // 생존자 선택 요청
            var chooser = panel.transform.Find("TreasureOwnerCanvas/Panel"); // 실제 행동자 선택 창 조회
            Assert.IsFalse(chooser.Find("Owner_" + other.CharacterId).GetComponent<Button>().interactable); // 사망자의 버튼 선택 차단
            chooser.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 대상을 고르기 전에 취소
            Assert.IsFalse(session.IsEventResolved(rareId)); // 취소한 방 재선택 가능 확인
            Assert.IsFalse(GlassFieldEvent.TryResolve(rareId, GlassFieldEvent.BreakGlass, other, out _)); // 사망자 직접 입력 차단
            Assert.IsTrue(party.TryGetSavedAllyState(actor, out int health, out _, out _)); // 선택 취소 뒤 체력 확인
            Assert.AreEqual(15, health); // 선택 취소와 실패 시 미차감 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 결과를 받기 전 체크포인트 저장
            var rare = WinterfallRiskCardReward.GetReward(rareId); // 방에 고정된 희귀 원본 확인
            int before = CountCards(actor, rare); // 행동자가 이미 가진 같은 카드 수
            panel.SendMessage("HandleChoiceSelected", (object)GlassFieldEvent.BreakGlass); // 유리 깨기 재선택
            panel.transform.Find("TreasureOwnerCanvas/Panel/Owner_" + actor.CharacterId).GetComponent<Button>().onClick.Invoke(); // 실제 버튼으로 행동자와 소유자 확정
            Assert.AreEqual(before + 1, CountCards(actor, rare)); // 선택한 행동자의 희귀 카드 한 장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 결과를 받기 전 체크포인트 복원
            Assert.AreEqual(rare, WinterfallRiskCardReward.GetReward(rareId)); // 불러오기 후 같은 카드 고정 확인
            Assert.AreEqual(before, CountCards(actor, rare)); // 이전 체크포인트의 보유량 복원 확인
            Assert.IsTrue(GlassFieldEvent.TryResolve(rareId, GlassFieldEvent.BreakGlass, actor, out _)); // 같은 방 희귀 결과 재현
            Assert.IsFalse(GlassFieldEvent.TryResolve(rareId, GlassFieldEvent.BreakGlass, actor, out _)); // 처리한 방의 비용과 보상 중복 차단
            Assert.IsTrue(party.TryGetSavedAllyState(actor, out health, out _, out _)); // 단일 체력 비용 확인
            Assert.AreEqual(9, health); // 여섯 점만 차감 확인
            Assert.AreEqual(before + 1, CountCards(actor, rare)); // 중복 시도 뒤 보유량 유지 확인
            var curse = WinterfallRiskCardReward.GetReward(curseId); // 저주 분기 원본 조회
            Assert.AreEqual(WinterfallRiskCardReward.CurseCardId, curse.CardId); // 실제 잿빛 저주 원본 확인
            Assert.IsTrue(GlassFieldEvent.TryResolve(curseId, GlassFieldEvent.BreakGlass, actor, out _)); // 별도 방에서 행동자의 저주 지급
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.right)); // 두 결과 확정 뒤 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 체력과 두 카드 소유권 복원
            Assert.AreEqual(1, CountCards(actor, curse)); // 행동자의 저주 한 장 확인
            Assert.AreEqual(0, CountCards(other, curse)); // 다른 동료에게 저주 미지급 확인
            Assert.IsTrue(party.TryGetSavedAllyState(actor, out health, out _, out _)); // 두 선택 뒤 행동자 체력 읽기
            Assert.AreEqual(3, health); // 성공한 두 번만 비용 차감 확인
            Assert.IsFalse(GlassFieldEvent.TryResolve(curseId, GlassFieldEvent.BreakGlass, actor, out _)); // 저장된 저주 중복 지급 차단
            Assert.IsTrue(GlassFieldEvent.TryResolve("final-glass", GlassFieldEvent.BreakGlass, actor, out _)); // 마지막 생존자의 치명적 유리 깨기
            Assert.IsTrue(session.IsExplorationCompleted && !session.IsExplorationSuccess); // 기존 위험과 같은 전멸 정산 확인
            Assert.AreEqual(0, party.GetLivingAllyCount()); // 마지막 생존자 사망 확인
            Object.Destroy(marker.gameObject); // 독립 방 표식 정리
        } // 분기 검증 종료
        finally // 임시 저장 정리
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 체크포인트와 복구본 정리
        } // 파일 정리 종료
    } // 카드 검증 종료

    [UnityTest] // 전투 효과 대기와 실제 씬 실행 검사
    public IEnumerator NextActualEncounterAppliesToAllLivingAlliesForTwoEnemyRoundsAndOnePlayerTurn() // 회피와 실명의 실제 명중률과 만료 시점 검증
    { // 실제 전투 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장용 프로필 격리 확인
        { // 프로필 보호 시작
            Assert.Ignore("ProjectCVerification 검증 복제본에서 실행하세요."); // 실제 플레이어 저장 보호
        } // 프로필 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 실행
        yield return CheckBattle(); // 진입 후 새 코루틴으로 씬 검사 실행
        yield return new ExitPlayMode(); // 게임 실행 종료
    } // 실제 전투 검사 종료

    private static IEnumerator CheckBattle() // 실제 지도 회피와 전투 시작 및 턴 진행 검사
    { // 실제 교전 검사 시작
        SceneManager.LoadScene("00_Boot"); // 게임의 기본 관리자 초기화
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null && !SceneFlowManager.Instance.IsLoadingScene); // 부트 완료와 메뉴 대기
        var session = PrepareParty(); // 정상 출전 파티 준비
        var normal = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 실제 일반 조우 조회
        string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 전투 시작 전 독립 저장 경로
        try // 저장 정리 보장
        { // 대기 효과 검사 시작
            Assert.IsTrue(GlassFieldEvent.TryResolve("evade", GlassFieldEvent.CrossCarefully, null, out _)); // 조심히 통과한 방 확정
            Assert.IsTrue(GlassFieldEvent.TryResolve("evade-again", GlassFieldEvent.CrossCarefully, null, out _)); // 별도 방에서 같은 회피 예약
            Assert.IsTrue(GlassFieldEvent.TryResolve("blind", GlassFieldEvent.RunBlind, null, out _)); // 별도 방에서 실명 함께 예약
            Assert.IsFalse(GlassFieldEvent.TryResolve("blind", GlassFieldEvent.CrossCarefully, null, out _)); // 같은 방 선택 변경 차단
            var pending = GlassFieldEffect.Evasion | GlassFieldEffect.Blind; // 다음 교전의 두 효과
            Assert.IsTrue(AshMapFragmentEvent.TryResolve("EV_F1_X86_Y1_S208", AshMapFragmentEvent.PickUp, null, out _)); // 회피 성공이 고정된 지도 조각 보관
            Assert.AreEqual(MapFragmentContact.Avoided, session.ResolveMapFragmentContact("skipped-fight", normal, Vector3.right, path)); // 실제 전투 없이 첫 일반 조우 통과
            Assert.AreEqual(pending, session.PendingGlassFieldEffects); // 교전을 피한 뒤 들판 효과 보존
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 회피 직후 저장 복원
            Assert.AreEqual(pending, session.PendingGlassFieldEffects); // 복원한 두 대기 효과 확인
            string modern = File.ReadAllText(path); // 이전 저장 호환 확인용 원본 보관
            string legacy = System.Text.RegularExpressions.Regex.Replace(modern, "\"glassFieldEffects\"\\s*:\\s*3\\s*,", string.Empty); // 이전 형식의 누락 필드 재현
            Assert.AreNotEqual(modern, legacy); // 이전 형식 재현 확인
            File.WriteAllText(path, legacy); // 대기 효과 없는 이전 저장 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 형식 복원
            Assert.AreEqual(GlassFieldEffect.None, session.PendingGlassFieldEffects); // 이전 저장의 효과 미보유 확인
            File.WriteAllText(path, modern); // 두 효과를 보관한 저장 복구
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 전투 직전 체크포인트 복원
            Assert.IsTrue(session.BeginEncounter("real-glass-battle", normal, Vector3.zero, Vector3.right)); // 회피하지 않은 실제 조우 진입
            SceneFlowManager.Instance.LoadScene("40_Battle"); // 게임 전투 씬으로 전환
            yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true && !SceneFlowManager.Instance.IsLoadingScene); // 전투 첫 턴 준비 대기
            var battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 아군과 적 및 턴 관리자 조회
            Assert.AreEqual(GlassFieldEffect.None, session.PendingGlassFieldEffects); // 전투 시작 직후 대기 소모
            var ally = battle.AllyUnits[0]; // 실제 출전 행동자 조회
            var enemy = battle.EnemyUnits[0]; // 실제 적 공격자 조회
            foreach (var member in battle.AllyUnits.Where(unit => !unit.IsDead)) // 모든 출전 생존자 순회
            { // 개인별 첫 턴 상태 확인 시작
                Assert.AreEqual(85, BattleHitChance.GetPercent(member)); // 모든 아군 실명 십오 퍼센트포인트 확인
                Assert.AreEqual(90, BattleHitChance.GetPercent(enemy, member)); // 모든 아군 회피 십 퍼센트포인트 확인
                Assert.AreEqual(2, member.GetStatusEffect(BattleStatusEffectType.Evasion).RemainingTurns); // 같은 회피 예약 중복 기간 방지
            } // 개인별 첫 턴 상태 확인 종료
            Assert.IsTrue(BattleHitChance.Hits(ally, 84)); // 첫 턴 아군 명중 성공 경계 확인
            Assert.IsFalse(BattleHitChance.Hits(ally, 85)); // 첫 턴 아군 명중 실패 경계 확인
            Assert.IsTrue(Attack(enemy, ally, 90).Execute().WasMissed); // 실제 적 공격의 회피 성공 경계 확인
            Assert.IsFalse(Attack(enemy, ally, 89).Execute().WasMissed); // 실제 적 공격의 회피 실패 경계 확인
            Assert.IsTrue(battle.BattleTurn.EndPlayerTurn()); // 첫 아군 턴 종료
            yield return WaitFor(() => battle.BattleTurn.IsPlayerTurn && battle.BattleTurn.CurrentRound == 2); // 첫 실제 적 턴 종료 대기
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 아군 한 턴 뒤 들판 실명 만료
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Evasion).RemainingTurns); // 둘째 적 턴까지 회피 유지
            GlassFieldEvent.ApplyBattleStart(session, battle.AllyUnits); // 같은 교전의 초기화 재호출
            Assert.AreEqual(1, ally.GetStatusEffect(BattleStatusEffectType.Evasion).RemainingTurns); // 중복 적용으로 기간 갱신 차단
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 소비한 실명 재적용 차단
            Assert.IsTrue(battle.BattleTurn.EndPlayerTurn()); // 두 번째 아군 턴 종료
            yield return WaitFor(() => battle.BattleTurn.IsPlayerTurn && battle.BattleTurn.CurrentRound == 3); // 두 번째 실제 적 턴 종료 대기
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Evasion)); // 적 턴 두 번 뒤 회피 만료
            enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 25, 1, 1); // 기존 윈터폴 실명 적용
            Assert.IsTrue(Attack(enemy, ally, 75).Execute().WasMissed); // 기존 실명 이십오 퍼센트포인트 경계 확인
            Assert.IsFalse(Attack(enemy, ally, 74).Execute().WasMissed); // 기존 실명 적중 경계 유지
        } // 대기 효과 검사 종료
        finally // 독립 저장 정리
        { // 체크포인트 정리 시작
            ExplorationSaveService.Remove(path); // 저장 파일과 복구본 정리
        } // 체크포인트 정리 종료
    } // 실제 교전 검사 종료

    private static BattleEnemyAction Attack(BattleUnitRuntime enemy, BattleUnitRuntime target, int roll) // 경계값을 고정한 실제 적 공격
    { // 공격 생성 시작
        return new BattleEnemyAction(enemy, target, EnemyActionType.Attack, BattleDamageType.Physical, 1, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "회피 확인", 1, 1, 1, 0, percentRoll: () => roll); // 실제 공격 처리기에 지정 난수 전달
    } // 공격 생성 종료

    private static int CountCards(CharacterData owner, CardData card) // 소유자별 실제 회차 카드 수
    { // 보유량 확인 시작
        return RunDeckManager.Instance.GetActiveCards(PartyLoadoutRuntime.Active.Deck).Count(entry => entry.Card == card && entry.Owner == owner); // 지정 소유자의 동일 카드 수 반환
    } // 보유량 확인 종료

    private static string FindRoom(bool curse) // 두 분기를 재현하는 고정 방 조회
    { // 방 조회 시작
        for (int index = 0; index < 200; index++) // 고정 방 후보 순회
        { // 분기 조회 시작
            string id = "EV_glass_" + index; // 검사 전용 방 식별자
            var card = WinterfallRiskCardReward.GetReward(id); // 고정 시드의 실제 보상 조회
            if (card != null && (card.CardId == WinterfallRiskCardReward.CurseCardId) == curse) // 요청한 분기의 실제 원본 확인
            { // 방 선택 시작
                return id; // 해당 분기 방 반환
            } // 방 선택 종료
        } // 분기 조회 종료
        Assert.Fail("희귀 또는 저주 분기의 실제 카드를 찾지 못했습니다."); // 보상 등록 누락 확인
        return string.Empty; // 검사 종료 기본값
    } // 방 조회 종료

    private static ExplorationSessionManager PrepareParty() // 실제 두 명의 출전 파티와 회차 덱 준비
    { // 편성 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 출전 명단 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 생존과 출전 조건 적용
        Assert.IsTrue(selection.TryAdd(roster.Characters[0])); // 첫 출전자 편성
        Assert.IsTrue(selection.TryAdd(roster.Characters[1])); // 두 번째 출전자 편성
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 실제 파티와 시작 덱 생성
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 회차 카드와 소유권 등록
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 상태 조회
        session.ResetExploration(); // 완료와 전투 효과 초기화
        session.SetCurrentFloorSeed(211); // 검사 층 고정 시드 지정
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(PartyLoadoutRuntime.Active.Party)); // 출전자 지속 상태 등록
        return session; // 출전 준비 결과 반환
    } // 편성 준비 종료

    private static IEnumerator WaitFor(Func<bool> ready) // 실제 씬 로드와 전투 연출 대기
    { // 실행 대기 시작
        float limit = Time.realtimeSinceStartup + 30f; // 게임 진행 확인 제한 시각
        while (!ready() && Time.realtimeSinceStartup < limit) // 실제 게임 진행 조건 확인
        { // 프레임 대기 시작
            yield return null; // 게임의 다음 프레임 진행
        } // 프레임 대기 종료
        Assert.IsTrue(ready()); // 실제 씬 또는 턴 진행 완료 확인
    } // 실행 대기 종료
} // 검사 종료
