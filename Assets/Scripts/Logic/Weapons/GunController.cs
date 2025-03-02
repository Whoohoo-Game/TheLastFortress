// GunController.cs
using System.Collections;
using UnityEngine;
using ZombieSurvival.Core;
using ZombieSurvival.Data.Weapons;
using ZombieSurvival.Logic.Combat;

namespace ZombieSurvival.Logic.Weapons
{
    /// <summary>
    /// Controller for gun type weapons
    /// </summary>
    public class GunController : WeaponController
    {
        [Header("Gun Specific Properties")]
        [SerializeField] private ParticleSystem muzzleFlashEffect;
        [SerializeField] private LineRenderer bulletTrailRenderer;
        [SerializeField] private GameObject impactEffectPrefab;
        
        // Gun specific data
        private GunData GunData => weaponData as GunData;
        
        // Ammunition state
        private int _currentAmmo;
        private int _reserveAmmo;
        
        // Fire mode state
        private int _currentFireModeIndex;
        private FireMode _currentFireMode = FireMode.Single;
        
        // Reload state
        private bool _isReloading;
        private Coroutine _reloadCoroutine;
        
        // Properties
        /// <summary>
        /// Current ammo in the magazine
        /// </summary>
        public override int CurrentAmmo => _currentAmmo;
        
        /// <summary>
        /// Total remaining reserve ammo
        /// </summary>
        public override int TotalAmmo => _reserveAmmo;
        
        /// <summary>
        /// Current fire mode of the gun
        /// </summary>
        public FireMode CurrentFireMode => _currentFireMode;
        
        /// <summary>
        /// Whether the gun is currently reloading
        /// </summary>
        public bool IsReloading => _isReloading;
        
        #region Initialization
        /// <summary>
        /// Initialize the gun with an owner
        /// </summary>
        /// <param name="playerController">Player that owns this gun</param>
        public override void Initialize(Player.PlayerController playerController)
        {
            base.Initialize(playerController);
            
            if (!(weaponData is GunData))
            {
                Debug.LogError("WeaponData is not GunData in GunController", this);
                return;
            }
            
            // Initialize ammo
            _currentAmmo = GunData != null ? GunData.magazineSize : 10;
            _reserveAmmo = GunData != null ? GunData.maxAmmo : 100;
            
            // Initialize fire mode
            _currentFireModeIndex = 0;
            if (GunData != null && GunData.availableFireModes != null && GunData.availableFireModes.Length > 0)
            {
                _currentFireMode = GunData.availableFireModes[_currentFireModeIndex];
            }
            else
            {
                _currentFireMode = FireMode.Single;
            }
            
            // Initialize effects
            if (muzzleFlashEffect == null && GunData != null && !string.IsNullOrEmpty(GunData.muzzleFlashAddress))
            {
                // Load muzzle flash from addressables
                LoadMuzzleFlashAsync();
            }
            
            // Notify about initial ammo state
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
        }
        
        /// <summary>
        /// Load muzzle flash effect from addressables
        /// </summary>
        private async void LoadMuzzleFlashAsync()
        {
            if (GunData == null || string.IsNullOrEmpty(GunData.muzzleFlashAddress)) return;
            
            var muzzleFlashPrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(GunData.muzzleFlashAddress);
            if (muzzleFlashPrefab != null)
            {
                var instance = Instantiate(muzzleFlashPrefab, muzzlePoint != null ? muzzlePoint : transform);
                muzzleFlashEffect = instance.GetComponent<ParticleSystem>();
                
                if (muzzleFlashEffect == null)
                {
                    Debug.LogWarning("Loaded muzzle flash does not have a ParticleSystem component", this);
                }
            }
        }
        #endregion
        
