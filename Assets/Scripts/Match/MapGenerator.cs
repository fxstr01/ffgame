using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using DestructionRoyale.Destruction;
using DestructionRoyale.Resources;

namespace DestructionRoyale.Match
{
    public class MapGenerator : NetworkBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private float mapSize = 400f;
        [SerializeField] private int buildingCount = 20;
        [SerializeField] private int treeCount = 100;
        [SerializeField] private int rockCount = 50;
        [SerializeField] private int scrapCount = 30;

        [Header("Prefabs")]
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject treePrefab;
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private GameObject scrapPrefab;

        [Header("Ground")]
        [SerializeField] private Material groundMaterial;

        private List<GameObject> spawnedObjects = new List<GameObject>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateMap();
            }
        }

        public void GenerateMap()
        {
            CreateGround();
            GenerateBuildings();
            GenerateResources();
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(mapSize / 10f, 1f, mapSize / 10f);
            ground.layer = LayerMask.NameToLayer("Default");

            if (groundMaterial != null)
            {
                ground.GetComponent<MeshRenderer>().material = groundMaterial;
            }

            spawnedObjects.Add(ground);
        }

        private void GenerateBuildings()
        {
            float halfMap = mapSize / 2f;
            float minDistance = 20f;
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < buildingCount; i++)
            {
                Vector3 pos = Vector3.zero;
                bool validPos = false;
                int attempts = 0;

                while (!validPos && attempts < 50)
                {
                    pos = new Vector3(
                        Random.Range(-halfMap, halfMap),
                        0f,
                        Random.Range(-halfMap, halfMap)
                    );

                    validPos = true;
                    foreach (Vector3 existing in positions)
                    {
                        if (Vector3.Distance(pos, existing) < minDistance)
                        {
                            validPos = false;
                            break;
                        }
                    }
                    attempts++;
                }

                if (!validPos) continue;
                positions.Add(pos);

                GameObject building = CreateProceduralBuilding(pos, i);
                if (building != null)
                {
                    spawnedObjects.Add(building);
                }
            }
        }

        private GameObject CreateProceduralBuilding(Vector3 position, int index)
        {
            GameObject building = new GameObject($"Building_{index}");
            building.transform.position = position;
            building.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float width = Random.Range(6f, 12f);
            float depth = Random.Range(6f, 12f);
            float height = Random.Range(3f, 5f);

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(building.transform);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(width, 0.1f, depth);

            // Create 4 walls as DestructibleWall components
            CreateBuildingWall(building.transform, new Vector3(0f, height / 2f, depth / 2f), Quaternion.identity, width, height, "FrontWall");
            CreateBuildingWall(building.transform, new Vector3(0f, height / 2f, -depth / 2f), Quaternion.Euler(0f, 180f, 0f), width, height, "BackWall");
            CreateBuildingWall(building.transform, new Vector3(width / 2f, height / 2f, 0f), Quaternion.Euler(0f, 90f, 0f), depth, height, "RightWall");
            CreateBuildingWall(building.transform, new Vector3(-width / 2f, height / 2f, 0f), Quaternion.Euler(0f, -90f, 0f), depth, height, "LeftWall");

            // Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0f, height, 0f);
            roof.transform.localScale = new Vector3(width + 0.5f, 0.15f, depth + 0.5f);

            // Doorway (remove front wall bottom-center segments later via runtime)

            DestructibleBuilding destructible = building.AddComponent<DestructibleBuilding>();
            NetworkObject netObj = building.AddComponent<NetworkObject>();
            netObj.Spawn();

            return building;
        }

        private void CreateBuildingWall(Transform parent, Vector3 localPos, Quaternion localRot, float width, float height, string wallName)
        {
            GameObject wallObj = new GameObject(wallName);
            wallObj.transform.SetParent(parent);
            wallObj.transform.localPosition = localPos;
            wallObj.transform.localRotation = localRot;

            DestructibleWall wall = wallObj.AddComponent<DestructibleWall>();
            NetworkObject netObj = wallObj.AddComponent<NetworkObject>();
        }

        private void GenerateResources()
        {
            float halfMap = mapSize / 2f;

            // Trees
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-halfMap, halfMap),
                    0f,
                    Random.Range(-halfMap, halfMap)
                );
                CreateResourceObject(pos, Data.ResourceType.Wood, $"Tree_{i}", new Vector3(0.5f, Random.Range(3f, 6f), 0.5f));
            }

            // Rocks
            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-halfMap, halfMap),
                    0f,
                    Random.Range(-halfMap, halfMap)
                );
                CreateResourceObject(pos, Data.ResourceType.Stone, $"Rock_{i}", new Vector3(Random.Range(1f, 2f), Random.Range(0.5f, 1.5f), Random.Range(1f, 2f)));
            }

            // Scrap piles
            for (int i = 0; i < scrapCount; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-halfMap, halfMap),
                    0f,
                    Random.Range(-halfMap, halfMap)
                );
                CreateResourceObject(pos, Data.ResourceType.Metal, $"Scrap_{i}", new Vector3(1f, 0.5f, 1f));
            }
        }

        private void CreateResourceObject(Vector3 position, Data.ResourceType type, string objName, Vector3 scale)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = objName;
            obj.transform.position = position + Vector3.up * (scale.y / 2f);
            obj.transform.localScale = scale;

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = type switch
            {
                Data.ResourceType.Wood => new Color(0.55f, 0.35f, 0.15f),
                Data.ResourceType.Stone => new Color(0.6f, 0.6f, 0.6f),
                Data.ResourceType.Metal => new Color(0.7f, 0.7f, 0.75f),
                _ => Color.white
            };
            renderer.material = mat;

            HarvestableResource resource = obj.AddComponent<HarvestableResource>();
            NetworkObject netObj = obj.AddComponent<NetworkObject>();

            netObj.Spawn();
            spawnedObjects.Add(obj);
        }
    }
}
