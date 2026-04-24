using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Data;
using DestructionRoyale.Player;
using DestructionRoyale.Destruction;

namespace DestructionRoyale.Weapons
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform grenadeSpawnPoint;
        [SerializeField] private GameObject grenadePrefab;

        [Header("Grenade Settings")]
        [SerializeField] private float grenadeThrowForce = 20f;
        [SerializeField] private float grenadeCooldown = 5f;
        [SerializeField] private int maxGrenades = 3;

        private PlayerInventory playerInventory;
        private ThirdPersonCamera thirdPersonCamera;

        private float nextFireTime;
        private float currentSpread;
        private bool isShooting;
        private bool isAiming;
        private bool isReloading;
        private float reloadTimer;
        private int currentAmmo;
        private int reserveAmmo;
        private int currentGrenades;
        private float grenadeCooldownTimer;

        private NetworkVariable<bool> networkIsAiming = new NetworkVariable<bool>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool IsAiming => isAiming;
        public bool IsReloading => isReloading;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int CurrentGrenades => currentGrenades;
        public float ReloadProgress => isReloading ? 1f - (reloadTimer / GetCurrentWeaponData()?.reloadTime ?? 1f) : 0f;

        public event System.Action OnShot;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadComplete;
        public event System.Action OnGrenadeThrown;

        private void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
            currentGrenades = maxGrenades;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
            playerInventory.OnWeaponSwitched += HandleWeaponSwitch;
        }

        public override void OnNetworkDespawn()
        {
            if (playerInventory != null)
                playerInventory.OnWeaponSwitched -= HandleWeaponSwitch;
        }

        private void Update()
        {
            if (!IsOwner) return;

            UpdateReload();
            UpdateSpread();

            grenadeCooldownTimer -= Time.deltaTime;
            networkIsAiming.Value = isAiming;
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        public void TryShoot()
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon == null || isReloading) return;

            if (currentAmmo <= 0)
            {
                Reload();
                return;
            }

            if (Time.time < nextFireTime) return;

            nextFireTime = Time.time + (1f / weapon.fireRate);
            isShooting = true;

            if (weapon.pelletCount > 1)
            {
                for (int i = 0; i < weapon.pelletCount; i++)
                {
                    FireProjectile(weapon, weapon.pelletSpread);
                }
            }
            else
            {
                FireProjectile(weapon, 0f);
            }

            currentAmmo--;
            currentSpread = Mathf.Min(currentSpread + weapon.spreadIncreasePerShot, weapon.maxSpread);

            ApplyRecoil(weapon);
            OnShot?.Invoke();
            FireEffectsServerRpc();
        }

        public void StopShooting()
        {
            isShooting = false;
        }

        private void FireProjectile(WeaponData weapon, float additionalSpread)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = cam.ScreenPointToRay(screenCenter);

            float spreadAmount = currentSpread + additionalSpread;
            if (isAiming) spreadAmount *= weapon.aimSpreadMultiplier;

            Vector3 spreadDir = ray.direction;
            spreadDir += new Vector3(
                Random.Range(-spreadAmount, spreadAmount),
                Random.Range(-spreadAmount, spreadAmount),
                Random.Range(-spreadAmount, spreadAmount)
            );

            if (Physics.Raycast(ray.origin, spreadDir.normalized, out RaycastHit hit, weapon.maxRange))
            {
                float damage = weapon.GetDamageAtDistance(hit.distance);

                PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null)
                {
                    bool isHeadshot = hit.collider.CompareTag("Head");
                    float finalDamage = isHeadshot ? damage * weapon.headshotMultiplier : damage;
                    targetHealth.TakeDamageServerRpc(finalDamage, OwnerClientId);
                }

                WallSegment wallSegment = hit.collider.GetComponent<WallSegment>();
                if (wallSegment != null)
                {
                    float structureDamage = damage * weapon.structureDamageMultiplier;
                    wallSegment.TakeDamageServerRpc(structureDamage);
                }

                SpawnImpactServerRpc(hit.point, hit.normal);
            }
        }

        private void ApplyRecoil(WeaponData weapon)
        {
            if (thirdPersonCamera != null)
            {
                float verticalRecoil = weapon.recoilVertical * Random.Range(0.7f, 1.3f);
                float horizontalRecoil = weapon.recoilHorizontal * Random.Range(-1f, 1f);

                if (isAiming)
                {
                    verticalRecoil *= 0.6f;
                    horizontalRecoil *= 0.6f;
                }

                thirdPersonCamera.AddRecoil(verticalRecoil, horizontalRecoil);
            }
        }

        public void Reload()
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon == null || isReloading) return;
            if (currentAmmo >= weapon.magazineSize || reserveAmmo <= 0) return;

            isReloading = true;
            reloadTimer = weapon.reloadTime;
            OnReloadStart?.Invoke();
        }

        private void UpdateReload()
        {
            if (!isReloading) return;

            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                CompleteReload();
            }
        }

        private void CompleteReload()
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon == null) return;

            int ammoNeeded = weapon.magazineSize - currentAmmo;
            int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToLoad;
            reserveAmmo -= ammoToLoad;
            isReloading = false;

            OnReloadComplete?.Invoke();
        }

        private void UpdateSpread()
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon == null) return;

            if (!isShooting)
            {
                currentSpread = Mathf.Max(weapon.baseSpread,
                    currentSpread - weapon.spreadRecoveryRate * Time.deltaTime);
            }
        }

        public void ThrowGrenade()
        {
            if (currentGrenades <= 0 || grenadeCooldownTimer > 0f) return;
            if (grenadePrefab == null) return;

            currentGrenades--;
            grenadeCooldownTimer = grenadeCooldown;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 throwDir = cam.transform.forward;
            Vector3 spawnPos = grenadeSpawnPoint != null ? grenadeSpawnPoint.position : transform.position + Vector3.up + transform.forward;

            ThrowGrenadeServerRpc(spawnPos, throwDir);
            OnGrenadeThrown?.Invoke();
        }

        [ServerRpc]
        private void ThrowGrenadeServerRpc(Vector3 position, Vector3 direction)
        {
            GameObject grenade = Instantiate(grenadePrefab, position, Quaternion.LookRotation(direction));
            grenade.GetComponent<NetworkObject>()?.Spawn();

            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * grenadeThrowForce + Vector3.up * 5f;
            }
        }

        [ServerRpc]
        private void FireEffectsServerRpc()
        {
            FireEffectsClientRpc();
        }

        [ClientRpc]
        private void FireEffectsClientRpc()
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon == null) return;

            if (weapon.muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(weapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }
        }

        [ServerRpc]
        private void SpawnImpactServerRpc(Vector3 position, Vector3 normal)
        {
            SpawnImpactClientRpc(position, normal);
        }

        [ClientRpc]
        private void SpawnImpactClientRpc(Vector3 position, Vector3 normal)
        {
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon?.impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(weapon.impactEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(impact, 2f);
            }
        }

        private void HandleWeaponSwitch(int newIndex)
        {
            isReloading = false;
            isShooting = false;
            currentSpread = 0f;

            WeaponData weapon = GetCurrentWeaponData();
            if (weapon != null)
            {
                currentAmmo = weapon.magazineSize;
                reserveAmmo = weapon.maxReserveAmmo;
            }
        }

        private WeaponData GetCurrentWeaponData()
        {
            return playerInventory?.GetCurrentWeapon();
        }

        public void PickupAmmo(int amount)
        {
            reserveAmmo += amount;
            WeaponData weapon = GetCurrentWeaponData();
            if (weapon != null)
            {
                reserveAmmo = Mathf.Min(reserveAmmo, weapon.maxReserveAmmo);
            }
        }
    }
}
