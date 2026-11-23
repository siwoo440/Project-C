using UnityEngine; // 아이템 칸 컴포넌트 사용
using UnityEngine.EventSystems; // 아이템 칸 마우스 입력 사용
public sealed class ExplorationInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // 탐사 아이템 칸 마우스 안내
{ // 아이템 칸 안내 시작
    private ExplorationInventoryView view; // 연결된 실제 아이템 화면
    private int slotIndex; // 실제 보관함 칸 순번
    public void Bind(ExplorationInventoryView target, int index) // 실제 아이템 화면과 칸 연결
    { // 아이템 칸 연결 시작
        view = target; // 실제 아이템 화면 저장
        slotIndex = index; // 실제 보관함 칸 순번 저장
    } // 아이템 칸 연결 종료
    public void OnPointerEnter(PointerEventData eventData) // 아이템 칸 마우스 진입
    { // 마우스 진입 처리 시작
        view?.ShowTooltip(slotIndex); // 실제 보유 아이템의 이름 안내 표시
    } // 마우스 진입 처리 종료
    public void OnPointerExit(PointerEventData eventData) // 아이템 칸 마우스 이탈
    { // 마우스 이탈 처리 시작
        view?.HideTooltip(slotIndex); // 현재 아이템의 이름 안내 숨김
    } // 마우스 이탈 처리 종료
} // 아이템 칸 안내 종료
