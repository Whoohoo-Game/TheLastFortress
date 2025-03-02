// AddressableManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Manages loading and unloading of addressable assets
    /// </summary>
    public class AddressableManager : MonoBehaviour
    {
        #region Singleton
        private static AddressableManager _instance;
        
        /// <summary>
        /// Singleton instance of AddressableManager
        /// </summary>
        public static AddressableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AddressableManager");
                    _instance = go.AddComponent<AddressableManager>();
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
        
        #region Asset Tracking
        // Dictionary to track loaded assets and their operations
        private Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();
        
        // Cache for commonly used assets
        private Dictionary<string, UnityEngine.Object> _assetCache = new Dictionary<string, UnityEngine.Object>();
        
        // Callback for initialization completion
        public event Action OnInitializationComplete;
        
        // Track initialization status
        private bool _isInitialized = false;
        
        /// <summary>
        /// Returns true if the AddressableManager is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        #endregion
        
        #region Initialization
        /// <summary>
        /// Initialize the Addressable system
        /// </summary>
        public async void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("AddressableManager is already initialized");
                return;
            }
            
            try
            {
                // Initialize Addressables
                var initOperation = Addressables.InitializeAsync();
                await initOperation.Task;
                
                _isInitialized = true;
                OnInitializationComplete?.Invoke();
                
                Debug.Log("AddressableManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Addressables: {ex.Message}");
            }
        }
        #endregion
        
        #region Asset Loading
        /// <summary>
        /// Load an asset asynchronously
        /// </summary>
        /// <typeparam name="T">Type of asset to load</typeparam>
        /// <param name="address">Addressable address of the asset</param>
        /// <returns>Task containing the loaded asset</returns>
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Cannot load asset with null or empty address");
                return null;
            }
            
            // Check if the asset is already in cache
            if (_assetCache.TryGetValue(address, out UnityEngine.Object cachedAsset) && cachedAsset is T)
            {
                return cachedAsset as T;
            }
            
            try
            {
                // Check if we're already loading this asset
                if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle existingHandle))
                {
                    await existingHandle.Task;
                    return (T)existingHandle.Result;
                }
                
                // Load the asset
                var loadOperation = Addressables.LoadAssetAsync<T>(address);
                _loadedAssets[address] = loadOperation;
                
                await loadOperation.Task;
                
                if (loadOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    T result = loadOperation.Result;
                    
                    // Cache the result
                    _assetCache[address] = result;
                    
                    return result;
                }
                else
                {
                    Debug.LogError($"Failed to load asset at address: {address}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading asset at address {address}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Instantiate a prefab from addressables
        /// </summary>
        /// <param name="address">Addressable address of the prefab</param>
        /// <param name="position">Position to instantiate at</param>
        /// <param name="rotation">Rotation to instantiate with</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>Task containing the instantiated GameObject</returns>
        public async Task<GameObject> InstantiatePrefabAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Cannot instantiate prefab with null or empty address");
                return null;
            }
            
            try
            {
                var instantiateOperation = Addressables.InstantiateAsync(address, position, rotation, parent);
                await instantiateOperation.Task;
                
                if (instantiateOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    return instantiateOperation.Result;
                }
                else
                {
                    Debug.LogError($"Failed to instantiate prefab at address: {address}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error instantiating prefab at address {address}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Load and initialize multiple assets of the same type
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <param name="addresses">List of addressable addresses</param>
        /// <returns>Task containing loaded assets</returns>
        public async Task<List<T>> LoadAssetsAsync<T>(List<string> addresses) where T : UnityEngine.Object
        {
            if (addresses == null || addresses.Count == 0)
            {
                return new List<T>();
            }
            
            List<T> results = new List<T>();
            List<Task<T>> loadTasks = new List<Task<T>>();
            
            foreach (string address in addresses)
            {
                loadTasks.Add(LoadAssetAsync<T>(address));
            }
            
            T[] loadedAssets = await Task.WhenAll(loadTasks);
            results.AddRange(loadedAssets);
            
            return results;
        }
        #endregion
        
        #region Asset Release
        /// <summary>
        /// Release a loaded asset
        /// </summary>
        /// <param name="address">Address of the asset to release</param>
        public void ReleaseAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }
            
            if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _loadedAssets.Remove(address);
                _assetCache.Remove(address);
            }
        }
        
        /// <summary>
        /// Release a GameObject that was instantiated from Addressables
        /// </summary>
        /// <param name="gameObject">GameObject to release</param>
        public void ReleaseInstance(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Addressables.ReleaseInstance(gameObject);
            }
        }
        
        /// <summary>
        /// Clear all cached assets
        /// </summary>
        public void ClearCache()
        {
            foreach (var handle in _loadedAssets.Values)
            {
                Addressables.Release(handle);
            }
            
            _loadedAssets.Clear();
            _assetCache.Clear();
            
            Debug.Log("AddressableManager cache cleared");
        }
        #endregion
        
        private void OnDestroy()
        {
            // Release all assets when the manager is destroyed
            ClearCache();
        }
    }
}