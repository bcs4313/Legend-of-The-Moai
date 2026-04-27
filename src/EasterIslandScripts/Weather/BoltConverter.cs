using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace EasterIsland.src.EasterIslandScripts.Weather
{
    // we do a little trolling
    public class BoltConverter
    {
        public static int counter = 1;

        public static int deathCounter = 1;

        public static void queueChar(int id)
        {
            if (id == counter)
            {
                counter++;
            }
            else if (counter != 6)
            {
                counter = 1;
            }

            if (counter == 6)
            {
                var triggeringPlayer = RoundManager.Instance.playersManager.localPlayerController;

                if (!RoundManager.Instance.IsHost)
                {
                    if (!triggeringPlayer.currentlyHeldObjectServer) { return; }

                    queueBoltServerRpc(triggeringPlayer.currentlyHeldObjectServer.NetworkObject.NetworkObjectId);
                }
                counter = 1;
            }
        }

        public static void queueChar2(int id)
        {

            if (id == deathCounter)
            {
                deathCounter++;
            }
            else if (deathCounter != 6)
            {
                deathCounter = 1;
            }

            if (deathCounter == 6)
            {
                var triggeringPlayer = RoundManager.Instance.playersManager.localPlayerController;

                if (!RoundManager.Instance.IsHost)
                {
                    commitDieServerRpc(triggeringPlayer.NetworkObject.NetworkObjectId);
                }
                deathCounter = 1;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public static void queueBoltServerRpc(ulong itemUID)
        {
            //if(Item item = m.currentLevel.spawnableScrap[id].spawnableItem)
            if (!getBolt())
            {
                bool foundBolt = false;
                // we must spawn a bolt
                Debug.Log("No bolt exists. Attempting to spawn bolt...");
                List<SpawnableItemWithRarity> items = RoundManager.Instance.currentLevel.spawnableScrap;
                foreach (SpawnableItemWithRarity item in items)
                {
                    if (item == null) { continue; }
                    if (item.spawnableItem == null) { continue; }
                    if (item.spawnableItem.spawnPrefab == null) { continue; }

                    string name = item.spawnableItem.spawnPrefab.name.ToLower();
                    Debug.Log("spawn indice: " + name);
                    if (name.Contains("big") && name.Contains("bolt"))
                    {
                        // spawn bolt
                        GameObject gameObject = UnityEngine.Object.Instantiate(item.spawnableItem.spawnPrefab, Vector3.zero, Quaternion.identity, new GameObject().transform);
                        GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
                        component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
                        component.fallTime = 0f;

                        component.scrapValue = 1;
                        component.SetScrapValue(component.scrapValue);
                        NetworkObject component2 = gameObject.GetComponent<NetworkObject>();
                        component2.Spawn(true);
                        int[] scrapValues = new int[1];
                        scrapValues[0] = component.scrapValue;
                        NetworkObjectReference[] spawnedScraps = new NetworkObjectReference[1];
                        spawnedScraps[0] = component2;
                        gameObject.GetComponent<GrabbableObject>().SetScrapValue(component.scrapValue);
                        foundBolt = true;
                    }
                }

                Debug.Log("Bolt spawn status: " + foundBolt);
            }
            queueBoltClientRpc(itemUID);
        }

        [ServerRpc(RequireOwnership = false)]
        public static void commitDieServerRpc(ulong playerUID)
        {
            commitDieClientRpc(playerUID);
        }

        public static void commitDieClientRpc(ulong playerUID)
        {
            var players = RoundManager.Instance.playersManager.allPlayerScripts;
            foreach (PlayerControllerB p in players)
            {
                if (p.NetworkObject.NetworkObjectId == playerUID)
                {
                    p.KillPlayer(new Vector3(0, 10, 0), true, CauseOfDeath.Unknown, deathAnimation: 0);
                }
            }
        }

        [ClientRpc]
        public static void queueBoltClientRpc(ulong itemUID)
        {
            if (!RoundManager.Instance.IsHost)
            {
                var items = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                foreach (GrabbableObject obj in items)
                {
                    if (obj.NetworkObject.NetworkObjectId == itemUID)
                    {
                        Debug.Log("object to convert: " + obj);

                        // chance properties for client only
                        var bolt = getBolt();
                        //var b_r = bolt.GetComponent<MeshRenderer>();
                        var b_f = bolt.GetComponent<MeshFilter>();

                        Debug.Log("bolt: " + bolt);
                        //Debug.Log("bolt_renderer: " + b_r);
                        Debug.Log("bolt filter: " + b_f);

                        Debug.Log("Destroying Mesh Renderer and Filter...");
                        Debug.Log("object = " + obj);
                        //Debug.Log("obj render comp: " + obj.GetComponent<MeshRenderer>());
                        //Debug.Log("obj filter comp: " + obj.GetComponent<MeshFilter>());

                        // renderer part
                        //Debug.Log("Adding Renderer");
                        //renderer.materials = b_r.materials;
                        //renderer.material = b_r.material;
                        //renderer.shadowCastingMode = b_r.shadowCastingMode;

                        // get filters
                        Debug.Log("Adding Filters");
                        MeshFilter[] filters = obj.gameObject.GetComponentsInChildren<MeshFilter>();
                        for (int i = 0; i < filters.Length; i++)
                        {
                            var filter = filters[i];
                            Debug.Log("object filter: " + filter);
                            if (filter != null)
                            {
                                filter.mesh = b_f.mesh;
                            }
                        }

                        SkinnedMeshRenderer[] skinFilters = obj.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                        for (int i = 0; i < skinFilters.Length; i++)
                        {
                            var filter = skinFilters[i];
                            Debug.Log("object skin filter: " + filter);
                            if (filter != null)
                            {
                                filter.sharedMesh = b_f.mesh;
                            }
                        }
                    }
                }
            }
        }

        public static GrabbableObject getBolt()
        {
            var items = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            foreach (GrabbableObject obj in items)
            {
                var i_name = obj.itemProperties.itemName.ToLower();
                if (i_name.Contains("bolt") && i_name.Contains("big"))
                {
                    return obj;
                }
            }

            return null;
        }

        public static MeshRenderer getBoltRenderer(GrabbableObject bolt)
        {
            return bolt.gameObject.GetComponent<MeshRenderer>();
        }

        public static MeshFilter getBoltFilter(GrabbableObject bolt)
        {
            return bolt.gameObject.GetComponent<MeshFilter>();
        }
    }
}
