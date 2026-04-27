using GameNetcodeStuff;
using System;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class TechRadarItem : GrabbableObject
{
    private AudioSource src;
    public AudioClip pingClip;
    public AudioClip turnOffClip;
    public AudioClip outOfBatteryClip;
    public AudioClip rechargeClip;
    public AudioClip continueClip;
    public Transform blankEnd;
    private LineRenderer line;

    private bool toggle = false;

    // units are in seconds
    private float lastTimeSinceFind = 0;
    private float lastTimeSinceScan = 0;
    private float lastTimeSincePing = 0;
    private float pingdelay = 1;
    private float minDelay = 0.2f;

    private float pingDivisor = 30;
    private float batteryDrainRate = 0.11f;
    private float scanPrecision = 1f;
    private float scanSteps = 120;

    // specifically for mod compatibility
    public Material lineMaterial;

    // entity tracking
    GrabbableObject[] currentObjects;
    EnemyAI[] currentEnemies;
    EntranceTeleport[] currentTeleports;

    public override void Start()
    {
        base.Start();

        itemProperties.verticalOffset = 0.12f;  // FIX FOR MATTY FIXES

        if (!this.NetworkObject.IsSpawned && RoundManager.Instance.IsHost)
        {
            this.NetworkObject.Spawn();
        }
        else
        {
            src = GetComponent<AudioSource>();
        }
    }

    private AnimationCurve CreateConstantCurve(float yValue, float xStart, float xEnd)
    {
        // Create keyframes for the start and end points
        Keyframe startKey = new Keyframe(xStart, yValue);
        Keyframe endKey = new Keyframe(xEnd, yValue);

        // Create and return the AnimationCurve
        return new AnimationCurve(startKey, endKey);
    }


    [ServerRpc(RequireOwnership = false)]
    public void spawnLineServerRpc()
    {
        spawnLineClientRpc();
    }

    [ClientRpc]
    public void spawnLineClientRpc()
    {
        if (this.GetComponent<LineRenderer>() == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
            Vector3[] startArr = new Vector3[2];
            startArr[0] = new Vector3(0, 0, 0);
            startArr[1] = new Vector3(0, 1, 0);
            line.material = lineMaterial;
            line.useWorldSpace = true;
            line.shadowBias = 0.5f;
            line.textureScale = new Vector2(1f, 1f);
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.widthCurve = CreateConstantCurve(0.2f, 0.0f, 1.0f);
        }
        else
        {
            line = this.GetComponent<LineRenderer>();
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        src = this.GetComponent<AudioSource>();

        if (line == null)
        {
            if(RoundManager.Instance.IsHost)
            {
                spawnLineClientRpc();
            }
            else
            {
                spawnLineServerRpc();
            }
        }

        toggle = !toggle;

        if (toggle)
        {
            Debug.Log("Artifact Activate");
            src.Stop();
            src.Play();
        }
        else
        {
            Debug.Log("Artifact Deactivated");
            line.SetPosition(1, this.transform.position);
            src.Stop();
            src.PlayOneShot(turnOffClip);
        }
    }

    public override void UseUpBatteries()
    {
        Debug.Log("Artifact Use up Batteries");
        base.UseUpBatteries();
        RoundManager.Instance.PlayAudibleNoise(base.transform.position, 13f, 0.65f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
        src.Stop();
    }

    public override void PocketItem()
    {
        if (!base.IsOwner)
        {
            base.PocketItem();
            return;
        }
        base.PocketItem();
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
    }

    public override void EquipItem()
    {
        base.EquipItem();
    }

    public override void ChargeBatteries()
    {
        base.ChargeBatteries();
        src.PlayOneShot(rechargeClip);
    }

    public override void Update()
    {
        base.Update();
        itemProperties.verticalOffset = 0.12f;  // FIX FOR MATTY FIXES

        if (!src.isPlaying && toggle)
        {
            src.PlayOneShot(continueClip);
            src.PlayOneShot(continueClip);
        }

        if(line == null) { return;  }
        if(toggle!)
        {
            line.SetPosition(1, transform.position);
        }

        if(base.insertedBattery.charge <= 0 && toggle)
        {
            toggle = false;
            src.PlayOneShot(outOfBatteryClip);
        }

        RaycastHit hit;
        Vector3 dir = blankEnd.position - transform.position;
        var raycast = navmeshRaycast();
        line.SetPosition(0, this.transform.position);
        if (toggle)
        {
            base.insertedBattery.charge -= 0.02f * Time.deltaTime * batteryDrainRate;
            // shoot a laser if the object is on, while draining battery.
            // auto-disable if the battery runs out
            if(raycast != Vector3.zero)
            {
                line.SetPosition(1, raycast);

                if ((lastTimeSinceScan + 0.2) < Time.time)
                {
                    scanArea(raycast);
                    lastTimeSinceScan = Time.time;
                }

                if ((lastTimeSincePing + pingdelay) < Time.time)
                {
                    src.PlayOneShot(pingClip);
                    lastTimeSincePing = Time.time;
                }
            }
            else if (Physics.Raycast(transform.position, dir, out hit))
            {
                base.insertedBattery.charge -= 0.02f * Time.deltaTime * batteryDrainRate;
                // shoot a laser if the object is on, while draining battery.
                // auto-disable if the battery runs out
                line.SetPosition(1, hit.transform.position);

                if ((lastTimeSinceScan + 0.2) < Time.time)
                {
                    scanArea(hit.transform.position);
                    lastTimeSinceScan = Time.time;
                }

                if ((lastTimeSincePing + pingdelay) < Time.time)
                {
                    src.PlayOneShot(pingClip);
                    lastTimeSincePing = Time.time;
                }
                
            }
            else
            {
                line.SetPosition(1, blankEnd.transform.position);
                pingdelay = 100;
                GetComponent<LineRenderer>().startColor = new UnityEngine.Color(1, 1, 1, 1);
                GetComponent<LineRenderer>().endColor = new UnityEngine.Color(1, 1, 1, 1);
            }
        }
        else
        {
            line.SetPosition(0, blankEnd.transform.position);
            line.SetPosition(1, blankEnd.transform.position);
        }
    }

    public Vector3 navmeshRaycast()
    {
        Vector3 directionVector = Vector3.Normalize(blankEnd.position - transform.position);

        for(int i = 0; i < scanSteps; i++)
        {
            Vector3 scanPosition = transform.position + directionVector * (i * scanPrecision);

            NavMeshHit hit;
            if(NavMesh.SamplePosition(scanPosition, out hit, scanPrecision, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return Vector3.zero;
    }

    // scan the area for items, monsters, and exits
    // change the laser color depending on what is closest
    // scanner should be called once every 0.2 seconds to reduce lag
    public void scanArea(Vector3 position)
    {
        if ((lastTimeSinceFind + 10) < Time.time)
        {
            currentObjects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            currentEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            currentTeleports = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            lastTimeSinceFind = Time.time;
        }

        GameObject closestObject = null;
        float closestDistance = 9999f;
        string closestType = "";

        foreach(GrabbableObject obj in currentObjects)
        {
            var dist = Vector3.Distance(obj.transform.position, position);
            if (dist < closestDistance && !obj.isHeld && !obj.isInShipRoom)
            {
                if (obj.name.ToLower().Contains("gold"))
                {
                    closestDistance = dist;
                    closestType = "gold";
                    closestObject = obj.gameObject;
                }
                else
                {
                    closestDistance = dist;
                    closestType = "object";
                    closestObject = obj.gameObject;
                }
            }
        }

        foreach (EnemyAI enemy in currentEnemies)
        {
            var dist = Vector3.Distance(enemy.transform.position, position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestType = "enemy";
                closestObject = enemy.gameObject;
            }
        }

        foreach (EntranceTeleport teleport in currentTeleports)
        {
            var dist = Vector3.Distance(teleport.transform.position, position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestType = "door";
                closestObject = teleport.gameObject;
            }
        }

        Debug.Log("closestObj: " + closestObject);
        Debug.Log("closestDistance: " + closestDistance);
        Debug.Log("closestType: " + closestType);

        if (closestObject == null) { return; }

        if (closestType.Equals("gold"))
        {
            GetComponent<LineRenderer>().startColor = new UnityEngine.Color(1, 1, 0, 1);
            GetComponent<LineRenderer>().endColor = new UnityEngine.Color(1, 1, 0, 1);
        }

        if (closestType.Equals("object"))
        {
            GetComponent<LineRenderer>().startColor = new UnityEngine.Color(0, 1, 0, 1);
            GetComponent<LineRenderer>().endColor = new UnityEngine.Color(0, 1, 0, 1);
        }

        if (closestType.Equals("enemy"))
        {
            GetComponent<LineRenderer>().startColor = new UnityEngine.Color(1, 0, 0, 1);
            GetComponent<LineRenderer>().endColor = new UnityEngine.Color(1, 0, 0, 1);
        }

        if (closestType.Equals("door"))
        {
            GetComponent<LineRenderer>().startColor = new UnityEngine.Color(0, 0, 1, 1);
            GetComponent<LineRenderer>().endColor = new UnityEngine.Color(0, 0, 1, 1);
        }

        pingdelay = closestDistance / pingDivisor + minDelay;
    }
}
