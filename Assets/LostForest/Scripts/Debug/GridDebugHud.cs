using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Debugging
{
    public sealed class GridDebugHud : MonoBehaviour
    {
        private const string CameraHudObjectName = "Grid Debug Camera Text";

        [SerializeField] private PlayerGridAddressTracker gridAddressTracker;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private PlayerTerrainMovementState playerTerrainMovementState;
        [SerializeField] private ActiveRegionRenderer activeRegionRenderer;
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool showHud = true;
        [SerializeField] private int fontSize = 36;
        [SerializeField] private float characterSize = 0.0045f;
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0.46f, 0.27f, 1.55f);
        [SerializeField] private Color textColor = new Color(0.035f, 0.045f, 0.05f, 1f);

        private TextMesh hudText;
        private bool loggedScene;

        public void SetSources(PlayerGridAddressTracker newGridAddressTracker, ActiveRegionRenderer newActiveRegionRenderer)
        {
            gridAddressTracker = newGridAddressTracker;
            activeRegionRenderer = newActiveRegionRenderer;
        }

        public void SetPlayerCondition(PlayerCondition newPlayerCondition)
        {
            playerCondition = newPlayerCondition;
        }

        public void SetPlayerTerrainMovementState(PlayerTerrainMovementState newPlayerTerrainMovementState)
        {
            playerTerrainMovementState = newPlayerTerrainMovementState;
        }

        public void SetRuneManager(RuneManager newRuneManager)
        {
            runeManager = newRuneManager;
        }

        public void SetCamera(Camera newTargetCamera)
        {
            targetCamera = newTargetCamera;
        }

        public void ApplyCompactDefaults()
        {
            fontSize = 36;
            characterSize = 0.0045f;
            cameraLocalPosition = new Vector3(0.46f, 0.27f, 1.55f);
            textColor = new Color(0.035f, 0.045f, 0.05f, 1f);

            if (hudText != null)
            {
                hudText.transform.localPosition = cameraLocalPosition;
                hudText.fontSize = fontSize;
                hudText.characterSize = characterSize;
                hudText.color = textColor;
            }
        }

        private void Start()
        {
            EnsureHudText();
            LogActiveSceneOnce();
        }

        private void LateUpdate()
        {
            if (!showHud)
            {
                if (hudText != null)
                {
                    hudText.gameObject.SetActive(false);
                }

                return;
            }

            EnsureHudText();
            UpdateHudText();
        }

        private void EnsureHudText()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            Transform existingHud = targetCamera.transform.Find(CameraHudObjectName);

            if (existingHud != null)
            {
                hudText = existingHud.GetComponent<TextMesh>();
                existingHud.localPosition = cameraLocalPosition;
                return;
            }

            GameObject hudObject = new GameObject(CameraHudObjectName);
            hudObject.transform.SetParent(targetCamera.transform, false);
            hudObject.transform.localPosition = cameraLocalPosition;
            hudObject.transform.localRotation = Quaternion.identity;
            hudObject.transform.localScale = Vector3.one;

            hudText = hudObject.AddComponent<TextMesh>();
            hudText.anchor = TextAnchor.UpperRight;
            hudText.alignment = TextAlignment.Right;
            hudText.fontSize = fontSize;
            hudText.characterSize = characterSize;
            hudText.color = textColor;
        }

        private string BuildGridAddressText()
        {
            FieldSlotData slot = gridAddressTracker == null ? null : gridAddressTracker.CurrentSlot;
            return slot == null ? "--" : slot.Address;
        }

        private string BuildElevationText()
        {
            FieldSlotData slot = gridAddressTracker == null ? null : gridAddressTracker.CurrentSlot;

            if (slot == null)
            {
                return "Elev --";
            }

            TerrainElevationSample elevationSample = GetCurrentElevationSample(slot);
            return $"Elev {elevationSample.LogicalElevationMeters:0.0} {elevationSample.ElevationBand} {elevationSample.Landform}";
        }

        private TerrainElevationSample GetCurrentElevationSample(FieldSlotData slot)
        {
            if (gridAddressTracker == null)
            {
                return default;
            }

            Vector3 playerPosition = gridAddressTracker.transform.position;

            if (activeRegionRenderer != null && activeRegionRenderer.TrySampleTerrainElevation(slot, playerPosition, out TerrainElevationSample elevationSample))
            {
                return elevationSample;
            }

            return new TerrainElevationSample(
                new TerrainSurfaceSample(playerPosition, Vector3.up, TerrainSurfaceSampleSource.FrameHeightFallback, null),
                playerPosition.y,
                playerPosition.y,
                TerrainElevationBand.Mid,
                TerrainLandform.Unknown,
                0f,
                0f,
                Vector3.zero,
                Vector3.zero,
                -1,
                0f,
                0f,
                Vector3.zero);
        }

        private string BuildConditionText()
        {
            if (playerCondition == null)
            {
                return string.Empty;
            }

            string state = BuildConditionStateText();
            return $"Sta {playerCondition.Stamina:0}/{playerCondition.EffectiveMaxStamina:0} ({playerCondition.StaminaNormalized * 100f:0}%){state}\nCap C{playerCondition.ChillStaminaCapMultiplier:0.00} F{playerCondition.SprintFatigueCapMultiplier:0.00}\nChill {playerCondition.ChillNormalized * 100f:0}% Move x{playerCondition.ConditionSpeedMultiplier:0.00}";
        }

        private string BuildMovementText()
        {
            DiscoverPlayerTerrainMovementStateIfNeeded();

            if (playerTerrainMovementState == null)
            {
                return string.Empty;
            }

            string sprint = playerTerrainMovementState.IsSprinting ? "Y" : "N";
            return $"Move {playerTerrainMovementState.TravelState} Slope {playerTerrainMovementState.CurrentSlopeDegrees:0}deg\nGrade {playerTerrainMovementState.SignedMovementGradeDegrees:+0.0;-0.0;0.0}deg x{playerTerrainMovementState.SpeedMultiplier:0.00}\nSpeed {playerTerrainMovementState.FinalMovementSpeedMetersPerSecond:0.0} Sprint {sprint}";
        }

        private void UpdateHudText()
        {
            if (hudText == null)
            {
                return;
            }

            hudText.gameObject.SetActive(true);
            hudText.fontSize = fontSize;
            hudText.characterSize = characterSize;
            hudText.color = textColor;
            string movementText = BuildMovementText();
            string conditionText = BuildConditionText();
            string runeText = BuildRuneText();
            hudText.text = $"{BuildGridAddressText()}\n{BuildElevationText()}{BuildOptionalLine(movementText)}{BuildOptionalLine(conditionText)}{BuildOptionalLine(runeText)}";
        }

        private void LogActiveSceneOnce()
        {
            if (loggedScene)
            {
                return;
            }

            loggedScene = true;
            Debug.Log($"Lost Forest Grid Debug HUD active in scene '{SceneManager.GetActiveScene().name}'. Camera text overlay enabled.");
        }

        private void DiscoverPlayerTerrainMovementStateIfNeeded()
        {
            if (playerTerrainMovementState != null || gridAddressTracker == null)
            {
                return;
            }

            playerTerrainMovementState = gridAddressTracker.GetComponent<PlayerTerrainMovementState>();
        }

        private static string BuildOptionalLine(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : $"\n{value}";
        }

        private string BuildRuneText()
        {
            if (runeManager == null)
            {
                return string.Empty;
            }

            string nearestMatchingSlot = "None";

            if (runeManager.TryGetNearestMatchingRuneSlotDebug(out string slotAddress, out char runeLetter, out float distanceMeters))
            {
                nearestMatchingSlot = $"{slotAddress} {runeLetter} {distanceMeters:0.0}m";
            }

            return $"Needed: {runeManager.NeededRunesDebugText}\nCarried: {runeManager.CarriedRuneDebugText}\nDeposited: {runeManager.DepositedRunesDebugText}\nActive Rune Markers: {runeManager.ActiveMarkerCount}\nNearest Matching Rune Slot: {nearestMatchingSlot}";
        }

        private string BuildConditionStateText()
        {
            if (playerCondition.IsGameOver)
            {
                return " GAME OVER";
            }

            if (playerCondition.IsFrozen)
            {
                return " FROZEN";
            }

            return playerCondition.IsExhausted ? " EXH" : string.Empty;
        }
    }
}
