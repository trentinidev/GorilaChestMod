using System.Collections.Generic;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>One material a deconstruction gives back.</summary>
    internal struct Refund
    {
        internal ItemDrop Item;
        internal int Amount;
    }

    /// <summary>
    /// The rules and the arithmetic of deconstruction, with no UI in it.
    ///
    /// What an item cost is read from its recipe the same way the game charges
    /// for it: level 1 costs Requirement.GetAmount(1), each upgrade to level n
    /// costs Requirement.GetAmount(n), and InventoryGui.DoCrafting pays those
    /// through Player.ConsumeResources. Adding every level up to the item's own
    /// gives exactly what went into it.
    /// </summary>
    internal static class Salvage
    {
        /// <summary>
        /// The recipe that makes this item, when it is one this station could
        /// deconstruct at all. Items that fail here do not appear in the list.
        /// </summary>
        internal static Recipe FindRecipe(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item?.m_shared == null || item.m_shared.m_questItem || ObjectDB.instance == null)
            {
                return null;
            }

            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe == null || !recipe.m_enabled || recipe.m_item == null || recipe.m_resources == null)
            {
                return null;
            }

            // Meads and feasts accept one ingredient out of several, so there is
            // no way to know which one this particular item was made from.
            if (recipe.m_requireOnlyOneIngredient)
            {
                return null;
            }

            // Only at the station the recipe belongs to, the same test the Craft tab uses.
            if (!player.RequiredCraftingStation(recipe, 1, checkLevel: false))
            {
                return null;
            }

            return Returns(recipe, item.m_quality, DeconstructSettings.MaxPercent).Count > 0 ? recipe : null;
        }

        /// <summary>
        /// Why this item cannot be deconstructed right now, or null when it can.
        /// Items that fail here still appear in the list, greyed out, with the
        /// reason shown in the panel.
        /// </summary>
        internal static string Refusal(Player player, ItemDrop.ItemData item, Recipe recipe)
        {
            if (player == null || item == null || recipe == null || !player.GetInventory().ContainsItem(item))
            {
                return "The item is no longer in your inventory";
            }

            if (item.m_equipped || player.IsItemEquiped(item))
            {
                return "Unequip it first";
            }

            if (ModConfig.DeconstructRespectFavorites.Value && Favorites.IsFavorite(item))
            {
                return "Marked as favorite, Alt click it to unmark";
            }

            if (item.m_stack < recipe.m_amount)
            {
                return $"Needs a full stack of {recipe.m_amount}";
            }

            if (Returns(recipe, item.m_quality, DeconstructSettings.Percent).Count == 0)
            {
                return "Too little would come back at this percentage";
            }

            return null;
        }

        /// <summary>The materials an item of this quality gives back at this percentage, rounded down one by one.</summary>
        internal static List<Refund> Returns(Recipe recipe, int quality, int percent)
        {
            var result = new List<Refund>();
            int maxQuality = Mathf.Max(1, recipe.m_item.m_itemData.m_shared.m_maxQuality);
            int levels = Mathf.Clamp(quality, 1, maxQuality);

            foreach (Piece.Requirement req in recipe.m_resources)
            {
                // Upgrader resources are a gamble with their own break rules, not a cost of the item.
                if (req == null || req.m_resItem == null || req.m_upgraderResource || !req.m_recover)
                {
                    continue;
                }

                int spent = 0;
                for (int level = 1; level <= levels; level++)
                {
                    spent += req.GetAmount(level);
                }

                int back = spent * percent / 100;
                if (back <= 0)
                {
                    continue;
                }

                // A recipe that lists the same material twice comes back as one line.
                int existing = result.FindIndex(m => m.Item.m_itemData.m_shared.m_name == req.m_resItem.m_itemData.m_shared.m_name);
                if (existing >= 0)
                {
                    Refund merged = result[existing];
                    merged.Amount += back;
                    result[existing] = merged;
                }
                else
                {
                    result.Add(new Refund { Item = req.m_resItem, Amount = back });
                }
            }

            return result;
        }

        /// <summary>Removes the item and hands its materials over. Returns false when nothing happened.</summary>
        internal static bool Deconstruct(Player player, ItemDrop.ItemData item, Recipe recipe)
        {
            string refusal = Refusal(player, item, recipe);
            if (refusal != null)
            {
                player?.Message(MessageHud.MessageType.Center, refusal);
                return false;
            }

            int percent = DeconstructSettings.Percent;
            List<Refund> materials = Returns(recipe, item.m_quality, percent);
            string itemName = item.m_shared.m_name;
            int quality = item.m_quality;
            bool cheated = item.m_cheated;

            Inventory inventory = player.GetInventory();
            if (!inventory.RemoveItem(item, recipe.m_amount))
            {
                return false;
            }

            int dropped = 0;
            foreach (Refund material in materials)
            {
                dropped += Give(player, material, cheated);
            }

            if (dropped > 0)
            {
                player.Message(MessageHud.MessageType.Center, "$inventory_full");
            }

            if (ModConfig.Verbose.Value)
            {
                var parts = new List<string>();
                foreach (Refund material in materials)
                {
                    parts.Add($"{material.Item.m_itemData.m_shared.m_name} x{material.Amount}");
                }

                GorilaChestModPlugin.Log.LogInfo(
                    $"Deconstructed {itemName} x{recipe.m_amount} at quality {quality}, {percent}%: {string.Join(", ", parts)}" +
                    (dropped > 0 ? $", {dropped} dropped on the ground" : "") + ".");
            }

            return true;
        }

        /// <summary>
        /// Puts a material into the inventory, and on the ground at the player's
        /// feet whatever does not fit. Inventory.AddItem(string, ...) is not used
        /// because its own overflow drop spawns a stack of one and loses the rest.
        /// Returns how many were dropped.
        /// </summary>
        private static int Give(Player player, Refund material, bool cheated)
        {
            // Unity objects fake their null, so no ?? here.
            GameObject prefab = ObjectDB.instance.GetItemPrefab(material.Item.gameObject.name);
            if (prefab == null)
            {
                prefab = material.Item.gameObject;
            }

            ItemDrop template = prefab.GetComponent<ItemDrop>();
            if (template == null)
            {
                GorilaChestModPlugin.Log.LogWarning($"No item prefab for {material.Item.gameObject.name}, skipped.");
                return 0;
            }

            Inventory inventory = player.GetInventory();
            int maxStack = Mathf.Max(1, template.m_itemData.m_shared.m_maxStackSize);
            int remaining = material.Amount;
            int dropped = 0;

            while (remaining > 0)
            {
                int chunk = Mathf.Min(remaining, maxStack);
                remaining -= chunk;

                ItemDrop.ItemData data = NewItem(template, prefab, chunk, cheated);
                int room = inventory.FindFreeStackSpace(data.m_shared.m_name, data.m_worldLevel) +
                           inventory.GetEmptySlots() * maxStack;
                int kept = Mathf.Min(room, chunk);

                if (kept > 0)
                {
                    inventory.AddItem(NewItem(template, prefab, kept, cheated));
                }

                if (chunk > kept)
                {
                    Transform t = player.transform;
                    ItemDrop.DropItem(data, chunk - kept, t.position + t.forward + Vector3.up, t.rotation);
                    dropped += chunk - kept;
                }
            }

            player.ShowPickupMessage(template.m_itemData, material.Amount);
            return dropped;
        }

        private static ItemDrop.ItemData NewItem(ItemDrop template, GameObject prefab, int stack, bool cheated)
        {
            ItemDrop.ItemData data = template.m_itemData.Clone();
            data.m_dropPrefab = prefab;
            data.m_stack = stack;
            data.m_durability = data.GetMaxDurability();
            data.m_worldLevel = (byte)Game.m_worldLevel;
            data.m_cheated = cheated;
            return data;
        }
    }
}
