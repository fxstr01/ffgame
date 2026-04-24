using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using DestructionRoyale.Data;

namespace DestructionRoyale.Player
{
    [Serializable]
    public struct ResourceAmount : INetworkSerializable, IEquatable<ResourceAmount>
    {
        public ResourceType Type;
        public int Amount;

        public ResourceAmount(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Amount);
        }

        public bool Equals(ResourceAmount other)
        {
            return Type == other.Type && Amount == other.Amount;
        }
    }

    public class PlayerInventory : NetworkBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private int maxWeaponSlots = 4;
        [SerializeField] private int maxResourcePerType = 999;

        private NetworkList<ResourceAmount> resources;
        private List<WeaponData> weapons = new List<WeaponData>();
        private int currentWeaponIndex;

        public int CurrentWeaponIndex => currentWeaponIndex;
        public int MaxWeaponSlots => maxWeaponSlots;

        public event Action OnInventoryChanged;
        public event Action<int> OnWeaponSwitched;

        private void Awake()
        {
            resources = new NetworkList<ResourceAmount>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                resources.Add(new ResourceAmount(ResourceType.Wood, 0));
                resources.Add(new ResourceAmount(ResourceType.Stone, 0));
                resources.Add(new ResourceAmount(ResourceType.Metal, 0));
                resources.Add(new ResourceAmount(ResourceType.Components, 0));
            }

            resources.OnListChanged += HandleResourceListChanged;
        }

        public override void OnNetworkDespawn()
        {
            resources.OnListChanged -= HandleResourceListChanged;
        }

        [ServerRpc]
        public void AddResourceServerRpc(ResourceType type, int amount)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Type == type)
                {
                    int newAmount = Mathf.Min(resources[i].Amount + amount, maxResourcePerType);
                    resources[i] = new ResourceAmount(type, newAmount);
                    return;
                }
            }
        }

        [ServerRpc]
        public void RemoveResourceServerRpc(ResourceType type, int amount)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Type == type)
                {
                    int newAmount = Mathf.Max(0, resources[i].Amount - amount);
                    resources[i] = new ResourceAmount(type, newAmount);
                    return;
                }
            }
        }

        public int GetResourceAmount(ResourceType type)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Type == type)
                    return resources[i].Amount;
            }
            return 0;
        }

        public bool HasResource(ResourceType type, int amount)
        {
            return GetResourceAmount(type) >= amount;
        }

        public bool AddWeapon(WeaponData weapon)
        {
            if (weapons.Count >= maxWeaponSlots) return false;
            weapons.Add(weapon);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public WeaponData RemoveWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return null;
            WeaponData weapon = weapons[index];
            weapons.RemoveAt(index);

            if (currentWeaponIndex >= weapons.Count)
                currentWeaponIndex = Mathf.Max(0, weapons.Count - 1);

            OnInventoryChanged?.Invoke();
            return weapon;
        }

        public WeaponData GetCurrentWeapon()
        {
            if (weapons.Count == 0 || currentWeaponIndex >= weapons.Count) return null;
            return weapons[currentWeaponIndex];
        }

        public WeaponData GetWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return null;
            return weapons[index];
        }

        public int WeaponCount => weapons.Count;

        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return;
            currentWeaponIndex = index;
            OnWeaponSwitched?.Invoke(currentWeaponIndex);
        }

        public void CycleWeapon(int direction)
        {
            if (weapons.Count <= 1) return;
            currentWeaponIndex = (currentWeaponIndex + direction + weapons.Count) % weapons.Count;
            OnWeaponSwitched?.Invoke(currentWeaponIndex);
        }

        private void HandleResourceListChanged(NetworkListEvent<ResourceAmount> changeEvent)
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
