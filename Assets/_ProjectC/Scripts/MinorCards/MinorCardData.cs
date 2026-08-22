using UnityEngine; // 유니티 데이터 기능 사용

[CreateAssetMenu(fileName = "MinorCard_", menuName = "Project C/Data/Minor Card")] // 마이너 카드 데이터 생성 메뉴
public sealed class MinorCardData : ScriptableObject // 전투 중 선택하는 마이너 카드 데이터
{
    [Header("기본 정보")]
    [SerializeField] private string minorCardId; // 마이너 카드 고유 ID
    [SerializeField] private string displayName; // 표시 이름
    [TextArea(2, 4)]
    [SerializeField] private string description; // 카드 설명
    [SerializeField] private Sprite icon; // 카드 아이콘

    [Header("전투 효과")]
    [SerializeField] private MinorCardTargetType targetType = MinorCardTargetType.AllAllies; // 효과 대상
    [SerializeField] private MinorCardEffectType effectType = MinorCardEffectType.None; // 효과 종류
    [Min(1)]
    [SerializeField] private int effectValue = 1; // 효과 수치

    public string MinorCardId => minorCardId; // 카드 ID 조회
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? minorCardId : displayName; // 표시 이름 조회
    public string Description => description; // 설명 조회
    public Sprite Icon => icon; // 아이콘 조회
    public MinorCardTargetType TargetType => targetType; // 대상 종류 조회
    public MinorCardEffectType EffectType => effectType; // 효과 종류 조회
    public int EffectValue => effectValue; // 효과 수치 조회

    public bool IsValidData() // 마이너 카드 데이터 유효성 검사
    {
        return !string.IsNullOrWhiteSpace(minorCardId)
            && targetType != MinorCardTargetType.None
            && effectType != MinorCardEffectType.None
            && effectValue > 0;
    }
}
