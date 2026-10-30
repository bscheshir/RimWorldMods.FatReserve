using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound; // Гарантирует доступ к методам расширения звука

namespace FatReserve
{
    public static class FatReserveSettingsUI
    {
        private const float RowHeight = 28f;
        private const float IconSize = 24f;
        private const float CheckboxSize = 20f;
        private const float ScrollWindowHeight = 240f;
        private const float VerticalGap = 10f;

        private static List<ThingDef> cachedAnimalDefs;
        private static Vector2 scrollPosition = Vector2.zero;

        private static string searchFilter = "";
        private static bool isDragging = false;
        private static bool dragTargetState = false;

        public static void DrawSettings(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            float percentage = FatReserveMod.Settings.targetNutritionThreshold * 100f;
            listing.Label("FatReserve_SettingThreshold".Translate(percentage.ToString("F0")));
            percentage = listing.Slider(percentage, 50f, 100f);
            FatReserveMod.Settings.targetNutritionThreshold = percentage / 100f;

            listing.Gap(VerticalGap);

            // 1. Фиксируем состояние главного дефолта ДО отрисовки чекбокса
            bool oldGlobalState = FatReserveMod.Settings.defaultEnabledForNewPawns;

            // 2. Отрисовываем чекбокс через стандартный листинг
            listing.CheckboxLabeled(
                "FatReserve_SettingDefaultState".Translate(),
                ref FatReserveMod.Settings.defaultEnabledForNewPawns,
                "FatReserve_SettingDefaultStateDesc".Translate()
            );

            // 3. ОТДЕЛЬНЫЙ ОБРАБОТЧИК ИНВЕРСИИ:
            if (FatReserveMod.Settings.defaultEnabledForNewPawns != oldGlobalState)
            {
                EnsureAnimalDefsCached();
                List<string> newInvertedList = new List<string>();

                foreach (ThingDef animal in cachedAnimalDefs)
                {
                    // Вычисляем, в каком именно визуальном состоянии находилась галочка породы ДО клика игрока
                    bool wasRaceInverted = FatReserveMod.Settings.enabledRaceDefs.Contains(animal.defName);
                    bool visualStateBeforeClick = wasRaceInverted ? !oldGlobalState : oldGlobalState;

                    // Целевое визуальное состояние должно быть противоположным (инвертированным)
                    bool targetVisualState = !visualStateBeforeClick;

                    // Сравниваем целевое состояние с НОВЫМ глобальным дефолтом, 
                    // чтобы понять, должна ли порода теперь быть исключением
                    if (targetVisualState != FatReserveMod.Settings.defaultEnabledForNewPawns)
                    {
                        newInvertedList.Add(animal.defName);
                    }
                }

                // Перезаписываем список исключений скорректированным результатом
                FatReserveMod.Settings.enabledRaceDefs = newInvertedList;
                FatReserveMod.Settings.Write();
            }

            listing.Gap(VerticalGap);
            listing.Label("FatReserve_SettingRaceListLabel".Translate());

            EnsureAnimalDefsCached();

            // РАСЧЕТ И ОТРИСОВКА ИНТЕРАКТИВНОЙ СТРОКИ ПОИСКА
            Rect searchRowRect = listing.GetRect(24f);

            // 1. Иконка лупы (слева, размер 24х24)
            Rect magnifierRect = new Rect(searchRowRect.x, searchRowRect.y, 24f, 24f);
            GUI.DrawTexture(magnifierRect, TexButton.Search);

            // 2. ОБНОВЛЕННАЯ МАТЕМАТИКА ЗАЗОРА:
            // Две кнопки теперь занимают 210px (105 + 105) + отступ 10px = 220px.
            // Суммируем: 220px (кнопки) + 24px (лупа) + 20px (крестик) + 12px (зазоры) = 276f.
            float availableWidth = searchRowRect.width - 276f;
            Rect textFieldRect = new Rect(magnifierRect.xMax + 4f, searchRowRect.y, availableWidth, 24f);

            searchFilter = Widgets.TextField(textFieldRect, searchFilter);

            // 3. Кнопка очистки в виде крестика (размер 20х20, вписана идеально в зазор)
            if (!string.IsNullOrEmpty(searchFilter))
            {
                Rect clearBtnRect = new Rect(textFieldRect.xMax + 4f, searchRowRect.y + 2f, 20f, 20f);

                if (Widgets.ButtonImage(clearBtnRect, TexButton.CloseXSmall))
                {
                    searchFilter = string.Empty;

                    if (SoundDefOf.CancelMode != null)
                    {
                        SoundStarter.PlayOneShotOnCamera(SoundDefOf.CancelMode, null);
                    }
                }
            }

            // Кнопки массового включения/выключения (Увеличена ширина до 105f, блок начинается с xMax - 220f)
            if (Widgets.ButtonText(new Rect(searchRowRect.xMax - 220f, searchRowRect.y, 105f, 24f), "FatReserve_BtnEnableAll".Translate()))
            {
                MassToggleVisibleAnimals(true);
            }
            if (Widgets.ButtonText(new Rect(searchRowRect.xMax - 110f, searchRowRect.y, 105f, 24f), "FatReserve_BtnDisableAll".Translate()))
            {
                MassToggleVisibleAnimals(false);
            }

            listing.Gap(5f);

            List<ThingDef> filteredAnimals = cachedAnimalDefs.FindAll(a =>
                string.IsNullOrEmpty(searchFilter) || a.label.ToLower().Contains(searchFilter.ToLower()) || a.defName.ToLower().Contains(searchFilter.ToLower())
            );

            Rect scrollRect = listing.GetRect(ScrollWindowHeight);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, filteredAnimals.Count * RowHeight);

