using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieSurvival.Core;

namespace ZombieSurvival.Logic.Inventory
{
    /// <summary>
    /// Manages player's inventory system
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        #region Singleton
        private static InventoryManager _instance;
        
        /// <summary>
        /// Singleton instance of InventoryManager
        /// </summary>
        public static InventoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("InventoryManager");
                    _instance = go.AddComponent<InventoryManager>();
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
            
            InitializeInventory();
        }
        #endregion
        
        [Header("Inventory Settings")]
        [SerializeField] private int maxInventorySlots = 20;
        [SerializeField] private int maxWeaponSlots = 3;
        [SerializeField] private int maxEquipmentSlots = 4; // Head, Torso, Legs, Feet
        
        // Inventory data
        private List<InventoryItem> _inventoryItems = new List<InventoryItem>();
        private List<InventoryItem> _equippedWeapons = new List<InventoryItem>();
        private Dictionary<EquipmentSlot, InventoryItem> _equippedEquipment = new Dictionary<EquipmentSlot, InventoryItem>();
        
        // Events
        /// <summary>
        /// Event fired when an item is added to inventory
        /// </summary>
        public event Action<InventoryItem> OnItemAdded;
        
        /// <summary>
        /// Event fired when an item is removed from inventory
        /// </summary>
        public event Action<InventoryItem> OnItemRemoved;
        
        /// <summary>
        /// Event fired when an item is equipped
        /// </summary>
        public event Action<InventoryItem, bool> OnItemEquipped; // Item, isWeapon
        
        /// <summary>
        /// Event fired when an item is unequipped
        /// </summary>
        public event Action<InventoryItem, bool> OnItemUnequipped; // Item, isWeapon
        
        #region Properties
        /// <summary>
        /// Current number of items in inventory
        /// </summary>
        public int ItemCount => _inventoryItems.Count;
        
        /// <summary>
        /// Maximum inventory capacity
        /// </summary>
        public int MaxInventorySlots => maxInventorySlots;
        
        /// <summary>
        /// Current number of equipped weapons
        /// </summary>
        public int EquippedWeaponCount => _equippedWeapons.Count;
        
        /// <summary>
        /// Maximum number of equipped weapons
        /// </summary>
        public int MaxWeaponSlots => maxWeaponSlots;
        #endregion
        
        #region Initialization
        /// <summary>
        /// Initialize inventory system
        /// </summary>
        private void InitializeInventory()
        {
            // Initialize equipment slots dictionary
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                _equippedEquipment[slot] = null;
            }
        }
        #endregion
        
        #region Inventory Management
        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <returns>True if successfully added</returns>
        public bool AddItem(InventoryItem item)
        {
            if (item == null) return false;
            
            // Check if inventory is full
            if (_inventoryItems.Count >= maxInventorySlots)
            {
                Debug.LogWarning("Inventory is full, cannot add item: " + item.ItemName);
                return false;
            }
            
            // Add item to inventory
            _inventoryItems.Add(item);
            
            // Notify listeners
            OnItemAdded?.Invoke(item);
            
            return true;
        }
        
        /// <summary>
        /// Remove an item from the inventory
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>True if successfully removed</returns>
        public bool RemoveItem(InventoryItem item)
        {
            if (item == null) return false;
            
            // Check if item is equipped
            if (_equippedWeapons.Contains(item))
            {
                UnequipWeapon(item);
            }
            
            foreach (var kvp in _equippedEquipment)
            {
                if (kvp.Value == item)
                {
                    UnequipEquipment(kvp.Key);
                    break;
                }
            }
            
            // Remove item from inventory
            bool removed = _inventoryItems.Remove(item);
            
            if (removed)
            {
                // Notify listeners
                OnItemRemoved?.Invoke(item);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Get all items in inventory
        /// </summary>
        /// <returns>List of inventory items</returns>
        public List<InventoryItem> GetAllItems()
        {
            return new List<InventoryItem>(_inventoryItems);
        }
        
        /// <summary>
        /// Get items of a specific type
        /// </summary>
        /// <param name="itemType">Type of items to get</param>
        /// <returns>List of inventory items of the specified type</returns>
        public List<InventoryItem> GetItemsByType(ItemType itemType)
        {
            List<InventoryItem> result = new List<InventoryItem>();
            
            foreach (var item in _inventoryItems)
            {
                if (item.Type == itemType)
                {
                    result.Add(item);
                }
            }
            
            return result;
        }
        #endregion
        
        #region Equipment Management
        /// <summary>
        /// Equip a weapon
        /// </summary>
        /// <param name="item">Weapon to equip</param>
        /// <returns>True if successfully equipped</returns>
        public bool EquipWeapon(InventoryItem item)
        {
            if (item == null || item.Type != ItemType.Weapon) return false;
            
            // Check if weapon slots are full
            if (_equippedWeapons.Count >= maxWeaponSlots)
            {
                Debug.LogWarning("All weapon slots are full, cannot equip: " + item.ItemName);
                return false;
            }
            
            // Ensure item is in inventory
            if (!_inventoryItems.Contains(item))
            {
                Debug.LogWarning("Cannot equip item that is not in inventory: " + item.ItemName);
                return false;
            }
            
            // Add to equipped weapons
            _equippedWeapons.Add(item);
            
            // Notify listeners
            OnItemEquipped?.Invoke(item, true);
            
            return true;
        }
        
        /// <summary>
        /// Unequip a weapon
        /// </summary>
        /// <param name="item">Weapon to unequip</param>
        /// <returns>True if successfully unequipped</returns>
        public bool UnequipWeapon(InventoryItem item)
        {
            if (item == null) return false;
            
            // Remove from equipped weapons
            bool removed = _equippedWeapons.Remove(item);
            
            if (removed)
            {
                // Notify listeners
                OnItemUnequipped?.Invoke(item, true);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Equip an equipment item
        /// </summary>
        /// <param name="item">Equipment to equip</param>
        /// <param name="slot">Equipment slot</param>
        /// <returns>True if successfully equipped</returns>
        public bool EquipEquipment(InventoryItem item, EquipmentSlot slot)
        {
            if (item == null || item.Type != ItemType.Equipment) return false;
            
            // Ensure item is in inventory
            if (!_inventoryItems.Contains(item))
            {
                Debug.LogWarning("Cannot equip item that is not in inventory: " + item.ItemName);
                return false;
            }
            
            // Check if slot is available for this item
            if (item.EquipmentSlot != slot)
            {
                Debug.LogWarning($"Cannot equip {item.ItemName} to {slot} slot");
                return false;
            }
            
            // Unequip current item in slot if any
            InventoryItem currentItem = _equippedEquipment[slot];
            if (currentItem != null)
            {
                UnequipEquipment(slot);
            }
            
            // Equip new item
            _equippedEquipment[slot] = item;
            
            // Notify listeners
            OnItemEquipped?.Invoke(item, false);
            
            return true;
        }
        
        /// <summary>
        /// Unequip an equipment item
        /// </summary>
        /// <param name="slot">Equipment slot to unequip</param>
        /// <returns>True if successfully unequipped</returns>
        public bool UnequipEquipment(EquipmentSlot slot)
        {
            // Get current item in slot
            InventoryItem currentItem = _equippedEquipment[slot];
            
            if (currentItem == null) return false;
            
            // Unequip item
            _equippedEquipment[slot] = null;
            
            // Notify listeners
            OnItemUnequipped?.Invoke(currentItem, false);
            
            return true;
        }
        
        /// <summary>
        /// Get all equipped weapons
        /// </summary>
        /// <returns>List of equipped weapons</returns>
        public List<InventoryItem> GetEquippedWeapons()
        {
            return new List<InventoryItem>(_equippedWeapons);
        }
        
        /// <summary>
        /// Get equipped item in a specific slot
        /// </summary>
        /// <param name="slot">Equipment slot</param>
        /// <returns>Equipped item or null if empty</returns>
        public InventoryItem GetEquippedItem(EquipmentSlot slot)
        {
            return _equippedEquipment[slot];
        }
        #endregion
    }
    
    /// <summary>
    /// Types of items
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Equipment,
        Consumable,
        Resource,
        Miscellaneous
    }
    
    /// <summary>
    /// Equipment slot types
    /// </summary>
    public enum EquipmentSlot
    {
        Head,
        Torso,
        Legs,
        Feet
    }
}