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
using LethalLib.Modules;
using System.Threading;
using System.Net;
using System.Collections;
using UnityEngine.UIElements;

namespace EasterIsland.src.EasterIslandScripts.Heaven.BodyMods
{
    public class DogHeadMod : NetworkBehaviour
    {
        public PlayerControllerB player;
        private Vector3 offset = Vector3.zero;
        private Vector3 rotOffset = new Vector3(0, 0, 270);
        float cooldown = 9f;
        bool coolingDown = true;
        float coolTimer = 9f;
        InputActionPhase prevJump = InputActionPhase.Waiting;

        public AudioSource dogGrowl;
        public AudioSource[] dogborks;
        public AudioSource dogMunch;
        private System.Random rnd = new System.Random();

        // bite logic
        protected float maxPlayerBiteRange = 7.5f;
        protected float maxEnemyBiteRange = 16f;
        public AnimationClip biteAnimation;
        public Transform bitePoint;
        private float thrashTime = 3.3f;
        private float timeThrashStarted = 0;
        private float timeDashStarted = 0;
        private float dashTime = 1f;
        private bool forceThrasAnim = false;

        public Transform headTransformLink;

        private PlayerControllerB attachedPlayer;
        private PlayerControllerB targetedPlayer;
        private EnemyAI attachedEnemy;
        private EnemyAI targetedEnemy;

        public SkinnedMeshRenderer mesh1;
        public MeshRenderer mesh2;
        public MeshRenderer mesh3;

        // new: tick damage
        private int playerTicksDone = 0;
        private int monsterTicksDone = 0;
        private float lastTickTime = 0f;

