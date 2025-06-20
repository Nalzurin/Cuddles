using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Cuddles.HarmonyPatches
{
    [HarmonyPatch]
    public static class PatchDrawRelationsAndOpinions
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawRelationsAndOpinions));
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(AccessTools.Method(typeof(Widgets), "EndGroup")))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(CuddlesUtility), nameof(CuddlesUtility.TryDrawCuddlesButton));
                }
                yield return code;
            }
        }
    }
}
