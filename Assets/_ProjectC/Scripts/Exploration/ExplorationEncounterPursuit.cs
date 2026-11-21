using System; // 실제 바닥 셀 판정 콜백 사용
using UnityEngine; // 탐사 적 물리 이동과 시야 표시 기능

[RequireComponent(typeof(Rigidbody2D))] // 탐사 접촉을 물리 틱에서 갱신할 적 몸체
public sealed class ExplorationEncounterPursuit : MonoBehaviour // 발견한 플레이어의 마지막 위치까지 접근하는 탐사 적
{ // 탐사 적 추격 처리 시작
    private const float DetectionRadius = 6f; // 실제 이동 타일 기준 시야 거리
    private const float LostSightMemory = 1.25f; // 마지막으로 본 위치를 추적하는 시간
    private const float ArrivalDistance = 0.02f; // 목적 셀에 도착한 것으로 보는 거리 오차
    private readonly ExplorationPursuitGrid path = new ExplorationPursuitGrid(); // 같은 적의 격자 탐색 자료 재사용
    private ExplorationTilemapView tilemap; // 현재 배치한 층의 바닥 격자
    private ExplorationPlayerController player; // 현재 탐사 플레이어의 위치 조회
    private Rigidbody2D body; // 기존 전투 접촉과 함께 움직이는 물리 몸체
    private SpriteRenderer alert; // 실제 발견 중 머리 위에 표시할 이미지
    private Func<Vector3Int, bool> isFloor; // 현재 층의 실제 바닥 조회 콜백
    private Vector3Int spawnCell; // 조우의 원래 이동 가능 출발 셀
    private Vector2 lastSeenPosition; // 벽에 가려지기 전 플레이어의 월드 위치
    private Vector2 waypoint; // 도중 방향을 급변시키지 않을 현재 목표 셀 중심
    private bool hasWaypoint; // 현재 격자 이동 중 여부
    private float memory; // 마지막 위치를 쫓을 남은 시간
    private float speed; // 기본 조우 등급의 실제 이동 속도
    private int leash; // 원래 방 근처를 지킬 수 있는 최대 격자 반경

    internal Vector2 SpawnPosition { get; private set; } // 시작 위치와 자동 복귀의 원래 지점
    internal bool IsPursuing => memory > 0f; // 실제 시야 확보 또는 마지막 위치 쫓는 중 여부

    internal void Bind(ExplorationTilemapView map, BattleType type) // 배치된 같은 조우에 실제 월드 이동 연결
    { // 적의 현재 층과 원래 위치 설정 시작
        tilemap = map; // 벽과 통로가 실제 생성된 층 저장
        body = GetComponent<Rigidbody2D>(); // 원래 접촉 반경에 연결할 물리 몸체 조회
        body.bodyType = RigidbodyType2D.Kinematic; // 외부 충돌로 밀리지 않는 자동 경로 이동
        body.interpolation = RigidbodyInterpolation2D.Interpolate; // 실제 화면에서 물리 틱 사이 이동 보간
        body.freezeRotation = true; // 적의 방향은 회전 대신 여덟 방향 이미지로 반영
        SpawnPosition = transform.position; // 현재 맵의 조우 저장과 같은 최초 위치 보관
        if (tilemap == null || !tilemap.TryGetWalkableCell(SpawnPosition, out spawnCell)) // 이동 불가 지점은 기존 고정 접촉만 유지
        { // 잘못 배치된 위치의 추격 제외 시작
            enabled = false; // 벽과 다른 방으로 자동 이동하는 동작 차단
            return; // 초기화되지 않은 시야와 경로 사용 차단
        } // 잘못 배치된 위치의 추격 제외 종료
        isFloor = tilemap.IsWalkableCell; // 같은 바닥 격자를 모든 시야와 경로 계산에 사용
        speed = type == BattleType.Boss ? 1.65f : 2.3f; // 보스는 관문 주변에서 느리게 이동
        leash = type == BattleType.Boss ? 4 : 8; // 정상 적의 원위치 복귀와 보스 관문 유지 반경
        var image = ExplorationWorldArt.GetProp(ExplorationProp.Alert); // 발견 안내에 사용할 투명 게임 이미지
        if (image != null) // 실제 투명 안내 리소스를 가져온 경우
        { // 적을 발견한 머리 위 이미지 생성 시작
            var badge = new GameObject("PursuitAlert", typeof(SpriteRenderer)); // 충돌 없는 실제 발견 안내 객체
            badge.transform.SetParent(transform, false); // 현재 움직이는 적을 계속 따라갈 부모 지정
            badge.transform.localPosition = new Vector3(0f, 3.05f, 0f); // 확대된 적의 머리 위에서 겹치지 않는 발견 표시 위치
            badge.transform.localScale = Vector3.one * 0.34f; // 바닥의 상호작용 표식과 구분되는 작은 이미지
            alert = badge.GetComponent<SpriteRenderer>(); // 발견 상태를 표시할 실제 렌더러
            alert.sprite = image; // 전투 원화와 같은 톤의 발견 아이콘 적용
            alert.sortingOrder = 7; // 플레이어와 적 원화 및 바닥 조작보다 앞쪽에 표시
            alert.enabled = false; // 플레이어를 보지 못한 적의 원래 상태
        } // 적을 발견한 머리 위 이미지 생성 종료
    } // 적의 현재 층과 원래 위치 설정 종료

