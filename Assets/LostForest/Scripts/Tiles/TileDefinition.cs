using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    [Serializable]
    public sealed class TileDefinition
    {
        [SerializeField] private int tileId;
        [SerializeField] private string debugName;
        [SerializeField] private TileReservedRole reservedRole;
        [SerializeField] private TileContentCategory contentCategory;
        [SerializeField] private bool runeEligible;
        [SerializeField] private List<string> terrainTags = new List<string>();
        [SerializeField] private List<string> contentTags = new List<string>();
        [SerializeField] private bool contentSupportsRotation = true;
        [SerializeField] private ForestFillProfile forestFill;
        [SerializeField] private TileConstructionAnchors anchors;

        public TileDefinition(
            int tileId,
            string debugName,
            TileReservedRole reservedRole,
            TileContentCategory contentCategory,
            bool runeEligible,
            IEnumerable<string> terrainTags,
            IEnumerable<string> contentTags,
            bool contentSupportsRotation,
            ForestFillProfile forestFill,
            TileConstructionAnchors anchors)
        {
            this.tileId = Mathf.Max(0, tileId);
            this.debugName = string.IsNullOrWhiteSpace(debugName) ? $"Tile {this.tileId:D3}" : debugName;
            this.reservedRole = reservedRole;
            this.contentCategory = contentCategory;
            this.runeEligible = runeEligible;
            this.terrainTags = terrainTags == null ? new List<string>() : new List<string>(terrainTags);
            this.contentTags = contentTags == null ? new List<string>() : new List<string>(contentTags);
            this.contentSupportsRotation = contentSupportsRotation;
            this.forestFill = forestFill;
            this.anchors = anchors;
        }

        public int TileId => tileId;
        public string TileIdLabel => tileId.ToString("D3");
        public string DebugName => debugName;
        public TileReservedRole ReservedRole => reservedRole;
        public TileContentCategory ContentCategory => contentCategory;
        public bool RuneEligible => runeEligible;
        public IReadOnlyList<string> TerrainTags => terrainTags;
        public IReadOnlyList<string> ContentTags => contentTags;
        public bool ContentSupportsRotation => contentSupportsRotation;
        public ForestFillProfile ForestFill => forestFill;
        public TileConstructionAnchors Anchors => anchors;
    }
}
