using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal.Internal;

public class ThirdPersonShooterController : MonoBehaviour
{

    #region --------------- Variable Declare ---------------

    [SerializeField]
    private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField]
    private ThirdPersonController thirdPersonController;
    [SerializeField]
    private Transform targetAimObj;
    [SerializeField]
    private Rig rig;
    [SerializeField]
    private LayerMask[] hitLayers;

    [SerializeField]
    private float normalSensitivity;
    [SerializeField]
    private float aimSensisitivity;

    private StarterAssetsInputs starterAssetsInputs;
    private LayerMask allHitLayer;
    private Vector3 targetPos;
    private float curAimLayerWeight;
    private float currentAimTime;
    private float aimWeight;

    #endregion

    #region --------------- Main Activity ---------------

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        CreateTotalHitLayer();

        aimWeight = 0;
    }

    // Update is called once per frame
    void Update()
    {
        aimVirtualCamera.gameObject.SetActive(starterAssetsInputs.aim);
        thirdPersonController.SetSensitivity(starterAssetsInputs.aim ? aimSensisitivity : normalSensitivity);
        thirdPersonController.SetRotateOnMove(starterAssetsInputs.aim == false);
        CheckAimAnimation();

        AimRotation();
        RotateCharacterDirectToAim();
    }

    #endregion

    #region --------------- Public Process ---------------


    #endregion

    #region --------------- Private Process ---------------

    private void CheckAimAnimation() {

        aimWeight = starterAssetsInputs.aim ? 1 : 0;

        rig.weight = Mathf.Lerp(rig.weight, aimWeight, 20 * Time.deltaTime);

        if(curAimLayerWeight == aimWeight)
        {
            return;
        }

        if (currentAimTime < 0.2f)
        {
            currentAimTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentAimTime / 0.2f);
            float newWeight = Mathf.Lerp(curAimLayerWeight, aimWeight, t);

            thirdPersonController.SetAim(newWeight);
        }
        else
        {
            // Reset time when the transition is complete
            currentAimTime = 0f;
            curAimLayerWeight = aimWeight;
        }
    }

    private void AimRotation() {

        var screenCenterPoint = new Vector2(Screen.width / 2, Screen.height / 2);
        var ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out var hit, 999, allHitLayer)) {
            targetPos = hit.point;
            targetAimObj.position = hit.point;
        }
    }

    private void RotateCharacterDirectToAim() {

        if (starterAssetsInputs.aim == false) {
            return;
        }

        var tempTargetPos = targetPos;
        tempTargetPos.y = transform.position.y;

        var direct = (tempTargetPos - transform.position).normalized;

        transform.forward = Vector3.Lerp(transform.forward, direct, Time.deltaTime * 20f);
    }

    private void CreateTotalHitLayer() { 
        foreach(var layer in  hitLayers)
        {
            allHitLayer |= layer;
        }   
    }

#endregion
}
