using UnityEngine; // 전투 UI와 스프라이트 위치 계산
using UnityEngine.Sprites; // 원화의 실제 표시 여백 조회
using UnityEngine.UI; // 입력을 가리지 않는 그림자 표시

internal sealed class BattleGroundShadow : MonoBehaviour // 유닛 발과 바닥 사이의 짧은 접지 그림자
{ // 전투 접지 그림자 시작
    private static Texture2D falloffTexture; // 전투 전체에서 공유하는 부드러운 그림자 원본
    private Image portrait; // 실제로 움직이는 유닛 원화
    private RawImage shadow; // 유닛 바로 아래의 그림자 그래픽
    private BattleUnitRuntime unit; // 생존 상태를 읽는 실제 전투 유닛
    private Vector2 contact; // 스프라이트의 발 중심과 최저 표시 위치
    private float contactWidth; // 발과 낮은 원화 영역의 너비

    internal void Bind(Image image, BattleUnitRuntime runtimeUnit) // 현재 초상화와 바닥 접점 연결
    { // 그림자 대상 설정 시작
        portrait = image; // 같은 위치에서 움직이는 실제 원화 참조
        unit = runtimeUnit; // 생존과 사망 표시 기준 지정
        if (image == null || image.sprite == null) // 유효한 그림이 있는 전투 유닛인지 확인
        { // 그림을 그릴 수 없는 유닛 처리 시작
            SetVisible(false); // 대체 아이콘 아래에 그림자를 표시하지 않도록 설정
            return; // 그림자 자료 계산 생략
        } // 그림을 그릴 수 없는 유닛 처리 종료

        MeasureContact(image.sprite); // 현재 원화의 실제 발 부분을 한 번 계산
        if (shadow == null) // 아직 유닛에 그림자가 설치되지 않았는지 확인
        { // 그림자 레이어 생성 시작
            var item = new GameObject("GroundContactShadow", typeof(RectTransform), typeof(RawImage)); // 발 아래에만 놓을 장식 이미지
            item.transform.SetParent(image.transform.parent, false); // 유닛 원화와 같은 부모 크기 적용
            item.transform.SetSiblingIndex(image.transform.GetSiblingIndex()); // 캐릭터 앞을 가리지 않도록 뒤에 배치
            shadow = item.GetComponent<RawImage>(); // 그림자 UI 조회
            shadow.texture = GetFalloffTexture(); // 캐릭터마다 중복 생성하지 않는 공용 텍스처
            shadow.color = new Color(0.012f, 0.017f, 0.019f, 0.87f); // 석재에 떨어지는 어두운 차광 색
            shadow.raycastTarget = false; // 카드 사용과 유닛 클릭을 방해하지 않는 그림
        } // 그림자 레이어 생성 종료
        LateUpdate(); // 최초 프레임에도 현재 원화 아래 표시
    } // 그림자 대상 설정 종료

    private void MeasureContact(Sprite sprite) // 그림 아래쪽의 실제 원화 폭과 발 높이 계산
    { // 발 중심 계산 시작
        Rect rect = sprite.rect; // 투명 여백을 포함한 원화 좌표
        Vector4 padding = DataUtility.GetPadding(sprite); // Unity 스프라이트의 실제 가장자리 여백
        Vector2 leftBottom = new Vector2(padding.x / rect.width, padding.y / rect.height); // 좌측과 아래의 정규화 여백
        Vector2 rightTop = new Vector2(1f - padding.z / rect.width, 1f - padding.w / rect.height); // 우측과 위의 실제 원화 범위
        Vector2[] points = sprite.vertices; // 피해와 공격 화면에 쓰는 스프라이트 모양 점
        float lowest = 1f; // 아래쪽을 담을 가장 낮은 점 초기화
        foreach (Vector2 point in points) // 스프라이트의 아래 원화 범위 탐색
        { // 정규화된 발 높이 조사 시작
            lowest = Mathf.Min(lowest, (point.y * sprite.pixelsPerUnit + sprite.pivot.y) / rect.height); // 원래 픽셀 높이를 비율로 환산
        } // 정규화된 발 높이 조사 종료
        float footLine = points.Length > 0 ? Mathf.Clamp(lowest, leftBottom.y, rightTop.y) : leftBottom.y; // 스프라이트 내의 실제 바닥 높이
        float minX = rightTop.x; // 아래쪽 원화의 왼쪽 기준
        float maxX = leftBottom.x; // 아래쪽 원화의 오른쪽 기준
        foreach (Vector2 point in points) // 바닥에 가까운 실제 모양 점만 추출
        { // 발 주변의 좌우 폭 조사 시작
            float y = (point.y * sprite.pixelsPerUnit + sprite.pivot.y) / rect.height; // 현재 점의 원화 높이
            if (y > footLine + 0.15f) // 몸통이나 머리의 넓은 폭 제외
            { // 발 주변이 아닌 점 처리 시작
                continue; // 아래쪽 원화 범위 유지
            } // 발 주변이 아닌 점 처리 종료
            float x = (point.x * sprite.pixelsPerUnit + sprite.pivot.x) / rect.width; // 현재 점의 실제 원화 가로 비율
            minX = Mathf.Min(minX, x); // 바닥에 가까운 왼쪽 범위 업데이트
            maxX = Mathf.Max(maxX, x); // 바닥에 가까운 오른쪽 범위 업데이트
        } // 발 주변의 좌우 폭 조사 종료
        float centerX = minX <= maxX ? (minX + maxX) * 0.5f : (leftBottom.x + rightTop.x) * 0.5f; // 발 중심 또는 원화의 가운데 사용
        contact = new Vector2(Mathf.Clamp(centerX, leftBottom.x, rightTop.x), footLine); // 석재에서 사용할 실제 접점 저장
        contactWidth = Mathf.Clamp(Mathf.Max(maxX - minX, rightTop.x - leftBottom.x) * 0.71f, 0.27f, 0.69f); // 유닛 폭에 따라 자연스럽게 보이는 그림자 크기
    } // 발 중심 계산 종료

