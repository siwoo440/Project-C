using System.Collections; // 실제 Unity 모드와 카드 연출 대기
using System.Linq; // 공용 손패의 실제 카드와 지정 아군 화면 조회
using NUnit.Framework; // 실제 행동력 및 피해와 영역 이동 검증
using UnityEngine; // 런타임 UI와 실제 연출 시간 제공
using UnityEngine.TestTools; // 실제 플레이 모드 진입과 회귀 실행
using Fight = WinterfallSwordCombatTests.Fight; // 일반 손패 및 대상 입력의 실제 전투 구성
using Random = UnityEngine.Random; // 정확한 독립 확률 경계의 재생

public sealed class WinterfallPentacleSupportActionTests // 펜타클의 실제 행동력 및 혼합 회복 카드 효과 검사
{ // 일반 카드 사용 회귀 시작
    [UnityTest] // 체력 대가와 무료 동전의 실제 소비 검사
    public IEnumerator RitualPaysNonlethalHealthAndCoinExcludesOnlyAfterRealUse() // 일반 체력 비용과 전투 한정 제외의 실제 사용 확인
    { // 실체력과 손패 제외 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 전투 카드의 사용 환경 진행
        yield return VerifyPayments(); // 체력 다섯의 경계와 사용 뒤 동전의 제외 및 환급 확인
        yield return new ExitPlayMode(); // 실제 전투 자원 정리 뒤 편집 모드 복귀
    } // 실체력과 손패 제외 검사 종료

    [UnityTest] // 실제 회복과 일반 피해의 광역 적용 및 최종 드로우 검사
    public IEnumerator FusionHealsOnePartyAndDrawsOnceBeforeLethalVictory() // 일반 방어도와 종료 전에 완료되는 공용 드로우 확인
    { // 혼합 광역 정산 검사 시작
        yield return new EnterPlayMode(); // 실제 일반 광역 타격과 회복의 환경 진입
        yield return VerifyFusion(); // 살아 있는 전체 대상 및 승리 보류의 실제 정산 검증
        yield return new ExitPlayMode(); // 실제 전투 해제 뒤 편집 모드 복귀
    } // 혼합 광역 정산 검사 종료

    [UnityTest] // 공용 한 장과 지정 회복의 독립 확률 실제 검사
    public IEnumerator SuppliesDrawOnceAndTemporaryRefundRollsEvenAtFullHealth() // 방어도 지급과 별도의 적중 금액 없는 행동력 확률
    { // 보급 플로우 큐 검증 준비 시작
        yield return new EnterPlayMode(); // 실제 방어 및 지정 회복이 가능한 전투 시작
        yield return VerifySupplies(); // 방어도와 원본 드로우 및 확률 경계를 실제 카드에서 검사
        yield return new ExitPlayMode(); // 실제 카드와 화면 자원 정리 뒤 편집 모드 복귀
    } // 보급 플로우 큐 검증 준비 종료

