using System.Collections; // 전투 장면 플레이 모드 검사 사용
using System.Linq; // 실제 카드와 대상 화면 검색 사용
using NUnit.Framework; // 테스트 결과 확인 사용
using TMPro; // 선택 안내와 카드 효과 문구 검증 사용
using UnityEditor; // 실제 전투 유닛 원본 조회 사용
using UnityEngine; // 게임 검사 화면 생성 사용
using UnityEngine.EventSystems; // 실제 드롭 대상 입력 사용
using UnityEngine.TestTools; // 검사 플레이 모드 전환 사용
using UnityEngine.UI; // 실제 대상 윤곽선 확인 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 카드 사용 검사 구성

public sealed class WinterfallLightFragmentTests // 빛의 조각 두 진영 지정과 결산 검사
{ // 빛의 조각 검사 시작
    [UnityTest] // 적과 아군을 순서대로 고르는 클릭 검사 지정
    public IEnumerator LightFragment_ClickRequiresTwoLivingTargetsAndOnePayment() // 적과 아군 양쪽 선택 후 한 번 결제 확인
    { // 클릭 검사 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 준비
        yield return CheckClickResolution(); // 도메인 전환이 끝난 뒤 클릭 검사 실행
        yield return new ExitPlayMode(); // 클릭 검사 플레이 모드 종료
    } // 클릭 검사 플레이 모드 종료

    [UnityTest] // 두 번 드래그와 마지막 적의 회복 저장 검사 지정
    public IEnumerator LightFragment_DragSupportsCancellationAndHealsBeforeVictorySnapshot() // 잘못된 드롭 취소와 승리 전에 반영된 회복 확인
    { // 드래그 검사 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 입력 환경 준비
        yield return CheckDragAndVictory(); // 도메인 전환이 끝난 뒤 드래그 검사 실행
        yield return new ExitPlayMode(); // 드래그 검사 플레이 모드 종료
    } // 드래그 검사 플레이 모드 종료

