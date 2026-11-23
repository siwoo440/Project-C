using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 카드 목록 사용
using System.Reflection; // 실제 카드 효과 처리 사용
using NUnit.Framework; // 체력 소모 규칙 검증 사용
using UnityEngine; // 임시 캐릭터 원본 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용
public sealed class TemperanceDevilTests // 절제와 악마 임시 능력 검사
{ // 전용 능력 검사 시작
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresBothExclusiveCards() // 두 전용 카드의 실제 저장 복원 확인
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
            PartyDepartureTestSupport.StartExploration(14, 15); // 절제와 악마로 탐사 시작
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
            bool temperanceFound = false; // 절제 전용 카드 복원 여부 초기화
            bool devilFound = false; // 악마 전용 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                temperanceFound |= entry.Card.CardId == "CRD_ARC_14_CLEANSE" && entry.Owner.CharacterId == "ARC_14"; // 절제 카드와 소유자 확인
                devilFound |= entry.Card.CardId == "CRD_ARC_15_BLOOD_PRICE" && entry.Owner.CharacterId == "ARC_15"; // 악마 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(temperanceFound); // 절제 전용 카드 복원 확인
            Assert.IsTrue(devilFound); // 악마 전용 카드 복원 확인
        } // 실제 저장 복원 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 탐사 저장 검사 종료
    [UnityTest] // 실제 정화와 체력 비용 카드 검사 지정
    public IEnumerator StarterCards_CleanseAllAndPayHealthOnlyWhenSafe() // 실제 정화와 생존 비용 공격 확인
    { // 실제 전용 카드 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData temperanceData = roster.Characters[14]; // 절제 캐릭터 조회
        CharacterData devilData = roster.Characters[15]; // 악마 캐릭터 조회
        CardData cleanseCard = Resources.Load<CardData>("StarterCards/TemperanceCleanse"); // 절제 전용 정화 카드 조회
        CardData bloodCard = Resources.Load<CardData>("StarterCards/DevilBloodPrice"); // 악마 전용 공격 카드 조회
        Assert.AreEqual(CardEffectType.RemoveDebuffs, cleanseCard.EffectType); // 절제 카드의 전체 정화 효과 확인
        Assert.AreEqual(20, bloodCard.EffectValue); // 악마 카드의 기본 피해 이십 확인
        Assert.AreEqual(5, bloodCard.SecondaryEffectValue); // 악마 카드의 체력 비용 다섯 확인
        BattleUnitRuntime temperance = BattleUnitRuntime.CreateAlly(temperanceData); // 절제 전투 유닛 생성
        BattleUnitRuntime devil = BattleUnitRuntime.CreateAlly(devilData); // 악마 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 임시 적 유닛 생성
        BattleUnitRuntime[] allies = { temperance, devil }; // 실제 전투 아군 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 전투 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(cleanseCard, temperanceData), new RunDeckCardEntry(bloodCard, devilData) }; // 두 전용 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 5, 1); // 전용 카드 전투 덱 생성
        deck.DrawCards(2); // 전용 카드 두 장 드로우
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 공용 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("TemperanceDevilCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패 덱 연결 확인
        BattleUnitView temperanceView = CreateView(canvas.transform, temperance); // 절제 화면 생성
        BattleUnitView devilView = CreateView(canvas.transform, devil); // 악마 화면 생성
        BattleUnitView enemyView = CreateView(canvas.transform, enemy); // 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { temperanceView, devilView }, new[] { enemyView }, actions); // 실제 카드 처리기 생성
        CardInstance cleanse = null; // 정화 전용 카드 참조 준비
        CardInstance blood = null; // 체력 비용 카드 참조 준비
        foreach (CardInstance card in deck.Hand) // 실제 손패 카드 순회
        { // 전용 카드 분류 시작
            if (card.SourceData == cleanseCard) // 절제 전용 카드 확인
            { // 정화 카드 저장 시작
                cleanse = card; // 정화 카드 저장
            } // 정화 카드 저장 종료
            if (card.SourceData == bloodCard) // 악마 전용 카드 확인
            { // 체력 비용 카드 저장 시작
                blood = card; // 체력 비용 카드 저장
            } // 체력 비용 카드 저장 종료
        } // 전용 카드 분류 종료
        devil.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 3, 2); // 정화할 중독 적용
        devil.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 3, 2, 1); // 정화할 방어 약화 적용
        devil.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 2, 1); // 보존할 재생 효과 적용
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { cleanse, new[] { devil } }); // 악마에게 절제 카드 정화 적용
        Assert.IsFalse(devil.HasDebuff); // 선택 아군의 모든 디버프 제거 확인
        Assert.IsNotNull(devil.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 이로운 재생 효과 보존 확인
        devil.GainBlock(10); // 체력 비용 검사용 방어도 적용
        Assert.IsTrue(devil.ApplyPersistentHealth(5)); // 체력 다섯으로 제한
        hand.RefreshCardAvailability(); // 현재 체력 기준 카드 사용 가능 상태 갱신
        BattleCardView bloodView = null; // 악마 카드 화면 참조 준비
        foreach (BattleCardView candidate in hand.GetComponentsInChildren<BattleCardView>()) // 실제 손패 카드 화면 순회
        { // 악마 카드 화면 검색 시작
            if (candidate.RuntimeCard == blood && candidate.gameObject.activeInHierarchy) // 악마 전용 카드 확인
            { // 악마 카드 화면 발견 시작
                bloodView = candidate; // 악마 카드 화면 저장
                break; // 카드 화면 검색 종료
            } // 악마 카드 화면 발견 종료
        } // 악마 카드 화면 검색 종료
        Assert.IsNotNull(bloodView); // 악마 카드 화면 존재 확인
        bloodView.OnPointerClick(null); // 체력 다섯 상태의 카드 선택 시도
        Assert.IsNull(controller.SelectedCard); // 사망 위험 카드 선택 차단 확인
        Assert.AreEqual(6, points.CurrentActionPoints); // 사용 차단 시 행동력 보존 확인
        Assert.AreEqual(1, devil.RestoreHealth(1)); // 체력 한 점 회복과 화면 갱신 확인
        bloodView.OnPointerClick(null); // 체력 여섯 상태의 카드 선택
        Assert.AreSame(blood, controller.SelectedCard); // 회복 직후 카드 사용 가능 확인
        int enemyHealth = enemy.CurrentHealth; // 공격 전 적 체력 저장
        enemyView.OnPointerClick(null); // 실제 적을 카드 대상으로 선택
        for (float deadline = Time.realtimeSinceStartup + 10f; Time.realtimeSinceStartup < deadline && devil.CurrentHealth > 1;) // 실제 공격 연출 완료 대기
        { // 공격 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 공격 연출 대기 종료
        Assert.AreEqual(1, devil.CurrentHealth); // 악마의 체력 다섯 소모와 생존 확인
        Assert.AreEqual(10, devil.CurrentBlock); // 체력 비용이 방어도에 흡수되지 않음 확인
        Assert.AreEqual(enemyHealth - 20, enemy.CurrentHealth); // 적에게 기본 피해 이십 적용 확인
        Assert.AreEqual(5, points.CurrentActionPoints); // 실제 사용 시 행동력 한 점 소모 확인
        Assert.IsTrue(deck.IsStateValid()); // 전용 카드 사용 후 덱 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 테스트 적 원본 정리
        Object.DestroyImmediate(source); // 테스트 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 전용 카드 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 체력 소모와 생존 제한 검사 지정
    public void HealthCost_BypassesBlockAndCannotKill() // 체력 소모와 사망 방지 확인
    { // 체력 소모 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(source); // 임시 아군 전투 유닛 생성
        unit.GainBlock(10); // 소모 전 방어도 열 점 적용
        int damageEvents = 0; // 피격 이벤트 횟수 초기화
        unit.DamageTaken += (target, result) => damageEvents++; // 피격 이벤트 감시
        Assert.IsTrue(unit.TrySpendHealth(5)); // 생존 가능한 체력 다섯 소모 확인
        Assert.AreEqual(95, unit.CurrentHealth); // 실제 체력 다섯 감소 확인
        Assert.AreEqual(10, unit.CurrentBlock); // 체력 소모 시 방어도 보존 확인
        Assert.AreEqual(0, damageEvents); // 체력 소모의 피격 이벤트 미발생 확인
        Assert.IsTrue(unit.ApplyPersistentHealth(5)); // 현재 체력을 다섯으로 설정
        Assert.IsFalse(unit.TrySpendHealth(5)); // 체력이 비용과 같을 때 사용 차단 확인
        Assert.AreEqual(5, unit.CurrentHealth); // 사용 차단 시 체력 유지 확인
        Assert.IsTrue(unit.ApplyPersistentHealth(6)); // 현재 체력을 여섯으로 설정
        Assert.IsTrue(unit.TrySpendHealth(5)); // 소모 후 한 체력이 남는 사용 허용 확인
        Assert.AreEqual(1, unit.CurrentHealth); // 최소 생존 체력 유지 확인
        Assert.IsFalse(unit.TrySpendHealth(0)); // 잘못된 체력 비용 차단 확인
        Assert.IsFalse(unit.IsDead); // 체력 소모에 의한 사망 없음 확인
        Object.DestroyImmediate(source); // 임시 캐릭터 원본 정리
    } // 체력 소모 검사 종료
} // 전용 능력 검사 종료
