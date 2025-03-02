// WeaponController.cs
using System;
using UnityEngine;
using ZombieSurvival.Data.Weapons;
using ZombieSurvival.Interfaces;
using ZombieSurvival.Logic.Player;

namespace ZombieSurvival.Logic.Weapons
{
    /// <summary>
    /// Base class for all weapon controllers
    /// </summary>
    public abstract class WeaponController : MonoBehaviour, IWeapon
    {
        [Header("Base Weapon Properties")]
        [SerializeField] protected WeaponData weaponData;
        [SerializeField] protected Transform muzzlePoint;
        [SerializeField] protected AudioSource audioSource;
        
        // References
        protected PlayerController owner;
        
        // Timing
        protected float lastAttackTime;
        
        // Properties from IWeapon interface
        /// <summary>
        /// Current ammo in the weapon
        /// </summary>
        public abstract int CurrentAmmo { get; }
        
        /// <summary>
        /// Total remaining ammo for the weapon
        /// </summary>
        public abstract int TotalAmmo { get; }
        
        /// <summary>
        /// Name of the weapon
        /// </summary>
        public string WeaponName => weaponData != null ? weaponData.weaponName : "Unknown Weapon";
        
        /// <summary>
        /// Base damage of the weapon
        /// </summary>
        public float Damage => weaponData != null ? weaponData.baseDamage : 0f;
        
        // Events
        /// <summary>
        /// Event fired when the weapon attacks
        /// </summary>
        public event Action OnWeaponFired;
        
        /// <summary>
        /// Event fired when the weapon is reloaded
        /// </summary>
        public event Action OnWeaponReloaded;
        
        /// <summary>
        /// Event fired when the weapon's ammo changes
        /// </summary>
        public event Action<int, int> OnAmmoChanged;
        
        /// <summary>
        /// Initialize the weapon with an owner
        /// </summary>
        /// <param name="playerController">Player that owns this weapon</param>
        public virtual void Initialize(PlayerController playerController)
        {
            owner = playerController;
            lastAttackTime = -1000f; // Ensure weapon can be fired immediately
            
            if (weaponData == null)
            {
                Debug.LogError("WeaponData is null in WeaponController", this);
            }
        }
        
        /// <summary>
        /// Try to attack with the weapon
        /// </summary>
        /// <returns>True if attack was successful</returns>
        public abstract bool TryAttack();
        
        /// <summary>
        /// Try to reload the weapon
        /// </summary>
        /// <returns>True if reload was successful</returns>
        public abstract bool TryReload();
        
        /// <summary>
        /// Switch the weapon's firing mode
        /// </summary>
        /// <returns>True if mode was switched</returns>
        public abstract bool SwitchFireMode();
        
        /// <summary>
        /// Check if enough time has passed since the last attack
        /// </summary>
        /// <returns>True if weapon can attack again</returns>
        protected bool CanAttackBasedOnRate()
        {
            if (weaponData == null) return false;
            
            return Time.time >= lastAttackTime + (1f / weaponData.attackRate);
        }
        
        /// <summary>
        /// Play attack sound
        /// </summary>
        protected void PlayAttackSound()
        {
            if (audioSource != null && weaponData != null && !string.IsNullOrEmpty(weaponData.attackSoundAddress))
            {
                // Play sound through audio system
                // In a real implementation, this would use AudioManager or similar
                // audioSource.PlayOneShot(sound);
            }
        }
    }
}