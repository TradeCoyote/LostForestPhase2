#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LostForest.Phase2.Core;
using LostForest.Phase2.Debugging;
using LostForest.Phase2.Feedback;
using LostForest.Phase2.Player;
using LostForest.Phase2.Pursuer;
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
        private const string FirstAlarmDrumClipPath = "Assets/LostForest/Assets/soundreality-taiko-drum-367656.mp3";
        private const string SecondStageHuntDrumClipPath = "Assets/LostForest/Assets/dragon-studio-tribal-drum-beat-443144.mp3";
        private const string CrowsClipPath = "Assets/LostForest/Assets/Crows1.mp3";
        private const string OwlClipPath = "Assets/LostForest/Assets/Owl1.mp3";

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
                out RuneManager runeManager,
                out WorldEndFrostController worldEndFrostController,
                out RunVictoryController runVictoryController,
                out TheHunterPursuerController theHunter,
                out TheHunterDrumCueDirector theHunterDrums);
            GameObject playerObject = EnsurePlayer(
                out PlayerGridAddressTracker gridAddressTracker,
                out PlayerCondition playerCondition,
                out PlayerTerrainMovementState playerTerrainMovementState,
                out RuneInteraction runeInteraction);
            Camera playerCamera = playerObject.GetComponentInChildren<Camera>();
            EarlyWalkThruFirstPersonController firstPersonController = playerObject.GetComponent<EarlyWalkThruFirstPersonController>();
            Light directSun = EnsureLight();
            PrototypeLightingDirector lightingDirector = EnsurePrototypeLighting(directSun);

            worldManager.SetPlayer(playerObject.transform);
            worldManager.SetActiveRegionRenderer(activeRegionRenderer);
            worldManager.SetPlayerGridAddressTracker(gridAddressTracker);
            worldManager.SetPlayerFieldTravelLog(playerFieldTravelLog);
            worldManager.SetGridDebugHud(gridDebugHud);
            worldManager.SetRuneManager(runeManager);
            worldManager.SetWorldEndFrostController(worldEndFrostController);
            worldManager.SetTheHunter(theHunter);
            worldManager.ApplyOpeningViewDefaults();
            runeManager.ApplyPrototypeRuneColorDefaults();
            runeManager.SetPlayer(playerObject.transform);
            runeManager.SetCamera(playerCamera);
            activeRegionRenderer.SetRuneManager(runeManager);
            worldEndFrostController.ApplyPrototypeDefaults();
            firstPersonController?.SetWorldEndFrostController(worldEndFrostController);
            runeInteraction.SetSources(runeManager, playerCamera);
            runeInteraction.SetInteractionKey(KeyCode.X);
            runVictoryController.ApplyPrototypeDefaults();
            runVictoryController.SetSources(runeManager, playerCondition, firstPersonController, runeInteraction, playerCamera);
            runVictoryController.ResetVictoryState();
            PrototypeOwlGuidanceDirector owlGuidance = GetOrAddComponent<PrototypeOwlGuidanceDirector>(worldManager.gameObject);
            owlGuidance.ApplyPrototypeDefaults();
            owlGuidance.SetSources(runeManager, playerObject.transform, playerCamera, playerCondition, runVictoryController);
            owlGuidance.SetOwlClip(LoadOwlClip());
            playerFieldTravelLog.SetTracker(gridAddressTracker);
            playerTerrainMovementState.SetSources(gridAddressTracker, activeRegionRenderer);
            gridDebugHud.SetSources(gridAddressTracker, activeRegionRenderer);
            gridDebugHud.SetPlayerCondition(playerCondition);
            gridDebugHud.SetPlayerTerrainMovementState(playerTerrainMovementState);
            gridDebugHud.SetRuneManager(runeManager);
            gridDebugHud.SetTheHunter(theHunter);
            gridDebugHud.SetWorldEndFrostController(worldEndFrostController);
            gridDebugHud.SetLightingDirector(lightingDirector);
            gridDebugHud.SetCamera(playerCamera);
            gridDebugHud.ApplyCompactDefaults();
            activeRegionRenderer.SetActiveRadius(1);
            activeRegionRenderer.SetOuterFrostRenderRings(3);
            activeRegionRenderer.ApplyBroadSlopeTerrainDefaults();
            activeRegionRenderer.ApplyHiddenBoundaryVisualDefaults();

            PrototypeFogDirector fogDirector = EnsurePrototypeFog();
            PrototypeFogCeilingDirector fogCeilingDirector = EnsurePrototypeFogCeiling(playerObject.transform);
            fogDirector.ResetToNormalAndScheduleNextWhiteout();
            theHunter.ApplyTheHunterPrototypeDefaults();
            theHunter.SetSources(
                gridAddressTracker,
                playerFieldTravelLog,
                firstPersonController,
                playerCondition,
                runeManager,
                runVictoryController,
                fogDirector);
            theHunterDrums.ApplyTheHunterPrototypeDefaults();
            theHunterDrums.SetSources(theHunter, playerCondition, runVictoryController);
            theHunterDrums.SetFirstAlarmDrumClip(LoadFirstAlarmDrumClip());
            theHunterDrums.SetSecondStageHuntDrumClip(LoadSecondStageHuntDrumClip());
            TheHunterCrowCueDirector theHunterCrows = GetOrAddComponent<TheHunterCrowCueDirector>(worldManager.gameObject);
            theHunterCrows.ApplyPrototypeDefaults();
            theHunterCrows.SetSources(theHunter, playerObject.transform, playerCondition, runVictoryController);
            theHunterCrows.SetCrowsClip(LoadCrowsClip());

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
            WorldEndFrostController worldEndFrostController = UnityObject.FindAnyObjectByType<WorldEndFrostController>();
            PrototypeLightingDirector lightingDirector = UnityObject.FindAnyObjectByType<PrototypeLightingDirector>();
            PrototypeFogDirector fogDirector = UnityObject.FindAnyObjectByType<PrototypeFogDirector>();
            PrototypeFogCeilingDirector fogCeilingDirector = UnityObject.FindAnyObjectByType<PrototypeFogCeilingDirector>();
            PrototypeOwlGuidanceDirector owlGuidance = UnityObject.FindAnyObjectByType<PrototypeOwlGuidanceDirector>();
            RunVictoryController runVictoryController = UnityObject.FindAnyObjectByType<RunVictoryController>();
            TheHunterPursuerController theHunter = UnityObject.FindAnyObjectByType<TheHunterPursuerController>();
            TheHunterDrumCueDirector theHunterDrums = UnityObject.FindAnyObjectByType<TheHunterDrumCueDirector>();
            TheHunterCrowCueDirector theHunterCrows = UnityObject.FindAnyObjectByType<TheHunterCrowCueDirector>();

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

            if (worldEndFrostController == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no WorldEndFrostController exists in the scene.");
            }

            if (lightingDirector == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PrototypeLightingDirector exists in the scene.");
            }

            if (fogDirector == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PrototypeFogDirector exists in the scene.");
            }

            if (fogCeilingDirector == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PrototypeFogCeilingDirector exists in the scene.");
            }

            if (owlGuidance == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no PrototypeOwlGuidanceDirector exists in the scene.");
            }

            if (runVictoryController == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no RunVictoryController exists in the scene.");
            }

            if (theHunter == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no TheHunterPursuerController exists in the scene.");
            }

            if (theHunterDrums == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no TheHunterDrumCueDirector exists in the scene.");
            }

            if (theHunterCrows == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no TheHunterCrowCueDirector exists in the scene.");
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

            Vector3 expectedOpeningPosition = worldManager.HomeSlot.WorldCenter + Vector3.back * 5f;
            Vector2 actualOpeningPosition = new Vector2(gridAddressTracker.transform.position.x, gridAddressTracker.transform.position.z);
            Vector2 expectedOpeningPlanarPosition = new Vector2(expectedOpeningPosition.x, expectedOpeningPosition.z);

            if (Mathf.Abs(worldManager.OpeningViewBackwardOffsetMeters - 5f) > 0.001f || Vector2.Distance(actualOpeningPosition, expectedOpeningPlanarPosition) > 0.05f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: opening view must begin 5 meters behind Home. Offset={worldManager.OpeningViewBackwardOffsetMeters:0.00}m, Actual=({actualOpeningPosition.x:0.00},{actualOpeningPosition.y:0.00}), Expected=({expectedOpeningPlanarPosition.x:0.00},{expectedOpeningPlanarPosition.y:0.00}).");
            }

            if (Vector3.Dot(gridAddressTracker.transform.forward, Vector3.forward) < 0.999f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: opening view is not centered toward the Home stones.");
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
            ValidateWorldEndFrostPrototype(worldManager, activeRegionRenderer, playerCondition, worldEndFrostController);
            ValidateRunePrototype(worldManager, activeRegionRenderer, runeManager);
            ValidateOwlGuidancePrototype(owlGuidance);
            ValidateTheHunterPrototype(worldManager, theHunter, playerCondition, fogDirector);
            ValidateTheHunterDrumPrototype(theHunterDrums);
            ValidateTheHunterCrowPrototype(theHunterCrows);
            ValidateVictoryPrototype(runeManager, runVictoryController);
            ValidatePrototypeLighting(lightingDirector);
            ValidatePrototypeFog(fogDirector);
            ValidatePrototypeFogCeiling(fogCeilingDirector);
            ValidateTerrainExtremesAndHiddenBoundary(activeRegionRenderer);

            Debug.Log($"Lost Forest Grid Movement validation passed: Field={worldManager.FieldData.Rows}x{worldManager.FieldData.Columns}, Home={worldManager.HomeSlot.Address}, ActiveSlots={activeRegionRenderer.ActiveRenderedSlotCount}, FrostRings={activeRegionRenderer.OuterFrostRenderRings}, CurrentGridAddress={gridAddressTracker.CurrentGridAddress}, TravelSteps={playerFieldTravelLog.StepCount}, Stamina={playerCondition.Stamina:0}/{playerCondition.EffectiveMaxStamina:0}, Chill={playerCondition.Chill:0}, ConditionSpeedMultiplier={playerCondition.ConditionSpeedMultiplier:0.00}, Frozen={playerCondition.IsFrozen}, GameOver={playerCondition.IsGameOver}, MovementSlope={playerTerrainMovementState.CurrentSlopeDegrees:0.0}deg, MovementGrade={playerTerrainMovementState.SignedMovementGradeDegrees:+0.0;-0.0;0.0}deg, TerrainSpeedMultiplier={playerTerrainMovementState.SpeedMultiplier:0.00}, NeededRunes={runeManager.NeededRunesDebugText}, Deposited={runeManager.DepositedRunesDebugText}, ActiveRuneMarkers={runeManager.ActiveMarkerCount}, Hunter={theHunter.BuildDebugSummary()}, Drums={theHunterDrums.BuildDebugSummary()}, Crows={theHunterCrows.BuildDebugSummary()}, Owl={owlGuidance.BuildDebugSummary()}, Victory={runVictoryController.BuildDebugSummary()}, Lighting={lightingDirector.BuildDebugSummary()}, Fog={fogDirector.BuildDebugSummary()}, Ceiling={fogCeilingDirector.BuildDebugSummary()}, ExtremeSpots={activeRegionRenderer.ExtremeHeightSpotFraction * 100f:0.0}% x{activeRegionRenderer.ExtremeHeightMultiplier:0.00}, HiddenBoundary={activeRegionRenderer.BoundaryVisualsMatchPlayableForest}");
        }

        private static GridMovementWorldManager EnsureWorldManager(
            out ActiveRegionRenderer activeRegionRenderer,
            out GridDebugHud gridDebugHud,
            out PlayerFieldTravelLog playerFieldTravelLog,
            out RuneManager runeManager,
            out WorldEndFrostController worldEndFrostController,
            out RunVictoryController runVictoryController,
            out TheHunterPursuerController theHunter,
            out TheHunterDrumCueDirector theHunterDrums)
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
            worldEndFrostController = GetOrAddComponent<WorldEndFrostController>(worldObject);
            runVictoryController = GetOrAddComponent<RunVictoryController>(worldObject);
            theHunter = GetOrAddComponent<TheHunterPursuerController>(worldObject);
            theHunterDrums = GetOrAddComponent<TheHunterDrumCueDirector>(worldObject);
            GetOrAddComponent<AudioLowPassFilter>(worldObject);
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
            fogDirector.ResetToNormalAndScheduleNextWhiteout();
            fogDirector.ApplyFogSettings();
            return fogDirector;
        }

        private static PrototypeFogCeilingDirector EnsurePrototypeFogCeiling(Transform playerTransform)
        {
            PrototypeFogCeilingDirector fogCeilingDirector = UnityObject.FindAnyObjectByType<PrototypeFogCeilingDirector>();

            if (fogCeilingDirector == null)
            {
                GameObject fogCeilingObject = new GameObject("Prototype Wavering Fog Ceiling Director");
                fogCeilingDirector = fogCeilingObject.AddComponent<PrototypeFogCeilingDirector>();
            }

            fogCeilingDirector.gameObject.name = "Prototype Wavering Fog Ceiling Director";
            fogCeilingDirector.ApplyPrototypeDefaults();
            fogCeilingDirector.SetPlayer(playerTransform);
            return fogCeilingDirector;
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

        private static PrototypeLightingDirector EnsurePrototypeLighting(Light directSun)
        {
            PrototypeLightingDirector lightingDirector = UnityObject.FindAnyObjectByType<PrototypeLightingDirector>();

            if (lightingDirector == null)
            {
                GameObject lightingObject = new GameObject("Prototype Light and Shadow Director");
                lightingDirector = lightingObject.AddComponent<PrototypeLightingDirector>();
            }

            lightingDirector.gameObject.name = "Prototype Light and Shadow Director";
            lightingDirector.ApplyPrototypeDefaults();
            lightingDirector.SetDirectSun(directSun);
            lightingDirector.CaptureCurrentSunAsReference();
            lightingDirector.ResetToOvercastAndScheduleNextWindow();
            return lightingDirector;
        }

        private static Light EnsureLight()
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
            light.shadowStrength = 1f;
            light.color = Color.white;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.sun = light;
            return light;
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

        private static void ValidateWorldEndFrostPrototype(
            GridMovementWorldManager worldManager,
            ActiveRegionRenderer activeRegionRenderer,
            PlayerCondition playerCondition,
            WorldEndFrostController worldEndFrostController)
        {
            if (!worldEndFrostController.IsConfigured)
            {
                throw new InvalidOperationException("Grid Movement validation failed: World's End Frost controller was not configured.");
            }

            if (activeRegionRenderer.OuterFrostRenderRings != 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected World's End to render 3 frost rings in the border test scene, got {activeRegionRenderer.OuterFrostRenderRings}.");
            }

            FieldSlotData edgeSlot = worldManager.FieldData.GetSlot(0, 0);

            if (edgeSlot == null)
            {
                throw new InvalidOperationException("Grid Movement validation failed: could not resolve edge Slot A1 for frost render validation.");
            }

            Vector2Int outsideAxial = HexFrameMath.GetAxialNeighbor(edgeSlot.AxialCoordinate, HexDirection.West);
            int ringDepth = FieldBoundaryMath.GetRingDepthFromPlayableField(worldManager.FieldData, outsideAxial, out FieldSlotData nearestPlayableSlot);

            if (ringDepth != 1 || nearestPlayableSlot == null)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected west of A1 to be frost ring 1, got Ring={ringDepth}, Nearest={(nearestPlayableSlot == null ? "None" : nearestPlayableSlot.Address)}.");
            }

            Vector3 outsideWorldCenter = HexFrameMath.GetFlatTopHexCenterFromAxial(outsideAxial, 45f);

            if (FieldBoundaryMath.TryResolvePlayableSlot(worldManager.FieldData, 45f, outsideWorldCenter, out FieldSlotData outsideSlot) && outsideSlot != null)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: outside frost coordinate resolved to canonical Slot {outsideSlot.Address}.");
            }

            activeRegionRenderer.RenderAround(edgeSlot);

            if (activeRegionRenderer.ActiveRenderedFrostTileCount <= 0)
            {
                throw new InvalidOperationException("Grid Movement validation failed: rendering an edge Slot did not create temporary frost tiles.");
            }

            if (!activeRegionRenderer.TrySampleFrostTerrainElevation(outsideWorldCenter, out TerrainElevationSample frostElevationSample))
            {
                throw new InvalidOperationException("Grid Movement validation failed: rendered frost terrain could not be sampled.");
            }

            int activeFrostTilesAtEdge = activeRegionRenderer.ActiveRenderedFrostTileCount;

            playerCondition.ResetCondition();
            playerCondition.SetFrostChillPressure(0f, playerCondition.MaxChill / 30f);
            playerCondition.Tick(30f, true, false, false);

            if (!playerCondition.IsFrozen || !playerCondition.IsGameOver)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: 30 seconds of frost pressure did not freeze the player, Chill={playerCondition.Chill:0.00}/{playerCondition.MaxChill:0.00}.");
            }

            playerCondition.ResetCondition();
            activeRegionRenderer.RenderAround(worldManager.HomeSlot);
            Debug.Log($"Lost Forest World's End Frost validation passed: Edge={edgeSlot.Address}, OutsideAxial=({outsideAxial.x},{outsideAxial.y}), Ring={ringDepth}, ActiveFrostTiles={activeFrostTilesAtEdge}, SampleElevation={frostElevationSample.LogicalElevationMeters:0.0}m.");
        }

        private static void ValidateRunePrototype(GridMovementWorldManager worldManager, ActiveRegionRenderer activeRegionRenderer, RuneManager runeManager)
        {
            if (Mathf.Abs(runeManager.OwlFeatherReplacementChance - 0.025f) > 0.0001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: Owl feather replacement chance must be 2.5%, got {runeManager.OwlFeatherReplacementChance * 100f:0.00}%.");
            }

            if (runeManager.NeededRuneCount != 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected 3 needed runes, got {runeManager.NeededRuneCount}.");
            }

            if (runeManager.GuaranteedCopiesPerSelectedRune != 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: expected 3 guaranteed copies per selected rune, got {runeManager.GuaranteedCopiesPerSelectedRune}.");
            }

            HashSet<char> distinctRunes = new HashSet<char>();
            HashSet<string> allGuaranteedSlotAddresses = new HashSet<string>();
            List<FieldSlotData> allGuaranteedSlots = new List<FieldSlotData>();

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
                int minimumDistanceFromHome = runeManager.GetRequiredRuneMinimumSlotDistanceFromHome(i);

                if (distanceFromHome < minimumDistanceFromHome)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} at index {i} should be at least {minimumDistanceFromHome} rings from Home, got distance {distanceFromHome}.");
                }

                if (runeManager.GetGuaranteedRuneCopyCount(runeLetter) != runeManager.GuaranteedCopiesPerSelectedRune)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} should have {runeManager.GuaranteedCopiesPerSelectedRune} guaranteed copies, got {runeManager.GetGuaranteedRuneCopyCount(runeLetter)}.");
                }

                for (int copyIndex = 0; copyIndex < runeManager.GuaranteedCopiesPerSelectedRune; copyIndex++)
                {
                    if (!runeManager.TryGetGuaranteedRuneSlotAddress(runeLetter, copyIndex, out string copySlotAddress))
                    {
                        throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} is missing guaranteed copy {copyIndex + 1}.");
                    }

                    if (copySlotAddress == worldManager.HomeSlot.Address)
                    {
                        throw new InvalidOperationException($"Grid Movement validation failed: needed rune {runeLetter} copy {copyIndex + 1} was placed on the Home Slot.");
                    }

                    if (!allGuaranteedSlotAddresses.Add(copySlotAddress))
                    {
                        throw new InvalidOperationException($"Grid Movement validation failed: guaranteed rune copy slot {copySlotAddress} was assigned more than once.");
                    }

                    FieldSlotData copySlot = worldManager.FieldData.GetSlot(copySlotAddress);

                    if (copySlot == null)
                    {
                        throw new InvalidOperationException($"Grid Movement validation failed: guaranteed rune copy slot {copySlotAddress} was not found in the Field.");
                    }

                    for (int assignedIndex = 0; assignedIndex < allGuaranteedSlots.Count; assignedIndex++)
                    {
                        int separation = HexFrameMath.GetHexDistance(copySlot.AxialCoordinate, allGuaranteedSlots[assignedIndex].AxialCoordinate);

                        if (separation < runeManager.GuaranteedRuneMinimumSlotSeparationHexes)
                        {
                            throw new InvalidOperationException($"Grid Movement validation failed: guaranteed rune copy slot {copySlotAddress} is only {separation} rings from {allGuaranteedSlots[assignedIndex].Address}; the minimum is {runeManager.GuaranteedRuneMinimumSlotSeparationHexes}.");
                        }
                    }

                    allGuaranteedSlots.Add(copySlot);
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
                for (int copyIndex = 0; copyIndex < runeManager.GuaranteedCopiesPerSelectedRune; copyIndex++)
                {
                    runeManager.TryGetGuaranteedRuneSlotAddress(runeLetter, copyIndex, out string slotAddress);
                    FieldSlotData requiredSlot = worldManager.FieldData.GetSlot(slotAddress);

                    if (requiredSlot == null)
                    {
                        throw new InvalidOperationException($"Grid Movement validation failed: guaranteed copy {copyIndex + 1} slot {slotAddress} for needed rune {runeLetter} was not found in the Field.");
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
                        throw new InvalidOperationException($"Grid Movement validation failed: rendering guaranteed copy {copyIndex + 1} slot {slotAddress} did not create needed rune marker {runeLetter}.");
                    }
                }
            }

            activeRegionRenderer.RenderAround(worldManager.HomeSlot);

            if (runeManager.ActiveMarkerCount <= 0)
            {
                throw new InvalidOperationException("Grid Movement validation failed: no active tree rune markers were spawned.");
            }
        }

        private static void ValidateOwlGuidancePrototype(PrototypeOwlGuidanceDirector owlGuidance)
        {
            if (!owlGuidance.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            if (!owlGuidance.HasOwlClip)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: Owl guidance is not assigned from {OwlClipPath}.");
            }

            Vector2 owlLeadDuration = owlGuidance.LeadHomeVisibleDurationRangeSeconds;

            if (Mathf.Abs(owlLeadDuration.x - 5f) > 0.001f || Mathf.Abs(owlLeadDuration.y - 7f) > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: Owl guidance should fade 5-7 seconds after turning toward Home, got {owlLeadDuration.x:0.0}-{owlLeadDuration.y:0.0}s.");
            }

            Debug.Log($"Lost Forest Owl guidance validation passed: {owlGuidance.BuildDebugSummary()}");
        }

        private static void ValidateTheHunterPrototype(
            GridMovementWorldManager worldManager,
            TheHunterPursuerController theHunter,
            PlayerCondition playerCondition,
            PrototypeFogDirector fogDirector)
        {
            if (!theHunter.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            if (!theHunter.IsConfigured || theHunter.Archetype != PursuerArchetype.TheHunter)
            {
                throw new InvalidOperationException("Grid Movement validation failed: TheHunter was not configured as the active first pursuer.");
            }

            if (Mathf.Abs(theHunter.GetMoveIntervalForDepositedRunes(0) - 26f) > 0.001f ||
                Mathf.Abs(theHunter.GetMoveIntervalForDepositedRunes(1) - 20f) > 0.001f ||
                Mathf.Abs(theHunter.GetMoveIntervalForDepositedRunes(2) - 15f) > 0.001f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: TheHunter movement must be 26/20/15 seconds for 0/1/2 returned runes.");
            }

            if (theHunter.CurrentHiddenSlot != worldManager.PursuerOriginSlot || theHunter.State != PursuerState.Dormant)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: TheHunter should begin dormant on the pursuer origin. State={theHunter.State}, Origin={(theHunter.CurrentHiddenSlot == null ? "None" : theHunter.CurrentHiddenSlot.Address)}.");
            }

            theHunter.TickForValidation(18.1f);

            if (theHunter.State != PursuerState.Interest)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: TheHunter did not leave Dormant after its deterministic delay. State={theHunter.State}.");
            }

            theHunter.ForceStateForDebug(PursuerState.ClosePressure);

            if (theHunter.State != PursuerState.ClosePressure || fogDirector.ExternalVisibilityPressure <= 0f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: close TheHunter pressure did not set state and indirect fog cue. State={theHunter.State}, FogPressure={fogDirector.ExternalVisibilityPressure:0.00}.");
            }

            // Restore a clean natural run before later rune/victory validation.
            theHunter.ConfigureRun(worldManager.FieldData, worldManager.HomeSlot, worldManager.PursuerOriginSlot);

            if (theHunter.State != PursuerState.Dormant || fogDirector.ExternalVisibilityPressure > 0.001f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: TheHunter did not reset cleanly after validation controls.");
            }

            theHunter.ForceStateForDebug(PursuerState.Catch);

            if (theHunter.State != PursuerState.Catch || !playerCondition.IsGameOver)
            {
                throw new InvalidOperationException("Grid Movement validation failed: a Hunter catch did not enter the shared loss state.");
            }

            playerCondition.ResetCondition();
            theHunter.ConfigureRun(worldManager.FieldData, worldManager.HomeSlot, worldManager.PursuerOriginSlot);

            Debug.Log($"Lost Forest TheHunter validation passed: {theHunter.BuildDebugSummary()}");
        }

        private static void ValidateTheHunterDrumPrototype(TheHunterDrumCueDirector theHunterDrums)
        {
            if (!theHunterDrums.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            if (TheHunterDrumCueDirector.GetProximityForDistance(4) != TheHunterDrumProximity.None ||
                TheHunterDrumCueDirector.GetProximityForDistance(3) != TheHunterDrumProximity.ThreeHexes ||
                TheHunterDrumCueDirector.GetProximityForDistance(2) != TheHunterDrumProximity.TwoHexes ||
                TheHunterDrumCueDirector.GetProximityForDistance(1) != TheHunterDrumProximity.Adjacent)
            {
                throw new InvalidOperationException("Grid Movement validation failed: TheHunter drum proximity bands do not match 3/2/1 hidden-slot distance rules.");
            }

            if (!theHunterDrums.HasFirstAlarmDrumClip)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: the first Hunter alarm drum is not assigned from {FirstAlarmDrumClipPath}.");
            }

            if (!theHunterDrums.HasSecondStageHuntDrumClip)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: the second Hunter drum cue is not assigned from {SecondStageHuntDrumClipPath}.");
            }

            if (Mathf.Abs(theHunterDrums.AdjacentAlarmIntervalSeconds - 1.5f) > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: the adjacent Hunter alarm must repeat every 1.5 seconds, got {theHunterDrums.AdjacentAlarmIntervalSeconds:0.00} seconds.");
            }

            Debug.Log($"Lost Forest TheHunter drum validation passed: {theHunterDrums.BuildDebugSummary()}");
        }

        private static void ValidateTheHunterCrowPrototype(TheHunterCrowCueDirector theHunterCrows)
        {
            if (!theHunterCrows.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            if (!theHunterCrows.HasCrowsClip)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: Hunter crow cue is not assigned from {CrowsClipPath}.");
            }

            if (theHunterCrows.FlockCountRange.x != 10 || theHunterCrows.FlockCountRange.y != 20 ||
                theHunterCrows.AudioLayerCountRange.x != 5 || theHunterCrows.AudioLayerCountRange.y != 6)
            {
                throw new InvalidOperationException("Grid Movement validation failed: Hunter crow flock or layered-audio ranges are incorrect.");
            }

            if (!theHunterCrows.RandomizeTriggerRollsEachRun)
            {
                throw new InvalidOperationException("Grid Movement validation failed: Hunter crow trigger rolls must be reseeded for every playthrough.");
            }

            Debug.Log($"Lost Forest TheHunter crow validation passed: {theHunterCrows.BuildDebugSummary()}");
        }

        private static AudioClip LoadFirstAlarmDrumClip()
        {
            AudioClip firstAlarmDrumClip = AssetDatabase.LoadAssetAtPath<AudioClip>(FirstAlarmDrumClipPath);

            if (firstAlarmDrumClip == null)
            {
                throw new InvalidOperationException($"Lost Forest could not load the first Hunter alarm drum at {FirstAlarmDrumClipPath}.");
            }

            return firstAlarmDrumClip;
        }

        private static AudioClip LoadSecondStageHuntDrumClip()
        {
            AudioClip secondStageHuntDrumClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SecondStageHuntDrumClipPath);

            if (secondStageHuntDrumClip == null)
            {
                throw new InvalidOperationException($"Lost Forest could not load the second Hunter drum cue at {SecondStageHuntDrumClipPath}.");
            }

            return secondStageHuntDrumClip;
        }

        private static AudioClip LoadCrowsClip()
        {
            AudioClip crowsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CrowsClipPath);

            if (crowsClip == null)
            {
                throw new InvalidOperationException($"Lost Forest could not load the Hunter crow cue at {CrowsClipPath}.");
            }

            return crowsClip;
        }

        private static AudioClip LoadOwlClip()
        {
            AudioClip owlClip = AssetDatabase.LoadAssetAtPath<AudioClip>(OwlClipPath);

            if (owlClip == null)
            {
                throw new InvalidOperationException($"Lost Forest could not load the Owl guidance cue at {OwlClipPath}.");
            }

            return owlClip;
        }

        private static void ValidateVictoryPrototype(RuneManager runeManager, RunVictoryController runVictoryController)
        {
            runVictoryController.ResetVictoryState();

            if (!runVictoryController.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            if (runeManager.IsRunComplete || runeManager.DepositedRuneCount != 0 || runVictoryController.IsVictory)
            {
                throw new InvalidOperationException("Grid Movement validation failed: victory state was already active before any rune stones were returned.");
            }

            for (int i = 0; i < runeManager.NeededRuneCount; i++)
            {
                char runeLetter = runeManager.GetNeededRuneAt(i);

                if (!runeManager.DepositNeededRuneForValidation(runeLetter))
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: could not return rune stone {runeLetter} during victory validation.");
                }

                runVictoryController.EvaluateRunCompletion();

                bool shouldBeVictory = i == runeManager.NeededRuneCount - 1;

                if (runVictoryController.IsVictory != shouldBeVictory)
                {
                    throw new InvalidOperationException($"Grid Movement validation failed: victory state after deposit {i + 1}/{runeManager.NeededRuneCount} was {runVictoryController.IsVictory}, expected {shouldBeVictory}.");
                }
            }

            if (!runeManager.IsRunComplete || runeManager.DepositedRuneCount != 3 || !runVictoryController.IsVictory)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: returning all three rune stones did not complete the run. Deposited={runeManager.DepositedRuneCount}, Complete={runeManager.IsRunComplete}, Victory={runVictoryController.IsVictory}.");
            }

            if (runVictoryController.PlayAgainYesKey != KeyCode.Y || runVictoryController.PlayAgainNoKey != KeyCode.N)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: victory replay prompt must use Y/N, got {runVictoryController.PlayAgainYesKey}/{runVictoryController.PlayAgainNoKey}.");
            }

            if (runVictoryController.OpeningSceneName != "MainPlayScreenLoop")
            {
                throw new InvalidOperationException($"Grid Movement validation failed: victory replay must return to MainPlayScreenLoop, got '{runVictoryController.OpeningSceneName}'.");
            }

            Debug.Log($"Lost Forest Run Victory validation passed: {runVictoryController.BuildDebugSummary()}");
            runVictoryController.ResetVictoryState();
        }

        private static void ValidatePrototypeLighting(PrototypeLightingDirector lightingDirector)
        {
            if (!lightingDirector.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            lightingDirector.ResetToOvercastAndScheduleNextWindow();

            if (lightingDirector.CurrentState != PrototypeLightingDirector.LightingState.Overcast || lightingDirector.CurrentSunPercent > 0.01f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: lighting should start fully overcast at 0%, got {lightingDirector.CurrentState} {lightingDirector.CurrentSunPercent:0.00}%.");
            }

            if (lightingDirector.SecondsUntilNextWindow < lightingDirector.IntervalRangeSeconds.x || lightingDirector.SecondsUntilNextWindow > lightingDirector.IntervalRangeSeconds.y)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: first lighting interval was outside range, got {lightingDirector.SecondsUntilNextWindow:0.00}s.");
            }

            lightingDirector.ForceImmediateCloudThinningWindow();

            if (lightingDirector.CurrentState != PrototypeLightingDirector.LightingState.CloudsThinning)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: forced cloud thinning did not enter the CloudsThinning state, got {lightingDirector.CurrentState}.");
            }

            if (lightingDirector.TargetSunPercent < lightingDirector.MinimumSunPercent || lightingDirector.TargetSunPercent > lightingDirector.MaximumSunPercent)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: cloud-thinning target was outside allowed sun range, got {lightingDirector.TargetSunPercent:0.00}%.");
            }

            if (lightingDirector.ActiveWindowSecondsRemaining < lightingDirector.WindowDurationRangeSeconds.x || lightingDirector.ActiveWindowSecondsRemaining > lightingDirector.WindowDurationRangeSeconds.y)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: forced lighting window duration was outside range, got {lightingDirector.ActiveWindowSecondsRemaining:0.00}s.");
            }

            for (int i = 0; i < 12; i++)
            {
                lightingDirector.TickForValidation(1f);
            }

            if (lightingDirector.CurrentSunPercent <= 0.01f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: forced cloud-thinning window did not raise sun strength above 0%.");
            }

            lightingDirector.ForceReturnToOvercast();

            for (int i = 0; i < 30; i++)
            {
                lightingDirector.TickForValidation(1f);

                if (lightingDirector.CurrentState == PrototypeLightingDirector.LightingState.Overcast && lightingDirector.CurrentSunPercent <= 0.01f)
                {
                    break;
                }
            }

            if (lightingDirector.CurrentSunPercent > 0.01f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: lighting director could not return fully to 0%, got {lightingDirector.CurrentSunPercent:0.00}%.");
            }

            Debug.Log($"Lost Forest Light and Shadow validation passed: {lightingDirector.BuildDebugSummary()}");
        }

        private static void ValidatePrototypeFog(PrototypeFogDirector fogDirector)
        {
            if (!fogDirector.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            fogDirector.ResetToNormalAndScheduleNextWhiteout();

            if (fogDirector.CurrentState != PrototypeFogDirector.FogCycleState.Normal || fogDirector.CurrentWhiteoutIntensity > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: fog should start normal, got {fogDirector.CurrentState} at {fogDirector.CurrentWhiteoutIntensity * 100f:0.0}% whiteout.");
            }

            if (fogDirector.SecondsUntilNextWhiteout < fogDirector.WhiteoutIntervalRangeSeconds.x || fogDirector.SecondsUntilNextWhiteout > fogDirector.WhiteoutIntervalRangeSeconds.y)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: first whiteout interval was outside 360-720 seconds, got {fogDirector.SecondsUntilNextWhiteout:0.00}s.");
            }

            float initialNormalEndDistance = fogDirector.CurrentNormalFogEndDistanceMeters;
            fogDirector.TickForValidation(5f);

            if (fogDirector.CurrentNormalFogEndDistanceMeters < fogDirector.NormalFogEndDistanceRangeMeters.x || fogDirector.CurrentNormalFogEndDistanceMeters > fogDirector.NormalFogEndDistanceRangeMeters.y)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: normal fog wandered outside 50-70 meters, got {fogDirector.CurrentNormalFogEndDistanceMeters:0.00}m.");
            }

            if (Mathf.Abs(fogDirector.CurrentNormalFogEndDistanceMeters - initialNormalEndDistance) <= 0.01f)
            {
                throw new InvalidOperationException("Grid Movement validation failed: normal fog did not begin its slow 50-70 meter waver.");
            }

            fogDirector.ForceImmediateWhiteout();
            fogDirector.TickForValidation(fogDirector.WhiteoutFadeInSeconds + 0.1f);

            if (fogDirector.CurrentState != PrototypeFogDirector.FogCycleState.Whiteout || fogDirector.CurrentWhiteoutIntensity < 0.999f || fogDirector.CurrentAppliedFogEndDistanceMeters > 1f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: forced fog did not reach zero-visibility whiteout. State={fogDirector.CurrentState}, Intensity={fogDirector.CurrentWhiteoutIntensity:0.000}, End={fogDirector.CurrentAppliedFogEndDistanceMeters:0.00}m.");
            }

            if (fogDirector.WhiteoutHoldSecondsRemaining < fogDirector.WhiteoutHoldRangeSeconds.x || fogDirector.WhiteoutHoldSecondsRemaining > fogDirector.WhiteoutHoldRangeSeconds.y)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: whiteout hold was outside 15-30 seconds, got {fogDirector.WhiteoutHoldSecondsRemaining:0.00}s.");
            }

            if (fogDirector.WhiteoutGlimpseCount < 2 || fogDirector.WhiteoutGlimpseCount > 3)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: whiteout must schedule two or three wavering views, got {fogDirector.WhiteoutGlimpseCount}.");
            }

            int expectedGlimpseCount = fogDirector.WhiteoutGlimpseCount;
            int observedGlimpseCount = 0;
            bool insideGlimpse = false;
            float strongestVisibilityReturn = 0f;
            int safetyTicks = 0;

            while (fogDirector.CurrentState == PrototypeFogDirector.FogCycleState.Whiteout && safetyTicks < 700)
            {
                fogDirector.TickForValidation(0.1f);
                safetyTicks++;

                if (fogDirector.CurrentState != PrototypeFogDirector.FogCycleState.Whiteout)
                {
                    continue;
                }

                float glimpseVisibility = fogDirector.CurrentWhiteoutGlimpseVisibility;
                strongestVisibilityReturn = Mathf.Max(strongestVisibilityReturn, glimpseVisibility);
                bool glimpseVisible = glimpseVisibility > 0.005f;

                if (glimpseVisible && !insideGlimpse)
                {
                    observedGlimpseCount++;
                }

                insideGlimpse = glimpseVisible;
            }

            if (observedGlimpseCount != expectedGlimpseCount || strongestVisibilityReturn < 0.19f || strongestVisibilityReturn > 0.21f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: whiteout wavering views were incorrect. Scheduled={expectedGlimpseCount}, Observed={observedGlimpseCount}, StrongestVisibility={strongestVisibilityReturn * 100f:0.0}%.");
            }

            if (fogDirector.CurrentState != PrototypeFogDirector.FogCycleState.Clearing || fogDirector.CurrentWhiteoutIntensity < 0.999f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: whiteout did not return to full white after its wavering views. State={fogDirector.CurrentState}, Intensity={fogDirector.CurrentWhiteoutIntensity:0.000}.");
            }

            fogDirector.ForceReturnToNormal();
            fogDirector.TickForValidation(16f);

            if (fogDirector.CurrentState != PrototypeFogDirector.FogCycleState.Normal || fogDirector.CurrentWhiteoutIntensity > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: fog could not clear back to normal, got {fogDirector.CurrentState} at {fogDirector.CurrentWhiteoutIntensity * 100f:0.0}%.");
            }

            Debug.Log($"Lost Forest Fog validation passed: WaveringViews={observedGlimpseCount}/{expectedGlimpseCount}, PeakVisibility={strongestVisibilityReturn * 100f:0.0}%. {fogDirector.BuildDebugSummary()}");
            fogDirector.ResetToNormalAndScheduleNextWhiteout();
        }

        private static void ValidatePrototypeFogCeiling(PrototypeFogCeilingDirector fogCeilingDirector)
        {
            if (!fogCeilingDirector.ValidateConfiguration(out string failureReason))
            {
                throw new InvalidOperationException($"Grid Movement validation failed: {failureReason}");
            }

            fogCeilingDirector.TickForValidation(5f);

            if (fogCeilingDirector.CurrentCeilingHeightAbovePlayerMeters < fogCeilingDirector.MinimumCeilingHeightMeters ||
                fogCeilingDirector.CurrentCeilingHeightAbovePlayerMeters > fogCeilingDirector.MaximumCeilingHeightMeters ||
                fogCeilingDirector.CurrentVisibilityFraction < fogCeilingDirector.ThickestVisibilityFraction ||
                fogCeilingDirector.CurrentVisibilityFraction > fogCeilingDirector.ThinnestVisibilityFraction)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: fog-ceiling sample escaped its canopy or visibility range. {fogCeilingDirector.BuildDebugSummary()}");
            }

            Debug.Log($"Lost Forest fog-ceiling validation passed: {fogCeilingDirector.BuildDebugSummary()}");
        }

        private static void ValidateTerrainExtremesAndHiddenBoundary(ActiveRegionRenderer activeRegionRenderer)
        {
            if (activeRegionRenderer.ExtremeHeightSpotFraction < 0.2f || activeRegionRenderer.ExtremeHeightSpotFraction > 0.25f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: extreme terrain selection must remain between 20-25%, got {activeRegionRenderer.ExtremeHeightSpotFraction * 100f:0.0}%.");
            }

            if (Mathf.Abs(activeRegionRenderer.ExtremeHeightMultiplier - 1.3f) > 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: selected terrain extremes must use a 30% multiplier, got x{activeRegionRenderer.ExtremeHeightMultiplier:0.000}.");
            }

            if (!activeRegionRenderer.BoundaryVisualsMatchPlayableForest)
            {
                throw new InvalidOperationException("Grid Movement validation failed: out-of-bounds terrain or tree colors still differ from the playable forest.");
            }

            TerrainFrameSettings baselineSettings = new TerrainFrameSettings(
                100f,
                4242,
                42f,
                1.35f,
                0.0034f,
                0.0022f,
                Vector3.zero,
                0f,
                1f,
                2.4f);
            TerrainFrameSettings boostedSettings = new TerrainFrameSettings(
                100f,
                4242,
                42f,
                1.35f,
                0.0034f,
                0.0022f,
                Vector3.zero,
                activeRegionRenderer.ExtremeHeightSpotFraction,
                activeRegionRenderer.ExtremeHeightMultiplier,
                2.4f);
            int eligibleHighOrLowSamples = 0;
            int boostedSamples = 0;
            bool boostedHighFound = false;
            bool boostedLowFound = false;
            float strongestMultiplier = 1f;

            for (int z = -1800; z <= 1800; z += 60)
            {
                for (int x = -1800; x <= 1800; x += 60)
                {
                    Vector3 position = new Vector3(x, 0f, z);
                    float baselineHeight = TerrainFrameGenerator.GetLogicalHeightAtWorldPosition(position, baselineSettings);

                    if (Mathf.Abs(baselineHeight) < 5f)
                    {
                        continue;
                    }

                    eligibleHighOrLowSamples++;
                    float boostedHeight = TerrainFrameGenerator.GetLogicalHeightAtWorldPosition(position, boostedSettings);
                    float multiplier = Mathf.Abs(boostedHeight) / Mathf.Max(0.001f, Mathf.Abs(baselineHeight));
                    strongestMultiplier = Mathf.Max(strongestMultiplier, multiplier);

                    if (multiplier <= 1.005f)
                    {
                        continue;
                    }

                    boostedSamples++;
                    boostedHighFound |= baselineHeight > 0f;
                    boostedLowFound |= baselineHeight < 0f;
                }
            }

            float boostedFraction = eligibleHighOrLowSamples <= 0 ? 0f : boostedSamples / (float)eligibleHighOrLowSamples;

            if (boostedFraction < 0.16f || boostedFraction > 0.30f || !boostedHighFound || !boostedLowFound)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: selective terrain boost did not affect the expected minority of highs and lows. Boosted={boostedFraction * 100f:0.0}%, High={boostedHighFound}, Low={boostedLowFound}.");
            }

            if (strongestMultiplier > activeRegionRenderer.ExtremeHeightMultiplier + 0.001f)
            {
                throw new InvalidOperationException($"Grid Movement validation failed: terrain boost exceeded 30%, strongest x{strongestMultiplier:0.000}.");
            }

            Debug.Log($"Lost Forest terrain and hidden-boundary validation passed: BoostedHighLowSamples={boostedFraction * 100f:0.0}%, Strongest=x{strongestMultiplier:0.00}, BoundaryColorsMatch={activeRegionRenderer.BoundaryVisualsMatchPlayableForest}.");
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
