#if UNITY_EDITOR
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
        [MenuItem("Nemesis/Setup/1. Build BossFight Scene")]
        public static void BuildScene()
        {
            // Ensure we are working in a scene
            Scene scene = EditorSceneManager.GetActiveScene();
            
            // 1. Setup Folders
            CreateFolders();

            // 2. Create Configs
            GameConfig gameConfig = GetOrCreateConfig<GameConfig>("Assets/_Game/Settings/GameConfig.asset");
            BossConfig bossConfig = GetOrCreateConfig<BossConfig>("Assets/_Game/Settings/BossConfig.asset");

            // 3. Create Lighting and Camera
            SetupEnvironment();

            // 4. Create GameManager
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj == null) gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.GetComponent<GameManager>();
            if (gm == null) gm = gmObj.AddComponent<GameManager>();
            var gmConfigField = typeof(GameManager).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gmConfigField != null) gmConfigField.SetValue(gm, gameConfig);

            // 5. Create Zilar (Player)
            GameObject player = GameObject.Find("Zilar");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Zilar";
                player.transform.position = new Vector3(0, 1, -5);
            }
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            Rigidbody pRb = player.GetComponent<Rigidbody>();
            if (pRb == null) pRb = player.AddComponent<Rigidbody>();
            pRb.mass = 80f;
            pRb.linearDamping = 5f;
            pRb.angularDamping = 10f;
            pRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PlayerController pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl == null) pCtrl = player.AddComponent<PlayerController>();
            var pCtrlConfigField = typeof(PlayerController).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pCtrlConfigField != null) pCtrlConfigField.SetValue(pCtrl, gameConfig);

            PlayerStats pStats = player.GetComponent<PlayerStats>();
            if (pStats == null) pStats = player.AddComponent<PlayerStats>();
            var pStatsConfigField = typeof(PlayerStats).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pStatsConfigField != null) pStatsConfigField.SetValue(pStats, gameConfig);

            PlayerCombat pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat == null) pCombat = player.AddComponent<PlayerCombat>();
            var hitboxField = typeof(PlayerCombat).GetField("hitboxObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Player Hitbox
            Transform pHitboxTrans = player.transform.Find("PlayerHitbox");
            GameObject pHitbox = pHitboxTrans != null ? pHitboxTrans.gameObject : null;
            if (pHitbox == null)
            {
                pHitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pHitbox.name = "PlayerHitbox";
                pHitbox.transform.SetParent(player.transform);
                pHitbox.transform.localPosition = new Vector3(0, 0, 1);
                pHitbox.transform.localScale = new Vector3(1, 1, 1.5f);
                DestroyImmediate(pHitbox.GetComponent<MeshRenderer>());
                pHitbox.GetComponent<BoxCollider>().isTrigger = true;
                pHitbox.SetActive(false);
            }
            if (hitboxField != null) hitboxField.SetValue(pCombat, pHitbox);

            if (player.GetComponent<CombatLogger>() == null) player.AddComponent<CombatLogger>();
            var pInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pInput == null) pInput = player.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Game/Scripts/Player/NemesisInputActions.inputactions");
            if (inputAsset != null)
            {
                pInput.actions = inputAsset;
                pInput.defaultControlScheme = "Gamepad"; // Or KeyboardMouse based on your setup
                pInput.defaultActionMap = "Player";
            }
            else
            {
                Debug.LogWarning("NemesisInputActions not found at Assets/_Game/Scripts/Player/NemesisInputActions.inputactions");
            }

            // 6. Create Humbaba (Boss)
            GameObject boss = GameObject.Find("Humbaba");
            if (boss == null)
            {
                boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                boss.name = "Humbaba";
                boss.transform.position = new Vector3(0, 1.5f, 5);
                boss.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }
            boss.tag = "Boss";
            boss.layer = LayerMask.NameToLayer("Default");

            Rigidbody bRb = boss.GetComponent<Rigidbody>();
            if (bRb == null) bRb = boss.AddComponent<Rigidbody>();
            bRb.isKinematic = true;

            BossStats bStats = boss.GetComponent<BossStats>();
            if (bStats == null) bStats = boss.AddComponent<BossStats>();
            
            BossController bCtrl = boss.GetComponent<BossController>();
            if (bCtrl == null) bCtrl = boss.AddComponent<BossController>();
            var bCtrlStatsField = typeof(BossController).GetField("bossStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bCtrlConfigField = typeof(BossController).GetField("bossConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bCtrlStatsField != null) bCtrlStatsField.SetValue(bCtrl, bStats);
            if (bCtrlConfigField != null) bCtrlConfigField.SetValue(bCtrl, bossConfig);

            if (boss.GetComponent<NemesisWeightReceiver>() == null) boss.AddComponent<NemesisWeightReceiver>();

            // Boss Hitbox
            Transform bHitboxTrans = boss.transform.Find("Hitbox_PUNISH_RIGHT_DODGE");
            GameObject bHitbox = bHitboxTrans != null ? bHitboxTrans.gameObject : null;
            if (bHitbox == null)
            {
                bHitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bHitbox.name = "Hitbox_PUNISH_RIGHT_DODGE";
                bHitbox.transform.SetParent(boss.transform);
                bHitbox.transform.localPosition = new Vector3(0, 0, 1.5f);
                bHitbox.transform.localScale = new Vector3(2, 2, 2);
                DestroyImmediate(bHitbox.GetComponent<MeshRenderer>());
                bHitbox.GetComponent<BoxCollider>().isTrigger = true;
                bHitbox.SetActive(false);
            }

            // Wire lock-on
            pCtrl.SetLockOnTarget(boss.transform);

            // 7. UI Setup
            SetupUI(pStats);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("✅ BossFight Scene successfully populated! Open Unity to view.");
        }

        private static void CreateFolders()
        {
            string[] folders = { "Assets/_Game/Settings", "Assets/_Game/Scripts/Nemesis" };
            foreach (var f in folders)
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = f.Substring(0, f.LastIndexOf('/'));
                    string newFolder = f.Substring(f.LastIndexOf('/') + 1);
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
            }
        }

        private static T GetOrCreateConfig<T>(string path) where T : ScriptableObject
        {
            T config = AssetDatabase.LoadAssetAtPath<T>(path);
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
            if (GameObject.Find("Main Camera") == null && Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.transform.position = new Vector3(0, 5, -12);
                camObj.transform.rotation = Quaternion.Euler(15, 0, 0);
            }

            if (GameObject.Find("Directional Light") == null && FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            if (GameObject.Find("Floor") == null)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.localScale = new Vector3(10, 1, 10);
            }
            
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private static void SetupUI(PlayerStats playerStats)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject statsUIObj = GameObject.Find("PlayerStatsUI");
            if (statsUIObj == null)
            {
                statsUIObj = new GameObject("PlayerStatsUI");
                statsUIObj.transform.SetParent(canvasObj.transform, false);
                PlayerStatsUI statsUI = statsUIObj.AddComponent<PlayerStatsUI>();
                
                var pStatsField = typeof(PlayerStatsUI).GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pStatsField != null) pStatsField.SetValue(statsUI, playerStats);
            }
        }
    }
}
#endif
