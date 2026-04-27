using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    // Collider that toggles its host gameobjects off a 
    // player is within the area
    public class InverseConditionalArea : MonoBehaviour
    {
        // set in unity
        public GameObject[] hostObjects;
        public Light[] linkedLights;
        protected List<PlayerControllerB> playersInside;

        private void Start()
        {
            playersInside = new List<PlayerControllerB>();
        }

        private void OnTriggerEnter(Collider other)
        {
            var ply = getPlayer(other);
            if (ply != null) // Ensure the player has the "Player" tag
            {
                for(int i = 0; i < hostObjects.Length; i++)
                {
                    hostObjects[i].SetActive(true);

                    if (!playersInside.Contains(ply))
                    {
                        playersInside.Add(ply);
                    }
                }

                if (playersInside.Count > 0)
                {
                    for (int i = 0; i < hostObjects.Length; i++)
                    {
                        hostObjects[i].SetActive(false);
                    }


                    for (int i = 0; i < linkedLights.Length; i++)
                    {
                        linkedLights[i].enabled = false;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var ply = getPlayer(other);
            if (ply != null) // Ensure the player has the "Player" tag
            {
                if (playersInside.Contains(ply))
                {
                    playersInside.Remove(ply);
                }

                if (playersInside.Count == 0)
                {
                    for (int i = 0; i < hostObjects.Length; i++)
                    {
                        hostObjects[i].SetActive(true);
                    }

                    for (int i = 0; i < linkedLights.Length; i++)
                    {
                        linkedLights[i].enabled = true;
                    }
                }
            }
        }

        public PlayerControllerB getPlayer(Collider other)
        {
            GameObject plyGO = other.gameObject;
            if(plyGO == null) { return null; }

            PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null && ply.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
            {
                return ply;
            }

            return null;
        }
    }
}
