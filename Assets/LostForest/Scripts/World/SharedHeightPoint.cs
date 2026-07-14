using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [Serializable]
    public sealed class SharedHeightPoint
    {
        [SerializeField] private string pointId;
        [SerializeField] private TerrainPointKind kind;
        [SerializeField] private Vector3 position;
        [SerializeField] private float height;
        [SerializeField] private List<string> localReferences = new List<string>();

        public SharedHeightPoint(string pointId, TerrainPointKind kind, Vector3 position, float height)
        {
            this.pointId = pointId;
            this.kind = kind;
            this.position = position;
            this.height = height;
        }

        public string PointId => pointId;
        public TerrainPointKind Kind => kind;
        public Vector3 Position => position;
        public float Height => height;
        public IReadOnlyList<string> LocalReferences => localReferences;
        public int ReferenceCount => localReferences.Count;

        public void AddLocalReference(string localReference)
        {
            if (string.IsNullOrWhiteSpace(localReference) || localReferences.Contains(localReference))
            {
                return;
            }

            localReferences.Add(localReference);
        }
    }
}
