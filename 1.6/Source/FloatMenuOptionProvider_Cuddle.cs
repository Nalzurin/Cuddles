using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Cuddles
{
    public class FloatMenuOptionProvider_Cuddle : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool CanSelfTarget => false;
        public override bool CanTargetDespawned => false;
        protected override bool MechanoidCanDo => false;
        protected override bool IgnoreFogged => true;
        protected override bool RequiresManipulation => false;
        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            Log.Message("Testing selected");
            if(!base.SelectedPawnValid(pawn, context))
            {
                Log.Message("Base selected pawn invalid");
                return false;
            }
            if (!SocialInteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                Log.Message("pawn can't initiate or has basic need");
                return false;
            }
            return true;
        }
        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            Log.Message($"Testing target: {pawn.Label}");
            if (!base.TargetPawnValid(pawn, context))
            {
                Log.Message("Base target invalid");
                return false;
            }
            if (!pawn.IsColonist && pawn.HostFaction == null)
            {
                Log.Message("Pawn is not colonist and host faction is null");

                return false;
            }
            if (pawn.DevelopmentalStage.Baby())
            {
                Log.Message("Pawn is baby");

                return false;
            }
            return true;
        }
        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Log.Message("test");
            Pawn pawn = context.FirstSelectedPawn;
            if (!pawn.isPossibleCuddlePartner(clickedPawn))
            {
                return null;
            }
            if (!BedUtility.WillingToShareBed(pawn, clickedPawn) || !BedUtility.WillingToShareBed(clickedPawn, pawn))
            {
                return null;
            }
            Building_Bed bed = CuddlesUtility.FindBedForCuddling(pawn, clickedPawn);
            if (bed == null)
            {
                return null;
            }
            return new FloatMenuOption("CuddlesCuddleWith".Translate(clickedPawn.Label), delegate
            {
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefOfs.AskToCuddle, clickedPawn, bed));
            }, MenuOptionPriority.Low);
        }
    }
}
