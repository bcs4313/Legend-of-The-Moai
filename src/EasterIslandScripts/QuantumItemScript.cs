using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace EasterIsland.src.EasterIslandScripts
{
    public class QuantumItemScript : NetworkBehaviour
    {
        public float instability = 0; // 100 instability will make it teleport the nearest player / object
        public GameObject parent; // used to identify the item to teleport and/or identify
        public GameObject ball;  // particle system 1
        public GameObject arcs;  // particle system 2
        private Vector3 prevPos;
        private bool justTeleported = false;
        public static List<int> itemsTeleportedTo = new List<int>();
        private float ballUpdateTick = 0;

        public AudioSource T1;
        public AudioSource T2;
        public AudioSource T3;
        public AudioSource TELE;

        // prevents all teleport actions
        // for 2.5 seconds
        public float teleportTime;

        bool awaitReassign = false;
        float lastReassignTime = 0f;


        // item parent, might be held by player
        public GrabbableObject hostItem;

        public Vector3 sampleObjFall(GrabbableObject objTarget)
        {
            if (!objTarget) { return Vector3.zero; }
            else
            {
                NavMeshHit hit;
                var result = NavMesh.SamplePosition(objTarget.transform.position, out hit, 15f, NavMesh.AllAreas);
                if (result) { return hit.position; }
                else { return Vector3.zero; }
            }
        }

        void Start()
        {
            // in no case do we want an active GrabbableObject inside here. It is only for saving.
            GetComponent<GrabbableObject>().enabled = false;

            // another trick. If there is no hostItem and or parent, we attach to the nearest object.
            var t = gameObject.transform.parent;

            // restart and reassign the parent transform if the netobj is missing
            if (!GetComponent<NetworkObject>().IsSpawned && RoundManager.Instance.IsHost && hostItem)
            {
                GetComponent<NetworkObject>().Spawn();
                this.transform.parent = hostItem.gameObject.transform;
            }

            if (hostItem != null)
            {
                Plugin.Logger.LogInfo("Setting Painting Floor Position to::: ");
                Plugin.Logger.LogInfo(hostItem.gameObject.transform.localPosition);
                hostItem.targetFloorPosition = hostItem.gameObject.transform.localPosition;
            }

            prevPos = transform.position;

            if (!t)
            {
                Debug.Log("LegendOfTheMoai: Failed to find parent of QuantamItem. Reassigning...");
                awaitReassign = true;
                lastReassignTime = Time.time;
                return;
            }

            parent = t.gameObject;

            // get item
            hostItem = parent.GetComponent<GrabbableObject>();

            if (!hostItem)
            {
                Debug.Log("LegendOfTheMoai: Failed to find root item of QuantamItem. Reassigning...");
                awaitReassign = true;
                lastReassignTime = Time.time;
                return;
            }
        }

        [ClientRpc]
        public void initClientRpc(ulong id)
        {
            Plugin.Logger.LogInfo("Quantum Client Init:");
            var objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>(false);
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if(!obj || !obj.NetworkObject) { continue; }
                if (obj.NetworkObject.NetworkObjectId == id)
                {
                    Plugin.Logger.LogInfo("Found object to inject data into: " + obj.name + " injecting...");

                    if (RoundManager.Instance.IsServer)
                    {
                        transform.parent = obj.transform;
                    }
                    var quantumScript = GetComponent<QuantumItemScript>();
                    if (RoundManager.Instance.IsServer)
                    {
                        quantumScript.parent = obj.gameObject;
                    }
                    quantumScript.hostItem = obj;

                    finalizeVariables(obj);
                }
            }
        }

        public async void finalizeVariables(GrabbableObject obj)
        {
            Plugin.Logger.LogInfo("setting quantum item localpos");

            while (!obj)
            {
                Plugin.Logger.LogInfo("awaiting Find for obj");
                await Task.Delay(200);
            }

            while (!obj.gameObject)
            {
                Plugin.Logger.LogInfo("awaiting Find for obj.gameObject");
                await Task.Delay(200);
            }

            while (!obj.gameObject.transform.Find("QuantumItem(Clone)"))
            {
                Plugin.Logger.LogInfo("awaiting Find for QuantumItem(Clone)");
                await Task.Delay(200);
            }

            Plugin.Logger.LogInfo("Finalizing...");
            obj.gameObject.transform.Find("QuantumItem(Clone)").transform.localPosition = new Vector3(0, 0, 0);
        }

        [ClientRpc]
        private void tier1ClientRpc()
        {
            if (!T1.isPlaying)
            {
                T1.Play();
                T2.Stop();
                T3.Stop();
            }
        }

        [ClientRpc]
        private void tier2ClientRpc()
        {
            if (!T2.isPlaying)
            {
                T1.Stop();
                T2.Play();
                T3.Stop();
            }
        }

        [ClientRpc]
        private void tier3ClientRpc()
        {
            if (!T3.isPlaying)
            {
                T1.Stop();
                T2.Stop();
                T3.Play();
            }
        }

        [ClientRpc]
        private void teleSoundClientRpc()
        {
            T1.Stop();
            T2.Stop();
            T3.Stop();
            TELE.Play();
        }

        [ClientRpc]
        private void stopSoundsClientRpc()
        {
            T1.Stop();
            T2.Stop();
            T3.Stop();
        }

        [ClientRpc]
        private void setArcsClientRpc(bool value)
        {
            if (value)
            {
                arcs.SetActive(true);
                arcs.GetComponent<ParticleSystem>().Play();
            }
            else
            {
                arcs.SetActive(false);
                arcs.GetComponent<ParticleSystem>().Stop();
            }
        }

        [ClientRpc]
        private void setBallSizeClientRpc(float size)
        {
            ball.transform.localScale = new Vector3(1 + size, 1 + size, 1 + size);
        }

        public void attemptReassign()
        { 
            try
            {
                Debug.Log("LegendOfTheMoai: Reassigning Quantum Item Parent (From Save)");
                Vector3 pos = gameObject.transform.position;

                GrabbableObject[] objs = FindObjectsOfType<GrabbableObject>();

                GrabbableObject closestObj = null;
                float closestDistance = 9999f;
                foreach (GrabbableObject obj in objs)
                {
                    if(!obj.itemProperties) { continue; }
                    if (!obj.itemProperties.itemName.Equals(this.gameObject.GetComponent<GrabbableObject>().itemProperties.itemName))
                    {
                        Vector3 otherObjPos = obj.transform.position;
                        var dist = Vector3.Distance(pos, otherObjPos);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            closestObj = obj;
                        }
                    }
                }

                Debug.Log("LegendOfTheMoai: Assigned quantum host item: " + closestObj.name);
                hostItem = closestObj;

                // restart and reassign the parent transform if the netobj is missing
                if (!GetComponent<NetworkObject>().IsSpawned && RoundManager.Instance.IsHost)
                {
                    GetComponent<NetworkObject>().Spawn();
                }

                this.transform.parent = hostItem.gameObject.transform;
                hostItem = closestObj;
                awaitReassign = false;
                this.transform.localPosition = Vector3.zero;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        // note, multiplying by delta time regularizes updates to apply x value per second
        void Update()
        {
            // reaSsign area
            if(awaitReassign && Time.time - lastReassignTime > 2f)  // try every 2 seconds
            {
                lastReassignTime = Time.time;

                if(RoundManager.Instance.IsHost)
                {
                    attemptReassign();
                }
            }

            // host item is needed to function
            if (!hostItem) { return; }

            // teleport chain prevention
            if(Time.time < (teleportTime + 2.5f))
            {
                instability = 0;
                return;
            }

            // should be a host only item core logic wise
            if(!RoundManager.Instance.IsHost) { return; }

            if (instability > 0)
            {
                instability -= 10f * Time.deltaTime;  // 10 seconds to stabilize (from max instability 100)
            }

            // destabilize object based on change in position
            var curPos = transform.position;

            var vel = curPos - prevPos;
            if(vel.magnitude < 1 && !hostItem.isInShipRoom)
            {
                instability += vel.magnitude * 3.6f;  // this doesn't need delta, since change in position is applied on per frame basis
            }

            // speeds up stabilization if not moving at all
            if (vel.magnitude < 0.01 && instability > 0)
            {
                instability -= 35f * Time.deltaTime;
            }

            prevPos = transform.position;


            if (justTeleported)
            {
                instability = 0;
                justTeleported = false;
            }
            if (instability < 25)
            {
                if(T1.isPlaying || T2.isPlaying || T3.isPlaying)
                {
                    stopSoundsClientRpc();
                }
            }
            if (instability > 25 && instability < 50) 
            {
                if (!T1.isPlaying)
                {
                    tier1ClientRpc();
                }
            
            }
            if (instability >= 50 && instability < 75)
            {
                if (!T2.isPlaying)
                {
                    tier2ClientRpc();
                }
            }
            if (instability > 75)
            {
                if (!T3.isPlaying)
                {
                    tier3ClientRpc();
                }
            }

            // apply scaling to particle effects depending on instability value
            if (instability > 75 && (!arcs.activeInHierarchy || !arcs.GetComponent<ParticleSystem>().isPlaying))
            {
                setArcsClientRpc(true);
            }
            if (instability < 35 && arcs.activeInHierarchy)
            {
                setArcsClientRpc(false);
            }

            if (ballUpdateTick + 0.1 < Time.time)
            {
                float extraSize = instability * 0.07f;
                setBallSizeClientRpc(extraSize);
                ballUpdateTick = Time.time;
            }

            // instability of 100 causes a teleportation swap with a matching item (and any nearby player).
            // if there is no match, teleport to itself
            if (instability >= 100)
            {
                teleportTime = Time.time;
                instability = 0;
                justTeleported = true;
                teleSoundClientRpc();

                Debug.Log("LegendOfTheMoai: A quantum item is very unstable, attempting to swap item with another instance of itself.");
                List<GrabbableObject> itemTargets = getItemTargets(hostItem);  // should always return at least 1 item (self)
                GrabbableObject target = itemTargets[UnityEngine.Random.Range(0, itemTargets.Count)];

                Vector3 hostPos = hostItem.transform.position;
                Vector3 targetPos = target.transform.position;
                List<PlayerControllerB> nearbyPlayers = getNearbyPlayers();

                // swap position of players
                foreach (PlayerControllerB player in nearbyPlayers)
                {
                    Vector3 randTransform = targetPos + new Vector3(-5 + UnityEngine.Random.Range(0f, 10f), -5 + UnityEngine.Random.Range(0f, 10f), -5 + UnityEngine.Random.Range(0f, 10f));
                    NavMeshHit destination;
                    var sample = NavMesh.SamplePosition(randTransform, out destination, 15f, NavMesh.AllAreas);
                    if (sample)
                    {
                        teleportPlayerClientRpc(player.NetworkObjectId, destination.position);
                    }
                    else
                    {
                        teleportPlayerClientRpc(player.NetworkObjectId, targetPos);
                    }

                    // force audio preset to prevent indoor rain
                    if (target.isInFactory)
                    {
                        SetAudioPresetClientRpc(player.NetworkObjectId);
                    }
                }

                // swap position of items
                if (hostItem.isHeld && hostItem.playerHeldBy)  // case where player is holding the item
                {
                    // do nothing with the hostItem, the player has already moved with the item
                    if (hostItem.GetInstanceID() != target.GetInstanceID())
                    {
                        teleportItemClientRpc(hostPos, target.NetworkObjectId);
                        var h1 = hostItem.isInFactory;
                        var t1 = target.isInFactory;
                        hostItem.isInFactory = t1;
                        target.isInFactory = h1;
                    }
                }
                else if (hostItem.isHeld && hostItem.isHeldByEnemy)
                {
                    hostItem.DiscardItemFromEnemy();
                    teleportItemClientRpc(targetPos, hostItem.NetworkObjectId);
                    teleportItemClientRpc(hostPos, target.NetworkObjectId);
                    var h1 = hostItem.isInFactory;
                    var t1 = target.isInFactory;
                    hostItem.isInFactory = t1;
                    target.isInFactory = h1;
                }
                else  // case where item is not being held by player
                {
                    teleportItemClientRpc(targetPos, hostItem.NetworkObjectId);
                    teleportItemClientRpc(hostPos, target.NetworkObjectId);
                    var h1 = hostItem.isInFactory;
                    var t1 = target.isInFactory;
                    hostItem.isInFactory = t1;
                    target.isInFactory = h1;
                }

                itemsTeleportedTo.Add(target.GetInstanceID());
            }
        }

        [ClientRpc]
        public void teleportItemClientRpc(Vector3 pos, ulong itemid)
        {
            var items = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            GrabbableObject targetObj = null;
            foreach (GrabbableObject obj in items)
            {
                if (obj.NetworkObjectId == itemid)
                {
                    targetObj = obj;
                }
            }

            if(targetObj == null)
            {
                Plugin.Logger.LogError("Easter Island Quantum Item Error: failed to find  item for player teleport!");
                return;
            }

            targetObj.startFallingPosition = pos;
            targetObj.transform.position = pos;
            targetObj.FallToGround();
        }

        [ClientRpc]
        public void teleportPlayerClientRpc(ulong playerid, Vector3 pos)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;

            PlayerControllerB targetPlayer = null;
            foreach (PlayerControllerB player in players)
            {
                if (player.NetworkObjectId == playerid)
                {
                    targetPlayer = player;
                }
            }

            if (targetPlayer == null)
            {
                Plugin.Logger.LogError("Easter Island Quantum Item Error: failed to find player for player teleport!");
                return;
            }

            targetPlayer.transform.position = pos;
        }

        public List<PlayerControllerB> getNearbyPlayers()
        {
            PlayerControllerB[] players = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            var playerList = new List<PlayerControllerB>();

            foreach (var player in players)
            {
                if (Vector3.Distance(player.transform.position, hostItem.transform.position) < 8)
                {
                    playerList.Add(player);
                }
            }

            return playerList;
        }

        public PlayerControllerB getNearestPlayer()
        {
            PlayerControllerB[] players = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            float closestDist = 9999;
            PlayerControllerB closestPlayer = null;

            foreach (var player in players)
            {
                if (Vector3.Distance(player.transform.position, hostItem.transform.position) < closestDist)
                {
                    closestPlayer = player;
                    closestDist = Vector3.Distance(player.transform.position, hostItem.transform.position);
                }
            }

            return closestPlayer;
        }

        [ClientRpc]
        private void SetAudioPresetClientRpc(ulong playerid)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;

            PlayerControllerB targetPlayer = null;
            foreach(PlayerControllerB player in players)
            {
                if(player.NetworkObjectId == playerid)
                {
                    targetPlayer = player;
                }
            }

            if(targetPlayer == null)
            {
                Plugin.Logger.LogError("Easter Island Quantum Item Error: failed to find player for Audio Preset!");
                return;
            }

            UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(targetPlayer);
        }

        // finds items like the hosting item. If there are none, return a list
        // that contains ALL items
        public List<GrabbableObject> getItemTargets(GrabbableObject hostingItem)
        {
            var items = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            var itemList = new List<GrabbableObject>();

            // only has self
            if (items.Length == 1) { 
                itemList.Add(items[0]);
                return itemList;
            }

            foreach (var itemIter in items)
            {
                if(itemIter.itemProperties.name.Equals(hostingItem.itemProperties.name) && itemIter.GetInstanceID() != hostingItem.GetInstanceID())
                {
                    // you can't teleport from inside to outside with a quantum item (balancing)
                    if (!itemIter.isInFactory && hostingItem.isInFactory)
                    {
                       
                    }
                    else
                    {
                        itemList.Add(itemIter);
                    }
                }
            }

            // only has self 2
            if (itemList.Count == 0) 
            {
                itemList = new List<GrabbableObject>();
                itemList.Add(hostingItem);
            }

            return itemList;
        }
    }
}
