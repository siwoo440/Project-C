using System; // 검사에서 쓸 빈 적 목록
using System.Linq; // 출전 인물과 확률 시드 조회
using NUnit.Framework; // 시작 정화와 재판정 검증
using UnityEngine; // 실제 리소스와 전투 난수 저장

public sealed class WinterfallStartingCleanseRelicTests // 식은 향 주머니의 전투 시작 정화 검사
{ // 정화 유물 검사 시작
    [TestCase(true, false)] // 실제 전투 시작 이벤트에서 성공한 확률 판정
    [TestCase(false, true)] // 기존 초기 상태 보정에서 실패한 확률 판정
    public void RollsOnlyOnceAndCleansesOneLivingAllyDebuff(bool wins, bool initialFallback) // 확률 경계와 실제 정화 범위 및 중복 방지 검증
    { // 한 번만 정화하는 유물 검사 시작
        var relic = Resources.Load<RelicData>("Relics/ColdIncensePouch"); // 상점과 저장에 등록된 실제 식은 향 주머니
        Assert.IsNotNull(relic); // 실제 게임 원본 등록 확인
        Assert.AreEqual(25, relic.TriggerChancePercent); // 원본 시트의 실제 정화 확률 확인
        var wallet = new RelicGoldRuntime(); // 유물 보관과 효과에서 함께 사용하는 독립 지갑
        var inventory = new RelicInventoryRuntime(wallet); // 독립 전투의 평소 보유 유물 관리자
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 원본 유물 획득 확인
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 원본 캐릭터 명단
        var allies = roster.Characters.Take(2).Select(character => BattleUnitRuntime.CreateAlly(character)).ToArray(); // 두 실제 생존 아군의 전투 상태
        allies[0].ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1); // 첫 생존 아군의 실제 출혈
        allies[1].ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 2, 1); // 둘째 생존 아군의 실제 화상
        allies[0].ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 2, 1); // 정화 대상과 구분할 기존 강화
        using var events = new BattleEventDispatcher(); // 실제 전투 시작 이벤트 발행기
        using var controller = new BattleRelicEffectController(events, inventory, wallet, allies, Array.Empty<BattleUnitRuntime>()); // 실제 유물 발동 관리자를 두 아군에 연결
        var random = UnityEngine.Random.state; // 다른 검사에서 쓰는 전역 난수 상태 보존
        try // 전역 난수 복구와 실제 적용 결과 검사
        { // 시작 신호 정화 확인 시작
            UnityEngine.Random.InitState(FindSeed(wins)); // 원하는 시작 판정 값을 재현하는 실제 난수 시드
            if (initialFallback) // 실제 시작 이벤트에 늦게 연결한 경우 확인
            { // 현재 전투의 최초 상태 보정 시작
                controller.ProcessInitialBattleState(BattleTurnPhase.PlayerTurn, 1); // 실제 전투 시작 보정 경로의 유물 판정
            } // 현재 전투의 최초 상태 보정 종료
            else // 정상 생성 시 전투 시작 이벤트 연결 경로 확인
            { // 실제 첫 전투 시작 시작
                events.Publish(new BattleEventContext(BattleEventType.BattleStarted, 1, BattleTurnPhase.PlayerTurn)); // 실제 공용 이벤트로 최초 정화 판정
            } // 실제 첫 전투 시작 종료
            int expected = wins ? 1 : 2; // 성공의 약화 하나 제거 및 실패의 기존 상태 보존
            Assert.AreEqual(expected, allies.Sum(ally => ally.StatusEffects.Count(effect => effect.IsDebuff))); // 파티 전체에서 발생한 단 한 가지 정화 비교
            Assert.IsNotNull(allies[0].GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 일반 강화 보존 확인
            UnityEngine.Random.InitState(FindSeed(true)); // 두 번째 이벤트가 잘못 추첨하는 경우 성공할 시드
            events.Publish(new BattleEventContext(BattleEventType.BattleStarted, 1, BattleTurnPhase.PlayerTurn)); // 같은 전투의 중복 시작 신호 재현
            Assert.AreEqual(expected, allies.Sum(ally => ally.StatusEffects.Count(effect => effect.IsDebuff))); // 성공 또는 실패한 최초 판정과 무관한 추가 발동 차단
        } // 시작 신호 정화 확인 종료
        finally // 외부 검사 결과 보호를 위한 난수 상태 복구
        { // 전역 난수 복구 시작
            UnityEngine.Random.state = random; // 이전 Unity 검사 난수 상태 복원
        } // 전역 난수 복구 종료
    } // 한 번만 정화하는 유물 검사 종료

    private static int FindSeed(bool wins) // 원하는 실제 25% 범위의 첫 추첨 시드 조회
    { // 확률 시드 탐색 시작
        return Enumerable.Range(0, 200).First(seed => // 넉넉한 고정 범위의 실제 난수 시드 순회
        { // 전투 난수 검사 시작
            UnityEngine.Random.InitState(seed); // 세부 값을 재현할 Unity 난수 초기화
            return (UnityEngine.Random.Range(0, 100) < 25) == wins; // 원본 유물의 시작 판정 기준 대조
        }); // 전투 난수 검사 종료
    } // 확률 시드 탐색 종료
} // 정화 유물 검사 종료
