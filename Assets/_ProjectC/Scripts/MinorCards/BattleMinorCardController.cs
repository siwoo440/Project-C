using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleMinorCardController : IDisposable
{
    private readonly BattleSceneSetup battleSceneSetup;
    private readonly PlayerLevelRunManager levelManager;
    private readonly PlayerLevelConfig levelConfig;
    private readonly List<MinorCardData> cardPool = new List<MinorCardData>();
    private readonly List<MinorCardData> currentChoices = new List<MinorCardData>();
    private readonly List<MinorCardData> selectedCards = new List<MinorCardData>();
    private readonly HashSet<string> selectedCardIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<IDisposable> subscriptions = new List<IDisposable>();
    private bool selectionActive;
    private bool battleEnded;
    private bool disposed;

    public IReadOnlyList<MinorCardData> CurrentChoices => currentChoices;
    public IReadOnlyList<MinorCardData> SelectedCards => selectedCards;
    public bool SelectionActive => selectionActive;

    public event Action StateChanged;

    public BattleMinorCardController(
        BattleSceneSetup sceneSetup,
        PlayerLevelRunManager playerLevelManager,
        PlayerLevelConfig config,
        IReadOnlyList<MinorCardData> minorCardPool)
    {
        battleSceneSetup = sceneSetup ?? throw new ArgumentNullException(nameof(sceneSetup));
        levelManager = playerLevelManager ?? throw new ArgumentNullException(nameof(playerLevelManager));
        levelConfig = config ?? throw new ArgumentNullException(nameof(config));

        if (minorCardPool != null)
        {
            for (int index = 0; index < minorCardPool.Count; index++)
            {
                MinorCardData cardData = minorCardPool[index];
                if (cardData != null && cardData.IsValidData())
                {
                    cardPool.Add(cardData);
                }
            }
        }

        BattleEventDispatcher dispatcher = battleSceneSetup.BattleEvents ??
                                           throw new InvalidOperationException("전투 이벤트 발행기가 초기화되지 않았습니다.");

        subscriptions.Add(dispatcher.Subscribe(BattleEventType.CardUsed, HandleCardUsed));
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.TurnStarted, HandleTurnStarted));
        subscriptions.Add(dispatcher.Subscribe(BattleEventType.BattleEnded, HandleBattleEnded));
    }

    public void ProcessCurrentTurn()
    {
        if (disposed || battleSceneSetup.BattleTurn == null)
        {
            return;
        }

        if (battleSceneSetup.BattleTurn.CurrentPhase == BattleTurnPhase.PlayerTurn)
        {
            TryPrepareSelection();
        }
    }

    public bool TrySelectCard(MinorCardData cardData)
    {
        if (disposed || battleEnded || !selectionActive || cardData == null || !currentChoices.Contains(cardData))
        {
            return false;
        }

        ApplyCardEffect(cardData);
        selectedCards.Add(cardData);
        selectedCardIds.Add(cardData.MinorCardId);
        levelManager.TryConsumeMinorCardChoice();

        Debug.Log(
            $"[MinorCard] 선택 - {cardData.DisplayName} / " +
            $"{cardData.TargetType} / {cardData.EffectType} {cardData.EffectValue}");

        selectionActive = false;
        currentChoices.Clear();
        TryPrepareSelection(false);
        StateChanged?.Invoke();
        return true;
    }

    private void HandleCardUsed(BattleEventContext eventContext)
    {
        if (disposed || battleEnded || eventContext == null || eventContext.Card == null ||
            eventContext.Phase != BattleTurnPhase.PlayerTurn)
        {
            return;
        }

        int experience = levelConfig.CardUsedExperience;
        if (experience <= 0)
        {
            return;
        }

        int gainedLevels = levelManager.GainExperience(experience);
        Debug.Log(
            $"[PlayerLevel] 카드 사용 EXP +{experience} / {eventContext.Card.DisplayName} / " +
            $"Lv.{levelManager.Level} EXP {levelManager.CurrentExperience}/{levelManager.RequiredExperience}");

        if (gainedLevels > 0)
        {
            Debug.Log(
                $"[PlayerLevel] 레벨업 {gainedLevels}회 / " +
                $"대기 마이너 카드 선택 {levelManager.PendingMinorCardChoices}회");
        }
    }

    private void HandleTurnStarted(BattleEventContext eventContext)
    {
        if (disposed || battleEnded || eventContext == null || eventContext.Phase != BattleTurnPhase.PlayerTurn)
        {
            return;
        }

        TryPrepareSelection();
    }

    private void HandleBattleEnded(BattleEventContext eventContext)
    {
        if (disposed || battleEnded)
        {
            return;
        }

        battleEnded = true;
        BattleMinorCardEffectRegistry.ClearBattleEffects();
        levelManager.EndBattle();
        selectionActive = false;
        currentChoices.Clear();
        selectedCards.Clear();
        selectedCardIds.Clear();
        StateChanged?.Invoke();

        Debug.Log("[MinorCard] 전투 종료 - 마이너 카드 효과와 선택 기록 정리 완료");
    }

    private void TryPrepareSelection(bool notify = true)
    {
        if (disposed || battleEnded || selectionActive || levelManager.PendingMinorCardChoices <= 0)
        {
            return;
        }

        BattleTurnRuntime battleTurn = battleSceneSetup.BattleTurn;
        if (battleTurn == null || battleTurn.IsBattleEnded || battleTurn.CurrentPhase != BattleTurnPhase.PlayerTurn)
        {
            return;
        }

        List<MinorCardData> eligibleCards = new List<MinorCardData>();
        for (int index = 0; index < cardPool.Count; index++)
        {
            MinorCardData cardData = cardPool[index];
            if (cardData != null && !selectedCardIds.Contains(cardData.MinorCardId))
            {
                eligibleCards.Add(cardData);
            }
        }

        if (eligibleCards.Count == 0)
        {
            while (levelManager.PendingMinorCardChoices > 0)
            {
                levelManager.TryConsumeMinorCardChoice();
            }

            Debug.LogWarning("[MinorCard] 현재 전투에서 더 이상 선택할 수 있는 마이너 카드가 없습니다.");
            selectionActive = false;
            currentChoices.Clear();

            if (notify)
            {
                StateChanged?.Invoke();
            }

            return;
        }

        currentChoices.Clear();
        int optionCount = Mathf.Min(levelConfig.ChoiceOptionCount, eligibleCards.Count);

        for (int index = 0; index < optionCount; index++)
        {
            int randomIndex = UnityEngine.Random.Range(0, eligibleCards.Count);
            currentChoices.Add(eligibleCards[randomIndex]);
            eligibleCards.RemoveAt(randomIndex);
        }

        selectionActive = currentChoices.Count > 0;

        if (notify)
        {
            StateChanged?.Invoke();
        }
    }

    private void ApplyCardEffect(MinorCardData cardData)
    {
        IReadOnlyList<BattleUnitRuntime> targets = cardData.TargetType == MinorCardTargetType.AllAllies
            ? battleSceneSetup.AllyUnits
            : battleSceneSetup.EnemyUnits;

        int appliedCount = 0;
        for (int index = 0; index < targets.Count; index++)
        {
            BattleUnitRuntime targetUnit = targets[index];
            if (targetUnit == null || targetUnit.IsDead)
            {
                continue;
            }

            if (BattleMinorCardEffectRegistry.Apply(cardData, targetUnit))
            {
                appliedCount++;
            }
        }

        Debug.Log($"[MinorCard] {cardData.DisplayName} 효과 적용 대상 {appliedCount}명");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        for (int index = 0; index < subscriptions.Count; index++)
        {
            subscriptions[index]?.Dispose();
        }

        subscriptions.Clear();

        if (!battleEnded)
        {
            BattleMinorCardEffectRegistry.AbandonBattleTracking();
        }

        currentChoices.Clear();
        selectedCards.Clear();
        selectedCardIds.Clear();
        selectionActive = false;
    }
}
