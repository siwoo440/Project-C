using System.Collections; // 실제 카드 연출과 플레이 모드 대기
using System.Collections.Generic; // 여러 타격의 실제 기본 피해 기록
using System.Linq; // 살아 있는 검사 적 및 손패 원본 탐색
using NUnit.Framework; // 기본 피해와 실제 행동력 및 기절 검증
using TMPro; // 원본 카드 상세 텍스트 확인
using UnityEngine; // 실제 화면 양식과 연출 타임아웃
using UnityEngine.TestTools; // 편집 모드에서 실제 전투 모드 진입
using Fight = WinterfallSwordCombatTests.Fight; // 실제 공통 카드 입력 환경 재사용
using Random = UnityEngine.Random; // 실제 타격과 상태 난수 경계 재현

public sealed class WinterfallWandLiberationRiteTests // 마도 해방식의 비용과 광역 마법 피해 및 기절 회귀 검사
{ // 마도 해방식 원본 사용 검사 시작
    private const string Rite = "ARCANE_LIBERATION_RITE"; // 보물 목록의 실제 원본 카드 식별자

    [UnityTest] // 임계 미만 실제 가변 행동력과 원본 표시 검사
    public IEnumerator SpendsAtActualCostAndDoesNotRollStunBelowFour() // 영 행동력 차단과 세 점 광역 피해 경계
    { // 낮은 비용의 플레이 모드 검증 시작
        yield return new EnterPlayMode(); // 실제 원본 손패와 전투 화면 준비
        yield return VerifyLowCost(); // 행동력 표시 및 상태 없는 직접 타격 확인
        yield return new ExitPlayMode(); // 사용한 원본 전투 정리 뒤 편집 모드 복귀
    } // 낮은 비용의 플레이 모드 검증 종료

    [UnityTest] // 실제 독립 판정과 면역 및 사망 경계 검사
    public IEnumerator FourPointsRollsThirtyPercentForEachEligibleSurvivor() // 두 독립 확률과 면역 및 치명 피해 경계
    { // 높은 비용의 플레이 모드 검증 시작
        yield return new EnterPlayMode(); // 원본 카드와 두 진영의 실제 상태 준비
        yield return VerifyIndependentStuns(); // 실제 살아 있는 명중 대상만 확률과 기절 검증
        yield return new ExitPlayMode(); // 전투 및 원본 정리 뒤 편집 모드 복귀
    } // 높은 비용의 플레이 모드 검증 종료

    [UnityTest] // 마법 반향과 확정 행동력 및 부가 상태 분리 검사
    public IEnumerator EchoReusesAcceptedActionPointsWithoutAnExtraStunRoll() // 원본과 절반 마법 피해 및 단일 기절 판정
    { // 반향의 플레이 모드 검증 시작
        yield return new EnterPlayMode(); // 실제 두 원본 카드의 손패와 시전 환경 진입
        yield return VerifyEcho(); // 실제 지불량과 반향의 순수 타격 및 상태 기록 확인
        yield return new ExitPlayMode(); // 기존 전투 검사 객체 해제 뒤 편집 모드 복귀
    } // 반향의 플레이 모드 검증 종료

