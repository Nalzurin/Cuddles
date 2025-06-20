using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Cuddles
{
    public class CuddleSettings : ModSettings
    {
        public static bool enableAddiction = true;
        public static bool enableFurryMode = false;
        public static int minOpinionForCuddles = 10;
        public string minOpinionForCuddlesBuffer = "";
        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);
            ls.CheckboxLabeled("CuddlesSettingsEnableAddiction".Translate(), ref enableAddiction);
            ls.CheckboxLabeled("CuddlesSettingsEnableAltIcon".Translate(), ref enableFurryMode);
            ls.TextFieldNumericLabeled("CuddlesSettingsMinOpinionForCuddles".Translate(), ref minOpinionForCuddles, ref minOpinionForCuddlesBuffer);
            ls.End();

        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableAddiction, "enableAddiction", true);
            Scribe_Values.Look(ref enableFurryMode, "enableFurryMode", false);
            Scribe_Values.Look(ref minOpinionForCuddles, "minOpinionForCuddles", 10);

        }
    }
}
