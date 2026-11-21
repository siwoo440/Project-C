using System; // 바닥 판정 함수와 정수 연산 사용
using System.Collections.Generic; // 탐색 대기열과 방문 기록 사용
using UnityEngine; // 타일 셀 좌표 사용

internal sealed class ExplorationPursuitGrid // 탐사 적의 격자 시야와 최단 경로 탐색
{ // 추적 격자 시작
    private static readonly Vector3Int[] NeighborOffsets = // 길이가 같은 경로의 고정 방향 순서
    { // 이웃 방향 목록 시작
        new Vector3Int(0, 1, 0), // 위
        new Vector3Int(1, 0, 0), // 오른쪽
        new Vector3Int(0, -1, 0), // 아래
        new Vector3Int(-1, 0, 0), // 왼쪽
        new Vector3Int(1, 1, 0), // 오른쪽 위
        new Vector3Int(1, -1, 0), // 오른쪽 아래
        new Vector3Int(-1, -1, 0), // 왼쪽 아래
        new Vector3Int(-1, 1, 0) // 왼쪽 위
    }; // 이웃 방향 목록 종료

    private readonly Queue<Vector3Int> pending = new Queue<Vector3Int>(); // 재사용할 방문 대기열
    private readonly Dictionary<Vector3Int, Vector3Int> firstSteps = new Dictionary<Vector3Int, Vector3Int>(); // 방문 셀마다 출발점에서의 첫 이동 기록

    internal static bool HasLineOfSight(Vector3Int start, Vector3Int end, Func<Vector3Int, bool> isFloor) // 두 바닥 셀 사이의 직선 시야 확인
    { // 직선 시야 검사 시작
        if (start.z != end.z || !isFloor(start)) // 같은 평면의 출발 바닥 확인
        { // 잘못된 출발점 처리 시작
            return false; // 시야 차단 반환
        } // 잘못된 출발점 처리 종료

        long deltaX = (long)end.x - start.x; // 부호를 포함한 가로 거리
        long deltaY = (long)end.y - start.y; // 부호를 포함한 세로 거리
        int stepX = Math.Sign(deltaX); // 가로 이동 방향
        int stepY = Math.Sign(deltaY); // 세로 이동 방향
        long distanceX = Math.Abs(deltaX); // 가로 이동 칸 수
        long distanceY = Math.Abs(deltaY); // 세로 이동 칸 수
        long boundaryDifference = distanceY - distanceX; // 다음 가로·세로 경계 도달 시점의 초기 차이
        Vector3Int current = start; // 시야 검사 시작 셀 지정

        while (current != end) // 도착 셀까지 축별 경계 통과
        { // 한 경계 처리 시작
            bool crossesX = boundaryDifference <= 0; // 가로 축 경계 선행 또는 동시 통과 여부
            bool crossesY = boundaryDifference >= 0; // 세로 축 경계 선행 또는 동시 통과 여부
            Vector3Int next = new Vector3Int( // 경계 통과 후 검사할 셀
                current.x + (crossesX ? stepX : 0), // 가로 축 이동 적용
                current.y + (crossesY ? stepY : 0), // 세로 축 이동 적용
                current.z); // 같은 타일 평면 유지

            if (!CanEnter(current, next, isFloor)) // 통과하는 셀과 모서리 양옆의 바닥 확인
            { // 벽에 막힌 시야 처리 시작
                return false; // 막힌 시야 반환
            } // 벽에 막힌 시야 처리 종료

            current = next; // 확인한 셀로 검사 위치 갱신
            boundaryDifference += (crossesX ? 2 * distanceY : 0) - (crossesY ? 2 * distanceX : 0); // 통과한 축의 다음 경계 차이 갱신
        } // 한 경계 처리 종료

        return true; // 두 셀 사이의 시야 확보 반환
    } // 직선 시야 검사 종료

