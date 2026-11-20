using System.Collections; // 카드 연출 대기
using System.Linq; // 손패와 유물 조회
using NUnit.Framework; // 전투 효과 비교
using UnityEngine; // 검사 카드 데이터 생성
using UnityEngine.TestTools; // 플레이 모드 진입과 종료
using UnityEngine.UI; // 확인 후보 선택 버튼
using Fight = WinterfallSwordCombatTests.Fight; // 실제 손패와 전투 환경 재사용
using Object = UnityEngine.Object; // 임시 카드 원본 정리

public sealed class WinterfallInspectionAndPendantRelicTests // 덱 확인과 첫 피해 및 턴당 유지 유물 검사
{ // 전투 유물 검사 시작
    [UnityTest] // 첫 직접 명중과 턴별 유지 피해 검사
    public IEnumerator FirstDamageIgnoresIndirectDamageAndRetainedDamageRequiresAnotherCard() // 직접 명중과 사용 후 손패 조건 확인
    { // 첫 피해 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return VerifyFirstAndRetainedDamage(); // 전환한 플레이 모드에서 직접 명중의 전체 흐름 실행
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 첫 피해 검사 종료

    private static IEnumerator VerifyFirstAndRetainedDamage() // 플레이 모드에서 초기화한 실제 직접 피해와 유지 확인
    { // 직접 피해와 유지 검사 시작
        Hold(RelicEffectType.FirstCardDamageBonus, RelicEffectType.RetainedHandDamage); // 붉은 잉크병과 검은 깃털 펜던트 획득
        var attack = MakeCard("DAY240_TEST_ATTACK", CardEffectType.Damage, CardTargetType.SingleEnemy, 5); // 고정 피해를 주는 적 지정 카드
        var retainedAttack = MakeCard("DAY240_TEST_RETAINED_ATTACK", CardEffectType.Damage, CardTargetType.SingleEnemy, 5, true); // 사용을 확정하면 손패를 떠나는 유지 공격
        var retained = MakeCard("DAY240_TEST_RETAINED", CardEffectType.GainBlock, CardTargetType.Self, 1, true); // 유지 속성을 가진 손패 대조 카드
        try // 검사 카드 원본 정리 보장
        { // 실제 피해 검증 시작
            using var fight = new Fight(new[] { retainedAttack, attack, attack, retained }); // 유지 공격과 일반 직접 타격 준비
            var saved = fight.Deck.Hand.Single(card => card.SourceData == retained); // 실제 유지 카드 조회
            Assert.IsTrue(fight.Deck.DiscardCard(saved)); // 실제로 사용할 공격 카드만 유지 속성을 가진 상태
            var foe = fight.Enemies[0]; // 직접 명중을 검증할 첫 적
            foe.TakeDamage(1, BattleDamageType.None, fight.Allies[0]); // 카드 타격을 거치지 않는 피해 적용
            int health = foe.CurrentHealth; // 간접 피해 뒤 기준 체력
            foe.GainBlock(20); // 직접 명중을 전부 흡수할 방어도 확보
            int damage = fight.Allies[0].ModifyOutgoingDamage(attack.EffectValue); // 소유자 보정을 반영한 기본 피해
            yield return Play(fight, retainedAttack, fight.EnemyViews[0]); // 사용 후 다른 유지 카드가 남지 않는 첫 직접 명중
            Assert.AreEqual(health, foe.CurrentHealth); // 방어도 흡수와 실제 체력 보존 확인
            Assert.AreEqual(20 - damage - 2, foe.CurrentBlock); // 잉크병만 방어도 앞에서 한 번 추가
            Assert.IsTrue(fight.Deck.TryRecoverDiscard(saved)); // 유지 카드가 손패에 다시 들어온 조건 준비
            var second = fight.Enemies[1]; // 방어도가 없는 두 번째 적
            yield return Play(fight, attack, fight.EnemyViews[1]); // 같은 턴의 첫 유지 조건 명중
            Assert.AreEqual(second.MaxHealth - damage - 1, second.CurrentHealth); // 잉크병 재발동 없이 유지 피해만 적용
            yield return Play(fight, attack, fight.EnemyViews[1]); // 같은 턴에 다시 직접 명중
            Assert.AreEqual(second.MaxHealth - 2 * damage - 1, second.CurrentHealth); // 같은 턴의 유지 피해 중복 방지
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴까지 실제 손패 정리
            Assert.Contains(saved, fight.Deck.Hand.ToArray()); // 유지 카드의 다음 턴 손패 보존 확인
            yield return Play(fight, attack, fight.EnemyViews[1]); // 다음 턴의 첫 직접 명중
            Assert.AreEqual(second.MaxHealth - 3 * damage - 2, second.CurrentHealth); // 다음 턴 유지 피해만 다시 지급
        } // 실제 피해 검증 종료
        finally // 임시 원본 해제
        { // 피해 검사 정리 시작
            Object.Destroy(attack); // 공격 카드 원본 제거
            Object.Destroy(retainedAttack); // 유지 공격 카드 원본 제거
            Object.Destroy(retained); // 유지 카드 원본 제거
        } // 피해 검사 정리 종료
    } // 직접 피해와 유지 검사 종료

    [UnityTest] // 회복 대상 체력에 따른 턴당 발동 검사
    public IEnumerator RetainedHealingSkipsFullHealthAndRequiresAnotherRetainedCard() // 실제 부상 회복과 사용 후 손패 조건 확인
    { // 유지 회복 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return VerifyRetainedHealing(); // 전환한 플레이 모드에서 유지 회복의 전체 흐름 실행
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 유지 회복 검사 종료

    private static IEnumerator VerifyRetainedHealing() // 플레이 모드에서 준비한 회복 카드와 턴별 유지 확인
    { // 실제 유지 회복 검사 시작
        Hold(RelicEffectType.RetainedHandHealing); // 흰 깃털 펜던트 획득
        var heal = MakeCard("DAY240_TEST_HEAL", CardEffectType.Heal, CardTargetType.Self, 2); // 소유자의 직접 체력 회복 카드
        var retainedHeal = MakeCard("DAY240_TEST_RETAINED_HEAL", CardEffectType.Heal, CardTargetType.Self, 2, true); // 사용 뒤 손패를 떠나는 유지 회복 카드
        var retained = MakeCard("DAY240_TEST_RETAINED", CardEffectType.GainBlock, CardTargetType.Self, 1, true); // 실제 손패에 남기는 유지 카드
        try // 검사 카드 원본 정리 보장
        { // 회복 전투 검증 시작
            using var fight = new Fight(new[] { heal, heal, heal, retainedHeal, retained }); // 실제 회복과 유지 조건별 비교 전투
            var owner = fight.Allies[0]; // 모든 회복 카드의 실제 대상
            var saved = fight.Deck.Hand.Single(card => card.SourceData == retained); // 별도 유지 카드 조회
            Assert.IsTrue(fight.Deck.DiscardCard(saved)); // 유지 회복 카드만 손패에 남긴 상태
            yield return Play(fight, heal); // 체력이 가득한 상태의 회복 카드 사용
            Assert.AreEqual(owner.MaxHealth, owner.CurrentHealth); // 체력 상한 유지 확인
            int injury = owner.MaxHealth - 15; // 여러 회복을 비교할 부상 체력
            owner.ApplyPersistentHealth(injury); // 정신력 변화 없는 부상 준비
            int healing = owner.ModifyOutgoingHealing(heal.EffectValue); // 소유자 보정을 반영한 기본 회복량
            yield return Play(fight, retainedHeal); // 사용 후 다른 유지 카드가 남지 않는 회복
            Assert.AreEqual(injury + healing, owner.CurrentHealth); // 사용 카드 한 장만으로 펜던트 미발동 확인
            Assert.IsTrue(fight.Deck.TryRecoverDiscard(saved)); // 다른 유지 카드가 손패에 남는 조건 준비
            yield return Play(fight, heal); // 이번 턴의 첫 유효 유지 회복
            Assert.AreEqual(injury + 2 * healing + 1, owner.CurrentHealth); // 최대 체력과 유지 카드 부족 상태에서 기회 보존 확인
            yield return Play(fight, heal); // 같은 턴의 두 번째 실제 회복
            Assert.AreEqual(injury + 3 * healing + 1, owner.CurrentHealth); // 같은 턴의 회복 증가 중복 방지
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 유지 카드가 남은 다음 아군 턴 시작
            yield return Play(fight, heal); // 새 턴의 첫 실제 회복
            Assert.AreEqual(injury + 4 * healing + 2, owner.CurrentHealth); // 턴당 회복 증가 갱신 확인
        } // 회복 전투 검증 종료
        finally // 임시 원본 해제
        { // 회복 검사 정리 시작
            Object.Destroy(heal); // 회복 카드 원본 제거
            Object.Destroy(retainedHeal); // 유지 회복 카드 원본 제거
            Object.Destroy(retained); // 유지 카드 원본 제거
        } // 회복 검사 정리 종료
    } // 실제 유지 회복 검사 종료

    [UnityTest] // 확인 범위 안의 추가 후보 실제 선택 검사
    public IEnumerator BookmarkOffersAndAcceptsExtraCandidateForBothEffects() // 재배치와 한 장 선택의 후보 범위 확인
    { // 확인 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 진입
        yield return VerifyBothBookmarkChoices(); // 전환한 플레이 모드에서 추가 확인 후보 두 종류 검증
        yield return new ExitPlayMode(); // 실제 전투 입력 환경 종료
    } // 확인 유물 검사 종료

    private static IEnumerator VerifyBothBookmarkChoices() // 플레이 모드에서 초기화한 실제 두 카드의 후보 확인
    { // 확인 후보 조작 검사 시작
        Hold(RelicEffectType.InspectionChoiceBonus); // 금박 책갈피 획득
        var arrange = MakeCard("DAY240_TEST_ARRANGE", CardEffectType.ArrangeDrawTop, CardTargetType.Self, 2); // 덱 위 순서를 바꾸는 카드
        var choose = MakeCard("DAY240_TEST_CHOOSE", CardEffectType.ChooseDraw, CardTargetType.Self, 2); // 후보 중 한 장만 뽑는 카드
        var filler = MakeCard("DAY240_TEST_FILLER", CardEffectType.GainBlock, CardTargetType.Self, 1); // 추가 확인 후보를 채울 카드
        try // 검사 카드 원본 정리 보장
        { // 덱 위 후보 검증 시작
            using var fight = new Fight(new[] { arrange, choose, filler, filler, filler, filler }); // 확인 한도를 넘어서는 후보가 있는 전투
            foreach (var candidate in fight.Deck.Hand.Where(card => card.SourceData == filler).ToArray()) // 덱 위 후보용 손패 순회
            { // 후보 위치 조정 시작
                Assert.IsTrue(fight.Deck.DiscardCard(candidate)); // 손패 후보를 버린 더미로 이동
            } // 후보 위치 조정 종료
            var candidates = fight.Deck.PrepareDrawChoices(4); // 비교용 후보 네 장의 순서 보관
            Assert.AreEqual(4, candidates.Count); // 기본 한도와 유물 한도를 모두 채운 후보 수
            yield return Play(fight, arrange); // 정상 손패 입력으로 정리 후보 표시
            SelectThirdChoice(fight); // 책갈피로 추가된 세 번째 후보 선택
            Assert.AreSame(candidates[2], fight.Deck.PeekDrawTop(1)[0]); // 추가 후보의 실제 덱 위 배치 확인
            Assert.AreEqual(1, fight.Deck.HandCount); // 순서 정리에 따른 추가 드로우 없음 확인
            var extra = fight.Deck.PeekDrawTop(3)[2]; // 정리 이후 새 세 번째 후보 조회
            yield return Play(fight, choose); // 정상 손패 입력으로 선택 뽑기 후보 표시
            SelectThirdChoice(fight); // 책갈피로 추가된 세 번째 카드 드로우
            Assert.AreSame(extra, fight.Deck.Hand.Single()); // 후보가 늘어나도 실제로 뽑은 카드는 한 장
            Assert.IsTrue(fight.Deck.IsStateValid()); // 재배치와 뽑기 뒤 카드 위치 보존
        } // 덱 위 후보 검증 종료
        finally // 임시 원본 해제
        { // 확인 검사 정리 시작
            Object.Destroy(arrange); // 덱 순서 정리 카드 원본 제거
            Object.Destroy(choose); // 선택 드로우 카드 원본 제거
            Object.Destroy(filler); // 확인 후보 카드 원본 제거
        } // 확인 검사 정리 종료
    } // 확인 후보 조작 검사 종료

    private static void SelectThirdChoice(Fight fight) // 실제 확인 화면의 세 번째 후보 입력
    { // 추가 후보 선택 시작
        Transform panel = fight.Hand.transform.parent.Find("DrawChoicePanel"); // 손패와 같은 화면의 후보 창 조회
        Assert.IsNotNull(panel); // 실제 선택 창 생성 확인
        Assert.IsNull(panel.Find("Choice_3")); // 원래 두 장과 추가 한 장까지 표시 확인
        var extra = panel.Find("Choice_2"); // 세 번째 후보 버튼 조회
        Assert.IsNotNull(extra); // 유물로 늘어난 세 번째 후보 생성 확인
        panel.Find("NextPage").GetComponent<Button>().onClick.Invoke(); // 세 번째 후보가 보이는 다음 페이지 이동
        Assert.IsTrue(extra.gameObject.activeSelf); // 화면에서 추가 후보가 실제 보이는 상태 확인
        extra.GetComponent<Button>().onClick.Invoke(); // 추가 후보 확정 입력
    } // 추가 후보 선택 종료

    private static IEnumerator Play(Fight fight, CardData card, BattleUnitView target = null) // 실제 손패 입력과 카드 이동 검증
    { // 사용 검사 위임 시작
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, target, fight.Runner, fight.Deck, fight.Points); // 기존 카드 사용 검사 재사용
    } // 사용 검사 위임 종료

    private static CardData MakeCard(string id, CardEffectType effect, CardTargetType target, int value, bool retain = false) // 수치와 유지 여부를 고정한 검사 카드 생성
    { // 검사 카드 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 임시 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)CardType.Shield},\"targetType\":{(int)target},\"apCost\":1,\"effectType\":{(int)effect},\"effectValue\":{value},\"damageType\":{(int)BattleDamageType.Physical},\"retainInHand\":{(retain ? "true" : "false")}}}", card); // 실제 카드 데이터의 직렬화 필드 설정
        return card; // 전투 덱에서 사용할 원본 반환
    } // 검사 카드 생성 종료

    private static void Hold(params RelicEffectType[] types) // 실제 카탈로그 유물을 현재 회차에 지급
    { // 보유 유물 준비 시작
        var inventory = RelicRunManager.EnsureInstance().Inventory; // 실제 회차의 유물 인벤토리
        var relics = Resources.LoadAll<RelicData>("Relics"); // 저장 카탈로그에서 쓰는 원본 유물 목록
        foreach (var type in types) // 이번 전투에서 발동할 유물 종류 순회
        { // 유물 지급 시작
            var relic = relics.Single(item => item.EffectType == type); // 종류가 일치하는 실제 유물 원본
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic), relic.DisplayName); // 정상적인 인벤토리 등록 확인
        } // 유물 지급 종료
    } // 보유 유물 준비 종료
} // 전투 유물 검사 종료
