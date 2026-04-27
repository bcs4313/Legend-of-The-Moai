using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg;
using EasterIsland.src.EasterIslandScripts.Heaven.Surgery;
using EasterIsland.src.EasterIslandScripts.Technical.Dynamic_Loading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Device;
using static UnityEngine.Rendering.VolumeComponent;

namespace EasterIsland.src.EasterIslandScripts.Heaven
{
    public class ButtonPressTeleportToHeaven : NetworkBehaviour
    {
        public Animator buttonAnimator;

        public AudioSource flickSound;
        public AudioSource teleportSound;
        public GameObject moaiShip;
        public ShipCrashScript shipScript;
        public string moaiShipHeavenDestination;
        public Vector3 moaiShipHome;

        bool atHeaven = false;

        public void Start()
        {
        }

        public void Update()
        {
            // getting the home position
            if (shipScript.getCrashing())
            {
                moaiShipHome = moaiShip.transform.position;
            }
        }

        public void CreateAndPlayAnimation()
        {
            if (RoundManager.Instance.IsHost)
            {
                animClientRpc();
            }
            else
            {
                animServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void animServerRpc()
        {
            animClientRpc();
        }

        [ClientRpc]
        public void animClientRpc()
        {
            startEvent();
        }

        public async void startEvent()
        {
            buttonAnimator.Play("Pressing_Anim");
            HeavenLoader.LoadHeavenWorld();
            flickSound.Play();
            await Task.Delay(2000);
            teleportSound.Play();

            if (atHeaven)
            {
                HUDManager.Instance.DisplayTip("Quantum Drive Charging", "DESTINATION: Home");
            }
            else
            {
                HUDManager.Instance.DisplayTip("Quantum Drive Charging", "DESTINATION: Road_To_Heaven");
            }
            await Task.Delay(4100);
            try
            {
                moaiShip.GetComponent<Animator>().enabled = false;
            }
            catch (Exception e) { Debug.LogError(e); }
            if (atHeaven)
            {
                Debug.Log("ATHEAVEN-> GOTO HOME");
                atHeaven = false;
                moaiShip.transform.position = moaiShipHome;
            }
            else
            {
                Debug.Log("!ATHEAVEN-> GOTO HEAVEN");
                Vector3 loc = GameObject.Find(moaiShipHeavenDestination).transform.position;
                Debug.Log("HEAVENTO->"+loc);
                atHeaven = true;
                moaiShip.transform.position = loc;
            }
            HUDManager.Instance.DisplayTip("Destabilization Complete", "Please Unboard the Ship.");
        }
    }
}
