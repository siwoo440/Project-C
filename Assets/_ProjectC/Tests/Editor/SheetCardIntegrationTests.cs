using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 카드 목록 사용
using System.Reflection; // 실제 카드 효과 호출 사용
using NUnit.Framework; // 카드 효과 검사 사용
using UnityEditor; // 실제 카드 에셋 조회 사용
using UnityEngine; // 유니티 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용

public sealed class SheetCardIntegrationTests // 시트 카드 적용 검사
{ // 카드 검사 시작
    [UnityTest] // 실제 카드 효과 검사 지정
    public IEnumerator SheetCardsUseOriginalArtAndMatchingEffects() // 원본 그림과 실제 효과 확인
    { // 카드 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CardData strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_TestHeavyStrike.asset"); // 실제 강타 카드 조회
        CardData wind = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_TestBladeStorm.asset"); // 실제 검풍 카드 조회
        CardData cleanse = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_TestCleanse.asset"); // 실제 정화 카드 조회
        Assert.IsNotNull(strike.Artwork); // 강타 원본 이미지 확인
        Assert.IsNotNull(wind.Artwork); // 검풍 원본 이미지 확인
        Assert.IsNotNull(cleanse.Artwork); // 정화 원본 이미지 확인
        Assert.AreEqual(1, strike.ApCost); // 시트 강타 비용 확인
        Assert.AreEqual(8, strike.EffectValue); // 시트 강타 피해 확인
        Assert.AreEqual(3, wind.ApCost); // 시트 검풍 비용 확인
        Assert.AreEqual(10, wind.EffectValue); // 시트 검풍 피해 확인
        Assert.AreEqual(CardTargetType.AllEnemies, wind.TargetType); // 검풍 전체 적 대상 확인
        Assert.AreEqual(CardEffectType.RemoveOneDebuff, cleanse.EffectType); // 정화 한 개 해제 효과 확인
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 테스트 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(owner); // 실제 아군 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateEnemy(enemyData); // 첫 번째 적 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateEnemy(enemyData); // 두 번째 적 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(strike, owner), new RunDeckCardEntry(wind, owner), new RunDeckCardEntry(cleanse, owner) }; // 실제 카드 덱 구성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { ally }, 5, 1); // 전투 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { ally }, new[] { first, second }, 0, BattleType.Normal); // 실제 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { first, second }, new[] { ally }); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(3)); // 실제 전투 시작과 손패 생성 확인
        GameObject canvas = new GameObject("SheetCardTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 부모 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 전투 덱과 손패 연결
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new BattleUnitView[0], new BattleUnitView[0], actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 효과 처리 조회
        CardInstance strikeInstance = null; // 실제 강타 손패 참조 준비
        CardInstance windInstance = null; // 실제 검풍 손패 참조 준비
        CardInstance cleanseInstance = null; // 실제 정화 손패 참조 준비
        foreach (CardInstance card in deck.Hand) // 실제 손패 카드 순회
        { // 손패 카드 확인 시작
            if (card.SourceData == strike) // 강타 원본 확인
            { // 강타 보관 시작
                strikeInstance = card; // 강타 손패 보관
            } // 강타 보관 종료
            if (card.SourceData == wind) // 검풍 원본 확인
            { // 검풍 보관 시작
                windInstance = card; // 검풍 손패 보관
            } // 검풍 보관 종료
            if (card.SourceData == cleanse) // 정화 원본 확인
            { // 정화 보관 시작
                cleanseInstance = card; // 정화 손패 보관
            } // 정화 보관 종료
        } // 손패 카드 확인 종료
        Assert.IsNotNull(strikeInstance); // 강타 손패 확인
        Assert.IsNotNull(windInstance); // 검풍 손패 확인
        Assert.IsNotNull(cleanseInstance); // 정화 손패 확인
        MethodInfo validTarget = typeof(BattleCardActionController).GetMethod("IsValidTarget", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 대상 검사 조회
        MethodInfo effectDetail = typeof(BattleCardTooltipView).GetMethod("GetEffectDetail", BindingFlags.Static | BindingFlags.NonPublic); // 카드 상세 문구 조회
        Assert.IsFalse((bool)validTarget.Invoke(controller, new object[] { cleanseInstance, ally })); // 디버프 없는 대상 선택 차단 확인
        Assert.AreEqual("먼저 적용된 디버프 1개 해제", effectDetail.Invoke(null, new object[] { cleanseInstance })); // 정화 상세 문구 확인
        int firstHealth = first.CurrentHealth; // 첫 적 기존 체력 보관
        int secondHealth = second.CurrentHealth; // 두 번째 적 기존 체력 보관
        impact.Invoke(controller, new object[] { strikeInstance, new[] { first } }); // 실제 강타 효과 적용
        Assert.AreEqual(firstHealth - 8, first.CurrentHealth); // 강타 물리 피해 확인
        impact.Invoke(controller, new object[] { windInstance, new[] { first, second } }); // 실제 전체 검풍 효과 적용
        Assert.AreEqual(firstHealth - 18, first.CurrentHealth); // 첫 적 누적 피해 확인
        Assert.AreEqual(secondHealth - 10, second.CurrentHealth); // 두 번째 적 피해 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 3, 1); // 먼저 적용한 중독 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 1, 2, 1); // 다음 디버프 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 2, 1); // 보존할 버프 준비
        Assert.IsTrue((bool)validTarget.Invoke(controller, new object[] { cleanseInstance, ally })); // 디버프 대상 선택 허용 확인
        impact.Invoke(controller, new object[] { cleanseInstance, new[] { ally } }); // 실제 정화 효과 적용
        Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Poison)); // 먼저 적용한 디버프 제거 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 다른 디버프 유지 확인
        Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Regeneration)); // 버프 유지 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 해제
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(owner); // 아군 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 카드 검사 본문 종료
} // 카드 검사 종료
