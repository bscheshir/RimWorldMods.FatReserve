using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FatReserve
{
    public static class FatReserveUtility
    {
        public static CompFatReserve EnsureCompInjected(Pawn pawn)
        {
            if (pawn == null) return null;
            CompFatReserve comp = pawn.TryGetComp<CompFatReserve>();
            if (comp == null)
            {
                if (pawn.def.comps == null)
                {
                    pawn.def.comps = new List<CompProperties>();
                }
                pawn.def.comps.Add(new CompProperties(typeof(CompFatReserve)));
                pawn.InitializeComps();
                comp = pawn.TryGetComp<CompFatReserve>();
            }
            return comp;
        }

        public static void OpenSharedRaceContextMenu(Pawn pawn)
        {
            var options = GetSharedRaceContextMenuOptions(pawn);
            if (options != null && options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static List<FloatMenuOption> GetSharedRaceContextMenuOptions(Pawn pawn)
        {
            if (pawn?.def == null || pawn.Map == null) return new List<FloatMenuOption>();

            string raceDefName = pawn.def.defName;
            string labelRace = string.Format("{0}", pawn.def.LabelCap);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            Texture2D iconOn = Widgets.CheckboxOnTex;
            Texture2D iconOff = Widgets.CheckboxOffTex;

            // 1-2. В пределах текущей КАРТЫ
            options.Add(CreateComplexOption("FatReserve_MenuRaceMapOn_Prefix".Translate(), labelRace, pawn.def, iconOn, () => SetStateForRaceOnMap(pawn, true)));
            options.Add(CreateComplexOption("FatReserve_MenuRaceMapOff_Prefix".Translate(), labelRace, pawn.def, iconOff, () => SetStateForRaceOnMap(pawn, false)));

            // 3-4. ГЛОБАЛЬНО на всех картах одновременно
            options.Add(CreateComplexOption("FatReserve_MenuRaceGlobalOn_Prefix".Translate(), labelRace, pawn.def, iconOn, () => SetStateForRaceGlobally(pawn.def, true)));
            options.Add(CreateComplexOption("FatReserve_MenuRaceGlobalOff_Prefix".Translate(), labelRace, pawn.def, iconOff, () => SetStateForRaceGlobally(pawn.def, false)));

            // 5-6. Значение ПО УМОЛЧАНИЮ для породы
            options.Add(CreateComplexOption("FatReserve_MenuRaceDefaultOn_Prefix".Translate(), labelRace, pawn.def, iconOn, () => UpdateRaceTemplateOnly(raceDefName, true)));
            options.Add(CreateComplexOption("FatReserve_MenuRaceDefaultOff_Prefix".Translate(), labelRace, pawn.def, iconOff, () => UpdateRaceTemplateOnly(raceDefName, false)));

            // 7. Включить нагул по умолчанию для абсолютно ВСЕХ существ мода в таблице пород
            options.Add(CreateComplexOption("FatReserve_MenuAllGlobalOn_Prefix".Translate(), "", null, iconOn, () =>
            {
                List<string> newList = new List<string>();

                // Если главный дефолт выключен, принудительно делаем ВСЕ породы в игре исключениями
                if (!FatReserveMod.Settings.defaultEnabledForNewPawns)
                {
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                    {
                        if (def.race != null && def.race.Animal && def.thingClass == typeof(Pawn) && def.race.IsFlesh)
                        {
                            newList.Add(def.defName);
                        }
                    }
                }
                // Если главный дефолт и так включен — newList останется пустым (все галочки станут On автоматически)

                FatReserveMod.Settings.enabledRaceDefs = newList;
                FatReserveMod.Settings.Write();
            }));

            // 8. Выключить нагул по умолчанию для абсолютно ВСЕХ существ мода в таблице пород
            options.Add(CreateComplexOption("FatReserve_MenuAllGlobalOff_Prefix".Translate(), "", null, iconOff, () =>
            {
                List<string> newList = new List<string>();

                // Если главный дефолт включен, принудительно делаем ВСЕ породы в игре исключениями
                if (FatReserveMod.Settings.defaultEnabledForNewPawns)
                {
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                    {
                        if (def.race != null && def.race.Animal && def.thingClass == typeof(Pawn) && def.race.IsFlesh)
                        {
                            newList.Add(def.defName);
                        }
                    }
                }
                // Если главный дефолт и так выключен — newList останется пустым (все галочки станут Off автоматически)

                FatReserveMod.Settings.enabledRaceDefs = newList;
                FatReserveMod.Settings.Write();
            }));

            return options;
        }

        private static void SetStateForRaceOnMap(Pawn samplePawn, bool state)
        {
            var manager = samplePawn.Map?.GetComponent<FatReserveToggleManager>();
            if (manager == null) return;

            foreach (Pawn p in samplePawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (p.def == samplePawn.def && p.Faction == Faction.OfPlayer)
                {
                    manager.SetReserveStateManual(p, state);
                }
            }
        }

        private static void SetStateForRaceGlobally(ThingDef raceDef, bool state)
        {
            if (Find.Maps == null) return;

            foreach (Map map in Find.Maps)
            {
                var manager = map.GetComponent<FatReserveToggleManager>();
                if (manager == null) continue;

                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.def == raceDef && p.Faction == Faction.OfPlayer)
                    {
                        manager.SetReserveStateManual(p, state);
                    }
                }
            }
        }

        private static void UpdateRaceTemplateOnly(string raceDefName, bool state)
        {
            bool defaultState = FatReserveMod.Settings.defaultEnabledForNewPawns;
            if (state != defaultState)
            {
                if (!FatReserveMod.Settings.enabledRaceDefs.Contains(raceDefName))
                    FatReserveMod.Settings.enabledRaceDefs.Add(raceDefName);
            }
            else
            {
                FatReserveMod.Settings.enabledRaceDefs.Remove(raceDefName);
            }
        }

        private static FloatMenuOption CreateComplexOption(string prefixText, string raceLabel, ThingDef raceDef, Texture2D statusIcon, System.Action action)
        {
            string fullText = string.IsNullOrEmpty(raceLabel) ? prefixText : $"{prefixText} {raceLabel}";
            FloatMenuOption option = new FloatMenuOption(fullText, action);
            option.extraPartWidth = 50f;
            option.extraPartOnGUI = (Rect rect) =>
            {
                Rect statusRect = new Rect(rect.x + 4f, rect.y + (rect.height - 18f) / 2f, 18f, 18f);
                if (statusIcon != null) GUI.DrawTexture(statusRect, statusIcon);

                if (raceDef != null)
                {
                    Rect raceIconRect = new Rect(rect.x + 26f, rect.y + (rect.height - 20f) / 2f, 20f, 20f);
                    Widgets.ThingIcon(raceIconRect, raceDef, null, null, 1f);
                }
                return false;
            };
            return option;
        }

        // Фильтр пород ThingDef (Определений пород в базе данных)
        public static bool IsValidFatteningAnimalDef(ThingDef def)
        {
            return def?.race != null
                   && def.race.Animal
                   && def.thingClass == typeof(Pawn)
                   && def.race.IsFlesh
                   && def.race.foodType != FoodTypeFlags.None;
        }

        //Фильтр существ (Pawn на карте)
        public static bool IsValidFatteningAnimal(Pawn pawn)
        {
            if (pawn == null || pawn.needs?.food == null || !IsValidFatteningAnimalDef(pawn.def))
            {
                return false;
            }
            switch (FatReserveMod.Settings.storageMode)
            {
                case StorageMode.Map:
                    // Для режима КАРТЫ: животное ОБЯЗАНО быть физически заспавнено на активной локации
                    return pawn.Spawned && pawn.Map != null;

                case StorageMode.Pawn:
                    // Для режима ЖИВОТНОГО: если пешка на карте — она валидна.
                    if (pawn.Spawned && pawn.Map != null)
                    {
                        return true;
                    }
                    // Если пешка в караване — разрешил ли игрок разрешать жировать в пути, согласно настройкам (ну, а вдруг)
                    if (FatReserveMod.Settings.applyInCaravan && pawn.IsCaravanMember())
                    {
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        public static bool ShouldApplyReserve(Pawn pawn)
        {
            if (!IsValidFatteningAnimal(pawn)) return false;

            var manager = pawn.Map?.GetComponent<FatReserveToggleManager>();
            if (manager == null) return false;
            if (!manager.IsReserveEnabled(pawn)) return false;

            return pawn.needs.food.CurLevelPercentage < FatReserveMod.Settings.targetNutritionThreshold;
        }

    }
}
