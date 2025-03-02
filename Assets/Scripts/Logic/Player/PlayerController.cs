using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZombieSurvival.Core;
using ZombieSurvival.Data.Characters;
using ZombieSurvival.Interfaces;
using ZombieSurvival.Logic.Combat;
using ZombieSurvival.Logic.Weapons;

namespace ZombieSurvival.Logic.Player
{
    /// <summary>
    /// Main player controller that handles movement, input, and actions
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        #region References
        [Header("References")]
        [SerializeField] private Transform aimPivot;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Transform cameraTarget;
        
        [Header("Configuration")]
        [SerializeField] private PlayerData playerData;
        [SerializeField] private LayerMask interactionLayerMask;
        
        [Header("Effects")]
        [SerializeField] private string footstepSoundAddress;
        [SerializeField] private string hurtSoundAddress;
        [SerializeField] private string deathSoundAddress;
        
        // Component references
        private Rigidbody _rigidbody;
        private PlayerStats _playerStats;
        private Camera _mainCamera;
        private Animator _animator;
        
        // Current weapons
        private List<WeaponController> _weapons = new List<WeaponController>();
        private int _currentWeaponIndex = 0;
        private WeaponController _currentWeapon;
        #endregion
        
        #region Properties
        /// <summary>
        /// Current movement speed
        /// </summary>
        public float CurrentSpeed { get; private set; }
        
        /// <summary>
        /// Whether the player is currently sprinting
        /// </summary>
        public bool IsSprinting { get; private set; }
        
        /// <summary>
        /// Whether the player is currently dead
        /// </summary>
        public bool IsDead { get; private set; }
        
        /// <summary>
        /// Player's current health
        /// </summary>
        public float CurrentHealth => _playerStats.CurrentHealth;
        
        /// <summary>
        /// Player's maximum health
        /// </summary>
        public float MaxHealth => _playerStats.MaxHealth;
        
        /// <summary>
        /// Current player aim direction in world space
        /// </summary>
        public Vector3 AimDirection { get; private set; }
        
        /// <summary>
        /// Current stamina level
        /// </summary>
        public float CurrentStamina => _playerStats.CurrentStamina;
        
        /// <summary>
        /// Maximum stamina
        /// </summary>
        public float MaxStamina => _playerStats.MaxStamina;
        #endregion
        
        #region Events
        /// <summary>
        /// Event fired when player takes damage
        /// </summary>
        public event Action<float, float> OnHealthChanged;
        
        /// <summary>
        /// Event fired when player dies
        /// </summary>
        public event Action OnPlayerDied;
        
        /// <summary>
        /// Event fired when player's stamina changes
        /// </summary>
        public event Action<float, float> OnStaminaChanged;
        
        /// <summary>
        /// Event fired when player's weapon changes
        /// </summary>
        public event Action<WeaponController> OnWeaponChanged;
        
        /// <summary>
        /// Event fired when player interacts with an object
        /// </summary>
        public event Action<IInteractable> OnInteracted;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Get components
            _rigidbody = GetComponent<Rigidbody>();
            _playerStats = GetComponent<PlayerStats>();
            _animator = GetComponentInChildren<Animator>();
            _mainCamera = Camera.main;
            
