using UnityEngine; // 플레이어 이동과 물리 기능 사용
using UnityEngine.InputSystem; // 키보드 입력 사용
using System; // 자동 위치 변경 알림 사용
using System.Collections.Generic; // 현재 닿은 상호작용 충돌체 관리

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ExplorationPlayerController : MonoBehaviour // 탐사 플레이어 이동 처리
{
    private const float MoveSpeed = 4f; // 기본 이동 속도

    private static bool inputBlocked; // 외부 UI 입력 차단 상태

    private Rigidbody2D body; // 플레이어 물리 몸체
    private SpriteRenderer actorArt; // 표시된 플레이어 원화의 화면 위치 기준
    private Vector2 moveInput; // 현재 이동 입력
    private readonly List<Collider2D> nearbyInteractables = new List<Collider2D>(); // 플레이어 주변의 현재 상호작용 충돌체
    private RectTransform interactionPrompt; // 캐릭터 오른쪽 F 키 안내 영역

    public static bool InputBlocked => inputBlocked; // 입력 차단 상태 조회
    public event Action<Vector2> Teleported; // 탐사 위치 자동 변경 알림

    public static void SetInputBlocked(bool isBlocked) // 입력 차단 상태 설정
    {
        inputBlocked = isBlocked; // 전역 입력 차단 상태 저장
    }

    private void Awake() // 플레이어 초기화
    {
        body = GetComponent<Rigidbody2D>(); // Rigidbody2D 조회
        actorArt = GetComponent<SpriteRenderer>(); // 플레이어의 현재 이동 원화 조회
        body.gravityScale = 0f; // 중력 제거
        body.freezeRotation = true; // 물리 회전 방지
    }

    private void Update() // 이동 입력 갱신
    {
        if (inputBlocked)
        {
            moveInput = Vector2.zero; // 입력 차단 시 이동 입력 제거
            SetInteractionPrompt(false); // 다른 탐사 창 위의 F 키 안내 숨김
            return;
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 조회
        UpdateInteraction(keyboard); // 실제 오브젝트 근접 안내와 F 키 처리

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

    internal void BindInteractionPrompt(RectTransform prompt) // 생성된 탐사 HUD의 안내 영역 연결
    { // 탐사 상호작용 안내 연결 시작
        interactionPrompt = prompt; // 현재 탐사 씬에서만 쓸 F 키 안내
        SetInteractionPrompt(false); // 오브젝트를 만나기 전 안내 숨김
    } // 탐사 상호작용 안내 연결 종료

    private void OnTriggerEnter2D(Collider2D other) // 접촉한 조작 가능 오브젝트 확인
    { // 탐사 오브젝트 접촉 처리 시작
        if (other.TryGetComponent<IExplorationInteractable>(out _) && !nearbyInteractables.Contains(other)) // 장식이나 적을 제외한 새 상호작용 충돌체 확인
        { // 새 상호작용 대상 보관 시작
            nearbyInteractables.Add(other); // 빠져나가기 전 F 키 후보에 등록
        } // 새 상호작용 대상 보관 종료
    } // 탐사 오브젝트 접촉 처리 종료

    private void OnTriggerExit2D(Collider2D other) // 상호작용 거리에서 벗어난 오브젝트 확인
    { // 탐사 오브젝트 이탈 처리 시작
        nearbyInteractables.Remove(other); // 이탈한 대상의 상호작용과 안내 제거
    } // 탐사 오브젝트 이탈 처리 종료

    private void OnDisable() // 탐사 씬 종료와 플레이어 비활성화 처리
    { // 탐사 상호작용 정리 시작
        nearbyInteractables.Clear(); // 종료한 맵의 이전 오브젝트 해제
        SetInteractionPrompt(false); // 플레이어가 사라진 뒤 안내 숨김
    } // 탐사 상호작용 정리 종료

    private void UpdateInteraction(Keyboard keyboard) // 가장 가까운 실제 대상과 한 번의 F 키 처리
    { // 이번 프레임의 상호작용 조회 시작
        IExplorationInteractable target = InteractionAvailable() ? FindNearbyInteractable() : null; // 열린 모달과 씬 전환 중 안내 및 조작 제한
        if (target != null && keyboard?.fKey.wasPressedThisFrame == true) // 새로 누른 실제 F 키와 유효 대상 확인
        { // 현재 상호작용 실행 시작
            target.Interact(this); // 선택된 오브젝트의 원래 동작 실행
            target = InteractionAvailable() ? FindNearbyInteractable() : null; // 실행으로 모달이 열리거나 대상이 사라진 뒤 상태 확인
        } // 현재 상호작용 실행 종료
        SetInteractionPrompt(target != null); // 선택할 대상이 있는 동안만 캐릭터 옆에 표시
    } // 이번 프레임의 상호작용 조회 종료

    private void SetInteractionPrompt(bool visible) // 현재 탐사 안내의 안전한 표시 갱신
    { // 안내 오브젝트 표시 변경 시작
        if (interactionPrompt != null && interactionPrompt.gameObject.activeSelf != visible) // 씬 종료 때의 제거 여부와 이전 표시 확인
        { // 가까운 오브젝트에 따른 안내 갱신 시작
            interactionPrompt.gameObject.SetActive(visible); // 바뀐 프레임에만 F 키 안내 표시
        } // 가까운 오브젝트에 따른 안내 갱신 종료
    } // 안내 오브젝트 표시 변경 종료

    internal void PlaceInteractionPrompt(Camera worldCamera) // 카메라 이동 뒤 캐릭터 오른쪽에 안내 배치
    { // 현재 표시 중인 안내 위치 갱신 시작
        if (interactionPrompt == null || !interactionPrompt.gameObject.activeSelf || worldCamera == null) // 보이지 않는 안내와 무효 카메라 확인
        { // 안내 위치 처리 생략 시작
            return; // 실제 표시 중인 안내만 월드 좌표 변환
        } // 안내 위치 처리 생략 종료
        Bounds actorBounds = actorArt != null ? actorArt.bounds : new Bounds(transform.position, Vector3.zero); // 현재 보여 주는 캐릭터 원화의 실제 크기
        Vector3 rightEdge = new Vector3(actorBounds.max.x, actorBounds.center.y, actorBounds.center.z); // 캐릭터 오른쪽 면의 가운데 월드 위치
        Vector2 screenPoint = worldCamera.WorldToScreenPoint(rightEdge); // 이동한 카메라로 계산한 현재 화면 픽셀
        RectTransform hud = (RectTransform)interactionPrompt.parent; // 탐사 오버레이 캔버스의 기준 영역
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, screenPoint, null, out Vector2 localPoint)) // 화면 픽셀을 캔버스 배율에 맞춰 변환
        { // 오른쪽 안내 배치 시작
            interactionPrompt.anchoredPosition = localPoint - hud.rect.center + new Vector2(12f, 0f); // 캐릭터 밖의 작은 간격과 수직 가운데 정렬
        } // 오른쪽 안내 배치 종료
    } // 현재 표시 중인 안내 위치 갱신 종료

    private static bool InteractionAvailable() // 탐사 상호작용을 받을 수 있는 화면 상태
    { // 일반 탐사 조작 상태 확인 시작
        return !inputBlocked && Time.timeScale > 0f && SceneFlowManager.Instance?.IsLoadingScene != true; // 이벤트 창·상점·휴식·씬 전환 중 F 키 차단
    } // 일반 탐사 조작 상태 확인 종료

    private IExplorationInteractable FindNearbyInteractable() // 실제로 닿은 오브젝트 중 가장 가까운 유효 대상
    { // 가까운 상호작용 조회 시작
        nearbyInteractables.RemoveAll(collider => collider == null || !collider.isActiveAndEnabled); // 사라졌거나 사용 완료된 오브젝트 정리
        IExplorationInteractable target = null; // 유효한 최근접 대상의 초기값
        float nearestDistance = float.PositiveInfinity; // 실제 오브젝트 간 위치 비교의 초기값
        foreach (Collider2D collider in nearbyInteractables) // 실제로 접촉 중인 조작 가능 후보 비교
        { // 상호작용 후보 한 개 비교 시작
            float distance = ((Vector2)collider.bounds.center - body.position).sqrMagnitude; // 플레이어와 오브젝트 중심 거리의 제곱
            if (distance < nearestDistance && collider.TryGetComponent<IExplorationInteractable>(out var candidate) && candidate.CanInteract) // 현재 사용 가능한 가장 가까운 후보 확인
            { // 가까운 대상 저장 시작
                nearestDistance = distance; // 이후 후보의 비교 기준 저장
                target = candidate; // 현재 안내와 F 키의 실제 대상 저장
            } // 가까운 대상 저장 종료
        } // 상호작용 후보 한 개 비교 종료
        return target; // 실제 안내와 상호작용에 넘길 대상 또는 없음
    } // 가까운 상호작용 조회 종료

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
        Teleported?.Invoke(worldPosition); // 맵 자동 위치 변경 알림
    }
}
