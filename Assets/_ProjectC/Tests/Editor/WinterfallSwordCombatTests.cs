using System; // 검사 객체 정리 사용
using System.Collections; // 실제 카드 연출 대기
using System.Collections.Generic; // 타격 순서 기록
using System.Linq; // 실제 카드와 유닛 화면 조회
using NUnit.Framework; // 검증 결과 비교
using UnityEngine; // 전투 자산과 화면 사용
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 손패 화면 사용
using Object = UnityEngine.Object; // 유니티 객체 정리
using Random = UnityEngine.Random; // 전투 난수 재현
using static WinterfallCommonCardExpansionTests; // 기존 손패 클릭과 전투 화면 생성 재사용

public sealed class WinterfallSwordCombatTests // 검 카드 여덟 장의 실제 사용 검사
{ // 검사 시작
    internal sealed class Fight : IDisposable // 실제 전투를 독립 구성하는 검사 환경
    { // 전투 환경 시작
        internal readonly BattleUnitRuntime[] Allies; // 카드 소유자와 동료 목록
        internal readonly BattleUnitRuntime[] Enemies; // 실제 위치를 유지한 적 네 명
        internal readonly BattleUnitView[] EnemyViews; // 위치별 적 클릭 화면
        internal readonly BattleDeckRuntime Deck; // 보물 원본으로 만든 전투 덱
        internal readonly BattleActionPointRuntime Points = new BattleActionPointRuntime(20); // 턴당 검사 행동력
        internal readonly BattleTurnRuntime Turns; // 아군과 적의 실제 턴 전환
        internal readonly BattleCardActionController Controller; // 실제 카드 입력 처리
        internal readonly BattleHandView Hand; // 실제 손패 화면
        internal readonly BattleActionSequenceRunner Runner; // 실제 타격 연출
        private readonly Random.State randomState = Random.state; // 검사 전 게임 난수 보관
        private readonly GameObject canvas; // 독립 전투 화면
        private readonly EnemyData enemyData; // 방어력 영인 검사 적 원본
        private readonly DeckData source; // 회차 덱 원본
        private readonly BattleEnemyActionRuntime actions; // 적 행동 연결
        private readonly BattleStatusEffectController statuses; // 진영별 지속 효과 처리
        internal Fight(params string[] ids) : this(CreateView, ids) // 기본 검사에는 기존 경량 전투 화면 사용
        { // 기본 전투 환경 시작
        } // 기본 전투 환경 종료
        internal Fight(Func<Transform, BattleUnitRuntime, BattleUnitView> createEnemyView, params string[] ids) // 검사할 실제 적 화면과 보물 원본으로 전투 구성
        { // 전투 구성 시작
            var offers = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers; // 실제 보물 풀 조회
            var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 아군 명단 조회
            Allies = roster.Characters.Take(3).Select(BattleUnitRuntime.CreateAlly).ToArray(); // 생존자와 전투불능 검사 아군 생성
            enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 저항과 약점이 없는 검사 적 생성
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200}", enemyData); // 반복 타격 측정용 체력 설정
            Enemies = Enumerable.Range(0, 4).Select(index => BattleUnitRuntime.CreateEnemy(enemyData)).ToArray(); // 실제 순서의 네 칸 생성
            var cards = ids.Select(id => id.Contains("/") ? Resources.Load<CardData>(id) : offers.Single(offer => offer.OfferId == "REWARD_SHEET_" + id).Card).ToArray(); // 지정한 시작 카드나 보물 카드 원본 조회
            source = DeckData.CreateRuntime(cards.Select(card => new DeckCardEntry(card, Allies[0].CharacterSource)).ToArray()); // 같은 소유자의 독립 비교 덱 생성
            Deck = BattleDeckRuntime.Create(source, Allies, cards.Length, 224); // 손패 순서 고정
            canvas = new GameObject("SwordCombat", typeof(RectTransform), typeof(Canvas)); // 전투 화면 생성
            var item = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 전투 손패 화면 생성
            item.transform.SetParent(canvas.transform, false); // 손패를 전투 화면에 연결
            Hand = item.GetComponent<BattleHandView>(); // 손패 입력 조회
            var allies = Allies.Select(unit => CreateView(canvas.transform, unit)).ToArray(); // 아군 화면 연결
            EnemyViews = Enemies.Select(unit => createEnemyView(canvas.transform, unit)).ToArray(); // 적 화면 연결
            Runner = canvas.AddComponent<BattleActionSequenceRunner>(); // 실제 연출 생성
            var processor = new BattleStatusEffectProcessor(); // 상태 처리기 생성
            Turns = new BattleTurnRuntime(Deck, Points, Allies, Enemies, cards.Length, BattleType.Normal); // 사용한 카드까지 매 턴 다시 뽑는 검사 덱 구성
            actions = new BattleEnemyActionRuntime(Enemies, Allies); // 실제 적 명단 연결
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, processor); // 지속 피해와 지속 시간 연결
            Assert.IsTrue(Turns.StartBattle(cards.Length)); // 손패를 채우고 첫 턴 시작
            Assert.IsTrue(Hand.Bind(Deck, Points, Turns)); // 턴 상태와 손패 입력 연결
            Controller = new BattleCardActionController(Deck, Points, Turns, Hand, Runner, processor, allies, EnemyViews, actions); // 실제 카드 선택과 충돌 처리 연결
        } // 전투 구성 종료
        internal BattleCardView View(string id) // 사용 가능한 손패 화면 조회
        { // 카드 검색 시작
            return Hand.GetComponentsInChildren<BattleCardView>().First(view => view.RuntimeCard?.SourceData.CardId == "CRD_SHEET_" + id && Deck.Hand.Contains(view.RuntimeCard)); // 버린 카드와 파기 대기 화면 제외
        } // 카드 검색 종료
        internal IEnumerator Use(string id, int target = -1) // 실제 손패와 대상 클릭
        { // 카드 사용 시작
            return Play(View(id).RuntimeCard.SourceData, Hand, target < 0 ? null : EnemyViews[target], Runner, Deck, Points); // 비용 차감과 카드 버림까지 확인
        } // 카드 사용 종료
        public void Dispose() // 검사 객체와 게임 난수 복원
        { // 전투 환경 정리 시작
            Controller.Dispose(); // 카드 입력 해제
            Hand.Unbind(); // 손패 연결 해제
            statuses.Dispose(); // 지속 효과 연결 해제
            actions.Dispose(); // 적 행동 연결 해제
            Turns.Dispose(); // 턴 관리자 연결 해제
            Object.Destroy(canvas); // 임시 전투 화면 정리
            Object.Destroy(source); // 임시 덱 정리
            Object.Destroy(enemyData); // 검사 적 정리
            Random.state = randomState; // 검사 전 난수 복원
        } // 전투 환경 정리 종료
    } // 전투 환경 종료

    [Test] // 새로운 상태 두 종류의 도감 등록 검사
    public void StatusCodexShowsArtAndRulesForBothSwordEffects() // 상태 도감의 실제 설명과 아이콘 연동 확인
    { // 도감 검사 시작
        var entries = CodexData.Load(4); // 실제 도감 상태 분류 조회
        foreach (var type in new[] { BattleStatusEffectType.CriticalChanceUp, BattleStatusEffectType.ChargeDefenseDown }) // 새 강화와 약화 순회
        { // 개별 상태 검사 시작
            var entry = entries.Single(item => item.Name == BattleStatusEffectInstance.GetDisplayName(type)); // 공식 이름의 단일 도감 항목 조회
            Assert.IsNotEmpty(entry.Description); // 빈 상세 안내 방지
            Assert.IsNotNull(entry.Image); // 상태 도감 원화 연결 확인
            Assert.AreSame(BattleStatusIconArt.Get(type), entry.Image); // 전투 표시와 도감의 동일한 원본 확인
        } // 개별 상태 검사 종료
    } // 도감 검사 종료

    [UnityTest] // 아군 강화와 적 약화 검사 지정
    public IEnumerator CriticalStrikeMoraleChargeAndBurnUseRealPhaseBoundaries() // 치명타 합산과 지연 방어 감소 및 화상 실전 확인
    { // 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 입력 환경 진입
        yield return PlayStatusCards(); // 네 원본 카드의 명중과 상태 변화 확인
        yield return new ExitPlayMode(); // 검사 환경 종료
    } // 플레이 모드 검사 종료
    private static IEnumerator PlayStatusCards() // 상태 효과 카드 실제 사용
    { // 상태 카드 검사 시작
        using (var fight = new Fight("CRITICAL_STRIKE", "CRITICAL_STRIKE", "INSPIRE_MORALE", "CHARGE", "FLAME_SLASH", "FLAME_SLASH")) // 네 원본과 중복 사용 준비
        { // 전투 검증 시작
            var foe = fight.Enemies[0]; // 치명과 돌진 대조 대상
            var burned = fight.Enemies[1]; // 화상 지속 대상
            fight.Allies[2].ApplyPersistentHealth(0); // 전투불능 아군 설정
            int seed = 0; // 두 치명 확률을 나누는 난수 시드
            for (; seed < 10000; seed++) // 이십 이상 이십오 미만의 첫 난수 검색
            { // 시드 후보 확인 시작
                Random.InitState(seed); // 후보 시드 설정
                int roll = Random.Range(0, 100); // 실제 치명 난수 한 번 확인
                if (roll >= 20 && roll < 25) // 카드 단독 실패 및 강화 후 성공 구간 확인
                { // 시드 확정 시작
                    break; // 재현 가능한 시드 선택
                } // 시드 확정 종료
            } // 시드 후보 확인 종료
            Assert.Less(seed, 10000); // 경계 시드 존재 확인
            fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 매번 타격 직전 같은 경계 난수 사용
            yield return fight.Use("CRITICAL_STRIKE", 0); // 기본 영 퍼센트에 카드 이십 퍼센트 추가
            Assert.AreEqual(185, foe.CurrentHealth); // 이십 퍼센트 밖의 일반 피해 열다섯 확인
            yield return fight.Use("INSPIRE_MORALE"); // 아군 전체에 치명 확률 부여
            Assert.IsTrue(fight.Allies.Take(2).All(ally => ally.GetStatusEffect(BattleStatusEffectType.CriticalChanceUp)?.EffectiveValue == 5)); // 생존 아군 둘에만 오 퍼센트 증가
            Assert.IsNull(fight.Allies[2].GetStatusEffect(BattleStatusEffectType.CriticalChanceUp)); // 전투불능 아군에 상태 없음
            yield return fight.Use("CRITICAL_STRIKE", 0); // 같은 난수로 이십오 퍼센트 치명 판정
            Assert.AreEqual(163, foe.CurrentHealth); // 열다섯의 일점오 배를 내린 스물두 피해 확인
            foe.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 4, 8, 1); // 방어 감소 관찰용 물리 방어 설정
            yield return fight.Use("CHARGE", 0); // 선택한 적에게 돌진 사용
            Assert.AreEqual(157, foe.CurrentHealth); // 물리 방어 사 점을 반영한 일곱 피해 확인
            Assert.AreEqual(4, foe.PhysicalDefense); // 사용한 턴의 기존 적 방어 유지
            int ownerDefense = fight.Allies[0].PhysicalDefense; // 소유자 방어 대조값 보관
            foe.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 1, 1); // 면역 중 화상 부여 차단 준비
            yield return fight.Use("FLAME_SLASH", 0); // 면역 대상에게 화염 참격 사용
            Assert.IsNull(foe.GetStatusEffect(BattleStatusEffectType.Burn)); // 면역 대상의 화상 차단 확인
            foe.RemoveStatusEffect(BattleStatusEffectType.StatusImmunity); // 돌진 예약 발동 전 면역 해제
            yield return fight.Use("FLAME_SLASH", 1); // 다른 생존 적에게 화염 참격 사용
            Assert.AreEqual(190, burned.CurrentHealth); // 즉시 물리 피해 열 확인
            Assert.AreEqual(2, burned.GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 화상 시작 수치 둘 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 첫 적 턴 진입
            Assert.AreEqual(4, foe.PhysicalDefense); // 다음 아군 턴 전 방어 유지
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 적 턴 종료 뒤 두 번째 아군 턴 시작
            Assert.AreEqual(188, burned.CurrentHealth); // 첫 화상 피해 둘 확인
            Assert.AreEqual(2, foe.PhysicalDefense); // 맞은 적 방어만 두 점 감소
            Assert.AreEqual(ownerDefense, fight.Allies[0].PhysicalDefense); // 공격자 방어 보존
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 아군 턴 종료
            Assert.AreEqual(4, foe.PhysicalDefense); // 돌진 약화가 한 아군 턴 후 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 적 턴 종료
            Assert.AreEqual(187, burned.CurrentHealth); // 두 번째 화상 피해 하나 확인
            Assert.IsNull(burned.GetStatusEffect(BattleStatusEffectType.Burn)); // 둘과 하나 이후 화상 소진
            Assert.AreEqual(1, fight.Allies[0].GetStatusEffect(BattleStatusEffectType.CriticalChanceUp).RemainingTurns); // 기존 부여 턴 보호 후 잔여 한 턴
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 전체 지속 턴 종료
            Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.CriticalChanceUp)); // 아군 치명 강화 만료
        } // 전투 검증 종료
    } // 상태 카드 검사 종료

    [UnityTest] // 대가와 가변 공격 검사 지정
    public IEnumerator CounterattackDoomBladeAndFinisherUseActualLossAndPaidAp() // 실제 체력 손실과 남은 행동력 검증
    { // 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 입력 환경 진입
        yield return PlayCostCards(); // 세 장의 사용 조건과 결과 확인
        yield return new ExitPlayMode(); // 검사 환경 종료
    } // 플레이 모드 검사 종료
    private static IEnumerator PlayCostCards() // 역공과 행동력 및 체력 대가 카드 실제 사용
    { // 대가 카드 검사 시작
        using (var fight = new Fight("COUNTERATTACK", "DOOM_BLADE", "FINAL_STRIKE", "FINAL_STRIKE")) // 대가와 잔여 행동력 비교 덱 구성
        { // 전투 검증 시작
            var owner = fight.Allies[0]; // 실제 카드 소유자
            var target = fight.Enemies[0]; // 일반 공격과 반격 대상
            yield return fight.Use("COUNTERATTACK", 0); // 첫 적 턴 전 반격 사용
            Assert.AreEqual(200, target.CurrentHealth); // 누적 손실 영의 반격 피해 영 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴 시작으로 누적 손실 초기화
            owner.GainBlock(4); // 적 피해 일부를 방어도로 흡수
            owner.TakeDamage(11, BattleDamageType.None, target); // 방어도 네 점과 실제 피해 일곱 발생
            fight.Allies[1].TakeDamage(5, BattleDamageType.None, target); // 다른 아군의 손실은 별도로 집계
            owner.RestoreHealth(3); // 회복해도 누적 손실을 되돌리지 않는 조건
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 피해 기록을 다음 아군 턴까지 유지
            int restored = owner.CurrentHealth; // 정상 체력 대가 검사 기준 보관
            owner.ApplyPersistentHealth(10); // 지불 시 사망하는 경계 체력 설정
            fight.Hand.RefreshCardAvailability(); // 검사 설정을 손패에 반영
            fight.View("DOOM_BLADE").OnPointerClick(null); // 생존 불가 대가 카드 선택 시도
            fight.EnemyViews[0].OnPointerClick(null); // 대상 클릭 시도
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 거절 시 비용 보존
            owner.ApplyPersistentHealth(restored); // 생존 가능한 소유자 체력 복원
            fight.Hand.RefreshCardAvailability(); // 생존 가능한 카드 활성화
            owner.GainBlock(6); // 대가가 방어도를 무시하는 조건
            yield return fight.Use("DOOM_BLADE", 0); // 파멸의 검 정상 사용
            Assert.AreEqual(restored - 10, owner.CurrentHealth); // 소유자 체력 열 지불
            Assert.AreEqual(6, owner.CurrentBlock); // 체력 대가 지불 후 방어도 보존
            Assert.AreEqual(175, target.CurrentHealth); // 선택한 적에게 스물다섯 피해
            Assert.AreEqual(17, owner.HealthLostSinceEnemyTurn); // 다른 동료 피해와 흡수를 제외한 일곱 및 대가 열 집계
            yield return fight.Use("COUNTERATTACK", 0); // 실제 체력 손실 열일곱의 절반 반격
            Assert.AreEqual(167, target.CurrentHealth); // 홀수 절반을 내린 여덟 피해
            var weak = fight.Enemies[1]; // 체력 오분의 일 경계 적
            weak.TakeDamage(159, BattleDamageType.None); // 최대 이백에서 마흔하나로 설정
            var finisher = fight.View("FINAL_STRIKE"); // 실제 남은 행동력 카드 화면
            Assert.AreEqual("X", finisher.RuntimeCard.CostLabel); // 손패에 가변 비용 표기
            Assert.IsTrue(fight.Points.Spend(fight.Points.CurrentActionPoints - 3)); // 실제 잔여 행동력 셋 준비
            finisher.OnPointerClick(null); // 이십 퍼센트 초과 상태에서 사용 시도
            fight.EnemyViews[1].OnPointerClick(null); // 체력 마흔하나의 적 선택
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 부적격한 상태의 행동력 보존
            weak.TakeDamage(1, BattleDamageType.None); // 정확히 이십 퍼센트인 체력 마흔 설정
            var used = fight.View("FINAL_STRIKE").RuntimeCard; // 실제 소모할 인스턴스 보관
            fight.View("FINAL_STRIKE").OnPointerClick(null); // 유효 대상 등장 후 활성 카드 선택
            fight.EnemyViews[0].OnPointerClick(null); // 여유 체력인 다른 적 선택 시도
            Assert.AreEqual(3, fight.Points.CurrentActionPoints); // 잘못된 대상에는 행동력 보존
            fight.EnemyViews[1].OnPointerClick(null); // 정확히 이십 퍼센트인 대상 선택
            Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 남은 행동력 셋을 한 번에 지불
            float deadline = Time.realtimeSinceStartup + 10f; // 타격 연출 대기 기한
            while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 타격과 복귀 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 종료
            Assert.IsFalse(fight.Runner.IsBusy); // 효과 처리 완료 확인
            CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 행동력을 지불한 카드 한 장 이동
            Assert.AreEqual(25, weak.CurrentHealth); // 실제 지불 셋에 다섯을 곱한 피해 확인
            var spare = fight.View("FINAL_STRIKE"); // 행동력 영에서 두 번째 원본 조회
            spare.OnPointerClick(null); // 남은 행동력 영에서 사용 시도
            fight.EnemyViews[1].OnPointerClick(null); // 같은 대상 클릭
            CollectionAssert.Contains(fight.Deck.Hand, spare.RuntimeCard); // 행동력 영의 카드 보존
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 다음 적 턴으로 기준점 갱신
            Assert.AreEqual(0, owner.HealthLostSinceEnemyTurn); // 이전 손실 이월 차단
            fight.Allies[1].ApplyPersistentHealth(3); // 초과 피해 대상의 잔여 체력 세 점 설정
            fight.Allies[1].TakeDamage(99, BattleDamageType.None, target); // 잔여 체력보다 큰 공격 피격
            Assert.AreEqual(3, fight.Allies[1].HealthLostSinceEnemyTurn); // 실제 체력을 넘는 아흔여섯 점 제외
        } // 전투 검증 종료
    } // 대가 카드 검사 종료

    [UnityTest] // 처치 보상과 지불 행동력 검사 지정
    public IEnumerator FinisherFixesItsPaidValueBeforeDaggerRefundsOneActionPoint() // 처치 후 회복이 원래 지불한 X에 영향을 주지 않는지 검증
    { // 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 입력 환경 진입
        yield return PlayDagger(); // 처형 두 번과 유물 회복 한 번 확인
        yield return new ExitPlayMode(); // 검사 환경 종료
    } // 플레이 모드 검사 종료
    private static IEnumerator PlayDagger() // 실제 봉인된 단도와 처형 두 장 비교
    { // 유물 연계 검사 시작
        using (var fight = new Fight("FINAL_STRIKE", "FINAL_STRIKE")) // 두 차례 처치할 원본 카드 준비
        using (var dispatcher = new BattleEventDispatcher()) // 실제 처치와 턴 이벤트 발행기 구성
        using (var events = new BattleEventController(dispatcher, fight.Turns, fight.Controller, fight.Allies, fight.Enemies)) // 런타임의 실제 피격과 사망 신호 연결
        { // 전투 검증 시작
            var gold = new RelicGoldRuntime(); // 독립 유물 지갑 생성
            var inventory = new RelicInventoryRuntime(gold); // 독립 보관함 생성
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(Resources.Load<RelicData>("Relics/SealedDagger"))); // 실제 단도 원본 획득
            using (var relics = new BattleRelicEffectController(dispatcher, inventory, gold, fight.Allies, fight.Enemies, fight.Deck, fight.Points)) // 정상 처치 보상 연결
            { // 원본 카드 사용 시작
                relics.ProcessInitialBattleState(fight.Turns.CurrentPhase, fight.Turns.CurrentRound); // 씬 연결 후 실제 첫 턴 유물 초기화
                int[] health = { 15, 5 }; // 각각 행동력 셋과 하나로 처치할 잔여 체력
                int[] paid = { 3, 1 }; // 사용 순간 카드가 지불할 행동력
                int[] raw = { 0, 0 }; // 실제 카드 공격 피해 기록
                for (int index = 0; index < 2; index++) // 두 적의 체력과 기록 준비
                { // 측정 준비 시작
                    int slot = index; // 기록 대상의 실제 칸 보관
                    fight.Enemies[index].TakeDamage(200 - health[index], BattleDamageType.None); // 저체력 적 둘 준비
                    fight.Enemies[index].DamageResolved += (source, target, damage) => raw[slot] = damage.RawDamage; // 실제 공격의 방어 전 피해 보관
                } // 측정 준비 종료
                Assert.IsTrue(fight.Points.Spend(17)); // 첫 카드의 잔여 행동력 셋 준비
                for (int index = 0; index < 2; index++) // 첫 처치 보상으로 같은 턴 두 번째 공격
                { // 처치 확인 시작
                    Assert.AreEqual(paid[index], fight.Points.CurrentActionPoints); // 사용 순간의 실제 X 확인
                    var view = fight.View("FINAL_STRIKE"); // 아직 손에 있는 원본 조회
                    var used = view.RuntimeCard; // 실제 인스턴스 보관
                    view.OnPointerClick(null); // 손패에서 카드 선택
                    fight.EnemyViews[index].OnPointerClick(null); // 차례로 저체력 적 선택
                    float deadline = Time.realtimeSinceStartup + 10f; // 행동 연출 종료 기한
                    while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 카드 타격과 처치 보상 대기
                    { // 다음 프레임 대기 시작
                        yield return null; // 연출 진행
                    } // 다음 프레임 대기 종료
                    Assert.IsFalse(fight.Runner.IsBusy); // 타격 연출 종료 확인
                    CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 지불한 카드 버림 확인
                    Assert.AreEqual(paid[index] * 5, raw[index]); // 회복 전 지불한 행동력으로 타격 확정
                    Assert.IsTrue(fight.Enemies[index].IsDead); // 아군 턴의 실제 처치 확인
                } // 처치 확인 종료
                Assert.AreEqual(0, fight.Points.CurrentActionPoints); // 두 번째 처치는 턴당 한 번 회복 제한 확인
            } // 원본 카드 사용 종료
        } // 전투 검증 종료
    } // 유물 연계 검사 종료

    [UnityTest] // 범위 두 칸의 무작위 타격 검사 지정
    public IEnumerator SwordDanceRerollsThreeHitsInsideTheSameTwoPositions() // 실제 주사위 보유와 사망 및 가장자리 대조
    { // 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 입력 환경 진입
        yield return PlayDance(); // 범위와 매 타격 추첨 확인
        yield return new ExitPlayMode(); // 검사 환경 종료
    } // 플레이 모드 검사 종료
    private static IEnumerator PlayDance() // 동일 후보의 독립 타격 세 번 검증
    { // 범위 카드 검사 시작
        using (var fight = new Fight("SWORD_DANCE", "SWORD_DANCE", "SWORD_DANCE")) // 원본 카드 세 장 준비
        { // 전투 검증 시작
            var hits = new List<int>(); // 실제 대상별 피격 위치 기록
            for (int index = 0; index < fight.Enemies.Length; index++) // 네 적의 피격 이벤트 연결
            { // 위치 기록 시작
                int slot = index; // 현재 위치 보관
                fight.Enemies[index].DamageResolved += (source, target, hit) => hits.Add(slot); // 실제 피격 순서 기록
            } // 위치 기록 종료
            var previous = RelicRunManager.Instance; // 기존 회차 관리자 보관
            var manager = RelicRunManager.EnsureInstance(); // 독립 플레이 모드 보관함 준비
            var die = Resources.Load<ShopCatalogData>("Shop/ShopCatalog_Test").Offers.Single(offer => offer.Relic != null && offer.Relic.RelicId == "RLC_WORN_DIE").Relic; // 실제 보물의 낡은 주사위 조회
            bool acquired = false; // 이번 검사에서 획득한 유물 여부
            try // 검사 유물 정리 보장
            { // 주사위 사용 검사 시작
                Assert.IsFalse(manager.Inventory.ContainsRelic(die.RelicId)); // 빈 회차의 미보유 기준 확인
                int seed = 0; // 기본 추첨과 주사위가 서로 다른 시드 검색
                int[] normal = null; // 기본 세 타격 예상 위치
                int[] twice = null; // 매번 한 번 더 뽑은 예상 위치
                for (; seed < 10000; seed++) // 재추첨과 중복 선택이 드러나는 시드 검색
                { // 시드 후보 확인 시작
                    Random.InitState(seed); // 기본 추첨 시작점 복원
                    normal = Enumerable.Range(0, 3).Select(hit => 1 + Random.Range(0, 2)).ToArray(); // 왼쪽에서 둘째와 셋째 칸의 세 번 추첨
                    Random.InitState(seed); // 주사위 추첨 시작점 복원
                    twice = Enumerable.Range(0, 6).Select(hit => 1 + Random.Range(0, 2)).Where((slot, index) => index % 2 == 1).ToArray(); // 매 타격의 둘째 추첨만 채택
                    if (!normal.SequenceEqual(twice) && twice[0] == 1 && twice.Distinct().Count() == 2) // 두 경로와 첫 타격 순서를 구분할 시드 확인
                    { // 시드 선택 시작
                        break; // 검증 가능한 시드 확정
                    } // 시드 선택 종료
                } // 시드 후보 확인 종료
                Assert.Less(seed, 10000); // 대조 가능한 시드 존재 확인
                fight.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 각 카드에 같은 무작위열 적용
                yield return fight.Use("SWORD_DANCE", 1); // 실제 미보유 상태에서 가운데 적 선택
                CollectionAssert.AreEqual(normal, hits); // 두 칸 안에서 복원추출 세 번 확인
                Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(die)); // 실제 주사위 보유 처리
                acquired = true; // 종료 시 정리할 유물 보관
                hits.Clear(); // 첫 공격 기록 정리
                yield return fight.Use("SWORD_DANCE", 1); // 같은 시드와 같은 칸에서 주사위 보유 대조
                CollectionAssert.AreEqual(twice, hits); // 각 타격마다 두 번째 추첨을 채택한 세 위치 확인
                fight.Enemies[1].TakeDamage(fight.Enemies[1].CurrentHealth - 1, BattleDamageType.None); // 첫 추첨 대상의 체력 하나 설정
                hits.Clear(); // 준비용 피해 기록 제외
                yield return fight.Use("SWORD_DANCE", 1); // 첫 타격에서 한 적이 사망하는 범위 선택
                CollectionAssert.AreEqual(new[] { 1, 2, 2 }, hits); // 사망한 칸 제외 후 남은 적에게 두 번 명중
                Assert.AreEqual(200, fight.Enemies[0].CurrentHealth); // 범위 왼쪽 밖의 적 보존
                Assert.AreEqual(200, fight.Enemies[3].CurrentHealth); // 범위 오른쪽 밖의 적 보존
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 사용 카드 재사용을 위한 턴 종료
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 새로운 손패 준비
                fight.Enemies[2].TakeDamage(fight.Enemies[2].CurrentHealth, BattleDamageType.None); // 맨 오른쪽의 왼쪽 인접 칸 사망 설정
                hits.Clear(); // 준비용 피해 기록 제외
                yield return fight.Use("SWORD_DANCE", 3); // 오른쪽 끝의 유일한 생존 대상 선택
                CollectionAssert.AreEqual(new[] { 3, 3, 3 }, hits); // 사망한 옆 칸을 건너뛰지 않는 세 번 타격
                Assert.AreEqual(182, fight.Enemies[3].CurrentHealth); // 같은 적에게 여섯 피해 세 번 확인
            } // 주사위 사용 검사 종료
            finally // 기존 회차 보관함 보존
            { // 정리 시작
                if (acquired) // 검사 유물 획득 확인
                { // 검사 유물 제거 시작
                    manager.TryRemove(die.RelicId); // 이번 검사에서 얻은 주사위 정리
                } // 검사 유물 제거 종료
                if (previous == null) // 검사 전 관리자 부재 확인
                { // 임시 관리자 정리 시작
                    Object.Destroy(manager.gameObject); // 검사에서 만든 회차 관리자 제거
                } // 임시 관리자 정리 종료
            } // 정리 종료
        } // 전투 검증 종료
    } // 범위 카드 검사 종료
} // 검사 종료
