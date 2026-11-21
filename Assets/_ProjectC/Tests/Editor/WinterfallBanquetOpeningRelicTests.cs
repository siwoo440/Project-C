using System; // 테스트 전투 자원 해제
using System.Collections; // 실제 Unity 플레이 모드 전환
using System.Linq; // 실제 카드와 선택 버튼 탐색
using NUnit.Framework; // 개막 유물과 선택 상태 단언
using UnityEngine; // 실제 전투와 임시 게임 객체
using UnityEngine.TestTools; // 에디터에서 플레이 모드 정산
using UnityEngine.UI; // 실제 선택 버튼 입력
using Object = UnityEngine.Object; // 임시 유니티 객체 해제
using static WinterfallCommonCardExpansionTests; // 기존 전투 유닛 화면 구성 재사용

public sealed class WinterfallBanquetOpeningRelicTests // 서고의 선택과 계약서의 실제 전투 적용
{ // 유물 회귀 검사 시작
    [UnityTest] // 실제 적 선제와 손패 선택 클릭 검사
    public IEnumerator BookmarkChoosesOneOriginalCardAfterTheWholeAmbushHand() // 임시 찌르기를 포함한 개막 손패와 선택 순서
    { // 개막 선택 검사 시작
        yield return new EnterPlayMode(); // 실제 모달을 닫을 플레이 모드 진입
        VerifySelection(); // 손패 잠금과 선택 더미 항목의 실제 이동 확인
        yield return new ExitPlayMode(); // 검사 전투 종료 뒤 편집 환경 복귀
    } // 개막 선택 검사 종료

    [UnityTest] // 실제 취소와 후보 부족 경계 검사
    public IEnumerator BookmarkCancelsWithoutMovementAndSkipsFullOrEmptyChoices() // 취소 동작과 손패 상한 및 실제 뽑을 카드 부족
    { // 선택 불가 경계 검사 시작
        yield return new EnterPlayMode(); // 실제 선택 화면과 원본 유물 획득 준비
        VerifyCancellationAndLimits(); // 취소와 후보 없음의 실제 턴 진행 확인
        yield return new ExitPlayMode(); // 검사 전투 종료 뒤 편집 환경 복귀
    } // 선택 불가 경계 검사 종료

    [UnityTest] // 현재 진영의 상태 시간과 실제 공격력 검사
    public IEnumerator ServantContractMarksOnlyFirstLivingAllyForTwoActualAllyTurns() // 적 선제와 별개인 실제 두 번의 아군 턴
    { // 개막 상태 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 획득과 턴 상태 처리 환경 진입
        VerifyContract(); // 첫 생존 선택과 실제 공격력 및 만료 확인
        yield return new ExitPlayMode(); // 상태 검사 종료 뒤 편집 환경 복귀
    } // 개막 상태 검사 종료

