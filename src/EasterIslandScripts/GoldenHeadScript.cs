using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace EasterIsland.src.EasterIslandScripts
{
    internal class GoldenHeadScript : NetworkBehaviour
    {
        GameObject summonedMoai;
        public GrabbableObject item;
        public AudioSource moaiBelch;

        void Update()
        {
            var c = Plugin.controls;
            if (!item.playerHeldBy || !item.playerHeldBy.isPlayerControlled) { return; }

            // Check if the "F" key is pressed
            if (c.K1.triggered)
            {
                playBelch(0.5f);
            }
            if (c.K2.triggered)
            {
                playBelch(0.6667f);
            }
            if (c.K3.triggered)
            {
                playBelch(0.8333f);
            }
            if (c.K4.triggered)
            {
                playBelch(1.0f);
            }
            if (c.K5.triggered)
            {
                playBelch(1.1667f);
            }
            if (c.K6.triggered)
            {
                playBelch(1.3333f);
            }
            if (c.K7.triggered)
            {
                playBelch(1.5f);
            }
            if (c.K8.triggered)
            {
                playBelch(1.6667f);
            }
            if (c.K9.triggered)
            {
                playBelch(1.8333f);
            }
            if (c.K0.triggered)
            {
                playBelch(2f);
            }

            // summon gold moai
            if(c.summonGeorge.triggered)
            {
                if (!summonedMoai || !summonedMoai.activeInHierarchy)
                {
                    summonGeorgeKeyServerRpc(item.playerHeldBy.transform.position);
                }
                else
                {
                    summonGeorgeKeyServerRpc(item.playerHeldBy.transform.position);
                }
            }
        }

        [ServerRpc]
        public void summonGeorgeKeyServerRpc(Vector3 position)
        {
            if (!summonedMoai || !summonedMoai.activeInHierarchy)
            {
                summonGeorge(position);
            }
            else
            {
                teleportGeorge(position);
            }
        }

        public void summonGeorge(Vector3 playerPosition)
        {
            var george = findGeorgeInMods();

            if (george)
            {
                var randomPosition = GenerateRandomPosition(playerPosition, 5f);
                if(randomPosition == Vector3.zero) { return; }
                NetworkObjectReference georgeNet = RoundManager.Instance.SpawnEnemyGameObject(randomPosition, 0, 1, george);
                NetworkObject netObj;
                var tryResult = georgeNet.TryGet(out netObj);

                if (tryResult)
                {
                    summonedMoai = netObj.gameObject;

                    if(item.playerHeldBy.isInsideFactory)
                    {
                        summonedMoai.GetComponent<EnemyAI>().isOutside = false;
                        summonedMoai.GetComponent<EnemyAI>().allAINodes = GameObject.FindGameObjectsWithTag("AINode");
                    }
                    else
                    {
                        summonedMoai.GetComponent<EnemyAI>().isOutside = true;
                        summonedMoai.GetComponent<EnemyAI>().allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                    }
                }
                else
                {
                    Debug.LogError("Gold Moai: Net retrieve failure. Failed to find Gold Moai network reference!");
                }
            }
            else
            {
                Debug.LogError("Gold Moai: Spawn failure. Failed to find Gold Moai as a defined enemy!");
            }
        }

        Vector3 GenerateRandomPosition(Vector3 target, float radius)
        {
            // Generate a random direction
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;

            // Calculate the random position
            Vector3 randomPosition = target + new Vector3(randomDirection.x, 0, randomDirection.y) * radius;

            // try to generate a navmesh position
            NavMeshHit hit;
            var result = NavMesh.SamplePosition(randomPosition, out hit, 10f, NavMesh.AllAreas);
            
            if(result)
            {
                return hit.position;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public void teleportGeorge(Vector3 playerPosition)
        {
            var randomPosition = GenerateRandomPosition(playerPosition, 5f);
            if (randomPosition == Vector3.zero) { return; }

            summonedMoai.transform.position = randomPosition;
        }

        public EnemyType findGeorgeInMods()
        {
            RoundManager m = RoundManager.Instance;
            var enemies = m.currentLevel.DaytimeEnemies;
            // George is found in daytime enemies
            for(int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if(enemy.enemyType.name.Contains("Moai") && enemy.enemyType.name.Contains("Gold"))
                {
                    return enemy.enemyType;
                }
            }

            return null;
        }

        public void playBelch(float pitchValue)
        {
            if(RoundManager.Instance.IsServer)
            {
                playBelchClientRpc(pitchValue);
            }
            else
            {
                playBelchServerRpc(pitchValue);
            }
        }


        // server rebound
        [ServerRpc]
        public void playBelchServerRpc(float pitchValue)
        {
            playBelchClientRpc(pitchValue);
        }

        [ClientRpc]
        public void playBelchClientRpc(float pitchValue)
        {
            moaiBelch.pitch = pitchValue;
            moaiBelch.Play();
        }
    }
}
