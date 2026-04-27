using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    public class TentacleScript : MonoBehaviour
    {
        public Animator animator;

        public AudioSource attackWarning;
        public AudioSource slamSound;

        // The GameObject representing the hitbox (indicator)
        public GameObject hitIndicator;

        public TentacleDamageHitbox hitbox1;
        public TentacleDamageHitbox hitbox2;
        public TentacleDamageHitbox hitbox3;

        // The renderer used to display the visual telegraph
        public MeshRenderer hitboxRenderer;

        public void Start()
        {
            hitIndicator.SetActive(false);
        }

        public async void attackGround()
        {
            attackWarning.Play();
            hitbox1.clear();
            hitbox2.clear();
            hitbox3.clear();

            // 1) Show & Flash Hitbox as a warning
            await ShowAndFlashHitbox(1.5f, 0.2f);

            // Start the slam animation
            animator.Play("Slam");

            // The animation takes ~2.23 seconds before actual impact
            // So keep it hidden or off for some of that time, or keep it visible longer if desired
            await Task.Delay(2200);

            // 2) Right before the impact, you can quickly show the hitbox again if you like
            //    or simply activate it if the player must see the final strike zone:
            slamSound.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

            // Possibly show the hitbox briefly at impact
            hitIndicator.SetActive(true);
            hitboxRenderer.enabled = true;

            // Wait a short moment (~30ms) for the effect
            await Task.Delay(30);

            // The actual hit calculation can happen here
            // e.g. if you have a script that checks collisions OnTriggerEnter 
            // or do a Physics.OverlapBox manually
            // Then hide the hitbox again
            hitIndicator.SetActive(false);
            hitboxRenderer.enabled = false;
        }

        /// <summary>
        /// Shows the hitbox object and flashes it on/off as a "warning" for the given duration.
        /// flashInterval = time between toggles (e.g., 0.2 seconds).
        /// </summary>
        private async Task ShowAndFlashHitbox(float warningDuration, float flashInterval)
        {
            // Ensure it's visible to start
            hitIndicator.SetActive(true);
            hitboxRenderer.enabled = true;

            float elapsed = 0f;
            bool visible = true;

            while (elapsed < warningDuration)
            {
                // Toggle after each interval
                await Task.Delay((int)(flashInterval * 1000));

                visible = !visible;
                hitboxRenderer.enabled = visible;

                elapsed += flashInterval;
            }

            // Hide it at the end of the flash
            hitIndicator.SetActive(false);
            hitboxRenderer.enabled = false;
        }
    }
}