            // Configure rigidbody
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Initialize properties
            CurrentSpeed = playerData != null ? playerData.movementSpeed : 5f;
            IsSprinting = false;
            IsDead = false;
        }
        
        private void OnEnable()
        {
            // Subscribe to input events
            InputManager.Instance.OnMove += HandleMove;
            InputManager.Instance.OnSprint += HandleSprintStart;
            InputManager.Instance.OnSprintReleased += HandleSprintEnd;
            InputManager.Instance.OnFirePressed += HandleFireStart;
            InputManager.Instance.OnFireReleased += HandleFireEnd;
            InputManager.Instance.OnReload += HandleReload;
            InputManager.Instance.OnSwitchFireMode += HandleSwitchFireMode;
            InputManager.Instance.OnSwitchWeapon += HandleSwitchWeapon;
            InputManager.Instance.OnInteract += HandleInteract;
            
            // Subscribe to player stats events
            if (_playerStats != null)
            {
                _playerStats.OnHealthChanged += HandleHealthChanged;
                _playerStats.OnStaminaChanged += HandleStaminaChanged;
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from input events
            InputManager.Instance.OnMove -= HandleMove;
            InputManager.Instance.OnSprint -= HandleSprintStart;
            InputManager.Instance.OnSprintReleased -= HandleSprintEnd;
            InputManager.Instance.OnFirePressed -= HandleFireStart;
            InputManager.Instance.OnFireReleased -= HandleFireEnd;
            InputManager.Instance.OnReload -= HandleReload;
            InputManager.Instance.OnSwitchFireMode -= HandleSwitchFireMode;
            InputManager.Instance.OnSwitchWeapon -= HandleSwitchWeapon;
            InputManager.Instance.OnInteract -= HandleInteract;
            
            // Unsubscribe from player stats events
            if (_playerStats != null)
            {
                _playerStats.OnHealthChanged -= HandleHealthChanged;
                _playerStats.OnStaminaChanged -= HandleStaminaChanged;
            }
        }
        
        private async void Start()
        {
            // Initialize player stats
            if (_playerStats != null && playerData != null)
            {
                _playerStats.Initialize(playerData);
            }
            
            // Load initial weapons
            await LoadInitialWeapons();
            
            // Set the first weapon as active if any are available
            if (_weapons.Count > 0)
            {
                SwitchToWeapon(0);
            }
        }
        
        private void Update()
        {
            if (IsDead || GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            {
                return;
            }
            
            UpdateAim();
            UpdateInteractionPrompt();
            
            // Handle auto-fire if current weapon supports it
            if (_currentWeapon != null && InputManager.Instance.IsFirePressed)
            {
                _currentWeapon.TryAttack();
            }
            
            // Update sprinting
            if (IsSprinting && InputManager.Instance.IsSprintPressed)
            {
                // Only sprint if moving and have stamina
                if (InputManager.Instance.MovementDirection.magnitude > 0.1f && _playerStats.CurrentStamina > 0)
                {
                    _playerStats.ConsumeStamina(playerData != null ? playerData.sprintStaminaCost * Time.deltaTime : 10f * Time.deltaTime);
                }
                else
                {
                    StopSprinting();
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (IsDead || GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            {
                return;
            }
            
            MovePlayer();
        }
        #endregion
        
        #region Movement
        private Vector2 _moveInput;
        
        /// <summary>
        /// Handle movement input
        /// </summary>
        /// <param name="moveDirection">Direction of movement</param>
        private void HandleMove(Vector2 moveDirection)
        {
            _moveInput = moveDirection;
        }
        
        /// <summary>
        /// Move the player based on input
        /// </summary>
        private void MovePlayer()
        {
            if (_moveInput.magnitude > 0.1f)
            {
                // Calculate movement vector
                Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
                
                float targetSpeed;
                if (playerData != null)
                {
                    targetSpeed = IsSprinting ? playerData.movementSpeed * playerData.sprintMultiplier : playerData.movementSpeed;
                }
                else
                {
                    targetSpeed = IsSprinting ? 7.5f : 5f; // Default values if playerData is null
                }
                
                // Apply movement
                _rigidbody.velocity = moveDirection * targetSpeed;
                
                // Update animation if available
                if (_animator != null)
                {
                    _animator.SetFloat("Speed", _rigidbody.velocity.magnitude);
                    _animator.SetBool("IsMoving", true);
                }
            }
            else
            {
                // Stop movement
                _rigidbody.velocity = Vector3.zero;
                
                // Update animation if available
                if (_animator != null)
                {
                    _animator.SetFloat("Speed", 0);
                    _animator.SetBool("IsMoving", false);
                }
            }
        }
        
        /// <summary>
        /// Handle sprint start
        /// </summary>
        private void HandleSprintStart()
        {
            if (IsDead || (_playerStats != null && _playerStats.CurrentStamina <= 0)) return;
            
            IsSprinting = true;
            
            // Update animation if available
            if (_animator != null)
            {
                _animator.SetBool("IsSprinting", true);
            }
        }
        
        /// <summary>
        /// Handle sprint end
        /// </summary>
        private void HandleSprintEnd()
        {
            StopSprinting();
        }
        
        /// <summary>
        /// Stop sprinting
        /// </summary>
        private void StopSprinting()
        {
            IsSprinting = false;
            
            // Update animation if available
            if (_animator != null)
            {
                _animator.SetBool("IsSprinting", false);
            }
        }
        #endregion
        
        #region Aiming
        private Vector3 _aimWorldPosition;
        
        /// <summary>
        /// Update player's aim direction based on mouse position
        /// </summary>
        private void UpdateAim()
        {
            if (_mainCamera == null) return;
            
            // Get mouse world position
            _aimWorldPosition = InputManager.Instance.GetMouseWorldPosition(_mainCamera);
            
            // Calculate aim direction (ignore Y)
            Vector3 targetPosition = new Vector3(_aimWorldPosition.x, transform.position.y, _aimWorldPosition.z);
            AimDirection = (targetPosition - transform.position).normalized;
            
            // Rotate aim pivot to face target
            if (aimPivot != null && AimDirection != Vector3.zero)
            {
                aimPivot.rotation = Quaternion.LookRotation(AimDirection);
            }
        }
        #endregion
        
        #region Weapons
        /// <summary>
        /// Load initial weapons specified in player data
        /// </summary>
        private async System.Threading.Tasks.Task LoadInitialWeapons()
        {
            if (playerData == null || playerData.startingWeaponAddresses == null || playerData.startingWeaponAddresses.Count == 0)
            {
                Debug.LogWarning("No starting weapons specified in player data");
                return;
            }
            
            foreach (string weaponAddress in playerData.startingWeaponAddresses)
            {
                if (string.IsNullOrEmpty(weaponAddress)) continue;
                
                GameObject weaponPrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(weaponAddress);
                if (weaponPrefab != null)
                {
                    GameObject weaponInstance = Instantiate(weaponPrefab, weaponSocket);
                    WeaponController weaponController = weaponInstance.GetComponent<WeaponController>();
                    
                    if (weaponController != null)
                    {
                        weaponController.Initialize(this);
                        _weapons.Add(weaponController);
                        weaponInstance.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError($"Weapon at address {weaponAddress} does not have a WeaponController component", this);
                        Destroy(weaponInstance);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load weapon at address {weaponAddress}", this);
                }
            }
        }
        
        /// <summary>
        /// Switch to a specific weapon
        /// </summary>
        /// <param name="index">Index of the weapon to switch to</param>
        private void SwitchToWeapon(int index)
        {
            if (_weapons.Count == 0 || index < 0 || index >= _weapons.Count) return;
            
            // Deactivate current weapon
            if (_currentWeapon != null)
            {
                _currentWeapon.gameObject.SetActive(false);
            }
            
            // Activate new weapon
            _currentWeaponIndex = index;
            _currentWeapon = _weapons[_currentWeaponIndex];
            _currentWeapon.gameObject.SetActive(true);
            
            // Notify listeners
            OnWeaponChanged?.Invoke(_currentWeapon);
            
            // Update animation if available
            if (_animator != null)
            {
                _animator.SetTrigger("WeaponSwitch");
            }
        }
        
        /// <summary>
        /// Handle firing start
        /// </summary>
        private void HandleFireStart()
        {
            if (IsDead || _currentWeapon == null) return;
            
            _currentWeapon.TryAttack();
        }
        
        /// <summary>
        /// Handle firing end
        /// </summary>
        private void HandleFireEnd()
        {
            // Nothing to do for now, as weapon auto-fire is handled in Update
        }
        
        /// <summary>
        /// Handle reload input
        /// </summary>
        private void HandleReload()
        {
            if (IsDead || _currentWeapon == null) return;
            
            _currentWeapon.TryReload();
        }
        
        /// <summary>
        /// Handle fire mode switch input
        /// </summary>
        private void HandleSwitchFireMode()
        {
            if (IsDead || _currentWeapon == null) return;
            
            _currentWeapon.SwitchFireMode();
        }
        
        /// <summary>
        /// Handle weapon switch input
        /// </summary>
        private void HandleSwitchWeapon()
        {
            if (IsDead || _weapons.Count <= 1) return;
            
            // Switch to next weapon
            int nextIndex = (_currentWeaponIndex + 1) % _weapons.Count;
            SwitchToWeapon(nextIndex);
        }
        #endregion
        
        #region Interaction
        private IInteractable _currentInteractable;
        
        /// <summary>
        /// Update interaction prompt for nearby interactable objects
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            RaycastHit hit;
            float interactionRange = playerData != null ? playerData.interactionRange : 2f;
            
            if (Physics.Raycast(transform.position, AimDirection, out hit, interactionRange, interactionLayerMask))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    _currentInteractable = interactable;
                    // Update UI prompt here or delegate to UI manager
                    return;
                }
            }
            
            // No interactable in range
            _currentInteractable = null;
        }
        
        /// <summary>
        /// Handle interaction input
        /// </summary>
        private void HandleInteract()
        {
            if (IsDead || _currentInteractable == null) return;
            
            _currentInteractable.Interact(transform);
            OnInteracted?.Invoke(_currentInteractable);
        }
        #endregion
        
        #region Health and Damage
        /// <summary>
        /// Apply damage to the player
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="damageSource">Source of the damage</param>
        /// <returns>True if damage was applied successfully</returns>
        public bool TakeDamage(float damage, Transform damageSource)
        {
            if (IsDead || _playerStats == null) return false;
            
            bool damageApplied = _playerStats.TakeDamage(damage);
            
            if (damageApplied)
            {
                // Play hurt sound
                if (!string.IsNullOrEmpty(hurtSoundAddress))
                {
                    // Play sound using audio system
                }
                
                // Update animation
                if (_animator != null)
                {
                    _animator.SetTrigger("TakeHit");
                }
                
                // Check for death
                if (_playerStats.CurrentHealth <= 0)
                {
                    Die();
                }
            }
            
            return damageApplied;
        }
        
        /// <summary>
        /// Handle death of the player
        /// </summary>
        private void Die()
        {
            if (IsDead) return;
            
            IsDead = true;
            
            // Stop all movement
            _rigidbody.velocity = Vector3.zero;
            
            // Play death animation
            if (_animator != null)
            {
                _animator.SetTrigger("Die");
                _animator.SetBool("IsDead", true);
            }
            
            // Play death sound
            if (!string.IsNullOrEmpty(deathSoundAddress))
            {
                // Play sound using audio system
            }
            
            // Disable player collider and rigidbody
            Collider playerCollider = GetComponent<Collider>();
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }
            
            _rigidbody.isKinematic = true;
            
            // Notify listeners
            OnPlayerDied?.Invoke();
            
            // Notify game manager
            GameManager.Instance.GameOver();
        }
        
        /// <summary>
        /// Handle health changed event from player stats
        /// </summary>
        /// <param name="currentHealth">Current health value</param>
        /// <param name="maxHealth">Maximum health value</param>
        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// Handle stamina changed event from player stats
        /// </summary>
        /// <param name="currentStamina">Current stamina value</param>
        /// <param name="maxStamina">Maximum stamina value</param>
        private void HandleStaminaChanged(float currentStamina, float maxStamina)
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            
            // If stamina is depleted, stop sprinting
            if (currentStamina <= 0 && IsSprinting)
            {
                StopSprinting();
            }
        }
        #endregion
    }
}