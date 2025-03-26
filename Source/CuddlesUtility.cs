using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using static RimWorld.PsychicRitualRoleDef;

namespace Cuddles
{
    [StaticConstructorOnStartup]
    public static class CuddlesUtility
    {
        public static readonly Texture2D CuddleIcon = ContentFinder<Texture2D>.Get("Things/Mote/Cuddle");
        public static readonly Texture2D CuddleAlt = ContentFinder<Texture2D>.Get("Things/Mote/CuddleAlt");

        public static Texture2D GetCuddleIcon()
        {
            if (CuddleSettings.enableFurryMode)
            {
                return CuddleAlt;
            }
            else
            {
                return CuddleIcon;
            }
        }
        public static bool CanCuddle(Pawn pawn)
        {
            //Log.Message($"{pawn.LabelCap} is dead: {pawn.Dead}");
            if (pawn.Dead)
            {
                return false;
            }
            //Log.Message($"{pawn.LabelCap} is Downed: {pawn.Downed}");

            if (pawn.Downed)
            {
                return false;
            }
            //Log.Message($"{pawn.LabelCap} is Drafted: {pawn.Drafted}");

            if (pawn.Drafted)
            {
                return false;
            }
            //Log.Message($"{pawn.LabelCap} is InMentalState: {pawn.InMentalState}");

            if (pawn.InMentalState)
            {
                return false;
            }
            //Log.Message($"{pawn.LabelCap} is in labor: {pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor) || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing)}");

            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor) || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing))
            {
                return false;
            }

            Pawn_TimetableTracker timetable = pawn.timetable;
