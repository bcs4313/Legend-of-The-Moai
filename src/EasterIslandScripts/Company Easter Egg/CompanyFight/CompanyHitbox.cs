using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    public class CompanyHitbox : MonoBehaviour, IHittable
    {
        public CompanyFightScript fightScript;

        //public List<GameObject> collidedOBJS;

        
        public void Start()
        {
            //collidedOBJS = new List<GameObject>();
        }

        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            if(playerWhoHit == null) { return false; }
            if(!CompanyFightScript.hostile) { return false; }
            if (!RoundManager.Instance.IsHost) { fightScript.takeDamageServerRpc(force); }
            else
            {
                fightScript.takeDamage(force);
            }
            return true;
        }

        /**
        public void OnTriggerEnter(Collider other)
        {
            // make sure the object is a plasma ball
            Debug.Log("LegendOfTheMoai: Company Trigger Enter: " + other.name);
            if (!collidedOBJS.Contains(other.gameObject))
            {
                if (other.gameObject.name.ToLower().Contains("plasma") && other.gameObject.name.ToLower().Contains("01") && other.gameObject.name.ToLower().Contains("ball"))
                {
                    fightScript.takeDamage(5);

                    if(collidedOBJS.Count > 10)
                    {
                        collidedOBJS.RemoveRange(0, 3);  // remove 3 first-in GameObjects
                    }

                    collidedOBJS.Add(other.gameObject);
                }
            }
        }

        public void OnColliderEnter(Collider other)
        {
            // make sure the object is a plasma ball
            Debug.Log("LegendOfTheMoai: Company Collider Enter: " + other.name);
            if (!collidedOBJS.Contains(other.gameObject))
            {
                if (other.gameObject.name.ToLower().Contains("plasma") && other.gameObject.name.ToLower().Contains("01") && other.gameObject.name.ToLower().Contains("ball"))
                {
                    fightScript.takeDamage(5);

                    if (collidedOBJS.Count > 10)
                    {
                        collidedOBJS.RemoveRange(0, 3);  // remove 3 first-in GameObjects
                    }

                    collidedOBJS.Add(other.gameObject);
                }
            }
        }
        **/
    }
}
