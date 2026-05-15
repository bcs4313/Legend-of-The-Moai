using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    using System.Threading.Tasks;
    using Unity.Netcode;
    using UnityEngine;

    public class CannonConstructor : NetworkBehaviour
    {
        int itemIDMatch = 34873823;  // REQUIRED FOR THIS TO WORK
        public Transform quantumCannonSpawnPoint;

        public NetworkObject netObjSelf;

        public GameObject goldRef;  // sacrifice 1
        public GameObject artifactRef;  // sacrifice 2

        public GameObject phase0;
        public GameObject phase1;
        public GameObject phase2;
        public GameObject phase3;
        public GameObject burst;
        public GameObject cut;
        public GameObject explosion;

        float tUpdate = 0;

        protected bool inCutscene = false;

        public void Start()
        {
            Debug.Log("CAVE INIT");

            // Only the server spawns the NetworkObject
            if (RoundManager.Instance.IsHost && !netObjSelf.IsSpawned)
            {
                Debug.Log("IsServer=>spawning object for client");
                netObjSelf.Spawn();
            }

            if (RoundManager.Instance.IsHost)
            {
                setPhase0ClientRpc(false); // light 1
                setPhase1ClientRpc(false); // light 2
                setPhase2ClientRpc(false); // light 3
                setPhase3ClientRpc(false); // full ethereal blast on the pedestal
                setBurstClientRpc(false);
                setExplosionClientRpc(false);
                setCutClientRpc(false);
            }
        }

        public void Update()
        {
            if(!inCutscene && Time.time > tUpdate + 3f)
            {
                setPhase0ClientRpc(false); // light 1
                setPhase1ClientRpc(false); // light 2
                setPhase2ClientRpc(false); // light 3
                setPhase3ClientRpc(false); // full ethereal blast on the pedestal
                setBurstClientRpc(false);
                setExplosionClientRpc(false);
                setCutClientRpc(false);
                tUpdate = Time.time;
            }
        }

        public void startCutscene()
        {
            if (RoundManager.Instance.IsHost)
            {
                if (goldRef.activeInHierarchy && artifactRef.activeInHierarchy)
                {
                    beginCutscene();
                }
            }
            else
            {
                startCutsceneServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void startCutsceneServerRpc()
        {
            if (goldRef.activeInHierarchy && artifactRef.activeInHierarchy)
            {
                beginCutscene();
            }
        }

        [ClientRpc]
        public void setCutsceneClientRpc(bool value)
        {
            inCutscene = value;
        }

        public async void beginCutscene()
        {
            if (inCutscene) { return; }
            if (RoundManager.Instance.IsHost) { setCutsceneClientRpc(true); }
            if (!RoundManager.Instance.IsHost) { return; }

            await Task.Delay(1000);  // 1 sec wait

            // material extractor
            setPhase0ClientRpc(true);
            await Task.Delay(4000);  // 4 second wait

            // lights
            setPhase1ClientRpc(true);
            await Task.Delay(1000);  // 1 second wait
            setPhase2ClientRpc(true);
            await Task.Delay(1000);  // 1 second wait
            setPhase3ClientRpc(true);
            await Task.Delay(1000);  // 1 second wait
            setBurstClientRpc(true);
            await Task.Delay(3700);  // 3.7 second wait

            // turn off blast and extraction process
            setBurstClientRpc(false);
            setPhase0ClientRpc(false);

            setCutClientRpc(true);
            setGoldRefClientRpc(false);
            setArtifactRefClientRpc(false);
            await Task.Delay(550);  // 3.7 second wait
            setExplosionClientRpc(true);

            // create the quantum cannon
            GameObject projectile = UnityEngine.Object.Instantiate(Plugin.PlasmaCannonPrefab, quantumCannonSpawnPoint.position, Plugin.PlasmaCannonPrefab.transform.rotation);
            projectile.SetActive(true);
            projectile.GetComponent<NetworkObject>().Spawn();

            // turning off lights
            await Task.Delay(1000);  // 1 second wait
            setPhase3ClientRpc(false);
            await Task.Delay(1000);  // 1 second wait
            setPhase2ClientRpc(false);
            await Task.Delay(1000);  // 1 second wait
            setPhase1ClientRpc(false);

            await Task.Delay(4000);
            setExplosionClientRpc(false);
            setCutClientRpc(false); ;
            //inCutscene = false; in no situation should this cutscene occur twice
        }


        [ClientRpc]
        public void setArtifactRefClientRpc(bool active)
        {
            artifactRef.SetActive(active);
        }


        [ClientRpc]
        public void setGoldRefClientRpc(bool active)
        {
            goldRef.SetActive(active);
        }


        [ClientRpc]
        public void setPhase0ClientRpc(bool active)
        {
            phase0.SetActive(active);
        }


        [ClientRpc]
        public void setPhase1ClientRpc(bool active)
        {
            phase1.SetActive(active);
        }


        [ClientRpc]
        public void setPhase2ClientRpc(bool active)
        {
            phase2.SetActive(active);
        }


        [ClientRpc]
        public void setPhase3ClientRpc(bool active)
        {
            phase3.SetActive(active);
        }


        [ClientRpc]
        public void setBurstClientRpc(bool active)
        {
            burst.SetActive(active);
        }


        [ClientRpc]
        public void setCutClientRpc(bool active)
        {
            cut.SetActive(active);
        }

        [ClientRpc]
        public void setExplosionClientRpc(bool active)
        {
            explosion.SetActive(active);
        }
    }
}
