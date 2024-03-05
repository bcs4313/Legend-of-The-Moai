using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExampleEnemy {

    // You may be wondering, how does the Example Enemy know it is from class ExampleEnemyAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    class ExampleEnemyAI : EnemyAI {

        // ThunderMoai vars
        float ticksTillThunder = 5; // ticks occur 5 times per second


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

        public override void Start()
        {
            base.Start();
            //LogIfDebugBuild("Moai Enemy Spawned");
            // account for config binds
            // creature sfx is music, while creature voice is idle noises (yes its weird)
            this.creatureVoice.volume = Plugin.moaiGlobalVoiceVol.Value;
            this.creatureSFX.volume = Plugin.moaiGlobalMusicVol.Value;
            this.gameObject.transform.localScale *= Plugin.moaiGlobalSize.Value;


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
            
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };

            switch(currentBehaviourStateIndex) {
                case (int)State.SearchingForPlayer:
                    agent.speed = 3f * Plugin.moaiGlobalSpeed.Value;

                    // sound switch
                    if (!creatureVoice.isPlaying)
                    {
                        creatureVoice.Play();
                        creatureSFX.Stop();
                    }

                    if (FoundClosestPlayerInRange(32f)){
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
                        creatureSFX.Play();
                        creatureVoice.Stop();
                    }
                    thunderTick();
                    // Keep targetting closest player, unless they are over 20 units away and we can't see them.
                    if (!TargetClosestPlayerInAnyCase() && !FoundClosestPlayerInRange(25f)) {
                        //LogIfDebugBuild("Stop Target Player");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForPlayer);
                        return;
                    }
                    StickingInFrontOfPlayer();
                    break;

                case (int)State.HeadSwingAttackInProgress:
                    // We don't care about doing anything here
                    break;
                    
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
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

            LogIfDebugBuild("MOAI: spawning LBolt");
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
                m.LightningStrikeClientRpc(position);
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
            mostOptimalDistance = 1600f;
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
                playerControllerB.DamagePlayer(30);
            }
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            //LogIfDebugBuild($"Animation: {animationName}");
            //creatureAnimator.SetTrigger(animationName);
        }

    }
}