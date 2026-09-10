using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace GorilaChestMod
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
    /// Rewrites selected instructions so that vanilla code asks this mod instead
    /// of the game's own state, without touching the rest of the method body.
    /// </summary>
    internal static class CallRedirector
    {
        /// <summary>
        /// Turns instance calls into static calls on a mod method. An instance call
        /// already has "this" as its first stack argument, so a static method whose
        /// first parameter is the instance is a drop-in replacement.
        /// </summary>
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
                    if (!SameMember(called, swaps[i].From))
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
                    GorilaChestModPlugin.Log.LogError(
                        $"{patchedMethod}: no call to {swaps[i].From.DeclaringType?.Name}.{swaps[i].From.Name} " +
                        "was found. The game code changed, part of the mod is inactive.");
                }
                else if (ModConfig.Verbose.Value)
                {
                    GorilaChestModPlugin.Log.LogInfo(
                        $"{patchedMethod}: redirected {hits[i]} call(s) to {swaps[i].From.Name}.");
                }
            }

            return code;
        }

        /// <summary>
        /// Turns a field read into a call that can answer differently per context.
        /// The field's own value is still reachable inside the replacement, which
        /// receives the object the field was about to be read from, followed by
        /// whatever <paramref name="extraArgs"/> pushes.
        /// </summary>
        internal static IEnumerable<CodeInstruction> ReplaceFieldReads(
            IEnumerable<CodeInstruction> instructions,
            string patchedMethod,
            FieldInfo field,
            MethodInfo replacement,
            Func<IEnumerable<CodeInstruction>> extraArgs,
            out int replaced)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            replaced = 0;

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Ldfld)
                {
                    continue;
                }

                if (!(code[i].operand is FieldInfo read) || !SameMember(read, field))
                {
                    continue;
                }

                List<CodeInstruction> extras = new List<CodeInstruction>(extraArgs());
                if (extras.Count == 0)
                {
                    continue;
                }

                // A branch that targeted the field read has to land on the first
                // pushed argument instead, the object is already on the stack.
                extras[0].labels.AddRange(code[i].labels);
                code[i].labels.Clear();
                extras[0].blocks.AddRange(code[i].blocks);
                code[i].blocks.Clear();

                code[i].opcode = OpCodes.Call;
                code[i].operand = replacement;

                code.InsertRange(i, extras);
                i += extras.Count;
                replaced++;
            }

            if (replaced > 0 && ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"{patchedMethod}: redirected {replaced} read(s) of {field.DeclaringType?.Name}.{field.Name}.");
            }

            return code;
        }

        /// <summary>
        /// Reflection can hand out different MethodInfo or FieldInfo objects for the
        /// same member, so compare by what they actually point at.
        /// </summary>
        internal static bool SameMember(MemberInfo a, MemberInfo b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return a != null && b != null && a.Module == b.Module && a.MetadataToken == b.MetadataToken;
        }
    }
}
