using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using LC_API;
using static ExampleEnemy.Plugin;

namespace ExampleEnemy
{

    // You may be wondering, how does the Example Enemy know it is from class ExampleEnemyAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    class RedEnemyAI : EnemyAI {

        // ThunderMoai vars
        float ticksTillThunder = 5; // ticks occur 5 times per second

        // updated once every 15 seconds
        GrabbableObject[] source = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
        int sourcecycle = 75;

        // extra audio sources
        public AudioSource creatureFood;
        public AudioSource creatureEat;
        public AudioSource creatureEatHuman;
        bool eatingScrap = false;
        bool eatingHuman = false;
        int eatingTimer = -1;

        float rawSpawnProbability = 0.166f; // forced probability to spawn, affecting true spawnrate
        float groupSpawnChance = 0.18f;  // probability to form a group
        int rawSpawnGroup = 0; // enables group spawn, ignoring probability


        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
        #pragma warning disable 0649
        public Transform turnCompass;
        public Transform attackArea;
        #pragma warning restore 0649
        float timeSinceHittingLocalPlayer;
        float timeSinceNewRandPos;
        Vector3 positionRandomness;
        Vector3 StalkPos;
        System.Random enemyRandom;
        bool isDeadAnimationDone;
        enum State {
            SearchingForPlayer,
            StickingInFrontOfPlayer,
            HeadSwingAttackInProgress,
        }

        void LogIfDebugBuild(string text) {
            #if DEBUG
            Plugin.Logger.LogInfo(text);
            #endif
        }
        

        public void stopAllSound()
        {
            creatureSFX.Stop();
            creatureVoice.Stop();
            creatureEat.Stop();
            creatureEatHuman.Stop();
            creatureFood.Stop();
        }

        public override void Start()
        {
            // spawnrate control for strictly the daytime moai
            float trueSpawnProbability = rawSpawnProbability / ExampleEnemy.Plugin.moaiGlobalRarity.Value;
            if (!this.gameObject.name.Contains("Blue") && UnityEngine.Random.Range(0.0f, 1.0f) >= trueSpawnProbability && rawSpawnGroup <= 0)
            {
                LogIfDebugBuild("MOAI: spawncontrol -> probability failed at -> " + (trueSpawnProbability * 100) + "%");
                Destroy(gameObject);
                return;
            }
            else
            {
                LogIfDebugBuild("MOAI: spawncontrol -> probability SUCCESS at -> " + (trueSpawnProbability * 100) + "%");
                if (rawSpawnGroup > 0) { rawSpawnGroup--; }
                else if (UnityEngine.Random.Range(0.0f, 1.0f) <= groupSpawnChance)
                {
                    rawSpawnGroup = UnityEngine.Random.RandomRangeInt(1, 4);
                    LogIfDebugBuild("MOAI: Forming spawn cluster of size: " + rawSpawnGroup);
                }
            }
            base.Start();

            // additional audio sources
            if (!this.creatureFood) { this.creatureFood = grabSource("CreatureFood") as AudioSource; }
            if (!this.creatureEat) { this.creatureEat = grabSource("CreatureEat") as AudioSource; }
            if (!this.creatureEatHuman) { this.creatureEatHuman = grabSource("CreatureEatHuman") as AudioSource; }

            // size variant modification
            if (RoundManager.Instance.IsHost && UnityEngine.Random.Range(0.0f, 1.0f) <= Plugin.moaiGlobalSizeVar.Value)
            {
                float newSize = 1;
                if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                { // small
                    newSize = 1 - UnityEngine.Random.Range(0.0f, 0.95f);
                }
                else
                { // large
                    newSize = 1 + UnityEngine.Random.Range(0.0f, 5.0f);
                }

                if (newSize < 1)
                {
                    var p = (double)newSize;
                    LC_API.Networking.Network.Broadcast<moaiSizePkg>("moaisizeset", new moaiSizePkg(this.NetworkObject.NetworkObjectId, newSize, (float)Math.Pow(p, 0.3)));
                }
                else
                {
                    LC_API.Networking.Network.Broadcast<moaiSizePkg>("moaisizeset", new moaiSizePkg(this.NetworkObject.NetworkObjectId, newSize, newSize));
                }
            }

            //LogIfDebugBuild("Moai Enemy Spawned");
            // account for config binds
            // creature sfx is music, while creature voice is idle noises (yes its weird)
            this.creatureVoice.volume = Plugin.moaiGlobalVoiceVol.Value;
            this.creatureSFX.volume = Plugin.moaiGlobalMusicVol.Value / 1.3f;
            this.creatureFood.volume = Plugin.moaiGlobalVoiceVol.Value;
            this.creatureEat.volume = Plugin.moaiGlobalMusicVol.Value;

            // enforce navmeshagent size
            if (RoundManager.Instance.IsHost)
            {
                if (Plugin.moaiGlobalSize.Value < 1)
                {
                    var p = (double)Plugin.moaiGlobalSize.Value;
                    LC_API.Networking.Network.Broadcast<moaiSizePkg>("moaisizeset", new moaiSizePkg(this.NetworkObject.NetworkObjectId, Plugin.moaiGlobalSize.Value, (float)Math.Pow(p, 0.3)));
                }
                else
                {
                    LC_API.Networking.Network.Broadcast<moaiSizePkg>("moaisizeset", new moaiSizePkg(this.NetworkObject.NetworkObjectId, Plugin.moaiGlobalSize.Value, Plugin.moaiGlobalSize.Value));
                }
            }


            timeSinceHittingLocalPlayer = 0;
            //creatureAnimator.SetTrigger("startWalk");
            timeSinceNewRandPos = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;

            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
            currentBehaviourStateIndex = (int)State.SearchingForPlayer;
            // We make the enemy start searching. This will make it start wandering around.
            StartSearch(transform.position);
        }

