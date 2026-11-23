using NUnit.Framework; // Unity Editor 테스트 기능 사용
using System.Collections.Generic; // 방 역할 비교 목록 사용
using UnityEngine; // 격자 좌표 사용

public sealed class ExplorationRoomRoleTests // 56일차 방 역할 규칙 테스트
{
    [Test]
    public void Generate_SameSeedCreatesSameRoomRoles() // 동일 Seed 방 역할 재현 확인
    {
        ExplorationMapData firstMap = ExplorationMapGenerator.Generate(14, 123456); // 첫 번째 절차 맵 생성
        ExplorationMapData secondMap = ExplorationMapGenerator.Generate(14, 123456); // 두 번째 절차 맵 생성

        Assert.AreEqual(firstMap.Cells.Count, secondMap.Cells.Count); // 전체 방 수 일치 확인

        for (int index = 0; index < firstMap.Cells.Count; index++) // 전체 방 순회
        {
            ExplorationMapCell firstCell = firstMap.Cells[index]; // 첫 번째 방 조회
            Assert.IsTrue(secondMap.TryGetCell(firstCell.Coordinate, out ExplorationMapCell secondCell)); // 동일 좌표 방 존재 확인
            Assert.AreEqual(firstCell.RoomType, secondCell.RoomType); // 동일 좌표 방 역할 일치 확인
        }
    }

    [Test]
    public void Generate_StartRoomIsExcludedFromSpecialRoles() // 시작 방 특수 역할 제외 확인
    {
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 654321); // 절차 맵 생성
        Assert.IsTrue(map.TryGetCell(map.StartCoordinate, out ExplorationMapCell startCell)); // 시작 방 조회
        Assert.AreEqual(ExplorationCellType.Start, startCell.Type); // 시작 구조 확인
        Assert.AreEqual(ExplorationRoomType.Normal, startCell.RoomType); // 시작 방 기본 역할 확인
    }

    [Test]
    public void Generate_FarthestStairsRoomIsBoss() // 최장 거리 계단 방 보스 지정 확인
    {
        ExplorationMapData map = ExplorationMapGenerator.Generate(14, 777777); // 절차 맵 생성
        Assert.IsTrue(map.TryGetCell(map.StairsCoordinate, out ExplorationMapCell bossCell)); // 계단 방 조회
        Assert.AreEqual(ExplorationCellType.Stairs, bossCell.Type); // 계단 구조 확인
        Assert.AreEqual(ExplorationRoomType.Boss, bossCell.RoomType); // 보스 역할 확인
    }

    [Test]
    public void Generate_ShopAndRestAreCappedAtOne() // 상점과 휴식 최대 한 개 제한 확인
    {
        for (int seed = 0; seed < 200; seed++) // 여러 Seed 생성 반복
        {
            ExplorationMapData map = ExplorationMapGenerator.Generate(14, seed); // Seed별 절차 맵 생성
            int shopCount = 0; // 상점 방 수 초기화
            int restCount = 0; // 휴식 방 수 초기화

            foreach (ExplorationMapCell cell in map.Cells) // 전체 방 순회
            {
                if (cell.RoomType == ExplorationRoomType.Shop) // 상점 방 확인
                {
                    shopCount += 1; // 상점 방 수 증가
                }

                if (cell.RoomType == ExplorationRoomType.Rest) // 휴식 방 확인
                {
                    restCount += 1; // 휴식 방 수 증가
                }
            }

            Assert.LessOrEqual(shopCount, 1); // 상점 최대 한 개 확인
            Assert.LessOrEqual(restCount, 1); // 휴식 최대 한 개 확인
        }
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
}
