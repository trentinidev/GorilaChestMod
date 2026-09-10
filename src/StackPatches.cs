using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace GorilaChestMod
{
    internal static class StackFields
    {
        internal static readonly FieldInfo MaxStackSize =
            AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_maxStackSize));

        internal static readonly MethodInfo MaxStackFor =
            AccessTools.Method(typeof(ChestStacks), nameof(ChestStacks.MaxStackFor));

        internal static readonly FieldInfo GridInventory =
            AccessTools.Field(typeof(InventoryGrid), "m_inventory");

        /// <summary>Every method of a type, including its compiler generated nested ones, that reads the stack limit.</summary>
        internal static IEnumerable<MethodBase> MethodsReadingMaxStack(Type type)
        {
            List<Type> types = new List<Type> { type };
            types.AddRange(type.GetNestedTypes(AccessTools.all));

            foreach (Type candidate in types)
            {
                foreach (MethodBase method in AccessTools.GetDeclaredMethods(candidate).Cast<MethodBase>())
                {
                    if (method.IsAbstract || method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (ReadsMaxStack(method))
                    {
                        yield return method;
                    }
                }
            }
        }

        private static bool ReadsMaxStack(MethodBase method)
        {
            try
            {
                if (method.GetMethodBody() == null)
                {
                    return false;
                }

                return PatchProcessor.GetOriginalInstructions(method).Any(instruction =>
                    instruction.opcode == OpCodes.Ldfld &&
                    instruction.operand is FieldInfo field &&
                    CallRedirector.SameMember(field, MaxStackSize));
            }
            catch (Exception e)
            {
                GorilaChestModPlugin.Log.LogWarning($"Could not read the body of {method.DeclaringType?.Name}.{method.Name}: {e.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Every place inside Inventory that asks how large a stack may be now asks per
    /// inventory, so a chest can hold far more of an item than a backpack can.
    /// The set of methods is discovered from the game's own IL rather than hard
    /// coded, and logged on startup.
    /// </summary>
    [HarmonyPatch]
    internal static class InventoryStackLimitPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> methods = StackFields.MethodsReadingMaxStack(typeof(Inventory)).ToList();

            if (methods.Count == 0)
            {
                GorilaChestModPlugin.Log.LogError(
                    "No Inventory method reads m_maxStackSize any more. The game code changed, chest stacks are inactive.");
            }
            else
            {
                GorilaChestModPlugin.Log.LogInfo(
                    "Chest stacks hooked into Inventory: " + string.Join(", ", methods.Select(m => m.Name).Distinct().ToArray()));
            }

            return methods;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Inside Inventory, "this" is the inventory the limit applies to.
            return CallRedirector.ReplaceFieldReads(
                instructions,
                "Inventory." + original.Name,
                StackFields.MaxStackSize,
                StackFields.MaxStackFor,
                () => new[] { new CodeInstruction(OpCodes.Ldarg_0) },
                out int _);
        }
    }

    /// <summary>Shows "800 / 1000" instead of "800 / 50" on the slots of a chest.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGridStackTextPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            IEnumerable<CodeInstruction> result = CallRedirector.ReplaceFieldReads(
                instructions,
                "InventoryGrid.UpdateGui",
                StackFields.MaxStackSize,
                StackFields.MaxStackFor,
                () => new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, StackFields.GridInventory)
                },
                out int replaced);

            if (replaced == 0)
            {
                GorilaChestModPlugin.Log.LogWarning(
                    "InventoryGrid.UpdateGui no longer reads m_maxStackSize, chest slots will show the vanilla limit.");
            }

            return result;
        }
    }

    /// <summary>
    /// Splits an oversized stack when it lands somewhere that cannot hold it, which
    /// is what happens when a chest stack is shift clicked or taken into the player
    /// inventory. Without this the oversized stack would simply move across.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
    internal static class InventoryAddItemSplitPatch
    {
        private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (item == null || item.m_shared == null)
            {
                return true;
            }

            int max = ChestStacks.MaxStackFor(item.m_shared, __instance);
            if (max <= 1 || item.m_stack <= max)
            {
                return true;
            }

            __result = ChestStacks.AddInChunks(__instance, item, max);

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"Split an oversized stack of {item.m_shared.m_name} into chunks of {max}, leftover {item.m_stack}.");
            }

            return false;
        }
    }

    /// <summary>
    /// The same protection for the slot targeted version, used when dragging a
    /// stack onto a specific square. Only a vanilla sized portion is moved and the
    /// caller is told the move was partial, so the rest stays in the chest.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "AddItem",
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    internal static class InventoryAddItemAtSlotClampPatch
    {
        private static void Prefix(Inventory __instance, ItemDrop.ItemData item, ref int amount, out bool __state)
        {
            __state = false;

            if (item == null || item.m_shared == null)
            {
                return;
            }

            int max = ChestStacks.MaxStackFor(item.m_shared, __instance);
            if (max <= 1)
            {
                return;
            }

            int wanted = Mathf.Min(amount, item.m_stack);
            if (wanted <= max)
            {
                return;
            }

            amount = max;
            __state = true;
        }

        private static void Postfix(ref bool __result, bool __state)
        {
            if (__state)
            {
                // Something was left behind, so this was not a complete move.
                __result = false;
            }
        }
    }

    /// <summary>
    /// Keeps oversized stacks out of the world. A destroyed chest drops its
    /// contents, and one item drop holding 1000 wood would hand that stack to
    /// whoever picks it up, so it becomes several vanilla sized drops instead.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
    internal static class ItemDropSplitPatch
    {
        private static bool Prefix(ItemDrop.ItemData item, int amount, Vector3 position, Quaternion rotation, ref ItemDrop __result)
        {
            if (item == null || item.m_shared == null)
            {
                return true;
            }

            int max = item.m_shared.m_maxStackSize;
            int total = amount > 0 ? amount : item.m_stack;

            if (max <= 1 || total <= max)
            {
                return true;
            }

            ItemDrop last = null;
            int left = total;
            while (left > 0)
            {
                int chunk = Mathf.Min(max, left);

                // Each chunk is within the vanilla limit, so this reaches the
                // original method rather than coming back through this prefix.
                last = ItemDrop.DropItem(item, chunk, position + UnityEngine.Random.insideUnitSphere * 0.3f, rotation);
                left -= chunk;
            }

            __result = last;

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo($"Dropped {total}x {item.m_shared.m_name} as stacks of {max}.");
            }

            return false;
        }
    }
}
