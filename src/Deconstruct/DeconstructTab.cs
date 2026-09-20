using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GorilaChestMod
{
    /// <summary>
    /// The third tab beside Craft and Upgrade.
    ///
    /// It is a copy of the Upgrade tab, so the font, size, colours, sprites and
    /// click sound are the game's own. The vanilla tabs mark the selected one by
    /// making it non interactable, and this one follows the same convention:
    /// while it is selected both vanilla tabs are interactable, which makes the
    /// game's InCraftTab and InUpradeTab both answer false.
    /// </summary>
    internal static class DeconstructTab
    {
        internal const string Label = "Deconstruct";

        private static Button _button;

        /// <summary>True while the Deconstruct tab is the selected one.</summary>
        internal static bool Active { get; private set; }

        internal static void Create(InventoryGui gui)
        {
            // The crafting panel changed under us, so the rest of the mod carries
            // on and only this tab stays away.
            if (!GuiAccess.Ready || !ModConfig.DeconstructEnabled.Value)
            {
                return;
            }

            Button upgrade = gui.m_tabUpgrade;
            Button craft = gui.m_tabCraft;
            if (upgrade == null || craft == null)
            {
                GorilaChestModPlugin.Log.LogError("InventoryGui has no Craft or Upgrade tab to copy, the Deconstruct tab stays off.");
                return;
            }

            GameObject clone = Object.Instantiate(upgrade.gameObject, upgrade.transform.parent, false);
            clone.name = "TabDeconstruct";

            // A copied gamepad binding would press this tab together with Upgrade,
            // and a copied Localize would put the Upgrade caption back.
            foreach (UIGamePad pad in clone.GetComponentsInChildren<UIGamePad>(true))
            {
                if (pad.m_hint != null)
                {
                    Object.DestroyImmediate(pad.m_hint);
                }

                Object.DestroyImmediate(pad);
            }

            foreach (Localize localize in clone.GetComponentsInChildren<Localize>(true))
            {
                Object.DestroyImmediate(localize);
            }

            foreach (UITooltip tooltip in clone.GetComponentsInChildren<UITooltip>(true))
            {
                tooltip.m_text = "";
            }

            // Same spacing again to the right of Upgrade. A layout group on the
            // parent, if there ever is one, overrides this and places it itself.
            var upgradeRect = (RectTransform)upgrade.transform;
            var craftRect = (RectTransform)craft.transform;
            var cloneRect = (RectTransform)clone.transform;
            Vector2 step = upgradeRect.anchoredPosition - craftRect.anchoredPosition;
            if (step.sqrMagnitude < 1f)
            {
                step = new Vector2(upgradeRect.rect.width, 0f);
            }

            cloneRect.anchoredPosition = upgradeRect.anchoredPosition + step;
            clone.transform.SetSiblingIndex(upgrade.transform.GetSiblingIndex() + 1);

            _button = clone.GetComponent<Button>();

            // Replacing the event drops the persistent OnTabUpgradePressed listener the copy came with.
            _button.onClick = new Button.ButtonClickedEvent();
            _button.onClick.AddListener(() => Select(gui));

            SetCaption();
            Active = false;
            _button.interactable = true;
            clone.SetActive(false);

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"Tabs: craft at {craftRect.anchoredPosition} size {craftRect.rect.size}, " +
                    $"upgrade at {upgradeRect.anchoredPosition} size {upgradeRect.rect.size}, " +
                    $"deconstruct at {cloneRect.anchoredPosition}, parent {upgrade.transform.parent?.name}.");
            }
        }

        internal static void Destroy()
        {
            if (_button != null)
            {
                Object.Destroy(_button.gameObject);
            }

            _button = null;
            Active = false;
        }

        /// <summary>
        /// Whether the tab belongs on the panel at all: only at a real crafting
        /// station that has a Craft tab, never with bare hands and never at an
        /// upgrader station.
        /// </summary>
        internal static bool Available(Player player)
        {
            if (!ModConfig.DeconstructEnabled.Value || player == null || _button == null)
            {
                return false;
            }

            CraftingStation station = player.GetCurrentCraftingStation();
            return station != null && station.m_hasCraftTab && !station.m_upgrader;
        }

        /// <summary>Runs before vanilla lays the tabs out, so the list is built for the right tab.</summary>
        internal static void BeforePanelUpdate(InventoryGui gui, Player player)
        {
            if (_button == null)
            {
                return;
            }

            bool available = Available(player);
            if (!available && Active)
            {
                // Hand the selection back to Craft, the game's default.
                Active = false;
                gui.m_tabCraft.interactable = false;
                gui.m_tabUpgrade.interactable = true;
            }

            _button.gameObject.SetActive(available);
            _button.interactable = !Active;

            if (Active)
            {
                gui.m_tabCraft.interactable = true;
                gui.m_tabUpgrade.interactable = true;
            }

            SetCaption();
        }

        /// <summary>Craft or Upgrade was clicked.</summary>
        internal static void Deselect()
        {
            Active = false;
            if (_button != null)
            {
                _button.interactable = true;
            }
        }

        private static void Select(InventoryGui gui)
        {
            if (!Available(Player.m_localPlayer))
            {
                return;
            }

            GuiAccess.FocusCraftingPanel(gui);
            Active = true;
            gui.m_tabCraft.interactable = true;
            gui.m_tabUpgrade.interactable = true;
            _button.interactable = false;
            GuiAccess.UpdateCraftingPanel(gui);
        }

        private static void SetCaption()
        {
            foreach (TMP_Text text in _button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text != Label)
                {
                    text.text = Label;
                }
            }
        }
    }
}
