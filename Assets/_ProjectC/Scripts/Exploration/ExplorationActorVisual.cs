using UnityEngine; // 좌표·시간·스프라이트 표시 기능

internal sealed class ExplorationActorVisual : MonoBehaviour // 월드 이동에 맞춘 탐사 캐릭터 표시
{ // 탐사 캐릭터 표시 시작
    private const float StepDistance = 0.36f; // 보행 자세를 바꾸는 누적 이동 거리
    private const float MinimumTravelSquared = 0.000001f; // 작은 위치 오차를 거르는 거리의 제곱
    private const float TeleportDistanceSquared = 1.25f * 1.25f; // 한 화면에서 순간 이동으로 판단할 거리의 제곱
    private const float IdleDelay = 0.075f; // 물리 갱신 사이의 빈 화면을 허용하는 시간

    private SpriteRenderer spriteRenderer; // 원화 적용에 성공한 캐릭터 렌더러
    private ExplorationActorStyle style; // 연결된 캐릭터 원화 종류
    private Vector2 previousPosition; // 직전 화면에서 기록한 월드 위치
    private float stepRemainder; // 다음 보행 자세까지 누적된 이동 거리
    private float secondsStill; // 최근 이동 이후 움직임이 없는 시간

    internal int FacingIndex { get; private set; } // 남쪽부터 반시계 방향의 원화 번호
    internal int CurrentFrameIndex { get; private set; } // 한 방향 안에서 표시 중인 자세 번호

    internal void Bind(SpriteRenderer renderer, ExplorationActorStyle style) // 캐릭터 종류와 표시 대상 연결
    { // 캐릭터 원화 연결 시작
        this.style = style; // 요청된 캐릭터 종류 저장
        FacingIndex = style == ExplorationActorStyle.Hero ? 0 : 6; // 주인공은 남쪽, 적은 왼쪽을 향하는 첫 방향
        CurrentFrameIndex = 0; // 처음 표시할 서 있는 자세
        previousPosition = transform.position; // 생성 또는 재연결 시점의 실제 월드 위치
        stepRemainder = 0f; // 보행에 누적된 이전 이동 거리 초기화
        secondsStill = 0f; // 정지 시간 초기화

        Sprite first = renderer != null ? ExplorationWorldArt.GetActorFrame(style, FacingIndex, CurrentFrameIndex) : null; // 사용할 첫 이미지 조회
        spriteRenderer = first != null ? renderer : null; // 실제 원화가 준비된 표시 대상만 연결
        if (spriteRenderer == null) // 기존 표시를 유지해야 하는 상태 확인
        { // 대체 표시 처리 시작
            return; // 원래 이미지와 색상 유지
        } // 대체 표시 처리 종료

        spriteRenderer.sprite = first; // 첫 캐릭터 자세 표시
        spriteRenderer.color = new Color(1f, 1f, 1f, spriteRenderer.color.a); // 기존 투명도를 유지한 원본 색상
    } // 캐릭터 원화 연결 종료

    internal static int DirectionFor(Vector2 displacement) // 실제 이동량에 가장 가까운 여덟 방향 분류
    { // 이동 방향 분류 시작
        if (displacement.sqrMagnitude == 0f) // 방향이 없는 위치 확인
        { // 기본 방향 처리 시작
            return 0; // 기본 남쪽 방향 번호
        } // 기본 방향 처리 종료

        float angle = Mathf.Atan2(displacement.x, -displacement.y) * Mathf.Rad2Deg; // 남쪽을 기준으로 계산한 이동 각도
        int direction = Mathf.RoundToInt(angle / (360f / ExplorationWorldArt.DirectionCount)); // 가장 가까운 원화 방향 번호
        return (direction + ExplorationWorldArt.DirectionCount) % ExplorationWorldArt.DirectionCount; // 음수 각도를 포함한 배열 번호 변환
    } // 이동 방향 분류 종료

    private void OnEnable() // 캐릭터의 표시를 다시 켤 때 이동 기록 동기화
    { // 활성화 위치 동기화 시작
        previousPosition = transform.position; // 비활성 상태의 위치 변경을 뺀 시작점
        stepRemainder = 0f; // 재활성화 전 누적 이동 거리 초기화
        secondsStill = 0f; // 재활성화 전 정지 시간 초기화
        ApplyPose(FacingIndex, 0); // 마지막 방향의 서 있는 자세
    } // 활성화 위치 동기화 종료