    private static IEnumerator VerifyLowCost() // 영 행동력 경계와 원본 X 표시 및 세 점 피해 관측
    { // 가변 표시와 진입 검사 시작
        using var fight = new Fight(Rite); // 실제 보물 목록에서 가져온 마도 해방식 전투
        Assert.IsTrue(fight.Allies[0].ApplyPersistentMental(50)); // 정신력 가산이 없는 원본 피해 비교 상태
        var view = fight.View(Rite); // 실제 손패에서 클릭할 카드 화면
        var card = view.RuntimeCard; // 같은 원본 카드의 비용과 마법 피해 분류
        Assert.AreEqual("X", card.CostLabel); // 실제 가변 비용 기호 표시 확인
        Assert.AreEqual(CardTargetType.AllEnemies, card.TargetType); // 지정 입력 없는 전체 생존 적 분류 확인
        Assert.AreEqual(BattleDamageType.Magical, card.DamageType); // 원본 마법 피해의 실제 자료 확인
        Assert.IsTrue(fight.Points.Spend(20)); // 행동력이 없는 상태의 실제 입력 경계 준비
        view.OnPointerClick(null); // 영 행동력에서 전체 대상 사용 시도
        Assert.IsFalse(fight.Runner.IsBusy); // 사용 불가 상태에서 타격 연출 미시작 확인
        CollectionAssert.Contains(fight.Deck.Hand, card); // 행동력이 없는 시점에 원본 카드의 손패 보존 확인
        Assert.AreEqual(3, fight.Points.Gain(3)); // 정상 세 점 사용과 피해 미리보기 준비
        view.OnPointerEnter(null); // 열려 있는 실제 카드 상세 표시 진입
        var tooltip = fight.Hand.transform.Find("CardTooltip").GetComponent<BattleCardTooltipView>(); // 기존 손패의 실제 카드 상세 컴포넌트
        tooltip.Show(card, fight.Points.CurrentActionPoints); // 현재 행동력을 전달한 원본 상세 표시 준비
        var detail = tooltip.transform.Find("DetailText").GetComponent<TMP_Text>(); // 실제 툴팁의 표시 본문 조회
        StringAssert.Contains("AP X", detail.text); // 실제 비용이 고정 영으로 보이지 않는지 확인
        StringAssert.Contains("마법 피해 X × 6", detail.text); // 원본 피해 배수 표기의 실제 수치 확인
        StringAssert.Contains("현재 기본 피해 18", detail.text); // 실제 현재 세 점의 기본 피해 표시 확인
        Assert.IsTrue(fight.Points.Spend(1)); // 표시를 보며 실제 남은 행동력을 두 점으로 감소
        StringAssert.Contains("현재 기본 피해 12", detail.text); // 열린 가변 피해 표시의 실시간 갱신 확인
        Assert.AreEqual(1, fight.Points.Gain(1)); // 아래 경계의 실제 세 점 시전을 복구
        Random.InitState(1967); // 실제 타격에서 상태 난수의 시작점 설정
        Random.State before = Random.state; // 이 시전에서 소비되지 않아야 할 난수 사본
        fight.Controller.CardUsed += (_, __) => Random.InitState(1967); // 실제 충돌 직전에 설정한 같은 난수
        yield return Cast(fight); // 손패의 실제 클릭과 정상 사용 정산 실행
        foreach (var enemy in fight.Enemies) // 실제 명중한 네 생존 적의 피해와 상태 확인
        { // 낮은 비용의 개별 타격 검증 시작
            Assert.AreEqual(enemy.MaxHealth - 18, enemy.CurrentHealth); // 실제 세 점 비용의 배수 마법 피해 확인
            Assert.IsNull(enemy.GetStatusEffect(BattleStatusEffectType.Stun)); // 임계 미만의 모든 적에서 기절 없음 확인
        } // 낮은 비용의 개별 타격 검증 종료
        Assert.AreEqual(before, Random.state); // 임계 미만에서 모든 상태 확률이 소비되지 않았는지 확인
        Assert.Zero(fight.Points.CurrentActionPoints); // 세 점 전액의 한 번 지불 결과 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 실제 원본 카드의 일반 사용 이동 확인
    } // 가변 표시와 진입 검사 종료