    internal bool TryNextStep(Vector3Int start, Vector3Int goal, Vector3Int spawn, int leash, Func<Vector3Int, bool> isFloor, out Vector3Int step) // 범위 안 최단 경로의 첫 이동 조회
    { // 최단 경로 탐색 시작
        step = default; // 경로가 없을 때의 출력 기본값

        if (start == goal || // 현재 셀에 이미 도착한 상태 확인
            !IsWithinLeash(start, spawn, leash) || !IsWithinLeash(goal, spawn, leash) || // 추적 범위 내 시작점과 목표 확인
            !isFloor(start) || !isFloor(goal)) // 출발과 목표의 실제 바닥 확인
        { // 탐색할 이동이 없는 경우 처리 시작
            return false; // 첫 이동 없음 반환
        } // 탐색할 이동이 없는 경우 처리 종료

        pending.Clear(); // 이전 탐색의 대기열 비우기
        firstSteps.Clear(); // 이전 탐색의 방문 기록 비우기
        pending.Enqueue(start); // 출발 셀 탐색 예약
        firstSteps.Add(start, start); // 시작 셀 중복 방문 방지

        while (pending.Count > 0) // 이동 횟수가 적은 셀부터 검사
        { // 셀 단위 탐색 시작
            Vector3Int current = pending.Dequeue(); // 다음 탐색 셀 조회
            Vector3Int firstStep = firstSteps[current]; // 현재 셀까지의 첫 이동 조회

            foreach (Vector3Int offset in NeighborOffsets) // 여덟 이웃을 고정 순서로 검사
            { // 이웃 단위 탐색 시작
                Vector3Int next = current + offset; // 다음 이동 후보 셀

                if (!IsWithinLeash(next, spawn, leash) || firstSteps.ContainsKey(next) || !CanEnter(current, next, isFloor)) // 범위 밖과 방문 또는 진입 불가 셀 제외
                { // 이동 불가 셀 처리 시작
                    continue; // 다음 이웃 검사
                } // 이동 불가 셀 처리 종료

                Vector3Int nextFirstStep = current == start ? next : firstStep; // 출발 이웃의 좌표를 이후에도 첫 이동으로 유지

                if (next == goal) // 최소 이동 횟수의 목표 도착 확인
                { // 최단 경로 반환 시작
                    step = nextFirstStep; // 최단 경로의 첫 이동 출력
                    return true; // 목표 경로 발견 반환
                } // 최단 경로 반환 종료

                firstSteps.Add(next, nextFirstStep); // 새 셀 방문과 첫 이동 기록
                pending.Enqueue(next); // 새 셀 탐색 예약
            } // 이웃 단위 탐색 종료
        } // 셀 단위 탐색 종료

        return false; // 도달 가능한 목표 경로 없음 반환
    } // 최단 경로 탐색 종료

    private static bool IsWithinLeash(Vector3Int cell, Vector3Int spawn, int leash) // 기준 셀과의 체비쇼프 거리 제한 확인
    { // 거리 제한 검사 시작
        return cell.z == spawn.z && // 스폰 셀과 같은 타일 평면 확인
               Math.Abs((long)cell.x - spawn.x) <= leash && // 가로 거리 제한 확인
               Math.Abs((long)cell.y - spawn.y) <= leash; // 세로 거리 제한 확인
    } // 거리 제한 검사 종료

    private static bool CanEnter(Vector3Int current, Vector3Int next, Func<Vector3Int, bool> isFloor) // 이웃 바닥 및 모서리 통과 가능 여부 확인
    { // 이웃 진입 검사 시작
        return isFloor(next) && // 진입할 셀의 바닥 확인
               (current.x == next.x || current.y == next.y || // 한 축으로만 이동하는 경우의 모서리 검사 생략
                (isFloor(new Vector3Int(next.x, current.y, current.z)) && // 대각선 모서리의 가로 쪽 바닥 확인
                 isFloor(new Vector3Int(current.x, next.y, current.z)))); // 대각선 모서리의 세로 쪽 바닥 확인
    } // 이웃 진입 검사 종료
} // 추적 격자 종료
