using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MinorCardBuffWindowView : MonoBehaviour, IDisposable
{
    private BattleMinorCardController controller;
    private GameObject panelObject;
    private Button toggleButton;
    private TMP_Text toggleText;
    private TMP_Text contentText;
    private bool disposed;

    public static MinorCardBuffWindowView Create(Canvas parentCanvas, BattleMinorCardController battleController)
    {
        if (parentCanvas == null)
        {
            throw new ArgumentNullException(nameof(parentCanvas));
        }

        GameObject rootObject = new GameObject(
            "MinorCardBuffWindow",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(MinorCardBuffWindowView));

        rootObject.transform.SetParent(parentCanvas.transform, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 440;

        MinorCardBuffWindowView view = rootObject.GetComponent<MinorCardBuffWindowView>();
        view.Initialize(battleController);
        return view;
    }

    private void Initialize(BattleMinorCardController battleController)
    {
        controller = battleController ?? throw new ArgumentNullException(nameof(battleController));
        CreateToggleButton();
        CreatePanel();
        controller.StateChanged += Refresh;
        panelObject.SetActive(false);
        Refresh();
    }

    private void CreateToggleButton()
    {
        toggleButton = CreateButton("MinorBuffToggle", transform, new Color(0.13f, 0.13f, 0.17f, 0.97f));
        RectTransform buttonRect = toggleButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.sizeDelta = new Vector2(145f, 42f);
        buttonRect.anchoredPosition = new Vector2(-170f, -20f);
        toggleButton.onClick.AddListener(TogglePanel);

        toggleText = CreateText("Label", toggleButton.transform, 17f, Color.white, TextAlignmentOptions.Center);
        Stretch(toggleText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private void CreatePanel()
    {
        Image panelImage = CreateImage("MinorBuffPanel", transform, new Color(0.04f, 0.04f, 0.055f, 0.97f));
        panelObject = panelImage.gameObject;

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.sizeDelta = new Vector2(480f, 330f);
        panelRect.anchoredPosition = new Vector2(-20f, -72f);

        TMP_Text titleText = CreateText("Title", panelObject.transform, 22f, Color.white, TextAlignmentOptions.Left);
        titleText.text = "현재 전투 마이너 카드";
        SetRect(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 42f),
            new Vector2(0f, -12f),
            new Vector2(0.5f, 1f));
        titleText.rectTransform.offsetMin = new Vector2(18f, titleText.rectTransform.offsetMin.y);
        titleText.rectTransform.offsetMax = new Vector2(-18f, titleText.rectTransform.offsetMax.y);

        contentText = CreateText("Content", panelObject.transform, 17f, Color.white, TextAlignmentOptions.TopLeft);
        Stretch(
            contentText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(18f, 18f),
            new Vector2(-18f, -62f));
        contentText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void TogglePanel()
    {
        if (panelObject == null)
        {
            return;
        }

        panelObject.SetActive(!panelObject.activeSelf);
    }

    private void Refresh()
    {
        if (disposed || controller == null)
        {
            return;
        }

        int count = controller.SelectedCards.Count;
        if (toggleText != null)
        {
            toggleText.text = count > 0 ? $"현재 강화 ({count})" : "현재 강화";
        }

        if (contentText == null)
        {
            return;
        }

        if (count == 0)
        {
            contentText.text = "이번 전투에서 선택한 마이너 카드가 없습니다.";
            return;
        }

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < count; index++)
        {
            MinorCardData cardData = controller.SelectedCards[index];
            if (cardData == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append("• ");
            builder.AppendLine(cardData.DisplayName);
            builder.Append("  ");
            builder.Append(GetEffectLabel(cardData));
        }

        contentText.text = builder.ToString();
    }

    private static string GetEffectLabel(MinorCardData cardData)
    {
        string target = cardData.TargetType == MinorCardTargetType.AllAllies ? "모든 아군" : "모든 적";
        string effect;

        switch (cardData.EffectType)
        {
            case MinorCardEffectType.IncreaseMaxHealth:
                effect = $"최대 체력 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.AttackPowerUp:
                effect = $"공격력 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.PhysicalDefenseUp:
                effect = $"물리 방어 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.PhysicalDefenseDown:
                effect = $"물리 방어 -{cardData.EffectValue}";
                break;
            case MinorCardEffectType.MagicalResistanceUp:
                effect = $"마법 저항 +{cardData.EffectValue}";
                break;
            case MinorCardEffectType.MagicalResistanceDown:
                effect = $"마법 저항 -{cardData.EffectValue}";
                break;
            default:
                effect = "효과 없음";
                break;
        }

        return $"{target} · {effect}";
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(string objectName, Transform parent, Color color)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = ProjectCFontProvider.KoreanFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void SetRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Vector2 pivot)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.pivot = pivot;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (controller != null)
        {
            controller.StateChanged -= Refresh;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePanel);
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
