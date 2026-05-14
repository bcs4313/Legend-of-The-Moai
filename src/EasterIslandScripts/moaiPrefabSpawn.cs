using Unity.Netcode;
using UnityEngine.VFX;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AI;
using EasterIsland;

public class GoldMoaiSpawn : MonoBehaviour
{
    bool awaitSpawn = true;

    [Space(5f)]
    public GameObject hivePrefab;

    public NoisemakerProp hive;

    public void OnEnable()
    {
        Debug.Log("Gold Moai: OnEnable Call");
        if (RoundManager.Instance.IsServer)
        {
            SpawnHiveNearEnemy();
        }
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
            while (RoundManager.Instance.dungeonIsGenerating == true)
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn gold moai - -3...");
                await Task.Delay(1000);
            }
            while (RoundManager.Instance.dungeonCompletedGenerating == false)
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn gold moai - -2...");
                await Task.Delay(1000);
            }
            while (!StartOfRound.Instance.shipHasLanded)  // assuming 15 Scrap objects always spawn
            {
                Debug.Log($"Moai Enemy: Awaiting to spawn gold moai - -1...");
                await Task.Delay(1000);
            }

            while (awaitSpawn)
            {
                var nodes = RoundManager.Instance.outsideAINodes;
                while (nodes == null || nodes.Length == 0)
                {
                    Debug.Log($"Moai Enemy: Awaiting to spawn gold moai - 1...");
                    await Task.Delay(1000);
                    nodes = RoundManager.Instance.outsideAINodes;
                }

                Vector3 originPos = nodes[new System.Random().Next(0, nodes.Length)].transform.position;
                Vector3 randomNavMeshPositionInBoxPredictable = GetRandomNavMeshPositionInBoxPredictable(originPos, 3f, RoundManager.Instance.navHit, new System.Random(), -5);
                if (randomNavMeshPositionInBoxPredictable == originPos)
                {
                    Debug.Log($"Moai Enemy: Awaiting to spawn gold moai - 2...");
                    await Task.Delay(1000);
                    continue;
                }

                Debug.Log($"Moai Enemy: Set gold moai random position: {randomNavMeshPositionInBoxPredictable}");
                awaitSpawn = false;
                hivePrefab = Plugin.GoldenHead;
                GameObject gameObject = UnityEngine.Object.Instantiate(hivePrefab, randomNavMeshPositionInBoxPredictable + Vector3.up * 0.5f, Quaternion.Euler(Vector3.zero), RoundManager.Instance.spawnedScrapContainer);
                gameObject.SetActive(value: true);
                gameObject.GetComponent<NetworkObject>().Spawn();
                gameObject.GetComponent<NoisemakerProp>().targetFloorPosition = randomNavMeshPositionInBoxPredictable + Vector3.up * 0.5f;
                SpawnHiveClientRpc(hiveObject: gameObject.GetComponent<NetworkObject>(), hivePosition: randomNavMeshPositionInBoxPredictable + Vector3.up * 0.5f);
            }
        }
    }

    [ClientRpc]
    public void SpawnHiveClientRpc(NetworkObjectReference hiveObject, Vector3 hivePosition)
    {
        if (hiveObject.TryGet(out var networkObject))
        {
            hive = networkObject.gameObject.GetComponent<NoisemakerProp>();
            hive.targetFloorPosition = hivePosition;
            hive.isInFactory = false;
            int hiveScrapValue = new System.Random().Next(50, 200);

            hive.scrapValue = hiveScrapValue;
            ScanNodeProperties componentInChildren = hive.GetComponentInChildren<ScanNodeProperties>();
            if (componentInChildren != null)
            {
                componentInChildren.scrapValue = hiveScrapValue;
                componentInChildren.headerText = "Golden Moai Head";
                componentInChildren.subText = $"VALUE: ${hiveScrapValue}";
            }

            RoundManager.Instance.totalScrapValueInLevel += hive.scrapValue;
        }
        else
        {
            Debug.LogError("Moai Enemy: Error! gold moai could not be accessed from network object reference");
        }
        Destroy(this.gameObject);
    }

    public void OnDestroy()
    {
        Debug.Log("Moai Enemy: Gold Moai Spawner has been destroyed in scene.");
    }
}