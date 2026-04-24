using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Data;

namespace DestructionRoyale.Resources
{
    public class HarvestableResource : NetworkBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private int totalYield = 50;
        [SerializeField] private int yieldPerHit = 10;
        [SerializeField] private float harvestTime = 0.5f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Visual")]
        [SerializeField] private float scaleReductionPerHit = 0.1f;
        [SerializeField] private GameObject harvestEffectPrefab;
        [SerializeField] private GameObject depletedEffectPrefab;

        private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> remainingYield = new NetworkVariable<int>(
            50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> isDepleted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Vector3 originalScale;

        public ResourceType Type => resourceType;
        public bool IsDepleted => isDepleted.Value;
        public float HarvestTime => harvestTime;
        public float HealthPercent => currentHealth.Value / maxHealth;

        public override void OnNetworkSpawn()
        {
            originalScale = transform.localScale;

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                remainingYield.Value = totalYield;
            }

            currentHealth.OnValueChanged += HandleHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= HandleHealthChanged;
        }

        [ServerRpc(RequireOwnership = false)]
        public void HarvestServerRpc(ulong harvesterId)
        {
            if (isDepleted.Value) return;

            float damage = (maxHealth / totalYield) * yieldPerHit;
            currentHealth.Value = Mathf.Max(0f, currentHealth.Value - damage);

            int yield = Mathf.Min(yieldPerHit, remainingYield.Value);
            remainingYield.Value -= yield;

            // Grant resources to harvester
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(harvesterId, out var client))
            {
                Player.PlayerInventory inventory = client.PlayerObject?.GetComponent<Player.PlayerInventory>();
                inventory?.AddResourceServerRpc(resourceType, yield);
            }

            HarvestEffectClientRpc();

            if (currentHealth.Value <= 0f || remainingYield.Value <= 0)
            {
                isDepleted.Value = true;
                DepletedClientRpc();
            }
        }

        [ClientRpc]
        private void HarvestEffectClientRpc()
        {
            float healthPercent = currentHealth.Value / maxHealth;
            transform.localScale = originalScale * (0.3f + 0.7f * healthPercent);

            if (harvestEffectPrefab != null)
            {
                GameObject effect = Instantiate(harvestEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }

        [ClientRpc]
        private void DepletedClientRpc()
        {
            if (depletedEffectPrefab != null)
            {
                Instantiate(depletedEffectPrefab, transform.position, Quaternion.identity);
            }

            gameObject.SetActive(false);
        }

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            float healthPercent = newValue / maxHealth;
            transform.localScale = originalScale * (0.3f + 0.7f * healthPercent);
        }
    }
}
