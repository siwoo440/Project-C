using UnityEngine; // 유니티 컴포넌트 사용
using UnityEngine.EventSystems; // 마우스 입력 이벤트 사용
public sealed class BattleCardHitRegion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler // 겹친 카드의 독립 입력 영역
{ // 입력 영역 시작
    private BattleCardView cardView; // 연결된 카드 화면
    public BattleCardView CardView => cardView; // 연결 카드 화면 조회
    public void Bind(BattleCardView view) // 카드 화면 연결
    { // 카드 연결 시작
        cardView = view; // 카드 화면 저장
    } // 카드 연결 종료
    public void OnPointerEnter(PointerEventData eventData) // 포인터 진입 처리
    { // 진입 처리 시작
        BattleCardDragHandler dragHandler = cardView == null ? null : cardView.GetComponent<BattleCardDragHandler>(); // 기존 카드 드래그 처리기 조회
        if (dragHandler != null) // 드래그 처리기 존재 확인
        { // 드래그 진입 처리 시작
            dragHandler.OnPointerEnter(eventData); // 기존 드래그 진입 처리 전달
            return; // 중복 진입 처리 방지
        } // 드래그 진입 처리 종료
        cardView?.OnPointerEnter(eventData); // 연결 카드의 진입 처리 전달
    } // 진입 처리 종료
    public void OnPointerExit(PointerEventData eventData) // 포인터 이탈 처리
    { // 이탈 처리 시작
        BattleCardDragHandler dragHandler = cardView == null ? null : cardView.GetComponent<BattleCardDragHandler>(); // 기존 카드 드래그 처리기 조회
        if (dragHandler != null) // 드래그 처리기 존재 확인
        { // 드래그 이탈 처리 시작
            dragHandler.OnPointerExit(eventData); // 기존 드래그 이탈 처리 전달
            return; // 중복 이탈 처리 방지
        } // 드래그 이탈 처리 종료
        cardView?.OnPointerExit(eventData); // 연결 카드의 이탈 처리 전달
    } // 이탈 처리 종료
    public void OnPointerClick(PointerEventData eventData) // 포인터 클릭 처리
    { // 클릭 처리 시작
        if (cardView != null && cardView.isActiveAndEnabled) // 기존 카드 클릭 입력 상태 확인
        { // 카드 클릭 전달 시작
            cardView.OnPointerClick(eventData); // 연결 카드의 클릭 처리 전달
        } // 카드 클릭 전달 종료
    } // 클릭 처리 종료
    public void OnBeginDrag(PointerEventData eventData) // 카드 드래그 시작 처리
    { // 드래그 시작 전달 시작
        cardView?.GetComponent<BattleCardDragHandler>()?.OnBeginDrag(eventData); // 기존 드래그 시작 처리 전달
    } // 드래그 시작 전달 종료
    public void OnDrag(PointerEventData eventData) // 카드 드래그 진행 처리
    { // 드래그 진행 전달 시작
        cardView?.GetComponent<BattleCardDragHandler>()?.OnDrag(eventData); // 기존 드래그 진행 처리 전달
    } // 드래그 진행 전달 종료
    public void OnEndDrag(PointerEventData eventData) // 카드 드래그 종료 처리
    { // 드래그 종료 전달 시작
        cardView?.GetComponent<BattleCardDragHandler>()?.OnEndDrag(eventData); // 기존 드래그 종료 처리 전달
    } // 드래그 종료 전달 종료
} // 입력 영역 종료
