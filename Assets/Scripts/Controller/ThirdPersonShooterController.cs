using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonShooterController : MonoBehaviour
{

    #region --------------- Variable Declare ---------------

    [SerializeField]
    private CinemachineVirtualCamera aimVirtualCamera;

    private StarterAssetsInputs starterAssetsInputs;

    #endregion

    #region --------------- Main Activity ---------------

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    // Update is called once per frame
    void Update()
    {
        aimVirtualCamera.gameObject.SetActive(starterAssetsInputs.aim);
    }

#endregion

#region --------------- Public Process ---------------


#endregion

#region --------------- Private Process ---------------


#endregion
}
