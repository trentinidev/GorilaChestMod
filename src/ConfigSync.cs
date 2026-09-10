using HarmonyLib;

namespace GorilaChestMod
{
    /// <summary>
    /// Hands the chest stack settings from the server to every client that has the
    /// mod. Those two values have to match on both sides: whoever writes a chest
    /// back to its ZDO decides how large its stacks may be, so a client that
    /// believes in a smaller limit would trim the excess away.
    ///
    /// Everything else stays a personal setting, since craft range and quick stack
    /// only ever affect the player who is using them.
    /// </summary>
    internal static class ConfigSync
    {
        private const string RequestRpc = "GorilaChestMod_ConfigRequest";
        private const string ConfigRpc = "GorilaChestMod_Config";

        private static bool _registered;

        /// <summary>Set while the values came from a server, so we do not fight the config file.</summary>
        internal static bool ServerEnforced { get; private set; }

        internal static void Register()
        {
            if (_registered || ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.Register(RequestRpc, new System.Action<long>(OnConfigRequested));
            ZRoutedRpc.instance.Register(ConfigRpc, new System.Action<long, ZPackage>(OnConfigReceived));
            _registered = true;
        }

        internal static void Reset()
        {
            _registered = false;
            ServerEnforced = false;
        }

        /// <summary>Called on a client once it is connected, asks the server for its settings.</summary>
        internal static void RequestFromServer()
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc);

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo("Asked the server for its chest stack settings.");
            }
        }

        private static void OnConfigRequested(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            ZPackage package = new ZPackage();
            package.Write(ModConfig.ChestStacksEnabled.Value);
            package.Write(ModConfig.ChestStackSize.Value);

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ConfigRpc, package);

            GorilaChestModPlugin.Log.LogInfo(
                $"Sent chest stack settings to peer {sender}: enabled {ModConfig.ChestStacksEnabled.Value}, size {ModConfig.ChestStackSize.Value}.");
        }

        private static void OnConfigReceived(long sender, ZPackage package)
        {
            // A server never takes orders from a client.
            if (ZNet.instance == null || ZNet.instance.IsServer() || package == null)
            {
                return;
            }

            bool enabled = package.ReadBool();
            int size = package.ReadInt();

            ModConfig.ChestStacksEnabled.Value = enabled;
            ModConfig.ChestStackSize.Value = size;
            ServerEnforced = true;

            GorilaChestModPlugin.Log.LogInfo(
                $"Server set chest stacks to enabled {enabled}, size {size}. Your local values for those two are ignored while connected.");
        }
    }

    [HarmonyPatch(typeof(ZNet), "Awake")]
    internal static class ZNetAwakePatch
    {
        private static void Postfix()
        {
            ConfigSync.Reset();
            ConfigSync.Register();
        }
    }

    /// <summary>A client reaches this once the server's peer info has arrived, which is late enough to talk back.</summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class ZNetPeerInfoPatch
    {
        private static void Postfix()
        {
            ConfigSync.Register();
            ConfigSync.RequestFromServer();
        }
    }

    [HarmonyPatch(typeof(ZNet), "Shutdown")]
    internal static class ZNetShutdownPatch
    {
        private static void Postfix()
        {
            ConfigSync.Reset();
            ContainerTracker.Clear();
        }
    }
}
