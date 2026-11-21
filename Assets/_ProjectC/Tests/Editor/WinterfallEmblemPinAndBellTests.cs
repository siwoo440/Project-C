using System; // 전투 검사 자원 해제 계약
using System.Collections; // 플레이 모드와 실제 카드 입력 대기
using System.Linq; // 카드 소유자와 손패 검사
using System.Reflection; // 퇴색 조우인 검사 조건 설정
using NUnit.Framework; // 실제 상태와 카드 이동 검증
using UnityEngine; // 테스트용 카드와 유닛 원본 준비
using UnityEngine.TestTools; // 플레이 모드 검사 지원
using Fight = WinterfallSwordCombatTests.Fight; // 실제 전투 손패와 명중 경로
using Object = UnityEngine.Object; // 검사에서 만든 원본 정리

public sealed class WinterfallEmblemPinAndBellTests // 방어 약화와 표식 피해 및 시작 손패 검사
{ // 전투 유물 검사 시작
    [UnityTest] // 파손 문양과 기존 두 유물의 동시 적용 검사
    public IEnumerator EmblemChecksAuthoredDefenseLossAndStacksWithTheOtherRelics() // 최초 감소량 기준과 지속 시간 하한 검증
    { // 문양 검사 시작
        yield return new EnterPlayMode(); // 실제 회차 유물과 탐사 상태 준비
        yield return EmblemChecksAuthoredDefenseLossAndStacksWithTheOtherRelicsInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 탐사 해제
    } // 문양 검사 종료

