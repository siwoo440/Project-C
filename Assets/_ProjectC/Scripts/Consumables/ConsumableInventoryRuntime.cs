using System; // 기본 이벤트 기능 사용

public sealed class ConsumableInventoryRuntime // 고정 슬롯 소모품 보관함
{
    public const int ColumnCount = 5; // 가로 슬롯 수
    public const int RowCount = 2; // 세로 슬롯 수
    public const int SlotCount = ColumnCount * RowCount; // 전체 슬롯 수

    public const int MaximumSlotCount = SlotCount + 1; // 포션 전용 추가칸을 포함한 최대 개수
    private readonly ConsumableItemData[] slots = new ConsumableItemData[MaximumSlotCount]; // 기본 열 칸과 추가 포션칸
    public int Capacity { get; private set; } = SlotCount; // 현재 사용할 수 있는 보관칸 수
    public bool CanRemovePotionRack => slots[SlotCount] == null || Array.IndexOf(slots, null, 0, SlotCount) >= 0; // 추가 포션을 보존하며 닫을 수 있는지 확인

    public bool SetPotionRack(bool owned) // 유물 소유에 맞춘 포션 전용칸 전환
    { // 추가칸 전환 시작
        int capacity = owned ? MaximumSlotCount : SlotCount; // 전환 후 슬롯 수
        if (Capacity == capacity) // 동일한 보유 상태 확인
        { // 전환 생략 시작
            return true; // 변경 없이 성공
        } // 전환 생략 종료
        if (!owned && !CanRemovePotionRack) // 가득 찬 기본칸과 점유된 추가칸 확인
        { // 판매 거절 시작
            return false; // 소모품 유실 방지
        } // 판매 거절 종료
        Capacity = capacity; // 변경 알림 전 슬롯 수 확정
        if (!owned && slots[SlotCount] != null) // 반납할 슬롯의 포션 확인
        { // 기본칸 이전 시작
            int empty = Array.IndexOf(slots, null, 0, SlotCount); // 가장 앞의 빈 기본칸
            slots[empty] = slots[SlotCount]; // 원래 포션 그대로 이전
            slots[SlotCount] = null; // 추가칸 비우기
            SlotChanged?.Invoke(empty); // 이전한 기본칸 알림
        } // 기본칸 이전 종료
        NotifySlotChanged(SlotCount); // 보관칸 수 변경 즉시 화면 반영
        return true; // 용량 전환 성공
    } // 추가칸 전환 종료

    public bool CanAcquire(ConsumableItemData item) // 실제 빈칸에 이 소모품이 들어갈 수 있는지 확인
    { // 획득 가능 검사 시작
        return item != null && Array.IndexOf(slots, null, 0, item is PotionData ? Capacity : SlotCount) >= 0; // 포션 전용칸을 구분한 빈칸 검색
    } // 획득 가능 검사 종료

    public int Count // 현재 소모품 개수 조회
    {
        get
        {
            int count = 0; // 보유 개수 초기화
            for (int index = 0; index < slots.Length; index++) // 전체 슬롯 순회
            {
                if (slots[index] != null) // 소모품 존재 확인
                {
                    count++; // 보유 개수 증가
                }
            }
            return count; // 전체 보유 개수 반환
        }
    }

    public event Action Changed; // 전체 슬롯 변경 이벤트
    public event Action<int> SlotChanged; // 개별 슬롯 변경 이벤트

    public ConsumableItemData GetItem(int slotIndex) // 지정 슬롯 소모품 조회
    {
        return IsValidSlot(slotIndex) ? slots[slotIndex] : null; // 슬롯 범위 포함 소모품 반환
    }

    public bool TryAcquire(ConsumableItemData itemData, out int acquiredSlotIndex) // 첫 빈칸 소모품 획득
    {
        acquiredSlotIndex = -1; // 획득 슬롯 초기화
        if (itemData == null || !itemData.IsValidData()) // 소모품 데이터 유효성 확인
        {
            return false; // 획득 실패 반환
        }

        for (int index = 0; index < (itemData is PotionData ? Capacity : SlotCount); index++) // 소모품 종류에 맞는 첫 빈칸 탐색
        {
            if (slots[index] != null) // 점유 슬롯 확인
            {
                continue; // 다음 슬롯 이동
            }

            slots[index] = itemData; // 빈 슬롯에 소모품 저장
            acquiredSlotIndex = index; // 획득 슬롯 저장
            NotifySlotChanged(index); // 슬롯 변경 알림
            return true; // 획득 성공 반환
        }

        return false; // 빈 슬롯 없음 반환
    }