        public override void Update()
        {
            base.Update();
            if (isEnemyDead)
            {
                // For some weird reason I can't get an RPC to get called from HitEnemy() (works from other methods), so we do this workaround. We just want the enemy to stop playing the song.
                if (!isDeadAnimationDone)
                {
                    //LogIfDebugBuild("Stopping enemy voice with janky code.");
                    isDeadAnimationDone = true;
                    creatureVoice.Stop();
                    creatureVoice.PlayOneShot(dieSFX);
                }
                return;
            }
            timeSinceHittingLocalPlayer += Time.deltaTime;
            timeSinceNewRandPos += Time.deltaTime;
            if (targetPlayer != null && PlayerIsTargetable(targetPlayer))
            {
                //Debug.Log("looking at player");
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                //turnCompass.rotation *= Quaternion.Euler(0, -90, 0);  // forces moai to not be sideways since the import is sideways
            }
            if (stunNormalizedTimer > 0f)
            {
                agent.speed = 0f;
            }
            //turnCompass.GetRoot().rotation *= Quaternion.Euler(0, -90f, 0); // forces moai to not be sideways since the import is sideways

            switch (currentBehaviourStateIndex)
            {
                case (int)State.SearchingForPlayer:
                    thunderReset();
                    break;

                case (int)State.StickingInFrontOfPlayer:
                    thunderTick();
                    break;
            };
        }

