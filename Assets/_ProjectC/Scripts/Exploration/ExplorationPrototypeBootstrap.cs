using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-400)]
public sealed class ExplorationPrototypeBootstrap : MonoBehaviour
{
    private static Sprite runtimeSquareSprite;

    private readonly List<ExplorationEncounterView> encounterViews =
        new List<ExplorationEncounterView>();

    private ExplorationPlayerController playerController;

    private void Start()
    {
        ExplorationSessionManager sessionManager =
            ExplorationSessionManager.EnsureInstance();

        CharacterProgressionManager.EnsureInstance();
        PlayerResourceManager.EnsureInstance();

        EnsureRuntimeSquareSprite();
        CreatePlayer(sessionManager);
        CreateEncounters(sessionManager);
        CreateHud(sessionManager);
    }

    private void CreatePlayer(
        ExplorationSessionManager sessionManager)
    {
        GameObject playerObject = new GameObject(
            "ExplorationPlayer",
            typeof(SpriteRenderer),
            typeof(Rigidbody2D),
            typeof(BoxCollider2D),
            typeof(ExplorationPlayerController));

        playerObject.transform.position =
            sessionManager.GetPlayerSpawnPosition(
                new Vector3(-3.5f, -2.7f, 0f));

        SpriteRenderer spriteRenderer =
            playerObject.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = runtimeSquareSprite;
        spriteRenderer.color =
            new Color(0.2f, 0.7f, 1f, 1f);
        spriteRenderer.sortingOrder = 5;

        playerObject.transform.localScale =
            new Vector3(0.55f, 0.55f, 1f);

        Rigidbody2D body =
            playerObject.GetComponent<Rigidbody2D>();

        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D collider =
            playerObject.GetComponent<BoxCollider2D>();

        collider.size = Vector2.one;

        playerController =
            playerObject.GetComponent<ExplorationPlayerController>();
    }

    private void CreateEncounters(
        ExplorationSessionManager sessionManager)
    {
        EncounterData[] encounterData =
            Resources.LoadAll<EncounterData>("Encounters");

        Array.Sort(
            encounterData,
            (left, right) =>
                string.CompareOrdinal(
                    left.EncounterId,
                    right.EncounterId));

        for (int index = 0;
             index < encounterData.Length;
             index++)
        {
            EncounterData data = encounterData[index];

            if (data == null ||
                !data.IsValidData() ||
                sessionManager.IsEncounterCleared(
                    data.EncounterId))
            {
                continue;
            }

            GameObject encounterObject =
                new GameObject(
                    $"Encounter_{data.EncounterId}",
                    typeof(SpriteRenderer),
                    typeof(CircleCollider2D),
                    typeof(ExplorationEncounterView));

            encounterObject.transform.position =
                new Vector3(
                    data.ExplorationPosition.x,
                    data.ExplorationPosition.y,
                    0f);

            SpriteRenderer spriteRenderer =
                encounterObject.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite =
                runtimeSquareSprite;

            spriteRenderer.color =
                GetEncounterColor(index);

            spriteRenderer.sortingOrder = 4;

            encounterObject.transform.localScale =
                new Vector3(0.75f, 0.75f, 1f);

            CircleCollider2D collider =
                encounterObject.GetComponent<CircleCollider2D>();

            collider.isTrigger = true;
            collider.radius = 0.65f;

            ExplorationEncounterView encounterView =
                encounterObject.GetComponent<ExplorationEncounterView>();

            encounterView.Initialize(data);
            encounterViews.Add(encounterView);
        }
    }

