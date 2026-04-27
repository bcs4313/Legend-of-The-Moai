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
    public class ButtonPressAnimLibrary : NetworkBehaviour
    {
        public LibraryPopulator moaiSpawner;
        public Animator buttonAnimator;

        public GrabbableObject insertedGrabbableObject;  // object to double value

        public float[] boostValues = [2.0f, 1.75f, 1.5f];
        int activations = 0;

        private float elapsedTime = 0f; // Tracks the elapsed time of the animation.
        private bool isAnimating = false; // Tracks whether the animation is in progress.
        bool inEvent = false;

        public AudioSource flickSound;
        public AudioSource preparingSound;
        public AudioSource runningSound;
        public AudioSource completeSound;

        public GameObject boostingParticles;
        public GameObject plasmaExplosionParticles;

        public AnyItemPedestal ped;

        public void Start()
        {
            boostingParticles.SetActive(false);
            plasmaExplosionParticles.SetActive(false);
        }

        public void CreateAndPlayAnimation()
        {
            if(RoundManager.Instance.IsHost)
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
            elapsedTime = 0f;
            isAnimating = true;
            startEvent();
        }

        public async void startEvent()
        {
            if(inEvent) { return; }
            inEvent = true;
            int boostVal = Mathf.Min(2, activations);

            // you are allowed to put the same item in twice, but the increased value multiplicatively gets lower 
            buttonAnimator.Play("Pressing_Anim");
            flickSound.Play();
            HUDManager.Instance.DisplayTip("Please Wait.", "Preparing to increase item value in gold canister... Boost: " + boostValues[boostVal] + "x time estimation: " + (30 + activations * 15) + "s");
            await Task.Delay(3000);
            preparingSound.Play();

            await Task.Delay(1000);
            if(ped.insertedObj == null)
            {
                HUDManager.Instance.DisplayTip("Error", "Item not detected, please insert an item in the gold canister.");
                plasmaExplosionParticles.SetActive(false);
                boostingParticles.SetActive(false);
                inEvent = false;
                return;
            }
            int guardianAmount = (3 + UnityEngine.Random.Range(0, activations * 2));
            HUDManager.Instance.DisplayTip("WARNING", "You are not welcome here. Summoning " + guardianAmount + " Guardians.", true);
            plasmaExplosionParticles.SetActive(false);
            boostingParticles.SetActive(false);
            moaiSpawner.PopulateEnvironment(guardianAmount);

            boostingParticles.SetActive(true);

            await Task.Delay(20000 + activations * 10000);
            HUDManager.Instance.DisplayTip("Please Wait.", "Doubling...");
            runningSound.Play();

            await Task.Delay(10000 + activations * 5000);
            HUDManager.Instance.DisplayTip("SUCCESS", "Item Value Increased. Please extract your item.");

            ped.boostValueClientRpc(boostValues[boostVal]);

            completeSound.Play();
            plasmaExplosionParticles.SetActive(true);
            boostingParticles.SetActive(false);
            await Task.Delay(4000);
            plasmaExplosionParticles.SetActive(false);
            inEvent = false;
            activations++;
        }
    }
}
