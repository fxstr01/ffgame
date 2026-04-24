using UnityEngine;
using UnityEngine.UI;
using DestructionRoyale.Match;

namespace DestructionRoyale.UI
{
    public class MinimapController : MonoBehaviour
    {
        [Header("Minimap Settings")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private RectTransform playerIcon;
        [SerializeField] private RectTransform zoneCircle;

        [Header("Settings")]
        [SerializeField] private float minimapSize = 100f;
        [SerializeField] private float minimapHeight = 200f;
        [SerializeField] private float zoomLevel = 1f;

        private Transform playerTransform;
        private RenderTexture minimapTexture;

        private void Start()
        {
            SetupMinimapCamera();
        }

        private void SetupMinimapCamera()
        {
            if (minimapCamera == null)
            {
                GameObject camObj = new GameObject("MinimapCamera");
                minimapCamera = camObj.AddComponent<Camera>();
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = minimapSize;
                minimapCamera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
                minimapCamera.clearFlags = CameraClearFlags.SolidColor;
                minimapCamera.backgroundColor = new Color(0.1f, 0.15f, 0.1f, 1f);
            }

            minimapTexture = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = minimapTexture;

            if (minimapDisplay != null)
            {
                minimapDisplay.texture = minimapTexture;
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        private void LateUpdate()
        {
            if (playerTransform == null || minimapCamera == null) return;

            Vector3 camPos = playerTransform.position;
            camPos.y = minimapHeight;
            minimapCamera.transform.position = camPos;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapCamera.orthographicSize = minimapSize * zoomLevel;

            if (playerIcon != null)
            {
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
            }

            UpdateZoneCircle();
        }

        private void UpdateZoneCircle()
        {
            if (zoneCircle == null) return;

            SafeZoneController zone = FindFirstObjectByType<SafeZoneController>();
            if (zone == null) return;

            float zoneRadius = zone.CurrentRadius;
            float displayRadius = (zoneRadius / (minimapSize * zoomLevel * 2f)) * zoneCircle.parent.GetComponent<RectTransform>().rect.width;

            zoneCircle.sizeDelta = new Vector2(displayRadius * 2f, displayRadius * 2f);

            Vector3 offset = zone.ZoneCenter - (playerTransform != null ? playerTransform.position : Vector3.zero);
            float displayScale = zoneCircle.parent.GetComponent<RectTransform>().rect.width / (minimapSize * zoomLevel * 2f);
            zoneCircle.anchoredPosition = new Vector2(offset.x * displayScale, offset.z * displayScale);
        }

        public void SetZoom(float zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 0.5f, 3f);
        }

        private void OnDestroy()
        {
            if (minimapTexture != null)
            {
                minimapTexture.Release();
                Destroy(minimapTexture);
            }
        }
    }
}
