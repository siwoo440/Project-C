using System.Collections; // 실제 적 행동 화면 대기 사용
using System.IO; // 투명 아이콘 원본 검사 사용
using NUnit.Framework; // 공격 유형과 투명도 검증 사용
using TMPro; // 예상 피해 숫자 검사 사용
using UnityEngine; // 실제 전투 데이터 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 적 행동 이미지 검사 사용
public sealed class BattleEnemyAttackIconTests // 적 공격 유형 아이콘 검사
{ // 공격 유형 아이콘 검사 시작
    [UnityTest] // 실제 적 행동 표시 검사 지정
    public IEnumerator AttackTypesUseTransparentSymbolsAndKeepDamage() // 공격 종류별 아이콘과 피해 표시 확인
    { // 공격 유형 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 플레이 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 아군 원본 조회
        BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 실제 아군 전투 유닛 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 데이터 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 전투 유닛 생성
        GameObject root = new GameObject("Intent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleEnemyIntentIconView)); // 실제 적 행동 표시 생성
        BattleEnemyIntentIconView view = root.GetComponent<BattleEnemyIntentIconView>(); // 실제 적 행동 화면 조회
        Image icon = root.GetComponent<Image>(); // 실제 공격 아이콘 이미지 조회
        TMP_Text amount = root.transform.Find("IntentIconText").GetComponent<TMP_Text>(); // 실제 예상 피해 수치 조회
        BattleDamageType[] types = { BattleDamageType.Physical, BattleDamageType.Magical, BattleDamageType.None }; // 실제 피해 종류 목록
        string[] names = { "EnemyAttackPhysical", "EnemyAttackMagical", "EnemyAttackNeutral" }; // 피해 종류별 제작 이미지 이름
        for (int index = 0; index < types.Length; index++) // 피해 유형별 표시 검사
        { // 개별 공격 유형 검사 시작
            BattleEnemyAction action = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, types[index], 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.RandomLiving, "공격", 1, 1, 3, 0); // 실제 유형의 적 공격 생성
            view.Bind(action, null, false); // 실제 적 공격 예고 연결
            Assert.IsNotNull(icon.sprite); // 공격 유형별 아이콘 존재 확인
            Assert.AreEqual(names[index], icon.sprite.texture.name); // 실제 피해 유형과 이미지 일치 확인
            Assert.IsTrue(icon.raycastTarget); // 기존 상세 설명 입력 유지 확인
            Assert.IsTrue(int.TryParse(amount.text, out int shown)); // ATK 없이 예상 피해 숫자만 표시 확인
            Assert.AreEqual(action.PreviewDamage().AppliedDamage, shown); // 실제 예상 피해와 숫자 일치 확인
            Assert.IsFalse(amount.raycastTarget); // 숫자의 아이콘 입력 차단 방지 확인
            string path = "Assets/_ProjectC/Resources/UI/Battle/" + names[index] + ".png"; // 실제 생성 아이콘 원본 경로
            Texture2D raw = new Texture2D(2, 2); // 원본 투명도 검사 텍스처 생성
            Assert.IsTrue(raw.LoadImage(File.ReadAllBytes(path))); // 실제 PNG 원본 로드 확인
            Assert.AreEqual(0, raw.GetPixel(0, 0).a, 0.01f); // 사각 이미지 바깥 모서리 투명 확인
            Object.DestroyImmediate(raw); // 임시 검사 텍스처 정리
        } // 개별 공격 유형 검사 종료
        BattleEnemyAction special = new BattleEnemyAction(enemy, ally, EnemyActionType.Attack, BattleDamageType.Magical, 10, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.RandomLiving, "특수 공격", 1, 1, 3, 0); // 보스 특수 공격 생성
        view.Bind(special, null, true); // 실제 특수 공격 표시 연결
        Assert.AreEqual("EnemyAttackMagical", icon.sprite.texture.name); // 특수 공격의 실제 피해 유형 유지 확인
        Assert.IsTrue(root.transform.Find("SpecialMark").gameObject.activeSelf); // 보스 특수 행동 구분 표시 확인
        BattleEnemyAction status = new BattleEnemyAction(enemy, ally, EnemyActionType.ApplyStatusEffect, BattleDamageType.None, 2, BattleStatusEffectType.Poison, 2, 1, EnemyTargetRule.RandomLiving, "중독", 1, 1, 3, 0); // 실제 상태 이상 행동 생성
        view.Bind(status, null, false); // 일반 상태 이상 행동 연결
        Assert.IsNull(icon.sprite); // 이전 공격 아이콘의 상태 행동 표시 방지 확인
        Assert.IsTrue(amount.text.StartsWith("FX")); // 기존 상태 이상 예고 유지 확인
        Assert.IsFalse(root.transform.Find("SpecialMark").gameObject.activeSelf); // 일반 상태 행동의 특수 기호 숨김 확인
        Object.DestroyImmediate(root); // 테스트 행동 표시 정리
        Object.DestroyImmediate(enemyData); // 임시 적 데이터 정리
        yield return new ExitPlayMode(); // 실제 전투 플레이 종료
    } // 공격 유형 검사 종료
} // 적 공격 유형 아이콘 검사 종료
