#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nemesis.Player
{
    public class PlayerSetup
    {
        [MenuItem("Nemesis/Setup/Create Player Placeholder")]
        public static void CreatePlayerPlaceholder()
        {
            // Create a Capsule GameObject named "Zilar"
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Zilar";

            // Add these components: Rigidbody, CapsuleCollider, Animator
            // CapsuleCollider is already added by CreatePrimitive
            Rigidbody rb = player.AddComponent<Rigidbody>();
            Animator animator = player.AddComponent<Animator>();

            // Rigidbody: mass=80, drag=5, angularDrag=10, freezeRotation=true on X and Z axes
            rb.mass = 80f;
            rb.linearDamping = 5f;
            rb.angularDamping = 10f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Creates an empty AnimatorController asset
            string animFolder = "Assets/_Game/Animations/Player";
            if (!AssetDatabase.IsValidFolder("Assets/_Game")) AssetDatabase.CreateFolder("Assets", "_Game");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Animations")) AssetDatabase.CreateFolder("Assets/_Game", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Animations/Player")) AssetDatabase.CreateFolder("Assets/_Game/Animations", "Player");

            string animPath = animFolder + "/PlayerAnimator.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animPath);
            animator.runtimeAnimatorController = controller;

            // Positions Zilar at Vector3(0, 1, -5)
            player.transform.position = new Vector3(0f, 1f, -5f);

            // Tags it as "Player"
            player.tag = "Player";

            Debug.Log("Player placeholder 'Zilar' created successfully!");
        }
    }
}
#endif
