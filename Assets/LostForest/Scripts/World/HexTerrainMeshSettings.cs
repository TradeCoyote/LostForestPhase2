using System;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class HexTerrainMeshSettings
    {
        [SerializeField] private string groupObjectName = "Hex Terrain Mesh Group";
        [SerializeField] private string surfaceObjectNamePrefix = "Slot";
        [SerializeField] private string surfaceObjectNameSuffix = "White Terrain Surface";
        [SerializeField] private string meshNameSuffix = "Terrain Surface Mesh";
        [SerializeField] private float surfaceLift = -0.08f;
        [SerializeField] private bool addMeshCollider = true;
        [SerializeField] private bool markStatic;
        [SerializeField] private bool logGenerationReport;
        [SerializeField] private bool createSlotRootsForGroups = true;

        public HexTerrainMeshSettings()
        {
        }

        public HexTerrainMeshSettings(
            float surfaceLift,
            bool addMeshCollider,
            bool markStatic,
            bool logGenerationReport)
        {
            this.surfaceLift = surfaceLift;
            this.addMeshCollider = addMeshCollider;
            this.markStatic = markStatic;
            this.logGenerationReport = logGenerationReport;
        }

        public HexTerrainMeshSettings(
            float surfaceLift,
            bool addMeshCollider,
            bool markStatic,
            bool logGenerationReport,
            string groupObjectName,
            string surfaceObjectNamePrefix,
            string surfaceObjectNameSuffix,
            string meshNameSuffix,
            bool createSlotRootsForGroups)
            : this(surfaceLift, addMeshCollider, markStatic, logGenerationReport)
        {
            this.groupObjectName = groupObjectName;
            this.surfaceObjectNamePrefix = surfaceObjectNamePrefix;
            this.surfaceObjectNameSuffix = surfaceObjectNameSuffix;
            this.meshNameSuffix = meshNameSuffix;
            this.createSlotRootsForGroups = createSlotRootsForGroups;
        }

        public string GroupObjectName => string.IsNullOrWhiteSpace(groupObjectName) ? "Hex Terrain Mesh Group" : groupObjectName;
        public string SurfaceObjectNamePrefix => string.IsNullOrWhiteSpace(surfaceObjectNamePrefix) ? "Slot" : surfaceObjectNamePrefix;
        public string SurfaceObjectNameSuffix => string.IsNullOrWhiteSpace(surfaceObjectNameSuffix) ? "White Terrain Surface" : surfaceObjectNameSuffix;
        public string MeshNameSuffix => string.IsNullOrWhiteSpace(meshNameSuffix) ? "Terrain Surface Mesh" : meshNameSuffix;
        public float SurfaceLift => surfaceLift;
        public bool AddMeshCollider => addMeshCollider;
        public bool MarkStatic => markStatic;
        public bool LogGenerationReport => logGenerationReport;
        public bool CreateSlotRootsForGroups => createSlotRootsForGroups;

        public string GetSurfaceObjectName(string slotLabel)
        {
            return $"{SurfaceObjectNamePrefix} {GetSafeSlotLabel(slotLabel)} {SurfaceObjectNameSuffix}";
        }

        public string GetMeshName(string slotLabel)
        {
            return $"{SurfaceObjectNamePrefix} {GetSafeSlotLabel(slotLabel)} {MeshNameSuffix}";
        }

        private static string GetSafeSlotLabel(string slotLabel)
        {
            return string.IsNullOrWhiteSpace(slotLabel) ? "Unlabeled" : slotLabel;
        }
    }
}
