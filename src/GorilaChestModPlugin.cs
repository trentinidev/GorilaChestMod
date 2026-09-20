using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GorilaChestMod
{
    /// <summary>
    /// Chest quality of life for Valheim, in three parts:
    /// craft and build straight out of nearby chests, oversized stacks inside
    /// chests, and a quick stack button that dumps matching items into them.
    ///
    /// Loads on the client and on a dedicated server. The server half exists so
    /// that a server holding a chest does not clamp oversized stacks back to the
    /// vanilla limit, and so it can hand its stack settings to connecting clients.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class GorilaChestModPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.trentini.gorilachestmod";
        public const string PluginName = "GorilaChestMod";

        /// <summary>Set by the Version property in the csproj, generated at build time.</summary>
        public const string PluginVersion = BuildInfo.Version;

        internal static ManualLogSource Log;

        private Harmony _harmony;

        internal static bool IsDedicatedServer =>
            ZNet.instance != null ? ZNet.instance.IsDedicated() : UnityEngine.Application.isBatchMode;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(GorilaChestModPlugin).Assembly);

            Log.LogInfo(
                $"{PluginName} {PluginVersion} loaded. " +
                $"Craft range {ModConfig.Range.Value}m, building {ModConfig.UseForBuilding.Value}, " +
                $"chest stacks {StackSettings.Describe()}, " +
                $"quick stack {ModConfig.QuickStackEnabled.Value}.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            ContainerTracker.Clear();
        }
    }
}
