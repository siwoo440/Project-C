using System.Collections.Generic; // 위험 방과 Floor 위치 목록 사용
using UnityEngine; // 월드 오브젝트와 SpriteRenderer 사용

public sealed class ExplorationHazardOverlayView : MonoBehaviour // 퇴색 위험 방 월드 표시
{
    private readonly List<GameObject> overlayObjects =
        new List<GameObject>(); // 현재 위험 표시 오브젝트 목록

    private readonly List<Vector2> floorWorldPositions =
        new List<Vector2>(); // 현재 방 Floor 위치 임시 목록

    public void Build(
        ExplorationTilemapView tilemapView,
        IReadOnlyDictionary<Vector2Int, ExplorationHazardRoomState> hazardRooms,
        Sprite squareSprite) // 현재 층 퇴색 방 표시 생성
    {
        Clear(); // 기존 오버레이 정리
        Sprite fadeImage = ExplorationWorldArt.GetProp(ExplorationProp.Fade); // 퇴색 방 중심의 보랏빛 환기구 원화

        if (tilemapView == null ||
            hazardRooms == null ||
            squareSprite == null)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, ExplorationHazardRoomState> pair in hazardRooms)
        {
            ExplorationHazardRoomState hazardState =
                pair.Value; // 현재 방 위험 상태 조회

            if (hazardState == null ||
                hazardState.HazardType != ExplorationHazardType.Fade)
            {
                continue;
            }

            if (!tilemapView.TryGetRoomFloorWorldPositions(
                    pair.Key,
                    floorWorldPositions))
            {
                continue;
            }

            GameObject roomRoot =
                new GameObject(
                    $"FadeHazard_{pair.Key.x}_{pair.Key.y}_L{hazardState.Level}"); // 방별 위험 표시 루트 생성

            roomRoot.transform.SetParent(
                transform,
                false); // 탐사 맵 런타임 하위 배치

            overlayObjects.Add(
                roomRoot); // 정리용 위험 표시 루트 등록

            Color overlayColor =
                GetOverlayColor(
                    hazardState.Level); // 위험도별 반투명 색상 조회

            Vector2 roomCenter = tilemapView.GetWorldPosition(pair.Key); // 퇴색 방의 실제 중심 위치
            Vector2 ventPosition = floorWorldPositions.Count > 0 ? floorWorldPositions[0] : roomCenter; // 방 중앙 근처의 실제 바닥 위치
            for (int index = 0;
                 index < floorWorldPositions.Count;
                 index++)
            {
                Vector2 worldPosition =
                    floorWorldPositions[index]; // 현재 Floor 타일 World 위치 조회
                if ((worldPosition - roomCenter).sqrMagnitude < (ventPosition - roomCenter).sqrMagnitude) // 현재 바닥이 방 중심에 더 가까운지 확인
                { // 가운데 환기구 위치 갱신 시작
                    ventPosition = worldPosition; // 가까운 바닥 중심 후보 저장
                } // 가운데 환기구 위치 갱신 종료

                GameObject tileOverlay =
                    new GameObject(
                        "FadeTile",
                        typeof(SpriteRenderer)); // Floor 오버레이 오브젝트 생성

                tileOverlay.transform.SetParent(
                    roomRoot.transform,
                    false); // 방 위험 표시 루트 하위 배치

                tileOverlay.transform.position =
                    new Vector3(
                        worldPosition.x,
                        worldPosition.y,
                        0f); // Floor 타일 중앙에 위험 표시 배치

                tileOverlay.transform.localScale =
                    new Vector3(
                        0.96f,
                        0.96f,
                        1f); // 타일 사이 경계가 약간 보이도록 크기 설정

                SpriteRenderer spriteRenderer =
                    tileOverlay.GetComponent<SpriteRenderer>(); // 위험 Tile SpriteRenderer 조회

                spriteRenderer.sprite = squareSprite; // 영역 전체의 퇴색 위험도를 나타내는 얇은 색층
                spriteRenderer.color = fadeImage != null ? new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayColor.a * 0.50f) : overlayColor; // 바닥 벽돌과 방 경계를 읽을 수 있는 투명도

                spriteRenderer.sortingOrder = 2; // Floor보다 위, 이벤트·적보다 아래 표시
            }

