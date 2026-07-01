using System;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class FieldSlotData
    {
        [SerializeField] private string address;
        [SerializeField] private int rowIndex;
        [SerializeField] private int columnIndex;
        [SerializeField] private int axialQ;
        [SerializeField] private int axialR;
        [SerializeField] private int tileId;
        [SerializeField] private int orientationIndex;
        [SerializeField] private FieldSlotRole role;
        [SerializeField] private Vector3 worldCenter;
        [SerializeField] private int[] neighborIndices = { -1, -1, -1, -1, -1, -1 };

        public FieldSlotData(
            string address,
            int rowIndex,
            int columnIndex,
            Vector2Int axialCoordinate,
            Vector3 worldCenter,
            int tileId,
            int orientationIndex,
            FieldSlotRole role)
        {
            this.address = address;
            this.rowIndex = rowIndex;
            this.columnIndex = columnIndex;
            axialQ = axialCoordinate.x;
            axialR = axialCoordinate.y;
            this.worldCenter = worldCenter;
            this.tileId = tileId;
            this.orientationIndex = Mathf.Clamp(orientationIndex, 0, 5);
            this.role = role;
        }

        public string Address => address;
        public int RowIndex => rowIndex;
        public int ColumnIndex => columnIndex;
        public int AxialQ => axialQ;
        public int AxialR => axialR;
        public Vector2Int AxialCoordinate => new Vector2Int(axialQ, axialR);
        public Vector3 WorldCenter => worldCenter;
        public int TileId => tileId;
        public string TileIdLabel => tileId.ToString("D3");
        public int OrientationIndex => orientationIndex;
        public float OrientationDegrees => orientationIndex * 60f;
        public FieldSlotRole Role => role;

        public int GetNeighborIndex(HexDirection direction)
        {
            return neighborIndices[(int)direction];
        }

        public void SetNeighborIndex(HexDirection direction, int slotIndex)
        {
            neighborIndices[(int)direction] = slotIndex;
        }
    }
}
