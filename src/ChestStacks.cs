using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Decides how large a stack may be, per inventory. Chest inventories get the
    /// configured size, everything else keeps the vanilla limit.
    ///
    /// The game reads ItemData.m_shared.m_maxStackSize as a plain field in a
    /// handful of places inside Inventory and InventoryGrid. A transpiler swaps
    /// each of those field reads for a call to <see cref="MaxStackFor"/>, passing
    /// the inventory that is being worked on, so the same item type can hold 1000
    /// in a chest and the vanilla 50 in a backpack.
    ///
    /// This runs on a dedicated server as well. A server that loads a chest into
    /// memory would otherwise clamp oversized stacks back to the vanilla limit and
    /// destroy the excess the next time it wrote the chest out.
    /// </summary>
    internal static class ChestStacks
    {
        internal static int MaxStackFor(ItemDrop.ItemData.SharedData shared, Inventory inventory)
        {
            if (shared == null)
            {
                return 1;
            }

            int vanilla = shared.m_maxStackSize;

            // Weapons, armour and anything else that does not stack stays that way.
            if (vanilla <= 1)
            {
                return vanilla;
            }

            if (ModConfig.ChestStacksEnabled == null || !ModConfig.ChestStacksEnabled.Value)
            {
                return vanilla;
            }

            if (!ContainerTracker.IsContainerInventory(inventory))
            {
                return vanilla;
            }

            return Mathf.Max(vanilla, ModConfig.ChestStackSize.Value);
        }

        /// <summary>
        /// Splits an oversized stack across several slots of the destination.
        /// Taking 1000 wood out of a chest has to become twenty vanilla stacks of
        /// 50, otherwise the oversized stack leaks into the player inventory.
        /// Returns true when the whole stack made it across.
        /// </summary>
        internal static bool AddInChunks(Inventory destination, ItemDrop.ItemData item, int chunkSize)
        {
            while (item.m_stack > 0)
            {
                int chunk = Mathf.Min(chunkSize, item.m_stack);

                ItemDrop.ItemData piece = item.Clone();
                piece.m_stack = chunk;

                // The clone is within the destination limit, so this reaches the
                // vanilla path rather than coming back through the split prefix.
                if (!destination.AddItem(piece))
                {
                    return false;
                }

                item.m_stack -= chunk;
            }

            return true;
        }
    }
}