    private static IEnumerator CheckClickResolution() // 실제 카드 클릭과 살아 있는 두 대상 경계 확인
    { // 클릭으로 복합 효과 검증 시작
        using (var fight = new Fight(CreateView, CreateView, "LIGHT_FRAGMENT", "LIGHT_FRAGMENT")) // 실제 보물 카드와 양쪽의 전투 유닛 화면 생성
        { // 현재 전투 명단 검사 시작
            BattleCardView card = fight.View("LIGHT_FRAGMENT"); // 실제 손패의 첫 빛의 조각 조회
            CardInstance used = card.RuntimeCard; // 실제 결제 대상 카드 보관
            BattleUnitRuntime enemy = fight.Enemies[0]; // 마법 피해를 받을 지정 적
            BattleUnitRuntime ally = fight.Allies[1]; // 회복할 다른 아군
            BattleUnitView healedView = FindView(fight, ally); // 아군의 실제 클릭 대상
            BattleUnitView ownerView = FindView(fight, used.OwnerUnit); // 소유자의 실제 클릭 대상
            ally.TakeDamage(8, BattleDamageType.None); // 부상 상태로 직접 회복량 확인
            int allyHealth = ally.CurrentHealth; // 회복 전 최대 체력보다 낮은 체력 저장
            int castCount = 0; // 두 대상 입력에서 발생한 카드 사용 횟수
            BattleUnitRuntime[] usedTargets = null; // 실제 카드 사용 시 전달한 대상 기록
            fight.Controller.CardUsed += (_, targets) => // 일반 사용 이벤트에 한 번 전달된 두 대상 확인
            { // 카드 사용 알림 기록 시작
                castCount++; // 실제 카드 사용 알림 횟수 집계
                usedTargets = targets.ToArray(); // 피해와 회복에 사용된 두 대상 복사
            }; // 카드 사용 알림 기록 종료
            Assert.AreEqual(CardTargetType.EnemyAndAlly, used.TargetType); // 원본의 적과 아군 지정 방식 확인
            Assert.AreEqual(CardEffectType.Damage, used.EffectType); // 원본 마법 직접 공격 방식 확인
            Assert.AreEqual(CardType.Cup, used.CardType); // 원본 성배 카드 계열 확인
            Assert.AreEqual(1, used.ApCost); // 원본 카드 행동력 확인
            Assert.AreEqual(BattleDamageType.Magical, used.DamageType); // 피해의 마법 유형 확인
            Assert.AreEqual(6, used.EffectValue); // 피해의 원본 수치 확인
            Assert.AreEqual(3, used.SourceData.SecondaryEffectValue); // 지정 회복의 원본 수치 확인
            AssertTooltip(used); // 선택 순서와 두 효과의 실제 카드 툴팁 확인
            card.OnPointerClick(null); // 빛의 조각 클릭으로 첫 대상 선택
            StringAssert.Contains("피해를 줄 적", Prompt(fight)); // 첫 적 선택 안내 표시 확인
            Assert.IsTrue(IsOutlined(fight.EnemyViews[0])); // 선택 가능한 적에게 실제 노란 테두리 확인
            Assert.IsFalse(IsOutlined(healedView)); // 첫 단계에서 아군 강조 제외
            healedView.OnPointerClick(null); // 잘못된 순서로 회복 아군 먼저 클릭
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 첫 단계의 잘못된 대상에 행동력 보존
            fight.EnemyViews[0].OnPointerClick(null); // 첫 적의 실제 선택 확정
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 아군 미선택 상태의 행동력 보존
            Assert.AreEqual(enemy.MaxHealth, enemy.CurrentHealth); // 두 대상 선택 전에 피해 없음 확인
            StringAssert.Contains("회복할 아군", Prompt(fight)); // 두 번째 아군 선택 안내 표시 확인
            Assert.IsTrue(IsOutlined(healedView)); // 지정할 생존 아군의 실제 테두리 확인
            Assert.IsTrue(IsOutlined(ownerView)); // 자기 자신도 회복 대상 후보에 포함 확인
            Assert.IsFalse(IsOutlined(fight.EnemyViews[1])); // 두 번째 단계에서 다른 적 강조 제외
            fight.EnemyViews[1].OnPointerClick(null); // 잘못된 단계의 두 번째 적 입력
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 두 번째 단계의 잘못된 대상에 비용 보존
            healedView.OnPointerClick(null); // 살아 있는 지정 아군으로 선택 완료
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 대상 확정 후 행동력 한 점만 삭감
            yield return WaitForImpact(fight); // 실제 피해와 회복 연출 정산 대기
            Assert.AreEqual(1, castCount); // 카드 사용 이벤트 한 번만 발행 확인
            CollectionAssert.AreEqual(new[] { enemy, ally }, usedTargets); // 단일 피해 적과 별개의 단일 회복 아군 확인
            Assert.AreEqual(enemy.MaxHealth - 6, enemy.CurrentHealth); // 지정 적에게 마법 피해 육 점 적용 확인
            Assert.AreEqual(allyHealth + 3, ally.CurrentHealth); // 지정 아군에게 체력 삼 점 회복 확인
            Assert.AreEqual(fight.Enemies[1].MaxHealth, fight.Enemies[1].CurrentHealth); // 지정하지 않은 적 피해 방지
            Assert.AreEqual(used.OwnerUnit.MaxHealth, used.OwnerUnit.CurrentHealth); // 지정하지 않은 원래 소유자 피해 방지
            CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 두 효과를 처리한 카드 한 장의 정상 이동 확인
            StringAssert.Contains("플레이어 턴", Prompt(fight)); // 행동 완료 뒤 선택 안내 제거 확인

            BattleCardView nextCard = fight.View("LIGHT_FRAGMENT"); // 이후 첫 대상이 무효가 되는 두 번째 카드 조회
            nextCard.OnPointerClick(null); // 다음 카드의 첫 단계 열기
            fight.EnemyViews[1].OnPointerClick(null); // 살아 있는 적을 첫 대상으로 잠시 지정
            fight.Enemies[1].TakeDamage(1000, BattleDamageType.None); // 회복 대상 선택 전에 해당 적을 실제 사망 처리
            Assert.IsNull(fight.Controller.SelectedCard); // 확정했던 첫 대상 사망으로 순차 선택 취소 확인
            healedView.OnPointerClick(null); // 취소된 아군 입력을 계속 누르는 경우 재현
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 사망한 첫 대상 이후의 잘못된 재사용 비용 방지
            Assert.AreEqual(1, castCount); // 취소된 단계의 실제 사용 이벤트 발행 방지
            CollectionAssert.Contains(fight.Deck.Hand, nextCard.RuntimeCard); // 취소된 카드가 손패에 남는지 확인
            BattleUnitRuntime evasive = fight.Enemies[2]; // 직접 공격을 피하는 다른 생존 적 조회
            evasive.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 두 번째 카드의 피해 실패를 보장하는 회피 설정
            nextCard.OnPointerClick(null); // 취소 이후 같은 카드를 정상 재선택
            fight.EnemyViews[2].OnPointerClick(null); // 실제 회피할 적에 피해 지정
            healedView.OnPointerClick(null); // 같은 부상 동료에게 두 번째 회복 지정
            yield return WaitForImpact(fight); // 빗나감과 독립적인 아군 회복 처리 확인
            Assert.AreEqual(evasive.MaxHealth, evasive.CurrentHealth); // 적의 실제 빗나감으로 피해 없음 확인
            Assert.AreEqual(allyHealth + 6, ally.CurrentHealth); // 빗나가도 별도로 지정한 아군에게 체력 삼 점 회복
            Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 다시 사용한 카드도 한 번만 비용 지불 확인
        } // 현재 전투 명단 검사 종료
    } // 클릭으로 복합 효과 검증 종료

