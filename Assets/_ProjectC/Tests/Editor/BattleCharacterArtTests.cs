using NUnit.Framework; // 테스트 기능 사용
using UnityEditor; // 에셋 및 직렬화 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 이미지 화면 기능 사용
public sealed class BattleCharacterArtTests // 전투 캐릭터 일러스트 검증
{ // 테스트 클래스 시작
    [TestCase(true)] // 아군 배치 검사 지정
    [TestCase(false)] // 적군 배치 검사 지정
    public void BattleUnit_UsesLargeArtworkWithoutCardBackground(bool ally) // 양 진영 전투 화면 배치 확인
    { // 테스트 시작
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 전투 유닛 프리팹 조회
        GameObject unit = Object.Instantiate(prefab); // 전투 유닛 복제
        try // 복제 유닛 검사 시작
        { // 검사 구역 시작
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_ProjectC/ScriptableObjects/Characters/ARC_00.asset"); // 아군 데이터 조회
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_ProjectC/ScriptableObjects/Enemies/Enemy_Test.asset"); // 적군 데이터 조회
            BattleUnitView view = unit.GetComponent<BattleUnitView>(); // 전투 유닛 화면 조회
            view.Bind(ally ? BattleUnitRuntime.CreateAlly(character) : BattleUnitRuntime.CreateEnemy(enemy)); // 진영별 유닛 연결
            SerializedObject viewObject = new SerializedObject(view); // 화면 직렬화 객체 생성
            Image art = viewObject.FindProperty("portraitImage").objectReferenceValue as Image; // 캐릭터 그림 조회
            Image frame = viewObject.FindProperty("teamFrameImage").objectReferenceValue as Image; // 입력용 배경 조회
            RectTransform name = ((Component)viewObject.FindProperty("nameText").objectReferenceValue).GetComponent<RectTransform>(); // 이름 위치 조회
            RectTransform health = ((Component)viewObject.FindProperty("healthSlider").objectReferenceValue).GetComponent<RectTransform>(); // 체력 위치 조회
            Assert.AreEqual(new Vector2(260f, 275f), art.rectTransform.sizeDelta); // 기존 크기의 2.5배 확인
            Assert.AreEqual(0f, frame.color.a); // 카드 배경 투명 확인
            Assert.IsFalse(unit.transform.Find("Panel").gameObject.activeSelf); // 안쪽 카드 배경 숨김 확인
            Assert.Less(name.anchoredPosition.y, art.rectTransform.anchoredPosition.y - art.rectTransform.sizeDelta.y / 2f); // 이름의 그림 아래 위치 확인
            Assert.Less(health.anchoredPosition.y, name.anchoredPosition.y); // 체력의 이름 아래 위치 확인
            Assert.IsTrue(frame.raycastTarget); // 유닛 클릭 영역 유지 확인
            Outline outline = art.GetComponent<Outline>(); // 대상 강조 윤곽선 조회
            Assert.IsNotNull(outline); // 대상 강조 기능 존재 확인
            Assert.IsFalse(outline.enabled); // 기본 윤곽선 숨김 확인
            view.SetTargetable(true); // 대상 선택 강조 적용
            Assert.IsTrue(outline.enabled); // 선택 가능 윤곽선 확인
            view.SetTargetable(false); // 대상 선택 강조 해제
            Assert.IsFalse(outline.enabled); // 윤곽선 해제 확인
        } // 검사 구역 종료
        finally // 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(unit); // 복제 유닛 제거
        } // 정리 종료
    } // 테스트 종료
    [TestCase("ARC_00")] // 바보 전투 그림 검사 지정
    [TestCase("ARC_01")] // 마법사 전투 그림 검사 지정
    [TestCase("ARC_02")] // 여사제 전투 그림 검사 지정
    [TestCase("ARC_03")] // 여황제 전투 그림 검사 지정
    [TestCase("ARC_04")] // 황제 전투 그림 검사 지정
    [TestCase("ARC_05")] // 교황 전투 그림 검사 지정
    [TestCase("ARC_06")] // 연인 전투 그림 검사 지정
    [TestCase("ARC_07")] // 전차 전투 그림 검사 지정
    [TestCase("ARC_08")] // 힘 전투 그림 검사 지정
    [TestCase("ARC_09")] // 은둔자 전투 그림 검사 지정
    public void AssignedBattleArt_AppearsOnUnit(string identifier) // 전용 전투 그림 표시 확인
    { // 테스트 시작
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/_ProjectC/ScriptableObjects/Characters/{identifier}.asset"); // 캐릭터 에셋 조회
        Assert.IsNotNull(character); // 캐릭터 존재 확인
        SerializedProperty artProperty = new SerializedObject(character).FindProperty("battleIllustration"); // 전투 그림 설정 조회
        Assert.IsNotNull(artProperty); // 전투 그림 설정 존재 확인
        Sprite art = artProperty.objectReferenceValue as Sprite; // 전투 그림 조회
        Assert.IsNotNull(art); // 전용 전투 그림 연결 확인
        Assert.AreNotSame(character.Portrait, art); // 초상화와 별도 그림 확인
        Assert.AreEqual(art.texture.width, art.rect.width); // 그림 전체 너비 확인
        Assert.AreEqual(art.texture.height, art.rect.height); // 그림 전체 높이 확인
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 전투 유닛 프리팹 조회
        GameObject unit = Object.Instantiate(prefab); // 전투 유닛 복제
        try // 복제 유닛 검사 시작
        { // 검사 구역 시작
            BattleUnitView view = unit.GetComponent<BattleUnitView>(); // 전투 유닛 화면 조회
            view.Bind(BattleUnitRuntime.CreateAlly(character)); // 캐릭터 화면 연결
            Image image = new SerializedObject(view).FindProperty("portraitImage").objectReferenceValue as Image; // 그림 표시 이미지 조회
            Assert.AreSame(art, image.sprite); // 전투 전용 그림 표시 확인
            Assert.IsTrue(image.preserveAspect); // 그림 원본 비율 유지 확인
        } // 검사 구역 종료
        finally // 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(unit); // 복제 유닛 제거
        } // 정리 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MissingBattleArt_UsesPortrait() // 전투 그림 누락 시 초상화 대체 확인
    { // 테스트 시작
        CharacterData source = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_ProjectC/ScriptableObjects/Characters/ARC_21.asset"); // 원본 캐릭터 조회
        CharacterData character = Object.Instantiate(source); // 테스트용 캐릭터 복제
        SerializedObject characterObject = new SerializedObject(character); // 캐릭터 직렬화 객체 생성
        SerializedProperty artProperty = characterObject.FindProperty("battleIllustration"); // 전투 그림 설정 조회
        if (artProperty != null) // 전투 그림 설정 존재 확인
        { // 그림 제거 시작
            artProperty.objectReferenceValue = null; // 전투 그림 연결 제거
            characterObject.ApplyModifiedPropertiesWithoutUndo(); // 테스트 설정 반영
        } // 그림 제거 종료
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 전투 유닛 프리팹 조회
        GameObject unit = Object.Instantiate(prefab); // 전투 유닛 복제
        try // 복제 유닛 검사 시작
        { // 검사 구역 시작
            BattleUnitView view = unit.GetComponent<BattleUnitView>(); // 전투 유닛 화면 조회
            view.Bind(BattleUnitRuntime.CreateAlly(character)); // 캐릭터 화면 연결
            Image image = new SerializedObject(view).FindProperty("portraitImage").objectReferenceValue as Image; // 그림 표시 이미지 조회
            Assert.AreSame(character.Portrait, image.sprite); // 기존 초상화 대체 표시 확인
        } // 검사 구역 종료
        finally // 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(unit); // 복제 유닛 제거
            Object.DestroyImmediate(character); // 복제 캐릭터 제거
        } // 정리 종료
    } // 테스트 종료
} // 테스트 클래스 종료
