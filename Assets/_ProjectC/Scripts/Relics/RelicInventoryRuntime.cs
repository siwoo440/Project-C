using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록 자료형 사용

public sealed class RelicInventoryRuntime // 획득 순서 기반 유물 보관함
{
    private readonly List<RelicData> ownedRelics = new List<RelicData>(); // 현재 보유 유물 순서 목록
    private readonly RelicGoldRuntime goldRuntime; // 중복 변환 골드 지갑

    public IReadOnlyList<RelicData> OwnedRelics => ownedRelics; // 현재 보유 유물 목록 조회
    public int Count => ownedRelics.Count; // 현재 유물 개수 조회
    public event Action Changed; // 유물 목록 변경 이벤트
    public event Action<RelicData, int> RelicAcquired; // 신규 유물 획득 이벤트
    public event Action<RelicData, int> RelicRemoved; // 유물 제거 이벤트
    public event Action<RelicData, int> DuplicateConverted; // 중복 유물 골드 변환 이벤트

    public RelicInventoryRuntime(RelicGoldRuntime wallet) // 유물 보관함 생성
    {
        goldRuntime = wallet ?? throw new ArgumentNullException(nameof(wallet)); // 골드 지갑 저장
    }

    public RelicAcquireResult TryAcquire(RelicData relicData) // 유물 획득 시도
    {
        if (relicData == null || !relicData.IsValidData()) // 유물 데이터 유효성 확인
        {
            return RelicAcquireResult.Invalid; // 잘못된 획득 결과 반환
        }

        if (ContainsRelic(relicData.RelicId)) // 동일 유물 보유 여부 확인
        {
            int convertedGold = goldRuntime.AddGold(relicData.DuplicateGoldValue); // 중복 유물 골드 변환
            DuplicateConverted?.Invoke(relicData, convertedGold); // 중복 변환 결과 알림
            return RelicAcquireResult.DuplicateConverted; // 중복 변환 결과 반환
        }

        ownedRelics.Add(relicData); // 획득 순서 마지막에 유물 추가
        int orderNumber = ownedRelics.Count; // 일부터 시작하는 현재 획득 순서 계산
        RelicAcquired?.Invoke(relicData, orderNumber); // 신규 유물 획득 알림
        Changed?.Invoke(); // 유물 목록 변경 알림
        return RelicAcquireResult.Acquired; // 신규 획득 결과 반환
    }

    public bool TryRemove(string relicId) // ID 기준 유물 제거 시도
    {
        if (string.IsNullOrWhiteSpace(relicId)) // 제거 ID 유효성 확인
        {
            return false; // 제거 실패 반환
        }

        for (int index = 0; index < ownedRelics.Count; index++) // 보유 유물 순서 탐색
        {
            RelicData relicData = ownedRelics[index]; // 현재 순서 유물 조회
            if (relicData == null || !string.Equals(relicData.RelicId, relicId, StringComparison.Ordinal)) // 제거 대상 일치 여부 확인
            {
                continue; // 다음 유물 확인
            }

            int removedOrderNumber = index + 1; // 제거 전 획득 순서 계산
            ownedRelics.RemoveAt(index); // 현재 유물 목록에서 제거
            RelicRemoved?.Invoke(relicData, removedOrderNumber); // 유물 제거 결과 알림
            Changed?.Invoke(); // 순서 당김 포함 목록 변경 알림
            return true; // 제거 성공 반환
        }

        return false; // 일치 유물 없음 반환
    }

    public bool ContainsRelic(string relicId) // 동일 유물 보유 여부 확인
    {
        if (string.IsNullOrWhiteSpace(relicId)) // 조회 ID 유효성 확인
        {
            return false; // 보유하지 않음 반환
        }

        foreach (RelicData relicData in ownedRelics) // 보유 유물 순회
        {
            if (relicData != null && string.Equals(relicData.RelicId, relicId, StringComparison.Ordinal)) // ID 일치 여부 확인
            {
                return true; // 동일 유물 보유 반환
            }
        }

        return false; // 동일 유물 없음 반환
    }

    public int GetCurrentOrder(string relicId) // 현재 표시 순서 조회
    {
        if (string.IsNullOrWhiteSpace(relicId)) // 조회 ID 유효성 확인
        {
            return 0; // 순서 없음 반환
        }

        for (int index = 0; index < ownedRelics.Count; index++) // 보유 유물 순서 탐색
        {
            RelicData relicData = ownedRelics[index]; // 현재 유물 조회
            if (relicData != null && string.Equals(relicData.RelicId, relicId, StringComparison.Ordinal)) // ID 일치 여부 확인
            {
                return index + 1; // 현재 순서 반환
            }
        }

        return 0; // 순서 없음 반환
    }
}
