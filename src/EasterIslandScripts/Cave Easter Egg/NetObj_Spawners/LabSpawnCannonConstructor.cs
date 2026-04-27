using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    class LabSpawnCannonConstructor : MonoBehaviour
    {
        public Transform location;

        public void Start()
        {
            if(RoundManager.Instance.IsHost)
            {
                Debug.Log("Host Cannon Constructor Init");
                // Instantiate the registered prefab and spawn it as a network object
                var cannon = Instantiate(Plugin.CannonConstructor, location);

                // Ensure it has a NetworkObject and spawn it
                var netObj = cannon.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                    Debug.Log($"Spawned CannonConstructor at {location.position}");
                }
                else
                {
                    Debug.LogError("Failed to spawn CannonConstructor: NetworkObject component is missing!");
                }
            }
            else
            {
                // DONT DO IT CLIENT
                Debug.Log("Client detected, skipping CannonConstructor instantiation.");
            }
        }
    }
}
