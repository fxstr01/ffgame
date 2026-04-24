using UnityEngine;

namespace DestructionRoyale.Utils
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObj = new object();
        private static bool applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObj = new GameObject($"[Singleton] {typeof(T)}");
                            instance = singletonObj.AddComponent<T>();
                            DontDestroyOnLoad(singletonObj);
                        }
                    }
                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }
    }
}
