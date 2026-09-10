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
        private static readonly Vector2 FromWeight = new Vector2(78f, -96f);

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

            _anchor = gui.m_weight.rectTransform;

            // The root holds the player panel and the container panel as siblings,
            // so a child of it that comes last always draws above both.
            Transform root = gui.m_player.parent != null ? gui.m_player.parent : gui.m_player.transform;

            _button = Object.Instantiate(gui.m_takeAllButton, root);
            _button.name = "GorilaChestMod_QuickStack";
            _rect = _button.GetComponent<RectTransform>();

            RectTransform source = gui.m_takeAllButton.GetComponent<RectTransform>();
            if (_rect != null && source != null)
            {
                _rect.localScale = source.localScale;
                _rect.sizeDelta = Size;
            }

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
                // Done on the way in rather than every frame, both of these dirty the canvas.
                _button.transform.SetAsLastSibling();
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
            _rect.position = _anchor.TransformPoint(new Vector3(offset.x, offset.y, 0f));

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo($"Quick stack button placed at {_rect.position}, offset {offset}.");
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
