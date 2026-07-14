using System;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    [Serializable]
    public sealed class TileAnchor
    {
        [SerializeField] private string id;
        [SerializeField] private TileAnchorKind kind;
        [SerializeField] private int hexIndex;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private bool rotatesWithTileContent = true;

        public TileAnchor(string id, TileAnchorKind kind, int hexIndex, Vector3 localPosition, bool rotatesWithTileContent)
        {
            this.id = id;
            this.kind = kind;
            this.hexIndex = hexIndex;
            this.localPosition = localPosition;
            this.rotatesWithTileContent = rotatesWithTileContent;
        }

        public string Id => id;
        public TileAnchorKind Kind => kind;
        public int HexIndex => hexIndex;
        public Vector3 LocalPosition => localPosition;
        public bool RotatesWithTileContent => rotatesWithTileContent;
    }
}
