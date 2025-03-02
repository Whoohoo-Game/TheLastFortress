// IDamageable.cs
using UnityEngine;

namespace ZombieSurvival.Interfaces
{
    /// <summary>
    /// Interface for any entity that can receive damage
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Handle taking damage from a source
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        /// <param name="damageSource">The Transform of the damage source</param>
        /// <returns>True if the damage was successful, false otherwise</returns>
        bool TakeDamage(float damage, Transform damageSource);
        
        /// <summary>
        /// Current health of the entity
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Maximum health of the entity
        /// </summary>
        float MaxHealth { get; }
    }
}