using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    using System;
    using GameNetcodeStuff;
    using UnityEngine;
    using static UnityEngine.SendMouseEvents;

    // rip from radmech missile
    public class CompanyMissile : MonoBehaviour
    {
        protected float missileSpeed = 0.2f;

        private float currentMissileSpeed = 0.35f;

        private bool hitWall = true;

        private float despawnTimer;

        private System.Random missileFlyRandom;

        private float forwardDistance;

        private float lastRotationDistance;

        protected float missileWarbleLevel = 0.73f;

        static int missilesFired = 0;

        private void Start()
        {
            missileFlyRandom = new System.Random((int)(base.transform.position.x + base.transform.position.y) + missilesFired);
            hitWall = false;
            missilesFired++;
        }

        public void setMissileWarble(float val)
        {
            missileWarbleLevel = val;
        }

        public void setMissileSpeed(float val)
        {
            missileSpeed = val;
        }

        private void FixedUpdate()
        {
            if (hitWall)
            {
                return;
            }
            if (despawnTimer < 5f)
            {
                despawnTimer += Time.deltaTime;
                CheckCollision();
                base.transform.position += base.transform.forward * missileSpeed * currentMissileSpeed;
                forwardDistance += missileSpeed * currentMissileSpeed;
                if (forwardDistance - lastRotationDistance > 2f)
                {
                    lastRotationDistance = forwardDistance;
                    base.transform.rotation *= Quaternion.Euler(new Vector3(15f * missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0), 7f * missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0), 15f * missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0)));
                }
                currentMissileSpeed += 0.05f;
            }
            else
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        private void CheckCollision()
        {
            int defaultLayerMask = LayerMask.GetMask("Default");
            if (!Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, 0.6f * currentMissileSpeed, defaultLayerMask, QueryTriggerInteraction.Collide))
            {
                return;
            }
            if (hitInfo.collider.gameObject.layer == 19)
            {
                EnemyAICollisionDetect component = hitInfo.collider.GetComponent<EnemyAICollisionDetect>();
                if (component != null)
                {
                    return;
                }
            }
            bool calledByClient = false;
            if (hitInfo.collider.gameObject.layer == 3)
            {
                PlayerControllerB component2 = hitInfo.collider.gameObject.GetComponent<PlayerControllerB>();
                if (component2 != null && component2 == GameNetworkManager.Instance.localPlayerController)
                {
                    calledByClient = true;
                }
            }
            hitWall = true;

            Landmine.SpawnExplosion(base.transform.position - base.transform.forward * 0.5f, true, 4f, 7f, 15, 30, null, true);
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

}
