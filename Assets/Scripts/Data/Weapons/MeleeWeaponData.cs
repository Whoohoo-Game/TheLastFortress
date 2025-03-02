using UnityEngine;

namespace ZombieSurvival.Data.Weapons
{
    /// <summary>
    /// Data container for melee type weapons
    /// </summary>
    [CreateAssetMenu(fileName = "New Melee Weapon", menuName = "Zombie Survival/Weapons/Melee Weapon")]
    public class MeleeWeaponData : WeaponData
    {
        [Header("Melee Weapon Properties")]
        [Tooltip("Type of melee weapon")]
        public MeleeWeaponType meleeType;
        
        [Tooltip("Attack angle in degrees")]
        public float attackAngle = 60f;
        
        [Tooltip("Knockback force applied to hit targets")]
        public float knockbackForce = 5f;
        
        [Tooltip("Whether the weapon can perform a charged attack")]
        public bool canChargeAttack = false;
        
        [Tooltip("Maximum charge time in seconds")]
        public float maxChargeTime = 2f;
        
        [Tooltip("Damage multiplier when fully charged")]
        public float chargedDamageMultiplier = 2f;
        
        [Tooltip("Address of the swing effect prefab in Addressables")]
        public string swingEffectAddress;
    }
    
    /// <summary>
    /// Available melee weapon types
    /// </summary>
    public enum MeleeWeaponType
    {
        Knife,
        Bat,
        Sword,
        Axe,
        Hammer
    }
}