using System.Collections; // 실제 플레이 모드 검사 흐름 사용
using System.Linq; // 손패 및 선택 버튼 검색 사용
using NUnit.Framework; // 검사 결과 비교 사용
using UnityEngine; // 게임 자산과 난수 설정 사용
using UnityEngine.TestTools; // 테스트의 실제 플레이 모드 전환 사용
using UnityEngine.UI; // 실제 카드 선택 버튼 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 전투 손패 입력 환경 사용

public sealed class WinterfallCupAndPentacleCombatRelicTests // 성수 마개와 펜타클 펜촉의 실제 카드 사용 검사
{ // 전투 회복과 드로우 유물 검사 시작
    [UnityTest] // 최초 실회복 카드 전체 대상 검사 지정
    public IEnumerator HolyWaterStopperConsumesOnlyOnFirstActualCupHeal() // 비회복과 회복 억제 및 전체 대상의 전투당 한 번 적용 확인
    { // 첫 성배 회복 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 카드 입력 환경 진입
        yield return CheckClockedHealing(); // 플레이 도메인에서 첫 회복 효과 확인
        yield return new ExitPlayMode(); // 성배 회복 전투 종료
    } // 첫 성배 회복 플레이 모드 종료

    [UnityTest] // 복합 직접 회복과 예약 회복 분리 검사 지정
    public IEnumerator HolyWaterStopperAppliesToBothDirectCupPathsAndNeverToDelayedHealing() // 빛의 조각과 생명수에서 직접 회복에만 적용 확인
    { // 복합 성배 회복 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 복합 회복 카드 입력 환경 진입
        yield return CheckCombinedHealing(); // 플레이 도메인에서 복합과 예약 회복 확인
        yield return new ExitPlayMode(); // 복합 회복 전투 종료
    } // 복합 성배 회복 플레이 모드 종료

    [UnityTest] // 실제 카드 사용과 필수 선택 뒤 확률 드로우 검사 지정
    public IEnumerator WobblyNibRollsAfterPentacleSettlementAndWaitsForRequiredChoice() // 펜타클의 성공한 정산과 실제 드로우 확률 경계 확인
    { // 펜촉 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 드로우와 카드 선택 환경 진입
        yield return CheckPentacleDraw(); // 도메인 전환 뒤 실제 추가 드로우 검사
        yield return new ExitPlayMode(); // 펜타클 카드 환경 종료
    } // 펜촉 플레이 모드 종료

