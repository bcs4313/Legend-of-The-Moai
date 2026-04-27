using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    // responsible for light adjustments and music
    // inside of the collider
    public class LavaTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (isPlayer(other)) // Ensure the player has the "Player" tag
            {
                GameObject plyGO = other.gameObject;
                PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
                ply.KillPlayer(new Vector3(0, 20f, 0), true, CauseOfDeath.Burning);
                Debug.Log("Player entered the cave. Light disabled.");
            }
        }

        public bool isPlayer(Collider other)
        {
            GameObject plyGO = other.gameObject;
            if(plyGO == null) { return false; }

            PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                return true;
            }

            return false;
        }
    }
}
