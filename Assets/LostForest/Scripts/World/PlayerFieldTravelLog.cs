using System;
using System.Collections.Generic;
using LostForest.Phase2.Player;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class PlayerFieldTravelLog : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private PlayerGridAddressTracker gridAddressTracker;

        [Header("Logging")]
        [SerializeField] private bool logSlotEntries = true;
        [SerializeField] private bool logOnlyWhenPlaying = true;

        private readonly List<PlayerFieldTravelStep> routeSteps = new List<PlayerFieldTravelStep>();
        private readonly Dictionary<string, PlayerFieldSlotVisit> visitsByAddress = new Dictionary<string, PlayerFieldSlotVisit>();

        private FieldData fieldData;
        private FieldSlotData homeSlot;
        private FieldSlotData pursuerOriginSlot;
        private bool trackerSubscribed;

        public IReadOnlyList<PlayerFieldTravelStep> RouteSteps => routeSteps;
        public IReadOnlyDictionary<string, PlayerFieldSlotVisit> VisitsByAddress => visitsByAddress;
        public int StepCount => routeSteps.Count;
        public int UniqueVisitedSlotCount => visitsByAddress.Count;
        public PlayerFieldTravelStep LastStep => routeSteps.Count == 0 ? null : routeSteps[routeSteps.Count - 1];

        public void SetTracker(PlayerGridAddressTracker newGridAddressTracker)
        {
            UnsubscribeTracker();
            gridAddressTracker = newGridAddressTracker;
            SubscribeTracker();
        }

        public void SetWorldContext(FieldData newFieldData, FieldSlotData newHomeSlot, FieldSlotData newPursuerOriginSlot)
        {
            fieldData = newFieldData;
            homeSlot = newHomeSlot;
            pursuerOriginSlot = newPursuerOriginSlot;
        }

        [ContextMenu("Clear Player Field Travel Log")]
        public void ClearHistory()
        {
            routeSteps.Clear();
            visitsByAddress.Clear();
        }

        [ContextMenu("Log Player Field Travel Summary")]
        public void LogSummary()
        {
            Debug.Log(BuildSummaryLog(), this);
        }

        private void Awake()
        {
            DiscoverTracker();
        }

        private void OnEnable()
        {
            SubscribeTracker();
        }

        private void Start()
        {
            DiscoverTracker();
            SubscribeTracker();
        }

        private void OnDisable()
        {
            UnsubscribeTracker();
        }

        private void DiscoverTracker()
        {
            if (gridAddressTracker == null)
            {
                gridAddressTracker = FindAnyObjectByType<PlayerGridAddressTracker>();
            }
        }

        private void SubscribeTracker()
        {
            if (trackerSubscribed || gridAddressTracker == null)
            {
                return;
            }

            gridAddressTracker.CurrentSlotChanged += HandleCurrentSlotChanged;
            trackerSubscribed = true;
        }

        private void UnsubscribeTracker()
        {
            if (!trackerSubscribed || gridAddressTracker == null)
            {
                trackerSubscribed = false;
                return;
            }

            gridAddressTracker.CurrentSlotChanged -= HandleCurrentSlotChanged;
            trackerSubscribed = false;
        }

        private void HandleCurrentSlotChanged(FieldSlotData previousSlot, FieldSlotData currentSlot)
        {
            if (currentSlot == null)
            {
                return;
            }

            PlayerFieldTravelStep step = RecordStep(previousSlot, currentSlot);

            if (logSlotEntries && (!logOnlyWhenPlaying || Application.isPlaying))
            {
                Debug.Log(step.BuildDebugLog(), this);
            }
        }

        private PlayerFieldTravelStep RecordStep(FieldSlotData previousSlot, FieldSlotData currentSlot)
        {
            if (!visitsByAddress.TryGetValue(currentSlot.Address, out PlayerFieldSlotVisit visit))
            {
                visit = new PlayerFieldSlotVisit(currentSlot);
                visitsByAddress[currentSlot.Address] = visit;
            }

            visit.RecordVisit(Time.time);

            PlayerFieldTravelStep step = new PlayerFieldTravelStep(
                routeSteps.Count + 1,
                Time.time,
                previousSlot,
                currentSlot,
                visit.VisitCount,
                UniqueVisitedSlotCount,
                GetDistance(previousSlot, currentSlot),
                GetDistance(homeSlot, currentSlot),
                GetDistance(pursuerOriginSlot, currentSlot));

            routeSteps.Add(step);
            return step;
        }

        private string BuildSummaryLog()
        {
            PlayerFieldTravelStep lastStep = LastStep;
            string currentAddress = lastStep == null ? "None" : lastStep.CurrentAddress;
            string fieldLabel = fieldData == null ? "None" : $"{fieldData.Rows}x{fieldData.Columns}";
            return $"Lost Forest Player Field Travel Summary: Field={fieldLabel}, Steps={StepCount}, UniqueVisitedSlots={UniqueVisitedSlotCount}, Current={currentAddress}";
        }

        private static int GetDistance(FieldSlotData fromSlot, FieldSlotData toSlot)
        {
            if (fromSlot == null || toSlot == null)
            {
                return -1;
            }

            return HexFrameMath.GetHexDistance(fromSlot.AxialCoordinate, toSlot.AxialCoordinate);
        }
    }

    [Serializable]
    public sealed class PlayerFieldSlotVisit
    {
        public PlayerFieldSlotVisit(FieldSlotData slot)
        {
            Slot = slot;
        }

        public FieldSlotData Slot { get; }
        public string Address => Slot == null ? string.Empty : Slot.Address;
        public int VisitCount { get; private set; }
        public float FirstVisitTime { get; private set; } = -1f;
        public float LastVisitTime { get; private set; } = -1f;

        public void RecordVisit(float time)
        {
            VisitCount++;

            if (FirstVisitTime < 0f)
            {
                FirstVisitTime = time;
            }

            LastVisitTime = time;
        }
    }

    [Serializable]
    public sealed class PlayerFieldTravelStep
    {
        public PlayerFieldTravelStep(
            int stepIndex,
            float timeSeconds,
            FieldSlotData previousSlot,
            FieldSlotData currentSlot,
            int currentSlotVisitCount,
            int uniqueVisitedSlotCount,
            int distanceFromPrevious,
            int distanceFromHome,
            int distanceFromPursuerOrigin)
        {
            StepIndex = stepIndex;
            TimeSeconds = timeSeconds;
            PreviousSlot = previousSlot;
            CurrentSlot = currentSlot;
            CurrentSlotVisitCount = currentSlotVisitCount;
            UniqueVisitedSlotCount = uniqueVisitedSlotCount;
            DistanceFromPrevious = distanceFromPrevious;
            DistanceFromHome = distanceFromHome;
            DistanceFromPursuerOrigin = distanceFromPursuerOrigin;
        }

        public int StepIndex { get; }
        public float TimeSeconds { get; }
        public FieldSlotData PreviousSlot { get; }
        public FieldSlotData CurrentSlot { get; }
        public int CurrentSlotVisitCount { get; }
        public int UniqueVisitedSlotCount { get; }
        public int DistanceFromPrevious { get; }
        public int DistanceFromHome { get; }
        public int DistanceFromPursuerOrigin { get; }
        public string PreviousAddress => PreviousSlot == null ? "None" : PreviousSlot.Address;
        public string CurrentAddress => CurrentSlot == null ? "None" : CurrentSlot.Address;

        public string BuildDebugLog()
        {
            if (CurrentSlot == null)
            {
                return $"Lost Forest Player Field Travel: Step={StepIndex}, Current=None";
            }

            return $"Lost Forest Player Field Travel: Step={StepIndex}, Time={TimeSeconds:0.0}s, Previous={PreviousAddress}, Current={CurrentAddress}, Row={CurrentSlot.RowIndex}, Column={CurrentSlot.ColumnIndex}, Axial=({CurrentSlot.AxialQ},{CurrentSlot.AxialR}), Tile={CurrentSlot.TileIdLabel}, Orientation=O{CurrentSlot.OrientationIndex}/{CurrentSlot.OrientationDegrees:0}deg, Role={CurrentSlot.Role}, VisitCount={CurrentSlotVisitCount}, UniqueVisited={UniqueVisitedSlotCount}, DistanceFromPrevious={DistanceFromPrevious}, DistanceFromHome={DistanceFromHome}, DistanceFromPursuerOrigin={DistanceFromPursuerOrigin}";
        }
    }
}
