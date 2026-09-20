using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GorilaChestMod
{
    /// <summary>
    /// Everything the crafting panel shows and does while the Deconstruct tab is
    /// selected: the item list, the detail panel on the right, and the button.
    ///
    /// The vanilla panel does most of the work. The list entries are made by the
    /// game's own AddRecipeToList, selection and scrolling are vanilla, and the
    /// progress bar is the craft timer. This class fills in the parts that have
    /// to read differently: which items are listed, what the requirement slots
    /// show, and what happens when the timer runs out.
    /// </summary>
    internal static class DeconstructPanel
    {
        internal const string ButtonLabel = "Deconstruct";

        private struct Entry
        {
            internal Recipe Recipe;
            internal ItemDrop.ItemData Item;
            internal bool CanDeconstruct;
            internal string Name;
        }

        private static ItemDrop.ItemData _pendingItem;
        private static Recipe _pendingRecipe;

        /// <summary>Replaces InventoryGui.UpdateRecipeList while the tab is selected.</summary>
        internal static void BuildList(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            IList list = GuiAccess.AvailableRecipes(gui);

            foreach (object pair in list)
            {
                UnityEngine.Object.Destroy(GuiAccess.ElementOf(pair));
            }

            list.Clear();

            if (player == null)
            {
                return;
            }

            var entries = new List<Entry>();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                Recipe recipe = Salvage.FindRecipe(player, item);
                if (recipe == null)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    Recipe = recipe,
                    Item = item,
                    CanDeconstruct = Salvage.Refusal(player, item, recipe) == null,
                    Name = Localization.instance.Localize(item.m_shared.m_name),
                });
            }

            // Ready ones first, then by name, the higher level first, then by slot.
            entries.Sort((a, b) =>
            {
                int c = b.CanDeconstruct.CompareTo(a.CanDeconstruct);
                if (c == 0) c = string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
                if (c == 0) c = b.Item.m_quality.CompareTo(a.Item.m_quality);
                if (c == 0) c = a.Item.m_gridPos.y.CompareTo(b.Item.m_gridPos.y);
                if (c == 0) c = a.Item.m_gridPos.x.CompareTo(b.Item.m_gridPos.x);
                return c;
            });

            // AddRecipeToList places each entry one row below the previous, so
            // adding them in order is the whole sort.
            foreach (Entry entry in entries)
            {
                GuiAccess.AddRecipeToList(gui, player, entry.Recipe, entry.Item, entry.CanDeconstruct);
            }

            float height = Mathf.Max(GuiAccess.RecipeListBaseSize(gui), list.Count * gui.m_recipeListSpace);
            gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>Runs after InventoryGui.UpdateRecipe, overwriting what vanilla wrote for an upgrade.</summary>
        internal static void UpdateDetails(InventoryGui gui, Player player)
        {
            GuiAccess.Selected(gui, out Recipe recipe, out ItemDrop.ItemData item);
            if (recipe == null || item == null)
            {
                // Vanilla already blanked the panel and disabled the button.
                return;
            }

            string refusal = Salvage.Refusal(player, item, recipe);
            int percent = DeconstructSettings.Percent;

            string name = Localization.instance.Localize(item.m_shared.m_name);
            gui.m_recipeName.text = recipe.m_amount > 1 ? $"{name} x{recipe.m_amount}" : name;
            gui.m_recipeDecription.text = Localization.instance.Localize(
                ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, Game.m_worldLevel, -1, false));

            gui.m_itemCraftType.gameObject.SetActive(true);
            gui.m_itemCraftType.text = refusal ?? $"Returns {percent}% of the materials";

            gui.m_variantButton.gameObject.SetActive(false);
            gui.m_minStationLevelIcon.gameObject.SetActive(false);

            ShowMaterials(gui, Salvage.Returns(recipe, item.m_quality, percent));

            gui.m_craftButton.interactable = refusal == null;
            TMP_Text caption = gui.m_craftButton.GetComponentInChildren<TMP_Text>();
            if (caption != null)
            {
                caption.text = ButtonLabel;
            }

            UITooltip tooltip = gui.m_craftButton.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = refusal ?? "";
            }
        }

        /// <summary>Replaces InventoryGui.OnCraftPressed while the tab is selected.</summary>
        internal static void Begin(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            GuiAccess.Selected(gui, out Recipe recipe, out ItemDrop.ItemData item);
            if (player == null || recipe == null || item == null)
            {
                return;
            }

            string refusal = Salvage.Refusal(player, item, recipe);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, refusal);
                return;
            }

            GuiAccess.FocusCraftingPanel(gui);
            _pendingItem = item;
            _pendingRecipe = recipe;
            GuiAccess.StartTimer(gui, recipe);

            if (gui.CraftingVibration != null)
            {
                UnityEngine.Object.Instantiate(gui.CraftingVibration);
            }

            CraftingStation station = player.GetCurrentCraftingStation();
            if (station != null)
            {
                station.m_craftItemEffects.Create(player.transform.position, Quaternion.identity);
            }
            else
            {
                gui.m_craftItemEffects.Create(player.transform.position, Quaternion.identity);
            }
        }

        /// <summary>A vanilla craft is starting, so any deconstruction left over from a cancelled timer is forgotten.</summary>
        internal static void ClearPending()
        {
            _pendingItem = null;
            _pendingRecipe = null;
        }

        /// <summary>
        /// Runs in place of InventoryGui.DoCrafting when the timer that finished
        /// was started by Begin. Returns false when it was a normal craft.
        /// </summary>
        internal static bool Finish(InventoryGui gui, Player player)
        {
            if (_pendingItem == null)
            {
                return false;
            }

            ItemDrop.ItemData item = _pendingItem;
            Recipe recipe = _pendingRecipe;
            ClearPending();

            if (Salvage.Deconstruct(player, item, recipe))
            {
                CraftingStation station = player.GetCurrentCraftingStation();
                if (station != null)
                {
                    station.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }
                else
                {
                    gui.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }
            }

            GuiAccess.UpdateCraftingPanel(gui);
            return true;
        }

        /// <summary>Writes the materials into the four requirement slots, paging through them like vanilla when there are more.</summary>
        private static void ShowMaterials(InventoryGui gui, List<Refund> materials)
        {
            GameObject[] slots = gui.m_recipeRequirementList;
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            int first = 0;
            if (materials.Count > slots.Length)
            {
                int pages = Mathf.CeilToInt((float)materials.Count / slots.Length);
                first = (int)Time.fixedTime % pages * slots.Length;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Transform slot = slots[i].transform;
                int index = first + i;
                if (index >= materials.Count)
                {
                    InventoryGui.HideRequirement(slot);
                    continue;
                }

                Refund material = materials[index];
                ItemDrop.ItemData data = material.Item.m_itemData;
                string name = Localization.instance.Localize(data.m_shared.m_name);

                Image icon = slot.Find("res_icon").GetComponent<Image>();
                TMP_Text label = slot.Find("res_name").GetComponent<TMP_Text>();
                TMP_Text amount = slot.Find("res_amount").GetComponent<TMP_Text>();

                icon.gameObject.SetActive(true);
                label.gameObject.SetActive(true);
                amount.gameObject.SetActive(true);

                icon.sprite = data.GetIcon();
                icon.color = Color.white;
                label.text = name;
                amount.text = material.Amount.ToString();
                amount.color = Color.white;

                UITooltip tooltip = slot.GetComponent<UITooltip>();
                if (tooltip != null)
                {
                    tooltip.m_text = name;
                }
            }
        }
    }
}
