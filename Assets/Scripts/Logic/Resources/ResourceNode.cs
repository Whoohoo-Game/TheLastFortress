using System;
using UnityEngine;
using ZombieSurvival.Interfaces;

namespace ZombieSurvival.Logic.Resources
{
    /// <summary>
    /// Represents a resource node that can be harvested
    /// </summary>
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        [Header("Resource Configuration")]
        [SerializeField] private string resourceID;
        [SerializeField] private int totalAmount = 100;
        [SerializeField] private int amountPerHarvest = 10;
        [SerializeField] private float harvestTime = 2f;
        [SerializeField] private bool infiniteResource = false;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject harvestEffect;
        [SerializeField] private GameObject depletedEffect;
        [SerializeField] private GameObject nodeVisual;
        [SerializeField] private GameObject depletedVisual;
        
        // State
        private int _remainingAmount;
        private bool _isDepleted = false;
        private bool _isBeingHarvested = false;
        private float _harvestProgress = 0f;
        
        // Events
        /// <summary>
        /// Event fired when resource node is harvested
        /// </summary>
        public event Action<string, int> OnNodeHarvested; // Resource ID, amount
        
        /// <summary>
        /// Event fired when resource node is depleted
        /// </summary>
        public event Action<ResourceNode> OnNodeDepleted;
        
        #region Unity Lifecycle
        private void Awake()
        {
            _remainingAmount = totalAmount;
        }
        
        private void Start()
        {
            UpdateVisuals();
        }
        #endregion
        
        #region Harvesting
        /// <summary>
        /// Harvest resources from this node
        /// </summary>
        /// <param name="harvester">The entity harvesting</param>
        /// <param name="efficiency">Harvesting efficiency multiplier</param>
        /// <returns>Amount harvested</returns>
        public int Harvest(Transform harvester, float efficiency = 1.0f)
        {
            if (_isDepleted) return 0;
            
            // Calculate amount to harvest
            int harvestAmount = Mathf.RoundToInt(amountPerHarvest * efficiency);
            
            // Cap to remaining amount if needed
            if (!infiniteResource)
            {
                harvestAmount = Mathf.Min(harvestAmount, _remainingAmount);
                _remainingAmount -= harvestAmount;
                
                // Check if depleted
                if (_remainingAmount <= 0)
                {
                    SetDepleted();
                }
            }
            
            // Play harvest effect
            if (harvestEffect != null)
            {
                Instantiate(harvestEffect, transform.position, Quaternion.identity);
            }
            
            // Update visuals
            UpdateVisuals();
            
            // Notify listeners
            OnNodeHarvested?.Invoke(resourceID, harvestAmount);
            
            // Add to player resources
            Core.ResourceManager.Instance.AddResource(resourceID, harvestAmount);
            
            return harvestAmount;
        }
        
        /// <summary>
        /// Set the node as depleted
        /// </summary>
        private void SetDepleted()
        {
            _isDepleted = true;
            _remainingAmount = 0;
            
            // Play depleted effect
            if (depletedEffect != null)
            {
                Instantiate(depletedEffect, transform.position, Quaternion.identity);
            }
            
            // Update visuals
            UpdateVisuals();
            
            // Notify listeners
            OnNodeDepleted?.Invoke(this);
        }
        
        /// <summary>
        /// Update visual state of the node
        /// </summary>
        private void UpdateVisuals()
        {
            if (nodeVisual != null)
            {
                nodeVisual.SetActive(!_isDepleted);
            }
            
            if (depletedVisual != null)
            {
                depletedVisual.SetActive(_isDepleted);
            }
        }
        #endregion
        
        #region IInteractable Implementation
        /// <summary>
        /// Handle interaction from player
        /// </summary>
        /// <param name="interactor">Entity performing the interaction</param>
        public void Interact(Transform interactor)
        {
            if (_isDepleted) return;
            
            // Start harvesting process
            StartHarvesting(interactor);
        }
        
        /// <summary>
        /// Get interaction prompt text
        /// </summary>
        /// <returns>Prompt text for UI</returns>
        public string GetInteractionPrompt()
        {
            if (_isDepleted)
            {
                return "Depleted";
            }
            else
            {
                return $"Harvest {amountPerHarvest} {resourceID}";
            }
        }
        #endregion
        
        #region Harvesting Process
        /// <summary>
        /// Start the harvesting process
        /// </summary>
        /// <param name="harvester">Entity performing the harvest</param>
        private void StartHarvesting(Transform harvester)
        {
            if (_isBeingHarvested) return;
            
            _isBeingHarvested = true;
            _harvestProgress = 0f;
            
            // In a real implementation, this would start a coroutine for the harvest timer
            // and possibly show a progress bar. For simplicity, we'll just harvest immediately.
            FinishHarvesting(harvester);
        }
        
        /// <summary>
        /// Complete the harvesting process
        /// </summary>
        /// <param name="harvester">Entity performing the harvest</param>
        private void FinishHarvesting(Transform harvester)
        {
            // Harvest the resource
            Harvest(harvester);
            
            // Reset harvesting state
            _isBeingHarvested = false;
        }
        #endregion
        
        #region Debugging
        private void OnDrawGizmosSelected()
        {
            // Draw a sphere to indicate this is a resource node
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        #endregion
    }
}