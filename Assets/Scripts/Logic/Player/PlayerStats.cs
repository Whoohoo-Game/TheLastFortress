using System;
using UnityEngine;
using ZombieSurvival.Data.Characters;

namespace ZombieSurvival.Logic.Player
{
    /// <summary>
    /// Manages player statistics such as health, stamina, and armor
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        #region Properties
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Private backing fields
        private float _currentHealth;
        private float _maxHealth = 100f;
        private float _currentStamina;
        private float _maxStamina = 100f;
        private float _armorValue;
        private float _staminaRegenRate = 10f;
        
        /// <summary>
        /// Current health of the player
        /// </summary>
        public float CurrentHealth
        {
            get => _currentHealth;
            private set
            {
                float clampedValue = Mathf.Clamp(value, 0, _maxHealth);
                if (_currentHealth != clampedValue)
                {
                    _currentHealth = clampedValue;
                    OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                }
            }
        }
        
        /// <summary>
        /// Maximum health of the player
        /// </summary>
        public float MaxHealth
        {
            get => _maxHealth;
            private set
            {
                if (_maxHealth != value)
                {
                    _maxHealth = value;
                    OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                }
            }
        }
        
        /// <summary>
        /// Current stamina of the player
        /// </summary>
        public float CurrentStamina
        {
            get => _currentStamina;
            private set
            {
                float clampedValue = Mathf.Clamp(value, 0, _maxStamina);
                if (_currentStamina != clampedValue)
                {
                    _currentStamina = clampedValue;
                    OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                }
            }
        }
        
        /// <summary>
        /// Maximum stamina of the player
        /// </summary>
        public float MaxStamina
        {
            get => _maxStamina;
            private set
            {
                if (_maxStamina != value)
                {
                    _maxStamina = value;
                    OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                }
            }
        }
        
        /// <summary>
        /// Armor value of the player (damage reduction percentage)
        /// </summary>
        public float ArmorValue
        {
            get => _armorValue;
            private set => _armorValue = Mathf.Clamp(value, 0, 100);
        }
        
        /// <summary>
        /// Whether the player's health is critically low
        /// </summary>
        public bool IsHealthCritical => CurrentHealth <= (_maxHealth * 0.25f);
        
        /// <summary>
        /// Whether the player is at full health
        /// </summary>
        public bool IsFullHealth => Mathf.Approximately(CurrentHealth, _maxHealth);
        
        /// <summary>
        /// Current health as a normalized value (0-1)
        /// </summary>
        public float HealthPercentage => _maxHealth > 0 ? CurrentHealth / _maxHealth : 0;
        
        /// <summary>
        /// Current stamina as a normalized value (0-1)
        /// </summary>
        public float StaminaPercentage => _maxStamina > 0 ? CurrentStamina / _maxStamina : 0;
        #endregion
        
        #region Events
        /// <summary>
        /// Event fired when health changes
        /// </summary>
        public event Action<float, float> OnHealthChanged;
        
        /// <summary>
        /// Event fired when stamina changes
        /// </summary>
        public event Action<float, float> OnStaminaChanged;
        
