using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;
using GameNetcodeStuff;
using UnityEngine.InputSystem;

namespace EasterIsland.src.EasterIslandScripts.Heaven.BodyMods
{
    public class BaboonWingMod : NetworkBehaviour
    {
        public PlayerControllerB player;
        private Vector3 offset = new Vector3(0, 0, 0);
        private Vector3 rotOffset = new Vector3(0, 0, 0);
        float cooldown = 0.5f;
        float lastActuation = 0;
        float force = 2f;
        InputActionPhase prevJump = InputActionPhase.Waiting;

        public AudioSource flapSound;
        public AudioSource[] baboonSquawks;
        private System.Random rnd = new System.Random();
        public GameObject animContainer;  // for offsets
        public static void testAttach(PlayerControllerB ply)
        {
            var GO = Instantiate(Plugin.PartHawkWings, ply.transform.position, ply.transform.rotation);
            var net = GO.GetComponent<NetworkObject>();
            if (net)
            {
                net.Spawn();
                var mod = GO.GetComponent<BaboonWingMod>();
                if(mod)
                {
                    mod.player = ply;
                    mod.syncToClient();
                }
            }
            else
            {
                Debug.LogWarning("LegendOfTheMoai error: Network Object Missing from wings modification!");
            }
        }

        public async void syncToClient()
        {
            await Task.Delay(1000); // allows client object initiation
            clientAttachClientRpc(player.NetworkObject.NetworkObjectId);
        }

        [ClientRpc]
        public void clientAttachClientRpc(ulong playerid)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            foreach (var ply in players)
            {
                if (ply.NetworkObject.NetworkObjectId == playerid)
                {
                    player = ply;
                    this.transform.SetParent(player.transform.Find("ScavengerModel/metarig/spine/spine.001"));
                }
            }
        }

        public void Update()
        {
            var playerHeadLocation = player.gameObject.transform.Find("TurnCompass");
            var playerHeadAccurateLocation = player.transform.Find("ScavengerModel/metarig/spine/spine.001");

            // snap attachment to back
            if (playerHeadAccurateLocation)
            {
                this.transform.localPosition = offset;
                this.transform.localRotation = Quaternion.Euler(rotOffset);
            }

            var m = RoundManager.Instance;
            var j = player.playerActions.FindAction("Jump", false);

            if (prevJump != j.phase && m.playersManager.localPlayerController.NetworkObjectId == player.NetworkObjectId)
            {

                if (lastActuation + cooldown < Time.time)
                {
                    // Action Body
                    var loc = player.transform.position + player.velocityLastFrame*-1;
                    loc.y -= 0.12f;

                    //Debug.Log("JUMP");
                    lastActuation = Time.time;

                    if(player.fallValue > -12)
                    {
                        Landmine.SpawnExplosion(loc, false, 0, 0, 0, force/3f);
                        player.ResetFallGravity();
                        if (m.IsHost)
                        {
                            PlayFlapClientRpc();
                        }
                        else
                        {
                            PlayFlapServerRpc();
                        }
                    }
                    else
                    {
                        // requires stamina
                        if (m.playersManager.localPlayerController.sprintMeter < 0.35f) { return; }
                        else
                        {
                            m.playersManager.localPlayerController.sprintMeter -= 0.35f;
                        }

                        Landmine.SpawnExplosion(loc, false, 0, 0, 0, force);
                        player.ResetFallGravity();
                        if (m.IsHost)
                        {
                            PlaySquawkClientRpc();
                        }
                        else
                        {
                            PlaySquawkServerRpc();
                        }
                    }
                }
            }

            prevJump = j.phase;

            // snap to player's back
            this.transform.position = player.transform.position + offset;
            this.transform.rotation = player.transform.rotation;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaySquawkServerRpc()
        {
            PlaySquawkClientRpc();
        }

        [ClientRpc]
        public void PlaySquawkClientRpc()
        {
            baboonSquawks[rnd.Next(0, baboonSquawks.Length)].Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayFlapServerRpc()
        {
            PlayFlapClientRpc();
        }

        [ClientRpc]
        public void PlayFlapClientRpc()
        {
            flapSound.Play();
        }
    }
}
