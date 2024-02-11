using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TFFT.Weapon
{
    public class Bullet : MonoBehaviour
    {

        #region --------------- Variable Declare ---------------

        private bool isInit;
        private Vector3 targetPos;
        private float moveSpeed;

        #endregion

        #region --------------- Main Activity ---------------

        private void Update()
        {
            if (isInit == false)
            {
                return;
            }

            var distanceBefore = (targetPos - transform.position).sqrMagnitude;

            var moveDir = (targetPos - transform.position).normalized;
            transform.position += moveSpeed * Time.deltaTime * moveDir;

            var distanceAfter = (targetPos - transform.position).sqrMagnitude;

            if (distanceBefore < distanceAfter)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region --------------- Public Process ---------------

        public virtual void SetBulletPower(Vector3 targetPos, float moveSpeed)
        {
            this.targetPos = targetPos;
            this.moveSpeed = moveSpeed;

            transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized);

            isInit = true;
        }

        #endregion

        #region --------------- Private Process ---------------


        #endregion
    }
}
