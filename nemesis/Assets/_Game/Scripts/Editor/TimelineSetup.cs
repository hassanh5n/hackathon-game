#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;

namespace Nemesis.Editor
{
    /// <summary>
    /// Editor tool that creates HeroIntro and BossIntro Timeline assets
    /// and wires them into the BossFight scene with PlayableDirectors.
    /// 
    /// Usage: Unity menu → Nemesis → Setup → 3. Create Timeline Assets
    /// </summary>
    public class TimelineSetup
    {
        private const string TIMELINE_FOLDER = "Assets/_Game/Timelines";

        [MenuItem("Nemesis/Setup/3. Create Timeline Assets")]
        public static void CreateAllTimelines()
        {
            EnsureFolder(TIMELINE_FOLDER);

            TimelineAsset heroIntro = CreateHeroIntroTimeline();
            TimelineAsset bossIntro = CreateBossIntroTimeline();
            TimelineAsset victoryTimeline = CreateVictoryTimeline();

            WireTimelinesInScene(heroIntro, bossIntro, victoryTimeline);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ All Timeline assets created and wired into scene!");
            Debug.Log($"   HeroIntro:  {TIMELINE_FOLDER}/HeroIntro.playable");
            Debug.Log($"   BossIntro:  {TIMELINE_FOLDER}/BossIntro.playable");
            Debug.Log($"   Victory:    {TIMELINE_FOLDER}/Victory.playable");
        }

        // ─────────────────────────────────────────────
        // HERO INTRO TIMELINE  (GDD §5.1 — ~12 seconds)
        // ─────────────────────────────────────────────
        private static TimelineAsset CreateHeroIntroTimeline()
        {
            string path = $"{TIMELINE_FOLDER}/HeroIntro.playable";
            TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing != null)
            {
                Debug.Log("[TimelineSetup] HeroIntro.playable already exists. Skipping creation.");
                return existing;
            }

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "HeroIntro";

            // Track 1: Activation track — controls Player visibility/activation
            ActivationTrack playerActivation = timeline.CreateTrack<ActivationTrack>(null, "Player Activation");
            // Clip: Player hidden for first 2 seconds, then visible
            TimelineClip hideClip = playerActivation.CreateDefaultClip();
            hideClip.displayName = "Player Hidden";
            hideClip.start = 0;
            hideClip.duration = 2;

            // Track 2: Animation track placeholder for camera animation
            AnimationTrack cameraTrack = timeline.CreateTrack<AnimationTrack>(null, "Camera Animation");
            cameraTrack.infiniteClipOffsetPosition = new Vector3(15f, 8f, -10f);

            // Track 3: Activation track — controls subtitle/title objects
            ActivationTrack subtitleTrack = timeline.CreateTrack<ActivationTrack>(null, "Subtitle Display");
            // Clip: Show subtitle from 6s to 9.5s
            TimelineClip subtitleClip = subtitleTrack.CreateDefaultClip();
            subtitleClip.displayName = "Story Subtitle";
            subtitleClip.start = 6;
            subtitleClip.duration = 3.5;

            // Track 4: Activation track — Title card
            ActivationTrack titleTrack = timeline.CreateTrack<ActivationTrack>(null, "Title Card");
            TimelineClip titleClip = titleTrack.CreateDefaultClip();
            titleClip.displayName = "NEMESIS Title";
            titleClip.start = 9.5;
            titleClip.duration = 3;

            // Set timeline duration
            timeline.fixedDuration = 13;

            AssetDatabase.CreateAsset(timeline, path);
            Debug.Log($"[TimelineSetup] Created HeroIntro Timeline at {path}");
            return timeline;
        }

        // ─────────────────────────────────────────────
        // BOSS INTRO TIMELINE  (GDD §5.2 — ~8 seconds first, ~4 seconds retry)
        // ─────────────────────────────────────────────
        private static TimelineAsset CreateBossIntroTimeline()
        {
            string path = $"{TIMELINE_FOLDER}/BossIntro.playable";
            TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing != null)
            {
                Debug.Log("[TimelineSetup] BossIntro.playable already exists. Skipping creation.");
                return existing;
            }

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BossIntro";

