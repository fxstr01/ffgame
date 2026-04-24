using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using DestructionRoyale.Data;

namespace DestructionRoyale.Match
{
    [System.Serializable]
    public struct LootEntry
    {
        public GameObject prefab;
        public float weight;
        public LootCategory category;
    }

    public enum LootCategory
    {
        Weapon,
        Ammo,
        Health,
        Shield,
        Resource,
        Grenade
    }

    public class LootSpawner : NetworkBehaviour
    {
        [Header("Loot Configuration")]
        [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();
        [SerializeField] private Transform[] lootSpawnPoints;

        [Header("Spawn Settings")]
        [SerializeField] private float lootDensity = 0.7f;
        [SerializeField] private float minDistanceBetweenLoot = 2f;
        [SerializeField] private int maxLootPerArea = 5;

        [Header("Ground Loot")]
        [SerializeField] private float groundCheckHeight = 10f;
        [SerializeField] private LayerMask groundMask;

        private List<GameObject> spawnedLoot = new List<GameObject>();

        public void SpawnLoot()
        {
            if (!IsServer) return;

            ClearExistingLoot();

            if (lootSpawnPoints != null && lootSpawnPoints.Length > 0)
            {
                SpawnAtPoints();
            }
        }

        private void SpawnAtPoints()
        {
            foreach (Transform point in lootSpawnPoints)
            {
                if (point == null) continue;
                if (Random.value > lootDensity) continue;

                LootEntry entry = GetRandomLootEntry();
                if (entry.prefab == null) continue;

                SpawnLootItem(entry, point.position);
            }
        }

        private LootEntry GetRandomLootEntry()
        {
            float totalWeight = 0f;
            foreach (LootEntry entry in lootTable)
            {
                totalWeight += entry.weight;
            }

            float random = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (LootEntry entry in lootTable)
            {
                currentWeight += entry.weight;
                if (random <= currentWeight)
                    return entry;
            }

            return lootTable.Count > 0 ? lootTable[0] : default;
        }

        private void SpawnLootItem(LootEntry entry, Vector3 position)
        {
            Vector3 spawnPos = position;

            if (Physics.Raycast(position + Vector3.up * groundCheckHeight, Vector3.down,
                out RaycastHit hit, groundCheckHeight * 2f, groundMask))
            {
                spawnPos = hit.point + Vector3.up * 0.3f;
            }

            GameObject lootObj = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = lootObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            LootPickup pickup = lootObj.GetComponent<LootPickup>();
            if (pickup == null)
            {
                pickup = lootObj.AddComponent<LootPickup>();
            }

            spawnedLoot.Add(lootObj);
        }

        private void ClearExistingLoot()
        {
            foreach (GameObject loot in spawnedLoot)
            {
                if (loot != null)
                {
                    NetworkObject netObj = loot.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                    else
                    {
                        Destroy(loot);
                    }
                }
            }
            spawnedLoot.Clear();
        }
    }
}
