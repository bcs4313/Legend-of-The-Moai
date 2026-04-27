using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    public class LeverFlickAnim : NetworkBehaviour
    {
        public GameObject goldRef;  // sacrifice 1
        public GameObject artifactRef;  // sacrifice 2
        public float animationDuration = 0.4f; // Duration of the animation
        public float targetXRotation = 90f; // Target rotation on the X-axis

        private Quaternion initialRotation;

        private float elapsedTime = 0f; // Tracks the elapsed time of the animation.
        private bool isAnimating = false; // Tracks whether the animation is in progress.


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
            if (goldRef.activeInHierarchy && artifactRef.activeInHierarchy)
            {
                initialRotation = transform.localRotation;
                elapsedTime = 0f;
                isAnimating = true;
            }
        }

        private void Update()
        {
            if (!isAnimating) return;

            // Increment the elapsed time.
            elapsedTime += Time.deltaTime;

            // Calculate the animation progress as a normalized value (0 to 1).
            float progress = Mathf.Clamp01(elapsedTime / animationDuration);

            // Interpolate only the X-axis rotation.
            float currentXRotation = Mathf.Lerp(initialRotation.eulerAngles.x, targetXRotation, progress);

            // Preserve the Y and Z rotation values.
            Vector3 newEulerAngles = new Vector3(currentXRotation, initialRotation.eulerAngles.y, initialRotation.eulerAngles.z);

            // Apply the new rotation.
            transform.localRotation = Quaternion.Euler(newEulerAngles);

            // Stop the animation when it completes.
            if (progress >= 1f)
            {
                isAnimating = false;
            }
        }
    }
}
