using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Items
{
    public class InsideBaseSpawner : MonoBehaviour
    {
        public void Awake()
        {
            if(RoundManager.Instance.IsHost)
            {
                spawnBase();
            }

        }

        public void spawnBase()
        {
            // find nuke in item list
            GameObject fort = Plugin.HeavenBase;

            try
            {
                if (fort)
                {
                    // spawn it
                    GameObject gameObject = UnityEngine.Object.Instantiate(fort, this.transform.position, this.transform.rotation);
                    gameObject.SetActive(value: true);

                    var rootObj = gameObject.GetComponent<NetworkObject>();
                    rootObj.GetComponent<NetworkObject>().Spawn(true);
                }
                else
                {
                    Debug.LogError("LegendOfTheMoai: Couldn't find heaven to spawn! Entire Area will be missing in the heaven scene!");
                }
            }
            catch (Exception e) { Debug.LogError(e); }

        }
    }
}
