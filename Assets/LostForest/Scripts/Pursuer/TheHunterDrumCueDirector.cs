using LostForest.Phase2.Core;
using LostForest.Phase2.Player;
using UnityEngine;

namespace LostForest.Phase2.Pursuer
{
    public enum TheHunterDrumProximity
    {
        None,
        ThreeHexes,
        TwoHexes,
        Adjacent
    }

    /// <summary>
    /// Player-facing sound language for The Hunter. It deliberately consumes
    /// only proximity bands, never displaying the underlying distance, state,
    /// or hidden slot data.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AudioLowPassFilter))]
    public sealed class TheHunterDrumCueDirector : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private TheHunterPursuerController theHunter;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private RunVictoryController runVictoryController;
        [SerializeField] private AudioSource drumAudioSource;
        [SerializeField] private AudioSource huntDrumAudioSource;
        [SerializeField] private AudioLowPassFilter drumLowPassFilter;

        [Header("Optional Final Audio")]
        [Tooltip("The alarm hit at three hexes, repeated twice at two hexes, then played every 1.5 seconds while adjacent.")]
        [SerializeField] private AudioClip firstAlarmDrumClip;
        [SerializeField] private AudioClip loudTribalDrumClip;
        [Tooltip("The second cue: a random fade pulse at two hexes and a continuously fading loop while adjacent.")]
        [SerializeField] private AudioClip distantHuntDrumsClip;
        [SerializeField] private bool synthesizePrototypeDrumsWhenClipsAreMissing = true;

        [Header("Alarm Mix")]
        [SerializeField, Range(0f, 1f)] private float loudDrumVolume = 1f;
        [SerializeField, Range(1f, 3f)] private float loudAlarmGain = 2.1f;
        [SerializeField, Range(0f, 1f)] private float twoHexHuntVolume = 0.38f;
        [SerializeField, Range(0f, 1f)] private float adjacentHuntVolume = 0.72f;
        [SerializeField] private float doubleHitSpacingSeconds = 0.62f;
        [SerializeField] private float adjacentLoudHitSpacingSeconds = 0.72f;
        [SerializeField] private Vector2 twoHexHuntIntervalSeconds = new Vector2(5f, 8f);
        [SerializeField] private Vector2 twoHexHuntFadeInSeconds = new Vector2(0.85f, 1.45f);
        [SerializeField] private Vector2 twoHexHuntHoldSeconds = new Vector2(1f, 2f);
        [SerializeField] private Vector2 twoHexHuntFadeOutSeconds = new Vector2(1.1f, 1.9f);
        [SerializeField] private Vector2 adjacentHuntFadeHalfCycleSeconds = new Vector2(1.35f, 2.45f);
        [SerializeField, Range(80f, 1200f)] private float deepDrumLowPassCutoffHz = 360f;

        [Header("Hunt Cue Direction")]
        [SerializeField, Min(1f)] private float huntCueSpatialDistanceMeters = 12f;
        [SerializeField] private Vector2 adjacentHuntRepositionIntervalSeconds = new Vector2(2.5f, 5f);
        [SerializeField, Range(1f, 180f)] private float adjacentHuntOrbitMaxDegrees = 135f;
        [SerializeField, Range(1f, 360f)] private float adjacentHuntOrbitTurnDegreesPerSecond = 120f;

        [Header("Debug")]
        [SerializeField] private bool logProximityChanges;
        [Tooltip("Logs actual drum playback requests and missing-audio conditions during playtests.")]
        [SerializeField] private bool logAudioPlayback = true;

        private System.Random random;
        private TheHunterDrumProximity currentProximity;
        private int queuedLoudHits;
        private float nextLoudHitSeconds;
        private float nextHuntSeconds;
        private HuntCuePlaybackMode huntCuePlaybackMode;
        private HuntCueEnvelopePhase huntCueEnvelopePhase;
        private float huntCuePhaseDurationSeconds;
        private float huntCuePhaseSecondsRemaining;
        private float huntCuePhaseStartVolume;
        private float huntCuePhaseTargetVolume;
        private AudioClip generatedLoudDrum;
        private AudioClip generatedDistantHunt;
        private Transform audioListenerTransform;
        private float adjacentHuntOrbitAngleDegrees;
        private float adjacentHuntTargetOrbitAngleDegrees;
        private float secondsUntilAdjacentHuntReposition;
        private int loudDrumPlaybackCount;
        private int huntCuePlaybackCount;

