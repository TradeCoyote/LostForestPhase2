using System;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class FrameSettings
    {
        public const int CanonicalRows = 26;
        public const int CanonicalColumns = 26;
        public const int CanonicalTileBankSize = 1000;
        public const int PlayerHomeTileId = 0;
        public const int PursuerTileId = 666;

        [SerializeField] private int rows = CanonicalRows;
        [SerializeField] private int columns = CanonicalColumns;
        [SerializeField] private int tileBankSize = CanonicalTileBankSize;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private float hexOuterRadiusMeters = 45f;

        public int Rows => Mathf.Max(1, rows);
        public int Columns => Mathf.Max(1, columns);
        public int TileBankSize => Mathf.Max(Rows * Columns, tileBankSize);
        public int Seed => seed;
        public bool UseRandomSeed => useRandomSeed;
        public float HexOuterRadiusMeters => Mathf.Max(1f, hexOuterRadiusMeters);

        public static FrameSettings CreateSmallPrototype(int rows, int columns, int seed = 12345)
        {
            return new FrameSettings
            {
                rows = Mathf.Max(1, rows),
                columns = Mathf.Max(1, columns),
                tileBankSize = CanonicalTileBankSize,
                seed = seed,
                useRandomSeed = false
            };
        }
    }
}
