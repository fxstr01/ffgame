using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DestructionRoyale.Destruction
{
    public class DestructibleWall : NetworkBehaviour
    {
        [Header("Wall Grid Configuration")]
        [SerializeField] private int gridWidth = 4;
        [SerializeField] private int gridHeight = 3;
        [SerializeField] private float segmentWidth = 0.5f;
        [SerializeField] private float segmentHeight = 0.5f;
        [SerializeField] private float wallThickness = 0.15f;

        [Header("Segment Stats")]
        [SerializeField] private float segmentHealth = 100f;

        [Header("Structural Integrity")]
        [SerializeField] private bool enableStructuralCollapse = true;
        [SerializeField] private int minConnectedForStability = 2;
        [SerializeField] private float collapseDelay = 0.5f;

        [Header("Visual")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private Material crackedMaterial;
        [SerializeField] private GameObject debrisPrefab;

        [Header("Resource Drop")]
        [SerializeField] private Data.ResourceType dropResourceType = Data.ResourceType.Components;
        [SerializeField] private int dropAmountPerSegment = 2;

        private WallSegment[,] segments;
        private bool[,] segmentAlive;
        private bool isCollapsing;
        private int totalSegments;
        private int destroyedSegments;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float DestructionPercent => totalSegments > 0 ? (float)destroyedSegments / totalSegments : 0f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateWall();
            }
        }

        private void GenerateWall()
        {
            segments = new WallSegment[gridWidth, gridHeight];
            segmentAlive = new bool[gridWidth, gridHeight];
            totalSegments = gridWidth * gridHeight;
            destroyedSegments = 0;

            float totalWidth = gridWidth * segmentWidth;
            float totalHeight = gridHeight * segmentHeight;
            Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0f, 0f) + new Vector3(segmentWidth / 2f, segmentHeight / 2f, 0f);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 localPos = new Vector3(x * segmentWidth, y * segmentHeight, 0f);
                    Vector3 worldPos = startPos + transform.rotation * localPos;

                    GameObject segObj = CreateSegmentObject(worldPos, x, y);
                    NetworkObject netObj = segObj.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }

                    WallSegment segment = segObj.GetComponent<WallSegment>();
                    segment.Initialize(x, y, segmentHealth, this);

                    segments[x, y] = segment;
                    segmentAlive[x, y] = true;
                }
            }

            SpawnWallClientRpc();
        }

        private GameObject CreateSegmentObject(Vector3 position, int x, int y)
        {
            GameObject segObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segObj.name = $"WallSegment_{x}_{y}";
            segObj.transform.position = position;
            segObj.transform.rotation = transform.rotation;
            segObj.transform.localScale = new Vector3(segmentWidth * 0.95f, segmentHeight * 0.95f, wallThickness);
            segObj.transform.SetParent(transform);

            segObj.layer = gameObject.layer;

            if (wallMaterial != null)
            {
                MeshRenderer renderer = segObj.GetComponent<MeshRenderer>();
                renderer.material = wallMaterial;
            }

            WallSegment segment = segObj.AddComponent<WallSegment>();
            NetworkObject netObj = segObj.AddComponent<NetworkObject>();

            return segObj;
        }

        [ClientRpc]
        private void SpawnWallClientRpc()
        {
            // Client-side wall initialization if needed
        }

        public void OnSegmentDestroyed(WallSegment segment)
        {
            if (!IsServer) return;

            int x = segment.GridX;
            int y = segment.GridY;

            segmentAlive[x, y] = false;
            destroyedSegments++;

            CheckBreachFormation(x, y);

            if (enableStructuralCollapse)
            {
                CheckStructuralIntegrity();
            }
        }

        private void CheckBreachFormation(int destroyedX, int destroyedY)
        {
            // Check for rectangular breach patterns
            // If surrounding segments are destroyed, the center becomes a breach opening
            CheckSquareBreach(destroyedX, destroyedY);
            CheckLineBreach(destroyedX, destroyedY);
        }

        private void CheckSquareBreach(int cx, int cy)
        {
            // Check 2x2 square patterns around the destroyed segment
            int[] dx = { 0, 1, 0, 1 };
            int[] dy = { 0, 0, 1, 1 };

            for (int startX = cx - 1; startX <= cx; startX++)
            {
                for (int startY = cy - 1; startY <= cy; startY++)
                {
                    bool allDestroyed = true;
                    for (int i = 0; i < 4; i++)
                    {
                        int checkX = startX + dx[i];
                        int checkY = startY + dy[i];

                        if (checkX < 0 || checkX >= gridWidth || checkY < 0 || checkY >= gridHeight)
                        {
                            allDestroyed = false;
                            break;
                        }

                        if (segmentAlive[checkX, checkY])
                        {
                            allDestroyed = false;
                            break;
                        }
                    }

                    if (allDestroyed)
                    {
                        BreachFormedClientRpc(startX, startY, 2, 2);
                    }
                }
            }
        }

        private void CheckLineBreach(int cx, int cy)
        {
            // Check horizontal line breaches (3+ segments in a row)
            int hStart = cx, hEnd = cx;
            while (hStart > 0 && !segmentAlive[hStart - 1, cy]) hStart--;
            while (hEnd < gridWidth - 1 && !segmentAlive[hEnd + 1, cy]) hEnd++;

            if (hEnd - hStart >= 2)
            {
                BreachFormedClientRpc(hStart, cy, hEnd - hStart + 1, 1);
            }

            // Check vertical line breaches
            int vStart = cy, vEnd = cy;
            while (vStart > 0 && !segmentAlive[cx, vStart - 1]) vStart--;
            while (vEnd < gridHeight - 1 && !segmentAlive[cx, vEnd + 1]) vEnd++;

            if (vEnd - vStart >= 2)
            {
                BreachFormedClientRpc(cx, vStart, 1, vEnd - vStart + 1);
            }
        }

        [ClientRpc]
        private void BreachFormedClientRpc(int startX, int startY, int width, int height)
        {
            // Visual/audio feedback for breach formation
            Debug.Log($"Breach formed at ({startX},{startY}) size {width}x{height}");
        }

        private void CheckStructuralIntegrity()
        {
            // Check if any segments are floating (no ground connection)
            bool[,] visited = new bool[gridWidth, gridHeight];
            bool[,] stable = new bool[gridWidth, gridHeight];

            // Bottom row is always stable if alive
            for (int x = 0; x < gridWidth; x++)
            {
                if (segmentAlive[x, 0])
                {
                    FloodFillStability(x, 0, visited, stable);
                }
            }

            // Any alive segment not marked stable should collapse
            List<WallSegment> toCollapse = new List<WallSegment>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (segmentAlive[x, y] && !stable[x, y])
                    {
                        toCollapse.Add(segments[x, y]);
                    }
                }
            }

            foreach (WallSegment segment in toCollapse)
            {
                CollapseSegment(segment);
            }
        }

        private void FloodFillStability(int x, int y, bool[,] visited, bool[,] stable)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            if (visited[x, y] || !segmentAlive[x, y]) return;

            visited[x, y] = true;
            stable[x, y] = true;

            FloodFillStability(x + 1, y, visited, stable);
            FloodFillStability(x - 1, y, visited, stable);
            FloodFillStability(x, y + 1, visited, stable);
            FloodFillStability(x, y - 1, visited, stable);
        }

        private void CollapseSegment(WallSegment segment)
        {
            segmentAlive[segment.GridX, segment.GridY] = false;
            destroyedSegments++;

            segment.TakeDamageServerRpc(segment.MaxHealth);
            CollapseEffectClientRpc(segment.GridX, segment.GridY);
        }

        [ClientRpc]
        private void CollapseEffectClientRpc(int x, int y)
        {
            // Spawn collapse particles/debris
            if (segments != null && x < gridWidth && y < gridHeight && segments[x, y] != null)
            {
                if (debrisPrefab != null)
                {
                    GameObject debris = Instantiate(debrisPrefab, segments[x, y].transform.position, Random.rotation);
                    Rigidbody rb = debris.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(Vector3.down * 5f + Random.insideUnitSphere * 2f, ForceMode.Impulse);
                    }
                    Destroy(debris, 5f);
                }
            }
        }

        public void ApplyExplosionDamage(Vector3 explosionCenter, float radius, float maxDamage)
        {
            if (!IsServer || segments == null) return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!segmentAlive[x, y] || segments[x, y] == null) continue;

                    float distance = Vector3.Distance(segments[x, y].transform.position, explosionCenter);
                    if (distance <= radius)
                    {
                        float damageMultiplier = 1f - (distance / radius);
                        float damage = maxDamage * damageMultiplier;
                        segments[x, y].TakeDamageServerRpc(damage);
                    }
                }
            }
        }

        public void RepairSegment(int x, int y, float amount)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            if (segments[x, y] == null || segments[x, y].IsDestroyed) return;

            segments[x, y].RepairServerRpc(amount);
        }

        public WallSegment GetSegment(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
            return segments?[x, y];
        }

        public bool IsSegmentAlive(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
            return segmentAlive != null && segmentAlive[x, y];
        }
    }
}