        public static void testAttach(PlayerControllerB ply)
        {
            var GO = Instantiate(Plugin.PartDogHead, ply.transform.position, ply.transform.rotation);
            var net = GO.GetComponent<NetworkObject>();
            if (net)
            {
                net.Spawn();
                var mod = GO.GetComponent<DogHeadMod>();
                if (mod)
                {
                    mod.player = ply;
                    mod.syncToClient();
                }
            }
            else
            {
                Debug.LogWarning("LegendOfTheMoai error: Network Object Missing from mouthdog modification!");
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
            foreach(var ply in players)
            {
                if(ply.NetworkObject.NetworkObjectId == playerid)
                {
                    player = ply;
                    if(ply.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
                    {
                        mesh1.enabled = false;
                        mesh2.enabled = false;
                        mesh3.enabled = false;
                    }
                }
            }
        }

        // serverside trigger for thrashing
        bool chompTrigger = false;
        [ServerRpc(RequireOwnership = false)]
        public void initiateChompServerRpc()
        {
            chompTrigger = true;
        }

        [ClientRpc]
        public void SetPlayerPositionClientRpc(ulong playerid, Vector3 position)
        {
            PlayerControllerB playerTarget = null;

            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            foreach (var ply in players)
            {
                if (ply.NetworkObject.NetworkObjectId == playerid)
                {
                    playerTarget = ply;
                }
            }

            if (playerTarget != null)
            {
                playerTarget.transform.position = position;
            }
        }


        [ClientRpc]
        public void dmgPlayerClientRpc(ulong playerid, int amount)
        {
            PlayerControllerB playerTarget = null;

            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            foreach (var ply in players)
            {
                if (ply.NetworkObject.NetworkObjectId == playerid)
                {
                    playerTarget = ply;
                }
            }

            if (playerTarget != null)
            {
                playerTarget.DamagePlayer(amount);
            }
        }

        void Update()
        {
            var playerHeadLocation = player.gameObject.transform.Find("TurnCompass");
            var playerHeadAccurateLocation = player.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadPoint");

            // snap attachment to Head
            this.transform.position = player.transform.position + offset;
            this.transform.rotation = player.transform.rotation;
            if (playerHeadAccurateLocation)
            {
                headTransformLink.transform.position = playerHeadAccurateLocation.position + offset;
                headTransformLink.transform.rotation = playerHeadAccurateLocation.rotation * Quaternion.Euler(rotOffset);
            }
            else
            {
                headTransformLink.transform.position = playerHeadLocation.position + offset;
                headTransformLink.transform.rotation = playerHeadLocation.rotation * Quaternion.Euler(rotOffset);
            }

            var m = RoundManager.Instance;
            var j = player.playerActions.FindAction("Crouch", false);

            // client trigger
            if(prevJump != j.phase && !player.isPlayerDead && player.NetworkObject.NetworkObjectId == RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId)
            {
                prevJump = j.phase;
                initiateChompServerRpc();
            }

            // below this is server only logic
            if(!RoundManager.Instance.IsHost) { return; }

            // bite start
            if (chompTrigger)
            {
                chompTrigger = false;
                if (coolTimer < 0f)
                {
                    PlayerControllerB playerScan = scanForPlayerTarget();
                    EnemyAI monsterScan = scanForMonsterTarget();

                    if (playerScan)
                    {
                        targetedPlayer = playerScan;
                        timeDashStarted = Time.time;
                        prevJump = j.phase;
                        PlayBorkClientRpc();
                        DisablePlayerCollidersClientRpc();
                        coolingDown = true;
                        coolTimer = cooldown;
                        return;
                    }
                    else if (monsterScan)
                    {
                        targetedEnemy = monsterScan;
                        timeDashStarted = Time.time;
                        prevJump = j.phase;
                        PlayBorkClientRpc();
                        DisablePlayerCollidersClientRpc();
                        coolingDown = true;
                        coolTimer = cooldown;
                        return;
                    }
                }
            }

            prevJump = j.phase;

            // tween dash toward player
            if (targetedPlayer != null)
            {
                float t = (Time.time - timeDashStarted) / dashTime;
                if (t < 1f)
                {
                    var loc = Vector3.Lerp(
                        player.transform.position + offset,
                        targetedPlayer.transform.position,
                        t
                    );
                    SetPlayerPositionClientRpc(player.NetworkObject.NetworkObjectId, loc);
                }
                else
                {
                    attachedPlayer = targetedPlayer;
                    targetedPlayer = null;
                    timeThrashStarted = Time.time;
                    lastTickTime = Time.time;
                    playerTicksDone = 0;
                    monsterTicksDone = 0;
                    PlayMunchClientRpc();
                }
                return;
            }

            // tween dash toward enemy
            if (targetedEnemy != null)
            {
                float t = (Time.time - timeDashStarted) / dashTime;
                if (t < 1f)
                {
                    var loc = Vector3.Lerp(
                        player.transform.position + offset,
                        targetedEnemy.transform.position,
                        t
                    );
                    SetPlayerPositionClientRpc(player.NetworkObject.NetworkObjectId, loc);
                }
                else
                {
                    attachedEnemy = targetedEnemy;
                    targetedEnemy = null;
                    timeThrashStarted = Time.time;
                    lastTickTime = Time.time;
                    playerTicksDone = 0;
                    monsterTicksDone = 0;
                    PlayMunchClientRpc();
                }
                return;
            }

            // attachedPlayer thrash
            if (attachedPlayer != null)
            {
                float elapsed = Time.time - timeThrashStarted;
                if (elapsed < thrashTime)
                {
                    if (elapsed < 0.1f)
                        PlayDogAnimationClientRpc();

                    SetPlayerPositionClientRpc(attachedPlayer.NetworkObject.NetworkObjectId, headTransformLink.transform.position);

                    float wiggleAmount = Mathf.Sin(elapsed * 20f) * 20f;
                    setHeadTransformLinkRotationClientRpc(wiggleAmount);
                    attachedPlayer.ResetFallGravity();
                    player.ResetFallGravity();
                    // DAMAGE tick
                    float tickInterval = thrashTime / 5f;
                    if (Time.time - lastTickTime >= tickInterval && playerTicksDone < 5)
                    {
                        dmgPlayerClientRpc(attachedPlayer.NetworkObject.NetworkObjectId, 10);

                        lastTickTime = Time.time;
                        playerTicksDone++;

                        if (player.health <= 90)
                        {
                            dmgPlayerClientRpc(player.NetworkObject.NetworkObjectId, -10);
                        }
                    }
                }
                else
                {
                    attachedPlayer = null;
                    EnablePlayerCollidersClientRpc();
                }
                return;
            }

            // attachedEnemy thrash
            if (attachedEnemy != null)
            {
                float elapsed = Time.time - timeThrashStarted;
                if (elapsed < thrashTime)
                {
                    if (elapsed < 0.1f)
                        PlayDogAnimationClientRpc();

                    SetMonsterPositionClientRpc(attachedEnemy.NetworkObject.NetworkObjectId, headTransformLink.transform.position);
                    player.ResetFallGravity();
                    float wiggleAmount = Mathf.Sin(elapsed * 20f) * 20f;
                    setHeadTransformLinkRotationClientRpc(wiggleAmount);

                    // DAMAGE tick
                    float tickInterval = thrashTime / 5f;
                    if (Time.time - lastTickTime >= tickInterval && monsterTicksDone < 5)
                    {
                        attachedEnemy.HitEnemy (1, player, true);
                        lastTickTime = Time.time;
                        monsterTicksDone++;
                        if(player.health <= 90)
                        {
                            dmgPlayerClientRpc(player.NetworkObject.NetworkObjectId, -10);
                        }
                    }
                }
                else
                {
                    attachedEnemy = null;
                    EnablePlayerCollidersClientRpc();
                }
                return;
            }

            if(coolingDown == true && coolTimer <= 0)
            {
                coolingDown = false;
                PlayGrowlClientRpc();
            }
            else
            {
                coolTimer -= Time.deltaTime;
            }
        }

        private List<Collider> disabledColliders = new List<Collider>();

        [ClientRpc]
        void DisablePlayerCollidersClientRpc()
        {
            disabledColliders.Clear();
            foreach (var col in player.GetComponentsInChildren<Collider>())
            {
                if (col.enabled)
                {
                    col.enabled = false;
                    disabledColliders.Add(col);
                }
            }
        }

        [ClientRpc]
        void EnablePlayerCollidersClientRpc()
        {
            foreach (var col in disabledColliders)
            {
                if (col != null) col.enabled = true;
            }
            disabledColliders.Clear();
        }

        [ClientRpc]
        public void setHeadTransformLinkRotationClientRpc(float wiggleAmount)
        {
            headTransformLink.transform.localRotation = Quaternion.Euler(rotOffset.x, rotOffset.y + wiggleAmount, rotOffset.z);
        }

        public PlayerControllerB scanForPlayerTarget()
        {
            RoundManager m = RoundManager.Instance;
            PlayerControllerB[] players = m.playersManager.allPlayerScripts;

            float closestDist = 10000f;
            PlayerControllerB closestPly = null;
            for (int i = 0; i < players.Length; i++)
            {
                var ply = players[i];
                var dist = Vector3.Distance(transform.position, ply.gameObject.transform.position);

                if (dist < closestDist && ply.NetworkObject.NetworkObjectId != player.NetworkObject.NetworkObjectId)
                {
                    closestDist = dist;
                    closestPly = ply;
                }
            }

            if (closestDist < maxPlayerBiteRange)
            {
                return closestPly;
            }
            return null;
        }

        public EnemyAI scanForMonsterTarget()
        {
            RoundManager m = RoundManager.Instance;
            EnemyAI[] monsters = UnityEngine.Object.FindObjectsOfType<EnemyAI>();

            float closestDist = 10000f;
            EnemyAI closestMonster = null;
            for (int i = 0; i < monsters.Length; i++)
            {
                var monster = monsters[i];
                var dist = Vector3.Distance(transform.position, monster.gameObject.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestMonster = monster;
                }
            }

            if (closestDist < maxEnemyBiteRange)
            {
                return closestMonster;
            }
            return null;
        }

        [ClientRpc]
        public void PlayDogAnimationClientRpc()
        {
            try { PlayTemporaryAnimation("ClimbLadder", biteAnimation, thrashTime); } catch { }
            try { PlayTemporaryAnimation("CrouchDown", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("CrouchIdle", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Sprint", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Jump", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("JumpLand", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("ShovelHold", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("LimpWalk", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Dance1", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Dance2", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Hurt1", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("WalkSideways", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("WalkTired", biteAnimation, thrashTime); } catch { };
            try { PlayTemporaryAnimation("Idle1", biteAnimation, thrashTime); } catch { };
        }

        public void PlayTemporaryAnimation(string stateName, AnimationClip clip, float duration)
        {
            Animator playerAnimator = player.playerBodyAnimator;
            AnimatorOverrideController overrideController = new AnimatorOverrideController(playerAnimator.runtimeAnimatorController);
            AnimationClip originalClip = overrideController[stateName];

            overrideController[stateName] = clip;
            playerAnimator.runtimeAnimatorController = overrideController;
            playerAnimator.Play(stateName, 0, 0f);

            StartCoroutine(RestoreAnimationCoroutine(stateName, originalClip, overrideController, playerAnimator, duration));
        }

        private IEnumerator RestoreAnimationCoroutine(string stateName, AnimationClip originalClip, AnimatorOverrideController overrideController, Animator playerAnimator, float duration)
        {
            yield return new WaitForSeconds(duration);
            overrideController[stateName] = originalClip;
            playerAnimator.runtimeAnimatorController = overrideController;
            playerAnimator.Play("Idle1");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayBorkServerRpc()
        {
            PlayBorkClientRpc();
        }

        [ClientRpc]
        public void PlayBorkClientRpc()
        {
            dogborks[rnd.Next(0, dogborks.Length)].Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayGrowlServerRpc()
        {
            PlayGrowlClientRpc();
        }

        [ClientRpc]
        public void PlayGrowlClientRpc()
        {
            dogGrowl.Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayMunchServerRpc()
        {
            PlayMunchClientRpc();
        }

        [ClientRpc]
        public void PlayMunchClientRpc()
        {
            dogMunch.Play();
        }

        [ClientRpc]
        public void SetMonsterPositionClientRpc(ulong enemyId, Vector3 position)
        {
            foreach (var enemy in GameObject.FindObjectsOfType<EnemyAI>())
            {
                if (enemy.NetworkObject.NetworkObjectId == enemyId)
                {
                    enemy.transform.position = position;
                    break;
                }
            }
        }
    }
}
