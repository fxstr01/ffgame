using UnityEngine;
using Unity.Netcode;

namespace DestructionRoyale.Multiplayer
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnHeight = 2f;

        private int nextSpawnIndex;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerForClient;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayerForClient;
            }
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not assigned to PlayerSpawner!");
                return;
            }

            Vector3 spawnPosition = GetNextSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId);
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                float angle = nextSpawnIndex * (360f / 20f);
                float radius = 20f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    spawnHeight,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                nextSpawnIndex++;
                return pos;
            }

            Transform point = spawnPoints[nextSpawnIndex % spawnPoints.Length];
            nextSpawnIndex++;
            return point.position + Vector3.up * spawnHeight;
        }
    }
}
