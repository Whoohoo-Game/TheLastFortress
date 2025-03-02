// IDamageable.cs
using UnityEngine;

namespace ZombieSurvival.Interfaces
{
    /// <summary>
    /// Interface for any interactive object in the game
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Interaction method called when player interacts with the object
        /// </summary>
        /// <param name="interactor">The entity that is performing the interaction</param>
        void Interact(Transform interactor);
        
        /// <summary>
        /// Get the interaction prompt text to display
        /// </summary>
        /// <returns>Text to display when interaction is possible</returns>
        string GetInteractionPrompt();
    }
}