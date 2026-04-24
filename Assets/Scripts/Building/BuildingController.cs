using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Data;
using DestructionRoyale.Player;
using DestructionRoyale.Destruction;
using System.Collections.Generic;

namespace DestructionRoyale.Building
{
    public class BuildingController : NetworkBehaviour
    {
        [Header("Build Settings")]
        [SerializeField] private float buildRange = 8f;
        [SerializeField] private float repairRange = 4f;
        [SerializeField] private float repairRate = 30f;
        [SerializeField] private LayerMask buildSurfaceMask;
        [SerializeField] private LayerMask repairTargetMask;

        [Header("Buildables")]
        [SerializeField] private List<BuildableData> availableBuildables = new List<BuildableData>();

        [Header("Preview")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;

        private PlayerInventory playerInventory;
        private bool isInBuildMode;
        private int selectedBuildableIndex;
        private GameObject previewObject;
        private bool isValidPlacement;
        private float buildCooldownTimer;
        private Dictionary<BuildableType, int> placedCounts = new Dictionary<BuildableType, int>();
        private float previewRotation;

        public bool IsInBuildMode => isInBuildMode;
        public BuildableData SelectedBuildable => selectedBuildableIndex < availableBuildables.Count
            ? availableBuildables[selectedBuildableIndex] : null;
        public bool IsValidPlacement => isValidPlacement;

        public event System.Action<bool> OnBuildModeChanged;
        public event System.Action OnStructurePlaced;

        private void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            buildCooldownTimer -= Time.deltaTime;

            if (isInBuildMode)
            {
                UpdatePreview();
            }
        }

        public void ToggleBuildMode()
        {
            isInBuildMode = !isInBuildMode;
            OnBuildModeChanged?.Invoke(isInBuildMode);

            if (isInBuildMode)
            {
                CreatePreview();
            }
            else
            {
                DestroyPreview();
            }
        }

        public void SelectBuildable(int index)
        {
            if (index < 0 || index >= availableBuildables.Count) return;
            selectedBuildableIndex = index;
            DestroyPreview();
            CreatePreview();
        }

        public void RotatePreview()
        {
            previewRotation += 90f;
            if (previewRotation >= 360f) previewRotation -= 360f;
        }

        private void CreatePreview()
        {
            BuildableData data = SelectedBuildable;
            if (data == null) return;

            if (data.previewPrefab != null)
            {
                previewObject = Instantiate(data.previewPrefab);
            }
            else
            {
                previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                previewObject.transform.localScale = data.dimensions;
                Destroy(previewObject.GetComponent<Collider>());
            }

            previewObject.name = "BuildPreview";
            SetPreviewMaterial(false);
        }

        private void DestroyPreview()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }

        private void UpdatePreview()
        {
            if (previewObject == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            isValidPlacement = false;

            if (Physics.Raycast(ray, out RaycastHit hit, buildRange, buildSurfaceMask))
            {
                BuildableData data = SelectedBuildable;
                if (data == null) return;

                Vector3 placementPos = hit.point;

                if (data.snapToGround)
                {
                    placementPos.y = hit.point.y + data.dimensions.y / 2f;
                }

                previewObject.transform.position = placementPos;
                previewObject.transform.rotation = Quaternion.Euler(0f, previewRotation + transform.eulerAngles.y, 0f);

                isValidPlacement = CanPlace(data, placementPos);
                SetPreviewMaterial(isValidPlacement);
            }
            else
            {
                previewObject.transform.position = transform.position + transform.forward * buildRange;
            }
        }

        private bool CanPlace(BuildableData data, Vector3 position)
        {
            if (buildCooldownTimer > 0f) return false;

            if (!playerInventory.HasResource(data.requiredResource, data.resourceCost))
                return false;

            if (placedCounts.TryGetValue(data.buildableType, out int count))
            {
                if (count >= data.maxPlacedCount) return false;
            }

            float dist = Vector3.Distance(transform.position, position);
            if (dist > data.placementRange) return false;

            Collider[] overlaps = Physics.OverlapBox(position, data.dimensions / 2f,
                Quaternion.Euler(0f, previewRotation, 0f));
            foreach (Collider col in overlaps)
            {
                if (col.GetComponent<PlayerController>() != null) return false;
            }

            return true;
        }

        private void SetPreviewMaterial(bool valid)
        {
            if (previewObject == null) return;

            MeshRenderer renderer = previewObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = valid ? validPlacementMaterial : invalidPlacementMaterial;
                if (mat != null) renderer.material = mat;
            }
        }

        public void PlaceStructure()
        {
            if (!isInBuildMode || !isValidPlacement || previewObject == null) return;

            BuildableData data = SelectedBuildable;
            if (data == null) return;

            Vector3 pos = previewObject.transform.position;
            Quaternion rot = previewObject.transform.rotation;

            PlaceStructureServerRpc(pos, rot, selectedBuildableIndex);

            playerInventory.RemoveResourceServerRpc(data.requiredResource, data.resourceCost);
            buildCooldownTimer = data.cooldownTime;

            if (!placedCounts.ContainsKey(data.buildableType))
                placedCounts[data.buildableType] = 0;
            placedCounts[data.buildableType]++;

            OnStructurePlaced?.Invoke();
        }

        [ServerRpc]
        private void PlaceStructureServerRpc(Vector3 position, Quaternion rotation, int buildableIndex)
        {
            if (buildableIndex >= availableBuildables.Count) return;

            BuildableData data = availableBuildables[buildableIndex];
            if (data.buildablePrefab == null) return;

            GameObject structure = Instantiate(data.buildablePrefab, position, rotation);
            structure.GetComponent<NetworkObject>()?.Spawn();

            PlacedStructure placed = structure.GetComponent<PlacedStructure>();
            if (placed != null)
            {
                placed.Initialize(data, OwnerClientId);
            }
        }

        public void TryRepair()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, repairRange, repairTargetMask))
            {
                WallSegment segment = hit.collider.GetComponent<WallSegment>();
                if (segment != null && !segment.IsDestroyed && segment.CurrentHealth < segment.MaxHealth)
                {
                    float repairAmount = repairRate * Time.deltaTime;
                    segment.RepairServerRpc(repairAmount);
                }

                PlacedStructure structure = hit.collider.GetComponentInParent<PlacedStructure>();
                if (structure != null)
                {
                    structure.RepairServerRpc(repairRate * Time.deltaTime);
                }
            }
        }
    }
}
