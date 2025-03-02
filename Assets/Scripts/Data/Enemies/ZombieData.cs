using UnityEngine;

namespace ZombieSurvival.Data.Enemies
{
    /// <summary>
    /// Data container for zombie enemies
    /// </summary>
    [CreateAssetMenu(fileName = "New Zombie", menuName = "Zombie Survival/Enemies/Zombie")]
    public class ZombieData : ScriptableObject
    {
        [Header("Basic Zombie Properties")]
        [Tooltip("Name of the zombie type")]
        public string zombieName = "Zombie";
        
        [Tooltip("Maximum health of the zombie")]
        public float maxHealth = 100f;
        
        [Tooltip("Base movement speed in units per second")]
        public float movementSpeed = 3f;
        
        [Tooltip("Damage dealt per attack")]
        public float attackDamage = 20f;
        
        [Tooltip("Attack range")]
        public float attackRange = 1.5f;
        
        [Tooltip("Time between attacks in seconds")]
        public float attackCooldown = 2f;
        
        [Tooltip("Chance to drop items when killed (0-1)")]
        [Range(0, 1)]
        public float dropChance = 0.3f;
        
        [Tooltip("Experience points awarded when killed")]
        public int experienceReward = 10;
        
        [Tooltip("Address of the zombie prefab in Addressables")]
        public string zombiePrefabAddress;
        
        [Header("AI Properties")]
        [Tooltip("Detection range for spotting targets")]
        public float detectionRange = 10f;
        
        [Tooltip("Maximum chase distance before giving up")]
        public float maxChaseDistance = 20f;
        
        [Tooltip("Whether this zombie type can climb obstacles")]
        public bool canClimbObstacles = false;
        
        [Tooltip("Whether this zombie type can break through barriers")]
        public bool canBreakBarriers = false;
        
        [Tooltip("Strength for breaking barriers (damage per second)")]
        public float barrierBreakStrength = 5f;
        
        [Header("Special Abilities")]
        [Tooltip("Whether this zombie has special abilities")]
        public bool hasSpecialAbilities = false;
        
        [Tooltip("Type of special ability")]
        public ZombieSpecialAbility specialAbility = ZombieSpecialAbility.None;
        
        [Tooltip("Cooldown time for special ability in seconds")]
        public float specialAbilityCooldown = 10f;
    }
    
    /// <summary>
    /// Available special abilities for zombies
    /// </summary>
    public enum ZombieSpecialAbility
    {
        None,
        Spitter,    // Ranged acid attack
        Screamer,   // Attracts more zombies
        Exploder,   // Explodes when near target
        Tank,       // Extra tough and strong
        Crawler     // Can move under barriers
    }
}