using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.IO;
using BepInEx.Configuration;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalConfig;
using EasterIsland.src.EasterIslandScripts;
using System.Collections.Generic;
using EasterIsland.src;
using Unity.Netcode;
using System;
using System.Threading.Tasks;
using LethalLib.Extras;
using UnityEngine.Rendering;
using DayNightCycles;
using EasterIsland.src.EasterIslandScripts.Technical;
using GameNetcodeStuff;
using EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight;
using System.Linq;
using EasterIsland.src.EasterIslandScripts.Technical.Dynamic_Loading;
using EasterIsland.src.EasterIslandScripts.Heaven.Items;
using static LethalLib.Modules.Levels;

namespace EasterIsland
{
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig")]
    [BepInPlugin("LegendOfTheMoai", "Legend of The Moai", "1.8.6")]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony _harmony;
        public static EnemyType ExampleEnemy;
        public static new ManualLogSource Logger;

        // easter island net prefabs
        public static AssetBundle easterislandBundle;
        public static AssetBundle heavenNetBundle;
        public static GameObject EruptionController;
        public static GameObject CannonConstructor;
        public static GameObject CannonBall;
        public static GameObject DualPortal;
        int quantumPropertyIDMatch = 1144122785;

        // heaven net prefabs
        public static GameObject PartHawkWings;
        public static GameObject PartDogHead;
        public static GameObject HeavenBase;

        // As of V81, items must be registered in LethalLib or they won't be properly saved in dawnlib (moon transition bug, CRITICAL)
        public static GameObject GoldenHead;
        public static Item GoldenHeadItem;
        public static GameObject GHFPrefab;
        public static Item GHFItem;
        public static GameObject MorshuPrefab;
        public static Item MorshuItem;
        public static GameObject KingPrefab;
        public static Item KingItem;

        // eclipse volume management
        long seedSync = -1;

        // gum gum stuff haha
        public static List<PlayerControllerB> highPlayers = new List<PlayerControllerB>();
        public static GameObject EatenGumPrefab;

        // portal prefabs
        public static GameObject portalPair;

        // weathers
        public static Terminal terminal;
        public static GameObject weatherManagerAsset;
        public static GameObject weatherManager;
        public static EIWeatherManager weatherScript;

        // key mappings
        internal static InputMap controls;

        public static TerminalNode easterislandNode = null;
        public static TerminalNode easterrouteNode = null;

        public static List<GameObject> destroyOnLoad = new List<GameObject>();

        public void LogIfDebugBuild(string text)
        {
#if DEBUG
            Plugin.Logger.LogInfo(text);
#endif
        }

