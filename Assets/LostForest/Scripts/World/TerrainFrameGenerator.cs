using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public static class TerrainFrameGenerator
    {
        public static TerrainFrameData GenerateRadiusOne(TerrainFrameSettings settings)
        {
            if (settings == null)
            {
                settings = new TerrainFrameSettings();
            }

            Dictionary<string, SharedHeightPoint> sharedPointLookup = new Dictionary<string, SharedHeightPoint>();
            List<SharedHeightPoint> sharedPoints = new List<SharedHeightPoint>();
            List<TerrainSlotData> slots = new List<TerrainSlotData>(7);
            int localPointReferenceCount = 0;
            int reusedPointReferenceCount = 0;
            List<Vector2Int> axialSlots = GetRadiusOneAxialSlots();

            for (int i = 0; i < axialSlots.Count; i++)
            {
                TerrainSlotData slot = BuildSlot(
                    axialSlots[i],
                    settings,
                    sharedPointLookup,
                    sharedPoints,
                    ref localPointReferenceCount,
                    ref reusedPointReferenceCount);

                slots.Add(slot);
            }

            BuildNeighborIndices(slots);
            return new TerrainFrameData(settings, slots, sharedPoints, localPointReferenceCount, reusedPointReferenceCount);
        }

        private static TerrainSlotData BuildSlot(
            Vector2Int axial,
            TerrainFrameSettings settings,
            Dictionary<string, SharedHeightPoint> sharedPointLookup,
            List<SharedHeightPoint> sharedPoints,
            ref int localPointReferenceCount,
            ref int reusedPointReferenceCount)
        {
            string slotLabel = GetSlotLabel(axial);
            Vector3 center = HexFrameMath.GetFlatTopHexCenterFromAxial(axial, settings.HexOuterRadiusMeters);
            Vector3[] vertices = GetVertices(center, settings.HexOuterRadiusMeters);
            Vector3[] edgeMidpoints = GetEdgeMidpoints(vertices);
            Vector3[] innerPoints = GetInnerPoints(center, vertices);

            SharedHeightPoint centerPoint = RegisterPoint(
                slotLabel,
                "C",
                TerrainPointKind.Center,
                center,
                settings,
                sharedPointLookup,
                sharedPoints,
                ref localPointReferenceCount,
                ref reusedPointReferenceCount);

            SharedHeightPoint[] vertexPoints = RegisterPoints(
                slotLabel,
                "V",
                TerrainPointKind.Vertex,
                vertices,
                settings,
                sharedPointLookup,
                sharedPoints,
                ref localPointReferenceCount,
                ref reusedPointReferenceCount);

            SharedHeightPoint[] edgePoints = RegisterPoints(
                slotLabel,
                "E",
                TerrainPointKind.EdgeMidpoint,
                edgeMidpoints,
                settings,
                sharedPointLookup,
                sharedPoints,
                ref localPointReferenceCount,
                ref reusedPointReferenceCount);

            SharedHeightPoint[] innerHeightPoints = RegisterPoints(
                slotLabel,
                "I",
                TerrainPointKind.Inner,
                innerPoints,
                settings,
                sharedPointLookup,
                sharedPoints,
                ref localPointReferenceCount,
                ref reusedPointReferenceCount);

            return new TerrainSlotData(slotLabel, axial, center, centerPoint, vertexPoints, edgePoints, innerHeightPoints);
        }

        private static SharedHeightPoint[] RegisterPoints(
            string slotLabel,
            string labelPrefix,
            TerrainPointKind kind,
            IReadOnlyList<Vector3> positions,
            TerrainFrameSettings settings,
            Dictionary<string, SharedHeightPoint> sharedPointLookup,
            List<SharedHeightPoint> sharedPoints,
            ref int localPointReferenceCount,
            ref int reusedPointReferenceCount)
        {
            SharedHeightPoint[] points = new SharedHeightPoint[positions.Count];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = RegisterPoint(
                    slotLabel,
                    $"{labelPrefix}{i}",
                    kind,
                    positions[i],
                    settings,
                    sharedPointLookup,
                    sharedPoints,
                    ref localPointReferenceCount,
                    ref reusedPointReferenceCount);
            }

            return points;
        }

        private static SharedHeightPoint RegisterPoint(
            string slotLabel,
            string localLabel,
            TerrainPointKind kind,
            Vector3 planarPosition,
            TerrainFrameSettings settings,
            Dictionary<string, SharedHeightPoint> sharedPointLookup,
            List<SharedHeightPoint> sharedPoints,
            ref int localPointReferenceCount,
            ref int reusedPointReferenceCount)
        {
            string pointId = TerrainFrameData.GetPointIdFromWorldPosition(planarPosition);
            string localReference = $"{slotLabel}.{localLabel}";
            localPointReferenceCount++;

            if (!sharedPointLookup.TryGetValue(pointId, out SharedHeightPoint sharedPoint))
            {
                float height = GetHeight(planarPosition, settings);
                Vector3 elevatedPosition = new Vector3(planarPosition.x, height * settings.VisualHeightMultiplier, planarPosition.z);
                sharedPoint = new SharedHeightPoint(pointId, kind, elevatedPosition, height);
                sharedPointLookup.Add(pointId, sharedPoint);
                sharedPoints.Add(sharedPoint);
            }
            else
            {
                reusedPointReferenceCount++;
            }

            sharedPoint.AddLocalReference(localReference);
            return sharedPoint;
        }

        private static Vector3[] GetVertices(Vector3 center, float outerRadiusMeters)
        {
            Vector3[] vertices = new Vector3[6];

            for (int i = 0; i < vertices.Length; i++)
            {
                float radians = Mathf.Deg2Rad * (60f * i);
                vertices[i] = center + new Vector3(Mathf.Cos(radians) * outerRadiusMeters, 0f, Mathf.Sin(radians) * outerRadiusMeters);
            }

            return vertices;
        }

        private static Vector3[] GetEdgeMidpoints(IReadOnlyList<Vector3> vertices)
        {
            Vector3[] edgeMidpoints = new Vector3[6];

            for (int i = 0; i < edgeMidpoints.Length; i++)
            {
                edgeMidpoints[i] = (vertices[i] + vertices[(i + 1) % 6]) * 0.5f;
            }

            return edgeMidpoints;
        }

        private static Vector3[] GetInnerPoints(Vector3 center, IReadOnlyList<Vector3> vertices)
        {
            Vector3[] innerPoints = new Vector3[6];

            for (int i = 0; i < innerPoints.Length; i++)
            {
                innerPoints[i] = Vector3.Lerp(center, vertices[i], 0.5f);
            }

            return innerPoints;
        }

        private static List<Vector2Int> GetRadiusOneAxialSlots()
        {
            return new List<Vector2Int>
            {
                Vector2Int.zero,
                new Vector2Int(1, 0),
                new Vector2Int(1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int(0, 1)
            };
        }

        private static void BuildNeighborIndices(IReadOnlyList<TerrainSlotData> slots)
        {
            Dictionary<Vector2Int, int> slotIndexByAxial = new Dictionary<Vector2Int, int>();

            for (int i = 0; i < slots.Count; i++)
            {
                slotIndexByAxial[slots[i].AxialCoordinate] = i;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                TerrainSlotData slot = slots[i];

                for (int directionIndex = 0; directionIndex < 6; directionIndex++)
                {
                    HexDirection direction = (HexDirection)directionIndex;
                    Vector2Int neighborAxial = HexFrameMath.GetAxialNeighbor(slot.AxialCoordinate, direction);

                    if (slotIndexByAxial.TryGetValue(neighborAxial, out int neighborIndex))
                    {
                        slot.SetNeighborIndex(direction, neighborIndex);
                        continue;
                    }

                    slot.SetNeighborIndex(direction, -1);
                }
            }
        }

        private static string GetSlotLabel(Vector2Int axial)
        {
            if (axial == Vector2Int.zero)
            {
                return "Center";
            }

            if (axial == new Vector2Int(1, 0))
            {
                return "East";
            }

            if (axial == new Vector2Int(1, -1))
            {
                return "Northeast";
            }

            if (axial == new Vector2Int(0, -1))
            {
                return "Northwest";
            }

            if (axial == new Vector2Int(-1, 0))
            {
                return "West";
            }

            if (axial == new Vector2Int(-1, 1))
            {
                return "Southwest";
            }

            return "Southeast";
        }

        private static float GetHeight(Vector3 planarPosition, TerrainFrameSettings settings)
        {
            float broad = Mathf.Sin((planarPosition.x + settings.HeightSeed * 0.37f) * settings.BroadHeightScale)
                + Mathf.Cos((planarPosition.z - settings.HeightSeed * 0.23f) * settings.BroadHeightScale);
            broad *= 0.5f;

            float noise = Mathf.PerlinNoise(
                settings.HeightSeed * 0.011f + planarPosition.x * settings.NoiseHeightScale,
                settings.HeightSeed * 0.017f + planarPosition.z * settings.NoiseHeightScale) - 0.5f;

            return (broad * 0.65f + noise * 0.7f) * settings.HeightAmplitudeMeters;
        }
    }
}
