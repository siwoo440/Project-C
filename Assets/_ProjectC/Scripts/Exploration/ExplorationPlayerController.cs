using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ExplorationPlayerController : MonoBehaviour
{
    private const float MoveSpeed = 4f;

    private Rigidbody2D body;
    private Vector2 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        moveInput = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        Vector2 nextPosition = body.position + moveInput * MoveSpeed * Time.fixedDeltaTime;
        nextPosition.x = Mathf.Clamp(nextPosition.x, -4.35f, 4.35f);
        nextPosition.y = Mathf.Clamp(nextPosition.y, -4.35f, 4.35f);

        body.MovePosition(nextPosition);
    }

    public void Teleport(Vector2 worldPosition) // 탐사 층 이동용 순간 위치 변경
    {
        moveInput = Vector2.zero; // 기존 이동 입력 제거

        if (body != null) // 물리 몸체 존재 확인
        {
            body.position = worldPosition; // 물리 위치 변경
            body.linearVelocity = Vector2.zero; // 기존 이동 속도 제거
        }

        transform.position = new Vector3(
            worldPosition.x,
            worldPosition.y,
            0f); // Transform 위치 동기화
    }
}
