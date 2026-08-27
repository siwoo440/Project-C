using NUnit.Framework; // Unity Editor 테스트 기능 사용

public sealed class RestRoomHazardLevelTests // 57일차 위험도별 휴식 규칙 테스트
{
    [TestCase(0, 25)]
    [TestCase(1, 20)]
    [TestCase(2, 15)]
    [TestCase(3, 10)]
    public void GetHealthRecoveryPercent_UsesHazardLevel(int hazardLevel, int expectedPercent) // 위험도별 HP 회복률 확인
    {
        Assert.AreEqual(expectedPercent, RestRoomRecoveryService.GetHealthRecoveryPercent(hazardLevel)); // 예상 회복률 확인
    }

    [Test]
    public void CalculateRecoveredHealth_ClampsToMaximumHealth() // 최대 HP 초과 방지 확인
    {
        int result = RestRoomRecoveryService.CalculateRecoveredHealth(95, 100, 0); // 안전 휴식 회복 계산
        Assert.AreEqual(100, result); // 최대 HP 제한 확인
    }

    [Test]
    public void CalculateRecoveredHealth_LevelThreeUsesTenPercent() // Lv3 휴식 HP 10% 확인
    {
        int result = RestRoomRecoveryService.CalculateRecoveredHealth(40, 100, 3); // Lv3 휴식 회복 계산
        Assert.AreEqual(50, result); // 10 HP 회복 결과 확인
    }
}
