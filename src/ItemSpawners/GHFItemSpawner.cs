using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;

namespace EasterIsland.src.ItemSpawners
{
    internal class GHFItemSpawner : MonoBehaviour
    {
        bool awaitSpawn = true;

        public void OnEnable()
        {
            //Debug.Log("GHFItem: OnEnable Call");
            if (RoundManager.Instance.IsServer)
            {
                SpawnItem();
            }
        }
        private async void SpawnItem()
        {
            if (RoundManager.Instance.IsServer)
            {
                while (RoundManager.Instance.dungeonIsGenerating == true)
                {
                    //Debug.Log($"Moai Enemy: Awaiting to spawn portal  - -3...");
                    await Task.Delay(1000);
                }
                while (RoundManager.Instance.dungeonCompletedGenerating == false)
                {
                    //Debug.Log($"Moai Enemy: Awaiting to spawn portal - -2...");
                    await Task.Delay(1000);
                }
                while (!StartOfRound.Instance.shipHasLanded)  // assuming 15 Scrap objects always spawn
                {
                    //Debug.Log($"Moai Enemy: Awaiting to spawn portal - -1...");
                    await Task.Delay(1000);
                }

                while (awaitSpawn)
                {
                    awaitSpawn = false;
                    GameObject gameObject = UnityEngine.Object.Instantiate(Plugin.GHFPrefab, this.transform.position + Vector3.up * 0.5f, Quaternion.Euler(Vector3.zero), RoundManager.Instance.spawnedScrapContainer);
                    gameObject.SetActive(value: true);
                    gameObject.GetComponent<NetworkObject>().Spawn();
                    gameObject.GetComponent<NoisemakerProp>().targetFloorPosition = this.transform.position + Vector3.up * 0.5f;
                    Destroy(this.gameObject);
                }
            }
        }
    }
}
