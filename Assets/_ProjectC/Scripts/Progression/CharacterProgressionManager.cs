using System;
using UnityEngine;

public sealed class CharacterProgressionManager : MonoBehaviour
{
    private const int StartingLevel = 1;
    private const int BaseRequiredExperience = 20;
    private const int AdditionalRequiredExperiencePerLevel = 10;

    private static CharacterProgressionManager instance;

    private bool initialized;

    public static CharacterProgressionManager Instance => instance;
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int RequiredExperience => CalculateRequiredExperience(Level);

    public event Action ProgressChanged;

    public static CharacterProgressionManager EnsureInstance()
    {
        if (instance != null)
        {
            instance.EnsureInitialized();
            return instance;
        }

        CharacterProgressionManager existingManager =
            FindFirstObjectByType<CharacterProgressionManager>();

        if (existingManager != null)
        {
            instance = existingManager;
            instance.EnsureInitialized();
            return instance;
        }

        GameObject managerObject =
            new GameObject("CharacterProgressionManager");

        instance =
            managerObject.AddComponent<CharacterProgressionManager>();

        instance.EnsureInitialized();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInitialized();
    }

    public int AddExperience(int amount)
    {
        EnsureInitialized();

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount == 0)
        {
            return 0;
        }

        CurrentExperience += safeAmount;

        while (CurrentExperience >= RequiredExperience)
        {
            CurrentExperience -= RequiredExperience;
            Level++;
        }

        ProgressChanged?.Invoke();
        return safeAmount;
    }

    public void ResetProgress()
    {
        Level = StartingLevel;
        CurrentExperience = 0;
        initialized = true;
        ProgressChanged?.Invoke();
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        Level = StartingLevel;
        CurrentExperience = 0;
        initialized = true;
    }

    private static int CalculateRequiredExperience(int level)
    {
        int safeLevel = Mathf.Max(StartingLevel, level);

        return BaseRequiredExperience +
               (safeLevel - StartingLevel) *
               AdditionalRequiredExperiencePerLevel;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
