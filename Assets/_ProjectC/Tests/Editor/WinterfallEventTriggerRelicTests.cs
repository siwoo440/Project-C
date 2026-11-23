using System; // 이벤트 연결 해제 인터페이스
using System.Collections; // 실제 전투 카드와 창 전환 대기
using System.Linq; // 카드 원본과 실제 손패 보기 검색
using System.Reflection; // 게임의 내부 예약 드로우 진입점 연결
using NUnit.Framework; // 유물 발동과 체력 및 행동력 확인
using UnityEngine; // 유물 원본과 전투 선택 창 조회
using UnityEngine.TestTools; // 실제 플레이 모드 검사 규칙
using UnityEngine.UI; // 실제 선택 창의 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 카드 연출과 전투 상태의 기존 검사 환경
using Object = UnityEngine.Object; // 실제 화면과 임시 원본 정리
using Random = UnityEngine.Random; // 유물 확률 판정의 결정적 상태 확인

public sealed class WinterfallEventTriggerRelicTests // 상태와 정신력 공용 신호의 실제 유물 검사
{ // 유물 신호 검사 시작
    [UnityTest] // 실제 확인 선택과 적 제어 발동의 드로우 분리 검사
    public IEnumerator ControlDrawWaitsForBothChoiceWindowsAndSkipsNonControl() // 사용 중 손패와 두 실제 선택의 카드 보존 확인
    { // 적 제어 선택 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 입력과 연출의 플레이 환경 시작
        yield return CheckControlDraw(); // 실제 상태 신호와 선택 뒤 드로우 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 적 제어 선택 검사 종료

    private static IEnumerator CheckControlDraw() // 적 제어 신호와 확인 카드 선택의 안전한 정산 검사
    { // 밀랍인장 전투 준비 시작
        using var fight = new Fight("StarterCards/FoolChoice", "THRUST", "KNEE_SLASH", "POMMEL_STRIKE", "INTERCEPT_SLASH", "CHECK_SLASH", "BLOOD_EDGE"); // 실제 확인 카드와 일반 덱의 비교 후보 준비
        using var relics = new RelicEvents(fight, "AbyssWaxSeal", RelicEffectType.ActionPreventingStatusDraw, RelicTriggerType.StatusApplied, 1, 100); // 실제 전투 신호와 어비스 밀랍인장 연결
        var source = Resources.Load<CardData>("StarterCards/FoolChoice"); // 덱 상단 선택의 실제 시작 카드
        foreach (var card in fight.Deck.Hand.Where(card => card.SourceData != source).ToArray()) // 신호에 반응해 가져올 카드 확인
        { // 뽑을 카드 확보 시작
            Assert.IsTrue(fight.Deck.DiscardCard(card)); // 일반 손패를 실제 버린 더미로 이동
        } // 뽑을 카드 확보 종료
        var actor = fight.Allies[0]; // 원본 카드와 행동 불가를 적용할 실제 아군
        var enemy = fight.Enemies[0]; // 직접 제어와 제어 제외 상태의 실제 적
        int initial = fight.Deck.HandCount; // 상태 조건 검증 전 남은 실제 손패 수
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 1, 2, 1, actor); // 실제 실명은 행동 불가 제외
        enemy.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 2, 1, actor); // 실제 공포는 행동 불가 제외
        enemy.ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1); // 발생 아군 없는 제어 제외
        actor.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 1, 1, fight.Allies[1]); // 아군 대상 제어 제외
        fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 기존 상태 면역 준비
        fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1, actor); // 실제 면역에 막힌 제어 제외
        Assert.AreEqual(initial, fight.Deck.HandCount); // 제외된 전체 제어 후보의 드로우 미발생 확인

        int handInsideCardUse = -1; // 사용 카드 효과가 처리 중일 때의 손패 기록
        fight.Controller.CardUsed += (card, targets) => // 카드 해결 범위 안에서 실제 상태 부여 신호 발생
        { // 처리 중 제어 효과 시작
            enemy.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 2, 1, actor); // 선택형 카드의 해결 도중 발생하는 실제 제어 신호
            handInsideCardUse = fight.Deck.HandCount; // 완료 전 상태 드로우가 손패를 건드리는지 확인
        }; // 처리 중 제어 효과 종료
        var view = fight.Hand.GetComponentsInChildren<BattleCardView>().Single(item => item.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(item.RuntimeCard)); // 실제 확인 카드의 활성 손패 보기
        view.OnPointerClick(null); // 실제 카드 입력으로 확인 선택 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 선택 창이 열리기까지 실제 프레임 대기 한도
        while (Object.FindFirstObjectByType<BattleDrawChoiceView>() == null && Time.realtimeSinceStartup < deadline) // 첫 후보 창 표시 대기
        { // 실제 게임 프레임 대기 시작
            yield return null; // 선택 UI가 생성되는 다음 프레임 진행
        } // 실제 게임 프레임 대기 종료
        var choice = Object.FindFirstObjectByType<BattleDrawChoiceView>(); // 실제 덱 상단 선택 창
        Assert.IsNotNull(choice); // 기존 선택 카드의 정상 UI 열림 확인
        Assert.AreEqual(initial, handInsideCardUse); // 사용 도중 보너스 드로우 지연 확인
        Assert.AreEqual(initial - 1, fight.Deck.HandCount); // 첫 선택 전 사용 카드 한 장만 이동한 상태
        choice.transform.Find("Choice_0").GetComponent<Button>().onClick.Invoke(); // 첫 후보의 실제 선택 입력
        var remaining = Object.FindFirstObjectByType<BattleRemainingCardChoiceView>(); // 바보의 덱 위 남은 카드 처리 화면
        Assert.IsNotNull(remaining); // 기존 패시브 후보를 보너스가 소비하지 않는지 확인
        Assert.AreEqual(initial, fight.Deck.HandCount); // 두 번째 선택 전 기본 한 장만 손패 진입
        remaining.transform.Find("KeepRemaining").GetComponent<Button>().onClick.Invoke(); // 덱 위 보존의 실제 최종 입력
        Assert.AreEqual(initial + 1, fight.Deck.HandCount); // 두 선택 뒤 제어 보너스가 한 번만 도착하는지 확인
        fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1, actor); // 선택 없는 별개의 실제 기절 성공
        Assert.AreEqual(initial + 2, fight.Deck.HandCount); // 같은 턴의 다른 제어당 실제 한 장 지급 확인
        fight.Deck.DrawCards(fight.Deck.MaxHandSize); // 가능한 모든 손패를 게임의 원래 규칙으로 보충
        int fullHand = fight.Deck.HandCount; // 보너스 이전 실제 채워진 손패 수
        int discard = fight.Deck.DiscardPile.Count; // 초과 드로우가 버린 더미에 쌓이는지 비교할 수
        int draw = fight.Deck.DrawPile.Count; // 초과 드로우가 덱 순서를 소모하는지 비교할 수
        fight.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Stun, 1, 1, 1, actor); // 추가 가능한 카드나 공간이 없는 제어 신호
        Assert.AreEqual(fullHand, fight.Deck.HandCount); // 가득 찬 손패나 빈 덱에서 손패 불변 확인
        Assert.AreEqual(discard, fight.Deck.DiscardPile.Count); // 지급 불가 상태의 버림 카드 불변 확인
        Assert.AreEqual(draw, fight.Deck.DrawPile.Count); // 지급 불가 상태의 덱 카드 불변 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 선택과 드로우 뒤 겹치지 않는 실제 카드 위치 확인
    } // 밀랍인장 전투 검사 종료

    [UnityTest] // 적 디버프 성공 횟수의 전투 단위 누적 검사
    public IEnumerator FifthEnemyDebuffHealsOnlySurvivingAlliesAcrossTurns() // 면역과 상태 틱 제외 및 다섯 회마다 반복 확인
    { // 디버프 시점 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 상태의 플레이 모드 진입
        CheckDebuffGroups(); // 실제 상태 및 체력으로 두 회복 주기 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 디버프 시점 검사 종료

    private static void CheckDebuffGroups() // 적 디버프 횟수 비교와 살아 있는 아군만 회복하는지 확인
    { // 잔뿌리 전투 준비 시작
        using var fight = new Fight("THRUST"); // 실제 아군 명단과 적 유닛 배열을 가진 전투
        using var relics = new RelicEvents(fight, "VoidRootlets", RelicEffectType.FifthEnemyDebuffHeal, RelicTriggerType.StatusApplied, 3, 100); // 실제 공허의 잔뿌리와 공용 상태 이벤트 연결
        var actor = fight.Allies[0]; // 실제 디버프를 가할 아군
        var ally = fight.Allies[1]; // 회복을 검증할 부상 아군
        var fallen = fight.Allies[2]; // 전체 회복에서 제외할 전투불능 아군
        var enemy = fight.Enemies[0]; // 연속 성공과 중첩을 받을 첫 적
        var immune = fight.Enemies[1]; // 실제 상태 면역과 실패를 확인할 적
        ally.TakeDamage(12, BattleDamageType.None); // 기본 회복 두 번을 확인할 실제 체력 손실
        fallen.TakeDamage(10_000, BattleDamageType.None); // 실제 전투불능 대상과 일반 생존 회복 분리
        int allyStart = ally.CurrentHealth; // 회복 전 부상 아군의 실제 체력
        enemy.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 3, 1, actor); // 적에게 적용됐어도 이로운 상태 제외
        actor.ApplyStatusEffect(BattleStatusEffectType.Blind, 1, 3, 1, enemy); // 디버프여도 아군 대상이므로 제외
        immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 실제 상태 면역 준비
        Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, immune.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 5, actor)); // 막힌 실제 상태 이벤트 제외 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, enemy.ApplyStatusEffect(BattleStatusEffectType.None, 1, 3, 1, actor)); // 유효하지 않은 상태 요청 제외 확인
        enemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 5, actor); // 첫 성공 디버프
        Assert.AreEqual(BattleStatusEffectApplyResult.Stacked, enemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 5, actor)); // 같은 적에 실제 성공한 두 번째 적용과 중첩
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 1, 3, 1, actor); // 세 번째 실제 상태 적용
        Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 아군과 적 턴의 실제 전환으로 횟수 보존 검사
        enemy.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 3, 1, actor); // 다음 라운드의 네 번째 적용
        Assert.AreEqual(allyStart, ally.CurrentHealth); // 네 번째까지 기존 실패와 틱을 합치지 않는지 확인
        enemy.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 1, 3, 1); // 발생 유닛이 없어도 적이 실제 받은 다섯 번째 약화
        Assert.AreEqual(allyStart + 3, ally.CurrentHealth); // 다섯 번째 적 디버프에서 실제 고정 체력 회복
        Assert.AreEqual(0, fallen.CurrentHealth); // 일반 회복으로 전투불능 대상 부활 차단
        for (int count = 1; count <= 5; count++) // 다음 다섯 번의 실제 디버프 적용 순회
        { // 두 번째 회복 주기 시작
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 5, actor); // 서로 다른 적에 적용한 실제 초기와 중첩 이벤트
            Assert.AreEqual(allyStart + (count == 5 ? 6 : 3), ally.CurrentHealth); // 다음 다섯 번째에서만 한 번 더 실제 회복 확인
        } // 두 번째 회복 주기 종료
    } // 잔뿌리 전투 검사 종료

    [UnityTest] // 실제 정신력 원인과 지급 확률의 경계 검사
    public IEnumerator ChimeUsesActualAttributedEnemyMentalLossOnly() // 원인 미확인 상태와 확률 실패 및 행동력 상한 확인
    { // 방울목걸이 검사 시작
        yield return new EnterPlayMode(); // 실제 정신력 이벤트의 플레이 환경 시작
        yield return CheckMentalLoss(); // 실제 카드와 경계 난수로 행동력 변동 확인
        yield return new ExitPlayMode(); // 독립 플레이 검사 종료
    } // 방울목걸이 검사 종료

    private static IEnumerator CheckMentalLoss() // 정신력 손실의 실제 출처와 성공·실패 추첨 확인
    { // 실제 방울목걸이 전투 준비 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 적 정신력을 명시적으로 낮추는 검사 카드 원본
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"DAY256_TEST_MENTAL\",\"displayName\":\"DAY256_TEST_MENTAL\",\"targetType\":{(int)CardTargetType.SingleEnemy},\"apCost\":1,\"effectType\":{(int)CardEffectType.ChangeMental},\"effectValue\":1,\"mentalChangeValue\":-5}}", card); // 실제 카드 사용 경로에서 지불과 손실 수치 준비
        var savedRandom = Random.state; // 검사 시작 전 전역 난수 상태 보존
        try // 전투 덱과 난수 정리 보장
        { // 정신력 출처의 실제 검사 시작
            using var fight = new Fight(new[] { card }); // 실제 사용할 정신력 카드 한 장으로 전투 구성
            using var relics = new RelicEvents(fight, "CorrosionChimeNecklace", RelicEffectType.EnemyMentalLossActionPoint, RelicTriggerType.MentalChanged, 1, 25); // 실제 침식 방울목걸이와 정신력 시점 연결
            int successSeed = SeedFor(24); // 25퍼센트의 마지막 성공 난수 시드
            int failSeed = SeedFor(25); // 25퍼센트의 첫 실패 난수 시드
            var actor = fight.Allies[0]; // 실제 카드 시전자와 적 정신력 손실의 발생자
            var enemy = fight.Enemies[0]; // 명시된 정신력 피해를 받을 실제 적
            enemy.ChangeMental(70 - enemy.CurrentMental, BattleMentalChangeReason.RelicEffect); // 출처 없는 전투 변화로 적의 정신력을 실제 70으로 설정
            Assert.AreEqual(70, enemy.CurrentMental); // 연속 손실을 받을 실제 적의 기준 상태 확인
            Assert.IsTrue(fight.Points.Spend(5)); // 행동력 증가 여유 확보
            int points = fight.Points.CurrentActionPoints; // 비교할 기본 공용 행동력
            Random.InitState(successSeed); // 원인 없는 효과가 함부로 소비하면 검출할 성공 난수 준비
            var beforeRandom = Random.state; // 비적격 이벤트 전 실제 난수 상태
            enemy.ChangeMental(-2, BattleMentalChangeReason.RelicEffect); // 출처 없는 전투 턴의 유물 정신력 비용 제외
            actor.ChangeMental(-2, BattleMentalChangeReason.CardEffect, actor); // 아군 자신의 명시적 비용 제외
            enemy.TakeDamage(1, BattleDamageType.None, actor); // 일반 체력 피격의 자동 정신력 비용 제외
            Assert.AreEqual(beforeRandom, Random.state); // 비적격 정신력 손실로 확률 미소비 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 비적격 손실로 행동력 미지급 확인
            Random.InitState(failSeed); // 실패 경계에서 실제 적의 정신력 손실 시드 준비
            Assert.AreEqual(-2, enemy.ChangeMental(-2, BattleMentalChangeReason.CardEffect, actor).AppliedDelta); // 실패해도 카드의 실제 정신력 손실 적용 확인
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 실패 경계에서 행동력 미지급 확인
            Random.InitState(successSeed); // 적격 교란의 실제 성공 난수 상태 준비
            enemy.ApplyStatusEffect(BattleStatusEffectType.Disrupt, 1, 2, 1, actor); // 아군이 부여한 적의 실제 정신력 교란
            Assert.AreEqual(points + 1, fight.Points.CurrentActionPoints); // 실제 교란 한 번에 공용 행동력 한 점 지급 확인

            fight.Controller.CardUsed += (used, targets) => Random.InitState(successSeed); // 카드 비용 처리 이후의 실제 효과에 성공 경계 적용
            points = fight.Points.CurrentActionPoints; // 카드 사용 전 지불과 환급의 비교 기준
            yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 실제 적 대상 카드 소유자의 정신력 피해와 소비 후 환급 검증
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 행동력 한 점을 지불하고 한 점만 환급 확인

            fight.Points.Restore(); // 상한에 막힌 효과가 무작위 수를 소비하지 않는지 확인할 행동력
            Random.InitState(successSeed); // 상한 조건 전의 성공 난수 상태 준비
            beforeRandom = Random.state; // 상한에서의 실제 난수 기준
            enemy.ChangeMental(-1, BattleMentalChangeReason.CardEffect, actor); // 명시적 정신력 손실이 있어도 추가 행동력 공간 없음
            Assert.AreEqual(beforeRandom, Random.state); // 상한에서 확률 미소비 확인
            Assert.AreEqual(fight.Points.MaxActionPoints, fight.Points.CurrentActionPoints); // 공용 행동력 상한 유지 확인
            fight.Points.Spend(1); // 손실이 없는 요청의 판정을 분리할 공간 확보
            enemy.ChangeMental(-enemy.CurrentMental, BattleMentalChangeReason.RelicEffect); // 아군 출처 없는 실제 변화로 적의 정신력 소진
            Assert.AreEqual(0, enemy.CurrentMental); // 추가 정신력 손실이 불가능한 실제 상태 확인
            Assert.AreEqual(0, enemy.ChangeMental(-2, BattleMentalChangeReason.CardEffect, actor).AppliedDelta); // 실제 손실이 없음을 확인
            Assert.AreEqual(beforeRandom, Random.state); // 정신력 영에서 확률 미소비 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 적 턴으로 넘어가 실제 시점 조건 비교
            points = fight.Points.CurrentActionPoints; // 적 턴의 공용 행동력 기준
            enemy.ChangeMental(50 - enemy.CurrentMental, BattleMentalChangeReason.RelicEffect); // 다른 실제 턴 손실 비교에 쓸 적 정신력 복구
            Assert.AreEqual(50, enemy.CurrentMental); // 적 턴 비교의 실제 정신력 기준 확인
            beforeRandom = Random.state; // 적 턴의 비교 난수 상태
            enemy.ChangeMental(-2, BattleMentalChangeReason.CardEffect, actor); // 같은 계열의 변화도 적 턴에는 행동력 지급 제외
            Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 적 턴의 행동력 불변 확인
            Assert.AreEqual(beforeRandom, Random.state); // 적 턴의 확률 미소비 확인
        } // 정신력 출처의 실제 검사 종료
        finally // 전역 상태와 원본 정리
        { // 독립 검사 상태 복구 시작
            Random.state = savedRandom; // 다른 검사를 위해 전역 난수 상태 복원
            Object.Destroy(card); // 임시 정신력 피해 카드 원본 해제
        } // 독립 검사 상태 복구 종료
    } // 실제 방울목걸이 전투 검사 종료

    private static int SeedFor(int roll) // 실제 무작위 정수의 정확한 경계 선택
    { // 경계 난수 원본 검색 시작
        for (int seed = 1; seed < 10_000; seed++) // 예측 가능한 실제 Unity 난수 시드 탐색
        { // 후보 시드 확인 시작
            Random.InitState(seed); // 각 후보의 범위 제어
            if (Random.Range(0, 100) == roll) // 24와 25의 정확한 경계 일치 검증
            { // 경계 시드 반환 시작
                return seed; // 실제 확률 체크에 사용할 Unity 시드
            } // 경계 시드 반환 종료
        } // 후보 시드 확인 종료
        throw new InvalidOperationException("검사 난수 경계 시드 없음"); // 경계 난수 설정의 명확한 실패 알림
    } // 경계 난수 원본 검색 종료

    private sealed class RelicEvents : IDisposable // 실제 전투의 유물과 공용 이벤트 연결
    { // 독립 유물 이벤트 구성 시작
        private readonly BattleEventDispatcher dispatcher = new BattleEventDispatcher(); // 실제 전투 공용 이벤트 발행기
        private readonly BattleEventController bridge; // 실제 유닛 신호를 공통 시점에 전달하는 연결
        private readonly BattleRelicEffectController effects; // 등록된 실제 유물의 발동 관리자

        public RelicEvents(Fight fight, string resource, RelicEffectType effect, RelicTriggerType trigger, int amount, int chance) // 검사할 실제 원본 유물과 발동 조건 검증
        { // 전투와 보유 유물 연결 시작
            var relic = Resources.Load<RelicData>("Relics/" + resource); // 게임에서 지급되는 시트 유물 원본
            Assert.IsNotNull(relic); // 실제 유물 등록 확인
            Assert.AreEqual(effect, relic.EffectType); // 시트 효과와 실행 효과 연결 확인
            Assert.AreEqual(trigger, relic.TriggerType); // 시트 발동 시점과 공용 이벤트 연결 확인
            Assert.AreEqual(amount, relic.EffectValue); // 시트에 명시된 실제 적용 수치 확인
            Assert.AreEqual(chance, relic.TriggerChancePercent); // 시트에 명시된 실제 확률 확인
            var wallet = new RelicGoldRuntime(); // 독립 유물 획득의 골드 지갑
            var inventory = new RelicInventoryRuntime(wallet); // 사본 대신 실제 게임의 독립 유물 보관함
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 정상 획득 규칙으로 실제 유물 보유 준비
            var method = typeof(BattleCardActionController).GetMethod("TryDrawEventRelicCards", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드 행동 관리자의 예약 드로우 연결
            Assert.IsNotNull(method); // 게임 부트스트랩과 같은 예약 처리 경로 존재 확인
            var deferredDraw = (Func<int, bool>)method.CreateDelegate(typeof(Func<int, bool>), fight.Controller); // 별도 수동 드로우 없는 실제 카드 선택 대기 경로
            effects = new BattleRelicEffectController(dispatcher, inventory, wallet, fight.Allies, fight.Enemies, fight.Deck, fight.Points, deferredDraw); // 정상 전투 유물 실행 관리자 생성
            bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 실제 적용된 상태와 손실을 공통 유물 실행에 연결
        } // 전투와 보유 유물 연결 종료

        public void Dispose() // 독립 전투 유물과 공통 이벤트 구독 정리
        { // 공통 상태 구독 해제 시작
            bridge.Dispose(); // 유닛과 실제 전투 관리자 신호 해제
            effects.Dispose(); // 실제 획득 및 전투 유물 구독 해제
            dispatcher.Dispose(); // 공용 이벤트 발행기의 남은 구독 정리
        } // 공통 상태 구독 해제 종료
    } // 독립 유물 이벤트 구성 종료
} // 유물 신호 검사 종료
