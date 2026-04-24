using UnityEngine;

namespace DestructionRoyale.Data
{
    public enum WeaponType
    {
        SMG,
        AssaultRifle,
        Shotgun,
        Sniper,
        LMG,
        ExplosiveLauncher
    }

    public enum FireMode
    {
        Automatic,
        SemiAutomatic,
        BurstFire
    }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "DestructionRoyale/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        public WeaponType weaponType;
        public Sprite weaponIcon;
        public GameObject weaponPrefab;

        [Header("Combat Stats")]
        public float damage = 18f;
        public float headshotMultiplier = 2f;
        public float fireRate = 8f;
        public FireMode fireMode = FireMode.Automatic;
        public int burstCount = 3;

        [Header("Shotgun")]
        public int pelletCount = 1;
        public float pelletSpread = 0f;

        [Header("Accuracy")]
        public float baseSpread = 0.02f;
        public float maxSpread = 0.08f;
        public float spreadIncreasePerShot = 0.01f;
        public float spreadRecoveryRate = 0.05f;
        public float aimSpreadMultiplier = 0.4f;

        [Header("Recoil")]
        public float recoilVertical = 1.5f;
        public float recoilHorizontal = 0.5f;
        public float recoilRecoverySpeed = 5f;

        [Header("Range")]
        public float maxRange = 100f;
        public float damageDropoffStart = 30f;
        public float damageDropoffEnd = 80f;
        public float minDamageMultiplier = 0.5f;

        [Header("Magazine")]
        public int magazineSize = 30;
        public int maxReserveAmmo = 120;
        public float reloadTime = 2f;

        [Header("Wall Destruction")]
        public float structureDamageMultiplier = 1f;

        [Header("Audio/Visual")]
        public GameObject muzzleFlashPrefab;
        public GameObject impactEffectPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;

        public float GetDamageAtDistance(float distance)
        {
            if (distance <= damageDropoffStart) return damage;
            if (distance >= damageDropoffEnd) return damage * minDamageMultiplier;

            float t = (distance - damageDropoffStart) / (damageDropoffEnd - damageDropoffStart);
            return Mathf.Lerp(damage, damage * minDamageMultiplier, t);
        }
    }
}
