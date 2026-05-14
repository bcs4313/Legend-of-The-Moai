using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.MapExpansion
{
    // A moai that grabs the player's attention and talks to them.
    // Server-authoritative: server picks the closest in-range player, rotates the
    // moai toward them, and drives the dialogue. Rotation is synced via a
    // NetworkTransform component on the prefab. Voice clips play on every client
    // so everyone hears the moai positionally; only the target player sees the tip.
    [RequireComponent(typeof(SphereCollider))]
    public class MoaiStatueTalker : NetworkBehaviour
    {
        // parallel arrays of lines and their delays
        public string[] lines;
        public float[] lineDelays;

        // random voice clip per line. nulls and empty arrays are tolerated.
        public AudioSource[] voiceSounds;

        public string speakerName = "Moai";

        public float minTriggerRadius = 3f;
        public float maxTriggerRadius = 15f;

        public float rotationSpeed = 90f;
        public bool speakOncePerPlayer = false;

        private SphereCollider triggerCollider;
        private float activeRadius;

        // server-only
        private PlayerControllerB targetPlayer;
        private bool isSpeaking = false;
        private bool hasSpoken = false;

        // global lock so only one moai is speaking at a time across the map
        private static MoaiStatueTalker currentSpeaker;

        private void Awake()
        {
            triggerCollider = GetComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            activeRadius = Random.Range(minTriggerRadius, maxTriggerRadius);
            triggerCollider.radius = activeRadius;
        }

        private void Update()
        {
            if (!IsServer) return;

            // try to start a new conversation if nothing is speaking
            if (!isSpeaking && currentSpeaker == null)
            {
                if (speakOncePerPlayer && hasSpoken) return;

                PlayerControllerB closest = FindClosestPlayerInRange();
                if (closest != null)
                {
                    targetPlayer = closest;
                    StartCoroutine(SpeakRoutine());
                }
            }

            // rotate toward the locked target while speaking. NetworkTransform on
            // the prefab will replicate this rotation to every client.
            if (isSpeaking && targetPlayer != null)
            {
                Vector3 toPlayer = targetPlayer.transform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        private PlayerControllerB FindClosestPlayerInRange()
        {
            PlayerControllerB closest = null;
            float bestDistSqr = activeRadius * activeRadius;

            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerControllerB p = players[i];
                if (p == null || !p.isPlayerControlled || p.isPlayerDead) continue;

                float distSqr = (p.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    closest = p;
                }
            }
            return closest;
        }

        private bool IsPlayerStillInRange(PlayerControllerB p)
        {
            if (p == null || !p.isPlayerControlled || p.isPlayerDead) return false;
            float distSqr = (p.transform.position - transform.position).sqrMagnitude;
            return distSqr <= activeRadius * activeRadius;
        }

        private IEnumerator SpeakRoutine()
        {
            currentSpeaker = this;
            isSpeaking = true;

            ulong targetClientId = targetPlayer.actualClientId;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!IsPlayerStillInRange(targetPlayer)) break;

                // pick a random voice clip index. -1 means "no clip this line."
                int soundIndex = -1;
                if (voiceSounds != null && voiceSounds.Length > 0)
                {
                    soundIndex = Random.Range(0, voiceSounds.Length);
                }

                SpeakLineClientRpc(lines[i], targetClientId, soundIndex);

                float delay = i < lineDelays.Length ? lineDelays[i] : 3f;
                yield return new WaitForSeconds(delay);
            }

            isSpeaking = false;
            hasSpoken = true;
            targetPlayer = null;
            if (currentSpeaker == this) currentSpeaker = null;

            OnDialogueComplete();
        }

        [ClientRpc]
        private void SpeakLineClientRpc(string line, ulong targetClientId, int soundIndex)
        {
            // HUD tip only for the closest player
            PlayerControllerB local = GameNetworkManager.Instance?.localPlayerController;
            if (local != null && local.actualClientId == targetClientId)
            {
                HUDManager.Instance.DisplayTip(speakerName, line);
            }

            // voice clip plays on every client so everyone hears it positionally
            if (soundIndex >= 0 && voiceSounds != null && soundIndex < voiceSounds.Length)
            {
                AudioSource src = voiceSounds[soundIndex];
                if (src != null) src.Play();
            }
        }

        // hook for objective-bearing moai, wire this up later
        protected virtual void OnDialogueComplete() { }

        public override void OnNetworkDespawn()
        {
            if (currentSpeaker == this) currentSpeaker = null;
            base.OnNetworkDespawn();
        }
    }
}