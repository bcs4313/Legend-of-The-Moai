using EasterIsland;
using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(InteractTrigger))]
public class EasterDoorLock : NetworkBehaviour
{
    private InteractTrigger doorTrigger;

    public float maxTimeLeft = 60f;

    public float lockPickTimeLeft = 60f;

    public bool isLocked;

    public bool isPickingLock;

    public bool demandMet = false;

    [Space(5f)]
    public EasterDoorLock twinDoor;

    public Transform lockPickerPosition;

    public Transform lockPickerPosition2;

    public string requestedObjectID = "????";

    private float enemyDoorMeter;

    public GameObject paintingAnchorNode;

    private bool isDoorOpened;

    private NavMeshObstacle navMeshObstacle;

    public AudioClip pickingLockSFX;

    public AudioClip unlockSFX;

    public AudioSource doorLockSFX;

    private bool displayedLockTip;

    private bool localPlayerPickingLock;

    private int playersPickingDoor;

    private float playerPickingLockProgress;

    // client based item spawn validation
    private float validationPeriod = 5f;

    // value sync
    String storedItemName;
    Vector3 storedRotOffset = Vector3.zero;

    bool presentItem = false;

    public void Awake()
    {
        doorTrigger = base.gameObject.GetComponent<InteractTrigger>();
        lockPickTimeLeft = maxTimeLeft;
        navMeshObstacle = GetComponent<NavMeshObstacle>();
        LockDoor();
        spawnObj();
    }

