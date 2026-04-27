using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Threading.Tasks;

namespace EasterIsland.src.EasterIslandScripts.Environmental
{
    // teleporter is one way
    internal class CaveTeleporter3 : NetworkBehaviour
    {
        public GameObject destination;
        public string rawDestination;  // string destination input. used if not null and not ""

        // sound sources
        public AudioSource stage1;
        public AudioSource stage2;
        public AudioSource stage3;
        public AudioSource teleportSound;
        public AudioSource destinationTeleportSound;

        // internal logic
        private float charge;  // 100+ charge initiates teleport
        private bool charging = false;
        public List<PlayerControllerB> chargingPlayers;

        public float invincibilityPeriod;  // set period of time where the player is invincible after teleport (default 0, in seconds)
        public float chargeRate;
        public string preTeleTitle;  // sends a message if not blank / null
        public string preTeleMsg;  // sends a message if not blank / null

        public void Start()
        {
            if (RoundManager.Instance.IsHost && GetComponent<NetworkObject>() && !GetComponent<NetworkObject>().IsSpawned)
            {
                GetComponent<NetworkObject>().Spawn(true);
            }
            chargingPlayers = new List<PlayerControllerB>();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (isLocalPlayer(other))
            {
                if ((preTeleMsg != null && preTeleMsg != "") || (preTeleTitle != null && preTeleTitle != ""))
                {
                    HUDManager.Instance.DisplayTip(preTeleTitle, preTeleMsg);
                }
            }

            var ply = getPlayer(other);
            if (ply != null && !chargingPlayers.Contains(ply)) // Ensure the player has the "Player" tag
            {
                chargingPlayers.Add(ply);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            var ply = getPlayer(other);
            if (ply != null && chargingPlayers.Contains(ply)) // Ensure the player has the "Player" tag
            {
                chargingPlayers.Remove(ply);
            }
        }

        public PlayerControllerB getPlayer(Collider other)
        {
            GameObject plyGO = other.gameObject;
            if (plyGO == null) { return null; }

            PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                return ply;
            }

            return null;
        }

        void Update()
        {
            if(!RoundManager.Instance.IsHost) { return; }
            if(chargingPlayers.Count > 0) 
            { 
                charging = true; 
            } 
            else 
            { 
                charging = false; 
            }
            if (charging) { charge += chargeRate * Time.deltaTime; } // EXTREME CHARGE SPEED


            soundLogic(charge);


            Vector3 dest = Vector3.zero;
            if (rawDestination != null && rawDestination != "")
            {
                dest = GameObject.Find(rawDestination).transform.position;
            }
            else
            {
                dest = destination.transform.position;
            }
            

            if (charge > 100)
            {
                if (RoundManager.Instance.IsHost)
                {
                    teleportPlayersClientRpc(dest);
                }
                else
                {
                    teleportPlayersServerRpc(dest);
                }
                playSoundClientRpc(3);
                charge = 0;
            }
        }

        public bool isLocalPlayer(Collider other)
        {
            GameObject plyGO = other.gameObject;
            if (plyGO == null) { return false; }

            PlayerControllerB ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                var enteringPlayerUID = ply.NetworkObject.NetworkObjectId;
                var localPlayerUID = RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId;
                if (enteringPlayerUID == localPlayerUID)
                {
                    return true;
                }
            }

            return false;
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
                    if (teleportSound != null) { teleportSound.Play(); };
                    if (destinationTeleportSound != null) { destinationTeleportSound.Play(); };
                    stage1.Stop();
                    stage2.Stop();
                    stage3.Stop();
                    break;
            }
        }

        [ClientRpc]
        private void teleportPlayersClientRpc(Vector3 position)
        {
            foreach (PlayerControllerB player in chargingPlayers)
            {
                player.transform.position = position;

                if (invincibilityPeriod > 0)
                {
                    tempHealth(player);
                }
            }
        }

        private async void tempHealth(PlayerControllerB player)
        {
            var tempHealth = player.health;
            player.health = 1000;
            await Task.Delay((int)(1000 * invincibilityPeriod));
            player.health = tempHealth;
        }

        [ServerRpc]
        private void teleportPlayersServerRpc(Vector3 position)
        {
            teleportPlayersClientRpc(position);
        }
    }
}
