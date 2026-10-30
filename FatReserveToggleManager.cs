using System;
using System.Collections.Generic;
using Verse;

namespace FatReserve
{
    public class FatReserveToggleManager : MapComponent
    {
        // Key = ID животного, Value = (Выбор игрока [null/true/false], Зафиксированный дефолт [true/false])
        private Dictionary<int, Tuple<bool?, bool>> pawnStates = new Dictionary<int, Tuple<bool?, bool>>();

        public FatReserveToggleManager(Map map) : base(map) { }

        public static FatReserveToggleManager GetManagerForCurrentMap()
        {
            return Find.CurrentMap?.GetComponent<FatReserveToggleManager>();
        }

        // Честная проверка на ручной ввод: игрок выбрал значение лично (первое поле не null)
        public bool IsPawnStateManual(Pawn pawn)
        {
            if (pawn == null) return false;

            if (FatReserveMod.Settings.storageMode == StorageMode.Pawn)
            {
                var comp = pawn.TryGetComp<CompFatReserve>();
                // Если компонент есть, инициализирован (inherited != null) и игрок сделал выбор (enabled != null)
                return comp != null && comp.inheritedDefault != null && comp.isReserveEnabled != null;
            }
            else // StorageMode.Map
            {
                // Наличие записи в словаре + первый элемент кортежа не null означает ручной клик особи
                return pawnStates.TryGetValue(pawn.thingIDNumber, out var state) && state.Item1 != null;
            }
        }

        // Получение врождённого дефолта особи для подсказки гизмо
        public bool GetPawnInheritedDefault(Pawn pawn)
        {
            if (pawn == null) return false;

            if (FatReserveMod.Settings.storageMode == StorageMode.Pawn)
            {
                var comp = pawn.TryGetComp<CompFatReserve>();
                return comp?.inheritedDefault ?? GetCurrentTemplateStateForRace(pawn);
            }
            else
            {
                if (pawnStates.TryGetValue(pawn.thingIDNumber, out var state)) return state.Item2;
                return GetCurrentTemplateStateForRace(pawn);
            }
        }

        public bool IsReserveEnabled(Pawn pawn)
        {
            if (pawn == null) return false;

            if (FatReserveMod.Settings.storageMode == StorageMode.Pawn)
            {
                var comp = FatReserveUtility.EnsureCompInjected(pawn);
                if (comp != null)
                {
                    // ПЕРВАЯ ОБРАБОТКА: Если врожденный дефолт null, фиксируем его раз и навсегда
                    if (comp.inheritedDefault == null)
                    {
                        comp.inheritedDefault = GetCurrentTemplateStateForRace(pawn);
                        comp.isReserveEnabled = null; // Выбор игрока чист
                    }
                    return comp.isReserveEnabled ?? comp.inheritedDefault.Value;
                }
            }
            else // StorageMode.Map
            {
                // ПЕРВАЯ ОБРАБОТКА: Если записи в словаре нет, создаем её и фиксируем врожденный дефолт
                if (!pawnStates.TryGetValue(pawn.thingIDNumber, out var state))
                {
                    bool initialDefault = GetCurrentTemplateStateForRace(pawn);
                    state = new Tuple<bool?, bool>(null, initialDefault);
                    pawnStates[pawn.thingIDNumber] = state;
                }
                return state.Item1 ?? state.Item2;
            }

            return GetCurrentTemplateStateForRace(pawn);
        }

        // Запись ручного клика (ЛКМ гизмо, ячейка таблицы, или пункты 1-4 меню)
        public void SetReserveStateManual(Pawn pawn, bool state)
        {
            if (pawn == null) return;

            // Гарантируем прохождение первой обработки и фиксации дефолта перед записью клика
            IsReserveEnabled(pawn);

            if (FatReserveMod.Settings.storageMode == StorageMode.Pawn)
            {
                var comp = FatReserveUtility.EnsureCompInjected(pawn);
                if (comp != null) comp.isReserveEnabled = state;
            }
            else // StorageMode.Map
            {
                if (pawnStates.TryGetValue(pawn.thingIDNumber, out var currentState))
                {
                    pawnStates[pawn.thingIDNumber] = new Tuple<bool?, bool>(state, currentState.Item2);
                }
            }
        }

        public void SetReserveState(Pawn pawn, bool state)
        {
            SetReserveStateManual(pawn, state);
        }

        public bool GetCurrentTemplateStateForRace(Pawn pawn)
        {
            if (pawn.def?.defName == null) return FatReserveMod.Settings.defaultEnabledForNewPawns;

            bool isRaceInverted = FatReserveMod.Settings.enabledRaceDefs.Contains(pawn.def.defName);
            return isRaceInverted ? !FatReserveMod.Settings.defaultEnabledForNewPawns : FatReserveMod.Settings.defaultEnabledForNewPawns;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            List<int> keys = null;
            List<bool?> userChoices = null;
            List<bool> inheritedDefaults = null;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                keys = new List<int>(pawnStates.Keys);
                userChoices = new List<bool?>();
                inheritedDefaults = new List<bool>();
                foreach (var kvp in pawnStates)
                {
                    userChoices.Add(kvp.Value.Item1);
                    inheritedDefaults.Add(kvp.Value.Item2);
                }
            }

            Scribe_Collections.Look(ref keys, "pawnStates_Keys", LookMode.Value);
            Scribe_Collections.Look(ref userChoices, "pawnStates_Choices", LookMode.Value);
            Scribe_Collections.Look(ref inheritedDefaults, "pawnStates_Defaults", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pawnStates = new Dictionary<int, Tuple<bool?, bool>>();
                if (keys != null && userChoices != null && inheritedDefaults != null)
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (i < userChoices.Count && i < inheritedDefaults.Count)
                        {
                            pawnStates[keys[i]] = new Tuple<bool?, bool>(userChoices[i], inheritedDefaults[i]);
                        }
                    }
                }
            }
        }
    }
}
