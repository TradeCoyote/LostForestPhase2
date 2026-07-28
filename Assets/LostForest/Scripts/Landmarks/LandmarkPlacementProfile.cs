using UnityEngine;

namespace LostForest.Phase2.Landmarks
{
    public sealed class LandmarkPlacementProfile
    {
        public const int TypeCount = 20;

        public LandmarkPlacementProfile(
            LandmarkType type,
            string displayName,
            float footprintRadiusMeters,
            float footprintSampleRadiusMeters,
            float candidateOffsetRadiusMeters,
            float maxSlopeDegrees,
            float maxFootprintHeightDeltaMeters,
            float rootEmbedMeters)
        {
            Type = type;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? type.ToString() : displayName;
            FootprintRadiusMeters = Mathf.Max(0.5f, footprintRadiusMeters);
            FootprintSampleRadiusMeters = Mathf.Clamp(footprintSampleRadiusMeters, 0f, FootprintRadiusMeters);
            CandidateOffsetRadiusMeters = Mathf.Max(0f, candidateOffsetRadiusMeters);
            MaxSlopeDegrees = Mathf.Clamp(maxSlopeDegrees, 1f, 55f);
            MaxFootprintHeightDeltaMeters = Mathf.Max(0.05f, maxFootprintHeightDeltaMeters);
            RootEmbedMeters = Mathf.Clamp(rootEmbedMeters, 0f, 0.5f);
        }

        public LandmarkType Type { get; }
        public string DisplayName { get; }
        public float FootprintRadiusMeters { get; }
        public float FootprintSampleRadiusMeters { get; }
        public float CandidateOffsetRadiusMeters { get; }
        public float MaxSlopeDegrees { get; }
        public float MaxFootprintHeightDeltaMeters { get; }
        public float RootEmbedMeters { get; }

        public static LandmarkPlacementProfile GetProfile(LandmarkType type)
        {
            switch (type)
            {
                case LandmarkType.Well:
                    return new LandmarkPlacementProfile(type, "Well", 4.1f, 3.2f, 5.5f, 14f, 0.75f, 0.12f);
                case LandmarkType.CairnSphere:
                    return CreateCairnProfile(type, "Cairn 1 Sphere");
                case LandmarkType.CairnCube:
                    return CreateCairnProfile(type, "Cairn 2 Cube");
                case LandmarkType.CairnPyramid:
                    return CreateCairnProfile(type, "Cairn 3 Pyramid");
                case LandmarkType.CairnCylinder:
                    return CreateCairnProfile(type, "Cairn 4 Cylinder");
                case LandmarkType.BirchTreeCircle:
                    return new LandmarkPlacementProfile(type, "Birch Tree Circle", 5.8f, 5.1f, 4.5f, 16f, 1.15f, 0.03f);
                case LandmarkType.LowAltar:
                    return new LandmarkPlacementProfile(type, "Low Altar", 4.0f, 2.8f, 5.5f, 14f, 0.7f, 0.04f);
                case LandmarkType.RockWhite:
                    return CreateRockProfile(type, "Rock White");
                case LandmarkType.RockVeryLightGray:
                    return CreateRockProfile(type, "Rock Very Light Gray");
                case LandmarkType.RockLightGray:
                    return CreateRockProfile(type, "Rock Light Gray");
                case LandmarkType.RockMediumGray:
                    return CreateRockProfile(type, "Rock Medium Gray");
                case LandmarkType.TwoFallenParallelBirches:
                    return new LandmarkPlacementProfile(type, "Two Fallen Parallel Birches", 5.7f, 4.8f, 4.5f, 24f, 1.5f, 0.02f);
                case LandmarkType.BirchStumpCircle:
                    return new LandmarkPlacementProfile(type, "Birch Stump Circle", 4.9f, 4.1f, 4.5f, 18f, 1.1f, 0.03f);
                case LandmarkType.StoneHut:
                    return new LandmarkPlacementProfile(type, "Stone Hut", 4.8f, 3.8f, 5.5f, 13f, 0.8f, 0.04f);
                case LandmarkType.OneTotem:
                    return new LandmarkPlacementProfile(type, "One Totem", 2.1f, 1.3f, 5.5f, 26f, 1.2f, 0.04f);
                case LandmarkType.TwoTotems:
                    return new LandmarkPlacementProfile(type, "Two Totems", 3.2f, 2.2f, 5.5f, 25f, 1.25f, 0.04f);
                case LandmarkType.ThreeTotems:
                    return new LandmarkPlacementProfile(type, "Three Totems", 4.0f, 3.0f, 5.5f, 24f, 1.35f, 0.04f);
                case LandmarkType.CrossedTrees:
                    return new LandmarkPlacementProfile(type, "Crossed Trees", 5.7f, 4.8f, 4.5f, 24f, 1.5f, 0.02f);
                case LandmarkType.SmallRingOfStones:
                    return new LandmarkPlacementProfile(type, "Small Ring of Stones", 4.0f, 3.3f, 4.5f, 16f, 0.95f, 0.03f);
                case LandmarkType.LargeRingOfStoneSpires:
                    return new LandmarkPlacementProfile(type, "Large Ring of Stone Spires", 7.6f, 6.8f, 3.5f, 12f, 1f, 0.03f);
                default:
                    return CreateRockProfile(LandmarkType.RockLightGray, "Rock Light Gray");
            }
        }

        public static LandmarkType GetTypeByIndex(int index)
        {
            int safeIndex = Mathf.Abs(index) % TypeCount;
            return (LandmarkType)safeIndex;
        }

        private static LandmarkPlacementProfile CreateCairnProfile(LandmarkType type, string displayName)
        {
            return new LandmarkPlacementProfile(type, displayName, 2.6f, 1.6f, 6f, 28f, 1.4f, 0.03f);
        }

        private static LandmarkPlacementProfile CreateRockProfile(LandmarkType type, string displayName)
        {
            return new LandmarkPlacementProfile(type, displayName, 1.8f, 0.9f, 6f, 35f, 1.8f, 0.06f);
        }
    }
}
