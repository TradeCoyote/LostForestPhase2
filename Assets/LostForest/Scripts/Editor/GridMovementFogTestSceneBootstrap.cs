#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LostForest.Phase2.Debugging;
using LostForest.Phase2.Feedback;
using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace LostForest.Phase2.Editor
{
    public static class GridMovementFogTestSceneBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity";
        private const string WorldObjectName = "Grid Movement World";
        private const string PlayerObjectName = "Grid Movement Player";

        [MenuItem("Lost Forest/Bootstrap/Open Grid Movement Fog Test Scene")]
        public static void OpenGridMovementFogTestScene()
        {
            CreateOrRepairGridMovementFogTestScene();
            Selection.activeGameObject = UnityObject.FindAnyObjectByType<EarlyWalkThruFirstPersonController>()?.gameObject;
        }

        [MenuItem("Lost Forest/Bootstrap/Create or Repair Grid Movement Fog Test Scene")]
        public static void CreateOrRepairGridMovementFogTestScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GridMovementWorldManager worldManager = EnsureWorldManager(
                out ActiveRegionRenderer activeRegionRenderer,
                out GridDebugHud gridDebugHud,
                out PlayerFieldTravelLog playerFieldTravelLog,
                out RuneManager runeManager);
            GameObject playerObject = EnsurePlayer(
                out PlayerGridAddressTracker gridAddressTracker,
                out PlayerCondition playerCondition,
                out PlayerTerrainMovementState playerTerrainMovementState,
                out RuneInteraction runeInteraction);
            Camera playerCamera = playerObject.GetComponentInChildren<Camera>();

            worldManager.SetPlayer(playerObject.transform);
            worldManager.SetActiveRegionRenderer(activeRegionRenderer);
            worldManager.SetPlayerGridAddressTracker(gridAddressTracker);
            worldManager.SetPlayerFieldTravelLog(playerFieldTravelLog);
            worldManager.SetGridDebugHud(gridDebugHud);
            worldManager.SetRuneManager(runeManager);
            runeManager.SetPlayer(playerObject.transform);
            runeManager.SetCamera(playerCamera);
            activeRegionRenderer.SetRuneManager(runeManager);
            runeInteraction.SetSources(runeManager, playerCamera);
            runeInteraction.SetInteractionKey(KeyCode.X);
            playerFieldTravelLog.SetTracker(gridAddressTracker);
            playerTerrainMovementState.SetSources(gridAddressTracker, activeRegionRenderer);
            gridDebugHud.SetSources(gridAddressTracker, activeRegionRenderer);
            gridDebugHud.SetPlayerCondition(playerCondition);
            gridDebugHud.SetPlayerTerrainMovementState(playerTerrainMovementState);
            gridDebugHud.SetRuneManager(runeManager);
            gridDebugHud.SetCamera(playerCamera);
            gridDebugHud.ApplyCompactDefaults();
            activeRegionRenderer.SetActiveRadius(1);
            activeRegionRenderer.ApplyBroadSlopeTerrainDefaults();

            EnsurePrototypeFog();
            EnsureLight();

            Selection.activeGameObject = playerObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Lost Forest Grid Movement Fog test scene is ready: {ScenePath}");
        }

        [MenuItem("Lost Forest/Bootstrap/Validate Grid Movement Fog Test Scene")]
        public static void ValidateGridMovementFogTestScene()
        {
            CreateOrRepairGridMovementFogTestScene();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GridMovementWorldManager worldManager = UnityObject.FindAnyObjectByType<GridMovementWorldManager>();
            ActiveRegionRenderer activeRegionRenderer = UnityObject.FindAnyObjectByType<ActiveRegionRenderer>();
            PlayerGridAddressTracker gridAddressTracker = UnityObject.FindAnyObjectByType<PlayerGridAddressTracker>();
            PlayerFieldTravelLog playerFieldTravelLog = UnityObject.FindAnyObjectByType<PlayerFieldTravelLog>();
            PlayerCondition playerCondition = UnityObject.FindAnyObjectByType<PlayerCondition>();
            PlayerTerrainMovementState playerTerrainMovementState = UnityObject.FindAnyObjectByType<PlayerTerrainMovementState>();
            RuneManager runeManager = UnityObject.FindAnyObjectByType<RuneManager>();
            RuneInteraction runeInteraction = UnityObject.FindAnyObjectByType<RuneInteraction>();

            if (worldManager == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no GridMovementWorldManager exists in the scene.");
            }

            if (activeRegionRenderer == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no ActiveRegionRenderer exists in the scene.");
            }

            if (gridAddressTracker == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PlayerGridAddressTracker exists in the scene.");
            }

            if (playerFieldTravelLog == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PlayerFieldTravelLog exists in the scene.");
            }

            if (playerCondition == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PlayerCondition exists on the player.");
            }

            if (playerTerrainMovementState == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PlayerTerrainMovementState exists on the player.");
            }

            if (runeManager == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no RuneManager exists in the scene.");
            }

            if (runeInteraction == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no RuneInteraction exists on the player.");
            }

            worldManager.InitializeWorld();
            playerTerrainMovementState.SetSources(gridAddressTracker, activeRegionRenderer);
            runeManager.SetPlayer(gridAddressTracker.transform);
            runeManager.SetCamera(gridAddressTracker.GetComponentInChildren<Camera>());

            if (worldManager.FieldData == null || worldManager.FieldData.Rows != FrameSettings.CanonicalRows || worldManager.FieldData.Columns != FrameSettings.CanonicalColumns)
            {
                throw new InvalidOperationException("Grid Movement validation failed: canonical 26 x 26 Field was not generated.");
            }

            if (worldManager.HomeSlot == null || worldManager.HomeSlot.TileId != FrameSettings.PlayerHomeTileId)
            {
                throw new InvalidOperationException("Grid Movement validation failed: Home Slot / Tile 000 was not resolved.");
            }

            if (!activeRegionRenderer.TryGetRenderedSlot(worldManager.HomeSlot, out RenderedSlotInstance homeInstance) || homeInstance == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: Home Slot was not rendered.");
            }

            if (activeRegionRenderer.ActiveRadius != 1 || activeRegionRenderer.ActiveRenderedSlotCount != 7)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected radius 1 with 7 active Slots, got radius {activeRegionRenderer.ActiveRadius} with {activeRegionRenderer.ActiveRenderedSlotCount} active Slots.");
            }

            if (gridAddressTracker.CurrentSlot != worldManager.HomeSlot)
            {
                throw new InvalidOperationException("Grid Movement validation failed: player did not resolve to the Home Grid Address after spawn.");
            }

            if (playerFieldTravelLog.StepCount <= 0 || playerFieldTravelLog.LastStep == null || playerFieldTravelLog.LastStep.CurrentSlot != worldManager.HomeSlot)
            {
                throw new InvalidOperationException("Grid Movement validation failed: player Field travel log did not record the initial Home Slot.");
            }

            float movementValidationSpeed = playerTerrainMovementState.EvaluateMovement(Vector3.forward, 1f, 6.5f, false, false, 0f);

            if (!playerTerrainMovementState.HasTerrainSample || movementValidationSpeed <= 0f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: PlayerTerrainMovementState could not sample terrain under the player.");
            }

            ValidateConditionEconomy(playerCondition);
            ValidateRunePrototype(worldManager, activeRegionRenderer, runeManager);

            Debug.Log($"Lost Forest Grid Movement validation passed: Field={worldManager.FieldData.Rows}x{worldManager.FieldData.Columns}, Home={worldManager.HomeSlot.Address}, ActiveSlots={activeRegionRenderer.ActiveRenderedSlotCount}, CurrentGridAddress={gridAddressTracker.CurrentGridAddress}, TravelSteps={playerFieldTravelLog.StepCount}, Stamina={playerCondition.Stamina:0}/{playerCondition.EffectiveMaxStamina:0}, Chill={playerCondition.Chill:0}, ConditionSpeedMultiplier={playerCondition.ConditionSpeedMultiplier:0.00}, Frozen={playerCondition.IsFrozen}, GameOver={playerCondition.IsGameOver}, MovementSlope={playerTerrainMovementState.CurrentSlopeDegrees:0.0}deg, MovementGrade={playerTerrainMovementState.SignedMovementGradeDegrees:0.0}deg, TerrainSpeedMultiplier={playerTerrainMovementState.SpeedMultiplier:0.00}, NeededRunes={runeManager.NeededRunesDebugText}, Deposited={runeManager.DepositedRunesDebugText}, ActiveRuneMarkers={runeManager.ActiveMarkerCount}");
        }

        private static GridMovementWorldManager EnsureWorldManager(
            out ActiveRegionRenderer activeRegionRenderer,
            out GridDebugHud gridDebugHud,
            out PlayerFieldTravelLog playerFieldTravelLog,
            out RuneManager runeManager)
        {
            GridMovementWorldManager worldManager = UnityObject.FindAnyObjectByType<GridMovementWorldManager>();
            GameObject worldObject;

            if (worldManager == null)
            {
                worldObject = GameObject.Find(WorldObjectName);

                if (worldObject == null)
                {
                    worldObject = new GameObject(WorldObjectName);
                }

                worldManager = GetOrAddComponent<GridMovementWorldManager>(worldObject);
            }
            else
            {
                worldObject = worldManager.gameObject;
            }

            worldObject.name = WorldObjectName;
            activeRegionRenderer = GetOrAddComponent<ActiveRegionRenderer>(worldObject);
            gridDebugHud = GetOrAddComponent<GridDebugHud>(worldObject);
            playerFieldTravelLog = GetOrAddComponent<PlayerFieldTravelLog>(worldObject);
            runeManager = GetOrAddComponent<RuneManager>(worldObject);
            return worldManager;
        }

        private static GameObject EnsurePlayer(
            out PlayerGridAddressTracker gridAddressTracker,
            out PlayerCondition playerCondition,
            out PlayerTerrainMovementState playerTerrainMovementState,
            out RuneInteraction runeInteraction)
        {
            EarlyWalkThruFirstPersonController existingController = UnityObject.FindAnyObjectByType<EarlyWalkThruFirstPersonController>();
            GameObject playerObject = existingController == null
                ? GameObject.Find(PlayerObjectName)
                : existingController.gameObject;

            if (playerObject == null)
            {
                playerObject = new GameObject(PlayerObjectName);
            }

            playerObject.name = PlayerObjectName;
            playerObject.transform.position = Vector3.zero;
            playerObject.transform.rotation = Quaternion.identity;

            CharacterController characterController = GetOrAddComponent<CharacterController>(playerObject);
            characterController.height = 1.85f;
            characterController.radius = 0.34f;
            characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
            characterController.slopeLimit = 55f;
            characterController.stepOffset = 0.35f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0f;

            Transform cameraRoot = EnsurePlayerCamera(playerObject.transform);
            EarlyWalkThruFirstPersonController firstPersonController = GetOrAddComponent<EarlyWalkThruFirstPersonController>(playerObject);
            firstPersonController.SetCameraRoot(cameraRoot);
            firstPersonController.SetSprintKey(KeyCode.Space);

            playerCondition = GetOrAddComponent<PlayerCondition>(playerObject);
            playerCondition.ApplyPhase2PrototypeEconomyDefaults();
            firstPersonController.SetPlayerCondition(playerCondition);
            gridAddressTracker = GetOrAddComponent<PlayerGridAddressTracker>(playerObject);
            playerTerrainMovementState = GetOrAddComponent<PlayerTerrainMovementState>(playerObject);
            firstPersonController.SetPlayerTerrainMovementState(playerTerrainMovementState);
            runeInteraction = GetOrAddComponent<RuneInteraction>(playerObject);
            FirstPersonCameraWalkBob walkBob = GetOrAddComponent<FirstPersonCameraWalkBob>(playerObject);
            walkBob.SetCameraRoot(cameraRoot);
            walkBob.SetSources(firstPersonController, playerTerrainMovementState);

            DisableNonPlayerCameras(cameraRoot.GetComponent<Camera>());
            return playerObject;
        }

        private static PrototypeFogDirector EnsurePrototypeFog()
        {
            PrototypeFogDirector fogDirector = UnityObject.FindAnyObjectByType<PrototypeFogDirector>();

            if (fogDirector == null)
            {
                GameObject fogObject = new GameObject("Prototype Distance Fog Director");
                fogDirector = fogObject.AddComponent<PrototypeFogDirector>();
            }

            fogDirector.gameObject.name = "Prototype Distance Fog Director";
            fogDirector.ApplyEarlyFogDefaults();
            fogDirector.ApplyFogSettings();
            return fogDirector;
        }

        private static Transform EnsurePlayerCamera(Transform playerTransform)
        {
            Transform cameraRoot = playerTransform.Find("First Person Camera");

            if (cameraRoot == null)
            {
                cameraRoot = new GameObject("First Person Camera").transform;
                cameraRoot.SetParent(playerTransform, false);
            }

            cameraRoot.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraRoot.localRotation = Quaternion.identity;

            Camera camera = GetOrAddComponent<Camera>(cameraRoot.gameObject);
            camera.tag = "MainCamera";
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            return cameraRoot;
        }

        private static void DisableNonPlayerCameras(Camera playerCamera)
        {
            Camera[] cameras = UnityObject.FindObjectsByType<Camera>();

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != playerCamera)
                {
                    cameras[i].enabled = false;
                }
            }
        }

        private static void EnsureLight()
        {
            Light light = UnityObject.FindAnyObjectByType<Light>();

            if (light == null)
            {
                GameObject lightObject = new GameObject("Grid Movement Key Light");
                light = lightObject.AddComponent<Light>();
            }

            light.name = "Grid Movement Key Light";
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static void ValidateConditionEconomy(PlayerCondition playerCondition)
        {
            playerCondition.ResetCondition();
            playerCondition.Tick(60f, false, false, false);

            if (playerCondition.Chill <= 0f || playerCondition.Chill > 7f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: chill should creep slowly over one minute, got Chill={playerCondition.Chill:0.00}.");
            }

            ValidateLinearChillStaminaCap(playerCondition, "one idle minute");

            float idleOneMinuteChill = playerCondition.Chill;
            playerCondition.ResetCondition();
            playerCondition.Tick(60f, true, false, false);

            if (playerCondition.Chill >= idleOneMinuteChill)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: standing still should build more chill than walking, got IdleChill={idleOneMinuteChill:0.00}, WalkingChill={playerCondition.Chill:0.00}.");
            }

            playerCondition.ResetCondition();
            playerCondition.Tick(6f, true, true, true);

            if (playerCondition.Stamina >= playerCondition.EffectiveMaxStamina)
            {
                throw new InvalidOperationException("Grid Movement validation failed: sprinting did not drain stamina.");
            }

            if (playerCondition.SprintFatigueCapMultiplier >= 0.999f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: sprinting did not apply fatigue cap pressure.");
            }

            playerCondition.ResetCondition();
            playerCondition.Tick(900f, false, false, false);

            ValidateLinearChillStaminaCap(playerCondition, "deep chill");

            if (playerCondition.ChillStaminaCapMultiplier >= 0.999f || playerCondition.EffectiveMaxStamina >= playerCondition.BaseMaxStamina)
            {
                throw new InvalidOperationException("Grid Movement validation failed: chill did not lower the effective stamina cap.");
            }

            if (playerCondition.ConditionSpeedMultiplier >= 0.999f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: chill did not lower the condition speed multiplier.");
            }

            playerCondition.ResetCondition();
            playerCondition.Tick(2400f, false, false, false);

            if (!playerCondition.IsFrozen || !playerCondition.IsGameOver)
            {
                throw new InvalidOperationException("Grid Movement validation failed: 100% chill did not freeze the player and trigger prototype game over.");
            }

            playerCondition.ResetCondition();
        }

        private static void ValidateRunePrototype(GridMovementWorldManager worldManager, ActiveRegionRenderer activeRegionRenderer, RuneManager runeManager)
        {
            if (runeManager.NeededRuneCount != 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected 3 needed runes, got {runeManager.NeededRuneCount}.");
            }

            HashSet<char> distinctRunes = new HashSet<char>();

            for (int i = 0; i < runeManager.NeededRuneCount; i++)
            {
                char runeLetter = runeManager.GetNeededRuneAt(i);
                string slotAddress = null;

                if (!RuneId.IsValidRune(runeLetter))
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune index {i} is not A-Z.");
                }

                if (!distinctRunes.Add(runeLetter))
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} was chosen more than once.");
                }

                if (!runeManager.TryGetRequiredRuneSlotAddress(runeLetter, out slotAddress))
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} has no guaranteed slot placement.");
                }

                if (slotAddress == worldManager.HomeSlot.Address)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} was placed on the Home Slot.");
                }

                int distanceFromHome = runeManager.GetRequiredRuneSlotDistanceFromHome(runeLetter);

                if (distanceFromHome < 2)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} should be at least 2 Slots from Home, got distance {distanceFromHome}.");
                }
            }

            HomeRuneSocket[] sockets = UnityObject.FindObjectsByType<HomeRuneSocket>();

            if (sockets.Length != 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected 3 Home rune sockets, got {sockets.Length}.");
            }

            for (int i = 0; i < runeManager.NeededRuneCount; i++)
            {
                char runeLetter = runeManager.GetNeededRuneAt(i);
                bool foundSocket = false;

                for (int socketIndex = 0; socketIndex < sockets.Length; socketIndex++)
                {
                    if (sockets[socketIndex] != null && sockets[socketIndex].Letter == runeLetter)
                    {
                        foundSocket = true;
                        break;
                    }
                }

                if (!foundSocket)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: no Home rune socket exists for needed rune {runeLetter}.");
                }
            }

            for (int i = 0; i < runeManager.NeededRuneCount; i++)
            {
                char runeLetter = runeManager.GetNeededRuneAt(i);
                runeManager.TryGetRequiredRuneSlotAddress(runeLetter, out string slotAddress);
                FieldSlotData requiredSlot = worldManager.FieldData.GetSlot(slotAddress);

                if (requiredSlot == null)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: guaranteed slot {slotAddress} for needed rune {runeLetter} was not found in the Field.");
                }

                activeRegionRenderer.RenderAround(requiredSlot);
                RuneTreeMarker[] markers = UnityObject.FindObjectsByType<RuneTreeMarker>();
                bool foundRequiredMarker = false;

                for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                {
                    RuneTreeMarker marker = markers[markerIndex];

                    if (marker != null && marker.Letter == runeLetter && marker.FieldSlotAddress == slotAddress)
                    {
                        foundRequiredMarker = true;
                        break;
                    }
                }

                if (!foundRequiredMarker)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: rendering guaranteed slot {slotAddress} did not create needed rune marker {runeLetter}.");
                }
            }

            activeRegionRenderer.RenderAround(worldManager.HomeSlot);

            if (runeManager.ActiveMarkerCount <= 0)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no active tree rune markers were spawned.");
            }
        }

        private static void ValidateLinearChillStaminaCap(PlayerCondition playerCondition, string reason)
        {
            float expectedMultiplier = Mathf.Clamp01(1f - playerCondition.ChillNormalized);

            if (Mathf.Abs(playerCondition.ChillStaminaCapMultiplier - expectedMultiplier) > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: chill stamina cap should be linear for {reason}, expected {expectedMultiplier:0.000}, got {playerCondition.ChillStaminaCapMultiplier:0.000}.");
            }

            float expectedEffectiveMaxStamina = playerCondition.BaseMaxStamina * expectedMultiplier * playerCondition.SprintFatigueCapMultiplier;

            if (Mathf.Abs(playerCondition.EffectiveMaxStamina - expectedEffectiveMaxStamina) > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: effective stamina cap did not match linear chill cap for {reason}, expected {expectedEffectiveMaxStamina:0.000}, got {playerCondition.EffectiveMaxStamina:0.000}.");
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
        }
    }
}
#endif
