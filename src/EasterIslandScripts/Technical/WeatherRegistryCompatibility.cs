using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    public static class WeatherRegistryCompatibility
    {
        // we force the weather to be clear for weatherRegistry
        // used for custom weathers
        public static void ForceClearWeather(SelectableLevel level)
        {
            // Step 1: Get WeatherManager type
            var weatherManagerType = Type.GetType("WeatherRegistry.WeatherManager, WeatherRegistry");
            if (weatherManagerType == null)
            {
                Debug.LogWarning("LegendOfTheMoai: WeatherManager type not found.");
                return;
            }

            // Step 2: Get static field CurrentWeathers
            var currentWeathersField = weatherManagerType.GetField("CurrentWeathers", BindingFlags.Static | BindingFlags.Public);
            var currentWeathersInstance = currentWeathersField?.GetValue(null);
            if (currentWeathersInstance == null)
            {
                Debug.LogWarning("LegendOfTheMoai: CurrentWeathers instance not found.");
                return;
            }

            // Step 3: Access public Entries property
            var entriesProp = currentWeathersInstance.GetType().GetProperty("Entries", BindingFlags.Public | BindingFlags.Instance);
            var entries = entriesProp?.GetValue(currentWeathersInstance) as IDictionary;
            if (entries == null)
            {
                Debug.LogWarning("LegendOfTheMoai: Entries property not found or not a dictionary.");
                return;
            }

            // Step 4: Set weather to None (-1) for your level
            const int weatherNone = -1; // LevelWeatherType.None
            if (entries.Contains(level))
                entries[level] = weatherNone;
            else
                entries.Add(level, weatherNone);

            Debug.Log("LegendOfTheMoai: Forced WeatherRegistry.CurrentWeathers entry to None.");

            // refresh screen immediately to show clear weather
            // linked to harmony patch from WeatherRegistry (updates ship screen) 
            StartOfRound.Instance.SetMapScreenInfoToCurrentLevel();
        }
    }

    [HarmonyPatch(typeof(StartOfRound))]
    public static class WeatherRegScreenPath
    { 
        [HarmonyPatch("SetMapScreenInfoToCurrentLevel")]
        [HarmonyPostfix]
        [HarmonyPriority(200)]
        internal static void GameMethodPatch(ref TextMeshProUGUI ___screenLevelDescription, ref SelectableLevel ___currentLevel, StartOfRound __instance)
        {
            var weatherManagerType = Type.GetType("WeatherRegistry.WeatherManager, WeatherRegistry");

            // weather reg must exist and the selected level must be EI
            if (weatherManagerType != null && ___currentLevel.PlanetName.ToLower().Contains("easter") && ___currentLevel.PlanetName.ToLower().Contains("island"))
            {
                // weather reg exists, time to patch
                lazyOverride(___currentLevel);
            }
        }

        public static async void lazyOverride(SelectableLevel currentLevel)
        {
            await Task.Delay(500);

            if (EIWeatherManager.assignedWeather.ToLower().Contains("night"))  // nightfall
            {
                Debug.Log("LegendOfTheMoai: Screen Display Override to NIGHTFALL (Weather Registry Compatibility)");
                Regex multiNewLine = new Regex("\\n{2,}");
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("ORBITING: " + currentLevel.PlanetName + "\n");
                stringBuilder.Append("WEATHER:  <color=#1613bd>NightFall</color> \n");
                stringBuilder.Append(multiNewLine.Replace(currentLevel.LevelDescription, "\n") ?? "");
                StartOfRound.Instance.screenLevelDescription.SetText(stringBuilder.ToString(), true);
            }


            if (EIWeatherManager.assignedWeather.ToLower().Contains("quantum"))  // quantum storm
            {
                Debug.Log("LegendOfTheMoai: Screen Display Override to QUANTUM STORM (Weather Registry Compatibility)");
                Regex multiNewLine = new Regex("\\n{2,}");
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("ORBITING: " + currentLevel.PlanetName + "\n");
                stringBuilder.Append("WEATHER:  <color=#fd4ebe>Quantum Storm</color> \n");
                stringBuilder.Append(multiNewLine.Replace(currentLevel.LevelDescription, "\n") ?? "");
                StartOfRound.Instance.screenLevelDescription.SetText(stringBuilder.ToString(), true);
            }
        }
    }
}