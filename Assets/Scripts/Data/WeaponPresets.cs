using UnityEngine;

namespace DestructionRoyale.Data
{
    public static class WeaponPresets
    {
        public static void ConfigureSMG(WeaponData data)
        {
            data.weaponName = "Vector SMG";
            data.weaponType = WeaponType.SMG;
            data.damage = 14f;
            data.fireRate = 12f;
            data.fireMode = FireMode.Automatic;
            data.baseSpread = 0.03f;
            data.maxSpread = 0.1f;
            data.recoilVertical = 0.8f;
            data.recoilHorizontal = 0.6f;
            data.maxRange = 50f;
            data.damageDropoffStart = 15f;
            data.damageDropoffEnd = 40f;
            data.magazineSize = 35;
            data.maxReserveAmmo = 140;
            data.reloadTime = 1.8f;
            data.structureDamageMultiplier = 0.6f;
            data.headshotMultiplier = 1.8f;
        }

        public static void ConfigureAR(WeaponData data)
        {
            data.weaponName = "Havoc AR";
            data.weaponType = WeaponType.AssaultRifle;
            data.damage = 22f;
            data.fireRate = 8f;
            data.fireMode = FireMode.Automatic;
            data.baseSpread = 0.02f;
            data.maxSpread = 0.07f;
            data.recoilVertical = 1.5f;
            data.recoilHorizontal = 0.5f;
            data.maxRange = 100f;
            data.damageDropoffStart = 30f;
            data.damageDropoffEnd = 80f;
            data.magazineSize = 30;
            data.maxReserveAmmo = 120;
            data.reloadTime = 2.2f;
            data.structureDamageMultiplier = 1f;
            data.headshotMultiplier = 2f;
        }

        public static void ConfigureShotgun(WeaponData data)
        {
            data.weaponName = "Breacher Shotgun";
            data.weaponType = WeaponType.Shotgun;
            data.damage = 8f;
            data.pelletCount = 8;
            data.pelletSpread = 0.08f;
            data.fireRate = 1.5f;
            data.fireMode = FireMode.SemiAutomatic;
            data.baseSpread = 0.04f;
            data.maxSpread = 0.12f;
            data.recoilVertical = 3f;
            data.recoilHorizontal = 1f;
            data.maxRange = 25f;
            data.damageDropoffStart = 8f;
            data.damageDropoffEnd = 20f;
            data.minDamageMultiplier = 0.3f;
            data.magazineSize = 6;
            data.maxReserveAmmo = 30;
            data.reloadTime = 2.5f;
            data.structureDamageMultiplier = 1.5f;
            data.headshotMultiplier = 1.5f;
        }

        public static void ConfigureSniper(WeaponData data)
        {
            data.weaponName = "Longshot SR";
            data.weaponType = WeaponType.Sniper;
            data.damage = 55f;
            data.fireRate = 0.8f;
            data.fireMode = FireMode.SemiAutomatic;
            data.baseSpread = 0.005f;
            data.maxSpread = 0.02f;
            data.aimSpreadMultiplier = 0.1f;
            data.recoilVertical = 4f;
            data.recoilHorizontal = 0.3f;
            data.recoilRecoverySpeed = 3f;
            data.maxRange = 300f;
            data.damageDropoffStart = 100f;
            data.damageDropoffEnd = 250f;
            data.minDamageMultiplier = 0.7f;
            data.magazineSize = 5;
            data.maxReserveAmmo = 25;
            data.reloadTime = 3f;
            data.structureDamageMultiplier = 2f;
            data.headshotMultiplier = 2.5f;
        }

        public static void ConfigureLMG(WeaponData data)
        {
            data.weaponName = "Siege LMG";
            data.weaponType = WeaponType.LMG;
            data.damage = 16f;
            data.fireRate = 10f;
            data.fireMode = FireMode.Automatic;
            data.baseSpread = 0.04f;
            data.maxSpread = 0.12f;
            data.spreadIncreasePerShot = 0.005f;
            data.recoilVertical = 1.2f;
            data.recoilHorizontal = 0.8f;
            data.maxRange = 80f;
            data.damageDropoffStart = 25f;
            data.damageDropoffEnd = 65f;
            data.magazineSize = 75;
            data.maxReserveAmmo = 225;
            data.reloadTime = 4f;
            data.structureDamageMultiplier = 2f;
            data.headshotMultiplier = 1.8f;
        }
    }
}
