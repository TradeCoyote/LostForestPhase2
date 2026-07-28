using UnityEngine;

namespace LostForest.Phase2.Landmarks
{
    public sealed class LandmarkInstance : MonoBehaviour
    {
        [SerializeField] private LandmarkType landmarkType;
        [SerializeField] private string displayName;
        [SerializeField] private string fieldSlotAddress;
        [SerializeField] private string tileIdLabel;
        [SerializeField] private float footprintRadiusMeters;
        [SerializeField] private int placementSeed;

        public LandmarkType LandmarkType => landmarkType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? landmarkType.ToString() : displayName;
        public string FieldSlotAddress => fieldSlotAddress;
        public string TileIdLabel => tileIdLabel;
        public float FootprintRadiusMeters => footprintRadiusMeters;
        public int PlacementSeed => placementSeed;
        public Vector3 DebugWorldPosition => transform.position;

        public void Initialize(LandmarkPlacementPlan plan)
        {
            landmarkType = plan.Type;
            displayName = plan.Profile == null ? plan.Type.ToString() : plan.Profile.DisplayName;
            fieldSlotAddress = plan.SlotAddress;
            tileIdLabel = plan.TileIdLabel;
            footprintRadiusMeters = plan.Profile == null ? 0f : plan.Profile.FootprintRadiusMeters;
            placementSeed = plan.PlacementSeed;
        }

        public string BuildDebugSummary(float distanceMeters)
        {
            return $"{DisplayName} {fieldSlotAddress} {distanceMeters:0.0}m";
        }
    }
}
