using NUnit.Framework; // Unity Editor 테스트 기능 사용

public sealed class RestRoomPrototypeTests // 55일차 휴식 Prototype 규칙 테스트
{
    [Test]
    public void NormalRest_RecoversTwentyFivePercentOfMaximumHealth() // 일반 휴식 최대 HP 25퍼센트 회복 확인
    {
        int result = RestRoomRecoveryService.CalculateRecoveredHealth(40, 100, false); // 일반 휴식 회복 계산
        Assert.AreEqual(65, result); // 40에서 65 회복 결과 확인
    }

    [Test]
    public void HighRiskRest_RecoversFifteenPercentOfMaximumHealth() // 고위험 휴식 최대 HP 15퍼센트 회복 확인
    {
        int result = RestRoomRecoveryService.CalculateRecoveredHealth(40, 100, true); // 고위험 휴식 회복 계산
        Assert.AreEqual(55, result); // 40에서 55 회복 결과 확인
    }

    [Test]
    public void RestHealth_DoesNotExceedMaximumHealth() // 휴식 체력 최대치 제한 확인
    {
        int result = RestRoomRecoveryService.CalculateRecoveredHealth(90, 100, false); // 최대치 근처 회복 계산
        Assert.AreEqual(100, result); // 최대 체력 제한 확인
    }

    [Test]
    public void DeadAlly_IsNotRevivedByRest() // 사망 파티원 휴식 부활 차단 확인
    {
        int healthResult = RestRoomRecoveryService.CalculateRecoveredHealth(0, 100, false); // 사망 체력 회복 계산
        int mentalResult = RestRoomRecoveryService.CalculateRecoveredMental(30, true); // 사망 정신력 회복 계산

        Assert.AreEqual(0, healthResult); // 사망 체력 유지 확인
        Assert.AreEqual(30, mentalResult); // 사망 정신력 유지 확인
    }

    [Test]
    public void RestMental_RecoversFifteenAndClampsToMaximum() // 정신력 15 회복과 최대치 제한 확인
    {
        int normalResult = RestRoomRecoveryService.CalculateRecoveredMental(40, false); // 일반 정신력 회복 계산
        int cappedResult = RestRoomRecoveryService.CalculateRecoveredMental(
            BattleMentalRuntime.MaximumMental - 5,
            false); // 최대치 근처 정신력 회복 계산

        Assert.AreEqual(55, normalResult); // 정신력 +15 확인
        Assert.AreEqual(BattleMentalRuntime.MaximumMental, cappedResult); // 정신력 최대치 제한 확인
    }

    [Test]
    public void UpgradedCard_IncreasesEffectValueByTwentyFivePercent() // 카드 강화 효과값 25퍼센트 증가 확인
    {
        int result = CardInstance.CalculateUpgradedEffectValue(20, 1); // 강화 효과 수치 계산
        Assert.AreEqual(25, result); // 20에서 25 강화 결과 확인
    }
}
