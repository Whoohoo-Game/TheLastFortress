using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieSurvival.Core;
using ZombieSurvival.Data.Characters;
using ZombieSurvival.Interfaces;

namespace ZombieSurvival.Logic.Survivors
{
    /// <summary>
    /// Manages all survivors in the player's base
    /// </summary>
    public class SurvivorManager : MonoBehaviour
    {
        #region Singleton
        private static SurvivorManager _instance;
        
        /// <summary>
        /// Singleton instance of SurvivorManager
        /// </summary>
        public static SurvivorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SurvivorManager");
                    _instance = go.AddComponent<SurvivorManager>();
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
        }
        #endregion
        
        [Header("Survivor Settings")]
        [SerializeField] private int maxSurvivors = 10;
        [SerializeField] private float recruitCooldown = 300f; // 5 minutes
        [SerializeField] private Transform survivorSpawnPoint;
        
        [Header("Survivor Generation")]
        [SerializeField] private SurvivorData[] survivorTemplates;
        [SerializeField] private string[] maleNames;
        [SerializeField] private string[] femaleNames;
        
        // Runtime data
        private List<SurvivorController> _activeSurvivors = new List<SurvivorController>();
        private List<SurvivorTask> _availableTasks = new List<SurvivorTask>();
        private float _nextRecruitTime = 0f;
        
        // Events
        /// <summary>
        /// Event fired when a survivor is recruited
        /// </summary>
        public event Action<SurvivorController> OnSurvivorRecruited;
        
        /// <summary>
        /// Event fired when a survivor is lost (died or left)
        /// </summary>
        public event Action<SurvivorController> OnSurvivorLost;
        
        /// <summary>
        /// Event fired when available tasks are updated
        /// </summary>
        public event Action<List<SurvivorTask>> OnTasksUpdated;
        
        #region Properties
        /// <summary>
        /// Current number of survivors
        /// </summary>
        public int SurvivorCount => _activeSurvivors.Count;
        
        /// <summary>
        /// Maximum number of survivors
        /// </summary>
        public int MaxSurvivors => maxSurvivors;
        
        /// <summary>
        /// Whether new survivors can be recruited
        /// </summary>
        public bool CanRecruitSurvivors => SurvivorCount < MaxSurvivors && Time.time >= _nextRecruitTime;
        
