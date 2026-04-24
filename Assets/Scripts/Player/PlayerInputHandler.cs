using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Weapons;
using DestructionRoyale.Building;
using DestructionRoyale.Resources;

namespace DestructionRoyale.Player
{
    public class PlayerInputHandler : NetworkBehaviour
    {
        private PlayerController playerController;
        private PlayerInventory playerInventory;
        private WeaponController weaponController;
        private BuildingController buildingController;
        private ResourceGatherer resourceGatherer;
        private ThirdPersonCamera thirdPersonCamera;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerInventory = GetComponent<PlayerInventory>();
            weaponController = GetComponent<WeaponController>();
            buildingController = GetComponent<BuildingController>();
            resourceGatherer = GetComponent<ResourceGatherer>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
            if (thirdPersonCamera != null && playerController.CameraTarget != null)
            {
                thirdPersonCamera.SetTarget(playerController.CameraTarget);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleCameraRotation();
            HandleWeaponInput();
            HandleWeaponSwitch();
            HandleBuildInput();
            HandleGatherInput();
            HandleGrenadeInput();
        }

        private void HandleCameraRotation()
        {
            if (thirdPersonCamera != null)
            {
                playerController.RotatePlayer(thirdPersonCamera.GetYRotation());
            }
        }

        private void HandleWeaponInput()
        {
            if (weaponController == null) return;

            if (Input.GetMouseButton(0))
            {
                weaponController.TryShoot();
            }

            if (Input.GetMouseButtonUp(0))
            {
                weaponController.StopShooting();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                weaponController.Reload();
            }

            weaponController.SetAiming(Input.GetMouseButton(1));
        }

        private void HandleWeaponSwitch()
        {
            if (playerInventory == null) return;

            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    playerInventory.SwitchWeapon(i);
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) playerInventory.CycleWeapon(-1);
            else if (scroll < 0f) playerInventory.CycleWeapon(1);
        }

        private void HandleBuildInput()
        {
            if (buildingController == null) return;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                buildingController.ToggleBuildMode();
            }

            if (buildingController.IsInBuildMode)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    buildingController.PlaceStructure();
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    buildingController.RotatePreview();
                }
            }

            if (Input.GetKey(KeyCode.F))
            {
                buildingController.TryRepair();
            }
        }

        private void HandleGatherInput()
        {
            if (resourceGatherer == null) return;

            if (Input.GetKey(KeyCode.E))
            {
                resourceGatherer.TryGather();
            }
        }

        private void HandleGrenadeInput()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                weaponController?.ThrowGrenade();
            }
        }
    }
}
