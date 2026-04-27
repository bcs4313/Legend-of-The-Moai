using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts
{
    class SkyBoxBlendScript : MonoBehaviour
    {
        public Material skybox1; // Starting skybox
        public Material skybox2; // Target skybox
        public float transitionDuration = 5f; // Duration of the transition

        private float transitionProgress = 0f; // Progress of the transition

        void Start()
        {
            // Set the initial skybox
            RenderSettings.skybox = skybox1;
        }

        void Update()
        {
            // Update the transition progress over time
            transitionProgress += Time.deltaTime / transitionDuration;

            // Lerp between the two skybox materials
            RenderSettings.skybox.Lerp(skybox1, skybox2, transitionProgress);

            // Ensure the transition progress doesn't exceed 1
            if (transitionProgress > 1f)
            {
                transitionProgress = 1f;
            }
        }
    }
}
