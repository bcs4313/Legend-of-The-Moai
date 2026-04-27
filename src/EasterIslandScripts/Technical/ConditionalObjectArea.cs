using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    // Collider that toggles its host gameobjects on if a 
    // player is within the area
    public class ConditionalObjectArea : MonoBehaviour
    {
        // set in unity
        public GameObject[] hostObjects;
        protected List<PlayerControllerB> playersInside;

        private void Start()
        {
            playersInside = new List<PlayerControllerB>();
            for (int i = 0; i < hostObjects.Length; i++)
            {
                hostObjects[i].SetActive(false);
            }
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
                        hostObjects[i].SetActive(false);
                    }
                }
            }
        }

        public PlayerControllerB getPlayer(Collider other)
        {
            GameObject plyGO = other.gameObject;
            if(plyGO == null) { return null; }

            PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                return ply;
            }

            return null;
        }
    }
}