    private void LateUpdate() // 실제 물리·월드 이동 이후의 보행 원화 갱신
    { // 이동 이미지 갱신 시작
        if (spriteRenderer == null) // 원화가 표시 중인 캐릭터 확인
        { // 갱신할 이미지가 없는 경우 시작
            return; // 기존 이미지 표시 유지
        } // 갱신할 이미지가 없는 경우 종료

        Vector2 position = transform.position; // 현재 화면의 실제 월드 위치
        Vector2 displacement = position - previousPosition; // 직전 화면 대비 이동 벡터
        previousPosition = position; // 다음 화면에서 비교할 위치 기록
        float travelSquared = displacement.sqrMagnitude; // 현재 이동 거리의 제곱
        if (travelSquared >= TeleportDistanceSquared) // 보행 범위를 넘어선 위치 변경 확인
        { // 순간 이동 처리 시작
            stepRemainder = 0f; // 순간 이동 이전 보행 거리 초기화
            secondsStill = 0f; // 순간 이동 이전 정지 시간 초기화
            ApplyPose(FacingIndex, 0); // 이전 방향을 유지한 서 있는 자세
            return; // 순간 이동 방향과 거리의 보행 반영 제외
        } // 순간 이동 처리 종료

        if (travelSquared <= MinimumTravelSquared) // 벽 충돌이나 UI 입력 차단을 포함한 위치 정지 확인
        { // 관찰 위치 정지 처리 시작
            secondsStill += Time.unscaledDeltaTime; // 시간 배율과 무관한 실제 정지 시간 누적
            if (secondsStill >= IdleDelay) // 물리 갱신 사이의 일시적인 정지를 지난 시간 확인
            { // 서 있는 자세 처리 시작
                stepRemainder = 0f; // 다음 이동을 위한 누적 거리 초기화
                ApplyPose(FacingIndex, 0); // 마지막 방향의 서 있는 자세 표시
            } // 서 있는 자세 처리 종료

            return; // 움직임이 없을 때 보행 진행 생략
        } // 관찰 위치 정지 처리 종료

        secondsStill = 0f; // 움직임을 확인한 화면의 정지 시간 초기화
        stepRemainder += Mathf.Sqrt(travelSquared); // 실제 이동 거리를 보행 주기에 누적
        int steps = Mathf.FloorToInt(stepRemainder / StepDistance); // 이동 거리에서 지나간 보행 자세 수
        stepRemainder -= steps * StepDistance; // 다음 화면까지 남겨둘 보행 거리
        int frame = (CurrentFrameIndex + steps) % ExplorationWorldArt.FramesPerDirection; // 두 보행 자세의 반복 순서
        ApplyPose(DirectionFor(displacement), frame); // 움직인 방향과 현재 보행 자세 표시
    } // 이동 이미지 갱신 종료

    private void ApplyPose(int direction, int frame) // 달라진 방향 또는 자세를 렌더러에 적용
    { // 캐릭터 자세 적용 시작
        if (spriteRenderer == null || (direction == FacingIndex && frame == CurrentFrameIndex)) // 변경할 렌더러와 새 자세 확인
        { // 같은 자세 처리 시작
            return; // 이미 표시 중인 이미지 유지
        } // 같은 자세 처리 종료

        Sprite sprite = ExplorationWorldArt.GetActorFrame(style, direction, frame); // 방향과 자세에 해당하는 공유 이미지
        if (sprite == null) // 표시할 자세 이미지 존재 여부 확인
        { // 누락 자세 처리 시작
            return; // 직전에 표시한 실제 자세 유지
        } // 누락 자세 처리 종료

        FacingIndex = direction; // 표시할 새 방향 번호 갱신
        CurrentFrameIndex = frame; // 표시할 새 자세 번호 갱신
        spriteRenderer.sprite = sprite; // 렌더러의 현재 이미지 교체
    } // 캐릭터 자세 적용 종료
} // 탐사 캐릭터 표시 종료
