using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Landmarks
{
    public readonly struct LandmarkPlacementPlan
    {
        public LandmarkPlacementPlan(
            LandmarkPlacementProfile profile,
            string slotAddress,
            string tileIdLabel,
            Vector3 anchorPosition,
            Quaternion rotation,
            TerrainSurfaceSample surfaceSample,
            int placementSeed)
        {
            Profile = profile;
            SlotAddress = string.IsNullOrWhiteSpace(slotAddress) ? "Unknown" : slotAddress;
            TileIdLabel = string.IsNullOrWhiteSpace(tileIdLabel) ? "---" : tileIdLabel;
            AnchorPosition = anchorPosition;
            Rotation = rotation;
            SurfaceSample = surfaceSample;
            PlacementSeed = placementSeed;
        }

        public LandmarkPlacementProfile Profile { get; }
        public LandmarkType Type => Profile == null ? LandmarkType.RockLightGray : Profile.Type;
        public string SlotAddress { get; }
        public string TileIdLabel { get; }
        public Vector3 AnchorPosition { get; }
        public Quaternion Rotation { get; }
        public TerrainSurfaceSample SurfaceSample { get; }
        public int PlacementSeed { get; }
    }

    public static class LandmarkPlacementManager
    {
        private const int FootprintSampleCount = 6;

        public static bool TryCreatePlacementPlan(
            FieldSlotData fieldSlot,
            TerrainSlotData terrainSlot,
            TerrainSurfaceSampler surfaceSampler,
            int worldSeed,
            float hexOuterRadiusMeters,
            out LandmarkPlacementPlan plan,
            out string skipReason)
        {
            plan = default;
            skipReason = string.Empty;

            if (fieldSlot == null || terrainSlot == null)
            {
                skipReason = "missing Slot data";
                return false;
            }

            if (!fieldSlot.IsLandmarkTile)
            {
                skipReason = "Slot is not a landmark tile";
                return false;
            }

            if (surfaceSampler == null)
            {
                skipReason = "missing terrain surface sampler";
                return false;
            }

            int placementSeed = BuildPlacementSeed(worldSeed, fieldSlot);
            int startIndex = PositiveModulo(placementSeed, LandmarkPlacementProfile.TypeCount);
            string lastSkipReason = "no candidate evaluated";

            for (int typeAttempt = 0; typeAttempt < LandmarkPlacementProfile.TypeCount; typeAttempt++)
            {
                LandmarkType type = LandmarkPlacementProfile.GetTypeByIndex(startIndex + typeAttempt);
                LandmarkPlacementProfile profile = LandmarkPlacementProfile.GetProfile(type);

                if (TryFindPlacementForProfile(
                        profile,
                        fieldSlot,
                        terrainSlot,
                        surfaceSampler,
                        placementSeed,
                        hexOuterRadiusMeters,
                        out plan,
                        out lastSkipReason))
                {
                    skipReason = string.Empty;
                    return true;
                }
            }

            skipReason = lastSkipReason;
            return false;
        }

        private static bool TryFindPlacementForProfile(
            LandmarkPlacementProfile profile,
            FieldSlotData fieldSlot,
            TerrainSlotData terrainSlot,
            TerrainSurfaceSampler surfaceSampler,
            int placementSeed,
            float hexOuterRadiusMeters,
            out LandmarkPlacementPlan plan,
            out string skipReason)
        {
            plan = default;
            skipReason = string.Empty;

            Vector3 center = terrainSlot.CenterPoint == null ? terrainSlot.WorldCenter : terrainSlot.CenterPoint.Position;
            float yawDegrees = BuildYawDegrees(placementSeed, profile.Type);

            for (int candidateIndex = 0; candidateIndex < 7; candidateIndex++)
            {
                Vector3 offset = GetCandidateOffset(candidateIndex, fieldSlot.OrientationDegrees, profile.CandidateOffsetRadiusMeters);

                if (!IsInsideFlatTopHex(offset, profile.FootprintRadiusMeters, hexOuterRadiusMeters))
                {
                    skipReason = $"{profile.DisplayName} candidate outside Slot footprint";
                    continue;
                }

                Vector3 candidate = center + offset;

                if (!TryEvaluateCandidate(profile, candidate, yawDegrees, surfaceSampler, out TerrainSurfaceSample anchorSample, out skipReason))
                {
                    continue;
                }

                plan = new LandmarkPlacementPlan(
                    profile,
                    fieldSlot.Address,
                    fieldSlot.TileIdLabel,
                    anchorSample.Position - Vector3.up * profile.RootEmbedMeters,
                    Quaternion.Euler(0f, yawDegrees, 0f),
                    anchorSample,
                    placementSeed);

                return true;
            }

            return false;
        }

        private static bool TryEvaluateCandidate(
            LandmarkPlacementProfile profile,
            Vector3 candidate,
            float yawDegrees,
            TerrainSurfaceSampler surfaceSampler,
            out TerrainSurfaceSample anchorSample,
            out string skipReason)
        {
            anchorSample = default;
            skipReason = string.Empty;

            if (!surfaceSampler.TrySample(candidate, out anchorSample))
            {
                skipReason = $"{profile.DisplayName} failed center terrain sample";
                return false;
            }

            if (anchorSample.SlopeDegrees > profile.MaxSlopeDegrees)
            {
                skipReason = $"{profile.DisplayName} center slope {anchorSample.SlopeDegrees:0.0}deg exceeds {profile.MaxSlopeDegrees:0.0}deg";
                return false;
            }

            if (profile.FootprintSampleRadiusMeters <= 0.1f)
            {
                return true;
            }

            float minY = anchorSample.Position.y;
            float maxY = anchorSample.Position.y;
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            for (int i = 0; i < FootprintSampleCount; i++)
            {
                float angle = Mathf.Deg2Rad * ((360f / FootprintSampleCount) * i);
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * profile.FootprintSampleRadiusMeters;
                Vector3 samplePosition = candidate + (rotation * localOffset);

                if (!surfaceSampler.TrySample(samplePosition, out TerrainSurfaceSample footprintSample))
                {
                    skipReason = $"{profile.DisplayName} failed footprint terrain sample";
                    return false;
                }

                if (footprintSample.SlopeDegrees > profile.MaxSlopeDegrees + 4f)
                {
                    skipReason = $"{profile.DisplayName} footprint slope {footprintSample.SlopeDegrees:0.0}deg exceeds {profile.MaxSlopeDegrees + 4f:0.0}deg";
                    return false;
                }

                minY = Mathf.Min(minY, footprintSample.Position.y);
                maxY = Mathf.Max(maxY, footprintSample.Position.y);
            }

            if (maxY - minY > profile.MaxFootprintHeightDeltaMeters)
            {
                skipReason = $"{profile.DisplayName} footprint height delta {maxY - minY:0.00}m exceeds {profile.MaxFootprintHeightDeltaMeters:0.00}m";
                return false;
            }

            return true;
        }

        private static Vector3 GetCandidateOffset(int candidateIndex, float orientationDegrees, float radiusMeters)
        {
            if (candidateIndex <= 0 || radiusMeters <= 0f)
            {
                return Vector3.zero;
            }

            float angleDegrees = orientationDegrees + ((candidateIndex - 1) * 60f);
            float angleRadians = Mathf.Deg2Rad * angleDegrees;
            return new Vector3(Mathf.Cos(angleRadians) * radiusMeters, 0f, Mathf.Sin(angleRadians) * radiusMeters);
        }

        private static bool IsInsideFlatTopHex(Vector3 localOffset, float footprintRadiusMeters, float hexOuterRadiusMeters)
        {
            float usableRadius = Mathf.Max(1f, hexOuterRadiusMeters - Mathf.Max(4f, footprintRadiusMeters * 0.65f));
            float qx = Mathf.Abs(localOffset.x);
            float qz = Mathf.Abs(localOffset.z);
            float innerRadius = usableRadius * 0.8660254f;

            if (qx + footprintRadiusMeters > usableRadius || qz + footprintRadiusMeters > innerRadius)
            {
                return false;
            }

            return innerRadius * usableRadius - innerRadius * qx - usableRadius * 0.5f * qz >= 0f;
        }

        private static float BuildYawDegrees(int placementSeed, LandmarkType type)
        {
            unchecked
            {
                int hash = placementSeed;
                hash = hash * 397 + (int)type;
                return PositiveModulo(hash, 360);
            }
        }

        private static int BuildPlacementSeed(int worldSeed, FieldSlotData fieldSlot)
        {
            unchecked
            {
                int hash = 29;
                hash = hash * 397 + worldSeed;
                hash = hash * 397 + fieldSlot.TileId;
                hash = hash * 397 + fieldSlot.OrientationIndex;
                hash = hash * 397 + GetStableStringHash(fieldSlot.Address);
                return hash;
            }
        }

        private static int GetStableStringHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;

                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
            {
                return 0;
            }

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
