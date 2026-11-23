using System; // 기본 이벤트 기능 사용

public sealed class ConsumableInventoryRuntime // 고정 슬롯 소모품 보관함
{
    public const int ColumnCount = 5; // 가로 슬롯 수
    public const int RowCount = 2; // 세로 슬롯 수
    public const int SlotCount = ColumnCount * RowCount; // 전체 슬롯 수

    private readonly ConsumableItemData[] slots = new ConsumableItemData[SlotCount]; // 고정 소모품 슬롯

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

        for (int index = 0; index < slots.Length; index++) // 앞에서부터 빈 슬롯 탐색
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
        if (!IsValidSlot(slotIndex) || itemData == null || !itemData.IsValidData()) // 슬롯과 데이터 유효성 확인
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

        if (slots[sourceIndex] == null) // 원본 슬롯 소모품 확인
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

    private static bool IsValidSlot(int slotIndex) // 슬롯 범위 검사
    {
        return slotIndex >= 0 && slotIndex < SlotCount; // 유효 슬롯 범위 반환
    }

    private void NotifySlotChanged(int slotIndex) // 단일 슬롯 변경 전달
    {
        SlotChanged?.Invoke(slotIndex); // 지정 슬롯 변경 알림
        Changed?.Invoke(); // 전체 슬롯 변경 알림
    }
}
