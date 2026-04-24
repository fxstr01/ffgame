using UnityEngine;
using Unity.Netcode;
using DestructionRoyale.Destruction;

namespace DestructionRoyale.Weapons
{
    public class Grenade : NetworkBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private float fuseTime = 3f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float maxDamage = 80f;
        [SerializeField] private float wallDamage = 40f;
        [SerializeField] private float explosionForce = 500f;

        [Header("Visual")]
        [SerializeField] private GameObject explosionEffectPrefab;

        private float fuseTimer;
        private bool hasExploded;

        private void Start()
        {
            fuseTimer = fuseTime;
        }

        private void Update()
        {
            if (!IsServer) return;

            fuseTimer -= Time.deltaTime;
            if (fuseTimer <= 0f && !hasExploded)
            {
                Explode();
            }
        }

        private void Explode()
        {
            hasExploded = true;

            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider col in colliders)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                damageMultiplier = Mathf.Clamp01(damageMultiplier);

                Player.PlayerHealth playerHealth = col.GetComponentInParent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    float damage = maxDamage * damageMultiplier;
                    playerHealth.TakeDamageServerRpc(damage, OwnerClientId);
                }

                WallSegment wallSegment = col.GetComponent<WallSegment>();
                if (wallSegment != null)
                {
                    float damage = wallDamage * damageMultiplier;
                    wallSegment.TakeDamageServerRpc(damage);
                }

                DestructibleWall wall = col.GetComponentInParent<DestructibleWall>();
                if (wall != null)
                {
                    wall.ApplyExplosionDamage(transform.position, explosionRadius, wallDamage);
                }

                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }

            ExplodeEffectsClientRpc(transform.position);

            GetComponent<NetworkObject>()?.Despawn();
        }

        [ClientRpc]
        private void ExplodeEffectsClientRpc(Vector3 position)
        {
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 3f);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity *= 0.5f;
                rb.angularVelocity *= 0.5f;
            }
        }
    }
}
