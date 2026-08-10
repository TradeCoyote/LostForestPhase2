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
        private float hexOuterRadiusMeters = 45f;
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
        public float HexOuterRadiusMeters => Mathf.Max(1f, hexOuterRadiusMeters);

        public void SetFieldData(FieldData newFieldData)
        {
            SetFieldData(newFieldData, hexOuterRadiusMeters);
        }

        public void SetFieldData(FieldData newFieldData, float newHexOuterRadiusMeters)
        {
            fieldData = newFieldData;
            hexOuterRadiusMeters = Mathf.Max(1f, newHexOuterRadiusMeters);
            currentSlot = null;
            previousSlot = null;
            hasResolvedInitialSlot = false;
        }

        public void RefreshCurrentSlot(bool forceLog = false)
        {
            bool resolved = TryResolveSlot(transform.position, out FieldSlotData resolvedSlot);

            bool isInitialResolve = !hasResolvedInitialSlot;
            bool slotChanged = resolvedSlot != currentSlot;

            if (!resolved && !slotChanged && !forceLog && hasResolvedInitialSlot)
            {
                return;
            }

            if (!slotChanged && !forceLog && !isInitialResolve)
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
            return FieldBoundaryMath.TryResolvePlayableSlot(fieldData, HexOuterRadiusMeters, worldPosition, out slot);
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

        private static string BuildSlotTransitionLog(FieldSlotData oldSlot, FieldSlotData newSlot)
        {
            string previousAddress = oldSlot == null ? "None" : oldSlot.Address;
            if (newSlot == null)
            {
                return $"Lost Forest Player Grid Slot: Previous={previousAddress}, Current=None, OutsidePlayableField=True";
            }

            return $"Lost Forest Player Grid Slot: Previous={previousAddress}, Current={newSlot.Address}, Row={newSlot.RowIndex}, Column={newSlot.ColumnIndex}, Axial=({newSlot.AxialQ}, {newSlot.AxialR}), Tile={newSlot.TileIdLabel}, Orientation=O{newSlot.OrientationIndex}/{newSlot.OrientationDegrees:0}deg, Role={newSlot.Role}";
        }
    }
}
