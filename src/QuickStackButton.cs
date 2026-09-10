using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GorilaChestMod
{
    /// <summary>
    /// Builds the quick stack button by cloning the game's own "take all" button,
    /// so it inherits the vanilla look, font and sounds. It lives beside the
    /// inventory rather than inside it: parented to the root that holds both the
    /// player and the container panels, drawn last so an open chest cannot cover
    /// it, and glued to the weight readout so it follows the layout.
    /// Nothing here runs on a dedicated server, where there is no InventoryGui.
    /// </summary>
    internal static class QuickStackButton
    {
        /// <summary>A compact square, the caption wraps inside it.</summary>
        private static readonly Vector2 Size = new Vector2(118f, 118f);

        /// <summary>Offset from the weight readout, in canvas units, before the configured nudge.</summary>
        private static readonly Vector2 FromWeight = new Vector2(0f, -140f);

        private static Button _button;
        private static RectTransform _rect;
        private static RectTransform _anchor;

        internal static void Create(InventoryGui gui)
        {
            if (gui == null || gui.m_takeAllButton == null || gui.m_weight == null || gui.m_player == null)
            {
                GorilaChestModPlugin.Log.LogWarning("Inventory screen looks different than expected, the quick stack button was not created.");
                return;
            }

            Destroy();

            // Parented next to the weight readout, which already sits outside the
            // item grid and moves with the panel, so the anchoring is inherited.
            _anchor = gui.m_weight.rectTransform;

            _button = Object.Instantiate(gui.m_takeAllButton, _anchor.parent);
            _button.name = "GorilaChestMod_QuickStack";
            _rect = _button.GetComponent<RectTransform>();

            RectTransform source = gui.m_takeAllButton.GetComponent<RectTransform>();
            if (_rect != null && source != null)
            {
                _rect.anchorMin = _anchor.anchorMin;
                _rect.anchorMax = _anchor.anchorMax;
                _rect.pivot = _anchor.pivot;
                _rect.localScale = source.localScale;
                _rect.sizeDelta = Size;
            }

            // An open chest panel is drawn after the player panel, so hierarchy order
            // alone cannot keep the button visible. Its own canvas with a higher
            // sorting order can, and the raycaster keeps it clickable.
            Canvas canvas = _button.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            _button.gameObject.AddComponent<GraphicRaycaster>();

            SetLabel(_button, ModConfig.QuickStackButtonLabel.Value);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
            _button.gameObject.SetActive(false);
        }

        internal static void Destroy()
        {
            if (_button != null)
            {
                Object.Destroy(_button.gameObject);
            }

            _button = null;
            _rect = null;
            _anchor = null;
        }

        /// <summary>Shown only while the inventory is open and there is a chest in range.</summary>
        internal static void UpdateVisibility()
        {
            if (_button == null)
            {
                return;
            }

            bool show = ModConfig.QuickStackEnabled.Value &&
                        ModConfig.QuickStackButtonVisible.Value &&
                        InventoryGui.IsVisible() &&
                        QuickStack.HasTargets();

            if (_button.gameObject.activeSelf == show)
            {
                return;
            }

            _button.gameObject.SetActive(show);

            if (show)
            {
                // On the way in rather than every frame, this dirties the canvas.
                Reposition();
            }
        }

        private static void Reposition()
        {
            if (_rect == null || _anchor == null)
            {
                return;
            }

            Vector2 offset = FromWeight + ModConfig.QuickStackButtonOffset.Value;
            _rect.anchoredPosition = _anchor.anchoredPosition + offset;

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"Quick stack button at {_rect.anchoredPosition}, weight readout at {_anchor.anchoredPosition}, offset {offset}.");
            }
        }

        private static void OnClick()
        {
            QuickStack.Run();
        }

        private static void SetLabel(Button button, string label)
        {
            // Valheim relocalises button captions on language change, which would
            // overwrite the caption with the take all string.
            foreach (MonoBehaviour behaviour in button.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "Localize")
                {
                    Object.Destroy(behaviour);
                }
            }

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = label;

                // A square button, so the caption wraps over two or three lines.
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 9f;
                text.fontSizeMax = text.fontSize;

                RectTransform textRect = text.rectTransform;
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(6f, 6f);
                    textRect.offsetMax = new Vector2(-6f, -6f);
                }
            }

            UITooltip tooltip = button.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = label;
                tooltip.m_topic = string.Empty;
            }
        }
    }
}