    private static void VerifySelection() // 실제 선택 화면과 손패 순서 확인
    { // 개막 선택 검증 시작
        Acquire("DustyLibraryBookmark"); // 원본 서고 먼지갈피 장착
        Acquire("NobleBlackButton"); // 앞선 일차의 첫 손패 무료 찌르기 장착
        using var fight = new OpeningBattle(5, 7); // 첫 정상 카드와 임시 카드 뒤 선택 여유 확보
        Assert.IsTrue(fight.Turns.StartBattle(1, true)); // 적 선제가 실제 먼저 시작되도록 지정
        Assert.IsNull(fight.Choice); // 적 턴에는 서고 선택 화면 미생성 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 최초 아군 드로우와 개막 선택 처리
        Assert.AreEqual(2, fight.Deck.HandCount); // 선택 전에 실제 일반 카드와 임시 찌르기 도착 확인
        Assert.AreEqual(1, fight.Deck.Hand.Count(card => card.ApCost == 0)); // 첫 손패에 포함된 임시 원본 구분
        var previous = fight.Deck.DrawPile.ToArray(); // 실제 저장 순서의 선택 전 전체 덱
        var expected = previous[previous.Length - 3]; // 위에서 세 번째인 실제 선택 카드
        int points = fight.Points.CurrentActionPoints; // 선택 전 원래 행동력
        var overlay = fight.Choice; // 실제 전투에 붙은 기본 카드 선택 화면
        Assert.IsNotNull(overlay); // 첫 아군 손패 뒤 열린 기존 선택 모달 확인
        var modalCanvas = overlay.GetComponent<Canvas>(); // 열려 있는 카드 선택의 실제 그리기 캔버스
        Assert.IsTrue(modalCanvas.overrideSorting && modalCanvas.sortingOrder > 430); // 레벨 표시가 카드 설명을 가리지 않는 순서 확인
        Assert.AreEqual(3, overlay.GetComponentsInChildren<Button>(true).Count(button => button.name.StartsWith("Choice_"))); // 두 페이지에 준비된 세 실물 후보 확인
        Assert.AreEqual(2, overlay.GetComponentsInChildren<Button>().Count(button => button.name.StartsWith("Choice_"))); // 기존 선택 화면의 첫 페이지 두 후보 확인
        Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 선택 중 직접 턴 종료 차단
        Assert.IsFalse(fight.Turns.TryEscape()); // 선택 중 직접 일반 전투 도주 차단
        fight.Hand.GetComponentsInChildren<BattleCardView>().First().OnPointerClick(null); // 선택 중 원래 손패의 직접 클릭 시도
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 대기 중 카드 사용 비용과 손패 이동 차단 확인
        fight.Choose("NextPage"); // 기존 선택 화면의 두 번째 페이지로 이동
        Assert.AreEqual("Choice_2", overlay.GetComponentsInChildren<Button>().Single(button => button.name.StartsWith("Choice_")).name); // 두 번째 페이지에 열린 세 번째 후보 확인
        fight.Choose("Choice_2"); // 두 번째 페이지의 세 번째 원본 후보 선택
        Assert.IsNull(fight.Choice); // 누른 선택 화면의 즉시 입력 종료 확인
        Assert.AreSame(expected, fight.Deck.Hand.Last()); // 고른 기존 유일 카드만 끝 손패에 추가
        CollectionAssert.AreEqual(previous.Where(card => card != expected), fight.Deck.DrawPile); // 고르지 않은 모든 카드의 상대 순서 유지
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 유물 선택 행동력 무소비 확인
        Assert.AreEqual(8, fight.Deck.CardCount); // 선택으로 카드 추가 생성이 없음을 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 선택 뒤 정상 턴 종료 가능 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴 진입
        Assert.IsNull(fight.Choice); // 같은 전투에서 두 번째 개막 선택이 없음 확인
    } // 개막 선택 검증 종료

    private static void VerifyCancellationAndLimits() // 원본 덱을 보존하는 실제 선택 경계
    { // 개막 선택 경계 검증 시작
        Acquire("DustyLibraryBookmark"); // 실제 취소 분기가 필요한 개막 유물 보유
        using (var fight = new OpeningBattle(4, 2)) // 기본 한 장 뒤 정확히 한 후보가 남는 전투
        { // 적은 후보와 실제 취소 검증 시작
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 일반 개막 손패 이후 선택 시작
            var original = fight.Deck.DrawPile.ToArray(); // 취소 전 실제 드로우 더미 사본
            Assert.AreEqual(1, fight.Choice.GetComponentsInChildren<Button>().Count(button => button.name.StartsWith("Choice_"))); // 한 장만 남은 경우 실제 한 후보 확인
            fight.Choose("CancelChoice"); // 기본 선택 화면의 취소 버튼 실행
            Assert.IsNull(fight.Choice); // 취소 뒤 선택 화면 닫힘 확인
            CollectionAssert.AreEqual(original, fight.Deck.DrawPile); // 취소에 의한 순서 변경과 이동 없음 확인
            Assert.AreEqual(1, fight.Deck.HandCount); // 취소에 의한 손패 지급 없음 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 취소 뒤 정상 턴 입력 복구 확인
        } // 적은 후보와 실제 취소 검증 종료
        using (var full = new OpeningBattle(1, 3)) // 남은 덱이 있지만 실제 손패가 가득 찬 전투
        { // 가득 찬 개막 손패 확인 시작
            Assert.IsTrue(full.Turns.StartBattle(1)); // 한 장의 정상 상한을 채운 개막
            Assert.IsNull(full.Choice); // 입력을 잠그는 빈 모달이 없음 확인
            Assert.IsTrue(full.Turns.EndPlayerTurn()); // 정상 손패 상한에서 일반 진행 확인
        } // 가득 찬 개막 손패 확인 종료
        using var empty = new OpeningBattle(4, 1); // 손패 여유와 실제 잔여 드로우가 없는 전투
        Assert.IsTrue(empty.Turns.StartBattle(1)); // 마지막 유료 카드 한 장을 기본 손패에 발급
        Assert.IsNull(empty.Choice); // 드로우 더미 빈 경우 선택 화면 없음 확인
        Assert.IsTrue(empty.Turns.EndPlayerTurn()); // 선택 후보 없음에도 정상 진행 확인
    } // 개막 선택 경계 검증 종료