            // Track 1: Boss activation (ensure boss is visible)
            ActivationTrack bossActivation = timeline.CreateTrack<ActivationTrack>(null, "Boss Activation");
            TimelineClip bossClip = bossActivation.CreateDefaultClip();
            bossClip.displayName = "Boss Visible";
            bossClip.start = 0;
            bossClip.duration = 8;

            // Track 2: Camera orbit track placeholder
            AnimationTrack cameraOrbit = timeline.CreateTrack<AnimationTrack>(null, "Camera Orbit");
            cameraOrbit.infiniteClipOffsetPosition = new Vector3(0f, 4f, -8f);

            // Track 3: Boss name title
            ActivationTrack nameTrack = timeline.CreateTrack<ActivationTrack>(null, "Boss Name Title");
            TimelineClip nameClip = nameTrack.CreateDefaultClip();
            nameClip.displayName = "HUMBABA Title";
            nameClip.start = 1.5;
            nameClip.duration = 5;

            // Track 4: Taunt display (used on retries)
            ActivationTrack tauntTrack = timeline.CreateTrack<ActivationTrack>(null, "Taunt Display");
            TimelineClip tauntClip = tauntTrack.CreateDefaultClip();
            tauntClip.displayName = "Taunt Subtitle";
            tauntClip.start = 0.8;
            tauntClip.duration = 3;

            timeline.fixedDuration = 8;

