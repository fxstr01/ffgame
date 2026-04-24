using UnityEngine;
using System.Collections.Generic;

namespace DestructionRoyale.Utils
{
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public struct PoolEntry
        {
            public string tag;
            public GameObject prefab;
            public int initialSize;
            public int maxSize;
        }

        [SerializeField] private List<PoolEntry> pools = new List<PoolEntry>();

        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, PoolEntry> poolEntries = new Dictionary<string, PoolEntry>();

        private static ObjectPool instance;
        public static ObjectPool Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (PoolEntry entry in pools)
            {
                Queue<GameObject> queue = new Queue<GameObject>();
                poolEntries[entry.tag] = entry;

                for (int i = 0; i < entry.initialSize; i++)
                {
                    GameObject obj = CreateObject(entry);
                    queue.Enqueue(obj);
                }

                poolDictionary[entry.tag] = queue;
            }
        }

        private GameObject CreateObject(PoolEntry entry)
        {
            GameObject obj = Instantiate(entry.prefab, transform);
            obj.SetActive(false);
            return obj;
        }

        public GameObject Get(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag '{tag}' does not exist.");
                return null;
            }

            Queue<GameObject> queue = poolDictionary[tag];
            GameObject obj;

            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                PoolEntry entry = poolEntries[tag];
                obj = CreateObject(entry);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        public void Return(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }
    }
}
