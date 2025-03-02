// HealthSystem.cs
using System;
using UnityEngine;
using ZombieSurvival.Interfaces;

namespace ZombieSurvival.Logic.Combat
{
    /// <summary>
    /// Generic health system component that can be attached to any entity requiring health
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool isInvulnerable = false;
        [SerializeField] private float damageMultiplier = 1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject damageEffectPrefab;
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private string hitSoundAddress;
        [SerializeField] private string deathSoundAddress;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Events
        /// <summary>
        /// Event fired when health changes
        /// </summary>
        public event Action<float, float> OnHealthChanged;
        
        /// <summary>
        /// Event fired when the entity dies
        /// </summary>
        public event Action OnDeath;
        
        /// <summary>
        /// Event fired when the entity takes damage
        /// </summary>
        public event Action<float, Transform> OnDamageTaken;
        
        // Properties
        /// <summary>
        /// Current health of the entity
        /// </summary>
        public float CurrentHealth
        {
            get => currentHealth;
            private set
            {
                float oldHealth = currentHealth;
                currentHealth = Mathf.Clamp(value, 0, maxHealth);
                
                if (oldHealth != currentHealth)
                {
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[HealthSystem] Health changed: {oldHealth} -> {currentHealth}", this);
                    }
                    
                    // Check for death
                    if (currentHealth <= 0 && oldHealth > 0)
                    {
                        Die();
                    }
                }
            }
        }
        
        /// <summary>
        /// Maximum health of the entity
        /// </summary>
        public float MaxHealth
        {
            get => maxHealth;
            set
            {
                if (value <= 0)
                {
                    Debug.LogWarning("[HealthSystem] Attempted to set MaxHealth to a value <= 0", this);
                    return;
                }
                
                float oldMaxHealth = maxHealth;
                maxHealth = value;
                
                // If max health increased, increase current health by the same percentage
                if (maxHealth > oldMaxHealth)
                {
                    float healthPercentage = oldMaxHealth > 0 ? currentHealth / oldMaxHealth : 0;
                    currentHealth = maxHealth * healthPercentage;
                }
                
                // If max health decreased, ensure current health doesn't exceed new max
                if (currentHealth > maxHealth)
                {
                    currentHealth = maxHealth;
                }
                
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[HealthSystem] Max health changed: {oldMaxHealth} -> {maxHealth}", this);
                }
            }
        }
        
        /// <summary>
        /// Whether the entity is still alive
        /// </summary>
        public bool IsAlive => currentHealth > 0;
        
        /// <summary>
        /// Whether the entity is invulnerable to damage
        /// </summary>
        public bool IsInvulnerable
        {
            get => isInvulnerable;
            set => isInvulnerable = value;
        }
        
        /// <summary>
        /// The multiplier applied to incoming damage
        /// </summary>
        public float DamageMultiplier
        {
            get => damageMultiplier;
            set => damageMultiplier = Mathf.Max(0, value);
        }
        
        /// <summary>
        /// Health as a normalized value (0-1)
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0;
        
        private void Awake()
        {
            // Ensure health values are valid at start
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
        
        private void Start()
        {
            // Notify listeners of initial health state
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        #region Health Management
        /// <summary>
        /// Initialize health system with specific values
        /// </summary>
        /// <param name="maxHealthValue">Maximum health value</param>
        /// <param name="startAtFullHealth">Whether to start at full health</param>
        public void Initialize(float maxHealthValue, bool startAtFullHealth = true)
        {
            if (maxHealthValue <= 0)
            {
                Debug.LogError("[HealthSystem] Cannot initialize with max health <= 0", this);
                return;
            }
            
            maxHealth = maxHealthValue;
            currentHealth = startAtFullHealth ? maxHealthValue : currentHealth;
            
            // Ensure current health doesn't exceed max
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (showDebugInfo)
            {
                Debug.Log($"[HealthSystem] Initialized with max health: {maxHealth}, current health: {currentHealth}", this);
            }
        }
        
        /// <summary>
        /// Take damage from a source
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        /// <param name="damageSource">Source of the damage</param>
        /// <returns>True if damage was applied</returns>
        public bool TakeDamage(float damage, Transform damageSource)
        {
            // Skip if already dead or invulnerable
            if (!IsAlive || IsInvulnerable || damage <= 0)
            {
                return false;
            }
            
            // Apply damage multiplier
            float modifiedDamage = damage * damageMultiplier;
            
            // Apply damage
            float oldHealth = currentHealth;
            CurrentHealth -= modifiedDamage;
            
            // Notify of damage taken
            OnDamageTaken?.Invoke(modifiedDamage, damageSource);
            
            // Show damage effect if available
            if (damageEffectPrefab != null)
            {
                Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Play hit sound if specified
            if (!string.IsNullOrEmpty(hitSoundAddress) && IsAlive)
            {
                // Play sound using audio system
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[HealthSystem] Took damage: {modifiedDamage}, Health: {oldHealth} -> {currentHealth}", this);
            }
            
            return true;
        }
        
        /// <summary>
        /// Heal the entity
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        /// <returns>True if healing was applied</returns>
        public bool Heal(float healAmount)
        {
            // Skip if already dead or at full health
            if (!IsAlive || healAmount <= 0 || Mathf.Approximately(currentHealth, maxHealth))
            {
                return false;
            }
            
            float oldHealth = currentHealth;
            CurrentHealth += healAmount;
            
            if (showDebugInfo)
            {
                Debug.Log($"[HealthSystem] Healed: {healAmount}, Health: {oldHealth} -> {currentHealth}", this);
            }
            
            return true;
        }
        
        /// <summary>
        /// Restore to full health
        /// </summary>
        public void RestoreFullHealth()
        {
            if (!IsAlive) return;
            
            CurrentHealth = maxHealth;
            
            if (showDebugInfo)
            {
                Debug.Log("[HealthSystem] Restored to full health", this);
            }
        }
        
        /// <summary>
        /// Kill the entity immediately
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;
            
            CurrentHealth = 0;
            Die();
        }
        
        /// <summary>
        /// Handle entity death
        /// </summary>
        private void Die()
        {
            if (showDebugInfo)
            {
                Debug.Log("[HealthSystem] Entity died", this);
            }
            
            // Show death effect if available
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Play death sound if specified
            if (!string.IsNullOrEmpty(deathSoundAddress))
            {
                // Play sound using audio system
            }
            
            // Notify listeners of death
            OnDeath?.Invoke();
        }
        #endregion
    }
}