            Widgets.DrawMenuSection(scrollRect);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
            }

            float currentY = 0f;
            foreach (ThingDef animal in filteredAnimals)
            {
                Rect rowRect = new Rect(0f, currentY, viewRect.width, RowHeight);

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Rect iconRect = new Rect(4f, currentY + (RowHeight - IconSize) / 2f, IconSize, IconSize);
                Widgets.ThingIcon(iconRect, animal, null, null, 1f);

                Rect labelRect = new Rect(34f, currentY + 2f, rowRect.width - 70f, IconSize);
                Widgets.Label(labelRect, string.Format("{0}", animal.LabelCap));

                bool isRaceInverted = FatReserveMod.Settings.enabledRaceDefs.Contains(animal.defName);
                bool currentRaceState = isRaceInverted ? !FatReserveMod.Settings.defaultEnabledForNewPawns : FatReserveMod.Settings.defaultEnabledForNewPawns;

                Rect checkRect = new Rect(rowRect.width - 28f, currentY + (RowHeight - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
                Widgets.Checkbox(checkRect.position, ref currentRaceState, CheckboxSize);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rowRect))
                {
                    dragTargetState = !currentRaceState;
                    isDragging = true;
                    ApplyStateToRace(animal.defName, dragTargetState);
                    Event.current.Use();
                }

                if (isDragging && Mouse.IsOver(rowRect))
                {
                    if (currentRaceState != dragTargetState)
                    {
                        ApplyStateToRace(animal.defName, dragTargetState);
                    }
                }

                currentY += RowHeight;
            }

            Widgets.EndScrollView();
            listing.Gap(15f);

            listing.Label("FatReserve_SettingStorageModeLabel".Translate());
            string buttonLabel = string.Format("FatReserve_StorageMode_{0}", FatReserveMod.Settings.storageMode).Translate();
            if (listing.ButtonText(buttonLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("FatReserve_StorageMode_Map".Translate(), () => FatReserveMod.Settings.storageMode = StorageMode.Map),
                    new FloatMenuOption("FatReserve_StorageMode_Pawn".Translate(), () => FatReserveMod.Settings.storageMode = StorageMode.Pawn)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Gap(5f);
            listing.Label("FatReserve_SettingStorageModeDesc".Translate(), -1f);

            if (FatReserveMod.Settings.storageMode == StorageMode.Pawn)
            {
                listing.Gap(5f);
                listing.CheckboxLabeled(
                    "FatReserve_SettingApplyInCaravan".Translate(),
                    ref FatReserveMod.Settings.applyInCaravan
                );
            }
            else
            {
                FatReserveMod.Settings.applyInCaravan = false;
            }

            listing.End();
        }

        private static void ApplyStateToRace(string defName, bool targetState)
        {
            bool globalDefault = FatReserveMod.Settings.defaultEnabledForNewPawns;
            if (targetState != globalDefault)
            {
                if (!FatReserveMod.Settings.enabledRaceDefs.Contains(defName))
                    FatReserveMod.Settings.enabledRaceDefs.Add(defName);
            }
            else
            {
                FatReserveMod.Settings.enabledRaceDefs.Remove(defName);
            }
        }

        private static void MassToggleVisibleAnimals(bool targetState)
        {
            EnsureAnimalDefsCached();
            foreach (ThingDef animal in cachedAnimalDefs)
            {
                if (string.IsNullOrEmpty(searchFilter) || animal.label.ToLower().Contains(searchFilter.ToLower()) || animal.defName.ToLower().Contains(searchFilter.ToLower()))
                {
                    ApplyStateToRace(animal.defName, targetState);
                }
            }
        }

        private static void EnsureAnimalDefsCached()
        {
            if (cachedAnimalDefs != null) return;

            cachedAnimalDefs = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (FatReserveUtility.IsValidFatteningAnimalDef(def))
                {
                    cachedAnimalDefs.Add(def);
                }
            }
            cachedAnimalDefs.SortBy(d => d.label);
        }
    }
}
