using UnityEngine; // 플레이어 이동과 물리 기능 사용
using UnityEngine.InputSystem; // 키보드 입력 사용

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ExplorationPlayerController : MonoBehaviour // 탐사 플레이어 이동 처리
{
    private const float MoveSpeed = 4f; // 기본 이동 속도

    private static bool inputBlocked; // 외부 UI 입력 차단 상태

    private Rigidbody2D body; // 플레이어 물리 몸체
    private Vector2 moveInput; // 현재 이동 입력

    public static bool InputBlocked => inputBlocked; // 입력 차단 상태 조회

    public static void SetInputBlocked(bool isBlocked) // 입력 차단 상태 설정
    {
        inputBlocked = isBlocked; // 전역 입력 차단 상태 저장
    }

    private void Awake() // 플레이어 초기화
    {
        body = GetComponent<Rigidbody2D>(); // Rigidbody2D 조회
        body.gravityScale = 0f; // 중력 제거
        body.freezeRotation = true; // 물리 회전 방지
    }

    private void Update() // 이동 입력 갱신
    {
        if (inputBlocked)
        {
            moveInput = Vector2.zero; // 입력 차단 시 이동 입력 제거
            return;
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회

        if (keyboard == null)
        {
            moveInput = Vector2.zero; // 키보드 미연결 시 이동 중지
            return;
        }

        float horizontal = 0f; // 가로 입력 초기화
        float vertical = 0f; // 세로 입력 초기화

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f; // 왼쪽 이동 입력
        }

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f; // 오른쪽 이동 입력
        }

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f; // 아래 이동 입력
        }

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
        {
            vertical += 1f; // 위 이동 입력
        }

        moveInput =
            new Vector2(
                horizontal,
                vertical).normalized; // 대각선 속도 보정
    }

    private void FixedUpdate() // 물리 이동 적용
    {
        if (body == null)
        {
            return;
        }

        if (inputBlocked)
        {
            body.linearVelocity = Vector2.zero; // 입력 차단 시 이동 속도 제거
            return;
        }

        Vector2 nextPosition =
            body.position +
            moveInput *
            MoveSpeed *
            Time.fixedDeltaTime; // 다음 물리 위치 계산

        body.MovePosition(nextPosition); // Tilemap 전체 공간 자유 이동
    }

    public void Teleport(
        Vector2 worldPosition) // 탐사 층 이동용 순간 위치 변경
    {
        moveInput = Vector2.zero; // 기존 이동 입력 제거

        if (body != null)
        {
            body.position = worldPosition; // 물리 위치 변경
            body.linearVelocity = Vector2.zero; // 기존 이동 속도 제거
        }

        transform.position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0f); // Transform 위치 동기화
    }
}
