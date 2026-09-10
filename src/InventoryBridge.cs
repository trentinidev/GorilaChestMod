using System.Collections.Generic;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Replacement targets for the Inventory calls that the transpilers redirect.
    /// Every method takes the original inventory as its first argument, so the
    /// redirected call sites keep the exact same stack layout.
    /// Nearby containers only ever extend the LOCAL player's inventory, and the
    /// player's own items are always spent first.
    /// </summary>
    internal static class InventoryBridge
    {
        internal static bool ShouldExtend(Inventory inventory)
        {
            if (inventory == null || ModConfig.Enabled == null || !ModConfig.Enabled.Value)
            {
                return false;
            }

            if (BuildContext.Active && !ModConfig.UseForBuilding.Value)
            {
                return false;
            }

            Player player = Player.m_localPlayer;
            return player != null && ReferenceEquals(inventory, player.GetInventory());
        }

        // Inventory.CountItems(string name, int quality = -1, bool matchWorldLevel = true)
        public static int CountItems(Inventory inventory, string name, int quality, bool matchWorldLevel)
        {
            int total = inventory.CountItems(name, quality, matchWorldLevel);
            if (!ShouldExtend(inventory))
            {
                return total;
            }

            List<Container> containers = ContainerTracker.GetNearby();
            for (int i = 0; i < containers.Count; i++)
            {
                Inventory chest = containers[i].GetInventory();
                if (chest != null)
                {
                    total += chest.CountItems(name, quality, matchWorldLevel);
                }
            }

            return total;
        }

        // Inventory.HaveItem(string name, bool matchWorldLevel = true)
        public static bool HaveItem(Inventory inventory, string name, bool matchWorldLevel)
        {
            if (inventory.HaveItem(name, matchWorldLevel))
            {
                return true;
            }

            if (!ShouldExtend(inventory))
            {
                return false;
            }

            List<Container> containers = ContainerTracker.GetNearby();
            for (int i = 0; i < containers.Count; i++)
            {
                Inventory chest = containers[i].GetInventory();
                if (chest != null && chest.HaveItem(name, matchWorldLevel))
                {
                    return true;
                }
            }

            return false;
        }

        // Inventory.GetItem(string name, int quality = -1, bool isPrefabName = false)
        public static ItemDrop.ItemData GetItem(Inventory inventory, string name, int quality, bool isPrefabName)
        {
            ItemDrop.ItemData item = inventory.GetItem(name, quality, isPrefabName);
            if (item != null || !ShouldExtend(inventory))
            {
                return item;
            }

            List<Container> containers = ContainerTracker.GetNearby();
            for (int i = 0; i < containers.Count; i++)
            {
                Inventory chest = containers[i].GetInventory();
                if (chest == null)
                {
                    continue;
                }

                item = chest.GetItem(name, quality, isPrefabName);
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        // Inventory.RemoveItem(string name, int amount, int itemQuality = -1, bool worldLevelBased = true)
        public static void RemoveItem(Inventory inventory, string name, int amount, int itemQuality, bool worldLevelBased)
        {
            if (!ShouldExtend(inventory))
            {
                inventory.RemoveItem(name, amount, itemQuality, worldLevelBased);
                return;
            }

            int remaining = amount;

            int fromPlayer = Mathf.Min(remaining, inventory.CountItems(name, itemQuality, worldLevelBased));
            if (fromPlayer > 0)
            {
                inventory.RemoveItem(name, fromPlayer, itemQuality, worldLevelBased);
                remaining -= fromPlayer;
            }

            if (remaining <= 0)
            {
                return;
            }

            List<Container> containers = ContainerTracker.GetNearby();
            for (int i = 0; i < containers.Count && remaining > 0; i++)
            {
                Container container = containers[i];
                Inventory chest = container.GetInventory();
                if (chest == null)
                {
                    continue;
                }

                int available = chest.CountItems(name, itemQuality, worldLevelBased);
                if (available <= 0)
                {
                    continue;
                }

                if (!ContainerTracker.TryTakeOwnership(container))
                {
                    GorilaChestModPlugin.Log.LogWarning(
                        $"Could not take ownership of '{container.m_name}', skipping it for {name}.");
                    continue;
                }

                int taken = Mathf.Min(available, remaining);

                // Removing fires Inventory.Changed, which makes the (now owned)
                // container write itself back to its ZDO.
                chest.RemoveItem(name, taken, itemQuality, worldLevelBased);
                remaining -= taken;

                if (ModConfig.Verbose.Value)
                {
                    GorilaChestModPlugin.Log.LogInfo($"Took {taken}x {name} from '{container.m_name}'.");
                }
            }

            if (remaining > 0)
            {
                GorilaChestModPlugin.Log.LogWarning(
                    $"Still missing {remaining}x {name} after checking the player and {containers.Count} container(s).");
            }
        }
    }
}