    private static IEnumerator VerifyPayments() // 고정 네이티브 체력 비용과 전투 한정 무료 카드 분리
    { // 두 자원 청구 경로 검증 시작
        using var fight = new Fight("RITUAL", "PICK_UP_COIN"); // 실제 시전 체력 및 전투용 동전 원본 구성
        var caster = fight.Allies[0]; // 두 원본 카드의 공통 실제 소유자
        Assert.IsTrue(caster.ApplyPersistentMental(50)); // 피해와 체력 직접 비용을 비교할 일반 정신력
        Assert.IsTrue(caster.ApplyPersistentHealth(5)); // 체력 지불 뒤 쓰러지는 딱 다섯 경계 설정
        Assert.IsTrue(fight.Points.Spend(10)); // 정상 행동력 회복 범위 확인을 위한 충분한 여유 생성
        var ritual = fight.View("RITUAL").RuntimeCard; // 사용할 실제 원본 의식 손패 카드 기록
        int balance = fight.Points.CurrentActionPoints; // 사용 가능성 실패 전 실제 공용 행동력
        fight.View("RITUAL").OnPointerClick(null); // 생존할 수 없는 실제 체력에서 의식 시전 시도
        Assert.IsFalse(fight.Runner.IsBusy); // 생존 조건 실패가 연출까지 시작시키지 않는지 확인
        Assert.AreEqual(balance, fight.Points.CurrentActionPoints); // 지불 불가로 실제 행동력 그대로 유지
        Assert.AreEqual(5, caster.CurrentHealth); // 거절한 체력 비용으로 남은 체력 유지 확인
        Assert.AreEqual(1, caster.RestoreHealth(1)); // 회복 이벤트로 카드 입력을 다시 열고 최소 지불 가능 체력 확인
        caster.GainBlock(9); // 비용이 피격 흡수 경로를 거치지 않는지 확인할 방어도
        ritual.ApplyTemporaryApDiscount(1); // 사용 시점의 정상 실제 재계산 비용 검증 준비
        int paid = ritual.ApCost; // 할인과 함께 실제 사용해야 할 원본 카드의 정책 비용
        fight.View("RITUAL").OnPointerClick(null); // 현재 생존 체력과 실제 비용으로 의식 시전
        yield return Wait(fight); // 실제 충돌과 켜진 카드 정산의 애니메이션 종료 대기
        Assert.AreEqual(1, caster.CurrentHealth); // 실제 일반 피해 아닌 시전 비용 다섯 점 지불 확인
        Assert.AreEqual(9, caster.CurrentBlock); // 생존 체력 비용은 원래 방어도를 쓰지 않는지 확인
        Assert.AreEqual(balance - paid + 2, fight.Points.CurrentActionPoints); // 실제 할인가 이후 공용 행동력 두 점 환급 확인
        var coin = fight.View("PICK_UP_COIN").RuntimeCard; // 현재 전투에서만 제외되는 실제 원본 동전
        Assert.AreEqual(0, coin.ApCost); // 동전이 원본처럼 무료로 시작하는지 확인
        int beforeCoin = fight.Points.CurrentActionPoints; // 무료 동전의 실제 행동력 효과 비교 기준
        fight.View("PICK_UP_COIN").OnPointerClick(null); // 손패 동전의 실제 사용 클릭
        yield return Wait(fight); // 실제 판정과 제외 더미 이동 완료 대기
        Assert.AreEqual(beforeCoin + 1, fight.Points.CurrentActionPoints); // 현재 행동력 한 점의 실제 지급 확인
        CollectionAssert.Contains(fight.Deck.ExcludedPile, coin); // 사용한 원본 동전의 이번 전투만 제외 확인
        Assert.AreEqual(2, fight.Deck.SourceData.Cards.Count); // 전투 제외 후에도 실제 저장 덱에서 원본 보존
    } // 두 자원 청구 경로 검증 종료

