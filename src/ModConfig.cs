using BepInEx.Configuration;
using UnityEngine;

namespace GorilaChestMod
{
    internal static class ModConfig
    {
        // ---- craft from chests
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> Range;
        internal static ConfigEntry<bool> UseForBuilding;
        internal static ConfigEntry<bool> IncludeVehicleContainers;

        // ---- oversized chest stacks
        internal static ConfigEntry<bool> ChestStacksEnabled;
        internal static ConfigEntry<int> ChestStackSize;

        // ---- quick stack
        internal static ConfigEntry<bool> QuickStackEnabled;
        internal static ConfigEntry<bool> QuickStackSkipHotbar;
        internal static ConfigEntry<bool> QuickStackButtonVisible;
        internal static ConfigEntry<Vector2> QuickStackButtonOffset;
        internal static ConfigEntry<KeyboardShortcut> QuickStackHotkey;
        internal static ConfigEntry<string> QuickStackButtonLabel;
        internal static ConfigEntry<bool> QuickStackHotkeyNeedsInventory;

        // ---- chest slot text
        internal static ConfigEntry<bool> ShrinkStackText;

        // ---- misc
        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "1 - Craft from chests", "Enabled", true,
                "Count the contents of nearby chests when crafting, upgrading and building.");

            Range = config.Bind(
                "1 - Craft from chests", "Range", 20f,
                new ConfigDescription(
                    "Radius in meters around the player that chests are pulled from. The quick stack button uses the same radius.",
                    new AcceptableValueRange<float>(1f, 100f)));

            UseForBuilding = config.Bind(
                "1 - Craft from chests", "UseForBuilding", true,
                "Also pay hammer, hoe and cultivator building costs from nearby chests.");

            IncludeVehicleContainers = config.Bind(
                "1 - Craft from chests", "IncludeVehicleContainers", true,
                "Include containers that belong to carts and ships.");

            ChestStacksEnabled = config.Bind(
                "2 - Chest stacks", "Enabled", true,
                "Let items stack far beyond their normal limit while they sit in a chest. " +
                "The player inventory keeps the vanilla limits.");

            ChestStackSize = config.Bind(
                "2 - Chest stacks", "ChestStackSize", 100000,
                new ConfigDescription(
                    "Stack size a chest slot may hold, 100000 by default. Items whose vanilla limit is already higher keep theirs, " +
                    "and items that do not stack at all, like weapons and armour, are never affected. " +
                    "On a server this value is handed to every client that connects.",
                    new AcceptableValueRange<int>(1, 100000)));

            QuickStackEnabled = config.Bind(
                "3 - Quick stack", "Enabled", true,
                "Enable the quick stack button and hotkey, which push matching items from your inventory into nearby chests.");

            QuickStackSkipHotbar = config.Bind(
                "3 - Quick stack", "SkipHotbar", true,
                "Leave the hotbar row alone, so your weapons, tools and food stay where they are.");

            QuickStackButtonVisible = config.Bind(
                "3 - Quick stack", "ShowButton", true,
                "Show the button in the inventory screen. Turn this off to use only the hotkey.");

            QuickStackButtonOffset = config.Bind(
                "3 - Quick stack", "ButtonOffset", new Vector2(0f, 0f),
                "Nudge the button, in pixels, from its default spot under the weight readout, " +
                "outside the inventory grid. Use this if it lands on top of something else in your resolution.");

            QuickStackHotkey = config.Bind(
                "3 - Quick stack", "Hotkey", new KeyboardShortcut(KeyCode.G),
                "Hotkey for quick stacking. Works with the inventory closed as well. G is free in the vanilla key list, " +
                "unlike V which the game uses for auto pickup. Set to None to use only the button.");

            QuickStackHotkeyNeedsInventory = config.Bind(
                "3 - Quick stack", "HotkeyNeedsInventory", false,
                "Require the inventory to be open before the hotkey does anything.");

            QuickStackButtonLabel = config.Bind(
                "3 - Quick stack", "ButtonLabel", "Stack to nearby chests",
                "Caption on the button. Change it if you play in another language.");

            ShrinkStackText = config.Bind(
                "2 - Chest stacks", "ShrinkStackText", true,
                "Keep the amount label on a slot readable when the numbers get long: a large limit is written as 100k, and the font shrinks from there. Turn off for the vanilla label.");

            Verbose = config.Bind(
                "4 - Debug", "Verbose", false,
                "Log every chest withdrawal, every quick stack move and every patched call to the BepInEx log.");
        }
    }
}
