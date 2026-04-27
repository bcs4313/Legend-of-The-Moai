using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;
using LethalLib.Modules;
using EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg;
using Unity.Netcode;
using System.Security.Principal;
using UnityEngine.SceneManagement;

namespace EasterIsland.src.EasterIslandScripts
{
    public class CaveGenerator : NetworkBehaviour
    {
        public GameObject cavePopulatorRoot;  // root gameobject to spawn the navmesh

        // mandatory room part
        public GameObject[] mandatoryRoomStartNodes;  // rooms added at the end that must be included, no matter what
        public int mandatoryRoomIndex = 0;

        // backtracking variables for if the cave ends early
        // these lists run in parallel
        public List<GameObject> endNodesFinished;
        public List<GameObject> tilesFinished;
        public int retryGenerationDeleteQuantity = 1; // amount of nodes to remove on generation failure

        // DO NOT ADD THE END NODE HERE
        public GameObject[] caveTiles;

        public List<BoxCollider> allCollidersToRemove;

        // the queue includes all end nodes that currently need a
        // connection. Generation can not stop until the queue is satisfied.
        public List<GameObject> endNodeQueue;

        // we ALWAYS start with this node when generating
        // only has an EndNode
        public GameObject startCaveNode;

        // Once cave generation runs out,
        // supply all nodes with the dead end.
        // the dead end has no end node.
        public GameObject deadEndTile;

        // if the deadend intersects then we 
        // will use a cap tile instead.
        public GameObject capTile;

        // for debug purposes
        public GameObject scanSquare;

        // wait to finish before continuing
        public BoxCollider mostRecentCollider;

        // if attempts to link the tile go above 3,
        // cap the tile
        int failureStreak = 0;

        // generation rate, for debugging
        int generationRate = 50;   // delay in ms 

        protected int generationLimit = 50;  // once this tile count runs out the cave will only use end nodes
        protected int generationsLeft = -1;
        protected float tileSize = 0.25f;   // standard tile size
        protected bool deleteIntersectionColliders = true; // disable for debugging

        public void Start()
        {
            if(!GetComponent<NetworkObject>().IsSpawned && RoundManager.Instance.IsHost)
            {
                GetComponent<NetworkObject>().Spawn();
            }
        }

        // get the start node, which is the pivot for
        // the given tile that connects to an end node
        public GameObject GetStartNode(GameObject tile)
        {
            // Find the StartNode GameObject by name or tag
            Transform startNode = tile.transform.Find("StartNode");
            if (startNode == null)
            {
                Debug.LogError("StartNode not found on tile: " + tile.name);
            }
            return startNode.gameObject;
        }

        // get all available end nodes, which link to 
        // start nodes. Recursive search.
        public List<GameObject> GetEndNodes(GameObject tile)
        {
            List<GameObject> endNodes = new List<GameObject>();

            // Recursive function to search through the hierarchy
            void FindEndNodes(Transform parent)
            {
                foreach (Transform child in parent)
                {
                    if (child.name.StartsWith("EndNode")) // Match by name convention
                    {
                        endNodes.Add(child.gameObject);
                    }
                    // Recursively search child nodes
                    FindEndNodes(child);
                }
            }

            FindEndNodes(tile.transform);

            if (endNodes.Count == 0)
            {
                Debug.LogWarning($"No EndNodes found on tile: {tile.name}");
            }
            else
            {
                Debug.Log($"Found {endNodes.Count} EndNodes on tile: {tile.name}");
            }

            return endNodes;
        }


        // Connect tiles by using the pivot of the end node and the start node
        public void ConnectTiles(GameObject endNode, GameObject startNode)
        {
            startNode.transform.position = endNode.transform.position;
            startNode.transform.rotation = endNode.transform.rotation;
        }

