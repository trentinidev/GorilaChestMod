using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Keeps the amount label readable once a chest slot holds six digit numbers.
    /// The game writes "stack/limit" into a label sized for "50/50", so at
    /// "100000/100000" the text spills across the neighbouring slots.
    ///
    /// Two things happen here. A large limit is written in short form, 100000
    /// becomes 100k, which is where most of the width went. Whatever is left over
    /// then shrinks the font as the text grows, and the original size comes back on
    /// short text.
    /// </summary>
    internal static class StackText
    {
        /// <summary>Text at or below this length keeps the original font size.</summary>
        private const int ComfortableLength = 6;

        /// <summary>Never shrink past this fraction of the original size.</summary>
        private const float MinScale = 0.35f;

        /// <summary>Limits below this stay written out in full.</summary>
        private const int CompactFrom = 1000;

        private static readonly Dictionary<TMP_Text, float> OriginalSizes = new Dictionary<TMP_Text, float>();

        private static bool _announced;

        internal static void Fit(TMP_Text label)
        {
            if (label == null || !label.enabled || string.IsNullOrEmpty(label.text))
            {
                return;
            }

            string text = Shorten(label.text);
            if (text != label.text)
            {
                label.text = text;
            }

            if (!OriginalSizes.TryGetValue(label, out float original))
            {
                original = label.fontSize;
                OriginalSizes[label] = original;
            }

            float scale = text.Length <= ComfortableLength
                ? 1f
                : Mathf.Max(MinScale, (float)ComfortableLength / text.Length);

            float wanted = original * scale;
            if (Mathf.Approximately(label.fontSize, wanted))
            {
                return;
            }

            label.enableAutoSizing = false;
            label.fontSize = wanted;

            if (!_announced)
            {
                _announced = true;
                GorilaChestModPlugin.Log.LogInfo(
                    "Slot labels are being fitted, first one was '" + text + "' at " +
                    wanted.ToString("0.#", CultureInfo.InvariantCulture) + " instead of " +
                    original.ToString("0.#", CultureInfo.InvariantCulture) + ".");
            }
        }

        /// <summary>Turns "650/100000" into "650/100k" and leaves everything else alone.</summary>
        private static string Shorten(string text)
        {
            int slash = text.LastIndexOf('/');
            if (slash <= 0 || slash == text.Length - 1)
            {
                return text;
            }

            string tail = text.Substring(slash + 1);
            if (!int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit) || limit < CompactFrom)
            {
                return text;
            }

            return text.Substring(0, slash + 1) + Compact(limit);
        }

        private static string Compact(int value)
        {
            if (value >= 1000000)
            {
                return Trim(value / 1000000f) + "M";
            }

            return Trim(value / 1000f) + "k";
        }

        private static string Trim(float value)
        {
            return value >= 10f || Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        internal static void Forget()
        {
            OriginalSizes.Clear();
            _announced = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGridStackTextSizePatch
    {
        // InventoryElement is a top level class, only the list holding them is private.
        private static readonly FieldInfo Elements = AccessTools.Field(typeof(InventoryGrid), "m_elements");

        private static void Postfix(InventoryGrid __instance)
        {
            if (!ModConfig.ShrinkStackText.Value)
            {
                return;
            }

            if (Elements == null)
            {
                GorilaChestModPlugin.Log.LogError("InventoryGrid.m_elements is gone, slot labels are left at their vanilla size.");
                return;
            }

            if (!(Elements.GetValue(__instance) is List<InventoryElement> elements))
            {
                return;
            }

            foreach (InventoryElement element in elements)
            {
                if (element != null)
                {
                    StackText.Fit(element.m_amount);
                }
            }
        }
    }
}
