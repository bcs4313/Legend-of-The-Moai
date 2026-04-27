using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    public class TurretSpawner : MonoBehaviour
    {
        public void Start()
        {
            if (RoundManager.Instance.IsHost) { awaitSpawn(); }
            else { Destroy(this.gameObject); }
        }

        public async void awaitSpawn()
        {
            await Task.Delay(6000);
            GetComponent<NetworkObject>().Spawn();
        }
    }
}
