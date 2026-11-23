using System;
using UnityEngine;

public sealed class CharacterAffinityManager : MonoBehaviour
{
    private const int StartingAffinity = 0;

    private static CharacterAffinityManager instance;

    public static CharacterAffinityManager Instance => instance;
    public int Affinity { get; private set; } = StartingAffinity;

    public event Action AffinityChanged;

    public static CharacterAffinityManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance =
            FindFirstObjectByType<CharacterAffinityManager>();

        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject =
            new GameObject(nameof(CharacterAffinityManager));

        instance =
            managerObject.AddComponent<CharacterAffinityManager>();

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
    }

    public void GrantExplorationSuccessAffinity(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (safeAmount == 0)
        {
            return;
        }

        Affinity += safeAmount;
        AffinityChanged?.Invoke();

        Debug.Log(
            $"[CharacterAffinity] 탐사 성공 호감도 +{safeAmount} / " +
            $"현재 호감도 {Affinity}");
    }

    public void ResetAffinity()
    {
        Affinity = StartingAffinity;
        AffinityChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
