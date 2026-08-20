using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_New", menuName = "Project C/Data/Enemy")]
public sealed class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string enemyId; // 적 고유 ID
    [SerializeField] private string displayName; // 적 표시 이름
    [TextArea(3, 6)]
    [SerializeField] private string description; // 적 설명
    [SerializeField] private Sprite portrait; // 적 표시 이미지

    [Header("기본 전투 수치")]
    [Min(1)]
    [SerializeField] private int maxHealth = 50; // 최대 체력

    public string EnemyId => enemyId; // 적 ID 조회
    public string DisplayName => displayName; // 적 이름 조회
    public string Description => description; // 적 설명 조회
    public Sprite Portrait => portrait; // 적 이미지 조회
    public int MaxHealth => maxHealth; // 최대 체력 조회
}
