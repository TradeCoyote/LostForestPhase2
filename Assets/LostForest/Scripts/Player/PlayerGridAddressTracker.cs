using System;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Player
{
    public sealed class PlayerGridAddressTracker : MonoBehaviour
    {
        [SerializeField] private bool logSlotTransitions = true;
        [SerializeField] private bool logInitialSlot = true;

        private FieldData fieldData;
        private FieldSlotData currentSlot;
        private FieldSlotData previousSlot;
        private bool hasResolvedInitialSlot;

        public event Action<FieldSlotData, FieldSlotData> CurrentSlotChanged;

        public bool HasFieldData => fieldData != null && fieldData.SlotsFilled > 0;
        public bool HasCurrentSlot => currentSlot != null;
        public FieldData CurrentFieldData => fieldData;
        public FieldSlotData CurrentSlot => currentSlot;
        public FieldSlotData PreviousSlot => previousSlot;
        public string CurrentGridAddress => currentSlot == null ? string.Empty : currentSlot.Address;
        public Vector2Int CurrentAxialCoordinate => currentSlot == null ? Vector2Int.zero : currentSlot.AxialCoordinate;

        public void SetFieldData(FieldData newFieldData)
        {
            fieldData = newFieldData;
            currentSlot = null;
            previousSlot = null;
            hasResolvedInitialSlot = false;
        }

        public void RefreshCurrentSlot(bool forceLog = false)
        {
            if (!TryResolveSlot(transform.position, out FieldSlotData resolvedSlot))
            {
                return;
            }

            bool isInitialResolve = !hasResolvedInitialSlot;
            bool slotChanged = resolvedSlot != currentSlot;

            if (!slotChanged && !forceLog)
            {
                return;
            }

            previousSlot = currentSlot;
            currentSlot = resolvedSlot;
            hasResolvedInitialSlot = true;

            if (logSlotTransitions && ShouldLogSlotTransition(forceLog, isInitialResolve, slotChanged))
            {
                Debug.Log(BuildSlotTransitionLog(previousSlot, currentSlot));
            }

            if (slotChanged || forceLog)
            {
                CurrentSlotChanged?.Invoke(previousSlot, currentSlot);
            }
        }

        public bool TryResolveSlot(Vector3 worldPosition, out FieldSlotData slot)
        {
            slot = null;

            if (fieldData == null || fieldData.SlotsFilled == 0)
            {
                return false;
            }

            slot = FindNearestFieldSlotByHorizontalDistance(worldPosition, fieldData);
            return slot != null;
        }

        private void Update()
        {
            RefreshCurrentSlot(false);
        }

        private bool ShouldLogSlotTransition(bool forceLog, bool isInitialResolve, bool slotChanged)
        {
            if (forceLog)
            {
                return true;
            }

            return isInitialResolve ? logInitialSlot : slotChanged;
        }

        private static FieldSlotData FindNearestFieldSlotByHorizontalDistance(Vector3 worldPosition, FieldData data)
        {
            FieldSlotData nearestSlot = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < data.Slots.Count; i++)
            {
                FieldSlotData slot = data.Slots[i];

                if (slot == null)
                {
                    continue;
                }

                Vector3 center = slot.WorldCenter;
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

        private static string BuildSlotTransitionLog(FieldSlotData oldSlot, FieldSlotData newSlot)
        {
            string previousAddress = oldSlot == null ? "None" : oldSlot.Address;
            return $"Lost Forest Player Grid Slot: Previous={previousAddress}, Current={newSlot.Address}, Row={newSlot.RowIndex}, Column={newSlot.ColumnIndex}, Axial=({newSlot.AxialQ}, {newSlot.AxialR}), Tile={newSlot.TileIdLabel}, Orientation=O{newSlot.OrientationIndex}/{newSlot.OrientationDegrees:0}deg, Role={newSlot.Role}";
        }
    }
}
