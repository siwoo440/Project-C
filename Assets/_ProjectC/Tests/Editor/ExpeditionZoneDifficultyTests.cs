using NUnit.Framework; // 기존 및 실제 레벨별 피해 수치 판정
using UnityEngine; // 원래 Unity 체력과 공격 반올림 비교

public sealed class ExpeditionZoneDifficultyTests // 지역 출정 난이도의 실제 네이티브 전투 수치
{ // 단계 수치와 기존 방식의 호환 검사 시작
    [TestCase(1, 1d)] // 기존 출정의 전체 원래 수치
    [TestCase(2, 1.10d)] // 첫 상승의 열 퍼센트
    [TestCase(3, 1.265d)] // 이전 값의 추가 십오 퍼센트
    [TestCase(4, 1.518d)] // 이전 값의 추가 이십 퍼센트
    [TestCase(5, 1.8975d)] // 이전 값의 추가 이십오 퍼센트
    [TestCase(6, 2.08725d)] // 여섯 번째의 이전 값 추가 십 퍼센트
    [TestCase(7, 2.295975d)] // 여섯 번째 이후 동일 추가 십 퍼센트
    public void SelectedLevelCompoundsTheAuthoredStatSteps(int level, double multiplier) // 실제 적 체력과 공격에 같은 레벨 증가 적용
    { // 출정 레벨 증가의 해당 배율 및 체력과 공격 비교 시작
        Assert.AreEqual(multiplier, ExplorationDifficultyCalculator.GetZoneLevelStatMultiplier(level), 0.000001d); // 정해진 각 레벨 단계의 실효 누적 배율
        Assert.AreEqual((int)System.Math.Round(1000 * multiplier), ExplorationDifficultyCalculator.ScaleHealth(1000, 1, BattleType.Normal, level)); // 기존 체력 수식에 실제 배율 적용
        Assert.AreEqual((int)System.Math.Round(1000 * multiplier), ExplorationDifficultyCalculator.ScaleAttack(1000, 1, BattleType.Normal, level)); // 실제 기본 및 패턴 공격에 같은 배율 적용
    } // 출정 레벨 증가의 해당 배율 및 체력과 공격 비교 종료

    [Test] // 기존 저장 및 수치 상한의 원래 행동
    public void LevelOnePreservesFloorAndGradeAndHigherLevelsLayerOnTop() // 기존 보스와 정예에 적용한 실제 층 및 등급 함께 유지
    { // 기존 실제 Unity 수치와 높은 레벨의 상한 대조 시작
        const int floor = 5; // 층 효과와 조우 등급도 같이 적용할 대표 탐사 층
        float bossHealth = 350 * ExplorationDifficultyCalculator.GetCombinedHealthMultiplier(floor, BattleType.Boss); // 실제 기존 윈터폴 원본의 층과 등급 체력
        float eliteAttack = 17 * ExplorationDifficultyCalculator.GetCombinedAttackMultiplier(floor, BattleType.Elite); // 실제 기존 정예의 반올림 전 공격값
        Assert.AreEqual(Mathf.RoundToInt(bossHealth), ExplorationDifficultyCalculator.ScaleHealth(350, floor, BattleType.Boss, 1)); // 기존 저장과 같은 정확한 보스 체력
        Assert.AreEqual(Mathf.RoundToInt(eliteAttack), ExplorationDifficultyCalculator.ScaleAttack(17, floor, BattleType.Elite, 1)); // 기존 정예 공격 수치의 변화 없음
        Assert.AreEqual((int)System.Math.Round(bossHealth * 1.265d), ExplorationDifficultyCalculator.ScaleHealth(350, floor, BattleType.Boss, 3)); // 새 지역 레벨이 기존 층 및 등급과 곱으로 적용
        Assert.AreEqual(int.MaxValue, ExplorationDifficultyCalculator.ScaleHealth(int.MaxValue, floor, BattleType.Boss, int.MaxValue)); // 범위를 넘는 체력과 지역 레벨의 정수 상한
        Assert.AreEqual(int.MaxValue, ExplorationDifficultyCalculator.ScaleAttack(int.MaxValue, floor, BattleType.Elite, int.MaxValue)); // 범위를 넘는 공격 수치의 정수 상한
        Assert.AreEqual(0, ExplorationDifficultyCalculator.ScaleAttack(0, floor, BattleType.Elite, 5)); // 원래 공격을 갖지 않은 행동 수치 보존
    } // 기존 실제 Unity 수치와 높은 레벨의 상한 대조 종료
} // 단계 수치와 기존 방식의 호환 검사 종료
