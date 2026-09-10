using BepInEx.Configuration;

namespace CraftFromChests
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> Range;
        internal static ConfigEntry<bool> UseForBuilding;
        internal static ConfigEntry<bool> IncludeVehicleContainers;
        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "General", "Enabled", true,
                "Master switch. When off the mod behaves exactly like the vanilla game.");

            Range = config.Bind(
                "General", "Range", 20f,
                new ConfigDescription(
                    "Radius in meters around the player that containers are pulled from.",
                    new AcceptableValueRange<float>(1f, 100f)));

            UseForBuilding = config.Bind(
                "General", "UseForBuilding", true,
                "Also pay for hammer/hoe/cultivator building costs from nearby containers.");

            IncludeVehicleContainers = config.Bind(
                "General", "IncludeVehicleContainers", true,
                "Include containers that belong to carts and ships.");

            Verbose = config.Bind(
                "Debug", "Verbose", false,
                "Log every container withdrawal and every Harmony call redirection to the BepInEx log.");
        }
    }
}
