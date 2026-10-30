using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FatReserve
{
    public class Command_FatReserveToggle : Command
    {
        public Pawn pawn;
        public System.Action action;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                return FatReserveUtility.GetSharedRaceContextMenuOptions(pawn);
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (action != null)
            {
                action();
            }
        }

        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            base.DrawIcon(rect, buttonMat, parms);

            if (FatReserveAssets.IconSubmenuMouse != null)
            {
                Rect mouseIconRect = new Rect(rect.xMax - 16f, rect.y + 2f, 16f, 16f);
                Color oldColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(mouseIconRect, FatReserveAssets.IconSubmenuMouse);
                GUI.color = oldColor;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_Pawn_GetGizmos
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (__instance.Faction == Faction.OfPlayer && FatReserveUtility.IsValidFatteningAnimal(__instance))
            {
                var manager = __instance.Map?.GetComponent<FatReserveToggleManager>();
                if (manager == null) yield break;

                bool isActive = manager.IsReserveEnabled(__instance);
                bool isManual = manager.IsPawnStateManual(__instance);
                bool inheritedDefault = manager.GetPawnInheritedDefault(__instance);

                bool currentGlobalTemplate = FatReserveMod.Settings.defaultEnabledForNewPawns;
                bool isRaceCurrentlyInverted = FatReserveMod.Settings.enabledRaceDefs.Contains(__instance.def.defName);
                bool currentRaceTemplate = isRaceCurrentlyInverted ? !currentGlobalTemplate : currentGlobalTemplate;

                StringBuilder descBuilder = new StringBuilder();

                // палитра трёх градаций яркости (Через .Colorize)
                Color midGray = new Color(0.70f, 0.70f, 0.70f);  // Мягкий серебристый для Блока 2 (#B3B3B3)
                Color dimGray = new Color(0.45f, 0.45f, 0.45f);  // Глубокий серый для Блока 3 (#737373)

                // Текущий статус (белый цвет текста)
                string baseStateText = isActive ? "FatReserve_GizmoDesc_StateOn".Translate() : "FatReserve_GizmoDesc_StateOff".Translate();
                descBuilder.Append(baseStateText);

                // Источник изменения - чуть более тусклый
                string reasonText = "";
                if (isManual)
                {
                    reasonText = "FatReserve_GizmoDesc_ReasonManual".Translate();
                }
                else
                {
                    if (isRaceCurrentlyInverted)
                    {
                        reasonText = currentRaceTemplate ? "FatReserve_GizmoDesc_ReasonRaceOn".Translate() : "FatReserve_GizmoDesc_ReasonRaceOff".Translate();
                    }
                    else
                    {
                        reasonText = currentRaceTemplate ? "FatReserve_GizmoDesc_ReasonGlobalOn".Translate() : "FatReserve_GizmoDesc_ReasonGlobalOff".Translate();
                    }
                }
                // цвет второй градации
                descBuilder.Append(reasonText.Colorize(midGray));

                // Отличие от значения по умолчанию
                if (isActive != inheritedDefault)
                {
                    string birthText = inheritedDefault ? "FatReserve_TextStateOn".Translate() : "FatReserve_TextStateOff".Translate();
                    string rawBirthDesc = "FatReserve_GizmoDesc_DiffFromBirth".Translate(birthText);
                    descBuilder.Append(rawBirthDesc.Colorize(dimGray));
                }

                if (inheritedDefault != currentRaceTemplate)
                {
                    string rawTemplateDesc = isRaceCurrentlyInverted ?
                        "FatReserve_GizmoDesc_DiffFromRaceTemplate".Translate(string.Format("{0}", __instance.def.LabelCap)) :
                        "FatReserve_GizmoDesc_DiffFromGlobalTemplate".Translate();

                    descBuilder.Append(rawTemplateDesc.Colorize(dimGray));
                }

                // Подсказка по кнопкам мыши (в цвет третьей градации)
                descBuilder.AppendLine();
                descBuilder.AppendLine();
                descBuilder.Append("FatReserve_GizmoMouseHint".Translate().Colorize(dimGray));

                Texture2D dynamicIcon = isActive ? FatReserveAssets.IconFatReserveOn : FatReserveAssets.IconFatReserveOff;

                Command_FatReserveToggle toggleBtn = new Command_FatReserveToggle
                {
                    pawn = __instance,
                    defaultLabel = "FatReserve_GizmoLabel".Translate(),
                    defaultDesc = descBuilder.ToString(),
                    icon = dynamicIcon,
                    action = delegate
                    {
                        manager.SetReserveStateManual(__instance, !isActive);
                    }
                };

                yield return toggleBtn;
            }
        }
    }
}
