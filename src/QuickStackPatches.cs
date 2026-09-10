using HarmonyLib;

namespace GorilaChestMod
{
    /// <summary>Creates the button once the inventory screen exists.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    internal static class InventoryGuiAwakePatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (ModConfig.QuickStackEnabled.Value)
            {
                QuickStackButton.Create(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
    internal static class InventoryGuiOnDestroyPatch
    {
        private static void Postfix()
        {
            QuickStackButton.Destroy();
        }
    }

    /// <summary>Keeps the button in sync with what is around the player, and reads the hotkey.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdatePatch
    {
        private static void Postfix()
        {
            if (!ModConfig.QuickStackEnabled.Value || Player.m_localPlayer == null)
            {
                return;
            }

            QuickStackButton.UpdateVisibility();

            if (!InventoryGui.IsVisible() || Console.IsVisible() || Chat.instance?.HasFocus() == true)
            {
                return;
            }

            if (ModConfig.QuickStackHotkey.Value.IsDown())
            {
                QuickStack.Run();
            }
        }
    }
}
