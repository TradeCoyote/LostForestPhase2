using System;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    [Serializable]
    public sealed class ForestFillProfile
    {
        [SerializeField] private int seedSalt;
        [SerializeField] private int currentTileTreeCount;
        [SerializeField] private float minTreeSpacingMeters;
        [SerializeField] private float centerClearingRadiusMeters;
        [SerializeField] private float edgeInsetMeters;
        [SerializeField] private Vector2 trunkHeightRangeMeters;
        [SerializeField] private Vector2 trunkRadiusRangeMeters;

        public ForestFillProfile(
            int seedSalt,
            int currentTileTreeCount,
            float minTreeSpacingMeters,
            float centerClearingRadiusMeters,
            float edgeInsetMeters,
            Vector2 trunkHeightRangeMeters,
            Vector2 trunkRadiusRangeMeters)
        {
            this.seedSalt = seedSalt;
            this.currentTileTreeCount = Mathf.Max(0, currentTileTreeCount);
            this.minTreeSpacingMeters = Mathf.Max(0f, minTreeSpacingMeters);
            this.centerClearingRadiusMeters = Mathf.Max(0f, centerClearingRadiusMeters);
            this.edgeInsetMeters = Mathf.Max(0f, edgeInsetMeters);
            this.trunkHeightRangeMeters = SortRange(trunkHeightRangeMeters, new Vector2(7f, 13f));
            this.trunkRadiusRangeMeters = SortRange(trunkRadiusRangeMeters, new Vector2(0.55f, 1.05f));
        }

        public int SeedSalt => seedSalt;
        public int CurrentTileTreeCount => currentTileTreeCount;
        public float MinTreeSpacingMeters => minTreeSpacingMeters;
        public float CenterClearingRadiusMeters => centerClearingRadiusMeters;
        public float EdgeInsetMeters => edgeInsetMeters;
        public Vector2 TrunkHeightRangeMeters => trunkHeightRangeMeters;
        public Vector2 TrunkRadiusRangeMeters => trunkRadiusRangeMeters;

        public static ForestFillProfile CreateHomePrototype()
        {
            return new ForestFillProfile(101, 4, 9.5f, 24f, 7f, new Vector2(7f, 12f), new Vector2(0.45f, 0.85f));
        }

        public static ForestFillProfile CreateSparsePrototype(int seedSalt)
        {
            return new ForestFillProfile(seedSalt, 5, 9f, 10f, 6f, new Vector2(8f, 14f), new Vector2(0.45f, 0.95f));
        }

        public static ForestFillProfile CreateNormalPrototype(int seedSalt)
        {
            return new ForestFillProfile(seedSalt, 6, 8.5f, 8f, 5f, new Vector2(9f, 16f), new Vector2(0.5f, 1.05f));
        }

        public static ForestFillProfile CreateDensePrototype(int seedSalt)
        {
            return new ForestFillProfile(seedSalt, 8, 8f, 6f, 4f, new Vector2(10f, 18f), new Vector2(0.55f, 1.15f));
        }

        private static Vector2 SortRange(Vector2 range, Vector2 fallback)
        {
            if (range.x <= 0f && range.y <= 0f)
            {
                range = fallback;
            }

            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return new Vector2(Mathf.Max(0.01f, min), Mathf.Max(0.01f, max));
        }
    }
}
