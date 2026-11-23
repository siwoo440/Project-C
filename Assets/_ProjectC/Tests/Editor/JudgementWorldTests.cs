using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 카드 목록 사용
using System.Reflection; // 실제 카드 처리 호출 사용
using NUnit.Framework; // 전용 능력 규칙 검증 사용
using UnityEngine; // 실제 캐릭터 데이터 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용
public sealed class JudgementWorldTests // 심판과 세계 임시 능력 검사
{ // 전용 능력 검사 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 독립 출전 가능 상태 제공자
    { // 출전 상태 제공자 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태
    } // 출전 상태 제공자 종료
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresJudgementAndWorldCards() // 해금된 세계와 심판 카드의 실제 저장 복원 확인
    { // 실제 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        string key = "ProjectC.Test.World141." + System.Guid.NewGuid().ToString("N"); // 독립 세계 해금 저장 키 생성
        try // 독립 저장 자료 정리 보장
        { // 실제 저장 복원 검사 시작
            SceneManager.LoadScene("00_Boot"); // 시작 씬 열기
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 로비 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 대기
            { // 로비 대기 시작
                yield return null; // 다음 프레임 대기
            } // 로비 대기 종료
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
            CharacterUnlockProgress progress = new CharacterUnlockProgress(key); // 독립 세계 해금 상태 생성
            PartySelection selection = new PartySelection(new AvailableStatus(), progress); // 독립 해금 상태의 편성 생성
            Assert.IsFalse(selection.TryAdd(roster.Characters[21])); // 해금 전 세계 편성 차단 확인
            progress.RecordFinalClear(); // 독립 최종 탐사 승리 기록
            Assert.IsTrue(selection.TryAdd(roster.Characters[20])); // 심판 편성 확인
            Assert.IsTrue(selection.TryAdd(roster.Characters[21])); // 해금 후 세계 편성 확인
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 심판과 세계 시작 덱 생성 확인
            ExplorationSessionManager.EnsureInstance().ResetExploration(); // 새 탐사 상태 초기화
            RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 시작 덱 적용
            SceneFlowManager.Instance.LoadScene("30_Exploration"); // 실제 탐사 씬 이동
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 대기
            { // 플레이어 대기 시작
                yield return null; // 다음 프레임 대기
            } // 플레이어 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
            yield return null; // 탐사 초기화 대기
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 전용 카드 포함 탐사 저장 확인
            ExplorationSessionManager.Instance.ResetExploration(); // 현재 탐사 상태 제거
            RunDeckManager.Instance.ResetToSource(null); // 현재 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 현재 편성 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 탐사 저장 복원 확인
            bool judgementFound = false; // 심판 카드 복원 여부 초기화
            bool worldFound = false; // 세계 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                judgementFound |= entry.Card.CardId == "CRD_ARC_20_RESCUE" && entry.Owner.CharacterId == "ARC_20"; // 심판 카드와 소유자 확인
                worldFound |= entry.Card.CardId == "CRD_ARC_21_LINK" && entry.Owner.CharacterId == "ARC_21"; // 세계 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(judgementFound); // 심판 카드 복원 확인
            Assert.IsTrue(worldFound); // 세계 카드 복원 확인
            Assert.IsTrue(new CharacterUnlockProgress(key).IsUnlocked(roster.Characters[21])); // 해금 상태 재생성 후 유지 확인
        } // 실제 저장 복원 검사 종료
        finally // 독립 저장 자료 정리
        { // 저장 자료 정리 시작
            ExplorationSaveService.Remove(path); // 탐사 테스트 저장 제거
            PlayerPrefs.DeleteKey(key); // 독립 해금 상태 제거
            PlayerPrefs.Save(); // 독립 해금 상태 제거 저장
        } // 저장 자료 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 저장 검사 종료
    [UnityTest] // 실제 심판과 세계 카드 검사 지정
    public IEnumerator StarterCards_HealAtRiskAndProtectLowestOtherAlly() // 실제 카드 효과와 동료 연계 확인
    { // 실제 카드 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData deathData = roster.Characters[13]; // 죽음 캐릭터 조회
        CharacterData sunData = roster.Characters[19]; // 태양 캐릭터 조회
        CharacterData judgementData = roster.Characters[20]; // 심판 캐릭터 조회
        CharacterData worldData = roster.Characters[21]; // 세계 캐릭터 조회
        CardData deathCard = Resources.Load<CardData>("StarterCards/DeathFinisher"); // 죽음 전용 카드 조회
        CardData sunCard = Resources.Load<CardData>("StarterCards/SunChain"); // 태양 전용 카드 조회
        CardData rescueCard = Resources.Load<CardData>("StarterCards/JudgementRescue"); // 심판 전용 카드 조회
        CardData worldCard = Resources.Load<CardData>("StarterCards/WorldLink"); // 세계 전용 카드 조회
        Assert.IsNotNull(rescueCard); // 심판 전용 카드 존재 확인
        Assert.IsNotNull(worldCard); // 세계 전용 카드 존재 확인
        BattleUnitRuntime death = BattleUnitRuntime.CreateAlly(deathData); // 죽음 전투 유닛 생성
        BattleUnitRuntime sun = BattleUnitRuntime.CreateAlly(sunData); // 태양 전투 유닛 생성
        BattleUnitRuntime judgement = BattleUnitRuntime.CreateAlly(judgementData); // 심판 전투 유닛 생성
        BattleUnitRuntime world = BattleUnitRuntime.CreateAlly(worldData); // 세계 전투 유닛 생성
        BattleUnitRuntime[] allies = { death, sun, judgement, world }; // 전투 아군 목록 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 임시 적 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(deathCard, deathData), new RunDeckCardEntry(sunCard, sunData), new RunDeckCardEntry(rescueCard, judgementData), new RunDeckCardEntry(worldCard, worldData) }; // 네 캐릭터 전용 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 20, 1); // 실제 전투 덱 생성
        deck.DrawCards(4); // 전용 카드 네 장 드로우
        CardInstance deathInstance = null; // 죽음 카드 참조 초기화
        CardInstance sunInstance = null; // 태양 카드 참조 초기화
        CardInstance rescueInstance = null; // 심판 카드 참조 초기화
        CardInstance worldInstance = null; // 세계 카드 참조 초기화
        foreach (CardInstance card in deck.Hand) // 손패 카드 순회
        { // 전용 카드 분류 시작
            if (card.SourceData == deathCard) // 죽음 전용 카드 확인
            { // 죽음 카드 기록 시작
                deathInstance = card; // 죽음 카드 기록
            } // 죽음 카드 기록 종료
            else if (card.SourceData == sunCard) // 태양 전용 카드 확인
            { // 태양 카드 기록 시작
                sunInstance = card; // 태양 카드 기록
            } // 태양 카드 기록 종료
            else if (card.SourceData == rescueCard) // 심판 전용 카드 확인
            { // 심판 카드 기록 시작
                rescueInstance = card; // 심판 카드 기록
            } // 심판 카드 기록 종료
            else if (card.SourceData == worldCard) // 세계 전용 카드 확인
            { // 세계 카드 기록 시작
                worldInstance = card; // 세계 카드 기록
            } // 세계 카드 기록 종료
        } // 전용 카드 분류 종료
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 아군 턴 시작 확인
        GameObject canvas = new GameObject("JudgementWorldCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 덱 연결 확인
        BattleUnitView deathView = CreateView(canvas.transform, death); // 죽음 화면 생성
        BattleUnitView sunView = CreateView(canvas.transform, sun); // 태양 화면 생성
        BattleUnitView judgementView = CreateView(canvas.transform, judgement); // 심판 화면 생성
        BattleUnitView worldView = CreateView(canvas.transform, world); // 세계 화면 생성
        BattleUnitView enemyView = CreateView(canvas.transform, enemy); // 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { deathView, sunView, judgementView, worldView }, new[] { enemyView }, actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { deathInstance, new[] { enemy } }); // 첫 동료의 전용 카드 사용
        impact.Invoke(controller, new object[] { sunInstance, new[] { enemy } }); // 두 번째 동료의 전용 카드 사용
        death.ApplyPersistentHealth(death.MaxHealth / 4); // 가장 낮은 체력 비율 동료 설정
        judgement.ApplyPersistentHealth(judgement.MaxHealth / 2); // 심판 대상의 위험 체력 설정
        int judgementBefore = judgement.CurrentHealth; // 회복 전 체력 기록
        impact.Invoke(controller, new object[] { worldInstance, new[] { judgement } }); // 세계 카드로 심판 보호
        Assert.AreEqual(3, judgement.CurrentBlock); // 선택한 아군의 방어도 확인
        Assert.AreEqual(3, death.CurrentBlock); // 체력 비율이 낮은 다른 아군의 방어도 확인
        Assert.AreEqual(0, sun.CurrentBlock); // 다른 아군에게 중복 적용 없음 확인
        impact.Invoke(controller, new object[] { rescueInstance, new[] { judgement } }); // 심판 카드로 위험 아군 회복
        Assert.AreEqual(judgementBefore + 12, judgement.CurrentHealth); // 위험 구간 기본 회복과 추가 회복 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 사용 후 전체 덱 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 카드 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 위험 체력 경계 검사 지정
    public void JudgementRecovery_UsesHealthBeforeHealing() // 정확한 위험 체력 경계와 추가 회복 확인
    { // 위험 회복 검사 시작
        CharacterData data = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[20]; // 실제 심판 캐릭터 조회
        BattleUnitRuntime target = BattleUnitRuntime.CreateAlly(data); // 회복 대상 생성
        target.ApplyPersistentHealth(target.MaxHealth / 2 + 1); // 최대 체력 절반보다 높은 체력 설정
        Assert.AreEqual(8, JudgementRecovery.GetHealingAmount(target, 8, 4)); // 위험 구간 밖 기본 회복 확인
        target.ApplyPersistentHealth(target.MaxHealth / 2); // 최대 체력 절반 설정
        Assert.AreEqual(12, JudgementRecovery.GetHealingAmount(target, 8, 4)); // 절반 이하 추가 회복 확인
        target.ApplyPersistentHealth(0); // 사망한 대상 설정
        Assert.AreEqual(0, JudgementRecovery.GetHealingAmount(target, 8, 4)); // 사망 대상 회복 차단 확인
    } // 위험 회복 검사 종료
    [Test] // 세계의 서로 다른 전용 카드 연계 검사 지정
    public void WorldCoordination_RequiresDifferentOwnersAndResetsEachTurn() // 전용 카드 소유자와 턴 초기화 확인
    { // 세계 연계 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 첫 동료 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(roster.Characters[1]); // 두 번째 동료 생성
        BattleUnitRuntime world = BattleUnitRuntime.CreateAlly(roster.Characters[21]); // 세계 생성
        BattleUnitRuntime[] allies = { first, second, world }; // 전투 아군 목록 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 임시 적 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        WorldCoordinationRuntime coordination = new WorldCoordinationRuntime(turns, allies); // 세계 카드 연계 관리자 생성
        CardInstance firstCard = CardInstance.Create(Resources.Load<CardData>("StarterCards/FoolChoice"), first, 0); // 첫 동료 전용 카드 생성
        CardInstance secondCard = CardInstance.Create(Resources.Load<CardData>("StarterCards/MagicianRepair"), second, 1); // 두 번째 동료 전용 카드 생성
        CardInstance mismatched = CardInstance.Create(Resources.Load<CardData>("StarterCards/FoolChoice"), second, 2); // 다른 캐릭터의 전용 카드 생성
        CardInstance common = CardInstance.Create(Resources.Load<CardData>("StarterCards/Attack"), second, 3); // 공용 카드 생성
        Assert.IsTrue(turns.StartBattle(0)); // 아군 턴 시작 확인
        coordination.RecordCardUsed(firstCard); // 첫 동료 전용 카드 기록
        coordination.RecordCardUsed(firstCard); // 같은 동료 반복 기록
        coordination.RecordCardUsed(mismatched); // 소유자가 일치하지 않는 카드 기록 시도
        coordination.RecordCardUsed(common); // 공용 카드 기록 시도
        Assert.IsFalse(coordination.IsReady); // 서로 다른 두 동료 미충족 확인
        coordination.RecordCardUsed(secondCard); // 다른 동료 전용 카드 기록
        Assert.IsTrue(coordination.IsReady); // 서로 다른 두 동료 연계 조건 확인
        first.ApplyPersistentHealth(first.MaxHealth / 4); // 첫 동료의 낮은 체력 비율 설정
        second.ApplyPersistentHealth(second.MaxHealth / 2); // 두 번째 동료의 중간 체력 비율 설정
        Assert.AreSame(first, coordination.FindAdditionalTarget(world, world)); // 가장 낮은 다른 생존 아군 확인
        Assert.AreSame(second, coordination.FindAdditionalTarget(world, first)); // 선택한 아군을 추가 대상에서 제외 확인
        first.ApplyPersistentHealth(0); // 첫 동료 사망 처리
        second.ApplyPersistentHealth(0); // 두 번째 동료 사망 처리
        Assert.IsNull(coordination.FindAdditionalTarget(world, world)); // 다른 생존 아군이 없으면 추가 적용 없음 확인
        first.ApplyPersistentHealth(first.MaxHealth); // 첫 동료 생존 상태 복원
        second.ApplyPersistentHealth(second.MaxHealth); // 두 번째 동료 생존 상태 복원
        Assert.IsTrue(turns.EndPlayerTurn()); // 아군 턴 종료
        Assert.IsFalse(coordination.IsReady); // 턴 종료 시 연계 기록 초기화 확인
        coordination.Dispose(); // 연계 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 세계 연계 검사 종료
} // 전용 능력 검사 종료
