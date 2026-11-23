using NUnit.Framework; // 정신력 표시 검사 사용
using TMPro; // 정신력 문구 검사 사용
using UnityEngine; // 유니티 객체와 색상 사용
using UnityEngine.UI; // 실제 게이지 이미지 사용

public sealed class BattleMentalGaugeColorTests // 정신력 게이지 색상 검사
{ // 정신력 색상 검사 시작
    [Test] // 실제 유닛 표시 검사 지정
    public void GaugeTracksMentalValueFromRedThroughNeutralToBlue() // 정신력 수치별 색상과 상태 변화 확인
    { // 수치별 색상 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 테스트 아군 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(source); // 실제 아군 정신력 생성
        GameObject root = new GameObject("MentalGaugeTest", typeof(RectTransform), typeof(BattleMentalView)); // 실제 정신력 화면 생성
        BattleMentalView view = root.GetComponent<BattleMentalView>(); // 정신력 화면 조회
        view.Bind(unit); // 실제 아군 정신력 연결
        Transform panel = root.transform.Find("MentalView"); // 실제 정신력 패널 조회
        Assert.IsNotNull(panel); // 정신력 패널 존재 확인
        Image fill = panel.Find("GaugeFill").GetComponent<Image>(); // 실제 정신력 채움 조회
        Transform trackTransform = panel.Find("GaugeTrack"); // 정신력 색상 바탕 조회
        Assert.IsNotNull(trackTransform); // 빈 정신력에서도 보일 바탕 확인
        Image track = trackTransform.GetComponent<Image>(); // 정신력 바탕 이미지 조회
        TMP_Text label = panel.Find("MentalText").GetComponent<TMP_Text>(); // 실제 정신력 문구 조회
        Assert.IsFalse(fill.raycastTarget); // 게이지의 입력 차단 방지 확인
        Assert.IsFalse(track.raycastTarget); // 바탕의 입력 차단 방지 확인
        Assert.IsTrue(unit.ApplyPersistentMental(0)); // 최소 정신력 적용
        Color red = fill.color; // 최소 정신력 색상 보관
        Assert.Greater(red.r, red.b); // 최소 정신력 빨강 확인
        Assert.Greater(track.color.r, track.color.b); // 비어 있는 게이지의 빨간 바탕 확인
        Assert.AreEqual(0f, fill.rectTransform.anchorMax.x, 0.001f); // 최소 정신력 채움 너비 확인
        Assert.AreEqual("0", label.text); // 붕괴 상태의 정신력 숫자 확인
        Assert.AreEqual(Color.white, label.color); // 어두운 붕괴 바탕 글자 대비 확인
        Assert.IsTrue(unit.ApplyPersistentMental(25)); // 낮은 정신력 적용
        Color low = fill.color; // 낮은 정신력 색상 보관
        Assert.Greater(low.r, low.b); // 낮은 정신력 붉은 방향 확인
        Assert.AreEqual(0.25f, fill.rectTransform.anchorMax.x, 0.001f); // 낮은 정신력 채움 너비 확인
        Assert.IsTrue(unit.ApplyPersistentMental(50)); // 중립 정신력 적용
        Color neutral = fill.color; // 중립 정신력 색상 보관
        Assert.AreEqual(neutral.r, neutral.b, 0.001f); // 중립 무채색 확인
        Assert.AreEqual(0.5f, fill.rectTransform.anchorMax.x, 0.001f); // 중립 정신력 채움 너비 확인
        Assert.IsTrue(unit.ApplyPersistentMental(75)); // 높은 정신력 적용
        Color high = fill.color; // 높은 정신력 색상 보관
        Assert.Greater(high.b, high.r); // 높은 정신력 푸른 방향 확인
        Assert.AreEqual(0.75f, fill.rectTransform.anchorMax.x, 0.001f); // 높은 정신력 채움 너비 확인
        Assert.IsTrue(unit.ApplyPersistentMental(100)); // 최대 정신력 적용
        Color blue = fill.color; // 최대 정신력 색상 보관
        Assert.Greater(blue.b, blue.r); // 최대 정신력 파랑 확인
        Assert.AreEqual(1f, fill.rectTransform.anchorMax.x, 0.001f); // 최대 정신력 채움 너비 확인
        Assert.AreEqual("100", label.text); // 각성 상태의 정신력 숫자 확인
        Assert.AreEqual(Color.white, label.color); // 어두운 각성 바탕 글자 대비 확인
        unit.ChangeMental(-25, BattleMentalChangeReason.None); // 각성 중 정신력 감소 적용
        Assert.AreEqual(BattleMentalState.Awakening, unit.MentalState); // 각성 상태 유지 확인
        Assert.AreEqual(high, fill.color); // 각성 중 실제 정신력 색상 확인
        Assert.IsTrue(unit.ApplyPersistentMental(0)); // 붕괴 정신력 재설정
        unit.ChangeMental(25, BattleMentalChangeReason.None); // 붕괴 중 정신력 증가 적용
        Assert.AreEqual(BattleMentalState.Collapse, unit.MentalState); // 붕괴 상태 유지 확인
        Assert.AreEqual(low, fill.color); // 붕괴 중 실제 정신력 색상 확인
        view.Unbind(); // 정신력 연결 해제
        Object.DestroyImmediate(root); // 테스트 화면 제거
        Object.DestroyImmediate(source); // 테스트 아군 원본 제거
    } // 수치별 색상 검사 종료
} // 정신력 색상 검사 종료
