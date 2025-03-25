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

namespace Cuddles
{
    [StaticConstructorOnStartup]
    public static class CuddlesUtility
    {
        public static readonly Texture2D CuddleIcon = ContentFinder<Texture2D>.Get("Things/Mote/Cuddle");
        public static readonly Texture2D CuddleAlt = ContentFinder<Texture2D>.Get("Things/Mote/CuddleAlt");

        public static Texture2D GetCuddleIcon()
        {
            if(CuddleSettings.enableFurryMode)
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
            if (pawn.Dead)
            {
                return false;
            }
            if (pawn.Downed)
            {
                return false;
            }
            if (pawn.Drafted)
            {
                return false;
            }
            if (pawn.InMentalState)
            {
                return false;
            }
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor) || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing))
            {
                return false;
            }
            Pawn_TimetableTracker timetable = pawn.timetable;
            if ((timetable != null && !timetable.CurrentAssignment.allowJoy) || pawn.IsCarryingPawn() || !pawn.jobs.IsCurrentJobPlayerInterruptible())
            {
                return false;
            }
            if (pawn.CanCasuallyInteractNow(twoWayInteraction: true, false, false, false))
            {
                return false;
            }
            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return false;
            }
            if (PawnUtility.EnemiesAreNearby(pawn))
            {
                return false;
            }
            return true;
        }

        public static bool isPossibleCuddlePartner(this Pawn pawn, Pawn p)
        {
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
        public static bool FailureCheck(Pawn Partner, JobDef job)
        {
            return !Partner.Spawned || Partner.Dead || Partner.Downed || PawnUtility.WillSoonHaveBasicNeed(Partner) || Partner.CurJob?.def != job;
        }
        public static Pawn GetClosestCuddlePartner(this Pawn pawn)
        {
            List<Pawn> pawns = pawn.GetPossibleCuddlePartners();
            Pawn partner = null;
            foreach (Pawn p in pawns)
            {
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
            if(cuddleBed != null)
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
            if(freeBed != null)
            {
                return freeBed;
            }
            return null;
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
        public static void ApplyCuddlingHediffs(Pawn pawn, float severity)
        {
            Hediff hediff = HediffMaker.MakeHediff(DefOfs.CuddlesHigh, pawn);
            float effect = ((!(severity > 0f)) ? DefOfs.CuddlesHigh.initialSeverity : severity);
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
