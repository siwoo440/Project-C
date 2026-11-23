using NUnit.Framework; // 자동 검증 사용
using TMPro; // 체력과 정신력 글자 사용
using UnityEditor; // 실제 유닛 프리팹 조회
using UnityEngine; // 전투 유닛과 화면 사용
using UnityEngine.UI; // 실제 게이지 표시 사용
public sealed class BattleVitalGaugeArtTests // 체력과 정신력 디자인 검사
{ // 게이지 검사 시작
    [Test] // 실제 전투 프리팹 검사 지정
    public void VitalGaugesUseGeneratedFramesAndKeepValuesInside() // 프레임과 수치 위치 확인
    { // 게이지 검사 시작
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 실제 전투 프리팹 조회
        Assert.IsNotNull(prefab); // 실제 프리팹 존재 확인
        GameObject instance = Object.Instantiate(prefab); // 실제 전투 화면 생성
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 실제 아군 생성
        instance.GetComponent<BattleUnitView>().Bind(unit); // 전투 화면에 아군 연결
        Slider health = instance.GetComponentInChildren<Slider>(); // 실제 체력 바 조회
        Assert.IsNotNull(health); // 체력 바 존재 확인
        TMP_Text healthText = health.transform.Find("HealthText").GetComponent<TMP_Text>(); // 바 내부 체력 글자 조회
        Assert.AreEqual($"{unit.CurrentHealth} / {unit.MaxHealth}", healthText.text); // 실제 체력 글자 확인
        Image healthFrame = health.transform.Find("HealthGaugeFrame").GetComponent<Image>(); // 생성된 체력 프레임 조회
        Assert.AreEqual("VitalGaugeFrame", healthFrame.sprite.texture.name); // 공통 투명 프레임 이미지 확인
        Assert.IsFalse(healthFrame.raycastTarget); // 체력 프레임 입력 차단 방지 확인
        Image healthIcon = health.transform.Find("HealthGaugeIcon").GetComponent<Image>(); // 별도 하트 아이콘 조회
        Assert.AreEqual("HealthGaugeIcon", healthIcon.sprite.texture.name); // 분리된 하트 아이콘 확인
        Assert.IsFalse(healthIcon.raycastTarget); // 하트 아이콘 입력 차단 방지 확인
        Transform mental = instance.transform.Find("MentalView"); // 실제 정신력 바 조회
        Assert.IsNull(mental.Find("CenterMarker")); // 숫자와 겹치는 중앙 기준선 제거 확인
        Image mentalFrame = mental.Find("MentalGaugeFrame").GetComponent<Image>(); // 생성된 정신력 프레임 조회
        Assert.AreEqual("VitalGaugeFrame", mentalFrame.sprite.texture.name); // 공통 투명 프레임 이미지 확인
        Assert.IsFalse(mentalFrame.raycastTarget); // 정신력 프레임 입력 차단 방지 확인
        Image mentalIcon = mental.Find("MentalGaugeIcon").GetComponent<Image>(); // 별도 정신력 아이콘 조회
        Assert.AreEqual("MentalGaugeIcon", mentalIcon.sprite.texture.name); // 분리된 정신력 아이콘 확인
        Assert.IsFalse(mentalIcon.raycastTarget); // 정신력 아이콘 입력 차단 방지 확인
        TMP_Text mentalText = mental.Find("MentalText").GetComponent<TMP_Text>(); // 바 내부 정신력 글자 조회
        Assert.AreEqual(unit.CurrentMental.ToString(), mentalText.text); // 실제 정신력 숫자 확인
        Assert.IsTrue(unit.TrySpendHealth(unit.MaxHealth / 2)); // 실제 체력 감소 적용
        Assert.AreEqual(unit.CurrentHealth, health.value); // 체력 감소 바 반영 확인
        StringAssert.StartsWith(unit.CurrentHealth.ToString(), healthText.text); // 체력 감소 글자 반영 확인
        Assert.IsTrue(unit.ApplyPersistentMental(75)); // 실제 정신력 증가 적용
        StringAssert.Contains("75", mentalText.text); // 정신력 증가 글자 반영 확인
        Object.DestroyImmediate(instance); // 전투 화면 정리
    } // 게이지 검사 종료
} // 검사 종료
