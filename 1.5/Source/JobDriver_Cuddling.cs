using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Sprites;
using Verse;
using Verse.AI;

namespace Cuddles
{
    public class JobDriver_Cuddling : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private TargetIndex BedInd = TargetIndex.B;
        private readonly TargetIndex BedSlotInd = TargetIndex.C;
        private Pawn Partner => (Pawn)job.GetTarget(PartnerInd);
        private Building_Bed Bed => (Building_Bed)job.GetTarget(BedInd);
        private Pawn Actor => GetActor();
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
            }
            return false;
        }
        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }
        private bool IsInOrByBed(Building_Bed b, Pawn p)
        {
            for (int i = 0; i < b.SleepingSlotsCount; i++)
            {
                if (b.GetSleepingSlotPos(i).InHorDistOf(p.Position, 1f))
                {
                    return true;
                }
            }
            return false;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOnDespawnedOrNull(PartnerInd);
            this.FailOn(() => !Partner.health.capacities.CanBeAwake);
            yield return Toils_Reserve.Reserve(BedInd, 2, 0);
            Toil walkToBed = Toils_Goto.Goto(BedSlotInd, PathEndMode.OnCell);
            yield return walkToBed;
            Toil wait = ToilMaker.MakeToil("MakeNewToils");
            wait.initAction = delegate
            {
                ticksLeftThisToil = 1250;
            };
            wait.tickAction = delegate
            {
                if (IsInOrByBed(Bed, Partner))
                {
                    ReadyForNextToil();
                }
                else if (ticksLeftThisToil <= 1 && Partner.CurJobDef != DefOfs.Cuddling)
                {
                    Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            wait.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return wait;
            Toil layDown = ToilMaker.MakeToil("MakeNewToils");
            layDown.initAction = delegate
            {
                Actor.pather.StopDead();
                Actor.jobs.curDriver.asleep = false;
                Actor.jobs.posture = PawnPosture.LayingInBed;
            };
            layDown.tickAction = delegate
            {
                Actor.GainComfortFromCellIfPossible();
            };
            yield return layDown;
            Toil loveToil = ToilMaker.MakeToil("MakeNewToils");
            loveToil.initAction = delegate
            {
                ticksLeftThisToil = 1250;
            };
            loveToil.tickAction = delegate
            {
                if (ticksLeftThisToil % 100 == 0)
                {
                    FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, CuddleSettings.enableFurryMode ? DefOfs.Fleck_CuddlesAlt : DefOfs.Fleck_Cuddles);
                }
                Actor.GainComfortFromCellIfPossible();
                if (Actor.needs.joy != null)
                {
                    JoyUtility.JoyTickCheckEnd(Actor, JoyTickFullJoyAction.None);
                }
            };
            loveToil.defaultCompleteMode = ToilCompleteMode.Delay;
            loveToil.AddFailCondition(() => Partner.Dead || Partner.Downed || (ticksLeftThisToil > 100 && !IsInOrByBed(Bed, Partner)));
            yield return loveToil;
            Toil finalToil = ToilMaker.MakeToil("MakeNewToils");
            finalToil.initAction = delegate
            {
                if (CuddleSettings.enableAddiction)
                {
                    CuddlesUtility.ApplyCuddlingHediffs(Actor, Partner, 0.5f);
                    CuddlesUtility.AddictionPost(Actor);
                }
                List<Need> list = Actor.needs.AllNeeds.Where(x => x.def == DefOfs.Chemical_Cuddles).ToList();
                if (!list.Empty())
                {
                    Need_Chemical need = (Need_Chemical)list.First();
                    if (need != null)
                    {
                        need.CurLevel += 0.6f;
                    }
                }
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(DefOfs.GotSomeCuddles);
                if (Actor.needs.mood != null)
                {
                    Actor.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, Partner);
                }

            };
            finalToil.defaultCompleteMode = ToilCompleteMode.Instant;
            finalToil.socialMode = RandomSocialMode.Off;
            yield return finalToil;
        }
    }

}
