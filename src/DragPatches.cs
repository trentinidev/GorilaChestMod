using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Moving an oversized stack with the mouse, safely.
    ///
    /// InventoryGrid.DropItem has two paths. When the target slot holds a
    /// different item it swaps the two, and it does that by taking the dragged
    /// stack out of its inventory first, trusting that the whole stack will land
    /// on the other side. That assumption breaks for a chest stack larger than the
    /// player inventory may hold: the clamp in <see cref="InventoryAddItemAtSlotClampPatch"/>
    /// moves one vanilla stack and the rest is left in an item that no inventory
    /// owns any more, so it is gone at the next save.
    ///
    /// So an oversized move never reaches that path. It is carried out here
    /// instead: one vanilla sized stack lands on the slot that was aimed at and
    /// the rest is spread over the free slots of the same inventory, which is also
    /// what stops a thousand wood from needing twenty clicks to come out.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    internal static class InventoryGridDropItemPatch
    {
        private static bool Prefix(
            InventoryGrid __instance,
            Inventory fromInventory,
            ItemDrop.ItemData item,
            int amount,
            Vector2i pos,
            ref bool __result)
        {
            Inventory destination = __instance.GetInventory();

            if (destination == null || fromInventory == null || item == null || item.m_shared == null)
            {
                return true;
            }

            // Rearranging inside one inventory keeps one limit, vanilla is right.
            if (ReferenceEquals(destination, fromInventory))
            {
                return true;
            }

            int max = ChestStacks.MaxStackFor(item.m_shared, destination);
            if (max <= 1)
            {
                return true;
            }

            int wanted = Mathf.Min(amount, item.m_stack);
            if (wanted <= max)
            {
                // Fits under the destination's own limit, nothing to protect.
                return true;
            }

            ItemDrop.ItemData target = destination.GetItemAt(pos.x, pos.y);
            if (target != null && !target.IsSameType(item))
            {
                // This is the swap path, and a swap moves whole stacks. Refuse it
                // rather than losing the part that does not fit.
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    player.Message(MessageHud.MessageType.Center,
                        $"Stack too large to swap, move {max} or fewer at a time");
                }

                __result = false;
                return false;
            }

            __result = MoveOversized(fromInventory, destination, item, wanted, pos, max);
            return false;
        }

        /// <summary>
        /// Fills the targeted slot with one vanilla sized stack, then keeps going
        /// into the free slots. Returns true once everything that was asked for has
        /// moved, which is what tells the UI to let go of the item.
        /// </summary>
        private static bool MoveOversized(
            Inventory from, Inventory to, ItemDrop.ItemData item, int wanted, Vector2i pos, int max)
        {
            int before = item.m_stack;

            // The slot that was aimed at, through the game's own move so the
            // source is emptied and both inventories are marked as changed.
            to.MoveItemToThis(from, item, Mathf.Min(wanted, max), pos.x, pos.y);

            int remaining = Mathf.Min(wanted - (before - item.m_stack), item.m_stack);

            while (remaining > 0)
            {
                int chunk = Mathf.Min(max, remaining);
                int moved = ChestStacks.MoveChunk(to, item, chunk);

                if (moved <= 0)
                {
                    break;
                }

                item.m_stack -= moved;
                remaining -= moved;

                if (item.m_stack <= 0)
                {
                    from.RemoveItem(item);
                    break;
                }
            }

            // Items were taken straight out of the source stack above, so both
            // sides are told, which is what writes a chest back into its ZDO.
            ChestStacks.NotifyChanged(from);
            ChestStacks.NotifyChanged(to);

            int total = before - item.m_stack;

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"Moved {total} of {wanted}x {item.m_shared.m_name} out in stacks of {max}, {item.m_stack} left behind.");
            }

            if (total < wanted && total > 0)
            {
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    player.Message(MessageHud.MessageType.Center, "$inventory_full");
                }
            }

            return total >= wanted;
        }
    }

    /// <summary>
    /// The slot a dragged stack may be dropped on lights up, and the game decides
    /// that with ItemData.GetSpaceLeftInStack, which knows nothing about which
    /// inventory the item sits in and so answers with the vanilla limit. A chest
    /// slot past that limit stopped lighting up even though dropping there works.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "CanDropDragOntoItem")]
    internal static class InventoryGuiCanDropDragOntoItemPatch
    {
        private static readonly FieldInfo DragItem = AccessTools.Field(typeof(InventoryGui), "m_dragItem");

        private static bool Prefix(InventoryGui __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (DragItem == null || item == null || item.m_shared == null)
            {
                return true;
            }

            ItemDrop.ItemData dragged = DragItem.GetValue(__instance) as ItemDrop.ItemData;
            if (dragged == null || !item.IsSameType(dragged))
            {
                return true;
            }

            Inventory owner = ChestStacks.InventoryOf(item);
            if (owner == null)
            {
                return true;
            }

            __result = ChestStacks.MaxStackFor(item.m_shared, owner) - item.m_stack > 0;
            return false;
        }
    }
}
