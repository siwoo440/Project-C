using UnityEngine;

[CreateAssetMenu(fileName = "Character_New", menuName = "Project C/Data/Character")]
public sealed class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string characterId; // 캐릭터 고유 ID
    [SerializeField] private string displayName; // 캐릭터 표시 이름
    [TextArea(3, 6)]
    [SerializeField] private string description; // 캐릭터 설명
    [SerializeField] private CharacterRole role; // 캐릭터 역할
    [SerializeField] private Sprite portrait; // 캐릭터 초상화

    [Header("기본 전투 수치")]
    [Min(1)]
    [SerializeField] private int maxHealth = 100; // 최대 체력
    [Range(0, 100)]
    [SerializeField] private int initialMental = 50; // 초기 정신력

    public string CharacterId => characterId; // 캐릭터 ID 조회
    public string DisplayName => displayName; // 캐릭터 이름 조회
    public string Description => description; // 캐릭터 설명 조회
    public CharacterRole Role => role; // 캐릭터 역할 조회
    public Sprite Portrait => portrait; // 캐릭터 초상화 조회
    public int MaxHealth => maxHealth; // 최대 체력 조회
    public int InitialMental => initialMental; // 초기 정신력 조회
}
