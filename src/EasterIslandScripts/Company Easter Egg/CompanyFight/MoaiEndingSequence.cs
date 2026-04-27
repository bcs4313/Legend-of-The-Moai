using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;
    using System.Collections;
    using TMPro;

    public class MoaiEndingSequence : MonoBehaviour
    {
        public CanvasGroup fadeCanvas;  // Assign your Canvas Group in Inspector
        public TextMeshProUGUI titleText;          // Assign your UI Text for "Legend of the Moai"
        public TextMeshProUGUI endText;            // Assign your UI Text for "The End?"
        public AudioSource moaiSound;   // Assign an AudioSource with the Moai sound
        public float fadeSpeed;
        public float delayBeforeReset;

        public AudioSource rockHitSound;
        public AudioSource creditMusic;

        public CanvasGroup LikeGroup;

        private void Start()
        {
            fadeCanvas.alpha = 0;  // Ensure screen starts clear
            LikeGroup.alpha = 0;
            titleText.enabled = false;
            endText.enabled = false;
        }

        public void TriggerMoaiEnding()
        {
            StartCoroutine(PlayEndingSequence());
        }

        private IEnumerator PlayEndingSequence()
        {
            // Fade to black
            while (fadeCanvas.alpha < 1)
            {
                fadeCanvas.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }

            // Play Moai sounds
            moaiSound.Play();

            // Display title text
            titleText.enabled = true;
            yield return new WaitForSeconds(4f);  // Delay before showing "The End?"

            rockHitSound.Play();  // actually a company sound but whatever
            endText.enabled = true;

            yield return new WaitForSeconds(2.35f);

            LikeGroup.alpha = 1;

            yield return new WaitForSeconds(delayBeforeReset);

            // Reset game or return to menu
            //SceneManager.LoadScene("MainMenu"); // Change to your main menu scene name
            GameNetworkManager.Instance.Disconnect();
        }
    }
}