    private static IEnumerator VerifyFusion() // 살아 있는 원래 대상을 한 번씩 정산한 마지막 피격
    { // 합성 카드의 실제 혼합 효과 검사 시작
        using var fight = new Fight("FUSION_OF_POWER", "RITUAL", "PICK_UP_COIN", "GOLDEN_COIN"); // 두 회수 카드와 실제 광역 혼합 카드 준비
        var fusion = fight.View("FUSION_OF_POWER").RuntimeCard; // 사용과 승리 뒤까지 추적할 실제 혼합 원본
        var extras = fight.Deck.Hand.Where(card => card != fusion).Take(2).ToArray(); // 실제 두 장의 지급 카드 후보 사본
        foreach (var extra in extras) // 실제 공용 덱에 뽑을 수 있게 할 대상만 이동
        { // 드로우 카드 두 장 준비 시작
            Assert.IsTrue(fight.Deck.DiscardCard(extra)); // 원본을 제외하지 않는 네이티브 카드 버림과 이후 드로우 준비
        } // 드로우 카드 두 장 준비 종료
        foreach (var ally in fight.Allies.Take(2)) // 실제 살아 있는 아군의 회복 여유만 준비
        { // 혼합 효과의 수치를 비교할 대상 설정 시작
            Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 기존 원본 열 점 회복을 확인할 체력 상태
            Assert.IsTrue(ally.ApplyPersistentMental(50)); // 추가 정신력 발동을 배제할 비교 상태
        } // 혼합 효과의 수치를 비교할 대상 설정 종료
        Assert.IsTrue(fight.Allies[2].ApplyPersistentHealth(0)); // 쓰러져 실제 회복에 참여하지 않는 아군 준비
        foreach (var enemy in fight.Enemies) // 마지막 일반 피해로 동시에 전투가 끝나는 실제 적들
        { // 임계 이하의 생존 적 구성 시작
            enemy.TakeDamage(enemy.MaxHealth - 8, BattleDamageType.None); // 일반 피해로 실제 적 체력을 여덟 점으로 낮춤
            enemy.GainBlock(3); // 각 살아 있는 적에 들어갈 기존 방어도 세 점 설정
        } // 임계 이하의 생존 적 구성 종료
        BattleDamageResult observed = default; // 일반 피해와 방어도 흡수를 기록할 실제 결과
        fight.Enemies[0].DamageTaken += (_, damage) => observed = damage; // 실제 적 피해 접수 이벤트의 정확한 기록
        int handAtVictory = -1; // 승리가 발생할 때까지 완료할 공용 손패의 실수량
        fight.Turns.BattleCompleted += _ => handAtVictory = fight.Deck.HandCount; // 실제 승리 처리 후 손패 정산 상태를 관찰
        fight.View("FUSION_OF_POWER").OnPointerClick(null); // 전체 아군 대상으로 실제 혼합 카드 한 장 자동 사용
        yield return Wait(fight); // 피해와 회복 및 사용 카드 이동 완료까지 대기
        Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 실제 마지막 적 피해 이후 정상 승리 확인
        Assert.AreEqual(15, fight.Points.CurrentActionPoints); // 다섯 행동력의 실제 한 번 결제 결과
        Assert.AreEqual(15, observed.RawDamage); // 마법 및 물리 계수와 무관한 원래 일반 피해 열다섯 전달
        Assert.AreEqual(3, observed.AbsorbedDamage); // 같은 일반 피해가 원래 방어도 세 점을 흡수한 결과 확인
        Assert.AreEqual(20, fight.Allies[0].CurrentHealth); // 시전자도 실제 살아 있는 아군 회복에 포함 확인
        Assert.AreEqual(20, fight.Allies[1].CurrentHealth); // 다른 생존 아군의 실제 열 점 회복 확인
        Assert.Zero(fight.Allies[2].CurrentHealth); // 쓰러진 같은 진영의 체력 회복 미적용
        Assert.AreEqual(3, handAtVictory); // 원래 남은 한 장에 두 장을 한 번 드로우한 뒤 승리 이벤트
        foreach (var extra in extras) // 변하지 않은 실제 개별 카드 이동 확인
        { // 원래 드로우한 카드의 손패 검사 시작
            CollectionAssert.Contains(fight.Deck.Hand, extra); // 아군별 반복이 아닌 실제 공유 카드 한 장 도착 확인
        } // 원래 드로우한 카드의 손패 검사 종료
        CollectionAssert.Contains(fight.Deck.DiscardPile, fusion); // 승리 전에 사용한 원본의 실제 버림 정산 확인
    } // 합성 카드의 실제 혼합 효과 검사 종료

