#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nemesis.Editor
{
    public class BuildAnimatorControllers
    {
        [MenuItem("Nemesis/Setup/4. Build Animator Controllers")]
        public static void GenerateControllers()
        {
            EnsureFolder("Assets/_Game/Animations");
            EnsureFolder("Assets/_Game/Animations/Player");
            EnsureFolder("Assets/_Game/Animations/Boss");

            BuildPlayerController();
            BuildBossController();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Animator Controllers generated successfully!");
            Debug.Log("   Player Controller: Assets/_Game/Animations/Player/PlayerAnimator.controller");
            Debug.Log("   Boss Controller:   Assets/_Game/Animations/Boss/BossAnimator.controller");
            Debug.Log("💡 TIP: Open these in the Animator window to drag & drop your clips into the placeholder states!");
        }

        private static void BuildPlayerController()
        {
            string path = "Assets/_Game/Animations/Player/PlayerAnimator.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsDodging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsParrying", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("LightAttack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HeavyAttack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Parry", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Staggered", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Create States
            var locomotion = rootStateMachine.AddState("Locomotion");
            var crouch = rootStateMachine.AddState("Crouch");
            var lightAttack = rootStateMachine.AddState("LightAttack");
            var heavyAttack = rootStateMachine.AddState("HeavyAttack");
            var parry = rootStateMachine.AddState("Parry");
            var dodge = rootStateMachine.AddState("Dodge");
            var staggered = rootStateMachine.AddState("Staggered");
            var death = rootStateMachine.AddState("Death");

            rootStateMachine.defaultState = locomotion;

            // --- Transitions ---

            // Locomotion <-> Crouch
            var toCrouch = locomotion.AddTransition(crouch);
            toCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
            toCrouch.hasExitTime = false;

            var fromCrouch = crouch.AddTransition(locomotion);
            fromCrouch.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
            fromCrouch.hasExitTime = false;

            // Locomotion -> Attacks / Parry / Dodge
            var toLight = locomotion.AddTransition(lightAttack);
            toLight.AddCondition(AnimatorConditionMode.If, 0, "LightAttack");
            toLight.hasExitTime = false;

            var toHeavy = locomotion.AddTransition(heavyAttack);
            toHeavy.AddCondition(AnimatorConditionMode.If, 0, "HeavyAttack");
            toHeavy.hasExitTime = false;

            var toParry = locomotion.AddTransition(parry);
            toParry.AddCondition(AnimatorConditionMode.If, 0, "Parry");
            toParry.hasExitTime = false;

            var toDodge = locomotion.AddTransition(dodge);
            toDodge.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
            toDodge.hasExitTime = false;

            // Returns to Locomotion (using Exit Time when clip finishes)
            lightAttack.AddTransition(locomotion).hasExitTime = true;
            heavyAttack.AddTransition(locomotion).hasExitTime = true;
            parry.AddTransition(locomotion).hasExitTime = true;
            dodge.AddTransition(locomotion).hasExitTime = true;

            // Staggered & Death from Any State
            var toStaggered = rootStateMachine.AddAnyStateTransition(staggered);
            toStaggered.AddCondition(AnimatorConditionMode.If, 0, "Staggered");
            toStaggered.hasExitTime = false;

            staggered.AddTransition(locomotion).hasExitTime = true;

            var toDeath = rootStateMachine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
            toDeath.hasExitTime = false;
        }

        private static void BuildBossController()
        {
            string path = "Assets/_Game/Animations/Boss/BossAnimator.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Windup_HeavySwing", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Windup_Stomp", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Windup_Roar", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Windup_AoESlam", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Windup_Grapple", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Windup_CedarFireball", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PhaseTransition", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Create States
            var locomotion = rootStateMachine.AddState("Locomotion");
            var heavySwing = rootStateMachine.AddState("Attack_HeavySwing");
            var stomp = rootStateMachine.AddState("Attack_Stomp");
            var roar = rootStateMachine.AddState("Attack_Roar");
            var aoeSlam = rootStateMachine.AddState("Attack_AoESlam");
            var grapple = rootStateMachine.AddState("Attack_Grapple");
            var cedarFireball = rootStateMachine.AddState("Attack_CedarFireball");
            var phaseTransition = rootStateMachine.AddState("PhaseTransition");
            var death = rootStateMachine.AddState("Death");

            rootStateMachine.defaultState = locomotion;

            // Transitions to Attacks
            var attacks = new[]
            {
                new { name = "HeavySwing", state = heavySwing },
                new { name = "Stomp", state = stomp },
                new { name = "Roar", state = roar },
                new { name = "AoESlam", state = aoeSlam },
                new { name = "Grapple", state = grapple },
                new { name = "CedarFireball", state = cedarFireball }
            };

            foreach (var atk in attacks)
            {
                var toAtk = locomotion.AddTransition(atk.state);
                toAtk.AddCondition(AnimatorConditionMode.If, 0, $"Windup_{atk.name}");
                toAtk.hasExitTime = false;

                atk.state.AddTransition(locomotion).hasExitTime = true;
            }

            // Phase transition & Death from Any State
            var toPhase = rootStateMachine.AddAnyStateTransition(phaseTransition);
            toPhase.AddCondition(AnimatorConditionMode.If, 0, "PhaseTransition");
            toPhase.hasExitTime = false;

            phaseTransition.AddTransition(locomotion).hasExitTime = true;

            var toDeath = rootStateMachine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
            toDeath.hasExitTime = false;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
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
