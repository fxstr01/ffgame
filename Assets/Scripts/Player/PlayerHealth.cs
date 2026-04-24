using UnityEngine;
using Unity.Netcode;
using System;

namespace DestructionRoyale.Player
{
    public class PlayerHealth : NetworkBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxShield = 50f;

        private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> currentShield = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> isDead = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float CurrentHealth => currentHealth.Value;
        public float CurrentShield => currentShield.Value;
        public float MaxHealth => maxHealth;
        public float MaxShield => maxShield;
        public bool IsDead => isDead.Value;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnShieldChanged;
        public event Action<ulong> OnPlayerDied;
        public event Action OnPlayerRespawned;

        public override void OnNetworkSpawn()
        {
            currentHealth.OnValueChanged += HandleHealthChanged;
            currentShield.OnValueChanged += HandleShieldChanged;
            isDead.OnValueChanged += HandleDeathStateChanged;

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                currentShield.Value = 0f;
                isDead.Value = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= HandleHealthChanged;
            currentShield.OnValueChanged -= HandleShieldChanged;
            isDead.OnValueChanged -= HandleDeathStateChanged;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, ulong attackerClientId)
        {
            if (isDead.Value) return;

            float remainingDamage = damage;

            if (currentShield.Value > 0f)
            {
                float shieldDamage = Mathf.Min(currentShield.Value, remainingDamage);
                currentShield.Value -= shieldDamage;
                remainingDamage -= shieldDamage;
            }

            if (remainingDamage > 0f)
            {
                currentHealth.Value = Mathf.Max(0f, currentHealth.Value - remainingDamage);
            }

            if (currentHealth.Value <= 0f)
            {
                Die(attackerClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HealServerRpc(float amount)
        {
            if (isDead.Value) return;
            currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + amount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddShieldServerRpc(float amount)
        {
            if (isDead.Value) return;
            currentShield.Value = Mathf.Min(maxShield, currentShield.Value + amount);
        }

        private void Die(ulong killerClientId)
        {
            isDead.Value = true;
            OnPlayerDied?.Invoke(killerClientId);
            DiedClientRpc(killerClientId);
        }

        [ClientRpc]
        private void DiedClientRpc(ulong killerClientId)
        {
            OnPlayerDied?.Invoke(killerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespawnServerRpc()
        {
            currentHealth.Value = maxHealth;
            currentShield.Value = 0f;
            isDead.Value = false;
        }

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, maxHealth);
        }

        private void HandleShieldChanged(float oldValue, float newValue)
        {
            OnShieldChanged?.Invoke(newValue, maxShield);
        }

        private void HandleDeathStateChanged(bool oldValue, bool newValue)
        {
            if (!newValue)
            {
                OnPlayerRespawned?.Invoke();
            }
        }
    }
}
