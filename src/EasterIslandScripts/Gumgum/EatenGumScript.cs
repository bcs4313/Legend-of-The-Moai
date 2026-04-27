using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts
{
    public class EatenGumScript : NetworkBehaviour
    {
        public System.Random random;
        public AudioSource eatenAudioSource;
        public PlayerControllerB eater;

        public float jumpBoost = 1;
        public float speedBoost = 1;
        public float healthBoost = 1;
        public float impurity = 1f;
        public int durationMs = 1;

        bool hasPlayedAudio = false;

        public void Start()
        {
            if (RoundManager.Instance.IsHost)
            {
                random = new System.Random();
                if (!GetComponent<NetworkObject>().IsSpawned)
                {
                    GetComponent<NetworkObject>().Spawn();
                }

                applyBuffsClientRpc(eater.NetworkObject.NetworkObjectId, speedBoost, jumpBoost, impurity, healthBoost, durationMs);
                awaitDestructionServerOnly();
                eater.isHoldingObject = false;
            }
        }

        public void Update()
        {
            if (RoundManager.Instance.IsHost)
            {
                this.transform.position = eater.transform.position;

                if (!hasPlayedAudio)
                {
                    playConsumeClientRpc();

                    hasPlayedAudio = true;
                }
            }
        }

        [ClientRpc]
        public void playConsumeClientRpc()
        {
            eatenAudioSource.Play();
        }

        [ClientRpc]
        public void applyBuffsClientRpc(ulong uid, float _speedBoost, float _jumpBoost, float _impurity, float _healthBoost, int _durationMs) {
            applyBuffstoClient(uid, _speedBoost, _jumpBoost, _impurity, _healthBoost, _durationMs);
        }

        public async void applyBuffstoClient(ulong uid, float _speedBoost, float _jumpBoost, float _impurity, float _healthBoost, int _durationMs) {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            var loc_id = RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId;
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player.NetworkObject.NetworkObjectId == uid)
                {
                    Plugin.highPlayers.Add(player);
                    player.DamagePlayer((int)(-_healthBoost * 100));
                    if (player.health > 100)
                    {
                        player.health = 100;
                    }
                    var genericSpeed = 4.6f;
                    var genericJumpHeight = 13f;
                    var genericClimbSpeed = 3;

                    player.drunkness = _impurity;

                    player.movementSpeed = genericSpeed * _speedBoost;
                    player.jumpForce = genericJumpHeight * _jumpBoost;
                    player.climbSpeed = genericClimbSpeed * _speedBoost * _jumpBoost;
                    await Task.Delay(_durationMs);
                    player.movementSpeed = genericSpeed;
                    player.jumpForce = genericJumpHeight;
                    player.climbSpeed = genericClimbSpeed;

                    if (player.health > 100)
                    {
                        player.health = 100;
                    }

                    Plugin.highPlayers.Remove(player);
                }
            }
        }

        public async void awaitDestructionServerOnly()
        {
            if (RoundManager.Instance.IsHost)
            {
                await Task.Delay(durationMs);
                await Task.Delay(5000); // destroy item a bit after the buff ends
                NetworkObject.Despawn();
            }
        }
    }
}
