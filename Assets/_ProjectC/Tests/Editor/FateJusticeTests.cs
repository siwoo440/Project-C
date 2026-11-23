using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 전투 카드 목록 사용
using System.Reflection; // 적 행동 임시 설정 사용
using NUnit.Framework; // 전용 능력 검증 사용
using TMPro; // 피해 예고 문구 확인 사용
using UnityEditor; // 시험용 적 에셋 조회 사용
using UnityEngine; // 테스트 원본 데이터 생성 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용
public sealed class FateJusticeTests // 운명과 정의 임시 능력 검사
{ // 전용 능력 검사 시작
    [Test] // 시험용 적 공격 패턴 검사 지정
    public void TestEnemy_ProvidesRandomAttackForReroll() // 실제 탐사 조우의 재굴림 가능 공격 확인
    { // 시험용 적 패턴 검사 시작
        EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_TestPoison.asset"); // 시험용 독 적 조회
        Assert.IsNotNull(enemy); // 시험용 독 적 존재 확인
        Assert.AreEqual(EnemyActionType.Attack, enemy.GetActionPattern(0).ActionType); // 첫 패턴의 공격 행동 확인
        Assert.AreEqual(EnemyTargetRule.RandomLiving, enemy.GetActionPattern(0).TargetRule); // 첫 공격의 무작위 대상 확인
    } // 시험용 적 패턴 검사 종료
    [UnityTest] // 실제 전용 카드 사용 검사 지정
    public IEnumerator StarterCards_ApplyRerollShareAndDisplayDamage() // 실제 카드 효과와 피해 예고 확인
    { // 전용 카드 사용 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData fate = roster.Characters[10]; // 운명 캐릭터 조회
        CharacterData justice = roster.Characters[11]; // 정의 캐릭터 조회
        CardData fateCard = Resources.Load<CardData>("StarterCards/FateReroll"); // 운명 전용 카드 조회
        CardData justiceCard = Resources.Load<CardData>("StarterCards/JusticeShare"); // 정의 전용 카드 조회
        Assert.AreEqual(CardEffectType.RerollRandomAttack, fateCard.EffectType); // 운명 카드 효과 확인
        Assert.AreEqual(CardEffectType.ShareAllyAttack, justiceCard.EffectType); // 정의 카드 효과 확인
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(fate); // 운명 전투 유닛 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(justice); // 정의 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        typeof(EnemyData).GetField("targetRule", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemyData, EnemyTargetRule.RandomLiving); // 무작위 공격 대상 설정
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        BattleUnitRuntime[] allies = { first, second }; // 실제 전투 아군 목록 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 전투 덱 원본 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(fateCard, fate), new RunDeckCardEntry(justiceCard, justice) }; // 두 전용 카드 등록
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 5, 1); // 전용 카드 전투 덱 생성
        deck.DrawCards(2); // 두 전용 카드를 손패로 이동
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 전투 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 전투 턴 관리자 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, allies); // 적 행동 관리자 생성
        Assert.AreEqual(1, actions.PrepareActions()); // 무작위 공격 준비 확인
        Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작 확인
        GameObject canvas = new GameObject("FateJusticeCanvas", typeof(RectTransform), typeof(Canvas)); // 전투 화면 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 캔버스 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 손패와 전투 덱 연결 확인
        BattleUnitView firstView = CreateView(canvas.transform, first); // 운명 화면 생성
        BattleUnitView secondView = CreateView(canvas.transform, second); // 정의 화면 생성
        BattleUnitView enemyView = CreateView(canvas.transform, enemy); // 적 화면 생성
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new[] { firstView, secondView }, new[] { enemyView }, actions); // 실제 카드 처리기 생성
        CardInstance fateInstance = null; // 운명 카드 참조 준비
        CardInstance justiceInstance = null; // 정의 카드 참조 준비
        foreach (CardInstance card in deck.Hand) // 실제 손패 카드 순회
        { // 전용 카드 확인 시작
            if (card.SourceData == fateCard) // 운명 카드 원본 확인
            { // 운명 카드 저장 시작
                fateInstance = card; // 운명 카드 저장
            } // 운명 카드 저장 종료
            if (card.SourceData == justiceCard) // 정의 카드 원본 확인
            { // 정의 카드 저장 시작
                justiceInstance = card; // 정의 카드 저장
            } // 정의 카드 저장 종료
        } // 전용 카드 확인 종료
        MethodInfo valid = typeof(BattleCardActionController).GetMethod("IsValidTarget", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 대상 검증 조회
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 효과 처리 조회
        Assert.IsTrue((bool)valid.Invoke(controller, new object[] { fateInstance, enemy })); // 무작위 공격의 운명 카드 대상 확인
        impact.Invoke(controller, new object[] { fateInstance, new[] { enemy } }); // 운명 카드로 실제 공격 대상 재굴림
        Assert.IsTrue(actions.PlannedActions[0].ChangeTarget(first)); // 분담 검사 대상 지정
        Assert.IsTrue((bool)valid.Invoke(controller, new object[] { justiceInstance, first })); // 동료의 정의 카드 대상 확인
        impact.Invoke(controller, new object[] { justiceInstance, new[] { first } }); // 정의 카드로 실제 피해 분담 설정
        BattleEnemyAction action = actions.PlannedActions[0]; // 분담이 적용된 적 공격 조회
        Assert.AreSame(second, action.SharedDamageRecipient); // 정의가 분담자로 등록됨 확인
        enemyView.SetEnemyIntent(action); // 실제 적 공격 예고 화면 갱신
        TMP_Text intent = enemyView.transform.Find("EnemyIntent").GetComponentInChildren<TMP_Text>(); // 공격 예고 문구 조회
        StringAssert.Contains("분담", intent.text); // 분담 피해 화면 표시 확인
        StringAssert.Contains(second.DisplayName, intent.text); // 분담자 이름 화면 표시 확인
        Assert.IsTrue(deck.IsStateValid()); // 전용 카드 사용 후 덱 보존 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 화면 연결 정리
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(enemyData); // 테스트 적 원본 정리
        Object.DestroyImmediate(source); // 테스트 덱 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전용 카드 사용 검사 종료
    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 화면 생성
    { // 유닛 화면 생성 시작
        GameObject target = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 유닛 화면 객체 생성
        target.transform.SetParent(parent, false); // 전투 화면 자식 배치
        BattleUnitView view = target.GetComponent<BattleUnitView>(); // 유닛 화면 조회
        view.Bind(unit); // 전투 유닛 연결
        return view; // 유닛 화면 반환
    } // 유닛 화면 생성 종료
    [Test] // 분담 해제 조건 검사 지정
    public void ShareDamage_EndsWhenProtectorDiesOrAttackTargetChanges() // 사망과 대상 변경 시 분담 해제 확인
    { // 분담 해제 검사 시작
        CharacterData firstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 동료 원본 생성
        CharacterData secondData = ScriptableObject.CreateInstance<CharacterData>(); // 분담자 원본 생성
        CharacterData thirdData = ScriptableObject.CreateInstance<CharacterData>(); // 새 공격 대상 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(firstData); // 첫 동료 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(secondData); // 분담자 생성
        BattleUnitRuntime third = BattleUnitRuntime.CreateAlly(thirdData); // 새 공격 대상 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, new[] { first, second, third }); // 적 행동 관리자 생성
        actions.PrepareActions(); // 첫 동료 대상 공격 준비
        BattleEnemyAction action = actions.PlannedActions[0]; // 예정 공격 조회
        Assert.IsTrue(actions.TryShareAllyAttack(second, first)); // 첫 동료의 피해 분담 설정
        Assert.IsTrue(actions.TryRedirectAttack(enemy, third)); // 공격 대상을 새 동료로 변경
        Assert.IsNull(action.SharedDamageRecipient); // 대상 변경 시 기존 분담 해제 확인
        Assert.AreEqual(0, action.PreviewSharedDamage()); // 기존 분담 피해 예고 제거 확인
        Assert.IsTrue(actions.TryShareAllyAttack(second, third)); // 새 대상의 피해 분담 설정
        second.TakeDamage(100000, BattleDamageType.Physical); // 분담자 사망 처리
        Assert.IsNull(action.SharedDamageRecipient); // 사망한 분담자 연결 해제 확인
        Assert.AreEqual(0, action.PreviewSharedDamage()); // 사망한 분담자의 피해 예고 제거 확인
        int health = third.CurrentHealth; // 새 대상의 이전 체력 저장
        int fullDamage = action.PreviewDamage().AppliedDamage; // 분담 해제 후 전체 피해 확인
        actions.ExecuteAction(action); // 분담 없는 공격 실행
        Assert.AreEqual(health - fullDamage, third.CurrentHealth); // 전체 피해가 새 대상에 적용됨 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(firstData); // 첫 동료 원본 정리
        Object.DestroyImmediate(secondData); // 분담자 원본 정리
        Object.DestroyImmediate(thirdData); // 새 대상 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 분담 해제 검사 종료
    [Test] // 무작위 공격 대상 재굴림 검사 지정
    public void Reroll_OnlyAllowsRandomAttackAndKeepsLivingTargets() // 재굴림 대상과 유효 규칙 확인
    { // 재굴림 검사 시작
        CharacterData firstData = ScriptableObject.CreateInstance<CharacterData>(); // 첫 동료 원본 생성
        CharacterData secondData = ScriptableObject.CreateInstance<CharacterData>(); // 두 번째 동료 원본 생성
        EnemyData randomData = ScriptableObject.CreateInstance<EnemyData>(); // 무작위 공격 적 원본 생성
        EnemyData fixedData = ScriptableObject.CreateInstance<EnemyData>(); // 고정 공격 적 원본 생성
        typeof(EnemyData).GetField("targetRule", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(randomData, EnemyTargetRule.RandomLiving); // 무작위 대상 규칙 설정
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(firstData); // 첫 동료 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(secondData); // 두 번째 동료 생성
        BattleUnitRuntime randomEnemy = BattleUnitRuntime.CreateEnemy(randomData); // 무작위 공격 적 생성
        BattleUnitRuntime fixedEnemy = BattleUnitRuntime.CreateEnemy(fixedData); // 고정 공격 적 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { randomEnemy, fixedEnemy }, new[] { first, second }); // 적 행동 관리자 생성
        Assert.AreEqual(2, actions.PrepareActions()); // 두 공격 준비 확인
        Assert.IsFalse(actions.TryRerollRandomAttack(fixedEnemy)); // 고정 대상 공격의 재굴림 차단 확인
        Assert.IsTrue(actions.CanRerollRandomAttack(randomEnemy)); // 무작위 공격 재굴림 가능 확인
        Assert.IsTrue(actions.TryRerollRandomAttack(randomEnemy)); // 무작위 공격 재굴림 확인
        BattleUnitRuntime target = actions.FindAction(randomEnemy).Target; // 재굴림된 대상 조회
        Assert.IsTrue(target == first || target == second); // 생존 동료 중 재굴림 결과 확인
        second.TakeDamage(100000, BattleDamageType.Physical); // 한 동료 사망 처리
        Assert.IsFalse(actions.CanRerollRandomAttack(randomEnemy)); // 생존 대상 한 명인 재굴림 차단 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(firstData); // 첫 동료 원본 정리
        Object.DestroyImmediate(secondData); // 두 번째 동료 원본 정리
        Object.DestroyImmediate(randomData); // 무작위 적 원본 정리
        Object.DestroyImmediate(fixedData); // 고정 적 원본 정리
    } // 재굴림 검사 종료
    [Test] // 방어 적용 후 피해 분담 검사 지정
    public void ShareDamage_UsesTargetDefenseAndOddPointGoesToTarget() // 방어 후 피해와 홀수 분배 확인
    { // 피해 분담 검사 시작
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 보호 대상 원본 생성
        CharacterData justiceData = ScriptableObject.CreateInstance<CharacterData>(); // 정의 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 원본 생성
        typeof(EnemyData).GetField("basicAttackPower", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemyData, 11); // 홀수 공격 수치 설정
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 보호 대상 생성
        BattleUnitRuntime justice = BattleUnitRuntime.CreateAlly(justiceData); // 정의 유닛 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 공격 적 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, new[] { ally, justice }); // 예정 행동 관리자 생성
        ally.GainBlock(2); // 원래 대상의 방어도 두 점 설정
        justice.GainBlock(10); // 정의의 방어도 열 점 설정
        Assert.AreEqual(1, actions.PrepareActions()); // 예정 공격 준비 확인
        BattleEnemyAction action = actions.PlannedActions[0]; // 첫 예정 공격 조회
        int expectedDamage = action.PreviewDamage().AppliedDamage; // 방어 적용 후 예정 피해 저장
        Assert.AreEqual(9, expectedDamage); // 홀수 피해 설정 확인
        Assert.IsFalse(actions.TryShareAllyAttack(ally, ally)); // 자기 자신의 피해 분담 차단 확인
        Assert.IsTrue(actions.TryShareAllyAttack(justice, ally)); // 정의의 피해 분담 지정 확인
        Assert.AreEqual(5, action.PreviewDamage().AppliedDamage); // 원래 대상의 홀수 올림 예고 확인
        Assert.AreEqual(4, action.PreviewSharedDamage()); // 분담자의 실제 예상 피해 확인
        int allyHealth = ally.CurrentHealth; // 보호 대상의 이전 체력 저장
        int justiceHealth = justice.CurrentHealth; // 정의의 이전 체력 저장
        actions.ExecuteAction(action); // 실제 적 공격 실행
        Assert.AreEqual(allyHealth - 5, ally.CurrentHealth); // 원래 대상의 다섯 피해 확인
        Assert.AreEqual(justiceHealth - 4, justice.CurrentHealth); // 정의의 네 피해 확인
        Assert.AreEqual(0, ally.CurrentBlock); // 원래 대상의 방어도 소모 확인
        Assert.AreEqual(10, justice.CurrentBlock); // 이미 분담된 피해에 정의 방어도 미적용 확인
        actions.Dispose(); // 예정 행동 관리자 정리
        Object.DestroyImmediate(allyData); // 보호 대상 원본 정리
        Object.DestroyImmediate(justiceData); // 정의 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
    } // 피해 분담 검사 종료
} // 전용 능력 검사 종료
