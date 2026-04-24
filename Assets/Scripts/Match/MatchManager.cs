using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DestructionRoyale.Match
{
    public enum MatchState
    {
        WaitingForPlayers,
        Countdown,
        InProgress,
        EndGame,
        Results
    }

    public class MatchManager : NetworkBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int minPlayers = 2;
        [SerializeField] private int maxPlayers = 50;
        [SerializeField] private float countdownDuration = 10f;
        [SerializeField] private float matchDuration = 900f;
        [SerializeField] private float resultsScreenDuration = 10f;

        [Header("Safe Zone")]
        [SerializeField] private SafeZoneController safeZoneController;

        [Header("Loot")]
        [SerializeField] private LootSpawner lootSpawner;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        private NetworkVariable<MatchState> matchState = new NetworkVariable<MatchState>(
            MatchState.WaitingForPlayers, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> matchTimer = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> playersAlive = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private float countdownTimer;
        private List<ulong> activePlayers = new List<ulong>();
        private Dictionary<ulong, int> playerKills = new Dictionary<ulong, int>();

        public MatchState CurrentState => matchState.Value;
        public float MatchTimer => matchTimer.Value;
        public int PlayersAlive => playersAlive.Value;
        public int MaxPlayers => maxPlayers;

        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<ulong, int> OnPlayerEliminated;
        public event System.Action<ulong> OnMatchWon;

        private static MatchManager instance;
        public static MatchManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public override void OnNetworkSpawn()
        {
            matchState.OnValueChanged += HandleMatchStateChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            matchState.OnValueChanged -= HandleMatchStateChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            switch (matchState.Value)
            {
                case MatchState.WaitingForPlayers:
                    if (activePlayers.Count >= minPlayers)
                    {
                        StartCountdown();
                    }
                    break;

                case MatchState.Countdown:
                    countdownTimer -= Time.deltaTime;
                    matchTimer.Value = countdownTimer;
                    if (countdownTimer <= 0f)
                    {
                        StartMatch();
                    }
                    break;

                case MatchState.InProgress:
                    matchTimer.Value -= Time.deltaTime;

                    if (playersAlive.Value <= 1 || matchTimer.Value <= 0f)
                    {
                        EndMatch();
                    }
                    break;

                case MatchState.EndGame:
                    matchTimer.Value -= Time.deltaTime;
                    if (matchTimer.Value <= 0f)
                    {
                        matchState.Value = MatchState.Results;
                        matchTimer.Value = resultsScreenDuration;
                    }
                    break;

                case MatchState.Results:
                    matchTimer.Value -= Time.deltaTime;
                    if (matchTimer.Value <= 0f)
                    {
                        ResetMatch();
                    }
                    break;
            }
        }

        private void StartCountdown()
        {
            matchState.Value = MatchState.Countdown;
            countdownTimer = countdownDuration;
        }

        private void StartMatch()
        {
            matchState.Value = MatchState.InProgress;
            matchTimer.Value = matchDuration;
            playersAlive.Value = activePlayers.Count;

            if (lootSpawner != null)
            {
                lootSpawner.SpawnLoot();
            }

            if (safeZoneController != null)
            {
                safeZoneController.StartZone();
            }

            SpawnPlayersAtRandomPositions();
        }

        private void SpawnPlayersAtRandomPositions()
        {
            List<Transform> availableSpawns = new List<Transform>(spawnPoints);

            foreach (ulong clientId in activePlayers)
            {
                if (availableSpawns.Count == 0) break;

                int randomIndex = Random.Range(0, availableSpawns.Count);
                Transform spawnPoint = availableSpawns[randomIndex];
                availableSpawns.RemoveAt(randomIndex);

                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    if (client.PlayerObject != null)
                    {
                        client.PlayerObject.transform.position = spawnPoint.position;
                        client.PlayerObject.transform.rotation = spawnPoint.rotation;
                    }
                }
            }
        }

        private void EndMatch()
        {
            matchState.Value = MatchState.EndGame;
            matchTimer.Value = 5f;

            ulong winnerId = 0;
            if (activePlayers.Count > 0)
            {
                winnerId = activePlayers[0];
            }

            OnMatchWon?.Invoke(winnerId);
            MatchEndClientRpc(winnerId);
        }

        [ClientRpc]
        private void MatchEndClientRpc(ulong winnerId)
        {
            OnMatchWon?.Invoke(winnerId);
        }

        private void ResetMatch()
        {
            matchState.Value = MatchState.WaitingForPlayers;
            activePlayers.Clear();
            playerKills.Clear();

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                activePlayers.Add(kvp.Key);
            }

            playersAlive.Value = activePlayers.Count;
        }

        public void EliminatePlayer(ulong clientId, ulong killerId)
        {
            if (!IsServer) return;

            activePlayers.Remove(clientId);
            playersAlive.Value = activePlayers.Count;

            if (!playerKills.ContainsKey(killerId))
                playerKills[killerId] = 0;
            playerKills[killerId]++;

            int placement = playersAlive.Value + 1;
            OnPlayerEliminated?.Invoke(clientId, placement);
            PlayerEliminatedClientRpc(clientId, killerId, placement);
        }

        [ClientRpc]
        private void PlayerEliminatedClientRpc(ulong eliminatedId, ulong killerId, int placement)
        {
            OnPlayerEliminated?.Invoke(eliminatedId, placement);
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (matchState.Value == MatchState.WaitingForPlayers)
            {
                activePlayers.Add(clientId);
                playersAlive.Value = activePlayers.Count;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            activePlayers.Remove(clientId);
            if (matchState.Value == MatchState.InProgress)
            {
                playersAlive.Value = activePlayers.Count;
            }
        }

        public int GetPlayerKills(ulong clientId)
        {
            return playerKills.TryGetValue(clientId, out int kills) ? kills : 0;
        }

        private void HandleMatchStateChanged(MatchState oldState, MatchState newState)
        {
            OnMatchStateChanged?.Invoke(newState);
        }
    }
}
