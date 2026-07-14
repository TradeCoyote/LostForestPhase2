using UnityEngine;

namespace LostForest.Phase2.World
{
    public static class HexFrameMath
    {
        private static readonly Vector2Int[] AxialNeighborOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1)
        };

        public static string GetAddress(int row, int column)
        {
            return $"{(char)('A' + row)}{column + 1}";
        }

        public static Vector2Int OffsetToAxial(int row, int column)
        {
            int q = column;
            int r = row - column / 2;
            return new Vector2Int(q, r);
        }

        public static Vector2Int AxialToOffset(Vector2Int axial)
        {
            int column = axial.x;
            int row = axial.y + column / 2;
            return new Vector2Int(row, column);
        }

        public static Vector2Int GetAxialNeighbor(Vector2Int axial, HexDirection direction)
        {
            return axial + AxialNeighborOffsets[(int)direction];
        }

        public static bool IsInsideFrame(int row, int column, int rows, int columns)
        {
            return row >= 0 && row < rows && column >= 0 && column < columns;
        }

        public static int GetHexDistance(Vector2Int originAxial, Vector2Int destinationAxial)
        {
            int deltaQ = destinationAxial.x - originAxial.x;
            int deltaR = destinationAxial.y - originAxial.y;
            return (Mathf.Abs(deltaQ) + Mathf.Abs(deltaR) + Mathf.Abs(deltaQ + deltaR)) / 2;
        }

        public static Vector3 GetFlatTopHexCenter(int row, int column, float outerRadiusMeters)
        {
            float x = outerRadiusMeters * 1.5f * column;
            float z = Mathf.Sqrt(3f) * outerRadiusMeters * (row + 0.5f * (column & 1));
            return new Vector3(x, 0f, z);
        }

        public static Vector3 GetFlatTopHexCenterFromAxial(Vector2Int axial, float outerRadiusMeters)
        {
            float x = outerRadiusMeters * 1.5f * axial.x;
            float z = Mathf.Sqrt(3f) * outerRadiusMeters * (axial.y + axial.x * 0.5f);
            return new Vector3(x, 0f, z);
        }
    }
}
