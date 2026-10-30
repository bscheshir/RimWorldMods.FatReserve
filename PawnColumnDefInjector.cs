using HarmonyLib;
using RimWorld;
using Verse;

namespace FatReserve
{
    [StaticConstructorOnStartup]
    public static class PawnColumnDefInjector
    {
        static PawnColumnDefInjector()
        {
            // Находим определение колонки из XML
            PawnColumnDef reserveColumn = DefDatabase<PawnColumnDef>.GetNamedSilentFail("FR_FatReserveToggle");
            if (reserveColumn == null) return;

            // Находим целевую таблицу «Животные»
            PawnTableDef animalsTable = DefDatabase<PawnTableDef>.GetNamedSilentFail("Animals");
            if (animalsTable != null && animalsTable.columns != null)
            {
                // Защита от дублирования при перезагрузках
                if (!animalsTable.columns.Contains(reserveColumn))
                {
                    // Находим индекс колонки разрешённых зон "AllowedAreaWide".
                    int areaIndex = animalsTable.columns.FindIndex(c => c.defName == "AllowedAreaWide");

                    if (areaIndex != -1)
                    {
                        // Вставляем строго ПЕРЕД AllowedAreaWide. 
                        // Это нативно сместит зону вправо, а "Нагул" займет её идеальную позицию.
                        animalsTable.columns.Insert(areaIndex, reserveColumn);
                    }
                    else
                    {
                        // Безопасный фолбэк на случай экстремальных интерфейсных модов
                        animalsTable.columns.Add(reserveColumn);
                    }
                }
            }
        }
    }
}
