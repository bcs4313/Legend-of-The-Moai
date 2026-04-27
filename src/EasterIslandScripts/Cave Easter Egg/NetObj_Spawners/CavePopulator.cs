
using EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg;
using EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg.NetObj_Spawners;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace EasterIsland.src.EasterIslandScripts
{
    // responsible for:
    // adding a cave navmesh,
    // spawning some moai inside
    // spawning some loot1
    // spawning some traps
    public class CavePopulator : NetworkBehaviour
    {
        protected NavMeshSurface bakedNavmesh;
        protected GameObject[] caveAINodes;
        protected GameObject[] caveSpawnNodes;
        protected GameObject[] caveTrapNodes;
        public CaveEnvironment env;

        public void getAllNodes()
        {
            // find all cave nodes
            CaveAINode[] aObjs = FindObjectsOfType<CaveAINode>();
            caveAINodes = new GameObject[aObjs.Length];
            for (int i = 0; i < caveAINodes.Length; i++)
            {
                caveAINodes[i] = aObjs[i].gameObject;
            }

            // find all spawn nodes
            CaveSpawnNode[] sObjs = FindObjectsOfType<CaveSpawnNode>();
            caveSpawnNodes = new GameObject[sObjs.Length];
            for (int i = 0; i < caveSpawnNodes.Length; i++)
            {
                caveSpawnNodes[i] = sObjs[i].gameObject;
            }


            // find all trap nodes
            CaveTrapNode[] tObjs = FindObjectsOfType<CaveTrapNode>();
            caveTrapNodes = new GameObject[tObjs.Length];
            for (int i = 0; i < caveTrapNodes.Length; i++)
            {
                caveTrapNodes[i] = tObjs[i].gameObject;
            }
        }

        // cave gen root is itself
        public void PopulateEnvironment()
        {
            Plugin.Logger.LogMessage("Populating Cave Environment...");
            getAllNodes();
            // bake navmesh into all children, creating a master navmesh
            // called by all clients
            NavMeshSurface surface = GetComponent<NavMeshSurface>();

            BoxCollider basebounds = env.gameObject.GetComponent<BoxCollider>();
            surface.size = basebounds.size;
            surface.center = basebounds.center;
            if (surface != null)
            {
                surface.BuildNavMesh();
                bakedNavmesh = surface;
            }
            
            // transport 2-4 moais into the cave world.
            // server only
            int spawns = UnityEngine.Random.RandomRangeInt(2, 5);
            int mines = UnityEngine.Random.RandomRangeInt(10, 20);

            /*
            if (RoundManager.Instance.IsHost)
            {
                EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();

                foreach(EnemyAI enemy in enemies)
                {
                    if (enemy.enemyType.enemyName.ToLower().Contains("moai") && spawns > 0)
                    {
                        spawns--;
                        transportMoai(enemy);
                    }
                }
            }
            */
            if (RoundManager.Instance.IsHost)
            {
                spawnMoai(spawns);
                spawnMine(mines);
            }
        }

        public async void spawnMine(int amount)
        {
            GameObject mine = null;
            var trapList = RoundManager.Instance.currentLevel.spawnableMapObjects;
            foreach (SpawnableMapObject spawnable in trapList)
            {
                Plugin.Logger.LogInfo(spawnable.prefabToSpawn.name);
                if (spawnable.prefabToSpawn.name.ToLower().Contains("land") && spawnable.prefabToSpawn.name.ToLower().Contains("mine"))
                {
                    mine = spawnable.prefabToSpawn;
                }
            }

            if(mine != null)
            {
                List<int> nodesUsed = new List<int>();
                for(int i = 0; i < amount; i++)
                {
                    int val = UnityEngine.Random.RandomRangeInt(0, caveTrapNodes.Length);
                    if (!nodesUsed.Contains(val))
                    {
                        Vector3 sourcePos = caveTrapNodes[val].transform.position;
                        NavMeshHit hit;

                        bool result = NavMesh.SamplePosition(sourcePos, out hit, 5f, NavMesh.AllAreas);

                        if (result)
                        {
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(mine, hit.position, UnityEngine.Quaternion.Euler(UnityEngine.Vector3.zero));
                            gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                            nodesUsed.Add(val);
                        }
                        else
                        {
                            Plugin.Logger.LogWarning("Easter Island Cave Generator: Trap failed to find node to spawn in!");
                        }
                    }
                }
            }
        }

        public async void spawnMoai(int amount)
        {
            var enemyList = RoundManager.Instance.currentLevel.DaytimeEnemies;
            List<SpawnableEnemyWithRarity> possibleSpawns = new List<SpawnableEnemyWithRarity>();
            foreach (SpawnableEnemyWithRarity spawnable in enemyList)
            {
                Plugin.Logger.LogInfo(spawnable.enemyType.enemyName);
                if (spawnable.enemyType.enemyName.ToLower().Contains("moai"))
                {
                    possibleSpawns.Add(spawnable);
                }
            }


            for (int i = 0; i < amount; i++)
            {
                int randomSelect = Random.RandomRangeInt(0, possibleSpawns.Count);
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(possibleSpawns[randomSelect].enemyType.enemyPrefab, new Vector3(0f, 0f, 0f), UnityEngine.Quaternion.Euler(UnityEngine.Vector3.zero));
                gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                EnemyAI ai = gameObject.GetComponent<EnemyAI>();
                RoundManager.Instance.SpawnedEnemies.Add(ai);

                await Task.Delay(2000);
                transportMoai(ai);
            }
        }

        public void transportMoai(EnemyAI moai)
        {
            // get all ai nodes
            moai.allAINodes = caveAINodes;
            moai.isOutside = false;

            Vector3 sourcePos = caveSpawnNodes[UnityEngine.Random.RandomRangeInt(0, caveSpawnNodes.Length)].transform.position;
            NavMeshHit hit;

            bool result = NavMesh.SamplePosition(sourcePos, out hit, 5f, NavMesh.AllAreas);

            if(result)
            {
                Plugin.Logger.LogInfo("Easter Island Cave Generator: Transporting Moai to: " + moai.transform.position);
                moai.serverPosition = hit.position;
                moai.transform.position = hit.position;
                moai.isOutside = false;
                moai.agent.Warp(moai.serverPosition);
                moai.SyncPositionToClients();
            }
            else
            {
                Plugin.Logger.LogWarning("Easter Island Cave Generator: Moai transport Failed to find Navmesh!");
            }
        }
    }
}
