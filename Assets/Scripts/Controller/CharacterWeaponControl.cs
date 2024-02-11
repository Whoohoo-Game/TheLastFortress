using System.Collections;
using System.Collections.Generic;
using TFFT.Weapon;
using UnityEngine;

public class CharacterWeaponControl : MonoBehaviour
{

    #region --------------- Variable Declare ---------------

    [SerializeField]
    private Weapon weapon;

#endregion

#region --------------- Main Activity ---------------

    // Start is called before the first frame update
    void Start()
    {
        TestInitWeapon();
    }

    #endregion

    #region --------------- Public Process ---------------

    public void Attack(bool isAttack) {

        if(isAttack == false)
        {
            return;
        }

        weapon.Fire();
    }

    #endregion

    #region --------------- Private Process ---------------

    private void TestInitWeapon() {
        weapon.Init(new WeaponData() { 
            BaseDamage = 100,
            BaseMagazineAmmo = 1000,
            FirePower = 150,
            FireRate = 10f,
            Duration = 1000

        });
    }

#endregion
}
