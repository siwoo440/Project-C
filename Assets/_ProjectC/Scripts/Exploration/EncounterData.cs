using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Encounter_New", menuName = "Project C/Exploration/Encounter")]
public sealed class EncounterData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string encounterId;
    [SerializeField] private string displayName;
    [SerializeField] private BattleType battleType = BattleType.Normal;

    [Header("탐사 배치")]
    [SerializeField] private Vector2 explorationPosition;

    [Header("전투 적")]
    [SerializeField] private List<EnemyData> enemies = new List<EnemyData>();

    [Header("클리어 보상")]
    [Min(0)]
    [SerializeField] private int characterExperienceReward = 10;
    [Min(0)]
    [SerializeField] private int goldReward = 50;
    [Min(0)]
    [SerializeField] private int screwReward = 25;
    [Min(0)]
    [SerializeField] private int ironPlateReward = 20;
    [Min(0)]
    [SerializeField] private int wireReward = 15;

    public string EncounterId => encounterId;
    public string DisplayName => displayName;
    public BattleType BattleType => battleType;
    public Vector2 ExplorationPosition => explorationPosition;
    public IReadOnlyList<EnemyData> Enemies => enemies;

    public int CharacterExperienceReward =>
        Mathf.Max(0, characterExperienceReward);

    public int GoldReward =>
        Mathf.Max(0, goldReward);

    public int ScrewReward =>
        Mathf.Max(0, screwReward);

    public int IronPlateReward =>
        Mathf.Max(0, ironPlateReward);

    public int WireReward =>
        Mathf.Max(0, wireReward);

    public bool IsValidData()
    {
        if (string.IsNullOrWhiteSpace(encounterId) ||
            enemies == null ||
            enemies.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < enemies.Count; index++)
        {
            if (enemies[index] == null)
            {
                return false;
            }
        }

        return true;
    }
}
