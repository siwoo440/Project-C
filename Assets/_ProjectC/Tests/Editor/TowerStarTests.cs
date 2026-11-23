using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 카드 목록 사용
using System.Reflection; // 실제 카드 효과 처리 사용
using NUnit.Framework; // 전용 능력 규칙 검증 사용
using UnityEngine; // 전투 데이터 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용
public sealed class TowerStarTests // 탑과 별 임시 능력 검사
{ // 전용 능력 검사 시작
    [UnityTest] // 탐사 저장과 복원 검사 지정
    public IEnumerator ExplorationSave_RestoresBothExclusiveCards() // 탑과 별 전용 카드의 실제 저장 복원 확인
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
            PartyDepartureTestSupport.StartExploration(16, 17); // 탑과 별로 탐사 시작
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
            bool towerFound = false; // 탑 전용 카드 복원 여부 초기화
            bool starFound = false; // 별 전용 카드 복원 여부 초기화
            foreach (RunDeckCardEntry entry in RunDeckManager.Instance.Cards) // 복원된 회차 카드 순회
            { // 전용 카드 확인 시작
                towerFound |= entry.Card.CardId == "CRD_ARC_16_SHATTER" && entry.Owner.CharacterId == "ARC_16"; // 탑 카드와 소유자 확인
                starFound |= entry.Card.CardId == "CRD_ARC_17_HEAL" && entry.Owner.CharacterId == "ARC_17"; // 별 카드와 소유자 확인
            } // 전용 카드 확인 종료
            Assert.IsTrue(towerFound); // 탑 전용 카드 복원 확인
            Assert.IsTrue(starFound); // 별 전용 카드 복원 확인
        } // 실제 저장 복원 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 테스트 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 탐사 저장 검사 종료
    private sealed class BlockRemovalProbe // 광역 공격 전 방어도 제거 순서 확인
    { // 제거 순서 확인 시작
        public BattleUnitRuntime OtherEnemy; // 검사할 다른 적 유닛
        public bool WasRemovedBeforeDamage; // 피해 전 방어도 제거 여부
        public void HandleDamage(BattleUnitRuntime unit, BattleDamageResult result) // 첫 적의 피해 이벤트 처리
        { // 피해 이벤트 처리 시작
            WasRemovedBeforeDamage = OtherEnemy.CurrentBlock == 0; // 다른 적의 방어도 제거 여부 저장
        } // 피해 이벤트 처리 종료
    } // 제거 순서 확인 종료
    [UnityTest] // 실제 탑 카드와 별 패시브 검사 지정
    public IEnumerator StarterCards_ShatterAllBeforeDamageAndReserveExtraHealing() // 실제 광역 공격과 추가 회복 확인
    { // 실제 카드 사용 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        Assert.IsNotNull(roster); // 실제 캐릭터 목록 존재 확인
        CharacterData towerData = roster.Characters[16]; // 탑 캐릭터 원본 조회
        CharacterData starData = roster.Characters[17]; // 별 캐릭터 원본 조회
        CardData shatterCard = Resources.Load<CardData>("StarterCards/TowerShatter"); // 탑 전용 광역 카드 조회
        CardData healingCard = Resources.Load<CardData>("StarterCards/StarHealing"); // 별 전용 예약 회복 카드 조회
        Assert.IsNotNull(shatterCard); // 탑 전용 카드 존재 확인
        Assert.IsNotNull(healingCard); // 별 전용 카드 존재 확인
        Assert.AreEqual(CardTargetType.AllEnemies, shatterCard.TargetType); // 탑 카드의 전체 적 대상 확인
        Assert.AreEqual(8, shatterCard.EffectValue); // 탑 카드 기본 피해 여덟 확인
        Assert.AreEqual(3, shatterCard.SecondaryEffectValue); // 탑 카드 방어도 제거 셋 확인
        BattleUnitRuntime tower = BattleUnitRuntime.CreateAlly(towerData); // 탑 전투 유닛 생성
        BattleUnitRuntime star = BattleUnitRuntime.CreateAlly(starData); // 별 전투 유닛 생성
        EnemyData firstData = ScriptableObject.CreateInstance<EnemyData>(); // 첫 적 원본 생성
        EnemyData secondData = ScriptableObject.CreateInstance<EnemyData>(); // 두 번째 적 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(firstData); // 첫 적 전투 유닛 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(secondData); // 두 번째 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { tower, star }; // 실제 전투 아군 목록 생성
        BattleUnitRuntime[] enemies = { first, second }; // 실제 전투 적 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 전투 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(shatterCard, towerData), new RunDeckCardEntry(healingCard, starData) }; // 탑과 별 전용 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 5, 1); // 전용 카드 전투 덱 생성
        deck.DrawCards(2); // 전용 카드 두 장 드로우
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 전투 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, enemies, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, allies, enemies, new BattleStatusEffectProcessor()); // 실제 상태 효과 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(enemies, allies); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("TowerStarCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 화면 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 덱 연결 확인
        BattleUnitView towerView = CreateView(canvas.transform, tower); // 탑 유닛 화면 생성
        BattleUnitView starView = CreateView(canvas.transform, star); // 별 유닛 화면 생성
        BattleUnitView firstView = CreateView(canvas.transform, first); // 첫 적 화면 생성
        BattleUnitView secondView = CreateView(canvas.transform, second); // 두 번째 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { towerView, starView }, new[] { firstView, secondView }, actions); // 실제 카드 처리기 생성
        CardInstance shatter = null; // 탑 전용 카드 참조 준비
        CardInstance healing = null; // 별 전용 카드 참조 준비
        foreach (CardInstance card in deck.Hand) // 실제 손패 카드 순회
        { // 전용 카드 분류 시작
            if (card.SourceData == shatterCard) // 탑 전용 카드 확인
            { // 탑 카드 저장 시작
                shatter = card; // 탑 전용 카드 저장
            } // 탑 카드 저장 종료
            if (card.SourceData == healingCard) // 별 전용 카드 확인
            { // 별 카드 저장 시작
                healing = card; // 별 전용 카드 저장
            } // 별 카드 저장 종료
        } // 전용 카드 분류 종료
        first.GainBlock(5); // 첫 적 방어도 다섯 설정
        second.GainBlock(2); // 두 번째 적 방어도 둘 설정
        BlockRemovalProbe probe = new BlockRemovalProbe { OtherEnemy = second }; // 다른 적의 방어도 제거 순서 감시자 생성
        first.DamageTaken += probe.HandleDamage; // 첫 피해 시 두 번째 적 방어도 확인
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        impact.Invoke(controller, new object[] { shatter, enemies }); // 모든 적에게 탑 카드 적용
        Assert.IsTrue(probe.WasRemovedBeforeDamage); // 모든 방어도 제거 후 피해 적용 순서 확인
        Assert.AreEqual(44, first.CurrentHealth); // 방어도 둘이 남은 첫 적의 피해 확인
        Assert.AreEqual(42, second.CurrentHealth); // 방어도 전체가 제거된 두 번째 적의 피해 확인
        Assert.AreEqual(0, first.CurrentBlock); // 공격 후 첫 적의 방어도 소모 확인
        tower.TakeDamage(20, BattleDamageType.None); // 회복 대상의 체력 손실 설정
        int healthBefore = tower.CurrentHealth; // 회복 전 체력 저장
        impact.Invoke(controller, new object[] { healing, new[] { tower } }); // 별 카드의 실제 예약 회복 적용
        Assert.IsNotNull(tower.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 기존 회복 여섯 예약 확인
        Assert.IsNotNull(tower.GetStatusEffect(BattleStatusEffectType.StarPassiveReservedHealing)); // 추가 회복 두 점 예약 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 시작 확인
        Assert.AreEqual(healthBefore, tower.CurrentHealth); // 적 턴에는 회복 없음 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 아군 턴 시작 확인
        Assert.AreEqual(healthBefore + 8, tower.CurrentHealth); // 기존 예약과 추가 예약 합계 회복 확인
        Assert.IsTrue(deck.IsStateValid()); // 카드 사용 후 덱 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        statuses.Dispose(); // 상태 효과 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(firstData); // 첫 적 원본 정리
        Object.DestroyImmediate(secondData); // 두 번째 적 원본 정리
        Object.DestroyImmediate(source); // 임시 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 카드 사용 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 연결
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 방어도 부분 제거 검사 지정
    public void RemoveBlock_ClampsToAvailableAndNotifiesChanges() // 방어도 제거 수치와 알림 확인
    { // 방어도 제거 검사 시작
        EnemyData source = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(source); // 임시 적 유닛 생성
        enemy.GainBlock(10); // 초기 방어도 열 점 설정
        int notifications = 0; // 방어도 변경 알림 횟수 초기화
        enemy.BlockChanged += unit => notifications++; // 방어도 변경 이벤트 감시
        Assert.AreEqual(3, enemy.RemoveBlock(3)); // 방어도 세 점 제거 확인
        Assert.AreEqual(7, enemy.CurrentBlock); // 남은 방어도 일곱 확인
        Assert.AreEqual(7, enemy.RemoveBlock(20)); // 남은 방어도까지만 제거 확인
        Assert.AreEqual(0, enemy.CurrentBlock); // 방어도 음수 방지 확인
        Assert.AreEqual(0, enemy.RemoveBlock(3)); // 빈 방어도 제거량 확인
        Assert.AreEqual(2, notifications); // 실제 변경에만 화면 알림 확인
        Object.DestroyImmediate(source); // 임시 적 원본 정리
    } // 방어도 제거 검사 종료
    [Test] // 별의 추가 예약 회복 검사 지정
    public void StarPassive_StacksWithCardReservationOncePerTurn() // 기존 예약과 추가 회복의 독립 발동 확인
    { // 별 예약 회복 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData starData = roster.Characters[17]; // 별 캐릭터 원본 조회
        CharacterData allyData = roster.Characters[0]; // 회복 대상 캐릭터 원본 조회
        BattleUnitRuntime star = BattleUnitRuntime.CreateAlly(starData); // 별 전투 유닛 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 회복 대상 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 임시 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { star, ally }; // 전투 아군 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 전투 덱 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleStatusEffectController statuses = new BattleStatusEffectController(turns, allies, new[] { enemy }, new BattleStatusEffectProcessor()); // 상태 효과 관리자 생성
        StarRecoveryPassiveRuntime passive = new StarRecoveryPassiveRuntime(turns); // 별 추가 회복 패시브 생성
        CardData cardData = Resources.Load<CardData>("StarterCards/StarHealing"); // 기존 별 예약 회복 카드 조회
        CardInstance card = CardInstance.Create(cardData, star, 0); // 별 전용 회복 카드 생성
        CardInstance otherOwnerCard = CardInstance.Create(cardData, ally, 1); // 별이 아닌 소유자의 회복 카드 생성
        CardData attackData = Resources.Load<CardData>("StarterCards/Attack"); // 회복이 아닌 공용 공격 카드 조회
        CardInstance attackCard = CardInstance.Create(attackData, star, 2); // 별 소유의 일반 공격 카드 생성
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        ally.TakeDamage(20, BattleDamageType.None); // 회복할 체력 손실 설정
        int healthBefore = ally.CurrentHealth; // 회복 전 체력 저장
        ally.ApplyStatusEffect(BattleStatusEffectType.StarReservedHealing, 6, 1, 1, star); // 기존 카드의 회복 여섯 예약
        Assert.IsFalse(passive.TryApply(otherOwnerCard, new[] { ally })); // 별이 아닌 소유자의 패시브 발동 차단 확인
        Assert.IsFalse(passive.TryApply(attackCard, new[] { ally })); // 회복 카드가 아닌 공격의 발동 차단 확인
        Assert.IsTrue(passive.TryApply(card, new[] { ally })); // 별의 추가 회복 예약 확인
        Assert.IsFalse(passive.TryApply(card, new[] { ally })); // 같은 턴 추가 발동 차단 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 기존 회복 예약 유지 확인
        Assert.AreEqual(2, ally.GetStatusEffect(BattleStatusEffectType.StarPassiveReservedHealing).EffectiveValue); // 별도 회복 두 점 예약 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 시작 확인
        Assert.AreEqual(healthBefore, ally.CurrentHealth); // 적 턴에 예약 회복 미발동 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 다음 아군 턴 시작 확인
        Assert.AreEqual(healthBefore + 8, ally.CurrentHealth); // 기존 여섯과 추가 두 회복 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.StarReservedHealing)); // 기존 예약 소멸 확인
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.StarPassiveReservedHealing)); // 추가 예약 소멸 확인
        Assert.IsTrue(passive.TryApply(card, new[] { ally })); // 다음 턴 추가 패시브 재발동 확인
        passive.Dispose(); // 별 패시브 연결 정리
        statuses.Dispose(); // 상태 관리자 연결 정리
        turns.Dispose(); // 전투 턴 연결 정리
        Object.DestroyImmediate(source); // 임시 덱 원본 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
    } // 별 예약 회복 검사 종료
} // 전용 능력 검사 종료
