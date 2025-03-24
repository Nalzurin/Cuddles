using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Sprites;
using Verse;
using Verse.AI;

namespace Cuddles
{
    public class JobDriver_AskToCuddle : JobDriver
    {

        private int waitCount = 0;

        private Pawn Actor => GetActor();

        private Pawn TargetPawn => base.TargetThingA as Pawn;

        private Building_Bed TargetBed => base.TargetThingB as Building_Bed;

        private TargetIndex TargetPawnIndex => TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetPawnIndex);
            Toil walkToTarget = Toils_Interpersonal.GotoInteractablePosition(TargetPawnIndex);
            walkToTarget.AddPreInitAction(delegate
            {
                ticksLeftThisToil = 1875;
            });
            walkToTarget.AddPreTickAction(delegate
            {
                if (ticksLeftThisToil <= 1 && CuddlesUtility.DistanceFailure(Actor, TargetPawn, ref waitCount, ref ticksLeftThisToil))
                {
                    Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            });
            walkToTarget.socialMode = RandomSocialMode.Off;
            yield return walkToTarget;
            Toil wait = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            wait.socialMode = RandomSocialMode.Off;
            yield return wait;
            Toil askForCuddles = ToilMaker.MakeToil("MakeNewToils");
            askForCuddles.defaultCompleteMode = ToilCompleteMode.Delay;
            askForCuddles.initAction = delegate
            {
                ticksLeftThisToil = 50;
                FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, FleckDefOf.Heart);
               
            };
            askForCuddles.AddFailCondition(() => !IsTargetPawnOkay());
            yield return askForCuddles;
            Toil makeJobs = ToilMaker.MakeToil("MakeNewToils");
            makeJobs.defaultCompleteMode = ToilCompleteMode.Instant;
            makeJobs.initAction = delegate
            {
                CuddlesUtility.GiveCuddlingJob(Actor, TargetPawn, TargetBed);

            };
            yield return makeJobs;
        }
    }
}
