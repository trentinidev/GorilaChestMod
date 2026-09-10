using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CraftFromChests
{
    /// <summary>
    /// Client-side plugin that lets the local player craft, upgrade and build using
    /// the contents of nearby containers as if they were in the player's own inventory.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class CraftFromChestsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.trentini.craftfromchests";
        public const string PluginName = "CraftFromChests";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(CraftFromChestsPlugin).Assembly);

            Log.LogInfo(
                $"{PluginName} {PluginVersion} loaded. Range: {ModConfig.Range.Value}m, " +
                $"building: {ModConfig.UseForBuilding.Value}, enabled: {ModConfig.Enabled.Value}.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            ContainerTracker.Clear();
        }
    }
}