        private const string HuntDrumAudioObjectName = "TheHunter Hunt Drum Spatial Audio";

        private enum HuntCuePlaybackMode
        {
            None,
            TwoHexPulse,
            AdjacentLoop
        }

        private enum HuntCueEnvelopePhase
        {
            None,
            FadeIn,
            Hold,
            FadeOut
        }

        public TheHunterDrumProximity CurrentProximity => currentProximity;
        public int CurrentDistanceSlots => theHunter == null ? -1 : theHunter.ExactPlayerDistanceSlots;
        public bool HasFirstAlarmDrumClip => firstAlarmDrumClip != null;
        public bool HasSecondStageHuntDrumClip => distantHuntDrumsClip != null;
        public float AdjacentAlarmIntervalSeconds => adjacentLoudHitSpacingSeconds;

        /// <summary>
        /// Assigns the drum hit used for the three-hex first warning, both
        /// hits at the two-hex second stage, and the repeating adjacent alarm.
        /// </summary>
        public void SetFirstAlarmDrumClip(AudioClip newFirstAlarmDrumClip)
        {
            firstAlarmDrumClip = newFirstAlarmDrumClip;
        }

        /// <summary>
        /// Assigns the second warning cue, which fades in and out randomly at
        /// two hexes and loops through continuous fades while adjacent.
        /// </summary>
        public void SetSecondStageHuntDrumClip(AudioClip newSecondStageHuntDrumClip)
        {
            distantHuntDrumsClip = newSecondStageHuntDrumClip;
        }

        public void SetSources(
            TheHunterPursuerController newTheHunter,
            PlayerCondition newPlayerCondition,
            RunVictoryController newRunVictoryController,
            AudioSource newDrumAudioSource = null)
        {
            theHunter = newTheHunter;
            playerCondition = newPlayerCondition;
            runVictoryController = newRunVictoryController;

            if (newDrumAudioSource != null)
            {
                drumAudioSource = newDrumAudioSource;
            }

            ConfigureAudioSource();
        }

