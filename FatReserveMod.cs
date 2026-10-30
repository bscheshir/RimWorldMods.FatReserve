using UnityEngine;
using Verse;

namespace FatReserve
{
    public class FatReserveMod : Mod
    {
        public static FatReserveSettings Settings { get; private set; }

        public FatReserveMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FatReserveSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            FatReserveSettingsUI.DrawSettings(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "FatReserve_ModName".Translate();
    }
}