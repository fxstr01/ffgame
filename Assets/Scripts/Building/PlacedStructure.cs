using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Data;

namespace DestructionRoyale.Building
{
    public class PlacedStructure : NetworkBehaviour
    {
        [Header("Structure Settings")]
        [SerializeField] private float maxHealth = 200f;

        private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> decayTimer = new NetworkVariable<float>(
            120f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private BuildableData buildableData;
        private MeshRenderer meshRenderer;

        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth;
        public float HealthPercent => currentHealth.Value / maxHealth;
        public float RemainingDecayTime => decayTimer.Value;
        public ulong OwnerId => ownerId.Value;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(BuildableData data, ulong owner)
        {
            buildableData = data;
            maxHealth = data.maxHealth;

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                decayTimer.Value = data.decayTime;
                ownerId.Value = owner;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            decayTimer.Value -= Time.deltaTime;
            if (decayTimer.Value <= 0f)
            {
                DestroyStructure();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            currentHealth.Value = Mathf.Max(0f, currentHealth.Value - damage);

            if (currentHealth.Value <= 0f)
            {
                DestroyStructure();
            }

            UpdateVisualsClientRpc(currentHealth.Value / maxHealth);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RepairServerRpc(float amount)
        {
            currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + amount);
            UpdateVisualsClientRpc(currentHealth.Value / maxHealth);
        }

        [ClientRpc]
        private void UpdateVisualsClientRpc(float healthPercent)
        {
            if (meshRenderer != null)
            {
                Color baseColor = Color.white;
                if (healthPercent < 0.3f)
                    baseColor = new Color(1f, 0.3f, 0.3f);
                else if (healthPercent < 0.7f)
                    baseColor = new Color(1f, 0.8f, 0.5f);

                meshRenderer.material.color = baseColor;
            }
        }

        private void DestroyStructure()
        {
            DestroyEffectsClientRpc();
            GetComponent<NetworkObject>()?.Despawn();
        }

        [ClientRpc]
        private void DestroyEffectsClientRpc()
        {
            // Spawn break particles
        }
    }
}
