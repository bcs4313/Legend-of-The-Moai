using Unity.Netcode;
using UnityEngine.VFX;
using UnityEngine;
using System.Threading.Tasks;
using EasterIsland;
using EasterIsland.src.EasterIslandScripts;
using UnityEngine.AI;

public class PortalMoaiSpawn : MonoBehaviour
{
    bool awaitSpawn = true;

    public void OnEnable()
    {
        SpawnHiveNearEnemy();
    }


    private float RandomNumberInRadius(float radius, System.Random randomSeed)
    {
        return ((float)randomSeed.NextDouble() - 0.5f) * radius;
    }

    public Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius = 10f, NavMeshHit navHit = default(NavMeshHit), System.Random randomSeed = null, int layerMask = -1)
    {
        float y = pos.y;
        float x = RandomNumberInRadius(radius, randomSeed);
        float y2 = RandomNumberInRadius(radius, randomSeed);
        float z = RandomNumberInRadius(radius, randomSeed);
        Vector3 vector = new Vector3(x, y2, z) + pos;
        vector.y = y;
        float num = Vector3.Distance(pos, vector);
        if (NavMesh.SamplePosition(vector, out navHit, num + 2f, layerMask))
        {
            return navHit.position;
        }

        return pos;
    }

    private async void SpawnHiveNearEnemy()
    {
        if (RoundManager.Instance.IsServer)
        {
            while (RoundManager.Instance.dungeonGenerator == null)
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn portal  - -3...");
                await Task.Delay(1000);
            }
            while (RoundManager.Instance.dungeonGenerator.Generator == null)
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn portal - -2...");
                await Task.Delay(1000);
            }
            while (RoundManager.Instance.dungeonGenerator.Generator.IsGenerating)
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn portal - -1...");
                await Task.Delay(1000);
            }

            while (awaitSpawn)
            {

                var nodes = RoundManager.Instance.outsideAINodes;
                while (nodes == null || nodes.Length == 0)
                {
                    Debug.Log($"EasterIslandPortal: Awaiting to spawn portal - 1...");
                    await Task.Delay(1000);
                    nodes = RoundManager.Instance.outsideAINodes;
                }

                Vector3 originPos = nodes[new System.Random().Next(0, nodes.Length)].transform.position;
                Vector3 randomNavMeshPositionInBoxPredictable = GetRandomNavMeshPositionInBoxPredictable(originPos, 3f, RoundManager.Instance.navHit, new System.Random(), -5);
                if (randomNavMeshPositionInBoxPredictable == originPos)
                {
                    Debug.Log($"Moai Enemy: Awaiting to spawn portal - 2...");
                    await Task.Delay(1000);
                    continue;
                }

                // delete old portalpair if it exists:
                if(GameObject.Find("PortalPair(Clone)"))
                {
                    GameObject.Destroy(GameObject.Find("PortalPair(Clone)"));
                }

                Debug.Log($"EasterIslandPortal: Set portal random position: {randomNavMeshPositionInBoxPredictable}");
                awaitSpawn = false;
                GameObject gameObject = UnityEngine.Object.Instantiate(Plugin.portalPair, Plugin.portalPair.transform.position, Plugin.portalPair.transform.rotation);
                gameObject.SetActive(value: true);

                var rootObj = gameObject.GetComponent<NetworkObject>();
                rootObj.GetComponent<NetworkObject>().Spawn(true);
                
                while(!rootObj.IsSpawned)
                {
                    await Task.Delay(500);
                    Debug.Log($"EasterIslandPortal: Awaiting Root Object spawn: {randomNavMeshPositionInBoxPredictable}");
                }


                GameObject labportal = gameObject.transform.Find("LabPortal").gameObject;
                GameObject islandportal = gameObject.transform.Find("IslandPortal").gameObject;

                while (!labportal)
                {
                    await Task.Delay(500);
                    Debug.Log($"EasterIslandPortal: Awaiting Lab Portal Object spawn: {randomNavMeshPositionInBoxPredictable}");
                }

                while (!islandportal)
                {
                    await Task.Delay(500);
                    Debug.Log($"EasterIslandPortal: Awaiting Island Portal Object spawn: {randomNavMeshPositionInBoxPredictable}");
                }

                Debug.Log("Both portals spawned. Initializing portals.");
                labportal.GetComponent<PortalScript>().initialize(this.transform.position);
                islandportal.GetComponent<PortalScript>().initialize(randomNavMeshPositionInBoxPredictable + Vector3.up * 0.5f);
            }
        }
    }
}