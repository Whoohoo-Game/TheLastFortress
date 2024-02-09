using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TFFT.Ultility {
    public static class Ultility
    {
        public static LayerMask GetAimLayerMask() {

            return LayerMask.GetMask("Default", "WorldBox");
        }
    }
}
