using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Keeps the amount label readable once a chest slot holds six digit numbers.
    /// The game writes "stack/limit" into a label sized for "50/50", so at
    /// "100000/100000" the text spills over the neighbouring slots. Here the font
    /// shrinks as the text grows, and returns to its original size on short text.
    /// </summary>
    internal static class StackText
    {
        /// <summary>Text at or below this length keeps the original font size.</summary>
        private const int ComfortableLength = 7;

        /// <summary>Never shrink past this fraction of the original size.</summary>
        private const float MinScale = 0.4f;

        private static readonly Dictionary<TMP_Text, float> OriginalSizes = new Dictionary<TMP_Text, float>();

        internal static void Fit(TMP_Text label)
        {
            if (label == null || !label.enabled)
            {
                return;
            }

            if (!OriginalSizes.TryGetValue(label, out float original))
            {
                original = label.fontSize;
                OriginalSizes[label] = original;
            }

            int length = label.text != null ? label.text.Length : 0;
            float scale = length <= ComfortableLength
                ? 1f
                : Mathf.Max(MinScale, (float)ComfortableLength / length);

            float wanted = original * scale;
            if (!Mathf.Approximately(label.fontSize, wanted))
            {
                label.enableAutoSizing = false;
                label.fontSize = wanted;
            }
        }

        internal static void Forget()
        {
            OriginalSizes.Clear();
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGridStackTextSizePatch
    {
        private static readonly FieldInfo Elements = AccessTools.Field(typeof(InventoryGrid), "m_elements");
        private static readonly FieldInfo Amount = AccessTools.Field(AccessTools.Inner(typeof(InventoryGrid), "InventoryElement"), "m_amount");

        private static void Postfix(InventoryGrid __instance)
        {
            if (!ModConfig.ShrinkStackText.Value || Elements == null || Amount == null)
            {
                return;
            }

            if (!(Elements.GetValue(__instance) is IEnumerable<object> elements))
            {
                return;
            }

            foreach (object element in elements)
            {
                StackText.Fit(Amount.GetValue(element) as TMP_Text);
            }
        }
    }
}
