using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 카드 목록 사용
using System.Reflection; // 실제 카드 효과 처리 사용
using NUnit.Framework; // 디버프 이전 검증 사용
using UnityEngine; // 테스트 캐릭터 원본 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 디버프 선택 버튼 사용
public sealed class HangedDeathTests // 매달린 자와 죽음 임시 능력 검사
{ // 전용 능력 검사 시작
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresBothExclusiveCards() // 전용 카드 두 장의 실제 저장 복원 확인
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
            PartyDepartureTestSupport.StartExploration(12, 13); // 매달린 자와 죽음으로 탐사 시작
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
            bool hangedFound = false; // 매달린 자 전용 카드 복원 여부 초기화
            bool deathFound = false; // 죽음 전용 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                hangedFound |= entry.Card.CardId == "CRD_ARC_12_TRANSFER" && entry.Owner.CharacterId == "ARC_12"; // 매달린 자 카드와 소유자 확인
                deathFound |= entry.Card.CardId == "CRD_ARC_13_FINISH" && entry.Owner.CharacterId == "ARC_13"; // 죽음 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(hangedFound); // 매달린 자 전용 카드 복원 확인
            Assert.IsTrue(deathFound); // 죽음 전용 카드 복원 확인
        } // 실제 저장 복원 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 탐사 저장 검사 종료
    [UnityTest] // 실제 카드와 선택 화면 검사 지정
    public IEnumerator StarterCards_TransferChosenDebuffAndDamageWeakenedEnemy() // 디버프 선택과 조건부 피해 확인
    { // 전용 카드 사용 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData hangedData = roster.Characters[12]; // 매달린 자 캐릭터 조회
        CharacterData deathData = roster.Characters[13]; // 죽음 캐릭터 조회
        CardData transferCard = Resources.Load<CardData>("StarterCards/HangedTransfer"); // 디버프 이전 전용 카드 조회
        CardData finishCard = Resources.Load<CardData>("StarterCards/DeathFinisher"); // 마무리 전용 카드 조회
        Assert.AreEqual(CardEffectType.TransferAllyDebuff, transferCard.EffectType); // 디버프 이전 카드 효과 확인
        Assert.AreEqual(CardEffectType.Damage, finishCard.EffectType); // 마무리 카드의 기존 공격 효과 확인
        BattleUnitRuntime hanged = BattleUnitRuntime.CreateAlly(hangedData); // 매달린 자 전투 유닛 생성
        BattleUnitRuntime death = BattleUnitRuntime.CreateAlly(deathData); // 죽음 전투 유닛 생성
        EnemyData normalData = ScriptableObject.CreateInstance<EnemyData>(); // 일반 적 원본 생성
        EnemyData weakData = ScriptableObject.CreateInstance<EnemyData>(); // 약화 적 원본 생성
        BattleUnitRuntime normalEnemy = BattleUnitRuntime.CreateEnemy(normalData); // 일반 적 생성
        BattleUnitRuntime weakEnemy = BattleUnitRuntime.CreateEnemy(weakData); // 약화 적 생성
        BattleUnitRuntime[] allies = { hanged, death }; // 전투 아군 목록 생성
        BattleUnitRuntime[] enemies = { normalEnemy, weakEnemy }; // 전투 적 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(transferCard, hangedData), new RunDeckCardEntry(finishCard, deathData), new RunDeckCardEntry(finishCard, deathData) }; // 전용 카드와 중복 공격 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 5, 1); // 전용 카드 전투 덱 생성
        deck.DrawCards(3); // 세 전용 카드를 손패로 이동
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("HangedDeathCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 손패 덱 연결 확인
        BattleUnitView hangedView = CreateView(canvas.transform, hanged); // 매달린 자 화면 생성
        BattleUnitView deathView = CreateView(canvas.transform, death); // 죽음 화면 생성
        BattleUnitView normalView = CreateView(canvas.transform, normalEnemy); // 일반 적 화면 생성
        BattleUnitView weakView = CreateView(canvas.transform, weakEnemy); // 약화 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 행동 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { hangedView, deathView }, new[] { normalView, weakView }, actions); // 실제 카드 처리기 생성
        CardInstance transfer = null; // 디버프 이전 카드 참조 준비
        List<CardInstance> attacks = new List<CardInstance>(); // 마무리 카드 두 장 목록 생성
        foreach (CardInstance card in deck.Hand) // 실제 손패 카드 순회
        { // 전용 카드 분류 시작
            if (card.SourceData == transferCard) // 디버프 이전 카드 확인
            { // 이전 카드 저장 시작
                transfer = card; // 이전 카드 참조 저장
            } // 이전 카드 저장 종료
            if (card.SourceData == finishCard) // 마무리 카드 확인
            { // 공격 카드 저장 시작
                attacks.Add(card); // 마무리 카드 추가
            } // 공격 카드 저장 종료
        } // 전용 카드 분류 종료
        death.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 3, 2); // 동료에게 첫 선택 디버프 적용
        death.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1); // 동료에게 두 번째 선택 디버프 적용
        weakEnemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 두 번째 적을 디버프 상태로 설정
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { transfer, new[] { death } }); // 디버프 이전 카드 실제 효과 적용
        typeof(BattleCardActionController).GetMethod("HandleActionSequenceCompleted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null); // 실제 선택 화면 표시
        Transform panel = canvas.transform.Find("DebuffChoicePanel"); // 디버프 선택 화면 조회
        Assert.IsNotNull(panel); // 실제 디버프 선택 화면 존재 확인
        Assert.IsNotNull(panel.Find("Choice_0")); // 첫 디버프 선택 버튼 확인
        Assert.IsNotNull(panel.Find("Choice_1")); // 두 번째 디버프 선택 버튼 확인
        panel.Find("Choice_1").GetComponent<Button>().onClick.Invoke(); // 두 번째 디버프 선택
        Assert.IsNull(death.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 선택한 동료 디버프 제거 확인
        Assert.IsNotNull(death.GetStatusEffect(BattleStatusEffectType.Poison)); // 선택하지 않은 동료 디버프 유지 확인
        Assert.IsNotNull(hanged.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 선택한 디버프의 매달린 자 이전 확인
        int normalHealth = normalEnemy.CurrentHealth; // 일반 적의 이전 체력 저장
        int weakHealth = weakEnemy.CurrentHealth; // 약화 적의 이전 체력 저장
        impact.Invoke(controller, new object[] { attacks[0], new[] { normalEnemy } }); // 일반 적에게 마무리 공격 적용
        impact.Invoke(controller, new object[] { attacks[1], new[] { weakEnemy } }); // 약화 적에게 마무리 공격 적용
        Assert.AreEqual(normalHealth - 10, normalEnemy.CurrentHealth); // 일반 적에게 기본 피해 열 확인
        Assert.AreEqual(weakHealth - 15, weakEnemy.CurrentHealth); // 디버프 적에게 오십 퍼센트 추가 피해 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 사용 후 덱 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 전투 화면 정리
        Object.DestroyImmediate(normalData); // 일반 적 원본 정리
        Object.DestroyImmediate(weakData); // 약화 적 원본 정리
        Object.DestroyImmediate(source); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전용 카드 사용 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 전투 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 유닛 상태 연결
        return view; // 생성 화면 반환
    } // 전투 유닛 화면 생성 종료
    [Test] // 디버프 이전과 보존 검사 지정
    public void Transfer_OnlyMovesSelectedDebuffAndPreservesStacks() // 선택한 디버프 하나의 수치와 지속 보존 확인
    { // 디버프 이전 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 동료 원본 데이터 생성
        CharacterData hangedData = ScriptableObject.CreateInstance<CharacterData>(); // 매달린 자 원본 데이터 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 디버프를 넘길 동료 생성
        BattleUnitRuntime hanged = BattleUnitRuntime.CreateAlly(hangedData); // 디버프를 받을 매달린 자 생성
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 중독 한 중첩 적용
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 중독 두 번째 중첩 적용
        ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 2, 2, 1); // 이전하지 않을 방어 약화 적용
        BattleStatusEffectInstance poison = ally.GetStatusEffect(BattleStatusEffectType.Poison); // 선택할 중독 상태 조회
        Assert.IsTrue(BattleDebuffTransfer.CanTransfer(ally, hanged, poison)); // 선택 디버프 이전 가능 확인
        Assert.IsTrue(BattleDebuffTransfer.TryTransfer(ally, hanged, poison)); // 선택 디버프 이전 성공 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 원래 동료의 중독 제거 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 선택하지 않은 디버프 유지 확인
        BattleStatusEffectInstance moved = hanged.GetStatusEffect(BattleStatusEffectType.Poison); // 이전된 중독 상태 조회
        Assert.IsNotNull(moved); // 매달린 자의 중독 존재 확인
        Assert.AreEqual(3, moved.Value); // 중첩당 피해 수치 보존 확인
        Assert.AreEqual(3, moved.RemainingTurns); // 남은 발동 횟수 보존 확인
        Assert.AreEqual(2, moved.StackCount); // 중독 중첩 수 보존 확인
        Assert.IsFalse(BattleDebuffTransfer.TryTransfer(ally, hanged, poison)); // 이미 이전된 상태 재사용 차단 확인
        Object.DestroyImmediate(allyData); // 동료 원본 정리
        Object.DestroyImmediate(hangedData); // 매달린 자 원본 정리
    } // 디버프 이전 검사 종료
    [Test] // 면역과 기존 중첩 검사 지정
    public void Transfer_RespectsImmunityAndMergesExistingStatus() // 면역 차단과 기존 디버프 병합 확인
    { // 면역과 병합 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 동료 원본 데이터 생성
        CharacterData hangedData = ScriptableObject.CreateInstance<CharacterData>(); // 매달린 자 원본 데이터 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 디버프를 넘길 동료 생성
        BattleUnitRuntime hanged = BattleUnitRuntime.CreateAlly(hangedData); // 디버프를 받을 매달린 자 생성
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 이전할 중독 적용
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 3, 4); // 이전할 두 번째 중독 중첩
        hanged.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 1, 4); // 기존 중독 한 중첩 적용
        hanged.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 디버프 면역 적용
        BattleStatusEffectInstance poison = ally.GetStatusEffect(BattleStatusEffectType.Poison); // 이전할 중독 조회
        Assert.IsFalse(BattleDebuffTransfer.TryTransfer(ally, hanged, poison)); // 면역 상태의 이전 차단 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 면역 차단 시 기존 동료 상태 보존 확인
        hanged.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 디버프 면역 제거
        Assert.IsTrue(BattleDebuffTransfer.TryTransfer(ally, hanged, poison)); // 면역 제거 후 이전 성공 확인
        BattleStatusEffectInstance combined = hanged.GetStatusEffect(BattleStatusEffectType.Poison); // 병합된 중독 조회
        Assert.AreEqual(3, combined.StackCount); // 기존 중첩과 두 중첩 병합 확인
        Assert.AreEqual(3, combined.Value); // 높은 중첩당 수치 유지 확인
        Assert.AreEqual(3, combined.RemainingTurns); // 긴 남은 지속 유지 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 4); // 추가 이전 중독 한 중첩 적용
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 4); // 추가 이전 중독 두 중첩 적용
        BattleStatusEffectInstance overflow = ally.GetStatusEffect(BattleStatusEffectType.Poison); // 한도를 초과할 중독 조회
        Assert.IsFalse(BattleDebuffTransfer.TryTransfer(ally, hanged, overflow)); // 중첩 손실이 생기는 이전 차단 확인
        Assert.AreEqual(2, overflow.StackCount); // 이전 실패 시 원래 중첩 유지 확인
        Assert.AreEqual(3, combined.StackCount); // 이전 실패 시 기존 중첩 유지 확인
        Object.DestroyImmediate(allyData); // 동료 원본 정리
        Object.DestroyImmediate(hangedData); // 매달린 자 원본 정리
    } // 면역과 병합 검사 종료
} // 전용 능력 검사 종료
