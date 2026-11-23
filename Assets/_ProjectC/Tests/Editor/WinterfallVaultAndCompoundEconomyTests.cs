using System.Collections; // 실제 전투 카드 연출과 모드 전환
using System.Reflection; // 기존 독립 드로우 예약 접근
using NUnit.Framework; // 실제 자원과 카드 영역 검증
using UnityEngine; // 다음 프레임 대기와 연출 제한 시간
using UnityEngine.TestTools; // 실제 Unity 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 게임 손패와 실제 입력 도우미

public sealed class WinterfallVaultAndCompoundEconomyTests // 고정 대가와 아군 턴 자원 효과 회귀
{ // 자원 효과 검사 시작
    [UnityTest] // 두 금고의 예약과 다음 실제 아군 드로우 확인
    public IEnumerator SecretVaultStacksOnlyTheNextBaseDrawReduction() // 기본 드로우 두 장 차감과 별도 한 장 보너스 확인
    { // 두 금고의 정상 사용 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 전투 시작
        yield return VerifySecretVault(); // 적 턴 대비 한 번만 적용되는 기본 드로우 확인
        yield return new ExitPlayMode(); // 검사 장면 정리
    } // 두 금고의 정상 사용 검사 종료

    [UnityTest] // 실제 체력 비용과 AP 지급의 선후 확인
    public IEnumerator PayThePriceRequiresThirteenHealthAndLeavesOne() // 체력 열둘에서 불가하고 열셋에서 생존 확인
    { // 체력 선결제 검사 시작
        yield return new EnterPlayMode(); // 실제 입력과 카드 사용 준비
        yield return VerifyHealthPrice(); // 지불 불가 보존과 한 번의 유효 지불 확인
        yield return new ExitPlayMode(); // 검사 장면 정리
    } // 체력 선결제 검사 종료

    [UnityTest] // 행동력 지급과 상한 및 턴 종료 경계 확인
    public IEnumerator CompoundEffectStacksForRealGainsAndExpiresBeforeTheEnemyTurn() // 실제 카드 지급과 일반 회복만 아군 턴 안에서 증폭
    { // 복리 효과 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 행동력 런타임 준비
        yield return VerifyCompoundEffect(); // 정상 증가와 예비 자금 및 종료 차이 확인
        yield return new ExitPlayMode(); // 검사 장면 정리
    } // 복리 효과 검사 종료

    private static IEnumerator VerifySecretVault() // 실제 손패의 금고 두 장 사용
    { // 다음 기본 드로우 예약 검사 시작
        using var fight = new Fight("SECRET_VAULT", "SECRET_VAULT", "GOLDEN_COIN", "CHARGE", "RUNE_RECORDING"); // 기본 드로우 다섯 장의 실제 원본
        Assert.AreEqual(5, fight.Turns.LastDrawnCardCount); // 현재 기본 손패 다섯 장 확인
        Assert.IsTrue(fight.Points.Spend(1)); // 상한 아래에서 금고의 실제 회복량 검증 준비
        yield return Play(fight, "SECRET_VAULT"); // 첫 금고의 기본 비용과 자체 회복 정산
        Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 기본 비용 둘을 지불한 뒤 행동력 둘 회복
        yield return Play(fight, "SECRET_VAULT"); // 같은 아군 턴의 두 번째 금고 사용
        Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 두 번째 사용의 독립 회복량 확인
        Assert.AreEqual(5, fight.Turns.LastDrawnCardCount); // 두 차감 모두 이번 손패에는 영향 없음
        var queueBonus = typeof(BattleDeckRuntime).GetMethod("QueuePlayerTurnDraw", BindingFlags.Instance | BindingFlags.NonPublic); // 별도 예약 드로우의 기존 진입점 조회
        Assert.IsNotNull(queueBonus); // 테스트가 실제 예약 경로를 사용하는지 확인
        queueBonus.Invoke(fight.Deck, new object[] { 1 }); // 기본과 독립인 정상 예약 드로우 한 장 준비
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 차감을 유지한 채 실제 적 턴 진입
        Assert.AreEqual(5, fight.Turns.LastDrawnCardCount); // 적 턴 시작에서 드로우 미집행 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 기본 드로우 지급
        Assert.AreEqual(4, fight.Turns.LastDrawnCardCount); // 기본 다섯 장 중 세 장과 별도 예약 한 장 확인
        Assert.AreEqual(4, fight.Deck.HandCount); // 실제 공동 손패에 도착한 네 장 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 새 금고를 쓰지 않은 실제 아군 턴 종료
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 아군 경계에서 기본 드로우 복원
        Assert.AreEqual(5, fight.Turns.LastDrawnCardCount); // 예약 차감이 한 아군 턴에만 적용됐는지 확인
        Assert.IsTrue(fight.Deck.IsStateValid()); // 여러 드로우와 버림 뒤 실제 영역 일관성 확인
    } // 다음 기본 드로우 예약 검사 종료

