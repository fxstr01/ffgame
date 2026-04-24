using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DestructionRoyale.Match
{
    [System.Serializable]
    public struct ZonePhase
    {
        public float startDelay;
        public float shrinkDuration;
        public float targetRadiusMultiplier;
        public float damagePerSecond;
    }

    public class SafeZoneController : NetworkBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private float initialRadius = 200f;
        [SerializeField] private float minRadius = 10f;
        [SerializeField] private Vector3 mapCenter = Vector3.zero;

        [Header("Zone Phases")]
        [SerializeField] private List<ZonePhase> phases = new List<ZonePhase>
        {
            new ZonePhase { startDelay = 60f, shrinkDuration = 30f, targetRadiusMultiplier = 0.7f, damagePerSecond = 1f },
            new ZonePhase { startDelay = 45f, shrinkDuration = 25f, targetRadiusMultiplier = 0.5f, damagePerSecond = 2f },
            new ZonePhase { startDelay = 30f, shrinkDuration = 20f, targetRadiusMultiplier = 0.3f, damagePerSecond = 5f },
            new ZonePhase { startDelay = 20f, shrinkDuration = 15f, targetRadiusMultiplier = 0.15f, damagePerSecond = 10f },
            new ZonePhase { startDelay = 15f, shrinkDuration = 10f, targetRadiusMultiplier = 0.05f, damagePerSecond = 20f }
        };

        [Header("Visual")]
        [SerializeField] private GameObject zoneVisualPrefab;
        [SerializeField] private Color safeZoneColor = new Color(0f, 0.5f, 1f, 0.3f);
        [SerializeField] private Color dangerZoneColor = new Color(1f, 0.2f, 0f, 0.3f);

        private NetworkVariable<Vector3> zoneCenter = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> currentRadius = new NetworkVariable<float>(
            200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> targetRadius = new NetworkVariable<float>(
            200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> currentPhaseIndex = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> isShrinking = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private float phaseTimer;
        private float shrinkTimer;
        private float startRadius;
        private bool isActive;
        private GameObject zoneVisual;

        public Vector3 ZoneCenter => zoneCenter.Value;
        public float CurrentRadius => currentRadius.Value;
        public float TargetRadius => targetRadius.Value;
        public int CurrentPhase => currentPhaseIndex.Value;
        public bool IsShrinking => isShrinking.Value;

        public event System.Action<int> OnPhaseChanged;
        public event System.Action OnZoneShrinkStarted;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                zoneCenter.Value = mapCenter;
                currentRadius.Value = initialRadius;
                targetRadius.Value = initialRadius;
            }

            CreateZoneVisual();

            currentRadius.OnValueChanged += (_, _) => UpdateZoneVisual();
            zoneCenter.OnValueChanged += (_, _) => UpdateZoneVisual();
        }

        public void StartZone()
        {
            if (!IsServer) return;

            isActive = true;
            currentPhaseIndex.Value = 0;
            phaseTimer = phases[0].startDelay;

            PickNewCenter();
        }

        private void Update()
        {
            if (!IsServer || !isActive) return;

            int phase = currentPhaseIndex.Value;
            if (phase >= phases.Count) return;

            if (!isShrinking.Value)
            {
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    StartShrinking();
                }
            }
            else
            {
                shrinkTimer -= Time.deltaTime;
                float t = 1f - (shrinkTimer / phases[phase].shrinkDuration);
                t = Mathf.Clamp01(t);

                currentRadius.Value = Mathf.Lerp(startRadius, targetRadius.Value, t);

                if (shrinkTimer <= 0f)
                {
                    CompleteShrink();
                }
            }

            DamagePlayersOutsideZone();
        }

        private void StartShrinking()
        {
            int phase = currentPhaseIndex.Value;
            isShrinking.Value = true;
            startRadius = currentRadius.Value;
            targetRadius.Value = initialRadius * phases[phase].targetRadiusMultiplier;
            targetRadius.Value = Mathf.Max(targetRadius.Value, minRadius);
            shrinkTimer = phases[phase].shrinkDuration;

            OnZoneShrinkStarted?.Invoke();
            ZoneShrinkStartedClientRpc(phase);
        }

        [ClientRpc]
        private void ZoneShrinkStartedClientRpc(int phase)
        {
            OnZoneShrinkStarted?.Invoke();
        }

        private void CompleteShrink()
        {
            isShrinking.Value = false;
            currentPhaseIndex.Value++;

            if (currentPhaseIndex.Value < phases.Count)
            {
                phaseTimer = phases[currentPhaseIndex.Value].startDelay;
                PickNewCenter();
                OnPhaseChanged?.Invoke(currentPhaseIndex.Value);
                PhaseChangedClientRpc(currentPhaseIndex.Value);
            }
        }

        [ClientRpc]
        private void PhaseChangedClientRpc(int newPhase)
        {
            OnPhaseChanged?.Invoke(newPhase);
        }

        private void PickNewCenter()
        {
            float maxOffset = currentRadius.Value * 0.3f;
            Vector3 newCenter = zoneCenter.Value + new Vector3(
                Random.Range(-maxOffset, maxOffset),
                0f,
                Random.Range(-maxOffset, maxOffset)
            );

            zoneCenter.Value = newCenter;
        }

        private void DamagePlayersOutsideZone()
        {
            int phase = currentPhaseIndex.Value;
            if (phase >= phases.Count) return;

            float dps = phases[phase].damagePerSecond;

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                if (kvp.Value.PlayerObject == null) continue;

                Vector3 playerPos = kvp.Value.PlayerObject.transform.position;
                playerPos.y = 0f;
                Vector3 center = zoneCenter.Value;
                center.y = 0f;

                float distance = Vector3.Distance(playerPos, center);

                if (distance > currentRadius.Value)
                {
                    Player.PlayerHealth health = kvp.Value.PlayerObject.GetComponent<Player.PlayerHealth>();
                    if (health != null && !health.IsDead)
                    {
                        health.TakeDamageServerRpc(dps * Time.deltaTime, 0);
                    }
                }
            }
        }

        private void CreateZoneVisual()
        {
            if (zoneVisualPrefab != null)
            {
                zoneVisual = Instantiate(zoneVisualPrefab);
            }
            else
            {
                zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                zoneVisual.name = "SafeZoneVisual";
                Destroy(zoneVisual.GetComponent<Collider>());

                MeshRenderer renderer = zoneVisual.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = safeZoneColor;
                renderer.material = mat;
            }

            UpdateZoneVisual();
        }

        private void UpdateZoneVisual()
        {
            if (zoneVisual == null) return;

            zoneVisual.transform.position = new Vector3(zoneCenter.Value.x, 0.1f, zoneCenter.Value.z);
            float diameter = currentRadius.Value * 2f;
            zoneVisual.transform.localScale = new Vector3(diameter, 0.01f, diameter);
        }

        public bool IsInsideZone(Vector3 position)
        {
            Vector3 flatPos = new Vector3(position.x, 0f, position.z);
            Vector3 flatCenter = new Vector3(zoneCenter.Value.x, 0f, zoneCenter.Value.z);
            return Vector3.Distance(flatPos, flatCenter) <= currentRadius.Value;
        }
    }
}
