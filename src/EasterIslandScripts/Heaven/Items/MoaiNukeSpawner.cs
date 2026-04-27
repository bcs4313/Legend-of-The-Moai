using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Items
{
    public class MoaiNukeSpawner : MonoBehaviour
    {
        public void Awake()
        {
            if(RoundManager.Instance.IsHost)
            {
                spawnNuke();
            }

        }

        public void spawnNuke()
        {
            // find nuke in item list
            GameObject nuke = findNukeObj();

            try
            {
                if (nuke)
                {
                    // spawn it
                    GameObject gameObject = UnityEngine.Object.Instantiate(nuke, this.transform.position, this.transform.rotation);
                    gameObject.SetActive(value: true);

                    var rootObj = gameObject.GetComponent<NetworkObject>();
                    rootObj.GetComponent<NetworkObject>().Spawn(true);
                    var obj = gameObject.GetComponent<NuclearBomb>();
                    obj.transform.position = this.transform.position;
                    obj.targetFloorPosition = this.transform.position;
                    obj.scrapValue = 0;
                }
                else
                {
                    Debug.LogWarning("LegendOfTheMoai: Couldn't find nuke item to spawn! Item will be missing in the heaven scene!");
                }
            }
            catch (Exception e) { Debug.LogError(e); }

        }

        public GameObject findNukeObj()
        {
            var items = StartOfRound.Instance.allItemsList.itemsList;
            foreach (var item in items)
            {
                if(item && item.itemId == 654021)
                {
                    if(item.spawnPrefab && item.spawnPrefab.GetComponent<NuclearBomb>() != null)
                    {
                        return item.spawnPrefab;
                    }
                }
            }

            return null;
        }
    }
}
