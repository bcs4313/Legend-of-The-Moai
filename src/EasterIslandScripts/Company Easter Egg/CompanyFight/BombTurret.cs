using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using On;
using IL;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight;

public class BombTurret : NetworkBehaviour
{
    public GameObject root;
    public GameObject rotator;
    private float rotationSpeed = 5f;


    // controls if the weapon does anything
    public bool hostile = false;

    bool hasDoneWarning = false;
    public AudioSource warningAudioSource;
    public AudioSource fireAudioSource;

    // missile firing mechanics
    private float fireInterval = 0.25f;  // delay between shots in burst
    private float burstInterval = 15f;  // delay between bursts
    private int burstAmount = 16;   // number of missiles per burst
    private float burstTimer = 0; // time counter
    private float fireTimer = 0; // time counter

    // properties to place ON the missile
    float missileWarble = 0.1f;  // randomness of fired missiles
    float missileSpeed = 0.3f;


    private float targetChangeInterval = 15f;
    private float targetTimer = 0;

    private PlayerControllerB targetPlayer;

    public GameObject missilePrefab;
    public Transform firePoint1;
    public Transform firePoint2;
    private bool useP1 = false;

    // Called when this object is spawned across the network
    public void Start()
    {
        // Only let the server handle targeting/firing logic
        if (!RoundManager.Instance.IsHost) return;

        burstInterval = 7;

        selectTarget();
    }

    private void Update()
    {
        if (!hostile && warningAudioSource.isPlaying) { warningAudioSource.Stop(); }

        // Ensure only the server runs the turret logic
        if (!RoundManager.Instance.IsHost) return;

        if (!hostile) return;

        if (targetPlayer != null)
        {
            // Rotate the rotator object to face the target player
            RotateTowardTarget();

            burstTimer += Time.deltaTime;

            // warning of burst
            if(burstTimer >= burstInterval-4.3f && !hasDoneWarning)
            {
                warnPlayerClientRpc();
                hasDoneWarning = true;
            }

            if (burstTimer >= burstInterval)
            {
                if (burstAmount > 0)
                {
                    // firing during burst
                    fireTimer += Time.deltaTime;
                    if (fireTimer >= fireInterval)
                    {
                        fireTimer = 0f;
                        FireBomb();
                    }
                }
                else
                {
                    burstInterval = Random.Range(40, 60);
                    burstTimer = 0;
                    burstAmount = 20;
                    hasDoneWarning = false;
                }
            }


            // target changing
            targetTimer += Time.deltaTime;
            if (targetTimer >= targetChangeInterval)
            {
                selectTarget();
                targetChangeInterval = Random.Range(3, 30);
            }
        }
    }

    [ClientRpc]
    public void warnPlayerClientRpc()
    {
        warningAudioSource.Play();
    }

    private void selectTarget()
    {
        var players = RoundManager.Instance.playersManager.allPlayerScripts;

        List<PlayerControllerB> validPlayers = new List<PlayerControllerB>();
        if (players.Length > 0)
        {
            foreach(var player in players)
            {
                if(player.isPlayerControlled)
                {
                    validPlayers.Add(player);
                }
            }
            targetPlayer = validPlayers[Random.Range(0, validPlayers.Count)]; // Target the first player found
        }
    }

    private void RotateTowardTarget()
    {
        // Direction from turret's rotator to the player
        Vector3 direction = targetPlayer.transform.position - rotator.transform.position;

        // (Remove `direction.y = 0f;` so it can rotate up/down)
        if (direction.sqrMagnitude > 0.001f)
        {
            // Create a rotation looking along the direction to the target
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            rotator.transform.rotation = Quaternion.Slerp(
                rotator.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // You can invoke a server RPC here if you want the firing to be recognized on clients
    private void FireBomb()
    {
        if(!RoundManager.Instance.IsHost) { return; }
        if(!hostile) { return; }
        burstAmount--;
        Debug.Log("BombTurret: FireBomb");
        Transform spawnLoc = null;
        if (useP1)
        {
            spawnLoc = firePoint1;
        }
        else
        {
            spawnLoc = firePoint2;
        }
        useP1 = !useP1;

        makeMissileClientRpc(spawnLoc.transform.position);
    }
    
    [ClientRpc]
    public void makeMissileClientRpc(Vector3 pos)
    {
        GameObject gameObject = Object.Instantiate<GameObject>(missilePrefab, pos, this.rotator.transform.rotation);
        var missile = gameObject.GetComponent<CompanyMissile>();
        missile.setMissileSpeed(missileSpeed);
        missile.setMissileWarble(missileWarble);
        fireAudioSource.Play();
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
    }
}