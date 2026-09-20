using System.Collections.Generic;
using HarmonyLib;

namespace GorilaChestMod
{
    /// <summary>Builds the tab once the inventory screen exists.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    internal static class InventoryGuiAwakePatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            DeconstructTab.Create(__instance);
        }
    }

    /// <summary>Shows or hides the tab for the station in use, before vanilla builds the list for the selected tab.</summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) })]
    internal static class InventoryGuiUpdateCraftingPanelPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            DeconstructTab.BeforePanelUpdate(__instance, Player.m_localPlayer);
        }
    }

    /// <summary>Lists the items that can be taken apart instead of recipes or upgrades.</summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList", new[] { typeof(List<Recipe>) })]
    internal static class InventoryGuiUpdateRecipeListPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!DeconstructTab.Active)
            {
                return true;
            }

            DeconstructPanel.BuildList(__instance);
            return false;
        }
    }

    /// <summary>Rewrites the detail panel after vanilla filled it in as if for an upgrade.</summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe", new[] { typeof(Player), typeof(float) })]
    internal static class InventoryGuiUpdateRecipePatch
    {
        private static void Postfix(InventoryGui __instance, Player player)
        {
            if (DeconstructTab.Active)
            {
                DeconstructPanel.UpdateDetails(__instance, player);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
    internal static class InventoryGuiOnCraftPressedPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!DeconstructTab.Active)
            {
                DeconstructPanel.ClearPending();
                return true;
            }

            DeconstructPanel.Begin(__instance);
            return false;
        }
    }

    /// <summary>The craft timer ran out: deconstruct if that timer was ours.</summary>
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting", new[] { typeof(Player) })]
    internal static class InventoryGuiDoCraftingDeconstructPatch
    {
        private static bool Prefix(InventoryGui __instance, Player player)
        {
            return !DeconstructPanel.Finish(__instance, player);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
    internal static class InventoryGuiOnTabCraftPressedPatch
    {
        private static void Prefix()
        {
            DeconstructTab.Deselect();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
    internal static class InventoryGuiOnTabUpgradePressedPatch
    {
        private static void Prefix()
        {
            DeconstructTab.Deselect();
        }
    }
}
