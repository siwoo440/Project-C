using System; // 카드와 유물 검사 자원 정리 인터페이스
using System.Collections; // 실제 전투 진행을 기다리는 코루틴
using System.Collections.Generic; // 검사에 만든 카드 원본 목록
using System.Linq; // 실제 손패에서 원본이 같은 카드 찾기
using NUnit.Framework; // 실제 피해와 손패 상태 확인
using UnityEngine; // 카드 원본 생성과 플레이 시간 조회
using UnityEngine.TestTools; // 플레이 모드 테스트 순서
using UnityEngine.UI; // 실제 필수 카드 선택 버튼 입력
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투와 입력 검사 환경
using Object = UnityEngine.Object; // 독립 테스트 자원 제거

public sealed class DeeperRiftCardRelicTests // 깊은 균열의 카드 사용 유물 실전 검사
{ // 카드 유물 검사 시작
    [UnityTest] // 실제 검 명중과 중독의 독립 처리 확인
    public IEnumerator ClawDamageRequiresARealSwordHitOnAnActivelyPoisonedEnemy() // 명중 가산의 방어 및 중독 틱 분리 확인
    { // 네크레이트 발톱 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 충돌을 처리할 플레이 모드 시작
        yield return CheckClawDamage(); // 카드 타격과 적 턴의 독립 중독 확인
        yield return new ExitPlayMode(); // 플레이 모드 검사 종료
    } // 네크레이트 발톱 검사 종료

