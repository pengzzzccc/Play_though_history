using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    /// <summary>
    /// One-shot generator for the greybox scenes and prefabs. Reuses the assets that
    /// already exist on disk (input actions, materials, UI Toolkit assets); it never
    /// regenerates them. Run once via the menu, then maintain scenes by hand.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Root = "Assets/UnknownTechnology";
        private const string InputAssetPath = Root + "/Input/UnknownTechnologyActions.asset";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player.prefab";
        private const string BootstrapPrefabPath = Root + "/Prefabs/GameRoot.prefab";
        private const string PanelSettingsPath = Root + "/UI/PanelSettings.asset";
        private const string MainMenuUxmlPath = Root + "/UI/MainMenu.uxml";
        private const string EraUiUxmlPath = Root + "/UI/EraUI.uxml";
        private const string FloorMaterialPath = Root + "/Materials/Floor.mat";
        private const string WallMaterialPath = Root + "/Materials/Wall.mat";
        private const string AccentMaterialPath = Root + "/Materials/Accent.mat";

        private static readonly string[] ScenePaths =
        {
            Root + "/Scenes/Bootstrap.unity",
            Root + "/Scenes/MainMenu.unity",
            Root + "/Scenes/Era_Ancient.unity",
            Root + "/Scenes/Era_Modern.unity",
            Root + "/Scenes/Era_Future.unity"
        };

        [MenuItem("Unknown Technology/Build Scenes")]
        public static void BuildAll()
        {
            var inputAsset = LoadRequired<InputActionAsset>(InputAssetPath);
            var floor = LoadRequired<Material>(FloorMaterialPath);
            var wall = LoadRequired<Material>(WallMaterialPath);
            var accent = LoadRequired<Material>(AccentMaterialPath);

            CreatePlayerPrefab(accent);
            CreateBootstrapPrefab(inputAsset);
            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateEraScene("Era_Ancient", "ANCIENT EXHIBITION — PLAYABLE GREYBOX", floor, wall, accent, true);
            CreateEraScene("Era_Modern", "MODERN EXHIBITION — LOCKED PLACEHOLDER", floor, wall, accent, false);
            CreateEraScene("Era_Future", "FUTURE EXHIBITION — LOCKED PLACEHOLDER", floor, wall, accent, false);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Scenes built successfully.");
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static void CreatePlayerPrefab(Material accent)
        {
            var root = new GameObject("Player");
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.08f;

            var pitchPivot = new GameObject("Camera Pitch").transform;
            pitchPivot.SetParent(root.transform, false);
            pitchPivot.localPosition = new Vector3(0f, 1.65f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pitchPivot, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();

            var presentation = new GameObject("Hands And Sceptre").transform;
            presentation.SetParent(cameraObject.transform, false);
            presentation.localPosition = new Vector3(0.34f, -0.34f, 0.62f);
            CreatePrimitive(PrimitiveType.Cube, "Hand", presentation, new Vector3(-0.08f, -0.03f, 0f), new Vector3(0.16f, 0.12f, 0.28f), accent);
            var sceptre = CreatePrimitive(PrimitiveType.Cylinder, "Sceptre", presentation, new Vector3(0.05f, 0.03f, 0.16f), new Vector3(0.035f, 0.32f, 0.035f), accent);
            sceptre.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);

            var motor = root.AddComponent<PlayerMotor>();
            var cameraController = root.AddComponent<FirstPersonCameraController>();
            cameraController.Configure(root.transform, pitchPivot);
            var animationController = root.AddComponent<PlayerAnimationController>();
            animationController.Configure(null, presentation);
            root.AddComponent<FirstPersonPlayer>().Configure(motor, cameraController, animationController);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateBootstrapPrefab(InputActionAsset inputAsset)
        {
            var root = new GameObject("Game Root");
            var playerInput = root.AddComponent<PlayerInput>();
            playerInput.actions = inputAsset;
            playerInput.defaultControlScheme = "Keyboard&Mouse";
            playerInput.neverAutoSwitchControlSchemes = false;
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            root.AddComponent<GameBootstrap>();
            PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapPrefab = LoadRequired<GameObject>(BootstrapPrefabPath);
            PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePaths[0]);
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f);
            cameraObject.AddComponent<AudioListener>();

            var uiDocument = CreateUiDocument("Main Menu UI", MainMenuUxmlPath);
            uiDocument.AddComponent<UiScaleSettingsApplier>();
            uiDocument.AddComponent<MainMenuController>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePaths[1]);
        }

        private static void CreateEraScene(
            string sceneName,
            string heading,
            Material floor,
            Material wall,
            Material accent,
            bool detailed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            CreateGreyboxEnvironment(floor, wall, accent, detailed);

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0f, 0.05f, -6f);
            var playerPrefab = LoadRequired<GameObject>(PlayerPrefabPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

            var devSetup = new GameObject("DevSceneSetup").AddComponent<DevSceneSetup>();
            devSetup.Configure(LoadRequired<GameBootstrap>(BootstrapPrefabPath));

            var uiDocument = CreateUiDocument("Era UI", EraUiUxmlPath);
            uiDocument.AddComponent<UiScaleSettingsApplier>();
            uiDocument.AddComponent<EraHudController>().Configure(heading);
            uiDocument.AddComponent<PauseMenuController>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/" + sceneName + ".unity");
        }

        private static void CreateGreyboxEnvironment(Material floor, Material wall, Material accent, bool detailed)
        {
            CreateBlock("Floor", new Vector3(0f, -0.25f, 2f), new Vector3(20f, 0.5f, 22f), floor);
            CreateBlock("North Wall", new Vector3(0f, 2.5f, 12.5f), new Vector3(20f, 5f, 0.4f), wall);
            CreateBlock("West Wall", new Vector3(-10f, 2.5f, 2f), new Vector3(0.4f, 5f, 22f), wall);
            CreateBlock("East Wall", new Vector3(10f, 2.5f, 2f), new Vector3(0.4f, 5f, 22f), wall);
            CreateBlock("Entrance Left", new Vector3(-3.2f, 2.5f, -9f), new Vector3(6.4f, 5f, 0.4f), wall);
            CreateBlock("Entrance Right", new Vector3(3.2f, 2.5f, -9f), new Vector3(6.4f, 5f, 0.4f), wall);

            if (!detailed)
            {
                CreateBlock("Locked Exhibit", new Vector3(0f, 1.5f, 3f), new Vector3(5f, 3f, 1f), accent);
                return;
            }

            CreateBlock("Display Left", new Vector3(-5.5f, 1f, 1f), new Vector3(3f, 2f, 1.2f), wall);
            CreateBlock("Display Right", new Vector3(5.5f, 1f, 1f), new Vector3(3f, 2f, 1.2f), wall);
            var ramp = CreateBlock("Ramp", new Vector3(-4f, 0.45f, 7f), new Vector3(4f, 0.5f, 5f), accent);
            ramp.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            for (var index = 0; index < 3; index++)
            {
                CreateBlock($"Step {index + 1}", new Vector3(4f, 0.1f + index * 0.15f, 6f + index * 0.45f), new Vector3(2.5f, 0.2f + index * 0.3f, 0.45f), accent);
            }

            CreateBlock("Corridor Left", new Vector3(-1.2f, 1.5f, 9f), new Vector3(1.4f, 3f, 3.5f), wall);
            CreateBlock("Corridor Right", new Vector3(1.2f, 1.5f, 9f), new Vector3(1.4f, 3f, 3.5f), wall);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(primitive.GetComponent<Collider>());
            return primitive;
        }

        private static GameObject CreateUiDocument(string name, string uxmlPath)
        {
            // Load both assets at the point of use: an earlier-loaded PanelSettings can be
            // unloaded by NewScene/SaveScene (UnloadUnusedAssets) before it is assigned.
            var panelSettings = LoadRequired<PanelSettings>(PanelSettingsPath);
            var tree = LoadRequired<VisualTreeAsset>(uxmlPath);
            var uiObject = new GameObject(name);
            var uiDocument = uiObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = tree;
            return uiObject;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            var references = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath)
                .OfType<InputActionReference>()
                .ToDictionary(reference => reference.name, reference => reference);
            module.actionsAsset = LoadRequired<InputActionAsset>(InputAssetPath);
            module.point = references["UI_Point"];
            module.leftClick = references["UI_Click"];
            module.scrollWheel = references["UI_Scroll"];
            module.move = references["UI_Navigate"];
            module.submit = references["UI_Submit"];
            module.cancel = references["UI_Cancel"];
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = ScenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }
    }
}
