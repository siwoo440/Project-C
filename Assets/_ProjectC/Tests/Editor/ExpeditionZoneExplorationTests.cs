using System; // 지역 열거형의 전체 목록 조회
using System.Collections; // 실제 플레이 모드의 씬 렌더링 대기
using System.Collections.Generic; // 모든 지역의 적 ID와 타일 좌표 보관
using System.Linq; // 정해진 조우와 타일 모양 비교
using NUnit.Framework; // 네이티브 배치와 연결 상태 단언
using UnityEngine; // 실제 씬의 타일 오브젝트 구성
using UnityEngine.TestTools; // Unity 플레이 모드에서 실제 지역 렌더링 검증
using UnityEngine.Tilemaps; // 이동과 충돌에 쓰는 실제 타일맵 조회
using Object = UnityEngine.Object; // 테스트에 만든 실제 Unity 객체 제거

public sealed class ExpeditionZoneExplorationTests // 실제 지역 룸 콘텐츠 및 타일 검증
{ // 지역 탐사의 독립 로딩 검사 시작
    [Test] // 기존 원본과 새 일곱 지역이 섞이지 않는 조우 데이터
    public void RegionalEncountersUseDistinctActorsAndSeparateTheTwoFinalGates() // 실제 네이티브 조우와 다음 출정 진행의 보스 구분
    { // 새 일곱 지역 네이티브 조우 자료 확인 시작
        var enemyIds = new HashSet<string>(StringComparer.Ordinal); // 지역을 통틀어 같은 적 자료를 다시 쓰는지 관측하는 기록
        const int poolVersion = 2; // 기존 윈터폴 실제 보스를 사용할 수 있는 원래 조우 버전
        foreach (ExpeditionZoneId zone in Enum.GetValues(typeof(ExpeditionZoneId))) // 현재 실제 지역을 하나씩 검사
        { // 지역별 기존 또는 추가 조우 검증 시작
            EncounterData boss = ExpeditionZoneEncounters.GetMandatoryBoss(zone, poolVersion); // 현재 지역의 실제 고정 보스 원본
            Assert.IsTrue(ExpeditionZoneEncounters.IsMandatoryBoss(zone, boss), zone.ToString()); // 실제 현재 지역의 보스만 예약 진행에 사용
            if (zone == ExpeditionZoneId.WinterfallAshPlains) // 기존 윈터폴의 원본 보스 경로
            { // 윈터폴 호환 자료 검사 시작
                Assert.AreEqual("ENC_WF_ASHSTORM_CORE", boss.EncounterId); // 원래 세이브와 보스 전투 호환성
                Assert.IsNull(ExpeditionZoneEncounters.GetRegionalEliteGate(zone, poolVersion)); // 기존 필수 보스 관문은 새 정예 전투와 독립
                continue; // 다른 지역의 단계별 별도 자료만 아래에서 비교
            } // 윈터폴 호환 자료 검사 종료
            var candidates = ExpeditionZoneEncounters.LoadRandom(zone, poolVersion); // 지역 내 보통 방에 배치할 일반과 정예 원본
            CollectionAssert.AreEquivalent(new[] { BattleType.Normal, BattleType.Elite }, candidates.Select(entry => entry.BattleType)); // 실제 두 편성만 일반 방과 정예 방 후보
            Assert.IsFalse(ExpeditionZoneEncounters.IsMandatoryBoss(ExpeditionZoneId.WinterfallAshPlains, boss)); // 다른 지역 보스가 윈터폴 승리를 세지 않음
            var eliteGate = ExpeditionZoneEncounters.GetRegionalEliteGate(zone, poolVersion); // 미예약 탐사의 최종 정예 원본
            Assert.IsTrue(ExpeditionZoneEncounters.IsRegionalEliteGate(zone, eliteGate)); // 실제 지역에서만 미예약 완주에 사용하는 원본
            Assert.IsFalse(ExpeditionZoneEncounters.IsMandatoryBoss(zone, eliteGate)); // 최종 정예는 보스 진행을 올리지 않음
            foreach (var encounter in candidates.Append(boss)) // 보통, 정예 및 보스의 실제 적 원본 세 가지
            { // 해당 역할의 조우와 모션 데이터 검증 시작
                var actor = encounter.Enemies.Single(); // 조우 안에 배치한 실제 Unity 적 원본
                Assert.IsTrue(enemyIds.Add(actor.EnemyId), actor.EnemyId); // 다른 역할이나 지역이 같은 원본을 재사용하지 않는 조건
                Assert.GreaterOrEqual(actor.ActionPatternCount, 2); // 실제 전투에서 순환할 두 가지 이상 원래 행동
                Assert.IsTrue(actor.GetActionPattern(0).IsValid); // 첫 번째 행동이 네이티브 실행 가능한 원본인지 확인
                Assert.IsNotNull(actor.Portrait, actor.EnemyId); // 지역과 역할 원화가 실제 적에 연결되는 조건
            } // 해당 역할의 조우와 모션 데이터 검증 종료
        } // 지역별 기존 또는 추가 조우 검증 종료
        Assert.AreEqual(21, enemyIds.Count); // 일곱 새 지역의 일반과 정예 및 보스 원본 합계
    } // 새 일곱 지역 네이티브 조우 자료 확인 종료