        public override void DoAIInterval()
        {
            //Debug.Log("AI Interval");
            base.DoAIInterval();
            if (isEnemyDead) {
                return;
            };

            // source update cycle
            if (sourcecycle > 0)
            {
                sourcecycle--;
            }
            else
            {
                //Debug.Log("MOAI: Refreshing Source -N- ");
                this.source = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                sourcecycle = 75;
            }

            switch(currentBehaviourStateIndex) {
                case (int)State.SearchingForPlayer:
                    agent.speed = 3f * Plugin.moaiGlobalSpeed.Value;

                    // sound switch
                    if (!creatureVoice.isPlaying)
                    {
                        //Debug.Log("MSOUND: creatureVoice");
                        LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureVoice"));
                    }

                    // object search and state switch;
                    if (getObj() || getPlayerCorpse()) { SwitchToBehaviourClientRpc((int)State.HeadSwingAttackInProgress); }

                    if (FoundClosestPlayerInRange(28f)){
                        //LogIfDebugBuild("Start Target Player");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.StickingInFrontOfPlayer);
                    }
                    break;

                case (int)State.StickingInFrontOfPlayer:
                    agent.speed = 5.3f * Plugin.moaiGlobalSpeed.Value;

                    // sound switch 
                    if (!creatureSFX.isPlaying)
                    {
                        //Debug.Log("MSOUND: creatureSFX");
                        LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureSFX"));
                    }
                    thunderTick();

                    // object search and state switch;
                    if(getObj() || getPlayerCorpse()) { SwitchToBehaviourClientRpc((int)State.HeadSwingAttackInProgress);}

                    // Keep targetting closest player, unless they are over 20 units away and we can't see them.
                    if (!TargetClosestPlayerInAnyCase() && !FoundClosestPlayerInRange(28f)) {
                        //LogIfDebugBuild("Stop Target Player");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForPlayer);
                        return;
                    }
                    StickingInFrontOfPlayer();
                    break;

                case (int)State.HeadSwingAttackInProgress:
                    // sound switch
                    if (!eatingHuman && !eatingScrap)
                    {
                        if (!creatureFood.isPlaying)
                        {
                            //Debug.Log("MSOUND: creatureFood");
                            LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureFood"));
                        }
                    }
                    else
                    {
                        if (!creatureEat.isPlaying && eatingScrap)
                        {
                            //Debug.Log("MSOUND: creatureEat");
                            LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureEat"));
                        }
                        if(!creatureEatHuman.isPlaying && eatingHuman)
                        {
                            //Debug.Log("MSOUND: creatureEatHuman");
                            LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureEatHuman"));
                        }
                        if (eatingTimer > 0)
                        {
                            eatingTimer--;
                        }
                        else if(eatingTimer == 0)
                        {
                            GrabbableObject devouredObj = getObj();
                            if(devouredObj)
                            {
                                devouredObj.OnNetworkDespawn();
                                UnityEngine.GameObject.Destroy(devouredObj.NetworkObject);
                                UnityEngine.GameObject.Destroy(devouredObj.propBody);
                                UnityEngine.GameObject.Destroy(devouredObj.gameObject);
                                UnityEngine.GameObject.Destroy(devouredObj);
                            }

                            PlayerControllerB ply2 = getPlayerCorpse();
                            if(ply2)
                            {
                                ply2.deadBody.DeactivateBody(false);
                            }
                        }
                    }

                    // consumption
                    GrabbableObject obj = getObj();
                    PlayerControllerB ply = getPlayerCorpse();

                    if (obj == null && ply == null)
                    {
                        //Debug.Log("MOAI: Lost Object. Ending obj search.");
                        eatingHuman = false;
                        eatingScrap = false;
                        eatingTimer = -1;
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForPlayer);
                    }
                    else
                    {
     
                        if (ply)
                        {
                            //Debug.Log("MOAI: Heading to found Player");
                            this.targetPlayer = null;
                            this.targetNode = ply.deadBody.transform;
                            this.SetDestinationToPosition(ply.deadBody.transform.position);
                            if (Vector3.Distance(this.transform.position, ply.deadBody.transform.position) < (ply.deadBody.transform.localScale.magnitude + this.transform.localScale.magnitude))
                            {
                                if (!eatingHuman)
                                {
                                    Debug.Log("MOAI: Attaching Body to Mouth");
                                    eatingTimer = 150;
                                    LC_API.Networking.Network.Broadcast<moaiAttachBodyPkg>("moaiattachbody", new moaiAttachBodyPkg(this.NetworkObject.NetworkObjectId, ply.NetworkObject.NetworkObjectId));
                                    LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureEatHuman"));
                                }
                                eatingHuman = true;
                            }
                            else
                            {
                                eatingHuman = false;
                            }
                        }
                        else if (obj)
                        {
                            //Debug.Log("MOAI: Heading to found Scrap");
                            this.targetPlayer = null;
                            this.targetNode = obj.transform;
                            this.SetDestinationToPosition(obj.transform.position);
                            if (Vector3.Distance(this.transform.position, obj.transform.position) < (obj.transform.localScale.magnitude + this.transform.localScale.magnitude))
                            {
                                if(obj.IsLocalPlayer)
                                {
                                    if (!eatingHuman)
                                    {
                                        Debug.Log("MOAI: Attaching Body to Mouth");
                                        eatingTimer = 150;
                                        LC_API.Networking.Network.Broadcast<moaiAttachBodyPkg>("moaiattachbody", new moaiAttachBodyPkg(this.NetworkObject.NetworkObjectId, ply.NetworkObject.NetworkObjectId));
                                        LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureEatHuman"));
                                    }
                                    eatingHuman = true;
                                }
                                else if (!eatingScrap)
                                {
                                    eatingTimer = ((int)(obj.scrapValue / 1.8)+15);
                                    LC_API.Networking.Network.Broadcast<moaiSoundPkg>("moaisoundplay", new moaiSoundPkg(this.NetworkObject.NetworkObjectId, "creatureEat"));
                                }
                                eatingScrap = true;
                            }
                            else
                            {
                                eatingScrap = false;
                            }
                        }
                    }
                    if (!eatingHuman && !eatingScrap)
                    {
                        eatingTimer = -1;
                    }
                    break;
                    
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
        }
        public PlayerControllerB getPlayerCorpse()
        {
            //Debug.Log("MOAI: Human Food Search");
            // look for human food first
            for (int i = 0; i < RoundManager.Instance.playersManager.allPlayerScripts.Length; i++)
            {
                PlayerControllerB player = RoundManager.Instance.playersManager.allPlayerScripts[i];

                if (player != null && player.name != null && player.transform != null)
                {

                    var d = Vector3.Distance(transform.position, player.transform.position);
                    if(player.deadBody != null && player.deadBody.isActiveAndEnabled)
                    {
                        d = Vector3.Distance(transform.position, player.deadBody.transform.position);
                    }

                    //Debug.Log("MOAI: Human -> " + player.name + " dist - " + d + " dead? " + player.isPlayerDead);
                    if (d < 20.0f && player.deadBody != null && player.deadBody.isActiveAndEnabled)
                    {
                        //Debug.Log("found player to eat");
                        return player;
                    }
                }
            }
            //Debug.Log("Can't eat anyone...");
            return null;
        }

