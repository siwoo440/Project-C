using System; // 빈 원화 배열 사용
using UnityEngine; // 텍스처 조회와 스프라이트 생성

internal enum ExplorationProp // 지도 물건 원화의 칸 순서
{ // 지도 물건 종류 시작
    Stairs, // 층 이동 계단
    Treasure, // 보물 상자
    Rest, // 휴식 장소
    Shop, // 상점 표식
    Event, // 일반 사건 표식
    Fade, // 침식 위험 표식
    Ember, // 불씨 위험 표식
    Alert // 경고 표식
} // 지도 물건 종류 종료

internal enum ExplorationActorStyle // 탐사 캐릭터 원화 종류
{ // 캐릭터 종류 시작
    Hero, // 탐사 주인공
    Guardian, // 수호자형 조우
    Beast // 야수형 조우
} // 캐릭터 종류 종료

internal static class ExplorationWorldArt // 탐사 지도와 캐릭터의 공용 원화 조회
{ // 탐사 원화 조회 시작
    internal const int DirectionCount = 8; // 캐릭터가 향하는 방향 수
    internal const int FramesPerDirection = 2; // 한 방향에서 번갈아 표시할 자세 수
    internal const float SizeMultiplier = 2f; // 탐사 이미지의 화면 크기 배율

    private const string ResourceFolder = "Exploration/WorldActors/"; // 탐사 원화 리소스 폴더
    private const int ColumnCount = 4; // 모든 원화 묶음의 가로 칸 수
    private const int ActorRowCount = 4; // 캐릭터 원화의 세로 칸 수
    private const int PropRowCount = 2; // 지도 물건 원화의 세로 칸 수
    private const float ActorCellWidth = 1.65f * SizeMultiplier; // 발밑 위치를 유지한 캐릭터 원화 크기
    private const float PropCellWidth = 1.18f * SizeMultiplier; // 중심 위치를 유지한 지도 물건 원화 크기

    private static readonly Vector2 ActorPivot = new Vector2(0.5f, 0.14f); // 캐릭터의 가로 가운데와 발밑 기준점
    private static readonly Vector2 PropPivot = new Vector2(0.5f, 0.5f); // 지도 물건의 가운데 기준점
    private static readonly string[] ActorAtlasNames = { "Hero", "Guardian", "Beast" }; // 캐릭터 종류별 텍스처 이름
    private static readonly Sprite[][] ActorAtlases = new Sprite[ActorAtlasNames.Length][]; // 캐릭터 종류별 분할 원화 보관소
    private static Sprite[] propAtlas; // 지도 물건 분할 원화 보관소

    internal static Sprite GetProp(ExplorationProp kind) // 지정된 지도 물건 이미지 조회
    { // 지도 물건 조회 시작
        int index = (int)kind; // 종류에 해당하는 원화 칸 번호
        if ((uint)index >= ColumnCount * PropRowCount) // 지원하는 지도 물건 번호 확인
        { // 미지원 지도 물건 처리 시작
            return null; // 기존 지도 물건 표시를 쓰는 반환값
        } // 미지원 지도 물건 처리 종료

        Sprite[] sprites = propAtlas ??= LoadAtlas("MapObjects", PropRowCount, PropPivot, PropCellWidth); // 최초 요청에서 지도 물건 원화 분할
        return sprites.Length == 0 ? null : sprites[index]; // 지정 물건 이미지 또는 누락 시 대체 신호
    } // 지도 물건 조회 종료

    internal static Sprite GetActorFrame(ExplorationActorStyle style, int facingIndex, int frameIndex) // 종류·방향·자세에 해당하는 이미지 조회
    { // 캐릭터 자세 조회 시작
        int styleIndex = (int)style; // 캐릭터 종류에 해당하는 원화 번호
        if ((uint)styleIndex >= (uint)ActorAtlasNames.Length || (uint)facingIndex >= DirectionCount || (uint)frameIndex >= FramesPerDirection) // 지원하는 종류와 자세 범위 확인
        { // 미지원 캐릭터 자세 처리 시작
            return null; // 기존 캐릭터 표시를 쓰는 반환값
        } // 미지원 캐릭터 자세 처리 종료

        Sprite[] sprites = ActorAtlases[styleIndex] ??= LoadAtlas(ActorAtlasNames[styleIndex], ActorRowCount, ActorPivot, ActorCellWidth); // 같은 종류의 원화를 처음 한 번만 분할
        int index = facingIndex * FramesPerDirection + frameIndex; // 각 방향의 두 자세를 연속 배치한 원화 번호
        return sprites.Length == 0 ? null : sprites[index]; // 지정 자세 이미지 또는 누락 시 대체 신호
    } // 캐릭터 자세 조회 종료

    private static Sprite[] LoadAtlas(string name, int rowCount, Vector2 pivot, float worldCellWidth) // 정사각형 격자의 개별 원화 분할
    { // 원본 텍스처 분할 시작
        Texture2D texture = Resources.Load<Texture2D>(ResourceFolder + name); // 지정된 원본 텍스처 조회
        if (texture == null || texture.width % ColumnCount != 0 || texture.height != texture.width / ColumnCount * rowCount) // 원본의 존재와 정확한 격자 확인
        { // 텍스처 누락 또는 규격 불일치 처리 시작
            return Array.Empty<Sprite>(); // 기존 표시를 유지할 빈 원화 묶음
        } // 텍스처 누락 또는 규격 불일치 처리 종료

        int cellSize = texture.width / ColumnCount; // 정사각형 원화 한 칸의 픽셀 수
        float pixelsPerUnit = cellSize / worldCellWidth; // 루트 배율에 맞춘 월드 단위당 픽셀 수
        Sprite[] sprites = new Sprite[ColumnCount * rowCount]; // 각 칸을 저장할 공유 스프라이트 배열
        for (int index = 0; index < sprites.Length; index++) // 위쪽 행부터 왼쪽 순서로 원화 순회
        { // 스프라이트 한 칸 준비 시작
            int left = index % ColumnCount * cellSize; // 텍스처 왼쪽부터 현재 열까지의 거리
            int bottom = texture.height - (index / ColumnCount + 1) * cellSize; // 텍스처 아래부터 현재 행까지의 거리
            Rect rect = new Rect(left, bottom, cellSize, cellSize); // 원본 안에서 잘라낼 정사각형
            Sprite sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect); // 원본 텍스처를 공유하는 개별 원화
            sprite.name = name + "_" + index; // 원본 이름과 원화 칸 번호
            sprite.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild; // 생성한 원화의 씬과 빌드 저장 제외
            sprites[index] = sprite; // 다음 요청에서 재사용할 원화 기록
        } // 스프라이트 한 칸 준비 종료

        return sprites; // 원본 하나에서 분할한 전체 이미지
    } // 원본 텍스처 분할 종료
} // 탐사 원화 조회 종료