        /// <summary>
        /// Time until next recruitment is available
        /// </summary>
        public float TimeUntilRecruitAvailable => Mathf.Max(0, _nextRecruitTime - Time.time);
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            // Initialize any survivors that should be present at start
            InitializeStartingSurvivors();
        }
        
        private void Update()
        {
            // Update survivor states and tasks
            UpdateSurvivors();
        }
        #endregion
        
        #region Survivor Management
        /// <summary>
        /// Initialize starting survivors (if any)
        /// </summary>
        private void InitializeStartingSurvivors()
        {
            // Add initial survivors if needed
            // For example, start with 1-2 basic survivors
        }
        
        /// <summary>
        /// Update all survivors
        /// </summary>
        private void UpdateSurvivors()
        {
            foreach (var survivor in _activeSurvivors)
            {
                // Update survivor state, like checking if they need food, rest, etc.
                if (survivor.CurrentTask == null && _availableTasks.Count > 0)
                {
                    // Assign available task based on skill matching
                    AssignBestTask(survivor);
                }
            }
        }
        
        /// <summary>
        /// Recruit a new survivor
        /// </summary>
        /// <param name="survivorData">Optional specific survivor data</param>
        /// <returns>The recruited survivor controller</returns>
        public async System.Threading.Tasks.Task<SurvivorController> RecruitSurvivorAsync(SurvivorData survivorData = null)
        {
            if (SurvivorCount >= MaxSurvivors)
            {
                Debug.LogWarning("Cannot recruit more survivors. Maximum reached.");
                return null;
            }
            
            // Set recruitment cooldown
            _nextRecruitTime = Time.time + recruitCooldown;
            
            // If no specific data provided, generate random survivor
            if (survivorData == null)
            {
                survivorData = GenerateRandomSurvivor();
            }
            
            // Load survivor prefab
            GameObject survivorPrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(survivorData.characterPrefabAddress);
            if (survivorPrefab == null)
            {
                Debug.LogError($"Failed to load survivor prefab at address: {survivorData.characterPrefabAddress}");
                return null;
            }
            
            // Determine spawn position
            Vector3 spawnPosition = survivorSpawnPoint != null ? survivorSpawnPoint.position : transform.position;
            
            // Instantiate survivor
            GameObject survivorInstance = Instantiate(survivorPrefab, spawnPosition, Quaternion.identity);
            SurvivorController survivorController = survivorInstance.GetComponent<SurvivorController>();
            
            if (survivorController == null)
            {
                Debug.LogError("Spawned survivor doesn't have a SurvivorController component");
                Destroy(survivorInstance);
                return null;
            }
            
            // Initialize survivor
            survivorController.Initialize(survivorData);
            
            // Subscribe to survivor events
            survivorController.OnSurvivorDied += HandleSurvivorDied;
            
            // Add to active survivors
            _activeSurvivors.Add(survivorController);
            
            // Notify listeners
            OnSurvivorRecruited?.Invoke(survivorController);
            
            return survivorController;
        }
        
        /// <summary>
        /// Generate random survivor data
        /// </summary>
        /// <returns>Random survivor data</returns>
        private SurvivorData GenerateRandomSurvivor()
        {
            if (survivorTemplates == null || survivorTemplates.Length == 0)
            {
                Debug.LogError("No survivor templates configured");
                return null;
            }
            
            // Start with random template
            SurvivorData template = survivorTemplates[UnityEngine.Random.Range(0, survivorTemplates.Length)];
            
            // Create a copy to customize
            SurvivorData newSurvivor = ScriptableObject.CreateInstance<SurvivorData>();
            
            // Copy base values
            newSurvivor.characterPrefabAddress = template.characterPrefabAddress;
            newSurvivor.portraitAddress = template.portraitAddress;
            newSurvivor.maxHealth = template.maxHealth;
            newSurvivor.movementSpeed = template.movementSpeed;
            newSurvivor.baseArmor = template.baseArmor;
            newSurvivor.maxStamina = template.maxStamina;
            newSurvivor.staminaRegenRate = template.staminaRegenRate;
            
            // Copy survivor-specific values
            newSurvivor.primarySkill = template.primarySkill;
            newSurvivor.skillLevel = template.skillLevel;
            newSurvivor.attackDamage = template.attackDamage;
            newSurvivor.attackRange = template.attackRange;
            newSurvivor.attackRate = template.attackRate;
            newSurvivor.startingWeaponAddresses = new List<string>(template.startingWeaponAddresses);
            
            // Randomize some values
            newSurvivor.skillLevel = UnityEngine.Random.Range(
                Mathf.Max(template.skillLevel - 20, 0), 
                Mathf.Min(template.skillLevel + 20, 100)
            );
            
            // Assign random name
            bool isMale = UnityEngine.Random.value > 0.5f;
            if (isMale && maleNames != null && maleNames.Length > 0)
            {
                newSurvivor.characterName = maleNames[UnityEngine.Random.Range(0, maleNames.Length)];
            }
            else if (!isMale && femaleNames != null && femaleNames.Length > 0)
            {
                newSurvivor.characterName = femaleNames[UnityEngine.Random.Range(0, femaleNames.Length)];
            }
            else
            {
                newSurvivor.characterName = "Survivor " + SurvivorCount;
            }
            
            return newSurvivor;
        }
        
        /// <summary>
        /// Assign the best available task to a survivor
        /// </summary>
        /// <param name="survivor">Survivor to assign task to</param>
        /// <returns>True if task was assigned</returns>
        private bool AssignBestTask(SurvivorController survivor)
        {
            if (_availableTasks.Count == 0) return false;
            
            // Find best task based on survivor skills
            SurvivorTask bestTask = null;
            float bestMatchScore = 0f;
            
            foreach (var task in _availableTasks)
            {
                if (task.IsTaskAssigned) continue;
                
                float matchScore = CalculateTaskMatchScore(survivor, task);
                if (matchScore > bestMatchScore)
                {
                    bestMatchScore = matchScore;
                    bestTask = task;
                }
            }
            
            // Assign best task if found
            if (bestTask != null)
            {
                survivor.AssignTask(bestTask);
                bestTask.AssignSurvivor(survivor);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate how well a survivor matches a task
        /// </summary>
        /// <param name="survivor">Survivor to check</param>
        /// <param name="task">Task to match</param>
        /// <returns>Match score (higher is better)</returns>
        private float CalculateTaskMatchScore(SurvivorController survivor, SurvivorTask task)
        {
            float score = 1.0f; // Base score
            
            // Bonus if primary skill matches task type
            if (survivor.PrimarySkill == task.RequiredSkill)
            {
                score += 2.0f;
            }
            
            // Bonus based on skill level
            score += survivor.SkillLevel / 50.0f;
            
            return score;
        }
        
        /// <summary>
        /// Handle survivor death
        /// </summary>
        /// <param name="survivor">Survivor that died</param>
        private void HandleSurvivorDied(SurvivorController survivor)
        {
            // Unsubscribe from events
            survivor.OnSurvivorDied -= HandleSurvivorDied;
            
            // Remove from active survivors
            _activeSurvivors.Remove(survivor);
            
            // Notify listeners
            OnSurvivorLost?.Invoke(survivor);
        }
        #endregion
        
        #region Task Management
        /// <summary>
        /// Add a new task for survivors
        /// </summary>
        /// <param name="task">Task to add</param>
        public void AddTask(SurvivorTask task)
        {
            if (task == null) return;
            
            _availableTasks.Add(task);
            
            // Try to assign task to available survivors
            foreach (var survivor in _activeSurvivors)
            {
                if (survivor.CurrentTask == null)
                {
                    if (AssignBestTask(survivor))
                    {
                        break; // Task assigned
                    }
                }
            }
            
            // Notify listeners
            OnTasksUpdated?.Invoke(_availableTasks);
        }
        
        /// <summary>
        /// Remove a task
        /// </summary>
        /// <param name="task">Task to remove</param>
        public void RemoveTask(SurvivorTask task)
        {
            if (task == null) return;
            
            // If task is assigned, unassign survivor
            if (task.IsTaskAssigned && task.AssignedSurvivor != null)
            {
                task.AssignedSurvivor.ClearTask();
                task.ClearAssignment();
            }
            
            _availableTasks.Remove(task);
            
            // Notify listeners
            OnTasksUpdated?.Invoke(_availableTasks);
        }
        
        /// <summary>
        /// Get all survivors with a specific skill
        /// </summary>
        /// <param name="skill">Skill to search for</param>
        /// <returns>List of survivors with the skill</returns>
        public List<SurvivorController> GetSurvivorsBySkill(SurvivorSkill skill)
        {
            List<SurvivorController> result = new List<SurvivorController>();
            
            foreach (var survivor in _activeSurvivors)
            {
                if (survivor.PrimarySkill == skill)
                {
                    result.Add(survivor);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get all available survivors (not assigned to tasks)
        /// </summary>
        /// <returns>List of available survivors</returns>
        public List<SurvivorController> GetAvailableSurvivors()
        {
            List<SurvivorController> result = new List<SurvivorController>();
            
            foreach (var survivor in _activeSurvivors)
            {
                if (survivor.CurrentTask == null)
                {
                    result.Add(survivor);
                }
            }
            
            return result;
        }
        #endregion
    }
}