        #region Weapon Actions
        /// <summary>
        /// Try to fire the gun
        /// </summary>
        /// <returns>True if firing was successful</returns>
        public override bool TryAttack()
        {
            // Cannot fire if reloading or no ammo
            if (_isReloading || _currentAmmo <= 0 || !CanAttackBasedOnRate())
            {
                return false;
            }
            
            // Perform attack based on fire mode
            switch (_currentFireMode)
            {
                case FireMode.Single:
                    FireSingleShot();
                    break;
                    
                case FireMode.Burst:
                    StartCoroutine(FireBurst(3)); // 3-round burst
                    break;
                    
                case FireMode.Auto:
                    FireSingleShot(); // Auto mode is handled by continuous calls from PlayerController
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// Fire a single shot
        /// </summary>
        private void FireSingleShot()
        {
            // Update timing
            lastAttackTime = Time.time;
            
            // Consume ammo
            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
            
            // Calculate shot direction with spread
            Vector3 shotDirection = CalculateShotDirection();
            
            // Handle projectiles or hitscan
            if (GunData != null && !string.IsNullOrEmpty(GunData.projectilePrefabAddress))
            {
                // Spawn projectile
                FireProjectile(shotDirection);
            }
            else
            {
                // Hitscan (immediate hit)
                FireHitscan(shotDirection);
            }
            
            // Visual and audio effects
            PlayAttackEffects();
            
            // Notify listeners
            OnWeaponFired?.Invoke();
            
            // Auto reload if magazine is empty
            if (_currentAmmo <= 0)
            {
                TryReload();
            }
        }
        
        /// <summary>
        /// Fire a burst of shots
        /// </summary>
        /// <param name="burstCount">Number of shots in the burst</param>
        private IEnumerator FireBurst(int burstCount)
        {
            int shotsFired = 0;
            
            // Fire multiple shots with a delay between them
            while (shotsFired < burstCount && _currentAmmo > 0)
            {
                FireSingleShot();
                shotsFired++;
                
                if (shotsFired < burstCount && _currentAmmo > 0)
                {
                    // Wait for a fraction of the normal fire rate
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        /// <summary>
        /// Calculate shot direction with spread
        /// </summary>
        /// <returns>Direction vector for the shot</returns>
        private Vector3 CalculateShotDirection()
        {
            if (muzzlePoint == null)
            {
                // Default to forward direction if no muzzle point
                return transform.forward;
            }
            
            // Apply random spread
            float spreadAngle = GunData != null ? GunData.spread : 5f;
            Vector3 spreadDirection = muzzlePoint.forward;
            
            if (spreadAngle > 0)
            {
                spreadDirection = Quaternion.Euler(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    0) * muzzlePoint.forward;
            }
            
            return spreadDirection;
        }
        
        /// <summary>
        /// Fire a hitscan shot (immediate hit check)
        /// </summary>
        /// <param name="direction">Direction to fire in</param>
        private void FireHitscan(Vector3 direction)
        {
            // Define origin point and distance for the raycast
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            float maxDistance = GunData != null ? GunData.range : 50f;
            
            // Single shot (for rifles, pistols, etc.)
            if (GunData == null || GunData.projectilesPerShot == 1)
            {
                // Perform raycast
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
                {
                    // Hit something
                    ProcessHit(hit, GunData != null ? GunData.baseDamage : 10f);
                    
                    // Draw bullet trail if available
                    if (bulletTrailRenderer != null)
                    {
                        StartCoroutine(ShowBulletTrail(origin, hit.point));
                    }
                }
                else
                {
                    // No hit, draw trail to max distance if available
                    if (bulletTrailRenderer != null)
                    {
                        StartCoroutine(ShowBulletTrail(origin, origin + direction * maxDistance));
                    }
                }
            }
            // Multiple projectiles (for shotguns)
            else
            {
                for (int i = 0; i < GunData.projectilesPerShot; i++)
                {
                    // Calculate spread for each pellet
                    Vector3 pelletDirection = Quaternion.Euler(
                        Random.Range(-GunData.spread * 2, GunData.spread * 2),
                        Random.Range(-GunData.spread * 2, GunData.spread * 2),
                        0) * direction;
                    
                    // Perform raycast for each pellet
                    if (Physics.Raycast(origin, pelletDirection, out RaycastHit hit, maxDistance))
                    {
                        // Damage is divided by number of pellets (so total damage is same as base if all hit)
                        float pelletDamage = GunData.baseDamage / GunData.projectilesPerShot;
                        ProcessHit(hit, pelletDamage);
                        
                        // Draw bullet trail if available
                        if (bulletTrailRenderer != null)
                        {
                            StartCoroutine(ShowBulletTrail(origin, hit.point));
                        }
                    }
                    else
                    {
                        // No hit, draw trail to max distance if available
                        if (bulletTrailRenderer != null)
                        {
                            StartCoroutine(ShowBulletTrail(origin, origin + pelletDirection * maxDistance));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fire a projectile (for non-hitscan weapons)
        /// </summary>
        /// <param name="direction">Direction to fire in</param>
        private async void FireProjectile(Vector3 direction)
        {
            if (GunData == null || string.IsNullOrEmpty(GunData.projectilePrefabAddress)) return;
            
            // Define origin point
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            
            try 
            {
                // Load projectile prefab from addressables
                var projectilePrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(GunData.projectilePrefabAddress);
                if (projectilePrefab == null) return;
                
                // Instantiate projectile
                GameObject projectileObj = ObjectPool.Instance.GetFromPool(projectilePrefab, origin, Quaternion.LookRotation(direction));
                if (projectileObj != null)
                {
                    ProjectileController projectile = projectileObj.GetComponent<ProjectileController>();
                    if (projectile != null)
                    {
                        projectile.Initialize(direction, GunData.baseDamage, GunData.range, owner != null ? owner.transform : transform);
                    }
                    else
                    {
                        Debug.LogWarning("Projectile prefab does not have a ProjectileController component", this);
                        ObjectPool.Instance.ReturnToPool(projectileObj);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading projectile: {e.Message}", this);
            }
        }
        
        /// <summary>
        /// Process a hit on an object
        /// </summary>
        /// <param name="hit">The raycast hit data</param>
        /// <param name="damage">Amount of damage to apply</param>
        private void ProcessHit(RaycastHit hit, float damage)
        {
            // Check for damageable object
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Apply damage
                Transform damageSource = owner != null ? owner.transform : transform;
                damageable.TakeDamage(damage, damageSource);
            }
            
            // Spawn impact effect
            SpawnImpactEffect(hit);
        }
        
        /// <summary>
        /// Spawn impact effect at hit point
        /// </summary>
        /// <param name="hit">The raycast hit data</param>
        private async void SpawnImpactEffect(RaycastHit hit)
        {
            if (impactEffectPrefab == null && GunData != null && !string.IsNullOrEmpty(GunData.bulletImpactAddress))
            {
                try
                {
                    // Load impact effect from addressables if not already cached
                    var loadedPrefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(GunData.bulletImpactAddress);
                    impactEffectPrefab = loadedPrefab;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading impact effect: {e.Message}", this);
                    return;
                }
            }
            
            if (impactEffectPrefab != null)
            {
                // Calculate rotation to align with the hit surface
                Quaternion rotation = Quaternion.LookRotation(hit.normal);
                
                // Spawn the impact effect from object pool
                GameObject impact = ObjectPool.Instance.GetFromPool(impactEffectPrefab, hit.point, rotation);
                
                // Automatically return to pool after a delay
                if (impact != null)
                {
                    StartCoroutine(ReturnToPoolAfterDelay(impact, 2f));
                }
            }
        }
        
        /// <summary>
        /// Show bullet trail effect
        /// </summary>
        /// <param name="startPoint">Start point of the trail</param>
        /// <param name="endPoint">End point of the trail</param>
        private IEnumerator ShowBulletTrail(Vector3 startPoint, Vector3 endPoint)
        {
            if (bulletTrailRenderer == null) yield break;
            
            bulletTrailRenderer.enabled = true;
            bulletTrailRenderer.SetPosition(0, startPoint);
            bulletTrailRenderer.SetPosition(1, endPoint);
            
            // Show trail for a short time
            yield return new WaitForSeconds(0.05f);
            
            bulletTrailRenderer.enabled = false;
        }
        
        /// <summary>
        /// Return an object to the pool after a delay
        /// </summary>
        /// <param name="obj">Object to return</param>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (obj != null)
            {
                ObjectPool.Instance.ReturnToPool(obj);
            }
        }
        
        /// <summary>
        /// Play attack effects (muzzle flash, sound)
        /// </summary>
        private void PlayAttackEffects()
        {
            // Play muzzle flash effect
            if (muzzleFlashEffect != null)
            {
                muzzleFlashEffect.Play();
            }
            
            // Play attack sound
            PlayAttackSound();
        }
        
        /// <summary>
        /// Try to reload the weapon
        /// </summary>
        /// <returns>True if reload started successfully</returns>
        public override bool TryReload()
        {
            // Cannot reload if already reloading, magazine is full, or no reserve ammo
            if (GunData == null || _isReloading || _currentAmmo >= GunData.magazineSize || _reserveAmmo <= 0)
            {
                return false;
            }
            
            // Start reload process
            _reloadCoroutine = StartCoroutine(ReloadRoutine());
            return true;
        }
        
        /// <summary>
        /// Reload routine that handles the reload delay
        /// </summary>
        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            
            // Play reload animation/sound
            if (audioSource != null && GunData != null && !string.IsNullOrEmpty(GunData.reloadSoundAddress))
            {
                // Play reload sound
            }
            
            // Wait for reload time
            yield return new WaitForSeconds(GunData != null ? GunData.reloadTime : 2f);
            
            if (GunData != null)
            {
                // Calculate how many rounds to reload
                int bulletsToReload = GunData.magazineSize - _currentAmmo;
                int bulletsAvailable = Mathf.Min(bulletsToReload, _reserveAmmo);
                
                // Update ammo counts
                _currentAmmo += bulletsAvailable;
                _reserveAmmo -= bulletsAvailable;
            }
            else
            {
                // Default behavior if GunData is null
                _currentAmmo = 10;
                _reserveAmmo = Mathf.Max(0, _reserveAmmo - 10);
            }
            
            // Notify about ammo change
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
            
            // Notify about reload completion
            OnWeaponReloaded?.Invoke();
            
            _isReloading = false;
            _reloadCoroutine = null;
        }
        
        /// <summary>
        /// Switch the weapon's firing mode
        /// </summary>
        /// <returns>True if mode was switched</returns>
        public override bool SwitchFireMode()
        {
            // Check if weapon can switch fire modes
            if (GunData == null || !GunData.canSwitchFireMode || GunData.availableFireModes.Length <= 1)
            {
                return false;
            }
            
            // Cycle to next fire mode
            _currentFireModeIndex = (_currentFireModeIndex + 1) % GunData.availableFireModes.Length;
            _currentFireMode = GunData.availableFireModes[_currentFireModeIndex];
            
            return true;
        }
        
        /// <summary>
        /// Add ammo to the reserve
        /// </summary>
        /// <param name="amount">Amount of ammo to add</param>
        /// <returns>True if ammo was added</returns>
        public bool AddAmmo(int amount)
        {
            if (amount <= 0) return false;
            
            int previousReserveAmmo = _reserveAmmo;
            _reserveAmmo = Mathf.Min(_reserveAmmo + amount, GunData != null ? GunData.maxAmmo : 100);
            
            // Notify about ammo change if it changed
            if (_reserveAmmo != previousReserveAmmo)
            {
                OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
                return true;
            }
            
            return false;
        }
        #endregion
        
        private void OnDestroy()
        {
            // Clean up any coroutines
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                _reloadCoroutine = null;
            }
        }
    }
}