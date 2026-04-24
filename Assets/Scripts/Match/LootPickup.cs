using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Data;
using DestructionRoyale.Player;
using DestructionRoyale.Weapons;

namespace DestructionRoyale.Match
{
    public enum PickupType
    {
        Weapon,
        Ammo,
        Health,
        Shield,
        Resource,
        Grenade
    }

    public class LootPickup : NetworkBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private PickupType pickupType;
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int amount = 1;
        [SerializeField] private float healAmount = 25f;
        [SerializeField] private float shieldAmount = 25f;

        [Header("Visual")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float rotateSpeed = 90f;
        [SerializeField] private GameObject glowEffect;

        private Vector3 startPosition;
        private bool isPickedUp;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || isPickedUp) return;

            NetworkObject playerNetObj = other.GetComponentInParent<NetworkObject>();
            if (playerNetObj == null) return;

            bool success = false;

            switch (pickupType)
            {
                case PickupType.Weapon:
                    PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
                    if (inventory != null && weaponData != null)
                    {
                        success = inventory.AddWeapon(weaponData);
                    }
                    break;

                case PickupType.Ammo:
                    WeaponController weaponCtrl = other.GetComponentInParent<WeaponController>();
                    if (weaponCtrl != null)
                    {
                        weaponCtrl.PickupAmmo(amount);
                        success = true;
                    }
                    break;

                case PickupType.Health:
                    PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
                    if (health != null && health.CurrentHealth < health.MaxHealth)
                    {
                        health.HealServerRpc(healAmount);
                        success = true;
                    }
                    break;

                case PickupType.Shield:
                    PlayerHealth shieldHealth = other.GetComponentInParent<PlayerHealth>();
                    if (shieldHealth != null && shieldHealth.CurrentShield < shieldHealth.MaxShield)
                    {
                        shieldHealth.AddShieldServerRpc(shieldAmount);
                        success = true;
                    }
                    break;

                case PickupType.Resource:
                    PlayerInventory resInventory = other.GetComponentInParent<PlayerInventory>();
                    if (resInventory != null)
                    {
                        resInventory.AddResourceServerRpc(resourceType, amount);
                        success = true;
                    }
                    break;

                case PickupType.Grenade:
                    success = true;
                    break;
            }

            if (success)
            {
                isPickedUp = true;
                PickupEffectClientRpc();
                GetComponent<NetworkObject>()?.Despawn();
            }
        }

        [ClientRpc]
        private void PickupEffectClientRpc()
        {
            // Pickup sound and visual effect
        }
    }
}
