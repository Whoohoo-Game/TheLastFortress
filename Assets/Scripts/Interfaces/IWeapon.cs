// IDamageable.cs
using UnityEngine;

namespace ZombieSurvival.Interfaces
{

    /// <summary>
    /// Interface for all weapons in the game
    /// </summary>
    public interface IWeapon
    {
        /// <summary>
        /// Try to attack with the weapon
        /// </summary>
        /// <returns>True if attack was successful, false otherwise</returns>
        bool TryAttack();
        
        /// <summary>
        /// Start reloading the weapon (if applicable)
        /// </summary>
        /// <returns>True if reload started, false otherwise</returns>
        bool TryReload();
        
        /// <summary>
        /// Switch the weapon's firing mode (if applicable)
        /// </summary>
        /// <returns>True if mode was switched, false otherwise</returns>
        bool SwitchFireMode();
        
        /// <summary>
        /// Get the current ammo count
        /// </summary>
        int CurrentAmmo { get; }
        
        /// <summary>
        /// Get the total remaining ammo
        /// </summary>
        int TotalAmmo { get; }
        
        /// <summary>
        /// Get the weapon's name
        /// </summary>
        string WeaponName { get; }
        
        /// <summary>
        /// Get the weapon's damage
        /// </summary>
        float Damage { get; }
    }
}