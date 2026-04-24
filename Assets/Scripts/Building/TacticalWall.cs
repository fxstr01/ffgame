using UnityEngine;
using Unity.Netcode;

namespace DestructionRoyale.Building
{
    public class TacticalWall : NetworkBehaviour
    {
        [Header("Wall Configuration")]
        [SerializeField] private float mainPanelWidth = 2f;
        [SerializeField] private float mainPanelHeight = 1.8f;
        [SerializeField] private float sideWingWidth = 0.5f;
        [SerializeField] private float sideWingAngle = 30f;
        [SerializeField] private float wallThickness = 0.1f;

        [Header("Health")]
        [SerializeField] private float mainPanelHealth = 300f;
        [SerializeField] private float sideWingHealth = 150f;

        [Header("Visuals")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material damagedMaterial;

        private NetworkVariable<float> mainHealth = new NetworkVariable<float>(
            300f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> leftWingHealth = new NetworkVariable<float>(
            150f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> rightWingHealth = new NetworkVariable<float>(
            150f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private GameObject mainPanel;
        private GameObject leftWing;
        private GameObject rightWing;

        public float MainHealthPercent => mainHealth.Value / mainPanelHealth;
        public float LeftWingHealthPercent => leftWingHealth.Value / sideWingHealth;
        public float RightWingHealthPercent => rightWingHealth.Value / sideWingHealth;

        public override void OnNetworkSpawn()
        {
            GenerateWallGeometry();

            if (IsServer)
            {
                mainHealth.Value = mainPanelHealth;
                leftWingHealth.Value = sideWingHealth;
                rightWingHealth.Value = sideWingHealth;
            }

            mainHealth.OnValueChanged += (_, _) => UpdateVisuals();
            leftWingHealth.OnValueChanged += (_, _) => UpdateVisuals();
            rightWingHealth.OnValueChanged += (_, _) => UpdateVisuals();
        }

        private void GenerateWallGeometry()
        {
            // Main center panel
            mainPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainPanel.name = "MainPanel";
            mainPanel.transform.SetParent(transform);
            mainPanel.transform.localPosition = Vector3.zero;
            mainPanel.transform.localScale = new Vector3(mainPanelWidth, mainPanelHeight, wallThickness);
            mainPanel.transform.localRotation = Quaternion.identity;
            SetMaterial(mainPanel, wallMaterial);

            // Left wing
            leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWing.name = "LeftWing";
            leftWing.transform.SetParent(transform);
            float wingOffset = mainPanelWidth / 2f + sideWingWidth / 2f * Mathf.Cos(sideWingAngle * Mathf.Deg2Rad);
            float wingDepth = sideWingWidth / 2f * Mathf.Sin(sideWingAngle * Mathf.Deg2Rad);
            leftWing.transform.localPosition = new Vector3(-wingOffset, 0f, -wingDepth);
            leftWing.transform.localScale = new Vector3(sideWingWidth, mainPanelHeight, wallThickness);
            leftWing.transform.localRotation = Quaternion.Euler(0f, -sideWingAngle, 0f);
            SetMaterial(leftWing, wallMaterial);

            // Right wing
            rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWing.name = "RightWing";
            rightWing.transform.SetParent(transform);
            rightWing.transform.localPosition = new Vector3(wingOffset, 0f, -wingDepth);
            rightWing.transform.localScale = new Vector3(sideWingWidth, mainPanelHeight, wallThickness);
            rightWing.transform.localRotation = Quaternion.Euler(0f, sideWingAngle, 0f);
            SetMaterial(rightWing, wallMaterial);
        }

        private void SetMaterial(GameObject obj, Material mat)
        {
            if (mat != null)
            {
                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = mat;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, int panelIndex)
        {
            switch (panelIndex)
            {
                case 0: // Main
                    mainHealth.Value = Mathf.Max(0f, mainHealth.Value - damage);
                    if (mainHealth.Value <= 0f) DestroyPanel(0);
                    break;
                case 1: // Left wing
                    leftWingHealth.Value = Mathf.Max(0f, leftWingHealth.Value - damage);
                    if (leftWingHealth.Value <= 0f) DestroyPanel(1);
                    break;
                case 2: // Right wing
                    rightWingHealth.Value = Mathf.Max(0f, rightWingHealth.Value - damage);
                    if (rightWingHealth.Value <= 0f) DestroyPanel(2);
                    break;
            }

            if (mainHealth.Value <= 0f && leftWingHealth.Value <= 0f && rightWingHealth.Value <= 0f)
            {
                GetComponent<NetworkObject>()?.Despawn();
            }
        }

        private void DestroyPanel(int panelIndex)
        {
            DestroyPanelClientRpc(panelIndex);
        }

        [ClientRpc]
        private void DestroyPanelClientRpc(int panelIndex)
        {
            GameObject target = panelIndex switch
            {
                0 => mainPanel,
                1 => leftWing,
                2 => rightWing,
                _ => null
            };

            if (target != null)
            {
                target.SetActive(false);
            }
        }

        private void UpdateVisuals()
        {
            UpdatePanelVisual(mainPanel, mainHealth.Value / mainPanelHealth);
            UpdatePanelVisual(leftWing, leftWingHealth.Value / sideWingHealth);
            UpdatePanelVisual(rightWing, rightWingHealth.Value / sideWingHealth);
        }

        private void UpdatePanelVisual(GameObject panel, float healthPercent)
        {
            if (panel == null) return;

            MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (healthPercent <= 0f)
            {
                panel.SetActive(false);
            }
            else if (healthPercent < 0.5f && damagedMaterial != null)
            {
                renderer.material = damagedMaterial;
            }
        }
    }
}
