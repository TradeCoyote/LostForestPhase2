using System.Collections.Generic;
using System.Text;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public sealed class RuneManager : MonoBehaviour
    {
        private const int RequiredRuneTargetCount = 3;
        private const int AlphabetRuneCount = 26;

        [Header("Run Setup")]
        [SerializeField] private int runSeedSalt = 7317;
        [SerializeField, Range(1, 8)] private int preferredMinRequiredSlotDistanceFromHome = 2;
        [SerializeField] private bool logStateChanges = true;

        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;

        [Header("Interaction")]
        [SerializeField] private float pickupDistanceMeters = 4.5f;
        [SerializeField] private float pickupLookAngleDegrees = 42f;
        [SerializeField] private float socketDepositDistanceMeters = 4.75f;
        [SerializeField] private float socketDepositLookAngleDegrees = 70f;
        [SerializeField] private bool requireLookForPickup = true;
        [SerializeField] private bool requireLookForDeposit;

        [Header("Perception")]
        [SerializeField] private float markerVisibleDistanceMeters = 34f;
        [SerializeField] private float markerLetterReadableDistanceMeters = 16f;
        [SerializeField] private float homeSocketVisibleDistanceMeters = 30f;
        [SerializeField] private float homeSocketLetterReadableDistanceMeters = 12f;

        [Header("Prototype Colors")]
        [SerializeField] private Color forestDiscColor = new Color(0.08f, 0.11f, 0.12f, 1f);
        [SerializeField] private Color forestLetterColor = new Color(1f, 0.94f, 0.08f, 1f);
        [SerializeField] private Color emptySocketColor = new Color(0.46f, 0.46f, 0.46f, 1f);
        [SerializeField] private Color depositedSocketColor = new Color(1f, 0.02f, 0.02f, 1f);
        [SerializeField] private Color homeNeededLetterColor = new Color(1f, 0.02f, 0.02f, 1f);

        private readonly List<char> neededRunes = new List<char>(RequiredRuneTargetCount);
        private readonly HashSet<char> neededRuneSet = new HashSet<char>();
        private readonly HashSet<char> depositedRunes = new HashSet<char>();
        private readonly Dictionary<char, string> requiredSlotAddressByRune = new Dictionary<char, string>();
        private readonly HashSet<string> claimedMarkerKeys = new HashSet<string>();
        private readonly HashSet<RuneTreeMarker> activeMarkers = new HashSet<RuneTreeMarker>();
        private readonly HashSet<HomeRuneSocket> activeSockets = new HashSet<HomeRuneSocket>();

        private FieldData fieldData;
        private FieldSlotData homeSlot;
        private Material forestDiscMaterial;
        private Material emptySocketMaterial;
        private Material depositedSocketMaterial;
        private char carriedRune = RuneId.NoRune;
        private string carriedMarkerKey;
        private int runSeed;

        public int NeededRuneCount => neededRunes.Count;
        public bool HasCarriedRune => RuneId.IsValidRune(carriedRune);
        public char CarriedRune => carriedRune;
        public string NeededRunesDebugText => JoinRunes(neededRunes, false);
        public string CarriedRuneDebugText => HasCarriedRune ? carriedRune.ToString() : "None";
        public string DepositedRunesDebugText => BuildDepositedRunesDebugText();
        public int ActiveMarkerCount
        {
            get
            {
                PruneInactiveReferences();
                return activeMarkers.Count;
            }
        }

        public Material ForestDiscMaterial
        {
            get
            {
                EnsureMaterials();
                return forestDiscMaterial;
            }
        }

        public Material EmptySocketMaterial
        {
            get
            {
                EnsureMaterials();
                return emptySocketMaterial;
            }
        }

        public Material DepositedSocketMaterial
        {
            get
            {
                EnsureMaterials();
                return depositedSocketMaterial;
            }
        }

        public Color ForestLetterColor => forestLetterColor;
        public Color HomeNeededLetterColor => homeNeededLetterColor;
        public Camera PlayerCamera => playerCamera == null ? Camera.main : playerCamera;

        public bool IsMarkerVisible(Vector3 worldPosition)
        {
            return IsWithinPlayerDistance(worldPosition, markerVisibleDistanceMeters);
        }

        public bool IsMarkerLetterReadable(Vector3 worldPosition)
        {
            return IsWithinPlayerDistance(worldPosition, markerLetterReadableDistanceMeters);
        }

        public bool IsHomeSocketVisible(Vector3 worldPosition)
        {
            return IsWithinPlayerDistance(worldPosition, homeSocketVisibleDistanceMeters);
        }

        public bool IsHomeSocketLetterReadable(Vector3 worldPosition)
        {
            return IsWithinPlayerDistance(worldPosition, homeSocketLetterReadableDistanceMeters);
        }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void SetCamera(Camera newPlayerCamera)
        {
            playerCamera = newPlayerCamera;
        }

        public void InitializeRun(FieldData newFieldData, FieldSlotData newHomeSlot)
        {
            fieldData = newFieldData;
            homeSlot = newHomeSlot;
            activeMarkers.Clear();
            activeSockets.Clear();
            claimedMarkerKeys.Clear();
            depositedRunes.Clear();
            requiredSlotAddressByRune.Clear();
            neededRunes.Clear();
            neededRuneSet.Clear();
            carriedRune = RuneId.NoRune;
            carriedMarkerKey = null;

            if (fieldData == null || homeSlot == null)
            {
                return;
            }

            runSeed = BuildRunSeed(fieldData.Seed, runSeedSalt);
            System.Random random = new System.Random(runSeed);
            ChooseRequiredRunes(random);
            AssignGuaranteedRequiredSlots(random);

            if (logStateChanges)
            {
                Debug.Log($"Lost Forest Rune Run started: Needed={NeededRunesDebugText}, GuaranteedSlots={BuildGuaranteedSlotDebugText()}, Carried={CarriedRuneDebugText}, Deposited={DepositedRunesDebugText}", this);
            }
        }

        public char GetNeededRuneAt(int index)
        {
            return index >= 0 && index < neededRunes.Count ? neededRunes[index] : RuneId.NoRune;
        }

        public bool IsRuneNeeded(char runeLetter)
        {
            char normalized = RuneId.Normalize(runeLetter);
            return neededRuneSet.Contains(normalized) && !depositedRunes.Contains(normalized);
        }

        public bool IsRuneDeposited(char runeLetter)
        {
            return depositedRunes.Contains(RuneId.Normalize(runeLetter));
        }

        public bool TryGetRequiredRuneSlotAddress(char runeLetter, out string slotAddress)
        {
            return requiredSlotAddressByRune.TryGetValue(RuneId.Normalize(runeLetter), out slotAddress);
        }

        public int GetRequiredRuneSlotDistanceFromHome(char runeLetter)
        {
            if (fieldData == null || homeSlot == null || !TryGetRequiredRuneSlotAddress(runeLetter, out string slotAddress))
            {
                return -1;
            }

            FieldSlotData slot = fieldData.GetSlot(slotAddress);
            return slot == null ? -1 : HexFrameMath.GetHexDistance(homeSlot.AxialCoordinate, slot.AxialCoordinate);
        }

        public bool IsMarkerClaimed(string markerKey)
        {
            return !string.IsNullOrWhiteSpace(markerKey) && claimedMarkerKeys.Contains(markerKey);
        }

        public void RegisterMarker(RuneTreeMarker marker)
        {
            if (marker == null || !marker.IsAvailable || IsMarkerClaimed(marker.MarkerKey))
            {
                return;
            }

            activeMarkers.Add(marker);
        }

        public void UnregisterMarker(RuneTreeMarker marker)
        {
            if (marker != null)
            {
                activeMarkers.Remove(marker);
            }
        }

        public void RegisterSocket(HomeRuneSocket socket)
        {
            if (socket == null)
            {
                return;
            }

            activeSockets.Add(socket);
            socket.SetDeposited(IsRuneDeposited(socket.Letter));
        }

        public void UnregisterSocket(HomeRuneSocket socket)
        {
            if (socket != null)
            {
                activeSockets.Remove(socket);
            }
        }

        public int SpawnTreeMarkersForSlot(Transform contentRoot, FieldSlotData fieldSlot, IReadOnlyList<RuneTreeAnchor> treeAnchors)
        {
            if (contentRoot == null || fieldSlot == null || treeAnchors == null || treeAnchors.Count == 0 || neededRunes.Count == 0)
            {
                return 0;
            }

            if (IsHomeSlot(fieldSlot))
            {
                return 0;
            }

            EnsureMaterials();
            System.Random random = new System.Random(BuildSlotRuneSeed(fieldSlot));
            int markerCount = random.Next(1, Mathf.Min(3, treeAnchors.Count) + 1);
            List<int> treeIndices = PickTreeIndices(random, treeAnchors.Count, markerCount);
            List<char> markerLetters = BuildMarkerLettersForSlot(fieldSlot, random, markerCount);
            int spawnedCount = 0;

            for (int i = 0; i < treeIndices.Count && i < markerLetters.Count; i++)
            {
                RuneTreeAnchor anchor = treeAnchors[treeIndices[i]];
                string markerKey = BuildMarkerKey(fieldSlot.Address, anchor.TreeIndex);

                if (IsMarkerClaimed(markerKey))
                {
                    continue;
                }

                RuneTreeMarker marker = RuneTreeMarker.CreatePrototypeMarker(
                    anchor,
                    this,
                    markerLetters[i],
                    fieldSlot.Address,
                    markerKey,
                    forestDiscMaterial,
                    forestLetterColor,
                    PlayerCamera);

                if (marker != null)
                {
                    spawnedCount++;
                }
            }

            return spawnedCount;
        }

        public bool TryInteract(Transform actor, Camera interactionCamera)
        {
            if (actor != null)
            {
                player = actor;
            }

            if (interactionCamera != null)
            {
                playerCamera = interactionCamera;
            }

            PruneInactiveReferences();

            if (HasCarriedRune)
            {
                if (TryDepositCarriedRune())
                {
                    return true;
                }

                LogInteractionMiss($"No matching Home socket in range for carried rune {carriedRune}.");
                return false;
            }

            RuneTreeMarker pickupMarker = FindBestPickupMarker();

            if (pickupMarker == null)
            {
                LogInteractionMiss("No needed rune marker in pickup range.");
                return false;
            }

            return TryPickUpMarker(pickupMarker);
        }

        public bool TryGetNearestMatchingRuneSlotDebug(out string slotAddress, out char runeLetter, out float distanceMeters)
        {
            PruneInactiveReferences();
            RuneTreeMarker marker = FindNearestMatchingMarker(false);

            if (marker == null)
            {
                slotAddress = "None";
                runeLetter = RuneId.NoRune;
                distanceMeters = 0f;
                return false;
            }

            slotAddress = marker.FieldSlotAddress;
            runeLetter = marker.Letter;
            distanceMeters = GetActorDistance(marker.InteractionPosition);
            return true;
        }

        private bool TryPickUpMarker(RuneTreeMarker marker)
        {
            if (marker == null || !marker.IsAvailable)
            {
                return false;
            }

            char runeLetter = marker.Letter;

            if (HasCarriedRune || !IsRuneNeeded(runeLetter))
            {
                LogInteractionMiss($"Rune {runeLetter} is not currently needed or cannot be carried.");
                return false;
            }

            carriedRune = runeLetter;
            carriedMarkerKey = marker.MarkerKey;
            claimedMarkerKeys.Add(marker.MarkerKey);
            marker.SetCollected();

            if (logStateChanges)
            {
                Debug.Log($"Lost Forest Rune Pickup: Rune={carriedRune}, Slot={marker.FieldSlotAddress}, Carried={CarriedRuneDebugText}, Deposited={DepositedRunesDebugText}, ActiveMarkers={ActiveMarkerCount}", this);
            }

            return true;
        }

        private bool TryDepositCarriedRune()
        {
            HomeRuneSocket socket = FindBestSocketForCarriedRune();

            if (socket == null)
            {
                return false;
            }

            char depositedRune = carriedRune;
            depositedRunes.Add(depositedRune);
            carriedRune = RuneId.NoRune;
            carriedMarkerKey = null;
            socket.SetDeposited(true);
            RefreshSocketStates();

            if (logStateChanges)
            {
                Debug.Log($"Lost Forest Rune Deposit: Rune={depositedRune}, Deposited={DepositedRunesDebugText}, Carried={CarriedRuneDebugText}", this);
            }

            return true;
        }

        private RuneTreeMarker FindBestPickupMarker()
        {
            return FindNearestMatchingMarker(true);
        }

        private RuneTreeMarker FindNearestMatchingMarker(bool requireInteractionFit)
        {
            Vector3 origin = GetLookOrigin();
            Vector3 forward = GetLookForward();
            RuneTreeMarker bestMarker = null;
            float bestScore = float.PositiveInfinity;

            foreach (RuneTreeMarker marker in activeMarkers)
            {
                if (marker == null || !marker.IsAvailable || !IsRuneNeeded(marker.Letter))
                {
                    continue;
                }

                float actorDistance = GetActorDistance(marker.InteractionPosition);

                if (requireInteractionFit && actorDistance > pickupDistanceMeters)
                {
                    continue;
                }

                Vector3 toMarker = marker.InteractionPosition - origin;
                float angle = toMarker.sqrMagnitude <= 0.0001f ? 0f : Vector3.Angle(forward, toMarker.normalized);

                if (requireInteractionFit && requireLookForPickup && angle > pickupLookAngleDegrees)
                {
                    continue;
                }

                float score = (actorDistance * 0.5f) + angle;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMarker = marker;
                }
            }

            return bestMarker;
        }

        private HomeRuneSocket FindBestSocketForCarriedRune()
        {
            if (!HasCarriedRune)
            {
                return null;
            }

            Vector3 origin = GetLookOrigin();
            Vector3 forward = GetLookForward();
            HomeRuneSocket bestSocket = null;
            float bestDistance = float.PositiveInfinity;

            foreach (HomeRuneSocket socket in activeSockets)
            {
                if (socket == null || socket.IsDeposited || socket.Letter != carriedRune)
                {
                    continue;
                }

                float distance = GetActorDistance(socket.InteractionPosition);

                if (distance > socketDepositDistanceMeters)
                {
                    continue;
                }

                if (requireLookForDeposit)
                {
                    Vector3 toSocket = socket.InteractionPosition - origin;
                    float angle = toSocket.sqrMagnitude <= 0.0001f ? 0f : Vector3.Angle(forward, toSocket.normalized);

                    if (angle > socketDepositLookAngleDegrees)
                    {
                        continue;
                    }
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSocket = socket;
                }
            }

            return bestSocket;
        }

        private void ChooseRequiredRunes(System.Random random)
        {
            List<char> alphabet = new List<char>(AlphabetRuneCount);

            for (int i = 0; i < AlphabetRuneCount; i++)
            {
                alphabet.Add((char)('A' + i));
            }

            for (int i = 0; i < RequiredRuneTargetCount && alphabet.Count > 0; i++)
            {
                int index = random.Next(0, alphabet.Count);
                char runeLetter = alphabet[index];
                alphabet.RemoveAt(index);
                neededRunes.Add(runeLetter);
                neededRuneSet.Add(runeLetter);
            }
        }

        private void AssignGuaranteedRequiredSlots(System.Random random)
        {
            List<FieldSlotData> candidates = CollectRequiredSlotCandidates(preferredMinRequiredSlotDistanceFromHome);

            if (candidates.Count < neededRunes.Count)
            {
                candidates = CollectRequiredSlotCandidates(1);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("Lost Forest Rune Run could not assign guaranteed required rune slots because no non-Home slots exist.", this);
                return;
            }

            Shuffle(candidates, random);

            for (int i = 0; i < neededRunes.Count; i++)
            {
                requiredSlotAddressByRune[neededRunes[i]] = candidates[i % candidates.Count].Address;
            }
        }

        private List<FieldSlotData> CollectRequiredSlotCandidates(int minHexDistanceFromHome)
        {
            List<FieldSlotData> candidates = new List<FieldSlotData>();

            if (fieldData == null)
            {
                return candidates;
            }

            for (int i = 0; i < fieldData.Slots.Count; i++)
            {
                FieldSlotData slot = fieldData.Slots[i];

                if (slot == null || IsHomeSlot(slot))
                {
                    continue;
                }

                int distanceFromHome = homeSlot == null ? minHexDistanceFromHome : HexFrameMath.GetHexDistance(homeSlot.AxialCoordinate, slot.AxialCoordinate);

                if (distanceFromHome >= minHexDistanceFromHome)
                {
                    candidates.Add(slot);
                }
            }

            return candidates;
        }

        private List<char> BuildMarkerLettersForSlot(FieldSlotData fieldSlot, System.Random random, int markerCount)
        {
            List<char> markerLetters = new List<char>(markerCount);

            if (!IsHomeSlot(fieldSlot) && TryGetAssignedRequiredRuneForSlot(fieldSlot.Address, out char requiredRune) && IsRuneNeeded(requiredRune) && carriedRune != requiredRune)
            {
                markerLetters.Add(requiredRune);
            }

            while (markerLetters.Count < markerCount)
            {
                markerLetters.Add(GetRandomAmbientRune(random));
            }

            return markerLetters;
        }

        private char GetRandomAmbientRune(System.Random random)
        {
            return (char)('A' + random.Next(0, AlphabetRuneCount));
        }

        private bool TryGetAssignedRequiredRuneForSlot(string slotAddress, out char runeLetter)
        {
            foreach (KeyValuePair<char, string> entry in requiredSlotAddressByRune)
            {
                if (entry.Value == slotAddress)
                {
                    runeLetter = entry.Key;
                    return true;
                }
            }

            runeLetter = RuneId.NoRune;
            return false;
        }

        private void RefreshSocketStates()
        {
            foreach (HomeRuneSocket socket in activeSockets)
            {
                if (socket != null)
                {
                    socket.SetDeposited(IsRuneDeposited(socket.Letter));
                }
            }
        }

        private void PruneInactiveReferences()
        {
            activeMarkers.RemoveWhere(marker => marker == null || !marker.IsAvailable);
            activeSockets.RemoveWhere(socket => socket == null);
        }

        private Vector3 GetLookOrigin()
        {
            Camera resolvedCamera = PlayerCamera;

            if (resolvedCamera != null)
            {
                return resolvedCamera.transform.position;
            }

            return player == null ? transform.position : player.position + Vector3.up * 1.6f;
        }

        private Vector3 GetLookForward()
        {
            Camera resolvedCamera = PlayerCamera;

            if (resolvedCamera != null)
            {
                return resolvedCamera.transform.forward;
            }

            return player == null ? transform.forward : player.forward;
        }

        private float GetActorDistance(Vector3 targetPosition)
        {
            Vector3 actorPosition = player == null ? transform.position : player.position;
            return Vector3.Distance(actorPosition, targetPosition);
        }

        private bool IsWithinPlayerDistance(Vector3 worldPosition, float maxDistanceMeters)
        {
            if (maxDistanceMeters <= 0f)
            {
                return false;
            }

            return GetActorDistance(worldPosition) <= maxDistanceMeters;
        }

        private bool IsHomeSlot(FieldSlotData fieldSlot)
        {
            if (fieldSlot == null)
            {
                return false;
            }

            if (homeSlot != null && fieldSlot.Address == homeSlot.Address)
            {
                return true;
            }

            return fieldSlot.Role == FieldSlotRole.PlayerHomeSpawn || fieldSlot.TileId == FrameSettings.PlayerHomeTileId;
        }

        private void EnsureMaterials()
        {
            forestDiscMaterial = forestDiscMaterial == null ? CreateMaterial("Prototype Rune Tree Disc Material", forestDiscColor) : forestDiscMaterial;
            emptySocketMaterial = emptySocketMaterial == null ? CreateMaterial("Prototype Home Rune Empty Socket Material", emptySocketColor) : emptySocketMaterial;
            depositedSocketMaterial = depositedSocketMaterial == null ? CreateMaterial("Prototype Home Rune Deposited Socket Material", depositedSocketColor) : depositedSocketMaterial;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Material material = new Material(FindShader("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static Shader FindShader(string preferredShader)
        {
            Shader shader = Shader.Find(preferredShader);

            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");

            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Sprites/Default");
        }

        private static int BuildRunSeed(int fieldSeed, int seedSalt)
        {
            unchecked
            {
                int hash = 43;
                hash = hash * 397 + fieldSeed;
                hash = hash * 397 + seedSalt;
                return hash & 0x7fffffff;
            }
        }

        private int BuildSlotRuneSeed(FieldSlotData fieldSlot)
        {
            unchecked
            {
                int hash = runSeed == 0 ? BuildRunSeed(fieldData == null ? 0 : fieldData.Seed, runSeedSalt) : runSeed;
                hash = hash * 397 + GetStableStringHash(fieldSlot.Address);
                hash = hash * 397 + fieldSlot.TileId;
                hash = hash * 397 + fieldSlot.OrientationIndex;
                return hash & 0x7fffffff;
            }
        }

        private static string BuildMarkerKey(string slotAddress, int treeIndex)
        {
            return $"{slotAddress}:Tree{treeIndex:00}";
        }

        private static List<int> PickTreeIndices(System.Random random, int treeCount, int markerCount)
        {
            List<int> availableIndices = new List<int>(treeCount);

            for (int i = 0; i < treeCount; i++)
            {
                availableIndices.Add(i);
            }

            List<int> pickedIndices = new List<int>(markerCount);

            while (pickedIndices.Count < markerCount && availableIndices.Count > 0)
            {
                int availableIndex = random.Next(0, availableIndices.Count);
                pickedIndices.Add(availableIndices[availableIndex]);
                availableIndices.RemoveAt(availableIndex);
            }

            return pickedIndices;
        }

        private static void Shuffle<T>(IList<T> items, System.Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(0, i + 1);
                T value = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = value;
            }
        }

        private static int GetStableStringHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;

                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static string JoinRunes(IReadOnlyList<char> runes, bool useUnderscoreForMissing)
        {
            if (runes == null || runes.Count == 0)
            {
                return useUnderscoreForMissing ? "_ _ _" : "None";
            }

            StringBuilder builder = new StringBuilder(runes.Count * 2);

            for (int i = 0; i < runes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(RuneId.IsValidRune(runes[i]) ? runes[i] : '_');
            }

            return builder.ToString();
        }

        private string BuildDepositedRunesDebugText()
        {
            if (neededRunes.Count == 0)
            {
                return "_ _ _";
            }

            StringBuilder builder = new StringBuilder(neededRunes.Count * 2);

            for (int i = 0; i < neededRunes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                char runeLetter = neededRunes[i];
                builder.Append(depositedRunes.Contains(runeLetter) ? runeLetter : '_');
            }

            return builder.ToString();
        }

        private string BuildGuaranteedSlotDebugText()
        {
            if (neededRunes.Count == 0)
            {
                return "None";
            }

            StringBuilder builder = new StringBuilder(neededRunes.Count * 8);

            for (int i = 0; i < neededRunes.Count; i++)
            {
                char runeLetter = neededRunes[i];
                string slotAddress = requiredSlotAddressByRune.TryGetValue(runeLetter, out string address) ? address : "None";

                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(runeLetter);
                builder.Append('=');
                builder.Append(slotAddress);
            }

            return builder.ToString();
        }

        private void LogInteractionMiss(string reason)
        {
            if (logStateChanges)
            {
                Debug.Log($"Lost Forest Rune Interaction: {reason} Needed={NeededRunesDebugText}, Carried={CarriedRuneDebugText}, Deposited={DepositedRunesDebugText}, ActiveMarkers={ActiveMarkerCount}", this);
            }
        }
    }
}
