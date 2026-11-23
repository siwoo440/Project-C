using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLevelConfig", menuName = "Project C/Data/Player Level Config")]
public sealed class PlayerLevelConfig : ScriptableObject
{
    private static PlayerLevelConfig runtimeDefault;

    [Header("시작 레벨")]
    [Min(1)]
    [SerializeField] private int startingLevel = 1;

    [Header("경험치")]
    [Min(1)]
    [SerializeField] private int baseRequiredExperience = 5;

    [Min(0)]
    [SerializeField] private int additionalExperiencePerLevel = 3;

    [Min(0)]
    [SerializeField] private int cardUsedExperience = 1;

    [Header("마이너 카드 선택")]
    [Range(1, 5)]
    [SerializeField] private int choiceOptionCount = 3;

    public int StartingLevel => Mathf.Max(1, startingLevel);
    public int CardUsedExperience => Mathf.Max(0, cardUsedExperience);
    public int ChoiceOptionCount => Mathf.Clamp(choiceOptionCount, 1, 5);

    public int GetRequiredExperience(int currentLevel)
    {
        int safeLevel = Mathf.Max(StartingLevel, currentLevel);
        int levelOffset = safeLevel - StartingLevel;
        return Mathf.Max(1, baseRequiredExperience + additionalExperiencePerLevel * levelOffset);
    }

    public static PlayerLevelConfig GetRuntimeDefault()
    {
        if (runtimeDefault != null)
        {
            return runtimeDefault;
        }

        runtimeDefault = CreateInstance<PlayerLevelConfig>();
        runtimeDefault.name = "PLAYER_LEVEL_CONFIG_RUNTIME_DEFAULT";
        runtimeDefault.hideFlags = HideFlags.HideAndDontSave;

        runtimeDefault.startingLevel = 1;
        runtimeDefault.baseRequiredExperience = 5;
        runtimeDefault.additionalExperiencePerLevel = 3;
        runtimeDefault.cardUsedExperience = 1;
        runtimeDefault.choiceOptionCount = 3;

        return runtimeDefault;
    }
}
