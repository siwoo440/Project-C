using System; // 덱 변경 이벤트 사용
using System.Collections.Generic; // 런타임 카드 목록 사용
using UnityEngine; // 영구 관리자 오브젝트 사용

public sealed class RunDeckManager : MonoBehaviour // 탐사 회차 덱 관리자
{
    private static RunDeckManager instance; // 현재 회차 덱 관리자
    private readonly List<RunDeckCardEntry> cards = new List<RunDeckCardEntry>(); // 현재 보유 카드 목록
    private DeckData sourceDeck; // 초기 원본 덱

    public static RunDeckManager Instance => instance; // 현재 관리자 조회
    public IReadOnlyList<RunDeckCardEntry> Cards => cards; // 현재 카드 목록 조회
    public int CardCount => cards.Count; // 현재 카드 수 조회
    public event Action Changed; // 보유 덱 변경 이벤트

    public static RunDeckManager EnsureInstance() // 회차 덱 관리자 준비
    {
        if (instance != null) // 기존 관리자 확인
        {
            return instance; // 기존 관리자 반환
        }

        RunDeckManager existingManager = FindFirstObjectByType<RunDeckManager>(); // Scene 기존 관리자 탐색
        if (existingManager != null) // 기존 Scene 관리자 확인
        {
            instance = existingManager; // 기존 관리자 저장
            return instance; // 기존 Scene 관리자 반환
        }

        GameObject managerObject = new GameObject("RunDeckManager"); // 회차 덱 관리자 오브젝트 생성
        instance = managerObject.AddComponent<RunDeckManager>(); // 회차 덱 관리자 컴포넌트 추가
        return instance; // 새 관리자 반환
    }

    private void Awake() // 회차 덱 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Destroy(gameObject); // 중복 관리자 제거
            return; // 중복 초기화 중단
        }

        instance = this; // 현재 관리자 저장
        DontDestroyOnLoad(gameObject); // Scene 전환 상태 유지
    }

    public IReadOnlyList<RunDeckCardEntry> GetActiveCards(DeckData deckData) // 현재 전투용 카드 목록 조회
    {
        EnsureInitialized(deckData); // 원본 덱 기반 초기화 보장

        if (!BattleCombatRosterRuntime.ShouldFilterCurrentScene)
        {
            return cards; // 탐사·상점에서는 회차 카드 전체 반환
        }

        return BattleCombatRosterBuilder.FilterDeployableRunDeckCards(cards); // 전투 Scene에서는 실제 출전 캐릭터 카드만 반환
    }

    public bool TryAddCard(CardData cardData, CharacterData ownerData) // 상점 카드 추가 시도
    {
        RunDeckCardEntry entry = new RunDeckCardEntry(cardData, ownerData); // 신규 회차 카드 생성
        if (!entry.IsValid() || sourceDeck == null || !ContainsOwner(ownerData)) // 카드와 소유자 유효성 확인
        {
            return false; // 카드 추가 실패 반환
        }

        cards.Add(entry); // 보유 카드 마지막에 추가
        Changed?.Invoke(); // 덱 변경 알림
        return true; // 카드 추가 성공 반환
    }

    public bool CanRemoveAt(int cardIndex) // 지정 카드 제거 가능 여부
    {
        return cardIndex >= 0 && cardIndex < cards.Count && cards.Count > 1; // 유효 위치와 최소 한 장 유지 반환
    }

    public bool TryRemoveAt(int cardIndex) // 지정 카드 제거 시도
    {
        if (!CanRemoveAt(cardIndex)) // 제거 가능 여부 확인
        {
            return false; // 카드 제거 실패 반환
        }

        cards.RemoveAt(cardIndex); // 지정 보유 카드 제거
        Changed?.Invoke(); // 덱 변경 알림
        return true; // 카드 제거 성공 반환
    }

    public bool CanUpgradeAt(int cardIndex) // 지정 카드 강화 가능 여부
    {
        return cardIndex >= 0 &&
               cardIndex < cards.Count &&
               cards[cardIndex] != null &&
               cards[cardIndex].CanUpgrade; // 유효 위치와 미강화 카드 여부 반환
    }

    public bool TryUpgradeAt(int cardIndex) // 지정 카드 1회 강화 시도
    {
        if (!CanUpgradeAt(cardIndex)) // 강화 가능 여부 확인
        {
            return false; // 카드 강화 실패 반환
        }

        if (!cards[cardIndex].TryUpgrade()) // 회차 카드 강화 적용 확인
        {
            return false; // 카드 강화 실패 반환
        }

        Changed?.Invoke(); // 덱 변경 알림
        return true; // 카드 강화 성공 반환
    }

    public bool ContainsOwner(CharacterData ownerData) // 출전 카드 소유자 포함 여부
    {
        if (ownerData == null) // 빈 소유자 확인
        {
            return false; // 소유자 없음 반환
        }

        for (int index = 0; index < cards.Count; index++) // 현재 카드 순회
        {
            if (cards[index] != null && cards[index].Owner == ownerData) // 동일 소유자 확인
            {
                return true; // 소유자 포함 반환
            }
        }

        return false; // 소유자 없음 반환
    }

    public void ResetToSource(DeckData deckData) // 회차 덱 원본 상태 복원
    {
        cards.Clear(); // 기존 회차 카드 제거
        sourceDeck = deckData; // 새 원본 덱 저장

        if (sourceDeck != null) // 원본 덱 존재 확인
        {
            foreach (DeckCardEntry entry in sourceDeck.Cards) // 원본 카드 항목 순회
            {
                if (entry != null && entry.IsValid()) // 정상 원본 카드 확인
                {
                    cards.Add(new RunDeckCardEntry(entry.Card, entry.Owner)); // 회차 카드 복사 추가
                }
            }
        }

        Changed?.Invoke(); // 덱 초기화 알림
    }

    private void EnsureInitialized(DeckData deckData) // 원본 덱 기반 초기화 보장
    {
        if (sourceDeck == deckData && cards.Count > 0) // 동일 원본 초기화 완료 확인
        {
            return; // 재초기화 중단
        }

        ResetToSource(deckData); // 새 원본 덱 복사
    }

    private void OnDestroy() // 회차 덱 관리자 제거 처리
    {
        if (instance == this) // 현재 관리자 여부 확인
        {
            instance = null; // 정적 관리자 참조 해제
        }
    }
}
