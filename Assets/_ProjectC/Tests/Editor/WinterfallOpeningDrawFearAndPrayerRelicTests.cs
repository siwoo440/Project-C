using System; // 검사 전투 자원 해제 계약
using System.Collections; // 플레이 모드 전환 대기
using System.Linq; // 실제 유물과 적 및 시작 덱 카드 조회
using System.Reflection; // 다음 아군 턴 예약의 실제 연결 사용
using NUnit.Framework; // 전투 유물의 실제 결과 대조
using UnityEngine; // 독립 카드와 전투 원본 생성
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 공용 전투 이벤트의 검사 화면
using Object = UnityEngine.Object; // 임시 게임 원본 정리

public sealed class WinterfallOpeningDrawFearAndPrayerRelicTests // 아군 드로우와 시작 공포 및 기도 끈 검사
{ // 시작 유물 검사 시작
    [UnityTest] // 기본 드로우와 예약 및 손패 상한의 차이 검증
    public IEnumerator SilverCogCountsOnlyActualBaselineArrivalsAndProtectsTheFirstLivingAlly() // 실제 기본 두 장과 첫 생존 아군의 방어도 확인
    { // 은빛 톱니 검사 시작
        yield return new EnterPlayMode(); // 회차 유물과 실제 턴 환경 진입
        yield return SilverCogCountsOnlyActualBaselineArrivalsAndProtectsTheFirstLivingAllyInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 상태 해제
    } // 은빛 톱니 검사 종료

    private IEnumerator SilverCogCountsOnlyActualBaselineArrivalsAndProtectsTheFirstLivingAllyInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("SilverCog"); // 실제 은빛 톱니 획득
        using (var fight = new OpeningFight(5, 2)) // 기본 두 장을 실제 받을 수 있는 전투
        { // 반복 기본 드로우 검사 시작
            fight.Allies[0].TakeDamage(fight.Allies[0].CurrentHealth, BattleDamageType.None); // 첫 배치 아군이 전투 불능인 경우 준비
            Assert.IsTrue(fight.Turns.StartBattle(2)); // 기본 시작 손패 두 장 지급
            Assert.AreEqual(2, fight.Allies[1].CurrentBlock); // 첫 번째 생존 아군의 방어도 두 점 확인
            Assert.AreEqual(0, fight.Allies[0].CurrentBlock); // 사망한 첫 배치의 방어도 미지급 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 다음 아군 턴 전환 준비
            fight.Allies[1].ClearBlock(); // 다음 기본 드로우의 새 발동 비교 준비
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 기본 손패 지급
            Assert.AreEqual(2, fight.Allies[1].CurrentBlock); // 매 아군 턴의 실제 기본 두 장마다 한 번 발동 확인
        } // 반복 기본 드로우 검사 종료
        using (var fight = new OpeningFight(5, 1)) // 기본 한 장과 예약 두 장 대조
        { // 예약 드로우 검사 시작
            QueueDraw(fight.Deck, 2); // 과거 턴 또는 전투 보상 형태의 예약 드로우 추가
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 실제 시작 손패 기본 한 장 지급
            Assert.AreEqual(3, fight.Turns.LastDrawnCardCount); // 기본과 예약을 합쳐 실제 세 장이 들어온 경우 확인
            Assert.AreEqual(0, fight.Allies[0].CurrentBlock); // 예약 두 장으로 기본 두 장 조건 충족 방지 확인
        } // 예약 드로우 검사 종료
        using (var fight = new OpeningFight(2, 2)) // 기본 드로우 두 번 중 손패 공간이 하나인 전투
        { // 손패 상한 검사 시작
            Assert.AreEqual(1, fight.Deck.DrawCards(1)); // 이미 손에 든 카드 한 장 준비
            Assert.IsTrue(fight.Turns.StartBattle(2)); // 기본 두 번을 요청하지만 빈자리는 한 장뿐인 상황
            Assert.AreEqual(1, fight.Turns.LastDrawnCardCount); // 실제 손에 들어온 기본 드로우 한 장 확인
            Assert.AreEqual(1, fight.Deck.DiscardPileCount); // 상한을 넘긴 드로우의 기존 버림 확인
            Assert.AreEqual(0, fight.Allies[0].CurrentBlock); // 상한 초과 및 기존 보유 카드의 기본 지급 집계 제외 확인
        } // 손패 상한 검사 종료
        RelicData seal = Resources.LoadAll<RelicData>("Relics").Single(relic => relic.EffectType == RelicEffectType.DiscardFirstUnplayableDraw); // 기존 봉인 유물의 실제 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.Instance.TryAcquire(seal)); // 기본 드로우 결과를 바꾸는 기존 봉인 획득
        using (var fight = new OpeningFight(5, 2, includeCurse: true)) // 기본 한 장이 봉인되고 이후 예약이 도착하는 전투
        { // 봉인 드로우 검사 시작
            Assert.IsTrue(fight.Deck.TryMoveDrawTop(fight.Deck.Cards[0], fight.Deck.CardCount)); // 첫 기본 시도의 사용불가 카드 배치
            QueueDraw(fight.Deck, 2); // 후속 예약 드로우의 실제 손패 도착 준비
            Assert.IsTrue(fight.Turns.StartBattle(2)); // 봉인 카드와 정상 기본 카드의 두 번 시도
            Assert.AreEqual(3, fight.Turns.LastDrawnCardCount); // 실제 기본 한 장과 예약 두 장 도착 확인
            Assert.AreEqual(0, fight.Allies[0].CurrentBlock); // 봉인한 기본 드로우에 예약 카드가 대체 집계되지 않는지 확인
        } // 봉인 드로우 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 포스터의 첫 실제 아군 턴과 디버프 면역 검사
    public IEnumerator PosterWaitsForTheFirstAllyTurnAndExpiresWithBlackCrown() // 적 선제와 면역 및 독립 공포의 지속 검증
    { // 구겨진 포스터 검사 시작
        yield return new EnterPlayMode(); // 실제 시작 유물 환경 진입
        yield return PosterWaitsForTheFirstAllyTurnAndExpiresWithBlackCrownInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 상태 해제
    } // 구겨진 포스터 검사 종료

