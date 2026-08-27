using System; // 난수 생성 기능 사용
using System.Collections.Generic; // 좌표 목록과 집합 사용
using UnityEngine; // 격자 좌표와 수학 기능 사용

public static class ExplorationMapGenerator // 탐사 논리 맵 절차 생성기
{
    private const int NormalWeightEnd = 45; // 일반 방 누적 가중치
    private const int EliteWeightEnd = 60; // 엘리트 방 누적 가중치
    private const int EventWeightEnd = 80; // 이벤트 방 누적 가중치
    private const int TreasureWeightEnd = 90; // 보물 방 누적 가중치
    private const int RestWeightEnd = 95; // 휴식 방 누적 가중치
    private const int RoomTypeSeedSalt = 1489236017; // 방 역할 난수 분리값

    private static readonly Vector2Int[] Directions = // 상하좌우 확장 방향
    {
        Vector2Int.up, // 위쪽 방향
        Vector2Int.right, // 오른쪽 방향
        Vector2Int.down, // 아래쪽 방향
        Vector2Int.left // 왼쪽 방향
    };

    public static ExplorationMapData Generate(int requestedCellCount, int seed) // 지정 셀 수와 시드로 맵 생성
    {
        int targetCellCount = Mathf.Max(2, requestedCellCount); // 최소 두 셀 보장
        System.Random random = new System.Random(seed); // 시드 기반 난수 생성기 준비
        HashSet<Vector2Int> coordinates = new HashSet<Vector2Int>(); // 중복 방지 좌표 집합 생성
        List<Vector2Int> creationOrder = new List<Vector2Int>(); // 생성 순서 좌표 목록 생성
        Vector2Int startCoordinate = Vector2Int.zero; // 시작 좌표 원점 지정

        coordinates.Add(startCoordinate); // 시작 좌표 등록
        creationOrder.Add(startCoordinate); // 시작 좌표 생성 순서 등록

        while (creationOrder.Count < targetCellCount) // 목표 셀 수까지 반복
        {
            Vector2Int parentCoordinate = creationOrder[random.Next(creationOrder.Count)]; // 기존 셀 하나 무작위 선택
            Vector2Int direction = Directions[random.Next(Directions.Length)]; // 확장 방향 무작위 선택
            Vector2Int candidateCoordinate = parentCoordinate + direction; // 새 셀 후보 좌표 계산

            if (!coordinates.Add(candidateCoordinate)) // 이미 존재하는 좌표 확인
            {
                continue; // 중복 좌표 생성 생략
            }

            creationOrder.Add(candidateCoordinate); // 연결된 새 셀 등록
        }

        Vector2Int stairsCoordinate = FindStairsCoordinate(creationOrder, random); // 시작점에서 먼 계단 좌표 선정
        List<ExplorationMapCell> cells = CreateCells(creationOrder, startCoordinate, stairsCoordinate); // 논리 셀 데이터 생성
        ApplyConnections(cells, coordinates); // 상하좌우 연결 정보 계산
        ApplyRoomTypes(cells, startCoordinate, stairsCoordinate, seed); // 56일차 방 콘텐츠 역할 배정

        return new ExplorationMapData(seed, cells, startCoordinate, stairsCoordinate); // 완성된 탐사 맵 반환
    }

    public static ExplorationRoomType ResolveRoomTypeForRoll(
        int roll,
        bool canCreateRest,
        bool canCreateShop) // 0~99 가중치 값 기반 방 역할 결정
    {
        int safeRoll = Mathf.Clamp(roll, 0, 99); // 가중치 입력 범위 보정

        if (safeRoll < NormalWeightEnd) // 일반 방 범위 확인
        {
            return ExplorationRoomType.Normal; // 일반 전투 방 반환
        }

        if (safeRoll < EliteWeightEnd) // 엘리트 방 범위 확인
        {
            return ExplorationRoomType.Elite; // 엘리트 전투 방 반환
        }

        if (safeRoll < EventWeightEnd) // 이벤트 방 범위 확인
        {
            return ExplorationRoomType.Event; // 이벤트 방 반환
        }

        if (safeRoll < TreasureWeightEnd) // 보물 방 범위 확인
        {
            return ExplorationRoomType.Treasure; // 보물 방 반환
        }

        if (safeRoll < RestWeightEnd) // 휴식 방 범위 확인
        {
            return canCreateRest
                ? ExplorationRoomType.Rest
                : ExplorationRoomType.Normal; // 휴식 최대 수 초과 시 일반 방 대체
        }

        return canCreateShop
            ? ExplorationRoomType.Shop
            : ExplorationRoomType.Normal; // 상점 최대 수 초과 시 일반 방 대체
    }

