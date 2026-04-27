using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg;
    using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight;
    using GameNetcodeStuff;
    using MoaiEnemy.src.MoaiNormal;
    using System.Threading.Tasks;
    using Unity.Netcode;
    using UnityEngine;

    public class QuantumCannon : GrabbableObject
    {
        public GameObject projectilePrefab;  // not required anymore
        public Transform core;
        public Transform firePoint;
        public float chargeTime = 2.75f;
        public float projectileForce = 50f;
        public AudioSource chargeSound;
        public AudioSource fireSound;
        public AudioSource flickSound;

        // recall mechanism
        bool inRecallMode = false;
        public GameObject recallLight;
        public AudioSource recallOn;
        public AudioSource recallOff;
        int recallCycle = 0;

        // phases
        public GameObject rotationLever;
        public GameObject phase1;
        public GameObject phase2;
        public GameObject phase3;


        // quotes
        int quoteloop = 0;
        public AudioSource q1;
        public AudioSource q2;
        public AudioSource q3;
        public AudioSource q4;
        public AudioSource q5;
        float quoteProb = 0.25f;


        private bool isCharging = false;
        private float chargeTimer = 0f;

        private bool isRotating = false;
        private Quaternion targetRotation;
        private float rotationDuration = 0.5f;
        private float rotationTimer = 0f;

        public override void Start()
        {
            base.Start();
            insertedBattery.charge = 1;

            // Ensure all phases are inactive at the start
            phase1.SetActive(false);
            phase2.SetActive(false);
            phase3.SetActive(false);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (buttonDown)
            {
                if(RoundManager.Instance.IsHost) { setChargingClientRpc(true); }
                else { setChargingServerRpc(true); }
            }
            else
            {
                if (RoundManager.Instance.IsHost) { setChargingClientRpc(false); }
                else { setChargingServerRpc(false); }
            }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            if(RoundManager.Instance.IsHost)
            {
                toggleRecallClientRpc(inRecallMode);
            }
            else
            {
                toggleRecallServerRpc();
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void toggleRecallServerRpc()
        {
            toggleRecallClientRpc(inRecallMode);
        }

        [ClientRpc]
        public void toggleRecallClientRpc(bool currentRecall)
        {
            inRecallMode = !currentRecall;

            if(inRecallMode == true)
            {
                recallOn.Play();
            }
            else
            {
                recallOff.Play();
            }
        }

        public override void Update()
        {
            base.Update();

            // recall logic
            if (inRecallMode)
            {
                recallCycle++;
                if(recallCycle > 60)
                {
                    recallCycle = 0;
                }

                if(recallCycle > 30 && !recallLight.activeInHierarchy)
                {
                    recallLight.SetActive(true);
                }
                else if(recallLight.activeInHierarchy)
                {
                    recallLight.SetActive(false);
                }
            }

            if (!RoundManager.Instance.IsHost) { return; }

            if (isCharging)
            {
                chargeTimer += Time.deltaTime;

                // Handle phase activation based on charging progress
                UpdatePhasesClientRpc(chargeTimer, chargeTime);

                // Fully charged and ready to fire
                if (chargeTimer >= chargeTime)
                {
                    FireProjectile();
                    hardResetClientRpc();
                }
            }

            // Smoothly rotate the arm if it's rotating
            if (isRotating)
            {
                RotateArmClientRpc();
            }
        }


        [ServerRpc(RequireOwnership = false)]
        public void setChargingServerRpc(bool val) {
            setChargingClientRpc(val);
        }

        [ClientRpc]
        public void setChargingClientRpc(bool val)
        {
            if(val == true)
            {
                StartCharging();
            }
            else
            {
                CancelCharging();
            }
        }

        private void StartCharging()
        {
            var r = rotationLever.transform.localRotation;
            rotationLever.transform.localRotation = new Quaternion(0, r.y, r.z, r.w);
            if (insertedBattery.charge <= 0.15f)
            {
                Debug.LogWarning("No battery charge available!");
                return;
            }

            if (!isCharging)
            {
                isCharging = true;
                chargeTimer = 0f;

                // Play charging sound
                if (chargeSound)
                {
                    chargeSound.Play();
                }
            }
        }

        private void CancelCharging()
        {
            if (isCharging)
            {
                ResetCharge();
            }
        }

        [ClientRpc]
        public void hardResetClientRpc()
        {
            ResetCharge();
        }

        private void ResetCharge()
        {
            isCharging = false;
            chargeTimer = 0f;
            chargeSound.Stop();

            // Deactivate all phases
            phase1.SetActive(false);
            phase2.SetActive(false);
            phase3.SetActive(false);

            var r = rotationLever.transform.localRotation;
            rotationLever.transform.localRotation = new Quaternion(0, r.y, r.z, r.w);

            if(rotationLever.transform.localRotation.x < -45)
            {
                flickSound.Play();
            }
        }


        [ClientRpc]
        public void quoteClientRpc(int delay)
        {
            quote(delay);
        }

        public async void quote(int delay)
        {
            await Task.Delay(delay);

            switch (quoteloop)
            {
                case 0:
                    q1.Play();
                    break;
                case 1:
                    q2.Play();
                    break;
                case 2:
                    q3.Play();
                    break;
                case 3:
                    q4.Play();
                    break;
                case 4:
                    q5.Play();
                    break;
            }

            quoteloop++;

            if (quoteloop >= 5)
            {
                quoteloop = 0;
            }
        }

        [ClientRpc]
        private void UpdatePhasesClientRpc(float chargeTimerInj, float chargeTimeInj)
        {
            if (chargeTimerInj >= chargeTimeInj * 0.22f && chargeTimerInj < chargeTimeInj * 0.44f)
            {
                phase1.SetActive(true);
                phase2.SetActive(false);
                phase3.SetActive(false);
            }
            else if (chargeTimerInj >= chargeTimeInj * 0.6f && chargeTimerInj < chargeTimeInj)
            {
                phase1.SetActive(true);
                phase2.SetActive(true);
                phase3.SetActive(false);
            }
            else
            {
                phase1.SetActive(true);
                phase2.SetActive(true);
                phase3.SetActive(true);
            }
        }

        private void FireProjectile()
        {
            if(!RoundManager.Instance.IsHost) { return; }
            if (!firePoint || !Plugin.CannonBall)
            {
                Debug.LogError("FirePoint or ProjectilePrefab is not assigned!");
                return;
            }

            sharedFireLogicClientRpc();

            // Calculate the direction from Core to FirePoint
            Vector3 fireDirection = (firePoint.position - core.position).normalized;
            Quaternion fireRotation = Quaternion.LookRotation(fireDirection);

            // Spawn the projectile
            GameObject projectile = UnityEngine.Object.Instantiate(Plugin.CannonBall, firePoint.position, fireRotation);
            projectile.SetActive(true);
            projectile.GetComponent<NetworkObject>().Spawn();
            projectile.GetComponent<PlasmaBall>().owner = this.GetInstanceID();

            if (!CompanyFightScript.hostile)
            {
                if (Random.Range(0f, 1f) < quoteProb)
                {
                    quoteClientRpc(Random.RandomRangeInt(2000, 15000));
                    quoteProb = 0.25f;
                }
                else
                {
                    quoteProb += 0.1f;
                }
            }
        }

        [ClientRpc]
        public void sharedFireLogicClientRpc()
        {
            // Set the target rotation for the arm
            targetRotation = Quaternion.Euler(-90, rotationLever.transform.localRotation.y, rotationLever.transform.localRotation.z);
            isRotating = true;
            rotationTimer = 0f;

            // Play firing sound
            if (fireSound)
            {
                fireSound.Play();
            }

            // Drain battery charge
            insertedBattery.charge -= 0.2f;

            // Spawn explosion effect
            Landmine.SpawnExplosion(firePoint.position, false, 0, 0, 0, 17f);

            Debug.Log("Fired projectile!");

            // Reset phases
            phase1.SetActive(false);
            phase2.SetActive(false);
            phase3.SetActive(false);
        }

        [ClientRpc]
        private void RotateArmClientRpc()
        {
            rotationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(rotationTimer / rotationDuration); // Normalize time

            // Smoothly interpolate the rotation
            rotationLever.transform.localRotation = Quaternion.Slerp(rotationLever.transform.localRotation, targetRotation, t);

            // Stop rotation after reaching the target
            if (t >= 1f)
            {
                isRotating = false;
            }
        }
    }
}