/*            Log.Message($"{pawn.LabelCap} is IsCarryingPawn: {pawn.IsCarryingPawn()}");
            Log.Message($"{pawn.LabelCap} IsCurrentJobPlayerInterruptible: {pawn.jobs.IsCurrentJobPlayerInterruptible()}");
            Log.Message($"{pawn.LabelCap} can do joy: {timetable != null && !timetable.CurrentAssignment.allowJoy}");*/
            if ((timetable != null && !timetable.CurrentAssignment.allowJoy) || pawn.IsCarryingPawn() || !pawn.jobs.IsCurrentJobPlayerInterruptible())
            {
                return false;
            }

            //Log.Message($"{pawn.LabelCap} is WillSoonHaveBasicNeed: {PawnUtility.WillSoonHaveBasicNeed(pawn)}");

            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return false;
            }
            //Log.Message($"{pawn.LabelCap} EnemiesAreNearby: {PawnUtility.EnemiesAreNearby(pawn)}");

            if (PawnUtility.EnemiesAreNearby(pawn))
            {
                return false;
            }
            return true;
        }

        public static bool isPossibleCuddlePartner(this Pawn pawn, Pawn p)
        {
            if (pawn == p)
            {
                return false;
            }
            if (!p.ageTracker.Adult)
            {
                return false;
            }
            if (p.relations.OpinionOf(pawn) < CuddleSettings.minOpinionForCuddles || pawn.relations.OpinionOf(p) < CuddleSettings.minOpinionForCuddles)
            {
                return false;
            }
            return true;
        }
        public static List<Pawn> GetPossibleCuddlePartners(this Pawn pawn)
        {
            List<Pawn> pawns = pawn.Map.mapPawns.FreeColonists;
            List<Pawn> temp = [];
            foreach (Pawn p in pawns)
            {
                if (!pawn.isPossibleCuddlePartner(p))
                {
                    continue;
                }
                if (p.relations.DirectRelationExists(PawnRelationDefOf.Lover, pawn))
                {
                    temp.Prepend(p);
                    continue;
                }
                if (p.relations.DirectRelationExists(PawnRelationDefOf.Fiance, pawn))
                {
                    temp.Prepend(p);
                    continue;
                }
                if (p.relations.DirectRelationExists(PawnRelationDefOf.Spouse, pawn))
                {
                    temp.Prepend(p);
                    continue;
                }
                temp.Add(p);

            }
            return temp;
        }
        public static Pawn GetClosestCuddlePartner(this Pawn pawn)
        {
            List<Pawn> pawns = pawn.GetPossibleCuddlePartners();
            Pawn partner = null;
            foreach (Pawn p in pawns)
            {
                Log.Message($"{p.LabelCap} can cuddle {CanCuddle(p)}");
                if (p.Spawned && p.Position.InHorDistOf(pawn.Position, 100f) && CanCuddle(p) && !p.IsForbidden(pawn))
                {
                    PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, p.Position, pawn, PathEndMode.Touch);
                    if (path.NodesLeftCount <= 100f)
                    {
                        partner = p;
                        path?.Dispose();
                        break;
                    }
                    path?.Dispose();
                }
            }
            return partner;
        }
        public static Building_Bed FindBedForCuddling(Pawn pawn, Pawn partner)
        {
            Building_Bed cuddleBed = null;
            Building_Bed freeBed = null;
            List<Building_Bed> bedList = (from b in pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                                          where !b.OwnersForReading.Any() && b.def.building.bed_humanlike && b.SleepingSlotsCount > 1
                                          orderby b.GetStatValue(StatDefOf.BedRestEffectiveness) descending
                                          select b).ToList();
            foreach (Building_Bed bed in bedList)
            {
                if (CanBothUse(bed, pawn, partner) && !bed.AnyOccupants && CanBothReach(bed, pawn, partner))
                {
                    if (bed.GetCuddlingSpot().isCuddleSpot)
                    {
                        cuddleBed = bed;
                        break;
                    }
                    freeBed = bed;
                    break;
                }
            }
            if (cuddleBed != null)
            {
                return cuddleBed;
            }
            Building_Bed bed1 = pawn.ownership.OwnedBed;
            if (bed1 != null && bed1.SleepingSlotsCount > 1 && CanBothReach(bed1, pawn, partner) && (partner.ownership.OwnedBed == bed1 || (!bed1.AnyOccupants && RestUtility.CanUseBedEver(partner, bed1.def))))
            {
                return bed1;
            }
            Building_Bed bed2 = partner.ownership.OwnedBed;
            if (bed2 != null && bed2.SleepingSlotsCount > 1 && !bed2.AnyOccupants && CanBothReach(bed2, pawn, partner) && RestUtility.CanUseBedEver(pawn, bed2.def))
            {
                return bed2;
            }
            if (freeBed != null)
            {
                return freeBed;
            }
            return null;
        }

        public static void AddictionPost(Pawn pawn)
        {
            HediffDef addictionHediffDef = DefOfs.CuddlesAddiction;
            Hediff_Addiction hediff_Addiction = AddictionUtility.FindAddictionHediff(pawn, DefOfs.Chem_Cuddles);
            float num = AddictionUtility.FindToleranceHediff(pawn, DefOfs.Chem_Cuddles)?.Severity ?? 0f;
            if (hediff_Addiction != null)
            {
                hediff_Addiction.Severity += 0.20f;
            }
            else
            {
                float num2 = 0.05f;
                if (pawn.genes != null)
                {
                    num2 *= pawn.genes.AddictionChanceFactor(DefOfs.Chem_Cuddles);
                }
                if (Rand.Value < num2 && num >= 0f)
                {
                    pawn.health.AddHediff(addictionHediffDef);
                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        Find.LetterStack.ReceiveLetter("LetterLabelNewlyAddicted".Translate(DefOfs.Chem_Cuddles.label).CapitalizeFirst(), "LetterNewlyAddicted".Translate(pawn.LabelShort, DefOfs.Chem_Cuddles.label, pawn.Named("PAWN")).AdjustedFor(pawn).CapitalizeFirst(), LetterDefOf.NegativeEvent, pawn);
                    }
                    AddictionUtility.CheckDrugAddictionTeachOpportunity(pawn);
                }
            }
            if (addictionHediffDef.causesNeed != null)
            {
                Need need = pawn.needs.AllNeeds.Find((Need x) => x.def == addictionHediffDef.causesNeed);
                if (need != null)
                {
                    float effect = 0.9f;
                    AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize_NewTemp(pawn, DefOfs.Chem_Cuddles, ref effect, applyGeneToleranceFactor: false);
                    need.CurLevel += effect;
                }
            }
        }

        public static void GiveCuddlingJob(Pawn pawn, Pawn partner, Building_Bed bed)
        {
            pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(DefOfs.Cuddling, partner, bed, bed.GetSleepingSlotPos(0)), JobTag.SatisfyingNeeds);
            partner.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(DefOfs.Cuddling, pawn, bed, bed.GetSleepingSlotPos(1)), JobTag.SatisfyingNeeds);
            partner.jobs.EndCurrentJob(JobCondition.InterruptOptional);
            pawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
        }
        private static bool CanBothReach(Building_Bed bed, Pawn pawn, Pawn partner)
        {
            return pawn.Map.reachability.CanReach(pawn.Position, bed.Position, PathEndMode.OnCell, TraverseParms.For(pawn)) && partner.Map.reachability.CanReach(partner.Position, bed.Position, PathEndMode.OnCell, TraverseParms.For(partner)) && !bed.IsForbidden(pawn) && !bed.IsForbidden(partner);
        }
        private static bool CanBothUse(Building_Bed bed, Pawn pawn, Pawn partner)
        {
            return RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(partner, bed.def);
        }
        public static bool DistanceFailure(Pawn pawn, Pawn TargetPawn, ref int waitCount, ref int ticksLeft)
        {
            if (pawn.Position.InHorDistOf(TargetPawn.Position, 10f))
            {
                ticksLeft += 100;
                waitCount++;
                if (waitCount <= 5 || (pawn.Position.InHorDistOf(TargetPawn.Position, 5f) && waitCount < 8))
                {
                    return false;
                }
            }
            return true;
        }

        public static void TryDrawCuddlesButton(Pawn pawn, Rect rect)
        {
            if (pawn.ageTracker.AgeBiologicalYearsFloat >= 16f && pawn.Spawned && pawn.IsFreeColonist)
            {
                DrawCuddlesButton(pawn, new Rect(rect.xMin + 10f, rect.height - 28f, 115f, 28f));
            }
        }
        public static void DrawCuddlesButton(Pawn pawn, Rect rect)
        {
            Color color = GUI.color;
            bool incapacitated = pawn.DeadOrDowned;
            List<Pawn> list = pawn.GetPossibleCuddlePartners();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn p in list)
            {
                if (p.Drafted && p.DevelopmentalStage.Baby())
                {
                    continue;
                }
                if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
                {
                    continue;
                }
                if (!pawn.isPossibleCuddlePartner(p))
                {
                    continue;
                }
                if (!BedUtility.WillingToShareBed(pawn, p) || !BedUtility.WillingToShareBed(p, pawn))
                {
                    continue;
                }
                Building_Bed bed = CuddlesUtility.FindBedForCuddling(pawn, p);
                if (bed == null)
                {
                    continue;
                }
                options.Add(new FloatMenuOption("CuddlesCuddleWith".Translate(p.Label), delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefOfs.AskToCuddle, p, bed));
                    }, MenuOptionPriority.Low));

            }
            GUI.color = ((list.NullOrEmpty() || incapacitated) ? ColoredText.SubtleGrayColor : Color.white);
            if (Widgets.ButtonText(rect, "CuddlesTryCuddlingWith".Translate()))
            {

                if (incapacitated)
                {
                    Messages.Message("CuddlesPawnIncapacitated".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (options.NullOrEmpty())
                {
                    Messages.Message("CuddlesNoOneToCuddleWith".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            GUI.color = color;
        }
        public static void ApplyCuddlingHediffs(Pawn pawn, Pawn partner, float severity)
        {
            Hediff hediff = null;
            if (partner.genes.GenesListForReading.ContainsAny(c => c.def.HasModExtension<CuddlesFurExtension>()))
            {
                hediff = HediffMaker.MakeHediff(DefOfs.CuddlesHighFur, pawn);
            }
            else
            {
                hediff = HediffMaker.MakeHediff(DefOfs.CuddlesHigh, pawn);
            }
            Hediff presentHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediff.def == DefOfs.CuddlesHigh ? DefOfs.CuddlesHighFur : DefOfs.CuddlesHigh);
            if (presentHediff != null)
            {
                pawn.health.RemoveHediff(presentHediff);
            }
            float effect = ((!(severity > 0f)) ? hediff.def.initialSeverity : severity);
            AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize_NewTemp(pawn, DefOfs.Chem_Cuddles, ref effect, true, true);
            hediff.Severity = effect;
            pawn.health.AddHediff(hediff);
        }
        public static bool IsCuddleBed(this Building_Bed bed)
        {
            if (bed.TryGetComp<CompCuddlingSpot>() != null)
            {
                return true;
            }
            return false;
        }
        public static CompCuddlingSpot GetCuddlingSpot(this Building_Bed bed)
        {
            return bed.GetComp<CompCuddlingSpot>();
        }
    }
}