    private IEnumerator EmblemChecksAuthoredDefenseLossAndStacksWithTheOtherRelicsInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("BrokenShieldEmblem"); // 파손된 방패문양 획득
        Hold("SquareMetalPlate"); // 같은 원본 수치에 적용하는 기존 금속판 획득
        Hold("IncompleteGasMask"); // 퇴색 조우에서 지속을 줄이는 방독면 획득
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 지속 판정이 읽는 탐사 관리자
        var fade = typeof(ExplorationSessionManager).GetProperty("ActiveEncounterInFade", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 조우의 퇴색 표시 조회
        Assert.IsNotNull(fade); // 실제 퇴색 교전 판정 연결 확인
        fade.SetValue(session, true); // 퇴색 교전 상태의 대조 구간 시작
        using (var fight = new OpeningFight(MakeCard(CardEffectType.GainBlock))) // 지속 상태를 적용할 생존 유닛 준비
        { // 디버프 검사 시작
            var ally = fight.Allies[0]; // 실제 수신 판정의 아군
            AssertDuration(ally, BattleStatusEffectType.PhysicalDefenseDown, 2, 4, 1, 2); // 금속판 수치 감소와 두 지속 감소의 독립 적용
            AssertDuration(ally, BattleStatusEffectType.ChargeDefenseDown, 2, 2, 1, 1); // 돌진 약화와 한 턴의 최소 지속 확인
            AssertDuration(ally, BattleStatusEffectType.PhysicalDefenseDown, 3, 4, 3, 3); // 원본 감소량 세 점에는 방독면만 적용 확인
            AssertDuration(ally, BattleStatusEffectType.MagicalResistanceDown, 2, 4, 2, 3); // 마법 저항 감소에는 방독면만 적용 확인
            AssertDuration(fight.Enemies[0], BattleStatusEffectType.PhysicalDefenseDown, 2, 4, 2, 4); // 적 상태의 원본 수치와 지속 유지 확인
            fade.SetValue(session, false); // 일반 조우의 문양 단독 지속 감소 대조
            AssertDuration(ally, BattleStatusEffectType.PhysicalDefenseDown, 2, 4, 1, 3); // 퇴색 밖에서 문양만 지속에 적용 확인
        } // 디버프 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 표식 대상에 카드 타격을 실제로 실행하는 검사
    public IEnumerator MarkPinAddsOneToEachMagicalHitOnEitherMarkAndStacksWithChalk() // 두 표식에도 타격당 한 차례의 마법 피해 증가 확인
    { // 표식핀 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 사용 화면 초기화
        yield return MarkPinAddsOneToEachMagicalHitOnEitherMarkAndStacksWithChalkInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 카드와 유물 상태 해제
    } // 표식핀 검사 종료

    private IEnumerator MarkPinAddsOneToEachMagicalHitOnEitherMarkAndStacksWithChalkInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("CrudeMarkPin"); // 투박한 표식핀 획득
        Hold("MarkingChalk"); // 기존 직접 피해 증가 분필 획득
        var magical = MakeCard(CardEffectType.Damage, damage: BattleDamageType.Magical, hits: 2); // 고정 마법 피해를 두 번 주는 비교 카드
        var physical = MakeCard(CardEffectType.Damage, damage: BattleDamageType.Physical); // 동일 계열의 물리 피해 비교 카드
        try // 카드 원본 정리 보장
        { // 직접 카드 타격 검사 시작
            using var fight = new Fight(new[] { magical, magical, magical, magical, physical }); // 동일 카드와 마법이 아닌 비교 카드의 다섯 장 손패
            var marked = fight.Enemies[0]; // 두 표식이 겹친 첫 실제 대상
            var clear = fight.Enemies[1]; // 표식이 없는 두 번째 대상
            var publicOnly = fight.Enemies[2]; // 공명 표식만 있는 세 번째 대상
            marked.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 수신기의 공명 표식 적용
            marked.ApplyStatusEffect(BattleStatusEffectType.ChariotMark, 1, 1, 1); // 전차 표식을 같은 대상에 추가
            publicOnly.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 다른 대상에 공명 표식 단독 적용
            yield return WinterfallCommonCardExpansionTests.Play(magical, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 두 표식 대상에게 마법 카드 실제 입력
            Assert.AreEqual(marked.MaxHealth - 20, marked.CurrentHealth); // 두 타격 각각 기본 칠 점과 분필 이 점 및 표식핀 한 점 확인
            marked.RemoveStatusEffect(BattleStatusEffectType.PublicMark); // 전차 표식만 남긴 마법 아닌 피해 대조
            yield return WinterfallCommonCardExpansionTests.Play(physical, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 기존 분필만 적용되는 물리 카드 실제 입력
            Assert.AreEqual(marked.MaxHealth - 29, marked.CurrentHealth); // 물리 피해는 기존 분필 이 점만 가산 확인
            yield return WinterfallCommonCardExpansionTests.Play(magical, fight.Hand, fight.EnemyViews[0], fight.Runner, fight.Deck, fight.Points); // 전차 표식만으로 표식핀 효과 확인
            Assert.AreEqual(marked.MaxHealth - 49, marked.CurrentHealth); // 전차 표식의 단독 가산 확인
            yield return WinterfallCommonCardExpansionTests.Play(magical, fight.Hand, fight.EnemyViews[2], fight.Runner, fight.Deck, fight.Points); // 공명 표식만으로 표식핀 효과 확인
            Assert.AreEqual(publicOnly.MaxHealth - 20, publicOnly.CurrentHealth); // 공명 표식의 단독 가산 확인
            yield return WinterfallCommonCardExpansionTests.Play(magical, fight.Hand, fight.EnemyViews[1], fight.Runner, fight.Deck, fight.Points); // 표식 없이 같은 마법 카드 실제 입력
            Assert.AreEqual(clear.MaxHealth - 14, clear.CurrentHealth); // 표식 없는 대상의 모든 마법 피해 원본 확인
        } // 직접 카드 타격 검사 종료
        finally // 공용 덱 카드 원본 정리
        { // 카드 원본 제거 시작
            Object.Destroy(magical); // 마법 검사 카드 제거
            Object.Destroy(physical); // 물리 검사 카드 제거
        } // 카드 원본 제거 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 시작 선제와 실제 첫 손패만 보관하는 유물 검사
    public IEnumerator BellRetainsOneUsableOriginalOnceWithoutOverridingIntrinsicRetain() // 저주와 휘발을 뺀 첫 손패 및 고유 유지 동작 확인
    { // 핸드벨 유지 검사 시작
        yield return new EnterPlayMode(); // 실제 첫 손패 드로우 환경 준비
        yield return BellRetainsOneUsableOriginalOnceWithoutOverridingIntrinsicRetainInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 유물 상태 해제
    } // 핸드벨 유지 검사 종료

    private IEnumerator BellRetainsOneUsableOriginalOnceWithoutOverridingIntrinsicRetainInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("MetalHandbell"); // 금속 핸드벨 획득
        foreach (bool intrinsic in new[] { false, true }) // 일반과 카드 자체 유지 비교
        { // 유지 방식별 검사 시작
            using var fight = new OpeningFight(MakeCard(CardEffectType.GainBlock, retain: intrinsic), MakeCard(CardEffectType.Unplayable), MakeCard(CardEffectType.GainBlock, volatileInHand: true)); // 첫 보관 카드 한 장과 제외 후보 두 장 구성
            var intended = fight.Deck.Cards[0]; // 처음부터 보유한 실제 카드 인스턴스
            Assert.IsTrue(fight.Turns.StartBattle(3, true)); // 적 선제로 먼저 시작
            Assert.AreEqual(0, fight.Deck.HandCount); // 실제 아군 드로우 전 손패가 비어 있는지 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 아군 손패 드로우 진행
            Assert.AreEqual(3, fight.Deck.HandCount); // 사용할 수 없는 저주도 포함된 실제 시작 손패 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 표준 손패 정리
            Assert.AreSame(intended, fight.Deck.Hand.Single()); // 사용 가능한 원래 한 장만 유지 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, fight.Deck.Cards[1]); // 사용불가 카드의 정상 버림 확인
            CollectionAssert.Contains(fight.Deck.ExcludedPile, fight.Deck.Cards[2]); // 휘발 카드의 기존 전투 제외 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 추가 드로우 없이 두 번째 아군 턴 이동
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 표준 손패 정리
            Assert.AreEqual(intrinsic ? 1 : 0, fight.Deck.HandCount); // 유물 유지 한 번과 카드 자체 유지의 독립 확인
            Assert.IsTrue(fight.Deck.IsStateValid()); // 모든 실제 카드 영역 보존 확인
        } // 유지 방식별 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 직접 카드 이동은 개시 유지 권리를 소모하는지 검사
    public IEnumerator BellDoesNotKeepACardThatLeftTheHandEvenIfIndependentlyRecovered() // 사용과 수동 버림 이후 자동 회수 및 재유지 차단 확인
    { // 손패 이탈 검사 시작
        yield return new EnterPlayMode(); // 시작 카드 유지 환경 준비
        yield return BellDoesNotKeepACardThatLeftTheHandEvenIfIndependentlyRecoveredInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 유물 상태 해제
    } // 손패 이탈 검사 종료

    private IEnumerator BellDoesNotKeepACardThatLeftTheHandEvenIfIndependentlyRecoveredInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("MetalHandbell"); // 금속 핸드벨 획득
        foreach (bool used in new[] { false, true }) // 실제 사용 정산과 수동 버림 모두 대조
        { // 이동 방식별 검사 시작
            using var fight = new OpeningFight(MakeCard(CardEffectType.GainBlock)); // 첫 손패 하나로 유지 대상 확정
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 첫 아군 손패 지급
            var card = fight.Deck.Hand.Single(); // 유지 대상으로 선택된 원래 카드
            Assert.IsTrue(used ? fight.Deck.TryPlayAndDraw(card, 0, out _) : fight.Deck.DiscardCard(card)); // 실제 손패 사용 정산 또는 명시적 버림
            Assert.AreEqual(0, fight.Deck.HandCount); // 유물이 직접 사용과 버림을 막지 않는지 확인
            Assert.IsTrue(fight.Deck.TryRecoverDiscard(card)); // 다른 효과를 통해 같은 객체가 손패로 돌아오는 경우
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 표준 손패 정리 실행
            Assert.AreEqual(0, fight.Deck.HandCount); // 한 번 떠난 카드의 이전 예약 재발동 차단 확인
            Assert.AreSame(card, fight.Deck.DiscardPile.Single()); // 회수한 카드의 정상 자동 버림 위치 확인
        } // 이동 방식별 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    private static void AssertDuration(BattleUnitRuntime target, BattleStatusEffectType effect, int authoredValue, int duration, int expectedValue, int expectedDuration) // 실제 상태의 원본 대비 수치와 지속 검사
    { // 상태 지속 검증 시작
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, target.ApplyStatusEffect(effect, authoredValue, duration, 1)); // 실제 상태의 최초 적용 확인
        var status = target.GetStatusEffect(effect); // 실질적으로 남은 적용 상태 조회
        Assert.AreEqual(expectedValue, status.EffectiveValue); // 부여된 감소량 일치 확인
        Assert.AreEqual(expectedDuration, status.RemainingTurns); // 부여된 지속 시간 일치 확인
        target.RemoveStatusEffect(effect); // 다음 대조 전 겹침과 갱신 효과 제거
    } // 상태 지속 검증 종료

    private static void Hold(string asset) // 등록된 실제 유물 획득
    { // 보유 유물 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + asset); // 실제 게임의 유물 원본 조회
        Assert.IsNotNull(relic); // 등록된 원본 유물 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 회차 보관함 지급 확인
    } // 보유 유물 준비 종료

    private static CardData MakeCard(CardEffectType effect, bool retain = false, bool volatileInHand = false, BattleDamageType damage = BattleDamageType.Physical, int hits = 1) // 지팡이 속성의 고정 원본 피해 및 손패 검사 카드
    { // 검사 카드 원본 준비 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 빈 실제 카드 원본 생성
        string retained = retain ? "true" : "false"; // 원본 유지 상태의 직렬화 값
        string volatileCard = volatileInHand ? "true" : "false"; // 원본 휘발 상태의 직렬화 값
        JsonUtility.FromJsonOverwrite($"{{\"cardType\":{(int)CardType.Wand},\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)effect},\"damageType\":{(int)damage},\"damageHitCount\":{hits},\"effectValue\":7,\"retainInHand\":{retained},\"volatileInHand\":{volatileCard}}}", card); // 가변 보정 없는 원본과 지정 손패 속성 입력
        return card; // 검사 덱에 넣을 원본 반환
    } // 검사 카드 원본 준비 종료

    private sealed class OpeningFight : IDisposable // 실제 손패와 턴 경계 중심의 독립 전투
    { // 검사 전투 시작
        private readonly CharacterData allySource = ScriptableObject.CreateInstance<CharacterData>(); // 한 명의 실제 카드 소유자 원본
        private readonly EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 정상 턴을 진행할 적 원본
        private readonly CardData[] cards; // 해제 대상 카드 원본 목록
        private readonly DeckData source; // 생성한 기본 전투 덱 원본
        internal BattleUnitRuntime[] Allies { get; } // 실제 아군 대상 목록
        internal BattleUnitRuntime[] Enemies { get; } // 실제 적 대상 목록
        internal BattleDeckRuntime Deck { get; } // 실제 손패와 드로우 더미
        internal BattleTurnRuntime Turns { get; } // 실제 시작과 정리 및 적 단계

        internal OpeningFight(params CardData[] sources) // 지정 카드로 시작 손패 검사 전투 구성
        { // 독립 전투 구성 시작
            cards = sources; // 임시 카드 원본 정리 목록 보관
            Allies = new[] { BattleUnitRuntime.CreateAlly(allySource) }; // 실제 카드 소유자 한 명 준비
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemySource) }; // 정상 턴에 필요한 생존 적 준비
            source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, allySource)).ToArray()); // 개별 카드와 소유자를 전투 덱에 구성
            Deck = BattleDeckRuntime.Create(source, Allies, 5, 244); // 고정 시드의 검사 손패 주문
            Turns = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, BattleType.Normal); // 손패 보관을 분리해 검사할 추가 드로우 없는 턴
        } // 독립 전투 구성 종료

        public void Dispose() // 검사에 필요한 실제 턴과 원본 해제
        { // 전투 검사 자원 정리 시작
            Turns.Dispose(); // 턴 및 상태 이벤트 연결 해제
            Object.Destroy(source); // 생성한 검사 덱 원본 제거
            Object.Destroy(allySource); // 검사 아군 원본 제거
            Object.Destroy(enemySource); // 검사 적 원본 제거
            foreach (var card in cards) // 빈 손패 검사에 사용한 카드 순회
            { // 카드 해제 시작
                Object.Destroy(card); // 임시 검사 카드 제거
            } // 카드 해제 종료
        } // 전투 검사 자원 정리 종료
    } // 검사 전투 종료
} // 전투 유물 검사 종료
