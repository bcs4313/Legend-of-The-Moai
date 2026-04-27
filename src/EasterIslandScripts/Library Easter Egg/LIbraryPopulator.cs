
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace EasterIsland.src.EasterIslandScripts.Library_Easter_egg
{
    // responsible for:
    // adding a cave navmesh,
    // spawning some moai inside
    // spawning some loot1
    // spawning some traps
    public class LibraryPopulator : NetworkBehaviour
    {
        protected GameObject[] libraryAINodes;
        protected GameObject[] librarySpawnNodes;

        public void getAllNodes()
        {
            // find all library nodes
            LibraryAINode[] aObjs = FindObjectsOfType<LibraryAINode>();
            libraryAINodes = new GameObject[aObjs.Length];
            for (int i = 0; i < libraryAINodes.Length; i++)
            {
                libraryAINodes[i] = aObjs[i].gameObject;
            }

            // find all library nodes
            LibrarySpawnNode[] bObjs = FindObjectsOfType<LibrarySpawnNode>();
            librarySpawnNodes = new GameObject[bObjs.Length];
            for (int i = 0; i < librarySpawnNodes.Length; i++)
            {
                librarySpawnNodes[i] = bObjs[i].gameObject;
            }
        }

        // cave gen root is itself
        public void PopulateEnvironment(int amount)
        {
            Plugin.Logger.LogMessage("EI: Populating Library Environment...");
            getAllNodes();

            // transport 2-4 moais into the cave world.
            // server only
            int spawns = amount;
            if (RoundManager.Instance.IsHost)
            {
                spawnMoai(spawns);
            }
        }

        public async void spawnMoai(int amount)
        {
            var enemyList = RoundManager.Instance.currentLevel.DaytimeEnemies;
            List<SpawnableEnemyWithRarity> possibleSpawns = new List<SpawnableEnemyWithRarity>();
            foreach (SpawnableEnemyWithRarity spawnable in enemyList)
            {
                Plugin.Logger.LogInfo(spawnable.enemyType.enemyName);
                if (spawnable.enemyType.enemyName.ToLower().Contains("moai") && !spawnable.enemyType.enemyName.ToLower().Contains("gold"))
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

                await Task.Delay(500);
                transportMoai(ai);
            }
        }

        public void transportMoai(EnemyAI moai)
        {
            // get all ai nodes
            moai.allAINodes = libraryAINodes;
            moai.isOutside = true;

            Vector3 sourcePos = librarySpawnNodes[UnityEngine.Random.RandomRangeInt(0, librarySpawnNodes.Length)].transform.position;
            NavMeshHit hit;

            bool result = NavMesh.SamplePosition(sourcePos, out hit, 5f, NavMesh.AllAreas);

            if (result)
            {
                Plugin.Logger.LogInfo("Easter Island Library: Transporting Moai to: " + moai.transform.position);
                moai.serverPosition = hit.position;
                moai.transform.position = hit.position;
                moai.isOutside = true;
                moai.agent.Warp(moai.serverPosition);
                moai.SyncPositionToClients();
                moai.SetDestinationToPosition(libraryAINodes[UnityEngine.Random.RandomRangeInt(0, libraryAINodes.Length)].transform.position);
                resetSearch(moai);

            }
            else
            {
                Plugin.Logger.LogWarning("Easter Island Cave Generator: Moai transport Failed to find Navmesh!");
            }
        }

        public async void resetSearch(EnemyAI moai)
        {
            if (moai.currentSearch != null)
            {
                moai.StopSearch(moai.currentSearch);
                await Task.Delay(1000);
                moai.StartSearch(moai.transform.position);
            }
        }
    }
}
