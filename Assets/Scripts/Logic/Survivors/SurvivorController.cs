using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ZombieSurvival.Core;
using ZombieSurvival.Data.Characters;
using ZombieSurvival.Interfaces;
using ZombieSurvival.Logic.Combat;
using ZombieSurvival.Logic.Weapons;

namespace ZombieSurvival.Logic.Survivors
{
    /// <summary>
    /// Controls individual survivor behavior and state
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(HealthSystem))]
    public class SurvivorController : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private SurvivorData survivorData;
        [SerializeField] private Transform weaponSocket;
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string walkAnimParameter = "IsWalking";
        [SerializeField] private string attackAnimParameter = "IsAttacking";
        [SerializeField] private string deathAnimParameter = "IsDead";
        
        [Header("AI Settings")]
        [SerializeField] private float stoppingDistance = 1.5f;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private LayerMask enemyLayers;
        
        // Components
        private NavMeshAgent _navAgent;
        private HealthSystem _healthSystem;
        
        // State
        private bool _isDead = false;
        private SurvivorTask _currentTask;
        private Transform _moveTarget;
        private Transform _attackTarget;
        private WeaponController _equippedWeapon;
        private float _lastAttackTime;
        
        // Events
        /// <summary>
        /// Event fired when the survivor dies
        /// </summary>
        public event Action<SurvivorController> OnSurvivorDied;
        
        /// <summary>
        /// Event fired when the survivor's task changes
        /// </summary>
        public event Action<SurvivorTask> OnTaskChanged;
        
        /// <summary>
        /// Event fired when the survivor's health changes
        /// </summary>
        public event Action<float, float> OnHealthChanged;
        
        #region Properties
        /// <summary>
        /// Current health of the survivor
        /// </summary>
        public float CurrentHealth => _healthSystem != null ? _healthSystem.CurrentHealth : 0;
        
        /// <summary>
        /// Maximum health of the survivor
        /// </summary>
        public float MaxHealth => _healthSystem != null ? _healthSystem.MaxHealth : 0;
        
        /// <summary>
        /// Name of the survivor
        /// </summary>
        public string SurvivorName => survivorData != null ? survivorData.characterName : "Unknown Survivor";
        
        /// <summary>
        /// Primary skill of the survivor
        /// </summary>
        public SurvivorSkill PrimarySkill => survivorData != null ? survivorData.primarySkill : SurvivorSkill.Combat;
        
        /// <summary>
        /// Skill level of the survivor (0-100)
        /// </summary>
        public int SkillLevel => survivorData != null ? survivorData.skillLevel : 0;
        
        /// <summary>
        /// Current task the survivor is performing
        /// </summary>
        public SurvivorTask CurrentTask => _currentTask;
        
        /// <summary>
        /// Whether the survivor is idle (not performing a task)
        /// </summary>
        public bool IsIdle => _currentTask == null;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _healthSystem = GetComponent<HealthSystem>();
            
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        private void OnEnable()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnDeath += HandleDeath;
            }
        }
        
        private void OnDisable()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnDeath -= HandleDeath;
            }
        }
        
        private void Start()
        {
            // Initialize with data if provided
            if (survivorData != null)
            {
                Initialize(survivorData);
            }
        }
        
        public async void Initialize(SurvivorData data)
        {
            survivorData = data;
            
            // Initialize health system
            if (_healthSystem != null)
            {
                _healthSystem.Initialize(data.maxHealth);
            }
            
            // Configure NavMeshAgent
            if (_navAgent != null)
            {
                _navAgent.speed = data.movementSpeed;
                _navAgent.stoppingDistance = stoppingDistance;
            }
            
            // Load and equip starting weapon if available
            if (data.startingWeaponAddresses != null && data.startingWeaponAddresses.Count > 0 && weaponSocket != null)
            {
                string weaponAddress = data.startingWeaponAddresses[0];
                
                if (!string.IsNullOrEmpty(weaponAddress))
                {
                    GameObject weaponPrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(weaponAddress);
                    
                    if (weaponPrefab != null)
                    {
                        GameObject weaponInstance = Instantiate(weaponPrefab, weaponSocket);
                        _equippedWeapon = weaponInstance.GetComponent<WeaponController>();
                        
                        if (_equippedWeapon != null)
                        {
                            // Initialize weapon (assuming it can work with survivor owner)
                            // This might need adaptation based on how your weapon system works
                        }
                    }
                }
            }
        }
        
        private void Update()
        {
            if (_isDead) return;
            
            // State machine for survivor behavior
            if (_currentTask != null)
            {
                // Performing assigned task
                UpdateTaskBehavior();
            }
            else
            {
                // Idle behavior - either follow player or defend area
                UpdateIdleBehavior();
            }
            
            // Update animation
            UpdateAnimation();
        }
        #endregion
        
        #region Behavior
        /// <summary>
        /// Update task-related behavior
        /// </summary>
        private void UpdateTaskBehavior()
        {
            if (_currentTask == null) return;
            
            // Handle different task types
            switch (_currentTask.TaskType)
            {
                case SurvivorTaskType.Defend:
                    UpdateDefendBehavior();
                    break;
                    
                case SurvivorTaskType.Resource:
                    UpdateResourceBehavior();
                    break;
                    
                case SurvivorTaskType.Follow:
                    UpdateFollowBehavior();
                    break;
                    
                case SurvivorTaskType.Build:
                    UpdateBuildBehavior();
                    break;
            }
            
            // Update task progress
            _currentTask.UpdateProgress(Time.deltaTime, CalculateEfficiency());
            
            // Check if task is complete
            if (_currentTask.IsComplete)
            {
                CompleteTask();
            }
        }
        
        /// <summary>
        /// Update idle behavior
        /// </summary>
        private void UpdateIdleBehavior()
        {
            // Look for nearby enemies
            if (DetectEnemies())
            {
                // Handle combat
                UpdateCombatBehavior();
            }
            else
            {
                // Patrol or stay in place
                UpdatePatrolBehavior();
            }
        }
        
        /// <summary>
        /// Update defend task behavior
        /// </summary>
        private void UpdateDefendBehavior()
        {
            // Stay at defense position and attack enemies
            if (_currentTask != null && _currentTask.TargetPosition.HasValue)
            {
                // Move to defense position if needed
                if (Vector3.Distance(transform.position, _currentTask.TargetPosition.Value) > stoppingDistance)
                {
                    _navAgent.SetDestination(_currentTask.TargetPosition.Value);
                }
                else
                {
                    // At position, look for enemies
                    if (DetectEnemies())
                    {
                        UpdateCombatBehavior();
                    }
                    else
                    {
                        // Stand and face different directions periodically
                        // Could implement a random rotation behavior here
                    }
                }
            }
        }
        
        /// <summary>
        /// Update resource gathering task behavior
        /// </summary>
        private void UpdateResourceBehavior()
        {
            // Move to resource and gather
            if (_currentTask != null && _currentTask.TargetPosition.HasValue)
            {
                // Move to resource position if needed
                if (Vector3.Distance(transform.position, _currentTask.TargetPosition.Value) > stoppingDistance)
                {
                    _navAgent.SetDestination(_currentTask.TargetPosition.Value);
                }
                else
                {
                    // At position, perform gathering animation/action
                    // This should increment the task progress
                    
                    // Face the resource
                    Vector3 lookDirection = _currentTask.TargetPosition.Value - transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(lookDirection),
                            10f * Time.deltaTime
                        );
                    }
                }
            }
        }
        
        /// <summary>
        /// Update follow task behavior
        /// </summary>
        private void UpdateFollowBehavior()
        {
            // Follow target (usually player)
            if (_currentTask != null && _currentTask.TargetTransform != null)
            {
                // Get the follow target
                Transform followTarget = _currentTask.TargetTransform;
                
                // Move to target if too far
                float distanceToTarget = Vector3.Distance(transform.position, followTarget.position);
                if (distanceToTarget > stoppingDistance)
                {
                    _navAgent.SetDestination(followTarget.position);
                }
                else
                {
                    // Close enough, stop moving
                    _navAgent.ResetPath();
                    
                    // Face the same direction as target
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        followTarget.rotation,
                        5f * Time.deltaTime
                    );
                }
                
                // Look for enemies and help in combat
                if (DetectEnemies())
                {
                    UpdateCombatBehavior();
                }
            }
        }
        
        /// <summary>
        /// Update build task behavior
        /// </summary>
        private void UpdateBuildBehavior()
        {
            // Move to build location and construct
            if (_currentTask != null && _currentTask.TargetPosition.HasValue)
            {
                // Move to build position if needed
                if (Vector3.Distance(transform.position, _currentTask.TargetPosition.Value) > stoppingDistance)
                {
                    _navAgent.SetDestination(_currentTask.TargetPosition.Value);
                }
                else
                {
                    // At position, perform building animation/action
                    // This should increment the task progress
                    
                    // Face the building site
                    Vector3 lookDirection = _currentTask.TargetPosition.Value - transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(lookDirection),
                            10f * Time.deltaTime
                        );
                    }
                    
                    // Building animation would be triggered here
                }
            }
        }
        
        /// <summary>
        /// Update patrol behavior
        /// </summary>
        private void UpdatePatrolBehavior()
        {
            // Simple patrol or idle behavior when no task or enemies
            // Could implement wandering around a base location
        }
        
        /// <summary>
        /// Update combat behavior
        /// </summary>
        private void UpdateCombatBehavior()
        {
            if (_attackTarget == null) return;
            
            // Calculate distance to target
            float distanceToTarget = Vector3.Distance(transform.position, _attackTarget.position);
            
            // Check if target is in attack range
            float attackRange = _equippedWeapon != null ? _equippedWeapon.Damage : survivorData.attackRange;
            
            if (distanceToTarget <= attackRange)
            {
                // Stop moving
                _navAgent.ResetPath();
                
                // Face target
                FaceTarget(_attackTarget);
                
                // Try to attack
                TryAttack();
            }
            else
            {
                // Move towards target
                _navAgent.SetDestination(_attackTarget.position);
            }
        }
        
        /// <summary>
        /// Try to attack the current target
        /// </summary>
        private void TryAttack()
        {
            if (_attackTarget == null) return;
            
            // Calculate attack cooldown
            float attackCooldown = _equippedWeapon != null ? 1.0f / _equippedWeapon.Damage : 1.0f / survivorData.attackRate;
            
            // Check if enough time has passed since last attack
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                _lastAttackTime = Time.time;
                
                // Trigger attack animation
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                    animator.SetBool(attackAnimParameter, true);
                }
                
                // Attack with weapon if available
                if (_equippedWeapon != null)
                {
                    _equippedWeapon.TryAttack();
                }
                else
                {
                    // Perform melee attack
                    PerformMeleeAttack();
                }
                
                // Reset attack animation after a delay
                StartCoroutine(ResetAttackAnimation(0.5f));
            }
        }
        
        /// <summary>
        /// Reset attack animation after a delay
        /// </summary>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator ResetAttackAnimation(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (animator != null)
            {
                animator.SetBool(attackAnimParameter, false);
            }
        }
        
        /// <summary>
        /// Perform melee attack if no weapon equipped
        /// </summary>
        private void PerformMeleeAttack()
        {
            if (_attackTarget == null) return;
            
            // Check if target is in range
            float distanceToTarget = Vector3.Distance(transform.position, _attackTarget.position);
            if (distanceToTarget <= survivorData.attackRange)
            {
                // Apply damage to target
                IDamageable damageable = _attackTarget.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(survivorData.attackDamage, transform);
                }
            }
        }
        
        /// <summary>
        /// Detect enemies in range
        /// </summary>
        /// <returns>True if enemies detected</returns>
        private bool DetectEnemies()
        {
            // Check for enemies in detection range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayers);
            
            if (hitColliders.Length > 0)
            {
                // Find closest enemy
                Transform closestEnemy = null;
                float closestDistance = float.MaxValue;
                
                foreach (var hitCollider in hitColliders)
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = hitCollider.transform;
                    }
                }
                
                if (closestEnemy != null)
                {
                    _attackTarget = closestEnemy;
                    return true;
                }
            }
            
            // No enemies found
            _attackTarget = null;
            return false;
        }
        
        /// <summary>
        /// Face a target
        /// </summary>
        /// <param name="target">Target to face</param>
        private void FaceTarget(Transform target)
        {
            if (target == null) return;
            
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Update animation states
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null) return;
            
            // Update walking animation
            bool isMoving = _navAgent != null && _navAgent.velocity.magnitude > 0.1f;
            animator.SetBool(walkAnimParameter, isMoving);
        }
        #endregion
        
        #region Task Management
        /// <summary>
        /// Assign a task to the survivor
        /// </summary>
        /// <param name="task">Task to assign</param>
        public void AssignTask(SurvivorTask task)
        {
            if (task == null) return;
            
            // Clear current task if any
            if (_currentTask != null)
            {
                _currentTask.ClearAssignment();
            }
            
            // Assign new task
            _currentTask = task;
            
            // Notify listeners
            OnTaskChanged?.Invoke(_currentTask);
            
            // Initialize task behavior
            if (_currentTask.TargetPosition.HasValue)
            {
                // Move to task position
                _navAgent.SetDestination(_currentTask.TargetPosition.Value);
            }
        }
        
        /// <summary>
        /// Clear the current task
        /// </summary>
        public void ClearTask()
        {
            if (_currentTask == null) return;
            
            SurvivorTask oldTask = _currentTask;
            _currentTask = null;
            
            // Stop movement
            if (_navAgent != null)
            {
                _navAgent.ResetPath();
            }
            
            // Notify listeners
            OnTaskChanged?.Invoke(null);
        }
        
        /// <summary>
        /// Complete the current task
        /// </summary>
        private void CompleteTask()
        {
            if (_currentTask == null) return;
            
            SurvivorTask completedTask = _currentTask;
            _currentTask = null;
            
            // Notify task of completion
            completedTask.CompleteTask();
            
            // Stop movement
            if (_navAgent != null)
            {
                _navAgent.ResetPath();
            }
            
            // Notify listeners
            OnTaskChanged?.Invoke(null);
        }
        
        /// <summary>
        /// Calculate the survivor's efficiency at current task
        /// </summary>
        /// <returns>Efficiency multiplier (1.0 is baseline)</returns>
        private float CalculateEfficiency()
        {
            if (_currentTask == null) return 1.0f;
            
            float efficiency = 1.0f;
            
            // Adjust based on skill match
            if (survivorData.primarySkill == _currentTask.RequiredSkill)
            {
                efficiency *= 1.5f;
            }
            
            // Adjust based on skill level (0.5 to 1.5 range)
            efficiency *= 0.5f + (survivorData.skillLevel / 100f);
            
            // Health penalty if injured
            if (_healthSystem != null)
            {
                float healthPercentage = _healthSystem.CurrentHealth / _healthSystem.MaxHealth;
                if (healthPercentage < 0.5f)
                {
                    efficiency *= healthPercentage + 0.5f; // Scale from 0.5 to 1.0
                }
            }
            
            return efficiency;
        }
        #endregion
        
        #region Health and Damage
        /// <summary>
        /// Take damage implementation from IDamageable
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        /// <param name="damageSource">Source of the damage</param>
        /// <returns>True if damage was applied</returns>
        public bool TakeDamage(float damage, Transform damageSource)
        {
            if (_isDead || _healthSystem == null) return false;
            
            // Apply damage through health system
            bool damageApplied = _healthSystem.TakeDamage(damage, damageSource);
            
            // If damage was from an enemy, set it as attack target
            if (damageApplied && damageSource != null)
            {
                // Check if source is an enemy
                if (((1 << damageSource.gameObject.layer) & enemyLayers) != 0)
                {
                    _attackTarget = damageSource;
                }
            }
            
            return damageApplied;
        }
        
        /// <summary>
        /// Handle health changed event
        /// </summary>
        /// <param name="currentHealth">Current health</param>
        /// <param name="maxHealth">Max health</param>
        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            // Notify listeners
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// Handle death event
        /// </summary>
        private void HandleDeath()
        {
            if (_isDead) return;
            
            _isDead = true;
            
            // Stop all behavior
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }
            
            // Clear current task
            if (_currentTask != null)
            {
                _currentTask.ClearAssignment();
                _currentTask = null;
            }
            
            // Play death animation
            if (animator != null)
            {
                animator.SetBool(deathAnimParameter, true);
                animator.SetTrigger("Die");
            }
            
            // Disable collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Notify listeners
            OnSurvivorDied?.Invoke(this);
            
            // Schedule destruction
            StartCoroutine(DestroyAfterDelay(5f));
        }
        
        /// <summary>
        /// Destroy game object after delay
        /// </summary>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        #endregion
        
        #region Debug Visualization
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, survivorData != null ? survivorData.attackRange : 1.5f);
            
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        #endregion
    }
}