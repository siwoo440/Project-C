using NUnit.Framework; // 마이너 카드 표시 회귀 검증
using TMPro; // 실제 생성된 글자 영역과 크기 확인
using UnityEditor; // 실제 마이너 카드 데이터 파일 조회와 시험 값 연결
using UnityEngine; // 시험 캔버스와 카드 그림 생성
using UnityEngine.UI; // 선택 버튼과 카드 이미지 확인

public sealed class MinorCardSelectionViewTests // 마이너 카드 선택의 세부 영역과 버튼 표시 검사
{ // 마이너 카드 화면 검사 시작
    [Test] // 실제 선택지 데이터 검사 등록
    public void AuthoredChoices_FitSeparateRegionsAndRemainSelectable() // 실제 제목·설명·효과의 글자 범위와 선택 버튼 확인
    { // 실제 카드 선택지 확인 시작
        string[] guids = AssetDatabase.FindAssets("t:MinorCardData", new[] { "Assets/_ProjectC/Data/MinorCards" }); // 실제 선택에 쓰이는 마이너 카드 데이터 목록
        Assert.GreaterOrEqual(guids.Length, 3); // 실제 세 선택지 이상 존재 확인
        GameObject canvas = new GameObject("MinorCardLayoutTest", typeof(RectTransform), typeof(Canvas)); // 격리된 시험용 캔버스 생성

        try // 시험 화면 정리 보장
        { // 실제 카드 선택지 반복 검사 시작
            foreach (string guid in guids) // 게임에 등록된 마이너 카드별 반복 검사
            { // 개별 카드 화면 생성 시작
                MinorCardData data = AssetDatabase.LoadAssetAtPath<MinorCardData>(AssetDatabase.GUIDToAssetPath(guid)); // 실제 원본 카드 데이터 조회
                int selected = 0; // 실제 선택 이벤트 호출 횟수
                Button choice = MinorCardSelectionView.CreateCardButton((RectTransform)canvas.transform, data, () => selected++); // 전투에서 사용하는 실제 카드 버튼 생성
                RectTransform card = (RectTransform)choice.transform; // 선택지 내용 배치의 기준 카드 영역
                TMP_Text name = card.Find("Name").GetComponent<TMP_Text>(); // 분리된 카드 이름 영역 조회
                Image icon = card.Find("Icon").GetComponent<Image>(); // 카드 그림 또는 대체 원화 조회
                TMP_Text description = card.Find("Description").GetComponent<TMP_Text>(); // 분리된 카드 설명 영역 조회
                TMP_Text effect = card.Find("Effect").GetComponent<TMP_Text>(); // 분리된 카드 효과 영역 조회
                Assert.IsNotNull(choice.image.sprite, data.MinorCardId); // 기존 금속 카드 프레임 원화 확인
                Assert.IsNotNull(icon.sprite, data.MinorCardId); // 아이콘이 비어 있는 원본 카드의 대체 그림 확인
                Assert.IsFalse(icon.raycastTarget); // 그림 위에서도 카드 선택 입력 통과 확인

                Assert.Greater(LowerY(card, name.rectTransform), UpperY(card, icon.rectTransform)); // 제목이 그림 위에 분리된 위치 확인
                Assert.Greater(LowerY(card, icon.rectTransform), UpperY(card, description.rectTransform)); // 설명이 그림 아래에 분리된 위치 확인
                Assert.Greater(LowerY(card, description.rectTransform), UpperY(card, effect.rectTransform)); // 효과가 설명 아래에 분리된 위치 확인
                Assert.AreEqual(data.DisplayName, name.text); // 기존 카드 표시 이름 보존 확인
                Assert.AreEqual(data.Description, description.text); // 기존 설명 문구 보존 확인
                Canvas.ForceUpdateCanvases(); // 현재 글자 영역과 화면 배치 확정
                AssertTextFits(name); // 제목의 실제 한글 글자 범위 확인
                AssertTextFits(description); // 설명의 실제 한글 글자 범위 확인
                AssertTextFits(effect); // 효과의 실제 한글 글자 범위 확인
                choice.onClick.Invoke(); // 마이너 카드 선택 버튼 이벤트 실행
                Assert.AreEqual(1, selected); // 개별 선택 버튼의 이벤트 한 번 호출 확인
            } // 개별 카드 화면 생성 종료
        } // 실제 카드 선택지 반복 검사 종료
        finally // 독립 화면 정리
        { // 화면 정리 시작
            Object.DestroyImmediate(canvas); // 시험 캔버스와 카드 제거
        } // 화면 정리 종료
    } // 실제 카드 선택지 확인 종료

