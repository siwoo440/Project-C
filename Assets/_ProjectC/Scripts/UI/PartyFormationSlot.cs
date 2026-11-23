using UnityEngine; // 유니티 컴포넌트 사용
using UnityEngine.EventSystems; // 편성 칸 클릭 이벤트 사용
public sealed class PartyFormationSlot : MonoBehaviour, IPointerClickHandler // 파티 드롭과 선택 대상 슬롯 정보
{ // 슬롯 정보 시작
    public PartyFormationView Owner { get; private set; } // 소속 편성 화면 조회
    public int SlotIndex { get; private set; } // 왼쪽부터 슬롯 위치 조회
    public void Bind(PartyFormationView owner, int slotIndex) // 편성 화면과 슬롯 위치 연결
    { // 슬롯 연결 시작
        Owner = owner; // 소속 편성 화면 저장
        SlotIndex = slotIndex; // 슬롯 위치 저장
    } // 슬롯 연결 종료
    public void OnPointerClick(PointerEventData eventData) // 편성 칸 클릭 처리
    { // 편성 칸 선택 시작
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) // 왼쪽 클릭 여부 확인
        { // 다른 버튼 처리 시작
            return; // 캐릭터 선택 중단
        } // 다른 버튼 처리 종료
        Owner?.SelectSlot(SlotIndex); // 배치된 캐릭터 상세 정보 선택
    } // 편성 칸 선택 종료
} // 슬롯 정보 종료
