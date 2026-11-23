using NUnit.Framework; // 테스트 기능 사용
using UnityEditor; // 에셋 및 직렬화 기능 사용
using UnityEditor.U2D; // 스프라이트 아틀라스 편집 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.U2D; // 스프라이트 아틀라스 기능 사용
using UnityEngine.UI; // 이미지 화면 기능 사용
public sealed class BattleCharacterArtTests // 전투 캐릭터 일러스트 검증
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void BattleArtAtlas_PacksAllCharacterSpritesTogether() // 전체 전투 그림 아틀라스 확인
    { // 테스트 시작
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_ProjectC/Art/BattleCharacters.spriteatlas"); // 전투 그림 아틀라스 조회
        Assert.IsNotNull(atlas); // 아틀라스 에셋 존재 확인
        Assert.IsTrue(atlas.isVariant == false); // 원본 아틀라스 확인
        Assert.IsTrue(atlas.IsIncludeInBuild()); // 자동 포함 설정 확인
        SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings(); // 아틀라스 텍스처 설정 조회
        Assert.IsTrue(textureSettings.generateMipMaps); // 축소용 밉맵 생성 확인
        Assert.AreEqual(FilterMode.Trilinear, textureSettings.filterMode); // 부드러운 축소 필터 확인
        TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform"); // 기본 플랫폼 설정 조회
        Assert.AreEqual(TextureImporterCompression.Uncompressed, platformSettings.textureCompression); // 아틀라스 무압축 확인
        Object[] sources = atlas.GetPackables(); // 아틀라스 입력 목록 조회
        Assert.AreEqual(1, sources.Length); // 단일 입력 폴더 확인
        Assert.AreEqual("Assets/_ProjectC/Art/BattleCharacters", AssetDatabase.GetAssetPath(sources[0])); // 전체 그림 폴더 연결 확인
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget); // 편집기용 아틀라스 묶기
        Assert.AreEqual(22, atlas.spriteCount); // 전체 캐릭터 그림 수 확인
        Texture2D sharedTexture = null; // 공통 아틀라스 텍스처 저장
        for (int index = 0; index <= 21; index++) // 전체 캐릭터 순회
        { // 개별 그림 확인 시작
            string identifier = $"ARC_{index:00}"; // 캐릭터 식별자 생성
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/_ProjectC/ScriptableObjects/Characters/{identifier}.asset"); // 캐릭터 데이터 조회
            Assert.IsTrue(atlas.CanBindTo(character.BattleIllustration), identifier); // 캐릭터 그림의 아틀라스 연결 확인
            Sprite packed = atlas.GetSprite($"{identifier}_0"); // 묶인 캐릭터 그림 조회
            Assert.IsNotNull(packed, identifier); // 묶인 그림 존재 확인
            if (sharedTexture == null) // 첫 텍스처 확인
            { // 공통 텍스처 저장 시작
                sharedTexture = packed.texture; // 첫 그림 텍스처 저장
            } // 공통 텍스처 저장 종료
            Assert.AreSame(sharedTexture, packed.texture, identifier); // 단일 아틀라스 텍스처 확인
        } // 개별 그림 확인 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void BattleArtImport_UsesSmoothFilteringWithoutCompression() // 전투 그림 화질 설정 확인
    { // 테스트 시작
        for (int index = 0; index <= 21; index++) // 전체 캐릭터 순회
        { // 개별 그림 설정 확인 시작
            string path = $"Assets/_ProjectC/Art/BattleCharacters/ARC_{index:00}.png"; // 그림 에셋 경로 생성
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; // 그림 가져오기 설정 조회
            Assert.IsNotNull(importer, path); // 가져오기 설정 존재 확인
            Assert.IsTrue(importer.mipmapEnabled, path); // 축소용 밉맵 생성 확인
            Assert.IsTrue(importer.alphaIsTransparency, path); // 투명 가장자리 보정 확인
            Assert.AreEqual(FilterMode.Trilinear, importer.filterMode, path); // 부드러운 축소 필터 확인
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path); // 무압축 그림 사용 확인
            Assert.AreEqual(1024, importer.maxTextureSize, path); // 전투 화면용 최대 해상도 확인
        } // 개별 그림 설정 확인 종료
    } // 테스트 종료
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
    [TestCase("ARC_10")] // 운명 전투 그림 검사 지정
    [TestCase("ARC_11")] // 정의 전투 그림 검사 지정
    [TestCase("ARC_12")] // 매달린 자 전투 그림 검사 지정
    [TestCase("ARC_13")] // 죽음 전투 그림 검사 지정
    [TestCase("ARC_14")] // 절제 전투 그림 검사 지정
    [TestCase("ARC_15")] // 악마 전투 그림 검사 지정
    [TestCase("ARC_16")] // 탑 전투 그림 검사 지정
    [TestCase("ARC_17")] // 별 전투 그림 검사 지정
    [TestCase("ARC_18")] // 달 전투 그림 검사 지정
    [TestCase("ARC_19")] // 태양 전투 그림 검사 지정
    [TestCase("ARC_20")] // 심판 전투 그림 검사 지정
    [TestCase("ARC_21")] // 세계 전투 그림 검사 지정
    public void AssignedBattleArt_AppearsOnUnit(string identifier) // 전용 전투 그림 표시 확인
    { // 테스트 시작
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/_ProjectC/ScriptableObjects/Characters/{identifier}.asset"); // 캐릭터 에셋 조회
        Assert.IsNotNull(character); // 캐릭터 존재 확인
        SerializedProperty artProperty = new SerializedObject(character).FindProperty("battleIllustration"); // 전투 그림 설정 조회
        Assert.IsNotNull(artProperty); // 전투 그림 설정 존재 확인
        Sprite art = artProperty.objectReferenceValue as Sprite; // 전투 그림 조회
        Assert.IsNotNull(art); // 전용 전투 그림 연결 확인
        Assert.AreNotSame(character.Portrait, art); // 초상화와 별도 그림 확인
        Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_ProjectC/Art/BattleCharacters/{identifier}.png"); // 원본 그림 텍스처 조회
        Assert.That(art.rect.width, Is.EqualTo(sourceTexture.width).Within(0.001f)); // 그림 전체 너비 확인
        Assert.That(art.rect.height, Is.EqualTo(sourceTexture.height).Within(0.001f)); // 그림 전체 높이 확인
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