    private static IEnumerator CheckClawDamage() // 중독과 방어도 및 빗나감의 체력 차이 확인
    { // 실제 피해 대조 시작
        using var setup = new Fixture("NecrateClawDust"); // 실제 발톱 가루 원본 획득
        var sword = setup.Create("sword", CardType.Sword, CardEffectType.Damage, CardTargetType.SingleEnemy, 5); // 고정 다섯 피해 검 카드
        using var fight = new Fight(new[] { sword, sword, sword }); // 같은 원본의 실제 타격 세 장 준비
        var poisoned = fight.Enemies[0]; // 방어 뒤 실제 피해가 드러날 중독 적
        var normal = fight.Enemies[1]; // 중독이 없는 기본 피해 대조 적
        var dodging = fight.Enemies[2]; // 중독 상태에서 타격을 피할 적
        poisoned.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 1, 1, fight.Allies[0]); // 방어를 가진 적에게 실제 중독 적용
        poisoned.GainBlock(6); // 다섯 기본 피해만으로 넘지 못하는 방어도
        dodging.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 1, 1, fight.Allies[0]); // 빗나감과 중독 틱 대조용 중독
        dodging.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 직접 카드가 확정 빗나갈 회피
        yield return Use(fight, sword, 0); // 방어를 가진 중독 적에게 실제 검 카드 사용
        Assert.AreEqual(198, poisoned.CurrentHealth); // 기본 다섯과 가산 셋을 합친 뒤 방어 여섯 차감 확인
        Assert.AreEqual(0, poisoned.CurrentBlock); // 추가 타격이 실제 방어도를 통과했는지 확인
        yield return Use(fight, sword, 1); // 중독 없는 적에게 같은 원본 사용
        Assert.AreEqual(195, normal.CurrentHealth); // 중독 없는 적의 기본 피해 보존
        yield return Use(fight, sword, 2); // 회피하는 중독 적에게 같은 원본 사용
        Assert.AreEqual(200, dodging.CurrentHealth); // 빗나간 검의 가산과 기본 피해 제외
        AdvanceRound(fight); // 정상 적 턴 종료에서 실제 중독 피해 처리
        Assert.AreEqual(198, dodging.CurrentHealth); // 중독 자체의 두 피해에 발톱 가산 미적용
    } // 실제 피해 대조 종료

    [UnityTest] // 속성별 성공한 카드와 필수 선택 뒤 보상 검사
    public IEnumerator CrestCountsShieldAndWaitsForTheThirdCardsChoiceOncePerTurn() // 방패를 포함하는 서로 다른 속성 세 개와 턴 제한 검증
    { // 역별 문장 검사 시작
        yield return new EnterPlayMode(); // 카드 선택 창과 실제 턴의 플레이 모드 시작
        yield return CheckCrestDraw(); // 필수 선택 뒤 한 장과 매 턴 초기화 확인
        yield return new ExitPlayMode(); // 플레이 모드 검사 종료
    } // 역별 문장 검사 종료

    private static IEnumerator CheckCrestDraw() // 속성 중복과 선택 대기 및 새 턴 보상의 실제 손패 검사
    { // 서로 다른 카드 속성 대조 시작
        using var setup = new Fixture("InvertedStarCrest"); // 실제 역별 문장 원본 획득
        var sword = setup.Create("sword", CardType.Sword, CardEffectType.Damage, CardTargetType.SingleEnemy, 5); // 바보 소유의 첫 검 속성
        var wand = setup.Create("wand", CardType.Wand, CardEffectType.Damage, CardTargetType.SingleEnemy, 5); // 같은 소유자의 둘째 지팡이 속성
        var shield = setup.Create("shield", CardType.Shield, CardEffectType.DrawThenDiscard, CardTargetType.Self, 1); // 드로우 뒤 버릴 후보를 고르는 셋째 방패 속성
        var cup = setup.Create("cup", CardType.Cup, CardEffectType.Heal, CardTargetType.Self, 2); // 실제 체력이 가득 차도 사용한 성배 속성
        var pentacle = setup.Create("pentacle", CardType.Pentacle, CardEffectType.GainBlock, CardTargetType.Self, 2); // 다음 턴에 먼저 기록할 펜타클 속성
        var curse = setup.Create("curse", CardType.Curse, CardEffectType.Unplayable, CardTargetType.Self, 0); // 사용 불가한 사건 카드
        using var fight = new Fight(new[] { sword, sword, wand, shield, cup, pentacle, curse }); // 모든 카드가 같은 아르카나 소유인 실제 전투
        int startingHand = fight.Deck.HandCount; // 원래 손패와 기본 드로우의 비교 수
        yield return Use(fight, sword, 0); // 첫 실제 검 속성 기록
        yield return Use(fight, sword, 0); // 같은 속성 두 번째 사용
        yield return Use(fight, wand, 0); // 두 번째 서로 다른 속성 기록
        Assert.AreEqual(startingHand - 3, fight.Deck.HandCount); // 중복 검을 별도 속성으로 세지 않았는지 확인
        View(fight, curse).OnPointerClick(null); // 실제 사용 불가 저주 카드 선택 시도
        Assert.IsNull(fight.Controller.SelectedCard); // 사용할 수 없는 저주의 선택 차단 확인
        yield return Use(fight, shield); // 방패 속성의 실제 사용과 기본 카드 한 장 드로우
        var choice = Object.FindFirstObjectByType<BattleDrawChoiceView>(); // 반드시 완료해야 하는 실제 손패 후보 창
        Assert.IsNotNull(choice); // 방패 사용 뒤 버릴 카드 선택 대기 확인
        Assert.AreEqual(startingHand - 3, fight.Deck.HandCount); // 기본 카드만 뽑고 문장 보상은 대기하는 손패 확인
        choice.transform.Find("Choice_0").GetComponent<Button>().onClick.Invoke(); // 실제 후보 첫 카드의 버림 결정
        Assert.AreEqual(startingHand - 3, fight.Deck.HandCount); // 선택으로 빈 자리에 문장이 정확히 한 장 보충
        var nextSuit = fight.Deck.Hand.Any(card => card.SourceData == cup) ? cup : pentacle; // 의무 선택에서 남은 네 번째 속성 확인
        yield return Use(fight, nextSuit); // 같은 턴의 다른 속성 한 번 추가
        Assert.AreEqual(startingHand - 4, fight.Deck.HandCount); // 네 번째 속성으로 문장 재발동이 없는지 확인
        AdvanceRound(fight); // 다음 실제 아군 턴과 카드 덱 복원
        int nextHand = fight.Deck.HandCount; // 새로운 속성 주기의 시작 손패 수
        yield return Use(fight, cup); // 새 턴 첫 번째 속성 기록
        yield return Use(fight, pentacle); // 새 턴 두 번째 속성 기록
        Assert.AreEqual(nextHand - 2, fight.Deck.HandCount); // 이전 턴의 속성 집계가 새 턴에 섞이지 않는지 확인
        yield return Use(fight, wand, 0); // 새 턴의 정확한 세 번째 서로 다른 속성 사용
        Assert.AreEqual(nextHand - 2, fight.Deck.HandCount); // 새 턴 보상이 다시 한 장 지급되는지 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 드로우와 선택 뒤 모든 카드 위치의 무결성 확인
    } // 서로 다른 카드 속성 대조 종료

    [UnityTest] // 배틀 전체 지팡이 사용과 생존 적 광역 처리 검사
    public IEnumerator ResonanceCountsAcrossTurnsAndDamagesLivingEnemiesEveryThirdWand() // 취소 제외와 다음 턴의 모든 생존자 피해 검증
    { // 심연 공명석 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 진행용 플레이 모드 시작
        yield return CheckWandPulse(); // 두 번의 공명 주기와 일반 방어 피해 확인
        yield return new ExitPlayMode(); // 플레이 모드 검사 종료
    } // 심연 공명석 검사 종료

    private static IEnumerator CheckWandPulse() // 취소와 빗나감 및 반복 광역을 실제 카드로 대조
    { // 지팡이 반복 검사 시작
        using var setup = new Fixture("AbyssResonanceStone"); // 실제 심연 공명석 원본 획득
        var wand = setup.Create("wand", CardType.Wand, CardEffectType.Damage, CardTargetType.SingleEnemy, 5); // 명중하면 기본 다섯 피해인 지팡이 원본
        using var fight = new Fight(new[] { wand, wand, wand, wand, wand }); // 두 턴에 반복 사용 가능한 지팡이 손패
        for (int use = 0; use < 2; use++) // 첫 턴에 정확히 지팡이 두 번 사용
        { // 실제 첫 두 지팡이 입력 시작
            yield return Use(fight, wand, 0); // 살아 있는 같은 대상을 선택해 카드 정산
        } // 실제 첫 두 지팡이 입력 종료
        View(fight, wand).OnPointerClick(null); // 대상 선택만 시작하고 기다리는 실제 입력
        Assert.IsNotNull(fight.Controller.SelectedCard); // 취소할 세 번째 선택 존재 확인
        fight.Controller.CancelSelection(); // 비용 및 카드 이동 전의 실제 대상 취소
        Assert.AreEqual(200, fight.Enemies[1].CurrentHealth); // 두 번 사용과 하나 취소 뒤 공명 없음 확인
        AdvanceRound(fight); // 전투는 계속하고 실제 아군 턴만 전환
        fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 사용은 성공하고 기본 타격만 빗나가도록 설정
        fight.Enemies[1].GainBlock(4); // 독립 공명도 일반 방어도를 거치는지 확인할 보호
        fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 카드 명중을 거치지 않는 공명 대조
        fight.Enemies[3].TakeDamage(200, BattleDamageType.None); // 이미 쓰러진 적은 광역 살아 있는 대상에서 제거
        yield return Use(fight, wand, 0); // 다음 턴의 실제 세 번째 카드 이동과 공명 해결
        Assert.AreEqual(184, fight.Enemies[0].CurrentHealth); // 이전 기본 두 타격 뒤 이번 빗나감과 광역 여섯만 적용
        Assert.AreEqual(198, fight.Enemies[1].CurrentHealth); // 공명 여섯에 방어도 넷을 처리한 체력 피해 둘
        Assert.AreEqual(194, fight.Enemies[2].CurrentHealth); // 높은 회피를 굴리지 않는 일반 유물 여섯 피해
        Assert.AreEqual(0, fight.Enemies[3].CurrentHealth); // 이미 쓰러진 적 체력에 추가 영향 없음
        yield return Use(fight, wand, 0); // 직전 공명 다음 첫 번째 지팡이 사용
        yield return Use(fight, wand, 0); // 직전 공명 다음 두 번째 지팡이 사용
        Assert.AreEqual(194, fight.Enemies[2].CurrentHealth); // 독립 광역이 자신을 다시 집계하지 않는지 확인
        yield return Use(fight, wand, 0); // 전투 누적 여섯 번째 지팡이의 두 번째 광역
        Assert.AreEqual(188, fight.Enemies[2].CurrentHealth); // 전투 두 번째 주기에서 생존 적에게 여섯 피해
    } // 지팡이 반복 검사 종료

    private static BattleCardView View(Fight fight, CardData source) // 실제 손패에서 지정 원본의 활성 카드 검색
    { // 손패 보기 조회 시작
        return fight.Hand.GetComponentsInChildren<BattleCardView>().First(view => view.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(view.RuntimeCard)); // 버린 카드와 폐기 대기 화면 제외
    } // 손패 보기 조회 종료

    private static IEnumerator Use(Fight fight, CardData source, int enemy = -1) // 실제 손패 클릭과 선택 대상의 효과 완료 대기
    { // 실제 카드 입력 시작
        int before = fight.Points.CurrentActionPoints; // 정상 비용을 비교할 시작 행동력
        View(fight, source).OnPointerClick(null); // 실제 전투 손패 카드 클릭
        if (enemy >= 0) // 직접 선택할 적 위치 확인
        { // 적 대상 클릭 시작
            fight.EnemyViews[enemy].OnPointerClick(null); // 실제 적 보기를 통한 선택 확정
        } // 적 대상 클릭 종료
        float deadline = Time.realtimeSinceStartup + 10f; // 연출 정산의 실제 시간 한도
        while ((fight.Runner.IsBusy || fight.Points.CurrentActionPoints == before) && Time.realtimeSinceStartup < deadline) // 충돌과 최종 카드 이동까지 대기
        { // 다음 게임 프레임 필요 시작
            yield return null; // 실제 연출 실행 기회
        } // 다음 게임 프레임 필요 종료
        Assert.AreEqual(before - 1, fight.Points.CurrentActionPoints); // 제대로 사용한 카드 한 장의 실제 비용 확인
        Assert.IsFalse(fight.Runner.IsBusy); // 카드 이동 뒤 최종 정산 완료 확인
    } // 실제 카드 입력 종료

    private static void AdvanceRound(Fight fight) // 실제 적 턴을 지나 다음 아군 턴 시작
    { // 실제 라운드 전환 시작
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 아군 손패 정리와 실제 적 턴 시작
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 상태 정산과 다음 아군 덱 준비
    } // 실제 라운드 전환 종료

    private sealed class Fixture : IDisposable // 실제 유물과 테스트 카드 자원 정리
    { // 임시 자원 관리 시작
        private readonly List<CardData> sources = new List<CardData>(); // 독립 테스트에 만든 카드 원본 목록
        private readonly RelicRunManager manager; // 실제 카드 유물 조회에 사용할 회차 보관함
        private readonly RelicRunManager previous; // 기존 회차 관리자 확인용 참조
        private readonly RelicData relic; // 이번 실제 전투에 지급한 유물 원본

        internal Fixture(string name) // 요청한 실제 유물 하나를 전투에서 사용
        { // 원본 유물 준비 시작
            previous = RelicRunManager.Instance; // 플레이 시작 전 회차 상태 보관
            manager = RelicRunManager.EnsureInstance(); // 실제 회차 유물 보관함 준비
            relic = Resources.Load<RelicData>("Relics/" + name); // 유물 효과 수치의 실제 원본 조회
            Assert.IsNotNull(relic, name); // 원본 데이터와 파일 이름 연결 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 실제 회차에서 검사 유물 획득
        } // 원본 유물 준비 종료

        internal CardData Create(string id, CardType type, CardEffectType effect, CardTargetType target, int value) // 수치가 정해진 실제 사용 카드 원본 생성
        { // 검사 원본 생성 시작
            var card = ScriptableObject.CreateInstance<CardData>(); // 일반 원본과 동일한 카드 데이터 객체
            sources.Add(card); // 테스트 뒤 없앨 원본 참조 저장
            JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"DAY258_{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)type},\"effectType\":{(int)effect},\"targetType\":{(int)target},\"apCost\":1,\"effectValue\":{value}}}", card); // 실제 사용 경로를 위한 속성과 대상 및 효과 직렬화
            return card; // 기존 전투에 지급할 고정 카드 원본 반환
        } // 검사 원본 생성 종료

        public void Dispose() // 테스트 이전 유물 관리자와 카드 원본 상태 복원
        { // 전투 자원 정리 시작
            foreach (var source in sources) // 생성한 독립 카드 원본 순회
            { // 원본 제거 시작
                Object.Destroy(source); // 임시 카드 원본 삭제
            } // 원본 제거 종료
            manager.TryRemove(relic.RelicId); // 이번 검사에서 넣은 유물만 정리
            if (previous == null) // 없어도 만들었던 실제 보관함인지 확인
            { // 새 회차 관리자 제거 시작
                Object.Destroy(manager.gameObject); // 임시로 만든 관리자 제거
            } // 새 회차 관리자 제거 종료
        } // 전투 자원 정리 종료
    } // 임시 자원 관리 종료
} // 카드 유물 검사 종료
