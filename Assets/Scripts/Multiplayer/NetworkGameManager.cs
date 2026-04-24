using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace DestructionRoyale.Multiplayer
{
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;

        [Header("UI")]
        [SerializeField] private GameObject connectionUI;
        [SerializeField] private GameObject gameUI;

        private static NetworkGameManager instance;
        public static NetworkGameManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (connectionUI != null) connectionUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public void StartHost()
        {
            ConfigureTransport();
            NetworkManager.Singleton.StartHost();
            OnConnected();
        }

        public void StartClient()
        {
            ConfigureTransport();
            NetworkManager.Singleton.StartClient();
        }

        public void StartServer()
        {
            ConfigureTransport();
            NetworkManager.Singleton.StartServer();
            OnConnected();
        }

        public void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            if (connectionUI != null) connectionUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);
        }

        private void ConfigureTransport()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(serverAddress, serverPort);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"Client {clientId} connected");
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnConnected();
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (connectionUI != null) connectionUI.SetActive(true);
                if (gameUI != null) gameUI.SetActive(false);
            }
        }

        private void OnConnected()
        {
            if (connectionUI != null) connectionUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);
        }

        public void SetServerAddress(string address)
        {
            serverAddress = address;
        }

        public void SetServerPort(string port)
        {
            if (ushort.TryParse(port, out ushort parsedPort))
            {
                serverPort = parsedPort;
            }
        }
    }
}
