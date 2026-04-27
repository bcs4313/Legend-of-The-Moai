using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Library_Easter_egg
{
    // insert and remove any item at will
    public class AnyItemPedestal : NetworkBehaviour
    {
        public InteractTrigger trigger;
        public Transform insertionTransform;
        public Transform dropTransform;
        public GrabbableObject insertedObj = null;
        bool hasObj = false;

        public void interact(PlayerControllerB ply)
        {
            if (!hasObj)
            {
                yoinkItem(ply);
            }
            else
            {
                grantItem(ply);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void setActivedServerRpc()
        {
            //setActivatedClientRpc();
        }

        [ClientRpc]
        public void setActivatedClientRpc()
        {
            //activated = true;
            //goldMoaiPlaceholder.SetActive(true);
        }

        [ClientRpc]
        public void boostValueClientRpc(float mult)
        {
            if (insertedObj)
            {
                insertedObj.SetScrapValue((int)(insertedObj.scrapValue * mult));
            }
        }

        // checks target player to see if they have the target item (gold moai)
        // if they do, remove item across clients. Then return the object id.
        public bool yoinkItem(PlayerControllerB player)
        {
            var inventory = player.ItemSlots;

            GrabbableObject targetObj = null;
            targetObj = player.currentlyHeldObjectServer;
            Plugin.Logger.LogMessage("Held Object being inserted: " + targetObj);
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

        public void grantItem(PlayerControllerB player)
        {
            if (RoundManager.Instance.IsHost)
            {
                onGrantClientRpc();
            }
            else
            {
                onGrantServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void onGrantServerRpc()
        {
            onGrantClientRpc();
        }

        [ClientRpc]
        public void onGrantClientRpc()
        {
            hasObj = false;
            insertedObj.enabled = true;
            insertedObj.transform.parent = null;
            //insertedObj.FallToGround(false);
            insertedObj.transform.position = dropTransform.position;
            insertedObj.startFallingPosition = dropTransform.position;
            insertedObj.targetFloorPosition = dropTransform.position;
            insertedObj.transform.position = dropTransform.position;
            trigger.hoverTip = "Insert Item";
            insertedObj = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void takeItemServerRpc(ulong playerid, ulong itemid)
        {
            takeItemClientRpc(playerid, itemid);
        }

        [ClientRpc]
        public void takeItemClientRpc(ulong playerid, ulong itemid)
        {
            hasObj = true;
            trigger.hoverTip = "Remove Item";

            // get player from id
            PlayerControllerB targetPlayer = getPlayer(playerid);

            if (!targetPlayer)
            {
                UnityEngine.Debug.LogError("Item Pedestal: Could not find target player for item removal! Cancelling item removal.");
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
                        insertedObj = DropHeldItem(targetPlayer, true);
                        if (!insertedObj)
                        {
                            UnityEngine.Debug.LogError("Item Pedestal: Could not find target item for movement to pedestal (player was found)!");
                        }
                        insertedObj.enabled = false;
                        insertedObj.gameObject.transform.parent = insertionTransform;
                        insertedObj.gameObject.transform.localPosition = Vector3.zero;
                        UnityEngine.Debug.Log("Item Pedestal: Dropped Held Object");
                    }
                    return;
                }
            }

            UnityEngine.Debug.LogError("Item Pedestal: Could not find target item for movement to pedestal (player was found)! Cancelling item removal.");
        }

        // modified code from DropAllHeldItems
        // returns an object if successful
        public GrabbableObject DropHeldItem(PlayerControllerB player, bool itemsFall = true)
        {
            var ItemSlots = player.ItemSlots;
            for (int i = 0; i < ItemSlots.Length; i++)
            {

                GrabbableObject grabbableObject = ItemSlots[i];
                if (grabbableObject != null && grabbableObject.isHeld)
                {

                    if (player.isHoldingObject)
                    {
                        player.isHoldingObject = false;
                        player.playerBodyAnimator.SetBool("cancelHolding", true);
                        player.playerBodyAnimator.SetTrigger("Throw");
                    }

                    if (itemsFall)
                    {
                        grabbableObject.parentObject = null;
                        grabbableObject.heldByPlayerOnServer = false;
                        grabbableObject.EnablePhysics(true);
                        grabbableObject.EnableItemMeshes(true);
                        grabbableObject.transform.localScale = grabbableObject.originalScale;
                        grabbableObject.isHeld = false;
                        grabbableObject.isPocketed = false;
                        if (grabbableObject.transform.parent)
                        {
                            grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
                        }
                        //grabbableObject.FallToGround(true);
                        grabbableObject.fallTime = UnityEngine.Random.Range(-0.3f, 0.05f);
                        if (base.IsOwner)
                        {
                            grabbableObject.DiscardItemOnClient();
                        }
                        else if (!grabbableObject.itemProperties.syncDiscardFunction)
                        {
                            grabbableObject.playerHeldBy = null;
                        }
                    }
                    if (base.IsOwner)
                    {
                        HUDManager.Instance.holdingTwoHandedItem.enabled = false;
                        HUDManager.Instance.itemSlotIcons[i].enabled = false;
                        HUDManager.Instance.ClearControlTips();
                        player.activatingItem = false;
                    }
                    player.ItemSlots[i] = null;
                    player.activatingItem = false;
                    player.twoHanded = false;
                    player.carryWeight = 1f;
                    player.currentlyHeldObjectServer = null;

                    return grabbableObject;
                }
            }

            return null;
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
