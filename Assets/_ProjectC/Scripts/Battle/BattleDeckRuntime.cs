using System.Collections.Generic; // List 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleDeckRuntime : MonoBehaviour // 전투 덱 런타임 관리자
{
    [Header("원본 덱")] // 원본 덱 구분
    [SerializeField] private List<CardData> originalCards = new List<CardData>(); // 원본 카드 목록

    [Header("전투용 덱")] // 런타임 덱 구분
    [SerializeField] private List<CardInstance> runtimeCards = new List<CardInstance>(); // 전투 카드 목록

    public IReadOnlyList<CardInstance> RuntimeCards => runtimeCards; // 외부 읽기 전용 목록 반환
    public int RuntimeCardCount => runtimeCards.Count; // 전투 카드 수 반환

    private void Start() // 전투 덱 초기화
    {
        BuildRuntimeDeck(); // 원본 덱 기반 인스턴스 생성
    }

    [ContextMenu("Rebuild Runtime Deck")] // Inspector 테스트 메뉴
    public void BuildRuntimeDeck() // 전투 덱 생성
    {
        runtimeCards.Clear(); // 기존 전투 카드 제거

        if (originalCards.Count == 0) // 원본 덱 비어 있음 확인
        {
            Debug.LogWarning("Original deck is empty.", this); // 빈 덱 경고
            return; // 덱 생성 중단
        }

        for (int index = 0; index < originalCards.Count; index++) // 원본 카드 순회
        {
            CardData originalCard = originalCards[index]; // 현재 원본 카드 가져오기

            if (originalCard == null) // 원본 카드 누락 확인
            {
                Debug.LogWarning($"Original card at index {index} is missing.", this); // 누락 위치 경고
                continue; // 다음 카드 처리
            }

            CardInstance runtimeCard = originalCard.CreateRuntimeInstance(); // 전투 카드 생성
            runtimeCards.Add(runtimeCard); // 전투 덱에 카드 추가
        }

        LogRuntimeDeck(); // 생성 결과 출력
    }

    [ContextMenu("Set First Card Cost To Zero")] // 비용 분리 테스트 메뉴
    private void SetFirstCardCostToZero() // 첫 카드 비용 테스트 변경
    {
        if (runtimeCards.Count == 0) // 전투 덱 비어 있음 확인
        {
            Debug.LogWarning("Runtime deck is empty.", this); // 빈 덱 경고
            return; // 변경 중단
        }

        runtimeCards[0].SetTemporaryCost(0); // 첫 카드 비용 0 설정
        Debug.Log($"First runtime card cost changed: {runtimeCards[0].CurrentCost}", this); // 변경 결과 출력
    }

    private void LogRuntimeDeck() // 전투 덱 생성 결과 출력
    {
        Debug.Log($"Runtime deck created: {runtimeCards.Count} cards.", this); // 전체 카드 수 출력

        for (int index = 0; index < runtimeCards.Count; index++) // 전투 카드 순회
        {
            CardInstance card = runtimeCards[index]; // 현재 전투 카드 가져오기
            Debug.Log($"[{index}] {card.CardName} / {card.InstanceId} / Cost {card.CurrentCost}", this); // 카드 정보 출력
        }
    }
}