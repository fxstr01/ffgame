using UnityEngine;
using Unity.Netcode;
using System;

namespace DestructionRoyale.Destruction
{
    public enum SegmentState
    {
        Intact,
        Damaged,
        Cracked,
        Destroyed
    }

    public class WallSegment : NetworkBehaviour
    {
        [Header("Segment Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        [Header("Visual")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject debrisPrefab;
        [SerializeField] private Material intactMaterial;
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private Material crackedMaterial;

        private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<SegmentState> state = new NetworkVariable<SegmentState>(
            SegmentState.Intact, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private DestructibleWall parentWall;
        private BoxCollider segmentCollider;

        public int GridX => gridX;
        public int GridY => gridY;
        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth;
        public SegmentState State => state.Value;
        public bool IsDestroyed => state.Value == SegmentState.Destroyed;

        public event Action<WallSegment> OnSegmentDestroyed;
        public event Action<WallSegment> OnSegmentDamaged;

        private void Awake()
        {
            segmentCollider = GetComponent<BoxCollider>();
            if (segmentCollider == null)
                segmentCollider = gameObject.AddComponent<BoxCollider>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }

        public override void OnNetworkSpawn()
        {
            currentHealth.OnValueChanged += HandleHealthChanged;
            state.OnValueChanged += HandleStateChanged;

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                state.Value = SegmentState.Intact;
            }
        }

        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= HandleHealthChanged;
            state.OnValueChanged -= HandleStateChanged;
        }

        public void Initialize(int x, int y, float health, DestructibleWall wall)
        {
            gridX = x;
            gridY = y;
            maxHealth = health;
            parentWall = wall;

            if (IsServer)
            {
                currentHealth.Value = health;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (state.Value == SegmentState.Destroyed) return;

            currentHealth.Value = Mathf.Max(0f, currentHealth.Value - damage);

            float healthPercent = currentHealth.Value / maxHealth;

            if (healthPercent <= 0f)
            {
                state.Value = SegmentState.Destroyed;
                OnSegmentDestroyed?.Invoke(this);
                parentWall?.OnSegmentDestroyed(this);
                DestroySegmentClientRpc();
            }
            else if (healthPercent <= 0.3f)
            {
                state.Value = SegmentState.Cracked;
                OnSegmentDamaged?.Invoke(this);
            }
            else if (healthPercent <= 0.7f)
            {
                state.Value = SegmentState.Damaged;
                OnSegmentDamaged?.Invoke(this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RepairServerRpc(float amount)
        {
            if (state.Value == SegmentState.Destroyed) return;

            currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + amount);

            float healthPercent = currentHealth.Value / maxHealth;

            if (healthPercent > 0.7f)
                state.Value = SegmentState.Intact;
            else if (healthPercent > 0.3f)
                state.Value = SegmentState.Damaged;
            else
                state.Value = SegmentState.Cracked;
        }

        [ClientRpc]
        private void DestroySegmentClientRpc()
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (segmentCollider != null)
                segmentCollider.enabled = false;

            SpawnDebris();
        }

        private void SpawnDebris()
        {
            if (debrisPrefab != null)
            {
                GameObject debris = Instantiate(debrisPrefab, transform.position, Random.rotation);
                Destroy(debris, 5f);
            }
        }

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            UpdateVisuals();
        }

        private void HandleStateChanged(SegmentState oldState, SegmentState newState)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (meshRenderer == null) return;

            switch (state.Value)
            {
                case SegmentState.Intact:
                    if (intactMaterial != null) meshRenderer.material = intactMaterial;
                    meshRenderer.enabled = true;
                    break;
                case SegmentState.Damaged:
                    if (damagedMaterial != null) meshRenderer.material = damagedMaterial;
                    meshRenderer.enabled = true;
                    break;
                case SegmentState.Cracked:
                    if (crackedMaterial != null) meshRenderer.material = crackedMaterial;
                    meshRenderer.enabled = true;
                    break;
                case SegmentState.Destroyed:
                    meshRenderer.enabled = false;
                    if (segmentCollider != null) segmentCollider.enabled = false;
                    break;
            }
        }
    }
}