    private static IEnumerator CheckDragAndVictory() // 연속 드롭과 실제 승리 정산의 순서 확인
    { // 드래그 입력과 승리 결과 검증 시작
        using (var fight = new Fight(CreateView, CreateView, "LIGHT_FRAGMENT", "LIGHT_FRAGMENT")) // 실제 두 장으로 잘못된 드롭과 유효한 연속 사용 준비
        { // 드롭 단계 검사 시작
            BattleCardView card = fight.View("LIGHT_FRAGMENT"); // 첫 번째 드래그 카드 조회
            BattleCardDragHandler drag = CreateDrag(card); // 카드와 실제 드래그 이벤트 연결
            BattleUnitView owner = FindView(fight, card.RuntimeCard.OwnerUnit); // 체력이 가득 찬 자기 자신 대상 조회
            BattleUnitView enemy = fight.EnemyViews[0]; // 첫 번째 적 드롭 대상 조회
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 왼쪽 버튼에 대응하는 실제 입력 이벤트 준비
            Drop(drag, pointer, owner); // 첫 단계에 잘못된 아군 드롭 시도
            Assert.IsNull(fight.Controller.SelectedCard); // 첫 단계 잘못된 드롭의 선택 취소 확인
            Drop(drag, pointer, enemy); // 정확한 적을 첫 대상으로 드롭
            StringAssert.Contains("회복할 아군", Prompt(fight)); // 적 드롭으로 회복 단계 전환 확인
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 적 하나만 고른 드롭의 행동력 보존
            Drop(drag, pointer, fight.EnemyViews[1]); // 두 번째 단계에서 다른 적에 잘못 놓기
            Assert.IsNull(fight.Controller.SelectedCard); // 잘못된 두 번째 드롭과 첫 선택까지 취소 확인
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 잘못된 두 번째 드롭의 행동력 보존
            Drop(drag, pointer, enemy); // 취소 뒤 처음부터 유효한 적 선택
            Drop(drag, pointer, owner); // 두 번째 드롭으로 체력이 가득 찬 본인 선택
            Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 완성한 두 드롭의 한 번 비용 차감 확인
            yield return WaitForImpact(fight); // 첫 드래그 카드의 정산 완료 대기
            Assert.AreEqual(enemy.RuntimeUnit.MaxHealth - 6, enemy.RuntimeUnit.CurrentHealth); // 두 드롭의 실제 마법 피해 한 번 적용 확인
            Assert.AreEqual(owner.RuntimeUnit.MaxHealth, owner.RuntimeUnit.CurrentHealth); // 최대 체력인 자신도 선택 가능하고 상한 유지 확인

            foreach (BattleUnitRuntime other in fight.Enemies.Skip(1)) // 승리를 분리하여 측정할 마지막 적 외의 대상 순회
            { // 남은 다른 적 정리 시작
                other.TakeDamage(1000, BattleDamageType.None); // 마지막 적 이전의 실제 사망 처리
            } // 남은 다른 적 정리 종료
            enemy.RuntimeUnit.TakeDamage(enemy.RuntimeUnit.CurrentHealth - 1, BattleDamageType.None); // 실제 직접 마법 피해로 처치할 마지막 적의 체력 한 점 준비
            BattleUnitRuntime ally = fight.Allies[1]; // 승리 직전에 회복을 받을 동료 확인
            ally.TakeDamage(8, BattleDamageType.None); // 승리 결과에 차이가 생기는 회복 체력 준비
            int expectedHealth = ally.CurrentHealth + 3; // 승리 결과에 보관할 실제 회복 후 체력
            int victimHealthAtHeal = -1; // 치료 시점의 마지막 적 체력 확인용 값
            int completionCount = 0; // 승리 완료 이벤트 발생 횟수
            int allyHealthAtCompletion = -1; // 승리가 저장되는 순간의 체력 확인용 값
            bool cardMovedBeforeCompletion = false; // 승리 이벤트 전 카드 이동 성공 여부
            BattleCardView final = fight.View("LIGHT_FRAGMENT"); // 마지막 처치를 처리할 남은 카드 조회
            CardInstance finalCard = final.RuntimeCard; // 처치와 회복을 정산할 카드 참조 저장
            ally.HealthRestored += (_, _) => victimHealthAtHeal = enemy.RuntimeUnit.CurrentHealth; // 공격이 회복 전에 처리됐는지 실제 알림에서 확인
            fight.Turns.BattleCompleted += result => // 전투 결과 저장 시점의 실제 결과와 체력 읽기
            { // 승리 시점 상태 기록 시작
                Assert.AreEqual(BattleResult.Victory, result); // 최종 적 처치의 실제 승리 판정 확인
                completionCount++; // 실제 승리 이벤트 횟수 기록
                allyHealthAtCompletion = ally.CurrentHealth; // 승리 저장 시점에 읽는 회복 반영 체력 저장
                cardMovedBeforeCompletion = fight.Deck.DiscardPile.Contains(finalCard); // 카드 이동이 이미 완료됐는지 기록
            }; // 승리 시점 상태 기록 종료
            final.OnPointerClick(null); // 두 번째 카드의 일반 클릭 입력
            enemy.OnPointerClick(null); // 마지막 생존 적의 피해 대상 선택
            Drop(CreateDrag(final), pointer, FindView(fight, ally)); // 서로 다른 두 입력 방식으로 회복 대상 드롭
            Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 마지막 카드도 대상 선택을 완료한 뒤 한 번 결제
            yield return WaitForImpact(fight); // 마지막 피해와 회복 및 승리 정산 대기
            Assert.AreEqual(0, victimHealthAtHeal); // 마지막 적을 처치한 실제 피해 뒤 회복 실행 확인
            Assert.AreEqual(1, completionCount); // 두 효과에 대해 한 번만 승리 정산 확인
            Assert.AreEqual(expectedHealth, allyHealthAtCompletion); // 지정 회복이 승리 체력 저장 전에 적용된 값 확인
            Assert.IsTrue(cardMovedBeforeCompletion); // 최종 카드 이동까지 반영한 승리 시점 확인
        } // 드롭 단계 검사 종료
    } // 드래그 결과 검증 종료

