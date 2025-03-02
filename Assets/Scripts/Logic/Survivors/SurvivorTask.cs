// SurvivorTask.cs
using System;
using UnityEngine;
using ZombieSurvival.Data.Characters;

namespace ZombieSurvival.Logic.Survivors
{
    /// <summary>
    /// Types of tasks that survivors can perform
    /// </summary>
    public enum SurvivorTaskType
    {
        Defend,     // Defend an area
        Resource,   // Gather resources
        Follow,     // Follow a target (usually player)
        Build       // Build or repair structures
    }
    
    /// <summary>
    /// Represents a task that can be assigned to survivors
    /// </summary>
    public class SurvivorTask : MonoBehaviour
    {
        [Header("Task Configuration")]
        [SerializeField] private string taskName = "Task";
        [SerializeField] private string taskDescription = "Task description";
        [SerializeField] private SurvivorTaskType taskType = SurvivorTaskType.Defend;
        [SerializeField] private SurvivorSkill requiredSkill = SurvivorSkill.Combat;
        [SerializeField] private float taskDuration = 60f; // In seconds
        
        [Header("Task Location")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private bool useTransform = true;
        
        [Header("Task Rewards")]
        [SerializeField] private int experienceReward = 10;
        [SerializeField] private bool hasResourceReward = false;
        [SerializeField] private string resourceType = "";
        [SerializeField] private int resourceAmount = 0;
        
        // Runtime state
        private SurvivorController _assignedSurvivor;
        private float _progress = 0f; // 0 to 1
        private bool _isComplete = false;
        
        // Events
        /// <summary>
        /// Event fired when task progress is updated
        /// </summary>
        public event Action<float> OnProgressUpdated;
        
        /// <summary>
        /// Event fired when task is completed
        /// </summary>
        public event Action<SurvivorTask> OnTaskCompleted;
        
        #region Properties
        /// <summary>
        /// Name of the task
        /// </summary>
        public string TaskName => taskName;
        
        /// <summary>
        /// Description of the task
        /// </summary>
        public string TaskDescription => taskDescription;
        
        /// <summary>
        /// Type of the task
        /// </summary>
        public SurvivorTaskType TaskType => taskType;
        
        /// <summary>
        /// Required skill for the task
        /// </summary>
        public SurvivorSkill RequiredSkill => requiredSkill;
        
        /// <summary>
        /// Target transform for the task (if any)
        /// </summary>
        public Transform TargetTransform => targetTransform;
        
        /// <summary>
        /// Target position for the task
        /// </summary>
        public Vector3? TargetPosition
        {
            get
            {
                if (useTransform && targetTransform != null)
                {
                    return targetTransform.position;
                }
                else
                {
                    return targetPosition;
                }
            }
        }
        
        /// <summary>
        /// Current progress of the task (0-1)
        /// </summary>
        public float Progress => _progress;
        
        /// <summary>
        /// Whether the task is complete
        /// </summary>
        public bool IsComplete => _isComplete;
        
        /// <summary>
        /// Whether the task is assigned to a survivor
        /// </summary>
        public bool IsTaskAssigned => _assignedSurvivor != null;
        
        /// <summary>
        /// The survivor assigned to this task
        /// </summary>
        public SurvivorController AssignedSurvivor => _assignedSurvivor;
        #endregion
        
        #region Task Management
        /// <summary>
        /// Assign a survivor to this task
        /// </summary>
        /// <param name="survivor">Survivor to assign</param>
        public void AssignSurvivor(SurvivorController survivor)
        {
            if (survivor == null) return;
            
            _assignedSurvivor = survivor;
            _progress = 0f;
            _isComplete = false;
        }
        
        /// <summary>
        /// Clear the task assignment
        /// </summary>
        public void ClearAssignment()
        {
            _assignedSurvivor = null;
        }
        
        /// <summary>
        /// Update progress of the task
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <param name="efficiencyMultiplier">Efficiency multiplier of the assigned survivor</param>
        public void UpdateProgress(float deltaTime, float efficiencyMultiplier = 1.0f)
        {
            if (_isComplete || _assignedSurvivor == null) return;
            
            // Calculate progress increment
            float progressIncrement = (deltaTime / taskDuration) * efficiencyMultiplier;
            
            // Update progress
            _progress = Mathf.Clamp01(_progress + progressIncrement);
            
            // Notify listeners
            OnProgressUpdated?.Invoke(_progress);
            
            // Check for completion
            if (_progress >= 1.0f)
            {
                CompleteTask();
            }
        }
        
        /// <summary>
        /// Mark the task as complete
        /// </summary>
        public void CompleteTask()
        {
            if (_isComplete) return;
            
            _isComplete = true;
            _progress = 1.0f;
            
            // Award experience to survivor if assigned
            if (_assignedSurvivor != null)
            {
                // Would call some method to grant experience
                // _assignedSurvivor.AddExperience(experienceReward);
            }
            
            // Handle resource rewards
            if (hasResourceReward && !string.IsNullOrEmpty(resourceType) && resourceAmount > 0)
            {
                // Would call some method to add resources to inventory
                // ResourceManager.Instance.AddResource(resourceType, resourceAmount);
            }
            
            // Notify listeners
            OnTaskCompleted?.Invoke(this);
        }
        
        /// <summary>
        /// Reset the task to its initial state
        /// </summary>
        public void ResetTask()
        {
            _progress = 0f;
            _isComplete = false;
            _assignedSurvivor = null;
        }
        #endregion
    }
}