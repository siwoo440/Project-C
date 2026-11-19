using System; // 전체 상태 종류 순회 사용
using System.Collections; // 실제 전투 화면 대기 사용
using System.Collections.Generic; // 상태 아이콘 중복 검사 사용
using System.IO; // 투명 아이콘 원본 읽기 사용
using NUnit.Framework; // 상태 아이콘 검사 사용
using TMPro; // 상태 수치 글자 검사 사용
using UnityEditor; // 아이콘 원본 경로 조회 사용
using UnityEngine; // 전투 유닛과 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 상태 아이콘 이미지 검사 사용
using Object = UnityEngine.Object; // 유니티 객체 정리 이름 지정
public sealed class BattleStatusIconTests // 전투 상태 아이콘 화면 검사
{ // 상태 아이콘 검사 시작
    [Test] // 상태 종류별 이미지 검사 지정
    public void EveryStatus_UsesItsOwnSquareGeneratedIcon() // 모든 상태의 개별 정사각형 이미지 확인
    { // 상태 아이콘 종류 검사 시작
        HashSet<string> areas = new HashSet<string>(); // 이미지와 영역별 상태 아이콘 목록
        foreach (BattleStatusEffectType effect in Enum.GetValues(typeof(BattleStatusEffectType))) // 모든 상태 종류 순회
        { // 개별 상태 아이콘 검사 시작
            if (effect == BattleStatusEffectType.None) // 상태 없음 항목 확인
            { // 상태 없음 제외 시작
                continue; // 다음 실제 상태 확인
            } // 상태 없음 제외 종료
            Sprite icon = BattleStatusIconArt.Get(effect); // 생성한 상태 이미지 조회
            Assert.IsNotNull(icon, effect.ToString()); // 각 상태 이미지 존재 확인
            bool separate = effect == BattleStatusEffectType.Awakening || effect == BattleStatusEffectType.Collapse || effect == BattleStatusEffectType.Evasion || effect == BattleStatusEffectType.Disorientation || effect == BattleStatusEffectType.CriticalChanceUp || effect == BattleStatusEffectType.ChargeDefenseDown || effect == BattleStatusEffectType.Bleed || effect == BattleStatusEffectType.Stagger || effect == BattleStatusEffectType.Stun || effect == BattleStatusEffectType.NextAttackDown || effect == BattleStatusEffectType.AttackPowerDown || effect == BattleStatusEffectType.BladeMaintenance || effect == BattleStatusEffectType.PhysicalRiposte || effect == BattleStatusEffectType.BloodOath || effect == BattleStatusEffectType.Fear || effect == BattleStatusEffectType.FinishingStance || effect == BattleStatusEffectType.HealingReceivedDown; // 개별 투명 원화 상태 확인
            Assert.AreEqual(separate ? effect + "StatusIcon" : "StatusIcons", icon.texture.name); // 상태별 생성 이미지 사용 확인
            if (!separate) // 공용 아이콘 시트 상태 확인
            { // 기존 정사각형 이미지 검사 시작
                Assert.AreEqual(icon.rect.width, icon.rect.height, 0.01f); // 기존 상태 이미지의 정사각형 비율 확인
            } // 기존 정사각형 이미지 검사 종료
            else // 별도 투명 정신 상태 아이콘 확인
            { // 투명 이미지 검사 시작
                Texture2D raw = new Texture2D(2, 2); // 원본 투명도 검사 이미지 생성
                Assert.IsTrue(raw.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(icon.texture)))); // 실제 아이콘 파일 읽기 확인
                Assert.Less(raw.GetPixel(0, 0).a, 0.01f); // 아이콘 바깥 배경 투명 확인
                Object.DestroyImmediate(raw); // 원본 검사 이미지 정리
            } // 투명 이미지 검사 종료
            Assert.IsTrue(areas.Add(icon.texture.name + icon.rect), effect.ToString()); // 서로 다른 상태의 독립 이미지 확인
        } // 개별 상태 아이콘 검사 종료
        Assert.AreEqual(Enum.GetValues(typeof(BattleStatusEffectType)).Length - 1, areas.Count); // 상태 없음 이외의 모든 고유 아이콘 수 확인
    } // 상태 아이콘 종류 검사 종료
    [UnityTest] // 실제 상태 아이콘 화면 검사 지정
    public IEnumerator StatusView_ShowsIconsAndCornerValuesAndUpdates() // 상태 아이콘과 구석 수치의 실제 갱신 확인
    { // 실제 상태 화면 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 시작
        GameObject canvas = new GameObject("StatusCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 독립 화면 유지
        GameObject unitObject = new GameObject("Unit", typeof(RectTransform), typeof(BattleStatusEffectView)); // 실제 상태 화면 생성
        unitObject.transform.SetParent(canvas.transform, false); // 전투 캔버스에 상태 화면 연결
        unitObject.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 300f); // 실제 유닛 화면 크기 설정
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 원본 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(data); // 실제 전투 유닛 생성
        BattleStatusEffectView view = unitObject.GetComponent<BattleStatusEffectView>(); // 실제 상태 화면 조회
        view.Bind(unit); // 실제 유닛 상태 연결
        unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 5, 3, 2); // 표시할 중독 상태 적용
        unit.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 2, 2, 1); // 표시할 재생 상태 적용
        Canvas.ForceUpdateCanvases(); // 상태 화면 배치 갱신
        Transform root = unitObject.transform.Find("StatusEffects"); // 실제 상태 아이콘 영역 조회
        Transform poison = root.Find("Icon_0"); // 첫 중독 아이콘 조회
        Transform regeneration = root.Find("Icon_1"); // 두 번째 재생 아이콘 조회
        Assert.IsNotNull(poison.GetComponent<Image>().sprite); // 중독 아이콘 이미지 확인
        Assert.IsNotNull(regeneration.GetComponent<Image>().sprite); // 재생 아이콘 이미지 확인
        Assert.AreEqual("5", poison.Find("Amount").GetComponent<TMP_Text>().text); // 오른쪽 아래 중독 수치 확인
        Assert.AreEqual("3", poison.Find("Duration").GetComponent<TMP_Text>().text); // 왼쪽 아래 남은 턴 확인
        Assert.AreEqual(1f, poison.Find("Amount").GetComponent<RectTransform>().anchorMax.x); // 상태 수치 오른쪽 아래 위치 확인
        Assert.AreEqual(0f, poison.Find("Duration").GetComponent<RectTransform>().anchorMin.x); // 남은 턴 왼쪽 아래 위치 확인
        unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 5, 3, 2); // 중독을 한 번 더 중첩
        Assert.AreEqual("10", poison.Find("Amount").GetComponent<TMP_Text>().text); // 중첩에 따른 전체 중독 수치 갱신 확인
        Assert.IsTrue(unit.RemoveStatusEffect(BattleStatusEffectType.Poison)); // 중독 제거 확인
        Assert.AreEqual("2", root.Find("Icon_0/Amount").GetComponent<TMP_Text>().text); // 남은 재생 아이콘의 수치 갱신 확인
        Assert.IsFalse(root.Find("Icon_1").gameObject.activeSelf); // 사라진 상태 아이콘 숨김 확인
        BattleStatusEffectType[] additional = { BattleStatusEffectType.Poison, BattleStatusEffectType.Burn, BattleStatusEffectType.Blind, BattleStatusEffectType.PhysicalDefenseDown, BattleStatusEffectType.MagicalResistanceDown, BattleStatusEffectType.AttackPowerUp, BattleStatusEffectType.PhysicalDefenseUp, BattleStatusEffectType.MagicalResistanceUp, BattleStatusEffectType.StarReservedHealing }; // 여러 줄 상태 표시용 종류 목록
        foreach (BattleStatusEffectType effect in additional) // 여러 상태 순회
        { // 추가 상태 적용 시작
            unit.ApplyStatusEffect(effect, 1, 3, 1); // 실제 상태 적용
        } // 추가 상태 적용 종료
        RectTransform firstIcon = root.Find("Icon_0").GetComponent<RectTransform>(); // 첫 상태 아이콘 영역 조회
        RectTransform lastIcon = root.Find("Icon_9").GetComponent<RectTransform>(); // 마지막 상태 아이콘 영역 조회
        Assert.AreEqual(firstIcon.rect.width, firstIcon.rect.height, 0.01f); // 여러 상태에서도 정사각형 유지 확인
        Assert.LessOrEqual(-lastIcon.anchoredPosition.y, firstIcon.rect.height + 2.1f); // 모든 상태가 최대 두 줄 안에 배치되는지 확인
        Object.DestroyImmediate(canvas); // 독립 전투 화면 정리
        Object.DestroyImmediate(data); // 임시 캐릭터 원본 정리
        yield return new ExitPlayMode(); // 실제 전투 화면 플레이 종료
    } // 실제 상태 화면 검사 종료
} // 상태 아이콘 검사 종료
