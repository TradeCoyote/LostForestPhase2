#if UNITY_EDITOR
using System.IO;
using LostForest.Phase2.Player;
using LostForest.Phase2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Editor
{
    public static class EarlyWalkThruTestSceneBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/Phase2_EarlyWalkThruTest.unity";

        [MenuItem("Lost Forest/Bootstrap/Open Early WalkThru Test Scene")]
        public static void OpenEarlyWalkThruTestScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateOrRepairEarlyWalkThruTestScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = Object.FindFirstObjectByType<EarlyWalkThruFirstPersonController>()?.gameObject;
        }

        [MenuItem("Lost Forest/Bootstrap/Create or Repair Early WalkThru Test Scene")]
        public static void CreateOrRepairEarlyWalkThruTestScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SevenHexTerrainFrameDebugView terrainFrame = EnsureTerrainFrame();
            terrainFrame.ApplyEarlyWalkThruVisualDefaults();
            terrainFrame.Rebuild();

            GameObject playerObject = EnsurePlayer(terrainFrame);
            EnsureLight();

            Selection.activeGameObject = playerObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Lost Forest Early WalkThru test scene is ready: {ScenePath}");
        }

        private static SevenHexTerrainFrameDebugView EnsureTerrainFrame()
        {
            SevenHexTerrainFrameDebugView terrainFrame = Object.FindFirstObjectByType<SevenHexTerrainFrameDebugView>();

            if (terrainFrame != null)
            {
                terrainFrame.gameObject.name = "Early WalkThru 7 Hex Terrain Frame";
                return terrainFrame;
            }

            GameObject terrainObject = new GameObject("Early WalkThru 7 Hex Terrain Frame");
            return terrainObject.AddComponent<SevenHexTerrainFrameDebugView>();
        }

        private static GameObject EnsurePlayer(SevenHexTerrainFrameDebugView terrainFrame)
        {
            EarlyWalkThruFirstPersonController existingController = Object.FindFirstObjectByType<EarlyWalkThruFirstPersonController>();
            GameObject playerObject = existingController == null
                ? new GameObject("Early WalkThru Player")
                : existingController.gameObject;

            playerObject.name = "Early WalkThru Player";
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

            EarlyWalkThruCenterSpawn centerSpawn = GetOrAddComponent<EarlyWalkThruCenterSpawn>(playerObject);
            centerSpawn.SetTerrainFrame(terrainFrame);
            centerSpawn.SetSpawnSlot(EarlyWalkThruSpawnSlot.Center);

            GetOrAddComponent<EarlyWalkThruPositionLogger>(playerObject);

            DisableNonPlayerCameras(cameraRoot.GetComponent<Camera>());
            centerSpawn.SpawnAtTerrainSlotCenter();
            return playerObject;
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
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

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
            Light light = Object.FindFirstObjectByType<Light>();

            if (light == null)
            {
                GameObject lightObject = new GameObject("Early WalkThru Key Light");
                light = lightObject.AddComponent<Light>();
            }

            light.name = "Early WalkThru Key Light";
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
        }
    }
}
#endif