    private IEnumerator PosterWaitsForTheFirstAllyTurnAndExpiresWithBlackCrownInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("CrumpledPoster"); // 구겨진 포스터 획득
        Hold("BlackCrown"); // 같은 시작 지속 경계를 쓰는 검은 왕관 획득
        using (var fight = new OpeningFight(5, 0)) // 면역 없는 일반 전투의 전체 적 대조
        { // 정상 첫 턴 검사 시작
            var prior = fight.Enemies[1]; // 포스터 이전부터 독립 공포를 가진 적
            prior.ApplyStatusEffect(BattleStatusEffectType.Fear, 1, 3, 1); // 더 긴 직접 효과가 남은 기존 공포 준비
            Assert.IsTrue(fight.Turns.StartBattle(0)); // 정상 첫 아군 턴 시작
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Fear) != null)); // 전체 생존 적의 공포 확인
            Assert.AreEqual(1, fight.Enemies.Count(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable) != null)); // 왕관의 기존 무작위 한 대상 유지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 실제 아군 턴과 두 시작 유물 종료
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)); // 포스터의 신규 공포만 종료 확인
            Assert.AreEqual(3, prior.GetStatusEffect(BattleStatusEffectType.Fear).RemainingTurns); // 시작 전 독립 공포의 지속 유지 확인
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable) == null)); // 기존 검은 왕관 지속도 함께 종료 확인
        } // 정상 첫 턴 검사 종료
        using (var fight = new OpeningFight(5, 0)) // 적 선제와 면역을 포함한 첫 아군 턴 전투
        { // 적 선제 검사 시작
            fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 두 번째 적의 기존 디버프 면역 준비
            Assert.IsTrue(fight.Turns.StartBattle(0, true)); // 먼저 움직이는 적 턴 시작
            Assert.IsTrue(fight.Enemies.All(enemy => enemy.GetStatusEffect(BattleStatusEffectType.Fear) == null)); // 적 선제에서 포스터 공포의 조기 소모 방지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 아군 턴 지급 단계
            Assert.AreEqual(1, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear).RemainingTurns); // 첫 실제 아군 턴 전체의 공포 한 턴 확인
            Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Fear)); // 기존 상태 면역에 정상 차단 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 선제 뒤 첫 아군 턴 종료
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Fear)); // 다음 적 턴에 남지 않는 첫 턴 전용 공포 확인
        } // 적 선제 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 기존 정신력 효과가 실제 BattleStarted에서 실행되는지 검사
    public IEnumerator PrayerCordRestoresOnlyLivingAlliesAtTheRealBattleStart() // 생존 대상과 최대 정신력 및 중복 발동 검증
    { // 기도 끈 검사 시작
        yield return new EnterPlayMode(); // 실제 공용 전투 신호 환경 진입
        yield return PrayerCordRestoresOnlyLivingAlliesAtTheRealBattleStartInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 상태 해제
    } // 기도 끈 검사 종료

    private IEnumerator PrayerCordRestoresOnlyLivingAlliesAtTheRealBattleStartInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("BrokenPrayerCord"); // 끊어진 기도 끈 획득
        using (var fight = new Fight("BALANCE_GUARD")) // 실제 손패와 카드 입력 신호의 검사 화면
        using (var turns = new BattleTurnRuntime(fight.Deck, fight.Points, fight.Allies, fight.Enemies, 0, BattleType.Normal)) // 새 전투 시작 신호를 직접 검증할 턴
        using (var dispatcher = new BattleEventDispatcher()) // 기존 전투 시작 유물의 공용 발행기
        using (var effects = new BattleRelicEffectController(dispatcher, RelicRunManager.Instance.Inventory, RelicRunManager.Instance.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points)) // 실제 시작 효과와 아군 대상 해결 연결
        using (var bridge = new BattleEventController(dispatcher, turns, fight.Controller, fight.Allies, fight.Enemies)) // 실제 진영 시작 신호를 전투 시작 이벤트에 연결
        { // 정신력 시작 효과 검사 시작
            var full = fight.Allies[0]; // 최대 정신력 가까이 있는 첫 아군
            var less = fight.Allies[1]; // 세 점을 모두 받을 두 번째 아군
            var fallen = fight.Allies[2]; // 전투 불능인 세 번째 아군
            fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None); // 시작 전에 전투 불능인 동료 준비
            int deadMental = fallen.CurrentMental; // 사망자 정신력 비교 보관
            full.ChangeMental(98 - full.CurrentMental, BattleMentalChangeReason.PersistentState); // 최대치 두 점 전의 아군 정신력 설정
            less.ChangeMental(70 - less.CurrentMental, BattleMentalChangeReason.PersistentState); // 세 점을 모두 받을 아군 정신력 설정
            Assert.IsTrue(turns.StartBattle(0, true)); // 적 선제의 실제 전투 시작 신호 발행
            Assert.AreEqual(100, full.CurrentMental); // 최대값까지만 정신력 증가 확인
            Assert.AreEqual(73, less.CurrentMental); // 다른 생존 아군의 세 점 정신력 증가 확인
            Assert.AreEqual(deadMental, fallen.CurrentMental); // 사망자에게 시작 회복 미적용 확인
            Assert.IsTrue(turns.CompleteEnemyTurn()); // 적 선제 뒤 첫 실제 아군 턴 시작
            Assert.AreEqual(73, less.CurrentMental); // 아군 시작 신호가 전투 시작 보상을 반복하지 않는지 확인
        } // 정신력 시작 효과 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    private static void Hold(string asset) // 게임이 등록한 실제 유물 획득
    { // 획득 조건 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + asset); // 실제 유물 원본 조회
        Assert.IsNotNull(relic); // 등록된 원본 유물 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 회차 유물 보관함에 등록 확인
    } // 획득 조건 준비 종료

    private static void QueueDraw(BattleDeckRuntime deck, int count) // 게임의 다음 아군 턴 예약 드로우 생성
    { // 예약 드로우 준비 시작
        var queue = typeof(BattleDeckRuntime).GetMethod("QueuePlayerTurnDraw", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 전투 보상이 호출하는 예약 진입점 조회
        Assert.IsNotNull(queue); // 원래 예약 동작의 실제 연결 확인
        queue.Invoke(deck, new object[] { count }); // 실제 다음 턴 드로우의 추가 수량 예약
    } // 예약 드로우 준비 종료

    private sealed class OpeningFight : IDisposable // 첫 드로우와 적 상태 경계 검사 전투
    { // 검사 전투 자료 시작
        private readonly CharacterData front = ScriptableObject.CreateInstance<CharacterData>(); // 앞쪽 배치 아군 원본
        private readonly CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 카드 소유 후열 아군 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 비교에 사용할 적 원본
        private readonly CardData filler = ScriptableObject.CreateInstance<CardData>(); // 비용 효과가 무관한 보통 카드
        private readonly CardData curse = ScriptableObject.CreateInstance<CardData>(); // 사용불가 드로우 비교 카드
        private readonly DeckData source; // 독립 덱 원본
        private readonly BattleStatusEffectController statuses; // 먼저 움직이는 적의 실제 상태 지속 연결
        internal BattleDeckRuntime Deck { get; } // 실제 손패와 드로우 순서
        internal BattleTurnRuntime Turns { get; } // 실제 진영별 단계와 지급 수량
        internal BattleUnitRuntime[] Allies { get; } // 앞뒤 아군의 실제 배치 목록
        internal BattleUnitRuntime[] Enemies { get; } // 첫 공포와 면역 검사용 실제 적 목록

        internal OpeningFight(int maxHand, int baseDraw, bool includeCurse = false) // 지정 손패 상한과 기본 드로우의 전투 생성
        { // 전투 구성 시작
            JsonUtility.FromJsonOverwrite($"{{\"effectType\":{(int)CardEffectType.Unplayable}}}", curse); // 사용하는 것이 불가능한 검사 카드 규칙 설정
            Allies = new[] { BattleUnitRuntime.CreateAlly(front), BattleUnitRuntime.CreateAlly(owner) }; // 독립 원본의 앞뒤 아군 순서 고정
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy), BattleUnitRuntime.CreateEnemy(enemy) }; // 서로 다른 상태를 적용할 생존 적 둘 준비
            source = DeckData.CreateRuntime(Enumerable.Range(0, 7).Select(index => new DeckCardEntry(includeCurse && index == 0 ? curse : filler, owner)).ToArray()); // 실제 드로우 시도의 독립 카드 일곱 장 구성
            Deck = BattleDeckRuntime.Create(source, Allies, maxHand, 246); // 실제 첫 손패 상한과 고정 셔플 생성
            Turns = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(3), Allies, Enemies, baseDraw, BattleType.Normal); // 지정한 일반 턴 기본 드로우 적용
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 포스터와 기존 상태의 턴 수명 검증 연결
        } // 전투 구성 종료

        public void Dispose() // 실제 턴과 임시 원본 정리
        { // 검사 자원 해제 시작
            statuses.Dispose(); // 지속 상태 변화의 신호 연결 해제
            Turns.Dispose(); // 아군과 적의 전투 이벤트 연결 해제
            Object.Destroy(source); // 준비한 공용 덱 원본 제거
            Object.Destroy(filler); // 일반 카드 원본 제거
            Object.Destroy(curse); // 저주 카드 원본 제거
            Object.Destroy(enemy); // 적 원본 제거
            Object.Destroy(front); // 첫 아군 원본 제거
            Object.Destroy(owner); // 카드 소유 아군 원본 제거
        } // 검사 자원 해제 종료
    } // 검사 전투 자료 종료
} // 시작 유물 검사 종료