    private static IEnumerator VerifySupplies() // 한 번의 공용 드로우와 지정 실회복 없이도 작동하는 독립 효과
    { // 보급 카드의 두 원본 대조 시작
        using (var secure = new Fight("SECURE_SUPPLIES", "GOLDEN_COIN")) // 정상 방어와 드로우가 함께 나가는 원래 지원 카드
        { // 보급 확보 기본 효과 확인 시작
            var recovered = secure.View("GOLDEN_COIN").RuntimeCard; // 실제로 손패에 돌려 줄 공용 한 장
            Assert.IsTrue(secure.Deck.DiscardCard(recovered)); // 기본 덱 내부의 한 장을 꽂아 둘 실제 버림 실행
            yield return secure.Use("SECURE_SUPPLIES"); // 원래 행동력 비용 하나의 실제 카드 클릭과 정산
            Assert.AreEqual(3, secure.Allies[0].CurrentBlock); // 자신에게 주는 실제 기본 방어도 세 점 확인
            CollectionAssert.Contains(secure.Deck.Hand, recovered); // 다른 생존 아군의 수와 무관한 공유 드로우 하나 확인
        } // 보급 확보 기본 효과 확인 종료
        using var temporary = new Fight("TEMPORARY_SUPPLIES"); // 지정 생존 아군 회복과 별도 환급 확률 확인
        var owner = temporary.Allies[0]; // 선택 대상 외의 실제 소유자
        var target = temporary.Allies[1]; // 현재 최대 체력으로 실회복 영인 실제 대상
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 독립 확률만 비교할 실제 원본 정신력 준비
        Assert.IsTrue(temporary.Points.Spend(16)); // 지불 및 환급을 볼 실제 남은 네 점 행동력
        int seed = BoundarySeed(29); // 독립 서른 퍼센트에서 정확히 성공하는 실제 난수 시드
        temporary.Controller.CardUsed += (_, __) => Random.InitState(seed); // 실제 충돌 직전의 확률 경계 난수 지정
        var view = temporary.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(item => item.RuntimeUnit == target); // 원래 실제 지정 대상 화면
        temporary.View("TEMPORARY_SUPPLIES").OnPointerClick(null); // 실제 생존 아군 선택이 대기하는 카드 입력
        view.OnPointerClick(null); // 최대 체력 동료의 실제 선택을 완료
        yield return Wait(temporary); // 실제 공용 행동력의 독립 지급까지 대기
        Assert.AreEqual(target.MaxHealth, target.CurrentHealth); // 유효한 지정에도 현재 체력이 실제 상한 유지
        Assert.AreEqual(4, temporary.Points.CurrentActionPoints); // 실회복 영과 독립된 성공 확률로 한 점 환급 확인
    } // 보급 카드의 두 원본 대조 종료

    private static int BoundarySeed(int result) // 성공 임계값의 실제 Unity 난수를 찾는 검사 도우미
    { // 작은 후보 집합의 실제 난수 원본 순회 시작
        for (int seed = 0; seed < 10000; seed++) // 실제 범위의 요청 결과를 맞출 시드 검색
        { // 개별 난수 순서 확인 시작
            Random.InitState(seed); // 카드 충돌에서 사용할 원래 상태 구성
            if (Random.Range(0, 100) == result) // 원본과 같을 다음 실제 경계 결과 확인
            { // 정확한 확률 결과 발견 시작
                return seed; // 실제 충돌에 다시 넣을 일치 시드 반환
            } // 정확한 확률 결과 발견 종료
        } // 개별 난수 순서 확인 종료
        Assert.Fail("실제 독립 상태 확률의 경계 시드 누락"); // 난수 시드를 만들지 못한 테스트 준비 실패
        return 0; // 유효한 재생값이 없으면 형식상 필요한 반환
    } // 작은 후보 집합의 실제 난수 원본 순회 종료

    private static IEnumerator Wait(Fight fight) // 실제 타격 및 자원과 지원 연출을 기다리는 공통 절차
    { // 물리적 사용 완료 대기 시작
        float deadline = Time.realtimeSinceStartup + 12f; // 전체 실제 효과의 완료를 기다릴 시간 경계
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 입력 처리의 충돌과 모션 대기
        { // Unity 시전 프레임 대기 시작
            yield return null; // 카드 연출과 효과 및 이벤트 진행 허용
        } // Unity 시전 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 실제 전투 카드 정산 완료 확인
    } // 물리적 사용 완료 대기 종료
} // 일반 카드 사용 회귀 종료
