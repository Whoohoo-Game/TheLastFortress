// WeaponData.cs
using UnityEngine;

namespace ZombieSurvival.Data.Weapons
{
    /// <summary>
    /// Base class for all weapon data
    /// </summary>
    public abstract class WeaponData : ScriptableObject
    {
        [Header("Basic Weapon Properties")]
        [Tooltip("Name of the weapon")]
        public string weaponName = "Weapon";
        
        [Tooltip("Description of the weapon")]
        [TextArea(3, 5)]
        public string description = "Weapon description";
        
        [Tooltip("Base damage of the weapon")]
        public float baseDamage = 10f;
        
        [Tooltip("Attack rate in attacks per second")]
        public float attackRate = 1f;
        
        [Tooltip("Range of the weapon in units")]
        public float range = 2f;
        
        [Tooltip("Address of the weapon prefab in Addressables")]
        public string weaponPrefabAddress;
        
        [Tooltip("Address of the weapon icon in Addressables")]
        public string weaponIconAddress;
        
        [Tooltip("Sound played when attacking")]
        public string attackSoundAddress;
    }
}