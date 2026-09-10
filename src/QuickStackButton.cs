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
        private static Button _button;

        internal static void Create(InventoryGui gui)
        {
            if (gui == null || gui.m_takeAllButton == null || gui.m_player == null)
            {
                GorilaChestModPlugin.Log.LogWarning("Inventory screen looks different than expected, the quick stack button was not created.");
                return;
            }

            Destroy();

            _button = Object.Instantiate(gui.m_takeAllButton, gui.m_player);
            _button.name = "GorilaChestMod_QuickStack";

            CopyPlacement(gui.m_takeAllButton, _button);
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
        /// Puts the clone in the same relative spot of the player panel that the
        /// original occupies in the container panel, then applies the configured
        /// nudge. That keeps it inside the panel at any resolution.
        /// </summary>
        private static void CopyPlacement(Button source, Button clone)
        {
            RectTransform from = source.GetComponent<RectTransform>();
            RectTransform to = clone.GetComponent<RectTransform>();
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta;
            to.localScale = from.localScale;
            to.anchoredPosition = from.anchoredPosition + ModConfig.QuickStackButtonOffset.Value;
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
