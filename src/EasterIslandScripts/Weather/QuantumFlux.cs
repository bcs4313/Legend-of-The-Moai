using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.PlayerLoop;
using Vector3 = UnityEngine.Vector3;

namespace EasterIsland.src.EasterIslandScripts.Weather
{
    public class QuantumFlux : NetworkBehaviour
    {
        public NavMeshAgent agent;
        Vector3 targetPosition;
        private System.Random r = new System.Random();
        private float lifetime = 10;  // lifetime in seconds
        private float chargeTime = 3;  // charge time in seconds
        private bool activated = false;
        public Collider collider;

        public GameObject chargingObject;
        public GameObject fluxObject;
        public static EnemyAI[] ais;

        public AudioSource quantumExplosion;
        private float timeTick = -1;

        public struct teleportLog
        {
            public ulong uid;
            public float time;
        }

        public static List<teleportLog> teleports = null;
        public static List<GameObject> fluxes = null;
        public GameObject teleportFX;
        public AudioClip teleportSound;

        public void Start()
        {
            findNewPosition();
            chargingObject.SetActive(true);
            fluxObject.SetActive(false);

            if (teleports == null)
            {
                teleports = new List<teleportLog>();
                fluxes = new List<GameObject>();
            }

            // shouldn't be too expensive
            ais = FindObjectsOfType<EnemyAI>();

            transform.SetParent(RoundManager.Instance.mapPropsContainer.transform);
            fluxes.Add(gameObject);
        }

        public void Update()
        {
            if (Time.time - timeTick > 0.2f)
            {
                timeTick = Time.time;

                // teleport any ai within a certain distance (5.5f)
                foreach (EnemyAI ai in ais)
                {
                    if (Vector3.Distance(transform.position, ai.transform.position) <= 7.5f)
                    {
                        Debug.Log("Teleporting Enemy: " + ai.name);
                        teleportEnemy(ai);
                    }
                }
            }

            if (activated)
            {
                // active phase
                if (Vector3.Distance(gameObject.transform.position, agent.destination) < 8 || agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    findNewPosition();
                    agent.SetDestination(targetPosition);
                }

                lifetime -= Time.deltaTime;

                if (lifetime <= 0)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // charging phase
                chargingObject.transform.localPosition = new Vector3(0, chargeTime / 3, 0);

                if (chargeTime <= 0)
                {
                    activated = true;
                    chargingObject.SetActive(false);
                    fluxObject.SetActive(true);
                    quantumExplosion.Play();
                }
                chargeTime -= Time.deltaTime;
            }
        }

        // collision logic with a rigidbody
        void OnCollisionEnter(Collision collision)
        {
            if (!RoundManager.Instance.IsHost)
            {
                return;
            }

            var hit = collision.gameObject;
            if (collision.gameObject)
            {
                // sound effect here
                //spawnExplosionClientRpc();

                // player has Cube and PlayerPhysicsBox collisions, both must be accounted for.
                var player = playerParentWalk(hit);
                if (player)
                {
                    updateLogs();
                    teleportPlayer(player);
                }
            }
            else if (collision.collider)
            {
                // do nothing
            }
        }

        public void OnDestroy()
        {
            fluxes.Remove(gameObject);
        }

        public void updateLogs()
        {
            teleports.RemoveAll(log => Time.time - log.time >= 2);
        }

        public bool notRecentlyTeleported(ulong uid)
        {
            foreach (teleportLog log in teleports)
            {
                // 2 second grace
                if (log.uid == uid && Time.time - log.time < 2)
                {
                    return false;
                }
            }
            return true;
        }

        public void teleportPlayer(PlayerControllerB player)
        {
            var pos = getTelePosition();
            if (pos == Vector3.zero) { return; }

            if (notRecentlyTeleported(player.NetworkObjectId))
            {
                var l = new teleportLog();
                l.uid = player.NetworkObject.NetworkObjectId;
                l.time = Time.time;
                teleports.Add(l);
                // teleport 
                confirmTeleportPlayerClientRpc(player.NetworkObjectId, getTelePosition());
            }
        }

        public void teleportEnemy(EnemyAI enemy)
        {
            var pos = getTelePosition();
            if (pos == Vector3.zero) { return; }

            if (notRecentlyTeleported(enemy.NetworkObjectId))
            {
                var l = new teleportLog();
                l.uid = enemy.NetworkObject.NetworkObjectId;
                l.time = Time.time;
                teleports.Add(l);
                // teleport 
                confirmTeleportEnemyClientRpc(enemy.NetworkObject.NetworkObjectId, getTelePosition());
            }
            else
            {
                Debug.Log("Denying Teleport. Already teleported recently.");
            }
        }

