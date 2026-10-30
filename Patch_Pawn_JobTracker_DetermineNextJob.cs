using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FatReserve
{
    [HarmonyPatch(typeof(Pawn_JobTracker), "DetermineNextJob")]
    public static class Patch_Pawn_JobTracker_DetermineNextJob
    {
        private static readonly JobGiver_GetFood foodGiver = new JobGiver_GetFood();

        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;

            // БАЗОВЫЕ ФИЛЬТРЫ: Существует, на карте, живо, животное игрока
            if (pawn != null && pawn.Spawned && !pawn.Dead && pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
            {
                // Проверяем логику применения
                if (FatReserveUtility.ShouldApplyReserve(pawn))
                {
                    Job currentJob = __result.Job;

                    // Допустимые работы
                    if (currentJob != null && IsAcceptableJob(currentJob.def, __result.SourceNode))
                    {
                        ThinkResult foodResult = foodGiver.TryIssueJobPackage(pawn, default(JobIssueParams));

                        // Если еда успешно найдена внутри Allowed Area / разрешенной зоны животного
                        if (foodResult.Job != null)
                        {
                            string oldJobName = __result.Job.def.defName;

                            // Перезаписываем блуждание/отдых на команду "жрац!"
                            __result = foodResult;

                            // Вывод локализованного лога
                            Log.Message("FatReserve.LogJobIntercept".Translate(
                                pawn.LabelShort,
                                oldJobName,
                                __result.Job.def.defName,
                                __result.Job.targetA.Thing?.LabelShort ?? "plant",
                                (pawn.needs.food.CurLevelPercentage * 100f).ToString("F1")
                            ));
                        }
                    }
                }
            }
        }

        /// Фильтр допустимых работ для перехвата (безделье, блуждание, ожидание)
        private static bool IsAcceptableJob(JobDef jobDef, ThinkNode sourceNode)
        {
            if (jobDef == null) return false;

            // 1. Проверяем группу ИИ (WanderHerd, WanderCurrentRoom и т.д.)
            if (sourceNode is JobGiver_Wander)
            {
                return true;
            }

            // 2. Проверяем поведенческий драйвер работы
            System.Type driverType = jobDef.driverClass;
            if (driverType == typeof(JobDriver_Wait))
            {
                // Отсекаем критические виды ожидания (боевое ожидание, сон, нокдаун)
                if (jobDef != RimWorld.JobDefOf.Wait_Combat &&
                    jobDef != RimWorld.JobDefOf.Wait_Asleep &&
                    jobDef != RimWorld.JobDefOf.Wait_Downed)
                {
                    return true;
                }
            }

            // 3. Прямой предохранитель по базовым ванильным DefOf-заданиям
            if (jobDef == RimWorld.JobDefOf.Wait || jobDef == RimWorld.JobDefOf.Wait_Wander)
            {
                return true;
            }

            return false;
        }
    }
}
