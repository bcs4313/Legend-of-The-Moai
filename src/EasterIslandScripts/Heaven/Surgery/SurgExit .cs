using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg
{
    class SurgExit : NetworkBehaviour
    {
        public String DestName;

        public void teleportOutBase(PlayerControllerB target)
        {
            if (target == null)
            {
                target = RoundManager.Instance.playersManager.localPlayerController;
            }
            Debug.Log("TeleportOutShip: " + target);

            if (RoundManager.Instance.IsHost)
            {
                teleportOutBaseClientRpc(target.NetworkObject.NetworkObjectId);
            }
            else
            {
                teleportOutBaseServerRpc(target.NetworkObject.NetworkObjectId);
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void teleportOutBaseServerRpc(ulong uid)
        {
            teleportOutBaseClientRpc(uid);
        }

        [ClientRpc]
        public void teleportOutBaseClientRpc(ulong uid)
        {
            Debug.Log("TeleportOutShipC: " + uid);
            var ply = getPlayer(uid);
            Debug.Log("TeleportOutShipC: " + ply);
            ply.transform.position = GameObject.Find(DestName).transform.position;
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
