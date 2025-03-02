using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieSurvival.Interfaces;

namespace ZombieSurvival.Logic.Combat
{
    /// <summary>
    /// System for managing damage calculations, resistances, and modifiers
    /// </summary>
    public class DamageSystem : MonoBehaviour
    {
        #region Singleton
        private static DamageSystem _instance;
        
        /// <summary>
        /// Singleton instance of DamageSystem
        /// </summary>
        public static DamageSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DamageSystem");
                    _instance = go.AddComponent<DamageSystem>();
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
            
            InitializeDamageTypes();
        }
        #endregion

        [Header("Damage Settings")]
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private float criticalHitMultiplier = 2.0f;
        [SerializeField] private float headShotMultiplier = 1.5f;
        
        // Damage type definitions
        private Dictionary<DamageType, DamageTypeDefinition> _damageDefinitions = new Dictionary<DamageType, DamageTypeDefinition>();
        
        // Events
        /// <summary>
        /// Event fired when damage is applied
        /// </summary>
        public event Action<Transform, Transform, float, DamageType, bool> OnDamageApplied; // Target, Source, amount, damageType, isCritical

        #region Initialization
        /// <summary>
        /// Initialize the damage system with default damage types
        /// </summary>
        private void InitializeDamageTypes()
        {
            // Define standard damage types
            _damageDefinitions[DamageType.Physical] = new DamageTypeDefinition
            {
                name = "Physical",
                color = Color.white,
                description = "Standard physical damage from impacts and bullets"
            };
            
            _damageDefinitions[DamageType.Fire] = new DamageTypeDefinition
            {
                name = "Fire",
                color = Color.red,
                description = "Damage from fire and heat sources",
                applyStatusEffect = true,
                statusEffectPrefabAddress = "Prefabs/Effects/BurningEffect"
            };
            
            _damageDefinitions[DamageType.Explosive] = new DamageTypeDefinition
            {
                name = "Explosive",
                color = new Color(1.0f, 0.5f, 0.0f), // Orange
                description = "Area damage from explosions",
                areaEffectMultiplier = 1.5f
            };
            
            _damageDefinitions[DamageType.Toxic] = new DamageTypeDefinition
            {
                name = "Toxic",
                color = Color.green,
                description = "Damage from toxic substances and chemicals",
                applyStatusEffect = true,
                statusEffectPrefabAddress = "Prefabs/Effects/PoisonEffect"
            };
        }
        #endregion
        
        #region Damage Calculation
        /// <summary>
        /// Apply damage to a target
        /// </summary>
        /// <param name="target">Target to damage</param>
        /// <param name="source">Source of the damage</param>
        /// <param name="amount">Base damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="hitLocation">Optional hit location (for headshots, etc.)</param>
        /// <returns>Actual damage applied</returns>
        public float ApplyDamage(Transform target, Transform source, float amount, DamageType damageType, HitLocation hitLocation = HitLocation.Body)
        {
            if (target == null) return 0;
            
            // Get damageable component
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null) return 0;
            
            // Calculate final damage with modifiers
            float finalDamage = CalculateDamage(amount, damageType, hitLocation);
            
            // Check if this is a critical hit
            bool isCritical = IsRandomCritical() || hitLocation == HitLocation.Head;
            
            if (isCritical)
            {
                finalDamage *= criticalHitMultiplier;
            }
            
            // Apply the damage
            bool damageApplied = damageable.TakeDamage(finalDamage, source);
            
            if (damageApplied)
            {
                // Show damage numbers if enabled
                if (showDamageNumbers && damageNumberPrefab != null)
                {
                    ShowDamageNumber(target, finalDamage, damageType, isCritical);
                }
                
                // Apply any status effects
                if (_damageDefinitions.TryGetValue(damageType, out DamageTypeDefinition definition) && definition.applyStatusEffect)
                {
                    ApplyStatusEffect(target, definition, finalDamage);
                }
                
                // Handle area damage if applicable
                if (_damageDefinitions.TryGetValue(damageType, out definition) && definition.areaEffectMultiplier > 0)
                {
                    ApplyAreaDamage(target, source, amount, damageType, definition.areaEffectMultiplier);
                }
                
                // Notify listeners
                OnDamageApplied?.Invoke(target, source, finalDamage, damageType, isCritical);
            }
            
