using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.EventSystems; // 프로필 드래그 입력 사용
using UnityEngine.UI; // 드래그 초상화 이미지 사용
public sealed class PartyProfileDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler // 편성 프로필 드래그 입력
{ // 프로필 드래그 시작
    private PartyFormationView formation; // 소속 편성 화면
    private CharacterData character; // 드래그할 캐릭터
    private RectTransform canvasRect; // 드래그 초상화 기준 캔버스
    private GameObject ghost; // 마우스를 따라가는 초상화
    private bool dragging; // 현재 드래그 여부
    public void Bind(PartyFormationView owner, CharacterData target) // 편성 화면과 캐릭터 연결
    { // 드래그 연결 시작
        formation = owner; // 소속 편성 화면 저장
        character = target; // 캐릭터 원본 저장
    } // 드래그 연결 종료
    public void OnBeginDrag(PointerEventData eventData) // 프로필 드래그 시작
    { // 드래그 시작 처리 시작
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || formation == null || !formation.CanDragProfile(character)) // 드래그 가능 조건 확인
        { // 드래그 불가 처리 시작
            return; // 드래그 시작 중단
        } // 드래그 불가 처리 종료
        Canvas canvas = formation.GetComponentInParent<Canvas>(); // 편성 화면 캔버스 조회
        if (canvas == null) // 캔버스 존재 확인
        { // 캔버스 없음 처리 시작
            return; // 드래그 시작 중단
        } // 캔버스 없음 처리 종료
        canvasRect = canvas.transform as RectTransform; // 드래그 기준 캔버스 저장
        ghost = new GameObject("PartyProfileDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup)); // 드래그 초상화 생성
        ghost.transform.SetParent(canvas.transform, false); // 캔버스 자식 배치
        ghost.transform.SetAsLastSibling(); // 드래그 초상화 최상단 배치
        RectTransform rect = ghost.GetComponent<RectTransform>(); // 드래그 초상화 위치 조회
        rect.sizeDelta = new Vector2(150f, 150f); // 드래그 초상화 크기 설정
        Image image = ghost.GetComponent<Image>(); // 드래그 초상화 이미지 조회
        image.sprite = character.Portrait; // 실제 캐릭터 초상화 적용
        image.preserveAspect = true; // 초상화 비율 유지
        image.raycastTarget = false; // 슬롯 입력 차단 해제
        CanvasGroup group = ghost.GetComponent<CanvasGroup>(); // 드래그 입력 그룹 조회
        group.alpha = 0.85f; // 드래그 초상화 투명도 설정
        group.blocksRaycasts = false; // 슬롯 포인터 적중 허용
        group.interactable = false; // 드래그 초상화 직접 입력 차단
        dragging = true; // 드래그 진행 상태 저장
        MoveGhost(eventData); // 초기 포인터 위치 적용
    } // 드래그 시작 처리 종료
    public void OnDrag(PointerEventData eventData) // 프로필 드래그 이동
    { // 드래그 이동 시작
        if (dragging && eventData != null) // 유효 드래그와 포인터 확인
        { // 드래그 이동 처리 시작
            MoveGhost(eventData); // 포인터 위치로 초상화 이동
        } // 드래그 이동 처리 종료
    } // 드래그 이동 종료
    public void OnEndDrag(PointerEventData eventData) // 프로필 드래그 종료
    { // 드래그 종료 시작
        if (!dragging) // 드래그 진행 여부 확인
        { // 드래그 없음 처리 시작
            return; // 종료 처리 중단
        } // 드래그 없음 처리 종료
        GameObject target = eventData == null ? null : eventData.pointerCurrentRaycast.gameObject; // 현재 포인터 아래 객체 조회
        PartyFormationSlot slot = target == null ? null : target.GetComponentInParent<PartyFormationSlot>(); // 포인터 아래 편성 슬롯 조회
        if (slot != null && slot.Owner == formation) // 같은 편성 화면의 슬롯 확인
        { // 슬롯 드롭 처리 시작
            formation.DropProfile(character, slot.SlotIndex); // 지정 슬롯에 캐릭터 배치
        } // 슬롯 드롭 처리 종료
        ClearGhost(); // 드래그 초상화 정리
    } // 드래그 종료 종료
    private void MoveGhost(PointerEventData eventData) // 드래그 초상화 위치 갱신
    { // 초상화 위치 갱신 시작
        if (ghost != null && canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 position)) // 캔버스 기준 포인터 위치 계산
        { // 초상화 위치 적용 시작
            ghost.GetComponent<RectTransform>().anchoredPosition = position; // 포인터 위치로 초상화 이동
        } // 초상화 위치 적용 종료
    } // 초상화 위치 갱신 종료
    private void ClearGhost() // 드래그 초상화 정리
    { // 초상화 정리 시작
        dragging = false; // 드래그 진행 상태 해제
        if (ghost != null) // 드래그 초상화 존재 확인
        { // 초상화 제거 시작
            ghost.SetActive(false); // 드래그 초상화 즉시 숨김
            Destroy(ghost); // 드래그 초상화 객체 제거
            ghost = null; // 초상화 참조 제거
        } // 초상화 제거 종료
    } // 초상화 정리 종료
    private void OnDisable() // 프로필 화면 비활성화 처리
    { // 비활성화 처리 시작
        ClearGhost(); // 남은 드래그 초상화 제거
    } // 비활성화 처리 종료
} // 프로필 드래그 종료
