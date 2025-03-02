using UnityEngine;

namespace ZombieSurvival.Logic.Inventory
{
    /// <summary>
    /// Represents an item in the inventory
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        // Basic properties
        public string ItemID;
        public string ItemName;
        public string Description;
        public ItemType Type;
        public Sprite Icon;
        
        // Equipment specific properties
        public EquipmentSlot EquipmentSlot;
        public float ArmorValue;
        
        // Weapon specific properties
        public string WeaponPrefabAddress;
        public float Damage;
        
        // Consumable specific properties
        public float HealthRestoreAmount;
        public float StaminaRestoreAmount;
        
        // Resource specific properties
        public string ResourceType;
        public int ResourceAmount;
    }
}