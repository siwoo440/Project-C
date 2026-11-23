using System; // 상태 종류 이름 검사 사용
using NUnit.Framework; // 자동 검증 사용
using UnityEditor; // 실제 전투 프리팹 조회
using UnityEngine; // 유닛과 화면 객체 사용
using UnityEngine.UI; // 상태 이미지 검사 사용
using Object = UnityEngine.Object; // 유니티 객체 이름 구분
public sealed class BattleMentalStatusEffectTests // 정신 상태 이상 검사
{ // 정신 상태 검사 시작
    [Test] // 정화와 지속 처리 검사 지정
    public void MentalStatesAreProtectedStatusesUntilExpiryOrBattleEnd() // 상태 목록과 강제 해제 방지 확인
    { // 정신 상태 규칙 검사 시작
        Assert.IsTrue(Enum.TryParse("Awakening", out BattleStatusEffectType awakening)); // 각성 상태 종류 확인
        Assert.IsTrue(Enum.TryParse("Collapse", out BattleStatusEffectType collapse)); // 붕괴 상태 종류 확인
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(source); // 실제 아군 생성
        BattleUnitRuntime recipient = BattleUnitRuntime.CreateAlly(source); // 디버프 이전 대상 생성
        BattleStatusEffectProcessor processor = new BattleStatusEffectProcessor(); // 공통 상태 처리기 생성
        Assert.IsTrue(unit.ApplyPersistentMental(0)); // 붕괴 정신력 적용
        BattleStatusEffectInstance collapseEffect = unit.GetStatusEffect(collapse); // 실제 붕괴 상태 조회
        Assert.IsNotNull(collapseEffect); // 붕괴 상태 목록 포함 확인
        Assert.IsTrue(collapseEffect.IsDebuff); // 붕괴 디버프 분류 확인
        Assert.IsTrue(unit.HasDebuff); // 붕괴를 디버프로 인식 확인
        Assert.IsFalse(unit.HasRemovableDebuff); // 붕괴만으로 정화 카드 사용 방지 확인
        Assert.AreEqual(3, collapseEffect.RemainingTurns); // 붕괴 지속 턴 확인
        Assert.IsFalse(unit.RemoveStatusEffect(collapse)); // 직접 제거 차단 확인
        Assert.IsFalse(BattleDebuffTransfer.TryTransfer(unit, recipient, collapseEffect)); // 디버프 이전 차단 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 3, 1)); // 다른 디버프 적용
        Assert.AreEqual(1, processor.CleanseDebuffs(unit).Count); // 일반 디버프만 정화 확인
        Assert.IsNotNull(unit.GetStatusEffect(collapse)); // 정화 후 붕괴 유지 확인
        Assert.IsFalse(unit.HasRemovableDebuff); // 정화 후 제거 가능 디버프 없음 확인
        collapseEffect.AdvanceDuration(); // 외부 상태 턴 감소 시도
        Assert.AreEqual(3, collapseEffect.RemainingTurns); // 외부 턴 감소 차단 확인
        processor.ProcessPhaseEnd(unit, 1); // 일반 상태 턴 종료 처리
        Assert.AreEqual(3, unit.GetStatusEffect(collapse).RemainingTurns); // 일반 상태 처리의 중복 감소 방지 확인
        unit.AdvanceMentalStateTurn(); // 정신 상태 첫 턴 진행
        Assert.AreEqual(2, unit.GetStatusEffect(collapse).RemainingTurns); // 정신 상태 남은 턴 동기화 확인
        unit.AdvanceMentalStateTurn(); // 정신 상태 둘째 턴 진행
        unit.AdvanceMentalStateTurn(); // 정신 상태 셋째 턴 진행
        Assert.IsNull(unit.GetStatusEffect(collapse)); // 자연 만료 후 붕괴 제거 확인
        Assert.AreEqual(50, unit.CurrentMental); // 자연 만료 후 중립 정신력 확인
        Assert.IsTrue(unit.ApplyPersistentMental(100)); // 각성 정신력 적용
        Assert.IsNotNull(unit.GetStatusEffect(awakening)); // 각성 상태 목록 포함 확인
        Assert.IsFalse(unit.GetStatusEffect(awakening).IsDebuff); // 각성 버프 분류 확인
        Assert.IsFalse(unit.RemoveStatusEffect(awakening)); // 각성 직접 제거 차단 확인
        unit.ResolveMentalStateAtBattleEnd(); // 전투 종료 처리
        Assert.IsNull(unit.GetStatusEffect(awakening)); // 전투 종료 후 각성 제거 확인
        Assert.AreEqual(50, unit.CurrentMental); // 전투 종료 후 중립 정신력 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 정신 상태 규칙 검사 종료
    [Test] // 사망과 상태 면역 예외 검사 지정
    public void MentalThresholdBypassesImmunityAndSurvivesForcedRemoval() // 면역과 사망 중 보호 상태 유지 확인
    { // 보호 상태 예외 검사 시작
        CharacterData source = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(source); // 실제 아군 생성
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, unit.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1)); // 일반 디버프 면역 적용
        Assert.IsTrue(unit.ApplyPersistentMental(0)); // 면역 중 붕괴 정신력 적용
        Assert.IsNotNull(unit.GetStatusEffect(BattleStatusEffectType.Collapse)); // 정신 상태 임계 발동 확인
        Assert.AreEqual(BattleStatusEffectApplyResult.Invalid, unit.ApplyStatusEffect(BattleStatusEffectType.Collapse, 1, 9, 1)); // 외부 붕괴 재적용 차단 확인
        unit.TakeDamage(unit.MaxHealth * 2, BattleDamageType.None); // 유닛 사망 피해 적용
        Assert.IsTrue(unit.IsDead); // 실제 사망 확인
        Assert.IsNotNull(unit.GetStatusEffect(BattleStatusEffectType.Collapse)); // 사망 시 강제 상태 제거 방지 확인
        unit.ResolveMentalStateAtBattleEnd(); // 전투 종료 처리
        Assert.IsNull(unit.GetStatusEffect(BattleStatusEffectType.Collapse)); // 전투 종료 예외 해제 확인
        Assert.AreEqual(50, unit.CurrentMental); // 전투 종료 정신력 복귀 확인
        Object.DestroyImmediate(source); // 임시 아군 원본 정리
    } // 보호 상태 예외 검사 종료
    [Test] // 실제 유닛 화면 검사 지정
    public void MentalStatusShowsTransparentIconAndHeadBacklight() // 상태 아이콘과 머리 역광 확인
    { // 정신 상태 화면 검사 시작
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 실제 전투 프리팹 조회
        GameObject instance = Object.Instantiate(prefab); // 실제 전투 화면 생성
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 실제 아군 생성
        instance.GetComponent<BattleUnitView>().Bind(unit); // 화면에 아군 연결
        Assert.IsTrue(unit.ApplyPersistentMental(100)); // 각성 정신력 적용
        Transform status = instance.transform.Find("StatusEffects"); // 실제 상태 아이콘 영역 조회
        Assert.IsTrue(status.gameObject.activeSelf); // 각성 상태 아이콘 표시 확인
        Image icon = status.GetChild(0).GetComponent<Image>(); // 각성 상태 아이콘 조회
        Assert.AreEqual("AwakeningStatusIcon", icon.sprite.texture.name); // 투명 각성 아이콘 확인
        Assert.IsFalse(icon.raycastTarget); // 상태 아이콘 입력 차단 방지 확인
        Assert.AreEqual(Color.clear, status.GetChild(0).Find("Counters").GetComponent<Image>().color); // 정신 상태 아이콘의 투명 배경 확인
        Transform light = instance.transform.Find("MentalStateBacklight"); // 머리 뒤 역광 조회
        Assert.IsNotNull(light); // 머리 뒤 역광 존재 확인
        Assert.IsTrue(light.gameObject.activeSelf); // 각성 역광 표시 확인
        Assert.IsFalse(light.GetComponent<Image>().raycastTarget); // 역광 입력 차단 방지 확인
        Assert.IsTrue(unit.ApplyPersistentMental(0)); // 붕괴 정신력 적용
        Assert.AreEqual("CollapseStatusIcon", status.GetChild(0).GetComponent<Image>().sprite.texture.name); // 투명 붕괴 아이콘 확인
        Assert.IsTrue(light.gameObject.activeSelf); // 붕괴 역광 표시 확인
        unit.ResolveMentalStateAtBattleEnd(); // 전투 종료 처리
        Assert.IsFalse(light.gameObject.activeSelf); // 정신 상태 종료 시 역광 숨김 확인
        Object.DestroyImmediate(instance); // 실제 전투 화면 정리
    } // 정신 상태 화면 검사 종료
} // 검사 종료