    private static IEnumerator VerifyHealthPrice() // 실제 대가 카드의 비용과 공용 행동력
    { // 체력 경계의 실제 카드 입력 시작
        using var fight = new Fight("PAY_THE_PRICE"); // 원본 수치가 있는 카드 한 장의 전투
        var owner = fight.Allies[0]; // 체력을 부담하는 실제 원본 소유자
        Assert.IsTrue(owner.ApplyPersistentHealth(12)); // 생존 비용과 같은 체력으로 준비
        owner.GainBlock(7); // 체력과 별개로 유지할 방어도 준비
        Assert.IsTrue(fight.Points.Spend(4)); // 회복이 상한에 잘리지 않을 공용 행동력 준비
        var card = fight.View("PAY_THE_PRICE").RuntimeCard; // 실패 뒤 손패 보존을 확인할 실물
        fight.View("PAY_THE_PRICE").OnPointerClick(null); // 생존할 수 없는 원본을 정상 입력에서 시도
        yield return null; // 취소가 실제 손패에 반영될 프레임 대기
        Assert.AreEqual(12, owner.CurrentHealth); // 불가 원본에서 체력 대가 없음 확인
        Assert.AreEqual(16, fight.Points.CurrentActionPoints); // 불가 원본에서 행동력 결제 및 회복 없음 확인
        CollectionAssert.Contains(fight.Deck.Hand, card); // 불가 원본이 실제 공동 손패에 유지됐는지 확인
        Assert.AreEqual(1, owner.RestoreHealth(1)); // 현재 손패에도 알릴 생존 가능 체력 회복
        yield return Play(fight, "PAY_THE_PRICE"); // 생존 조건을 만족한 원본 한 번 사용
        Assert.AreEqual(1, owner.CurrentHealth); // 고정 열둘을 선결제하고 남은 원래 생존 체력
        Assert.AreEqual(7, owner.CurrentBlock); // 실제 공격 피해를 거치지 않은 방어도 보존
        Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 비용 셋을 지불한 뒤 공용 네 점 회복
    } // 체력 경계의 실제 카드 입력 종료

    private static IEnumerator VerifyCompoundEffect() // 카드와 행동력의 턴 한정 증가 검사
    { // 실제 복리 활성화 및 다음 턴 지급 검사 시작
        using var fight = new Fight("COMPOUND_EFFECT", "COMPOUND_EFFECT", "GOLDEN_COIN", "EMERGENCY_FUNDS"); // 복리 두 장과 기존 즉시 및 예약 지급
        yield return Play(fight, "EMERGENCY_FUNDS"); // 다음 실제 아군 턴으로 기존 예비 자금 예약
        yield return Play(fight, "COMPOUND_EFFECT"); // 첫 복리는 일반 비용만 지불
        int beforeCoin = fight.Points.CurrentActionPoints; // 동전으로 증가하기 전 실제 공용 행동력
        yield return Play(fight, "GOLDEN_COIN"); // 기존 무료 동전의 양수 행동력 회복
        Assert.AreEqual(beforeCoin + 2, fight.Points.CurrentActionPoints); // 원본 한 점과 복리 한 점의 실제 합
        yield return Play(fight, "COMPOUND_EFFECT"); // 같은 턴의 독립 복리 활성 한 건 추가
        Assert.AreEqual(0, fight.Points.Gain(0)); // 영점 요청에서 보너스를 만들지 않는지 확인
        Assert.AreEqual(3, fight.Points.Gain(1)); // 두 복리와 한 점의 실제 회복 합
        Assert.AreEqual(20, fight.Points.MaxActionPoints); // 복리가 원래 최대 행동력을 바꾸지 않음 확인
        int capacityLeft = 20 - fight.Points.CurrentActionPoints; // 원래 상한까지의 실제 회복 여유 확인
        Assert.AreEqual(capacityLeft, fight.Points.Gain(int.MaxValue)); // 큰 양수 합도 원래 상한까지만 회복
        Assert.AreEqual(0, fight.Points.Gain(1)); // 최대치에서 남은 증가가 쌓이지 않는지 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 현재 실제 아군 턴을 끝내고 적 턴으로 이동
        Assert.AreEqual(1, fight.Points.Gain(1)); // 아군 턴에 끝난 복리가 적 턴 지급에는 없음 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴의 원래 회복과 예비 자금 적용
        Assert.AreEqual(21, fight.Points.MaxActionPoints); // 예비 자금의 한 점만 원래 상한에 추가됐는지 확인
        Assert.AreEqual(21, fight.Points.CurrentActionPoints); // 기본 초기화와 예약 지급의 복리 제외 확인
        Assert.IsTrue(fight.Points.Spend(3)); // 다음 아군 턴의 정상 회복 여유 준비
        Assert.AreEqual(1, fight.Points.Gain(1)); // 새 실제 아군 턴에 이전 복리가 없음을 확인
    } // 실제 복리 활성화 및 다음 턴 지급 검사 종료

    private static IEnumerator Play(Fight fight, string id) // 정상 손패 클릭과 실제 사용 정산 확인
    { // 성공한 카드의 연출 대기 시작
        var view = fight.View(id); // 사용할 실제 화면 카드 선택
        var card = view.RuntimeCard; // 손패 이탈을 확인할 유일 카드 원본
        view.OnPointerClick(null); // 기존 손패 입력으로 자기 대상 사용
        float deadline = Time.realtimeSinceStartup + 12f; // 실제 카드 연출의 제한 시간
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 효과와 손패 이동의 완료 대기
        { // 실제 전투 프레임 대기 시작
            yield return null; // 기존 실제 연출 진행
        } // 실제 전투 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 카드 사용 연출 정산 확인
        CollectionAssert.DoesNotContain(fight.Deck.Hand, card); // 실제 사용한 카드의 정상 손패 이탈 확인
    } // 성공한 카드의 연출 대기 종료
} // 자원 효과 검사 종료
