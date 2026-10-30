using System.Collections.Generic;
using Verse;

namespace FatReserve
{
    public enum StorageMode
    {
        Map,   // Режим 1: Привязка к карте (MapComponent)
        Pawn   // Режим 2: Привязка к животному (ThingComp)
    }

    public class FatReserveSettings : ModSettings
    {
        public float targetNutritionThreshold = 0.95f;
        public bool defaultEnabledForNewPawns = false;
        public StorageMode storageMode = StorageMode.Pawn;
        public bool applyInCaravan = false;

        // Список пород (defName), у которых нагул инвертирован относительно общего дефолта
        public List<string> enabledRaceDefs = new List<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetNutritionThreshold, "targetNutritionThreshold", 0.95f);
            Scribe_Values.Look(ref defaultEnabledForNewPawns, "defaultEnabledForNewPawns", false);
            Scribe_Values.Look(ref storageMode, "storageMode", StorageMode.Pawn);
            Scribe_Collections.Look(ref enabledRaceDefs, "enabledRaceDefs", LookMode.Value);
            Scribe_Values.Look(ref applyInCaravan, "applyInCaravan", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && enabledRaceDefs == null)
            {
                enabledRaceDefs = new List<string>();
            }
        }
    }
}