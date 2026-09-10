using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GorilaChestMod
{
    /// <summary>
    /// Builds the quick stack button by cloning the game's own "take all" button,
    /// so it inherits the vanilla look, font and sounds, and parks it in the player
    /// inventory panel. Nothing here runs on a dedicated server, where there is no
    /// InventoryGui at all.
    /// </summary>
    internal static class QuickStackButton
    {
        /// <summary>Width the caption needs, the vanilla take all button is narrower than this.</summary>
        private const float Width = 230f;

        /// <summary>How far below the weight readout the button sits by default.</summary>
        private static readonly Vector2 BelowWeight = new Vector2(0f, -74f);

        private static Button _button;

        internal static void Create(InventoryGui gui)
        {
            if (gui == null || gui.m_takeAllButton == null || gui.m_weight == null)
            {
                GorilaChestModPlugin.Log.LogWarning("Inventory screen looks different than expected, the quick stack button was not created.");
                return;
            }

            Destroy();

            // Parented next to the weight readout, which already lives outside the
            // item grid, so the button never covers a slot.
            RectTransform anchor = gui.m_weight.rectTransform;
            _button = Object.Instantiate(gui.m_takeAllButton, anchor.parent);
            _button.name = "GorilaChestMod_QuickStack";

            PlaceUnder(anchor, gui.m_takeAllButton, _button);
            SetLabel(_button, ModConfig.QuickStackButtonLabel.Value);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
            _button.gameObject.SetActive(false);

            if (ModConfig.Verbose.Value)
            {
                RectTransform rect = _button.GetComponent<RectTransform>();
                GorilaChestModPlugin.Log.LogInfo($"Quick stack button created at {rect.anchoredPosition}.");
            }
        }

        internal static void Destroy()
        {
            if (_button != null)
            {
                Object.Destroy(_button.gameObject);
            }

            _button = null;
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

            if (_button.gameObject.activeSelf != show)
            {
                _button.gameObject.SetActive(show);
            }
        }

        private static void OnClick()
        {
            QuickStack.Run();
        }

        /// <summary>
        /// Anchors the clone to whatever the weight readout is anchored to and drops
        /// it below, so it follows the panel at any resolution and stays clear of the
        /// grid. The configured offset nudges it from there.
        /// </summary>
        private static void PlaceUnder(RectTransform anchor, Button source, Button clone)
        {
            RectTransform from = source.GetComponent<RectTransform>();
            RectTransform to = clone.GetComponent<RectTransform>();
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = anchor.anchorMin;
            to.anchorMax = anchor.anchorMax;
            to.pivot = anchor.pivot;
            to.localScale = from.localScale;
            to.sizeDelta = new Vector2(Width, from.sizeDelta.y);
            to.anchoredPosition = anchor.anchoredPosition + BelowWeight + ModConfig.QuickStackButtonOffset.Value;
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

                // The caption is longer than the vanilla one it was cloned from.
                text.enableAutoSizing = true;
                text.fontSizeMin = 10f;
                text.fontSizeMax = text.fontSize;
                text.textWrappingMode = TextWrappingModes.NoWrap;
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