    private void CreateHud(
        ExplorationSessionManager sessionManager)
    {
        GameObject canvasObject = new GameObject(
            "ExplorationPrototypeHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;

        CreateInstructionText(
            canvasObject.transform);

        CreateProgressText(
            canvasObject.transform,
            sessionManager);

        CreateLastRewardText(
            canvasObject.transform,
            sessionManager);
    }

    private static void CreateInstructionText(
        Transform parent)
    {
        TMP_Text instructionText = CreateText(
            "Instructions",
            parent,
            24f,
            TextAlignmentOptions.TopLeft);

        RectTransform instructionRect =
            instructionText.rectTransform;

        instructionRect.anchorMin =
            new Vector2(0f, 1f);

        instructionRect.anchorMax =
            new Vector2(0f, 1f);

        instructionRect.pivot =
            new Vector2(0f, 1f);

        instructionRect.sizeDelta =
            new Vector2(850f, 150f);

        instructionRect.anchoredPosition =
            new Vector2(24f, -24f);

        instructionText.text =
            "33일차 탐사 테스트\n" +
            "WASD / 방향키 이동 · 색상 사각형에 닿으면 전투\n" +
            "승리: 영구 캐릭터 EXP + 자원 획득 · 도주/패배: 보상 없음";
    }

    private static void CreateProgressText(
        Transform parent,
        ExplorationSessionManager sessionManager)
    {
        CharacterProgressionManager progressionManager =
            CharacterProgressionManager.EnsureInstance();

        PlayerResourceManager resourceManager =
            PlayerResourceManager.EnsureInstance();

        TMP_Text progressText = CreateText(
            "PersistentProgress",
            parent,
            24f,
            TextAlignmentOptions.TopRight);

        RectTransform progressRect =
            progressText.rectTransform;

        progressRect.anchorMin =
            new Vector2(1f, 1f);

        progressRect.anchorMax =
            new Vector2(1f, 1f);

        progressRect.pivot =
            new Vector2(1f, 1f);

        progressRect.sizeDelta =
            new Vector2(650f, 180f);

        progressRect.anchoredPosition =
            new Vector2(-24f, -24f);

        int totalCount =
            Resources.LoadAll<EncounterData>(
                "Encounters").Length;

        progressText.text =
            $"캐릭터 Lv.{progressionManager.Level}  " +
            $"EXP {progressionManager.CurrentExperience}" +
            $"/{progressionManager.RequiredExperience}\n" +
            $"Gold {resourceManager.Gold}\n" +
            $"나사 {resourceManager.Screw}  " +
            $"철판 {resourceManager.IronPlate}  " +
            $"전선 {resourceManager.Wire}\n" +
            $"클리어 " +
            $"{sessionManager.ClearedEncounterIds.Count}" +
            $" / {totalCount}";
    }

    private static void CreateLastRewardText(
        Transform parent,
        ExplorationSessionManager sessionManager)
    {
        ExplorationClearRewardResult reward =
            sessionManager.LastClearReward;

        if (reward == null)
        {
            return;
        }

        TMP_Text rewardText = CreateText(
            "LastClearReward",
            parent,
            25f,
            TextAlignmentOptions.BottomLeft);

        RectTransform rewardRect =
            rewardText.rectTransform;

        rewardRect.anchorMin =
            new Vector2(0f, 0f);

        rewardRect.anchorMax =
            new Vector2(0f, 0f);

        rewardRect.pivot =
            new Vector2(0f, 0f);

        rewardRect.sizeDelta =
            new Vector2(1100f, 140f);

        rewardRect.anchoredPosition =
            new Vector2(24f, 24f);

        string levelUpText =
            reward.LeveledUp
                ? $" · LEVEL UP! Lv.{reward.CurrentCharacterLevel}"
                : string.Empty;

        rewardText.text =
            $"{reward.EncounterName} 클리어 보상\n" +
            $"캐릭터 EXP +{reward.CharacterExperience}" +
            $"{levelUpText}\n" +
            $"Gold +{reward.Gold} · " +
            $"나사 +{reward.Screw} · " +
            $"철판 +{reward.IronPlate} · " +
            $"전선 +{reward.Wire}";
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(
            parent,
            false);

        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

        text.font =
            ProjectCFontProvider.KoreanFontAsset;

        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;

        return text;
    }

    private static void EnsureRuntimeSquareSprite()
    {
        if (runtimeSquareSprite != null)
        {
            return;
        }

        Texture2D texture =
            Texture2D.whiteTexture;

        runtimeSquareSprite =
            Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
    }

    private static Color GetEncounterColor(
        int index)
    {
        switch (index % 3)
        {
            case 0:
                return new Color(
                    0.9f,
                    0.2f,
                    0.2f,
                    1f);

            case 1:
                return new Color(
                    0.85f,
                    0.45f,
                    0.15f,
                    1f);

            default:
                return new Color(
                    0.75f,
                    0.2f,
                    0.65f,
                    1f);
        }
    }
}
