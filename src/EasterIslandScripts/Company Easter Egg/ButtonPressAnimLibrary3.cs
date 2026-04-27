using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.VolumeComponent;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg
{
    // the lever is risky!
    // it doubles the value of items, but each flick summons more
    // moai. Like a minigame!
    public class ButtonPressAnimLibrary3 : NetworkBehaviour
    {
        public Animator buttonAnimator;
        public Animator satelliteAnimator;

        bool inEvent = false;

        public AudioSource flickSound;
        public AudioSource scanningSound;
        public AudioSource blastSound;

        public ShipCrashScript shipCrash;
        public ParticleSystem satelliteFire;

        public Text consoleText;

        public void Start()
        {
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
            if (inEvent) { return; }
            inEvent = true;

            // you are allowed to put the same item in twice, but the increased value multiplicatively gets lower 
            buttonAnimator.Play("Pressing_Anim");
            flickSound.Play();
            await Task.Delay(2000);
            scanningSound.Play();
            satelliteAnimator.Play("Scanning");
            consoleText.text = "Scanning For Satellite Location...";

            await Task.Delay(6500);
            consoleText.text = "Satellite found. Sending LAND Signal at locale (LANDBRIDGE_RIGHT)";
            satelliteFire.Play();

            await Task.Delay(4000);
            blastSound.Play();

            await Task.Delay(1000);
            if (RoundManager.Instance.IsHost)
            {
                shipCrash.awaitCrash();
            }
        }
    }
}
