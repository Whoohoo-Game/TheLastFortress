using UnityEngine;

namespace ZombieSurvival.Data.Characters
{
    /// <summary>
    /// Base class for all character data
    /// </summary>
    public abstract class CharacterData : ScriptableObject
    {
        [Header("Basic Character Properties")]
        [Tooltip("Name of the character")]
        public string characterName = "Character";
        
        [Tooltip("Maximum health of the character")]
        public float maxHealth = 100f;
        
        [Tooltip("Base movement speed in units per second")]
        public float movementSpeed = 5f;
        
        [Tooltip("Base armor value (damage reduction percentage)")]
        public float baseArmor = 0f;
        
        [Tooltip("Stamina capacity")]
        public float maxStamina = 100f;
        
        [Tooltip("Stamina regeneration rate per second")]
        public float staminaRegenRate = 10f;
        
        [Tooltip("Address of the character prefab in Addressables")]
        public string characterPrefabAddress;
        
        [Tooltip("Address of the character portrait in Addressables")]
        public string portraitAddress;
    }
}