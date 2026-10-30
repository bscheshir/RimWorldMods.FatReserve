using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FatReserve
{
    public class PawnColumnWorker_FatReserve : PawnColumnWorker_Checkbox
    {
        private const float CustomWidth = 30f;
        private const float CellSize = 24f;

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), (int)CustomWidth);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            if (this.def.HeaderIcon != null)
            {
                MouseoverSounds.DoRegion(rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    HeaderClicked(rect, table);
                }
                TooltipHandler.TipRegion(rect, () => GetHeaderTip(table), rect.GetHashCode());

                Vector2 headerIconSize = this.def.headerIconSize;
                float x = rect.x + (rect.width - headerIconSize.x) / 2f;
                float y = rect.yMax - headerIconSize.y - 2f;
                Rect iconRect = new Rect(x, y, headerIconSize.x, headerIconSize.y);

                Widgets.DrawTextureFitted(iconRect, this.def.HeaderIcon, 1f);
            }
            else
            {
                base.DoHeader(rect, table);
            }
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (this.HasCheckbox(pawn))
            {
                var manager = pawn.Map?.GetComponent<FatReserveToggleManager>();
                if (manager == null) return;

                bool isEnabled = manager.IsReserveEnabled(pawn);
                bool oldState = isEnabled;

                float xOffset = (rect.width - CellSize) / 2f;
                float yOffset = (rect.height - CellSize) / 2f;
                Vector2 topLeft = new Vector2(rect.x + xOffset, rect.y + yOffset);

                // ЛКМ / Протягивание
                Widgets.Checkbox(topLeft, ref isEnabled, CellSize, disabled: false, paintable: true);

                if (isEnabled != oldState)
                {
                    manager.SetReserveState(pawn, isEnabled);
                }

                // ПКМ по ячейке таблицы животных
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    FatReserveUtility.OpenSharedRaceContextMenu(pawn);
                }
            }
        }

        protected override bool HasCheckbox(Pawn pawn) => pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer;

        protected override bool GetValue(Pawn pawn) => pawn.Map?.GetComponent<FatReserveToggleManager>()?.IsReserveEnabled(pawn) ?? false;

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            var manager = pawn.Map?.GetComponent<FatReserveToggleManager>();
            if (manager != null)
            {
                manager.SetReserveState(pawn, value);
            }
        }
    }
}
