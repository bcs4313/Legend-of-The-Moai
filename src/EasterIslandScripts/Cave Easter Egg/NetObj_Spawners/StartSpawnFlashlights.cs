using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    class StartSpawnFlashLight : MonoBehaviour
    {
        public Transform location1;
        public Transform location2;

        private GameObject instFlash1;
        private GameObject instFlash2;

        public GameObject findPrefab()
        {
            List<Item> items = StartOfRound.Instance.allItemsList.itemsList;
            foreach (Item it in items)
            {
                if (it.itemName.ToLower().Contains("pro") && it.itemName.ToLower().Contains("light") && it.itemName.ToLower().Contains("flash"))
                {
                    return it.spawnPrefab;
                }
            }
            return null;
        }

        public void Start()
        { 
            if(RoundManager.Instance.IsHost)
            {
                // Instantiate the registered prefab and spawn it as a network object
                var prefab = findPrefab();
                //var oldScale = prefab.transform.localScale;
                //prefab.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                var flash1 = Instantiate(prefab, location1.position, location2.rotation);
                var flash2 = Instantiate(prefab, location2.position, location2.rotation);
                //flash1.transform.localPosition = Vector3.zero;
                //flash2.transform.localPosition = Vector3.zero;
                //prefab.transform.localScale = oldScale;

                // Ensure it has a NetworkObject and spawn it
                var netObj1 = flash1.GetComponent<NetworkObject>();
                var netObj2 = flash2.GetComponent<NetworkObject>();

                if (netObj1 != null)
                {
                    netObj1.Spawn();
                    Debug.Log($"CAVE: Spawned flash1");
                }

                if (netObj2 != null)
                {
                    netObj2.Spawn();
                    Debug.Log($"CAVE: Spawned flash2");
                }

                instFlash1 = flash1;
                instFlash2 = flash2;
            }
            else
            {
                // DONT DO IT CLIENT
                Debug.Log("Client detected, skipping CannonConstructor instantiation.");
            }
        }
    }
}