    [Test] // 원본에 지정된 카드 그림 검사 등록
    public void CardWithSuppliedArt_PrefersThatSpriteAndFitsLongerCopy() // 원화 우선순위와 긴 설명의 별도 영역 확인
    { // 지정 그림 우선순위 검사 시작
        MinorCardData source = AssetDatabase.LoadAssetAtPath<MinorCardData>("Assets/_ProjectC/Data/MinorCards/MINOR_LIFE_EMBER.asset"); // 그림 없는 실제 카드 원본 조회
        Assert.IsNotNull(source); // 시험 기준 마이너 카드 조회 확인
        MinorCardData data = Object.Instantiate(source); // 원본을 편집하지 않는 시험용 카드 사본
        GameObject canvas = new GameObject("MinorCardArtTest", typeof(RectTransform), typeof(Canvas)); // 독립 그림 시험 캔버스
        Texture2D texture = new Texture2D(8, 8); // 시험에 사용할 작은 그림 데이터 생성
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f)); // 대체 이미지와 구분되는 지정 그림 생성

        try // 시험용 카드와 그림 정리 보장
        { // 원본 그림 우선 확인 시작
            SerializedObject serialized = new SerializedObject(data); // 시험 카드의 시리얼라이즈 필드 조회
            serialized.FindProperty("icon").objectReferenceValue = sprite; // 시험 카드에 고유 원화 지정
            serialized.FindProperty("description").stringValue = "모든 아군의 최대 체력이 증가한다. 이 효과는 현재 전투가 끝날 때까지 유지되며 생존한 아군 각각에게 적용된다."; // 실제 카드보다 긴 여러 줄의 한국어 설명 지정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 지정된 그림의 시험 카드 적용
            Button choice = MinorCardSelectionView.CreateCardButton((RectTransform)canvas.transform, data, () => Assert.Fail("선택 입력 없이 원화만 검사")); // 실제 파이프라인으로 그림 지정 카드 생성
            Assert.AreSame(sprite, choice.transform.Find("Icon").GetComponent<Image>().sprite); // 기존 효과 원화 대신 지정 그림 표시 확인
            AssertTextFits(choice.transform.Find("Description").GetComponent<TMP_Text>()); // 더 긴 설명의 실제 글자 범위 검사
        } // 원본 그림 우선 확인 종료
        finally // 시험 카드와 시각 객체 정리
        { // 시험 객체 제거 시작
            Object.DestroyImmediate(canvas); // 독립 화면 제거
            Object.DestroyImmediate(data); // 시험 카드 사본 제거
            Object.DestroyImmediate(sprite); // 시험 지정 이미지 제거
            Object.DestroyImmediate(texture); // 시험 그림 텍스처 제거
        } // 시험 객체 제거 종료
    } // 지정 그림 우선순위 검사 종료

    private static float LowerY(RectTransform card, RectTransform child) // 카드 기준 영역의 하단 위치 조회
    { // 하단 계산 시작
        return card.InverseTransformPoint(child.TransformPoint(new Vector2(0f, child.rect.yMin))).y; // 카드 기준 하단 좌표
    } // 하단 계산 종료

    private static float UpperY(RectTransform card, RectTransform child) // 카드 기준 영역의 상단 위치 조회
    { // 상단 계산 시작
        return card.InverseTransformPoint(child.TransformPoint(new Vector2(0f, child.rect.yMax))).y; // 카드 기준 상단 좌표
    } // 상단 계산 종료

    private static void AssertTextFits(TMP_Text text) // 잘림 없이 완전히 표시되는 글자 검사
    { // 글자 검사 시작
        text.ForceMeshUpdate(true, true); // 실제 생성된 글자 위치와 정렬 계산
        Assert.IsFalse(text.isTextOverflowing, text.text); // 기존 문구의 말줄임과 넘침 없음 확인
        Rect region = text.rectTransform.rect; // 현재 글자 영역의 로컬 사각형
        Bounds glyphs = text.textBounds; // 실제로 생성된 글자 범위
        Assert.Greater(glyphs.size.x, 0f, text.text); // 표시할 글자가 생성되었는지 확인
        Assert.GreaterOrEqual(glyphs.min.x, region.xMin - 1f, text.text); // 왼쪽 영역 밖 글자 없음 확인
        Assert.LessOrEqual(glyphs.max.x, region.xMax + 1f, text.text); // 오른쪽 영역 밖 글자 없음 확인
        Assert.GreaterOrEqual(glyphs.min.y, region.yMin - 1f, text.text); // 아래쪽 영역 밖 글자 없음 확인
        Assert.LessOrEqual(glyphs.max.y, region.yMax + 1f, text.text); // 위쪽 영역 밖 글자 없음 확인
    } // 글자 검사 종료
} // 마이너 카드 화면 검사 종료
