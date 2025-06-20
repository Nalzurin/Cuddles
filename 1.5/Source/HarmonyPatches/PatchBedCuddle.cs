using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Cuddles.HarmonyPatches
{
    [HarmonyPatch]
    public static class PatchBedCuddleGizmo
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Building_Bed), nameof(Building_Bed.GetGizmos));
        }
        public static void Postfix(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
        {
            bool flag = false;
            if (__instance.IsCuddleBed() && __instance.GetCuddlingSpot().isCuddleSpot)
            {
                flag = true;
            }
            List<Gizmo> list = new List<Gizmo>();
            foreach (var gizmo in __result)
            {
                if (flag)
                {
                    if (gizmo is Command_Toggle && ((Command_Toggle)gizmo).defaultLabel == "CommandBedSetAsGuestLabel".Translate())
                    {
                        continue;
                    };
                    if (gizmo is Command_Toggle && ((Command_Toggle)gizmo).defaultLabel == "CommandBedSetAsMedicalLabel".Translate())
                    {
                        continue;
                    };
                    if (ModsConfig.IdeologyActive && gizmo is Command_SetBedOwnerType)
                    {
                        continue;
                    }
                    if (gizmo is Command_Toggle && ((Command_Toggle)gizmo).defaultLabel == "CommandBedSetForPrisonersLabel".Translate())
                    {
                        continue;
                    }
                }
                list.Add(gizmo);
            }
            __result = list;
        }
    }

    [HarmonyPatch]
    public static class PatchShouldShowAssignable
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CompAssignableToPawn_Bed), "ShouldShowAssignmentGizmo");
        }
        public static void Postfix(CompAssignableToPawn_Bed __instance, ref bool __result)
        {
            Building_Bed bed = (Building_Bed)__instance.parent;
            if (!bed.IsCuddleBed())
            {
                return;
            }
            if (!bed.GetCuddlingSpot().isCuddleSpot)
            {
                return;
            }
            __result = false;
        }
    }
    [HarmonyPatch]
    public static class PatchBedInspectString
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Building_Bed), nameof(Building_Bed.GetInspectString));
        }
        public static void Postfix(Building_Bed __instance, ref string __result)
        {
            if (__instance.def.building.bed_humanlike && __instance.Faction == Faction.OfPlayerSilentFail && __instance.IsCuddleBed() && __instance.GetCuddlingSpot().isCuddleSpot)
            {
                __result = __result + "\n" + "CuddlesBedForCuddles".Translate();
            }
        }

    }

    [HarmonyPatch]
    public static class PatchBedColor
    {
        private static readonly Color sheetColorForCuddles = new Color(64 / 255f, 224 / 255f, 208 / 255f);
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Building_Bed), "get_DrawColorTwo");
        }
        public static void Postfix(Building_Bed __instance, ref Color __result)
        {
            if (!__instance.IsCuddleBed())
            {
                return;
            }
            if (!__instance.GetCuddlingSpot().isCuddleSpot)
            {
                return;
            }
            __result = sheetColorForCuddles;
        }
    }
    [HarmonyPatch]
    public static class PatchBedOwnerLabel
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Building_Bed), nameof(Building_Bed.DrawGUIOverlay));
        }
        public static bool Prefix(Building_Bed __instance)
        {
            if (!__instance.IsCuddleBed())
            {
                return true;
            }
            if (__instance.GetCuddlingSpot().isCuddleSpot && !__instance.OwnersForReading.Any<Pawn>())
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class IsValidBedPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(RestUtility), nameof(RestUtility.IsValidBedFor));
        }
        public static void Postfix(Pawn sleeper, Thing bedThing, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            Building_Bed building_Bed = bedThing as Building_Bed;
            if (!building_Bed.IsCuddleBed())
            {
                return;
            }
            bool isOwner = sleeper.ownership != null && sleeper.ownership.OwnedBed == bedThing;
            if (building_Bed.GetCuddlingSpot().isCuddleSpot && !isOwner)
            {
                __result = false;
            }
        }
    }
}
