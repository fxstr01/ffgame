using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DestructionRoyale.Player;
using DestructionRoyale.Weapons;
using DestructionRoyale.Match;
using DestructionRoyale.Data;

namespace DestructionRoyale.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider shieldBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Weapon")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Slider reloadBar;
        [SerializeField] private Image[] weaponSlotIcons;

        [Header("Resources")]
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI metalText;
        [SerializeField] private TextMeshProUGUI componentsText;

        [Header("Match")]
        [SerializeField] private TextMeshProUGUI matchTimerText;
        [SerializeField] private TextMeshProUGUI playersAliveText;
        [SerializeField] private TextMeshProUGUI matchStateText;
        [SerializeField] private TextMeshProUGUI killsText;

        [Header("Build Mode")]
        [SerializeField] private GameObject buildModePanel;
        [SerializeField] private TextMeshProUGUI buildModeText;

        [Header("Gather")]
        [SerializeField] private Slider gatherProgressBar;
        [SerializeField] private GameObject gatherPrompt;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Image zoneCircle;

        [Header("Kill Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedEntryPrefab;
        [SerializeField] private int maxKillFeedEntries = 5;

        [Header("Grenade")]
        [SerializeField] private TextMeshProUGUI grenadeCountText;

        private PlayerHealth playerHealth;
        private PlayerInventory playerInventory;
        private WeaponController weaponController;
        private Building.BuildingController buildingController;
        private Resources.ResourceGatherer resourceGatherer;

        private void Start()
        {
            if (buildModePanel != null) buildModePanel.SetActive(false);
            if (gatherPrompt != null) gatherPrompt.SetActive(false);
            if (reloadBar != null) reloadBar.gameObject.SetActive(false);
        }

        public void Initialize(GameObject player)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerInventory = player.GetComponent<PlayerInventory>();
            weaponController = player.GetComponent<WeaponController>();
            buildingController = player.GetComponent<Building.BuildingController>();
            resourceGatherer = player.GetComponent<Resources.ResourceGatherer>();

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
                playerHealth.OnShieldChanged += UpdateShieldUI;
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += UpdateInventoryUI;
                playerInventory.OnWeaponSwitched += UpdateWeaponUI;
            }

            if (buildingController != null)
            {
                buildingController.OnBuildModeChanged += UpdateBuildModeUI;
            }
        }

        private void Update()
        {
            UpdateMatchUI();
            UpdateWeaponHUD();
            UpdateResourceUI();
            UpdateGatherUI();
        }

        private void UpdateHealthUI(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)}";
        }

        private void UpdateShieldUI(float current, float max)
        {
            if (shieldBar != null)
            {
                shieldBar.maxValue = max;
                shieldBar.value = current;
            }
        }

        private void UpdateWeaponHUD()
        {
            if (weaponController == null) return;

            if (ammoText != null)
                ammoText.text = $"{weaponController.CurrentAmmo} / {weaponController.ReserveAmmo}";

            if (reloadBar != null)
            {
                bool reloading = weaponController.IsReloading;
                reloadBar.gameObject.SetActive(reloading);
                if (reloading)
                    reloadBar.value = weaponController.ReloadProgress;
            }

            if (grenadeCountText != null)
                grenadeCountText.text = $"G: {weaponController.CurrentGrenades}";

            WeaponData currentWeapon = playerInventory?.GetCurrentWeapon();
            if (weaponNameText != null)
                weaponNameText.text = currentWeapon != null ? currentWeapon.weaponName : "---";
        }

        private void UpdateWeaponUI(int index)
        {
            WeaponData weapon = playerInventory?.GetWeapon(index);
            if (weaponNameText != null)
                weaponNameText.text = weapon != null ? weapon.weaponName : "---";

            if (weaponSlotIcons != null)
            {
                for (int i = 0; i < weaponSlotIcons.Length; i++)
                {
                    if (weaponSlotIcons[i] == null) continue;

                    WeaponData slotWeapon = playerInventory?.GetWeapon(i);
                    weaponSlotIcons[i].gameObject.SetActive(slotWeapon != null);
                    if (slotWeapon?.weaponIcon != null)
                        weaponSlotIcons[i].sprite = slotWeapon.weaponIcon;

                    weaponSlotIcons[i].color = (i == index) ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                }
            }
        }

        private void UpdateInventoryUI()
        {
            UpdateResourceUI();
        }

        private void UpdateResourceUI()
        {
            if (playerInventory == null) return;

            if (woodText != null)
                woodText.text = playerInventory.GetResourceAmount(ResourceType.Wood).ToString();
            if (stoneText != null)
                stoneText.text = playerInventory.GetResourceAmount(ResourceType.Stone).ToString();
            if (metalText != null)
                metalText.text = playerInventory.GetResourceAmount(ResourceType.Metal).ToString();
            if (componentsText != null)
                componentsText.text = playerInventory.GetResourceAmount(ResourceType.Components).ToString();
        }

        private void UpdateMatchUI()
        {
            MatchManager match = MatchManager.Instance;
            if (match == null) return;

            if (matchTimerText != null)
            {
                float timer = match.MatchTimer;
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                matchTimerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (playersAliveText != null)
                playersAliveText.text = $"{match.PlayersAlive} alive";

            if (matchStateText != null)
            {
                string stateText = match.CurrentState switch
                {
                    MatchState.WaitingForPlayers => "Waiting for players...",
                    MatchState.Countdown => $"Starting in {Mathf.CeilToInt(match.MatchTimer)}",
                    MatchState.InProgress => "",
                    MatchState.EndGame => "Match Over!",
                    MatchState.Results => "Results",
                    _ => ""
                };
                matchStateText.text = stateText;
            }
        }

        private void UpdateBuildModeUI(bool isInBuildMode)
        {
            if (buildModePanel != null)
                buildModePanel.SetActive(isInBuildMode);

            if (buildModeText != null)
                buildModeText.text = isInBuildMode ? "BUILD MODE - [Q] to exit | [LMB] to place | [R] to rotate" : "";
        }

        private void UpdateGatherUI()
        {
            if (resourceGatherer == null) return;

            bool canGather = resourceGatherer.CurrentTarget != null;
            if (gatherPrompt != null)
                gatherPrompt.SetActive(canGather && !resourceGatherer.IsGathering);

            if (gatherProgressBar != null)
            {
                gatherProgressBar.gameObject.SetActive(resourceGatherer.IsGathering);
                gatherProgressBar.value = resourceGatherer.GatherProgress;
            }
        }

        public void AddKillFeedEntry(string killerName, string victimName, string weaponName)
        {
            if (killFeedContainer == null || killFeedEntryPrefab == null) return;

            if (killFeedContainer.childCount >= maxKillFeedEntries)
            {
                Destroy(killFeedContainer.GetChild(0).gameObject);
            }

            GameObject entry = Instantiate(killFeedEntryPrefab, killFeedContainer);
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{killerName} [{weaponName}] {victimName}";
            }

            Destroy(entry, 5f);
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthUI;
                playerHealth.OnShieldChanged -= UpdateShieldUI;
            }
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= UpdateInventoryUI;
                playerInventory.OnWeaponSwitched -= UpdateWeaponUI;
            }
            if (buildingController != null)
            {
                buildingController.OnBuildModeChanged -= UpdateBuildModeUI;
            }
        }
    }
}
