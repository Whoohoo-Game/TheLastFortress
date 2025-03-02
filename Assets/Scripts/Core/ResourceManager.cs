using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Manages all resources in the game
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Singleton
        private static ResourceManager _instance;
        
        /// <summary>
        /// Singleton instance of ResourceManager
        /// </summary>
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ResourceManager");
                    _instance = go.AddComponent<ResourceManager>();
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
            
            InitializeResources();
        }
        #endregion
        
        [Header("Resource Settings")]
        [SerializeField] private List<ResourceDefinition> resourceDefinitions;
        
        // Resource data
        private Dictionary<string, int> _resourceAmounts = new Dictionary<string, int>();
        private Dictionary<string, int> _resourceCapacities = new Dictionary<string, int>();
        
        // Events
        /// <summary>
        /// Event fired when a resource amount changes
        /// </summary>
        public event Action<string, int, int> OnResourceChanged; // Resource ID, new amount, capacity
        
        /// <summary>
        /// Event fired when a resource capacity changes
        /// </summary>
        public event Action<string, int> OnResourceCapacityChanged; // Resource ID, new capacity
        
        #region Initialization
        /// <summary>
        /// Initialize resource system
        /// </summary>
        private void InitializeResources()
        {
            // Initialize all defined resources
            if (resourceDefinitions != null)
            {
                foreach (var def in resourceDefinitions)
                {
                    // Set initial amount to zero or starting amount
                    _resourceAmounts[def.resourceID] = def.startingAmount;
                    
                    // Set initial capacity
                    _resourceCapacities[def.resourceID] = def.initialCapacity;
                }
            }
        }
        #endregion
        
        #region Resource Management
        /// <summary>
        /// Get the current amount of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <returns>Amount of the resource</returns>
        public int GetResourceAmount(string resourceID)
        {
            if (string.IsNullOrEmpty(resourceID)) return 0;
            
            return _resourceAmounts.ContainsKey(resourceID) ? _resourceAmounts[resourceID] : 0;
        }
        
        /// <summary>
        /// Get the current capacity of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <returns>Capacity of the resource</returns>
        public int GetResourceCapacity(string resourceID)
        {
            if (string.IsNullOrEmpty(resourceID)) return 0;
            
            return _resourceCapacities.ContainsKey(resourceID) ? _resourceCapacities[resourceID] : 0;
        }
        
        /// <summary>
        /// Add an amount of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <param name="amount">Amount to add</param>
        /// <returns>Actual amount added (may be less if at capacity)</returns>
        public int AddResource(string resourceID, int amount)
        {
            if (string.IsNullOrEmpty(resourceID) || amount <= 0) return 0;
            
            // Ensure resource is initialized
            if (!_resourceAmounts.ContainsKey(resourceID))
            {
                _resourceAmounts[resourceID] = 0;
            }
            
            if (!_resourceCapacities.ContainsKey(resourceID))
            {
                _resourceCapacities[resourceID] = int.MaxValue;
            }
            
            // Calculate how much can be added
            int currentAmount = _resourceAmounts[resourceID];
            int capacity = _resourceCapacities[resourceID];
            int canAdd = Mathf.Min(amount, capacity - currentAmount);
            
            if (canAdd <= 0) return 0;
            
            // Add the resource
            _resourceAmounts[resourceID] = currentAmount + canAdd;
            
            // Notify listeners
            OnResourceChanged?.Invoke(resourceID, _resourceAmounts[resourceID], capacity);
            
            return canAdd;
        }
        
        /// <summary>
        /// Use an amount of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <param name="amount">Amount to use</param>
        /// <returns>True if successful, false if not enough</returns>
        public bool UseResource(string resourceID, int amount)
        {
            if (string.IsNullOrEmpty(resourceID) || amount <= 0) return false;
            
            // Check if resource exists and has enough
            if (!_resourceAmounts.ContainsKey(resourceID) || _resourceAmounts[resourceID] < amount)
            {
                return false;
            }
            
            // Use the resource
            _resourceAmounts[resourceID] -= amount;
            
            // Notify listeners
            OnResourceChanged?.Invoke(resourceID, _resourceAmounts[resourceID], GetResourceCapacity(resourceID));
            
            return true;
        }
        
        /// <summary>
        /// Check if there is enough of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <param name="amount">Amount to check for</param>
        /// <returns>True if there is enough</returns>
        public bool HasEnoughResource(string resourceID, int amount)
        {
            if (string.IsNullOrEmpty(resourceID) || amount <= 0) return true;
            
            return _resourceAmounts.ContainsKey(resourceID) && _resourceAmounts[resourceID] >= amount;
        }
        
        /// <summary>
        /// Set the capacity of a resource
        /// </summary>
        /// <param name="resourceID">ID of the resource</param>
        /// <param name="capacity">New capacity</param>
        public void SetResourceCapacity(string resourceID, int capacity)
        {
            if (string.IsNullOrEmpty(resourceID) || capacity < 0) return;
            
            // Set the new capacity
            _resourceCapacities[resourceID] = capacity;
            
            // Ensure amount doesn't exceed capacity
            if (_resourceAmounts.ContainsKey(resourceID) && _resourceAmounts[resourceID] > capacity)
            {
                _resourceAmounts[resourceID] = capacity;
                
                // Notify listeners for amount change
                OnResourceChanged?.Invoke(resourceID, _resourceAmounts[resourceID], capacity);
            }
            
            // Notify listeners for capacity change
            OnResourceCapacityChanged?.Invoke(resourceID, capacity);
        }
        
        /// <summary>
        /// Get all resource IDs
        /// </summary>
        /// <returns>List of resource IDs</returns>
        public List<string> GetAllResourceIDs()
        {
            List<string> result = new List<string>();
            
            foreach (var pair in _resourceAmounts)
            {
                result.Add(pair.Key);
            }
            
            return result;
        }
        #endregion
    }
    
    /// <summary>
    /// Defines a resource type
    /// </summary>
    [System.Serializable]
    public class ResourceDefinition
    {
        public string resourceID;
        public string displayName;
        public string description;
        public Sprite icon;
        public int startingAmount = 0;
        public int initialCapacity = 100;
        public bool canBeNegative = false;
    }
}