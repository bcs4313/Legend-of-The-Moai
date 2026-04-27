using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    // responsible for light adjustments and music
    // inside of the collider
    public class CaveEnvironment : MonoBehaviour
    {
        public Light maindirectionalLight;
        public Light sunLight;
        public Light moonLight;
        public Light eclipsedLight;

        private Collider triggerZone;
        private bool playerWasInside = false;
        private PlayerControllerB localPlayer;

        private void Start()
        {
            triggerZone = GetComponent<Collider>();
            if (triggerZone == null || !triggerZone.isTrigger)
            {
                Debug.LogError("CaveEnvironment: This script requires a trigger collider on the same GameObject.");
                enabled = false;
                return;
            }

            localPlayer = StartOfRound.Instance?.localPlayerController;
            if (localPlayer == null)
            {
                Debug.LogWarning("CaveEnvironment: Local player not found at Start.");
            }

            StartCoroutine(CheckPlayerBounds());
        }

        private IEnumerator CheckPlayerBounds()
        {
            while (true)
            {
                if (localPlayer != null)
                {
                    bool isInside = triggerZone.bounds.Contains(localPlayer.transform.position);

                    if (isInside && !playerWasInside)
                    {
                        playerWasInside = true;
                        HandleEnter();
                    }
                    else if (!isInside && playerWasInside)
                    {
                        playerWasInside = false;
                        HandleExit();
                    }
                }
                else
                {
                    // Try to reacquire local player
                    localPlayer = StartOfRound.Instance?.localPlayerController;
                }

                yield return new WaitForSeconds(0.1f); // adjust frequency if needed
            }
        }

        private void HandleEnter()
        {
            maindirectionalLight.enabled = false;
            sunLight.enabled = false;
            moonLight.enabled = false;
            if (eclipsedLight) eclipsedLight.enabled = false;
            Debug.Log("Player entered the cave. Lights disabled.");
        }

        private void HandleExit()
        {
            maindirectionalLight.enabled = true;
            sunLight.enabled = true;
            moonLight.enabled = true;
            if (eclipsedLight) eclipsedLight.enabled = true;
            Debug.Log("Player exited the cave. Lights enabled.");
        }
    }
}
