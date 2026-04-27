using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    // responsible for light adjustments and music
    // inside of the collider
    public class GoldMoaiPedestal : NetworkBehaviour
    {
        bool activated = false;
        public GameObject goldMoaiPlaceholder;  // visual, starts as disabled

        public void Start()
        {
            if (RoundManager.Instance.IsHost)
            {
                goldMoaiPlaceholder.SetActive(false);
            }
        }

        public void interact(PlayerControllerB ply)
        {
            if (yoinkItem(ply) == true) // Ensure the player has the "Player" tag
            {
                Debug.Log("Inserting Gold Moai Into Pedestal");
                if(RoundManager.Instance.IsHost)
                {
                    setActivatedClientRpc();
                }
                else
                {
                    setActivedServerRpc();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void setActivedServerRpc()
        {
            setActivatedClientRpc();
        }

        [ClientRpc]
        public void setActivatedClientRpc()
        {
            activated = true;
            goldMoaiPlaceholder.SetActive(true);
        }

        // checks target player to see if they have the target item (gold moai)
        // if they do, remove item across clients. Then return true in this method.
        public bool yoinkItem(PlayerControllerB player)
        {
            var inventory = player.ItemSlots;

            GrabbableObject targetObj = null;

            // prioritize a held object
            foreach (GrabbableObject obj in inventory)
            {
                if (!obj) { continue; }
                if (!obj.gameObject) { continue; }

                if (obj.gameObject.name.ToLower().Contains("golden") || obj.name.ToLower().Contains("golden"))
                {
                    if (obj.gameObject.transform.Find("QuantumItem(Clone)") != null)
                    {
                        if (!targetObj)
                        {
                            targetObj = obj;
                        }
                        else if (!obj.isPocketed)
                        {
                            targetObj = obj;
                        }
                    }
                }
            }

            if (targetObj)
            {
                if (RoundManager.Instance.IsHost)
                {
                    takeItemClientRpc(player.NetworkObjectId, targetObj.NetworkObjectId);
                }
                else
                {
                    takeItemServerRpc(player.NetworkObjectId, targetObj.NetworkObjectId);
                }
                return true;
            }
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void takeItemServerRpc(ulong playerid, ulong itemid)
        {
            takeItemClientRpc(playerid, itemid);
        }

        [ClientRpc]
        public void takeItemClientRpc(ulong playerid, ulong itemid)
        {
            // get player from id
            PlayerControllerB targetPlayer = getPlayer(playerid);

            if (!targetPlayer)
            {
                UnityEngine.Debug.LogError("Gold Pedestal: Could not find target player for item removal! Cancelling item removal.");
                return;
            }

            var inventory = targetPlayer.ItemSlots;
            for (int i = 0; i < inventory.Length; i++)
            {
                GrabbableObject obj = inventory[i];
                if (!obj) { continue; }
                if (!obj.gameObject) { continue; }

                if (obj.NetworkObjectId == itemid)
                {
                    if (!obj.isPocketed)
                    {
                        UnityEngine.Debug.Log("Gold Pedestal: Despawned Held Object (Gold Moai)");
                        targetPlayer.DespawnHeldObject();
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Gold Pedestal: Despawned Object in Slot (Gold Moai)");
                        targetPlayer.DestroyItemInSlotAndSync(i);
                        HUDManager.Instance.itemSlotIcons[i].enabled = false;
                    }
                    return;
                }
            }

            UnityEngine.Debug.LogError("Gold Pedestal: Could not find target item for removal (player was found)! Cancelling item removal.");
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