    private static void VerifyContract() // 첫 생존 아군의 실제 공격 강화와 소멸
    { // 종복 표식 검증 시작
        Acquire("OldServantContract"); // 원본 낡은 종복 계약서 보유
        using var fight = new OpeningBattle(4, 3); // 두 아군과 실제 적을 가진 독립 전투
        Assert.IsTrue(fight.Allies[0].ApplyPersistentHealth(0)); // 배치 앞의 쓰러진 아군 준비
        var target = fight.Allies[1]; // 실제 첫 배치 생존 아군 선택
        int originalAttack = target.AttackPowerBonus; // 다른 강화가 적용되기 전 실제 공격력
        Assert.IsTrue(fight.Turns.StartBattle(1, true)); // 앞선 적 턴이 있는 시작으로 개막 상태 적용
        Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.ServantContractMark)); // 사망한 선배 아군의 계약 제외 확인
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.ServantContractMark).RemainingTurns); // 적 행동 이전 실제 두 턴 지속 확인
        Assert.AreEqual(originalAttack + 5, target.AttackPowerBonus); // 일반 카드가 받는 현재 공격력 증가 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 선제 종료하여 실제 첫 아군 턴 도달
        Assert.AreEqual(2, target.GetStatusEffect(BattleStatusEffectType.ServantContractMark).RemainingTurns); // 적 턴 이후에도 첫 아군 턴의 두 카운트 유지
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 턴의 실제 만료 정산
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 아군 턴 진입
        Assert.AreEqual(1, target.GetStatusEffect(BattleStatusEffectType.ServantContractMark).RemainingTurns); // 두 번째 아군 턴에서 한 카운트 남음 확인
        Assert.AreEqual(originalAttack + 5, target.AttackPowerBonus); // 두 번째 턴까지 실공격력 유지 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 턴의 종료 만료 진행
        Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.ServantContractMark)); // 실제 만료된 표식 제거 확인
        Assert.AreEqual(originalAttack, target.AttackPowerBonus); // 계약 외의 원래 공격력 복구 확인
    } // 종복 표식 검증 종료

    private static void Acquire(string key) // 실제 유물 리소스와 전투 보관함으로 획득
    { // 검사 유물 보유 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 고유 자산 경로의 원본 유물 로드
        Assert.IsNotNull(relic, key); // 실제 등록을 확인할 원본 자료 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 기존 회차 획득 경로로 실제 보유
    } // 검사 유물 보유 종료

    private sealed class OpeningBattle : IDisposable // 두 진영과 실제 카드 선택을 사용하는 검사 전투
    { // 검사 구성 시작
        private readonly GameObject canvas; // 게임 모달이 연결되는 실제 캔버스
        private readonly EnemyData enemyData; // 살아 있는 적을 만들 빈 검사 원본
        private readonly DeckData source; // 실제 손패 카드의 원본 회차 덱
        private readonly BattleEnemyActionRuntime actions; // 방어 없는 적의 실제 약화와 전투 연결
        private readonly BattleStatusEffectController statuses; // 일반 턴 상태 수명 처리
        private readonly BattleCardActionController controller; // 실제 플레이어 카드 입력과 개막 모달 연결
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(5); // 선택 전후 비교할 실제 행동력
        internal readonly BattleUnitRuntime[] Allies; // 정해진 순서의 실제 생존 확인 대상
        internal readonly BattleDeckRuntime Deck; // 원본 이동과 카드 수를 확인하는 실제 덱
        internal readonly BattleTurnRuntime Turns; // 개막과 진영 종료를 처리하는 턴 상태
        internal readonly BattleHandView Hand; // 실제 클릭과 선택 모달을 연결하는 손패
        internal BattleDrawChoiceView Choice => canvas.GetComponentInChildren<BattleDrawChoiceView>(); // 실제 활성 개막 카드 선택 화면

        internal OpeningBattle(int capacity, int cards) // 다른 수의 후보와 손패 상한을 생성
        { // 독립 전투 구성 시작
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 원래 플레이 가능한 실제 아군 명단
            Allies = roster.Characters.Take(2).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 첫 생존 효과를 확인할 순서의 아군 생성
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 외부 전투에 영향 없는 임시 적 원본
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", enemyData); // 지속 테스트에 안전한 실제 적 체력
            var enemies = new[] { BattleUnitRuntime.CreateEnemy(enemyData) }; // 실제 적 턴의 전투 지속 대상
            var thrust = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog").Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 전투와 같은 원본 기본 찌르기
            source = DeckData.CreateRuntime(Enumerable.Range(0, cards).Select(_ => new DeckCardEntry(thrust, Allies[1].CharacterSource)).ToArray()); // 죽지 않은 소유자의 개별 원본 더미 구성
            Deck = BattleDeckRuntime.Create(source, Allies, capacity, 266); // 실제 고정 셔플과 매검사 손패 상한 설정
            canvas = new GameObject("OpeningChoiceBattle", typeof(RectTransform), typeof(Canvas)); // 실제 개막 모달이 렌더되는 캔버스 생성
            var item = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 컴포넌트와 배경 생성
            item.transform.SetParent(canvas.transform, false); // 전투 캔버스와 손패 연결
            Hand = item.GetComponent<BattleHandView>(); // 일반 전투의 손패 입력 컴포넌트
            var allyViews = Allies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 실제 아군 대상 입력 화면 생성
            var enemyViews = enemies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 실제 적 대상 입력 화면 생성
            var runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 사용과 잠금을 확인할 실제 연출
            var processor = new BattleStatusEffectProcessor(); // 기존 상태 수명과 시작 턴 효과 계산기
            Turns = new BattleTurnRuntime(Deck, Points, Allies, enemies, 1, BattleType.Normal); // 실제 개막과 이후 기본 드로우 한 장을 잇는 전투 구성
            actions = new BattleEnemyActionRuntime(enemies, Allies); // 연결된 실제 적 행동 관리자 등록
            statuses = new BattleStatusEffectController(Turns, Allies, enemies, processor); // 원래 진영별 상태 변화 연결
            Assert.IsTrue(Hand.Bind(Deck, Points, Turns)); // 실제 전투와 같은 순서로 손패 연결
            controller = new BattleCardActionController(Deck, Points, Turns, Hand, runner, processor, allyViews, enemyViews, actions); // 시작 전에 실제 카드 선택 관리자 장착
        } // 독립 전투 구성 종료

        internal void Choose(string buttonName) // 화면에 실제 생성된 선택 입력 수행
        { // 실제 모달 버튼 확인 시작
            Choice.GetComponentsInChildren<Button>().Single(button => button.name == buttonName).onClick.Invoke(); // 후보 또는 취소 버튼의 실제 이벤트
        } // 실제 모달 버튼 확인 종료

        public void Dispose() // 이번에 만든 전투와 원본만 해제
        { // 검사 자원 정리 시작
            controller.Dispose(); // 실제 카드 입력과 열려 있는 모달 제거
            Hand.Unbind(); // 실제 손패 이벤트 연결 해제
            statuses.Dispose(); // 턴 상태 처리의 모든 구독 제거
            actions.Dispose(); // 생성 적 행동 관리자의 상태 구독 제거
            Turns.Dispose(); // 실제 전투 영역과 유물 이벤트 연결 해제
            Object.Destroy(canvas); // 검사 자체의 캔버스만 제거
            Object.Destroy(source); // 게임 원본과 별개인 임시 회차 덱 제거
            Object.Destroy(enemyData); // 검사 전용 적 원본 제거
        } // 검사 자원 정리 종료
    } // 검사 구성 종료
} // 유물 회귀 검사 종료
