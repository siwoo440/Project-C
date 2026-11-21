using System.Collections; // 플레이 모드 단계별 검사 사용
using System.Linq; // 실제 손패 카드 화면과 비용 UI 검색 사용
using NUnit.Framework; // 테스트 결과 확인 사용
using TMPro; // 실제 공용 행동력 표시 확인 사용
using UnityEngine; // 카드 원본과 무작위 입력 사용
using UnityEngine.TestTools; // 테스트의 실제 플레이 모드 전환 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투와 손패 검사 환경

public sealed class WinterfallActionPointRelicTests // 균열 가루병과 헐거운 탄띠 검증
{ // 행동력 유물 테스트 시작
    [UnityTest] // 실제 적 처치와 확률 경계 검사 지정
    public IEnumerator RiftDustJar_RollsPerCreditedPlayerTurnKillAndHonorsCapacity() // 아군 턴 적 처치의 독립 확률과 행동력 상한 확인
    { // 가루병 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 전투 준비
        yield return CheckRiftDustJar(); // 플레이 도메인에서 실제 적 처치 검사 실행
        yield return new ExitPlayMode(); // 실제 카드 입력 전투 종료
    } // 가루병 플레이 모드 종료

    [UnityTest] // 일반 시작과 적 선제 이후의 행동력 상한 검사 지정
    public IEnumerator LooseBandolier_AddsOnlyToFirstActualAllyTurn() // 첫 실제 아군 턴의 임시 상한과 기존 효과 중첩 확인
    { // 탄띠 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 손패와 턴 전환 준비
        yield return CheckLooseBandolier(); // 플레이 도메인에서 첫 턴 중첩 검사 실행
        yield return new ExitPlayMode(); // 실제 손패와 턴 환경 종료
    } // 탄띠 플레이 모드 종료