    private void LateUpdate() // 공격·피격 이동과 마지막 프레임의 발 위치 적용
    { // 현재 발의 그림자 위치 갱신 시작
        bool visible = unit != null && !unit.IsDead && portrait != null && portrait.isActiveAndEnabled && portrait.sprite != null && shadow != null; // 실제 생존 및 표시 상태 확인
        SetVisible(visible); // 죽거나 숨겨진 캐릭터의 접지 그림자 숨김
        if (!visible) // 그릴 대상이 없는 경우 확인
        { // 접지 계산 제외 시작
            return; // 숨겨진 유닛의 좌표 계산 생략
        } // 접지 계산 제외 종료
        RectTransform source = portrait.rectTransform; // 유닛 원화가 이동하는 실제 화면 좌표
        Rect rect = portrait.GetPixelAdjustedRect(); // UI에서 원화를 그리는 실제 단위 사각형
        if (portrait.preserveAspect) // 세로와 가로 원화 비율을 보존하는 이미지
        { // 원화 비율 적용 시작
            Vector2 imageSize = portrait.sprite.rect.size; // 현재 장면 원화의 실제 가로세로 비율
            float scale = Mathf.Min(rect.width / imageSize.x, rect.height / imageSize.y); // 화면에 들어가는 비례 배율
            Vector2 fitted = imageSize * scale; // 화면에 표시하는 실제 원화 크기
            rect.position += Vector2.Scale(rect.size - fitted, source.pivot); // Unity 기준점에 맞춘 여백 이동
            rect.size = fitted; // 실제 발 위치를 구할 이미지 영역 반영
        } // 원화 비율 적용 종료
        Vector2 ground = rect.min + Vector2.Scale(rect.size, contact); // 화면 사각형 안의 원화 발 위치
        float width = rect.width * contactWidth; // 캐릭터와 적의 실제 원화에 맞는 너비
        shadow.rectTransform.sizeDelta = new Vector2(width, Mathf.Clamp(width * 0.22f, 12f, 32f)); // 석재 위에 눌린 낮은 타원 형태
        shadow.rectTransform.position = source.TransformPoint(new Vector3(ground.x, ground.y - 1f, 0f)); // 원화 뒤쪽에 발 위치를 정확히 따라가는 그림자
    } // 현재 발의 그림자 위치 갱신 종료

    private void SetVisible(bool visible) // 그림자 레이어 표시 상태 갱신
    { // 그림자 가시성 처리 시작
        if (shadow != null && shadow.enabled != visible) // 현재와 실제 필요한 표시가 다른지 확인
        { // 변경된 표시 반영 시작
            shadow.enabled = visible; // 캔버스 레이아웃과 클릭을 유지하며 그림자만 전환
        } // 변경된 표시 반영 종료
    } // 그림자 가시성 처리 종료

    private static Texture2D GetFalloffTexture() // 얇은 타원에 적용할 공용 투명도 질감 생성
    { // 그림자 질감 조회 시작
        if (falloffTexture != null) // 이 게임 세션에서 이미 만든 텍스처가 있는지 확인
        { // 기존 그림자 자료 사용 시작
            return falloffTexture; // 작은 공용 원본 재사용
        } // 기존 그림자 자료 사용 종료
        const int size = 32; // 여러 유닛에 공유할 작은 텍스처의 한 변 크기
        var pixels = new Color32[size * size]; // 중앙부터 가장자리까지의 투명도 배열
        for (int y = 0; y < size; y++) // 그림자 원본의 줄 순회
        { // 그림자 명암 계산 시작
            for (int x = 0; x < size; x++) // 이 줄의 각 픽셀 순회
            { // 현재 픽셀의 투명도 계산 시작
                var vector = new Vector2((x + 0.5f) * 2f / size - 1f, (y + 0.5f) * 2f / size - 1f); // 중심에 대한 정규화 거리
                float strength = Mathf.Clamp01(1f - vector.sqrMagnitude); // 바깥으로 갈수록 부드러운 감쇠
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * strength * strength)); // 유닛별 색상과 혼합할 투명도
            } // 현재 픽셀의 투명도 계산 종료
        } // 그림자 명암 계산 종료
        falloffTexture = new Texture2D(size, size, TextureFormat.RGBA32, false); // 작고 반복 사용 가능한 렌더 텍스처
        falloffTexture.name = "BattleGroundContactFalloff"; // 전투 그림자 자산을 확인할 이름
        falloffTexture.hideFlags = HideFlags.HideAndDontSave; // 임시 그림이 씬 자산에 저장되지 않도록 설정
        falloffTexture.wrapMode = TextureWrapMode.Clamp; // 가장자리의 반복 무늬 방지
        falloffTexture.SetPixels32(pixels); // 공용 명암 픽셀 적용
        falloffTexture.Apply(false, true); // 렌더 업로드와 불필요한 CPU 사본 해제
        return falloffTexture; // 모든 유닛이 공유할 텍스처
    } // 그림자 질감 조회 종료
} // 전투 접지 그림자 종료