    private static IEnumerator VerifyIndependentStuns() // 독립 난수 경계와 쓰러진 적 및 면역 입력
    { // 정확한 기절 확률 검증 시작
        using var fight = new Fight(Rite); // 실제 전체 적 카드 사용 환경
        Assert.IsTrue(fight.Allies[0].ApplyPersistentMental(50)); // 원본 배수와 비교할 일반 정신력 준비
        Assert.IsTrue(fight.Points.Spend(16)); // 실제 임계 비용 네 점만 남기기
        fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 정상 기절에 면역인 살아 있는 적 준비
        fight.Enemies[3].TakeDamage(fight.Enemies[3].MaxHealth - 1, BattleDamageType.None); // 실제 원본 타격으로 쓰러질 적의 현재 체력 준비
        int seed = BoundarySeed(); // 첫 29와 다음 30을 만드는 정확한 성공 실패 경계 난수
        Random.InitState(seed); // 실제 시전에서 돌아갈 난수 시작점
        Random.Range(0, 100); // 첫 혜택 대상에게 쓸 독립 난수 한 번 대조
        Random.Range(0, 100); // 추가 면역과 사망에는 소비하지 않는 두 번째 난수
        Random.State afterTwo = Random.state; // 오직 두 실제 생존 비면역 대상만의 기대 상태
        int uses = 0; // 실제 카드 사용으로 연결된 신호의 횟수
        fight.Controller.CardUsed += (_, __) => // 실제 비용 승인 뒤 충돌에 들어오는 카드 사용 관찰
        { // 정확한 난수와 사용 횟수 기록 시작
            Random.InitState(seed); // 실제 조건을 검증할 첫 성공 및 둘째 실패 난수 적용
            uses++; // 복사되거나 중복 사용된 카드가 있는지 실제 횟수 기록
        }; // 정확한 난수와 사용 횟수 기록 종료
        yield return Cast(fight); // 실제 전체 적 타격 연출과 정산 완료
        Assert.AreEqual(1, uses); // 현재 행동력 전액을 소비한 원본 카드 사용 하나 확인
        Assert.Zero(fight.Points.CurrentActionPoints); // 네 점 전액을 한 번 지불한 실제 결과 확인
        Assert.AreEqual(1, fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stun)?.RemainingTurns); // 29에서 성공한 기절의 원래 한 턴 지속 확인
        Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Stun)); // 30에서는 다른 적의 독립 판정 실패 확인
        Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Stun)); // 원래 기절 면역이 실제 적에게 반영됨 확인
        Assert.IsTrue(fight.Enemies[3].IsDead); // 네 번째 실제 피해가 적을 쓰러뜨렸는지 확인
        Assert.AreEqual(afterTwo, Random.state); // 면역과 처치가 다른 확률을 소비하지 않는지 확인
        foreach (var enemy in fight.Enemies.Take(3)) // 처음부터 살아 남는 실제 세 적의 직접 피해 확인
        { // 기본 마법 피해 확인 시작
            Assert.AreEqual(enemy.MaxHealth - 24, enemy.CurrentHealth); // 기절 성공과 무관한 네 점의 실제 원본 피해 확인
        } // 기본 마법 피해 확인 종료
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 기절이 지속될 실제 적 턴 시작
        Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stun)); // 적 첫 턴의 실제 기절 상태 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 적 턴 종료와 상태 지속 정산
        Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Stun)); // 한 적 턴이 끝난 뒤 실제 기절 만료 확인
    } // 정확한 기절 확률 검증 종료

    private static IEnumerator VerifyEcho() // 실제 이전 가변 비용을 재사용한 반향 및 단일 판정
    { // 반향 피해와 원래 기절 분리 검증 시작
        using var fight = new Fight("MAGIC_ECHO", Rite); // 준비 카드와 가변 피해 카드의 실제 원본 전투
        var owner = fight.Allies[0]; // 마법 준비와 피해 양쪽 카드의 실제 시전자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 원래 피해 배수를 비교할 기본 정신력
        yield return fight.Use("MAGIC_ECHO"); // 원래 준비 카드의 실제 비용 지불과 적용 확인
        for (int index = 2; index < fight.Enemies.Length; index++) // 두 독립 확률만 검증할 실제 적 목록 구성
        { // 나머지 실제 적 제거 시작
            fight.Enemies[index].TakeDamage(10000, BattleDamageType.None); // 원본과 반향의 실제 대상에서 제외
        } // 나머지 실제 적 제거 종료
        Assert.IsTrue(fight.Points.Spend(fight.Points.CurrentActionPoints - 4)); // 정상 전액 지불 직전 가용 네 점 상태
        var damage = new List<int>(); // 원본과 반향이 준 실제 피해 기록
        int stuns = 0; // 같은 원본 상태를 실제로 시도한 횟수
        fight.Enemies[0].DamageTaken += (_, result) => damage.Add(result.RawDamage); // 첫 적에 적용된 원본 및 반향 피해 구분
        fight.Enemies[0].StatusEffectResolved += (_, __, type, ___) => stuns += type == BattleStatusEffectType.Stun ? 1 : 0; // 첫 적의 원래 기절 적용 이벤트만 기록
        int seed = BoundarySeed(); // 원래 두 대상의 기절 성공과 실패를 재현
        Random.InitState(seed); // 두 확률만 소모할 실제 난수의 시작점
        Random.Range(0, 100); // 원래 첫 적의 독립 기절 확률 한 번
        Random.Range(0, 100); // 원래 두 번째 적의 독립 기절 확률 한 번
        Random.State afterTwo = Random.state; // 반향에서는 상태 난수가 추가되지 않는 기대값
        fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 실제 원본 타격이 시작될 때 위 경계 난수를 설정
        yield return Cast(fight); // 행동력 전액 지불한 원본과 마법 반향의 실제 처리
        CollectionAssert.AreEqual(new[] { 24, 12 }, damage); // 처음 지불한 네 점의 배수와 절반 원본 피해 확인
        Assert.AreEqual(1, stuns); // 알림 기준 한 번만 적용된 실제 첫 적의 기절 확인
        Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Stun)); // 복제 공격이 실패한 두 번째 기절을 새로 굴리지 않는지 확인
        Assert.AreEqual(afterTwo, Random.state); // 반향에 의한 추가 상태 난수 없음 확인
        Assert.Zero(fight.Points.CurrentActionPoints); // 반향에서 추가 준비 비용이나 환급이 없음 확인
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.WandEcho)); // 실제 가변 피해 카드로 한 번 소비한 준비 상태 확인
    } // 반향 피해와 원래 기절 분리 검증 종료

    private static IEnumerator Cast(Fight fight) // 실제 전투에서 대상 자동 카드의 손패 원본 클릭
    { // 실제 사용 및 정산 대기 시작
        fight.View(Rite).OnPointerClick(null); // 모든 생존 적 자동 지정 후 실제 전액 지불
        float deadline = Time.realtimeSinceStartup + 12f; // 두 광역 연출을 모두 기다리는 실제 경계
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 카드 애니메이션과 피해 정산 대기
        { // 다음 전투 프레임 대기 시작
            yield return null; // 실제 전투 엔진의 다음 프레임 대기
        } // 다음 전투 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 카드 사용과 복귀 연출 완료 확인
    } // 실제 사용 및 정산 대기 종료

    private static int BoundarySeed() // 성공 29와 실패 30을 만드는 실제 Unity 두 난수
    { // 결정적 순서 시드 탐색 시작
        for (int seed = 0; seed < 100000; seed++) // 실제 엔진 난수 순서를 확인할 유한 범위
        { // 현재 후보의 두 확률 검사 시작
            Random.InitState(seed); // 완전히 독립적인 확률 경계 재생 준비
            if (Random.Range(0, 100) == 29 && Random.Range(0, 100) == 30) // 서른 미만 성공 그리고 서른 실패의 실제 경계
            { // 실제 경계 재현 성공 시작
                return seed; // 실제 카드에 사용할 동일한 원본 시드 전달
            } // 실제 경계 재현 성공 종료
        } // 현재 후보의 두 확률 검사 종료
        Assert.Fail("29와 30을 생성하는 Unity 난수 시드 누락"); // 전체 검사 경계의 실제 구성 실패 보고
        return 0; // 실패하지 않은 경우에만 도달하는 형식상 반환
    } // 결정적 순서 시드 탐색 종료
} // 마도 해방식 원본 사용 검사 종료
