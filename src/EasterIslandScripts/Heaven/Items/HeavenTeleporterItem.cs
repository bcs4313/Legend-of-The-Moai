using EasterIsland.src.EasterIslandScripts.Technical.Dynamic_Loading;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.RectTransform;
using Random = System.Random;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Items
{
    public class HeavenTeleporterItem : GrabbableObject
    {
        public bool radarEnabled;

        public Animator radarBoosterAnimator;

        public GameObject radarDot;

        public AudioSource pingAudio;

        public AudioClip pingSFX;

        public AudioSource radarBoosterAudio;

        public AudioClip turnOnSFX;

        public AudioClip turnOffSFX;

        public AudioClip flashSFX;

        public string radarBoosterName;

        private bool setRandomBoosterName;

        private int timesPlayingPingAudioInOneSpot;

        private Vector3 pingAudioLastPosition;

        private float timeSincePlayingPingAudio;

        private int radarBoosterNameIndex = -1;

        private float flashCooldown;

        public Transform radarSpherePosition;

        // teleporter variables
        private float chargeDuration = 5.2f;  // 5.2 seconds to charge and teleport the player
        private float currentChargeTime = 0.0f;  // amount of time in charge state, resets when off
        public String moaiShipHeavenDestination;  // name of the heaven dest
        float teleportRange = 30f;
        bool toHeaven = true;
        Vector3 lastLocation = Vector3.zero;
        bool toggle = true;

        public override void Update()
        {
            base.Update();
            if (timeSincePlayingPingAudio > 5f)
            {
                timeSincePlayingPingAudio = 0f;
                timesPlayingPingAudioInOneSpot = Mathf.Max(timesPlayingPingAudioInOneSpot - 1, 0);
            }
            else
            {
                timeSincePlayingPingAudio += Time.deltaTime;
            }
            if (flashCooldown >= 0f)
            {
                flashCooldown -= Time.deltaTime;
            }

            if (radarEnabled)
            {
                currentChargeTime += Time.deltaTime;
                if (currentChargeTime > chargeDuration && toggle)
                {
                    teleportTrigger();
                    currentChargeTime = 0;
                    toggle = false;
                }
            }
            else
            {
                currentChargeTime = 0;
                toggle = true;
            }
        }

        [ClientRpc]
        private void teleportPlayersClientRpc(Vector3 position)
        {
            foreach (PlayerControllerB player in getNearestPlayers(teleportRange))
            {
                player.transform.position = position;
            }
            this.transform.position = position;
            this.targetFloorPosition = position;
        }

        [ServerRpc]
        private void teleportPlayersServerRpc(Vector3 position)
        {
            teleportPlayersClientRpc(position);
        }

        private List<PlayerControllerB> getNearestPlayers(float dist)
        {
            RoundManager m = RoundManager.Instance;
            var players = m.playersManager.allPlayerScripts;
            var nearPlayers = new List<PlayerControllerB>();

            foreach (PlayerControllerB player in players)
            {
                if (Vector3.Distance(player.transform.position, transform.position) <= dist)
                {
                    nearPlayers.Add(player);
                }
            }

            return nearPlayers;
        }

        // SERVER ONLY
        public void teleportTrigger()
        {
            if (toHeaven)
            {
                lastLocation = this.transform.position;
                toHeaven = false;
                var heaven = GameObject.Find(moaiShipHeavenDestination);
                if (heaven)
                {
                    Debug.Log("!ATHEAVEN-> GOTO HEAVEN");
                    Vector3 loc = GameObject.Find(moaiShipHeavenDestination).transform.position;
                    Debug.Log("HEAVENTO->" + loc);

                    // take all nearby players and teleport them
                    teleportPlayersClientRpc(loc);
                }
                else
                {
                    Vector3 loc = Vector3.zero;
                    GameObject[] nodes = null;
                    // teleport completely randomly inside factory
                    nodes = RoundManager.Instance.insideAINodes;

                    if(nodes != null)
                    {
                        try
                        {
                            loc = nodes[UnityEngine.Random.RandomRangeInt(0, nodes.Length)].transform.position;
                        }
                        catch { }
                    }

                    teleportPlayersClientRpc(loc);
                }
            }
            else
            {
                var heaven = GameObject.Find(moaiShipHeavenDestination);
                if (heaven)
                {
                    Vector3 loc = Vector3.zero;
                    GameObject[] nodes = null;
                    // teleport completely randomly, either inside factory or outside
                    double result = new Random().NextDouble();
                    if (result < 0.5)
                    {
                        nodes = RoundManager.Instance.outsideAINodes;
                    }
                    else
                    {
                        nodes = RoundManager.Instance.insideAINodes;
                    }

                    if (nodes != null)
                    {
                        try
                        {
                            loc = nodes[UnityEngine.Random.RandomRangeInt(0, nodes.Length)].transform.position;
                        }
                        catch { }
                    }

                    teleportPlayersClientRpc(loc);
                }
                else
                {
                    toHeaven = true;
                    teleportPlayersClientRpc(lastLocation);
                }
            }
        }

        public override void Start()
        {
            base.Start();
            radarBoosterName = "Gateway to Heaven";
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            if (radarEnabled)
            {
                RemoveBoosterFromRadar();
            }
        }

        public override int GetItemDataToSave()
        {
            base.GetItemDataToSave();
            return radarBoosterNameIndex;
        }

        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            radarBoosterNameIndex = saveData;
        }

        public void FlashAndSync()
        {
            Flash();
            RadarBoosterFlashServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RadarBoosterFlashServerRpc()
        {
            RadarBoosterFlashClientRpc();
        }

        [ClientRpc]
        public void RadarBoosterFlashClientRpc()
        {
            if (!(flashCooldown >= 0f))
            {
                Flash();
            }
        }
        public void Flash()
        {
            if (radarEnabled && !(flashCooldown >= 0f))
            {
                flashCooldown = 2.25f;
                radarBoosterAnimator.SetTrigger("Flash");
                radarBoosterAudio.PlayOneShot(flashSFX);
                WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, flashSFX);
                StunGrenadeItem.StunExplosion(radarSpherePosition.position, affectAudio: false, 0.8f, 1.75f, 2f, isHeldItem: false, null, null, 0.3f);
            }
        }

        public void SetRadarBoosterNameLocal(string newName)
        {
            radarBoosterName = "Gateway to Heaven";
            base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = radarBoosterName;
            StartOfRound.Instance.mapScreen.ChangeNameOfTargetTransform(base.transform, newName);
        }

        private void RemoveBoosterFromRadar()
        {
            StartOfRound.Instance.mapScreen.RemoveTargetFromRadar(base.transform);
        }

        private void AddBoosterToRadar()
        {
            if (!setRandomBoosterName)
            {
                setRandomBoosterName = true;
                int num = (radarBoosterNameIndex = ((radarBoosterNameIndex != -1) ? radarBoosterNameIndex : new System.Random(Mathf.Min(StartOfRound.Instance.randomMapSeed + (int)base.NetworkObjectId, 99999999)).Next(0, StartOfRound.Instance.randomNames.Length)));
                radarBoosterName = "Gateway to Heaven"; ;
                base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = radarBoosterName;
            }
            string text = StartOfRound.Instance.mapScreen.AddTransformAsTargetToRadar(base.transform, radarBoosterName, isNonPlayer: true);
            if (!string.IsNullOrEmpty(text))
            {
                base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = text;
            }
            StartOfRound.Instance.mapScreen.SyncOrderOfRadarBoostersInList();
        }

        public void EnableRadarBooster(bool enable)
        {
            if(RoundManager.Instance.IsHost)
            {
                createHeavenClientRpc();
            }
            else
            {
                createHeavenServerRpc();
            }

            radarBoosterAnimator.SetBool("on", enable);
            radarDot.SetActive(enable);
            if (enable)
            {
                AddBoosterToRadar();
                radarBoosterAudio.Play();
                radarBoosterAudio.PlayOneShot(turnOnSFX);
                WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, turnOnSFX);
            }
            else
            {
                RemoveBoosterFromRadar();
                if (radarBoosterAudio.isPlaying)
                {
                    radarBoosterAudio.Stop();
                    radarBoosterAudio.PlayOneShot(turnOffSFX);
                    WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, turnOffSFX);
                }
            }
            radarEnabled = enable;
        }

        [ServerRpc(RequireOwnership = false)]
        public void createHeavenServerRpc()
        {
            createHeavenClientRpc();
        }

        [ClientRpc]
        public void createHeavenClientRpc() { 
            if(!HeavenLoader.isSceneLoaded)
            {
                HeavenLoader.LoadHeavenWorld();
            }
        }

        public void PlayPingAudio()
        {
            timesPlayingPingAudioInOneSpot += 2;
            timeSincePlayingPingAudio = 0f;
            pingAudio.PlayOneShot(pingSFX);
            WalkieTalkie.TransmitOneShotAudio(pingAudio, pingSFX);
            RoundManager.Instance.PlayAudibleNoise(pingAudio.transform.position, 12f, 0.8f, timesPlayingPingAudioInOneSpot, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed, 1015);
        }

        public void PlayPingAudioAndSync()
        {
            PlayPingAudio();
            PingRadarBoosterServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PingRadarBoosterServerRpc(int playerWhoPlayedPing)
        {
            PingRadarBoosterClientRpc(playerWhoPlayedPing);
        }

        [ClientRpc]
        public void PingRadarBoosterClientRpc(int playerWhoPlayedPing)
        {
            if (!(GameNetworkManager.Instance.localPlayerController == null) && playerWhoPlayedPing != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
            {
                PlayPingAudio();
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            pingAudioLastPosition = base.transform.position;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            EnableRadarBooster(used);
        }

        public override void PocketItem()
        {
            base.PocketItem();
            isBeingUsed = false;
            EnableRadarBooster(enable: false);
        }

        public override void DiscardItem()
        {
            if (Vector3.Distance(base.transform.position, pingAudioLastPosition) > 5f)
            {
                timesPlayingPingAudioInOneSpot = 0;
            }
            base.DiscardItem();
        }
    }
}
