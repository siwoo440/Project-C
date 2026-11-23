using System.Collections.Generic; // 런타임 타일과 이미지 목록 사용
using UnityEngine; // 이미지와 타일 좌표 사용
using UnityEngine.Tilemaps; // 타일 유형과 충돌 속성 사용

internal sealed class ExplorationDungeonTiles // 탐사 바닥·벽·장식의 시드 기반 선택
{ // 탐사 타일 선택 시작
    private const int FloorSalt = 58037; // 바닥 무늬 전용 식별값
    private const int WallSalt = 71471; // 벽돌 무늬 전용 식별값
    private const int DetailSalt = 93307; // 장식 위치 전용 식별값
    private static readonly Vector3Int[] Directions = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left }; // 시계 방향의 네 인접 칸
    private static readonly int[] EdgeIndices = BuildEdgeIndices(); // 벽 방향 모양을 원화 칸 번호로 조회

    private readonly List<Object> ownedAssets = new List<Object>(); // 이 탐사 화면에서 생성한 이미지와 타일
    private readonly Tile baseFloor; // 원래 바닥과 단색 대체 타일
    private readonly Tile baseWall; // 원래 벽과 단색 대체 타일
    private readonly Tile[] floorVariants; // 새 석판 무늬 네 가지
    private readonly Tile[] wallVariants; // 새 석재 벽면 네 가지
    private readonly Tile[] wallEdges; // 직선·모서리 조합의 벽면 턱
    private readonly Tile[] details; // 벽등과 격자 및 바닥 잔해
    private readonly uint floorBaseWeight; // 원래 바닥 자료가 차지하는 고정 비중
    private readonly uint wallBaseWeight; // 원래 벽 자료가 차지하는 고정 비중

    internal ExplorationDungeonTiles(ExpeditionZoneId? zone = null) // 원래 기본 자료 또는 선택한 실제 출정 지역의 타일 풀
    { // 타일 자료 준비 시작
        const string sharedPath = "Exploration"; // 기존 기본 바닥과 벽의 리소스 폴더
        string regionPath = zone.HasValue ? sharedPath + "/Regions/" + ExpeditionZones.Get(zone.Value).VisualKey : null; // 현재 출정의 실제 원화 키
        Sprite fallback = CreateSprite(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), "DungeonTileFallback"); // 이미지 누락 시의 한 칸 대체 스프라이트
        var regionalFloor = LoadAtlas(regionPath, "FloorVariants", 4, Tile.ColliderType.None, Color.white, 4); // 지역별 실제 바닥 무늬 네 가지
        var regionalWall = LoadAtlas(regionPath, "WallVariants", 4, Tile.ColliderType.Grid, Color.white, 4); // 전체 격자 충돌을 유지한 지역 벽 네 가지
        bool usesRegionalFloor = regionalFloor.Length > 0; // 저장 지역에 유효한 바닥 제작 이미지 존재 여부
        bool usesRegionalWall = regionalWall.Length > 0; // 저장 지역에 유효한 벽 제작 이미지 존재 여부
        floorBaseWeight = usesRegionalFloor ? 0u : 40u; // 지역 재질은 네 변형을 균등하게 선택
        wallBaseWeight = usesRegionalWall ? 0u : 35u; // 기존 벽이 있는 기본 지도만 기존 비중 적용
        baseFloor = usesRegionalFloor ? regionalFloor[0] : LoadBase("DungeonFloor", new Color(0.12f, 0.20f, 0.28f, 1f), fallback, Tile.ColliderType.None); // 사용할 바닥 기준의 완전한 한 칸
        baseWall = usesRegionalWall ? regionalWall[0] : LoadBase("DungeonWall", new Color(0.05f, 0.07f, 0.10f, 1f), fallback, Tile.ColliderType.Grid); // 사용할 벽 기준의 격자 전체 충돌
        floorVariants = usesRegionalFloor ? regionalFloor : LoadAtlas(sharedPath, "DungeonFloorVariants", 4, Tile.ColliderType.None, Color.white); // 실제 지역 또는 공용 바닥 변형
        wallVariants = usesRegionalWall ? regionalWall : LoadAtlas(sharedPath, "DungeonWallVariants", 4, Tile.ColliderType.Grid, Color.white); // 실제 지역 또는 공용 벽 변형
        var regionalEdges = LoadAtlas(regionPath, "WallEdges", 47, Tile.ColliderType.None, Color.white); // 선택 지역에 제작한 동일 벽 연결 규칙
        var regionalDetails = LoadAtlas(regionPath, "Details", 6, Tile.ColliderType.None, Color.white, 3); // 선택 지역의 벽 장식과 통행 장식
        Color sharedTint = usesRegionalFloor || usesRegionalWall ? GetConnectedTileTint(zone) : Color.white; // 지역 원화에 맞는 공용 경계와 장식 색조
        wallEdges = regionalEdges.Length > 0 ? regionalEdges : LoadAtlas(sharedPath, "DungeonWallEdges", 47, Tile.ColliderType.None, sharedTint); // 실제 도안이나 공용 47가지의 이음 유지
        details = regionalDetails.Length > 0 ? regionalDetails : LoadAtlas(sharedPath, "DungeonDetails", 6, Tile.ColliderType.None, sharedTint); // 물리 충돌과 분리한 벽과 바닥 장식
        float detailScale = ExplorationWorldArt.SizeMultiplier; // 캐릭터와 같은 장식 원화 확대 비율
        Matrix4x4 detailTransform = Matrix4x4.Scale(new Vector3(detailScale, detailScale, 1f)); // 바닥과 벽의 단위 격자에 영향을 주지 않는 장식 그림 배율
        foreach (Tile detail in details) // 벽등과 사슬 및 바닥 잔해의 공유 타일 순회
        { // 물리 없는 장식 배율 적용 시작
            detail.transform = detailTransform; // 현재 장식 중심 기준으로 원화만 두 배 확대
        } // 물리 없는 장식 배율 적용 종료
    } // 타일 자료 준비 종료

    private static Color GetConnectedTileTint(ExpeditionZoneId? zone) // 공유되는 벽 연결 도안을 지역 재질에 맞추는 색
    { // 지역별 연결 돌 색상 선택 시작
        return zone switch // 현재 지역의 바닥·벽과 어울리는 색조 조회
        { // 값별 연결 도안 색 시작
            ExpeditionZoneId.FracturedEarth => new Color32(220, 196, 166, 255), // 갈라진 돌과 흙의 따뜻한 회색
            ExpeditionZoneId.CorrodedIndustrialDistrict => new Color32(179, 207, 178, 255), // 산화 금속과 이끼의 옅은 녹색
            ExpeditionZoneId.FadedDunes => new Color32(246, 216, 162, 255), // 따뜻한 사구와 모래돌 경계
            ExpeditionZoneId.ScorchedRavine => new Color32(243, 150, 113, 255), // 열기로 붉은 현무암 경계
            ExpeditionZoneId.SubterraneanVault => new Color32(160, 214, 221, 255), // 지하 금고의 찬 빛 철제 경계
            ExpeditionZoneId.RuinedCitadel => new Color32(197, 187, 221, 255), // 망령 성채의 보랏빛 돌 경계
            ExpeditionZoneId.TheVoid => new Color32(165, 152, 227, 255), // 공백 영역의 짙은 자주색 경계
            _ => Color.white // 윈터폴 기본 도안의 원래 색
        }; // 값별 연결 도안 색 종료
    } // 지역별 연결 돌 색상 선택 종료

    internal Tile GetFloor(int seed, Vector3Int cell) // 현재 층과 좌표에 맞춘 바닥 무늬
    { // 바닥 원화 선택 시작
        return PickMaterial(baseFloor, floorVariants, HashCell(seed, cell, FloorSalt), floorBaseWeight); // 기본 바닥 마흔 퍼센트와 변형 무늬 선택
    } // 바닥 원화 선택 종료

    internal Tile GetWall(int seed, Vector3Int cell) // 현재 층과 좌표에 맞춘 벽 무늬
    { // 벽 원화 선택 시작
        return PickMaterial(baseWall, wallVariants, HashCell(seed, cell, WallSalt), wallBaseWeight); // 기본 벽과 여러 석재 벽면 선택
    } // 벽 원화 선택 종료

    internal Tile GetWallEdge(int mask) // 바닥과 맞닿은 방향의 돌 테두리
    { // 돌 테두리 선택 시작
        return mask != 0 && wallEdges.Length > 0 ? wallEdges[EdgeIndices[mask]] : null; // 텅 빈 원화와 누락된 자료를 사용하지 않는 반환
    } // 돌 테두리 선택 종료

    internal Tile GetDetail(int seed, Vector3Int cell, bool wall) // 통로를 가리지 않는 드문 환경 장식
    { // 장식 선택 시작
        uint value = HashCell(seed, cell, DetailSalt); // 타일 무늬와 독립된 장식 난수
        if (details.Length == 0 || value % 100 >= 15) // 자산이 있거나 장식할 위치인지 확인
        { // 장식 없는 위치 처리 시작
            return null; // 별도 스프라이트 없이 유지
        } // 장식 없는 위치 처리 종료
        int first = wall ? 0 : 3; // 원화의 벽 장식과 바닥 장식 행 구분
        return details[first + (int)(value / 100 % 3)]; // 종류별 세 장식 중 정해진 하나 반환
    } // 장식 선택 종료

    internal static int GetCardinalMask(Vector3Int cell, HashSet<Vector3Int> floor) // 해당 칸에서 바로 맞닿은 이동 공간
    { // 상하좌우 경계 조사 시작
        int mask = 0; // 북·동·남·서 방향의 바닥 표시
        for (int index = 0; index < Directions.Length; index++) // 네 방향의 이웃을 순서대로 검사
        { // 인접 칸 확인 시작
            if (floor.Contains(cell + Directions[index])) // 이동 가능한 바닥이 바로 이웃한 방향 확인
            { // 바닥 이웃 추가 시작
                mask |= 1 << index; // 해당 방향의 비트 추가
            } // 바닥 이웃 추가 종료
        } // 인접 칸 확인 종료
        return mask; // 직선 벽면과 두 방향 모서리에 쓸 결과
    } // 상하좌우 경계 조사 종료

    internal static int GetWallMask(Vector3Int cell, HashSet<Vector3Int> floor) // 직선과 모서리를 선택할 여덟 방향의 마스크
    { // 벽면 방향 조사 시작
        int mask = GetCardinalMask(cell, floor); // 벽 옆 바닥의 네 면을 우선 기록
        for (int index = 0; index < Directions.Length; index++) // 북동부터 북서까지 순서대로 검사
        { // 대각 이웃 검사 시작
            int next = (index + 1) & 3; // 대각을 이루는 두 번째 방향
            if ((mask & ((1 << index) | (1 << next))) == 0 && floor.Contains(cell + Directions[index] + Directions[next])) // 두 직선이 닿지 않고 대각선에만 바닥이 있는지 확인
            { // 독립 모서리 기록 시작
                mask |= 16 << index; // 대각 바닥을 향한 별도 모서리 적용
            } // 독립 모서리 기록 종료
        } // 대각 이웃 검사 종료
        return mask; // 앞선 직선과 별도 모서리를 합친 벽 조합 반환
    } // 벽면 방향 조사 종료

    private static int[] BuildEdgeIndices() // 자료의 오름차순과 같은 순서로 유효한 벽 모양 번호 구성
    { // 벽면 조회표 구성 시작
        int[] indices = new int[256]; // 여덟 방향의 모든 비트 조합 위치
        int atlasIndex = 0; // 문양이 없는 첫 타일부터 시작
        for (int mask = 0; mask < indices.Length; mask++) // 작은 마스크부터 원화 순서 적용
        { // 한 마스크의 정규화 확인 시작
            int normalized = mask; // 연결된 직선 옆의 중복 모서리를 정리할 값
            for (int index = 0; index < Directions.Length; index++) // 각 대각선을 이루는 두 직선 확인
            { // 중복 모서리 제외 시작
                int adjacent = (1 << index) | (1 << ((index + 1) & 3)); // 대각선 양옆의 두 직선 마스크
                if ((mask & adjacent) != 0) // 대각선 옆에 이미 직선 경계가 있는지 확인
                { // 모서리 겹침 제외 시작
                    normalized &= ~(16 << index); // 직선에 연결되는 중복 대각선 제거
                } // 모서리 겹침 제외 종료
            } // 중복 모서리 제외 종료
            if (normalized == mask) // 독립된 의미가 있는 경계 조합 확인
            { // 실제 원화 번호 기록 시작
                indices[mask] = atlasIndex++; // 바닥 가장자리 원화의 배치 순서
            } // 실제 원화 번호 기록 종료
        } // 한 마스크의 정규화 확인 종료
        return indices; // 벽 조합별 실제 원화 칸 조회표
    } // 벽면 조회표 구성 종료

    private static uint HashCell(int seed, Vector3Int cell, int salt) // 같은 층에서 항상 같은 좌표 무늬를 고르는 값
    { // 좌표 난수 생성 시작
        unchecked // 부호와 정수 범위에 관계없는 순환 정수 연산
        { // 32비트 값 혼합 시작
            uint mixed = (uint)seed ^ (uint)cell.x * 0x9E3779B1u ^ (uint)cell.y * 0x85EBCA77u ^ (uint)salt; // 층 시드와 두 좌표 및 용도 조합
            mixed = (mixed ^ (mixed >> 16)) * 0x7FEB352Du; // 가까운 좌표 사이에 값 섞기
            mixed = (mixed ^ (mixed >> 15)) * 0x846CA68Bu; // 하위 비트까지 섞어 분포 균등화
            return mixed ^ (mixed >> 16); // Unity 전투 난수와 분리된 결정적 값
        } // 32비트 값 혼합 종료
    } // 좌표 난수 생성 종료

    private static Tile PickMaterial(Tile original, Tile[] variants, uint roll, uint originalWeight) // 기본 재질과 여러 변형 중 선택
    { // 석재 무늬 선택 시작
        return variants.Length == 0 || roll % 100 < originalWeight ? original : variants[(int)(roll / 100 % (uint)variants.Length)]; // 원화 누락에도 기본 타일을 사용하는 분포
    } // 석재 무늬 선택 종료

    private Tile LoadBase(string name, Color fallbackColor, Sprite fallback, Tile.ColliderType collision) // 기존 원화 하나와 대체 색상 준비
    { // 기본 타일 준비 시작
        Texture2D texture = Resources.Load<Texture2D>("Exploration/" + name); // 프로젝트의 기존 탐사 원화 조회
        if (texture == null || texture.width != texture.height) // 기존 원화의 유효한 한 칸 크기 확인
        { // 이미지 대체 시작
            return CreateTile(fallback, fallbackColor, collision); // 한 칸 크기의 기존 단색 대체 원화
        } // 이미지 대체 종료
        int margin = Mathf.CeilToInt(texture.width * 0.016f); // 기존 원화 파일의 검은 바깥 여백 폭
        int size = Mathf.Max(1, texture.width - 2 * margin); // 검은 외곽선 없이 표시할 실제 석재 너비
        Sprite sprite = CreateSprite(texture, new Rect((texture.width - size) / 2f, (texture.height - size) / 2f, size, size), name); // 변형 타일과 맞닿을 중앙 석재 원화
        return CreateTile(sprite, Color.white, collision); // 원래 색상과 충돌 형태 유지
    } // 기본 타일 준비 종료

    private Tile[] LoadAtlas(string folder, string name, int count, Tile.ColliderType collision, Color tint, int columns = 0) // 분할 원화나 원본 텍스처를 고정 타일 순서로 읽기
    { // 독립 원화 재질 조회 시작
        if (folder == null) // 기존 타일 화면에서 지역 자료 사용 여부 확인
        { // 선택 지역 미지정 처리 시작
            return System.Array.Empty<Tile>(); // 원래 공용 자료를 사용하는 상태
        } // 선택 지역 미지정 처리 종료
        string path = folder + "/" + name; // 현재 타일 원화의 Unity 리소스 경로
        Sprite[] sprites = Resources.LoadAll<Sprite>(path); // 편집기에서 준비한 개별 원화 조각 우선 조회
        if (sprites.Length != count) // 분할 스프라이트가 준비되지 않은 실제 원본 확인
        { // 단일 원본의 규칙 격자 대체 시작
            return SliceAtlas(path, name, count, columns, collision, tint); // 유효한 셀 규격 원본만 정사각형 런타임 분할
        } // 단일 원본의 규칙 격자 대체 종료
        Sprite[] ordered = new Sprite[count]; // 불러온 순서와 무관한 고정 원화 배열
        string prefix = name + "_"; // 편집기에서 확인 가능한 원화 종류 이름
        foreach (Sprite sprite in sprites) // 자산에서 직접 분할된 원화 순회
        { // 이름의 고정 번호 확인 시작
            if (!sprite.name.StartsWith(prefix, System.StringComparison.Ordinal) || !int.TryParse(sprite.name.Substring(prefix.Length), out int index) || index < 0 || index >= count || ordered[index] != null) // 모양과 변형 순서의 훼손 방지
            { // 잘못된 원화 배열 처리 시작
                return SliceAtlas(path, name, count, columns, collision, tint); // 정확한 셀 규격으로 다시 확인한 뒤 공용 자료로 대체
            } // 잘못된 원화 배열 처리 종료
            ordered[index] = sprite; // 불러온 순서와 무관한 고정 위치로 정렬
        } // 이름의 고정 번호 확인 종료
        Tile[] tiles = new Tile[count]; // 맵에서 여러 셀이 재사용할 타일 배열
        for (int index = 0; index < count; index++) // 고정 위치에 따라 각각 타일 객체 준비
        { // 개별 스프라이트 생성 시작
            tiles[index] = CreateTile(ordered[index], tint, collision); // 동일한 재질 색과 충돌 규칙의 타일 등록
        } // 개별 스프라이트 생성 종료
        return tiles; // 공유할 해당 재질의 전체 타일 반환
    } // 독립 원화 재질 조회 종료

    private Tile[] SliceAtlas(string path, string name, int count, int columns, Tile.ColliderType collision, Color tint) // 실제 지역 원본의 행과 열을 고정 타일로 변환
    { // 정사각형 타일 규격 검사 시작
        Texture2D source = columns > 0 ? Resources.Load<Texture2D>(path) : null; // 분할 규격이 명시된 실제 지역 원본만 조회
        if (source == null || source.width % columns != 0 || source.height != source.width / columns * (count / columns)) // 여백 없는 정확한 행 수와 정방형 셀 확인
        { // 원본 누락 또는 비정방형 처리 시작
            return System.Array.Empty<Tile>(); // 다른 재질까지 바꾸지 않는 독립 대체
        } // 원본 누락 또는 비정방형 처리 종료
        int size = source.width / columns; // 실제 원화 한 칸의 너비와 높이
        var tiles = new Tile[count]; // 인덱스와 좌표가 고정된 런타임 이미지
        for (int index = 0; index < count; index++) // 위쪽 행 먼저, 같은 행은 왼쪽부터 순회
        { // 실제 타일 하나의 스프라이트와 충돌 생성 시작
            var rect = new Rect(index % columns * size, source.height - (index / columns + 1) * size, size, size); // 참조 원화의 왼쪽 위를 기준으로 한 셀
            tiles[index] = CreateTile(CreateSprite(source, rect, name + "_" + index), tint, collision); // 고정 순서와 원래 충돌이 있는 타일
        } // 실제 타일 하나의 스프라이트와 충돌 생성 종료
        return tiles; // 사용할 지역의 실제 원화 타일 집합
    } // 정사각형 타일 규격 검사 종료

    private Sprite CreateSprite(Texture2D texture, Rect rect, string name) // 1 격자에 정확히 맞는 런타임 원화 배치
    { // 스프라이트 준비 시작
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), rect.width, 0, SpriteMeshType.FullRect); // 고정 중심과 한 칸 크기의 전체 사각형
        sprite.name = name; // 자산과 원화 칸의 식별자
        sprite.hideFlags = HideFlags.HideAndDontSave; // 씬 설정에 임시 스프라이트 저장 방지
        ownedAssets.Add(sprite); // 씬을 닫을 때 정리할 개별 이미지
        return sprite; // 런타임 재질에 사용할 스프라이트
    } // 스프라이트 준비 종료

    private Tile CreateTile(Sprite sprite, Color color, Tile.ColliderType collision) // 기존 이동 규칙을 유지한 타일 객체 생성
    { // 타일 생성 시작
        Tile tile = ScriptableObject.CreateInstance<Tile>(); // 여러 셀에 재사용할 실제 타일 객체
        tile.name = sprite.name; // 무늬 및 모서리를 확인할 개별 이름
        tile.sprite = sprite; // 바닥과 벽 및 장식에 사용할 이미지
        tile.color = color; // 기존 원화 색조 또는 대체색
        tile.colliderType = collision; // 실제 벽만 물리 충돌 유지
        tile.hideFlags = HideFlags.HideAndDontSave; // 씬 원본에 임시 타일 저장 방지
        ownedAssets.Add(tile); // 씬과 함께 정리할 개별 타일
        return tile; // 다음 실제 맵 배열에 넣을 타일
    } // 타일 생성 종료

    internal void Release() // 현재 탐사 씬이 만든 타일과 스프라이트 정리
    { // 생성한 자료 정리 시작
        foreach (Object asset in ownedAssets) // 원본 텍스처를 제외한 실제 생성 객체 순회
        { // 임시 자료 제거 시작
            if (Application.isPlaying) // 현재 게임의 프레임 끝 일반 제거 여부
            { // 게임 씬의 예약 제거 시작
                Object.Destroy(asset); // 런타임 스프라이트와 타일의 프레임 끝 정리
            } // 게임 씬의 예약 제거 종료
            else // 편집기에서 실행하지 않는 지도를 닫을 때의 제거 방식
            { // 편집 중 즉시 임시 객체 제거 시작
                Object.DestroyImmediate(asset); // 프로젝트로부터 불러온 텍스처를 제외한 생성 객체만 제거
            } // 편집 중 즉시 임시 객체 제거 종료
        } // 임시 자료 제거 종료
        ownedAssets.Clear(); // 파괴한 객체 목록 참조 해제
    } // 생성한 자료 정리 종료
} // 탐사 타일 선택 종료
