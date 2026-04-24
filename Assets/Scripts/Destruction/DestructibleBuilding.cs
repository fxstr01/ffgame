using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DestructionRoyale.Destruction
{
    public class DestructibleBuilding : NetworkBehaviour
    {
        [Header("Building Configuration")]
        [SerializeField] private string buildingName = "House";
        [SerializeField] private List<DestructibleWall> walls = new List<DestructibleWall>();
        [SerializeField] private GameObject roofObject;
        [SerializeField] private GameObject floorObject;

        [Header("Collapse Settings")]
        [SerializeField] private float collapseThreshold = 0.7f;
        [SerializeField] private bool canFullyCollapse = true;
        [SerializeField] private GameObject collapseDebrisPrefab;

        private NetworkVariable<float> totalIntegrity = new NetworkVariable<float>(
            1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> hasCollapsed = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float TotalIntegrity => totalIntegrity.Value;
        public bool HasCollapsed => hasCollapsed.Value;
        public string BuildingName => buildingName;

        private void Start()
        {
            if (walls.Count == 0)
            {
                walls.AddRange(GetComponentsInChildren<DestructibleWall>());
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            UpdateIntegrity();
        }

        private void UpdateIntegrity()
        {
            if (walls.Count == 0 || hasCollapsed.Value) return;

            float totalDestruction = 0f;
            foreach (DestructibleWall wall in walls)
            {
                totalDestruction += wall.DestructionPercent;
            }
            totalIntegrity.Value = 1f - (totalDestruction / walls.Count);

            if (canFullyCollapse && totalIntegrity.Value <= 1f - collapseThreshold)
            {
                CollapseBuilding();
            }
        }

        private void CollapseBuilding()
        {
            hasCollapsed.Value = true;
            CollapseBuildingClientRpc();
        }

        [ClientRpc]
        private void CollapseBuildingClientRpc()
        {
            if (roofObject != null)
            {
                Rigidbody roofRb = roofObject.AddComponent<Rigidbody>();
                roofRb.mass = 50f;
                roofRb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
                Destroy(roofObject, 5f);
            }

            if (collapseDebrisPrefab != null)
            {
                Instantiate(collapseDebrisPrefab, transform.position, Quaternion.identity);
            }
        }

        public List<DestructibleWall> GetWalls()
        {
            return walls;
        }

        public DestructibleWall GetMostDamagedWall()
        {
            DestructibleWall mostDamaged = null;
            float highestDamage = 0f;

            foreach (DestructibleWall wall in walls)
            {
                if (wall.DestructionPercent > highestDamage)
                {
                    highestDamage = wall.DestructionPercent;
                    mostDamaged = wall;
                }
            }

            return mostDamaged;
        }
    }
}