            AssetDatabase.CreateAsset(timeline, path);
            Debug.Log($"[TimelineSetup] Created BossIntro Timeline at {path}");
            return timeline;
        }

        // ─────────────────────────────────────────────
        // VICTORY TIMELINE  (GDD §5.4 — ~22 seconds)
        // ─────────────────────────────────────────────
        private static TimelineAsset CreateVictoryTimeline()
        {
            string path = $"{TIMELINE_FOLDER}/Victory.playable";
            TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing != null)
            {
                Debug.Log("[TimelineSetup] Victory.playable already exists. Skipping creation.");
                return existing;
            }

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "Victory";

            // Track 1: Boss activation (stays visible then deactivates at dissolve)
            ActivationTrack bossTrack = timeline.CreateTrack<ActivationTrack>(null, "Boss Dissolve");
            TimelineClip bossClip = bossTrack.CreateDefaultClip();
            bossClip.displayName = "Boss Visible Then Dissolve";
            bossClip.start = 0;
            bossClip.duration = 10; // Boss disappears at ~10s

            // Track 2: Camera animation placeholder
            AnimationTrack camTrack = timeline.CreateTrack<AnimationTrack>(null, "Victory Camera");

            // Track 3: Humbaba dialogue subtitle
            ActivationTrack dialogueTrack = timeline.CreateTrack<ActivationTrack>(null, "Boss Dialogue");
            TimelineClip dialogueClip = dialogueTrack.CreateDefaultClip();
            dialogueClip.displayName = "Final Words";
            dialogueClip.start = 4;
            dialogueClip.duration = 4;

            // Track 4: Forest parts text
            ActivationTrack forestTrack = timeline.CreateTrack<ActivationTrack>(null, "Forest Path Text");
            TimelineClip forestClip = forestTrack.CreateDefaultClip();
            forestClip.displayName = "Golden Path";
            forestClip.start = 10;
            forestClip.duration = 3;

            // Track 5: Reunion text
            ActivationTrack reunionTrack = timeline.CreateTrack<ActivationTrack>(null, "Reunion");
            TimelineClip reunionClip = reunionTrack.CreateDefaultClip();
            reunionClip.displayName = "Daughter Reunion";
            reunionClip.start = 13;
            reunionClip.duration = 3.5;

            // Track 6: Credits title
            ActivationTrack creditsTrack = timeline.CreateTrack<ActivationTrack>(null, "Credits Title");
            TimelineClip creditsClip = creditsTrack.CreateDefaultClip();
            creditsClip.displayName = "NEMESIS Credits";
            creditsClip.start = 17;
            creditsClip.duration = 3;

            timeline.fixedDuration = 22;

            AssetDatabase.CreateAsset(timeline, path);
            Debug.Log($"[TimelineSetup] Created Victory Timeline at {path}");
            return timeline;
        }

        // ─────────────────────────────────────────────
        // SCENE WIRING
        // ─────────────────────────────────────────────
        private static void WireTimelinesInScene(TimelineAsset heroIntro, TimelineAsset bossIntro, TimelineAsset victory)
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            // Create or find the Timeline holder objects
            SetupDirector("HeroIntroDirector", heroIntro, false);
            SetupDirector("BossIntroDirector", bossIntro, false);
            SetupDirector("VictoryDirector", victory, false);

            // Create subtitle UI objects that Timeline activation tracks can target
            CreateTimelineUI();

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static PlayableDirector SetupDirector(string name, TimelineAsset timeline, bool playOnAwake)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
            }

            PlayableDirector director = obj.GetComponent<PlayableDirector>();
            if (director == null)
            {
                director = obj.AddComponent<PlayableDirector>();
            }

            director.playableAsset = timeline;
            director.playOnAwake = playOnAwake;
            director.extrapolationMode = DirectorWrapMode.None;

            Debug.Log($"[TimelineSetup] PlayableDirector '{name}' configured.");
            return director;
        }

        private static void CreateTimelineUI()
        {
            // Create subtitle GameObjects that Timeline Activation tracks can show/hide
            // These are separate from CinematicDirector's auto-UI — they're for Timeline binding

            string[] uiNames = {
                "TL_StorySubtitle",      // "My daughter's soul..."
                "TL_TitleCard",          // "NEMESIS: THE ETERNAL GUARDIAN"
                "TL_BossNameTitle",      // "HUMBABA — GUARDIAN OF THE CEDAR"
                "TL_TauntSubtitle",      // Nemesis taunt text
                "TL_BossDialogue",       // "Ten thousand warriors..."
                "TL_ForestPathText",     // "The forest parts..."
                "TL_ReunionText",        // "He kneels..."
                "TL_CreditsTitle"        // Credits
            };

            string[] defaultTexts = {
                "My daughter's soul is inside the forest. One god stands between us.",
                "NEMESIS: THE ETERNAL GUARDIAN",
                "HUMBABA — GUARDIAN OF THE CEDAR",
                "The forest remembers every step you have taken.",
                "Ten thousand warriors. None fought for something real. You... are different.",
                "The forest parts. A golden path opens.",
                "He kneels. Takes her hand. She opens her eyes.",
                "NEMESIS: THE ETERNAL GUARDIAN"
            };

            // Find or create Canvas
            GameObject canvasObj = GameObject.Find("TimelineCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("TimelineCanvas");
                UnityEngine.Canvas canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 850;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            }

            for (int i = 0; i < uiNames.Length; i++)
            {
                GameObject existing = GameObject.Find(uiNames[i]);
                if (existing != null) continue;

                GameObject textObj = new GameObject(uiNames[i]);
                textObj.transform.SetParent(canvasObj.transform, false);

                UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
                text.text = defaultTexts[i];
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = uiNames[i].Contains("Title") ? 48 : 30;
                text.color = new Color(0.95f, 0.9f, 0.7f);
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;

                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.4f);
                rect.anchorMax = new Vector2(0.9f, 0.6f);
                rect.sizeDelta = Vector2.zero;

                textObj.SetActive(false); // Hidden by default — Timeline activates them
            }

            Debug.Log("[TimelineSetup] Timeline UI text objects created.");
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────
        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
