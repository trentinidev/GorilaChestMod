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
            StackText.Forget();
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

            if (!ModConfig.QuickStackHotkey.Value.IsDown() || !CanTakeHotkey())
            {
                return;
            }

            QuickStack.Run();
        }

        /// <summary>
        /// The hotkey works out in the world, not only over an open inventory, so
        /// everything that swallows keyboard input has to be ruled out by hand.
        /// </summary>
        private static bool CanTakeHotkey()
        {
            if (ModConfig.QuickStackHotkeyNeedsInventory.Value && !InventoryGui.IsVisible())
            {
                return false;
            }

            if (Menu.IsVisible() || Console.IsVisible() || TextInput.IsVisible())
            {
                return false;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return false;
            }

            return !Player.m_localPlayer.IsDead();
        }
    }
}