            return finalDamage;
        }
        
        /// <summary>
        /// Calculate damage based on damage type and hit location
        /// </summary>
        /// <param name="baseAmount">Base damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="hitLocation">Location that was hit</param>
        /// <returns>Modified damage amount</returns>
        private float CalculateDamage(float baseAmount, DamageType damageType, HitLocation hitLocation)
        {
            float locationMultiplier = 1.0f;
            
            // Apply hit location multiplier
            switch (hitLocation)
            {
                case HitLocation.Head:
                    locationMultiplier = headShotMultiplier;
                    break;
                case HitLocation.Limb:
                    locationMultiplier = 0.8f;
                    break;
                case HitLocation.Body:
                default:
                    locationMultiplier = 1.0f;
                    break;
            }
            
            return baseAmount * locationMultiplier;
        }
        
        /// <summary>
        /// Determine if this hit is a random critical hit
        /// </summary>
        /// <returns>True if critical hit</returns>
        private bool IsRandomCritical()
        {
            // 5% chance of critical hit by default
            return UnityEngine.Random.value < 0.05f;
        }
        
        /// <summary>
        /// Apply area damage around a target
        /// </summary>
        /// <param name="centerTarget">Center of the area damage</param>
        /// <param name="source">Source of the damage</param>
        /// <param name="baseAmount">Base damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="areaMultiplier">Multiplier for area effect</param>
        private void ApplyAreaDamage(Transform centerTarget, Transform source, float baseAmount, DamageType damageType, float areaMultiplier)
        {
            // Calculate radius based on damage amount and multiplier
            float radius = Mathf.Clamp(baseAmount * 0.1f, 2f, 10f);
            
            // Find all damageables in radius
            Collider[] colliders = Physics.OverlapSphere(centerTarget.position, radius);
            foreach (var collider in colliders)
            {
                // Skip the original target
                if (collider.transform == centerTarget) continue;
                
                // Apply reduced damage to nearby targets
                IDamageable nearbyDamageable = collider.GetComponent<IDamageable>();
                if (nearbyDamageable != null)
                {
                    // Calculate damage falloff based on distance
                    float distance = Vector3.Distance(centerTarget.position, collider.transform.position);
                    float falloff = 1f - Mathf.Clamp01(distance / radius);
                    
                    // Apply reduced damage
                    float areaDamage = baseAmount * falloff * 0.5f;
                    nearbyDamageable.TakeDamage(areaDamage, source);
                }
            }
        }
        #endregion
        
        #region Status Effects
        /// <summary>
        /// Apply status effect to a target
        /// </summary>
        /// <param name="target">Target to apply effect to</param>
        /// <param name="definition">Damage type definition</param>
        /// <param name="damageAmount">Amount of damage that caused the effect</param>
        private void ApplyStatusEffect(Transform target, DamageTypeDefinition definition, float damageAmount)
        {
            if (string.IsNullOrEmpty(definition.statusEffectPrefabAddress)) return;
            
            // In a real implementation, this would instantiate a status effect component
            // from the addressable system and attach it to the target
            
            // Example psuedo-code:
            // var statusEffect = AddressableManager.Instance.LoadAssetAsync<GameObject>(definition.statusEffectPrefabAddress);
            // var instance = Instantiate(statusEffect, target);
            // instance.GetComponent<StatusEffect>().Initialize(damageAmount);
            
            Debug.Log($"Applied {definition.name} status effect to {target.name}");
        }
        #endregion
        
        #region Visual Effects
        /// <summary>
        /// Show damage number above target
        /// </summary>
        /// <param name="target">Damaged target</param>
        /// <param name="amount">Damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="isCritical">Whether it was a critical hit</param>
        private void ShowDamageNumber(Transform target, float amount, DamageType damageType, bool isCritical)
        {
            if (damageNumberPrefab == null) return;
            
            // Get the color for this damage type
            Color textColor = Color.white;
            if (_damageDefinitions.TryGetValue(damageType, out DamageTypeDefinition definition))
            {
                textColor = definition.color;
            }
            
            // Calculate position above target
            Vector3 position = target.position + Vector3.up * 2f;
            
            // Create the damage number object
            GameObject damageNumber = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            
            // Get the component to set values
            DamageNumber damageNumberComponent = damageNumber.GetComponent<DamageNumber>();
            if (damageNumberComponent != null)
            {
                damageNumberComponent.Initialize(Mathf.RoundToInt(amount), textColor, isCritical);
            }
            else
            {
                // If no custom component, try to find text component and set it
                UnityEngine.UI.Text textComponent = damageNumber.GetComponentInChildren<UnityEngine.UI.Text>();
                if (textComponent != null)
                {
                    textComponent.text = Mathf.RoundToInt(amount).ToString();
                    textComponent.color = textColor;
                    
                    if (isCritical)
                    {
                        textComponent.fontSize *= 1.5f;
                    }
                }
            }
            
            // Destroy after delay
            Destroy(damageNumber, 2f);
        }
        #endregion
    }
    
    /// <summary>
    /// Types of damage in the game
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,
        Explosive,
        Toxic
    }
    
    /// <summary>
    /// Hit locations for damage calculations
    /// </summary>
    public enum HitLocation
    {
        Body,
        Head,
        Limb
    }
    
    /// <summary>
    /// Definition of a damage type
    /// </summary>
    [System.Serializable]
    public class DamageTypeDefinition
    {
        public string name;
        public Color color = Color.white;
        public string description;
        public bool applyStatusEffect = false;
        public string statusEffectPrefabAddress = "";
        public float areaEffectMultiplier = 0f;
    }
    
    /// <summary>
    /// Component for damage number display
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        public void Initialize(int amount, Color color, bool isCritical)
        {
            // Set text value and appearance
            // In a real implementation, this would update a UI element
            // and potentially animate it
        }
    }
}