    private void FixedUpdate() // 실제 탐사에서 발견·접근·복귀를 차례로 처리
    { // 물리 틱의 탐사 적 동작 시작
        var session = ExplorationSessionManager.Instance; // 현재 게임의 원래 탐사와 전투 상태 조회
        if (tilemap == null || body == null || ExplorationPlayerController.InputBlocked || session == null || session.ActiveEncounter != null || session.IsExplorationCompleted || SceneFlowManager.Instance?.IsLoadingScene == true) // 모달과 씬 전환 중에는 남은 추격 시간까지 보존
        { // 입력 중지 상태의 추격 일시정지 시작
            return; // 플레이어가 조작할 수 있을 때에만 이동
        } // 입력 중지 상태의 추격 일시정지 종료
        if (player == null) // 현재 층에서 실제 플레이어가 준비되었는지 확인
        { // 시작 파티 캐릭터 연결 시작
            player = FindFirstObjectByType<ExplorationPlayerController>(); // 같은 탐사 씬을 조작할 현재 캐릭터 조회
        } // 시작 파티 캐릭터 연결 종료
        if (player == null || !tilemap.TryGetWalkableCell(body.position, out Vector3Int from)) // 기다리는 기본 씬 또는 벽 밖의 잘못된 경로 확인
        { // 이동할 바닥 미확정 상태 시작
            return; // 현재 원래 조우 위치 보존
        } // 이동할 바닥 미확정 상태 종료

        Vector2 target = player.transform.position; // 갱신된 실제 플레이어 월드 위치
        bool seesPlayer = (target - body.position).sqrMagnitude <= DetectionRadius * DetectionRadius && // 시야 반경에서 먼저 멀리 있는 플레이어 제외
            tilemap.TryGetWalkableCell(target, out Vector3Int targetCell) && // 실제 걷는 바닥에 있는 플레이어만 발견
            Mathf.Abs(targetCell.x - spawnCell.x) <= leash && Mathf.Abs(targetCell.y - spawnCell.y) <= leash && // 최초 배치 주변의 추격 허용 거리
            ExplorationPursuitGrid.HasLineOfSight(from, targetCell, isFloor); // 사이에 벽이나 막힌 모서리가 있는지 직접 검증
        if (seesPlayer) // 실제로 시야에 들어온 플레이어 확인
        { // 마지막으로 확인한 플레이어 위치 갱신 시작
            lastSeenPosition = target; // 가려지는 순간 이후 쫓을 위치 보관
            memory = LostSightMemory; // 시야가 열린 정상 추격 상태 복구
        } // 마지막으로 확인한 플레이어 위치 갱신 종료
        else // 시야를 놓쳤거나 방 경계 밖의 플레이어
        { // 마지막 위치까지만 따라가는 시간 갱신 시작
            memory = Mathf.Max(0f, memory - Time.fixedDeltaTime); // 시간이 끝나면 원래 조우 위치로 복귀
        } // 마지막 위치까지만 따라가는 시간 갱신 종료
        if (alert != null) // 실제 투명 아이콘을 사용 중인 적 확인
        { // 플레이어 발견 표시 갱신 시작
            alert.enabled = IsPursuing; // 짧은 재탐색 동안에만 발견 표시 유지
        } // 플레이어 발견 표시 갱신 종료

        Vector2 destination = IsPursuing ? lastSeenPosition : SpawnPosition; // 마지막으로 본 위치 또는 원래 조우 위치 선택
        if (!hasWaypoint || (body.position - waypoint).sqrMagnitude <= ArrivalDistance * ArrivalDistance) // 현재 셀 중심에 도착하거나 첫 경로 선택 여부 확인
        { // 다음 유효 바닥 셀 경로 선택 시작
            hasWaypoint = TryGetWaypoint(from, destination, out waypoint); // 벽과 모서리를 피하는 다음 실제 셀 계산
        } // 다음 유효 바닥 셀 경로 선택 종료
        if (hasWaypoint) // 실제로 통행 가능한 다음 경로가 있는지 확인
        { // 물리 접촉을 유지한 한 틱 이동 시작
            body.MovePosition(Vector2.MoveTowards(body.position, waypoint, speed * Time.fixedDeltaTime)); // 정해진 이동량만 전진해 기존 접촉 전투 처리 유지
        } // 물리 접촉을 유지한 한 틱 이동 종료
    } // 물리 틱의 탐사 적 동작 종료

    private bool TryGetWaypoint(Vector3Int from, Vector2 destination, out Vector2 next) // 현재 목표를 향할 바닥 위치 탐색
    { // 같은 셀 접근과 인접 격자 이동 나누기 시작
        next = body.position; // 정상 경로가 없을 때의 실제 정지 위치
        if (!tilemap.TryGetWalkableCell(destination, out Vector3Int goal)) // 걷지 못하는 목표 좌표 확인
        { // 바닥 없는 위치 제외 시작
            return false; // 현재 위치 보존
        } // 바닥 없는 위치 제외 종료
        if (from == goal) // 벽을 사이에 두지 않는 같은 실제 바닥 셀 확인
        { // 원래 위치 또는 플레이어 접촉으로 마무리 시작
            next = destination; // 같은 셀 안에서 안전하게 정확한 목표로 접근
            return (body.position - destination).sqrMagnitude > ArrivalDistance * ArrivalDistance; // 이미 도착한 상태의 애니메이션과 이동 중지
        } // 원래 위치 또는 플레이어 접촉으로 마무리 종료
        if (!path.TryNextStep(from, goal, spawnCell, leash, isFloor, out Vector3Int step)) // 벽을 피할 다음 바닥 셀이 있는지 확인
        { // 길이 막힌 경우 처리 시작
            return false; // 벽 위로 이동하거나 시야 밖으로 순간이동하는 동작 차단
        } // 길이 막힌 경우 처리 종료
        next = tilemap.GetWalkableCellCenter(step); // 다음 셀 중심까지 걸어 모서리 가로지르기 방지
        return true; // 다음 실제 이동 경로의 설정 완료
    } // 같은 셀 접근과 인접 격자 이동 나누기 종료
} // 탐사 적 추격 처리 종료
