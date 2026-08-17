using System;
using System.Collections.Generic;
using LostForest.Phase2.Core;
using LostForest.Phase2.Player;
using LostForest.Phase2.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostForest.Phase2.Pursuer
{
    /// <summary>
    /// An infrequent directional warning: a flock begins in the neighbouring
    /// hex toward The Hunter, crosses the player, and exits through the
    /// opposite neighbouring hex. It receives only genuine Hunter advances,
    /// never hidden-slot setup or rune-retreat teleports.
    /// </summary>
    public sealed class TheHunterCrowCueDirector : MonoBehaviour
    {
        private const string PresentationRootName = "The Hunter Crow Flight Presentation";
        private const string CrowMeshName = "Prototype Black Triangle Crow Mesh";

        [Header("Sources")]
        [SerializeField] private TheHunterPursuerController theHunter;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private RunVictoryController runVictoryController;
        [SerializeField] private AudioClip crowsClip;

        [Header("Trigger")]
        [SerializeField] private bool enableCrowWarnings = true;
        [SerializeField, Range(0.01f, 0.25f)] private float minimumTriggerChance = 0.05f;
        [SerializeField, Range(0.01f, 0.25f)] private float maximumTriggerChance = 0.1f;
        [Tooltip("Creates a fresh sequence of 5-10% crow rolls for every playthrough. Disable only when reproducing a specific test run.")]
        [SerializeField] private bool randomizeTriggerRollsEachRun = true;
        [SerializeField] private int randomSeed = 20260816;

        [Header("Flock Flight")]
        [SerializeField] private Vector2Int flockCountRange = new Vector2Int(10, 20);
        [SerializeField, Min(10f)] private float adjacentHexDistanceMeters = 72f;
        [SerializeField] private Vector2 flightDurationRangeSeconds = new Vector2(4.5f, 6.2f);
        [SerializeField, Min(1f)] private float flightAltitudeAbovePlayerMeters = 24f;
        [SerializeField, Min(0f)] private float flightVerticalJitterMeters = 6f;
        [SerializeField, Min(0f)] private float flightLateralSpreadMeters = 22f;
        [SerializeField, Min(0f)] private float launchStaggerSeconds = 1.35f;
        [SerializeField] private Vector2 crowTriangleSizeRange = new Vector2(0.7f, 1.35f);
        [SerializeField] private Color crowColor = Color.black;

        [Header("Layered Crow Audio")]
        [SerializeField] private Vector2Int audioLayerCountRange = new Vector2Int(5, 6);
        [SerializeField, Range(0f, 1f)] private float maximumLayerVolume = 0.27f;
        [Tooltip("Each layer starts later than the previous one, so individual calls remain readable.")]
        [SerializeField] private Vector2 audioLayerStartStaggerSeconds = new Vector2(0.1f, 3.1f);
        [Tooltip("Each layer has a separately staggered fade-out / stop time measured from flock launch.")]
        [SerializeField] private Vector2 audioLayerEndTimeRangeSeconds = new Vector2(3.2f, 6.7f);
        [SerializeField] private Vector2 audioPulseRateRange = new Vector2(0.35f, 0.9f);
        [SerializeField, Min(1f)] private float audioMaxDistanceMeters = 115f;
        [SerializeField] private Vector2 echoDelayRangeMilliseconds = new Vector2(150f, 280f);
        [SerializeField, Range(0f, 1f)] private float echoDecayRatio = 0.26f;
        [SerializeField, Range(0f, 1f)] private float echoWetMix = 0.24f;

        [Header("Development Debug")]
        [SerializeField] private bool enableDevelopmentHotkey = true;
        [Tooltip("Development-only preview shortcut. Press C while the Game view has focus.")]
        [SerializeField] private KeyCode forceCrowFlightKey = KeyCode.C;
        [Tooltip("Logs each Hunter roll, crow-flight scheduling decision, and actual audio-layer start for playtest diagnosis.")]
        [SerializeField] private bool logTriggeredFlights = true;

        private readonly List<CrowAgent> activeCrows = new List<CrowAgent>();
        private readonly List<CrowAudioLayer> activeAudioLayers = new List<CrowAudioLayer>();
        private System.Random random;
        private Transform presentationRoot;
        private Material crowMaterial;
        private Mesh crowMesh;
        private int activeRandomSeed;
        private bool subscribed;
        private bool flightActive;
        private float flightElapsedSeconds;
        private float flightLifetimeSeconds;
        private Vector3 flightStart;
        private Vector3 flightEnd;

        public float MinimumTriggerChance => Mathf.Clamp(minimumTriggerChance, 0.01f, 0.25f);
        public float MaximumTriggerChance => Mathf.Clamp(maximumTriggerChance, MinimumTriggerChance, 0.25f);
        public Vector2 TriggerChanceRange => new Vector2(MinimumTriggerChance, MaximumTriggerChance);
        public Vector2Int FlockCountRange => new Vector2Int(Mathf.Min(flockCountRange.x, flockCountRange.y), Mathf.Max(flockCountRange.x, flockCountRange.y));
        public Vector2Int AudioLayerCountRange => new Vector2Int(Mathf.Min(audioLayerCountRange.x, audioLayerCountRange.y), Mathf.Max(audioLayerCountRange.x, audioLayerCountRange.y));
        public bool HasCrowsClip => crowsClip != null;
        public bool IsFlightActive => flightActive;
        public int CurrentFlockCount => activeCrows.Count;
        public int CurrentAudioLayerCount => activeAudioLayers.Count;
        public int TriggeredFlightCount { get; private set; }
        public int StartedAudioLayerCount { get; private set; }
        public float LastRolledTriggerChance { get; private set; }
        public bool RandomizeTriggerRollsEachRun => randomizeTriggerRollsEachRun;

        public void SetSources(
            TheHunterPursuerController newTheHunter,
            Transform newPlayer,
            PlayerCondition newPlayerCondition,
            RunVictoryController newRunVictoryController)
        {
            Unsubscribe();
            theHunter = newTheHunter;
            player = newPlayer;
            playerCondition = newPlayerCondition;
            runVictoryController = newRunVictoryController;
            Subscribe();
        }

        public void SetCrowsClip(AudioClip newCrowsClip)
        {
            crowsClip = newCrowsClip;
        }

        public void ApplyPrototypeDefaults()
        {
            enableCrowWarnings = true;
            minimumTriggerChance = 0.05f;
            maximumTriggerChance = 0.1f;
            randomizeTriggerRollsEachRun = true;
            randomSeed = 20260816;
            flockCountRange = new Vector2Int(10, 20);
            adjacentHexDistanceMeters = 72f;
            flightDurationRangeSeconds = new Vector2(4.5f, 6.2f);
            flightAltitudeAbovePlayerMeters = 24f;
            flightVerticalJitterMeters = 6f;
            flightLateralSpreadMeters = 22f;
            launchStaggerSeconds = 1.35f;
            crowTriangleSizeRange = new Vector2(0.7f, 1.35f);
            crowColor = Color.black;
            audioLayerCountRange = new Vector2Int(5, 6);
            maximumLayerVolume = 0.27f;
            audioLayerStartStaggerSeconds = new Vector2(0.1f, 3.1f);
            audioLayerEndTimeRangeSeconds = new Vector2(3.2f, 6.7f);
            audioPulseRateRange = new Vector2(0.35f, 0.9f);
            audioMaxDistanceMeters = 115f;
            echoDelayRangeMilliseconds = new Vector2(150f, 280f);
            echoDecayRatio = 0.26f;
            echoWetMix = 0.24f;
            enableDevelopmentHotkey = true;
            forceCrowFlightKey = KeyCode.C;
            logTriggeredFlights = true;
            random = null;
            activeRandomSeed = 0;

            if (Application.isPlaying)
            {
                ResetRandomForRun();
            }
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (Mathf.Abs(MinimumTriggerChance - 0.05f) > 0.001f || Mathf.Abs(MaximumTriggerChance - 0.1f) > 0.001f)
            {
                failureReason = $"Crow-warning chance must vary from 5%-10%, got {MinimumTriggerChance * 100f:0.0}-{MaximumTriggerChance * 100f:0.0}%.";
                return false;
            }

            if (FlockCountRange.x != 10 || FlockCountRange.y != 20)
            {
                failureReason = $"Crow flocks must contain 10-20 crows, got {FlockCountRange.x}-{FlockCountRange.y}.";
                return false;
            }

            if (AudioLayerCountRange.x != 5 || AudioLayerCountRange.y != 6)
            {
                failureReason = $"Crow warnings must layer 5-6 audio tracks, got {AudioLayerCountRange.x}-{AudioLayerCountRange.y}.";
                return false;
            }

            if (crowsClip == null)
            {
                failureReason = "Crow-warning audio is missing Crows1.mp3.";
                return false;
            }

            if (adjacentHexDistanceMeters < 10f || flightAltitudeAbovePlayerMeters <= 0f || !IsValidRange(flightDurationRangeSeconds, 0.1f) || !IsValidRange(audioLayerStartStaggerSeconds, 0f) || !IsValidRange(audioLayerEndTimeRangeSeconds, 0.1f) || !IsValidRange(audioPulseRateRange, 0.01f) || !IsValidRange(echoDelayRangeMilliseconds, 1f))
            {
                failureReason = "Crow flight or audio settings are invalid.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void ForceCrowFlightForDebug()
        {
            if (theHunter == null || theHunter.CurrentHiddenSlot == null)
            {
                return;
            }

            BeginCrowFlight(theHunter.CurrentHiddenSlot);
        }

        public string BuildDebugSummary()
        {
            string state = flightActive ? "Flying" : "Idle";
            return $"Hunter crows {state} Flock={CurrentFlockCount} Audio={CurrentAudioLayerCount} Started={StartedAudioLayerCount} Triggered={TriggeredFlightCount} LastChance={LastRolledTriggerChance * 100f:0.0}% Seed={activeRandomSeed}";
        }

        private void Awake()
        {
            ResetRandomForRun();
        }

        private void OnEnable()
        {
            DiscoverSources();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopActiveFlight();
        }

        private void OnDestroy()
        {
            DestroyUnityObject(crowMaterial);
            DestroyUnityObject(crowMesh);
        }

        private void OnValidate()
        {
            minimumTriggerChance = Mathf.Clamp(minimumTriggerChance, 0.01f, 0.25f);
            maximumTriggerChance = Mathf.Clamp(maximumTriggerChance, minimumTriggerChance, 0.25f);
            flockCountRange = NormalizeCountRange(flockCountRange, 10, 20);
            audioLayerCountRange = NormalizeCountRange(audioLayerCountRange, 5, 6);
            adjacentHexDistanceMeters = Mathf.Max(10f, adjacentHexDistanceMeters);
            flightDurationRangeSeconds = NormalizeRange(flightDurationRangeSeconds, 0.1f);
            flightAltitudeAbovePlayerMeters = Mathf.Max(0.1f, flightAltitudeAbovePlayerMeters);
            flightVerticalJitterMeters = Mathf.Max(0f, flightVerticalJitterMeters);
            flightLateralSpreadMeters = Mathf.Max(0f, flightLateralSpreadMeters);
            launchStaggerSeconds = Mathf.Max(0f, launchStaggerSeconds);
            crowTriangleSizeRange = NormalizeRange(crowTriangleSizeRange, 0.05f);
            audioLayerStartStaggerSeconds = NormalizeRange(audioLayerStartStaggerSeconds, 0f);
            audioLayerEndTimeRangeSeconds = NormalizeRange(audioLayerEndTimeRangeSeconds, 0.1f);
            audioPulseRateRange = NormalizeRange(audioPulseRateRange, 0.01f);
            audioMaxDistanceMeters = Mathf.Max(1f, audioMaxDistanceMeters);
            echoDelayRangeMilliseconds = NormalizeRange(echoDelayRangeMilliseconds, 1f);
            echoDecayRatio = Mathf.Clamp01(echoDecayRatio);
            echoWetMix = Mathf.Clamp01(echoWetMix);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DiscoverSources();
            Subscribe();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enableDevelopmentHotkey && Input.GetKeyDown(forceCrowFlightKey))
            {
                ForceCrowFlightForDebug();
            }
#endif

            if (IsRunTerminal())
            {
                StopActiveFlight();
                return;
            }

            TickActiveFlight(Time.deltaTime);
        }

        private void DiscoverSources()
        {
            if (theHunter == null)
            {
                theHunter = FindAnyObjectByType<TheHunterPursuerController>();
            }

            if (player == null)
            {
                PlayerGridAddressTracker tracker = FindAnyObjectByType<PlayerGridAddressTracker>();
                player = tracker == null ? null : tracker.transform;
            }

            if (playerCondition == null)
            {
                playerCondition = FindAnyObjectByType<PlayerCondition>();
            }

            if (runVictoryController == null)
            {
                runVictoryController = FindAnyObjectByType<RunVictoryController>();
            }
        }

        private void Subscribe()
        {
            if (subscribed || theHunter == null)
            {
                return;
            }

            theHunter.AdvancedToHiddenSlot += HandleHunterAdvanced;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || theHunter == null)
            {
                subscribed = false;
                return;
            }

            theHunter.AdvancedToHiddenSlot -= HandleHunterAdvanced;
            subscribed = false;
        }

        private void HandleHunterAdvanced(FieldSlotData previousSlot, FieldSlotData currentSlot)
        {
            if (!enableCrowWarnings || currentSlot == null || IsRunTerminal())
            {
                return;
            }

            EnsureRandom();
            LastRolledTriggerChance = RollRange(MinimumTriggerChance, MaximumTriggerChance);
            float roll = (float)random.NextDouble();

            if (logTriggeredFlights)
            {
                Debug.Log($"Lost Forest Hunter crows roll: From={previousSlot?.Address ?? "None"}, To={currentSlot.Address}, Roll={roll * 100f:0.0}%, Chance={LastRolledTriggerChance * 100f:0.0}%, Triggered={roll <= LastRolledTriggerChance}", this);
            }

            if (roll > LastRolledTriggerChance)
            {
                return;
            }

            BeginCrowFlight(currentSlot);
        }

        private void BeginCrowFlight(FieldSlotData hunterSlot)
        {
            if (player == null || hunterSlot == null || crowsClip == null || IsRunTerminal())
            {
                if (logTriggeredFlights)
                {
                    Debug.LogWarning($"Lost Forest Hunter crows did not start: Player={(player == null ? "Missing" : "Ready")}, HunterSlot={(hunterSlot == null ? "Missing" : hunterSlot.Address)}, Clip={(crowsClip == null ? "Missing" : crowsClip.name)}, Terminal={IsRunTerminal()}.", this);
                }

                return;
            }

            StopActiveFlight();
            EnsureRandom();
            EnsurePresentationResources();

            Vector3 playerPosition = player.position;
            Vector3 directionTowardHunter = hunterSlot.WorldCenter - playerPosition;
            directionTowardHunter.y = 0f;

            if (directionTowardHunter.sqrMagnitude < 0.001f)
            {
                directionTowardHunter = player.forward;
                directionTowardHunter.y = 0f;
            }

            directionTowardHunter = directionTowardHunter.sqrMagnitude < 0.001f ? Vector3.forward : directionTowardHunter.normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, directionTowardHunter).normalized;
            float altitude = playerPosition.y + flightAltitudeAbovePlayerMeters;
            flightStart = playerPosition + directionTowardHunter * adjacentHexDistanceMeters;
            flightEnd = playerPosition - directionTowardHunter * adjacentHexDistanceMeters;
            flightStart.y = altitude;
            flightEnd.y = altitude;
            flightElapsedSeconds = 0f;
            flightLifetimeSeconds = Mathf.Max(
                flightDurationRangeSeconds.y + launchStaggerSeconds,
                audioLayerEndTimeRangeSeconds.y) + 0.5f;

            int flockCount = RollInt(FlockCountRange.x, FlockCountRange.y + 1);

            for (int i = 0; i < flockCount; i++)
            {
                float startDelay = RollRange(0f, launchStaggerSeconds);
                float duration = RollRange(flightDurationRangeSeconds.x, flightDurationRangeSeconds.y);
                float lateralOffset = RollRange(-flightLateralSpreadMeters, flightLateralSpreadMeters);
                float verticalOffset = RollRange(-flightVerticalJitterMeters, flightVerticalJitterMeters);
                float triangleSize = RollRange(crowTriangleSizeRange.x, crowTriangleSizeRange.y);
                CreateCrow(i, lateral, startDelay, duration, lateralOffset, verticalOffset, triangleSize);
            }

            int layerCount = RollInt(AudioLayerCountRange.x, AudioLayerCountRange.y + 1);

            for (int i = 0; i < layerCount; i++)
            {
                float layer01 = layerCount <= 1 ? 0.5f : i / (float)(layerCount - 1);
                float startDelay = Mathf.Lerp(audioLayerStartStaggerSeconds.x, audioLayerStartStaggerSeconds.y, layer01) + RollRange(-0.12f, 0.12f);
                startDelay = Mathf.Clamp(startDelay, audioLayerStartStaggerSeconds.x, audioLayerStartStaggerSeconds.y);
                float stopTime = Mathf.Lerp(audioLayerEndTimeRangeSeconds.x, audioLayerEndTimeRangeSeconds.y, layer01) + RollRange(-0.18f, 0.18f);
                float duration = Mathf.Max(0.8f, stopTime - startDelay);
                float lateralOffset = RollRange(-flightLateralSpreadMeters * 0.6f, flightLateralSpreadMeters * 0.6f);
                float verticalOffset = RollRange(-flightVerticalJitterMeters * 0.4f, flightVerticalJitterMeters * 0.4f);
                CreateAudioLayer(i, lateral, startDelay, duration, lateralOffset, verticalOffset);
            }

            flightActive = activeCrows.Count > 0 || activeAudioLayers.Count > 0;
            TriggeredFlightCount++;

            if (logTriggeredFlights)
            {
                Debug.Log($"Lost Forest Hunter crows scheduled: Flight={TriggeredFlightCount}, Flock={flockCount}, AudioLayers={layerCount}, Clip={crowsClip.name} ({crowsClip.length:0.00}s), From={hunterSlot.Address}, Direction=({directionTowardHunter.x:0.00},{directionTowardHunter.z:0.00})", this);
            }
        }

        private void CreateCrow(
            int index,
            Vector3 lateralAxis,
            float startDelay,
            float duration,
            float lateralOffset,
            float verticalOffset,
            float triangleSize)
        {
            GameObject crowObject = new GameObject($"Crow Triangle {index + 1:00}");
            crowObject.transform.SetParent(presentationRoot, false);
            MeshFilter meshFilter = crowObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = crowObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = crowMesh;
            meshRenderer.sharedMaterial = crowMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            activeCrows.Add(new CrowAgent
            {
                Transform = crowObject.transform,
                Renderer = meshRenderer,
                StartDelay = startDelay,
                Duration = duration,
                LateralAxis = lateralAxis,
                LateralOffset = lateralOffset,
                VerticalOffset = verticalOffset,
                TriangleSize = triangleSize,
                FlapPhase = RollRange(0f, Mathf.PI * 2f),
                FlapRate = RollRange(5f, 9f)
            });
        }

        private void CreateAudioLayer(
            int index,
            Vector3 lateralAxis,
            float startDelay,
            float duration,
            float lateralOffset,
            float verticalOffset)
        {
            GameObject audioObject = new GameObject($"Crow Flight Audio Layer {index + 1}");
            audioObject.transform.SetParent(presentationRoot, false);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = crowsClip;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.spread = 155f;
            audioSource.dopplerLevel = 0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = audioMaxDistanceMeters;
            audioSource.volume = 0f;

            AudioEchoFilter echo = audioObject.AddComponent<AudioEchoFilter>();
            echo.delay = RollRange(echoDelayRangeMilliseconds.x, echoDelayRangeMilliseconds.y);
            echo.decayRatio = echoDecayRatio;
            echo.dryMix = 1f;
            echo.wetMix = echoWetMix;

            AudioReverbFilter reverb = audioObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.Forest;

            activeAudioLayers.Add(new CrowAudioLayer
            {
                Transform = audioObject.transform,
                AudioSource = audioSource,
                StartDelay = startDelay,
                Duration = duration,
                LateralAxis = lateralAxis,
                LateralOffset = lateralOffset,
                VerticalOffset = verticalOffset,
                MaximumVolume = RollRange(maximumLayerVolume * 0.68f, maximumLayerVolume),
                PulseRate = RollRange(audioPulseRateRange.x, audioPulseRateRange.y),
                PulseOffset = RollRange(0f, 100f)
            });
        }

        private void TickActiveFlight(float deltaSeconds)
        {
            if (!flightActive)
            {
                return;
            }

            flightElapsedSeconds += Mathf.Max(0f, deltaSeconds);
            Camera mainCamera = Camera.main;

            for (int i = activeCrows.Count - 1; i >= 0; i--)
            {
                CrowAgent crow = activeCrows[i];

                if (crow.Transform == null || !UpdateFlightTransform(crow.StartDelay, crow.Duration, crow.LateralAxis, crow.LateralOffset, crow.VerticalOffset, crow.Transform, out float normalizedProgress))
                {
                    DestroyUnityObject(crow.Transform == null ? null : crow.Transform.gameObject);
                    activeCrows.RemoveAt(i);
                    continue;
                }

                float visibility = EvaluateFlightEnvelope(normalizedProgress);
                crow.Renderer.GetPropertyBlock(crow.PropertyBlock);
                crow.PropertyBlock.SetColor("_Color", new Color(crowColor.r, crowColor.g, crowColor.b, visibility));
                crow.Renderer.SetPropertyBlock(crow.PropertyBlock);

                if (mainCamera != null)
                {
                    Vector3 cameraDirection = mainCamera.transform.position - crow.Transform.position;
                    crow.Transform.rotation = cameraDirection.sqrMagnitude < 0.001f
                        ? Quaternion.identity
                        : Quaternion.LookRotation(cameraDirection.normalized, Vector3.up);
                    float flapScale = 1f + Mathf.Sin(flightElapsedSeconds * crow.FlapRate + crow.FlapPhase) * 0.16f;
                    crow.Transform.localScale = new Vector3(crow.TriangleSize * flapScale, crow.TriangleSize / Mathf.Max(0.1f, flapScale), crow.TriangleSize);
                }
            }

            for (int i = activeAudioLayers.Count - 1; i >= 0; i--)
            {
                CrowAudioLayer layer = activeAudioLayers[i];

                if (layer.Transform == null || !UpdateFlightTransform(layer.StartDelay, layer.Duration, layer.LateralAxis, layer.LateralOffset, layer.VerticalOffset, layer.Transform, out float normalizedProgress))
                {
                    DestroyUnityObject(layer.Transform == null ? null : layer.Transform.gameObject);
                    activeAudioLayers.RemoveAt(i);
                    continue;
                }

                if (layer.AudioSource == null)
                {
                    if (!layer.HasStarted)
                    {
                        Debug.LogWarning($"Lost Forest Hunter crows layer {i + 1} could not play: AudioSource is missing.", this);
                        layer.HasStarted = true;
                    }

                    continue;
                }

                if (!layer.HasStarted && flightElapsedSeconds >= layer.StartDelay)
                {
                    if (layer.AudioSource.clip == null)
                    {
                        Debug.LogWarning($"Lost Forest Hunter crows layer {i + 1} could not play: Crows1 clip is missing.", this);
                    }
                    else
                    {
                        layer.AudioSource.Play();
                        StartedAudioLayerCount++;

                        if (logTriggeredFlights)
                        {
                            Debug.Log($"Lost Forest Hunter crows audio started: Flight={TriggeredFlightCount}, Layer={i + 1}, Clip={layer.AudioSource.clip.name}, Start={flightElapsedSeconds:0.00}s, PlannedEnd={layer.StartDelay + layer.Duration:0.00}s, Playing={layer.AudioSource.isPlaying}", this);
                        }
                    }

                    layer.HasStarted = true;
                }

                float envelope = EvaluateFlightEnvelope(normalizedProgress);
                float pulse = Mathf.Lerp(0.28f, 1f, Mathf.PerlinNoise(layer.PulseOffset, flightElapsedSeconds * layer.PulseRate));
                layer.AudioSource.volume = layer.MaximumVolume * envelope * pulse;
            }

            if ((activeCrows.Count == 0 && activeAudioLayers.Count == 0) || flightElapsedSeconds >= flightLifetimeSeconds)
            {
                StopActiveFlight();
            }
        }

        private bool UpdateFlightTransform(
            float startDelay,
            float duration,
            Vector3 lateralAxis,
            float lateralOffset,
            float verticalOffset,
            Transform flightTransform,
            out float normalizedProgress)
        {
            normalizedProgress = (flightElapsedSeconds - startDelay) / Mathf.Max(0.01f, duration);

            if (normalizedProgress < 0f)
            {
                flightTransform.gameObject.SetActive(false);
                return true;
            }

            if (normalizedProgress > 1f)
            {
                return false;
            }

            flightTransform.gameObject.SetActive(true);
            float smoothedProgress = normalizedProgress * normalizedProgress * (3f - 2f * normalizedProgress);
            Vector3 position = Vector3.Lerp(flightStart, flightEnd, smoothedProgress);
            position += lateralAxis * lateralOffset;
            position.y += verticalOffset + Mathf.Sin(normalizedProgress * Mathf.PI) * 1.5f;
            flightTransform.position = position;
            return true;
        }

        private void StopActiveFlight()
        {
            for (int i = 0; i < activeCrows.Count; i++)
            {
                CrowAgent crow = activeCrows[i];
                DestroyUnityObject(crow.Transform == null ? null : crow.Transform.gameObject);
            }

            for (int i = 0; i < activeAudioLayers.Count; i++)
            {
                CrowAudioLayer layer = activeAudioLayers[i];

                if (layer.AudioSource != null)
                {
                    layer.AudioSource.Stop();
                }

                DestroyUnityObject(layer.Transform == null ? null : layer.Transform.gameObject);
            }

            activeCrows.Clear();
            activeAudioLayers.Clear();
            flightActive = false;
            flightElapsedSeconds = 0f;
            flightLifetimeSeconds = 0f;
        }

        private void EnsurePresentationResources()
        {
            if (presentationRoot == null)
            {
                Transform existing = transform.Find(PresentationRootName);
                presentationRoot = existing == null ? new GameObject(PresentationRootName).transform : existing;
                presentationRoot.SetParent(transform, false);
            }

            if (crowMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                shader = shader == null ? Shader.Find("Unlit/Transparent") : shader;
                shader = shader == null ? Shader.Find("Unlit/Color") : shader;
                crowMaterial = new Material(shader)
                {
                    name = "Prototype Black Triangle Crow Material",
                    hideFlags = HideFlags.DontSave,
                    color = crowColor,
                    renderQueue = 3001
                };
            }

            if (crowMesh != null)
            {
                return;
            }

            crowMesh = new Mesh
            {
                name = CrowMeshName,
                hideFlags = HideFlags.DontSave
            };
            crowMesh.vertices = new[]
            {
                new Vector3(-0.65f, -0.38f, 0f),
                new Vector3(0.65f, -0.38f, 0f),
                new Vector3(0f, 0.62f, 0f)
            };
            crowMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            crowMesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
            crowMesh.RecalculateBounds();
        }

        private bool IsRunTerminal()
        {
            return (playerCondition != null && playerCondition.IsGameOver) ||
                   (runVictoryController != null && runVictoryController.IsVictory);
        }

        private void EnsureRandom()
        {
            if (random == null)
            {
                ResetRandomForRun();
            }
        }

        private void ResetRandomForRun()
        {
            int seed = randomSeed;

            if (randomizeTriggerRollsEachRun && Application.isPlaying)
            {
                unchecked
                {
                    seed ^= System.Environment.TickCount;
                    long utcTicks = System.DateTime.UtcNow.Ticks;
                    seed ^= (int)utcTicks;
                    seed ^= (int)(utcTicks >> 32);
                }
            }

            activeRandomSeed = seed;
            random = new System.Random(activeRandomSeed);
        }

        private int RollInt(int minimumInclusive, int maximumExclusive)
        {
            return random.Next(minimumInclusive, Math.Max(minimumInclusive + 1, maximumExclusive));
        }

        private float RollRange(float minimum, float maximum)
        {
            return Mathf.Lerp(Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum), (float)random.NextDouble());
        }

        private static float EvaluateFlightEnvelope(float normalizedProgress)
        {
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedProgress / 0.18f));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalizedProgress - 0.68f) / 0.32f));
            return fadeIn * fadeOut;
        }

        private static bool IsValidRange(Vector2 range, float minimum)
        {
            return range.x >= minimum && range.y >= range.x;
        }

        private static Vector2 NormalizeRange(Vector2 range, float minimum)
        {
            float x = Mathf.Max(minimum, range.x);
            float y = Mathf.Max(minimum, range.y);
            return x <= y ? new Vector2(x, y) : new Vector2(y, x);
        }

        private static Vector2Int NormalizeCountRange(Vector2Int range, int minimum, int maximum)
        {
            int x = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
            int y = Mathf.Clamp(Mathf.Max(range.x, range.y), x, maximum);
            return new Vector2Int(x, y);
        }

        private static void DestroyUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unityObject);
            }
            else
            {
                DestroyImmediate(unityObject);
            }
        }

        private sealed class CrowAgent
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
            public float StartDelay;
            public float Duration;
            public Vector3 LateralAxis;
            public float LateralOffset;
            public float VerticalOffset;
            public float TriangleSize;
            public float FlapPhase;
            public float FlapRate;
        }

        private sealed class CrowAudioLayer
        {
            public Transform Transform;
            public AudioSource AudioSource;
            public float StartDelay;
            public float Duration;
            public Vector3 LateralAxis;
            public float LateralOffset;
            public float VerticalOffset;
            public float MaximumVolume;
            public float PulseRate;
            public float PulseOffset;
            public bool HasStarted;
        }
    }
}
