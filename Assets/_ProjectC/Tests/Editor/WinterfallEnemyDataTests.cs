using System.Collections.Generic; // 적 이미지 중복 확인 집합 사용
using NUnit.Framework; // 실제 적과 조우 데이터 검증 사용
using UnityEngine; // 리소스와 스프라이트 사용
public sealed class WinterfallEnemyDataTests // 윈터폴 초기 적 데이터 검사
{ // 적 데이터 검사 시작
    [Test] // 임시 조우의 신규 탐사 제외 검사 지정
    public void PrototypeEncounters_StayAvailableForLegacySavesOnly() // 이전 조우와 임시 보스 호환 확인
    { // 조우 버전 확인 시작
        foreach (string name in new[] { "A", "B" }) // 일반과 정예 임시 조우 순회
        { // 임시 조우 확인 시작
            EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_Test_" + name); // 기존 임시 조우 조회
            Assert.IsNotNull(encounter); // 기존 임시 조우 존재 확인
            Assert.IsTrue(encounter.IsAvailableForPoolVersion(0)); // 이전 탐사 호환 확인
            Assert.IsFalse(encounter.IsAvailableForPoolVersion(1)); // 새 탐사 임시 적 제외 확인
        } // 임시 조우 확인 종료
        EncounterData boss = Resources.Load<EncounterData>("Encounters/Encounter_Test_C"); // 기존 임시 보스 조회
        Assert.IsNotNull(boss); // 임시 보스 존재 확인
        Assert.IsTrue(boss.IsAvailableForPoolVersion(0)); // 이전 탐사의 임시 보스 확인
        Assert.IsTrue(boss.IsAvailableForPoolVersion(1)); // 새 보스 구현 전 임시 보스 유지 확인
        Assert.IsFalse(boss.IsAvailableForPoolVersion(2)); // 새 보스 적용 탐사의 임시 보스 제외 확인
    } // 조우 버전 확인 종료
    [Test] // 실제 적 행동 생성 시 추가 상태 확인 지정
    public void HunterAction_PreservesBurnChanceAndDuration() // 실제 예정 공격의 화상 예고 값 확인
    { // 실제 예정 행동 확인 시작
        EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter"); // 사냥꾼 실제 조우 조회
        Assert.IsNotNull(encounter); // 사냥꾼 조우 존재 확인
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(encounter.Enemies[0]); // 사냥꾼 전투 유닛 생성
        CharacterData allyData = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(allyData); // 임시 아군 생성
        BattleEnemyActionRuntime actions = new BattleEnemyActionRuntime(new[] { enemy }, new[] { ally }); // 실제 적 행동 관리자 생성
        Assert.AreEqual(1, actions.PrepareActions()); // 사냥꾼 예정 행동 생성 확인
        BattleEnemyAction action = actions.FindAction(enemy); // 생성된 사냥꾼 행동 조회
        Assert.AreEqual(BattleStatusEffectType.Burn, action.StatusEffectType); // 화상 부여 종류 확인
        Assert.AreEqual(2, action.AdditionalStatusValue); // 승인된 초기 화상 수치 확인
        Assert.AreEqual(2, action.StatusDuration); // 화상 지속 두 턴 확인
        Assert.AreEqual(50, action.AdditionalStatusChancePercent); // 기획 화상 확률 확인
        actions.Dispose(); // 적 행동 관리자 정리
        Object.DestroyImmediate(allyData); // 임시 아군 원본 정리
    } // 실제 예정 행동 확인 종료
    [Test] // 기획 적 일곱 종과 기본 패턴 검사 지정
    public void Encounters_LoadSevenDistinctEnemiesWithDocumentedOpeningAttacks() // 일반 적 다섯과 정예 둘의 초기 데이터 확인
    { // 적 데이터 확인 시작
        string[] names = { "AshHunter", "BrokenScarecrow", "AshMaggot", "AshWanderer", "AshTracker", "AshSentinel", "AshBellkeeper" }; // 검사할 적 리소스 이름
        string[] labels = { "잿풍의 사냥꾼", "망가진 허수아비", "재먹는 구더기", "잿빛 방랑자", "재폭풍 추적자", "재의 파수꾼", "잿더미 종지기" }; // 적 기획 이름
        int[] health = { 42, 58, 32, 50, 46, 130, 145 }; // 쉬움 체력 수치
        int[] defense = { 2, 4, 1, 2, 2, 8, 6 }; // 쉬움 물리 방어 수치
        int[] resistance = { 5, 10, 5, 15, 10, 10, 20 }; // 승인된 임시 마법 저항 수치
        int[] attack = { 10, 9, 7, 8, 11, 12, 12 }; // 기획의 첫 기본 공격 수치
        int[] chance = { 50, 0, 0, 40, 40, 40, 0 }; // 구현된 공격 후 상태 확률
        BattleStatusEffectType[] effects = { BattleStatusEffectType.Burn, BattleStatusEffectType.None, BattleStatusEffectType.None, BattleStatusEffectType.Blind, BattleStatusEffectType.Blind, BattleStatusEffectType.Burn, BattleStatusEffectType.None }; // 구현된 공격 후 상태 종류
        HashSet<Sprite> portraits = new HashSet<Sprite>(); // 적별 전투 그림 중복 확인 집합
        for (int index = 0; index < names.Length; index++) // 기획 적 목록 순회
        { // 개별 적 데이터 확인 시작
            EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_WF_" + names[index]); // 실제 탐사 조우 조회
            Assert.IsNotNull(encounter, names[index]); // 조우 리소스 존재 확인
            Assert.IsTrue(encounter.IsValidData(), names[index]); // 조우 유효성 확인
            Assert.IsFalse(encounter.IsAvailableForPoolVersion(0), names[index]); // 이전 탐사에서 새 조우 제외 확인
            Assert.IsTrue(encounter.IsAvailableForPoolVersion(1), names[index]); // 새 탐사에서 조우 허용 확인
            Assert.AreEqual(index < 5 ? BattleType.Normal : BattleType.Elite, encounter.BattleType, names[index]); // 일반과 정예 등급 확인
            Assert.AreEqual(1, encounter.Enemies.Count, names[index]); // 단독 초기 조우 확인
            EnemyData enemy = encounter.Enemies[0]; // 실제 조우 적 원본 조회
            Assert.AreEqual(labels[index], enemy.DisplayName); // 기획 이름 일치 확인
            Assert.AreEqual(health[index], enemy.MaxHealth, names[index]); // 쉬움 체력 확인
            Assert.AreEqual(defense[index], enemy.PhysicalDefense, names[index]); // 물리 방어 수치 확인
            Assert.AreEqual(resistance[index], enemy.MagicalResistance, names[index]); // 임시 마법 저항 수치 확인
            Assert.IsNotNull(enemy.Portrait, names[index]); // 적 전투 이미지 연결 확인
            Assert.IsTrue(portraits.Add(enemy.Portrait), names[index]); // 서로 다른 적의 이미지 확인
            Assert.AreEqual(1, enemy.ActionPatternCount, names[index]); // 초기 구현 패턴 하나 확인
            EnemyActionPatternData pattern = enemy.GetActionPattern(0); // 첫 기본 공격 패턴 조회
            Assert.IsTrue(pattern.IsValid, names[index]); // 실제 실행 가능한 공격 설정 확인
            Assert.AreEqual(attack[index], pattern.ActionAmount, names[index]); // 기획 기본 공격 피해 확인
            Assert.AreEqual(EnemyTargetRule.RandomLiving, pattern.TargetRule, names[index]); // 승인된 무작위 생존 대상 확인
            Assert.AreEqual(3, pattern.MinimumActionSpeed, names[index]); // 승인된 최소 행동 속도 확인
            Assert.AreEqual(7, pattern.MaximumActionSpeed, names[index]); // 승인된 최대 행동 속도 확인
            Assert.AreEqual(effects[index], pattern.StatusEffectType, names[index]); // 현재 구현된 추가 상태 종류 확인
            Assert.AreEqual(chance[index], pattern.StatusEffectChancePercent, names[index]); // 현재 구현된 추가 상태 확률 확인
        } // 개별 적 데이터 확인 종료
    } // 적 데이터 확인 종료
} // 적 데이터 검사 종료
