using UnityEngine; // IMGUI 디버그 표시 기능 사용

public sealed class ExplorationMapDebugView : MonoBehaviour // 39일차 탐사 상태 보존 디버그 화면
{
    private const float CellSize = 28f; // 셀 표시 크기
    private const float CellStep = 36f; // 셀 간 표시 간격
    private const float HeaderHeight = 122f; // 패널 상단 정보 높이
    private const float PanelPadding = 18f; // 패널 내부 여백

    private ExplorationMapRuntime mapRuntime; // 절차 맵 런타임 참조

    private void Awake() // 디버그 화면 초기화
    {
        mapRuntime =
            GetComponent<ExplorationMapRuntime>(); // 동일 오브젝트 맵 런타임 조회
    }

    private void OnGUI() // 논리 맵 화면 표시
    {
        if (mapRuntime == null ||
            mapRuntime.CurrentMap == null)
        {
            return;
        }

        ExplorationMapData mapData =
            mapRuntime.CurrentMap; // 현재 논리 맵 조회

        GetBounds(
            mapData,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY); // 맵 좌표 범위 계산

        int widthInCells =
            maxX - minX + 1; // 가로 셀 범위 계산

        int heightInCells =
            maxY - minY + 1; // 세로 셀 범위 계산

        float contentWidth =
            widthInCells * CellStep; // 맵 표시 가로 크기 계산

        float contentHeight =
            heightInCells * CellStep; // 맵 표시 세로 크기 계산

        float panelWidth =
            contentWidth + PanelPadding * 2f; // 전체 패널 가로 크기 계산

        float panelHeight =
            HeaderHeight +
            contentHeight +
            PanelPadding; // 전체 패널 세로 크기 계산

        float panelX =
            Mathf.Max(
                12f,
                Screen.width -
                panelWidth -
                18f); // 화면 오른쪽 기준 X 계산

        float panelY =
            Mathf.Max(
                12f,
                Screen.height -
                panelHeight -
                18f); // 화면 아래쪽 기준 Y 계산

        string stateText =
            mapRuntime.RestoredFromSession
                ? "복원"
                : "신규"; // 현재 층 상태 표시 문구 결정

        GUI.Box(
            new Rect(
                panelX,
                panelY,
                panelWidth,
                panelHeight),
            $"39일차 상태 보존  |  {mapRuntime.CurrentFloor}F  |  {stateText}\n" +
            $"Seed {mapData.Seed}\n" +
            $"S 시작 / E 조우 / ▼ 계단\n" +
            $"현재 조우 {mapRuntime.CurrentEncounterCount}개\n" +
            $"F9 새 Seed로 현재 층 재생성"); // 디버그 패널 표시

        foreach (ExplorationMapCell cell in mapData.Cells)
        {
            float cellX =
                panelX +
                PanelPadding +
                (cell.Coordinate.x - minX) *
                CellStep; // 셀 화면 X 계산

            float cellY =
                panelY +
                HeaderHeight +
                (maxY - cell.Coordinate.y) *
                CellStep; // 셀 화면 Y 계산

            DrawConnections(
                cell,
                cellX,
                cellY); // 셀 연결선 표시
        }

        foreach (ExplorationMapCell cell in mapData.Cells)
        {
            float cellX =
                panelX +
                PanelPadding +
                (cell.Coordinate.x - minX) *
                CellStep; // 셀 화면 X 계산

            float cellY =
                panelY +
                HeaderHeight +
                (maxY - cell.Coordinate.y) *
                CellStep; // 셀 화면 Y 계산

            GUI.Box(
                new Rect(
                    cellX,
                    cellY,
                    CellSize,
                    CellSize),
                GetCellLabel(cell)); // 셀 종류와 조우 기호 표시
        }
    }

    private string GetCellLabel(
        ExplorationMapCell cell) // 셀 표시 기호 조회
    {
        if (cell.Type == ExplorationCellType.Start)
        {
            return "S"; // 시작 셀 기호 반환
        }

        if (cell.Type == ExplorationCellType.Stairs)
        {
            return "▼"; // 계단 셀 기호 반환
        }

        if (mapRuntime.HasEncounterAt(cell.Coordinate))
        {
            return "E"; // 절차 조우 셀 기호 반환
        }

        return "·"; // 일반 셀 기호 반환
    }

    private static void DrawConnections(
        ExplorationMapCell cell,
        float cellX,
        float cellY) // 셀 연결선 표시
    {
        float centerX =
            cellX + CellSize * 0.5f; // 셀 중심 X 계산

        float centerY =
            cellY + CellSize * 0.5f; // 셀 중심 Y 계산

        float gapLength =
            CellStep - CellSize; // 셀 사이 연결 길이 계산

        if (cell.ConnectedUp)
        {
            GUI.Box(
                new Rect(
                    centerX - 2f,
                    cellY - gapLength,
                    4f,
                    gapLength),
                string.Empty); // 위쪽 연결선 표시
        }

        if (cell.ConnectedRight)
        {
            GUI.Box(
                new Rect(
                    cellX + CellSize,
                    centerY - 2f,
                    gapLength,
                    4f),
                string.Empty); // 오른쪽 연결선 표시
        }

        if (cell.ConnectedDown)
        {
            GUI.Box(
                new Rect(
                    centerX - 2f,
                    cellY + CellSize,
                    4f,
                    gapLength),
                string.Empty); // 아래쪽 연결선 표시
        }

        if (cell.ConnectedLeft)
        {
            GUI.Box(
                new Rect(
                    cellX - gapLength,
                    centerY - 2f,
                    gapLength,
                    4f),
                string.Empty); // 왼쪽 연결선 표시
        }
    }

    private static void GetBounds(
        ExplorationMapData mapData,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY) // 전체 맵 좌표 범위 계산
    {
        ExplorationMapCell firstCell =
            mapData.Cells[0]; // 첫 셀 기준값 조회

        minX = firstCell.Coordinate.x; // 최소 X 초기화
        maxX = firstCell.Coordinate.x; // 최대 X 초기화
        minY = firstCell.Coordinate.y; // 최소 Y 초기화
        maxY = firstCell.Coordinate.y; // 최대 Y 초기화

        foreach (ExplorationMapCell cell in mapData.Cells)
        {
            minX =
                Mathf.Min(
                    minX,
                    cell.Coordinate.x); // 최소 X 갱신

            maxX =
                Mathf.Max(
                    maxX,
                    cell.Coordinate.x); // 최대 X 갱신

            minY =
                Mathf.Min(
                    minY,
                    cell.Coordinate.y); // 최소 Y 갱신

            maxY =
                Mathf.Max(
                    maxY,
                    cell.Coordinate.y); // 최대 Y 갱신
        }
    }
}
