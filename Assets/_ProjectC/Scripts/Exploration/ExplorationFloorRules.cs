public static class ExplorationFloorRules // 57일차 탐사 층 진행 규칙
{
    public const int TotalFloorCount = 10; // 한 탐사 총 층수
    public const int BossFloorInterval = 5; // 보스 등장 층 간격

    public static bool IsBossFloor(int floor) // 현재 층 보스층 여부 확인
    {
        return floor > 0 &&
               floor <= TotalFloorCount &&
               floor % BossFloorInterval == 0; // 5F와 10F 보스층 판정
    }

    public static bool IsFinalBossFloor(int floor) // 최종 보스층 여부 확인
    {
        return floor == TotalFloorCount; // 10F만 최종 보스층 판정
    }

    public static ExplorationRoomType GetGateRoomType(int floor) // 현재 층 마지막 진행 방 역할 조회
    {
        return IsBossFloor(floor)
            ? ExplorationRoomType.Boss
            : ExplorationRoomType.Elite; // 일반 층 엘리트·5/10F 보스 관문 지정
    }
}