    public bool TrySetForDebug(int slotIndex, ConsumableItemData itemData) // 테스트용 지정 슬롯 배치
    {
        if (!IsValidSlot(slotIndex) || (slotIndex == SlotCount && !(itemData is PotionData)) || itemData == null || !itemData.IsValidData()) // 슬롯과 데이터 유효성 확인
        {
            return false; // 지정 배치 실패 반환
        }

        if (slots[slotIndex] != null) // 기존 점유 여부 확인
        {
            return false; // 점유 슬롯 배치 실패 반환
        }

        slots[slotIndex] = itemData; // 지정 슬롯에 소모품 저장
        NotifySlotChanged(slotIndex); // 슬롯 변경 알림
        return true; // 지정 배치 성공 반환
    }

    public bool TryRemoveAt(int slotIndex) // 지정 슬롯 소모품 제거
    {
        if (!IsValidSlot(slotIndex) || slots[slotIndex] == null) // 슬롯과 소모품 존재 확인
        {
            return false; // 제거 실패 반환
        }

        slots[slotIndex] = null; // 지정 슬롯 비우기
        NotifySlotChanged(slotIndex); // 슬롯 변경 알림
        return true; // 제거 성공 반환
    }

    public bool TryMoveOrSwap(int sourceIndex, int targetIndex) // 슬롯 이동 또는 교환
    {
        if (!IsValidSlot(sourceIndex) || !IsValidSlot(targetIndex) || sourceIndex == targetIndex) // 슬롯 이동 범위 확인
        {
            return false; // 이동 실패 반환
        }

        if (slots[sourceIndex] == null || (targetIndex == SlotCount && !(slots[sourceIndex] is PotionData)) || (sourceIndex == SlotCount && slots[targetIndex] != null && !(slots[targetIndex] is PotionData))) // 포션 전용칸의 양방향 이동과 교환 검사
        {
            return false; // 빈 원본 이동 실패 반환
        }

        ConsumableItemData sourceItem = slots[sourceIndex]; // 원본 소모품 임시 저장
        slots[sourceIndex] = slots[targetIndex]; // 대상 소모품을 원본 슬롯으로 이동
        slots[targetIndex] = sourceItem; // 원본 소모품을 대상 슬롯으로 이동
        SlotChanged?.Invoke(sourceIndex); // 원본 슬롯 변경 알림
        SlotChanged?.Invoke(targetIndex); // 대상 슬롯 변경 알림
        Changed?.Invoke(); // 전체 슬롯 변경 알림
        return true; // 이동 또는 교환 성공 반환
    }

    public void Clear() // 전체 소모품 슬롯 초기화
    {
        bool changed = false; // 변경 여부 초기화
        for (int index = 0; index < slots.Length; index++) // 전체 슬롯 순회
        {
            if (slots[index] == null) // 빈 슬롯 확인
            {
                continue; // 다음 슬롯 이동
            }

            slots[index] = null; // 현재 슬롯 비우기
            SlotChanged?.Invoke(index); // 현재 슬롯 변경 알림
            changed = true; // 변경 상태 저장
        }

        if (changed) // 실제 변경 여부 확인
        {
            Changed?.Invoke(); // 전체 슬롯 변경 알림
        }
    }

    private bool IsValidSlot(int slotIndex) // 슬롯 범위 검사
    {
        return slotIndex >= 0 && slotIndex < Capacity; // 유효 슬롯 범위 반환
    }

    private void NotifySlotChanged(int slotIndex) // 단일 슬롯 변경 전달
    {
        SlotChanged?.Invoke(slotIndex); // 지정 슬롯 변경 알림
        Changed?.Invoke(); // 전체 슬롯 변경 알림
    }
}
