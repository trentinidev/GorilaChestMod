using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Writes the vanilla stack limits to the log once the item database is
    /// populated. Every item carries its own limit in its prefab, so this is the
    /// only honest way to see what the mod is actually raising: which limits exist
    /// in this build of the game, and how many items sit on each one.
    /// Verbose only, and only once per session.
    /// </summary>
    internal static class StackReport
    {
        private static bool _reported;

        internal static void Report()
        {
            if (_reported || !ModConfig.Verbose.Value || ObjectDB.instance == null)
            {
                return;
            }

            List<GameObject> items = ObjectDB.instance.m_items;
            if (items == null || items.Count == 0)
            {
                return;
            }

            Dictionary<int, int> byLimit = new Dictionary<int, int>();
            List<string> samples = new List<string>();

            foreach (GameObject prefab in items)
            {
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop == null || drop.m_itemData?.m_shared == null)
                {
                    continue;
                }

                int limit = drop.m_itemData.m_shared.m_maxStackSize;
                byLimit[limit] = byLimit.TryGetValue(limit, out int count) ? count + 1 : 1;

                if (samples.Count < 8 && limit > 1)
                {
                    samples.Add($"{prefab.name} {limit}");
                }
            }

            if (byLimit.Count == 0)
            {
                return;
            }

            _reported = true;

            string spread = string.Join(", ", byLimit.OrderBy(p => p.Key).Select(p => $"{p.Key} -> {p.Value} item(s)").ToArray());
            GorilaChestModPlugin.Log.LogInfo($"Vanilla stack limits in this build: {spread}.");
            GorilaChestModPlugin.Log.LogInfo($"Examples: {string.Join(", ", samples.ToArray())}.");
            GorilaChestModPlugin.Log.LogInfo(
                $"Inside chests those become {StackSettings.StackSize}, except where the vanilla limit is already higher.");
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDbAwakePatch
    {
        private static void Postfix()
        {
            StackReport.Report();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    internal static class ObjectDbCopyPatch
    {
        private static void Postfix()
        {
            StackReport.Report();
        }
    }
}
