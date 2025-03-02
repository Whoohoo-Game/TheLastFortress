using UnityEngine;
using System.Collections.Generic;

namespace ZombieSurvival.Data.Characters
{
    /// <summary>
    /// Data container for player character
    /// </summary>
    [CreateAssetMenu(fileName = "Player Data", menuName = "Zombie Survival/Characters/Player")]
    public class PlayerData : CharacterData
    {
        [Header("Player Specific Properties")]
        [Tooltip("Sprint speed multiplier")]
        public float sprintMultiplier = 1.5f;
        
        [Tooltip("Stamina consumption rate while sprinting (per second)")]
        public float sprintStaminaCost = 20f;
        
        [Tooltip("Base inventory capacity (number of items)")]
        public int inventoryCapacity = 10;
        
        [Tooltip("Maximum number of weapons that can be carried")]
        public int maxWeapons = 3;
        
        [Tooltip("Initial weapon addresses (from Addressables)")]
        public List<string> startingWeaponAddresses = new List<string>();
        
        [Tooltip("Initial equipment addresses (from Addressables)")]
        public List<string> startingEquipmentAddresses = new List<string>();
        
        [Tooltip("Interaction range")]
        public float interactionRange = 2f;
    }
}