#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Nemesis.Editor
{
    public class InspectAnimations
    {
        [MenuItem("Nemesis/Inspect/List Model Animations")]
        public static void ListAnimations()
        {
            string[] models = {
                "Assets/_Game/Models/Player/Base mesh/Skin_1.fbx",
                "Assets/_Game/Models/Player/Base mesh/Skin_2.fbx",
                "Assets/_Game/Models/Player/Base mesh/Skin_3.fbx",
                "Assets/_Game/Models/Boss/stage_1.fbx",
                "Assets/_Game/Models/Boss/stage_2.fbx",
                "Assets/_Game/Models/Boss/stage_3.fbx"
            };

            foreach (var path in models)
            {
                Debug.Log($"--- Inspecting: {path} ---");
                var representations = AssetDatabase.LoadAllAssetsAtPath(path);
                int clipCount = 0;
                foreach (var rep in representations)
                {
                    if (rep is AnimationClip clip)
                    {
                        Debug.Log($"   Clip found: {clip.name} (Duration: {clip.length}s)");
                        clipCount++;
                    }
                }
                if (clipCount == 0)
                {
                    Debug.Log("   No AnimationClips found inside this FBX.");
                }
            }
        }
    }
}
#endif
