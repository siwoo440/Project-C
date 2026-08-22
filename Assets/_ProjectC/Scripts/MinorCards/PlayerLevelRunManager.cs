using System;
using UnityEngine;

public sealed class PlayerLevelRunManager : MonoBehaviour
{
    private static PlayerLevelRunManager instance;
    private PlayerLevelConfig levelConfig;
    private bool initialized;

    public static PlayerLevelRunManager Instance => instance;
    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int PendingMinorCardChoices { get; private set; }
    public int RequiredExperience => levelConfig == null ? 0 : levelConfig.GetRequiredExperience(Level);

    public event Action ProgressChanged;
    public event Action<int> LevelUp;

    public static PlayerLevelRunManager EnsureInstance(PlayerLevelConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (instance == null)
        {
            instance = FindFirstObjectByType<PlayerLevelRunManager>();
        }

        if (instance == null)
        {
            GameObject managerObject = new GameObject("PlayerLevelRunManager");
            instance = managerObject.AddComponent<PlayerLevelRunManager>();
        }

        instance.Configure(config);
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
    }

    public void Configure(PlayerLevelConfig config)
    {
        if (config == null)
        {
            return;
        }

        levelConfig = config;
        if (!initialized)
        {
            ResetProgress();
        }
        else
        {
            ProgressChanged?.Invoke();
        }
    }

    public void BeginBattle()
    {
        ResetProgress();
    }

    public void EndBattle()
    {
        if (!initialized)
        {
            return;
        }

        PendingMinorCardChoices = 0;
        ProgressChanged?.Invoke();
    }

    public int GainExperience(int amount)
    {
        if (!initialized || levelConfig == null || amount <= 0)
        {
            return 0;
        }

        CurrentExperience += amount;
        int gainedLevels = 0;
        int requiredExperience = RequiredExperience;

        while (requiredExperience > 0 && CurrentExperience >= requiredExperience)
        {
            CurrentExperience -= requiredExperience;
            Level++;
            PendingMinorCardChoices++;
            gainedLevels++;
            LevelUp?.Invoke(Level);
            requiredExperience = RequiredExperience;
        }

        ProgressChanged?.Invoke();
        return gainedLevels;
    }

    public bool TryConsumeMinorCardChoice()
    {
        if (PendingMinorCardChoices <= 0)
        {
            return false;
        }

        PendingMinorCardChoices--;
        ProgressChanged?.Invoke();
        return true;
    }

    public void ResetProgress()
    {
        if (levelConfig == null)
        {
            return;
        }

        Level = levelConfig.StartingLevel;
        CurrentExperience = 0;
        PendingMinorCardChoices = 0;
        initialized = true;
        ProgressChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
