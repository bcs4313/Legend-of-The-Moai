
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts
{
    public class GumScript : NetworkBehaviour
    {
        public System.Random random;
        public AudioClip[] warioDropSounds;
        public AudioClip[] warioPickupSounds;
        public AudioClip consumeSound;
        public AudioSource eatenAudioSource;

        public GrabbableObject wario;
        public bool held = true;

        public ScanNodeProperties scan;

        float rarity;

        public float jumpBoost;
        public float speedBoost;
        public float healthBoost;
        public float impurity;
        public int durationMs;

        public void Start()
        {
            try
            {
                random = new System.Random();
                if (RoundManager.Instance.IsHost)
                {
                    rarity = GenerateRarity();

                    List<string> availableBuffs = new List<string> { "Jump", "Speed", "Health" };

                    float chanceToAddBuff = 1.0f;
                    int div = 1;

                    while (random.NextDouble() < chanceToAddBuff && availableBuffs.Count > 0)
                    {
                        var buffToAdd = availableBuffs[random.Next(0, availableBuffs.Count)];
                        availableBuffs.Remove(buffToAdd);

                        if (buffToAdd.Equals("Jump"))
                        {
                            jumpBoost = (float)((random.NextDouble() * 0.2 + 0.05) * rarity + 1);
                        }

                        if (buffToAdd.Equals("Health"))
                        {
                            healthBoost = (float)((random.NextDouble() * 0.25) * rarity);
                        }

                        if (buffToAdd.Equals("Speed"))
                        {
                            speedBoost = (float)((random.NextDouble() * 0.5 + 0.1) * rarity + 1);
                        }

                        chanceToAddBuff = Math.Max(0.1f, 0.5f * rarity / div);
                        div++;
                    }

                    durationMs = (int)(random.Next(1000, (int)(800 * rarity + 4000)) * rarity);
                    impurity = (float)Math.Max(0, 1 - (random.NextDouble() * random.NextDouble() * rarity));
                    setWarioStatsClientRpc(speedBoost, healthBoost, jumpBoost, durationMs, impurity, rarity);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        [ClientRpc]
        public void setWarioStatsClientRpc(float _speedBoost, float _healthboost, float _jumpboost, int _durationMs, float _impurity, float _rarity)
        {
            rarity = _rarity;
            impurity = _impurity;
            durationMs = _durationMs;
            jumpBoost = _jumpboost;
            healthBoost = _healthboost;
            speedBoost = _speedBoost;


            if (rarity >= 10f)
            {
                this.scan.headerText = "Gum Gum (Mythical)";
            }
            else if (rarity >= 6f)
            {
                this.scan.headerText = "Gum Gum (Legendary)";
            }
            else if (rarity >= 4f)
            {
                this.scan.headerText = "Gum Gum (Epic)";
            }
            else if (rarity >= 3f)
            {
                this.scan.headerText = "Gum Gum (Rare)";
            }
            else if (rarity >= 2f)
            {
                this.scan.headerText = "Gum Gum (Uncommon)";
            }
            else
            {
                this.scan.headerText = "Gum Gum (Common)";
            }
        }

        // rarities:
        // 1 = common
        // 1.5 = uncommon
        // 2 = rare
        // 3 = epic
        // 4 = legendary
        // 10 = mythical
        // Target probabilities for each rarity threshold
        private readonly (float threshold, float probability)[] rarityProbabilities = {
            (1f, 0.35f),    // Common
            (2f, 0.25f),   // Uncommon
            (3f, 0.18f),   // Rare
            (4f, 0.12f),   // Epic
            (6f, 0.07f),   // Legendary
            (10f, 0.03f)   // Mythical
        };

        float GenerateRarity()
        {
            float randValue = (float)random.NextDouble(); // Generate a number between 0 and 1
            float cumulativeProbability = 0f;

            foreach (var (threshold, probability) in rarityProbabilities)
            {
                cumulativeProbability += probability; // Accumulate probability
                if (randValue < cumulativeProbability)
                    return threshold; // Select this rarity
            }

            return 1f; // Default to Common if all else fails
        }

        public void Update()
        {
            if(!wario.isHeld || (wario.isHeld && wario.isPocketed))
            {
                if (held && RoundManager.Instance.IsHost)
                {
                    changeDropAndPickupSFX();
                }

                held = false;
            }
            else
            {
                held = true;
            }

            if (Plugin.controls.InspectGum.triggered)
            {
                // held check
                if (wario.isHeld && !wario.isPocketed)
                {
                    // local player check
                    if (wario.playerHeldBy.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
                    {
                        showItemStats();
                    }
                }
            }

            // check if being used
            if (wario.isBeingUsed)
            {
                // local player check
                if (wario.playerHeldBy.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
                {
                    //DropHeldItem(wario.playerHeldBy, true);
                    if(RoundManager.Instance.IsHost)
                    {
                        wario.isBeingUsed = false;
                        eatWario();
                    }
                    else
                    {
                        wario.isBeingUsed = false;
                        eatWarioServerRpc();
                    }
                }
            }
        }

        public void DropHeldItem(bool itemsFall = true, bool disconnecting = false)
        {
            var ply = this.wario.playerHeldBy;

            if(wario == null)
            {
                return;
            }

            if(this.wario.playerHeldBy == null)
            {
                return;
            }

            if (this.wario.playerHeldBy.ItemSlots == null)
            {
                return;
            }

            try
            {
                for (int i = 0; i < this.wario.playerHeldBy.ItemSlots.Length; i++)
                {
                    var grabbableObject = this.wario.playerHeldBy.ItemSlots[i];
                    if (grabbableObject != null && grabbableObject.isHeld && !grabbableObject.isPocketed)
                    {
                        if (itemsFall)
                        {
                            grabbableObject.parentObject = null;
                            grabbableObject.heldByPlayerOnServer = false;
                            if (grabbableObject.playerHeldBy.isInElevator)
                            {
                                grabbableObject.transform.SetParent(ply.playersManager.elevatorTransform, true);
                            }
                            else
                            {
                                grabbableObject.transform.SetParent(ply.playersManager.propsContainer, true);
                            }
                            grabbableObject.playerHeldBy.SetItemInElevator(ply.isInHangarShipRoom, ply.isInElevator, grabbableObject);
                            grabbableObject.EnablePhysics(true);
                            grabbableObject.EnableItemMeshes(true);
                            grabbableObject.transform.localScale = grabbableObject.originalScale;
                            grabbableObject.isHeld = false;
                            grabbableObject.isPocketed = false;
                            grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
                            grabbableObject.FallToGround(true, false, default);
                            grabbableObject.fallTime = UnityEngine.Random.Range(-0.3f, 0.05f);

                            Plugin.Logger.LogInfo($"Gum Gum: DiscardingItem");
                            grabbableObject.DiscardItemOnClient();
                            grabbableObject.playerHeldBy = null;
                        }
                        if (base.IsOwner && !disconnecting)
                        {
                            HUDManager.Instance.holdingTwoHandedItem.enabled = false;
                            HUDManager.Instance.itemSlotIcons[i].enabled = false;
                            HUDManager.Instance.ClearControlTips();
                            ply.activatingItem = false;
                        }
                        ply.ItemSlots[i] = null;
                        break;
                    }
                }

                Plugin.Logger.LogInfo($"Gum Gum: Resetting player hand...");
                ply.isHoldingObject = false;
                SetSpecialGrabAnimationBool(false, ply.currentlyHeldObjectServer, ply);
                ply.playerBodyAnimator.SetBool("cancelHolding", true);
                ply.playerBodyAnimator.SetTrigger("Throw");
                ply.activatingItem = false;
                ply.currentlyHeldObjectServer = null;
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private void SetSpecialGrabAnimationBool(bool setTrue, GrabbableObject currentItem, PlayerControllerB ply)
        {
            if (!base.IsOwner)
            {
                return;
            }
            ply.playerBodyAnimator.SetBool("Grab", setTrue);
            if (!string.IsNullOrEmpty(currentItem.itemProperties.grabAnim))
            {
                try
                {
                    ply.playerBodyAnimator.SetBool(currentItem.itemProperties.grabAnim, setTrue);
                }
                catch (Exception)
                {
                    Debug.LogError("An item tried to set an animator bool which does not exist: " + currentItem.itemProperties.grabAnim);
                }
            }
        }


        public void eatWario()
        {
            activateBuffClientRpc(Plugin.highPlayers.Contains(wario.playerHeldBy));
        }

        [ClientRpc]
        public void warnPlayerClientRpc()
        {
            if (wario.playerHeldBy && wario.playerHeldBy.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
            {
                HUDManager.Instance.DisplayTip("Hey DUM DUM", "You already ate your gum gum. Wait first to let me have Sum Sum.");
                wario.isBeingUsed = false;
            }
        }

        [ClientRpc]
        public void activateBuffClientRpc(bool guard)
        {
            PlayerControllerB playerTarget = wario.playerHeldBy;
            if (Plugin.highPlayers.Contains(wario.playerHeldBy) || guard)
            {
                HUDManager.Instance.DisplayTip("Hey DUM DUM", "You already ate your gum gum. Wait first to let me have Sum Sum.");
                wario.isBeingUsed = false;
                return;
            }

            Destroy(wario.gameObject.GetComponent<BoxCollider>());
            GameObject.Destroy(wario.gameObject.transform.Find("Sphere").GetComponent<MeshRenderer>());
            Destroy(wario.gameObject.transform.Find("ScanNode").GetComponent<BoxCollider>());

            DropHeldItem();

            // local player check
            Plugin.Logger.LogDebug("Gum Gum: Local Player Check...");
            if (playerTarget && playerTarget.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
            {
                // buff player + sound
                if (RoundManager.Instance.IsHost)
                {
                    Plugin.Logger.LogDebug("Gum Gum: Applying Buffs...");
                    applyBuffsClientRpc(RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId);
                }
                else
                {
                    Plugin.Logger.LogDebug("Gum Gum: Applying Buffs...");
                    applyBuffsServerRpc(RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId);
                }
            }

            wario.DiscardItemOnClient();       
        }

        [ServerRpc(RequireOwnership = false)]
        public void applyBuffsServerRpc(ulong uid)
        {
            applyBuffsClientRpc(uid);
        }

        [ClientRpc]
        public void applyBuffsClientRpc(ulong uid)
        {
            eatenAudioSource.Play();
            applyBuffstoClient(uid);
        }

        public async void applyBuffstoClient(ulong uid)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player.NetworkObject.NetworkObjectId == uid)
                {
                    Plugin.highPlayers.Add(player);
                    player.DamagePlayer((int)(healthBoost * -100));
                    if (player.health > 100)
                    {
                        player.health = 100;
                    }
                    var genericSpeed = 4.6f;
                    var genericJumpHeight = 13f;
                    var genericClimbSpeed = 3;

                    player.drunkness = impurity;

                    player.movementSpeed = genericSpeed * speedBoost;
                    player.jumpForce = genericJumpHeight * jumpBoost;
                    player.climbSpeed = genericClimbSpeed * speedBoost * jumpBoost;
                    await Task.Delay(durationMs);
                    player.movementSpeed = genericSpeed;
                    player.jumpForce = genericJumpHeight;
                    player.climbSpeed = genericClimbSpeed;

                    if (player.health > 100)
                    {
                        player.health = 100;
                    }

                    // destroy and remove high status
                    Plugin.highPlayers.Remove(player);
                    
                    if(RoundManager.Instance.IsHost)
                    {
                        Demolish();
                    }
                    else
                    {
                        destroyServerRpc();
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void destroyServerRpc()
        {
            Demolish();
        }

        public void Demolish()
        {
            Destroy(this.gameObject);
        }
        

        [ServerRpc (RequireOwnership = false)]
        public void eatWarioServerRpc()
        {
            eatWario();
        }

        public void changeDropAndPickupSFX()
        {
            setSFXClientRpc(random.Next(0, warioDropSounds.Length), random.Next(0, warioPickupSounds.Length));
        }

        [ClientRpc]
        public void setSFXClientRpc(int dropIndex, int pickupIndex)
        {
            AudioClip dropSound = warioDropSounds[dropIndex];
            AudioClip pickupSound = warioPickupSounds[pickupIndex];

            wario.itemProperties.dropSFX = dropSound;
            wario.itemProperties.grabSFX = pickupSound;
        }

        public void showItemStats()
        {
            String info = "";
            String rarityTime = "";

            if (rarity >= 10f)
            {
                rarityTime += "Rarity: Mythical ";
            }
            else if (rarity >= 6f)
            {
                rarityTime += "Rarity: Legendary ";
            }
            else if (rarity >= 4f)
            {
                rarityTime += "Rarity: Epic ";
            }
            else if (rarity >= 3f)
            {
                rarityTime += "Rarity: Rare ";
            }
            else if (rarity >= 2f)
            {
                rarityTime += "Rarity: Uncommon ";
            }
            else
            {
                rarityTime += "Rarity: Common ";
            }

            rarityTime += "Time: " + (durationMs)/1000 + "s\n";

            if (speedBoost != 1.0f)
            {
                info += "Speed Boost: " + 100 * (speedBoost - 1) + "%\n";
            }

            if (healthBoost != 1.0f)
            {
                info += "HP Heal: " + healthBoost * 100 + "\n";
            }

            if (jumpBoost != 1.0f)
            {
                info += "Jump Boost: " + ((jumpBoost - 1)*100) + "%\n";
            }

            info += "Purity: " + (100 - (100 * impurity)) + "%\n";

            HUDManager.Instance.DisplayTip(rarityTime, info);
        }
    }
}
