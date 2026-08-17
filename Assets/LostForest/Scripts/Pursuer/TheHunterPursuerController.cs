using System;
using System.Collections.Generic;
using LostForest.Phase2.Core;
using LostForest.Phase2.Feedback;
using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Pursuer
{
    /// <summary>
    /// The first Phase 2 pursuer. The Hunter advances through the authoritative
    /// hidden Field, but communicates through pressure and fog rather than a
    /// visible board piece. A future creature can replace this policy while
    /// retaining the same run sources, cues, and catch contract.
    /// </summary>
    public sealed class TheHunterPursuerController : MonoBehaviour
    {
        private const int FirstRuneRetreatDistance = 10;
        private const int SecondRuneRetreatDistance = 5;

        [Header("Identity")]
        [SerializeField] private PursuerArchetype archetype = PursuerArchetype.TheHunter;

        [Header("Run Sources")]
        [SerializeField] private PlayerGridAddressTracker playerGridAddressTracker;
        [SerializeField] private PlayerFieldTravelLog playerFieldTravelLog;
        [SerializeField] private EarlyWalkThruFirstPersonController firstPersonController;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private RunVictoryController runVictoryController;
        [SerializeField] private PrototypeFogDirector fogDirector;

        [Header("State Timing")]
        [SerializeField] private float dormantDurationSeconds = 18f;
        [SerializeField] private float passiveInterestPerSecond = 0.5f;
        [SerializeField] private float distanceFromHomeInterestPerSecond = 0.16f;
        [SerializeField] private int distanceFromHomeInterestThreshold = 2;
        [SerializeField] private float carriedRuneInterestPerSecond = 1.1f;
        [SerializeField] private float runePickupInterest = 28f;
        [SerializeField] private float runeDepositInterest = 14f;
        [SerializeField] private float searchAlertThreshold = 12f;
        [SerializeField] private float stalkAlertThreshold = 42f;
        [SerializeField] private int closePressureDistanceSlots = 5;

        [Header("Hidden Field Movement")]
        [SerializeField] private float noRunesReturnedMoveIntervalSeconds = 26f;
        [SerializeField] private float oneRuneReturnedMoveIntervalSeconds = 20f;
        [SerializeField] private float twoRunesReturnedMoveIntervalSeconds = 15f;
        [SerializeField, Range(1, 4)] private int searchTrailRefreshEverySlotEntries = 2;
        [SerializeField, Range(0f, 1f)] private float sprintLostTrailChance = 0.6f;
        [SerializeField] private float retreatGraceSeconds = 7f;

        [Header("Indirect Feedback")]
        [SerializeField, Range(0f, 1f)] private float stalkFogPressure = 0.3f;
        [SerializeField, Range(0f, 1f)] private float closeFogPressure = 0.72f;

        [Header("Development Debug")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logHunterMoves = true;
        [SerializeField] private bool enableDevelopmentHotkeys = true;
        [SerializeField] private KeyCode cycleForceStateKey = KeyCode.F7;
        [SerializeField] private KeyCode forceCatchKey = KeyCode.F8;

        private FieldData fieldData;
        private FieldSlotData homeSlot;
        private FieldSlotData originSlot;
        private FieldSlotData hunterSlot;
        private FieldSlotData targetSlot;
        private System.Random random;
        private PursuerState state = PursuerState.Disabled;
        private PursuerState forcedState;
        private bool hasForcedState;
        private bool configured;
        private bool sourcesSubscribed;
        // A held sprint remains one burst even if stamina temporarily drops the
        // player out of actual sprinting. This prevents stamina recovery from
        // creating repeated Hunter trail-break rolls while Space is held.
        private bool sprintBurstHandled;
        private bool skipNextMoveOpportunity;
        private int playerSlotEntryCount;
        private int observedDepositCount;
        private float activeSeconds;
        private float alert;
        private float secondsUntilMove;
        private float retreatGraceRemaining;
        private int moveCount;

        public event Action<PursuerState, PursuerState> StateChanged;
        public event Action<string> CueRaised;
        public event Action<FieldSlotData> HiddenSlotChanged;
        /// <summary>
        /// Raised only when the Hunter naturally advances into its next hidden
        /// Slot. Setup, forced state changes, and rune retreat placement do not
        /// raise this event, so directional warning cues remain meaningful.
        /// </summary>
        public event Action<FieldSlotData, FieldSlotData> AdvancedToHiddenSlot;
        public event Action CaughtPlayer;

        public PursuerArchetype Archetype => archetype;
        public PursuerState State => state;
        public bool IsConfigured => configured;
        public FieldSlotData CurrentHiddenSlot => hunterSlot;
        public FieldSlotData CurrentTargetSlot => targetSlot;
        public float Alert => alert;
        public float RetreatGraceRemaining => retreatGraceRemaining;
        public bool IsSkippingNextMoveOpportunity => skipNextMoveOpportunity;
        public int ExactPlayerDistanceSlots => GetDistance(hunterSlot, CurrentPlayerSlot);
        public int TargetDistanceSlots => GetDistance(hunterSlot, targetSlot);
        public float CurrentMoveIntervalSeconds => GetMoveIntervalForDepositedRunes(runeManager == null ? 0 : runeManager.DepositedRuneCount);
        public int MoveCount => moveCount;

        private FieldSlotData CurrentPlayerSlot => playerGridAddressTracker == null ? null : playerGridAddressTracker.CurrentSlot;

        public void SetSources(
            PlayerGridAddressTracker newPlayerGridAddressTracker,
            PlayerFieldTravelLog newPlayerFieldTravelLog,
            EarlyWalkThruFirstPersonController newFirstPersonController,
            PlayerCondition newPlayerCondition,
            RuneManager newRuneManager,
            RunVictoryController newRunVictoryController,
            PrototypeFogDirector newFogDirector)
        {
            UnsubscribeSources();
            playerGridAddressTracker = newPlayerGridAddressTracker;
            playerFieldTravelLog = newPlayerFieldTravelLog;
            firstPersonController = newFirstPersonController;
            playerCondition = newPlayerCondition;
            runeManager = newRuneManager;
            runVictoryController = newRunVictoryController;
            fogDirector = newFogDirector;
            SubscribeSources();
        }

        public void ConfigureRun(FieldData newFieldData, FieldSlotData newHomeSlot, FieldSlotData newOriginSlot)
        {
            fieldData = newFieldData;
            homeSlot = newHomeSlot;
            originSlot = newOriginSlot;
            configured = fieldData != null && homeSlot != null && originSlot != null;
            random = configured ? new System.Random(BuildRunSeed(fieldData.Seed)) : null;
            hunterSlot = configured ? originSlot : null;
            targetSlot = CurrentPlayerSlot ?? homeSlot;
            state = configured ? PursuerState.Dormant : PursuerState.Disabled;
            hasForcedState = false;
            sprintBurstHandled = false;
            skipNextMoveOpportunity = false;
            playerSlotEntryCount = 0;
            observedDepositCount = runeManager == null ? 0 : runeManager.DepositedRuneCount;
            activeSeconds = 0f;
            alert = 0f;
            secondsUntilMove = RollMoveInterval(state);
            retreatGraceRemaining = 0f;
            moveCount = 0;
            UpdateFogPressure();
            HiddenSlotChanged?.Invoke(hunterSlot);

            if (logStateChanges && configured)
            {
                Debug.Log($"Lost Forest TheHunter initialized: Origin={originSlot.Address}, Target={(targetSlot == null ? "None" : targetSlot.Address)}, Seed={fieldData.Seed}, State={state}", this);
            }
        }

        public void ApplyTheHunterPrototypeDefaults()
        {
            archetype = PursuerArchetype.TheHunter;
            dormantDurationSeconds = 18f;
            passiveInterestPerSecond = 0.5f;
            distanceFromHomeInterestPerSecond = 0.16f;
            distanceFromHomeInterestThreshold = 2;
            carriedRuneInterestPerSecond = 1.1f;
            runePickupInterest = 28f;
            runeDepositInterest = 14f;
            searchAlertThreshold = 12f;
            stalkAlertThreshold = 42f;
            closePressureDistanceSlots = 5;
            noRunesReturnedMoveIntervalSeconds = 26f;
            oneRuneReturnedMoveIntervalSeconds = 20f;
            twoRunesReturnedMoveIntervalSeconds = 15f;
            searchTrailRefreshEverySlotEntries = 2;
            sprintLostTrailChance = 0.6f;
            retreatGraceSeconds = 7f;
            stalkFogPressure = 0.3f;
            closeFogPressure = 0.72f;
            logHunterMoves = true;
            cycleForceStateKey = KeyCode.F7;
            forceCatchKey = KeyCode.F8;
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (archetype != PursuerArchetype.TheHunter)
            {
                failureReason = "TheHunter controller must use the TheHunter archetype.";
                return false;
            }

            if (dormantDurationSeconds < 0f || passiveInterestPerSecond < 0f || searchAlertThreshold <= 0f || stalkAlertThreshold < searchAlertThreshold)
            {
                failureReason = "TheHunter state thresholds are invalid.";
                return false;
            }

            if (closePressureDistanceSlots < 1 ||
                noRunesReturnedMoveIntervalSeconds <= 0f ||
                oneRuneReturnedMoveIntervalSeconds <= 0f ||
                twoRunesReturnedMoveIntervalSeconds <= 0f ||
                noRunesReturnedMoveIntervalSeconds < oneRuneReturnedMoveIntervalSeconds ||
                oneRuneReturnedMoveIntervalSeconds < twoRunesReturnedMoveIntervalSeconds)
            {
                failureReason = "TheHunter rune-progress movement intervals or close distance are invalid.";
                return false;
            }

            if (configured && (fieldData == null || homeSlot == null || originSlot == null || hunterSlot == null))
            {
                failureReason = "TheHunter run is marked configured without a valid hidden-field origin.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public string BuildDebugSummary()
        {
            string current = hunterSlot == null ? "None" : hunterSlot.Address;
            string target = targetSlot == null ? "None" : targetSlot.Address;
            string player = CurrentPlayerSlot == null ? "None" : CurrentPlayerSlot.Address;
            string forced = hasForcedState ? $" Forced={forcedState}" : string.Empty;
            return $"TheHunter {state} Alert {alert:0.0} Hidden={current} Target={target} Player={player}\nDist Player={ExactPlayerDistanceSlots} Target={TargetDistanceSlots} Move={CurrentMoveIntervalSeconds:0}s Count={moveCount} Grace={retreatGraceRemaining:0.0}s Skip={skipNextMoveOpportunity}{forced}";
        }

        public void ForceStateForDebug(PursuerState newState)
        {
            if (!configured || newState == PursuerState.Disabled)
            {
                return;
            }

            if (newState == PursuerState.Catch)
            {
                hasForcedState = false;
                CatchPlayer();
                return;
            }

            hasForcedState = true;
            forcedState = newState;
            SetState(newState, "Debug force state");
        }

        [ContextMenu("TheHunter/Force Interest")]
        private void ForceInterestForDebug()
        {
            ForceStateForDebug(PursuerState.Interest);
        }

        [ContextMenu("TheHunter/Force Search")]
        private void ForceSearchForDebug()
        {
            ForceStateForDebug(PursuerState.Search);
        }

        [ContextMenu("TheHunter/Force Stalk")]
        private void ForceStalkForDebug()
        {
            ForceStateForDebug(PursuerState.Stalk);
        }

        [ContextMenu("TheHunter/Force Close Pressure")]
        private void ForceClosePressureForDebug()
        {
            ForceStateForDebug(PursuerState.ClosePressure);
        }

        [ContextMenu("TheHunter/Force Catch")]
        private void ForceCatchForDebug()
        {
            ForceStateForDebug(PursuerState.Catch);
        }

        [ContextMenu("TheHunter/Resume Natural State")]
        private void ResumeNaturalStateForDebug()
        {
            hasForcedState = false;
            EvaluateState("Debug resume natural state");
        }

        private void Awake()
        {
            DiscoverSources();
        }

        private void OnEnable()
        {
            DiscoverSources();
            SubscribeSources();
        }

        private void OnDisable()
        {
            UnsubscribeSources();
            SetFogPressure(0f);
        }

        private void OnValidate()
        {
            dormantDurationSeconds = Mathf.Max(0f, dormantDurationSeconds);
            passiveInterestPerSecond = Mathf.Max(0f, passiveInterestPerSecond);
            distanceFromHomeInterestPerSecond = Mathf.Max(0f, distanceFromHomeInterestPerSecond);
            distanceFromHomeInterestThreshold = Mathf.Max(0, distanceFromHomeInterestThreshold);
            carriedRuneInterestPerSecond = Mathf.Max(0f, carriedRuneInterestPerSecond);
            runePickupInterest = Mathf.Max(0f, runePickupInterest);
            runeDepositInterest = Mathf.Max(0f, runeDepositInterest);
            searchAlertThreshold = Mathf.Max(0.01f, searchAlertThreshold);
            stalkAlertThreshold = Mathf.Max(searchAlertThreshold, stalkAlertThreshold);
            closePressureDistanceSlots = Mathf.Max(1, closePressureDistanceSlots);
            noRunesReturnedMoveIntervalSeconds = Mathf.Max(0.01f, noRunesReturnedMoveIntervalSeconds);
            oneRuneReturnedMoveIntervalSeconds = Mathf.Max(0.01f, oneRuneReturnedMoveIntervalSeconds);
            twoRunesReturnedMoveIntervalSeconds = Mathf.Max(0.01f, twoRunesReturnedMoveIntervalSeconds);
            searchTrailRefreshEverySlotEntries = Mathf.Max(1, searchTrailRefreshEverySlotEntries);
            sprintLostTrailChance = Mathf.Clamp01(sprintLostTrailChance);
            retreatGraceSeconds = Mathf.Max(0f, retreatGraceSeconds);
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDevelopmentInput();
#endif

            Tick(Time.deltaTime);
        }

        public void TickForValidation(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        private void Tick(float deltaSeconds)
        {
            if (!configured || deltaSeconds <= 0f || state == PursuerState.Catch || state == PursuerState.Disabled)
            {
                return;
            }

            if (IsRunTerminal())
            {
                SetState(PursuerState.Disabled, "Run ended elsewhere");
                return;
            }

            activeSeconds += deltaSeconds;
            UpdateSprintResponse();
            UpdateAlert(deltaSeconds);
            EvaluateState("Pressure update");
            UpdateFogPressure();

            if (hunterSlot != null && hunterSlot == CurrentPlayerSlot)
            {
                CatchPlayer();
                return;
            }

            retreatGraceRemaining = Mathf.Max(0f, retreatGraceRemaining - deltaSeconds);

            if (retreatGraceRemaining > 0f || state == PursuerState.Dormant)
            {
                return;
            }

            secondsUntilMove -= deltaSeconds;

            if (secondsUntilMove > 0f)
            {
                return;
            }

            TakeMoveOpportunity();
        }

        private void UpdateAlert(float deltaSeconds)
        {
            // A validation tick may cross the dormant boundary in one large
            // step. Only the portion after that boundary can build pressure.
            float activePressureDelta = Mathf.Min(
                deltaSeconds,
                Mathf.Max(0f, activeSeconds - dormantDurationSeconds));

            if (activePressureDelta <= 0f)
            {
                return;
            }

            float gainedInterest = passiveInterestPerSecond * activePressureDelta;
            int homeDistance = GetDistance(homeSlot, CurrentPlayerSlot);

            if (homeDistance > distanceFromHomeInterestThreshold)
            {
                gainedInterest += (homeDistance - distanceFromHomeInterestThreshold) * distanceFromHomeInterestPerSecond * activePressureDelta;
            }

            if (runeManager != null && runeManager.HasCarriedRune)
            {
                gainedInterest += carriedRuneInterestPerSecond * activePressureDelta;
            }

            alert = Mathf.Clamp(alert + gainedInterest, 0f, 100f);
        }

        private void UpdateSprintResponse()
        {
            bool wantsSprint = firstPersonController != null && firstPersonController.WantsSprint;
            bool isSprinting = firstPersonController != null && firstPersonController.IsSprinting;

            // Releasing sprint (or stopping movement) ends the burst and arms
            // the next deliberate sprint. Stamina exhaustion alone does not.
            if (!wantsSprint)
            {
                sprintBurstHandled = false;
                return;
            }

            if (!isSprinting || sprintBurstHandled)
            {
                return;
            }

            sprintBurstHandled = true;
            ObserveCurrentPlayerSlot("Sprint burst");
            alert = Mathf.Clamp(alert + 4f, 0f, 100f);

            if ((state == PursuerState.Stalk || state == PursuerState.ClosePressure) && RollChance(sprintLostTrailChance))
            {
                skipNextMoveOpportunity = true;
                RaiseCue("The trail breaks for a moment.");
            }
            else
            {
                RaiseCue("Something has heard the sprint.");
            }
        }

        private void EvaluateState(string reason)
        {
            if (!configured || state == PursuerState.Catch || state == PursuerState.Disabled)
            {
                return;
            }

            if (hasForcedState)
            {
                SetState(forcedState, reason);
                return;
            }

            PursuerState desiredState;

            if (activeSeconds < dormantDurationSeconds && alert < searchAlertThreshold)
            {
                desiredState = PursuerState.Dormant;
            }
            else if (TargetDistanceSlots >= 0 && TargetDistanceSlots <= closePressureDistanceSlots)
            {
                desiredState = PursuerState.ClosePressure;
            }
            else if (alert >= stalkAlertThreshold || (runeManager != null && runeManager.HasCarriedRune))
            {
                desiredState = PursuerState.Stalk;
            }
            else if (alert >= searchAlertThreshold)
            {
                desiredState = PursuerState.Search;
            }
            else
            {
                desiredState = PursuerState.Interest;
            }

            SetState(desiredState, reason);
        }

        private void TakeMoveOpportunity()
        {
            if (skipNextMoveOpportunity)
            {
                skipNextMoveOpportunity = false;
                secondsUntilMove = RollMoveInterval(state);
                LogHunterMovement(null, hunterSlot, "Skipped by sprint trail break");
                RaiseCue("The forest goes still.");
                return;
            }

            if (targetSlot == null)
            {
                ObserveCurrentPlayerSlot("No target fallback");
            }

            FieldSlotData previousHunterSlot = hunterSlot;
            FieldSlotData nextSlot = ChooseNextSlot();

            if (nextSlot != null && nextSlot != hunterSlot)
            {
                hunterSlot = nextSlot;
                moveCount++;
                HiddenSlotChanged?.Invoke(hunterSlot);
                AdvancedToHiddenSlot?.Invoke(previousHunterSlot, hunterSlot);
                LogHunterMovement(previousHunterSlot, hunterSlot, "Advanced");
            }

            if (hunterSlot != null && hunterSlot == CurrentPlayerSlot)
            {
                CatchPlayer();
                return;
            }

            EvaluateState("Hunter moved");
            UpdateFogPressure();
            secondsUntilMove = RollMoveInterval(state);
        }

        private void LogHunterMovement(FieldSlotData fromSlot, FieldSlotData toSlot, string action)
        {
            if (!logHunterMoves || !Application.isPlaying)
            {
                return;
            }

            string from = fromSlot == null ? "None" : fromSlot.Address;
            string to = toSlot == null ? "None" : toSlot.Address;
            string target = targetSlot == null ? "None" : targetSlot.Address;
            string player = CurrentPlayerSlot == null ? "None" : CurrentPlayerSlot.Address;
            int depositedRunes = runeManager == null ? 0 : runeManager.DepositedRuneCount;
            Debug.Log(
                $"Lost Forest TheHunter Move: Count={moveCount}, Time={Time.time:0.0}s, Action={action}, State={state}, RunesReturned={depositedRunes}, Interval={CurrentMoveIntervalSeconds:0.0}s, From={from}, To={to}, Target={target}, Player={player}, DistanceToPlayer={ExactPlayerDistanceSlots}, DistanceToTarget={TargetDistanceSlots}",
                this);
        }

        private FieldSlotData ChooseNextSlot()
        {
            if (fieldData == null || hunterSlot == null || targetSlot == null)
            {
                return hunterSlot;
            }

            List<FieldSlotData> candidates = GetNeighborSlots(hunterSlot);

            if (candidates.Count == 0)
            {
                return hunterSlot;
            }

            candidates.Sort((left, right) =>
            {
                int leftDistance = GetDistance(left, targetSlot);
                int rightDistance = GetDistance(right, targetSlot);
                return leftDistance != rightDistance ? leftDistance.CompareTo(rightDistance) : string.CompareOrdinal(left.Address, right.Address);
            });

            int accuracyCandidateCount = state == PursuerState.ClosePressure
                ? 1
                : state == PursuerState.Stalk
                    ? 2
                    : 3;
            int choiceCount = Mathf.Min(accuracyCandidateCount, candidates.Count);
            return candidates[RollInt(0, choiceCount)];
        }

        private List<FieldSlotData> GetNeighborSlots(FieldSlotData slot)
        {
            List<FieldSlotData> neighbors = new List<FieldSlotData>(6);

            if (fieldData == null || slot == null)
            {
                return neighbors;
            }

            for (int directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                Vector2Int axial = HexFrameMath.GetAxialNeighbor(slot.AxialCoordinate, (HexDirection)directionIndex);
                Vector2Int offset = HexFrameMath.AxialToOffset(axial);
                FieldSlotData neighbor = fieldData.GetSlot(offset.x, offset.y);

                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        private void HandlePlayerSlotChanged(FieldSlotData previousSlot, FieldSlotData currentSlot)
        {
            if (!configured || currentSlot == null || state == PursuerState.Catch || state == PursuerState.Disabled)
            {
                return;
            }

            playerSlotEntryCount++;

            if (state == PursuerState.Stalk || state == PursuerState.ClosePressure)
            {
                ObserveCurrentPlayerSlot("Stalk trail");
            }
            else if (state == PursuerState.Search && ShouldRefreshSearchTrail())
            {
                ObserveCurrentPlayerSlot("Search trail");
            }

            if (hunterSlot == currentSlot)
            {
                CatchPlayer();
            }
        }

        private void HandleRunePickedUp(char runeLetter, string slotAddress)
        {
            if (!configured || state == PursuerState.Catch || state == PursuerState.Disabled)
            {
                return;
            }

            alert = Mathf.Clamp(alert + runePickupInterest, 0f, 100f);
            ObserveCurrentPlayerSlot($"Rune {runeLetter} pickup at {slotAddress}");
            EvaluateState("Rune pickup");
            RaiseCue("A distant branch cracks in the white.");
        }

        private void HandleRuneDeposited(char runeLetter, int depositedCount, int requiredCount)
        {
            if (!configured || state == PursuerState.Catch || state == PursuerState.Disabled)
            {
                return;
            }

            observedDepositCount = Mathf.Max(observedDepositCount, depositedCount);
            alert = Mathf.Clamp(alert + runeDepositInterest, 0f, 100f);

            if (depositedCount < requiredCount)
            {
                RetreatAfterRuneDeposit(depositedCount);
                RaiseCue("For a moment, the forest lets go.");
            }

            EvaluateState($"Rune {runeLetter} deposited");
        }

        private void RetreatAfterRuneDeposit(int depositedCount)
        {
            int desiredDistance = depositedCount <= 1 ? FirstRuneRetreatDistance : SecondRuneRetreatDistance;
            FieldSlotData retreatSlot = FindRetreatSlot(desiredDistance);

            if (retreatSlot != null)
            {
                hunterSlot = retreatSlot;
                HiddenSlotChanged?.Invoke(hunterSlot);
            }

            ObserveCurrentPlayerSlot("Rune retreat target");
            retreatGraceRemaining = retreatGraceSeconds;
            skipNextMoveOpportunity = false;
            secondsUntilMove = RollMoveInterval(PursuerState.Interest);
        }

        private FieldSlotData FindRetreatSlot(int desiredDistanceFromHome)
        {
            if (fieldData == null || homeSlot == null)
            {
                return hunterSlot;
            }

            List<FieldSlotData> bestSlots = new List<FieldSlotData>();
            int bestDifference = int.MaxValue;
            int bestDistanceFromPlayer = int.MinValue;

            for (int i = 0; i < fieldData.Slots.Count; i++)
            {
                FieldSlotData candidate = fieldData.Slots[i];

                if (candidate == null)
                {
                    continue;
                }

                int distanceFromHome = GetDistance(homeSlot, candidate);
                int difference = Mathf.Abs(distanceFromHome - desiredDistanceFromHome);
                int distanceFromPlayer = GetDistance(candidate, CurrentPlayerSlot);

                if (difference < bestDifference || (difference == bestDifference && distanceFromPlayer > bestDistanceFromPlayer))
                {
                    bestSlots.Clear();
                    bestSlots.Add(candidate);
                    bestDifference = difference;
                    bestDistanceFromPlayer = distanceFromPlayer;
                }
                else if (difference == bestDifference && distanceFromPlayer == bestDistanceFromPlayer)
                {
                    bestSlots.Add(candidate);
                }
            }

            return bestSlots.Count == 0 ? hunterSlot : bestSlots[RollInt(0, bestSlots.Count)];
        }

        private void ObserveCurrentPlayerSlot(string reason)
        {
            FieldSlotData currentPlayerSlot = CurrentPlayerSlot;

            if (currentPlayerSlot == null)
            {
                return;
            }

            targetSlot = currentPlayerSlot;

            if (logStateChanges && Application.isPlaying)
            {
                Debug.Log($"Lost Forest TheHunter trail: Reason={reason}, Target={targetSlot.Address}, State={state}", this);
            }
        }

        private bool ShouldRefreshSearchTrail()
        {
            // The travel log is the pursuer's trail record. If it is not
            // available during scene setup, the local entry counter preserves
            // the same deterministic cadence.
            int recordedStepCount = playerFieldTravelLog == null
                ? playerSlotEntryCount
                : playerFieldTravelLog.StepCount;
            return recordedStepCount > 0 && recordedStepCount % searchTrailRefreshEverySlotEntries == 0;
        }

        private void SetState(PursuerState newState, string reason)
        {
            if (state == newState)
            {
                return;
            }

            PursuerState previousState = state;
            state = newState;
            secondsUntilMove = RollMoveInterval(state);
            UpdateFogPressure();
            StateChanged?.Invoke(previousState, newState);

            if (logStateChanges)
            {
                Debug.Log($"Lost Forest TheHunter state: {previousState} -> {newState}. Reason={reason}, Alert={alert:0.0}, Hidden={(hunterSlot == null ? "None" : hunterSlot.Address)}, Target={(targetSlot == null ? "None" : targetSlot.Address)}", this);
            }
        }

        private void CatchPlayer()
        {
            if (state == PursuerState.Catch || IsRunTerminal())
            {
                return;
            }

            hasForcedState = false;
            SetState(PursuerState.Catch, "Hunter reached the player");
            SetFogPressure(1f);
            CaughtPlayer?.Invoke();
            playerCondition?.TriggerGameOver(
                "Caught",
                "The Hunter found your trail.\nPress R to Play Again.");
        }

        private bool IsRunTerminal()
        {
            return (runVictoryController != null && runVictoryController.IsVictory) ||
                   (playerCondition != null && playerCondition.IsGameOver);
        }

        private void UpdateFogPressure()
        {
            float pressure = state == PursuerState.ClosePressure
                ? closeFogPressure
                : state == PursuerState.Stalk
                    ? stalkFogPressure
                    : 0f;
            SetFogPressure(pressure);
        }

        private void SetFogPressure(float pressure)
        {
            if (fogDirector != null)
            {
                fogDirector.SetExternalVisibilityPressure(pressure);
            }
        }

        private void RaiseCue(string cue)
        {
            CueRaised?.Invoke(cue);

            if (logStateChanges)
            {
                Debug.Log($"Lost Forest TheHunter cue: {cue}", this);
            }
        }

        private float RollMoveInterval(PursuerState forState)
        {
            // States still govern tracking accuracy and proximity feedback, but
            // rune progress alone governs speed. The Hunter can never step more
            // quickly than the player's roughly 15-second hex-crossing pace.
            return GetMoveIntervalForDepositedRunes(runeManager == null ? 0 : runeManager.DepositedRuneCount);
        }

        public float GetMoveIntervalForDepositedRunes(int depositedRuneCount)
        {
            return depositedRuneCount >= 2
                ? twoRunesReturnedMoveIntervalSeconds
                : depositedRuneCount == 1
                    ? oneRuneReturnedMoveIntervalSeconds
                    : noRunesReturnedMoveIntervalSeconds;
        }

        private int RollInt(int minimumInclusive, int maximumExclusive)
        {
            return random == null
                ? minimumInclusive
                : random.Next(minimumInclusive, Mathf.Max(minimumInclusive + 1, maximumExclusive));
        }

        private bool RollChance(float chance)
        {
            return random != null && random.NextDouble() < Mathf.Clamp01(chance);
        }

        private void HandleDevelopmentInput()
        {
            if (!enableDevelopmentHotkeys)
            {
                return;
            }

            if (forceCatchKey != KeyCode.None && Input.GetKeyDown(forceCatchKey))
            {
                ForceStateForDebug(PursuerState.Catch);
                return;
            }

            if (cycleForceStateKey != KeyCode.None && Input.GetKeyDown(cycleForceStateKey))
            {
                PursuerState nextState = state == PursuerState.Dormant
                    ? PursuerState.Interest
                    : state == PursuerState.Interest
                        ? PursuerState.Search
                        : state == PursuerState.Search
                            ? PursuerState.Stalk
                            : state == PursuerState.Stalk
                                ? PursuerState.ClosePressure
                                : PursuerState.Dormant;
                ForceStateForDebug(nextState);
            }
        }

        private void DiscoverSources()
        {
            if (playerGridAddressTracker == null)
            {
                playerGridAddressTracker = FindAnyObjectByType<PlayerGridAddressTracker>();
            }

            if (playerFieldTravelLog == null)
            {
                playerFieldTravelLog = FindAnyObjectByType<PlayerFieldTravelLog>();
            }

            if (firstPersonController == null)
            {
                firstPersonController = FindAnyObjectByType<EarlyWalkThruFirstPersonController>();
            }

            if (playerCondition == null)
            {
                playerCondition = FindAnyObjectByType<PlayerCondition>();
            }

            if (runeManager == null)
            {
                runeManager = FindAnyObjectByType<RuneManager>();
            }

            if (runVictoryController == null)
            {
                runVictoryController = FindAnyObjectByType<RunVictoryController>();
            }

            if (fogDirector == null)
            {
                fogDirector = FindAnyObjectByType<PrototypeFogDirector>();
            }
        }

        private void SubscribeSources()
        {
            if (sourcesSubscribed)
            {
                return;
            }

            if (playerGridAddressTracker != null)
            {
                playerGridAddressTracker.CurrentSlotChanged += HandlePlayerSlotChanged;
            }

            if (runeManager != null)
            {
                runeManager.RunePickedUp += HandleRunePickedUp;
                runeManager.RuneDeposited += HandleRuneDeposited;
            }

            sourcesSubscribed = true;
        }

        private void UnsubscribeSources()
        {
            if (!sourcesSubscribed)
            {
                return;
            }

            if (playerGridAddressTracker != null)
            {
                playerGridAddressTracker.CurrentSlotChanged -= HandlePlayerSlotChanged;
            }

            if (runeManager != null)
            {
                runeManager.RunePickedUp -= HandleRunePickedUp;
                runeManager.RuneDeposited -= HandleRuneDeposited;
            }

            sourcesSubscribed = false;
        }

        private int BuildRunSeed(int fieldSeed)
        {
            unchecked
            {
                return (fieldSeed * 486187739) ^ 0x48554E54;
            }
        }

        private static int GetDistance(FieldSlotData fromSlot, FieldSlotData toSlot)
        {
            return fromSlot == null || toSlot == null
                ? -1
                : HexFrameMath.GetHexDistance(fromSlot.AxialCoordinate, toSlot.AxialCoordinate);
        }

    }
}
