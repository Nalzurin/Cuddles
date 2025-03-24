using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Cuddles
{
    public class JoyGiver_Cuddling : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {

            if (!pawn.ageTracker.Adult)
            {
                return null;
            }
            if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            Pawn partner = pawn.GetClosestCuddlePartner();
            if (partner == null || !partner.Spawned || !partner.Awake())
            {
                return null;
            }
            Building_Bed bed = CuddlesUtility.FindBedForCuddling(pawn, partner);
            if (bed == null)
            {
                return null;
            }
            return JobMaker.MakeJob(def.jobDef, partner, bed);
        }
    }
}
