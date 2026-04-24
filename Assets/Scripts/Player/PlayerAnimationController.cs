using UnityEngine;
using Unity.Netcode;

namespace DestructionRoyale.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : NetworkBehaviour
    {
        private Animator animator;
        private PlayerController playerController;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int ShootTriggerHash = Animator.StringToHash("Shoot");
        private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");

        private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (animator == null) return;

            if (IsOwner && playerController != null)
            {
                float speed = playerController.CurrentSpeed > 0.1f ? playerController.CurrentSpeed : 0f;
                networkSpeed.Value = speed;

                animator.SetFloat(SpeedHash, speed);
                animator.SetBool(IsGroundedHash, playerController.IsGrounded);
                animator.SetBool(IsSprintingHash, playerController.IsSprinting);
                animator.SetBool(IsSlidingHash, playerController.IsSliding);
            }
            else
            {
                animator.SetFloat(SpeedHash, networkSpeed.Value);
            }
        }

        public void PlayShootAnimation()
        {
            if (animator != null)
                animator.SetTrigger(ShootTriggerHash);
        }

        public void PlayReloadAnimation()
        {
            if (animator != null)
                animator.SetTrigger(ReloadTriggerHash);
        }

        public void PlayJumpAnimation()
        {
            if (animator != null)
                animator.SetTrigger(JumpTriggerHash);
        }

        public void SetAiming(bool isAiming)
        {
            if (animator != null)
                animator.SetBool(IsAimingHash, isAiming);
        }
    }
}
