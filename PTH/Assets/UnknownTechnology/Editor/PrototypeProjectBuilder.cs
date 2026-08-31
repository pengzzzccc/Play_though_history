using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
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
using UnityEngine.UI;

namespace UnknownTechnology.Editor
{
    public static class PrototypeProjectBuilder
    {
        private const string Root = "Assets/UnknownTechnology";
        private const string InputAssetPath = Root + "/Input/UnknownTechnologyActions.asset";
        private const string SceneConfigPath = Root + "/Data/SceneFlowConfig.asset";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player.prefab";
        private const string BootstrapPrefabPath = Root + "/Prefabs/GameRoot.prefab";
        private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
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
            var font = CreateFontAsset();
            CreatePlayerPrefab(materials.Accent);
            CreateBootstrapPrefab(inputAsset, sceneConfig);

            CreateBootstrapScene();
            CreateMainMenuScene(font);
            CreateEraScene(SceneFlowConfig.AncientRoute, "Era_Ancient", "ANCIENT EXHIBITION — PLAYABLE GREYBOX", font, materials, true);
            CreateEraScene(SceneFlowConfig.ModernRoute, "Era_Modern", "MODERN EXHIBITION — LOCKED PLACEHOLDER", font, materials, false);
            CreateEraScene(SceneFlowConfig.FutureRoute, "Era_Future", "FUTURE EXHIBITION — LOCKED PLACEHOLDER", font, materials, false);
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
            var folders = new[] { "Input", "Data", "Prefabs", "Scenes", "Materials" };
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

        private static TMP_FontAsset CreateFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                throw new InvalidOperationException("TextMesh Pro Essential Resources are required before building the prototype.");
            }

            return fontAsset;
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

