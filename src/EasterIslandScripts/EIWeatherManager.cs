using EasterIsland.src.EasterIslandScripts.Technical;
using EasterIsland.src.EasterIslandScripts.Weather;
using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts
{
    // patch to replace weather display in console
    // interrupts weather registry display without assembly req.
    [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
    public class Patch_Terminal_WeatherDisplay
    {
        static void Postfix(ref string __result, TerminalNode node)
        {
            Debug.Log("Legend Of The Moai: Post Process Terminal Patch");
            if (node != null)
            {

                if (__result.ToLower().Contains("company"))  // has to be in the moon list with easter island
                {
                    string[] lines = __result.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("Easter Island") && EIWeatherManager.assignedWeather != "")
                        {
                            lines[i] = "Easter Island (" + EIWeatherManager.assignedWeather + ")";
                            Debug.Log("Patched line to: " + lines[i]);
                        }
                    }
                    __result = string.Join("\n", lines);
                }
            }
        }
    }

    public class EIWeatherManager : NetworkBehaviour
    {
        public int currentMapSeed = 0;  // we use the map seed to check if the weather needs updating
        public static System.Random random = new System.Random();
        public double[] nightFallChance;
        public double[] fluxChance;
        public double[] sunnyChance;
        public static string assignedWeather = "";

        // host vars
        public static int hostVar1 = -1;
        public static int hostVar2 = -1;

        [ServerRpc(RequireOwnership = false)]
        public void weatherTickServerRpc()
        {
            weatherTickServerOnly();
            serverUpdateWeatherClientRpc(assignedWeather, hostVar1, hostVar2);
        }

        // just update the weather for all clients.
        [ClientRpc]
        public void serverUpdateWeatherClientRpc(string weatherName, int var1, int var2)
        {
            modifyWeatherClientRpc(weatherName, var1, var2);
        }

        public void weatherTickServerOnly()
        {
            if (!RoundManager.Instance.IsHost) { return; }

            // node weather change
            if (currentMapSeed != StartOfRound.Instance.randomMapSeed)
            {
                var roll = random.NextDouble();
                if (roll < Plugin.nightfallChance.Value / 100)
                {
                    modifyWeatherClientRpc("Night Fall", 1, 2);
                }
                else if (roll > Plugin.nightfallChance.Value / 100 && roll < (Plugin.quantumStormChance.Value / 100 + Plugin.nightfallChance.Value / 100))
                {
                    modifyWeatherClientRpc("Quantum Storm", 1, 2);
                }
                else
                {
                    clearCustomWeathersClientRpc();
                }
            }
            else
            {
                serverUpdateWeatherClientRpc(assignedWeather, hostVar1, hostVar2);
            }
            currentMapSeed = StartOfRound.Instance.randomMapSeed;
        }

        [ClientRpc]
        public void clearCustomWeathersClientRpc()
        {
            assignedWeather = "";
        }

        [ClientRpc]
        public void modifyWeatherClientRpc(string weatherName, int var1, int var2)
        {
            assignedWeather = weatherName;
            hostVar1 = var1;
            hostVar2 = var2;

            if (!Plugin.terminal)
                Plugin.terminal = FindObjectsOfType<Terminal>()[0];

            var moonList = Plugin.terminal.moonsCatalogueList;
            foreach (SelectableLevel level in moonList)
            {
                string planetName = level.PlanetName.ToLower();
                if (planetName.Contains("easter") && planetName.Contains("island"))
                {
                    Debug.Log("LegendOfTheMoai: stabilized weather.");
                    level.currentWeather = LevelWeatherType.None;

                    var weatherManagerType = Type.GetType("WeatherRegistry.WeatherManager, WeatherRegistry");
                    if (weatherManagerType != null)
                    {
                        WeatherRegistryCompatibility.ForceClearWeather(level);
                    }
                }
            }
        }
    }
}
