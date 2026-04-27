
using LethalLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.VirtualTexturing;

public class SpewerSpawner : MonoBehaviour
{
    private GameObject controller;
    int checkLoop = 75;

    // Start is called before the first frame update
    void Start()
    {
        // Adding logging to help debug the issue
        //Debug.Log("SpewerSpawner Start called");

        if (RoundManager.Instance == null)
        {
            //Debug.LogError("RoundManager.Instance is null in SpewerSpawner Start");
            return;
        }

        if (RoundManager.Instance.IsHost)
        {
            Debug.Log("IsHost is true, spawning EruptionController");
            controller = UnityEngine.Object.Instantiate(EasterIsland.Plugin.EruptionController, this.transform.position, Quaternion.Euler(Vector3.zero));
            controller.SetActive(value: true);

            NetworkObject networkObject = controller.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                Debug.Log("NetworkObject spawned successfully");
            }
            else
            {
                //Debug.LogError("NetworkObject component not found on EruptionController");
            }
        }
        else
        {
            //Debug.Log("IsHost is false, not spawning EruptionController");
        }
    }

    void Update()
    {
        if(checkLoop > 0)
        {
            checkLoop--;
        }
        else
        {
            checkLoop = 75;
            if (!controller)
            {
                Start();
            }
        }
    }
}