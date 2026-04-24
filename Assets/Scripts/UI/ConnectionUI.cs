using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DestructionRoyale.Multiplayer;

namespace DestructionRoyale.UI
{
    public class ConnectionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button serverButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI titleText;

        private void Start()
        {
            if (titleText != null)
                titleText.text = "DESTRUCTION ROYALE";

            if (addressInput != null)
                addressInput.text = "127.0.0.1";

            if (portInput != null)
                portInput.text = "7777";

            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);

            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);

            if (serverButton != null)
                serverButton.onClick.AddListener(OnServerClicked);

            SetStatus("Ready to connect");
        }

        private void OnHostClicked()
        {
            ApplyConnectionSettings();
            SetStatus("Starting host...");
            NetworkGameManager.Instance?.StartHost();
        }

        private void OnJoinClicked()
        {
            ApplyConnectionSettings();
            SetStatus("Connecting...");
            NetworkGameManager.Instance?.StartClient();
        }

        private void OnServerClicked()
        {
            ApplyConnectionSettings();
            SetStatus("Starting server...");
            NetworkGameManager.Instance?.StartServer();
        }

        private void ApplyConnectionSettings()
        {
            if (NetworkGameManager.Instance == null) return;

            if (addressInput != null)
                NetworkGameManager.Instance.SetServerAddress(addressInput.text);

            if (portInput != null)
                NetworkGameManager.Instance.SetServerPort(portInput.text);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void OnDestroy()
        {
            if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.RemoveListener(OnJoinClicked);
            if (serverButton != null) serverButton.onClick.RemoveListener(OnServerClicked);
        }
    }
}