    private static IEnumerator CheckClockedHealing() // 최초 실제 회복을 행한 성배의 모든 부상 대상 확인
    { // 성수 마개 실제 회복 검사 시작
        RelicData stopper = Hold(RelicEffectType.FirstCupHealingBonus); // 실제 등록한 성수 마개 유물 보유
        Assert.AreEqual(RelicTriggerType.DirectCardHealing, stopper.TriggerType); // 직접 카드 회복 발동 시점 확인
        Assert.AreEqual(3, stopper.EffectValue); // 직접 회복의 추가 수치 삼 확인
        Assert.AreEqual(1, stopper.MaximumTriggersPerBattle); // 첫 성배 한 장의 전투당 발동 횟수 확인
        using (var fight = new Fight("HOLY_WATER", "DETOX_RITUAL", "DETOX_RITUAL", "DETOX_RITUAL", "DETOX_RITUAL")) // 비회복 성배와 네 차례 전체 성배로 전투 구성
        { // 실제 회복의 조건별 검증 시작
            yield return fight.Use("HOLY_WATER", 0); // 성배 마법 공격으로 첫 기회를 소비하지 않는지 확인
            yield return fight.Use("DETOX_RITUAL"); // 최대 체력만 있는 실제 회복 카드 사용
            var blocked = fight.Allies[0]; // 회복량이 백 퍼센트 감소하는 첫 아군
            var half = fight.Allies[1]; // 실제 회복량이 절반인 둘째 아군
            var normal = fight.Allies[2]; // 후속 대상에도 추가량을 받아야 할 셋째 아군
            SetHealth(blocked, 30); // 회복이 막힌 부상자 체력 준비
            blocked.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 100, 3, 1); // 가능한 회복을 완전히 막는 기존 받는 회복 감소
            yield return fight.Use("DETOX_RITUAL"); // 유일한 부상자가 실제 회복하지 않는 첫 후보 카드 사용
            Assert.AreEqual(30, blocked.CurrentHealth); // 완전히 막힌 회복의 실제 체력 변화 없음 확인
            SetHealth(half, 30); // 감소가 있는 생존 아군의 실제 회복 검증 준비
            SetHealth(normal, 30); // 보정 없는 생존 아군의 실제 회복 검증 준비
            half.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 50, 3, 1); // 추가량을 합친 뒤에도 원래 반감이 적용되는지 확인
            yield return fight.Use("DETOX_RITUAL"); // 처음 두 아군을 실제 회복하는 한 장의 전체 성배 사용
            Assert.AreEqual(30, blocked.CurrentHealth); // 완전히 막힌 대상에게 추가 체력 없음 확인
            Assert.AreEqual(33, half.CurrentHealth); // 기본 사와 추가 삼을 합산한 뒤 반감해서 삼 회복 확인
            Assert.AreEqual(37, normal.CurrentHealth); // 같은 카드의 다음 아군도 추가 삼을 따로 받는지 확인
            yield return fight.Use("DETOX_RITUAL"); // 이후 실제로 회복하는 두 번째 카드 사용
            Assert.AreEqual(35, half.CurrentHealth); // 전투당 첫 카드가 끝난 뒤 기본 회복만 반감 확인
            Assert.AreEqual(41, normal.CurrentHealth); // 후속 카드에는 기본 회복만 적용 확인
        } // 실제 회복의 조건별 검증 종료
    } // 성수 마개 실제 회복 검사 종료

    private static IEnumerator CheckCombinedHealing() // 마법 피해와 별개 직접 회복 및 다음 턴 회복 확인
    { // 복합 성배의 회복 범위 검사 시작
        Hold(RelicEffectType.FirstCupHealingBonus); // 두 별도 전투에서 같은 실제 유물 보유
        using (var fight = new Fight("LIGHT_FRAGMENT")) // 마법 피해와 선택 아군 회복을 한꺼번에 주는 원본 카드 준비
        { // 빛의 조각 직접 회복 확인 시작
            var ally = fight.Allies[1]; // 직접 회복을 지정할 부상 아군
            SetHealth(ally, 30); // 기본과 추가 회복의 합을 확인할 체력 준비
            fight.View("LIGHT_FRAGMENT").OnPointerClick(null); // 실제 빛의 조각 손패 클릭
            fight.EnemyViews[0].OnPointerClick(null); // 먼저 마법 피해가 적용될 적 클릭
            FindAllyView(fight, ally).OnPointerClick(null); // 따로 실제 직접 회복을 받을 아군 클릭
            yield return WaitForAction(fight); // 복합 카드의 직접 피해 및 회복 충돌 대기
            Assert.AreEqual(36, ally.CurrentHealth); // 빛의 조각의 삼 회복에 성수 마개 삼을 더한 실제 체력 확인
        } // 빛의 조각 직접 회복 확인 종료
        using (var fight = new Fight("WATER_OF_LIFE")) // 회복을 별도 차례에 예약하는 원본 카드 준비
        { // 생명수의 직접 및 예약 회복 확인 시작
            var ally = fight.Allies[1]; // 직접과 예약 회복을 모두 받을 부상 아군
            SetHealth(ally, 30); // 동일 대상의 두 회복 수치 검증 준비
            CardData water = fight.View("WATER_OF_LIFE").RuntimeCard.SourceData; // 실제 직접 및 예약 회복 카드 원본 조회
            yield return WinterfallCommonCardExpansionTests.Play(water, fight.Hand, FindAllyView(fight, ally), fight.Runner, fight.Deck, fight.Points); // 아군 대상 클릭과 실제 회복 카드 사용
            Assert.AreEqual(43, ally.CurrentHealth); // 직접 십 회복에 성수 마개 삼이 가산됐는지 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 적 턴까지 진행
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 다음 아군 턴에 예약 회복 발동
            Assert.AreEqual(49, ally.CurrentHealth); // 예약 회복에는 원래 육 점만 적용 확인
        } // 생명수의 직접 및 예약 회복 확인 종료
    } // 복합 성배의 회복 범위 검사 종료

    private static IEnumerator CheckPentacleDraw() // 실제 카드 이동과 선택 이후 드로우 검증
    { // 펜촉 확률 검사 시작
        RelicData nib = Hold(RelicEffectType.PentacleResolvedChanceDraw); // 실제 등록한 흔들리는 펜촉 보유
        Assert.AreEqual(RelicTriggerType.AfterCardResolved, nib.TriggerType); // 사용 후 정산에만 발동하는 시점 확인
        Assert.AreEqual(15, nib.TriggerChancePercent); // 기존 실제 자산의 펜촉 발동 확률 확인
        Assert.AreEqual(1, nib.EffectValue); // 추가로 한 장만 뽑는 원본 수치 확인
        int hit = FindSeed(14); // 성공 상단 정수 경계 시드 조회
        int miss = FindSeed(15); // 실패 하단 정수 경계 시드 조회
        CardData pentacle = CreateCard("NIB_PENTACLE", CardType.Pentacle, CardEffectType.GainBlock, true); // 정산 후 제외되는 단순 펜타클로 드로우 검증
        CardData recover = CreateCard("NIB_RECOVER", CardType.Pentacle, CardEffectType.RecoverDiscard, true); // 정산 후 실제 카드 선택을 요청하는 펜타클
        CardData first = CreateCard("NIB_FIRST", CardType.Cup, CardEffectType.GainBlock, false); // 선택하거나 추가로 뽑을 첫 일반 카드
        CardData second = CreateCard("NIB_SECOND", CardType.Cup, CardEffectType.GainBlock, false); // 선택하거나 추가로 뽑을 둘째 일반 카드
        try // 독립 카드 원본 정리 보장
        { // 실제 펜타클 정산 검사 시작
            foreach (bool success in new[] { true, false }) // 실제 성공 경계와 실패 경계의 전투 비교
            { // 확률 경계 검사 시작
                using var fight = new Fight(new[] { pentacle, first }); // 실제 손패 두 장의 간단한 드로우 전투
                CardInstance filler = fight.Deck.Hand.Single(card => card.SourceData == first); // 뽑을 수 있는 독립 후보 카드 조회
                Assert.IsTrue(fight.Deck.DiscardCard(filler)); // 사용 전 별개 카드 회수 후보를 버린 더미로 이동
                fight.Controller.CardUsed += (_, _) => Random.InitState(success ? hit : miss); // 실제 효과와 사용 카드 정산 직전 확률 고정
                var played = FindCardView(fight, pentacle); // 현재 손패의 실제 펜타클 화면 조회
                CardInstance used = played.RuntimeCard; // 정산된 펜타클과 뽑은 후보의 개별 참조 확인
                played.OnPointerClick(null); // 정상 입력과 같은 단순 펜타클 사용
                yield return WaitForAction(fight); // 실제 정산 뒤 한 번의 확률 드로우 확인
                CollectionAssert.Contains(fight.Deck.ExcludedPile, used); // 사용 카드가 손패에서 먼저 실제 이동 확인
                Assert.AreEqual(success ? 1 : 0, fight.Deck.HandCount); // 확률 경계에 따라 한 장 또는 영 장 드로우 확인
                Assert.AreEqual(success, fight.Deck.Hand.Contains(filler)); // 같은 실제 카드가 성공한 경우에만 손패 복귀 확인
            } // 확률 경계 검사 종료
            using (var fight = new Fight(new[] { recover, first, second })) // 실제 결정이 끝날 때까지 기다려야 하는 전투 준비
            { // 필수 선택과 추가 드로우 순서 확인 시작
                foreach (CardInstance candidate in fight.Deck.Hand.Where(card => card.SourceData != recover).ToArray()) // 이미 손패에 있는 회수 후보 두 장 준비
                { // 회수 후보 준비 시작
                    Assert.IsTrue(fight.Deck.DiscardCard(candidate)); // 실제 버린 더미에 회수 후보 이동
                } // 회수 후보 준비 종료
                fight.Controller.CardUsed += (_, _) => Random.InitState(hit); // 조기 소비하면 잡아낼 수 있게 성공 시드 고정
                FindCardView(fight, recover).OnPointerClick(null); // 손패에서 실제 선택 펜타클 사용
                yield return WaitForAction(fight); // 우선 선택을 요구하는 화면의 실제 표시 대기
                Assert.AreEqual(0, fight.Deck.HandCount); // 선택 종료 전에 펜촉 드로우 없음 확인
                var choices = fight.Hand.transform.parent.GetComponentInChildren<BattleDrawChoiceView>(); // 실제 카드 회수 선택 화면 조회
                Assert.IsNotNull(choices); // 추가 드로우보다 기존 선택이 먼저 열렸는지 확인
                choices.GetComponentsInChildren<Button>().First(button => button.name.StartsWith("Choice_")).onClick.Invoke(); // 실제 UI의 첫 기존 회수 후보 선택
                Assert.AreEqual(2, fight.Deck.HandCount); // 선택한 정상 회수와 뒤의 유물 드로우 한 장 확인
                Assert.AreEqual(fight.Deck.HandCount, fight.Deck.Hand.Distinct().Count()); // 같은 카드를 두 번 끌어오지 않는 정상 덱 확인
                Assert.LessOrEqual(fight.Deck.HandCount, fight.Deck.MaxHandSize); // 기존 손패 제한을 넘어가지 않는지 확인
            } // 필수 선택과 추가 드로우 순서 확인 종료
        } // 실제 펜타클 정산 검사 종료
        finally // 임시 카드 원본 정리 시작
        { // 생성 원본 정리 시작
            Object.Destroy(pentacle); // 단순 펜타클 원본 해제
            Object.Destroy(recover); // 선택 펜타클 원본 해제
            Object.Destroy(first); // 첫 후보 원본 해제
            Object.Destroy(second); // 둘째 후보 원본 해제
        } // 임시 카드 원본 정리 종료
    } // 펜타클 정산 검사 종료

    private static RelicData Hold(RelicEffectType effect) // 실제 등록한 전투 유물 원본 보유
    { // 유물 획득 준비 시작
        RelicData relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == effect); // 등록된 효과와 일치하는 실제 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 회차 유물 보유 처리 확인
        return relic; // 테스트에서 원본 효과 수치를 비교할 자료 반환
    } // 유물 획득 준비 종료

    private static void SetHealth(BattleUnitRuntime ally, int amount) // 회복 테스트에 필요한 충분한 체력 여유 준비
    { // 아군 체력 설정 시작
        ally.ModifyMaxHealth(100 - ally.MaxHealth); // 정량 회복 비교용 공통 최대 체력 설정
        ally.ApplyPersistentHealth(amount); // 실제 처치 이벤트 없이 검증할 부상 체력 적용
    } // 아군 체력 설정 종료

    private static CardData CreateCard(string id, CardType type, CardEffectType effect, bool exclude) // 현재 실제 카드 이동을 측정할 독립 원본 구성
    { // 검사 카드 원본 준비 시작
        CardData card = ScriptableObject.CreateInstance<CardData>(); // 검사 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)type},\"effectType\":{(int)effect},\"excludeOnUse\":{(exclude ? "true" : "false")}}}", card); // 실제 효과와 정산 위치 및 카드 계열 지정
        return card; // 정상 입력으로 사용할 원본 반환
    } // 검사 카드 원본 준비 종료

    private static BattleCardView FindCardView(Fight fight, CardData source) // 손패에 실제로 남은 검사 카드 검색
    { // 카드 화면 조회 시작
        return fight.Hand.GetComponentsInChildren<BattleCardView>().Single(view => view.RuntimeCard?.SourceData == source && fight.Deck.Hand.Contains(view.RuntimeCard)); // 실제 클릭할 현재 손패 UI 반환
    } // 카드 화면 조회 종료

    private static BattleUnitView FindAllyView(Fight fight, BattleUnitRuntime ally) // 현재 명단에서 실제 아군 화면 찾기
    { // 아군 화면 검색 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == ally); // 같은 전투 객체의 실제 클릭 화면 반환
    } // 아군 화면 검색 종료

    private static int FindSeed(int roll) // 실제 게임 정수 난수 결과에 해당하는 시드 조회
    { // 게임 난수 경계 확인 시작
        Random.State original = Random.state; // 기존 난수 상태 보관
        try // 원래 난수 흐름의 복구 보장
        { // 경계 후보 탐색 시작
            for (int seed = 0; seed < 10000; seed++) // 충분한 후보 시드의 몇 가지 경계 탐색
            { // 후보 난수 생성 시작
                Random.InitState(seed); // 현재 후보의 첫 난수 준비
                if (Random.Range(0, 100) == roll) // 정확한 확률 경계 반환 확인
                { // 경계 시드 반환 시작
                    return seed; // 실제 경계를 생성한 시드 반환
                } // 경계 시드 반환 종료
            } // 후보 난수 생성 종료
            throw new System.InvalidOperationException($"테스트 시드 미발견: {roll}"); // 현재 엔진의 예상 경계 없음 검사 실패
        } // 경계 후보 탐색 종료
        finally // 이전 난수 복원 시작
        { // 전역 난수 복구 시작
            Random.state = original; // 테스트 시드 탐색과 실제 게임 순서 분리
        } // 전역 난수 복구 종료
    } // 게임 난수 경계 확인 종료

    private static IEnumerator WaitForAction(Fight fight) // 실제 카드 이동과 효과 연출의 정산 대기
    { // 카드 연출 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 실제 시간 기준 검사 기한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 정상 카드 충돌과 연출 종료 확인
        { // 연출 프레임 대기 시작
            yield return null; // 다음 실제 프레임 대기
        } // 연출 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 기한 내 카드 효과와 연출 정산 확인
    } // 카드 연출 대기 종료
} // 전투 회복과 드로우 유물 검사 종료
