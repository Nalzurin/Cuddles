using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Cuddles
{
    public class CompProperties_CuddlingSpot : CompProperties
    {
        public CompProperties_CuddlingSpot()
        {
            compClass = typeof(CompCuddlingSpot);
        }
    }
    public class CompCuddlingSpot : ThingComp
    {
        public bool isCuddleSpot = false;
        public Building_Bed bed => (Building_Bed)parent;
        public void ToggleCuddleSpot()
        {
            isCuddleSpot = !isCuddleSpot;
            parent.Notify_ColorChanged();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!bed.ForPrisoners &&
                 !bed.Medical &&
                  bed.def.building.bed_humanlike &&
                  bed.Faction == Faction.OfPlayerSilentFail &&
                 !bed.def.defName.Contains("Guest") &&
                 !bed.def.defName.Contains("Android") &&
                 !bed.ForHumanBabies &&
                  bed.GetRoom()?.IsPrisonCell == false)
            {
                Command_Toggle cuddleSpotToggle = new Command_Toggle();
                cuddleSpotToggle.defaultLabel = "CuddlesToggleCuddleSpot".Translate();
                cuddleSpotToggle.icon = CuddlesUtility.GetCuddleIcon();
                cuddleSpotToggle.isActive = () => isCuddleSpot;
                cuddleSpotToggle.toggleAction = ToggleCuddleSpot;
                yield return cuddleSpotToggle;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isCuddleSpot, "isCuddleSpot", false);
        }
    }
}
