using System; // 전투 검사 자원 해제 계약
using System.Collections; // 유니티 플레이 도메인 전환 대기
using System.IO; // 독립 탐사 저장 파일 조회
using System.Linq; // 카드와 유물 및 아군 원본 조회
using System.Reflection; // 탐사와 전투의 내부 연결 호출
using System.Text.RegularExpressions; // 이전 저장 형식 재현
using NUnit.Framework; // 실제 전투와 저장 결과 검증
using UnityEngine; // 임시 카드와 적 원본 생성
using UnityEngine.TestTools; // 플레이 모드 검사 등록
using Fight = WinterfallSwordCombatTests.Fight; // 실제 피해 이벤트용 전투 환경
using Object = UnityEngine.Object; // 임시 게임 원본 해제

public sealed class WinterfallCrownAndSpecimenRelicTests // 받는 피해와 시작 보상 유물 검사
{ // 유물 검사 시작
    [UnityTest] // 실제 체력 피해 유물의 파티 공용 횟수 검사
    public IEnumerator BellGrantsBlockOnlyAfterPositiveHealthLossTwice() // 방어도 흡수와 체력 비용 및 사망 제외 확인
    { // 방울 검사 시작
        yield return new EnterPlayMode(); // 검사 전투 활성화
        yield return BellGrantsBlockOnlyAfterPositiveHealthLossTwiceInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 상태 해제
    } // 방울 검사 종료

    private IEnumerator BellGrantsBlockOnlyAfterPositiveHealthLossTwiceInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("RupturedBell"); // 파열된 방울 획득
        using (var fight = new Fight("BALANCE_GUARD")) // 실제 손패와 전투 이벤트 준비
        using (var dispatcher = new BattleEventDispatcher()) // 피해 완료 공용 발행기
        using (var effects = new BattleRelicEffectController(dispatcher, RelicRunManager.Instance.Inventory, RelicRunManager.Instance.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points)) // 전투 유물의 횟수와 적용 연결
        using (var bridge = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 런타임의 피해 후 실제 공용 신호 연결
        { // 체력 손실 검사 시작
            var first = fight.Allies[0]; // 첫 피격 아군
            var second = fight.Allies[1]; // 다른 피격 아군
            var enemy = fight.Enemies[0]; // 고정 피해의 적 발생자
            first.GainBlock(4); // 완전 흡수 시험용 방어도
            first.TakeDamage(2, BattleDamageType.None, enemy); // 체력 피해 없는 첫 공격
            Assert.AreEqual(2, first.CurrentBlock); // 완전히 막은 피해에 방울 미발동 확인
            first.ClearBlock(); // 비교용 방어도 초기화
            Assert.IsTrue(first.TrySpendHealth(1)); // 카드의 실제 체력 비용 지불
            Assert.AreEqual(0, first.CurrentBlock); // 체력 비용에 방울 미발동 확인
            var fallen = fight.Allies[2]; // 치명 피해를 맞을 세 번째 아군
            fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None, enemy); // 사망하는 실제 체력 피해
            Assert.AreEqual(0, fallen.CurrentBlock); // 사망자에게 방울 기회 미소비 확인
            first.TakeDamage(4, BattleDamageType.None, enemy); // 첫 생존 체력 피해
            second.ApplyStatusEffect(BattleStatusEffectType.Bleed, 4, 1, 1, enemy); // 다른 아군의 지속 피해 준비
            new BattleStatusEffectProcessor().ProcessPhaseEnd(second, fight.Turns.CurrentRound); // 출혈로 두 번째 실제 체력 피해 발생
            Assert.AreEqual(3, first.CurrentBlock); // 첫 생존 피격자의 방어도 지급 확인
            Assert.AreEqual(3, second.CurrentBlock); // 두 번째 생존 피격자의 방어도 지급 확인
            Assert.AreEqual(1, second.TakeDamage(4, BattleDamageType.None, enemy).AppliedDamage); // 다음 공격에서 방어도 세 점 적용 확인
            Assert.AreEqual(0, second.CurrentBlock); // 세 번째 체력 피해의 전투 공용 발동 차단 확인
        } // 체력 손실 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 고정 카드 비용과 선제 전투의 시작 약화 검사
    public IEnumerator CrownsStackOnTheFirstRealPlayerTurnAndExpireTogether() // 첫 실제 손패 할인과 적 취약 해제 확인
    { // 두 왕관 검사 시작
        yield return new EnterPlayMode(); // 검사 유물 보관함 활성화
        yield return CrownsStackOnTheFirstRealPlayerTurnAndExpireTogetherInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 검사 전투와 유물 상태 해제
    } // 두 왕관 검사 종료

