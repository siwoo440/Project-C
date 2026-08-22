using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ConsumableWindowToggleView : MonoBehaviour, IDisposable
{
    private ConsumableSlotBarView slotBarView;
    private CanvasGroup slotBarCanvasGroup;
    private Button toggleButton;
    private TMP_Text toggleLabel;
    private Coroutine delayedDisableCoroutine;
    private bool windowVisible;
    private bool disposed;

    public static ConsumableWindowToggleView Create(Canvas parentCanvas, ConsumableSlotBarView targetSlotBarView)
    {
        if (parentCanvas == null)
        {
            throw new ArgumentNullException(nameof(parentCanvas));
        }

        if (targetSlotBarView == null)
        {
            throw new ArgumentNullException(nameof(targetSlotBarView));
        }

        GameObject rootObject = new GameObject(
            "ConsumableWindowToggle",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(ConsumableWindowToggleView));

        rootObject.transform.SetParent(parentCanvas.transform, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas toggleCanvas = rootObject.GetComponent<Canvas>();
        toggleCanvas.overrideSorting = true;
        toggleCanvas.sortingOrder = 421;

        ConsumableWindowToggleView view = rootObject.GetComponent<ConsumableWindowToggleView>();
        view.Initialize(targetSlotBarView);
        return view;
    }

    private void Initialize(ConsumableSlotBarView targetSlotBarView)
    {
        slotBarView = targetSlotBarView;

        slotBarCanvasGroup = slotBarView.GetComponent<CanvasGroup>();
        if (slotBarCanvasGroup == null)
        {
            slotBarCanvasGroup = slotBarView.gameObject.AddComponent<CanvasGroup>();
        }

        MoveConsumablePanelBelowToggle();
        CreateToggleButton();
        SetWindowVisible(false, true);
    }

    private void MoveConsumablePanelBelowToggle()
    {
        Transform panelTransform = slotBarView.transform.Find("ConsumablePanel");
        if (panelTransform == null)
        {
            return;
        }

        RectTransform panelRect = panelTransform as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(20f, -70f);
        }
    }

    private void CreateToggleButton()
    {
        GameObject buttonObject = new GameObject(
            "ItemWindowToggleButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        buttonObject.transform.SetParent(transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.sizeDelta = new Vector2(170f, 42f);
        buttonRect.anchoredPosition = new Vector2(20f, -20f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.13f, 0.13f, 0.17f, 0.97f);

        toggleButton = buttonObject.GetComponent<Button>();
        toggleButton.targetGraphic = buttonImage;
        toggleButton.onClick.AddListener(HandleToggleClicked);

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        toggleLabel = textObject.GetComponent<TextMeshProUGUI>();
        toggleLabel.font = ProjectCFontProvider.KoreanFontAsset;
        toggleLabel.fontSize = 18f;
        toggleLabel.color = Color.white;
        toggleLabel.alignment = TextAlignmentOptions.Center;
        toggleLabel.raycastTarget = false;
    }

    private void HandleToggleClicked()
    {
        SetWindowVisible(!windowVisible, false);
    }

    private void SetWindowVisible(bool visible, bool immediate)
    {
        if (disposed || slotBarView == null || slotBarCanvasGroup == null)
        {
            return;
        }

        windowVisible = visible;

        if (delayedDisableCoroutine != null)
        {
            StopCoroutine(delayedDisableCoroutine);
            delayedDisableCoroutine = null;
        }

        slotBarCanvasGroup.alpha = visible ? 1f : 0f;
        slotBarCanvasGroup.interactable = visible;
        slotBarCanvasGroup.blocksRaycasts = visible;

        if (visible)
        {
            slotBarView.enabled = true;
        }
        else if (immediate || !IsAltPressed())
        {
            slotBarView.enabled = false;
        }
        else
        {
            delayedDisableCoroutine = StartCoroutine(DisableSlotBarAfterAltRelease());
        }

        RefreshToggleLabel();
    }

    private IEnumerator DisableSlotBarAfterAltRelease()
    {
        while (IsAltPressed())
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        if (!windowVisible && slotBarView != null)
        {
            slotBarView.enabled = false;
        }

        delayedDisableCoroutine = null;
    }

    private static bool IsAltPressed()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
    }

    private void RefreshToggleLabel()
    {
        if (toggleLabel == null)
        {
            return;
        }

        toggleLabel.text = windowVisible ? "아이템 닫기" : "아이템 열기";
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (delayedDisableCoroutine != null)
        {
            StopCoroutine(delayedDisableCoroutine);
            delayedDisableCoroutine = null;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(HandleToggleClicked);
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
