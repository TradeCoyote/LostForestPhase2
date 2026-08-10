using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class RenderedFrostTileInstance
    {
        private readonly TerrainSurfaceSampler surfaceSampler;

        public RenderedFrostTileInstance(
            Vector2Int axialCoordinate,
            int ringDepth,
            GameObject root,
            TerrainFrameData terrainFrameData,
            TerrainSlotData terrainSlot,
            HexTerrainMeshData terrainMeshData,
            TerrainSurfaceSampler surfaceSampler,
            int distanceFromCenter)
        {
            AxialCoordinate = axialCoordinate;
            RingDepth = Mathf.Max(1, ringDepth);
            Root = root;
            TerrainFrameData = terrainFrameData;
            TerrainSlot = terrainSlot;
            TerrainMeshData = terrainMeshData;
            this.surfaceSampler = surfaceSampler;
            SetDistanceBand(distanceFromCenter);
        }

        public Vector2Int AxialCoordinate { get; }
        public int RingDepth { get; }
        public GameObject Root { get; }
        public TerrainFrameData TerrainFrameData { get; }
        public TerrainSlotData TerrainSlot { get; }
        public HexTerrainMeshData TerrainMeshData { get; }
        public int DistanceFromCenter { get; private set; }

        public bool TrySampleSurface(Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            if (surfaceSampler == null)
            {
                sample = default;
                return false;
            }

            return surfaceSampler.TrySample(worldXzPosition, out sample);
        }

        public void SetDistanceBand(int distanceFromCenter)
        {
            DistanceFromCenter = Mathf.Max(0, distanceFromCenter);
        }

        public void Destroy()
        {
            if (Root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(Root);
            }
            else
            {
                Object.DestroyImmediate(Root);
            }
        }
    }
}
