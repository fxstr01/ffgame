using UnityEngine;
using Unity.Netcode;

namespace DestructionRoyale.Resources
{
    public class ResourceGatherer : NetworkBehaviour
    {
        [Header("Gathering Settings")]
        [SerializeField] private float gatherRange = 3f;
        [SerializeField] private LayerMask resourceLayerMask;

        [Header("UI Feedback")]
        [SerializeField] private float indicatorRange = 5f;

        private float gatherTimer;
        private HarvestableResource currentTarget;
        private bool isGathering;

        public bool IsGathering => isGathering;
        public HarvestableResource CurrentTarget => currentTarget;
        public float GatherProgress => currentTarget != null ? gatherTimer / currentTarget.HarvestTime : 0f;

        public event System.Action<Data.ResourceType, int> OnResourceGathered;

        private void Update()
        {
            if (!IsOwner) return;

            FindNearestResource();

            if (isGathering && currentTarget != null)
            {
                gatherTimer += Time.deltaTime;
                if (gatherTimer >= currentTarget.HarvestTime)
                {
                    gatherTimer = 0f;
                    currentTarget.HarvestServerRpc(OwnerClientId);
                }
            }
        }

        private void FindNearestResource()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, indicatorRange, resourceLayerMask);
            HarvestableResource nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider col in colliders)
            {
                HarvestableResource resource = col.GetComponent<HarvestableResource>();
                if (resource != null && !resource.IsDepleted)
                {
                    float dist = Vector3.Distance(transform.position, resource.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = resource;
                    }
                }
            }

            currentTarget = nearestDist <= gatherRange ? nearest : null;
        }

        public void TryGather()
        {
            if (currentTarget != null && !currentTarget.IsDepleted)
            {
                isGathering = true;
            }
            else
            {
                StopGathering();
            }
        }

        public void StopGathering()
        {
            isGathering = false;
            gatherTimer = 0f;
        }
    }
}
