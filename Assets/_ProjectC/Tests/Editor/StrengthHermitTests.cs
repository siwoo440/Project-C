using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 덱 카드 목록 사용
using System.Reflection; // 실제 카드 충돌 처리 사용
using NUnit.Framework; // 전용 능력 검증 사용
using UnityEngine; // 전투 데이터 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 카드 선택 버튼 사용
public sealed class StrengthHermitTests // 힘과 은둔자 임시 능력 검사
{ // 전용 능력 검사 시작
    private sealed class AvailableStatus : IPartyDeploymentStatusProvider // 출전 가능 상태 제공자
    { // 출전 상태 제공 시작
        public bool IsDead(CharacterData character) => false; // 사망 상태 없음 반환
        public bool IsRecovering(CharacterData character) => false; // 회복 상태 없음 반환
        public bool CanDeploy(CharacterData character) => true; // 출전 가능 상태 반환
    } // 출전 상태 제공 종료
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresBothExclusiveCards() // 두 전용 카드의 실제 저장 복원 확인
    { // 탐사 저장 복원 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        try // 테스트 저장 파일 정리 보장
        { // 실제 탐사 저장 검사 시작
            SceneManager.LoadScene("00_Boot"); // 시작 씬 열기
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 준비 대기
            { // 메뉴 준비 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 준비 대기 종료
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 로비 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 준비 대기
            { // 로비 준비 대기 시작
                yield return null; // 다음 프레임 대기
            } // 로비 준비 대기 종료
            PartyDepartureTestSupport.StartExploration(8, 9); // 힘과 은둔자로 탐사 시작
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 준비 대기
            { // 탐사 플레이어 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 플레이어 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
            yield return null; // 탐사 상태 초기화 대기
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 두 전용 카드가 포함된 탐사 저장
            ExplorationSessionManager.Instance.ResetExploration(); // 현재 탐사 상태 제거
            RunDeckManager.Instance.ResetToSource(null); // 현재 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 현재 파티와 시작 덱 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 탐사 저장 복원 확인
            bool strengthFound = false; // 힘 전용 카드 복원 여부 초기화
            bool hermitFound = false; // 은둔자 전용 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                strengthFound |= entry.Card.CardId == "CRD_ARC_08_GUARD" && entry.Owner.CharacterId == "ARC_08"; // 힘 카드와 소유자 확인
                hermitFound |= entry.Card.CardId == "CRD_ARC_09_ARRANGE" && entry.Owner.CharacterId == "ARC_09"; // 은둔자 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(strengthFound); // 힘 전용 카드 복원 확인
            Assert.IsTrue(hermitFound); // 은둔자 전용 카드 복원 확인
        } // 실제 탐사 저장 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 탐사 저장 복원 검사 종료
    [Test] // 실제 탐사 출발 덱 검사 지정
    public void Loadout_IncludesBothExclusiveCards() // 파티 출발 시 전용 카드 연결 확인
    { // 출발 덱 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        PartySelection party = new PartySelection(new AvailableStatus()); // 출전 가능 파티 생성
        Assert.IsTrue(party.TryAdd(roster.Characters[8])); // 힘 캐릭터 편성 확인
        Assert.IsTrue(party.TryAdd(roster.Characters[9])); // 은둔자 캐릭터 편성 확인
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(party)); // 실제 탐사 출발 덱 생성 확인
        Assert.AreEqual("CRD_ARC_08_GUARD", PartyLoadoutRuntime.Active.Deck.Cards[0].Card.CardId); // 힘 전용 카드 배정 확인
        Assert.AreEqual("CRD_ARC_09_ARRANGE", PartyLoadoutRuntime.Active.Deck.Cards[1].Card.CardId); // 은둔자 전용 카드 배정 확인
        PartyLoadoutRuntime.Clear(); // 테스트 출발 덱 정리
    } // 출발 덱 검사 종료
    [UnityTest] // 실제 전용 카드 사용 검사 지정
    public IEnumerator StarterCards_ApplyGuardAndArrangeThroughChoiceScreen() // 실제 카드와 선택 화면 연결 확인
    { // 실제 카드 사용 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData strength = roster.Characters[8]; // 힘 캐릭터 조회
        CharacterData hermit = roster.Characters[9]; // 은둔자 캐릭터 조회
        CardData guardCard = Resources.Load<CardData>("StarterCards/StrengthGuard"); // 힘 전용 카드 조회
        CardData arrangeCard = Resources.Load<CardData>("StarterCards/HermitArrange"); // 은둔자 전용 카드 조회
        CardData filler = Resources.Load<CardData>("StarterCards/Attack"); // 후보용 공용 카드 조회
        Assert.IsNotNull(guardCard); // 힘 전용 카드 존재 확인
        Assert.IsNotNull(arrangeCard); // 은둔자 전용 카드 존재 확인
        Assert.AreEqual(CardEffectType.GuardAllyAttack, guardCard.EffectType); // 힘 카드 효과 연결 확인
        Assert.AreEqual(CardEffectType.ArrangeDrawTop, arrangeCard.EffectType); // 은둔자 카드 효과 연결 확인
        BattleUnitRuntime guard = BattleUnitRuntime.CreateAlly(strength); // 힘 전투 유닛 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(hermit); // 은둔자 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 데이터 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { ally, guard }; // 은둔자가 먼저 공격받는 파티 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 데이터 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(guardCard, strength), new RunDeckCardEntry(arrangeCard, hermit) }; // 전용 카드 두 장 등록
        for (int index = 0; index < 4; index++) // 덱 순서 후보 네 장 준비
        { // 후보 카드 등록 시작
            entries.Add(new RunDeckCardEntry(filler, hermit)); // 생존 소유자 후보 추가
        } // 후보 카드 등록 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 8, 1); // 실제 카드가 포함된 덱 생성
        deck.DrawCards(6); // 전체 테스트 카드를 손패로 이동
        CardInstance guardInstance = null; // 힘 전용 카드 참조 준비
        CardInstance arrangeInstance = null; // 은둔자 전용 카드 참조 준비
        foreach (CardInstance card in new List<CardInstance>(deck.Hand)) // 현재 손패의 복사본 순회
        { // 전용 카드 분류 시작
            if (card.SourceData == guardCard) // 힘 전용 카드 확인
            { // 힘 카드 보관 시작
                guardInstance = card; // 힘 카드 저장
            } // 힘 카드 보관 종료
            else if (card.SourceData == arrangeCard) // 은둔자 전용 카드 확인
            { // 은둔자 카드 보관 시작
                arrangeInstance = card; // 은둔자 카드 저장
            } // 은둔자 카드 보관 종료
            else // 후보 카드 확인
            { // 후보 카드 이동 시작
                deck.DiscardCard(card); // 후보 카드를 버린 더미로 이동
            } // 후보 카드 이동 종료
        } // 전용 카드 분류 종료
        deck.PrepareDrawChoices(3); // 후보 카드를 덱으로 재구성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 전투 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, allies); // 적 행동 관리자 생성
        actions.PrepareActions(); // 동료 대상 적 공격 준비
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("StrengthHermitCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 화면 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 덱 연결 확인
        BattleUnitView allyView = CreateView(canvas.transform, ally); // 은둔자 화면 생성
        BattleUnitView guardView = CreateView(canvas.transform, guard); // 힘 화면 생성
        BattleUnitView enemyView = CreateView(canvas.transform, enemy); // 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 연출 실행기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { allyView, guardView }, new[] { enemyView }, actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { guardInstance, new[] { ally } }); // 동료에게 힘 카드 효과 적용
        Assert.AreSame(guard, actions.PlannedActions[0].Target); // 실제 카드의 공격 대신 받기 확인
        impact.Invoke(controller, new object[] { arrangeInstance, new[] { ally } }); // 은둔자 카드 효과 적용
        IReadOnlyList<CardInstance> candidates = deck.PeekDrawTop(3); // 선택 전 덱 위 후보 조회
        typeof(BattleCardActionController).GetMethod("ShowPendingCardChoices", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null); // 실제 카드 선택 화면 표시
        Transform panel = canvas.transform.Find("DrawChoicePanel"); // 선택 화면 조회
        Assert.IsNotNull(panel); // 덱 순서 선택 화면 표시 확인
        panel.Find("NextPage").GetComponent<Button>().onClick.Invoke(); // 세 번째 후보가 있는 페이지 이동
        panel.Find("Choice_2").GetComponent<Button>().onClick.Invoke(); // 세 번째 후보 선택
        Assert.AreSame(candidates[2], deck.PeekDrawTop(3)[0]); // 선택 카드가 덱 맨 위로 이동 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 사용 후 전체 덱 상태 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 연결 정리
        Object.DestroyImmediate(canvas); // 임시 화면 정리
        Object.DestroyImmediate(enemyData); // 임시 적 데이터 정리
        Object.DestroyImmediate(source); // 임시 덱 데이터 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 카드 사용 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 전투 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 배치
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 유닛 화면 반환
    } // 전투 유닛 화면 생성 종료
    [Test] // 동료 보호 검사 지정
    public void Guard_RedirectsOnlyFirstAttackAndValidatesAllies() // 첫 예정 공격의 동료 보호 확인
    { // 동료 보호 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 동료 데이터 생성
        CharacterData guardData = ScriptableObject.CreateInstance<CharacterData>(); // 보호자 데이터 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 데이터 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 보호받을 동료 생성
        BattleUnitRuntime guard = BattleUnitRuntime.CreateAlly(guardData); // 대신 받을 보호자 생성
        BattleUnitRuntime enemyOne = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 적 생성
        BattleUnitRuntime enemyTwo = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemyOne, enemyTwo }, new[] { ally, guard }); // 적 행동 관리자 생성
        Assert.AreEqual(2, actions.PrepareActions()); // 두 예정 행동 준비 확인
        BattleEnemyAction first = actions.PlannedActions[0]; // 가장 빠른 예정 행동 조회
        BattleEnemyAction second = actions.PlannedActions[1]; // 다음 예정 행동 조회
        Assert.IsFalse(actions.TryProtectAlly(ally, ally)); // 자신을 대신 보호하는 행동 차단 확인
        Assert.IsTrue(actions.TryProtectAlly(guard, ally)); // 첫 공격 대신 받기 확인
        Assert.AreSame(guard, first.Target); // 첫 공격의 실제 대상 변경 확인
        Assert.AreSame(ally, second.Target); // 두 번째 공격의 기존 대상 유지 확인
        int guardHealth = guard.CurrentHealth; // 보호자의 이전 체력 저장
        actions.ExecuteAction(first); // 대신 받은 공격 실행
        Assert.Less(guard.CurrentHealth, guardHealth); // 보호자의 실제 피해 확인
        Assert.AreEqual(ally.MaxHealth, ally.CurrentHealth); // 보호된 동료의 피해 없음 확인
        guard.TakeDamage(100000, BattleDamageType.Physical); // 보호자 사망 처리
        Assert.IsFalse(actions.TryProtectAlly(guard, ally)); // 사망 보호자 차단 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(allyData); // 동료 데이터 정리
        Object.DestroyImmediate(guardData); // 보호자 데이터 정리
        Object.DestroyImmediate(enemyData); // 적 데이터 정리
    } // 동료 보호 검사 종료
    [Test] // 덱 순서 변경 검사 지정
    public void ArrangeTop_PreservesCardsAndRejectsOutOfRange() // 선택 카드의 맨 위 배치 확인
    { // 덱 순서 검사 시작
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 카드 소유자 생성
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 카드 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 덱 카드 목록 생성
        for (int index = 0; index < 5; index++) // 카드 다섯 장 생성
        { // 카드 생성 시작
            entries.Add(new RunDeckCardEntry(card, owner)); // 동일 원본의 개별 카드 등록
        } // 카드 생성 종료
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 카드 소유 전투 유닛 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { unit }, 5, 1); // 전투 덱 생성
        IReadOnlyList<CardInstance> candidates = deck.PeekDrawTop(3); // 기존 덱 위 세 장 확인
        Assert.AreEqual(3, candidates.Count); // 세 후보 확인
        Assert.IsFalse(deck.TryMoveDrawTop(deck.DrawPile[0], 3)); // 후보 밖 카드 변경 차단 확인
        Assert.IsTrue(deck.TryMoveDrawTop(candidates[2], 3)); // 세 번째 후보를 맨 위로 이동
        Assert.AreSame(candidates[2], deck.PeekDrawTop(3)[0]); // 선택 카드의 맨 위 위치 확인
        Assert.AreEqual(5, deck.DrawPileCount); // 덱 카드 수 보존 확인
        Assert.AreEqual(0, deck.HandCount); // 손패 카드 수 유지 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 중복과 누락 없음 확인
        unit.TakeDamage(100000, BattleDamageType.Physical); // 카드 소유자 사망 처리
        Assert.AreEqual(0, deck.PeekDrawTop(3).Count); // 사망 소유자의 후보 제외 확인
        Assert.IsFalse(deck.TryMoveDrawTop(candidates[0], 3)); // 사망 소유자의 카드 선택 차단 확인
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(card); // 카드 원본 정리
        Object.DestroyImmediate(owner); // 소유자 원본 정리
    } // 덱 순서 검사 종료
} // 전용 능력 검사 종료
