using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;

namespace EasterIsland.src.EasterIslandScripts.Environmental
{
    // teleporter is one way
    internal class CaveTeleporter : NetworkBehaviour
    {
        public GameObject destination;
        public JudgementScript judge;

        // sound sources
        public AudioSource stage1;
        public AudioSource stage2;
        public AudioSource stage3;
        public AudioSource teleportSound;
        public AudioSource destinationTeleportSound;

        // internal logic
        private float charge;  // 100+ charge initiates teleport
        private int cycle = 0;
        private bool charging = false;

        public void Start()
        {
            if (!GetComponent<NetworkObject>().IsSpawned)
            {
                GetComponent<NetworkObject>().Spawn(true);
            }
        }

        void Update()
        {
            if (charging) { charge += 700f * Time.deltaTime; } // EXTREME CHARGE SPEED


            soundLogic(charge);


            if (charge > 100)
            {
                if (RoundManager.Instance.IsServer)
                {
                    teleportPlayersClientRpc(destination.transform.position);
                }
                else
                {
                    teleportPlayersServerRpc(destination.transform.position);
                }
                playSoundClientRpc(3);
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

            charging = getNearestPlayers(15).Count > 0;

        }

        private void soundLogic(float c)
        {
            if (c < 25 && !stage1.isPlaying)
            {
                playSoundClientRpc(0);
            }
            else if (c < 50 && !stage2.isPlaying)
            {
                playSoundClientRpc(1);
            }
            else if (c < 75 && !stage3.isPlaying)
            {
                playSoundClientRpc(2);
            }
        }

        [ClientRpc]
        private void playSoundClientRpc(int id)
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
                    destinationTeleportSound.Play();
                    stage1.Stop();
                    stage2.Stop();
                    stage3.Stop();
                    break;
            }
        }

        [ClientRpc]
        private void teleportPlayersClientRpc(Vector3 position)
        {
            foreach (PlayerControllerB player in getNearestPlayers(40))
            {
                player.transform.position = position;
                judge.beginCutscene();
            }
        }

        [ServerRpc]
        private void teleportPlayersServerRpc(Vector3 position)
        {
            teleportPlayersClientRpc(position);
        }

        private List<PlayerControllerB> getNearestPlayers(float dist)
        {
            RoundManager m = RoundManager.Instance;
            var players = m.playersManager.allPlayerScripts;
            var nearPlayers = new List<PlayerControllerB>();

            foreach (PlayerControllerB player in players)
            {
                if (Vector3.Distance(player.transform.position, transform.position) <= dist)
                {
                    nearPlayers.Add(player);
                }
            }

            return nearPlayers;
        }
    }
}
