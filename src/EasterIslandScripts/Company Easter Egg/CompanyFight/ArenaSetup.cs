using EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg;
using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    // responsible for setting up the company fight and
    // also hooking / checking when to set it up.
    public class ArenaSetup
    {
        public static GameObject shatteredWallPrefab;
        public static GameObject wall;

        // hook link
        public static void Start()
        {
            // load wall asset
            shatteredWallPrefab = Plugin.easterislandBundle.LoadAsset<GameObject>("CompanyWall");

            // register as net prefab
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(shatteredWallPrefab);

            On.RoundManager.LoadNewLevel += (On.RoundManager.orig_LoadNewLevel orig, global::RoundManager self, int randomSeed, global::SelectableLevel newLevel) =>
            {
                orig.Invoke(self, randomSeed, newLevel);
                CompanyFightScript.hostile = false;
            };

            // hangar door open hook
            // I assume this is called on client. It might not.
            On.HangarShipDoor.SetDoorOpen += (On.HangarShipDoor.orig_SetDoorOpen orig, global::HangarShipDoor self) =>
            {
                orig.Invoke(self);

                if(qualifyForArena() && wall == null)
                {
                    Plugin.Logger.LogDebug("Legend of The Moai: Company Fight Pre Requisites Met. Setting up Arena.");

                    // replace the original wall!
                    var tempWall = getWall();

                    if (RoundManager.Instance.IsHost)
                    {
                        wall = UnityEngine.Object.Instantiate(shatteredWallPrefab);
                        wall.transform.position = new Vector3(0.310000002f, 0.899999976f, 1013.91998f);  // dumb? yes. It does work though
                    }

                    GameObject.Destroy(tempWall);

                    sendMsg();

                    // Remove lights, they will be replaced too!
                    if (GameObject.Find("LEDHangingLight (3)"))
                    {
                        GameObject.Destroy(GameObject.Find("LEDHangingLight (3)"));
                    }
                    if (GameObject.Find("LEDHangingLight (4)"))
                    {
                        GameObject.Destroy(GameObject.Find("LEDHangingLight (4)"));
                    }
                    if (GameObject.Find("LEDHangingLight (2)"))
                    {
                        GameObject.Destroy(GameObject.Find("LEDHangingLight (2)"));
                    }

                    // Remove Cube Counter
                    //if (GameObject.Find("Cube"))
                    //{
                    //    GameObject.Destroy(GameObject.Find("Cube"));
                    //}

                    // Remove Cube Counter
                    //if (GameObject.Find("Canvas"))
                    //{
                    //    GameObject.Destroy(GameObject.Find("Canvas"));
                    //}

                    // Remove Deposit Counter
                    //if (GameObject.Find("DepositCounter"))
                    //{
                    //    GameObject.Destroy(GameObject.Find("DepositCounter"));
                    //}

                    // Remove Bell Dinger
                    //if (GameObject.Find("BellDinger"))
                    //{
                    //    GameObject.Destroy(GameObject.Find("BellDinger"));
                    //}

                }
            };
        }


        public static async void sendMsg()
        {
            await Task.Delay(5000);
            HUDManager.Instance.DisplayTip("WARNING", "Anomalous Weapon Detected. You are to remain an asset to the company. Ignoring such a warning risks termination.", true);
        }

        // check to see if pre req for arena is applicable
        // 1. Must be on Gordion
        // 2. Quantum Cannon is present
        // 3. Company wall is available (not deleted yet)
        public static bool qualifyForArena()
        {
            var m = RoundManager.Instance;

            if (m.currentLevel.PlanetName.ToLower().Contains("gordion") && hasQuantumCannon() && getWall())
            {
                return true;
            }

            return false;
        }

        public static bool hasQuantumCannon()
        {
            var cannons = UnityEngine.Object.FindObjectsOfType<QuantumCannon>();
            if(cannons.Length > 0)
            {
                return true;
            }

            return false;
        }

        public static GameObject getWall()
        {
            var planet = GameObject.Find("CompanyPlanet");

            if(planet)
            {
                return GameObject.Find("Cube.003");
            }

            return null;
        }

        public static void StartArena()
        {

        }
    }
}
