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

    public string EncounterId => encounterId;
    public string DisplayName => displayName;
    public BattleType BattleType => battleType;
    public Vector2 ExplorationPosition => explorationPosition;
    public IReadOnlyList<EnemyData> Enemies => enemies;

    public bool IsValidData()
    {
        if (string.IsNullOrWhiteSpace(encounterId) || enemies == null || enemies.Count == 0)
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
