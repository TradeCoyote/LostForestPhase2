using System;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    [Serializable]
    public sealed class TileInstance
    {
        [SerializeField] private string instanceId;
        [SerializeField] private int tileId;
        [SerializeField] private string tileDebugName;
        [SerializeField] private string slotAddress;
        [SerializeField] private int rowIndex;
        [SerializeField] private int columnIndex;
        [SerializeField] private int axialQ;
        [SerializeField] private int axialR;
        [SerializeField] private int orientationIndex;
        [SerializeField] private Vector3 worldPosition;

        public TileInstance(TileDefinition definition, FieldSlotData slot)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (slot == null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            instanceId = $"{slot.Address}-Tile-{definition.TileIdLabel}";
            tileId = definition.TileId;
            tileDebugName = definition.DebugName;
            slotAddress = slot.Address;
            rowIndex = slot.RowIndex;
            columnIndex = slot.ColumnIndex;
            axialQ = slot.AxialQ;
            axialR = slot.AxialR;
            orientationIndex = Mathf.Clamp(slot.OrientationIndex, 0, 5);
            worldPosition = slot.WorldCenter;
        }

        public string InstanceId => instanceId;
        public int TileId => tileId;
        public string TileIdLabel => tileId.ToString("D3");
        public string TileDebugName => tileDebugName;
        public string SlotAddress => slotAddress;
        public int RowIndex => rowIndex;
        public int ColumnIndex => columnIndex;
        public int AxialQ => axialQ;
        public int AxialR => axialR;
        public Vector2Int AxialCoordinate => new Vector2Int(axialQ, axialR);
        public int OrientationIndex => orientationIndex;
        public float OrientationDegrees => orientationIndex * 60f;
        public Vector3 WorldPosition => worldPosition;

        public Vector3 GetPlacedContentAnchorPosition(TileDefinition definition, TileAnchor anchor)
        {
            return TileHexAnchorMath.ToPlacedContentPosition(
                worldPosition,
                anchor.LocalPosition,
                orientationIndex,
                definition.ContentSupportsRotation && anchor.RotatesWithTileContent);
        }

        public string BuildDebugSummary()
        {
            return $"TileInstance {InstanceId}: Tile={TileIdLabel} ({TileDebugName}), Slot={slotAddress}, Axial=({axialQ},{axialR}), World={worldPosition}, Orientation={orientationIndex}/{OrientationDegrees:0}deg";
        }
    }
}
