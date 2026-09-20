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
        /// <summary>
        /// While a saved chest is being rebuilt, the stack coming off disk sets a
        /// floor for the limit. Lowering ChestStackSize would otherwise trim
        /// chests that were filled under the old setting, and the excess would be
        /// gone for good. Chests never grow past the configured size, they are
        /// only allowed to keep what they already hold.
        /// </summary>
        internal static int LoadFloor;

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

            if (!StackSettings.Enabled)
            {
                return vanilla;
            }

            if (!ContainerTracker.IsContainerInventory(inventory))
            {
                return vanilla;
            }

            return Mathf.Max(vanilla, Mathf.Max(StackSettings.StackSize, LoadFloor));
        }

        /// <summary>The limit that applies to an item where it is sitting right now, chest or backpack.</summary>
        internal static int MaxStackForItem(ItemDrop.ItemData.SharedData shared, ItemDrop.ItemData item)
        {
            return MaxStackFor(shared, InventoryOf(item));
        }

        /// <summary>
        /// Which open inventory holds this item: the container on screen, or the
        /// player's own. Null when it is neither, which leaves the vanilla limit
        /// in place.
        /// </summary>
        internal static Inventory InventoryOf(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return null;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui != null && gui.ContainerGrid != null)
            {
                Inventory container = gui.ContainerGrid.GetInventory();
                if (container != null && container.ContainsItem(item))
                {
                    return container;
                }
            }

            Player player = Player.m_localPlayer;
            if (player != null)
            {
                Inventory backpack = player.GetInventory();
                if (backpack != null && backpack.ContainsItem(item))
                {
                    return backpack;
                }
            }

            return null;
        }

        /// <summary>
        /// Inventory.Changed is private, and it is what saves a chest back into
        /// its ZDO through Container.OnContainerChanged. Taking items straight out
        /// of a stack has to announce itself, otherwise the chest is written out
        /// still holding what was removed and the items come back.
        /// </summary>
        private static readonly System.Reflection.MethodInfo ChangedMethod =
            HarmonyLib.AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });

        internal static void NotifyChanged(Inventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            if (ChangedMethod == null)
            {
                GorilaChestModPlugin.Log.LogWarning("Inventory.Changed is gone, a chest may not save right away.");
                return;
            }

            ChangedMethod.Invoke(inventory, new object[] { false, false });
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
                int moved = MoveChunk(destination, item, chunk);

                item.m_stack -= moved;

                if (moved < chunk)
                {
                    // The destination ran out of room part way through.
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Copies up to <paramref name="chunk"/> items into the destination and
        /// reports how many actually landed there. The caller owns the source
        /// stack and subtracts what moved.
        ///
        /// The count matters: Inventory.AddItem merges into existing stacks one
        /// item at a time and only then looks for an empty slot, so a destination
        /// that fills up mid chunk keeps part of it and still answers false.
        /// Trusting that answer alone would either duplicate the part that moved
        /// or destroy the part that did not.
        /// </summary>
        internal static int MoveChunk(Inventory destination, ItemDrop.ItemData item, int chunk)
        {
            if (chunk <= 0)
            {
                return 0;
            }

            ItemDrop.ItemData piece = item.Clone();
            piece.m_stack = chunk;

            // The clone is within the destination limit, so this reaches the
            // vanilla path rather than coming back through the split prefix.
            if (destination.AddItem(piece))
            {
                return chunk;
            }

            // Whatever AddItem could not place stays in the clone, which it did not keep.
            return Mathf.Clamp(chunk - piece.m_stack, 0, chunk);
        }
    }
}
