using System.Collections;
using LostForest.Phase2.Landmarks;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Player
{
    public enum EarlyWalkThruSpawnSlot
    {
        Center = 0,
        East = 1,
        Northeast = 2,
        Northwest = 3,
        West = 4,
        Southwest = 5,
        Southeast = 6,
        Random = 7
    }

    public sealed class EarlyWalkThruCenterSpawn : MonoBehaviour
    {
        private const string HomeLandmarkObjectName = "Home Standing Stone Landmark";

        [SerializeField] private SevenHexTerrainFrameDebugView terrainFrame;
        [SerializeField] private EarlyWalkThruSpawnSlot spawnSlot = EarlyWalkThruSpawnSlot.Center;
        [SerializeField] private float footClearanceMeters = 0.18f;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool logSpawnPosition = true;
        [SerializeField] private bool ensureHomeSystemsOnSpawn = true;

        private CharacterController characterController;
        private EarlyWalkThruFirstPersonController firstPersonController;

        public void SetTerrainFrame(SevenHexTerrainFrameDebugView newTerrainFrame)
        {
            terrainFrame = newTerrainFrame;
        }

        public void SetSpawnSlot(EarlyWalkThruSpawnSlot newSpawnSlot)
        {
            spawnSlot = newSpawnSlot;
        }

        private void Awake()
        {
            RefreshComponentHandles();
        }

        private IEnumerator Start()
        {
            if (!spawnOnStart)
            {
                yield break;
            }

            yield return null;
            SpawnAtTerrainSlotCenter();
        }

        [ContextMenu("Spawn At Terrain Slot Center")]
        public void SpawnAtTerrainSlotCenter()
        {
            RefreshComponentHandles();

            if (terrainFrame == null)
            {
                terrainFrame = FindFirstObjectByType<SevenHexTerrainFrameDebugView>();
            }

            if (terrainFrame == null)
            {
                Debug.LogWarning("Lost Forest Early WalkThru spawn skipped: no SevenHexTerrainFrameDebugView was found.");
                return;
            }

            TerrainFrameData frameData = terrainFrame.TerrainFrameData;

            if (frameData == null || frameData.SlotCount == 0)
            {
                terrainFrame.Rebuild();
                frameData = terrainFrame.TerrainFrameData;
            }

            if (frameData == null || frameData.SlotCount == 0)
            {
                Debug.LogWarning("Lost Forest Early WalkThru spawn skipped: terrain frame data was not available after rebuild.");
                return;
            }

            if (ensureHomeSystemsOnSpawn)
            {
                EnsureHomeSystems();
            }

            TerrainSlotData slot = ResolveSpawnSlot(frameData);

            if (slot == null || slot.CenterPoint == null)
            {
                Debug.LogWarning("Lost Forest Early WalkThru spawn skipped: selected terrain slot had no center point.");
                return;
            }

            Vector3 centerPoint = slot.CenterPoint.Position;
            float controllerYOffset = GetControllerFootToTransformOffset();
            Vector3 spawnPosition = new Vector3(centerPoint.x, centerPoint.y + footClearanceMeters + controllerYOffset, centerPoint.z);
            bool controllerWasEnabled = characterController == null || characterController.enabled;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = spawnPosition;

            if (characterController != null)
            {
                characterController.enabled = controllerWasEnabled;
            }

            if (firstPersonController != null)
            {
                firstPersonController.ResetVerticalVelocity();
            }

            if (logSpawnPosition)
            {
                Debug.Log($"Lost Forest Early WalkThru player spawned at Slot={slot.Label}, XYZ=({spawnPosition.x:0.00}, {spawnPosition.y:0.00}, {spawnPosition.z:0.00})");
            }
        }

        private void RefreshComponentHandles()
        {
            characterController = GetComponent<CharacterController>();
            firstPersonController = GetComponent<EarlyWalkThruFirstPersonController>();
        }

        private void EnsureHomeSystems()
        {
            if (terrainFrame == null)
            {
                return;
            }

            HomeRegionDefinition homeRegion = terrainFrame.GetComponent<HomeRegionDefinition>();

            if (homeRegion == null)
            {
                homeRegion = terrainFrame.gameObject.AddComponent<HomeRegionDefinition>();
            }

            homeRegion.SetTerrainFrame(terrainFrame);
            homeRegion.SetHomeAxialCoordinate(Vector2Int.zero);

            HomeLandmarkBuilder homeLandmark = FindAnyObjectByType<HomeLandmarkBuilder>();
            GameObject landmarkObject;

            if (homeLandmark == null)
            {
                landmarkObject = GameObject.Find(HomeLandmarkObjectName);

                if (landmarkObject == null)
                {
                    landmarkObject = new GameObject(HomeLandmarkObjectName);
                }

                homeLandmark = landmarkObject.AddComponent<HomeLandmarkBuilder>();
            }
            else
            {
                landmarkObject = homeLandmark.gameObject;
            }

            landmarkObject.name = HomeLandmarkObjectName;
            homeLandmark.SetTerrainFrame(terrainFrame);
            homeLandmark.SetHomeRegion(homeRegion);
            homeLandmark.TryRebuildLandmark();

            PlayerTerrainRegionTracker regionTracker = GetComponent<PlayerTerrainRegionTracker>();

            if (regionTracker == null)
            {
                regionTracker = gameObject.AddComponent<PlayerTerrainRegionTracker>();
            }

            regionTracker.SetTerrainFrame(terrainFrame);
            regionTracker.SetHomeRegion(homeRegion);
            regionTracker.RefreshCurrentRegion(true);
        }

        private TerrainSlotData ResolveSpawnSlot(TerrainFrameData frameData)
        {
            int slotIndex = spawnSlot == EarlyWalkThruSpawnSlot.Random
                ? Random.Range(0, frameData.SlotCount)
                : Mathf.Clamp((int)spawnSlot, 0, frameData.SlotCount - 1);

            return frameData.Slots[slotIndex];
        }

        private float GetControllerFootToTransformOffset()
        {
            if (characterController == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, (characterController.height * 0.5f) - characterController.center.y);
        }
    }
}