    private static IEnumerator CheckRiftDustJar() // 실제 전투의 피해와 처치 경로에서 행동력 회복 확인
    { // 적 처치 행동력 검사 시작
        RelicData jar = Hold("RiftDustJar"); // 실제 등록한 균열 가루병 보유
        Assert.AreEqual(RelicTriggerType.UnitDefeated, jar.TriggerType); // 실제 처치 신호가 필수인 발동 시점 확인
        Assert.AreEqual(RelicEffectType.GainActionPoints, jar.EffectType); // 기존 현재 행동력 회복 경로 확인
        Assert.AreEqual(20, jar.TriggerChancePercent); // 가루병의 독립 발동 확률 확인
        Assert.AreEqual(1, jar.EffectValue); // 성공 시 현재 행동력 한 점 회복 확인
        Assert.AreEqual(0, jar.MaximumTriggersPerTurn); // 같은 아군 턴에 여러 번의 실제 처치 판정 허용
        Assert.AreEqual(0, jar.MaximumTriggersPerBattle); // 전투 전체의 독립 처치 판정 허용
        Hold("SealedDagger"); // 첫 적 처치로 한 점을 확정 회복하는 기존 봉인된 단도 획득
        int successSeed = FindSeedForRoll(19); // 성공하는 마지막 정수 난수 시드 조회
        int failureSeed = FindSeedForRoll(20); // 실패하는 첫 정수 난수 시드 조회
        CardData attack = ScriptableObject.CreateInstance<CardData>(); // 원본 확률과 무관한 고정 비용 검사 공격 생성
        try // 검사 중 실패해도 독립 카드 정리 보장
        { // 실제 처치 카드 검사 시작
            JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"TEST_RIFT_DUST\",\"displayName\":\"검사 공격\",\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"apCost\":1,\"effectValue\":10}}", attack); // 행동력 한 점과 단일 적 고정 피해 설정
            using (var fight = new Fight(new[] { attack, attack, attack, attack })) // 실제 카드 네 장과 생존 적 명단 준비
            using (var events = new BattleEventDispatcher()) // 전투 장면과 같은 이벤트 발행기 준비
            using (var bridge = new BattleEventController(events, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 실제 카드 입력과 피해 및 처치 이벤트 연결
            using (var relics = BindRelics(fight, events)) // 전투 장면과 같은 초기 상태 보정과 유물 연결
            { // 행동력 확률과 중첩 검사 시작
                yield return CastWithSeed(fight, attack, 3, successSeed); // 피해만 주는 실제 카드 사용
                Assert.IsFalse(fight.Enemies[3].IsDead); // 죽지 않은 적 공격 확인
                Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 처치가 없을 때 비용만 차감
                yield return CastWithSeed(fight, attack, 0, successSeed, fatal: true); // 난수 열아홉으로 실제 적 처치
                Assert.IsTrue(fight.Enemies[0].IsDead); // 첫 카드의 실제 적 처치 확인
                Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 비용 한 점과 가루병·단도 두 점이 함께 반영된 행동력
                yield return CastWithSeed(fight, attack, 1, failureSeed, fatal: true); // 난수 스물로 두 번째 실제 적 처치
                Assert.IsTrue(fight.Enemies[1].IsDead); // 확률에 실패해도 실제 적 처치 확인
                Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 난수 스물에서 가루병 회복 없음 확인
                yield return CastWithSeed(fight, attack, 2, successSeed, fatal: true); // 같은 턴에 다시 난수 열아홉으로 적 처치
                Assert.IsTrue(fight.Enemies[2].IsDead); // 다시 성공한 처치의 실제 사망 확인
                Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 단도 소모 뒤에도 가루병이 독립적으로 한 점 회복
                Assert.AreEqual(20, fight.Points.MaxActionPoints); // 처치 회복으로 최대 행동력 변경 방지
            } // 행동력 확률과 중첩 검사 종료
            using (var fight = new Fight(new[] { attack })) // 같은 회차 보유 유물로 별도 전투 구성
            using (var events = new BattleEventDispatcher()) // 새 전투 이벤트 발행기 준비
            using (var bridge = new BattleEventController(events, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 새 전투의 실제 사망과 턴 신호 연결
            using (var relics = BindRelics(fight, events)) // 새 전투 유물의 초기 상태 연결
            { // 상한과 유효하지 않은 처치 검사 시작
                Random.InitState(successSeed); // 상한이 가득 찬 처치의 성공 확률 준비
                fight.Enemies[0].TakeDamage(500, BattleDamageType.None, fight.Allies[0]); // 다른 행동력 결제 없이 실제 아군 처치 발생
                Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 상한에서 두 유물의 회복량을 모두 제한
                Assert.AreEqual(20, fight.Points.MaxActionPoints); // 처치가 행동력 상한을 늘리지 않는지 확인
                Assert.IsTrue(fight.Points.Spend(2)); // 무효한 아군 턴 처치의 회복 여지 준비
                Random.InitState(successSeed); // 공격자 없는 처치에서 소비하면 안 되는 난수 시드 설정
                fight.Enemies[1].TakeDamage(500, BattleDamageType.None); // 출처를 가진 아군이 없는 실제 적 처치
                Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 환경 처치의 회복 제외 확인
                Assert.AreEqual(19, Random.Range(0, 100)); // 환경 처치가 확률 추첨을 소비하지 않는지 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴으로 전환
                Random.InitState(successSeed); // 적 턴 처치에서 소비하면 안 되는 난수 시드 설정
                fight.Enemies[2].TakeDamage(500, BattleDamageType.None, fight.Allies[0]); // 반사처럼 적 턴에 아군 출처로 적 사망 처리
                Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 적 턴에 아군이 적을 쓰러뜨려도 회복 제외
                Assert.AreEqual(19, Random.Range(0, 100)); // 적 턴 처치가 확률 추첨을 소비하지 않는지 확인
            } // 상한과 유효하지 않은 처치 검사 종료
        } // 실제 처치 카드 검사 종료
        finally // 검사 원본 정리 보장
        { // 일회성 카드 정리 시작
            Object.Destroy(attack); // 실제 입력 검사에 사용한 임시 카드 원본 제거
        } // 일회성 카드 정리 종료
    } // 적 처치 행동력 검사 종료

    private static IEnumerator CheckLooseBandolier() // 두 진입 방식에서만 실제 첫 아군 턴 행동력 증가 확인
    { // 첫 아군 턴 유물 검사 시작
        RelicData bandolier = Hold("LooseBandolier"); // 실제 등록한 헐거운 탄띠 보유
        Assert.AreEqual(RelicTriggerType.TurnStarted, bandolier.TriggerType); // 진영별 턴 시작 신호의 발동 시점 확인
        Assert.AreEqual(RelicEffectType.GainFirstTurnCapacity, bandolier.EffectType); // 기존 첫 아군 턴의 임시 상한 경로 확인
        Assert.AreEqual(100, bandolier.TriggerChancePercent); // 첫 아군 턴 확정 지급 확인
        Assert.AreEqual(1, bandolier.EffectValue); // 최대와 현재 행동력의 실제 한 점 수치 확인
        Assert.AreEqual(1, bandolier.MaximumTriggersPerBattle); // 전투당 한 번 지급 확인
        Hold("BrokenMedal"); // 같은 발동 방식의 기존 첫 턴 유물 보유
        Hold("TornEnvelope"); // 첫 검 카드 손패의 기존 별개 행동력 상한 유물 보유
        foreach (bool enemyFirst in new[] { false, true }) // 일반 전투와 적 선제 전투 비교
        { // 두 진입 방식 검사 시작
            using var fight = new Fight(enemyFirst, "CRITICAL_STRIKE"); // 첫 손패의 실제 검 카드와 지정 진영 선제로 전투 구성
            using var events = new BattleEventDispatcher(); // 실제 전투와 같은 발행기 준비
            using var bridge = new BattleEventController(events, fight.Turns, fight.Controller, fight.Allies, fight.Enemies); // 실제 전투와 같은 진영별 턴 시작 연결
            using var relics = BindRelics(fight, events); // 실제 부트스트랩과 같은 기존 현재 턴 초기 상태 보정
            if (enemyFirst) // 아군 턴 이전의 적 선제 확인
            { // 적 선제 처리 검사 시작
                Assert.AreEqual(BattleTurnPhase.EnemyTurn, fight.Turns.CurrentPhase); // 실제 첫 적 턴에서 먼저 시작
                Assert.AreEqual(20, fight.Points.MaxActionPoints); // 선제 중에는 탄띠와 기존 유물의 상한 증가 없음
                Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 선제 중에는 아군 행동력 지급 없음
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 선제를 끝내고 실제 첫 아군 턴 시작
            } // 적 선제 처리 검사 종료
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 첫 실제 아군 턴 확인
            Assert.AreEqual(1, fight.Turns.CurrentRound); // 적 선제 뒤에도 첫 아군 턴 라운드 유지
            Assert.AreEqual(23, fight.Points.MaxActionPoints); // 탄띠와 부서진 메달과 우편봉투의 임시 상한 합산
            Assert.AreEqual(23, fight.Points.CurrentActionPoints); // 같은 세 효과의 현재 행동력까지 함께 지급
            StringAssert.Contains("23/23", ActionPointLabel(fight)); // 실제 행동력 표시의 첫 턴 수치 확인
            int cost = fight.View("CRITICAL_STRIKE").RuntimeCard.ApCost; // 손패에서 실제 결제할 검 카드 비용 조회
            yield return fight.Use("CRITICAL_STRIKE", 0); // 실제 손패 클릭과 적 선택으로 추가 행동력 사용
            Assert.AreEqual(23 - cost, fight.Points.CurrentActionPoints); // 첫 턴의 증가된 공용 행동력에서 정상 비용 차감 확인
            if (enemyFirst) // 적 선제 사례에서 이후 정상 라운드까지 진행
            { // 다음 턴 복원 검사 시작
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 임시 상한을 주는 아군 턴 종료
                Assert.AreEqual(20, fight.Points.MaxActionPoints); // 적 턴으로 전환 시 임시 상한 모두 회수
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 정상 다음 아군 턴까지 실제로 진행
                Assert.AreEqual(2, fight.Turns.CurrentRound); // 이후 아군 턴 라운드 증가 확인
                Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 다음 아군 턴에는 기존 행동력까지만 회복
                StringAssert.Contains("20/20", ActionPointLabel(fight)); // 이후 실제 행동력 상한과 현재 수치 표시
            } // 다음 턴 복원 검사 종료
            else // 일반 시작 전투를 첫 턴 내에 종료하는 경우
            { // 전투 종료 복원 검사 시작
                Assert.IsTrue(fight.Turns.TryEscape()); // 임시 상한이 살아 있는 상태에서 실제 전투 종료
                Assert.AreEqual(20, fight.Points.MaxActionPoints); // 첫 턴에 종료해도 영구 상한 증가 방지
                Assert.LessOrEqual(fight.Points.CurrentActionPoints, 20); // 종료된 전투의 현재 행동력도 기본 상한 이내 확인
            } // 전투 종료 복원 검사 종료
        } // 두 진입 방식 검사 종료
    } // 첫 아군 턴 유물 검사 종료

    private static IEnumerator CastWithSeed(Fight fight, CardData source, int targetIndex, int seed, bool fatal = false) // 실제 카드 입력과 처치 직전의 확률 시드 제어
    { // 실제 카드 타격 준비 시작
        BattleUnitRuntime target = fight.Enemies[targetIndex]; // 피해와 처치 대상의 실제 전투 유닛 조회
        if (fatal) // 이번 카드로 실제 적을 쓰러뜨릴지 확인
        { // 처치 직전 체력 준비 시작
            target.TakeDamage(target.CurrentHealth - 1, BattleDamageType.None); // 다른 처치를 일으키지 않고 적 체력 한 점만 남김
        } // 처치 직전 체력 준비 종료
        var handCard = fight.Hand.GetComponentsInChildren<BattleCardView>().First(view => view.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(view.RuntimeCard)); // 실제로 선택 가능한 손패 카드 조회
        CardInstance used = handCard.RuntimeCard; // 사용 후 이동을 확인할 카드 보관
        void SeedOnImpact(BattleUnitRuntime attacker, BattleUnitRuntime victim, BattleDamageResult damage) // 피해 판정이 끝나고 처치 유물 판정 전에 실행할 시드 연결
        { // 처치 추첨의 시드 제어 시작
            Random.InitState(seed); // 실제 카드 명중 계산과 독립된 처치 추첨 시작 상태 설정
        } // 처치 추첨의 시드 제어 종료
        target.DamageResolved += SeedOnImpact; // 피해 신호 이후와 처치 신호 이전의 실제 확률 설정
        try // 실제 적 처치와 행동력 정산 대기
        { // 실제 카드 입력 시작
            handCard.OnPointerClick(null); // 정상 손패 클릭
            fight.EnemyViews[targetIndex].OnPointerClick(null); // 실제 전투 대상 클릭
            float deadline = Time.realtimeSinceStartup + 10f; // 실제 시간 기준 카드 충돌 대기 한도
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 카드 실제 행동과 연출 종료 확인
            { // 다음 연출 프레임 대기 시작
                yield return null; // 다음 프레임 대기
            } // 다음 연출 프레임 대기 종료
            Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 충돌 연출 완료 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 유효한 카드 입력과 정상 카드 이동 확인
        } // 실제 카드 입력 종료
        finally // 검사 타격의 시드 연결 정리 보장
        { // 시드 등록 해제 시작
            target.DamageResolved -= SeedOnImpact; // 다른 타격의 무작위 상태에 영향 방지
        } // 시드 등록 해제 종료
    } // 실제 카드 타격 검사 종료

    private static int FindSeedForRoll(int roll) // 실제 엔진의 정수 난수 경계 시드 탐색
    { // 경계 시드 탐색 시작
        Random.State prior = Random.state; // 검사 전 전역 난수 보관
        try // 검사 범위의 탐색과 기존 난수 보호
        { // 난수 경계 비교 시작
            for (int seed = 0; seed < 10000; seed++) // 비교 시드의 충분한 범위 순회
            { // 후보 난수 검사 시작
                Random.InitState(seed); // 해당 후보 난수로 무작위 시작
                if (Random.Range(0, 100) == roll) // 정확한 발동 경계와 비교
                { // 정확한 후보 발견 시작
                    return seed; // 실제 엔진에서 해당 값을 생성하는 시드 반환
                } // 정확한 후보 발견 종료
            } // 후보 난수 검사 종료
            throw new System.InvalidOperationException($"테스트 난수 {roll}에 해당하는 시드 없음"); // 예기치 않은 난수 범위 변경 확인
        } // 난수 경계 비교 종료
        finally // 다음 검사와 실제 난수 상태 보존
        { // 이전 난수 복원 시작
            Random.state = prior; // 탐색으로 자체 무작위 순서 변경 방지
        } // 이전 난수 복원 종료
    } // 경계 시드 탐색 종료

    private static RelicData Hold(string name) // 실제 사전 등록한 유물의 회차 획득
    { // 유물 보유 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 게임에서 쓰는 실제 유물 원본 조회
        Assert.IsNotNull(relic); // 검사 대상 유물 등록 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 일반 유물 획득 방식과 동일한 보유 확인
        return relic; // 실제 발동 설정 검사에 사용할 원본 반환
    } // 유물 보유 준비 종료

    private static BattleRelicEffectController BindRelics(Fight fight, BattleEventDispatcher events) // 실전 초기화 순서대로 유물 구독과 첫 턴 보정
    { // 전투 유물 연결 시작
        var run = RelicRunManager.Instance; // 현재 실제 보유 유물 관리자 조회
        var relics = new BattleRelicEffectController(events, run.Inventory, run.Gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points); // 게임과 같은 공용 이벤트와 행동력 지갑 연결
        relics.ProcessInitialBattleState(fight.Turns.CurrentPhase, fight.Turns.CurrentRound); // 실제 부트스트랩의 연결 전 첫 상태 보정
        return relics; // 해당 전투 범위가 끝날 때 정리할 유물 연결 반환
    } // 전투 유물 연결 종료

    private static string ActionPointLabel(Fight fight) // 실제 전투 화면의 공용 행동력 수치 확인
    { // 행동력 표시 조회 시작
        return fight.Hand.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == "ActionPointText").text; // 공용 행동력 배지의 실제 화면 문자 반환
    } // 행동력 표시 조회 종료
} // 행동력 유물 테스트 종료
