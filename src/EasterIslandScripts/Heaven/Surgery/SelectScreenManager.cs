using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine.PlayerLoop;
using UnityEngine.Video;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Surgery
{
    // manage the display, playing a video of the monster selected,
    // respond to button events
    public class SelectScreenManager : NetworkBehaviour
    {
        public VideoPlayer screen;

        // we will manually retrieve these in the final product rather
        // than duplicate them in the pack
        public int selectedID = 0;  // 0 = dog, 1 = baboon
        public VideoClip mouthDogClip;
        public VideoClip baboonHawkClip;
        private VideoClip selectedClip;

        public void Update()
        {
            if (!screen.isPlaying || screen.isPaused)
            {
                screen.Play();
            }
        }

        [ClientRpc]
        public void swapClientRpc()
        {
            selectedID += 1;
            if (selectedID > 1) { selectedID = 0; }

            switch (selectedID)
            {
                case 0:
                    selectedClip = mouthDogClip;
                    break;
                case 1:
                    selectedClip = baboonHawkClip;
                    break;
            }

            screen.clip = selectedClip;
        }
    }
}
