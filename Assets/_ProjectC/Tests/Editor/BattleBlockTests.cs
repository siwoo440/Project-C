using System.Collections; // 플레이 모드 검사 사용
using System.Collections.Generic; // 빈 덱 목록 사용
using System.Reflection; // 카드 충돌 처리 검증 사용
using NUnit.Framework; // 자동 검증 기능 사용
using TMPro; // 방어도 표시 검증 사용
using UnityEngine; // 유니티 원본 객체 사용
using UnityEngine.TestTools; // 플레이 모드 전환 사용
using UnityEngine.UI; // 손패 화면 생성 사용
public sealed class BattleBlockTests // 방어도 공통 동작 검사
{ // 검사 시작
    [UnityTest] // 실제 카드 적용 검사 지정
    public IEnumerator StarterCards_ApplyBlockAndRefreshActualUnitViews() // 카드 효과와 방어도 화면 확인
    { // 실제 카드 적용 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData priestess = roster.Characters[2]; // 여사제 원본 조회
        CharacterData empress = roster.Characters[3]; // 여황제 원본 조회
        CardData ward = Resources.Load<CardData>("StarterCards/PriestessWard"); // 여사제 카드 조회
        CardData feast = Resources.Load<CardData>("StarterCards/EmpressFeast"); // 여황제 카드 조회
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 생성
        GameObject canvas = new GameObject("BlockTestCanvas", typeof(RectTransform), typeof(Canvas)); // 검사용 캔버스 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(priestess); // 여사제 전투 유닛 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(empress); // 여황제 전투 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { first, second }; // 아군 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry> { new RunDeckCardEntry(ward, priestess), new RunDeckCardEntry(feast, empress) }, allies, 5, 1); // 실제 카드 덱 생성
        deck.DrawCards(2); // 두 카드 드로우
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, allies); // 적 행동 관리자 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패 덱 연결 확인
        BattleUnitView firstView = CreateUnitView(canvas.transform, first); // 여사제 화면 생성
        BattleUnitView secondView = CreateUnitView(canvas.transform, second); // 여황제 화면 생성
        BattleUnitView enemyView = CreateUnitView(canvas.transform, enemy); // 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 행동 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { firstView, secondView }, new[] { enemyView }, actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 충돌 처리 조회
        CardInstance wardCard = FindCard(deck.Hand, ward); // 손패의 여사제 카드 조회
        CardInstance feastCard = FindCard(deck.Hand, feast); // 손패의 여황제 카드 조회
        impact.Invoke(controller, new object[] { wardCard, new[] { first } }); // 여사제 카드 실제 효과 적용
        Assert.AreEqual(2, first.CurrentBlock); // 여사제 방어도 확인
        Assert.AreEqual("방어 2", firstView.transform.Find("BlockView/BlockText").GetComponent<TMP_Text>().text); // 실제 화면 방어도 확인
        impact.Invoke(controller, new object[] { feastCard, allies }); // 여황제 카드 실제 효과 적용
        Assert.AreEqual(4, first.CurrentBlock); // 첫 아군 배분 방어도 확인
        Assert.AreEqual(1, second.CurrentBlock); // 두 번째 아군 배분 방어도 확인
        Assert.AreEqual("방어 1", secondView.transform.Find("BlockView/BlockText").GetComponent<TMP_Text>().text); // 배분 대상 화면 갱신 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 임시 적 정리
        Object.DestroyImmediate(deckData); // 임시 덱 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 카드 적용 검사 종료
    private static BattleUnitView CreateUnitView(Transform parent, BattleUnitRuntime unit) // 실제 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject unitObject = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        unitObject.transform.SetParent(parent, false); // 캔버스 자식 배치
        BattleUnitView view = unitObject.GetComponent<BattleUnitView>(); // 화면 컴포넌트 조회
        view.Bind(unit); // 실제 전투 유닛 연결
        return view; // 생성 화면 반환
    } // 유닛 화면 생성 종료
    private static CardInstance FindCard(IReadOnlyList<CardInstance> cards, CardData source) // 원본 카드로 손패 조회
    { // 손패 조회 시작
        foreach (CardInstance card in cards) // 손패 카드 순회
        { // 원본 카드 확인 시작
            if (card.SourceData == source) // 카드 원본 일치 확인
            { // 일치 카드 처리 시작
                return card; // 일치 카드 반환
            } // 일치 카드 처리 종료
        } // 원본 카드 확인 종료
        return null; // 일치 카드 없음 반환
    } // 손패 조회 종료
    [Test] // 실제 카드 설정 검사 지정
    public void StarterCards_UseApprovedTemporaryBlockValues() // 승인된 임시 방어도 카드 설정 확인
    { // 실제 카드 설정 검사 시작
        CardData priestess = Resources.Load<CardData>("StarterCards/PriestessWard"); // 여사제 카드 조회
        CardData empress = Resources.Load<CardData>("StarterCards/EmpressFeast"); // 여황제 카드 조회
        Assert.AreEqual(CardEffectType.GainBlock, priestess.EffectType); // 여사제 방어도 효과 확인
        Assert.AreEqual(CardTargetType.SingleAlly, priestess.TargetType); // 여사제 단일 아군 대상 확인
        Assert.AreEqual(2, priestess.EffectValue); // 여사제 임시 방어도 확인
        Assert.AreEqual(CardEffectType.GainBlock, empress.EffectType); // 여황제 방어도 효과 확인
        Assert.AreEqual(CardTargetType.AllAllies, empress.TargetType); // 여황제 전체 아군 대상 확인
        Assert.AreEqual(3, empress.EffectValue); // 여황제 임시 전체 방어도 확인
    } // 실제 카드 설정 검사 종료
    [Test] // 고정 방어도 배분 검사 지정
    public void Distribution_PreservesTotalAndSkipsDeadAllies() // 전체 방어도와 생존 대상 배분 확인
    { // 고정 방어도 배분 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(data); // 첫 아군 생성
        BattleUnitRuntime dead = BattleUnitRuntime.CreateAlly(data); // 사망 아군 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(data); // 두 번째 생존 아군 생성
        dead.ApplyPersistentHealth(0); // 사망 상태 적용
        Assert.AreEqual(3, BattleBlockDistribution.Apply(new[] { first, dead, second }, 3)); // 전체 적용량 확인
        Assert.AreEqual(2, first.CurrentBlock); // 앞 순서 나머지 배분 확인
        Assert.AreEqual(0, dead.CurrentBlock); // 사망 대상 제외 확인
        Assert.AreEqual(1, second.CurrentBlock); // 두 번째 생존 대상 배분 확인
        Object.DestroyImmediate(data); // 임시 원본 정리
    } // 고정 방어도 배분 검사 종료
    [Test] // 방어도 범위 검사 지정
    public void Block_RejectsInvalidGainAndClampsOverflow() // 무효 획득과 정수 범위 확인
    { // 방어도 범위 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(data); // 아군 생성
        int changes = 0; // 방어도 변경 횟수 초기화
        ally.BlockChanged += changed => changes++; // 방어도 변경 횟수 기록
        Assert.AreEqual(0, ally.GainBlock(-1)); // 음수 획득 차단 확인
        Assert.AreEqual(int.MaxValue, ally.GainBlock(int.MaxValue)); // 최대 방어도 획득 확인
        Assert.AreEqual(0, ally.GainBlock(1)); // 정수 초과 획득 차단 확인
        Assert.AreEqual(1, changes); // 실제 변경만 알림 확인
        ally.ClearBlock(); // 방어도 제거
        ally.ClearBlock(); // 중복 제거
        Assert.AreEqual(2, changes); // 중복 제거 알림 방지 확인
        Object.DestroyImmediate(data); // 임시 원본 정리
    } // 방어도 범위 검사 종료
    [Test] // 피해 흡수 검사 지정
    public void Block_AbsorbsFinalDamageAndPreviewDoesNotMutate() // 예상 피해와 실제 피해 비교
    { // 피해 흡수 검사 시작
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(data); // 아군 생성
        int initialHealth = ally.CurrentHealth; // 시작 체력 저장
        Assert.AreEqual(7, ally.GainBlock(7)); // 방어도 획득 확인
        BattleDamageResult preview = ally.PreviewDamage(10, BattleDamageType.None); // 피해 예고 조회
        Assert.AreEqual(7, preview.AbsorbedDamage); // 예상 흡수량 확인
        Assert.AreEqual(3, preview.AppliedDamage); // 예상 체력 피해 확인
        Assert.AreEqual(7, ally.CurrentBlock); // 예고 시 방어도 유지 확인
        Assert.AreEqual(initialHealth, ally.CurrentHealth); // 예고 시 체력 유지 확인
        BattleDamageResult actual = ally.TakeDamage(10, BattleDamageType.None); // 실제 피해 적용
        Assert.AreEqual(preview.AbsorbedDamage, actual.AbsorbedDamage); // 실제 흡수량 일치 확인
        Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 실제 체력 피해 일치 확인
        Assert.AreEqual(0, ally.CurrentBlock); // 방어도 소모 확인
        Assert.AreEqual(initialHealth - 3, ally.CurrentHealth); // 체력 피해 적용 확인
        Object.DestroyImmediate(data); // 임시 원본 정리
    } // 피해 흡수 검사 종료
    [Test] // 라운드 전환 검사 지정
    public void Block_ExpiresOnlyWhenEnemyTurnCompletes() // 적 턴 종료 시 방어도 제거 확인
    { // 라운드 전환 검사 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), new[] { ally }, 5, 1); // 빈 전투 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), new[] { ally }, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        ally.GainBlock(7); // 아군 방어도 획득
        enemy.GainBlock(4); // 적 방어도 획득
        Assert.IsTrue(turns.EndPlayerTurn()); // 적 턴 전환 확인
        Assert.AreEqual(7, ally.CurrentBlock); // 적 턴 중 아군 방어도 유지 확인
        Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 턴 종료 확인
        Assert.AreEqual(2, turns.CurrentRound); // 다음 라운드 확인
        Assert.AreEqual(0, ally.CurrentBlock); // 아군 방어도 제거 확인
        Assert.AreEqual(0, enemy.CurrentBlock); // 적 방어도 제거 확인
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(deckData); // 임시 덱 정리
        Object.DestroyImmediate(enemyData); // 임시 적 정리
        Object.DestroyImmediate(character); // 임시 캐릭터 정리
    } // 라운드 전환 검사 종료
    [Test] // 연속 공격 예고 검사 지정
    public void PlannedAttacks_ConsumePredictedBlockInOrder() // 공격 순서별 방어도 예고 확인
    { // 연속 공격 검사 시작
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 아군 생성
        BattleUnitRuntime firstEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 적 생성
        BattleUnitRuntime secondEnemy = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { firstEnemy, secondEnemy }, new[] { ally }); // 적 행동 관리자 생성
        Assert.AreEqual(2, actions.PrepareActions()); // 두 공격 준비 확인
        ally.GainBlock(10); // 방어도 획득
        BattleEnemyAction first = actions.PlannedActions[0]; // 첫 공격 조회
        BattleEnemyAction second = actions.PlannedActions[1]; // 두 번째 공격 조회
        Assert.AreEqual(0, first.PreviewDamage().AppliedDamage); // 첫 공격 전부 흡수 확인
        Assert.AreEqual(6, second.PreviewDamage().AppliedDamage); // 두 번째 공격 예상 체력 피해 확인
        actions.ExecuteAction(first); // 첫 공격 실행
        Assert.AreEqual(2, ally.CurrentBlock); // 첫 공격 후 방어도 확인
        Assert.AreEqual(6, second.PreviewDamage().AppliedDamage); // 두 번째 공격 예고 유지 확인
        Assert.AreEqual(6, actions.ExecuteAction(second).DamageResult.AppliedDamage); // 두 번째 실제 피해 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(enemyData); // 임시 적 정리
        Object.DestroyImmediate(character); // 임시 캐릭터 정리
    } // 연속 공격 검사 종료
} // 검사 종료
