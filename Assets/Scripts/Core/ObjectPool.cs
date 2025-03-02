using System.Collections.Generic;
using UnityEngine;
using System;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Generic object pooling system for reusing game objects
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        #region Singleton
        private static ObjectPool _instance;
        
        /// <summary>
        /// Singleton instance of ObjectPool
        /// </summary>
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectPool");
                    _instance = go.AddComponent<ObjectPool>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion
        
        #region Pool Data Structure
        /// <summary>
        /// Pool dictionary that stores all object pools by prefab reference
        /// </summary>
        private Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
        
        /// <summary>
        /// Dictionary to track which pool an instantiated object belongs to
        /// </summary>
        private Dictionary<GameObject, GameObject> _instanceToPrefabMap = new Dictionary<GameObject, GameObject>();
        
        /// <summary>
        /// Parent transform for organizing pooled objects
        /// </summary>
        private Transform _poolContainer;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize a pool for a specific prefab
        /// </summary>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="initialSize">Initial number of instances to create</param>
        public void InitializePool(GameObject prefab, int initialSize = 10)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot initialize pool with null prefab");
                return;
            }
            
            if (_poolContainer == null)
            {
                GameObject containerGO = new GameObject("Pool Container");
                containerGO.transform.SetParent(transform);
                _poolContainer = containerGO.transform;
            }
            
            // Create pool GameObject container
            GameObject poolGO = new GameObject(prefab.name + " Pool");
            poolGO.transform.SetParent(_poolContainer);
            
            // Initialize queue if needed
            if (!_poolDictionary.ContainsKey(prefab))
            {
                _poolDictionary[prefab] = new Queue<GameObject>();
            }
            
            // Pre-instantiate objects
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreateNewInstance(prefab, poolGO.transform);
                _poolDictionary[prefab].Enqueue(obj);
            }
            
            Debug.Log($"Initialized pool for {prefab.name} with {initialSize} instances");
        }
        
        /// <summary>
        /// Get an object from the pool, or create one if the pool is empty
        /// </summary>
        /// <param name="prefab">Prefab to get from the pool</param>
        /// <param name="position">Position to set the object</param>
        /// <param name="rotation">Rotation to set the object</param>
        /// <returns>The pooled GameObject</returns>
        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot get null prefab from pool");
                return null;
            }
            
            // Initialize pool if it doesn't exist
            if (!_poolDictionary.ContainsKey(prefab))
            {
                InitializePool(prefab);
            }
            
            // Get or create instance
            GameObject obj;
            if (_poolDictionary[prefab].Count == 0)
            {
                // Create a new instance if pool is empty
                obj = CreateNewInstance(prefab, _poolContainer.Find(prefab.name + " Pool"));
            }
            else
            {
                // Get existing instance from pool
                obj = _poolDictionary[prefab].Dequeue();
            }
            
            // Position and enable the object
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            // Initialize poolable component if present
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnGetFromPool();
            }
            
            return obj;
        }
        
        /// <summary>
        /// Return an object to its pool
        /// </summary>
        /// <param name="obj">Object to return to pool</param>
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;
            
            // Check if object is from a pool
            if (!_instanceToPrefabMap.TryGetValue(obj, out GameObject prefab))
            {
                Debug.LogWarning($"Object {obj.name} was not created by the pool system");
                Destroy(obj);
                return;
            }
            
            // Call cleanup method if implementing IPoolable
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }
            
            // Deactivate and return to pool
            obj.SetActive(false);
            _poolDictionary[prefab].Enqueue(obj);
        }
        
        /// <summary>
        /// Clear all object pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _poolDictionary.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    Destroy(obj);
                }
            }
            
            _poolDictionary.Clear();
            _instanceToPrefabMap.Clear();
            
            Debug.Log("All object pools have been cleared");
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Create a new instance of a prefab for the pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="parent">Parent transform for the new instance</param>
        /// <returns>The created GameObject instance</returns>
        private GameObject CreateNewInstance(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.name = prefab.name;
            obj.SetActive(false);
            
            // Map instance to prefab
            _instanceToPrefabMap[obj] = prefab;
            
            return obj;
        }
        #endregion
    }
    
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from the pool
        /// </summary>
        void OnGetFromPool();
        
        /// <summary>
        /// Called when the object is returned to the pool
        /// </summary>
        void OnReturnToPool();
    }
}