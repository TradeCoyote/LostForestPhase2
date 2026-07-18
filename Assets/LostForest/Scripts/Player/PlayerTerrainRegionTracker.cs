using System.Collections;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Player
{
    public sealed class PlayerTerrainRegionTracker : MonoBehaviour
    {
        [SerializeField] private SevenHexTerrainFrameDebugView terrainFrame;
        [SerializeField] private HomeRegionDefinition homeRegion;
        [SerializeField] private bool discoverTerrainFrameOnStart = true;
        [SerializeField] private bool rebuildMissingFrameData = true;
        [SerializeField] private bool logRegionChanges = true;
        [SerializeField] private bool logInitialRegion = true;

        private TerrainFrameData frameData;
        private TerrainSlotData currentSlot;
        private TerrainSlotData previousSlot;
        private bool hasResolvedInitialRegion;

        public bool HasCurrentSlot => currentSlot != null;
        public TerrainSlotData CurrentSlot => currentSlot;
        public TerrainSlotData PreviousSlot => previousSlot;
        public string CurrentSlotLabel => currentSlot == null ? string.Empty : currentSlot.Label;
        public Vector2Int CurrentSlotAxialCoordinate => currentSlot == null ? Vector2Int.zero : currentSlot.AxialCoordinate;
        public Vector3 CurrentSlotCenterWorldPosition => currentSlot == null ? Vector3.zero : GetSlotCenter(currentSlot);
        public bool IsInHomeRegion => homeRegion != null && currentSlot != null && homeRegion.IsHomeSlot(currentSlot);
        public bool HasFrameData => frameData != null && frameData.SlotCount > 0;
        public TerrainFrameData CurrentFrameData => frameData;
        public HomeRegionDefinition HomeRegion => homeRegion;

        public void SetTerrainFrame(SevenHexTerrainFrameDebugView newTerrainFrame)
        {
            terrainFrame = newTerrainFrame;
            frameData = null;
            currentSlot = null;
            previousSlot = null;
            hasResolvedInitialRegion = false;
        }

        public void SetHomeRegion(HomeRegionDefinition newHomeRegion)
        {
            homeRegion = newHomeRegion;
        }

        public void RefreshCurrentRegion(bool forceLog = false)
        {
            if (!TryRefreshFrameData())
            {
                return;
            }

            TerrainSlotData nearestSlot = FindNearestSlotByHorizontalDistance(transform.position, frameData);

            if (nearestSlot == null)
            {
                return;
            }

            bool isInitialResolve = !hasResolvedInitialRegion;
            bool slotChanged = nearestSlot != currentSlot;

            if (!slotChanged && !forceLog)
            {
                return;
            }

            previousSlot = currentSlot;
            currentSlot = nearestSlot;
            hasResolvedInitialRegion = true;

            if (logRegionChanges && ShouldLogRegionChange(forceLog, isInitialResolve, slotChanged))
            {
                Debug.Log(BuildRegionLogLine(previousSlot, currentSlot));
            }
        }

        public bool TryFindNearestSlot(Vector3 worldPosition, out TerrainSlotData slot)
        {
            slot = null;

            if (!TryRefreshFrameData())
            {
                return false;
            }

            slot = FindNearestSlotByHorizontalDistance(worldPosition, frameData);
            return slot != null;
        }

        private IEnumerator Start()
        {
            if (discoverTerrainFrameOnStart)
            {
                DiscoverSceneReferences();
            }

            yield return null;
            RefreshCurrentRegion(logInitialRegion && !hasResolvedInitialRegion);
        }

        private void Update()
        {
            RefreshCurrentRegion(false);
        }

        private void DiscoverSceneReferences()
        {
            if (terrainFrame == null)
            {
                terrainFrame = FindAnyObjectByType<SevenHexTerrainFrameDebugView>();
            }

            if (homeRegion == null && terrainFrame != null)
            {
                homeRegion = terrainFrame.GetComponent<HomeRegionDefinition>();
            }

            if (homeRegion == null)
            {
                homeRegion = FindAnyObjectByType<HomeRegionDefinition>();
            }
        }

        private bool TryRefreshFrameData()
        {
            if (terrainFrame == null)
            {
                DiscoverSceneReferences();
            }

            if (terrainFrame == null)
            {
                return false;
            }

            frameData = terrainFrame.TerrainFrameData;

            if ((frameData == null || frameData.SlotCount == 0) && rebuildMissingFrameData)
            {
                terrainFrame.Rebuild();
                frameData = terrainFrame.TerrainFrameData;
            }

            return frameData != null && frameData.SlotCount > 0;
        }

        private bool ShouldLogRegionChange(bool forceLog, bool isInitialResolve, bool slotChanged)
        {
            if (forceLog)
            {
                return true;
            }

            return isInitialResolve ? logInitialRegion : slotChanged;
        }

        private string BuildRegionLogLine(TerrainSlotData oldSlot, TerrainSlotData newSlot)
        {
            string previousLabel = oldSlot == null ? "None" : oldSlot.Label;
            Vector2Int axial = newSlot.AxialCoordinate;
            Vector3 center = GetSlotCenter(newSlot);
            return $"Lost Forest Player Region: Previous={previousLabel}, Current={newSlot.Label}, Axial=({axial.x}, {axial.y}), Center=({center.x:0.00}, {center.y:0.00}, {center.z:0.00}), IsHome={IsInHomeRegion}";
        }

        private static TerrainSlotData FindNearestSlotByHorizontalDistance(Vector3 worldPosition, TerrainFrameData data)
        {
            if (data == null || data.SlotCount == 0)
            {
                return null;
            }

            TerrainSlotData nearestSlot = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < data.Slots.Count; i++)
            {
                TerrainSlotData slot = data.Slots[i];

                if (slot == null)
                {
                    continue;
                }

                Vector3 center = GetSlotCenter(slot);
                float deltaX = worldPosition.x - center.x;
                float deltaZ = worldPosition.z - center.z;
                float distanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);

                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestSlot = slot;
                }
            }

            return nearestSlot;
        }

        private static Vector3 GetSlotCenter(TerrainSlotData slot)
        {
            return slot.CenterPoint == null ? slot.WorldCenter : slot.CenterPoint.Position;
        }
    }
}