        /// <summary>
        /// Event fired when armor value changes
        /// </summary>
        public event Action<float> OnArmorChanged;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Initialize with default values
            _currentHealth = _maxHealth;
            _currentStamina = _maxStamina;
        }
        
        private void Update()
        {
            // Regenerate stamina over time when not at max
            if (CurrentStamina < MaxStamina)
            {
                RegenerateStamina(Time.deltaTime);
            }
            
            // Debug info
            if (showDebugInfo)
            {
                Debug.Log($"Health: {CurrentHealth}/{MaxHealth}, Stamina: {CurrentStamina}/{MaxStamina}, Armor: {ArmorValue}%");
            }
        }
        #endregion
        
        #region Initialization
        /// <summary>
        /// Initialize player stats from data
        /// </summary>
        /// <param name="data">Player data to initialize from</param>
        public void Initialize(PlayerData data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot initialize player stats with null data");
                return;
            }
            
            // Set base stats from data
            MaxHealth = data.maxHealth;
            CurrentHealth = MaxHealth;
            
            MaxStamina = data.maxStamina;
            CurrentStamina = MaxStamina;
            
            ArmorValue = data.baseArmor;
            
            _staminaRegenRate = data.staminaRegenRate;
            
            Debug.Log($"Player stats initialized: Health={MaxHealth}, Stamina={MaxStamina}, Armor={ArmorValue}%");
        }
        #endregion
        
        #region Health Methods
        /// <summary>
        /// Apply damage to the player, accounting for armor
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        /// <returns>True if damage was applied</returns>
        public bool TakeDamage(float damageAmount)
        {
            if (damageAmount <= 0) return false;
            
            // Apply armor damage reduction
            float actualDamage = damageAmount * (1 - (ArmorValue / 100f));
            
            // Apply damage
            CurrentHealth -= actualDamage;
            
            return true;
        }
        
        /// <summary>
        /// Heal the player
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        /// <returns>True if healing was applied</returns>
        public bool Heal(float healAmount)
        {
            if (healAmount <= 0 || IsFullHealth) return false;
            
            CurrentHealth += healAmount;
            return true;
        }
        
        /// <summary>
        /// Fully restore player health
        /// </summary>
        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
        }
        
        /// <summary>
        /// Apply a health modifier to maximum health
        /// </summary>
        /// <param name="modifier">Modifier to apply (additive)</param>
        public void ApplyMaxHealthModifier(float modifier)
        {
            if (modifier == 0) return;
            
            float oldMaxHealth = MaxHealth;
            MaxHealth += modifier;
            
            // Adjust current health proportionally if max was increased
            if (modifier > 0)
            {
                CurrentHealth += modifier;
            }
            // If max health was decreased, ensure current doesn't exceed max
            else
            {
                CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            }
        }
        #endregion
        
        #region Stamina Methods
        /// <summary>
        /// Consume stamina for actions like sprinting
        /// </summary>
        /// <param name="amount">Amount of stamina to consume</param>
        /// <returns>True if stamina was consumed</returns>
        public bool ConsumeStamina(float amount)
        {
            if (amount <= 0 || CurrentStamina <= 0) return false;
            
            CurrentStamina -= amount;
            return true;
        }
        
        /// <summary>
        /// Regenerate stamina over time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        private void RegenerateStamina(float deltaTime)
        {
            CurrentStamina += _staminaRegenRate * deltaTime;
        }
        
        /// <summary>
        /// Fully restore player stamina
        /// </summary>
        public void RestoreFullStamina()
        {
            CurrentStamina = MaxStamina;
        }
        
        /// <summary>
        /// Apply a stamina modifier to maximum stamina
        /// </summary>
        /// <param name="modifier">Modifier to apply (additive)</param>
        public void ApplyMaxStaminaModifier(float modifier)
        {
            if (modifier == 0) return;
            
            float oldMaxStamina = MaxStamina;
            MaxStamina += modifier;
            
            // Adjust current stamina proportionally if max was increased
            if (modifier > 0)
            {
                CurrentStamina += modifier;
            }
            // If max stamina was decreased, ensure current doesn't exceed max
            else
            {
                CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);
            }
        }
        #endregion
        
        #region Armor Methods
        /// <summary>
        /// Set the armor value directly
        /// </summary>
        /// <param name="newArmorValue">New armor value (0-100)</param>
        public void SetArmorValue(float newArmorValue)
        {
            float oldArmorValue = ArmorValue;
            ArmorValue = newArmorValue;
            
            if (oldArmorValue != ArmorValue)
            {
                OnArmorChanged?.Invoke(ArmorValue);
            }
        }
        
        /// <summary>
        /// Apply an armor modifier
        /// </summary>
        /// <param name="modifier">Modifier to apply (additive)</param>
        public void ApplyArmorModifier(float modifier)
        {
            if (modifier == 0) return;
            
            float oldArmorValue = ArmorValue;
            ArmorValue += modifier;
            
            if (oldArmorValue != ArmorValue)
            {
                OnArmorChanged?.Invoke(ArmorValue);
            }
        }
        #endregion
    }
}