    [UnityTest] // 다루는 지역만 바뀌고 기존 실제 이동 공간과 시드는 유지
    public IEnumerator RegionalTileSetChangesAtTheSameSeedAndDisposesThePreviousTiles() // 지역이 바뀌어도 연결 벽과 실제 통행이 일정한 화면
    { // 플레이 모드 타일 교체 검사 시작
        yield return new EnterPlayMode(); // 실제 Unity 렌더러와 격자 충돌 사용
        const int seed = 12055; // 두 물질로 같은 논리 좌표를 검증할 고정 시드
        var root = new GameObject("RegionalTileFixture", typeof(ExplorationTilemapView)); // 주변 탐사 화면과 독립된 실제 Tilemap 대상
        var view = root.GetComponent<ExplorationTilemapView>(); // 원본 런타임 타일맵 렌더러
        try // 검사 실패에도 원래 가상 씬 정리
        { // 같은 시드 지도의 지역별 두 원화 비교 시작
            view.Build(ExplorationMapGenerator.Generate(14, seed, 1), ExpeditionZoneId.FracturedEarth); // 실제 갈라진 대지의 저장 지역으로 생성
            var layers = root.GetComponentsInChildren<Tilemap>().ToDictionary(layer => layer.name); // 네 기존 Floor 및 Wall 레이어 조회
            var floors = Cells(layers["Floor"]); // 갈라진 대지의 실제 이동 좌표 저장
            var walls = Cells(layers["Wall"]); // 갈라진 대지의 실제 벽 좌표 저장
            var edges = Cells(layers["WallEdges"]); // 같은 조합의 벽 모서리 좌표 저장
            var before = layers["Floor"].GetTile<Tile>(floors[0]); // 정리가 필요한 첫 번째 지역의 생성 타일
            var regionTexture = before.sprite.texture; // 실제 지역 원화 파일의 사용 여부
            Assert.AreSame(Resources.Load<Texture2D>("Exploration/Regions/FracturedEarth/FloorVariants"), regionTexture); // 실제 갈라진 대지 원화 아틀라스 사용
            view.Build(ExplorationMapGenerator.Generate(14, seed, 1), ExpeditionZoneId.ScorchedRavine); // 같은 시드에서 다른 실제 저장 지역을 선택
            var now = layers["Floor"].GetTile<Tile>(floors[0]); // 협곡 바닥의 새 실제 재질 객체
            Assert.AreSame(Resources.Load<Texture2D>("Exploration/Regions/ScorchedRavine/FloorVariants"), now.sprite.texture); // 실제 그을린 협곡 원화 아틀라스 사용
            Assert.AreNotSame(regionTexture, now.sprite.texture); // 같은 원화를 색만 바꾸어 반복하지 않는 조건
            CollectionAssert.AreEquivalent(floors, Cells(layers["Floor"])); // 시드 결정적 논리 이동 공간 보존
            CollectionAssert.AreEquivalent(walls, Cells(layers["Wall"])); // 시드 결정적 벽 위치 보존
            CollectionAssert.AreEquivalent(edges, Cells(layers["WallEdges"])); // 이동 경계의 원래 모서리 관계 유지
            Assert.IsTrue(walls.All(cell => layers["Wall"].GetColliderType(cell) == Tile.ColliderType.Grid)); // 벽 변형 모두 같은 전체 충돌
            Assert.IsTrue(floors.All(cell => layers["Floor"].GetColliderType(cell) == Tile.ColliderType.None)); // 바닥 변형 모두 기본 통과 속성
            view.Build(ExplorationMapGenerator.Generate(14, seed, 1), ExpeditionZoneId.ScorchedRavine); // 같은 지역과 시드로 복원
            Assert.AreSame(now, layers["Floor"].GetTile<Tile>(floors[0])); // 같은 지역에 이미 준비한 타일 객체 재사용
            int frame = Time.frameCount; // 실제 지역 전환 직후의 플레이어 프레임 기록
            for (int wait = 0; wait < 20 && (before != null || Time.frameCount <= frame); wait++) // 플레이 모드의 실제 파괴 단계 실행 대기
            { // 프레임 뒤의 실제 객체 정리 비교 시작
                yield return null; // 지연 파괴가 적용되는 실제 다음 프레임
            } // 프레임 뒤의 실제 객체 정리 비교 종료
            Assert.IsTrue(before == null, "이전 구역의 생성 타일이 실제 다음 프레임에 정리돼야 합니다."); // 이전 지역 타일 누적 방지 확인
        } // 같은 시드 지도의 지역별 두 원화 비교 종료
        finally // 씬에 만든 실제 타일 계층 정리 보장
        { // 유니티 테스트 지도 종료 시작
            Object.Destroy(root); // 맵 계층과 실제 생성 타일 둘 다 정리
        } // 유니티 테스트 지도 종료 종료
        yield return new ExitPlayMode(); // 독립 플레이 모드 종료
    } // 플레이 모드 타일 교체 검사 종료

    private static List<Vector3Int> Cells(Tilemap layer) // 실제 레이어에서 타일이 있는 좌표만 수집
    { // 격자에 실제 배치한 원본 좌표 수집 시작
        var occupied = new List<Vector3Int>(); // 실제 원화가 놓인 셀만 담을 비교 목록
        foreach (var cell in layer.cellBounds.allPositionsWithin) // 원래 유니티 범위 열거자로 각 칸 순회
        { // 실제 원화 칸만 수집 시작
            if (layer.HasTile(cell)) // 현재 레이어의 타일 존재 여부
            { // 실제 서로 비교할 이동·벽·경계 좌표 추가 시작
                occupied.Add(cell); // 원래 좌표의 실제 타일 칸 추가
            } // 실제 서로 비교할 이동·벽·경계 좌표 추가 종료
        } // 실제 원화 칸만 수집 종료
        return occupied; // 비어 있는 레이어 영역을 제외한 실제 좌표만 반환
    } // 격자에 실제 배치한 원본 좌표 수집 종료
} // 지역 탐사의 독립 로딩 검사 종료