        private void Awake()
        {
            Logger = base.Logger;
            PopulateAssets();
            bindVars();

            // load assemblies
            bool hasWeatherRegistry = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name.ToLower().Contains("weatherregistry"));

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                }
                catch
                {
                    continue; // If type is broken due to missing assembly, skip it
                }

                foreach (var method in methods)
                {
                    object[] attributes;
                    try
                    {
                        attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    }
                    catch
                    {
                        continue; // Method itself can't be reflected due to missing type
                    }

                    if (attributes.Length == 0) continue;

                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"LegendOfTheMoai: Failed to invoke {method.Name}: {e.Message}");
                    }
                }
            }

            // harmony patch
            // affects terminal displays
            try
            {

                _harmony = new Harmony("bcs4313.legendofthemoai");
                _harmony.PatchAll();
            }
            catch (Exception e)
            {
                Debug.LogError("LegendOfTheMoai: Weather patch failure. This can cause general issues with Easter Island weather! " + e.ToString());
            }

            // load net assets
            EruptionController = easterislandBundle.LoadAsset<GameObject>("EruptionController");
            portalPair = easterislandBundle.LoadAsset<GameObject>("PortalPair");
            CannonConstructor = easterislandBundle.LoadAsset<GameObject>("CannonConstructor");
            CannonBall = easterislandBundle.LoadAsset<GameObject>("PlasmaBall01");
            DualPortal = easterislandBundle.LoadAsset<GameObject>("DualPortal");
            EatenGumPrefab = easterislandBundle.LoadAsset<GameObject>("EatenGumGum");
            PartHawkWings = heavenNetBundle.LoadAsset<GameObject>("PartHawkWings");
            PartDogHead = heavenNetBundle.LoadAsset<GameObject>("PartMouthDogHead");
            HeavenBase = heavenNetBundle.LoadAsset<GameObject>("HeavenInsideBase");

            // DawnLib item compatibilities (especially with item saving)
            try
            {
                GoldenHead = easterislandBundle.LoadAsset<GameObject>("GoldenHeadItemInside");
                GoldenHeadItem = easterislandBundle.LoadAsset<Item>("Golden Head");
                LethalLib.Modules.Items.RegisterScrap(GoldenHeadItem, 0, LevelTypes.None);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(GoldenHead);
            }
            catch (Exception e) { Debug.LogError("LegendOfTheMoai Error: " + e); }

            try
            {
                GHFPrefab = easterislandBundle.LoadAsset<GameObject>("TechRadarPrefab");
                GHFItem = easterislandBundle.LoadAsset<Item>("TechRadarItem");
                LethalLib.Modules.Items.RegisterScrap(GHFItem, 0, LevelTypes.None);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(GHFPrefab);
            }
            catch (Exception e) { Debug.LogError("LegendOfTheMoai Error: " + e); }


            try
            {
                MorshuPrefab = easterislandBundle.LoadAsset<GameObject>("MorshuPrefab");
                MorshuItem = easterislandBundle.LoadAsset<Item>("MorshuItem");
                LethalLib.Modules.Items.RegisterScrap(MorshuItem, 0, LevelTypes.None);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(MorshuPrefab);
            }
            catch (Exception e) { Debug.LogError("LegendOfTheMoai Error: " + e); }


            try
            {
                KingPrefab = easterislandBundle.LoadAsset<GameObject>("KingPrefab");
                KingItem = easterislandBundle.LoadAsset<Item>("KingItem");
                LethalLib.Modules.Items.RegisterScrap(KingItem, 0, LevelTypes.None);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(KingPrefab);
            }
            catch (Exception e) { Debug.LogError("LegendOfTheMoai Error: " + e); }


            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            // register phase 
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EruptionController);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(portalPair);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CannonConstructor);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CannonBall);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(DualPortal);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EatenGumPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PartDogHead);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PartHawkWings);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(HeavenBase);
            //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(QuantumCannon);

            // Cave piece register


            // add weather manager (Easter Island only)
            weatherManagerAsset = easterislandBundle.LoadAsset<GameObject>("EIWeatherManager");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(weatherManagerAsset);

            /*
            try
            {
                WeatherRegistryCompatibility.EnforceStableWeathers(); // for WeatherRegistry compatibility
            }
            catch (Exception e) { Debug.Log("???"); }
            */

            On.RoundManager.LoadNewLevel += (On.RoundManager.orig_LoadNewLevel orig, global::RoundManager self, int randomSeed, global::SelectableLevel newLevel) =>
            {
                orig.Invoke(self, randomSeed, newLevel);

                try
                {
                    foreach (GameObject g in destroyOnLoad)
                    {
                        if (g) { Destroy(g); }
                    }
                    QuantumItemScript.itemsTeleportedTo.Clear();
                    PortalScript.instances.Clear();

                    // change weights of easter island books to account for modded items!
                    String name = newLevel.PlanetName.ToLower();
                    if (name.Contains("easter") && name.Contains("island"))
                    {
                        try
                        {
                            Weight_Adjuster.adjustRarities(newLevel);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Easter Island Error: Failed to adjust spawn weights of critical items! " + e);
                        }
                    }
                }
                catch (Exception e) { }

                // heaven is on any moon this way!
                if (HeavenLoader.isSceneLoaded)
                {
                    HeavenLoader.UnloadHeavenWorld();
                }
            };

            On.RoundManager.DespawnPropsAtEndOfRound += (On.RoundManager.orig_DespawnPropsAtEndOfRound orig, global::RoundManager self, bool despawnAllItems) =>
            {
                orig.Invoke(self, despawnAllItems);
                try
                {
                    foreach (GameObject g in destroyOnLoad)
                    {
                        if (g) { Destroy(g); }
                    }

                    // remove heaven if it is spawned
                    if (HeavenLoader.isSceneLoaded)
                    {
                        HeavenLoader.UnloadHeavenWorld();
                    }
                }
                catch (Exception e)
                {

                }
            };

            // volume sync setup. Its dumb but it works.
            On.TimeOfDay.CalculatePlanetTime += (On.TimeOfDay.orig_CalculatePlanetTime orig, global::TimeOfDay self, global::SelectableLevel level) =>
            {
                var val = orig.Invoke(self, level);

                try
                {
                    if (StartOfRound.Instance && StartOfRound.Instance.randomMapSeed != seedSync)
                    {
                        seedSync = StartOfRound.Instance.randomMapSeed;
                        try
                        {
                            List<Volume> volumesToRemove = new List<Volume>();
                            if (DayAndNightCycle.volumesEnforced.Count > 0 && StartOfRound.Instance.currentLevel)
                            {
                                //Plugin.Logger.LogMessage("VManager:: passed check 1");
                                if (!(StartOfRound.Instance.currentLevel.PlanetName.ToLower().Contains("easter")) || (StartOfRound.Instance.currentLevel.currentWeather != LevelWeatherType.Eclipsed))
                                {
                                    //Plugin.Logger.LogMessage("VManager:: passed check 2");
                                    foreach (Volume v in DayAndNightCycle.volumesEnforced)
                                    {
                                        if (v != null)
                                        {
                                            v.enabled = true;
                                            volumesToRemove.Add(v);
                                        }
                                    }

                                    for (int i = 0; i < volumesToRemove.Count; i++)
                                    {
                                        if (DayAndNightCycle.volumesEnforced.Contains(volumesToRemove[i]))
                                        {
                                            DayAndNightCycle.volumesEnforced.Remove(volumesToRemove[i]);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
                catch (Exception e) { }

                return val;
            };

            // explosion link to portal and quantum item
            On.StunGrenadeItem.ExplodeStunGrenade += (On.StunGrenadeItem.orig_ExplodeStunGrenade orig, global::StunGrenadeItem self, bool destroy) =>
            {
                orig.Invoke(self, destroy);

                if (!RoundManager.Instance.IsServer) { return; }

                try
                {
                    foreach (PortalScript p in PortalScript.instances)
                    {
                        var g = p.gameObject;

                        if (Vector3.Distance(g.transform.position, self.transform.position) < 4f)
                        {
                            Logger.LogInfo("Moai Portal: Checking nearby objects to quantumfy");
                            // quantumfy nearby objects
                            var objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>(false);
                            for (int i = 0; i < objects.Length; i++)
                            {
                                var obj = objects[i];
                                if (Vector3.Distance(obj.transform.position, self.transform.position) < 4f && obj.isActiveAndEnabled)
                                {
                                    Logger.LogInfo("Moai Portal: Object found: " + obj.name + " ... quantumfying");
                                    if (!obj.transform.GetComponentInChildren<QuantumItemScript>())
                                    {
                                        // instantiate quantum object and add to parent
                                        var quantumObj = UnityEngine.Object.Instantiate<GameObject>(getQuantumItemRegisteredPrefab());
                                        quantumObj.GetComponent<NetworkObject>().Spawn();

                                        quantumLoop(obj, quantumObj);
                                    }
                                }
                            }
                        }
                    }

                    var quantumScripts = UnityEngine.Object.FindObjectsOfType<QuantumItemScript>();
                    foreach (QuantumItemScript q in quantumScripts)
                    {
                        var g = q.gameObject;

                        if (Vector3.Distance(g.transform.position, self.transform.position) < 4f)
                        {
                            Logger.LogInfo("Quantum Object: Checking nearby objects to quantumfy");
                            // quantumfy nearby objects
                            var objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>(false);
                            for (int i = 0; i < objects.Length; i++)
                            {
                                var obj = objects[i];
                                if (Vector3.Distance(obj.transform.position, self.transform.position) < 4f && obj.isActiveAndEnabled)
                                {
                                    Logger.LogInfo("Moai Portal: Object found: " + obj.name + " ... quantumfying");
                                    if (!obj.transform.GetComponentInChildren<QuantumItemScript>())
                                    {
                                        // instantiate quantum object and add to parent
                                        var quantumObj = UnityEngine.Object.Instantiate<GameObject>(getQuantumItemRegisteredPrefab());
                                        quantumObj.GetComponent<NetworkObject>().Spawn();

                                        quantumLoop(obj, quantumObj);
                                    }
                                }
                            }
                        }
                    }

                    var nukeScripts = UnityEngine.Object.FindObjectsOfType<NuclearBomb>();
                    foreach (var nuke in nukeScripts)
                    {
                        if (Vector3.Distance(self.transform.position, nuke.transform.position) < 7.2f)
                        {
                            nuke.dangerLevel++;
                            if (RoundManager.Instance.IsHost) { nuke.dangerResultActivateClientRpc(nuke.dangerLevel); }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            };

            // explosion link to portal and quantum item
            On.Landmine.SpawnExplosion += (On.Landmine.orig_SpawnExplosion orig, Vector3 explosionPosition, bool spawnExplosionEffect, float killRange, float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab, bool goThroughCar) =>
            {
                orig.Invoke(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab, goThroughCar);

                if (!RoundManager.Instance.IsServer) { return; }

                try
                {
                    foreach (PortalScript p in PortalScript.instances)
                    {
                        var g = p.gameObject;

                        if (Vector3.Distance(g.transform.position, explosionPosition) < 4f)
                        {
                            Logger.LogInfo("Moai Portal: Checking nearby objects to quantumfy");
                            // quantumfy nearby objects
                            var objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>(false);
                            for (int i = 0; i < objects.Length; i++)
                            {
                                var obj = objects[i];
                                if (Vector3.Distance(obj.transform.position, explosionPosition) < 4f && obj.isActiveAndEnabled)
                                {
                                    Logger.LogInfo("Moai Portal: Object found: " + obj.name + " ... quantumfying");
                                    if (!obj.transform.GetComponentInChildren<QuantumItemScript>())
                                    {
                                        // instantiate quantum object and add to parent
                                        var quantumObj = UnityEngine.Object.Instantiate<GameObject>(getQuantumItemRegisteredPrefab());
                                        quantumObj.GetComponent<NetworkObject>().Spawn();

                                        quantumLoop(obj, quantumObj);
                                    }
                                }
                            }
                        }
                    }

                    var quantumScripts = UnityEngine.Object.FindObjectsOfType<QuantumItemScript>();
                    foreach (QuantumItemScript q in quantumScripts)
                    {
                        var g = q.gameObject;

                        if (Vector3.Distance(g.transform.position, explosionPosition) < 4f)
                        {
                            Logger.LogInfo("Quantum Object: Checking nearby objects to quantumfy");
                            // quantumfy nearby objects
                            var objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>(false);
                            for (int i = 0; i < objects.Length; i++)
                            {
                                var obj = objects[i];
                                if (Vector3.Distance(obj.transform.position, explosionPosition) < 4f && obj.isActiveAndEnabled)
                                {
                                    Logger.LogInfo("Moai Portal: Object found: " + obj.name + " ... quantumfying");
                                    if (!obj.transform.GetComponentInChildren<QuantumItemScript>())
                                    {
                                        // instantiate quantum object and add to parent
                                        var quantumObj = UnityEngine.Object.Instantiate<GameObject>(getQuantumItemRegisteredPrefab());
                                        quantumObj.GetComponent<NetworkObject>().Spawn();

                                        quantumLoop(obj, quantumObj);
                                    }
                                }
                            }
                        }
                    }

                    var nukeScripts = UnityEngine.Object.FindObjectsOfType<NuclearBomb>();
                    foreach (var nuke in nukeScripts)
                    {
                        if (Vector3.Distance(explosionPosition, nuke.transform.position) < 7.2f)
                        {
                            nuke.dangerLevel += 2;
                            if (RoundManager.Instance.IsHost) { nuke.dangerResultActivateClientRpc(nuke.dangerLevel); }
                        }
                        if (Vector3.Distance(explosionPosition, nuke.transform.position) < 4f)
                        {
                            nuke.dangerLevel += 2;
                            if (RoundManager.Instance.IsHost) { nuke.dangerResultActivateClientRpc(nuke.dangerLevel); }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            };

            // we need to initialize a Weather Manager at game start
            On.StartOfRound.Awake += (On.StartOfRound.orig_Awake orig, global::StartOfRound self) =>
            {
                orig.Invoke(self);

                try
                {
                    if (StartOfRound.Instance.IsHost && !weatherManager && weatherManager.GetComponent<NetworkObject>() && weatherManager.GetComponent<NetworkObject>().IsSpawned)
                    {
                        initWeatherManager();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Easter Island: Error starting weather manager! Custom EI weathers will not load!");
                    Debug.LogError(e.ToString());
                }
            };

            // resync hook for weather manager
            On.StartOfRound.OnClientConnect += (On.StartOfRound.orig_OnClientConnect orig, global::StartOfRound self, ulong clientId) =>
            {
                // weather script should not exist anymore
                try
                {
                    if (weatherScript && !weatherScript.gameObject) { UnityEngine.Object.Destroy(weatherScript); }
                    if (weatherScript && weatherScript.gameObject) { GameObject.Destroy(weatherScript.gameObject); }
                }
                catch (Exception e) { }

                orig.Invoke(self, clientId);
            };

            On.StartOfRound.SetPlanetsWeather += (On.StartOfRound.orig_SetPlanetsWeather orig, global::StartOfRound self, int connectedPlayersOnServer) =>
            {
                orig.Invoke(self, connectedPlayersOnServer);

                // weather tick (asynchronous for connections)
                weatherManagerBoot();
            };

            // route cost adjustment
            On.Terminal.BeginUsingTerminal += (On.Terminal.orig_BeginUsingTerminal orig, global::Terminal self) =>
            {
                terminal = self;
                // node price change
                try
                {
                    if (easterislandNode)
                    {
                        easterislandNode.itemCost = easterIslandCost.Value;
                    }
                    else
                    {
                        attemptGetIslandNode();
                        if (easterislandNode)
                        {
                            easterislandNode.itemCost = easterIslandCost.Value;
                        }

                        if (easterrouteNode)
                        {
                            easterrouteNode.itemCost = easterIslandCost.Value;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                orig.Invoke(self);
            };

            controls = new InputMap();
            JetPackFix.setupFix();
            ArenaSetup.Start();  // for company Arena Setup (Company Easter Egg)
        }

        // avoids startup issues from people connecting in
        public async void weatherManagerBoot()
        {
            int attempts = 10;

            // weather tick
            try
            {
                if (RoundManager.Instance.IsHost)
                {
                    if (!weatherScript) { initWeatherManager(); }
                    else
                    {
                        weatherScript.weatherTickServerOnly();
                    }
                }
            }
            catch (Exception e)
            {
                await Task.Delay(1000);
                attempts--;
                if (attempts != 0)
                {
                    weatherManagerBoot();
                }
                else
                {
                    Debug.LogError("Easter Island: Error starting weather manager! Custom EI weathers will not load!");
                    Debug.LogError(e.ToString());
                }
            }
        }

        public GameObject getQuantumItemRegisteredPrefab()
        {
            List<Item> items = StartOfRound.Instance.allItemsList.itemsList;
            foreach (Item it in items)
            {
                if (it.itemId.Equals(quantumPropertyIDMatch))
                {
                    return it.spawnPrefab;
                }
            }
            return null;
        }

        public async void initWeatherManager()
        {
            weatherManager = UnityEngine.Object.Instantiate<GameObject>(weatherManagerAsset);
            var netObj = weatherManager.GetComponent<NetworkObject>();
            netObj.Spawn();
            weatherScript = weatherManager.GetComponent<EIWeatherManager>();

            int timeOut = 100;
            while (!netObj.IsSpawned)
            {
                await Task.Delay(100); // 100ms per tick = 10s max wait
                timeOut--;
                if (timeOut <= 0)
                {
                    Debug.LogError("EasterIsland: WeatherManager failed to spawn after waiting.");
                    return;
                }
            }

            Debug.Log("EasterIsland: WeatherManager successfully spawned.");
            weatherScript.weatherTickServerOnly();
        }


        public async void quantumLoop(GrabbableObject obj, GameObject quantumObj)
        {
            Logger.LogInfo("Moai Portal: Awaiting Spawn of quantum object");
            while (!quantumObj.GetComponent<NetworkObject>().IsSpawned)
            {
                Logger.LogInfo("Moai Portal: Awaiting Spawn of quantum object...");
                await Task.Delay(200);
            }
            Logger.LogInfo("Moai Portal: Quantum Object spawned: now initializing variables for all clients");
            quantumObj.GetComponent<QuantumItemScript>().initClientRpc(obj.NetworkObject.NetworkObjectId);
        }

        public static void attemptGetIslandNode()
        {
            //Logger.LogInfo("Attempting to get Easter Island node...");
            var nodes = UnityEngine.Object.FindObjectsOfType<TerminalNode>();
            foreach (var node in nodes)
            {
                if (node.name.ToLower().Equals("easterislandroute"))
                {
                    //Logger.LogInfo("Success!");
                    easterislandNode = node;
                    break;
                }
                if (node.name.ToLower().Equals("easterislandrouteconfirm"))
                {
                    //Logger.LogInfo("Success!");
                    easterrouteNode = node;
                    break;
                }
            }
        }

        // SETTINGS SECTION
        // consider these multipliers for existing values
        public static ConfigEntry<int> easterIslandCost;
        public static ConfigEntry<float> nightfallChance;
        public static ConfigEntry<float> quantumStormChance;
        public static ConfigEntry<bool> spawnOverride;

        public void bindVars()
        {
            easterIslandCost = Config.Bind("Global", "Easter Island Moon Cost", 650, "Cost to travel to the moon. ");
            nightfallChance = Config.Bind("Global", "Nightfall Weather Chance", 15f, "% Chance for nightfall to occur on Easter Island.");
            quantumStormChance = Config.Bind("Global", "Quantum Storm Chance", 0f, "% Chance for a quantum storm to occur on Easter Island.");
            spawnOverride = Config.Bind("Global", "Only spawn Moai on Easter Island.", false, "Ensures that moai only spawn on the Easter Island moon. Overrides all settings on moai enemy.");

            var costEntry = new IntInputFieldConfigItem(easterIslandCost, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100000000,
            });

            var nightfallChanceEntry = new FloatInputFieldConfigItem(nightfallChance, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0f,
                Max = 100f,
            });

            var quantumStormChanceEntry = new FloatInputFieldConfigItem(quantumStormChance, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0f,
                Max = 100f,
            });

            LethalConfigManager.AddConfigItem(costEntry);
            LethalConfigManager.AddConfigItem(nightfallChanceEntry);
            LethalConfigManager.AddConfigItem(quantumStormChanceEntry);
        }

        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            easterislandBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "eastermoonnetobjects"));
            heavenNetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "easterheavennetobjs"));
            try
            {
                HeavenLoader.heavenBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "easterislandheavenscene"));
            }
            catch (Exception e) { Debug.LogError("Easter Island Heaven Bundle Error"); }
            Debug.Log("Easter Heaven Bundle::: " + HeavenLoader.heavenBundle);

            // heaven net prefab registering
            HeavenLoader.LoadHeavenAssets();
        }
    }
}