using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class HomeRegionDefinition : MonoBehaviour
    {
        [SerializeField] private SevenHexTerrainFrameDebugView terrainFrame;
        [SerializeField] private Vector2Int homeAxialCoordinate = Vector2Int.zero;
        [SerializeField] private string homeLabelFallback = "Center";

        public Vector2Int HomeAxialCoordinate => homeAxialCoordinate;
        public string HomeLabelFallback => homeLabelFallback;

        public void SetTerrainFrame(SevenHexTerrainFrameDebugView newTerrainFrame)
        {
            terrainFrame = newTerrainFrame;
        }

        public void SetHomeAxialCoordinate(Vector2Int axialCoordinate)
        {
            homeAxialCoordinate = axialCoordinate;
        }

        public bool IsHomeSlot(TerrainSlotData slot)
        {
            if (slot == null)
            {
                return false;
            }

            if (slot.AxialCoordinate == homeAxialCoordinate)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(homeLabelFallback) && slot.Label == homeLabelFallback;
        }

        public bool TryGetHomeSlot(out TerrainSlotData homeSlot)
        {
            return TryGetHomeSlot(GetFrameData(), out homeSlot);
        }

        public bool TryGetHomeSlot(TerrainFrameData frameData, out TerrainSlotData homeSlot)
        {
            if (frameData == null || frameData.SlotCount == 0)
            {
                homeSlot = null;
                return false;
            }

            homeSlot = FindHomeSlotByAxial(frameData);

            if (homeSlot != null)
            {
                return true;
            }

            homeSlot = FindHomeSlotByLabel(frameData);
            return homeSlot != null;
        }

        private void Reset()
        {
            homeAxialCoordinate = Vector2Int.zero;
            homeLabelFallback = "Center";
            terrainFrame = GetComponent<SevenHexTerrainFrameDebugView>();
        }

        private void Awake()
        {
            if (terrainFrame == null)
            {
                terrainFrame = GetComponent<SevenHexTerrainFrameDebugView>();
            }
        }

        private TerrainSlotData FindHomeSlotByAxial(TerrainFrameData frameData)
        {
            for (int i = 0; i < frameData.Slots.Count; i++)
            {
                TerrainSlotData slot = frameData.Slots[i];

                if (slot != null && slot.AxialCoordinate == homeAxialCoordinate)
                {
                    return slot;
                }
            }

            return null;
        }

        private TerrainSlotData FindHomeSlotByLabel(TerrainFrameData frameData)
        {
            if (string.IsNullOrWhiteSpace(homeLabelFallback))
            {
                return null;
            }

            for (int i = 0; i < frameData.Slots.Count; i++)
            {
                TerrainSlotData slot = frameData.Slots[i];

                if (slot != null && slot.Label == homeLabelFallback)
                {
                    return slot;
                }
            }

            return null;
        }

        private TerrainFrameData GetFrameData()
        {
            if (terrainFrame == null)
            {
                terrainFrame = GetComponent<SevenHexTerrainFrameDebugView>();
            }

            if (terrainFrame == null)
            {
                terrainFrame = FindAnyObjectByType<SevenHexTerrainFrameDebugView>();
            }

            return terrainFrame == null ? null : terrainFrame.TerrainFrameData;
        }
    }
}