        public async Task GenerateNewTile(GameObject endNode)
        {
            var attemptingMandatoryRoom = false;
            if (failureStreak > 3)
            {
                Debug.LogWarning($"Failure streak maxed for end node: Capping off pathway.");
                capOutTile(endNode);
                endNodeQueue.Remove(endNode);
                failureStreak = 0;
                return;
            }

            GameObject newTile = null;
            // create new tile and connect the nodes
            if (generationsLeft > 0)
            {
                newTile = UnityEngine.Object.Instantiate(caveTiles[UnityEngine.Random.Range(0, caveTiles.Length)]);
            }
            else
            {
                // we ran out of generations. Mandatory rooms go in first, then dead ends.
                if (mandatoryRoomIndex < mandatoryRoomStartNodes.Length)
                {
                    newTile = UnityEngine.Object.Instantiate(mandatoryRoomStartNodes[mandatoryRoomIndex]);
                    mandatoryRoomIndex++;
                    attemptingMandatoryRoom = true;
                }
                else
                {
                    newTile = UnityEngine.Object.Instantiate(deadEndTile);
                }
            }
            var startNode = GetStartNode(newTile);
            startNode.transform.localScale = new Vector3(tileSize, tileSize, tileSize);  // scale tile appropriately
            ConnectTiles(endNode, startNode);

            newTile.transform.parent = cavePopulatorRoot.transform;
            Physics.SyncTransforms();

            // await for CaveCollider to be active (intersection detection)
            Transform colliderTransform = startNode.transform.Find("CaveCollider");
            if (colliderTransform != null)
            {
                var timeout = 10;
                mostRecentCollider = colliderTransform.GetComponent<BoxCollider>();
                while (mostRecentCollider == null || !mostRecentCollider.enabled || !mostRecentCollider.gameObject.activeInHierarchy)
                {

                    Physics.SyncTransforms();
                    await Task.Delay(50);
                    timeout--;
                    if (timeout <= 0)
                    {
                        break;
                    }
                }
            }

            await Task.Delay(generationRate);

            // we need a way to have this failure case not
            // mess with the expected length of the cave (unless we really don't care).
            if (!IsTilePlacementValid(startNode))
            {
                Debug.LogWarning($"Invalid placement detected for tile: {newTile.name}. Destroying tile.");
                Destroy(newTile); // Remove invalid tiles
                failureStreak++;

                if (mandatoryRoomIndex > 0 && attemptingMandatoryRoom)
                {
                    mandatoryRoomIndex--;
                }
                return; // Skip to the next iteration
            }


            failureStreak = 0;
            // update end node queue
            endNodeQueue.Remove(endNode);
            endNodeQueue.AddRange(GetEndNodes(newTile));

            // for backtracking
            endNodesFinished.Add(endNode);
            tilesFinished.Add(newTile);

            // collider removal
            allCollidersToRemove.Add(mostRecentCollider);
        }

        // for handling a special collision case where
        // the dead end intersects with something
        public void capOutTile(GameObject endNode)
        {
            GameObject newTile = UnityEngine.Object.Instantiate(capTile);
            var startNode = GetStartNode(newTile);
            startNode.transform.localScale = new Vector3(tileSize, tileSize, tileSize);  // scale tile appropriately
            ConnectTiles(endNode, startNode);
            tilesFinished.Add(newTile); 

            newTile.transform.parent = cavePopulatorRoot.transform;
        }

        public bool IsTilePlacementValid(GameObject startNode)
        {
            // Find the collider GameObject
            Transform colliderTransform = startNode.transform.Find("CaveCollider");

            if (colliderTransform == null)
            {
                Debug.LogError($"StartNode {startNode.name} is missing a Collider GameObject.");
                return false;
            }

            // Get the BoxCollider component
            BoxCollider tileCollider = colliderTransform.GetComponent<BoxCollider>();
            if (tileCollider == null)
            {
                Debug.LogError($"Tile {colliderTransform.name} is missing a BoxCollider component.");
                return false;
            }

            Vector3 worldCenter = tileCollider.transform.TransformPoint(tileCollider.center);
            Vector3 worldHalfExtents = Vector3.Scale(tileCollider.size, tileCollider.transform.lossyScale) * 0.5f;
            Collider[] overlappingColliders = Physics.OverlapBox(worldCenter, worldHalfExtents, tileCollider.transform.rotation);

            Debug.Log("Data for: CaveCollider GO: " + colliderTransform.gameObject.GetInstanceID());
            Debug.Log("World Center: " + worldCenter);
            Debug.Log("World Half Extents: " + worldHalfExtents);
            Debug.Log("Rotation: " + tileCollider.transform.rotation);

            // debug step
            //var debugObj = UnityEngine.Object.Instantiate(scanSquare, worldCenter, tileCollider.transform.rotation, null);
            //debugObj.transform.localScale = worldHalfExtents * 2;

            Debug.Log("Collision List: ");
            // Check for intersections
            foreach (Collider hit in overlappingColliders)
            {
                if (hit.gameObject != colliderTransform.gameObject) // Ignore self-collisions
                {
                    //Debug.Log($"COL: {hit.gameObject.name}");
                    if (hit.gameObject.name.ToLower().Contains("cavecollider"))
                    {
                        Debug.Log($"Valid Collision detected with {hit.gameObject.name}");
                        return false;
                    }
                }
                else
                {
                    Debug.Log("SELF COLLIDE: ");
                    Debug.Log("AT POSITION: " + hit.transform.position);
                }
            }

            return true; // No collisions detected
        }


