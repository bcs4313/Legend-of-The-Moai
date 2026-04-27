using EasterIsland.src.EasterIslandScripts.Heaven.BodyMods;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Surgery
{
    public class LeverFlickSurgery : NetworkBehaviour
    {
        public float animationDuration = 0.4f; // Duration of the animation
        public float targetXRotation = 90f; // Target rotation on the X-axis

        private Quaternion initialRotation;

        private float elapsedTime = 0f; // Tracks the elapsed time of the animation.
        private bool isAnimating = false; // Tracks whether the animation is in progress.

        public AudioSource flickSound;
        public AudioSource initialPrep;
        public AudioSource surgeryNoise;
        public AudioSource surgeryCompletedNoise;

        public SelectScreenManager screen;

        public Animator shrineAnimator;

        public Collider surgeryHitBox;
        public List<PlayerControllerB> targetPlayers = new List<PlayerControllerB>();

        public InteractTrigger opposingTrigger;

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
            initialRotation = transform.localRotation;
            elapsedTime = 0f;
            isAnimating = true;
            labFlick();
        }

        private Quaternion startRotation;

        private void Start()
        {
            startRotation = transform.localRotation;
            Debug.Log($"Lever initial local rotation: {startRotation.eulerAngles}");
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

        public async void labFlick()
        {
            opposingTrigger.currentCooldownValue = 30f;
            opposingTrigger.cooldownTime = 30f;

            if (RoundManager.Instance.IsHost)
            {
                pingCooldownClientRpc();
            }
            else
            {
                pingCoolDownServerRpc();
            }

            flickSound.Play();
            initialPrep.Play();
            await Task.Delay(2000);
            shrineAnimator.Play("Transform");
            surgeryNoise.Play();
            if (screen.selectedID == 0)
            {
                HUDManager.Instance.DisplayTip("Modification: MOUTHDOG", "Pending procedure. Please have a seat. Starting Modification Sequence in 10 seconds.");
            }
            else
            {
                HUDManager.Instance.DisplayTip("Modification: BABOONHAWK", "Pending procedure. Please have a seat. Starting Modification Sequence in 10 seconds.");
            }
            await Task.Delay(5000);
            HUDManager.Instance.DisplayTip("5 seconds remaining", " Align your body for optimal integration.");

            await Task.Delay(14000);

            if(screen.selectedID == 0)
            {
                foreach(PlayerControllerB ply in targetPlayers)
                {
                    if (ply && !ply.GetComponentInChildren<DogHeadMod>())
                    {
                        if (RoundManager.Instance.IsHost)
                        {
                            DogHeadMod.testAttach(ply);
                        }
                    }
                }
            }
            else
            {
                foreach (PlayerControllerB ply in targetPlayers)
                {
                    if (ply && !ply.GetComponentInChildren<BaboonWingMod>())
                    {
                        if (RoundManager.Instance.IsHost)
                        {
                            BaboonWingMod.testAttach(ply);
                        }
                    }
                }
            }
            await Task.Delay(8000);
            surgeryCompletedNoise.Play();
            shrineAnimator.Play("Idle");
            ReverseAnimationClientRpc();
        }


        private void Update()
        {
            if (!isAnimating) return;

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / animationDuration);

            if (reverseAnimation)
            {
                transform.localRotation = Quaternion.Lerp(initialRotation, startRotation, progress);
            }
            else
            {
                Quaternion targetRotation = Quaternion.Euler(targetXRotation, startRotation.eulerAngles.y, startRotation.eulerAngles.z);
                transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, progress);
            }

            if (progress >= 1f)
            {
                isAnimating = false;
                reverseAnimation = false;
            }
        }

        private bool reverseAnimation = false;

        [ClientRpc]
        public void ReverseAnimationClientRpc()
        {
            if (isAnimating) return; // avoid interrupting an ongoing animation

            // Set up for reverse animation
            initialRotation = transform.localRotation;
            elapsedTime = 0f;
            reverseAnimation = true;
            isAnimating = true;
        }
    }

}
