using NUnit.Framework; // Unity Editor 테스트 기능 사용
using UnityEngine; // 격자 좌표 사용

public sealed class ExplorationRoomRoleTests // 57일차 방 역할·층 진행 규칙 테스트
{
    [Test]
    public void Generate_SameSeedAndFloorCreateSameRoomRoles() // 동일 Seed와 층 방 역할 재현 확인
    {
        ExplorationMapData firstMap = ExplorationMapGenerator.Generate(14, 123456, 3); // 첫 번째 절차 맵 생성
        ExplorationMapData secondMap = ExplorationMapGenerator.Generate(14, 123456, 3); // 두 번째 절차 맵 생성

        Assert.AreEqual(firstMap.Cells.Count, secondMap.Cells.Count); // 전체 방 수 일치 확인

        for (int index = 0; index < firstMap.Cells.Count; index++) // 전체 방 순회
        {
            ExplorationMapCell firstCell = firstMap.Cells[index]; // 첫 번째 방 조회
            Assert.IsTrue(secondMap.TryGetCell(firstCell.Coordinate, out ExplorationMapCell secondCell)); // 동일 좌표 방 존재 확인
            Assert.AreEqual(firstCell.RoomType, secondCell.RoomType); // 동일 좌표 방 역할 일치 확인
        }
    }

    [Test]
    public void Generate_StartRoomIsSafeNormalRole() // 시작 방 안전 역할 확인
    {
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 654321, 1); // 1층 절차 맵 생성
        Assert.IsTrue(map.TryGetCell(map.StartCoordinate, out ExplorationMapCell startCell)); // 시작 방 조회
        Assert.AreEqual(ExplorationCellType.Start, startCell.Type); // 시작 구조 확인
        Assert.AreEqual(ExplorationRoomType.Normal, startCell.RoomType); // 시작 방 기본 역할 확인
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Generate_NormalFloorStairsRoomIsElite(int floor) // 일반 층 마지막 방 엘리트 관문 확인
    {
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 777777, floor); // 지정 일반 층 맵 생성
        Assert.IsTrue(map.TryGetCell(map.StairsCoordinate, out ExplorationMapCell gateCell)); // 마지막 진행 방 조회
        Assert.AreEqual(ExplorationCellType.Stairs, gateCell.Type); // 계단 구조 확인
        Assert.AreEqual(ExplorationRoomType.Elite, gateCell.RoomType); // 엘리트 관문 역할 확인
    }

    [TestCase(5)]
    [TestCase(10)]
    public void Generate_BossFloorStairsRoomIsBoss(int floor) // 5층·10층 마지막 방 보스 관문 확인
    {
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 888888, floor); // 지정 보스 층 맵 생성
        Assert.IsTrue(map.TryGetCell(map.StairsCoordinate, out ExplorationMapCell gateCell)); // 마지막 진행 방 조회
        Assert.AreEqual(ExplorationCellType.Stairs, gateCell.Type); // 계단 구조 확인
        Assert.AreEqual(ExplorationRoomType.Boss, gateCell.RoomType); // 보스 관문 역할 확인
    }

    [Test]
    public void Generate_StandardFloorGuaranteesOneShopAndOneRest() // 기본 14방 층 상점·휴식 한 개 보장 확인
    {
        for (int seed = 0; seed < 200; seed++) // 여러 Seed 생성 반복
        {
            ExplorationMapData map = ExplorationMapGenerator.Generate(14, seed, 3); // Seed별 일반 층 생성
            int shopCount = CountRoomType(map, ExplorationRoomType.Shop); // 상점 방 수 계산
            int restCount = CountRoomType(map, ExplorationRoomType.Rest); // 휴식 방 수 계산

            Assert.AreEqual(1, shopCount, $"Seed {seed} 상점 방 수"); // 상점 정확히 한 개 확인
            Assert.AreEqual(1, restCount, $"Seed {seed} 휴식 방 수"); // 휴식 정확히 한 개 확인
        }
    }

    [Test]
    public void Generate_EventAndTreasureRespectMaximumCounts() // 이벤트·보물 방 최대 수 확인
    {
        for (int seed = 0; seed < 200; seed++) // 여러 Seed 생성 반복
        {
            ExplorationMapData map = ExplorationMapGenerator.Generate(14, seed, 7); // Seed별 일반 층 생성
            int eventCount = CountRoomType(map, ExplorationRoomType.Event); // 이벤트 방 수 계산
            int treasureCount = CountRoomType(map, ExplorationRoomType.Treasure); // 보물 방 수 계산

            Assert.LessOrEqual(eventCount, 3, $"Seed {seed} 이벤트 방 수"); // 이벤트 최대 세 개 확인
            Assert.LessOrEqual(treasureCount, 1, $"Seed {seed} 보물 방 수"); // 보물 최대 한 개 확인
        }
    }

    [Test]
    public void FloorRules_OnlyTenthFloorIsFinalBoss() // 최종 보스층 10층 단독 여부 확인
    {
        Assert.IsTrue(ExplorationFloorRules.IsBossFloor(5)); // 5층 중간 보스 확인
        Assert.IsFalse(ExplorationFloorRules.IsFinalBossFloor(5)); // 5층 최종 보스 제외 확인
        Assert.IsTrue(ExplorationFloorRules.IsBossFloor(10)); // 10층 보스 확인
        Assert.IsTrue(ExplorationFloorRules.IsFinalBossFloor(10)); // 10층 최종 보스 확인
    }

    [Test]
    public void ResolveRoomType_UsesConfiguredWeightBoundaries() // 방 역할 가중치 경계 확인
    {
        Assert.AreEqual(ExplorationRoomType.Normal, ExplorationMapGenerator.ResolveRoomTypeForRoll(0, true, true)); // 일반 시작 경계 확인
        Assert.AreEqual(ExplorationRoomType.Normal, ExplorationMapGenerator.ResolveRoomTypeForRoll(44, true, true)); // 일반 끝 경계 확인
        Assert.AreEqual(ExplorationRoomType.Elite, ExplorationMapGenerator.ResolveRoomTypeForRoll(45, true, true)); // 엘리트 시작 경계 확인
        Assert.AreEqual(ExplorationRoomType.Event, ExplorationMapGenerator.ResolveRoomTypeForRoll(60, true, true)); // 이벤트 시작 경계 확인
        Assert.AreEqual(ExplorationRoomType.Treasure, ExplorationMapGenerator.ResolveRoomTypeForRoll(80, true, true)); // 보물 시작 경계 확인
        Assert.AreEqual(ExplorationRoomType.Rest, ExplorationMapGenerator.ResolveRoomTypeForRoll(90, true, true)); // 휴식 시작 경계 확인
        Assert.AreEqual(ExplorationRoomType.Shop, ExplorationMapGenerator.ResolveRoomTypeForRoll(95, true, true)); // 상점 시작 경계 확인
    }

    private static int CountRoomType(
        ExplorationMapData map,
        ExplorationRoomType roomType) // 지정 방 역할 개수 계산
    {
        int count = 0; // 방 수 초기화

        foreach (ExplorationMapCell cell in map.Cells) // 전체 방 순회
        {
            if (cell.RoomType == roomType) // 지정 역할 일치 여부 확인
            {
                count += 1; // 방 수 증가
            }
        }

        return count; // 최종 방 수 반환
    }
}
