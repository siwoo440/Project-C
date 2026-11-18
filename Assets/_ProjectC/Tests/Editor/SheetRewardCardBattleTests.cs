using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 카드 덱 목록 사용
using System.Reflection; // 실제 전투 효과 호출 사용
using NUnit.Framework; // 전투 카드 검사 사용
using UnityEditor; // 실제 카드 에셋 조회 사용
using UnityEngine; // 전투 객체 생성 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 화면 사용

public sealed class SheetRewardCardBattleTests // 시트 보상 카드 전투 검사
{ // 전투 검사 시작
    [UnityTest] // 실제 전투 효과 검사 지정
    public IEnumerator RewardCardsApplySheetEffectsInBattle() // 회복과 마법 피해 적용 확인
    { // 전투 검사 본문 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CardData heal = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetSmallHeal.asset"); // 실제 작은 치유 카드 조회
        CardData fireball = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetFireball.asset"); // 실제 불덩이 카드 조회
        CardData missile = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetMagicMissile.asset"); // 실제 마력탄 카드 조회
        Assert.IsNotNull(heal); // 회복 카드 존재 확인
        Assert.IsNotNull(fireball); // 불덩이 카드 존재 확인
        Assert.IsNotNull(missile); // 마력탄 카드 존재 확인
        CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 테스트 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 테스트 덱 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(owner); // 실제 아군 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 적 생성
        List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(heal, owner), new RunDeckCardEntry(fireball, owner), new RunDeckCardEntry(missile, owner) }; // 실제 카드 덱 구성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { ally }, 5, 1); // 전투 덱 생성
        BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 행동력 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, new[] { ally }, new[] { enemy }, 0, BattleType.Normal); // 실제 전투 턴 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, new[] { ally }); // 적 행동 관리자 생성
        Assert.IsTrue(turns.StartBattle(3)); // 실제 전투 시작과 손패 생성 확인
        GameObject canvas = new GameObject("SheetRewardBattleCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 손패 부모 연결
        BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
        Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 전투 덱과 손패 연결
        BattleActionSequenceRunner runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 카드 연출기 생성
        BattleCardActionController controller = new BattleCardActionController(deck, points, turns, hand, runner, new BattleStatusEffectProcessor(), new BattleUnitView[0], new BattleUnitView[0], actions); // 실제 카드 처리기 생성
        MethodInfo impact = typeof(BattleCardActionController).GetMethod("ResolveCardImpact", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 효과 처리 조회
        CardInstance healInstance = null; // 회복 손패 참조 준비
        CardInstance fireballInstance = null; // 불덩이 손패 참조 준비
        CardInstance missileInstance = null; // 마력탄 손패 참조 준비
        foreach (CardInstance card in deck.Hand) // 실제 손패 순회
        { // 손패 카드 확인 시작
            if (card.SourceData == heal) // 회복 카드 확인
            { // 회복 카드 보관 시작
                healInstance = card; // 회복 손패 보관
            } // 회복 카드 보관 종료
            if (card.SourceData == fireball) // 불덩이 카드 확인
            { // 불덩이 카드 보관 시작
                fireballInstance = card; // 불덩이 손패 보관
            } // 불덩이 카드 보관 종료
            if (card.SourceData == missile) // 마력탄 카드 확인
            { // 마력탄 카드 보관 시작
                missileInstance = card; // 마력탄 손패 보관
            } // 마력탄 카드 보관 종료
        } // 손패 카드 확인 종료
        Assert.IsNotNull(healInstance); // 회복 손패 확인
        Assert.IsNotNull(fireballInstance); // 불덩이 손패 확인
        Assert.IsNotNull(missileInstance); // 마력탄 손패 확인
        ally.TakeDamage(10, BattleDamageType.None); // 회복 검사용 체력 감소
        int damagedHealth = ally.CurrentHealth; // 회복 전 체력 보관
        impact.Invoke(controller, new object[] { healInstance, new[] { ally } }); // 실제 회복 효과 적용
        Assert.AreEqual(damagedHealth + 6, ally.CurrentHealth); // 시트 회복량 확인
        int enemyHealth = enemy.CurrentHealth; // 공격 전 적 체력 보관
        impact.Invoke(controller, new object[] { fireballInstance, new[] { enemy } }); // 실제 불덩이 효과 적용
        Assert.AreEqual(enemyHealth - 9, enemy.CurrentHealth); // 시트 불덩이 피해 확인
        impact.Invoke(controller, new object[] { missileInstance, new[] { enemy } }); // 실제 마력탄 효과 적용
        Assert.AreEqual(enemyHealth - 16, enemy.CurrentHealth); // 시트 마력탄 누적 피해 확인
        controller.Dispose(); // 카드 처리기 정리
        hand.Unbind(); // 손패 연결 해제
        actions.Dispose(); // 적 행동 관리자 정리
        turns.Dispose(); // 전투 턴 관리자 정리
        Object.DestroyImmediate(canvas); // 테스트 화면 정리
        Object.DestroyImmediate(source); // 덱 원본 정리
        Object.DestroyImmediate(enemyData); // 적 원본 정리
        Object.DestroyImmediate(owner); // 아군 원본 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 검사 본문 종료
} // 전투 검사 종료