    private IEnumerator CrownsStackOnTheFirstRealPlayerTurnAndExpireTogetherInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        Hold("FadedCrown"); // 퇴색한 왕관 획득
        Hold("BlackCrown"); // 검은 왕관 획득
        Hold("LeatherNotebook"); // 가죽 수첩과 비용 중첩 준비
        foreach (bool ambush in new[] { false, true }) // 일반 및 적 선제의 실제 첫 아군 턴 검사
        { // 전투 진입별 검사 시작
            using var fight = new OpeningFight(); // 기본 비용 4의 고정 카드가 있는 검사 전투
            Assert.IsTrue(fight.Deck.TryMoveDrawTop(fight.Deck.Cards[0], fight.Deck.CardCount)); // 고정 비용 카드가 맨 먼저 뽑히도록 배치
            Assert.IsTrue(fight.Turns.StartBattle(3, ambush)); // 선제 여부를 반영한 전투 시작
            if (ambush) // 적 턴을 먼저 거치는 검사 확인
            { // 선제 턴 처리 시작
                Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 선제 턴 종료로 취약이 낭비되지 않도록 확인
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 아군 턴 진입
            } // 선제 턴 처리 종료
            var card = fight.Deck.Cards[0]; // 유일하게 줄일 고정 기본 비용의 카드
            Assert.AreEqual(2, card.ApCost); // 가죽 수첩과 퇴색 왕관의 할인 합산 확인
            Assert.AreEqual(4, fight.Deck.Cards[1].ApCost); // 가변 비용 카드의 왕관 제외 확인
            Assert.AreEqual(0, fight.Deck.Cards[2].ApCost); // 무료 카드의 비용 바닥 유지 확인
            var enemy = fight.Enemies[0]; // 무작위 후보 중 유일한 생존 적
            Assert.AreEqual(20, enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable).EffectiveValue); // 상태 원본의 피해 증가율 확인
            enemy.GainBlock(6); // 방어도 흡수 순서 확인 준비
            Assert.AreEqual(1, enemy.PreviewDamage(1, BattleDamageType.Physical).RawDamage); // 소량 피해의 소수점 내림과 양수 보존 확인
            Assert.AreEqual(13, enemy.PreviewDamage(11, BattleDamageType.Physical).RawDamage); // 20퍼센트 증가 후 소수점 내림 확인
            Assert.AreEqual(7, enemy.TakeDamage(11, BattleDamageType.Physical, fight.Allies[0]).AppliedDamage); // 증가한 원본 피해 뒤 방어도 적용 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 아군 턴의 비용과 취약 제거
            Assert.AreEqual(4, card.ApCost); // 첫 손패에 적용한 두 할인 모두 만료 확인
            Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 첫 실제 아군 턴 종료의 취약 제거 확인
        } // 전투 진입별 검사 종료
        using (var fight = new OpeningFight()) // 상태 면역이 있는 적의 별도 전투 준비
        { // 면역 상태 검사 시작
            var enemy = fight.Enemies[0]; // 무작위 후보의 유일한 적
            enemy.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 일반 디버프의 확정 면역 적용
            Assert.IsTrue(fight.Turns.StartBattle(1)); // 면역이 있는 첫 아군 턴 시작
            Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Vulnerable)); // 검은 왕관도 기존 면역에 차단되는지 확인
        } // 면역 상태 검사 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    [UnityTest] // 실제 조우와 체크포인트의 표본병 발동 검사
    public IEnumerator SampleVialsCarryOnlyAnAshMaggotVictoryThroughSaveOnce() // 괴생체 판정과 저장 후 선제 전투 보상 확인
    { // 표본병 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 거점 저장과 재화 보호
        { // 격리 검사 시작
            Assert.Ignore("플레이 저장 경로가 격리된 검증 사본에서 실행"); // 검사 환경 이외의 회차 저장 보호
        } // 격리 검사 종료
        yield return new EnterPlayMode(); // 독립 탐사와 회차 유물 활성화
        yield return SampleVialsCarryOnlyAnAshMaggotVictoryThroughSaveOnceInPlayMode(); // 플레이 모드 전환 뒤 실제 검사 시작
        yield return new ExitPlayMode(); // 독립 탐사 도메인 종료
    } // 표본병 검사 종료

    private IEnumerator SampleVialsCarryOnlyAnAshMaggotVictoryThroughSaveOnceInPlayMode() // 플레이 모드의 실제 대상과 상태 검사
    { // 플레이 모드 본문 시작
        var session = PrepareExpedition(); // 실제 저장 가능한 두 아군의 탐사 시작
        Hold("TaintedSampleVial"); // 오염 표본병 획득
        Hold("RiftSampleVial"); // 균열 표본병 획득
        var maggot = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshMaggot"); // 괴생체의 실제 조우 원본
        var other = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 다른 일반 조우 대조 원본
        string path = Path.Combine(Application.temporaryCachePath, "specimen-" + Guid.NewGuid().ToString("N") + ".json"); // 독립 회차 저장 파일
        try // 검사 저장 파일 정리 보장
        { // 저장과 소모 검사 시작
            using var fight = new OpeningFight(); // 보상 적용의 실제 드로우와 행동력 준비
            var victory = new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, fight.Allies, Array.Empty<string>()); // 실제 생존 아군의 승리 스냅샷
            var escape = new BattleResultData(BattleResult.Escape, BattleType.Normal, 1, fight.Allies, Array.Empty<string>()); // 괴생체 도주의 대조 결과
            Assert.IsTrue(session.BeginEncounter("unrelated-specimen", other, Vector3.zero, Vector3.zero)); // 괴생체가 아닌 일반 조우 시작
            session.ResolveBattleResult(victory); // 무관한 일반 승리 정산
            Assert.IsTrue(session.BeginEncounter("maggot-specimen", maggot, Vector3.zero, Vector3.zero)); // 실제 괴생체 전투 진입
            session.ResolveBattleResult(escape); // 가산 없는 괴생체 도주 정산
            Assert.IsTrue(session.BeginEncounter("maggot-specimen", maggot, Vector3.zero, Vector3.zero)); // 같은 괴생체에 다시 도전
            session.ResolveBattleResult(victory); // 괴생체 승리의 두 표본병 보상 예약
            session.ResolveBattleResult(victory); // 같은 결과 중복 입력 차단 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero)); // 다음 교전 예약의 실제 체크포인트 기록
            string json = File.ReadAllText(path); // 새 예약 값이 들어간 저장 조회
            string legacy = Regex.Replace(json, ",\"pendingSpecimen(Draw|ActionPoints)\":\\d+", string.Empty); // 예약 필드가 없던 저장 재현
            Assert.AreNotEqual(json, legacy); // 새로 도입된 실제 저장 필드 확인
            Assert.IsTrue(ExplorationSaveService.CanLoadJson(legacy)); // 과거 탐사 저장 역호환 확인
            session.ResetExploration(); // 메모리의 이전 예약 완전 제거
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 체크포인트에서 표본병 보상 복원
            session.AdvanceFloor(); // 다음 층에도 미사용 예약 전달 확인
            Assert.IsTrue(session.BeginEncounter("later-specimen", other, Vector3.zero, Vector3.zero)); // 다음에 실제로 시작할 교전 생성
            Assert.IsTrue(fight.Turns.StartBattle(1, true, () => Transfer(session, fight.Turns))); // 탐사 시작 연결을 사용한 적 선제 교전 진입
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 적 선제 중 가산 행동력 미사용 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 전투의 첫 실제 아군 턴 진입
            Assert.AreEqual(2, fight.Turns.LastDrawnCardCount); // 기본 한 장과 표본병 한 장 및 중복 방지 확인
            Assert.AreEqual(4, fight.Points.CurrentActionPoints); // 첫 아군 턴의 기본 행동력에 한 점 지급 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴으로 이동
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 표본병의 일시 행동력 회수 확인
            using var retry = new OpeningFight(); // 같은 예약을 두 번 쓸 수 없는지 확인할 전투
            Assert.IsTrue(retry.Turns.StartBattle(1, false, () => Transfer(session, retry.Turns))); // 이미 사용한 보상의 재소모 요청
            Assert.AreEqual(1, retry.Turns.LastDrawnCardCount); // 한 번 소모한 추가 드로우의 재지급 차단
            Assert.AreEqual(3, retry.Points.CurrentActionPoints); // 한 번 소모한 추가 행동력의 재지급 차단
        } // 저장과 소모 검사 종료
        finally // 검사 체크포인트 삭제
        { // 파일 정리 시작
            ExplorationSaveService.Remove(path); // 원본과 백업 검사 파일 제거
        } // 파일 정리 종료
        yield break; // 동기 검증 뒤 실제 검사 루틴 종료
    } // 플레이 모드 본문 종료

    private static void Hold(string asset) // 실제 유물 카탈로그의 효과 획득
    { // 유물 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + asset); // 정식 플레이 유물 원본 조회
        Assert.IsNotNull(relic); // 유물 자산 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 회차 유물 획득 확인
    } // 유물 준비 종료

    private static void Transfer(ExplorationSessionManager session, BattleTurnRuntime turns) // 실제 씬의 내부 보상 전달 경로 호출
    { // 탐사 보상 연결 시작
        var method = typeof(ExplorationSessionManager).GetMethod("ApplyOpeningSpecimenRewards", BindingFlags.Instance | BindingFlags.NonPublic); // 씬에 사용하는 내부 전달 메서드 조회
        Assert.IsNotNull(method); // 실제 탐사 시작 연결의 존재 확인
        method.Invoke(session, new object[] { turns }); // 실제 전투 시작 콜백으로 예약 보상 전달
    } // 탐사 보상 연결 종료

    private static ExplorationSessionManager PrepareExpedition() // 실제 저장 가능한 탐사 구성
    { // 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 정식 출전 가능 아군 원본 조회
        var recovery = new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance()); // 실제 아군 출전 상태 연결
        var selection = new PartySelection(recovery); // 실제 출전과 시작 덱 구성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 출전 아군 두 명 등록
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 쓰기 가능한 탐사 파티 준비
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 저장할 실제 회차 덱 연결
        var session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 관리자 조회
        session.ResetExploration(); // 새 탐사의 보상과 조우 상태 초기화
        session.SetCurrentFloorSeed(242); // 저장 가능한 시험 층 생성 시드 설정
        return session; // 실제 준비한 탐사 반환
    } // 탐사 준비 종료

    private sealed class OpeningFight : IDisposable // 첫 손패와 실제 적 턴을 다루는 검사 전투
    { // 검사 전투 시작
        private readonly CharacterData owner = ScriptableObject.CreateInstance<CharacterData>(); // 한 명의 독립 소유자 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 무방어 적 원본
        private readonly CardData[] cards; // 실제 덱의 검사 카드 원본
        private readonly DeckData source; // 생성한 공용 덱 원본
        private readonly BattleStatusEffectController statuses; // 실제 턴 경계의 상태 처리
        internal BattleUnitRuntime[] Allies { get; } // 검사 전투의 실제 생존 아군
        internal BattleUnitRuntime[] Enemies { get; } // 검사 전투의 실제 살아 있는 적
        internal BattleDeckRuntime Deck { get; } // 검사 전투의 실제 카드 위치
        internal BattleTurnRuntime Turns { get; } // 실제 전투의 진영 턴과 상태
        internal BattleActionPointRuntime Points { get; } = new BattleActionPointRuntime(3); // 검사용 기본 공용 행동력 세 점

        internal OpeningFight() // 고정·가변·무료 비용 검사용 실제 전투 준비
        { // 검사 전투 구성 시작
            cards = new[] { CreateCard(4), CreateCard(4, SwordCardRule.Finisher), CreateCard(0) }; // 할인 대상 하나와 대조 카드 두 장 준비
            Allies = new[] { BattleUnitRuntime.CreateAlly(owner) }; // 카드 소유자의 전투 유닛 생성
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy) }; // 한 명의 확정 무작위 후보 생성
            source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, owner)).ToArray()); // 카드별 독립 덱 항목 준비
            Deck = BattleDeckRuntime.Create(source, Allies, 5, 242); // 고정 셔플과 기본 손패 상한의 실제 덱 생성
            Turns = new BattleTurnRuntime(Deck, Points, Allies, Enemies, 1, BattleType.Normal); // 기본 한 장 드로우의 실제 턴 관리자 생성
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 적 턴의 상태 지속 시간 연결
        } // 검사 전투 구성 종료

        private static CardData CreateCard(int cost, SwordCardRule rule = SwordCardRule.None) // 독립 비용 비교 카드 원본 생성
        { // 검사 카드 준비 시작
            var card = ScriptableObject.CreateInstance<CardData>(); // 비어 있는 실제 카드 원본 생성
            JsonUtility.FromJsonOverwrite($"{{\"apCost\":{cost},\"effectType\":{(int)CardEffectType.GainBlock},\"swordRule\":{(int)rule}}}", card); // 고정 비용과 처형용 가변 비용 규칙 구분
            return card; // 실제 덱에서 사용할 카드 원본 반환
        } // 검사 카드 준비 종료

        public void Dispose() // 실제 검사 전투의 모든 원본 정리
        { // 검사 자원 해제 시작
            statuses.Dispose(); // 상태 지속 이벤트 연결 해제
            Turns.Dispose(); // 진영 및 사망 이벤트 연결 해제
            Object.Destroy(source); // 임시 공용 덱 원본 제거
            Object.Destroy(owner); // 임시 아군 원본 제거
            Object.Destroy(enemy); // 임시 적 원본 제거
            foreach (var card in cards) // 카드 원본별 정리 순회
            { // 카드 원본 해제 시작
                Object.Destroy(card); // 검사 카드 원본 제거
            } // 카드 원본 해제 종료
        } // 검사 자원 해제 종료
    } // 검사 전투 종료
} // 유물 검사 종료
