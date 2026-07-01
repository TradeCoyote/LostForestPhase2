using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public static class FieldGenerator
    {
        public static FieldData Generate(FrameSettings settings)
        {
            if (settings == null)
            {
                settings = new FrameSettings();
            }

            int rows = settings.Rows;
            int columns = settings.Columns;
            int slotCount = rows * columns;
            int seed = settings.UseRandomSeed ? Environment.TickCount : settings.Seed;
            System.Random random = new System.Random(seed);
            List<int> tileBank = BuildTileBank(settings.TileBankSize);

            Vector2Int playerHomeCoordinate = GetMiddleRegionCoordinate(rows, columns, random);
            Vector2Int pursuerCoordinate = GetOuterLaneCoordinate(rows, columns, random);

            while (pursuerCoordinate == playerHomeCoordinate && slotCount > 1)
            {
                pursuerCoordinate = GetOuterLaneCoordinate(rows, columns, random);
            }

            tileBank.Remove(FrameSettings.PlayerHomeTileId);
            tileBank.Remove(FrameSettings.PursuerTileId);

            List<FieldSlotData> slots = new List<FieldSlotData>(slotCount);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector2Int coordinate = new Vector2Int(row, column);
                    FieldSlotRole role = FieldSlotRole.Field;
                    int tileId;

                    if (coordinate == playerHomeCoordinate)
                    {
                        role = FieldSlotRole.PlayerHomeSpawn;
                        tileId = FrameSettings.PlayerHomeTileId;
                    }
                    else if (coordinate == pursuerCoordinate)
                    {
                        role = FieldSlotRole.PursuerSpawn;
                        tileId = FrameSettings.PursuerTileId;
                    }
                    else
                    {
                        int bankIndex = random.Next(0, tileBank.Count);
                        tileId = tileBank[bankIndex];
                        tileBank.RemoveAt(bankIndex);
                    }

                    int orientation = random.Next(0, 6);
                    Vector2Int axial = HexFrameMath.OffsetToAxial(row, column);
                    Vector3 worldCenter = HexFrameMath.GetFlatTopHexCenter(row, column, settings.HexOuterRadiusMeters);

                    slots.Add(new FieldSlotData(
                        HexFrameMath.GetAddress(row, column),
                        row,
                        column,
                        axial,
                        worldCenter,
                        tileId,
                        orientation,
                        role));
                }
            }

            BuildNeighborIndices(slots, rows, columns);
            return new FieldData(rows, columns, settings.TileBankSize, seed, slots);
        }

        private static List<int> BuildTileBank(int tileBankSize)
        {
            List<int> tileBank = new List<int>(tileBankSize);

            for (int tileId = 0; tileId < tileBankSize; tileId++)
            {
                tileBank.Add(tileId);
            }

            return tileBank;
        }

        private static Vector2Int GetMiddleRegionCoordinate(int rows, int columns, System.Random random)
        {
            int minRow = Mathf.FloorToInt(rows * 0.36f);
            int maxRowExclusive = Mathf.Max(minRow + 1, Mathf.CeilToInt(rows * 0.64f));
            int minColumn = Mathf.FloorToInt(columns * 0.36f);
            int maxColumnExclusive = Mathf.Max(minColumn + 1, Mathf.CeilToInt(columns * 0.64f));

            return new Vector2Int(
                random.Next(minRow, Mathf.Min(rows, maxRowExclusive)),
                random.Next(minColumn, Mathf.Min(columns, maxColumnExclusive)));
        }

        private static Vector2Int GetOuterLaneCoordinate(int rows, int columns, System.Random random)
        {
            int laneDepth = Mathf.Max(1, Mathf.Min(3, Mathf.Min(rows, columns) / 3));
            int row;
            int column;

            do
            {
                row = random.Next(0, rows);
                column = random.Next(0, columns);
            }
            while (!IsOuterLane(row, column, rows, columns, laneDepth));

            return new Vector2Int(row, column);
        }

        private static bool IsOuterLane(int row, int column, int rows, int columns, int laneDepth)
        {
            return row < laneDepth || row >= rows - laneDepth || column < laneDepth || column >= columns - laneDepth;
        }

        private static void BuildNeighborIndices(IReadOnlyList<FieldSlotData> slots, int rows, int columns)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                FieldSlotData slot = slots[i];

                for (int directionIndex = 0; directionIndex < 6; directionIndex++)
                {
                    HexDirection direction = (HexDirection)directionIndex;
                    Vector2Int neighborAxial = HexFrameMath.GetAxialNeighbor(slot.AxialCoordinate, direction);
                    Vector2Int neighborOffset = HexFrameMath.AxialToOffset(neighborAxial);

                    if (!HexFrameMath.IsInsideFrame(neighborOffset.x, neighborOffset.y, rows, columns))
                    {
                        slot.SetNeighborIndex(direction, -1);
                        continue;
                    }

                    slot.SetNeighborIndex(direction, neighborOffset.x * columns + neighborOffset.y);
                }
            }
        }
    }
}
