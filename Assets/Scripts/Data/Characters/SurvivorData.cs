using UnityEngine;
using System.Collections.Generic;

namespace ZombieSurvival.Data.Characters
{
    /// <summary>
    /// Data container for survivor NPCs
    /// </summary>
    [CreateAssetMenu(fileName = "New Survivor", menuName = "Zombie Survival/Characters/Survivor")]
    public class SurvivorData : CharacterData
    {
        [Header("Survivor Specific Properties")]
        [Tooltip("Survivor's primary skill")]
        public SurvivorSkill primarySkill;
        
        [Tooltip("Survivor's skill level (0-100)")]
        [Range(0, 100)]
        public int skillLevel = 50;
        
        [Tooltip("Base attack damage")]
        public float attackDamage = 10f;
        
        [Tooltip("Attack range")]
        public float attackRange = 2f;
        
        [Tooltip("Attack rate in attacks per second")]
        public float attackRate = 1f;
        
        [Tooltip("Initial weapon addresses (from Addressables)")]
        public List<string> startingWeaponAddresses = new List<string>();
    }
    
    /// <summary>
    /// Available survivor skills
    /// </summary>
    public enum SurvivorSkill
    {
        Combat,
        Medical,
        Engineering,
        Farming,
        Scavenging,
        Crafting
    }
}