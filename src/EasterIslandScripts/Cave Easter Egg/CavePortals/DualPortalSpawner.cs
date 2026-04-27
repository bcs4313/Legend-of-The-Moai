using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    public class DualPortalSpawner : MonoBehaviour
    {
        public void Start()
        {
            this.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            this.transform.localScale = new Vector3(1f, 1f, 1f);
            if (RoundManager.Instance.IsHost)
            {
                // Instantiate the registered prefab and spawn it as a network object
                var dual = Instantiate(Plugin.DualPortal, this.transform);

                // Ensure it has a NetworkObject and spawn it
                var netObj = dual.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }
                else
                {
                    Debug.LogError("Failed to spawn DualPortal: NetworkObject component is missing!");
                }
            }
        }
    }
}
