using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Threading.Tasks;

namespace EasterIsland.src.EasterIslandScripts.Environmental
{
    // teleports one way
    internal class JudgementScript : NetworkBehaviour
    {
        // unity refs
        public Transform platformOriginPoint; // where the platform the player stands on is
        public Transform destination;  // dynamic, depends on the cave start node. Defaults to not be in the cave (if cavegen fails)
        public bool hasGeneratedCave = false;
        public GameObject goodEyes;
        public GameObject badEyes;
        public GameObject whiteEyes;
        public Animator judgeAnimator;

        public NetworkObject netObjSelf;

        // determines whether employees are sent to the
        // caverns, or are murdered in cold blood
        bool isWorthy = false;
        bool isJudging = false;
        bool judged = false;

        protected bool inCutscene = false;

        // sound sources
        public AudioSource abstractMusic;

        public void Start()
        {
            if(!netObjSelf.IsSpawned && RoundManager.Instance.IsHost)
            {
                netObjSelf.Spawn();
            }
        }

        public void Update()
        {
            if (judged)
            {
                if (isWorthy && !goodEyes.activeInHierarchy)
                {
                    goodEyes.SetActive(true);
                    badEyes.SetActive(false);
                    whiteEyes.SetActive(false);
                }
                else if (!badEyes.activeInHierarchy)
                {
                    badEyes.SetActive(true);
                    goodEyes.SetActive(false);
                    whiteEyes.SetActive(false);
                }
            }
            
            if(isJudging)
            {
                goodEyes.SetActive(false);
                goodEyes.SetActive(false);
                whiteEyes.SetActive(true);
            }
        }

        public async void beginCutscene() {

            if(inCutscene) { return; }

            if(!hasGeneratedCave && RoundManager.Instance.IsHost)
            {
                var caveGO = UnityEngine.GameObject.Find("EI_CaveGenerator");
                var caveGen = caveGO.GetComponent<CaveGenerator>();
                caveGen.generateCave(caveGO.transform.position);
                hasGeneratedCave = true;
            }
            else
            {
                hasGeneratedCave = true;
            }

            inCutscene = true;
            updateSquadWorthiness();
            abstractMusic.Play();  // 29 seconds long
            judgeAnimator.Play("Idle");
            await Task.Delay(15000);  // 15 second wait

            // animation step
            isJudging = true;
            judgeAnimator.Play("Look");

            await Task.Delay(7000);  // 7 second wait

            // judgement finish
            updateSquadWorthiness();
            isJudging = false;
            judged = true;

            await Task.Delay(8000);  // 8 second wait

            if (isWorthy)
            {
                var startingCaveGO = GameObject.Find("StartingCaveTeleportLocation");
                if(startingCaveGO)
                {
                    destination.transform.position = startingCaveGO.transform.position;
                }
                teleportPlayersClientRpc(destination.position);
            }
            else
            {
                Landmine.SpawnExplosion(platformOriginPoint.position, true, 50, 50, 100, 150, null, true);
            }
            judgeAnimator.Play("Idle");

            inCutscene = false;
            isJudging = false;
            judged = false;
        }

        // basically become friendly in any case
        // where someone brought a quantum gold
        // head item.
        public void updateSquadWorthiness()
        {
            bool foundHead = false;
            bool foundArtifact = false;
            var players = getNearestPlayers();

            foreach (PlayerControllerB player in players)
            {
                Plugin.Logger.LogMessage("Checking if " + player.name + " is worthy...");
                var inventory = player.ItemSlots;
                foreach (GrabbableObject obj in inventory)
                {
                    Plugin.Logger.LogMessage(obj);
                    if (obj != null && obj.itemProperties != null && obj.itemProperties.itemName != null)
                    {
                        var iName = obj.itemProperties.itemName.ToLower();
                        Plugin.Logger.LogMessage(":: " + iName);
                        if (iName.Contains("gold") && iName.Contains("head"))
                        {
                            Plugin.Logger.LogMessage("Has Gold Head. Checking if Quantum.");
                            if (obj.gameObject.transform.Find("QuantumItem(Clone)") != null)
                            {
                                foundHead = true;
                            }
                        }
                    }
                }
            }

            // case where head is on ground
            var goldenHeads = UnityEngine.Object.FindObjectsOfType<GoldenHeadScript>();
            foreach(GoldenHeadScript script in goldenHeads)
            {
                if(script.gameObject.transform.Find("QuantumItem(Clone)") != null)
                {
                    if(Vector3.Distance(script.gameObject.transform.position, transform.position) <= 70)
                    {
                        foundHead = true;
                    }
                }
            }

            // case where head is on ground
            var radars = UnityEngine.Object.FindObjectsOfType<TechRadarItem>();
            foreach (TechRadarItem script in radars)
            {
                if (Vector3.Distance(script.gameObject.transform.position, transform.position) <= 70)
                {
                    foundArtifact = true;
                }
            }

            // find artifact

            if (foundArtifact && foundHead)
            {
                if (RoundManager.Instance.IsHost)
                {
                    setWorthyClientRpc();
                }
                else
                {
                    setWorthyServerRpc();
                }
            }
        }

        [ClientRpc]
        public void setWorthyClientRpc()
        {
            isWorthy = true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void setWorthyServerRpc()
        {
            setWorthyClientRpc();
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
                if (Vector3.Distance(player.transform.position, transform.position) <= 70)
                {
                    nearPlayers.Add(player);
                }
            }

            return nearPlayers;
        }
    }
}
