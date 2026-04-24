using UnityEngine;

namespace DestructionRoyale.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Camera Settings")]
        [SerializeField] private float normalDistance = 3.5f;
        [SerializeField] private float aimDistance = 1.5f;
        [SerializeField] private Vector3 normalOffset = new Vector3(0.5f, 0f, 0f);
        [SerializeField] private Vector3 aimOffset = new Vector3(0.8f, 0.2f, 0f);
        [SerializeField] private float transitionSpeed = 10f;

        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = -40f;
        [SerializeField] private float maxVerticalAngle = 60f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float collisionSmoothing = 10f;

        private float currentX;
        private float currentY;
        private float currentDistance;
        private float targetDistance;
        private bool isAiming;

        public bool IsAiming => isAiming;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentDistance = normalDistance;
            targetDistance = normalDistance;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdateCamera();
        }

        private void HandleInput()
        {
            currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

            isAiming = Input.GetMouseButton(1);
        }

        private void UpdateCamera()
        {
            targetDistance = isAiming ? aimDistance : normalDistance;
            Vector3 offset = isAiming ? aimOffset : normalOffset;

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * transitionSpeed);

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
            Vector3 desiredPosition = target.position + rotation * (offset + Vector3.back * currentDistance);

            float actualDistance = currentDistance;
            Vector3 direction = desiredPosition - target.position;

            if (Physics.SphereCast(target.position, collisionRadius, direction.normalized,
                out RaycastHit hit, direction.magnitude, collisionMask))
            {
                actualDistance = hit.distance - collisionRadius;
                actualDistance = Mathf.Max(actualDistance, 0.3f);
            }

            Vector3 finalPosition = target.position + rotation * (offset + Vector3.back * actualDistance);

            transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * collisionSmoothing);
            transform.rotation = rotation;
        }

        public float GetYRotation()
        {
            return currentX;
        }

        public void AddRecoil(float vertical, float horizontal)
        {
            currentY -= vertical;
            currentX += horizontal;
        }
    }
}