        private static void CreateMainMenuScene(TMP_FontAsset font)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMenuCamera();
            var canvas = CreateCanvas("Main Menu Canvas");
            var background = CreatePanel(canvas.transform, "Background", new Color(0.035f, 0.055f, 0.075f, 1f));
            var title = CreateText(background.transform, "Title", "UNKNOWN TECHNOLOGY", font, 48, new Vector2(0f, 175f), new Vector2(820f, 80f));
            title.alignment = TextAlignmentOptions.Center;
            var status = CreateText(background.transform, "Status", "Investigate the missing technology relics.", font, 22, new Vector2(0f, 90f), new Vector2(800f, 70f));
            status.alignment = TextAlignmentOptions.Center;
            var newGame = CreateButton(background.transform, "New Game", font, new Vector2(0f, 10f));
            var continueGame = CreateButton(background.transform, "Continue", font, new Vector2(0f, -55f));
            var quit = CreateButton(background.transform, "Quit", font, new Vector2(0f, -120f));
            var controller = background.AddComponent<MainMenuController>();
            controller.Configure(newGame, continueGame, quit, status);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePaths[1]);
        }

        private static void CreateEraScene(
            string routeId,
            string sceneName,
            string heading,
            TMP_FontAsset font,
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

            CreateEraHud(heading, font);
            CreatePauseUi(font);
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

        private static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 600f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<UiScaleSettingsApplier>();
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, TMP_FontAsset font, int fontSize, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = content;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, TMP_FontAsset font, Vector2 position)
        {
            var buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 52f);
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.48f, 0.5f, 0.95f);
            var text = CreateText(buttonObject.transform, "Label", label, font, 24, Vector2.zero, rect.sizeDelta);
            text.alignment = TextAlignmentOptions.Center;
            return buttonObject.GetComponent<Button>();
        }

        private static void CreateEraHud(string heading, TMP_FontAsset font)
        {
            var canvas = CreateCanvas("HUD Canvas");
            var headingText = CreateText(canvas.transform, "Era Heading", heading, font, 20, new Vector2(0f, 266f), new Vector2(850f, 45f));
            headingText.alignment = TextAlignmentOptions.Center;
            var controls = CreateText(canvas.transform, "Controls", "WASD / Left Stick: Move   Mouse / Right Stick: Look   Esc / Start: Pause", font, 17, new Vector2(0f, -270f), new Vector2(900f, 36f));
            controls.alignment = TextAlignmentOptions.Center;
            var crosshair = CreateText(canvas.transform, "Crosshair", "+", font, 24, Vector2.zero, new Vector2(32f, 32f));
            crosshair.alignment = TextAlignmentOptions.Center;
        }

        private static void CreatePauseUi(TMP_FontAsset font)
        {
            var canvas = CreateCanvas("Pause Canvas");
            canvas.sortingOrder = 20;
            var controllerObject = new GameObject("Pause Controller");
            controllerObject.transform.SetParent(canvas.transform, false);

            var pausePanel = CreatePanel(canvas.transform, "Pause Panel", new Color(0.02f, 0.03f, 0.04f, 0.9f));
            var title = CreateText(pausePanel.transform, "Title", "PAUSED", font, 42, new Vector2(0f, 190f), new Vector2(500f, 70f));
            title.alignment = TextAlignmentOptions.Center;
            var resume = CreateButton(pausePanel.transform, "Resume", font, new Vector2(-180f, 85f));
            var settings = CreateButton(pausePanel.transform, "Settings", font, new Vector2(-180f, 20f));
            var deviceMessage = CreateText(pausePanel.transform, "Device Message", string.Empty, font, 18, new Vector2(0f, -230f), new Vector2(850f, 60f));
            deviceMessage.alignment = TextAlignmentOptions.Center;

            var settingsPanel = new GameObject("Settings Panel", typeof(RectTransform), typeof(Image));
            settingsPanel.transform.SetParent(pausePanel.transform, false);
            var settingsRect = settingsPanel.GetComponent<RectTransform>();
            settingsRect.sizeDelta = new Vector2(430f, 400f);
            settingsRect.anchoredPosition = new Vector2(210f, 0f);
            settingsPanel.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.14f, 0.98f);

            var resources = GetDefaultControlResources();
            var mouse = CreateSlider(settingsPanel.transform, "Mouse Sensitivity", resources, font, 130f);
            var gamepad = CreateSlider(settingsPanel.transform, "Gamepad Sensitivity", resources, font, 65f);
            var uiScale = CreateSlider(settingsPanel.transform, "UI Scale", resources, font, 0f);
            var invert = CreateToggle(settingsPanel.transform, "Invert Y", resources, font, -65f);
            var reduced = CreateToggle(settingsPanel.transform, "Reduced Motion", resources, font, -115f);
            var fullscreen = CreateToggle(settingsPanel.transform, "Fullscreen", resources, font, -165f);
            var back = CreateButton(settingsPanel.transform, "Back", font, new Vector2(0f, -225f));

            var controller = controllerObject.AddComponent<PauseMenuController>();
            controller.Configure(pausePanel, settingsPanel, resume, settings, back, mouse, gamepad, uiScale, invert, reduced, fullscreen, deviceMessage);
            settingsPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        private static Slider CreateSlider(Transform parent, string label, DefaultControls.Resources resources, TMP_FontAsset font, float y)
        {
            var labelText = CreateText(parent, label + " Label", label, font, 17, new Vector2(-100f, y + 25f), new Vector2(210f, 30f));
            labelText.alignment = TextAlignmentOptions.Left;
            var control = DefaultControls.CreateSlider(resources);
            control.name = label + " Slider";
            control.transform.SetParent(parent, false);
            var rect = control.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 24f);
            rect.anchoredPosition = new Vector2(0f, y);
            return control.GetComponent<Slider>();
        }

        private static Toggle CreateToggle(Transform parent, string label, DefaultControls.Resources resources, TMP_FontAsset font, float y)
        {
            var control = DefaultControls.CreateToggle(resources);
            control.GetComponent<Toggle>().isOn = false;
            control.name = label + " Toggle";
            control.transform.SetParent(parent, false);
            var rect = control.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 36f);
            rect.anchoredPosition = new Vector2(0f, y);
            var legacyText = control.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyText.gameObject);
            }
            var labelText = CreateText(control.transform, "Label", label, font, 17, new Vector2(35f, 0f), new Vector2(270f, 32f));
            labelText.alignment = TextAlignmentOptions.Left;
            return control.GetComponent<Toggle>();
        }

        private static DefaultControls.Resources GetDefaultControlResources()
        {
            return new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
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
