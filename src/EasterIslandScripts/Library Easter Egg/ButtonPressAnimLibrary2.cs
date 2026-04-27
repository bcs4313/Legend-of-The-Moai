using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;

namespace EasterIsland.src.EasterIslandScripts.Library_Easter_egg
{
    // the lever is risky!
    // it doubles the value of items, but each flick summons more
    // moai. Like a minigame!
    public class ButtonPressAnimLibrary2 : NetworkBehaviour
    {
        public LibraryPopulator moaiSpawner;
        public Animator buttonAnimator;
        protected Spewer volcano;

        bool inEvent = false;

        public AudioSource flickSound;
        public AudioSource preparingSound;
        public AudioSource runningSound;
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
            volcano = UnityEngine.Object.FindAnyObjectByType<Spewer>();
            if (inEvent || !volcano) { return; }
            inEvent = true;

            // you are allowed to put the same item in twice, but the increased value multiplicatively gets lower 
            buttonAnimator.Play("Pressing_Anim");
            flickSound.Play();
            HUDManager.Instance.DisplayTip("Please Wait...", "");
            await Task.Delay(6000);
            preparingSound.Play();

            await Task.Delay(1000);
            
            HUDManager.Instance.DisplayTip("Hello Bipedal Ones", "CONGRATULATIONS! You’ve just pressed the BIG RED VOLCANO BUTTON. Anyhow, enjoy the heat!", true);
            runningSound.Play();

            volcano.hoursForce = 2;
        }
    }
}
