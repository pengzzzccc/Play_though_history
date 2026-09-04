using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnknownTechnology.Bootstrap;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.State;
using UnknownTechnology.Gameplay.Input;
using UnknownTechnology.Gameplay.Player;
using UnknownTechnology.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnknownTechnology.Editor
{
    public static class PrototypeProjectBuilder
    {
        private const string Root = "Assets/UnknownTechnology";
        private const string InputAssetPath = Root + "/Input/UnknownTechnologyActions.asset";
        private const string SceneConfigPath = Root + "/Data/SceneFlowConfig.asset";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player.prefab";
        private const string BootstrapPrefabPath = Root + "/Prefabs/GameRoot.prefab";
        private const string UiPath = Root + "/UI";
        private const string ThemePath = UiPath + "/DefaultTheme.tss";
        private const string PanelSettingsPath = UiPath + "/PanelSettings.asset";
        private const string MainMenuUxmlPath = UiPath + "/MainMenu.uxml";
        private const string EraUiUxmlPath = UiPath + "/EraUI.uxml";
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

        [MenuItem("Unknown Technology/Build Playable Prototype")]
        public static void BuildAll()
        {
            EnsureFolders();
            var inputAsset = CreateInputAsset();
            var sceneConfig = CreateSceneConfig();
            var materials = CreateMaterials();
            EnsurePanelSettings();
            CreatePlayerPrefab(materials.Accent);
            CreateBootstrapPrefab(inputAsset, sceneConfig);

            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateEraScene(SceneFlowConfig.AncientRoute, "Era_Ancient", "ANCIENT EXHIBITION — PLAYABLE GREYBOX", materials, true);
            CreateEraScene(SceneFlowConfig.ModernRoute, "Era_Modern", "MODERN EXHIBITION — LOCKED PLACEHOLDER", materials, false);
            CreateEraScene(SceneFlowConfig.FutureRoute, "Era_Future", "FUTURE EXHIBITION — LOCKED PLACEHOLDER", materials, false);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Unknown Technology playable prototype assets were built successfully.");
        }

        public static void BuildWindowsDevelopment()
        {
            BuildAll();
            Directory.CreateDirectory("Builds/Development/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = "Builds/Development/Windows/UnknownTechnology.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
            }

            Debug.Log($"Windows development build succeeded ({report.summary.totalSize} bytes).");
        }

        public static void BuildWebGlDevelopment()
        {
            BuildAll();
            Directory.CreateDirectory("Builds/Development/WebGL");
            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = "Builds/Development/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
            }

            Debug.Log($"WebGL development build succeeded ({report.summary.totalSize} bytes).");
        }

        private static void EnsureFolders()
        {
            var folders = new[] { "Input", "Data", "Prefabs", "Scenes", "Materials", "UI" };
            foreach (var folder in folders)
            {
                var path = Root + "/" + folder;
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(Root, folder);
                }
            }
        }

        private static InputActionAsset CreateInputAsset()
        {
            AssetDatabase.DeleteAsset(InputAssetPath);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "UnknownTechnologyActions";

            var gameplay = asset.AddActionMap(InputRouter.GameplayMapName);
            var move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            AddKeyboardMove(move);
            move.AddBinding("<Gamepad>/leftStick", groups: "Gamepad").WithProcessor("StickDeadzone(min=0.15,max=0.95)");
            var look = gameplay.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta", groups: "Keyboard&Mouse");
            look.AddBinding("<Gamepad>/rightStick", groups: "Gamepad").WithProcessor("StickDeadzone(min=0.15,max=0.95)");
            AddButton(gameplay, "Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            AddButton(gameplay, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            var tool = AddButton(gameplay, "Tool", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            tool.AddBinding("<Mouse>/rightButton", groups: "Keyboard&Mouse");
            AddButton(gameplay, "Notebook", "<Keyboard>/tab", "<Gamepad>/select");
            AddButton(gameplay, "Pause", "<Keyboard>/escape", "<Gamepad>/start");

            var restoration = asset.AddActionMap(InputRouter.RestorationMapName);
            var movePiece = restoration.AddAction("MovePiece", InputActionType.Value, expectedControlLayout: "Vector2");
            AddKeyboardMove(movePiece);
            movePiece.AddBinding("<Gamepad>/leftStick", groups: "Gamepad").WithProcessor("StickDeadzone(min=0.15,max=0.95)");
            var rotatePiece = restoration.AddAction("RotatePiece", InputActionType.Value, expectedControlLayout: "Vector2");
            rotatePiece.AddBinding("<Mouse>/delta", groups: "Keyboard&Mouse");
            rotatePiece.AddBinding("<Gamepad>/rightStick", groups: "Gamepad").WithProcessor("StickDeadzone(min=0.15,max=0.95)");
            AddButton(restoration, "SelectPlace", "<Mouse>/leftButton", "<Gamepad>/buttonSouth");
            var cycle = restoration.AddAction("CyclePiece", InputActionType.Value, expectedControlLayout: "Axis");
            cycle.AddBinding("<Mouse>/scroll/y", groups: "Keyboard&Mouse");
            cycle.AddBinding("<Gamepad>/dpad/x", groups: "Gamepad");
            AddButton(restoration, "Hint", "<Keyboard>/h", "<Gamepad>/buttonNorth");
            AddButton(restoration, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            AddButton(restoration, "Pause", "<Keyboard>/p", "<Gamepad>/start");

            var ui = asset.AddActionMap(InputRouter.UiMapName);
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            AddKeyboardMove(navigate);
            navigate.AddBinding("<Gamepad>/leftStick", groups: "Gamepad").WithProcessor("StickDeadzone(min=0.2,max=0.95)");
            navigate.AddBinding("<Gamepad>/dpad", groups: "Gamepad");
            AddButton(ui, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            var cancel = AddButton(ui, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            cancel.AddBinding("<Keyboard>/backspace", groups: "Keyboard&Mouse");
            var point = ui.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position", groups: "Keyboard&Mouse");
            var click = ui.AddAction("Click", InputActionType.PassThrough, expectedControlLayout: "Button");
            click.AddBinding("<Mouse>/leftButton", groups: "Keyboard&Mouse");
            var scroll = ui.AddAction("Scroll", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            scroll.AddBinding("<Mouse>/scroll", groups: "Keyboard&Mouse");

            asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");
            AssetDatabase.CreateAsset(asset, InputAssetPath);
            AddInputActionReference(asset, ui.FindAction("Point", true));
            AddInputActionReference(asset, ui.FindAction("Click", true));
            AddInputActionReference(asset, ui.FindAction("Scroll", true));
            AddInputActionReference(asset, ui.FindAction("Navigate", true));
            AddInputActionReference(asset, ui.FindAction("Submit", true));
            AddInputActionReference(asset, ui.FindAction("Cancel", true));
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void AddInputActionReference(InputActionAsset asset, InputAction action)
        {
            var reference = InputActionReference.Create(action);
            reference.name = action.actionMap.name + "_" + action.name;
            AssetDatabase.AddObjectToAsset(reference, asset);
        }

        private static InputAction AddButton(InputActionMap map, string name, string keyboardPath, string gamepadPath)
        {
            var action = map.AddAction(name, InputActionType.Button, expectedControlLayout: "Button");
            action.AddBinding(keyboardPath, groups: "Keyboard&Mouse");
            action.AddBinding(gamepadPath, groups: "Gamepad");
            return action;
        }

        private static void AddKeyboardMove(InputAction action)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", groups: "Keyboard&Mouse")
                .With("Down", "<Keyboard>/s", groups: "Keyboard&Mouse")
                .With("Left", "<Keyboard>/a", groups: "Keyboard&Mouse")
                .With("Right", "<Keyboard>/d", groups: "Keyboard&Mouse");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", groups: "Keyboard&Mouse")
                .With("Down", "<Keyboard>/downArrow", groups: "Keyboard&Mouse")
                .With("Left", "<Keyboard>/leftArrow", groups: "Keyboard&Mouse")
                .With("Right", "<Keyboard>/rightArrow", groups: "Keyboard&Mouse");
        }

        private static SceneFlowConfig CreateSceneConfig()
        {
            AssetDatabase.DeleteAsset(SceneConfigPath);
            var config = ScriptableObject.CreateInstance<SceneFlowConfig>();
            config.Configure(new[]
            {
                new SceneRoute(SceneFlowConfig.MainMenuRoute, ScenePaths[1], GamePhase.MainMenu, false),
                new SceneRoute(SceneFlowConfig.AncientRoute, ScenePaths[2], GamePhase.Exploring, true),
                new SceneRoute(SceneFlowConfig.ModernRoute, ScenePaths[3], GamePhase.Exploring, true),
                new SceneRoute(SceneFlowConfig.FutureRoute, ScenePaths[4], GamePhase.Exploring, true)
            });
            AssetDatabase.CreateAsset(config, SceneConfigPath);
            return config;
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet(
                CreateMaterial(FloorMaterialPath, new Color(0.11f, 0.14f, 0.17f)),
                CreateMaterial(WallMaterialPath, new Color(0.30f, 0.34f, 0.37f)),
                CreateMaterial(AccentMaterialPath, new Color(0.16f, 0.72f, 0.72f)));
        }

        private static Material CreateMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsurePanelSettings()
        {
            if (!File.Exists(Path.GetFullPath(ThemePath)))
            {
                throw new InvalidOperationException($"UI Toolkit theme is missing: {ThemePath}. Keep DefaultTheme.tss, Main.uss, MainMenu.uxml, and EraUI.uxml inside {UiPath}.");
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new InvalidOperationException($"UI Toolkit theme could not be imported: {ThemePath}");
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                // Swap to the canonical loaded instance so scene references serialize reliably.
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
                if (panelSettings == null)
                {
                    throw new InvalidOperationException($"PanelSettings asset was created but could not be loaded: {PanelSettingsPath}");
                }
            }

            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(960, 600);
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();
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
            CreatePresentationPrimitive(PrimitiveType.Cube, "Hand", presentation, new Vector3(-0.08f, -0.03f, 0f), new Vector3(0.16f, 0.12f, 0.28f), accent);
            var sceptre = CreatePresentationPrimitive(PrimitiveType.Cylinder, "Sceptre", presentation, new Vector3(0.05f, 0.03f, 0.16f), new Vector3(0.035f, 0.32f, 0.035f), accent);
            sceptre.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);

            var motor = root.AddComponent<PlayerMotor>();
            motor.Configure(4f);
            var cameraController = root.AddComponent<FirstPersonCameraController>();
            cameraController.Configure(root.transform, pitchPivot);
            var animationController = root.AddComponent<PlayerAnimationController>();
            animationController.Configure(null, presentation);
            var coordinator = root.AddComponent<FirstPersonPlayerController>();
            coordinator.Configure(motor, cameraController, animationController);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PlayerPrefabPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static GameObject CreatePresentationPrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
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

        private static void CreateBootstrapPrefab(InputActionAsset inputAsset, SceneFlowConfig sceneConfig)
        {
            var root = new GameObject("Game Root");
            var playerInput = root.AddComponent<PlayerInput>();
            playerInput.actions = inputAsset;
            playerInput.defaultControlScheme = "Keyboard&Mouse";
            playerInput.neverAutoSwitchControlSchemes = false;
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var inputRouter = root.AddComponent<InputRouter>();
            var sceneFlow = root.AddComponent<SceneFlowController>();
            sceneFlow.Configure(sceneConfig);
            var bootstrap = root.AddComponent<GameBootstrap>();
            bootstrap.Configure(inputRouter, sceneFlow);
            PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(BootstrapPrefabPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePaths[0]);
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMenuCamera();
            var uiDocument = CreateUiDocument("Main Menu UI", MainMenuUxmlPath);
            uiDocument.AddComponent<UiScaleSettingsApplier>();
            uiDocument.AddComponent<MainMenuController>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePaths[1]);
        }

        private static void CreateEraScene(
            string routeId,
            string sceneName,
            string heading,
            MaterialSet materials,
            bool detailed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath).GetComponent<GameBootstrap>();
            CreateDirectionalLight();
            CreateGreyboxEnvironment(materials, detailed);

            var spawn = new GameObject("Player Spawn").transform;
            spawn.position = new Vector3(0f, 0.05f, -6f);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            var contextObject = new GameObject("Era Scene Context");
            var context = contextObject.AddComponent<EraSceneContext>();
            context.Configure(routeId, spawn, player.transform);
            var developmentBootstrap = contextObject.AddComponent<DevelopmentSceneBootstrapper>();
            developmentBootstrap.Configure(bootstrapPrefab);

            var uiDocument = CreateUiDocument("Era UI", EraUiUxmlPath);
            uiDocument.AddComponent<UiScaleSettingsApplier>();
            var hudController = uiDocument.AddComponent<EraHudController>();
            hudController.Configure(heading);
            uiDocument.AddComponent<PauseMenuController>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/" + sceneName + ".unity");
        }

        private static void CreateGreyboxEnvironment(MaterialSet materials, bool detailed)
        {
            CreateBlock("Floor", new Vector3(0f, -0.25f, 2f), new Vector3(20f, 0.5f, 22f), materials.Floor);
            CreateBlock("North Wall", new Vector3(0f, 2.5f, 12.5f), new Vector3(20f, 5f, 0.4f), materials.Wall);
            CreateBlock("West Wall", new Vector3(-10f, 2.5f, 2f), new Vector3(0.4f, 5f, 22f), materials.Wall);
            CreateBlock("East Wall", new Vector3(10f, 2.5f, 2f), new Vector3(0.4f, 5f, 22f), materials.Wall);
            CreateBlock("Entrance Left", new Vector3(-3.2f, 2.5f, -9f), new Vector3(6.4f, 5f, 0.4f), materials.Wall);
            CreateBlock("Entrance Right", new Vector3(3.2f, 2.5f, -9f), new Vector3(6.4f, 5f, 0.4f), materials.Wall);

            if (!detailed)
            {
                CreateBlock("Locked Exhibit", new Vector3(0f, 1.5f, 3f), new Vector3(5f, 3f, 1f), materials.Accent);
                return;
            }

            CreateBlock("Display Left", new Vector3(-5.5f, 1f, 1f), new Vector3(3f, 2f, 1.2f), materials.Wall);
            CreateBlock("Display Right", new Vector3(5.5f, 1f, 1f), new Vector3(3f, 2f, 1.2f), materials.Wall);
            var ramp = CreateBlock("Ramp", new Vector3(-4f, 0.45f, 7f), new Vector3(4f, 0.5f, 5f), materials.Accent);
            ramp.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            for (var index = 0; index < 3; index++)
            {
                CreateBlock($"Step {index + 1}", new Vector3(4f, 0.1f + index * 0.15f, 6f + index * 0.45f), new Vector3(2.5f, 0.2f + index * 0.3f, 0.45f), materials.Accent);
            }

            CreateBlock("Corridor Left", new Vector3(-1.2f, 1.5f, 9f), new Vector3(1.4f, 3f, 3.5f), materials.Wall);
            CreateBlock("Corridor Right", new Vector3(1.2f, 1.5f, 9f), new Vector3(1.4f, 3f, 3.5f), materials.Wall);
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

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void CreateMenuCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            cameraObject.GetComponent<Camera>().backgroundColor = new Color(0.035f, 0.055f, 0.075f);
            cameraObject.AddComponent<AudioListener>();
        }

        private static GameObject CreateUiDocument(string name, string uxmlPath)
        {
            // Load both assets at the point of use: an earlier-loaded PanelSettings can be
            // unloaded by NewScene/SaveScene (UnloadUnusedAssets) before it is assigned.
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                throw new InvalidOperationException($"PanelSettings could not be loaded from {PanelSettingsPath}; the UI document would render nothing.");
            }

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (tree == null)
            {
                throw new InvalidOperationException($"UI Toolkit document is missing: {uxmlPath}. Keep DefaultTheme.tss, Main.uss, MainMenu.uxml, and EraUI.uxml inside {UiPath}.");
            }

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
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            var references = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath)
                .OfType<InputActionReference>()
                .ToDictionary(reference => reference.name, reference => reference);
            module.actionsAsset = asset;
            module.point = references["UI_Point"];
            module.leftClick = references["UI_Click"];
            module.scrollWheel = references["UI_Scroll"];
            module.move = references["UI_Navigate"];
            module.submit = references["UI_Submit"];
            module.cancel = references["UI_Cancel"];
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in ScenePaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(Material floor, Material wall, Material accent)
            {
                Floor = floor;
                Wall = wall;
                Accent = accent;
            }

            public Material Floor { get; }
            public Material Wall { get; }
            public Material Accent { get; }
        }
    }
}
