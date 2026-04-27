using EasterIsland.src.EasterIslandScripts.Heaven.BodyMods;
using EasterIsland.src.EasterIslandScripts.Heaven.Surgery;
using GameNetcodeStuff;
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
    public class ButtonPressSurgeryClear : NetworkBehaviour
    {
        public Animator buttonAnimator;

        public AudioSource flickSound;

        private float elapsedTime = 0f; // Tracks the elapsed time of the animation.
        private bool isAnimating = false; // Tracks whether the animation is in progress.

        public AudioSource initialPrep;
        public AudioSource surgeryNoise;
        public AudioSource surgeryCompletedNoise;

        public SelectScreenManager screen;

        public Animator shrineAnimator;

        public Collider surgeryHitBox;
        public List<PlayerControllerB> targetPlayers = new List<PlayerControllerB>();

        public InteractTrigger opposingTrigger;


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
            opposingTrigger.currentCooldownValue = 30f;
            opposingTrigger.cooldownTime = 30f;

            if(RoundManager.Instance.IsHost)
            {
                pingCooldownClientRpc();
            }
            else
            {
                pingCoolDownServerRpc();
            }

            beginRemoval();
        }

        [ServerRpc]
        public void pingCoolDownServerRpc()
        {
            pingCooldownClientRpc();
        }

        [ClientRpc]
        public void pingCooldownClientRpc()
        {
            opposingTrigger.currentCooldownValue = 30f;
            opposingTrigger.cooldownTime = 30f;
        }

        public async void beginRemoval()
        {
            flickSound.Play();
            initialPrep.Play();
            await Task.Delay(2000);
            shrineAnimator.Play("Transform");
            surgeryNoise.Play();
            if (screen.selectedID == 0)
            {
                HUDManager.Instance.DisplayTip("Modification: REMOVAL", "Pending procedure. Please have a seat. Starting Modification Sequence in 10 seconds.");
            }
            else
            {
                HUDManager.Instance.DisplayTip("Modification: REMOVAL", "Pending procedure. Please have a seat. Starting Modification Sequence in 10 seconds.");
            }
            await Task.Delay(5000);
            HUDManager.Instance.DisplayTip("5 seconds remaining", "Please dispose of the wasted prototype post surgery");

            await Task.Delay(14000);

            removeParts();

            await Task.Delay(8000);
            surgeryCompletedNoise.Play();
            shrineAnimator.Play("Idle");
        }

        public void removeParts()
        {
            var heads = UnityEngine.Object.FindObjectsOfType<DogHeadMod>();
            var wings = UnityEngine.Object.FindObjectsOfType<BaboonWingMod>();

            foreach (PlayerControllerB ply in targetPlayers)
            {
                if (ply && !ply.GetComponentInChildren<DogHeadMod>())
                {
                    if (RoundManager.Instance.IsHost)
                    {
                        foreach(DogHeadMod mod in heads)
                        {
                            if (mod.gameObject && Vector3.Distance(ply.transform.position, mod.transform.position) < 1.5f)
                            {
                                GameObject.Destroy(mod.gameObject);
                            }
                        }

                        foreach (BaboonWingMod mod in wings)
                        {
                            if (mod.gameObject && Vector3.Distance(ply.transform.position, mod.transform.position) < 1.5f)
                            {
                                GameObject.Destroy(mod.gameObject);
                            }
                        }
                    }
                }
            }
        }
    }
}
