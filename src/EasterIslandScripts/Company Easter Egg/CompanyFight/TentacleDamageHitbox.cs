using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.UI.GridLayoutGroup;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    public class TentacleDamageHitbox : NetworkBehaviour
    {

        public List<GameObject> collidedOBJS;

        public void Start()
        {
            collidedOBJS = new List<GameObject>();
        }

        public void clear()
        {
            collidedOBJS.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            //if (!RoundManager.Instance.IsHost)
            //{
            //    return;
            //}

            var hit = other.gameObject;

            if (hit)
            {
                if (collidedOBJS.Contains(hit)) { return; }

                // Log and handle trigger collision
                //Debug.Log($"Company Tentacle: {hit.name}");

                var enemyAI = enemyParentWalk(hit);
                if (enemyAI)
                {
                    // Damage enemy AI
                    //Debug.Log($"Tentacle Hit Enemy: {enemyAI.name}");
                    enemyAI.HitEnemy(8, null, true);
                }

                var player = playerParentWalk(hit);
                if (player)
                {
                    // Damage player
                    //Debug.Log($"PlasmaBall triggered player: {player.playerUsername}");
                    player.DamagePlayer(50);
                }

                collidedOBJS.Add(hit);
            }
        }

        // goes up the parent tree until it finds player or null
        public PlayerControllerB playerParentWalk(GameObject leaf)
        {
            while (leaf != null && leaf.GetComponent<PlayerControllerB>() == null)
            {
                if (leaf.transform.parent && leaf.transform.parent.gameObject)
                {
                    leaf = leaf.transform.parent.gameObject;
                }
                else
                {
                    leaf = null;
                }
            }

            if (leaf && leaf.GetComponent<PlayerControllerB>())
            {
                return leaf.GetComponent<PlayerControllerB>();
            }

            return null;
        }

        // goes up the enemy tree until it finds player or null
        public EnemyAI enemyParentWalk(GameObject leaf)
        {
            while (leaf != null && leaf.GetComponent<EnemyAI>() == null)
            {
                if (leaf.transform.parent && leaf.transform.parent.gameObject)
                {
                    leaf = leaf.transform.parent.gameObject;
                }
                else
                {
                    leaf = null;
                }
            }

            if (leaf && leaf.GetComponent<EnemyAI>())
            {
                return leaf.GetComponent<EnemyAI>();
            }

            return null;
        }
    }
}
