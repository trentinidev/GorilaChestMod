using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CraftFromChests
{
    /// <summary>One "call this instead" rule for a transpiler.</summary>
    internal sealed class CallSwap
    {
        internal readonly MethodInfo From;
        internal readonly MethodInfo To;

        internal CallSwap(MethodInfo from, MethodInfo to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// Rewrites selected instance calls into static calls on <see cref="InventoryBridge"/>.
    /// An instance call already has "this" as the first stack argument, so a static
    /// method whose first parameter is the instance is a drop-in replacement and the
    /// rest of the method body is left untouched.
    /// </summary>
    internal static class CallRedirector
    {
        internal static IEnumerable<CodeInstruction> Apply(
            IEnumerable<CodeInstruction> instructions, string patchedMethod, params CallSwap[] swaps)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int[] hits = new int[swaps.Length];

            foreach (CodeInstruction instruction in code)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called == null)
                {
                    continue;
                }

                for (int i = 0; i < swaps.Length; i++)
                {
                    if (!SameMethod(called, swaps[i].From))
                    {
                        continue;
                    }

                    instruction.opcode = OpCodes.Call;
                    instruction.operand = swaps[i].To;
                    hits[i]++;
                    break;
                }
            }

            for (int i = 0; i < swaps.Length; i++)
            {
                if (hits[i] == 0)
                {
                    CraftFromChestsPlugin.Log.LogError(
                        $"{patchedMethod}: no call to {swaps[i].From.DeclaringType?.Name}.{swaps[i].From.Name} " +
                        "was found. The game code changed, part of the mod is inactive.");
                }
                else if (ModConfig.Verbose.Value)
                {
                    CraftFromChestsPlugin.Log.LogInfo(
                        $"{patchedMethod}: redirected {hits[i]} call(s) to {swaps[i].From.Name}.");
                }
            }

            return code;
        }

        private static bool SameMethod(MethodInfo a, MethodInfo b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return a.Module == b.Module && a.MetadataToken == b.MetadataToken;
        }
    }
}