        public void ApplyTheHunterPrototypeDefaults()
        {
            synthesizePrototypeDrumsWhenClipsAreMissing = true;
            loudDrumVolume = 1f;
            loudAlarmGain = 2.1f;
            twoHexHuntVolume = 0.38f;
            adjacentHuntVolume = 0.72f;
            doubleHitSpacingSeconds = 0.62f;
            adjacentLoudHitSpacingSeconds = 1.5f;
            twoHexHuntIntervalSeconds = new Vector2(5f, 8f);
            twoHexHuntFadeInSeconds = new Vector2(0.85f, 1.45f);
            twoHexHuntHoldSeconds = new Vector2(1f, 2f);
            twoHexHuntFadeOutSeconds = new Vector2(1.1f, 1.9f);
            adjacentHuntFadeHalfCycleSeconds = new Vector2(1.35f, 2.45f);
            huntCueSpatialDistanceMeters = 12f;
            adjacentHuntRepositionIntervalSeconds = new Vector2(2.5f, 5f);
            adjacentHuntOrbitMaxDegrees = 135f;
            adjacentHuntOrbitTurnDegreesPerSecond = 120f;
            deepDrumLowPassCutoffHz = 360f;
            logProximityChanges = true;
            logAudioPlayback = true;
            ConfigureAudioSource();
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (theHunter == null)
            {
                failureReason = "TheHunter drum cue director has no Hunter source.";
                return false;
            }

            if (doubleHitSpacingSeconds <= 0f ||
                adjacentLoudHitSpacingSeconds <= 0f ||
                !IsValidInterval(twoHexHuntIntervalSeconds) ||
                !IsValidInterval(twoHexHuntFadeInSeconds) ||
                !IsValidInterval(twoHexHuntHoldSeconds) ||
                !IsValidInterval(twoHexHuntFadeOutSeconds) ||
                !IsValidInterval(adjacentHuntFadeHalfCycleSeconds) ||
                !IsValidInterval(adjacentHuntRepositionIntervalSeconds) ||
                huntCueSpatialDistanceMeters < 1f ||
                adjacentHuntOrbitMaxDegrees <= 0f ||
                adjacentHuntOrbitTurnDegreesPerSecond <= 0f)
            {
                failureReason = "TheHunter drum cue timings are invalid.";
                return false;
            }

            if (drumAudioSource == null || huntDrumAudioSource == null)
            {
                failureReason = "TheHunter drum cue director is missing an alarm or hunt AudioSource.";
                return false;
            }

            if (!synthesizePrototypeDrumsWhenClipsAreMissing && (loudTribalDrumClip == null || distantHuntDrumsClip == null))
            {
                failureReason = "TheHunter needs assigned final drum clips when prototype synthesis is disabled.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public string BuildDebugSummary()
        {
            return $"TheHunter Drums {currentProximity} Dist={CurrentDistanceSlots} QueuedHits={queuedLoudHits} LoudHits={loudDrumPlaybackCount} HuntStarts={huntCuePlaybackCount}";
        }

        public static TheHunterDrumProximity GetProximityForDistance(int distanceSlots)
        {
            if (distanceSlots <= 0)
            {
                return TheHunterDrumProximity.None;
            }

            if (distanceSlots == 1)
            {
                return TheHunterDrumProximity.Adjacent;
            }

            if (distanceSlots == 2)
            {
                return TheHunterDrumProximity.TwoHexes;
            }

            return distanceSlots == 3 ? TheHunterDrumProximity.ThreeHexes : TheHunterDrumProximity.None;
        }

        private void Awake()
        {
            DiscoverSources();
            ConfigureAudioSource();
            random = new System.Random(0x4452554D);
        }

        private void OnDisable()
        {
            StopDrums();
        }

        private void OnDestroy()
        {
            DestroyGeneratedClip(generatedLoudDrum);
            DestroyGeneratedClip(generatedDistantHunt);
        }

        private void OnValidate()
        {
            loudDrumVolume = Mathf.Clamp01(loudDrumVolume);
            loudAlarmGain = Mathf.Clamp(loudAlarmGain, 1f, 3f);
            twoHexHuntVolume = Mathf.Clamp01(twoHexHuntVolume);
            adjacentHuntVolume = Mathf.Clamp01(adjacentHuntVolume);
            doubleHitSpacingSeconds = Mathf.Max(0.05f, doubleHitSpacingSeconds);
            adjacentLoudHitSpacingSeconds = Mathf.Max(0.05f, adjacentLoudHitSpacingSeconds);
            twoHexHuntIntervalSeconds = NormalizeInterval(twoHexHuntIntervalSeconds);
            twoHexHuntFadeInSeconds = NormalizeInterval(twoHexHuntFadeInSeconds);
            twoHexHuntHoldSeconds = NormalizeInterval(twoHexHuntHoldSeconds);
            twoHexHuntFadeOutSeconds = NormalizeInterval(twoHexHuntFadeOutSeconds);
            adjacentHuntFadeHalfCycleSeconds = NormalizeInterval(adjacentHuntFadeHalfCycleSeconds);
            huntCueSpatialDistanceMeters = Mathf.Max(1f, huntCueSpatialDistanceMeters);
            adjacentHuntRepositionIntervalSeconds = NormalizeInterval(adjacentHuntRepositionIntervalSeconds);
            adjacentHuntOrbitMaxDegrees = Mathf.Clamp(adjacentHuntOrbitMaxDegrees, 1f, 180f);
            adjacentHuntOrbitTurnDegreesPerSecond = Mathf.Clamp(adjacentHuntOrbitTurnDegreesPerSecond, 1f, 360f);
            deepDrumLowPassCutoffHz = Mathf.Clamp(deepDrumLowPassCutoffHz, 80f, 1200f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DiscoverSources();

            if (IsRunTerminal() || theHunter == null)
            {
                StopDrums();
                return;
            }

            TheHunterDrumProximity desiredProximity = GetProximityForDistance(theHunter.ExactPlayerDistanceSlots);

            if (desiredProximity != currentProximity)
            {
                EnterProximity(desiredProximity);
            }

            TickDrums(Time.deltaTime);
        }

        private void EnterProximity(TheHunterDrumProximity newProximity)
        {
            StopHuntCue();
            currentProximity = newProximity;
            queuedLoudHits = 0;
            nextLoudHitSeconds = float.PositiveInfinity;
            nextHuntSeconds = float.PositiveInfinity;

            switch (currentProximity)
            {
                case TheHunterDrumProximity.ThreeHexes:
                    QueueLoudHits(1, 0f);
                    break;

                case TheHunterDrumProximity.TwoHexes:
                    QueueLoudHits(2, 0f);
                    nextHuntSeconds = RollInterval(twoHexHuntIntervalSeconds);
                    break;

                case TheHunterDrumProximity.Adjacent:
                    nextLoudHitSeconds = 0f;
                    StartAdjacentHuntLoop();
                    break;
            }

            if (logProximityChanges)
            {
                Debug.Log($"Lost Forest TheHunter drums: Proximity={currentProximity}, Distance={CurrentDistanceSlots}", this);
            }
        }

        private void TickDrums(float deltaSeconds)
        {
            if (currentProximity == TheHunterDrumProximity.None)
            {
                return;
            }

            nextLoudHitSeconds -= deltaSeconds;
            nextHuntSeconds -= deltaSeconds;
            TickHuntCue(deltaSeconds);

            if (queuedLoudHits > 0 && nextLoudHitSeconds <= 0f)
            {
                PlayLoudDrum();
                queuedLoudHits--;
                nextLoudHitSeconds = queuedLoudHits > 0 ? doubleHitSpacingSeconds : float.PositiveInfinity;
            }
            else if (currentProximity == TheHunterDrumProximity.Adjacent && nextLoudHitSeconds <= 0f)
            {
                PlayLoudDrum();
                nextLoudHitSeconds = adjacentLoudHitSpacingSeconds;
            }

            if (currentProximity == TheHunterDrumProximity.TwoHexes && nextHuntSeconds <= 0f)
            {
                StartTwoHexHuntPulse();
            }
        }

        private void QueueLoudHits(int hitCount, float firstHitDelaySeconds)
        {
            queuedLoudHits = Mathf.Max(0, hitCount);
            nextLoudHitSeconds = Mathf.Max(0f, firstHitDelaySeconds);
        }

        private void PlayLoudDrum()
        {
            EnsureAudioClips();

            AudioClip alarmClip = firstAlarmDrumClip != null
                ? firstAlarmDrumClip
                : loudTribalDrumClip;

            if (drumAudioSource != null && alarmClip != null)
            {
                drumAudioSource.PlayOneShot(alarmClip, loudDrumVolume * loudAlarmGain);
                loudDrumPlaybackCount++;

                if (logAudioPlayback)
                {
                    Debug.Log($"Lost Forest TheHunter drum audio started: Type=Alarm, Count={loudDrumPlaybackCount}, Proximity={currentProximity}, Clip={alarmClip.name}, Volume={loudDrumVolume * loudAlarmGain:0.00}, Playing={drumAudioSource.isPlaying}", this);
                }
            }
            else if (logAudioPlayback)
            {
                Debug.LogWarning($"Lost Forest TheHunter drum alarm did not play: AudioSource={(drumAudioSource == null ? "Missing" : "Ready")}, Clip={(alarmClip == null ? "Missing" : alarmClip.name)}.", this);
            }
        }

        private void StartTwoHexHuntPulse()
        {
            EnsureAudioClips();

            if (huntDrumAudioSource == null || distantHuntDrumsClip == null)
            {
                if (logAudioPlayback)
                {
                    Debug.LogWarning($"Lost Forest TheHunter hunt pulse did not start: AudioSource={(huntDrumAudioSource == null ? "Missing" : "Ready")}, Clip={(distantHuntDrumsClip == null ? "Missing" : distantHuntDrumsClip.name)}.", this);
                }

                nextHuntSeconds = RollInterval(twoHexHuntIntervalSeconds);
                return;
            }

            huntCuePlaybackMode = HuntCuePlaybackMode.TwoHexPulse;
            huntDrumAudioSource.Stop();
            huntDrumAudioSource.clip = distantHuntDrumsClip;
            huntDrumAudioSource.loop = true;
            huntDrumAudioSource.volume = 0f;
            huntDrumAudioSource.Play();
            huntCuePlaybackCount++;

            if (logAudioPlayback)
            {
                Debug.Log($"Lost Forest TheHunter drum audio started: Type=TwoHexHunt, Count={huntCuePlaybackCount}, Clip={distantHuntDrumsClip.name}, Playing={huntDrumAudioSource.isPlaying}", this);
            }

            UpdateHuntCueSpatialPosition(0f);
            BeginHuntCueEnvelopePhase(HuntCueEnvelopePhase.FadeIn, RollInterval(twoHexHuntFadeInSeconds), twoHexHuntVolume);
            nextHuntSeconds = float.PositiveInfinity;
        }

        private void StartAdjacentHuntLoop()
        {
            EnsureAudioClips();

            if (huntDrumAudioSource == null || distantHuntDrumsClip == null)
            {
                if (logAudioPlayback)
                {
                    Debug.LogWarning($"Lost Forest TheHunter adjacent hunt loop did not start: AudioSource={(huntDrumAudioSource == null ? "Missing" : "Ready")}, Clip={(distantHuntDrumsClip == null ? "Missing" : distantHuntDrumsClip.name)}.", this);
                }

                return;
            }

            huntCuePlaybackMode = HuntCuePlaybackMode.AdjacentLoop;
            huntDrumAudioSource.Stop();
            huntDrumAudioSource.clip = distantHuntDrumsClip;
            huntDrumAudioSource.loop = true;
            huntDrumAudioSource.volume = 0f;
            huntDrumAudioSource.Play();
            huntCuePlaybackCount++;

            if (logAudioPlayback)
            {
                Debug.Log($"Lost Forest TheHunter drum audio started: Type=AdjacentHunt, Count={huntCuePlaybackCount}, Clip={distantHuntDrumsClip.name}, Playing={huntDrumAudioSource.isPlaying}", this);
            }

            adjacentHuntOrbitAngleDegrees = 0f;
            adjacentHuntTargetOrbitAngleDegrees = 0f;
            secondsUntilAdjacentHuntReposition = RollInterval(adjacentHuntRepositionIntervalSeconds);
            UpdateHuntCueSpatialPosition(0f);
            BeginHuntCueEnvelopePhase(HuntCueEnvelopePhase.FadeIn, RollInterval(adjacentHuntFadeHalfCycleSeconds), adjacentHuntVolume);
        }

        private void TickHuntCue(float deltaSeconds)
        {
            if (huntCuePlaybackMode == HuntCuePlaybackMode.None || huntDrumAudioSource == null)
            {
                return;
            }

            UpdateHuntCueSpatialPosition(deltaSeconds);
            huntCuePhaseSecondsRemaining -= deltaSeconds;
            float elapsed = huntCuePhaseDurationSeconds - Mathf.Max(0f, huntCuePhaseSecondsRemaining);
            float phaseProgress = Mathf.Clamp01(elapsed / huntCuePhaseDurationSeconds);
            huntDrumAudioSource.volume = Mathf.Lerp(huntCuePhaseStartVolume, huntCuePhaseTargetVolume, phaseProgress);

            if (huntCuePhaseSecondsRemaining > 0f)
            {
                return;
            }

            huntDrumAudioSource.volume = huntCuePhaseTargetVolume;

            if (huntCuePlaybackMode == HuntCuePlaybackMode.TwoHexPulse)
            {
                AdvanceTwoHexHuntPulse();
            }
            else
            {
                AdvanceAdjacentHuntLoop();
            }
        }

        private void AdvanceTwoHexHuntPulse()
        {
            switch (huntCueEnvelopePhase)
            {
                case HuntCueEnvelopePhase.FadeIn:
                    BeginHuntCueEnvelopePhase(HuntCueEnvelopePhase.Hold, RollInterval(twoHexHuntHoldSeconds), twoHexHuntVolume);
                    break;

                case HuntCueEnvelopePhase.Hold:
                    BeginHuntCueEnvelopePhase(HuntCueEnvelopePhase.FadeOut, RollInterval(twoHexHuntFadeOutSeconds), 0f);
                    break;

                default:
                    StopHuntCue();
                    nextHuntSeconds = RollInterval(twoHexHuntIntervalSeconds);
                    break;
            }
        }

        private void AdvanceAdjacentHuntLoop()
        {
            bool shouldFadeIn = huntCueEnvelopePhase != HuntCueEnvelopePhase.FadeIn;
            BeginHuntCueEnvelopePhase(
                shouldFadeIn ? HuntCueEnvelopePhase.FadeIn : HuntCueEnvelopePhase.FadeOut,
                RollInterval(adjacentHuntFadeHalfCycleSeconds),
                shouldFadeIn ? adjacentHuntVolume : 0f);
        }

        private void BeginHuntCueEnvelopePhase(HuntCueEnvelopePhase newPhase, float durationSeconds, float targetVolume)
        {
            huntCueEnvelopePhase = newPhase;
            huntCuePhaseDurationSeconds = Mathf.Max(0.05f, durationSeconds);
            huntCuePhaseSecondsRemaining = huntCuePhaseDurationSeconds;
            huntCuePhaseStartVolume = huntDrumAudioSource == null ? 0f : huntDrumAudioSource.volume;
            huntCuePhaseTargetVolume = Mathf.Clamp01(targetVolume);
        }

        private void StopHuntCue()
        {
            huntCuePlaybackMode = HuntCuePlaybackMode.None;
            huntCueEnvelopePhase = HuntCueEnvelopePhase.None;
            huntCuePhaseDurationSeconds = 0f;
            huntCuePhaseSecondsRemaining = 0f;
            huntCuePhaseStartVolume = 0f;
            huntCuePhaseTargetVolume = 0f;
            adjacentHuntOrbitAngleDegrees = 0f;
            adjacentHuntTargetOrbitAngleDegrees = 0f;
            secondsUntilAdjacentHuntReposition = 0f;

            if (huntDrumAudioSource != null)
            {
                huntDrumAudioSource.Stop();
                huntDrumAudioSource.volume = 0f;
            }
        }

        private void StopDrums()
        {
            currentProximity = TheHunterDrumProximity.None;
            queuedLoudHits = 0;
            nextLoudHitSeconds = float.PositiveInfinity;
            nextHuntSeconds = float.PositiveInfinity;
            StopHuntCue();

            if (drumAudioSource != null)
            {
                drumAudioSource.Stop();
            }
        }

        private void EnsureAudioClips()
        {
            if (!synthesizePrototypeDrumsWhenClipsAreMissing)
            {
                return;
            }

            if (loudTribalDrumClip == null)
            {
                generatedLoudDrum = generatedLoudDrum == null ? CreatePrototypeDrumClip("TheHunter Prototype Loud Frame Drum", 2.25f, 1) : generatedLoudDrum;
                loudTribalDrumClip = generatedLoudDrum;
            }

            if (distantHuntDrumsClip == null)
            {
                generatedDistantHunt = generatedDistantHunt == null ? CreatePrototypeDrumClip("TheHunter Prototype Distant Hunt", 4.8f, 4) : generatedDistantHunt;
                distantHuntDrumsClip = generatedDistantHunt;
            }
        }

        private static AudioClip CreatePrototypeDrumClip(string clipName, float durationSeconds, int hitCount)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            float[] samples = new float[sampleCount];
            float hitSpacing = hitCount <= 1 ? 0f : durationSeconds / (hitCount + 0.5f);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                float hitStartSeconds = hitIndex * hitSpacing;

                for (int sampleIndex = Mathf.FloorToInt(hitStartSeconds * sampleRate); sampleIndex < sampleCount; sampleIndex++)
                {
                    float localSeconds = (sampleIndex / (float)sampleRate) - hitStartSeconds;

                    if (localSeconds > 2.05f)
                    {
                        break;
                    }

                    // Keep this deliberately low and round: a soft mallet on a
                    // large hide drum, not a stick strike on a snare. The pitch
                    // drops after impact as the stretched skin settles.
                    float bodyFrequency = Mathf.Lerp(62f, 45f, Mathf.Clamp01(localSeconds / 0.22f));
                    float onset = 1f - Mathf.Exp(-localSeconds * 34f);
                    float body = Mathf.Sin(localSeconds * bodyFrequency * Mathf.PI * 2f) * onset * Mathf.Exp(-localSeconds * 1.72f);
                    float lowBody = Mathf.Sin(localSeconds * 36f * Mathf.PI * 2f) * onset * Mathf.Exp(-localSeconds * 1.35f) * 0.36f;
                    float warmMode = Mathf.Sin(localSeconds * bodyFrequency * 1.48f * Mathf.PI * 2f) * onset * Mathf.Exp(-localSeconds * 3.4f) * 0.16f;
                    float softMallet = Mathf.Sin(localSeconds * 112f * Mathf.PI * 2f) * Mathf.Exp(-localSeconds * 23f) * 0.025f;
                    float reverberation = 0f;

                    for (int echoIndex = 1; echoIndex <= 3; echoIndex++)
                    {
                        float echoSeconds = localSeconds - echoIndex * 0.38f;

                        if (echoSeconds < 0f)
                        {
                            continue;
                        }

                        float echoFrequency = Mathf.Lerp(48f, 35f, Mathf.Clamp01(echoSeconds / 0.6f));
                        float echoOnset = 1f - Mathf.Exp(-echoSeconds * 28f);
                        reverberation += Mathf.Sin(echoSeconds * echoFrequency * Mathf.PI * 2f) *
                            echoOnset * Mathf.Exp(-echoSeconds * 2.45f) * (0.2f / echoIndex);
                    }

                    samples[sampleIndex] = Mathf.Clamp(samples[sampleIndex] + body * 0.88f + lowBody + warmMode + softMallet + reverberation, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void DiscoverSources()
        {
            if (theHunter == null)
            {
                theHunter = FindAnyObjectByType<TheHunterPursuerController>();
            }

            if (playerCondition == null)
            {
                playerCondition = FindAnyObjectByType<PlayerCondition>();
            }

            if (audioListenerTransform == null)
            {
                AudioListener audioListener = FindAnyObjectByType<AudioListener>();
                audioListenerTransform = audioListener == null ? null : audioListener.transform;
            }

            if (runVictoryController == null)
            {
                runVictoryController = FindAnyObjectByType<RunVictoryController>();
            }

            if (drumAudioSource == null)
            {
                drumAudioSource = GetComponent<AudioSource>();
            }

            ConfigureHuntDrumAudioSource();

            if (drumLowPassFilter == null)
            {
                drumLowPassFilter = GetComponent<AudioLowPassFilter>();

                if (drumLowPassFilter == null)
                {
                    drumLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                }
            }
        }

        private void ConfigureAudioSource()
        {
            if (drumAudioSource == null)
            {
                drumAudioSource = GetComponent<AudioSource>();
            }

            ConfigureHuntDrumAudioSource();

            if (drumLowPassFilter == null)
            {
                drumLowPassFilter = GetComponent<AudioLowPassFilter>();

                if (drumLowPassFilter == null)
                {
                    drumLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                }
            }

            if (drumAudioSource == null)
            {
                return;
            }

            drumAudioSource.playOnAwake = false;
            drumAudioSource.loop = false;
            drumAudioSource.spatialBlend = 0f;
            drumAudioSource.volume = 1f;

            if (drumLowPassFilter != null)
            {
                drumLowPassFilter.enabled = true;
                drumLowPassFilter.cutoffFrequency = deepDrumLowPassCutoffHz;
                drumLowPassFilter.lowpassResonanceQ = 1f;
            }
        }

        private void ConfigureHuntDrumAudioSource()
        {
            if (huntDrumAudioSource == null)
            {
                Transform huntDrumAudioTransform = transform.Find(HuntDrumAudioObjectName);

                if (huntDrumAudioTransform == null)
                {
                    GameObject huntDrumAudioObject = new GameObject(HuntDrumAudioObjectName);
                    huntDrumAudioObject.transform.SetParent(transform, false);
                    huntDrumAudioTransform = huntDrumAudioObject.transform;
                }

                huntDrumAudioSource = huntDrumAudioTransform.GetComponent<AudioSource>();

                if (huntDrumAudioSource == null)
                {
                    huntDrumAudioSource = huntDrumAudioTransform.gameObject.AddComponent<AudioSource>();
                }
            }

            huntDrumAudioSource.playOnAwake = false;
            huntDrumAudioSource.loop = false;
            huntDrumAudioSource.spatialBlend = 1f;
            huntDrumAudioSource.spread = 0f;
            huntDrumAudioSource.dopplerLevel = 0f;
            huntDrumAudioSource.rolloffMode = AudioRolloffMode.Linear;
            huntDrumAudioSource.minDistance = 1f;
            huntDrumAudioSource.maxDistance = huntCueSpatialDistanceMeters * 3f;
            huntDrumAudioSource.volume = 0f;
        }

        private void UpdateHuntCueSpatialPosition(float deltaSeconds)
        {
            if (huntDrumAudioSource == null)
            {
                return;
            }

            Vector3 listenerPosition = audioListenerTransform == null ? transform.position : audioListenerTransform.position;
            Vector3 directionFromListenerToHunter = GetDirectionFromListenerToHunter(listenerPosition);

            if (huntCuePlaybackMode == HuntCuePlaybackMode.AdjacentLoop)
            {
                secondsUntilAdjacentHuntReposition -= deltaSeconds;

                if (secondsUntilAdjacentHuntReposition <= 0f)
                {
                    adjacentHuntTargetOrbitAngleDegrees = RollRange(-adjacentHuntOrbitMaxDegrees, adjacentHuntOrbitMaxDegrees);
                    secondsUntilAdjacentHuntReposition = RollInterval(adjacentHuntRepositionIntervalSeconds);
                }

                adjacentHuntOrbitAngleDegrees = Mathf.MoveTowardsAngle(
                    adjacentHuntOrbitAngleDegrees,
                    adjacentHuntTargetOrbitAngleDegrees,
                    adjacentHuntOrbitTurnDegreesPerSecond * deltaSeconds);
                directionFromListenerToHunter = Quaternion.AngleAxis(adjacentHuntOrbitAngleDegrees, Vector3.up) * directionFromListenerToHunter;
            }

            huntDrumAudioSource.transform.position = listenerPosition + directionFromListenerToHunter * huntCueSpatialDistanceMeters;
        }

        private Vector3 GetDirectionFromListenerToHunter(Vector3 listenerPosition)
        {
            Vector3 direction = theHunter == null || theHunter.CurrentHiddenSlot == null
                ? Vector3.forward
                : theHunter.CurrentHiddenSlot.WorldCenter - listenerPosition;
            direction.y = 0f;
            return direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
        }

        private bool IsRunTerminal()
        {
            return (playerCondition != null && playerCondition.IsGameOver) ||
                   (runVictoryController != null && runVictoryController.IsVictory);
        }

        private float RollInterval(Vector2 interval)
        {
            float t = random == null ? 0.5f : (float)random.NextDouble();
            return Mathf.Lerp(interval.x, interval.y, t);
        }

        private float RollRange(float minimum, float maximum)
        {
            float t = random == null ? 0.5f : (float)random.NextDouble();
            return Mathf.Lerp(minimum, maximum, t);
        }

        private static bool IsValidInterval(Vector2 interval)
        {
            return interval.x > 0f && interval.y >= interval.x;
        }

        private static Vector2 NormalizeInterval(Vector2 interval)
        {
            interval.x = Mathf.Max(0.01f, interval.x);
            interval.y = Mathf.Max(0.01f, interval.y);
            return interval.x <= interval.y ? interval : new Vector2(interval.y, interval.x);
        }

        private static void DestroyGeneratedClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(clip);
            }
            else
            {
                DestroyImmediate(clip);
            }
        }
    }
}
