using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nemesis.Editor
{
    public class BossFightSetup : EditorWindow
    {
        private const string ScenePath = "Assets/_Game/Scenes/BossFight.unity";
        private const string PlaceholderMaterialPath = "Assets/_Game/Materials/BossPlaceholder.mat";

        [MenuItem("Nemesis/Setup/1. Build BossFight Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("⚠️ Build BossFight Scene cannot be run during Play Mode. Please exit Play Mode first.");
                return;
            }

            // Open or create the scene only if it's not already the active open scene
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (scene == null)
                {
                    var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(newScene, ScenePath);
                }
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Clean up existing duplicates before populating
            Debug.Log("[BossFightSetup] Cleaning up existing scene duplicates...");
            DestroyExisting("GameManager");
            DestroyExisting("AudioManager");
            DestroyExisting("NemesisManager");
            DestroyExisting("Canvas");
            DestroyExisting("HUD");
            DestroyExisting("DeathScreen");
            DestroyExisting("PlayerStatsUI");

            // Force global illumination / skybox / environment lighting to update
            DynamicGI.UpdateEnvironment();

            // Ensure tags exist
            AddTag("Player");
            AddTag("Boss");

            // Create folders needed for config assets
            CreateFolders();

            // Create or load config assets
            var gameConfig = GetOrCreateConfig<GameConfig>("Assets/_Game/Settings/GameConfig.asset");
            var bossConfig = GetOrCreateConfig<BossConfig>("Assets/_Game/Settings/BossConfig.asset");

            // Setup lighting, camera, and floor
            Debug.Log("[BossFightSetup] 1. Setting up environment...");
            SetupEnvironment();

            // Create GameManager and AudioManager objects
            Debug.Log("[BossFightSetup] 2. Creating managers...");
            CreateGameManager(gameConfig);
            CreateAudioManager();

            // Create player (Zilar) and attach required components
            Debug.Log("[BossFightSetup] 3. Creating Zilar...");
            var player = CreatePlayer(gameConfig);

            // Create boss (Humbaba) with placeholder or model
            Debug.Log("[BossFightSetup] 4. Creating Humbaba...");
            var boss = CreateBoss(bossConfig);

            // Create Nemesis manager (API manager + state cache)
            Debug.Log("[BossFightSetup] 5. Creating NemesisManager...");
            CreateNemesisManager();

            // Wire inspector references based on the linking table
            Debug.Log("[BossFightSetup] 6. Linking references...");
            LinkReferences(player, boss);

            // Setup UI canvas and HUD/DeathScreen UI Documents
            Debug.Log("[BossFightSetup] 7. Setting up UI Canvas and Documents...");
            SetupUI();

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("✅ BossFight scene successfully populated – starting Play Mode.");

            // Start playing the scene immediately
            EditorApplication.isPlaying = true;
        }

        private static void CreateFolders()
        {
            string[] folders = { "Assets/_Game/Settings", "Assets/_Game/Scripts/Nemesis" };
            foreach (var f in folders)
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    var parent = System.IO.Path.GetDirectoryName(f);
                    var newFolder = System.IO.Path.GetFileName(f);
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
            }
        }

        private static T GetOrCreateConfig<T>(string path) where T : ScriptableObject
        {
            var config = AssetDatabase.LoadAssetAtPath<T>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        private static void SetupEnvironment()
        {
            // Main Camera
            if (GameObject.Find("Main Camera") == null && Camera.main == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                var cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.transform.position = new Vector3(0, 5, -12);
                camObj.transform.rotation = Quaternion.Euler(15, 0, 0);
            }

            // Directional Light & Shadows
            var lightObj = GameObject.Find("Directional Light");
            Light light = null;
            if (lightObj != null) light = lightObj.GetComponent<Light>();
            if (light == null) light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                lightObj = new GameObject("Directional Light");
                light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            
            // Explicitly enable soft shadows on the directional light
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1.0f;

            // Floor
            if (GameObject.Find("Floor") == null)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.localScale = new Vector3(10, 1, 10);
            }

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static void CreateGameManager(GameConfig config)
        {
            var gmObj = GameObject.Find("GameManager");
            if (gmObj == null) gmObj = new GameObject("GameManager");
            var gm = gmObj.GetComponent<GameManager>();
            if (gm == null) gm = gmObj.AddComponent<GameManager>();
            // Assuming GameManager has a private field called "config"
            var field = typeof(GameManager).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(gm, config);
        }

        private static void CreateAudioManager()
        {
            var amObj = GameObject.Find("AudioManager");
            if (amObj == null) amObj = new GameObject("AudioManager");
            if (amObj.GetComponent<AudioManager>() == null) amObj.AddComponent<AudioManager>();
        }

        private static GameObject CreatePlayer(GameConfig config)
        {
            var player = GameObject.Find("Zilar");
            if (player == null)
            {
                Debug.Log("[BossFightSetup] Zilar not found in scene. Creating fresh...");
                // Try to load a prefab, otherwise create a capsule placeholder
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/Player/Prefab/Skin_1.prefab");
                if (prefab != null)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                }
                player.name = "Zilar";
                player.transform.position = new Vector3(0, 1, -5);
            }
            else
            {
                Debug.Log("[BossFightSetup] Zilar found in scene: " + player.name);
            }
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            // Add required components (if missing)
            var pc = player.GetComponent<PlayerController>();
            if (pc == null)
            {
                Debug.Log("[BossFightSetup] Adding PlayerController to Zilar...");
                pc = player.AddComponent<PlayerController>();
            }
            var ps = player.GetComponent<PlayerStats>();
            if (ps == null)
            {
                Debug.Log("[BossFightSetup] Adding PlayerStats to Zilar...");
                ps = player.AddComponent<PlayerStats>();
            }
            var pcmb = player.GetComponent<PlayerCombat>();
            if (pcmb == null)
            {
                Debug.Log("[BossFightSetup] Adding PlayerCombat to Zilar...");
                pcmb = player.AddComponent<PlayerCombat>();
            }
            var logger = player.GetComponent<CombatLogger>();
            if (logger == null)
            {
                Debug.Log("[BossFightSetup] Adding CombatLogger to Zilar...");
                logger = player.AddComponent<CombatLogger>();
            }
            var coll = player.GetComponent<CapsuleCollider>();
            if (coll == null)
            {
                Debug.Log("[BossFightSetup] Adding CapsuleCollider to Zilar...");
                coll = player.AddComponent<CapsuleCollider>();
            }
            coll.center = new Vector3(0f, 1f, 0f);
            coll.height = 2f;
            coll.radius = 0.4f;
            var rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.Log("[BossFightSetup] Adding Rigidbody to Zilar...");
                rb = player.AddComponent<Rigidbody>();
            }
            rb.mass = 80f;
            rb.linearDamping = 5f;
            rb.angularDamping = 10f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Find and assign Player Animator Controller
            var animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Game/Animations/Player/PlayerAnimator.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[BossFightSetup] Assigned PlayerAnimator to Zilar's Animator component.");
                }
                
                var animField = typeof(PlayerController).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animField != null) animField.SetValue(pc, animator);
            }

            // Hook up config fields via reflection (if they exist)
            var ctrlField = typeof(PlayerController).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField != null) ctrlField.SetValue(pc, config);
            var statsField = typeof(PlayerStats).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (statsField != null) statsField.SetValue(ps, config);

            // Setup PlayerInput actions (optional)
            var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (input == null) input = player.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Game/Scripts/Player/NemesisInputActions.inputactions");
            if (actions != null)
            {
                input.actions = actions;
                input.defaultActionMap = "Player";
                input.notificationBehavior = UnityEngine.InputSystem.PlayerNotifications.SendMessages;
            }

            // UI Document for player (optional, can be added later)
            return player;
        }

        private static GameObject CreateBoss(BossConfig config)
        {
            var boss = GameObject.Find("Humbaba");
            if (boss == null)
            {
                boss = new GameObject("Humbaba");
                boss.transform.position = new Vector3(0, 0, 5);
                boss.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }
            boss.tag = "Boss";
            boss.layer = LayerMask.NameToLayer("Default");

            // Clean up any primitive renderers if switching from placeholder
            var mf = boss.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            var mr = boss.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);

            // Add collider and rigidbody
            var col = boss.GetComponent<CapsuleCollider>();
            if (col == null) col = boss.AddComponent<CapsuleCollider>();
            col.height = 3f;
            col.radius = 0.8f;
            var rb = boss.GetComponent<Rigidbody>();
            if (rb == null) rb = boss.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Add core boss components
            var stats = boss.GetComponent<BossStats>();
            if (stats == null) stats = boss.AddComponent<BossStats>();
            var ctrl = boss.GetComponent<BossController>();
            if (ctrl == null) ctrl = boss.AddComponent<BossController>();
            var adapt = boss.GetComponent<BossAdaptation>();
            if (adapt == null) adapt = boss.AddComponent<BossAdaptation>();

            // Reflectively assign config references
            var statsField = typeof(BossController).GetField("bossStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (statsField != null) statsField.SetValue(ctrl, stats);
            var configField = typeof(BossController).GetField("bossConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null) configField.SetValue(ctrl, config);

            // Load and cache Boss Animator Controller
            var bossControllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Game/Animations/Boss/BossAnimator.controller");

            // Load FBX stage models if they exist, otherwise use placeholder capsule
            string[] stagePaths = { "Assets/_Game/Models/Boss/stage_1.fbx", "Assets/_Game/Models/Boss/stage_2.fbx", "Assets/_Game/Models/Boss/stage_3.fbx" };
            for (int i = 0; i < stagePaths.Length; i++)
            {
                var childName = $"Stage_{i + 1}";
                var existing = boss.transform.Find(childName);
                if (existing == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(stagePaths[i]);
                    if (asset != null)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                        instance.name = childName;
                        instance.transform.SetParent(boss.transform);
                        instance.transform.localPosition = Vector3.zero;
                        instance.SetActive(i == 0);
                    }
                    else if (i == 0)
                    {
                        // Fallback placeholder visual for stage 1
                        var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        placeholder.name = "PlaceholderVisual";
                        placeholder.transform.SetParent(boss.transform);
                        placeholder.transform.localPosition = new Vector3(0, 1, 0);
                        Object.DestroyImmediate(placeholder.GetComponent<Collider>());
                    }
                }
            }

            // Assign Humbaba stage animators
            if (bossControllerAsset != null)
            {
                foreach (Transform child in boss.transform)
                {
                    var anim = child.GetComponent<Animator>();
                    if (anim == null) anim = child.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.runtimeAnimatorController = bossControllerAsset;
                        Debug.Log($"[BossFightSetup] Assigned BossAnimator to Humbaba child {child.name}'s Animator.");
                    }
                }
            }

            // Create placeholder material if it doesn't exist
            if (AssetDatabase.LoadAssetAtPath<Material>(PlaceholderMaterialPath) == null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(130f/255f, 25f/255f, 25f/255f);
                AssetDatabase.CreateAsset(mat, PlaceholderMaterialPath);
                AssetDatabase.SaveAssets();
            }

            // Apply placeholder material to any primitive renders
            var renderers = boss.GetComponentsInChildren<Renderer>();
            var placeholderMat = AssetDatabase.LoadAssetAtPath<Material>(PlaceholderMaterialPath);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null)
                    r.sharedMaterial = placeholderMat;
            }

            return boss;
        }

        private static void CreateNemesisManager()
        {
            var nemesisObj = new GameObject("NemesisManager");
            nemesisObj.AddComponent<NemesisAPIManager>();
            nemesisObj.AddComponent<NemesisStateCache>();
            // Set fields per specification
            var apiMgr = nemesisObj.GetComponent<NemesisAPIManager>();
            var urlField = typeof(NemesisAPIManager).GetField("baseUrl", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (urlField != null) urlField.SetValue(apiMgr, "http://127.0.0.1:8000");
            var deviceField = typeof(NemesisAPIManager).GetField("deviceId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (deviceField != null) deviceField.SetValue(apiMgr, "DEMO_DEVICE_01");
        }

        private static void LinkReferences(GameObject player, GameObject boss)
        {
            // Player -> PlayerController: Boss Reference
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetLockOnTarget(boss.transform);

            // Player -> CombatLogger: Boss Controller & NemesisAPIManager
            var logger = player.GetComponent<CombatLogger>();
            var bcField = typeof(CombatLogger).GetField("bossController", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var apiField = typeof(CombatLogger).GetField("nemesisAPIManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var bossCtrl = boss.GetComponent<BossController>();
            var apiMgr = GameObject.FindObjectOfType<NemesisAPIManager>();
            if (bcField != null) bcField.SetValue(logger, bossCtrl);
            if (apiField != null) apiField.SetValue(logger, apiMgr);

            // Player -> PlayerCombat: PlayerStats reference
            var combat = player.GetComponent<PlayerCombat>();
            var statsField = typeof(PlayerCombat).GetField("stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pStats = player.GetComponent<PlayerStats>();
            if (statsField != null) statsField.SetValue(combat, pStats);

            // Boss -> BossController: Player reference
            var bCtrl = boss.GetComponent<BossController>();
            var playerRefField = typeof(BossController).GetField("playerReference", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (playerRefField != null) playerRefField.SetValue(bCtrl, player.transform);

            // Boss -> BossAdaptation: BossController reference
            var adapt = boss.GetComponent<BossAdaptation>();
            var adaptCtrlField = typeof(BossAdaptation).GetField("bossController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (adaptCtrlField != null) adaptCtrlField.SetValue(adapt, bCtrl);

            // GameManager references
            var gmObj = GameObject.Find("GameManager");
            var gm = gmObj != null ? gmObj.GetComponent<GameManager>() : null;
            if (gm != null)
            {
                var playerField = typeof(GameManager).GetField("player", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var bossField = typeof(GameManager).GetField("boss", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (playerField != null) playerField.SetValue(gm, player.transform);
                if (bossField != null) bossField.SetValue(gm, boss.transform);
            }
        }

        private static void SetupUI()
        {
            // Canvas
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.Log("[BossFightSetup] Canvas not found, creating a new one...");
                canvasObj = new GameObject("Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                Debug.Log("[BossFightSetup] Canvas found in scene: " + canvasObj.name);
            }

            // Find PanelSettings asset dynamically in project
            var settingsGuids = AssetDatabase.FindAssets("t:PanelSettings");
            UnityEngine.UIElements.PanelSettings panelSettings = null;
            if (settingsGuids.Length > 0)
            {
                var settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuids[0]);
                panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(settingsPath);
                Debug.Log("[BossFightSetup] Resolved PanelSettings asset: " + settingsPath);
            }
            else
            {
                Debug.LogWarning("[BossFightSetup] No PanelSettings asset found in workspace. Creating default PanelSettings asset automatically...");
                panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
                // Make sure UI folder exists
                if (!AssetDatabase.IsValidFolder("Assets/_Game/UI"))
                {
                    AssetDatabase.CreateFolder("Assets/_Game", "UI");
                }
                AssetDatabase.CreateAsset(panelSettings, "Assets/_Game/UI/PanelSettings.panelSettings");
                AssetDatabase.SaveAssets();
                Debug.Log("[BossFightSetup] Created and saved new PanelSettings asset successfully at: Assets/_Game/UI/PanelSettings.panelSettings");
            }

            // PlayerStats UI (optional)
            var statsUI = GameObject.Find("PlayerStatsUI");
            if (statsUI == null)
            {
                Debug.Log("[BossFightSetup] PlayerStatsUI not found, creating...");
                statsUI = new GameObject("PlayerStatsUI");
                statsUI.transform.SetParent(canvasObj.transform, false);
                statsUI.AddComponent<PlayerStatsUI>();
            }

            // HUD UI Document
            var hudPath = "Assets/_Game/UI/HUD.uxml";
            var hudAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(hudPath);
            if (hudAsset != null)
            {
                Debug.Log("[BossFightSetup] HUD UXML loaded successfully! Creating HUD GameObject...");
                var hudObj = new GameObject("HUD");
                var hudDoc = hudObj.AddComponent<UnityEngine.UIElements.UIDocument>();
                hudDoc.visualTreeAsset = hudAsset;
                if (panelSettings != null) hudDoc.panelSettings = panelSettings;
                hudObj.transform.SetParent(canvasObj.transform, false);
                hudObj.AddComponent<HudUI>();
                Debug.Log("[BossFightSetup] HUD GameObject created and HudUI attached!");
            }
            else
            {
                Debug.LogError("[BossFightSetup] FAILED to load HUD UXML at: " + hudPath);
            }

            // DeathScreen UI Document
            var deathPath = "Assets/_Game/UI/DeathScreen.uxml";
            var deathAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(deathPath);
            if (deathAsset != null)
            {
                Debug.Log("[BossFightSetup] DeathScreen UXML loaded successfully! Creating DeathScreen GameObject...");
                var deathObj = new GameObject("DeathScreen");
                var deathDoc = deathObj.AddComponent<UnityEngine.UIElements.UIDocument>();
                deathDoc.visualTreeAsset = deathAsset;
                if (panelSettings != null) deathDoc.panelSettings = panelSettings;
                deathObj.transform.SetParent(canvasObj.transform, false);

                // Attach to GameManager's DeathScreenUI component (if exists)
                var gmObj = GameObject.Find("GameManager");
                if (gmObj != null)
                {
                    Debug.Log("[BossFightSetup] GameManager found. Attaching DeathScreenUI controller...");
                    var deathUI = gmObj.GetComponent<DeathScreenUI>();
                    if (deathUI == null) deathUI = gmObj.AddComponent<DeathScreenUI>();
                    var panelField = typeof(DeathScreenUI).GetField("panel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (panelField != null) panelField.SetValue(deathUI, deathDoc);
                }
                Debug.Log("[BossFightSetup] DeathScreen GameObject created!");
            }
            else
            {
                Debug.LogError("[BossFightSetup] FAILED to load DeathScreen UXML at: " + deathPath);
            }
        }

        private static void AddTag(string tag)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0) return;
            var so = new UnityEditor.SerializedObject(tagManager[0]);
            var tagsProp = so.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return; // Already exists
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        private static void DestroyExisting(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
            
            // Re-query scene for any remaining objects to fully clean duplicates
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var o in allObjects)
            {
                if (o.name == name)
                {
                    Object.DestroyImmediate(o);
                }
            }
        }
    }
}
