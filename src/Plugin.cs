using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using BepInEx.Logging;
using System.IO;
using BepInEx.Configuration;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalConfig;
using static UnityEngine.GraphicsBuffer;
using System;
using UnityEngine.AI;
using GameNetcodeStuff;

namespace ExampleEnemy
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin {
        public static Harmony _harmony; 
        public static EnemyType ExampleEnemy;
        public static new ManualLogSource Logger;

        public void LogIfDebugBuild(string text)
        {
        #if DEBUG
            Plugin.Logger.LogInfo(text);
        #endif
        }

        private void Awake() {
            Logger = base.Logger;
            Assets.PopulateAssets();
            bindVars();

            // asset loading phase
            var ExampleEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("MoaiEnemy");
            var tlTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("MoaiEnemyTN");
            var tlTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("MoaiEnemyTK");

            var MoaiBlue = Assets.MainAssetBundle.LoadAsset<EnemyType>("MoaiBlue");
            var MoaiBlueTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("MoaiBlueTN");
            var MoaiBlueTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("MoaiBlueTK");

            // debug phase
            Debug.Log("EX BUNDLE: " + Assets.MainAssetBundle.ToString());
            Debug.Log("EX ENEMY: " + ExampleEnemy);
            Debug.Log("EX TK: " + tlTerminalKeyword);
            Debug.Log("EX TN: " + tlTerminalNode);
            Debug.Log("BLUE ENEMY: " + ExampleEnemy);
            Debug.Log("BLUE TK: " + tlTerminalKeyword);
            Debug.Log("BLUE TN: " + tlTerminalNode);

            // register phase 
            NetworkPrefabs.RegisterNetworkPrefab(ExampleEnemy.enemyPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(MoaiBlue.enemyPrefab);

            // rarity range is 0-100 normally
            RegisterEnemy(ExampleEnemy, (int)(20 / moaiGlobalRarity.Value), LevelTypes.All, SpawnType.Daytime, tlTerminalNode, tlTerminalKeyword);
            RegisterEnemy(MoaiBlue, (int)(27 / moaiGlobalRarity.Value), LevelTypes.All, SpawnType.Outside, MoaiBlueTerminalNode, MoaiBlueTerminalKeyword);  // not common enough
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Required by https://github.com/EvaisaDev/UnityNetcodePatcher maybe?
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            Debug.Log("MOAI: Registering Moai Net Messages");
            // bind network messages
            LC_API.Networking.Network.RegisterMessage<moaiSoundPkg>("moaisoundplay", true, (long_identifier, moaiPkg) =>
            {
                // ai.NetworkObjectId synchronizes across moai
                ExampleEnemyAI target = null;
                Debug.Log("MOAI: received moaisound pkg from host: " + moaiPkg.netId.ToString() + " :: " + moaiPkg.soundName);
                ExampleEnemyAI[] moais = GameObject.FindObjectsOfType<ExampleEnemyAI>();
                for (int i = 0; i < moais.Length; i++)
                {
                    ExampleEnemyAI ai = moais[i];
                    if (ai.NetworkObjectId == moaiPkg.netId)
                    {
                        target = ai;
                    }
                }
                if (target == null)
                {
                    Debug.LogError("moaisoundplay call failed:: " + moaiPkg.netId.ToString() + " :: " + moaiPkg.soundName);
                    return;
                }

                switch (moaiPkg.soundName)
                {
                    case "creatureSFX":
                        target.stopAllSound();
                        target.creatureSFX.Play();
                        break;
                    case "creatureVoice":
                        target.stopAllSound();
                        target.creatureVoice.Play();
                        break;
                    case "creatureFood":
                        target.creatureSFX.Stop();
                        target.creatureVoice.Stop();
                        target.creatureFood.Play();
                        break;
                    case "creatureEat":
                        Debug.Log("Calling creatureEat on " + target + " :: " + target.creatureEat);
                        target.creatureSFX.Stop();
                        target.creatureVoice.Stop();
                        target.creatureEat.Play();
                        break;
                    case "creatureEatHuman":
                        Debug.Log("Calling creatureEatHuman on " + target + " :: " + target.creatureEatHuman);
                        target.creatureSFX.Stop();
                        target.creatureVoice.Stop();
                        target.creatureEatHuman.Play();
                        break;
                }
            });

            LC_API.Networking.Network.RegisterMessage<moaiSizePkg>("moaisizeset", true, (long_identifier, moaiSizePkg) =>
            {
                ExampleEnemyAI target = null;
                Debug.Log("MOAI: received moaisize pkg from host: " + moaiSizePkg.netId.ToString() + " :: " + moaiSizePkg.size);
                ExampleEnemyAI[] moais = GameObject.FindObjectsOfType<ExampleEnemyAI>();
                for (int i = 0; i < moais.Length; i++)
                {
                    ExampleEnemyAI ai = moais[i];
                    if (ai.NetworkObjectId == moaiSizePkg.netId)
                    {
                        target = ai;
                    }
                }
                if (target == null)
                {
                    Debug.LogError("moaisizeset call failed:: " + moaiSizePkg.netId.ToString() + " :: " + moaiSizePkg.size);
                    return;
                }
                target.gameObject.transform.localScale *= moaiSizePkg.size;
                target.gameObject.GetComponent<NavMeshAgent>().height *= moaiSizePkg.size;

                target.creatureSFX.pitch /= moaiSizePkg.pitchAlter;
                target.creatureVoice.pitch /= moaiSizePkg.pitchAlter;

                target.creatureFood = target.grabSource("CreatureFood");
                target.creatureEat = target.grabSource("CreatureEat");
                target.creatureEatHuman = target.grabSource("CreatureEatHuman");

                target.creatureFood.pitch /= moaiSizePkg.pitchAlter;
                target.creatureEat.pitch /= moaiSizePkg.pitchAlter;
                target.creatureEatHuman.pitch /= moaiSizePkg.pitchAlter;
            });

            LC_API.Networking.Network.RegisterMessage<moaiAttachBodyPkg>("moaiattachbody", true, (long_identifier, moaiAttachBodyPkg) =>
            {
                ExampleEnemyAI target = null;
                Debug.Log("MOAI: received moaiattachbody pkg from host: " + moaiAttachBodyPkg.netId.ToString() + " :: " + moaiAttachBodyPkg.humanNetId);
                ExampleEnemyAI[] moais = GameObject.FindObjectsOfType<ExampleEnemyAI>();
                for (int i = 0; i < moais.Length; i++)
                {
                    ExampleEnemyAI ai = moais[i];
                    if (ai.NetworkObjectId == moaiAttachBodyPkg.netId)
                    {
                        target = ai;
                    }
                }
                if (target == null)
                {
                    Debug.LogError("moaisizeset call failed:: " + moaiAttachBodyPkg.netId.ToString() + " :: " + moaiAttachBodyPkg.humanNetId);
                    return;
                }

                for (int i = 0; i < RoundManager.Instance.playersManager.allPlayerScripts.Length; i++)
                {
                    PlayerControllerB player = RoundManager.Instance.playersManager.allPlayerScripts[i];

                    if (player != null && player.name != null && player.transform != null)
                    {
                        if (player.NetworkObject.NetworkObjectId == moaiAttachBodyPkg.humanNetId)
                        {
                            Debug.Log("MOAI: Successfully attached body with id = " + moaiAttachBodyPkg.humanNetId);
                            player.deadBody.attachedLimb = player.deadBody.bodyParts[5];
                            player.deadBody.attachedTo = target.eye.transform;
                            player.deadBody.canBeGrabbedBackByPlayers = true;
                        }
                    }
                }

            });
        }

        [Serializable]
        public class moaiSoundPkg
        {
            public ulong netId { get; set; }
            public string soundName { get; set; }

            public moaiSoundPkg(ulong _netId, string _soundName)
            {
                this.netId = _netId;
                this.soundName = _soundName;
            }
        }

        [Serializable]
        public class moaiSizePkg
        {
            public ulong netId { get; set; }
            public float size { get; set; }

            public float pitchAlter { get; set; }

            public moaiSizePkg(ulong _netId, float _size, float _pitchAlter)
            {
                this.netId = _netId;
                this.size = _size;
                this.pitchAlter = _pitchAlter;
            }
        }

        [Serializable]
        public class moaiAttachBodyPkg
        {
            public ulong netId { get; set; }
            public ulong humanNetId { get; set; }

            public moaiAttachBodyPkg(ulong _netId, ulong _humanNetId)
            {
                this.netId = _netId;
                this.humanNetId = _humanNetId;
            }
        }

        // SETTINGS SECTION
        // consider these multipliers for existing values
        public static ConfigEntry<float> moaiGlobalSize;
        public static ConfigEntry<float> moaiGlobalSizeVar;
        public static ConfigEntry<float> moaiGlobalMusicVol;
        public static ConfigEntry<float> moaiGlobalVoiceVol;
        public static ConfigEntry<float> moaiGlobalRarity;
        public static ConfigEntry<float> moaiGlobalSpeed;

        public void bindVars()
        {
            moaiGlobalMusicVol = Config.Bind("Global", "Chase Sound Volume", 0.6f, "Changes the volume of the MOAHHHH sound during chase. Also affects all chase sound variants.");
            moaiGlobalVoiceVol = Config.Bind("Global", "Idle Sound Volume", 0.6f, "Changes the volume of moai sounds when they aren't chasing you. Changing this could make moai more or less sneaky.");
            moaiGlobalSizeVar = Config.Bind("Global", "Size Variant Chance", 0.2f, "The chance of a moai to spawn in a randomly scaled size. Affects their pitch too.");
            moaiGlobalSize = Config.Bind("Global", "Size Multiplier", 1f, "Changes the size of all moai models. Scales pretty violently. Affects SFX pitch.");
            moaiGlobalRarity = Config.Bind("Global", "Enemy Rarity Multiplier", 1f, "How rare are moai? A 2x multiplier makes them 2x more rare, and a 0.25x multiplier would make them 4x more common.");
            moaiGlobalSpeed = Config.Bind("Global", "Enemy Speed Multiplier", 1f, "Changes the speed of all moai. 4x would mean they are 4 times faster, 0.5x would be 2 times slower.");

            var sizeSlider = new FloatSliderConfigItem(moaiGlobalSize, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = 0.05f,
                Max = 5f
            });

            var sizeVarSlider = new FloatSliderConfigItem(moaiGlobalSizeVar, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = 0f,
                Max = 1f
            });


            var volumeSlider = new FloatSliderConfigItem(moaiGlobalMusicVol, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = 0.0f,
                Max = 1f
            });

            var volume2Slider = new FloatSliderConfigItem(moaiGlobalVoiceVol, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = 0.0f,
                Max = 1f
            });

            var raritySlider = new FloatSliderConfigItem(moaiGlobalRarity, new FloatSliderOptions
            {
                RequiresRestart = true,
                Min = 0.05f,
                Max = 10f
            });

            var speedSlider = new FloatSliderConfigItem(moaiGlobalSpeed, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = 0.0f,
                Max = 5f,
            });
            
            LethalConfigManager.AddConfigItem(volumeSlider);
            LethalConfigManager.AddConfigItem(volume2Slider);
            LethalConfigManager.AddConfigItem(sizeSlider);
            LethalConfigManager.AddConfigItem(sizeVarSlider);
            LethalConfigManager.AddConfigItem(raritySlider);
            LethalConfigManager.AddConfigItem(speedSlider);
        }
    }

    public static class Assets {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets() {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "moaibundle"));
            if (MainAssetBundle == null) {
                Plugin.Logger.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}