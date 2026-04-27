using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasterIsland.src.EasterIslandScripts.Technical.Dynamic_Loading
{
    public class HeavenLoader
    {
        public static string sceneName = "none";
        public static AssetBundle heavenBundle;
        public static Scene loadedScene;
        public static bool isSceneLoaded = false;

        // net objs
        //public static GameObject BaseExit;
        //public static GameObject ConsoleScreen;
        //public static GameObject ExitTrigger;
        //public static GameObject LeverSwitch;
        //public static GameObject ScreenButtonLeft;
        //public static GameObject ScreenButtonRight;

        public static void LoadHeavenAssets()
        {
            try
            {
                // might not be needed honestly (STREAMED SCENE ASSETS)
                //BaseExit = heavenBundle.LoadAsset<GameObject>("BaseExit");
                //ConsoleScreen = heavenBundle.LoadAsset<GameObject>("ConsoleScreen");
                //ExitTrigger = heavenBundle.LoadAsset<GameObject>("ExitTrigger");
                //LeverSwitch = heavenBundle.LoadAsset<GameObject>("LeverSwitch");
                //ScreenButtonLeft = heavenBundle.LoadAsset<GameObject>("ScreenButtonLeft");
                //ScreenButtonRight = heavenBundle.LoadAsset<GameObject>("ScreenButtonRight");

                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BaseExit);
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(ConsoleScreen);
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(ExitTrigger);
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(LeverSwitch);
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(ScreenButtonLeft);
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(ScreenButtonRight);
            }
            catch (Exception e) {
                Debug.LogError("LegendOfTheMoai Heaven Loading Error: " + e);
            }
        }

        public static void LoadHeavenWorld()
        {
            try
            {
                if(isSceneLoaded) { return; }
                isSceneLoaded = true;
                if (heavenBundle == null)
                {
                    Debug.LogError("Legend of The Moai ERROR: Could not load heaven asset bundle!");
                    isSceneLoaded = false;
                    return;
                }

                sceneName = heavenBundle.GetAllScenePaths()[0];
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                op.completed += (op) =>
                {
                    loadedScene = SceneManager.GetSceneByPath(sceneName);
                    // Ensure all NetworkObjects are spawned

                    if (RoundManager.Instance.IsHost)
                    {
                        foreach (var netObj in GameObject.FindObjectsOfType<NetworkObject>())
                        {
                            if (netObj != null && !netObj.IsSpawned)
                                netObj.Spawn();
                        }
                    }

                    Debug.Log($"Legend of The Moai: Heaven scene loaded: {sceneName}");
                };
            }
            catch(Exception e) { Debug.LogError(e); }
        }

        public static void UnloadHeavenWorld()
        {
            try
            {
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(loadedScene).completed += (op) =>
                    {
                        // idk nothing 
                        isSceneLoaded = false;
                    };
                }
                else
                {
                    isSceneLoaded = false;
                    Debug.LogWarning("Legend of The Moai: No valid heaven scene loaded to unload.");
                }
            }
            catch(Exception e) { Debug.LogError(e); }
        }
    }
}
