using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.ComponentModel;

namespace PoolRooms
{
    public class PoolLightBehaviour : MonoBehaviour
    {
        public Light LightToUpdate = null;

        public Color RedAlertColor = Color.red;

        public void OnApparatusPulled()
        {
            LightToUpdate.color = RedAlertColor;
        }
    }
}
