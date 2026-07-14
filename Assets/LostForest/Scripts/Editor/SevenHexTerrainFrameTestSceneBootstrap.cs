#if UNITY_EDITOR
using System.IO;
using LostForest.Phase2.DebugTools;
using LostForest.Phase2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Editor
{
    public static class SevenHexTerrainFrameTestSceneBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/Phase2_SevenHexTerrainFrameTest.unity";

        [MenuItem("Lost Forest/Bootstrap/Open 7 Hex Terrain Frame Test Scene")]
        public static void OpenSevenHexTerrainFrameTestScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateOrRepairSevenHexTerrainFrameTestScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = Object.FindFirstObjectByType<SevenHexTerrainFrameDebugView>()?.gameObject;
        }

        [MenuItem("Lost Forest/Bootstrap/Create or Repair 7 Hex Terrain Frame Test Scene")]
        public static void CreateOrRepairSevenHexTerrainFrameTestScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SevenHexTerrainFrameDebugView existingFrame = Object.FindFirstObjectByType<SevenHexTerrainFrameDebugView>();
            GameObject frameObject = existingFrame != null
                ? existingFrame.gameObject
                : new GameObject("7 Hex Terrain Frame Debug View");

            SevenHexTerrainFrameDebugView debugView = existingFrame != null
                ? existingFrame
                : frameObject.AddComponent<SevenHexTerrainFrameDebugView>();

            debugView.Rebuild();
            EnsureCamera();
            EnsureLight();

            Selection.activeGameObject = frameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Lost Forest 7 hex terrain frame test scene is ready: {ScenePath}");
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindFirstObjectByType<Camera>();

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Terrain Frame Test Camera");
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, 185f, -210f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            camera.orthographic = false;
            camera.fieldOfView = 45f;

            if (camera.GetComponent<DebugOrbitCamera>() == null)
            {
                camera.gameObject.AddComponent<DebugOrbitCamera>();
            }
        }

        private static void EnsureLight()
        {
            Light light = Object.FindFirstObjectByType<Light>();

            if (light == null)
            {
                GameObject lightObject = new GameObject("Terrain Frame Test Key Light");
                light = lightObject.AddComponent<Light>();
            }

            light.name = "Terrain Frame Test Key Light";
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
        }
    }
}
#endif
