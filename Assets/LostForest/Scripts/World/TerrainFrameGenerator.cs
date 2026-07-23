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

        public static TerrainFrameData GenerateForFieldSlots(TerrainFrameSettings settings, IEnumerable<FieldSlotData> fieldSlots)
        {
            if (settings == null)
            {
                settings = new TerrainFrameSettings();
            }

            Dictionary<string, SharedHeightPoint> sharedPointLookup = new Dictionary<string, SharedHeightPoint>();
            List<SharedHeightPoint> sharedPoints = new List<SharedHeightPoint>();
            List<TerrainSlotData> slots = new List<TerrainSlotData>();
            int localPointReferenceCount = 0;
            int reusedPointReferenceCount = 0;

            if (fieldSlots != null)
            {
                foreach (FieldSlotData fieldSlot in fieldSlots)
                {
                    if (fieldSlot == null)
                    {
                        continue;
                    }

                    TerrainSlotData slot = BuildSlot(
                        fieldSlot.Address,
                        fieldSlot.AxialCoordinate,
                        fieldSlot.WorldCenter,
                        settings,
                        sharedPointLookup,
                        sharedPoints,
                        ref localPointReferenceCount,
                        ref reusedPointReferenceCount);

                    slots.Add(slot);
                }
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
            return BuildSlot(
                slotLabel,
                axial,
                center,
                settings,
                sharedPointLookup,
                sharedPoints,
                ref localPointReferenceCount,
                ref reusedPointReferenceCount);
        }

        private static TerrainSlotData BuildSlot(
            string slotLabel,
            Vector2Int axial,
            Vector3 center,
            TerrainFrameSettings settings,
            Dictionary<string, SharedHeightPoint> sharedPointLookup,
            List<SharedHeightPoint> sharedPoints,
            ref int localPointReferenceCount,
            ref int reusedPointReferenceCount)
        {
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
                float height = GetLogicalHeightAtWorldPosition(planarPosition, settings);
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

        public static float GetLogicalHeightAtWorldPosition(Vector3 planarPosition, TerrainFrameSettings settings)
        {
            if (settings == null)
            {
                settings = new TerrainFrameSettings();
            }

            return BuildHeightProfile(planarPosition, settings).LogicalHeightMeters;
        }

        public static TerrainLandform GetLandformAtWorldPosition(Vector3 planarPosition, TerrainFrameSettings settings)
        {
            if (settings == null)
            {
                settings = new TerrainFrameSettings();
            }

            return BuildHeightProfile(planarPosition, settings).ResolveLandform();
        }

        private static HeightProfile BuildHeightProfile(Vector3 planarPosition, TerrainFrameSettings settings)
        {
            float broadScale = Mathf.Max(0.0001f, settings.BroadHeightScale);
            float detailScale = Mathf.Max(0.0001f, settings.NoiseHeightScale);
            float flatWidth = Mathf.Max(1f, settings.HexFlatToFlatMeters);
            Vector2 planar = ToPlanar(planarPosition);
            Vector2 home = ToPlanar(settings.HomeWorldCenter);
            Vector2 fromHome = planar - home;
            float distanceFromHome = fromHome.magnitude;

            Vector2 slopeAxis = GetSeedAxis(settings.HeightSeed, 17.25f);
            Vector2 crossSlopeAxis = new Vector2(-slopeAxis.y, slopeAxis.x);
            Vector2 ridgeAxis = GetSeedAxis(settings.HeightSeed, 131.5f);
            Vector2 valleyAxis = GetSeedAxis(settings.HeightSeed, 249.75f);
            Vector2 mesaAxis = GetSeedAxis(settings.HeightSeed, 303.5f);
            Vector2 mesaCrossAxis = new Vector2(-mesaAxis.y, mesaAxis.x);
            float homeCalm = Smooth01(distanceFromHome / (flatWidth * 2.6f));
            float macroInfluence = Mathf.Lerp(0.35f, 1f, homeCalm);

            float directionalSlopeRaw = Mathf.Clamp(Vector2.Dot(fromHome, slopeAxis) / (flatWidth * 10.5f), -1f, 1f);
            float directionalSlope = directionalSlopeRaw * 0.24f;
            float homeBasinStrength = SmoothFalloff(distanceFromHome / (flatWidth * 2.35f));
            float homeBasin = -homeBasinStrength * 0.24f;
            float outerRiseStrength = Smooth01((distanceFromHome - flatWidth * 2.4f) / (flatWidth * 12.5f));
            float outerRise = outerRiseStrength * 0.30f;
            float ridgeRaw = Mathf.Sin(Vector2.Dot(fromHome, ridgeAxis) * broadScale + settings.HeightSeed * 0.031f);
            float ridgeCrestStrength = Mathf.Clamp01(ridgeRaw * macroInfluence);
            float ridge = ridgeRaw * 0.28f * macroInfluence;
            float ridgeShoulder = Mathf.Sin(Vector2.Dot(fromHome, ridgeAxis) * broadScale * 0.46f - settings.HeightSeed * 0.014f) * 0.14f * macroInfluence;
            float saddleRaw = Mathf.Sin(Vector2.Dot(fromHome, slopeAxis) * broadScale * 0.55f + settings.HeightSeed * 0.007f)
                * Mathf.Cos(Vector2.Dot(fromHome, crossSlopeAxis) * broadScale * 0.43f - settings.HeightSeed * 0.011f);
            float saddleStrength = Mathf.Abs(saddleRaw) * macroInfluence;
            float saddle = saddleRaw
                * 0.16f
                * macroInfluence;
            float lowCorridorCore = 1f - Mathf.Abs(Mathf.Sin(Vector2.Dot(fromHome, valleyAxis) * broadScale * 0.74f + settings.HeightSeed * 0.019f));
            float lowCorridorStrength = Smooth01(lowCorridorCore) * macroInfluence;
            float lowCorridor = -lowCorridorStrength * 0.16f;
            Vector2 mesaCenter = home
                + (slopeAxis * flatWidth * 5.8f)
                + (crossSlopeAxis * flatWidth * GetSeedSigned(settings.HeightSeed, 89.3f) * 3.4f);
            Vector2 fromMesa = planar - mesaCenter;
            float mesaLong = Vector2.Dot(fromMesa, mesaAxis) / (flatWidth * 3.45f);
            float mesaShort = Vector2.Dot(fromMesa, mesaCrossAxis) / (flatWidth * 2.15f);
            float mesaDistance = Mathf.Sqrt((mesaLong * mesaLong) + (mesaShort * mesaShort));
            float mesaTopStrength = 1f - Smooth01((mesaDistance - 0.60f) / 0.35f);
            float mesaRimStrength = Smooth01((mesaDistance - 0.58f) / 0.17f) * SmoothFalloff((mesaDistance - 1.02f) / 0.18f);
            float mesa = (mesaTopStrength * 0.18f) + (mesaRimStrength * 0.035f);
            float detail = ((Mathf.PerlinNoise(
                settings.HeightSeed * 0.011f + planar.x * detailScale,
                settings.HeightSeed * 0.017f + planar.y * detailScale) - 0.5f) * 2f)
                * Mathf.Lerp(0.018f, 0.055f, homeCalm);

            float normalizedHeight = directionalSlope
                + homeBasin
                + outerRise
                + ridge
                + ridgeShoulder
                + saddle
                + lowCorridor
                + mesa
                + detail;

            return new HeightProfile(
                Mathf.Clamp(normalizedHeight, -1f, 1f),
                settings.HeightAmplitudeMeters,
                homeBasinStrength,
                outerRiseStrength,
                Mathf.Abs(directionalSlopeRaw),
                ridgeCrestStrength,
                saddleStrength,
                lowCorridorStrength,
                mesaTopStrength,
                distanceFromHome,
                flatWidth);
        }

        private static Vector2 ToPlanar(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private static Vector2 GetSeedAxis(int seed, float offsetDegrees)
        {
            float radians = Mathf.Deg2Rad * Mathf.Repeat(seed * 37.719f + offsetDegrees, 360f);
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static float GetSeedSigned(int seed, float offset)
        {
            return Mathf.Sin((seed * 0.01391f + offset) * 12.9898f);
        }

        private static float SmoothFalloff(float normalizedDistance)
        {
            return 1f - Smooth01(normalizedDistance);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private readonly struct HeightProfile
        {
            public HeightProfile(
                float normalizedHeight,
                float heightAmplitudeMeters,
                float homeBasinStrength,
                float outerRiseStrength,
                float directionalSlopeStrength,
                float ridgeCrestStrength,
                float saddleStrength,
                float lowCorridorStrength,
                float mesaTopStrength,
                float distanceFromHomeMeters,
                float flatWidthMeters)
            {
                NormalizedHeight = normalizedHeight;
                LogicalHeightMeters = normalizedHeight * heightAmplitudeMeters;
                HomeBasinStrength = homeBasinStrength;
                OuterRiseStrength = outerRiseStrength;
                DirectionalSlopeStrength = directionalSlopeStrength;
                RidgeCrestStrength = ridgeCrestStrength;
                SaddleStrength = saddleStrength;
                LowCorridorStrength = lowCorridorStrength;
                MesaTopStrength = mesaTopStrength;
                DistanceFromHomeMeters = distanceFromHomeMeters;
                FlatWidthMeters = flatWidthMeters;
            }

            public float NormalizedHeight { get; }
            public float LogicalHeightMeters { get; }
            public float HomeBasinStrength { get; }
            public float OuterRiseStrength { get; }
            public float DirectionalSlopeStrength { get; }
            public float RidgeCrestStrength { get; }
            public float SaddleStrength { get; }
            public float LowCorridorStrength { get; }
            public float MesaTopStrength { get; }
            public float DistanceFromHomeMeters { get; }
            public float FlatWidthMeters { get; }

            public TerrainLandform ResolveLandform()
            {
                if (HomeBasinStrength > 0.48f && DistanceFromHomeMeters < FlatWidthMeters * 2.6f)
                {
                    return TerrainLandform.HomeBasin;
                }

                if (LowCorridorStrength > 0.70f && NormalizedHeight < 0.18f)
                {
                    return TerrainLandform.LowCorridor;
                }

                if (MesaTopStrength > 0.58f && NormalizedHeight > 0.05f)
                {
                    return TerrainLandform.Mesa;
                }

                if (RidgeCrestStrength > 0.64f && NormalizedHeight > 0.18f)
                {
                    return TerrainLandform.RidgeLine;
                }

                if (SaddleStrength > 0.60f && Mathf.Abs(NormalizedHeight) < 0.26f)
                {
                    return TerrainLandform.Saddle;
                }

                if (NormalizedHeight < -0.30f)
                {
                    return TerrainLandform.Valley;
                }

                if (OuterRiseStrength > 0.65f && NormalizedHeight > 0.20f)
                {
                    return TerrainLandform.HighGround;
                }

                return DirectionalSlopeStrength > 0.25f ? TerrainLandform.LongSlope : TerrainLandform.RollingGround;
            }
        }
    }
}