            if (fadeImage != null && floorWorldPositions.Count > 0) // 생성된 환기구 원화와 실제 바닥 위치 확인
            { // 방 하나의 독립 환기구 표시 시작
                var vent = new GameObject("FadeVent", typeof(SpriteRenderer)); // 방의 환경 위험을 알리는 월드 원화
                vent.transform.SetParent(roomRoot.transform, false); // 방 정리와 함께 없어지는 환기구 위치
                vent.transform.position = ventPosition; // 가장 가까운 실제 중앙 타일에 원화 표시
                vent.transform.localScale = Vector3.one * 1.38f; // 바닥 무늬와 구분되는 환경 오브젝트 크기
                var renderer = vent.GetComponent<SpriteRenderer>(); // 실제 투명 환기구 렌더러 조회
                renderer.sprite = fadeImage; // 생성된 보라색 환기구 이미지 적용
                renderer.sortingOrder = 3; // 색층 앞과 캐릭터 뒤로 배치
            } // 방 하나의 독립 환기구 표시 종료

            CreateRoomMarker(
                roomRoot.transform,
                roomCenter, // 실제 방의 위험도 표시 기준 위치
                hazardState.Level); // 방 중앙 위험도 표식 생성
        }
    }

    public void Clear() // 현재 위험 방 표시 제거
    {
        for (int index = overlayObjects.Count - 1;
             index >= 0;
             index--)
        {
            GameObject overlayObject =
                overlayObjects[index]; // 현재 위험 표시 오브젝트 조회

            if (overlayObject != null)
            {
                Destroy(
                    overlayObject); // 위험 표시 오브젝트 제거
            }
        }

        overlayObjects.Clear(); // 위험 표시 목록 초기화
        floorWorldPositions.Clear(); // Floor 위치 임시 목록 초기화
    }

    private static Color GetOverlayColor(
        int hazardLevel) // 위험도별 퇴색 오버레이 색상 조회
    {
        switch (hazardLevel)
        {
            case 3:
                return new Color(
                    0.60f,
                    0.10f,
                    0.72f,
                    0.42f); // 위험도 3 진한 자주색

            case 2:
                return new Color(
                    0.49f,
                    0.13f,
                    0.65f,
                    0.32f); // 위험도 2 중간 자주색

            default:
                return new Color(
                    0.38f,
                    0.18f,
                    0.58f,
                    0.24f); // 위험도 1 연한 보라색
        }
    }

    private static void CreateRoomMarker(
        Transform parent,
        Vector2 roomCenter,
        int hazardLevel) // 방 중앙 위험도 표식 생성
    {
        GameObject markerObject =
            new GameObject(
                "FadeMarker",
                typeof(TextMesh)); // 위험도 텍스트 표식 생성

        markerObject.transform.SetParent(
            parent,
            false); // 위험 방 루트 하위 배치

        markerObject.transform.position =
            new Vector3(
                roomCenter.x,
                roomCenter.y + 1.8f, // 확대된 위험 환기구에 가리지 않는 안내 위치
                -0.1f); // 방 중앙 위쪽에 표식 배치

        TextMesh markerText =
            markerObject.GetComponent<TextMesh>(); // 위험도 TextMesh 조회

        markerText.text =
            $"!{hazardLevel}"; // 위험도 식별 문자 표시

        markerText.anchor =
            TextAnchor.MiddleCenter; // 문자 중앙 기준 정렬

        markerText.alignment =
            TextAlignment.Center; // 문자 중앙 정렬

        markerText.fontSize = 64; // 문자 해상도 설정
        markerText.characterSize = 0.07f; // 월드 문자 크기 설정
        markerText.color = Color.white; // 위험도 문자 흰색 표시

        MeshRenderer renderer =
            markerObject.GetComponent<MeshRenderer>(); // 문자 렌더러 조회

        if (renderer != null)
        {
            renderer.sortingOrder = 3; // 위험 오버레이보다 앞에 표시
        }
    }
}
