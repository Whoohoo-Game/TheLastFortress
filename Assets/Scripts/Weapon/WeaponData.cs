using System;

namespace TFFT.Weapon {

    [Serializable]
    public struct WeaponData
    {
        public WeaponID ID;
        public int BaseDamage;
        public int BaseMagazineAmmo;
        public int Duration;
        public float FirePower;
        public float FireRate;
    }

    public enum WeaponID { 
        Rifle
    }
}

