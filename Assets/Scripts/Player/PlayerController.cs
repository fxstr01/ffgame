using UnityEngine;
using Unity.Netcode;

namespace DestructionRoyale.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float slideSpeed = 10f;
        [SerializeField] private float slideDuration = 0.6f;
        [SerializeField] private float slideCooldown = 1.5f;

        [Header("Vaulting")]
        [SerializeField] private float vaultCheckDistance = 1f;
        [SerializeField] private float vaultHeight = 1.5f;
        [SerializeField] private float vaultSpeed = 5f;
        [SerializeField] private LayerMask vaultLayerMask;

        [Header("References")]
        [SerializeField] private Transform cameraTarget;

        private CharacterController characterController;
        private Vector3 velocity;
        private float currentSpeed;
        private bool isSprinting;
        private bool isSliding;
        private float slideTimer;
        private float slideCooldownTimer;
        private bool isVaulting;
        private Vector3 vaultTarget;
        private bool isGrounded;

        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> networkIsSprinting = new NetworkVariable<bool>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool IsSprinting => isSprinting;
        public bool IsSliding => isSliding;
        public bool IsGrounded => isGrounded;
        public float CurrentSpeed => currentSpeed;
        public Transform CameraTarget => cameraTarget;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            if (cameraTarget == null)
            {
                cameraTarget = new GameObject("CameraTarget").transform;
                cameraTarget.SetParent(transform);
                cameraTarget.localPosition = new Vector3(0f, 1.6f, 0f);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            isGrounded = characterController.isGrounded;

            if (isVaulting)
            {
                ProcessVault();
                return;
            }

            HandleMovement();
            HandleJump();
            HandleSlide();
            HandleVaultCheck();
            ApplyGravity();

            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkIsSprinting.Value = isSprinting;
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            isSprinting = Input.GetKey(KeyCode.LeftShift) && vertical > 0 && !isSliding;
            currentSpeed = isSliding ? slideSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            if (isSliding)
            {
                moveDir = transform.forward;
            }

            characterController.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        private void HandleJump()
        {
            if (Input.GetButtonDown("Jump") && isGrounded && !isSliding)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
        }

        private void HandleSlide()
        {
            slideCooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.LeftControl) && isSprinting && !isSliding && slideCooldownTimer <= 0f)
            {
                isSliding = true;
                slideTimer = slideDuration;
                slideCooldownTimer = slideCooldown;
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f)
                {
                    isSliding = false;
                }
            }
        }

        private void HandleVaultCheck()
        {
            if (!Input.GetButtonDown("Jump") || !isGrounded) return;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, vaultCheckDistance, vaultLayerMask))
            {
                Vector3 topCheck = hit.point + Vector3.up * vaultHeight;
                if (Physics.Raycast(topCheck, Vector3.down, out RaycastHit topHit, vaultHeight, vaultLayerMask))
                {
                    if (topHit.point.y - transform.position.y <= vaultHeight)
                    {
                        isVaulting = true;
                        vaultTarget = topHit.point + transform.forward * 0.5f;
                        vaultTarget.y = topHit.point.y + 0.1f;
                    }
                }
            }
        }

        private void ProcessVault()
        {
            transform.position = Vector3.MoveTowards(transform.position, vaultTarget, vaultSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, vaultTarget) < 0.1f)
            {
                isVaulting = false;
            }
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        public void RotatePlayer(float yRotation)
        {
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
}
