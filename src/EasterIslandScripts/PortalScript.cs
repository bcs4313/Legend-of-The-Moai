using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.SceneManagement;

namespace EasterIsland.src.EasterIslandScripts
{
    internal class PortalScript : NetworkBehaviour
    {
        public GameObject destination;
        public static List<PortalScript> instances = new List<PortalScript>();

        // sound sources
        public AudioSource stage1;
        public AudioSource stage2;
        public AudioSource stage3;
        public AudioSource teleportSound;
        public AudioSource destinationTeleportSound;
        bool initialized = false;

        // internal logic
        private float charge;  // 100+ charge initiates teleport
        private int cycle = 0;
        private bool charging = false;

        float timeStarted = 0;

        public void initialize(Vector3 spawnPos)
        {
            setupTeleporterClientRpc(spawnPos);
        }

        /*
        public void Start()
        {
            if(RoundManager.Instance.IsHost)
            {
                MoveObjectToScene();
            }
        }

        public void MoveObjectToScene()
        {
            // Ensure the target scene is loaded
            Scene targetScene = SceneManager.GetSceneByName("EasterIsland");
            if (!targetScene.isLoaded)
            {
                Debug.LogError($"Scene" + targetScene + "is not loaded. Load the scene before moving the object.");
                return;
            }

            // Move the GameObject to the target scene
            SceneManager.MoveGameObjectToScene(this.gameObject, targetScene);
        }
        */

        [ClientRpc]
        public void setupTeleporterClientRpc(Vector3 spawnPos)
        {
            var rootObj = gameObject.GetComponent<PortalScript>();
            var destinationObj = rootObj.destination.GetComponent<PortalScript>();

            rootObj.destination = destinationObj.gameObject;
            rootObj.destinationTeleportSound = destinationObj.GetComponent<PortalScript>().teleportSound;

            destinationObj.destination = rootObj.gameObject;
            destinationObj.destinationTeleportSound = rootObj.GetComponent<PortalScript>().teleportSound;

            instances.Add(destinationObj.GetComponent<PortalScript>());
            instances.Add(rootObj.GetComponent<PortalScript>());

            transform.position = spawnPos;
            initialized = true;
        }

        void Start()
        {
            timeStarted = Time.time;
        }

        void Update()
        {
            if (!initialized) { return; }
            if (charging) { charge += 25f * Time.deltaTime; } // 4 seconds to charge up completely

            // delete self if not on moon and older than 6 seconds (to deal with lag and elevatorUp change delays)
            if (!RoundManager.Instance.dungeonGenerator && Time.time - timeStarted > 10f)
            {
                if (GameObject.Find("PortalPair(Clone)"))
                {
                    Destroy(GameObject.Find("PortalPair(Clone)"));
                }
                else
                {
                    Destroy(this);
                }
            }

            soundLogic(charge);


            if (charge > 100)
            {
                if (RoundManager.Instance.IsServer)
                {
                    teleportPlayersClientRpc(destination.transform.position);
                }
                else
                {
                    teleportPlayersServerRpc(destination.transform.position);
                }
                playSoundClientRpc(3);
                charge = 0;
            }

            if (cycle < 20)
            {
                cycle++;
                return;
            }
            else
            {
                cycle = 0;
            }

            charging = getNearestPlayers().Count > 0;

        }

        private void soundLogic(float c)
        {
            if (c < 25 && !stage1.isPlaying)
            {
                playSoundClientRpc(0);
            }
            else if (c < 50 && !stage2.isPlaying)
            {
                playSoundClientRpc(1);
            }
            else if (c < 75 && !stage3.isPlaying)
            {
                playSoundClientRpc(2);
            }
        }

        [ClientRpc]
        private void playSoundClientRpc(int id)
        {
            switch (id)
            {
                case 0:
                    stage1.Play();
                    stage2.Stop();
                    stage3.Stop();
                    break;
                case 1:
                    stage2.Play();
                    stage1.Stop();
                    stage3.Stop();
                    break;
                case 2:
                    stage3.Play();
                    stage1.Stop();
                    stage2.Stop();
                    break;
                case 3:
                    teleportSound.Play();
                    destinationTeleportSound.Play();
                    stage1.Stop();
                    stage2.Stop();
                    stage3.Stop();
                    break;
            }
        }

        [ClientRpc]
        private void teleportPlayersClientRpc(Vector3 position)
        {
            foreach (PlayerControllerB player in getNearestPlayers())
            {
                player.transform.position = position;
            }
        }

        [ServerRpc]
        private void teleportPlayersServerRpc(Vector3 position)
        {
            teleportPlayersClientRpc(position);
        }

        private List<PlayerControllerB> getNearestPlayers()
        {
            RoundManager m = RoundManager.Instance;
            var players = m.playersManager.allPlayerScripts;
            var nearPlayers = new List<PlayerControllerB>();

            foreach (PlayerControllerB player in players)
            {
                if (Vector3.Distance(player.transform.position, transform.position) <= 2)
                {
                    nearPlayers.Add(player);
                }
            }

            return nearPlayers;
        }
    }
}
