using HarmonyLib;

namespace GorilaChestMod
{
    [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
    internal static class InventoryGuiOnDestroyPatch
    {
        private static void Postfix()
        {
            StackText.Forget();
            DeconstructTab.Destroy();
            DeconstructPanel.ClearPending();
        }
    }

    /// <summary>
    /// Reads the quick stack hotkey. There is no button: one lived beside the
    /// inventory for a while and kept landing over other panels, so the hotkey is
    /// the whole interface now.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdatePatch
    {
        private static void Postfix()
        {
            if (!ModConfig.QuickStackEnabled.Value || Player.m_localPlayer == null)
            {
                return;
            }

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
