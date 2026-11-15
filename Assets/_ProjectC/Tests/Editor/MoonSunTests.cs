using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 연속 공격 대상 목록 사용
using System.Reflection; // 임시 카드와 약점 설정 사용
using NUnit.Framework; // 연속 공격 규칙 검증 사용
using UnityEngine; // 임시 전투 데이터 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 선택 버튼 사용
public sealed class MoonSunTests // 달과 태양 임시 능력 검사
{ // 전용 능력 검사 시작
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresBothExclusiveCards() // 달과 태양 전용 카드의 실제 저장 복원 확인
    { // 실제 탐사 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로 생성
        try // 테스트 저장 파일 정리 보장
        { // 실제 저장 복원 검사 시작
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
            PartyDepartureTestSupport.StartExploration(18, 19); // 달과 태양로 탐사 시작
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 준비 대기
            { // 탐사 플레이어 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 플레이어 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
            yield return null; // 탐사 상태 초기화 대기
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 전용 카드가 포함된 탐사 저장 확인
            ExplorationSessionManager.Instance.ResetExploration(); // 현재 탐사 상태 제거
            RunDeckManager.Instance.ResetToSource(null); // 현재 회차 덱 제거
            PartyLoadoutRuntime.Clear(); // 현재 파티와 시작 덱 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 실제 탐사 저장 복원 확인
            bool moonFound = false; // 달 전용 카드 복원 여부 초기화
            bool sunFound = false; // 태양 전용 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                moonFound |= entry.Card.CardId == "CRD_ARC_18_HAND" && entry.Owner.CharacterId == "ARC_18"; // 달 카드와 소유자 확인
                sunFound |= entry.Card.CardId == "CRD_ARC_19_CHAIN" && entry.Owner.CharacterId == "ARC_19"; // 태양 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(moonFound); // 달 전용 카드 복원 확인
            Assert.IsTrue(sunFound); // 태양 전용 카드 복원 확인
        } // 실제 저장 복원 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 탐사 저장 검사 종료
    [UnityTest] // 실제 전용 카드와 손패 선택 화면 검사 지정
    public IEnumerator StarterCards_ChainAttackAndChooseHandDiscard() // 실제 카드 효과와 선택 버튼 확인
    { // 카드 효과 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData moonData = roster.Characters[18]; // 달 캐릭터 조회
        CharacterData sunData = roster.Characters[19]; // 태양 캐릭터 조회
        CardData moonCard = Resources.Load<CardData>("StarterCards/MoonHandChoice"); // 달 전용 카드 조회
        CardData sunCard = Resources.Load<CardData>("StarterCards/SunChain"); // 태양 전용 카드 조회
        CardData filler = Resources.Load<CardData>("StarterCards/Attack"); // 드로우용 일반 카드 조회
        Assert.IsNotNull(moonCard); // 달 전용 카드 존재 확인
        Assert.IsNotNull(sunCard); // 태양 전용 카드 존재 확인
        BattleUnitRuntime moon = BattleUnitRuntime.CreateAlly(moonData); // 달 전투 유닛 생성
        BattleUnitRuntime sun = BattleUnitRuntime.CreateAlly(sunData); // 태양 전투 유닛 생성
        EnemyData firstData = ScriptableObject.CreateInstance<EnemyData>(); // 첫 적 원본 생성
        EnemyData secondData = ScriptableObject.CreateInstance<EnemyData>(); // 두 번째 적 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(firstData); // 첫 적 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(secondData); // 두 번째 적 생성
        BattleUnitRuntime[] allies = { moon, sun }; // 아군 목록 생성
        BattleUnitRuntime[] enemies = { first, second }; // 적 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(moonCard, moonData), new RunDeckCardEntry(sunCard, sunData), new RunDeckCardEntry(filler, moonData), new RunDeckCardEntry(filler, moonData) }; // 전용 카드와 드로우 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 20, 1); // 실제 전투 덱 생성
        deck.DrawCards(4); // 네 장을 손패에 추가
        CardInstance moonInstance = null; // 달 카드 참조 초기화
        CardInstance sunInstance = null; // 태양 카드 참조 초기화
        List<CardInstance> fillers = new List<CardInstance>(); // 드로우할 카드 목록 생성
        foreach (CardInstance card in deck.Hand) // 손패 카드 순회
        { // 카드 분류 시작
            if (card.SourceData == moonCard) // 달 카드 확인
            { // 달 카드 기록 시작
                moonInstance = card; // 달 카드 기록
            } // 달 카드 기록 종료
            else if (card.SourceData == sunCard) // 태양 카드 확인
            { // 태양 카드 기록 시작
                sunInstance = card; // 태양 카드 기록
            } // 태양 카드 기록 종료
            else // 일반 카드 확인
            { // 일반 카드 기록 시작
                fillers.Add(card); // 드로우할 일반 카드 기록
            } // 일반 카드 기록 종료
        } // 카드 분류 종료
        foreach (CardInstance card in fillers) // 일반 카드 순회
        { // 드로우 준비 시작
            deck.DiscardCard(card); // 일반 카드를 버린 더미에 배치
        } // 드로우 준비 종료
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal); // 실제 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 아군 턴 시작 확인
        GameObject canvas = new GameObject("MoonSunCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 덱 연결 확인
        BattleUnitView moonView = CreateView(canvas.transform, moon); // 달 화면 생성
        BattleUnitView sunView = CreateView(canvas.transform, sun); // 태양 화면 생성
        BattleUnitView firstView = CreateView(canvas.transform, first); // 첫 적 화면 생성
        BattleUnitView secondView = CreateView(canvas.transform, second); // 두 번째 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { moonView, sunView }, new[] { firstView, secondView }, actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { sunInstance, new[] { first } }); // 태양 카드로 첫 적 공격
        Assert.AreEqual(42, first.CurrentHealth); // 첫 적 기본 피해 여덟 확인
        Assert.AreEqual(44, second.CurrentHealth); // 다른 적 기본 피해 여섯 확인
        impact.Invoke(controller, new object[] { moonInstance, new[] { moon } }); // 달 카드 사용과 두 장 드로우
        Assert.AreEqual(2, deck.HandCount); // 사용 카드 제외 두 장 드로우 확인
        typeof(BattleCardActionController).GetMethod("HandleActionSequenceCompleted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null); // 실제 카드 선택 화면 표시
        GameObject choice = GameObject.Find("Choice_0"); // 첫 번째 손패 선택 버튼 조회
        Assert.IsNotNull(choice); // 선택 버튼 존재 확인
        CardInstance selected = deck.Hand[0]; // 실제 선택할 카드 기록
        choice.GetComponent<Button>().onClick.Invoke(); // 실제 선택 버튼 누르기
        Assert.AreEqual(1, deck.HandCount); // 선택 카드 한 장 버리기 확인
        CollectionAssert.Contains(deck.DiscardPile, selected); // 선택 카드의 버린 더미 이동 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 위치 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(firstData); // 첫 적 원본 정리
        Object.DestroyImmediate(secondData); // 두 번째 적 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 카드 효과 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 손패 최대치와 사용 카드 재드로우 검사 지정
    public void PlayAndDraw_FreesSlotWithoutRedrawingPlayedCard() // 사용 카드 공간과 재구성 순서 확인
    { // 최대 손패 검사 시작
        CharacterData ownerData = ScriptableObject.CreateInstance<CharacterData>(); // 카드 소유자 원본 생성
        CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 임시 카드 원본 생성
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 카드 소유자 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry>(); // 카드 목록 생성
        for (int index = 0; index < 22; index++) // 최대 손패와 드로우 카드 수만큼 반복
        { // 카드 등록 시작
            entries.Add(new RunDeckCardEntry(cardData, ownerData)); // 같은 소유자의 카드 추가
        } // 카드 등록 종료
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { owner }, 20, 1); // 최대 손패 스무 장 덱 생성
        deck.DrawCards(20); // 손패를 최대치까지 채우기
        CardInstance played = deck.Hand[0]; // 사용할 손패 카드 저장
        Assert.IsTrue(deck.TryPlayAndDraw(played, 2, out int drawn)); // 카드 사용과 드로우 실행
        Assert.AreEqual(1, drawn); // 사용 카드가 비운 공간에 한 장 드로우 확인
        Assert.AreEqual(20, deck.HandCount); // 최대 손패 제한 확인
        Assert.AreEqual(2, deck.DiscardPileCount); // 사용 카드와 초과 카드 버리기 확인
        CollectionAssert.Contains(deck.DiscardPile, played); // 사용 카드의 버린 더미 이동 확인
        Assert.IsTrue(deck.IsStateValid()); // 전체 카드 위치 보존 확인
        BattleDeckRuntime singleDeck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry> { new RunDeckCardEntry(cardData, ownerData) }, new[] { owner }, 20, 1); // 카드 한 장만 있는 덱 생성
        singleDeck.DrawCards(1); // 유일한 카드 드로우
        CardInstance singleCard = singleDeck.Hand[0]; // 유일한 손패 카드 저장
        Assert.IsTrue(singleDeck.TryPlayAndDraw(singleCard, 2, out int singleDrawn)); // 사용 후 빈 덱 드로우 시도
        Assert.AreEqual(0, singleDrawn); // 사용한 카드를 즉시 다시 뽑지 않음 확인
        Assert.AreEqual(0, singleDeck.HandCount); // 빈 손패 유지 확인
        CollectionAssert.Contains(singleDeck.DiscardPile, singleCard); // 사용 카드의 버린 더미 이동 확인
        Assert.IsTrue(singleDeck.IsStateValid()); // 단일 카드 위치 보존 확인
        Object.DestroyImmediate(ownerData); // 소유자 원본 정리
        Object.DestroyImmediate(cardData); // 카드 원본 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
    } // 최대 손패 검사 종료
    [UnityTest] // 다른 적 연속 공격과 약점 검사 지정
    public IEnumerator SunChain_AttacksOnlyOtherLivingEnemyWithWeakness() // 다른 생존 적의 방어와 약점 적용 확인
    { // 연속 공격 검사 시작
        yield return new EnterPlayMode(); // 피해 디버그 화면이 사용 가능한 플레이 모드 시작
        CharacterData ownerData = ScriptableObject.CreateInstance<CharacterData>(); // 카드 소유자 원본 생성
        CardData cardData = ScriptableObject.CreateInstance<CardData>(); // 임시 전용 카드 생성
        typeof(CardData).GetField("cardId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cardData, "CRD_ARC_19_CHAIN"); // 태양 전용 카드 식별자 설정
        typeof(CardData).GetField("secondaryEffectValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cardData, 6); // 두 번째 기본 피해 여섯 설정
        typeof(CardData).GetField("damageType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cardData, BattleDamageType.Physical); // 물리 피해 유형 설정
        EnemyData firstData = ScriptableObject.CreateInstance<EnemyData>(); // 첫 공격 대상 원본 생성
        EnemyData secondData = ScriptableObject.CreateInstance<EnemyData>(); // 두 번째 공격 대상 원본 생성
        EnemyData deadData = ScriptableObject.CreateInstance<EnemyData>(); // 사망한 적 원본 생성
        typeof(EnemyData).GetField("weaknessCardTypes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(secondData, new List<CardType> { CardType.Sword }); // 두 번째 적의 검 약점 설정
        BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 태양 공격 소유자 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(firstData); // 첫 공격 대상 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(secondData); // 두 번째 공격 대상 생성
        BattleUnitRuntime dead = BattleUnitRuntime.CreateEnemy(deadData); // 사망한 적 생성
        dead.TakeDamage(100000, BattleDamageType.None); // 제외할 적을 사망 처리
        second.GainBlock(2); // 두 번째 적의 방어도 두 점 설정
        CardInstance card = CardInstance.Create(cardData, owner, 0); // 실제 태양 카드 인스턴스 생성
        int firstHealth = first.CurrentHealth; // 첫 대상의 이전 체력 저장
        int secondHealth = second.CurrentHealth; // 두 번째 적의 이전 체력 저장
        BattleUnitRuntime hit = SunChainAttack.TryAttack(card, first, new[] { first, second, dead }); // 다른 생존 적에게 연속 공격
        Assert.AreSame(second, hit); // 다른 생존 적이 선택됨 확인
        Assert.AreEqual(firstHealth, first.CurrentHealth); // 첫 대상에 두 번째 공격 미적용 확인
        Assert.AreEqual(secondHealth - 7, second.CurrentHealth); // 약점 피해 아홉에서 방어도 둘을 제외한 피해 확인
        Assert.AreEqual(0, second.CurrentBlock); // 두 번째 적의 방어도 소모 확인
        Assert.IsNull(SunChainAttack.TryAttack(card, first, new[] { first, dead })); // 다른 생존 적이 없으면 연속 공격 생략 확인
        Object.DestroyImmediate(ownerData); // 소유자 원본 정리
        Object.DestroyImmediate(cardData); // 카드 원본 정리
        Object.DestroyImmediate(firstData); // 첫 적 원본 정리
        Object.DestroyImmediate(secondData); // 두 번째 적 원본 정리
        Object.DestroyImmediate(deadData); // 사망 적 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 연속 공격 검사 종료
} // 전용 능력 검사 종료
