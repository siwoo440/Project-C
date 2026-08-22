using UnityEngine; // 유니티 기본 기능 사용

public abstract class ConsumableItemData : ScriptableObject // 공용 소모품 원본 데이터
{
    [Header("기본 정보")] // 기본 정보 구역
    [SerializeField] private string itemId; // 소모품 고유 ID
    [SerializeField] private string displayName; // 소모품 표시 이름
    [TextArea(2, 5)] // 여러 줄 설명 입력
    [SerializeField] private string description; // 소모품 설명
    [SerializeField] private Sprite icon; // 소모품 아이콘
    [SerializeField] private ConsumableItemCategory category = ConsumableItemCategory.Other; // 소모품 분류

    public string ItemId => itemId; // 소모품 ID 조회
    public string DisplayName => displayName; // 소모품 이름 조회
    public string Description => description; // 소모품 설명 조회
    public Sprite Icon => icon; // 소모품 아이콘 조회
    public ConsumableItemCategory Category => category; // 소모품 분류 조회

    public virtual bool IsValidData() // 기본 데이터 유효성 검사
    {
        return !string.IsNullOrWhiteSpace(itemId) && !string.IsNullOrWhiteSpace(displayName); // ID와 이름 유효성 반환
    }
}
