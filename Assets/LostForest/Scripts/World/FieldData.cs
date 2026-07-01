using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class FieldData
    {
        [SerializeField] private int rows;
        [SerializeField] private int columns;
        [SerializeField] private int tileBankSize;
        [SerializeField] private int seed;
        [SerializeField] private List<FieldSlotData> slots = new List<FieldSlotData>();

        public FieldData(int rows, int columns, int tileBankSize, int seed, List<FieldSlotData> slots)
        {
            this.rows = rows;
            this.columns = columns;
            this.tileBankSize = tileBankSize;
            this.seed = seed;
            this.slots = slots ?? new List<FieldSlotData>();
        }

        public int Rows => rows;
        public int Columns => columns;
        public int TileBankSize => tileBankSize;
        public int Seed => seed;
        public IReadOnlyList<FieldSlotData> Slots => slots;
        public int SlotsFilled => slots.Count;
        public int TilesRemaining => Mathf.Max(0, tileBankSize - slots.Count);

        public FieldSlotData GetSlot(int row, int column)
        {
            if (!HexFrameMath.IsInsideFrame(row, column, rows, columns))
            {
                return null;
            }

            int index = row * columns + column;
            return index >= 0 && index < slots.Count ? slots[index] : null;
        }

        public FieldSlotData GetSlot(string address)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Address == address)
                {
                    return slots[i];
                }
            }

            return null;
        }

        public string BuildFieldSlotReport()
        {
            StringBuilder builder = new StringBuilder(slots.Count * 72);
            builder.AppendLine("Lost Forest Phase 2 Field Slot Report");
            builder.AppendLine($"Rows\t{rows}");
            builder.AppendLine($"Columns\t{columns}");
            builder.AppendLine($"Slots Filled\t{SlotsFilled}");
            builder.AppendLine($"Tiles Remaining\t{TilesRemaining}");
            builder.AppendLine($"Seed\t{seed}");
            builder.AppendLine();
            builder.AppendLine("Slot\tRow\tColumn\tAxialQ\tAxialR\tTile\tOrientationIndex\tOrientationDegrees\tRole");

            for (int i = 0; i < slots.Count; i++)
            {
                FieldSlotData slot = slots[i];
                builder.Append(slot.Address);
                builder.Append('\t');
                builder.Append(slot.RowIndex);
                builder.Append('\t');
                builder.Append(slot.ColumnIndex);
                builder.Append('\t');
                builder.Append(slot.AxialQ);
                builder.Append('\t');
                builder.Append(slot.AxialR);
                builder.Append('\t');
                builder.Append(slot.TileIdLabel);
                builder.Append('\t');
                builder.Append(slot.OrientationIndex);
                builder.Append('\t');
                builder.Append(slot.OrientationDegrees.ToString("0"));
                builder.Append('\t');
                builder.Append(slot.Role);
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
