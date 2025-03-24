using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Cuddles.HarmonyPatches
{
    [HarmonyPatch]
    public static class PatchFloatMenuMakerHumanLikeOrders
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");
        }
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn.Drafted || !pawn.ageTracker.Adult)
            {
                return;
            }
            foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForRomance(pawn), thingsOnly: true))
            {
                Pawn p = (Pawn)item.Thing;
                if (p.Drafted && p.DevelopmentalStage.Baby())
                {
                    return;                       
                }
                if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
                {
                    return;
                }
                if (!pawn.isPossibleCuddlePartner(p))
                {
                    return;
                }
                if (!BedUtility.WillingToShareBed(pawn, p) || !BedUtility.WillingToShareBed(p, pawn))
                {
                    return;
                }
                Building_Bed bed = CuddlesUtility.FindBedForCuddling(pawn, p);
                if (bed == null)
                {
                    return;
                }
                opts.Add(new FloatMenuOption("CuddlesCuddleWith".Translate(p.Label), delegate
                {
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefOfs.AskToCuddle, p, bed));
                }, MenuOptionPriority.Low));
            }
        }
    }
}
