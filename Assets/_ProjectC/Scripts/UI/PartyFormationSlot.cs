using UnityEngine; // 유니티 컴포넌트 사용
public sealed class PartyFormationSlot : MonoBehaviour // 파티 드롭 대상 슬롯 정보
{ // 슬롯 정보 시작
    public PartyFormationView Owner { get; private set; } // 소속 편성 화면 조회
    public int SlotIndex { get; private set; } // 왼쪽부터 슬롯 위치 조회
    public void Bind(PartyFormationView owner, int slotIndex) // 편성 화면과 슬롯 위치 연결
    { // 슬롯 연결 시작
        Owner = owner; // 소속 편성 화면 저장
        SlotIndex = slotIndex; // 슬롯 위치 저장
    } // 슬롯 연결 종료
} // 슬롯 정보 종료
