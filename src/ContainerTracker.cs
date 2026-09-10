using System.Collections.Generic;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Keeps track of every <see cref="Container"/> that spawned in the world.
    /// Containers register themselves through a postfix on Container.Awake, so no
    /// FindObjectsOfType sweep is ever needed.
    ///
    /// Two questions are answered here: which chests may the local player use
    /// right now, and is a given inventory a chest inventory. The second one runs
    /// on a dedicated server too, where there is no local player at all.
    /// </summary>
    internal static class ContainerTracker
    {
        /// <summary>The nearby list is rebuilt at most this often; the UI asks many times per frame.</summary>
        private const float NearbyCacheSeconds = 0.25f;

        /// <summary>How often destroyed containers are swept out of the registry.</summary>
        private const float PruneSeconds = 5f;

        private static readonly HashSet<Container> Known = new HashSet<Container>();
        private static readonly HashSet<Inventory> ContainerInventories = new HashSet<Inventory>();
        private static readonly List<Container> Nearby = new List<Container>();

        private static float _nextRefresh;
        private static float _nextPrune;
        private static Vector3 _origin;

        internal static void Register(Container container)
        {
            if (container == null)
            {
                return;
            }

            Known.Add(container);

            Inventory inventory = container.GetInventory();
            if (inventory != null)
            {
                ContainerInventories.Add(inventory);
            }
        }

        internal static void Clear()
        {
            Known.Clear();
            ContainerInventories.Clear();
            Nearby.Clear();
            _nextRefresh = 0f;
            _nextPrune = 0f;
        }

        /// <summary>True when this inventory belongs to a chest, a cart, a ship or any other container.</summary>
        internal static bool IsContainerInventory(Inventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            Prune();
            return ContainerInventories.Contains(inventory);
        }

        /// <summary>Chests the player may draw from, closest first. The list is reused, do not store it.</summary>
        internal static List<Container> GetNearby()
        {
            if (Time.time < _nextRefresh)
            {
                return Nearby;
            }

            _nextRefresh = Time.time + NearbyCacheSeconds;
            Refresh();
            return Nearby;
        }

        /// <summary>Drops destroyed containers, and the inventories that went with them.</summary>
        private static void Prune()
        {
            if (Time.time < _nextPrune)
            {
                return;
            }

            _nextPrune = Time.time + PruneSeconds;

            int before = Known.Count;
            Known.RemoveWhere(container => container == null);
            if (Known.Count == before)
            {
                return;
            }

            ContainerInventories.Clear();
            foreach (Container container in Known)
            {
                Inventory inventory = container.GetInventory();
                if (inventory != null)
                {
                    ContainerInventories.Add(inventory);
                }
            }
        }

        private static void Refresh()
        {
            Nearby.Clear();

            Player player = Player.m_localPlayer;
            if (player == null || Game.instance == null)
            {
                return;
            }

            Prune();

            _origin = player.transform.position;
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            float range = ModConfig.Range.Value;
            float rangeSqr = range * range;

            foreach (Container container in Known)
            {
                if (container == null)
                {
                    continue;
                }

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
