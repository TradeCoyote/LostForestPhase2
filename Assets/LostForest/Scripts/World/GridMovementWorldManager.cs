using LostForest.Phase2.Debugging;
using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class GridMovementWorldManager : MonoBehaviour
    {
        [Header("Hidden Field")]
        [SerializeField] private FrameSettings frameSettings = new FrameSettings();
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool logInitialization = true;

        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private ActiveRegionRenderer activeRegionRenderer;
        [SerializeField] private PlayerGridAddressTracker playerGridAddressTracker;
        [SerializeField] private PlayerFieldTravelLog playerFieldTravelLog;
        [SerializeField] private GridDebugHud gridDebugHud;
        [SerializeField] private RuneManager runeManager;

        [Header("Player Spawn")]
        [SerializeField] private float footClearanceMeters = 0.18f;
        [SerializeField] private bool faceTowardPositiveZOnSpawn = true;

        private bool trackerEventSubscribed;

        public FieldData FieldData { get; private set; }
        public FieldSlotData HomeSlot { get; private set; }
        public FieldSlotData PursuerOriginSlot { get; private set; }
        public FieldSlotData CurrentPlayerSlot => playerGridAddressTracker == null ? null : playerGridAddressTracker.CurrentSlot;
        public PlayerFieldTravelLog FieldTravelLog => playerFieldTravelLog;
        public RuneManager RuneManager => runeManager;

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void SetActiveRegionRenderer(ActiveRegionRenderer newActiveRegionRenderer)
        {
            activeRegionRenderer = newActiveRegionRenderer;
        }

        public void SetPlayerGridAddressTracker(PlayerGridAddressTracker newPlayerGridAddressTracker)
        {
            UnsubscribeTracker();
            playerGridAddressTracker = newPlayerGridAddressTracker;
            SubscribeTracker();

            if (playerFieldTravelLog != null)
            {
                playerFieldTravelLog.SetTracker(playerGridAddressTracker);
            }
        }

        public void SetPlayerFieldTravelLog(PlayerFieldTravelLog newPlayerFieldTravelLog)
        {
            playerFieldTravelLog = newPlayerFieldTravelLog;

            if (playerFieldTravelLog != null)
            {
                playerFieldTravelLog.SetTracker(playerGridAddressTracker);
            }
        }

        public void SetGridDebugHud(GridDebugHud newGridDebugHud)
        {
            gridDebugHud = newGridDebugHud;
        }

        public void SetRuneManager(RuneManager newRuneManager)
        {
            runeManager = newRuneManager;

            if (activeRegionRenderer != null)
            {
                activeRegionRenderer.SetRuneManager(runeManager);
            }
        }

        [ContextMenu("Initialize Grid Movement World")]
        public void InitializeWorld()
        {
            DiscoverSceneReferences();
            SubscribeTracker();

            if (frameSettings == null)
            {
                frameSettings = new FrameSettings();
            }

            FieldData = FieldGenerator.Generate(frameSettings);
            HomeSlot = FindSlotByRoleOrTile(FieldSlotRole.PlayerHomeSpawn, FrameSettings.PlayerHomeTileId);
            PursuerOriginSlot = FindSlotByRoleOrTile(FieldSlotRole.PursuerSpawn, FrameSettings.PursuerTileId);

            Camera playerCamera = player == null ? null : player.GetComponentInChildren<Camera>();

            if (runeManager != null)
            {
                runeManager.SetPlayer(player);
                runeManager.SetCamera(playerCamera);
                runeManager.InitializeRun(FieldData, HomeSlot);
            }

            if (playerFieldTravelLog != null)
            {
                playerFieldTravelLog.SetWorldContext(FieldData, HomeSlot, PursuerOriginSlot);
                playerFieldTravelLog.SetTracker(playerGridAddressTracker);
                playerFieldTravelLog.ClearHistory();
            }

            if (activeRegionRenderer != null)
            {
                activeRegionRenderer.ClearRenderedSlots();
                activeRegionRenderer.SetRuneManager(runeManager);
                activeRegionRenderer.Configure(FieldData, frameSettings.HexOuterRadiusMeters);
                activeRegionRenderer.RenderAround(HomeSlot);
            }

            SpawnPlayerAtHome();

            if (playerGridAddressTracker != null)
            {
                playerGridAddressTracker.SetFieldData(FieldData);
                playerGridAddressTracker.RefreshCurrentSlot(true);
            }

            if (gridDebugHud != null)
            {
                gridDebugHud.SetSources(playerGridAddressTracker, activeRegionRenderer);
                gridDebugHud.SetRuneManager(runeManager);
            }

            if (logInitialization)
            {
                Debug.Log(BuildInitializationLog());
            }
        }

        private void Awake()
        {
            DiscoverSceneReferences();
        }

        private void OnEnable()
        {
            SubscribeTracker();
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeWorld();
            }
        }

        private void OnDisable()
        {
            UnsubscribeTracker();
        }

        private void DiscoverSceneReferences()
        {
            if (activeRegionRenderer == null)
            {
                activeRegionRenderer = GetComponent<ActiveRegionRenderer>();
            }

            if (activeRegionRenderer == null)
            {
                activeRegionRenderer = FindAnyObjectByType<ActiveRegionRenderer>();
            }

            if (playerGridAddressTracker == null && player != null)
            {
                playerGridAddressTracker = player.GetComponent<PlayerGridAddressTracker>();
            }

            if (playerGridAddressTracker == null)
            {
                playerGridAddressTracker = FindAnyObjectByType<PlayerGridAddressTracker>();
            }

            if (player == null && playerGridAddressTracker != null)
            {
                player = playerGridAddressTracker.transform;
            }

            if (player == null)
            {
                EarlyWalkThruFirstPersonController controller = FindAnyObjectByType<EarlyWalkThruFirstPersonController>();
                player = controller == null ? null : controller.transform;
            }

            if (gridDebugHud == null)
            {
                gridDebugHud = GetComponent<GridDebugHud>();
            }

            if (gridDebugHud == null)
            {
                gridDebugHud = FindAnyObjectByType<GridDebugHud>();
            }

            if (runeManager == null)
            {
                runeManager = GetComponent<RuneManager>();
            }

            if (runeManager == null)
            {
                runeManager = FindAnyObjectByType<RuneManager>();
            }

            if (playerFieldTravelLog == null)
            {
                playerFieldTravelLog = GetComponent<PlayerFieldTravelLog>();
            }

            if (playerFieldTravelLog == null)
            {
                playerFieldTravelLog = FindAnyObjectByType<PlayerFieldTravelLog>();
            }
        }

        private void SubscribeTracker()
        {
            if (trackerEventSubscribed || playerGridAddressTracker == null)
            {
                return;
            }

            playerGridAddressTracker.CurrentSlotChanged += HandlePlayerGridSlotChanged;
            trackerEventSubscribed = true;
        }

        private void UnsubscribeTracker()
        {
            if (!trackerEventSubscribed || playerGridAddressTracker == null)
            {
                trackerEventSubscribed = false;
                return;
            }

            playerGridAddressTracker.CurrentSlotChanged -= HandlePlayerGridSlotChanged;
            trackerEventSubscribed = false;
        }

        private void HandlePlayerGridSlotChanged(FieldSlotData previousSlot, FieldSlotData currentSlot)
        {
            if (activeRegionRenderer == null || currentSlot == null)
            {
                return;
            }

            if (activeRegionRenderer.CurrentCenterSlot == currentSlot)
            {
                return;
            }

            activeRegionRenderer.RenderAround(currentSlot);
        }

        private void SpawnPlayerAtHome()
        {
            if (player == null || HomeSlot == null)
            {
                return;
            }

            Vector3 groundPosition = HomeSlot.WorldCenter;

            if (activeRegionRenderer != null && activeRegionRenderer.TrySampleSlotSurface(HomeSlot, HomeSlot.WorldCenter, out TerrainSurfaceSample surfaceSample))
            {
                groundPosition = surfaceSample.Position;
            }

            CharacterController characterController = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = characterController == null || characterController.enabled;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            player.position = groundPosition + Vector3.up * (footClearanceMeters + GetControllerFootToTransformOffset(characterController));

            if (faceTowardPositiveZOnSpawn)
            {
                player.rotation = Quaternion.identity;
            }

            if (characterController != null)
            {
                characterController.enabled = controllerWasEnabled;
            }

            EarlyWalkThruFirstPersonController firstPersonController = player.GetComponent<EarlyWalkThruFirstPersonController>();

            if (firstPersonController != null)
            {
                firstPersonController.ResetVerticalVelocity();
            }

            PlayerCondition playerCondition = player.GetComponent<PlayerCondition>();

            if (playerCondition != null)
            {
                playerCondition.ResetCondition();
            }

            Debug.Log($"Lost Forest Grid Movement player spawned at Home Slot={HomeSlot.Address}, Tile={HomeSlot.TileIdLabel}, Row={HomeSlot.RowIndex}, Column={HomeSlot.ColumnIndex}, Axial=({HomeSlot.AxialQ}, {HomeSlot.AxialR}), XYZ=({player.position.x:0.00}, {player.position.y:0.00}, {player.position.z:0.00})");
        }

        private FieldSlotData FindSlotByRoleOrTile(FieldSlotRole role, int tileId)
        {
            if (FieldData == null)
            {
                return null;
            }

            for (int i = 0; i < FieldData.Slots.Count; i++)
            {
                FieldSlotData slot = FieldData.Slots[i];

                if (slot != null && slot.Role == role)
                {
                    return slot;
                }
            }

            for (int i = 0; i < FieldData.Slots.Count; i++)
            {
                FieldSlotData slot = FieldData.Slots[i];

                if (slot != null && slot.TileId == tileId)
                {
                    return slot;
                }
            }

            return FieldData.SlotsFilled == 0 ? null : FieldData.Slots[0];
        }

        private string BuildInitializationLog()
        {
            string homeAddress = HomeSlot == null ? "None" : HomeSlot.Address;
            string pursuerAddress = PursuerOriginSlot == null ? "None" : PursuerOriginSlot.Address;
            int activeRadius = activeRegionRenderer == null ? 0 : activeRegionRenderer.ActiveRadius;
            int activeSlots = activeRegionRenderer == null ? 0 : activeRegionRenderer.ActiveRenderedSlotCount;
            bool travelLogActive = playerFieldTravelLog != null;
            string neededRunes = runeManager == null ? "None" : runeManager.NeededRunesDebugText;
            return $"Lost Forest Grid Movement World initialized: Field={FieldData.Rows}x{FieldData.Columns}, Seed={FieldData.Seed}, Home={homeAddress}, PursuerOrigin={pursuerAddress}, ActiveRadius={activeRadius}, ActiveSlots={activeSlots}, TravelLogActive={travelLogActive}, NeededRunes={neededRunes}";
        }

        private static float GetControllerFootToTransformOffset(CharacterController characterController)
        {
            if (characterController == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, (characterController.height * 0.5f) - characterController.center.y);
        }
    }
}
