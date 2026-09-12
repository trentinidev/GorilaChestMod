using System.Collections.Generic;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Pushes items from the player inventory into the chests around them, the way
    /// a quick stack button works in other survival games. An item only moves into
    /// a chest that already holds that item, so chests keep the layout you gave
    /// them and nothing lands in a random empty one.
    /// </summary>
    internal static class QuickStack
    {
        /// <summary>Runs a quick stack and tells the player what happened. Returns how many items moved.</summary>
        internal static int Run()
        {
            Player player = Player.m_localPlayer;
            if (player == null || !ModConfig.QuickStackEnabled.Value)
            {
                return 0;
            }

            Inventory playerInventory = player.GetInventory();
            List<Container> containers = ContainerTracker.GetNearby();
            if (containers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_stackall_none");
                return 0;
            }

            // Counting the whole inventory before and after is the same trick the
            // game's own stack all uses, and it survives items being merged away.
            int before = playerInventory.CountItems(null);

            // The list is rebuilt as items leave, so work on a copy.
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(playerInventory.GetAllItems());
            List<Container> touched = new List<Container>();

            foreach (ItemDrop.ItemData item in items)
            {
                if (!CanMove(player, playerInventory, item))
                {
                    continue;
                }

                foreach (Container container in containers)
                {
                    if (item.m_stack <= 0 || !playerInventory.ContainsItem(item))
                    {
                        break;
                    }

                    Inventory chest = container.GetInventory();
                    if (chest == null || !chest.ContainsItemByName(item.m_shared.m_name))
                    {
                        continue;
                    }

                    if (!ContainerTracker.TryTakeOwnership(container))
                    {
                        continue;
                    }

                    chest.MoveItemToThis(playerInventory, item);

                    if (!touched.Contains(container))
                    {
                        touched.Add(container);
                    }

                    if (ModConfig.Verbose.Value)
                    {
                        GorilaChestModPlugin.Log.LogInfo(
                            $"Quick stacked {item.m_shared.m_name} into '{container.m_name}'.");
                    }
                }
            }

            int moved = before - playerInventory.CountItems(null);

            if (moved > 0)
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_stackall") + " " + moved);

                if (InventoryGui.instance != null)
                {
                    InventoryGui.instance.m_moveItemEffects.Create(player.transform.position, Quaternion.identity);
                }
            }
            else
            {
                player.Message(MessageHud.MessageType.Center, "$msg_stackall_none");
            }

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo($"Quick stack moved {moved} item(s) into {touched.Count} chest(s).");
            }

            return moved;
        }

        private static bool CanMove(Player player, Inventory playerInventory, ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            if (item.m_equipped || player.IsItemEquiped(item))
            {
                return false;
            }

            // A stack you marked stays where you put it.
            if (Favorites.IsFavorite(item))
            {
                return false;
            }

            // Quest items are bound to the player, they have no business in a chest.
            if (item.m_shared.m_questItem)
            {
                return false;
            }

            if (ModConfig.QuickStackSkipHotbar.Value && item.m_gridPos.y == 0)
            {
                return false;
            }

            return playerInventory.ContainsItem(item);
        }
    }
}
