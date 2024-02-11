using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TFFT.Weapon
{
    using TFFT.Ultility;

    public abstract class Weapon : MonoBehaviour
    {

        #region --------------- Variable Declare ---------------

        protected WeaponData weaponData;

        [SerializeField]
        private Transform firePos;
        [SerializeField]
        private Bullet bulletPreb;

        //Weapon current stat
        protected int currentMagazineAmmo;
        protected int currentDuration;
        protected float currentFireRate;

        private float tempFireRate;

        #endregion

        #region --------------- Main Activity ---------------

        // Start is called before the first frame update
        void Start()
        {

        }

        #endregion

        #region --------------- Public Process ---------------

        public void Init(WeaponData weaponData) {
            this.weaponData = weaponData;

            currentMagazineAmmo = weaponData.BaseMagazineAmmo;
            currentDuration = weaponData.Duration;
            currentFireRate = weaponData.FireRate;
        }

        /// <summary>
        /// Weapon fire
        /// </summary>
        public void Fire()
        {
            if(CanFire() == false) {
                return;
            }

            if (FireProgress())
            {
                UpdateDuration();
                return;
            }
        }

        /// <summary>
        /// Reload ammo for weapon
        /// </summary>
        /// <param name="amount"></param>
        public virtual void Reload(int amount) { }

        /// <summary>
        /// Update weapon duration if fire success
        /// </summary>
        public virtual void UpdateDuration()
        {
            currentDuration--;
        }

        #endregion

        #region --------------- Private Process ---------------

        /// <summary>
        /// Weapon fires action
        /// </summary>
        /// <returns></returns>
        protected virtual bool FireProgress() { 
            

            var ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            if(Physics.Raycast(ray, out var hit, 999, Ultility.GetAimLayerMask()) == false)
            {
                return false;
            }

            var bullet = Instantiate(bulletPreb, firePos.position, transform.rotation);
            bullet.SetBulletPower(hit.point, weaponData.FirePower);

            return true;
        }

        private bool CanFire() {

            return
                CanFireByFireRate() &&
                CanFireByAmmo() &&
                CanFireByDuration();
        }

        protected virtual bool CanFireByFireRate() {
            if (tempFireRate > Time.time)
            {
                return false;
            }

            tempFireRate = Time.time + (1 / currentFireRate);

            return true;
        }

        protected virtual bool CanFireByAmmo() {
            if (currentMagazineAmmo <= 0)
            {
                return false;
            }

            return true;
        }

        protected virtual bool CanFireByDuration() { 
            if(currentDuration <= 0)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
