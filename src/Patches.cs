using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CraftFromChests
{
    /// <summary>The vanilla calls that get redirected, resolved once.</summary>
    internal static class Swaps
    {
        private static readonly MethodInfo InventoryCountItems =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems),
                new[] { typeof(string), typeof(int), typeof(bool) });

        private static readonly MethodInfo InventoryHaveItem =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.HaveItem),
                new[] { typeof(string), typeof(bool) });

        private static readonly MethodInfo InventoryGetItem =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItem),
                new[] { typeof(string), typeof(int), typeof(bool) });

        private static readonly MethodInfo InventoryRemoveItem =
            AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItem),
                new[] { typeof(string), typeof(int), typeof(int), typeof(bool) });

        internal static readonly CallSwap CountItems = new CallSwap(
            InventoryCountItems, AccessTools.Method(typeof(InventoryBridge), nameof(InventoryBridge.CountItems)));

        internal static readonly CallSwap HaveItem = new CallSwap(
            InventoryHaveItem, AccessTools.Method(typeof(InventoryBridge), nameof(InventoryBridge.HaveItem)));

        internal static readonly CallSwap GetItem = new CallSwap(
            InventoryGetItem, AccessTools.Method(typeof(InventoryBridge), nameof(InventoryBridge.GetItem)));

        internal static readonly CallSwap RemoveItem = new CallSwap(
            InventoryRemoveItem, AccessTools.Method(typeof(InventoryBridge), nameof(InventoryBridge.RemoveItem)));
    }

    /// <summary>Containers announce themselves as they spawn.</summary>
    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class ContainerAwakePatch
    {
        private static void Postfix(Container __instance)
        {
            ContainerTracker.Register(__instance);
        }
    }

    // ---------------------------------------------------------------- crafting

    /// <summary>Decides whether a recipe can be crafted, and drives the craft button.</summary>
    [HarmonyPatch(typeof(Player), "HaveRequirementItems",
        new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    internal static class PlayerHaveRequirementItemsPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "Player.HaveRequirementItems", Swaps.CountItems);
        }
    }

    /// <summary>Used by recipes with m_requireOnlyOneIngredient (meads, feasts, ...).</summary>
    [HarmonyPatch]
    internal static class PlayerGetFirstRequiredItemPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.GetFirstRequiredItem));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "Player.GetFirstRequiredItem",
                Swaps.CountItems, Swaps.GetItem);
        }
    }

    /// <summary>Pays for a craft, an upgrade or a placed building piece.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    internal static class PlayerConsumeResourcesPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "Player.ConsumeResources", Swaps.RemoveItem);
        }
    }

    /// <summary>Pays the single ingredient of a m_requireOnlyOneIngredient recipe.</summary>
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting", new[] { typeof(Player) })]
    internal static class InventoryGuiDoCraftingPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "InventoryGui.DoCrafting", Swaps.RemoveItem);
        }
    }

    /// <summary>The "have / need" numbers under a recipe and under a building piece.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    internal static class InventoryGuiSetupRequirementPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "InventoryGui.SetupRequirement", Swaps.CountItems);
        }
    }

    // ---------------------------------------------------------------- building

    /// <summary>Decides whether a building piece can be placed, and greys out the build menu.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements),
        new[] { typeof(Piece), typeof(Player.RequirementMode) })]
    internal static class PlayerHaveRequirementsPiecePatch
    {
        private static void Prefix()
        {
            BuildContext.Enter();
        }

        private static void Finalizer()
        {
            BuildContext.Exit();
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(instructions, "Player.HaveRequirements(Piece)",
                Swaps.HaveItem, Swaps.CountItems);
        }
    }

    /// <summary>Marks the placement path as building, so ConsumeResources knows the context.</summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacement", new[] { typeof(bool), typeof(float) })]
    internal static class PlayerUpdatePlacementPatch
    {
        private static void Prefix()
        {
            BuildContext.Enter();
        }

        private static void Finalizer()
        {
            BuildContext.Exit();
        }
    }

    /// <summary>Marks the build hud requirement panel as building.</summary>
    [HarmonyPatch(typeof(Hud), "SetupPieceInfo", new[] { typeof(Piece) })]
    internal static class HudSetupPieceInfoPatch
    {
        private static void Prefix()
        {
            BuildContext.Enter();
        }

        private static void Finalizer()
        {
            BuildContext.Exit();
        }
    }
}
