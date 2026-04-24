using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Multiplayer;
using DestructionRoyale.UI;

namespace DestructionRoyale.Match
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkGameManager networkManager;
        [SerializeField] private GameHUD gameHUD;

        [Header("Auto Start (Debug)")]
        [SerializeField] private bool autoStartHost;

        private void Start()
        {
            if (networkManager == null)
                networkManager = FindFirstObjectByType<NetworkGameManager>();

            NetworkManager.Singleton.OnClientConnectedCallback += OnLocalPlayerSpawned;

            if (autoStartHost && networkManager != null)
            {
                networkManager.StartHost();
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnLocalPlayerSpawned;
            }
        }

        private void OnLocalPlayerSpawned(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            StartCoroutine(InitializeLocalPlayer());
        }

        private System.Collections.IEnumerator InitializeLocalPlayer()
        {
            yield return new WaitForSeconds(0.5f);

            NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer == null) yield break;

            // Initialize HUD
            if (gameHUD != null)
            {
                gameHUD.Initialize(localPlayer.gameObject);
            }

            // Initialize camera
            Player.ThirdPersonCamera cam = FindFirstObjectByType<Player.ThirdPersonCamera>();
            if (cam != null)
            {
                Player.PlayerController controller = localPlayer.GetComponent<Player.PlayerController>();
                if (controller != null && controller.CameraTarget != null)
                {
                    cam.SetTarget(controller.CameraTarget);
                }
            }

            // Initialize damage indicator
            DamageIndicator dmgIndicator = FindFirstObjectByType<DamageIndicator>();
            if (dmgIndicator != null)
            {
                dmgIndicator.SetPlayerTransform(localPlayer.transform);
            }
        }
    }
}
