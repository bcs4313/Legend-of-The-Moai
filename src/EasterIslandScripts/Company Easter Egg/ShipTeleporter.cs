using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg
{
    class ShipTeleporter : NetworkBehaviour
    {
        public String outsideShipDestName;
        public String insideShipDestName;

        public void teleportInShip(PlayerControllerB target)
        {
            if (target == null)
            {
                target = RoundManager.Instance.playersManager.localPlayerController;
            }
            Debug.Log("TeleportInShip: " + target);

            if (RoundManager.Instance.IsHost)
            {
                teleportInShipClientRpc(target.NetworkObject.NetworkObjectId);
            }
            else
            {
                teleportInShipServerRpc(target.NetworkObject.NetworkObjectId);
            }
        }

        public void teleportOutShip(PlayerControllerB target)
        {
            if (target == null)
            {
                target = RoundManager.Instance.playersManager.localPlayerController;
            }
            Debug.Log("TeleportOutShip: " + target);

            if (RoundManager.Instance.IsHost)
            {
                teleportOutShipClientRpc(target.NetworkObject.NetworkObjectId);
            }
            else
            {
                teleportOutShipServerRpc(target.NetworkObject.NetworkObjectId);
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void teleportInShipServerRpc(ulong uid)
        {
            teleportInShipClientRpc(uid);
        }

        [ServerRpc(RequireOwnership = false)]
        public void teleportOutShipServerRpc(ulong uid)
        {
            teleportOutShipClientRpc(uid);
        }

        [ClientRpc]
        public void teleportInShipClientRpc(ulong uid)
        {
            Debug.Log("TeleportInShipC: " + uid);
            var ply = getPlayer(uid);
            Debug.Log("TeleportInShipC: " + ply);
            ply.transform.position = GameObject.Find(insideShipDestName).transform.position;
        }

        [ClientRpc]
        public void teleportOutShipClientRpc(ulong uid)
        {
            Debug.Log("TeleportOutShipC: " + uid);
            var ply = getPlayer(uid);
            Debug.Log("TeleportOutShipC: " + ply);
            ply.transform.position = GameObject.Find(outsideShipDestName).transform.position;
        }

        public PlayerControllerB getPlayer(ulong playerid)
        {

            // get player from id
            var scripts = RoundManager.Instance.playersManager.allPlayerScripts;
            PlayerControllerB targetPlayer = null;
            for (int i = 0; i < scripts.Length; i++)
            {
                var player = scripts[i];
                if (player.NetworkObjectId == playerid)
                {
                    targetPlayer = player;
                }
            }
            return targetPlayer;
        }
    }
}
