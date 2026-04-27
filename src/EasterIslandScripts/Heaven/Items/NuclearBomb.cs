using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;
using UnityEngine.AI;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Items
{
    public class NuclearBomb : GrabbableObject, IHittable
    {
        // danger level:
        // 0 = default basic noise
        // 1 = nuke is armed
        // 2 = right before explosion (might be skipped if exposure is severe)
        // 3 = BOOOOOOOOOOOOOOOOOOM
        public int dangerLevel = 0;

        public AudioSource sound0;
        public AudioSource sound1;
        public AudioSource sound2;
        public AudioSource BOOMDelayed;
        public AudioSource BOOMImmediate;

        public ParticleSystem nuclearParticles;
        public GameObject nukeModel;
        public GameObject scanNode;

        bool exploded = false;
        bool armedSoundRinged = false;

        float dropCooldown = 2f;
        float lastDropTime = 0f;
        bool playedNukeSound = false;

        public override void Start()
        {
            base.Start();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!(GameNetworkManager.Instance.localPlayerController == null))
            {
                if (UnityEngine.Random.Range(0f, 1f) < 0.25)
                {
                    dangerLevel += 1;
                }
                if (RoundManager.Instance.IsHost) { dangerResultActivateClientRpc(dangerLevel); }
                else
                {
                    dangerResultActivateServerRpc();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void dangerResultActivateServerRpc()
        {
            dangerResultActivateClientRpc(dangerLevel);
        }

        // handle the nuke carefully (lol)
        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            dangerLevel += force;
            dangerResultActivateClientRpc(dangerLevel);
            return true;
        }

        [ClientRpc]
        public void dangerResultActivateClientRpc(int _dangerLevel_)
        {

            switch (_dangerLevel_)
            {
                case 0:
                    // normal beep
                    playSoundClientRpc("sound0", true);
                    break;
                case 1:
                    // arm warning
                    if (!armedSoundRinged)
                    {
                        playSoundClientRpc("sound0", false);
                        playSoundClientRpc("sound1", true);
                        armedSoundRinged = true;
                    }
                    else
                    {
                        playSoundClientRpc("sound0", false);
                        playSoundClientRpc("sound1", false);
                        playSoundClientRpc("sound2", true);
                    }
                    break;
                case 2:
                    // big warning signal
                    playSoundClientRpc("sound0", false);
                    playSoundClientRpc("sound1", false);
                    playSoundClientRpc("sound2", true);
                    break;
                case 3:
                    // delayed explosion
                    playSoundClientRpc("sound0", false);
                    playSoundClientRpc("sound1", false);
                    playSoundClientRpc("sound2", false);
                    playSoundClientRpc("BOOMDelayed", true);
                    BOOMAwait();
                    break;
                default:
                    // immediately explode
                    playSoundClientRpc("sound0", false);
                    playSoundClientRpc("sound1", false);
                    playSoundClientRpc("sound2", false);
                    playSoundClientRpc("BOOMDelayed", false);
                    playSoundClientRpc("BOOMImmediate", true);
                    BOOMClientRpc();
                    break;
            }
        }

        [ClientRpc]
        public void playSoundClientRpc(String soundName, bool play)
        {
            switch(soundName)
            {
                case "sound0":
                    if (play) { sound0.Play(); }
                    else { sound0.Stop(); }
                    break;
                case "sound1":
                    if (play) { sound1.Play(); }
                    else { sound0.Stop(); }
                    break;
                case "sound2":
                    if (play) { sound2.Play(); }
                    else { sound0.Stop(); }
                    break;
                case "BOOMDelayed":
                    if (play) { BOOMDelayed.Play(); }
                    else { BOOMDelayed.Stop(); }
                    break;
                case "BOOMImmediate":
                    if (playedNukeSound) { return; }
                    if (play) { BOOMImmediate.Play(); }
                    else { BOOMImmediate.Stop(); }
                    playedNukeSound = true;
                    break;
            }
        }

        public async void BOOMAwait()
        {
            await Task.Delay(5600);
            BOOMClientRpc();
        }

        public async void boomasync() {
            if (exploded) { return; }

            exploded = true;
            nuclearParticles.Play();
            GameObject.Destroy(nukeModel);
            GameObject.Destroy(scanNode);
            this.grabbable = false;
            this.grabbableToEnemies = false;

            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();

            for (int i = 0; i < 24; i++)
            {
                Landmine.SpawnExplosion(this.transform.position, false, 10 * i, 11 * i, 20, 100 - i * 2);

                await Task.Delay(200);
                float radius = 10 * i;
                Vector3 origin = this.transform.position;
                Vector3 randomPos = origin + UnityEngine.Random.insideUnitSphere * radius;
                randomPos.y = origin.y;

                for (int j = 0; j < 5 + (i / 4); j++)
                {
                    Vector3 microOffset = UnityEngine.Random.insideUnitSphere * 2f;
                    NavMeshHit microHit;
                    Vector3 microPos = randomPos + microOffset;
                    microPos.y = origin.y;

                    if (NavMesh.SamplePosition(microPos, out microHit, 3f, NavMesh.AllAreas))
                    {
                        Landmine.SpawnExplosion(microHit.position, true);
                    }
                    else
                    {
                        Landmine.SpawnExplosion(microPos, true);
                    }

                    foreach(var player in players)
                    {
                        if(Vector3.Distance(transform.position, player.gameObject.transform.position) < i * 5)
                        {
                            player.DamagePlayer((int)(100 / Vector3.Distance(transform.position, player.gameObject.transform.position)));
                        }
                    }

                    foreach (var enemy in enemies)
                    {
                        if (Vector3.Distance(transform.position, enemy.gameObject.transform.position) < i * 10)
                        {
                            if (RoundManager.Instance.IsHost)
                            {
                                enemy.HitEnemyClientRpc(3, -1, true);
                            }
                        }
                    }
                }
            }
            await Task.Delay(8000);
            GameObject.Destroy(gameObject);
        }

        public override void OnHitGround()
        {
            base.OnHitGround();  // keeps any base drop sound or flags

            if(dropCooldown + lastDropTime > Time.time) { return; }
            if(!RoundManager.Instance.IsHost) { return; }
            // Calculate the drop distance (in world space)
            float dropHeight = Vector3.Distance(this.startFallingPosition, this.targetFloorPosition);

            // Debug
            Debug.Log($"Nuke dropped from height: {dropHeight}");

            float riskFactor = dropHeight / 3f;
            float chance = riskFactor * 0.25f;

            if (UnityEngine.Random.value < chance)
            {
                dangerLevel++;
                Debug.Log($"Nuke danger increased to {dangerLevel} due to impact!");
                dangerResultActivateClientRpc(dangerLevel);
            }

            if (dropHeight > 9f)
            {
                dangerLevel = 3;  // delayed boom
                Debug.Log($"Nuke danger increased to {dangerLevel} due to impact!");
                dangerResultActivateClientRpc(dangerLevel);
            }

            if (dropHeight > 12f)
            {
                dangerLevel = 4;  // immediate boom
                Debug.Log($"Nuke danger increased to {dangerLevel} due to impact!");
                dangerResultActivateClientRpc(dangerLevel);
            }

            lastDropTime = Time.time;
        }

        [ClientRpc]
        public void BOOMClientRpc()
        {
            boomasync();
        }
    }

}
