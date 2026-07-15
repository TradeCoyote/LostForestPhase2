using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class TerrainFrameData
    {
        public const float PointIdScale = 100f;

        [SerializeField] private TerrainFrameSettings settings;
        [SerializeField] private List<TerrainSlotData> slots = new List<TerrainSlotData>();
        [SerializeField] private List<SharedHeightPoint> sharedPointList = new List<SharedHeightPoint>();
        [SerializeField] private int localPointReferenceCount;
        [SerializeField] private int reusedPointReferenceCount;

        private readonly Dictionary<string, SharedHeightPoint> sharedPoints = new Dictionary<string, SharedHeightPoint>();

        public TerrainFrameData(
            TerrainFrameSettings settings,
            IEnumerable<TerrainSlotData> slots,
            IEnumerable<SharedHeightPoint> sharedPoints,
            int localPointReferenceCount,
            int reusedPointReferenceCount)
        {
            this.settings = settings ?? new TerrainFrameSettings();
            this.slots = slots == null ? new List<TerrainSlotData>() : new List<TerrainSlotData>(slots);
            sharedPointList = sharedPoints == null ? new List<SharedHeightPoint>() : new List<SharedHeightPoint>(sharedPoints);
            this.localPointReferenceCount = Mathf.Max(0, localPointReferenceCount);
            this.reusedPointReferenceCount = Mathf.Max(0, reusedPointReferenceCount);

            RebuildSharedPointLookup();
        }

        public TerrainFrameSettings Settings => settings;
        public IReadOnlyList<TerrainSlotData> Slots => slots;
        public IReadOnlyList<SharedHeightPoint> SharedPointList => sharedPointList;
        public IReadOnlyDictionary<string, SharedHeightPoint> SharedPoints => sharedPoints;
        public int SlotCount => slots.Count;
        public int SharedPointCount => sharedPointList.Count;
        public int LocalPointReferenceCount => localPointReferenceCount;
        public int ReusedPointReferenceCount => reusedPointReferenceCount;
        public bool HasSharedBoundaryReuse => HasMultiReferencePoint(TerrainPointKind.Vertex) && HasMultiReferencePoint(TerrainPointKind.EdgeMidpoint);

        public static string GetPointIdFromWorldPosition(Vector3 position)
        {
            int x = Mathf.RoundToInt(position.x * PointIdScale);
            int z = Mathf.RoundToInt(position.z * PointIdScale);
            return $"HP_{x}_{z}";
        }

        public bool TryGetSharedPoint(string pointId, out SharedHeightPoint point)
        {
            if (string.IsNullOrWhiteSpace(pointId))
            {
                point = null;
                return false;
            }

            return sharedPoints.TryGetValue(pointId, out point);
        }

        public bool TryGetSharedPointAtPosition(Vector3 worldPosition, out SharedHeightPoint point)
        {
            return TryGetSharedPoint(GetPointIdFromWorldPosition(worldPosition), out point);
        }

        public int CountPointsByKind(TerrainPointKind kind)
        {
            int count = 0;

            for (int i = 0; i < sharedPointList.Count; i++)
            {
                if (sharedPointList[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountMultiReferencePoints()
        {
            int count = 0;

            for (int i = 0; i < sharedPointList.Count; i++)
            {
                if (sharedPointList[i].ReferenceCount > 1)
                {
                    count++;
                }
            }

            return count;
        }

        public SharedHeightPoint GetFirstMultiReferencePoint()
        {
            for (int i = 0; i < sharedPointList.Count; i++)
            {
                if (sharedPointList[i].ReferenceCount > 1)
                {
                    return sharedPointList[i];
                }
            }

            return null;
        }

        private bool HasMultiReferencePoint(TerrainPointKind kind)
        {
            for (int i = 0; i < sharedPointList.Count; i++)
            {
                SharedHeightPoint point = sharedPointList[i];

                if (point.Kind == kind && point.ReferenceCount > 1)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildSharedPointLookup()
        {
            sharedPoints.Clear();

            for (int i = 0; i < sharedPointList.Count; i++)
            {
                SharedHeightPoint point = sharedPointList[i];

                if (point == null || string.IsNullOrWhiteSpace(point.PointId))
                {
                    continue;
                }

                sharedPoints[point.PointId] = point;
            }
        }
    }
}
