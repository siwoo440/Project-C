using System.Collections.Generic; // 상세창 레이캐스트 결과 목록 사용
using UnityEngine.EventSystems; // 상세창 포인터 이벤트 사용
using UnityEngine.UI; // 상세창 캔버스 입력 사용

internal sealed class BattleStatusEffectTooltipRaycaster : GraphicRaycaster, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler // 상태 상세의 마우스 입력과 원래 전투 조작 연결
{ // 상세 입력 처리 시작
    private BattleUnitView owner; // 상세창과 포인터 상태를 공유하는 원래 유닛

    internal void Bind(BattleUnitView unit) // 독립 상세 패널에 원래 유닛 연결
    { // 포인터 공유 대상 설정 시작
        owner = unit; // 상세창을 연 실제 전투 유닛 참조
    } // 포인터 공유 대상 설정 종료

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) // 상세창 포인터 입력 대상 탐색
    { // 포인터 입력 탐색 시작
        if (ShouldPassThrough(eventData)) // 기존 전투 카드 드래그 여부 확인
        { // 전투 대상 탐색 보존 시작
            return; // 상세창 아래의 원래 전투 화면으로 카드 드래그 전달
        } // 전투 대상 탐색 보존 종료

        base.Raycast(eventData, resultAppendList); // 일반 포인터와 스크롤바 입력 탐색 유지
    } // 포인터 입력 탐색 종료

    internal static bool ShouldPassThrough(PointerEventData eventData) // 상세창 아래로 전달할 전투 카드 조작 확인
    { // 상세창 입력 우회 판단 시작
        var dragged = eventData.pointerDrag; // 실제로 드래그하는 전투 입력 대상
        return eventData.dragging && dragged != null // 시작된 드래그와 대상 존재 확인
            && (dragged.TryGetComponent<BattleCardDragHandler>(out _) || dragged.TryGetComponent<BattleCardHitRegion>(out _)); // 원래 카드와 별도 손패 입력 영역의 드래그만 전달
    } // 상세창 입력 우회 판단 종료

    public void OnPointerClick(PointerEventData eventData) // 상세창 클릭의 유닛 선택 전파 차단
    { // 상세창에서 클릭 종료
    } // 상세 클릭 처리 종료

    public void OnPointerEnter(PointerEventData eventData) // 상세창이 포인터 입력을 이어받은 상태
    { // 유닛과 상세창 공용 표시 갱신 시작
        owner?.OnPointerEnter(eventData); // 원래 유닛의 상세 켜짐 상태 유지
    } // 유닛과 상세창 공용 표시 갱신 종료

    public void OnPointerExit(PointerEventData eventData) // 상세창 밖으로 포인터가 이동한 상태
    { // 원래 유닛이나 상세창의 다음 입력 위치 확인 시작
        owner?.OnPointerExit(eventData); // 유닛까지 벗어났을 때 상세창 닫기
    } // 원래 유닛이나 상세창의 다음 입력 위치 확인 종료
} // 상세 입력 처리 종료
