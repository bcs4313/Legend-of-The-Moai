using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;

namespace EasterIsland.src.EasterIslandScripts.Environmental
{
    // teleporter is one way
    internal class CaveItemInit : NetworkBehaviour
    {
        public NetworkObject netObj;

        public void Start()
        {
            netObj.Spawn(true);
        }
    }
}
