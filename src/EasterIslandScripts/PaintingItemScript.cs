using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace EasterIsland.src.EasterIslandScripts
{
    public class PaintingItemScript : NetworkBehaviour
    {
        // item parent, might be held by player
        public GrabbableObject hostItem;

        void Start()
        {
            Plugin.Logger.LogInfo("Setting Painting Floor Position to::: ");
            Plugin.Logger.LogInfo(hostItem.gameObject.transform.localPosition);
            hostItem.targetFloorPosition = hostItem.gameObject.transform.localPosition;
            hostItem.SetScrapValue(0);
        }
    }
}