        public Vector3 getTelePosition()
        {
            GameObject target = null;
            for (int i = 0; i < 5; i++)
            {
                GameObject flux = fluxes[r.Next(0, fluxes.Count)];
                if (flux != gameObject)
                {
                    target = flux;
                    break;
                }
            }

            if (target == null)
            {
                Debug.Log("Get Teleposition failure");
                return Vector3.zero;
            }

            NavMeshHit hit;
            var result = NavMesh.SamplePosition(target.transform.position, out hit, 6f, NavMesh.AllAreas);

            if (result)
            {
                Debug.Log("Get Teleposition fully successful.");
                return hit.position;
            }
            else
            {
                Debug.Log("Get Teleposition partially successful.");
                return target.transform.position;
            }
        }

        [ClientRpc]
        public void confirmTeleportPlayerClientRpc(ulong uid, Vector3 position)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;

            foreach (PlayerControllerB p in players)
            {
                if (p.NetworkObject.NetworkObjectId == uid)
                {
                    PlaySoundAtPosition(teleportSound, p.transform.position, 1);
                    particleEffectSpawn(p.transform.position);
                    PlaySoundAtPosition(teleportSound, position, 1);
                    particleEffectSpawn(position);

                    // teleport
                    p.transform.position = position;
                }
            }
        }

        public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1.0f, float minDistance = 1.0f, float maxDistance = 35f)
        {
            // Create a temporary GameObject
            GameObject tempAudio = new GameObject("TempAudio");
            tempAudio.transform.position = position;

            // Add AudioSource
            AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1.0f; // Enable 3D sound

            // Set 3D sound distance properties
            audioSource.minDistance = minDistance; // Full volume within this distance
            audioSource.maxDistance = maxDistance; // Volume reaches zero at this distance
            audioSource.rolloffMode = AudioRolloffMode.Linear; // Use Linear or Logarithmic rolloff

            // Play the sound
            audioSource.Play();

            // Destroy the GameObject after the clip finishes
            Destroy(tempAudio, clip.length);
        }

        public async void particleEffectSpawn(Vector3 position)
        {
            var obj = Instantiate(teleportFX, position, teleportFX.transform.rotation);
            obj.transform.SetParent(RoundManager.Instance.mapPropsContainer.transform);

            await Task.Delay(1000 * 60);

            Destroy(obj);
        }

        [ClientRpc]
        public void confirmTeleportEnemyClientRpc(ulong uid, Vector3 position)
        {
            var enemies = RoundManager.Instance.SpawnedEnemies;

            foreach (EnemyAI a in enemies)
            {
                if (a.NetworkObject.NetworkObjectId == uid)
                {
                    PlaySoundAtPosition(teleportSound, a.serverPosition, 1);
                    particleEffectSpawn(a.serverPosition);
                    PlaySoundAtPosition(teleportSound, position, 1);
                    particleEffectSpawn(position);

                    // teleport
                    a.serverPosition = position;
                    a.transform.position = position;
                    a.agent.Warp(a.serverPosition);
                    a.SyncPositionToClients();
                    Debug.Log("Teleport Successful (ConfirmTeleportEnemyClientRPC)");
                    return;
                }
            }
            Debug.Log("Teleport Unsuccessful (ConfirmTeleportEnemyClientRPC)");
        }

        // goes up the parent tree until it finds player or null
        public PlayerControllerB playerParentWalk(GameObject leaf)
        {
            while (leaf != null && leaf.GetComponent<PlayerControllerB>() == null)
            {
                if (leaf.transform.parent && leaf.transform.parent.gameObject)
                {
                    leaf = leaf.transform.parent.gameObject;
                }
                else
                {
                    leaf = null;
                }
            }

            if (leaf && leaf.GetComponent<PlayerControllerB>())
            {
                return leaf.GetComponent<PlayerControllerB>();
            }

            return null;
        }


        // goes up the enemy tree until it finds player or null
        public EnemyAI enemyParentWalk(GameObject leaf)
        {
            while (leaf != null && leaf.GetComponent<EnemyAI>() == null)
            {
                if (leaf.transform.parent && leaf.transform.parent.gameObject)
                {
                    leaf = leaf.transform.parent.gameObject;
                }
                else
                {
                    leaf = null;
                }
            }

            if (leaf && leaf.GetComponent<EnemyAI>())
            {
                return leaf.GetComponent<EnemyAI>();
            }

            return null;
        }

        public void findNewPosition()
        {
            GameObject[] nodes = RoundManager.Instance.outsideAINodes;
            Vector3 node = nodes[r.Next(0, nodes.Length)].transform.position;
            float xVar = (float)(r.NextDouble() * 10 - 5);
            float yVar = (float)(r.NextDouble() * 10 - 5);
            float zVar = (float)(r.NextDouble() * 10 - 5);

            Vector3 genPos = new Vector3(node.x + xVar, node.y + yVar, node.z + zVar);

            NavMeshHit hit;
            bool sample = NavMesh.SamplePosition(genPos, out hit, 30f, NavMesh.AllAreas);

            if (sample)
            {
                targetPosition = hit.position;
            }
            else
            {
                targetPosition = node;
            }
        }
    }
}
