using UnityEngine;

namespace ZombieSurvival.Data.Weapons
{
    /// <summary>
    /// Data container for gun type weapons
    /// </summary>
    [CreateAssetMenu(fileName = "New Gun", menuName = "Zombie Survival/Weapons/Gun")]
    public class GunData : WeaponData
    {
        [Header("Gun Properties")]
        [Tooltip("Type of gun")]
        public GunType gunType;
        
        [Tooltip("Maximum ammo in a magazine")]
        public int magazineSize = 10;
        
        [Tooltip("Maximum amount of spare ammo that can be carried")]
        public int maxAmmo = 100;
        
        [Tooltip("Time it takes to reload in seconds")]
        public float reloadTime = 2f;
        
        [Tooltip("Sound played when reloading")]
        public string reloadSoundAddress;
        
        [Tooltip("Bullet spread angle in degrees")]
        public float spread = 5f;
        
        [Tooltip("Number of projectiles per shot (for shotguns)")]
        public int projectilesPerShot = 1;
        
        [Tooltip("Whether the gun can switch fire modes")]
        public bool canSwitchFireMode = false;
        
        [Tooltip("Available fire modes for this weapon")]
        public FireMode[] availableFireModes = new FireMode[] { FireMode.Single };
        
        [Tooltip("Address of the muzzle flash effect prefab in Addressables")]
        public string muzzleFlashAddress;
        
        [Tooltip("Address of the bullet impact effect prefab in Addressables")]
        public string bulletImpactAddress;
        
        [Tooltip("Address of the bullet or projectile prefab in Addressables")]
        public string projectilePrefabAddress;
    }
    
    /// <summary>
    /// Available gun types
    /// </summary>
    public enum GunType
    {
        Pistol,
        Shotgun,
        SMG,
        LMG,
        Rifle
    }
    
    /// <summary>
    /// Available fire modes for guns
    /// </summary>
    public enum FireMode
    {
        Single,
        Burst,
        Auto
    }
}