        // return null if there are no valid objects to eat.
        // otherwise return a object
        public GrabbableObject getObj()
        {
            try
            {
                for (int i = 0; i < source.Length; i++)
                {
                    GrabbableObject obj = source[i];
                    //LogIfDebugBuild(obj.name);

                    if (Vector3.Distance(this.transform.position, obj.transform.position) < 20.0f && !obj.heldByPlayerOnServer)
                    {
                        //Debug.Log("MOAI: Returning object -> " + obj.name);
                        return obj;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                //Debug.Log("MOAI: Refreshing Source -L- ");
                source = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            }
            catch (NullReferenceException)
            {
                return null;
            }
            return null;   // no food :(
        }

        public void thunderReset()
        {
            RoundManager m = RoundManager.Instance;

            if (!this.gameObject.name.Contains("Blue"))
            {
                return;
            }

            if (targetPlayer == null || ticksTillThunder > 0)
            {
                return;
            }

            //LogIfDebugBuild("MOAI: spawning LBolt");
            ticksTillThunder = Math.Min((float)Math.Pow(Vector3.Distance(transform.position, targetPlayer.transform.position), 1.75), 180);
            Vector3 position = this.serverPosition;
            position.y += (float)(this.enemyRandom.NextDouble() * ticksTillThunder * 0.2) - ticksTillThunder * 0.1f;
            position.x += (float)(this.enemyRandom.NextDouble() * ticksTillThunder * 0.2) - ticksTillThunder * 0.1f;

            GameObject weather = UnityEngine.GameObject.Find("TimeAndWeather");

            // find "Stormy" in weather
            GameObject striker = null;
            for (int i = 0; i < weather.transform.GetChildCount(); i++)
            {
                GameObject g = weather.transform.GetChild(i).gameObject;
                if (g.name.Equals("Stormy"))
                {
                    //Debug.Log("Lethal Chaos: Found Stormy!");
                    striker = g;
                }
            }
            if (striker != null)
            {
                // change to include warning
                striker.SetActive(true);
                m.LightningStrikeServerRpc(position);
                //m.ShowStaticElectricityWarningClientRpc
            }
            else
            {
                Debug.LogError("Lethal Chaos: Failed to find Stormy Weather container (LBolt)!");
            }
        }
        public void thunderTick()
        {
            ticksTillThunder -= 1;
            if (ticksTillThunder <= 0)
            {
                thunderReset();
            }
        }

        bool FoundClosestPlayerInRange(float range) {

            //  maybe better if  1.5f?
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if(targetPlayer == null) return false;
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }
        
        bool TargetClosestPlayerInAnyCase() {
            mostOptimalDistance = 23f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if(targetPlayer == null) return false;
            return true;
        }

        void StickingInFrontOfPlayer(){
            // We only run this method for the host because I'm paranoid about randomness not syncing I guess
            // This is fine because the game does sync the position of the enemy.
            // Also the attack is a ClientRpc so it should always sync
            if (targetPlayer == null || !IsOwner) {
                return;
            }

            // Charge into player
            StalkPos = targetPlayer.transform.position;
            SetDestinationToPosition(StalkPos, checkForPath: false);
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (timeSinceHittingLocalPlayer < 0.5f) {
                return;
            }
            PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                //LogIfDebugBuild("Example Enemy Collision with Player!");
                timeSinceHittingLocalPlayer = 0f;
                if (playerControllerB.health < 30)
                {
                    playerControllerB.KillPlayer(playerControllerB.velocityLastFrame, true, CauseOfDeath.Mauling, 0);
                }
                else
                {
                    playerControllerB.DamagePlayer(30);
                }
            }
        }

        public AudioSource grabSource(String argname)
        {
            var sources = this.GetComponentsInChildren<UnityEngine.AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource s = sources[i];
                if (s.name.Equals(argname))
                {
                    return s;
                }
            }
            return null;
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            //LogIfDebugBuild($"Animation: {animationName}");
            //creatureAnimator.SetTrigger(animationName);
        }

    }
}