using System.Collections.Generic; // 선택 가능한 막다른 방 목록
using UnityEngine; // 지도 격자 좌표

internal static class OptionalBossPlacement // 녹슨 표지판의 선택 보스 위치 판정
{ // 선택 보스 판정 시작
    internal const string EncounterId = "ENC_WF_ASH_SENTINEL_OPTIONAL_BOSS"; // 관문과 구분되는 선택 조우 원본 식별자
    private const string EncounterPath = "OptionalEncounters/Encounter_WF_AshSentinelOptionalBoss"; // 기본 조우 목록 밖의 선택 보스 경로
    private const int RollSeedSalt = 1065948937; // 원래 방과 조우 추첨을 보존하는 난수 분리값

    internal static EncounterData LoadEncounter() // 승인된 선택 보스 전용 원본 조회
    { // 선택 보스 원본 검사 시작
        EncounterData data = Resources.Load<EncounterData>(EncounterPath); // 기본 관문 풀과 독립된 실제 자산
        return data != null && data.IsValidData() && data.BattleType == BattleType.Boss && data.EncounterId == EncounterId ? data : null; // 유효한 실제 보스 원본만 반환
    } // 선택 보스 원본 검사 종료

    internal static Vector2Int? Select(ExplorationMapData map, int floor, int chance) // 신규 층의 선택 보스 추첨
    { // 층 판정 시작
        if (chance <= 0 || floor < 1 || floor > ExplorationFloorRules.TotalFloorCount || ExplorationFloorRules.IsBossFloor(floor)) // 유물 효과와 유효한 일반 층 확인
        { // 추첨 제외 시작
            return null; // 기존 지도 유지
        } // 추첨 제외 종료
        var random = new System.Random(map.Seed ^ RollSeedSalt ^ (floor * 1709)); // 층과 지도에 고정된 독립 난수
        if (random.Next(100) >= chance) // 층당 한 번의 백분율 추첨
        { // 추첨 실패 처리 시작
            return null; // 선택 보스 없는 지도 유지
        } // 추첨 실패 처리 종료
        var candidates = new List<Vector2Int>(); // 순서가 고정된 막다른 일반 방 목록
        foreach (ExplorationMapCell cell in map.Cells) // 실제 배치 순서의 방 확인
        { // 선택 방 수집 시작
            if (IsEligibleRoom(map, cell)) // 왕복할 수 있는 막다른 일반 전투 방 확인
            { // 후보 등록 시작
                candidates.Add(cell.Coordinate); // 선택할 수 있는 방 좌표 저장
            } // 후보 등록 종료
        } // 선택 방 수집 종료
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : (Vector2Int?)null; // 후보 한 곳 선택 또는 기존 배치 유지
    } // 층 판정 종료

    internal static bool IsEligibleRoom(ExplorationMapData map, ExplorationMapCell cell) // 통과 경로를 가리지 않는 말단 일반 방 확인
    { // 방 안전 조건 확인 시작
        if (cell.Type != ExplorationCellType.Normal || cell.RoomType != ExplorationRoomType.Normal || cell.Coordinate == map.StartCoordinate || cell.Coordinate == map.StairsCoordinate) // 출발점과 계단 및 고유 역할 제외
        { // 일반 방 아님 처리 시작
            return false; // 기존 방 역할 유지
        } // 일반 방 아님 처리 종료
        int exits = (cell.ConnectedUp ? 1 : 0) + (cell.ConnectedRight ? 1 : 0) + (cell.ConnectedDown ? 1 : 0) + (cell.ConnectedLeft ? 1 : 0); // 방에서 나가는 통로 수
        if (exits != 1) // 연결 없음 또는 지나가는 여러 통로 확인
        { // 막다른 방 아님 처리 시작
            return false; // 기존 진행 경로 보호
        } // 막다른 방 아님 처리 종료
        return (cell.ConnectedUp && map.TryGetCell(cell.Coordinate + Vector2Int.up, out var up) && up.ConnectedDown) || // 위쪽 왕복 통로 확인
            (cell.ConnectedRight && map.TryGetCell(cell.Coordinate + Vector2Int.right, out var right) && right.ConnectedLeft) || // 오른쪽 왕복 통로 확인
            (cell.ConnectedDown && map.TryGetCell(cell.Coordinate + Vector2Int.down, out var down) && down.ConnectedUp) || // 아래쪽 왕복 통로 확인
            (cell.ConnectedLeft && map.TryGetCell(cell.Coordinate + Vector2Int.left, out var left) && left.ConnectedRight); // 왼쪽 왕복 통로 확인
    } // 방 안전 조건 확인 종료
} // 선택 보스 판정 종료