    private static void Drop(BattleCardDragHandler drag, PointerEventData pointer, BattleUnitView target) // 실제 드래그 입력에 대상 화면 전달
    { // 드래그 입력 시작
        drag.OnBeginDrag(pointer); // 현재 대상 단계에서 드래그 시작
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = target.gameObject }; // 실제 드롭 위치의 유닛 지정
        drag.OnEndDrag(pointer); // 카드의 정상 드롭 처리 호출
    } // 드래그 입력 종료

    private static BattleCardDragHandler CreateDrag(BattleCardView card) // 실제 카드 화면에 드래그 연결
    { // 드래그 입력 준비 시작
        var drag = card.GetComponent<BattleCardDragHandler>() ?? card.gameObject.AddComponent<BattleCardDragHandler>(); // 기존 카드의 드래그 입력 요소 확보
        drag.Initialize(card); // 기존 카드 화면을 드래그 처리에 연결
        return drag; // 실제 드롭 검사에 사용할 입력 요소 반환
    } // 드래그 입력 준비 종료

    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 전투 유닛 UI 구성
    { // 실물 유닛 화면 생성 시작
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 게임이 쓰는 원본 전투 유닛 조회
        var view = Object.Instantiate(prefab, parent).GetComponent<BattleUnitView>(); // 실제 전투 화면 하위에 원본 생성
        view.Bind(unit); // 해당 명단 유닛과 클릭 및 체력 연결
        return view; // 대상 선택 검증에 사용할 실제 화면 반환
    } // 실물 유닛 화면 생성 종료

    private static BattleUnitView FindView(Fight fight, BattleUnitRuntime unit) // 현재 전투의 동일 유닛 화면 조회
    { // 유닛 화면 조회 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == unit); // 아군 또는 적의 정확한 현재 화면 반환
    } // 유닛 화면 조회 종료

    private static bool IsOutlined(BattleUnitView view) // 초상화에서 실제로 표시하는 선택 윤곽선 확인
    { // 대상 강조 조회 시작
        return view.GetComponentsInChildren<Outline>().Any(outline => outline.enabled); // 보이는 실제 선택 테두리 여부 반환
    } // 대상 강조 조회 종료

    private static string Prompt(Fight fight) // 현재 손패와 연결된 선택 안내 텍스트 조회
    { // 선택 문구 조회 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == "TurnStatusText").text; // 실제 플레이어 턴 위치의 표시 문구 반환
    } // 선택 문구 조회 종료

    private static void AssertTooltip(CardInstance card) // 빛의 조각의 실제 툴팁 수치와 순서 확인
    { // 복합 카드 툴팁 검사 시작
        var box = new GameObject("LightFragmentTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardTooltipView)); // 실제 카드 상세 화면 준비
        try // 실패해도 임시 상세 화면 정리 보장
        { // 실제 툴팁 표시 시작
            box.GetComponent<BattleCardTooltipView>().Show(card); // 실제 손패와 같은 카드 상세 렌더링
            string text = string.Join("\n", box.GetComponentsInChildren<TMP_Text>().Select(label => label.text)); // 표시 중인 제목과 상세 텍스트 합치기
            StringAssert.Contains("적 1명 → 아군 1명", text); // 적에서 아군으로 이어지는 순서 문구 확인
            StringAssert.Contains("마법 피해 6", text); // 적의 마법 피해 수치 확인
            StringAssert.Contains("아군 체력 3 회복", text); // 지정 아군의 회복 수치 확인
        } // 실제 툴팁 표시 종료
        finally // 검사 뒤 화면 해제 시작
        { // 임시 화면 정리 시작
            Object.Destroy(box); // 임시 상세 화면 제거
        } // 임시 화면 정리 종료
    } // 복합 카드 툴팁 검사 종료

    private static IEnumerator WaitForImpact(Fight fight) // 실제 코루틴의 카드 효과와 정산 완료 대기
    { // 카드 효과 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 현재 플레이 시간 기준 연출 종료 기한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 카드 연출 정산 완료 확인
        { // 완료 대기 시작
            yield return null; // 다음 프레임 대기
        } // 완료 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 정해진 시간 내 전투 연출 완료 확인
    } // 카드 효과 대기 종료
} // 빛의 조각 검사 종료
