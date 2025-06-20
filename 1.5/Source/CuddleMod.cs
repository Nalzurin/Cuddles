using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Cuddles
{
    public class CuddleMod : Mod
    {
        public static CuddleSettings settings;
        public CuddleMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<CuddleSettings>();
        }
        public override string SettingsCategory()
        {
            return "Cuddles";
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
