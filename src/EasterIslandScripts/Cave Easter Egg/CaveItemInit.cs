using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;

namespace EasterIsland.src.EasterIslandScripts.Environmental
{
    // teleporter is one way
    internal class CaveTeleporter2 : MonoBehaviour
    {
        public string dest;  // find destination by string

        // sound sources
        public AudioSource stage1;
        public AudioSource stage2;
        public AudioSource stage3;
        public AudioSource teleportSound;

        // internal logic
        private float charge;  // 100+ charge initiates teleport
        private int cycle = 0;
        private bool charging = false;

        protected float teleportDist = 10;

        public void Start()
        {
        }

        void Update()   
        {
            if (charging) { charge += 25f * Time.deltaTime; } // 4 seconds to charge up completely

            if(stage1 == null || stage2 == null || stage3 == null)
            {
                Destroy(gameObject);
            }

            var destination = GameObject.Find(dest);

            soundLogic(charge);


            if (charge > 100)
            {
                if (RoundManager.Instance.IsServer)
                {
                    teleportPlayers(destination.transform.position);
                }
                else
                {
                    teleportPlayers(destination.transform.position);
                }
                playSound(3);
                charge = 0;
            }

            if (cycle < 20)
            {
                cycle++;
                return;
            }
            else
            {
                cycle = 0;
            }

            charging = getNearestPlayers().Count > 0;

        }

        private void soundLogic(float c)
        {
            if (c < 25 && !stage1.isPlaying)
            {
                playSound(0);
            }
            else if (c < 50 && !stage2.isPlaying)
            {
                playSound(1);
            }
            else if (c < 75 && !stage3.isPlaying)
            {
                playSound(2);
            }
        }

        private void playSound(int id)
        {
            switch (id)
            {
                case 0:
                    stage1.Play();
                    stage2.Stop();
                    stage3.Stop();
                    break;
                case 1:
                    stage2.Play();
                    stage1.Stop();
                    stage3.Stop();
                    break;
                case 2:
                    stage3.Play();
                    stage1.Stop();
                    stage2.Stop();
                    break;
                case 3:
                    teleportSound.Play();
                    stage1.Stop();
                    stage2.Stop();
                    stage3.Stop();
                    break;
            }
        }

        private void teleportPlayers(Vector3 position)
        {
            foreach (PlayerControllerB player in getNearestPlayers())
            {
                player.transform.position = position;
            }
        }

        private List<PlayerControllerB> getNearestPlayers()
        {
            RoundManager m = RoundManager.Instance;
            var players = m.playersManager.allPlayerScripts;
            var nearPlayers = new List<PlayerControllerB>();

            foreach (PlayerControllerB player in players)
            {
                if (Vector3.Distance(player.transform.position, transform.position) <= teleportDist)
                {
                    nearPlayers.Add(player);
                }
            }

            return nearPlayers;
        }
    }
}
