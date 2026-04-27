using EasterIsland.src.EasterIslandScripts.Heaven.Surgery;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Device;
using static UnityEngine.Rendering.VolumeComponent;

namespace EasterIsland.src.EasterIslandScripts.Library_Easter_egg
{
    // the lever is risky!
    // it doubles the value of items, but each flick summons more
    // moai. Like a minigame!
    public class ButtonPressScreenUpdater : NetworkBehaviour
    {
        public Animator buttonAnimator;
        public SelectScreenManager screen;

        public AudioSource flickSound;
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
            buttonAnimator.Play("Pressing_Anim");
            flickSound.Play();

            screen.swapClientRpc();
        }
    }
}