    private static Vector2Int FindStairsCoordinate(List<Vector2Int> coordinates, System.Random random) // 계단 위치 선정
    {
        Vector2Int selectedCoordinate = coordinates[1]; // 시작점 외 초기 후보 지정
        int selectedDistance = GetManhattanDistance(selectedCoordinate); // 초기 후보 거리 계산

        for (int index = 2; index < coordinates.Count; index++) // 나머지 좌표 순회
        {
            Vector2Int candidate = coordinates[index]; // 현재 후보 좌표 조회
            int candidateDistance = GetManhattanDistance(candidate); // 시작점 거리 계산

            if (candidateDistance > selectedDistance) // 더 먼 좌표 여부 확인
            {
                selectedCoordinate = candidate; // 더 먼 좌표 선택
                selectedDistance = candidateDistance; // 최대 거리 갱신
                continue; // 다음 좌표 확인
            }

            if (candidateDistance == selectedDistance && random.Next(2) == 0) // 동일 거리 후보 무작위 교체 여부 확인
            {
                selectedCoordinate = candidate; // 동일 최장 거리 후보 선택
            }
        }

        return selectedCoordinate; // 최종 계단 좌표 반환
    }

    private static int GetManhattanDistance(Vector2Int coordinate) // 시작점 기준 격자 거리 계산
    {
        return Mathf.Abs(coordinate.x) + Mathf.Abs(coordinate.y); // 맨해튼 거리 반환
    }

    private static List<ExplorationMapCell> CreateCells(
        List<Vector2Int> coordinates,
        Vector2Int startCoordinate,
        Vector2Int stairsCoordinate) // 좌표를 논리 셀 데이터로 변환
    {
        List<ExplorationMapCell> cells = new List<ExplorationMapCell>(coordinates.Count); // 셀 목록 생성

        foreach (Vector2Int coordinate in coordinates) // 전체 좌표 순회
        {
            ExplorationMapCell cell = new ExplorationMapCell(coordinate); // 논리 셀 생성

            if (coordinate == startCoordinate) // 시작 좌표 여부 확인
            {
                cell.SetType(ExplorationCellType.Start); // 시작 셀 지정
            }
            else if (coordinate == stairsCoordinate) // 계단 좌표 여부 확인
            {
                cell.SetType(ExplorationCellType.Stairs); // 계단 셀 지정
            }

            cells.Add(cell); // 완성 셀 목록 등록
        }

        return cells; // 전체 셀 목록 반환
    }

    private static void ApplyRoomTypes(
        List<ExplorationMapCell> cells,
        Vector2Int startCoordinate,
        Vector2Int stairsCoordinate,
        int seed) // Seed 기반 방 콘텐츠 역할 배정
    {
        System.Random random = new System.Random(seed ^ RoomTypeSeedSalt); // 방 역할 전용 난수 생성기 준비
        bool restCreated = false; // 휴식 방 생성 여부 초기화
        bool shopCreated = false; // 상점 방 생성 여부 초기화

        foreach (ExplorationMapCell cell in cells) // 전체 셀 순회
        {
            if (cell.Coordinate == startCoordinate) // 시작 방 확인
            {
                cell.SetRoomType(ExplorationRoomType.Normal); // 시작 방 특수 콘텐츠 제외
                continue; // 다음 방 처리
            }

            if (cell.Coordinate == stairsCoordinate) // 최장 거리 계단 방 확인
            {
                cell.SetRoomType(ExplorationRoomType.Boss); // 최장 거리 방 보스 역할 고정
                continue; // 다음 방 처리
            }

            int roll = random.Next(100); // 방 역할 가중치 난수 생성
            ExplorationRoomType roomType = ResolveRoomTypeForRoll(
                roll,
                !restCreated,
                !shopCreated); // 상점·휴식 최대 한 개 제한 포함 역할 결정

            cell.SetRoomType(roomType); // 현재 방 역할 저장

            if (roomType == ExplorationRoomType.Rest) // 휴식 방 생성 확인
            {
                restCreated = true; // 추가 휴식 방 생성 차단
            }
            else if (roomType == ExplorationRoomType.Shop) // 상점 방 생성 확인
            {
                shopCreated = true; // 추가 상점 방 생성 차단
            }
        }
    }

    private static void ApplyConnections(
        List<ExplorationMapCell> cells,
        HashSet<Vector2Int> coordinates) // 셀별 상하좌우 연결 정보 계산
    {
        foreach (ExplorationMapCell cell in cells) // 전체 셀 순회
        {
            Vector2Int coordinate = cell.Coordinate; // 현재 셀 좌표 조회
            bool up = coordinates.Contains(coordinate + Vector2Int.up); // 위쪽 셀 존재 확인
            bool right = coordinates.Contains(coordinate + Vector2Int.right); // 오른쪽 셀 존재 확인
            bool down = coordinates.Contains(coordinate + Vector2Int.down); // 아래쪽 셀 존재 확인
            bool left = coordinates.Contains(coordinate + Vector2Int.left); // 왼쪽 셀 존재 확인
            cell.SetConnections(up, right, down, left); // 연결 상태 저장
        }
    }
}
