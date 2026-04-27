using EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg.NetObj_Spawners;
using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using Random = UnityEngine.Random;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg
{
    // fight setup will have...
    // 3 phases with monster selection
    // phase 3 will introduce an invincible radmech (not part of spawn calculations)
    // each phase will have a health bar, dealing any form of damage to the wall hole will chip it.

    // each phase will call monster spawning twice, once at the start of 
    // the phase and another when hp is at half
    // Spawn weights increase by +2 per half
    // 2, 4, 6, 8, 10, 12

    public class CompanyFightScript : NetworkBehaviour
    {
        // each 30 hp lost is a phase
        private int hp = 90;
        private int hpToSpawn = 75;

        // after phase 3, company go boom
        public Animator wallAnimator;
        public bool phase1Done = false;  // shatter 1
        public bool phase2Done = false;  // shatter 2
        public bool phase3Done = false;  // company explodes

        public GameObject companyHitBox;

        public static bool hostile = false;

        float spawnMimicTimer = 0f;
        float spawnMimicDelay = 70f;
        float checkAllDeadTimer = 0f;
        float checkAllDeadInterval = 0.5f;

        float spawnMonstersTimer = 0f;
        float spawnMonstersInterval = 90f;

        // assume the public vars are unity assigned
        public NavMeshSurface navmesh;
        public GameObject[] AINodes;
        public GameObject bossUI;
        public HealthBar healthBar;
        public GameObject healthBarGO;

        // tentacles
        public TentacleScript tentacleLeft;
        public TentacleScript tentacleMiddle;
        public TentacleScript tentacleRight;

        // turrets 
        public BombTurret rightTurret;
        public BombTurret leftTurret;

        // sounds
        public AudioSource awakenAudio;
        public AudioSource phase1Audio;
        public AudioSource phase2Audio;
        public AudioSource phase3Audio;
        public AudioSource deathAudio;

        //private GameObject leverActiveRef = null;
        //private Vector3 oldLeverPos = Vector3.zero;

        // for now, we will just have an attack every 20 seconds (random tentacle)
        private float tentacleInterval = 20f;  // delay between tentacle attacks
        private float tentacleTimer = 0f;

        // enemy types the script can spawn
        // uses string pair matching to the name, in lowercase
        // integers are the weights of each spawn (how severe they are)
        protected (int, String[]) angryLootbug = (3, ["hoard", "bug"]); // hoard bug needs to be activated so its angry
        protected (int, String[]) angryButler = (3, ["butler", ""]);  // butler needs to be activated so its angry
        protected (int, String[]) mouthDog = (3, ["mouth", "dog"]);
        protected (int, String[]) thumper = (3, ["crawler", ""]);  
        protected (int, String[]) giant = (5, ["giant", "forest"]);
        protected (int, String[]) baboonHawk = (2, ["bab", "hawk"]);
        public GameObject spawnIndicator;  // pre spawn indicator

        // after defeat...
        // we could definitely lock the ship lever to avoid many bugs and issues with it,
        // simply resetting the save to before the fight
        public GameObject endingSequenceUI;
        public MoaiEndingSequence endingSequence;

        public void Start()
        {
            Debug.Log("Company Boss: Fight Script Start");
            hostile = false;
            if (RoundManager.Instance.IsHost)
            {
                this.NetworkObject.Spawn();
                Plugin.destroyOnLoad.Add(this.gameObject); // destroy self on exit
            }
            healthBarGO.SetActive(false);  // we should only see this when the bossfight begins
            endingSequenceUI.SetActive(false);
        }

        // for when the boss finally dies...
        // play death animation and other stuff...
        // include end credit
        [ClientRpc]
        public void endFightClientRpc()
        {
            endFightAsync();
        }

        public async void endFightAsync()
        {
            Debug.Log("Company Boss: Ending Script Start");
            endingSequenceUI.SetActive(true);
            wallAnimator.Play("Shatter_3");
            phase3Done = true;
            deathAudio.Play();
            GameObject.Destroy(healthBarGO);
            endingSequenceUI.SetActive(true);
            hostile = false;
            leftTurret.hostile = false;
            rightTurret.hostile = false;
            // remove all enemies
            if (RoundManager.Instance.IsHost)
            {
                var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                foreach (var enemy in enemies)
                {
                    if (enemy.gameObject)
                    {
                        GameObject.Destroy(enemy.gameObject);
                    }
                }
            }

            // 15 secds before triggering ending
            await Task.Delay(15000);
            endingSequence.TriggerMoaiEnding();
        }

        // starts when the first hit from the quantum cannon is dealt
        public async void beginFight()
        {
            if (RoundManager.Instance.IsHost)
            {
                Debug.Log("Company Boss: begin fight host");
                fightStartEffectsClientRpc(hp);
                await Task.Delay(1000);
                initHealthBarClientRpc(hp);
                await Task.Delay(1000);
                bossStartEffectClientRpc();
            }
            else
            {
                Debug.Log("Company Boss: begin fight serverrpc call");
                beginFightServerRpc();
            }
        }

        [ClientRpc]
        public void fightStartEffectsClientRpc(int hpInput)
        {
            Debug.Log("Company Boss: fightstarteffectsclientrpc");
            healthBarGO.SetActive(true);

            // disable ability to leave 0v0
            /*
            var lever = GameObject.Find("StartGameLever");
            if(lever)
            {
                oldLeverPos = lever.transform.localPosition;
                lever.transform.localPosition += new Vector3(0, -30f, 0);
            }
            leverActiveRef = lever;
            */ // terrible idea, wont allow a boss fight restart. Just dumb
        }

        [ServerRpc(RequireOwnership = false)]
        public void beginFightServerRpc()
        {
            Debug.Log("Company Boss: beginfightserverrpc");
            beginFight();
        }

        [ClientRpc]
        public void bossStartEffectClientRpc()
        {
            Debug.Log("Company Boss: bossstarteffectclientrpc");
            bossStart();
        }

        public async void bossStart()
        {
            Debug.Log("Company Boss: bossstart");
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);

            phase1Audio.Play();
            await Task.Delay(3000);
            Debug.Log("LegendOfTheMoai: Company Boss Fight Begin");

            rightTurret.hostile = true;
            leftTurret.hostile = true;

            awakenAudio.Play();
            tentacleLeft.animator.Play("Rise");
            tentacleMiddle.animator.Play("Rise");
            tentacleRight.animator.Play("Rise");

            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            await Task.Delay(1000);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            await Task.Delay(1000);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);

            // Destroy Speaker
            if (GameObject.Find("SpeakerBox"))
            {
                GameObject.Destroy(GameObject.Find("SpeakerBox"));
            }
        }

        [ClientRpc]
        public void initHealthBarClientRpc(int hpInput)
        {
            Debug.Log("Company Boss: initHealthBarClientRpc");
            healthBar.healthbarColor = new Color(healthBar.healthbarColor.r, healthBar.healthbarColor.g, healthBar.healthbarColor.b, 1);
            healthBarGO.SetActive(true);
            healthBar.SetHealth(hpInput);
            hostile = true;
        }

        public void tentacleAttack()
        {
            Debug.Log("Company Boss: tentacleattack");
            int randTent = Random.Range(0, 3);

            tentacleAttackClientRpc(randTent);
        }

        [ClientRpc]
        public void tentacleAttackClientRpc(int randTent)
        {
            Debug.Log("Company Boss: tentacleattackclientrpc");
            switch (randTent)
            {
                case 0:
                    tentacleLeft.attackGround();
                    break;
                case 1:
                    tentacleRight.attackGround();
                    break;
                case 2:
                    tentacleMiddle.attackGround();
                    break;
            }
        }

        public void Update()
        {
            if(!RoundManager.Instance.IsHost) { return; }
            if(!hostile) { return; }

            spawnMimicTimer += Time.deltaTime;
            if(spawnMimicTimer > spawnMimicDelay)
            {
                spawnMimicTimer = 0;
                spawnMimics();
            }

            checkAllDeadTimer += Time.deltaTime;
            if (checkAllDeadTimer > checkAllDeadInterval)
            {
                checkAllDead();
            }

            tentacleTimer += Time.deltaTime;
            if(tentacleTimer > tentacleInterval)
            {
                tentacleTimer = 0;
                tentacleAttack();
            }

            spawnMonstersTimer += Time.deltaTime;
            if (spawnMonstersTimer > spawnMonstersInterval)
            {
                spawnMonstersTimer = 0;
                SpawnMonstersForPhase(Math.Min(90, hp * 2));  // nerfed version of enemy burst
            }
        }

        public void checkAllDead()
        {
            /*
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            foreach(var player in players)
            {
                if (player.isPlayerControlled && !player.isPlayerDead) { return; }
            }

            // everyone is dead, renable lever and recall
            try
            {
                if (leverActiveRef != null)
                {
                    leverActiveRef.SetActive(true);
                    leverActiveRef.transform.localPosition = oldLeverPos;
                }
            }
            catch (Exception e ) { }
            */
        }

        [ClientRpc]
        public void updateCurrentHealthClientRpc(int hpEntry)
        {
            Debug.Log("Company Boss: updatecurrenthealthclientrpc");
            healthBar.SetHealth(hpEntry);
        }

        [ClientRpc]
        public void updateMaxHealthClientRpc(int hpEntry)
        {
            Debug.Log("Company Boss: updatemaxhealthclientrpc");
            healthBar.SetMaxhealth(hpEntry);
        }

        [ClientRpc]
        public void phase1DoneClientRpc()
        {
            Debug.Log("Company Boss: phase1doneclientrpc");
            wallAnimator.Play("Shatter_1");
            phase1Done = true;
            phase2Audio.Play();
        }


        [ClientRpc]
        public void phase2DoneClientRpc()
        {
            Debug.Log("Company Boss: phase2doneclientrpc");
            wallAnimator.Play("Shatter_2");
            phase2Done = true;
            phase3Audio.Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void takeDamageServerRpc(int force)
        {
            takeDamage(force);
        }

        public async void takeDamage(int force)
        {
            Debug.Log("Company Boss: takeDamage");

            if (RoundManager.Instance.IsHost)
            {
                // phase detection and spawning
                if (hp <= hpToSpawn)  // every 15 hp lost == enemies spawning
                {
                    SpawnMonstersForPhase(hp);
                    hpToSpawn -= 15;
                }

                hp -= force;
                int tempHp = hp;

                updateCurrentHealthClientRpc(hp);


                // set animation if done with phase
                if (hp < 60 && !phase1Done)
                {
                    phase1DoneClientRpc();
                }


                if (hp < 30 && !phase2Done)
                {
                    phase2DoneClientRpc();
                }

                if (hp <= 0 && !phase3Done)
                {
                    endFightClientRpc();
                }

                await Task.Delay(800);
                updateMaxHealthClientRpc(tempHp);
            }
        }

        // mimics force players to not camp ship
        // in addition, we will make sure the door can't close!
        // pure evil haha
        public void spawnMimics()
        {
            int amount = ((90 - hp) / 30) + 1;
            GameObject prefab = getEnemyPrefab("masked", "");

            for (int i = 0; i < amount; i++)
            {
                asyncSpawn(prefab);
            }
        }

        // monsters are spawned at ai nodes
        // these nodes will show a particle animation
        // on the ground, and then spawn the monster
        public void SpawnMonstersForPhase(int hp)
        {
            if(!RoundManager.Instance.IsHost) { return; }

            // Maybe your phase weight increments like 10 -> 20 -> 30, etc.
            int phaseWeight = 3 + (int)(3 * (90-hp)/15);
            var monstersToSpawn = PickMonstersToSpawn(phaseWeight);

            //Debug.Log("LegendOfTheMoai BOSS: spawn weight is: " + phaseWeight);

            // Then you can loop over them and spawn actual enemies.
            foreach (var (keys, amount) in monstersToSpawn)
            {
                GameObject prefab = getEnemyPrefab(keys[0], keys[1]);
                if (prefab != null)
                {
                    //Debug.Log($"[LegendOfTheMoai BOSS: HP {hp}] Spawning {amount} x ({keys[0]}, {keys[1]})");
                    for (int i = 0; i < amount; i++)
                    {
                        asyncSpawn(prefab);
                    }
                }
            }
        }

        // handle the spawning and the particle effect
        public async void asyncSpawn(GameObject prefab)
        {
            Debug.Log("Company Boss: ASYNCSPAWN");
            var position = randomAINode().transform.position;
            var rotation = randomAINode().transform.rotation;

            // particle effect first
            spawnIndicatorClientRpc(position);

            await Task.Delay(5000);

            // Actually spawn your monster; e.g. Instantiate(prefab, randomSpawnPos, Quaternion.identity);
            var GO = UnityEngine.GameObject.Instantiate(prefab, position, rotation);
            GO.GetComponentInChildren<NetworkObject>().Spawn(true);
            var AI = GO.GetComponent<EnemyAI>();

            //Debug.Log("Setting AI NODES");
            Debug.Log(AINodes);
            Debug.Log(AINodes.Length);
            AI.allAINodes = AINodes;
            RoundManager.Instance.SpawnedEnemies.Add(AI);
            AI.isOutside = true;
            placeOnNavmesh(AI);
            modifyAI(AI);
        }

        // create a particle effect for 5 seconds before spawning
        [ClientRpc]
        public void spawnIndicatorClientRpc(Vector3 position)
        {
            Debug.Log("Company Boss: spawnindicatorclientrpc");
            spawnIndicatorAsync(position);
        }

        public async void spawnIndicatorAsync(Vector3 position)
        {
            var GO = UnityEngine.GameObject.Instantiate(spawnIndicator, position, Quaternion.identity);
            await Task.Delay(5000);
            Destroy(GO);
        }

        public PlayerControllerB getRandomPlayer()
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            List<PlayerControllerB> playerList = new List<PlayerControllerB>();
            foreach(var player in players)
            {
                if (player.isPlayerControlled)
                {
                    playerList.Add(player);
                }
            }

            return playerList[Random.RandomRangeInt(0, playerList.Count)];
        }

        public async void modifyAI(EnemyAI MAI)
        {
            if (MAI is ButlerEnemyAI)
            {
                PlayerControllerB target = getRandomPlayer();
                for (int i = 0; i < 4; i++)
                {
                    Debug.Log("Company Boss: Brainwashing Butler");
                    var ai = MAI as ButlerEnemyAI;
                    ai.PingAttention(5, 0.6f, target.transform.position, false);
                    await Task.Delay(1000);
                }
            }

            if (MAI is HoarderBugAI)
            {
                PlayerControllerB target = getRandomPlayer();
                for (int i = 0; i < 4; i++)
                {
                    Debug.Log("Company Boss: Brainwashing HoarderBug");
                    var ai = MAI as HoarderBugAI;
                    ai.angryAtPlayer = target;
                    ai.angryTimer += 18f;
                    await Task.Delay(1000);
                }
            }
        }

        public async void placeOnNavmesh(EnemyAI moai)
        {
            for(int i = 0; i < 20; i++)
            {
                if(moai && moai.agent && moai.serverPosition != null)
                {
                    break;
                }
                await Task.Delay(250);
            }
            // get all ai nodes
            moai.isOutside = false;

            Vector3 sourcePos = moai.gameObject.transform.position;
            NavMeshHit hit;

            bool result = NavMesh.SamplePosition(sourcePos, out hit, 5f, NavMesh.AllAreas);

            if (result)
            {
                Plugin.Logger.LogInfo("EI CompanyFightScript: Transporting Monster to: " + moai.transform.position);
                moai.serverPosition = hit.position;
                moai.transform.position = hit.position;
                moai.isOutside = true;
                moai.agent.Warp(moai.serverPosition);
                moai.SyncPositionToClients();
            }
            else
            {
                Plugin.Logger.LogWarning("EI CompanyFightScript: Mosnter transport Failed to find Navmesh!");
            }
        }

        public GameObject randomAINode()
        {
            return AINodes[Random.Range(0, AINodes.Length)];
        }

        // args must be lowercase
        // supply match2 with "" for only 1 match
        public GameObject getEnemyPrefab(String match1, String match2)
        {
            var enemies = Resources.FindObjectsOfTypeAll<EnemyType>();

            foreach (EnemyType enemy in enemies)
            {
                var name = enemy.enemyName.ToLower();

                if (name.Contains(match1) && (match2.Equals("") || name.Contains(match2)))
                {
                    if (enemy.enemyPrefab != null && !name.Contains("bee"))
                    {
                        return enemy.enemyPrefab;
                    }
                }
            }

            print("retrieval failure");
            return null;
        }

        /// Picks up to 3 monster types based on a totalWeight budget.
        /// Each time a monster is chosen, its weight is subtracted from totalWeight.
        /// We stop if we run out of budget or we have already chosen 3 monster types.
        /// <param name="totalWeight">The total weight budget (e.g. increases each phase).</param>
        /// <returns>
        /// List of (string[], int), where string[] is the monster key array and int is how many to spawn.
        /// </returns>
        public List<(string[] monsterKeys, int amount)> PickMonstersToSpawn(int totalWeight)
        {
            var monsterPool = new List<(int weight, string[] keys)>
            {
                angryLootbug,
                angryButler,
                mouthDog,
                thumper,
                giant,
                baboonHawk,
            };

            // We can pick up to 3 different types each call
            int groupCount = Random.Range(1, 4);
            var pickedMonsters = new List<(string[] monsterKeys, int amount)>();

            for (int i = 0; i < groupCount; i++)
            {
                // 1) Filter out monsters that still fit in the *remaining* totalWeight 
                //    even for at least 1 spawn.
                var feasibleMonsters = monsterPool.Where(m => m.weight <= totalWeight).ToList();
                if (feasibleMonsters.Count == 0)
                {
                    // No monster can fit the remaining budget.
                    break;
                }

                // 2) Weighted pick from only those feasible monsters.
                int feasibleWeightSum = feasibleMonsters.Sum(m => m.weight);
                var (chosenWeight, chosenKeys) = WeightedMonsterPick(feasibleMonsters, feasibleWeightSum);

                // 3) Decide how many of that monster we can afford. 
                //    The max we can afford is totalWeight / chosenWeight.
                //    Then pick a random number from 1 up to that max.
                int maxSpawnCount = totalWeight / chosenWeight;
                if (maxSpawnCount < 1)
                {
                    // Technically we shouldn't get here if feasibleMonsters is correct,
                    // but just in case:
                    break;
                }

                int amount = Random.Range(1, maxSpawnCount + 1);

                // 4) Deduct the actual cost = chosenWeight * amount
                totalWeight -= (chosenWeight * amount);

                // 5) Add the result
                pickedMonsters.Add((chosenKeys, amount));

                // If no more weight remains, we can stop spawning.
                if (totalWeight <= 0) break;
            }

            return pickedMonsters;
        }

        /// <summary>
        /// Weighted random selection from a list of (weight, keys).
        /// Rolls a random number between 1 and feasibleWeightSum, then picks accordingly.
        /// </summary>
        private (int weight, string[] keys) WeightedMonsterPick(
            List<(int weight, string[] keys)> monsters,
            int totalWeightSum)
        {
            int roll = Random.Range(1, totalWeightSum + 1);
            int cumulative = 0;
            foreach (var monster in monsters)
            {
                cumulative += monster.weight;
                if (roll <= cumulative)
                {
                    return monster;
                }
            }

            // Fallback (very unlikely due to rounding)
            return monsters[monsters.Count - 1];
        }
    }
}