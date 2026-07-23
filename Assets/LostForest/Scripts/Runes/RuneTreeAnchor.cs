using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public struct RuneTreeAnchor
    {
        public RuneTreeAnchor(Transform treeRoot, float trunkRadiusMeters, float trunkHeightMeters, int treeIndex)
        {
            TreeRoot = treeRoot;
            TrunkRadiusMeters = Mathf.Max(0.01f, trunkRadiusMeters);
            TrunkHeightMeters = Mathf.Max(0.01f, trunkHeightMeters);
            TreeIndex = Mathf.Max(0, treeIndex);
        }

        public Transform TreeRoot { get; }
        public float TrunkRadiusMeters { get; }
        public float TrunkHeightMeters { get; }
        public int TreeIndex { get; }
    }
}
