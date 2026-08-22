using System.Collections.Generic; // 셀 목록과 좌표 사전 사용
using UnityEngine; // 격자 좌표 기능 사용

public sealed class ExplorationMapData // 한 층의 절차 생성 맵 데이터
{
    private readonly List<ExplorationMapCell> cells; // 전체 셀 목록
    private readonly Dictionary<Vector2Int, ExplorationMapCell> cellsByCoordinate; // 좌표별 셀 조회 사전

    public int Seed { get; } // 맵 생성 시드
    public Vector2Int StartCoordinate { get; } // 시작 셀 좌표
    public Vector2Int StairsCoordinate { get; } // 계단 셀 좌표
    public IReadOnlyList<ExplorationMapCell> Cells => cells; // 전체 셀 읽기 전용 조회

    public ExplorationMapData(
        int seed,
        List<ExplorationMapCell> mapCells,
        Vector2Int startCoordinate,
        Vector2Int stairsCoordinate) // 절차 맵 데이터 생성
    {
        Seed = seed; // 생성 시드 저장
        cells = mapCells; // 전체 셀 목록 저장
        StartCoordinate = startCoordinate; // 시작 좌표 저장
        StairsCoordinate = stairsCoordinate; // 계단 좌표 저장
        cellsByCoordinate = new Dictionary<Vector2Int, ExplorationMapCell>(); // 좌표 사전 생성

        foreach (ExplorationMapCell cell in cells) // 전체 셀 순회
        {
            cellsByCoordinate[cell.Coordinate] = cell; // 좌표별 셀 등록
        }
    }

    public bool TryGetCell(Vector2Int coordinate, out ExplorationMapCell cell) // 지정 좌표 셀 조회
    {
        return cellsByCoordinate.TryGetValue(coordinate, out cell); // 셀 존재 여부와 데이터 반환
    }
}
