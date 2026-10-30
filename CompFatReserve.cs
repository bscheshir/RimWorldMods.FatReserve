using Verse;

namespace FatReserve
{
    public class CompFatReserve : ThingComp
    {
        // null - игрок не кликал ЛКМ. true/false - ручной выбор игрока
        public bool? isReserveEnabled = null;

        // null - особь еще не обрабатывалась. true/false - зафиксированный при рождении дефолт
        public bool? inheritedDefault = null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isReserveEnabled, "isReserveEnabled", null);
            Scribe_Values.Look(ref inheritedDefault, "inheritedDefault", null);
        }
    }
}
