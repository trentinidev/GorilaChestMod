using System.Collections.Generic;
using UnityEngine;

namespace CraftFromChests
{
    /// <summary>
    /// Keeps track of every <see cref="Container"/> that spawned in the world and
    /// answers "which containers may the local player use right now?". Containers
    /// register themselves through a postfix on Container.Awake, so no
    /// FindObjectsOfType sweep is ever needed.
    /// </summary>
    internal static class ContainerTracker
    {
        /// <summary>The nearby list is rebuilt at most this often; the UI asks many times per frame.</summary>
        private const float CacheSeconds = 0.25f;

        private static readonly HashSet<Container> Known = new HashSet<Container>();
        private static readonly List<Container> Nearby = new List<Container>();

        private static float _nextRefresh;
        private static Vector3 _origin;

        internal static void Register(Container container)
        {
            if (container != null)
            {
                Known.Add(container);
            }
        }

        internal static void Clear()
        {
            Known.Clear();
            Nearby.Clear();
            _nextRefresh = 0f;
        }

        /// <summary>Containers the player may draw from, closest first. The list is reused, do not store it.</summary>
        internal static List<Container> GetNearby()
        {
            if (Time.time < _nextRefresh)
            {
                return Nearby;
            }

            _nextRefresh = Time.time + CacheSeconds;
            Refresh();
            return Nearby;
        }

        private static void Refresh()
        {
            Nearby.Clear();

            Player player = Player.m_localPlayer;
            if (player == null || Game.instance == null)
            {
                return;
            }

            Known.RemoveWhere(container => container == null);

            _origin = player.transform.position;
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            float range = ModConfig.Range.Value;
            float rangeSqr = range * range;

            foreach (Container container in Known)
            {
                if ((container.transform.position - _origin).sqrMagnitude > rangeSqr)
                {
                    continue;
                }

                if (IsUsable(container, playerId))
                {
                    Nearby.Add(container);
                }
            }

            Nearby.Sort(CompareByDistance);
        }

        private static int CompareByDistance(Container a, Container b)
        {
            float da = (a.transform.position - _origin).sqrMagnitude;
            float db = (b.transform.position - _origin).sqrMagnitude;
            return da.CompareTo(db);
        }

        private static bool IsUsable(Container container, long playerId)
        {
            if (container.GetInventory() == null)
            {
                return false;
            }

            if (!ModConfig.IncludeVehicleContainers.Value && container.m_rootObjectOverride != null)
            {
                return false;
            }

            ZNetView nview = GetNetView(container);
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            // Same rule the game applies when you try to open the chest.
            if (!HasPrivacyAccess(container, playerId))
            {
                return false;
            }

            if (container.m_checkGuardStone &&
                !PrivateArea.CheckAccess(container.transform.position, 0f, flash: false, wardCheck: false))
            {
                return false;
            }

            // Never touch a chest another player has open, their client owns that inventory.
            if (!nview.IsOwner() && nview.GetZDO() != null && nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1)
            {
                return false;
            }

            return true;
        }

        /// <summary>Mirrors the private Container.CheckAccess.</summary>
        private static bool HasPrivacyAccess(Container container, long playerId)
        {
            switch (container.m_privacy)
            {
                case Container.PrivacySetting.Public:
                    return true;
                case Container.PrivacySetting.Private:
                    Piece piece = container.GetComponent<Piece>();
                    return piece != null && piece.GetCreator() == playerId;
                default:
                    return false;
            }
        }

        /// <summary>Mirrors how Container.Awake resolves its own ZNetView.</summary>
        internal static ZNetView GetNetView(Container container)
        {
            return container.m_rootObjectOverride != null
                ? container.m_rootObjectOverride.GetComponent<ZNetView>()
                : container.GetComponent<ZNetView>();
        }

        /// <summary>
        /// Claims network ownership of the container. Only the owner may write the
        /// inventory back to the ZDO, and Container.OnContainerChanged saves it for
        /// us as soon as the inventory changes.
        /// </summary>
        internal static bool TryTakeOwnership(Container container)
        {
            ZNetView nview = GetNetView(container);
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            return nview.IsOwner();
        }
    }
}