    public async void spawnObj()
    {

        if (RoundManager.Instance.IsHost)
        {
            while (RoundManager.Instance.dungeonIsGenerating == true)
            {
                Debug.Log($"Easter Door Lock: Awaiting for level to start... 1");
                await Task.Delay(1000);
            }
            while (RoundManager.Instance.dungeonCompletedGenerating == false)
            {
                Debug.Log($"Easter Door Lock: Awaiting for level to start... 2");
                await Task.Delay(1000);
            }
            while (!StartOfRound.Instance.shipHasLanded)  // assuming 15 Scrap objects always spawn
            {
                Debug.Log($"Easter Door Lock: Awaiting for ship to land... 4");
                await Task.Delay(1000);
            }
        }

        await Task.Delay(1000);
        // duplicate deletion
        while (GameObject.Find("X8957") != null)
        {
            await Task.Delay(1000);
            GameObject.Destroy(GameObject.Find("X8957"));
        }

        // no more waiting asynchronously and checking for existing items.
        // simply just spawn a netobj
        if (RoundManager.Instance.IsHost)
        {
            loadTriggerItem();
        }
        else
        {
            clientRequestLoop();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void requestItemServerRpc(ulong requestorID)
    {
        if(storedItemName != "")
        {
            sendItemNameClientRpc(requestorID, storedItemName, storedRotOffset);
        }
    }

    [ClientRpc]
    public void sendItemNameClientRpc(ulong requestorID, string itemName, Vector3 rotOffset)
    {
        Plugin.Logger.LogInfo("Easter Island Door Lock: Setting Item : " + itemName + " with offset " + rotOffset + " for local player: " + requestorID);
        var localId = RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId;
        if (localId == requestorID)
        {
            setItem(itemName, rotOffset);
        }
    }

    public async void clientRequestLoop()
    {
        while(storedItemName == "" || storedItemName == null)
        {
            Plugin.Logger.LogInfo("Easter Island Door Lock: Awaiting for host item to be available...");
            var localId = RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId;
            requestItemServerRpc(localId);
            await Task.Delay(1000);
        }
    }

    public async void loadTriggerItem()
    {
        if (RoundManager.Instance.IsHost)
        {
            demandMet = false;

            var rnd = new System.Random();
            GrabbableObject obj = null;

            while (obj == null)
            {
                // create a duplicate grabbable object from available grabbable objects
                // make it non interactable and attach it to the painting by the door
                // make it the key to unlock the door
                string name = "";
                int highestValue = -1;
                int retryLimit = 4;

                while ((name.Equals("") || name.Contains("goldenhead") || name.ToLower().Contains("techradaritem")) && retryLimit > 0)
                {
                    retryLimit--;
                    GrabbableObject[] objs = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                    var objTemp = objs[rnd.Next(0, objs.Length)];
                    name = objTemp.gameObject.name;
                    if (objTemp.scrapValue >= highestValue && objTemp.isInFactory)
                    {
                        highestValue = objTemp.scrapValue;
                        obj = objTemp;
                        name = objTemp.gameObject.name;
                    }
                }

                if (obj != null)
                {
                    setItem(obj.itemProperties.itemName, obj.itemProperties.rotationOffset);
                }

                if(obj == null)
                {
                    Plugin.Logger.LogInfo("Easter Island Door Lock: Awaiting for trigger item object to be available...");
                }

                await Task.Delay(1500);
            }
        }
    }

    public GameObject attachItemToPainting(GameObject GO)
    {
        GrabbableObject newObj = GO.GetComponent<GrabbableObject>();
        requestedObjectID = newObj.itemProperties.itemName;
        GameObject trueObj = newObj.gameObject;
        newObj.NetworkObject.DestroyWithScene = true;
        trueObj.GetComponent<GrabbableObject>().enabled = false;
        trueObj.GetComponent<BoxCollider>().enabled = false;

        Vector3 pos = new Vector3(paintingAnchorNode.transform.position.x + 0.25f, paintingAnchorNode.transform.position.y, paintingAnchorNode.transform.position.z);
        trueObj.transform.position = pos;
        trueObj.transform.rotation = Quaternion.Euler(0, 180, 0);

        //DESTROY WITH SCENE
        if (trueObj.GetComponent<NetworkObject>())
        {
            Destroy(trueObj.GetComponent<NetworkObject>());
        }


        trueObj.name = "X8957";
        GO.name = "X8957";
        return GO;
    }

    public async void setItem(string itemName, Vector3 rotOffset)
    {
        Plugin.Logger.LogInfo("Easter Island Door Lock: Setting Item " + itemName);
        GameObject GO = null;
        try
        {
            foreach (GrabbableObject obj in Object.FindObjectsOfType<GrabbableObject>())
            {
                if (obj.itemProperties.itemName.Equals(itemName) && obj.itemProperties.rotationOffset.Equals(rotOffset))
                {
                    GO = UnityEngine.Object.Instantiate(obj.gameObject);
                    GameObject result = attachItemToPainting(GO);
                    storedItemName = obj.itemProperties.itemName;
                    storedRotOffset = obj.itemProperties.rotationOffset;
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
            await Task.Delay(1000);  // try again
            if(GO != null)
            {
                GameObject.Destroy(GO);
            }
            storedItemName = "";
            storedRotOffset = Vector3.zero;
            loadTriggerItem();
        }
    }

    public void OnHoldInteract()
    {
        if (isLocked && !displayedLockTip && HUDManager.Instance.holdFillAmount / doorTrigger.timeToHold > 0.3f)
        {
            displayedLockTip = true;
            HUDManager.Instance.DisplayTip("TIP:", "To get through locked doors efficiently, order a <u>lock-picker</u> from the ship terminal.", isWarning: false, useSave: true, "LCTip_Autopicker");
        }
    }

    // hook for attempt to unlockdoor
    public void attemptUnlockDoor(PlayerControllerB player)
    {
        GrabbableObject obj;

        if(player.currentlyHeldObject)
        {
            obj = player.currentlyHeldObject;
        }
        else
        {
            obj = player.currentlyHeldObjectServer;
        }

        if(obj == null)
        {
            Debug.Log("Easter Island: No item found to unlock door with. Doing nothing...");
            return;
        }

        if (RoundManager.Instance.IsHost)
        {
            attemptUnlockDoorAction(player, obj.itemProperties.itemName);
        }
        else
        {
            attemptUnlockDoorServerRpc(player.NetworkObject.NetworkObjectId, obj.itemProperties.itemName);
        }
    }

    // makes it so only the server needs to synchronize the spawned object
    // actual check for object id
    public void attemptUnlockDoorAction(PlayerControllerB player, string objId)
    {
        if (isLocked)
        {
            if (objId == requestedObjectID)
            {
                if (RoundManager.Instance.IsHost)
                {
                    UnlockDoorClientRpc(player.NetworkObject.NetworkObjectId);
                    demandMet = true;
                }
                else
                {
                    demandMet = true;
                    UnlockDoorServerRpc(player.NetworkObject.NetworkObjectId);
                }
            }
            else
            {
                if (RoundManager.Instance.IsHost)
                {
                    playDoorLockSFXClientRpc();
                }
                else
                {
                    playDoorLockSFXServerRpc();
                }
            }
        }
    }

    // rpc receiver for attemptUnlockDoor->attemptUnlockDoorAction
    [ServerRpc(RequireOwnership = false)]
    public void attemptUnlockDoorServerRpc(ulong playerId, string objId)
    {
        PlayerControllerB ply = getPlayer(playerId);
        attemptUnlockDoorAction(ply, objId);
    }

    [ClientRpc]
    public void playDoorLockSFXClientRpc()
    {
        doorLockSFX.Play();
    }

    [ServerRpc]
    public void playDoorLockSFXServerRpc()
    {
        playDoorLockSFXClientRpc();
    }

    public PlayerControllerB getPlayer(ulong netid)
    {
        foreach(PlayerControllerB player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if(player.NetworkObject.NetworkObjectId == netid)
            {
                return player;
            }
        }
        return null;
    }

    public void LockDoor(float timeToLockPick = 30f)
    {
        doorTrigger.timeToHold = timeToLockPick;
        doorTrigger.hoverTip = "Me no normal door. Give me what me want.";
        doorTrigger.holdTip = "Dum Dum, Gimme Somthin";
        isLocked = true;
        navMeshObstacle.carving = true;
        navMeshObstacle.carveOnlyStationary = true;
        if (twinDoor != null)
        {
            twinDoor.doorTrigger.interactable = false;
            twinDoor.doorTrigger.timeToHold = 35f;
            twinDoor.doorTrigger.hoverTip = "Locked (pickable)";
            twinDoor.doorTrigger.holdTip = "Picking lock";
            twinDoor.isLocked = true;
        }
    }

    public void UnlockDoor(ulong netid)
    {

        doorLockSFX.Stop();
        doorLockSFX.PlayOneShot(unlockSFX);
        navMeshObstacle.carving = false;
        if (isLocked && demandMet)
        {
            doorTrigger.hoverTip = "Use door : [LMB]";
            doorTrigger.holdTip = "";
            isPickingLock = false;
            isLocked = false;
            doorTrigger.timeToHoldSpeedMultiplier = 1f;
            navMeshObstacle.carving = false;
            Debug.Log("Unlocking door");
            doorTrigger.timeToHold = 0.3f;

            var player = getPlayer(netid);
            if (!player)
            {
                Debug.LogError("Easter Island Door Error: failure to retrieve player id " + netid + " ::");
                return;
            }

            GetComponent<AnimatedObjectTrigger>().TriggerAnimation(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnlockDoorServerRpc(ulong netid)
    {
        var player = getPlayer(netid);
        if(!player)
        {
            Debug.LogError("Easter Island Door Error: failure to retrieve player id " + netid + " ::");
            return;
        }

        UnlockDoorClientRpc(player.NetworkObject.NetworkObjectId);
    }

    [ClientRpc]
    public void UnlockDoorClientRpc(ulong netid)
    {
        demandMet = true;
        var player = getPlayer(netid);
        if (!player)
        {
            Debug.LogError("Easter Island Door Error: failure to retrieve player id " + netid + " ::");
            return;
        }

        UnlockDoor(player.NetworkObject.NetworkObjectId);
    }

    private void Update()
    {
        if (isLocked)
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }
            if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemId == 14)
            {
                if (StartOfRound.Instance.localPlayerUsingController)
                {
                    doorTrigger.disabledHoverTip = "Me no want key -_- give me somethin else";
                }
                else
                {
                    doorTrigger.disabledHoverTip = "Me no want key -_- give me somethin else";
                }
            }
            else
            {
                doorTrigger.disabledHoverTip = "Me no normal door. Give me what me want.";
            }
        }
        else
        {
            navMeshObstacle.carving = false;
        }
        if (isLocked && isPickingLock)
        {
            doorTrigger.disabledHoverTip = $"It seems stuck...";
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (NetworkManager.Singleton == null || !base.IsServer || isLocked || isDoorOpened || !other.CompareTag("Enemy"))
        {
            return;
        }
        EnemyAICollisionDetect component = other.GetComponent<EnemyAICollisionDetect>();
        if (!(component == null))
        {
            enemyDoorMeter += Time.deltaTime * component.mainScript.openDoorSpeedMultiplier;
            if (enemyDoorMeter > 1f)
            {
                enemyDoorMeter = 0f;
                base.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(component.mainScript.useSecondaryAudiosOnAnimatedObjects, overrideBool: true);
                OpenDoorAsEnemyServerRpc();
            }
        }
    }

    public void OpenOrCloseDoor(PlayerControllerB playerWhoTriggered)
    {
        AnimatedObjectTrigger component = base.gameObject.GetComponent<AnimatedObjectTrigger>();
        component.TriggerAnimation(playerWhoTriggered);
        isDoorOpened = component.boolValue;
        navMeshObstacle.enabled = !component.boolValue;
    }

    public void SetDoorAsOpen(bool isOpen)
    {
        isDoorOpened = isOpen;
        navMeshObstacle.enabled = !isOpen;
    }

    public void OpenDoorAsEnemy()
    {
        isDoorOpened = true;
        navMeshObstacle.enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void OpenDoorAsEnemyServerRpc()
    {
        OpenDoorAsEnemyClientRpc();
    }

    [ClientRpc]
    public void OpenDoorAsEnemyClientRpc()
    {
        OpenDoorAsEnemy();
    }

    public void TryPickingLock()
    {
        if (isLocked)
        {
            HUDManager.Instance.holdFillAmount = playerPickingLockProgress;
            if (!localPlayerPickingLock)
            {
                localPlayerPickingLock = true;
                PlayerPickLockServerRpc();
            }
        }
    }

    public void StopPickingLock()
    {
        if (localPlayerPickingLock)
        {
            localPlayerPickingLock = false;
            if (playersPickingDoor == 1)
            {
                playerPickingLockProgress = Mathf.Clamp(playerPickingLockProgress - 1f, 0f, 45f);
            }
            PlayerStopPickingLockServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerStopPickingLockServerRpc()
    {
        PlayerStopPickingLockClientRpc();
    }

    [ClientRpc]
    public void PlayerStopPickingLockClientRpc()
    {
        doorLockSFX.Stop();
        playersPickingDoor = Mathf.Clamp(playersPickingDoor - 1, 0, 4);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerPickLockServerRpc()
    {
        PlayerPickLockClientRpc();
    }

    [ClientRpc]
    public void PlayerPickLockClientRpc()
    {
        doorLockSFX.clip = pickingLockSFX;
        doorLockSFX.Play();
        playersPickingDoor = Mathf.Clamp(playersPickingDoor + 1, 0, 4);
    }
}
