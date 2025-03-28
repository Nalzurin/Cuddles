using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Cuddles
{
    public class JobGiver_SatisfyCuddlingNeed : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            Need need = pawn.needs.TryGetNeed(DefOfs.Chemical_Cuddles);
            if(need == null || !ShouldSatisfy(need))
            {
                return 0f;
            }
            return 9.25f;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if(pawn.needs.TryGetNeed(DefOfs.Chemical_Cuddles) == null)
            {
                return null;
            }
            Need need = pawn.needs.TryGetNeed(DefOfs.Chemical_Cuddles);
            if (ShouldSatisfy(need))
            {
                Log.Message("Trying to give job");

                Pawn partner = pawn.GetClosestCuddlePartner();
                if (partner == null || !partner.Spawned || !partner.Awake())
                {
                    Log.Message("Problem with partner");
                    return null;
                }
                Building_Bed bed = CuddlesUtility.FindBedForCuddling(pawn, partner);
                if (bed == null)
                {
                    Log.Message("Bed is null");

                    return null;
                }
                Log.Message("Making job");
                return JobMaker.MakeJob(DefOfs.AskToCuddle, partner, bed);
            }
            return null;
        
        }

        private bool ShouldSatisfy(Need need)
        {
            if (need is Need_Chemical { CurCategory: <= DrugDesireCategory.Withdrawal })
            {
                return true;
            }
            return false;
        }
    }
}
