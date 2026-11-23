using System.Collections; // 플레이 모드와 카드 연출 대기
using System.Linq; // 실제 손패와 상태 결과 조회
using NUnit.Framework; // 전투 유물 결과 검증
using UnityEngine; // 임시 카드와 유물 원본 준비
using UnityEngine.TestTools; // 플레이라운드 검사 지원
using UnityEngine.UI; // 실제 선택 화면의 입력 버튼
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 화면과 런타임

public sealed class WinterfallResolvedBattleRelicTests // 종료 정화와 카드 최종 정산 유물 검사
{ // 전투 정산 검사 시작
    [UnityTest] // 손수건의 실제 결과 발행과 활성 약화 하나 제거 검사
    public IEnumerator HandkerchiefCleansesOneRemovableStatusOnlyOnTheActualBattleEnd() // 보호 정신 상태와 두 생존자 사이의 한 번 정화 확인
    { // 순백 손수건 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 이벤트 환경 진입
        yield return HandkerchiefCleansesOneRemovableStatusOnlyOnTheActualBattleEndInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 종료
    } // 순백 손수건 검사 종료

    private IEnumerator HandkerchiefCleansesOneRemovableStatusOnlyOnTheActualBattleEndInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Assert.AreEqual(30, Hold("WhiteHandkerchief").TriggerChancePercent); // 실제 순백 손수건의 발동 확률 확인
        using (var fight = new Fight("BALANCE_GUARD")) // 실제 일반 전투와 도주 가능 경로 준비
        using (var dispatcher = new BattleEventDispatcher()) // 실제 전투 결과의 공용 발행기
        using (var effects = new BattleRelicEffectController(dispatcher, RelicRunManager.Instance.Inventory, RelicRunManager.Instance.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points)) // 실제 순백 손수건의 전투 종료 연결
        using (var bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 전투 종료 결과 발행 연결
        { // 생존 아군의 상태 검사 시작
            var first = fight.Allies[0]; // 보호 정신 상태와 일반 약화 대상
            var second = fight.Allies[1]; // 별도 일반 약화 대상
            first.ChangeMental(-100, BattleMentalChangeReason.PersistentState); // 제거할 수 없는 실제 붕괴 상태 준비
            first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1); // 첫 아군의 제거 가능한 출혈 부여
            second.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 2, 1); // 둘째 아군의 제거 가능한 중독 부여
            int CountRemovable() // 두 일반 약화의 실제 남은 수 계산
            { // 일반 약화 내용 조회 시작
                return (first.GetStatusEffect(BattleStatusEffectType.Bleed) == null ? 0 : 1) + (second.GetStatusEffect(BattleStatusEffectType.Poison) == null ? 0 : 1); // 보호 상태를 뺀 실제 후보 개수
            } // 일반 약화 내용 조회 종료
            Assert.AreEqual(2, CountRemovable()); // 실제 전투 중 약화 조기 해제 없음 확인
            int seed = Enumerable.Range(0, 256).First(value => RollsBelow(value, 30)); // 순백 손수건의 성공 확률을 보장하는 유니티 시드
            Random.InitState(seed); // 실제 종료 확률의 다음 난수 초기화
            Assert.IsTrue(fight.Turns.TryEscape()); // 일반 전투의 실제 종료 결과 발행 확인
            Assert.AreEqual(1, CountRemovable()); // 전체 아군에 걸친 균등 후보 중 한 개만 정화 확인
            Assert.IsNotNull(first.GetStatusEffect(BattleStatusEffectType.Collapse)); // 보호 정신 상태의 기존 수명 유지 확인
            Random.InitState(seed); // 중복 결과도 성공할 난수로 다시 준비
            dispatcher.Publish(new BattleEventContext(BattleEventType.BattleEnded, fight.Turns.CurrentRound, fight.Turns.CurrentPhase, result: BattleResult.Escape)); // 중복된 실제 결과 형식의 대조 신호 발행
            Assert.AreEqual(1, CountRemovable()); // 같은 전투에서 다음 추첨과 추가 정화 없음 확인
        } // 생존 아군의 상태 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 스킬 선택의 최종 처리와 실제 방어 효과의 카드 유물 검사
    public IEnumerator InkWaitsForTheChoiceAndGlovesRequireAnActualDefense() // 독립 펜타클 지급 및 카드 소유자 방어도 확인
    { // 스킬과 방어 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 화면과 유물 초기화
        yield return InkWaitsForTheChoiceAndGlovesRequireAnActualDefenseInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 카드 화면과 검사 유물 종료
    } // 스킬과 방어 검사 종료

    private IEnumerator InkWaitsForTheChoiceAndGlovesRequireAnActualDefenseInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Assert.AreEqual(20, Hold("DriedInkBottle").TriggerChancePercent); // 실제 마른 잉크병의 확률 확인
        Hold("WornCalculator"); // 기존 펜타클 드로우 보유
        Hold("WornLeatherGloves"); // 닳은 가죽 장갑 보유
        var choice = MakeCard(CardEffectType.ChooseDraw, CardType.Pentacle); // 실제 카드 선택이 필요한 스킬 카드
        var guard = MakeCard(CardEffectType.GainBlock, CardType.Shield, CardTargetType.AllAllies); // 여러 아군에게 방어도를 주는 기본 방패 카드
        JsonUtility.FromJsonOverwrite("{\"grantsBlockToEachAlly\":true}", guard); // 아군별 방어 횟수 비교용 전체 적용 설정
        var attack = MakeCard(CardEffectType.Damage, CardType.Wand, CardTargetType.SingleEnemy); // 방어 효과 없는 순수 공격 카드
        var filler = MakeCard(CardEffectType.GainBlock, CardType.Shield); // 남은 실제 드로우 공간을 만들 대조 카드
        try // 임시 카드 원본 정리 보장
        { // 사용 정산의 실제 전투 시작
            using var fight = new Fight(new[] { choice, guard, attack, filler, filler, filler }); // 사용과 선택 및 추가 보너스용 손패 구성
            foreach (var item in fight.Deck.Hand.Where(card => card.SourceData == filler).ToArray()) // 보너스로 가져올 카드 세 장의 현재 위치 조회
            { // 빈 손패와 뽑을 카드 준비 시작
                Assert.IsTrue(fight.Deck.DiscardCard(item)); // 선택과 별도 보너스가 뽑을 버린 더미 구성
            } // 빈 손패와 뽑을 카드 준비 종료
            yield return WinterfallCommonCardExpansionTests.Play(choice, fight.Hand, null, fight.Runner, fight.Deck, fight.Points); // 실제 스킬 입력과 사용 카드 이동
            Assert.AreEqual(2, fight.Deck.HandCount); // 정상 선택 전에는 어느 예약 유물도 지급하지 않는지 확인
            var choiceView = fight.Hand.GetComponentInParent<Canvas>().GetComponentInChildren<BattleDrawChoiceView>(); // 실제로 띄운 덱 위 후보 화면 조회
            Assert.IsNotNull(choiceView); // 정상 카드 선택을 기다리는 실제 화면 확인
            int seed = Enumerable.Range(0, 256).First(value => RollsBelow(value, 20)); // 잉크병의 실제 이십 퍼센트 이내 값을 만드는 시드
            Random.InitState(seed); // 마지막 선택 후 실제 확률 판정 준비
            choiceView.GetComponentsInChildren<Button>().First(button => button.name == "Choice_0").onClick.Invoke(); // 실제 첫 후보 선택 실행
            Assert.AreEqual(5, fight.Deck.HandCount); // 선택 카드와 기존 펜타클 및 잉크병의 각각 한 장 확인
            Assert.AreEqual(0, fight.Allies[0].CurrentBlock); // 방어 효과 없는 선택 스킬의 장갑 미발동 확인
            yield return WinterfallCommonCardExpansionTests.Play(guard, fight.Hand, null, fight.Runner, fight.Deck, fight.Points); // 실제 방패 카드 사용 및 이동
            Assert.AreEqual(9, fight.Allies[0].CurrentBlock); // 기본 방어도 일곱 점과 장갑의 추가 두 점 확인
            Assert.IsTrue(fight.Allies.Skip(1).All(ally => ally.CurrentBlock == 7)); // 여러 방어 대상이어도 장갑은 실제 소유자에게만 한 번 확인
            yield return WinterfallCommonCardExpansionTests.Play(attack, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 순수 공격 카드의 실제 정산
            Assert.AreEqual(9, fight.Allies[0].CurrentBlock); // 단일 직접 공격의 장갑 미발동 확인
        } // 사용 정산의 실제 전투 종료
        finally // 검사에서 만든 전체 카드 정리
        { // 검사 원본 제거 시작
            Object.Destroy(choice); // 선택형 스킬 원본 제거
            Object.Destroy(guard); // 방패 카드 원본 제거
            Object.Destroy(attack); // 순수 공격 원본 제거
            Object.Destroy(filler); // 대조 카드 원본 제거
        } // 검사 원본 제거 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 직접 다중 마법 명중의 세 가지 표식 보너스 검사
    public IEnumerator SilverNailAddsTwoPerMarkedHitAlongsideChalkAndPin() // 같은 적의 두 표식에도 은못의 타격당 한 번 적용 확인
    { // 은못 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 카드 입력 준비
        yield return SilverNailAddsTwoPerMarkedHitAlongsideChalkAndPinInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투 도메인 종료
    } // 은못 검사 종료

    private IEnumerator SilverNailAddsTwoPerMarkedHitAlongsideChalkAndPinInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("BentSilverNail"); // 구부러진 은못 보유
        Hold("MarkingChalk"); // 표식용 분필의 기존 피해 증가 보유
        Hold("CrudeMarkPin"); // 투박한 표식핀의 기존 마법 피해 증가 보유
        var magic = MakeCard(CardEffectType.Damage, CardType.Wand, CardTargetType.SingleEnemy, BattleDamageType.Magical, 2); // 원본 피해 일곱의 두 마법 타격 카드
        try // 독립 공격 카드 원본 정리 보장
        { // 표식 카드 실제 사용 시작
            using var fight = new Fight(new[] { magic, magic }); // 두 서로 다른 실제 피해 대상을 확인할 시작 손패
            var marked = fight.Enemies[0]; // 둘의 표식을 같은 적에게 적용할 대상
            var clear = fight.Enemies[1]; // 같은 카드의 표식 없는 대조 대상
            marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 공명 표식 적용
            marked.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, 1); // 전차 표식 추가
            yield return WinterfallCommonCardExpansionTests.Play(magic, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 두 표식이 있는 적을 실제로 두 번 타격
            Assert.AreEqual(marked.MaxHealth - 24, marked.CurrentHealth); // 일곱 점과 분필 둘 및 핀 하나와 은못 둘을 매 타격에 한 번씩 적용 확인
            yield return WinterfallCommonCardExpansionTests.Play(magic, fight.Hand, fight.EnemyViews[1], fight.Runner, fight.Deck, fight.Points); // 표식 없는 적의 같은 두 마법 타격
            Assert.AreEqual(clear.MaxHealth - 14, clear.CurrentHealth); // 표식 없는 적에 기본 피해만 적용 확인
        } // 표식 카드 실제 사용 종료
        finally // 임시 공격 카드 원본 정리
        { // 사용 원본 해제 시작
            Object.Destroy(magic); // 마법 공격 검사 원본 제거
        } // 사용 원본 해제 종료
    } // 플레이 모드 본문 종료

    private static bool RollsBelow(int seed, int chance) // 확률 효과의 다음 유니티 난수 값 확인
    { // 확정 난수 조회 시작
        Random.InitState(seed); // 비교할 게임 난수 시드 저장
        return Random.Range(0, 100) < chance; // 다음 정수 값이 발동 범위에 있는지 확인
    } // 확정 난수 조회 종료

    private static RelicData Hold(string name) // 실제 등록한 유물의 회차 획득
    { // 검사 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 유물 게임 원본 조회
        Assert.IsNotNull(relic); // 실제 원본과 상점 및 회차 처리 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 소지 목록으로의 원본 지급 확인
        return relic; // 실제 원본 확률과 대상 및 효과 검증에 사용
    } // 검사 유물 획득 종료

    private static CardData MakeCard(CardEffectType effect, CardType type, CardTargetType target = CardTargetType.Self, BattleDamageType damage = BattleDamageType.Physical, int hits = 1) // 원본 피해와 방어 효과가 고정된 검사 카드 생성
    { // 검사 카드 구성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 빈 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardType\":{(int)type},\"effectType\":{(int)effect},\"targetType\":{(int)target},\"effectValue\":7,\"damageType\":{(int)damage},\"damageHitCount\":{hits}}}", card); // 실제 대상과 카드 효과 및 타격 수 지정
        return card; // 실제 손패에서 사용할 원본 반환
    } // 검사 카드 구성 종료
} // 전투 정산 검사 종료