        public async void generateCave(Vector3 startPosition)
        {
            generationsLeft = generationLimit;
            mandatoryRoomIndex = 0;

            // initialize the starting tile and the end node queue
            endNodeQueue = new List<GameObject>();
            endNodesFinished = new List<GameObject>();
            tilesFinished = new List<GameObject>();
            var CaveGenerationRoot = UnityEngine.Object.Instantiate(startCaveNode, Vector3.zero, startCaveNode.transform.rotation);
            CaveGenerationRoot.transform.position = startPosition;  // move cave node to position
            GetStartNode(CaveGenerationRoot).transform.localScale = new Vector3(tileSize, tileSize, tileSize);  // scale tile appropriately
            allCollidersToRemove.Add(GetStartNode(CaveGenerationRoot).transform.Find("CaveCollider").GetComponent<BoxCollider>());
            CaveGenerationRoot.transform.parent = cavePopulatorRoot.transform;

            // add the first end node
            endNodeQueue.AddRange(GetEndNodes(CaveGenerationRoot));

            // the loop picks random tiles, finds their end nodes, and then queues more tiles from those end nodes.
            // start nodes connect to the end nodes. Each tile has a start node no matter what.
            while (generationsLeft > 0 || mandatoryRoomIndex < mandatoryRoomStartNodes.Length)  // backtracker while loop
            {
                if (retryGenerationDeleteQuantity < endNodesFinished.Count)
                {
                    retryGenerationDeleteQuantity++;
                }
                else
                {
                    retryGenerationDeleteQuantity = endNodesFinished.Count;
                }
                while (endNodeQueue.Count > 0)
                {
                    var currentEndNode = endNodeQueue[0];
                    await GenerateNewTile(currentEndNode);
                    generationsLeft--;
                }

                // case handling for when we don't generate enough cave tiles
                if (generationsLeft > 0 || mandatoryRoomIndex < mandatoryRoomStartNodes.Length)  // generations ended early!
                {
                    for (int i = 0; i < retryGenerationDeleteQuantity; i++)
                    {
                        Destroy(tilesFinished[tilesFinished.Count - 1]);
                        endNodeQueue.Add(endNodesFinished[endNodesFinished.Count - 1]);
                        endNodesFinished.RemoveAt(endNodesFinished.Count - 1);
                        tilesFinished.RemoveAt(tilesFinished.Count - 1);

                        if (mandatoryRoomIndex > 0) 
                        {
                            if (mandatoryRoomIndex - retryGenerationDeleteQuantity < 0) // we deleted 1 or more mandatory rooms
                            {
                                mandatoryRoomIndex = 0;
                            }
                            else
                            {
                                mandatoryRoomIndex = mandatoryRoomIndex - retryGenerationDeleteQuantity;
                            }
                        }
                    }
                    continue;  // don't delete colliders
                }

                if (deleteIntersectionColliders)
                {
                    foreach (Collider c in allCollidersToRemove)
                    {
                        if (c != null && c.gameObject != null)
                        { // disable the cave collider
                            c.gameObject.SetActive(false);
                        }
                    }
                }

                // finally, transfer all cave data to clients
                transferServerData(CaveGenerationRoot);

                await Task.Delay(3000); // give some time for everything to populate

                // the generator is done. Now populate the cave.
                cavePopulatorRoot.GetComponent<CavePopulator>().PopulateEnvironment();
            }
        }

        // CLIENT SYNCHRONIZATION SECTION
        // spawn all tiles from tilesFinished in the same orientation
        public void transferServerData(GameObject caveGenerationRoot)
        {
            if(!RoundManager.Instance.IsHost) { return; }

            // Start tile step
            String identity = caveGenerationRoot.name;
            Vector3 position = GetStartNode(caveGenerationRoot).transform.position;
            Quaternion rotation = GetStartNode(caveGenerationRoot).transform.rotation;
            spawnTileClientRpc(identity, position, rotation);

            // all other tiles
            foreach (GameObject tile in tilesFinished)
            {
                identity = tile.name;
                position = GetStartNode(tile).transform.position;
                rotation = GetStartNode(tile).transform.rotation;
                spawnTileClientRpc(identity, position, rotation);
            }
        }

        public GameObject getPrefabFromID(String name)
        {
            if(name.Contains(startCaveNode.name)) { return startCaveNode;  }
            if (name.Contains(capTile.name)) { return capTile; }
            if (name.Contains(deadEndTile.name)) { return deadEndTile; }

            foreach (GameObject tile in caveTiles)
            {
                if(name.Contains(tile.name))
                {
                    return tile;
                }
            }

            foreach (GameObject tile in mandatoryRoomStartNodes)
            {
                if (name.Contains(tile.name))
                {
                    return tile;
                }
            }

            return null;
        }

        [ClientRpc]
        public void spawnTileClientRpc(String tileIdentity, Vector3 startNodePosition, Quaternion startNodeRotation)
        {
            if (RoundManager.Instance.IsHost) { return; }

            GameObject tileTarget = getPrefabFromID(tileIdentity);
            GameObject spawnedTile = UnityEngine.Object.Instantiate(tileTarget);

            GameObject startNode = GetStartNode(spawnedTile);
            startNode.transform.position = startNodePosition;
            startNode.transform.rotation = startNodeRotation;

            // remove the collider for client
            startNode.transform.Find("CaveCollider").gameObject.SetActive(false);
        }
    }
}
