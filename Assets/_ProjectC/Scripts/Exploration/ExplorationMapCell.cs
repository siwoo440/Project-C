using UnityEngine; // 격자 좌표 기능 사용

public enum ExplorationCellType // 탐사 셀 구조 종류
{
    Normal = 0, // 일반 셀
    Start = 1, // 시작 셀
    Stairs = 2 // 아래층 계단 셀
}

public enum ExplorationRoomType // 탐사 방 콘텐츠 역할
{
    Normal = 0, // 일반 전투 방
    Elite = 1, // 엘리트 전투 방
    Event = 2, // 탐사 이벤트 방
    Treasure = 3, // 보물 방
    Rest = 4, // 휴식 방
    Shop = 5, // 상점 방
    Boss = 6 // 보스 방
}

public sealed class ExplorationMapCell // 탐사 논리 셀 데이터
{
    public Vector2Int Coordinate { get; } // 셀 격자 좌표
    public ExplorationCellType Type { get; private set; } // 현재 셀 구조 종류
    public ExplorationRoomType RoomType { get; private set; } // 현재 방 콘텐츠 역할
    public bool ConnectedUp { get; private set; } // 위쪽 연결 여부
    public bool ConnectedRight { get; private set; } // 오른쪽 연결 여부
    public bool ConnectedDown { get; private set; } // 아래쪽 연결 여부
    public bool ConnectedLeft { get; private set; } // 왼쪽 연결 여부

    public ExplorationMapCell(Vector2Int coordinate) // 탐사 셀 생성
    {
        Coordinate = coordinate; // 셀 좌표 저장
        Type = ExplorationCellType.Normal; // 기본 일반 셀 지정
        RoomType = ExplorationRoomType.Normal; // 기본 일반 전투 역할 지정
    }

    public void SetType(ExplorationCellType type) // 셀 구조 종류 지정
    {
        Type = type; // 셀 구조 종류 저장
    }

    public void SetRoomType(ExplorationRoomType roomType) // 방 콘텐츠 역할 지정
    {
        RoomType = roomType; // 방 콘텐츠 역할 저장
    }

    public void SetConnections(bool up, bool right, bool down, bool left) // 인접 셀 연결 상태 지정
    {
        ConnectedUp = up; // 위쪽 연결 저장
        ConnectedRight = right; // 오른쪽 연결 저장
        ConnectedDown = down; // 아래쪽 연결 저장
        ConnectedLeft = left; // 왼쪽 연결 저장
    }
}
