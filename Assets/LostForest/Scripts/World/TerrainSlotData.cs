using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class TerrainSlotData
    {
        [SerializeField] private string label;
        [SerializeField] private int axialQ;
        [SerializeField] private int axialR;
        [SerializeField] private Vector3 worldCenter;
        [SerializeField] private SharedHeightPoint centerPoint;
        [SerializeField] private List<SharedHeightPoint> vertexPoints = new List<SharedHeightPoint>();
        [SerializeField] private List<SharedHeightPoint> edgeMidpointPoints = new List<SharedHeightPoint>();
        [SerializeField] private List<SharedHeightPoint> innerPoints = new List<SharedHeightPoint>();
        [SerializeField] private int[] neighborIndices = { -1, -1, -1, -1, -1, -1 };

        public TerrainSlotData(
            string label,
            Vector2Int axialCoordinate,
            Vector3 worldCenter,
            SharedHeightPoint centerPoint,
            IEnumerable<SharedHeightPoint> vertexPoints,
            IEnumerable<SharedHeightPoint> edgeMidpointPoints,
            IEnumerable<SharedHeightPoint> innerPoints)
        {
            this.label = label;
            axialQ = axialCoordinate.x;
            axialR = axialCoordinate.y;
            this.worldCenter = worldCenter;
            this.centerPoint = centerPoint;
            this.vertexPoints = vertexPoints == null ? new List<SharedHeightPoint>() : new List<SharedHeightPoint>(vertexPoints);
            this.edgeMidpointPoints = edgeMidpointPoints == null ? new List<SharedHeightPoint>() : new List<SharedHeightPoint>(edgeMidpointPoints);
            this.innerPoints = innerPoints == null ? new List<SharedHeightPoint>() : new List<SharedHeightPoint>(innerPoints);
        }

        public string Label => label;
        public int AxialQ => axialQ;
        public int AxialR => axialR;
        public Vector2Int AxialCoordinate => new Vector2Int(axialQ, axialR);
        public Vector3 WorldCenter => worldCenter;
        public SharedHeightPoint CenterPoint => centerPoint;
        public IReadOnlyList<SharedHeightPoint> VertexPoints => vertexPoints;
        public IReadOnlyList<SharedHeightPoint> EdgeMidpointPoints => edgeMidpointPoints;
        public IReadOnlyList<SharedHeightPoint> InnerPoints => innerPoints;

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
